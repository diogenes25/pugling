using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-735: a plan without an end date (an open frame of material, the exam date is not known yet) is read like a running
/// plan everywhere and never gets an invented date. Today is Monday 2026-11-30 (see <see cref="LerntageWebAppFactory"/>).
/// Each case names the change that turns it red.
/// </summary>
public class OffenerPlanTests : IClassFixture<LerntageWebAppFactory>
{
    private static readonly DateOnly Today = new(2026, 11, 30);
    private readonly LerntageWebAppFactory factory;

    public OffenerPlanTests(LerntageWebAppFactory factory)
    {
        this.factory = factory;
        factory.Clock.FreezeAt(new DateTimeOffset(2026, 11, 30, 12, 0, 0, TimeSpan.Zero));
    }

    private sealed record Setup(HttpClient Father, HttpClient Child, int ChildId, int PlanId, int PositionId);

    // A fresh child with one plan of one daily position, started `startDaysAgo` days ago, open unless `end` is given.
    private async Task<Setup> OpenPlanAsync(int startDaysAgo = 3, DateOnly? end = null, int penalty = 0,
        (string Front, string Back)[]? words = null)
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "7" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Offen-Kind"), pin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, words ?? []);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId);
        var start = Today.AddDays(-startDaysAgo);
        Write(db =>
        {
            var plan = db.StudyPlans.Single(p => p.Id == planId);
            plan.StartDate = start;
            plan.ActivatedAt = start;
            plan.EndDate = end;
            var pos = db.PlanPositions.Single(p => p.Id == positionId);
            pos.CreatedAt = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            pos.PenaltyCoins = penalty;
            // A failed test on the start day starts the penalty clock (B-535) without meeting the goal.
            if (penalty > 0)
            {
                var at = start.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc);
                db.TestAttempts.Add(new TestAttempt
                {
                    StudyPlanId = planId,
                    PlanPositionId = positionId,
                    Day = start,
                    StageValue = (int)TestStage.FreeText,
                    Graded = true,
                    StartedAt = at,
                    CompletedAt = at,
                    TotalItems = 2,
                });
            }
        });
        var child = await TestApi.ChildAsync(factory, childId, pin);
        return new Setup(father, child, childId, planId, positionId);
    }

    private void Write(Action<PuglingDbContext> change)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        change(db);
        db.SaveChanges();
    }

    private T Read<T>(Func<PuglingDbContext, T> read)
    {
        using var scope = factory.Services.CreateScope();
        return read(scope.ServiceProvider.GetRequiredService<PuglingDbContext>());
    }

    private static bool IsNull(JsonElement e, string prop) => e.GetProperty(prop).ValueKind == JsonValueKind.Null;

    /// <summary>
    /// AK 1 and 5, supervisor and child lists and reads: an open plan answers <c>endDate: null</c> and is playable. Red when a
    /// read compares the null end as "before today" (not playable) or fills a placeholder date.
    /// </summary>
    [Fact]
    public async Task Offener_Plan_Wird_Von_Vater_Und_Kind_Ohne_Datum_Gelesen()
    {
        var s = await OpenPlanAsync();

        var asFather = await s.Father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/study-plans/{s.PlanId}");
        Assert.True(IsNull(asFather, "endDate"));
        Assert.True(asFather.GetProperty("isPlayable").GetBoolean());

        var fatherList = await s.Father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/study-plans?childId={s.ChildId}");
        var inList = fatherList.EnumerateArray().Single(p => p.GetProperty("id").GetInt32() == s.PlanId);
        Assert.True(IsNull(inList, "endDate"));
        Assert.True(inList.GetProperty("isPlayable").GetBoolean());

        var childList = await s.Child.GetFromJsonAsync<JsonElement>("/api/v1/supervisor/study-plans");
        Assert.Contains(childList.EnumerateArray(), p => p.GetProperty("id").GetInt32() == s.PlanId);

        var studentList = await s.Child.GetFromJsonAsync<JsonElement>("/api/v1/student/study-plans");
        Assert.Contains(studentList.EnumerateArray(), p => p.GetProperty("id").GetInt32() == s.PlanId);
    }

    /// <summary>
    /// AK 1 and 5, the child's day, overview and progress: the position is on today's list, the overview carries no end date
    /// and the progress counts the days run so far (start to today = 4). Red when the day query or the progress view treats
    /// the null end as past, or the total is not bounded by today.
    /// </summary>
    [Fact]
    public async Task Offener_Plan_Erscheint_Im_Tag_Und_Zaehlt_Die_Gelaufenen_Tage()
    {
        var s = await OpenPlanAsync(startDaysAgo: 3);

        var today = await s.Father.GetFromJsonAsync<JsonElement>($"/api/v1/student/children/{s.ChildId}/today");
        Assert.Contains(today.GetProperty("positions").EnumerateArray(), p => p.GetProperty("positionId").GetInt32() == s.PositionId);

        var overview = await s.Child.GetFromJsonAsync<JsonElement>($"/api/v1/student/study-plans/{s.PlanId}/overview");
        Assert.True(IsNull(overview, "endDate"));

        var progress = await s.Child.GetFromJsonAsync<JsonElement>($"/api/v1/student/study-plans/{s.PlanId}/overview/progress");
        Assert.True(IsNull(progress, "endDate"));
        Assert.Equal(4, progress.GetProperty("totalDays").GetInt32());
        // Red when the day loop of `ProgressAsync` skips the open plan: the list would be empty.
        Assert.Equal(4, progress.GetProperty("days").GetArrayLength());
        Assert.All(progress.GetProperty("days").EnumerateArray(), d => Assert.True(
            DateOnly.Parse(d.GetProperty("day").GetString()!) <= Today));
    }

    /// <summary>AK 1: the child may play an open plan. Red when the playability check compares the null end.</summary>
    [Fact]
    public async Task Kind_Darf_Einen_Offenen_Plan_Spielen()
    {
        var s = await OpenPlanAsync();
        var res = await s.Child.PostAsJsonAsync(TestApi.PracticeBase(s.PlanId, s.PositionId), new { });
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// AK 4, settlement: an open plan runs until today and books the missed daily duty of each closed duty day once, none for
    /// today. Started Fri 27th; Sunday the 29th is free, so Friday and Saturday are booked. Red when the
    /// settlement loop skips the plan (nothing booked) or runs past today.
    /// </summary>
    [Fact]
    public async Task Offener_Plan_Rechnet_Die_Malus_Bis_Heute_Ab()
    {
        var s = await OpenPlanAsync(startDaysAgo: 3, penalty: 5);
        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<PositionProgressService>()
                .SettleClosedPeriodsAsync(s.ChildId, Today, CancellationToken.None);
        }
        var days = Read(db => db.PositionGoalPenalties.Where(p => p.PlanPositionId == s.PositionId)
            .Select(p => p.PeriodStart).OrderBy(d => d).ToList());
        Assert.Equal([Today.AddDays(-3), Today.AddDays(-2)], days);
    }

    /// <summary>
    /// AK 1, streak and complete days over the run: a daily test passed on Friday, Saturday and today (Sunday is free) gives a
    /// streak of 3 and the two closed days as complete days. Red when either loop skips the open plan (streak 0, no days).
    /// </summary>
    [Fact]
    public async Task Offener_Plan_Zaehlt_Serie_Und_Vollstaendige_Tage()
    {
        var s = await OpenPlanAsync(startDaysAgo: 3);
        Write(db =>
        {
            foreach (var day in new[] { Today.AddDays(-3), Today.AddDays(-2), Today })
            {
                var at = day.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc);
                db.TestAttempts.Add(new TestAttempt
                {
                    StudyPlanId = s.PlanId,
                    PlanPositionId = s.PositionId,
                    Day = day,
                    StageValue = (int)TestStage.FreeText,
                    Graded = true,
                    StartedAt = at,
                    CompletedAt = at,
                    TotalItems = 2,
                    CorrectItems = 2,
                    ScorePercent = 100,
                    Passed = true,
                });
            }
        });
        using var scope = factory.Services.CreateScope();
        var progress = scope.ServiceProvider.GetRequiredService<PositionProgressService>();
        Assert.Equal(3, await progress.ChildStreakAsync(s.ChildId, Today, int.MaxValue, CancellationToken.None));
        var complete = await progress.CompleteChildDaysAsync(s.ChildId, Today, CancellationToken.None);
        Assert.Contains(Today.AddDays(-3), complete);
        Assert.Contains(Today.AddDays(-2), complete);
    }

    /// <summary>
    /// AK 1, the supervisor's timeline: an open plan's daily goal counts on the days it ran. Red when the plan query or the
    /// per-day filter drops a plan without an end (goals total 0).
    /// </summary>
    [Fact]
    public async Task Offener_Plan_Steht_In_Der_Verlaufsuebersicht()
    {
        var s = await OpenPlanAsync(startDaysAgo: 3);
        var from = Today.AddDays(-3).ToString("yyyy-MM-dd");
        var res = await s.Father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/children/overview?from={from}&childId={s.ChildId}");
        var periods = res.GetProperty("children").EnumerateArray().Single().GetProperty("periods").EnumerateArray().ToList();
        Assert.Equal(4, periods.Count);
        var sunday = Today.AddDays(-1).ToString("yyyy-MM-dd");
        Assert.All(periods.Where(p => p.GetProperty("start").GetString() != sunday),
            p => Assert.True(p.GetProperty("goalsTotal").GetInt32() >= 1));
    }

    /// <summary>
    /// AK 1, the tower game: the position of an open plan is offered and a run starts on it. Red when the overview or the run's
    /// ownership check drops a plan without an end.
    /// </summary>
    [Fact]
    public async Task Offener_Plan_Ist_Im_Turmspiel_Spielbar()
    {
        var s = await OpenPlanAsync(words: [("dog", "Hund"), ("cat", "Katze"), ("house", "Haus"), ("tree", "Baum"), ("water", "Wasser"), ("bread", "Brot")]);
        var baseUrl = $"/api/v1/student/children/{s.ChildId}/tower-game";
        var overview = await s.Child.GetFromJsonAsync<JsonElement>(baseUrl);
        Assert.Contains(overview.GetProperty("positions").EnumerateArray(), p => p.GetProperty("planPositionId").GetInt32() == s.PositionId);
        var run = await s.Child.PostAsJsonAsync($"{baseUrl}/runs", new { planPositionId = s.PositionId });
        Assert.True(run.IsSuccessStatusCode, await run.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// AK 1, the image generator's work list: the words of an open plan are wanted. Red when the demand query drops a plan
    /// without an end (empty list).
    /// </summary>
    [Fact]
    public async Task Offener_Plan_Erzeugt_Bildbedarf()
    {
        var marker = Guid.NewGuid().ToString("N")[..6];
        var s = await OpenPlanAsync(words: [($"dog{marker}", $"Hund{marker}"), ($"cat{marker}", $"Katze{marker}")]);
        var tag = await s.Father.PostAsJsonAsync("/api/v1/creator/interest-tags",
            new { label = $"Fussball {marker}", facet = "Sport", imagePrompt = "on a pitch" });
        tag.EnsureSuccessStatusCode();
        (await s.Father.PutAsJsonAsync($"/api/v1/supervisor/children/{s.ChildId}/interests",
            new { interests = new[] { new { label = $"Fussball {marker}", weight = 3 } } })).EnsureSuccessStatusCode();
        var demand = await s.Father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/media/demand?childId={s.ChildId}");
        Assert.Equal(2, demand.GetArrayLength());
    }

    /// <summary>AK 2: the old way stays - <c>durationDays &lt;= 0</c> without <c>withoutEnd</c> still means an end after 10 days.</summary>
    [Fact]
    public async Task Anlegen_Ohne_WithoutEnd_Behaelt_Das_Enddatum()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Dauer-Kind"), pin = "7123" }));
        var subjectId = await TestApi.DefaultSubjectIdAsync(father);

        var plan = await (await father.PostAsJsonAsync("/api/v1/supervisor/study-plans",
            new { childId, title = "Zehn Tage", subjectId, startDate = "2026-11-30", durationDays = 0 }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2026-12-09", plan.GetProperty("endDate").GetString());
    }

    /// <summary>AK 2: <c>withoutEnd</c> creates an open plan and ignores <c>durationDays</c>; it is playable at once.</summary>
    [Fact]
    public async Task Anlegen_Mit_WithoutEnd_Erzeugt_Einen_Offenen_Plan()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Offen-Anlegen"), pin = "7124" }));
        var subjectId = await TestApi.DefaultSubjectIdAsync(father);

        var res = await father.PostAsJsonAsync("/api/v1/supervisor/study-plans",
            new { childId, title = "Stoff ohne Termin", subjectId, durationDays = 14, withoutEnd = true });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var plan = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(IsNull(plan, "endDate"));
        Assert.True(plan.GetProperty("isPlayable").GetBoolean());
    }

    /// <summary>
    /// AK 3: PATCH sets an end date, <c>clearEndDate</c> removes it, a bare <c>null</c> changes nothing, and both together clear (value
    /// first, then the switch). Red when a <c>null</c> clears the date or the switch is ignored.
    /// </summary>
    [Fact]
    public async Task Patch_Setzt_Leert_Und_Laesst_Das_Enddatum_Unveraendert()
    {
        var s = await OpenPlanAsync();
        var url = $"/api/v1/supervisor/study-plans/{s.PlanId}";

        var set = await (await s.Father.PatchAsJsonAsync(url, new { endDate = "2026-12-20" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2026-12-20", set.GetProperty("endDate").GetString());

        var untouched = await (await s.Father.PatchAsJsonAsync(url, new { endDate = (string?)null, title = "Umbenannt" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2026-12-20", untouched.GetProperty("endDate").GetString());

        var cleared = await (await s.Father.PatchAsJsonAsync(url, new { endDate = "2026-12-21", clearEndDate = true }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(IsNull(cleared, "endDate"));
    }

    /// <summary>
    /// B-783 for an open end: an expired plan (end 5 days ago) that is reopened was not playable in between, so the gap does not
    /// become duty - <c>ActivatedAt</c> moves to today. Red when only a later date, not an open end, frees the gap.
    /// </summary>
    [Fact]
    public async Task Wiedereroeffnen_Eines_Abgelaufenen_Plans_Wirkt_Ab_Heute()
    {
        var s = await OpenPlanAsync(startDaysAgo: 10, end: Today.AddDays(-5));
        var res = await s.Father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{s.PlanId}", new { clearEndDate = true });
        res.EnsureSuccessStatusCode();
        Assert.Equal(Today, Read(db => db.StudyPlans.Single(p => p.Id == s.PlanId).ActivatedAt));
    }
}
