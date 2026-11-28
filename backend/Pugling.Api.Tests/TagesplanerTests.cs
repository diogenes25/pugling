using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-734: the day planner against the real API and database. Today is Monday 2026-11-30, 07:00 (the suite's zone is UTC, so
/// that is past the planner's 6 a.m.); Sunday is free, so the last learning day before today is Saturday the 28th. A child has
/// three positions: A with three due reviews, B failed on Saturday, C with five words never shown. Each case names the change
/// that turns it red.
/// </summary>
public class TagesplanerTests : IClassFixture<LerntageWebAppFactory>
{
    private static readonly DateOnly Today = new(2026, 11, 30);
    private static readonly DateOnly Sunday = Today.AddDays(-1);
    private static readonly DateOnly Saturday = Today.AddDays(-2);
    private static readonly DateOnly Friday = Today.AddDays(-3);
    private static readonly DateOnly Thursday = Today.AddDays(-4);
    private static readonly DateOnly Wednesday = Today.AddDays(-5);

    private const string ChildPin = "8642";

    private readonly LerntageWebAppFactory factory;

    public TagesplanerTests(LerntageWebAppFactory factory)
    {
        this.factory = factory;
        factory.Clock.FreezeAt(At(Today, 7));
    }

    private static DateTimeOffset At(DateOnly day, int hour) =>
        new(day.ToDateTime(new TimeOnly(hour, 0)), TimeSpan.Zero);

    private sealed record Setup(HttpClient Father, int ChildId, int A, int B, int C)
    {
        public string Url => $"/api/v1/supervisor/children/{ChildId}/daily-plans";
    }

    private async Task<Setup> SetupAsync(bool switchOn = true)
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Planer-Kind"), pin = ChildPin }));
        var limits = await father.PatchAsJsonAsync($"/api/v1/supervisor/children/{childId}", new
        {
            dailyTasksMin = 1,
            dailyTasksMax = 5,
            dailyNewWordsMin = 2,
            dailyNewWordsMax = 6,
        });
        Assert.True(limits.IsSuccessStatusCode, await limits.Content.ReadAsStringAsync());

        var words = new[] { ("one", "eins"), ("two", "zwei"), ("three", "drei"), ("four", "vier"), ("five", "fünf") };
        var a = TestApi.SeedLeitnerPosition(factory, await TestApi.CreateVocabExerciseAsync(father, words),
            (int)TestStage.FreeText, childId: childId, introduced: false).positionId;
        var b = TestApi.SeedLeitnerPosition(factory, await TestApi.CreateVocabExerciseAsync(father, words),
            (int)TestStage.FreeText, childId: childId, introduced: false).positionId;
        var c = TestApi.SeedLeitnerPosition(factory, await TestApi.CreateVocabExerciseAsync(father, words),
            (int)TestStage.FreeText, childId: childId, introduced: false).positionId;
        Write(db =>
        {
            foreach (var plan in db.StudyPlans.Where(p => p.ChildId == childId))
            {
                plan.StartDate = Thursday;
                plan.EndDate = Today.AddDays(30);
            }
            // A: three words due today, two due later. B: all five introduced, none due. C: nothing introduced.
            for (var i = 0; i < 5; i++)
            {
                db.PositionItemProgress.Add(Progress(db, a, i, i < 3 ? Today : Today.AddDays(4)));
                db.PositionItemProgress.Add(Progress(db, b, i, Today.AddDays(3)));
            }
            // B on Saturday, the last learning day: two wrong answers, no right one; a row without a verdict does not count.
            var item = db.ExerciseItems.Where(x => x.ExerciseId == TestApi.ExerciseOf(db, b)).OrderBy(x => x.OrderIndex).First();
            Answer(db, childId, b, item, Saturday, false, boxBefore: 3);
            Answer(db, childId, b, item, Saturday, false, boxBefore: 2);
            Answer(db, childId, b, item, Saturday, true, boxBefore: null);
        });
        var s = new Setup(father, childId, a, b, c);
        if (switchOn) await SwitchAsync(s, "Shadow");
        return s;
    }

    private static PositionItemProgress Progress(PuglingDbContext db, int positionId, int index, DateOnly dueOn) =>
        new()
        {
            PlanPositionId = positionId,
            ExerciseId = TestApi.ExerciseOf(db, positionId),
            ItemIndex = index,
            Box = 3,
            DueOn = dueOn,
            IntroducedAt = Thursday,
            ImprintedAt = Thursday,
        };

    private static void Answer(PuglingDbContext db, int childId, int positionId, ExerciseItem item, DateOnly day, bool correct,
        int? boxBefore) =>
        db.ItemReviewEvents.Add(new ItemReviewEvent
        {
            ChildId = childId,
            ItemId = item.Id,
            ExerciseId = item.ExerciseId,
            VocabularyId = item.VocabularyId,
            PlanPositionId = positionId,
            Source = ItemReviewSource.Practice,
            StageValue = (int)TestStage.FreeText,
            WasCorrect = correct,
            BoxBefore = boxBefore,
            Day = day,
            At = day.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc),
        });

    /// <summary>
    /// B-735: the planner reads the positions of an open plan (no end date) like those of a running one. Red when the candidate
    /// query drops a plan without an end (no tasks).
    /// </summary>
    [Fact]
    public async Task Offener_Plan_liefert_dieselben_Aufgaben()
    {
        var s = await SetupAsync(switchOn: false);
        Write(db =>
        {
            foreach (var plan in db.StudyPlans.Where(p => p.ChildId == s.ChildId)) plan.EndDate = null;
        });
        await SwitchAsync(s, "Shadow");

        var tasks = (await TodayPlanAsync(s)).GetProperty("tasks").EnumerateArray().ToList();
        Assert.Equal([s.A, s.B, s.C], tasks.Select(t => t.GetProperty("planPositionId").GetInt32()));
    }

    // A class test of the child in the subject of position C, on `date`; no preparation duty (B-752), so the plan's positions stay.
    private async Task AddExamAsync(Setup s, DateOnly date, int[]? exerciseIds = null, string? status = null, bool wholeSubject = false)
    {
        // By default the test names C's exercise, so it sets a pace; a test without material only orders (B-1034).
        if (!wholeSubject) exerciseIds ??= [Read(db => TestApi.ExerciseOf(db, s.C))];
        var subjectId = Read(db => db.PlanPositions.Where(p => p.Id == s.C).Select(p => p.StudyPlan!.SubjectId ?? p.Exercise!.SeriesUnit!.Series!.SubjectId).Single());
        var res = await s.Father.PostAsJsonAsync("/api/v1/supervisor/class-tests", new
        {
            childId = s.ChildId,
            title = "Arbeit",
            subjectId,
            scheduledDate = date.ToString("yyyy-MM-dd"),
            prepWindowDays = 0,
            exerciseIds,
            status,
        });
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    private async Task<JsonElement> PlanOfAsync(Setup s, DateOnly day) =>
        (await OkJsonAsync(await s.Father.GetAsync(s.Url))).EnumerateArray().Single(p => p.GetProperty("day").GetString() == day.ToString("yyyy-MM-dd"));

    private static string ReasonOf(JsonElement plan, int positionId) =>
        plan.GetProperty("tasks").EnumerateArray().Single(t => t.GetProperty("planPositionId").GetInt32() == positionId)
            .GetProperty("reason").GetString()!;

    /// <summary>
    /// B-680 AK 2 and 8 over the real data: a test tomorrow leaves one learning day for C's five unseen words, so the budget rises
    /// from the floor of two to five and the reason names the test. Red if the planner does not read the class tests.
    /// </summary>
    [Fact]
    public async Task Klassenarbeit_morgen_hebt_die_neuen_Woerter_und_steht_in_der_Begruendung()
    {
        var s = await SetupAsync(switchOn: false);
        await AddExamAsync(s, Today.AddDays(1));
        await SwitchAsync(s, "Shadow");

        var plan = await TodayPlanAsync(s);
        Assert.Equal(5, plan.GetProperty("newWordsBudget").GetInt32());
        Assert.Contains("5 neue Wörter", ReasonOf(plan, s.C));
        Assert.Contains("Klassenarbeit am 01.12., noch 1 Lerntag", ReasonOf(plan, s.C));
    }

    /// <summary>
    /// B-680 AK 4 and 14: a test entered after the day's plan was made does not touch that frozen plan, and the next day's plan
    /// reads it. Red if a read composes today's plan again (budget 5) or the next day ignores the test (budget 2).
    /// </summary>
    [Fact]
    public async Task Nachgetragene_Klassenarbeit_aendert_den_Folgetag_und_nie_den_eingefrorenen_Plan()
    {
        var s = await SetupAsync();
        var before = (await TodayPlanAsync(s)).GetRawText();
        Assert.Equal(2, (await TodayPlanAsync(s)).GetProperty("newWordsBudget").GetInt32());

        await AddExamAsync(s, Today.AddDays(2));

        Assert.Equal(before, (await TodayPlanAsync(s)).GetRawText());
        factory.Clock.FreezeAt(At(Today.AddDays(1), 7));
        Assert.Equal(5, (await PlanOfAsync(s, Today.AddDays(1))).GetProperty("newWordsBudget").GetInt32());
    }

    /// <summary>
    /// B-680 AK 11 over the real data: a near partial test (bundle: C's exercise) dates C, a later subject-wide test dates the
    /// others. Red if only one test counts, or a bundle test is read as subject-wide.
    /// </summary>
    [Fact]
    public async Task Teilpruefung_datiert_nur_ihre_Uebung_die_Klausur_den_Rest()
    {
        var s = await SetupAsync(switchOn: false);
        await AddExamAsync(s, Today.AddDays(2), wholeSubject: true);
        await AddExamAsync(s, Today.AddDays(1), [Read(db => TestApi.ExerciseOf(db, s.C))]);
        await SwitchAsync(s, "Shadow");

        var plan = await TodayPlanAsync(s);
        Assert.Contains("Klassenarbeit am 01.12.", ReasonOf(plan, s.C));
        Assert.Contains("Klassenarbeit am 02.12.", ReasonOf(plan, s.A));
    }

    /// <summary>
    /// B-680 AK 12 over the real data: a timetable entry for tomorrow's weekday marks the subject's tasks, a child without one
    /// has no such mark. Red if the timetable is not read, or read for the wrong weekdays.
    /// </summary>
    [Fact]
    public async Task Stundenplan_von_heute_und_morgen_steht_in_der_Begruendung()
    {
        var with = await SetupAsync(switchOn: false);
        var subjectId = Read(db => db.PlanPositions.Where(p => p.Id == with.C).Select(p => p.StudyPlan!.SubjectId ?? p.Exercise!.SeriesUnit!.Series!.SubjectId).Single());
        var entry = await with.Father.PostAsJsonAsync($"/api/v1/supervisor/children/{with.ChildId}/timetable",
            new { subjectId, dayOfWeek = Today.AddDays(1).DayOfWeek.ToString() });
        Assert.True(entry.IsSuccessStatusCode, await entry.Content.ReadAsStringAsync());
        await SwitchAsync(with, "Shadow");
        var without = await SetupAsync();

        Assert.Contains("Fach laut Stundenplan heute oder morgen", ReasonOf(await TodayPlanAsync(with), with.C));
        Assert.DoesNotContain("Stundenplan", ReasonOf(await TodayPlanAsync(without), without.C));

        // Wednesday is neither today (Monday) nor tomorrow: an entry there marks nothing.
        var other = await SetupAsync(switchOn: false);
        (await other.Father.PostAsJsonAsync($"/api/v1/supervisor/children/{other.ChildId}/timetable",
            new { subjectId, dayOfWeek = Today.AddDays(2).DayOfWeek.ToString() })).EnsureSuccessStatusCode();
        await SwitchAsync(other, "Shadow");
        Assert.DoesNotContain("Stundenplan", ReasonOf(await TodayPlanAsync(other), other.C));
    }

    /// <summary>
    /// B-680 AK 13 over the real data: the subject's share of wrong answers over the last weeks (3 of 4 = 75 %) shows in the
    /// reason. Red if the evaluation is not read, or the share is computed over the wrong window.
    /// </summary>
    [Fact]
    public async Task Schwaeche_des_Fachs_stammt_aus_der_Auswertung()
    {
        var s = await SetupAsync(switchOn: false);
        var subjectId = Read(db => db.PlanPositions.Where(p => p.Id == s.C).Select(p => p.StudyPlan!.SubjectId ?? p.Exercise!.SeriesUnit!.Series!.SubjectId).Single());
        Write(db => db.DailyEvaluations.Add(new DailyEvaluation
        {
            ChildId = s.ChildId,
            Day = Wednesday,
            SubjectId = subjectId,
            CorrectAnswers = 1,
            WrongAnswers = 3,
        }));
        await SwitchAsync(s, "Shadow");

        Assert.Contains("75 % falsche Antworten im Fach", ReasonOf(await TodayPlanAsync(s), s.C));
    }

    /// <summary>
    /// B-680: a test on the planned day itself is the day's last chance and counts (one learning day left); a test already written
    /// does not. Red if today's date is read as past (budget 2) or a written test still presses (budget 5).
    /// </summary>
    [Fact]
    public async Task Heutige_Klassenarbeit_zaehlt_eine_geschriebene_nicht()
    {
        var today = await SetupAsync(switchOn: false);
        await AddExamAsync(today, Today);
        await SwitchAsync(today, "Shadow");
        var written = await SetupAsync(switchOn: false);
        await AddExamAsync(written, Today.AddDays(1), status: "Written");
        await SwitchAsync(written, "Shadow");

        Assert.Equal(5, (await TodayPlanAsync(today)).GetProperty("newWordsBudget").GetInt32());
        Assert.Equal(2, (await TodayPlanAsync(written)).GetProperty("newWordsBudget").GetInt32());
    }

    /// <summary>
    /// B-680: paused days are no learning days on the way to the test - Tuesday to Thursday paused, test on Friday: only today is
    /// left, so all five words are due today. Red if the days up to the test are counted on the calendar instead of the duty days.
    /// </summary>
    [Fact]
    public async Task Pausentage_zaehlen_nicht_als_Lerntage_bis_zur_Klassenarbeit()
    {
        var s = await SetupAsync(switchOn: false);
        (await s.Father.PostAsJsonAsync($"/api/v1/supervisor/children/{s.ChildId}/pauses",
            new { from = Today.AddDays(1), until = Today.AddDays(3) })).EnsureSuccessStatusCode();
        await AddExamAsync(s, Today.AddDays(4));
        await SwitchAsync(s, "Shadow");

        Assert.Equal(5, (await TodayPlanAsync(s)).GetProperty("newWordsBudget").GetInt32());
    }

    /// <summary>
    /// B-680 review: a cancelled test and another child's test press nobody - the budget stays at the floor of two. Red if the
    /// status filter drops Cancelled, or the exam query is not bound to the child.
    /// </summary>
    [Fact]
    public async Task Abgesagte_und_fremde_Klassenarbeiten_druecken_nicht()
    {
        var cancelled = await SetupAsync(switchOn: false);
        await AddExamAsync(cancelled, Today.AddDays(1), status: "Cancelled");
        await SwitchAsync(cancelled, "Shadow");
        var foreign = await SetupAsync(switchOn: false);
        var other = await SetupAsync(switchOn: false);
        await AddExamAsync(other, Today.AddDays(1));
        await SwitchAsync(foreign, "Shadow");

        Assert.Equal(2, (await TodayPlanAsync(cancelled)).GetProperty("newWordsBudget").GetInt32());
        Assert.Equal(2, (await TodayPlanAsync(foreign)).GetProperty("newWordsBudget").GetInt32());
    }

    /// <summary>
    /// B-1034 (answered: no pressure without material): a test without exercises and tags orders by its date and shows in the
    /// reason, but lifts no budget and raises no warning. Red if a test without material counts back from the whole subject.
    /// </summary>
    [Fact]
    public async Task Klassenarbeit_ohne_Uebungen_ordnet_nur_und_setzt_keinen_Druck()
    {
        var s = await SetupAsync(switchOn: false);
        await AddExamAsync(s, Today.AddDays(1), wholeSubject: true);
        await SwitchAsync(s, "Shadow");

        var plan = await TodayPlanAsync(s);
        Assert.Equal(2, plan.GetProperty("newWordsBudget").GetInt32());
        Assert.Contains("Klassenarbeit am 01.12.", ReasonOf(plan, s.C));
        Assert.DoesNotContain(plan.GetProperty("warnings").EnumerateArray(), w => w.GetString()!.Contains("Klassenarbeit"));
    }

    private static async Task SwitchAsync(Setup s, string mode)
    {
        var res = await s.Father.PatchAsJsonAsync($"/api/v1/supervisor/children/{s.ChildId}", new { plannerMode = mode });
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
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

    private static async Task<JsonElement> OkJsonAsync(HttpResponseMessage res)
    {
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<JsonElement> TodayPlanAsync(Setup s) =>
        (await OkJsonAsync(await s.Father.GetAsync(s.Url))).EnumerateArray().Single(p => p.GetProperty("day").GetString() == "2026-11-30");

    /// <summary>
    /// AK 2 to 4 over the gathered inputs: A leads with its three due reviews (two words due later do not count), B follows as
    /// Saturday's failure one stage easier (Sunday is no learning day), C brings the floor of two new words (Saturday was a
    /// learning day without a consolidated word: tempo 0).
    /// Red if the due query counts words not due, "yesterday" is the calendar day, or the new words ignore the pool.
    /// </summary>
    [Fact]
    public async Task Plan_folgt_den_Regeln_aus_den_echten_Daten()
    {
        var s = await SetupAsync();

        var plan = await TodayPlanAsync(s);
        var tasks = plan.GetProperty("tasks").EnumerateArray().ToList();

        Assert.Equal("Shadow", plan.GetProperty("mode").GetString());
        Assert.Equal(2, plan.GetProperty("newWordsBudget").GetInt32());
        Assert.Equal([s.A, s.B, s.C], tasks.Select(t => t.GetProperty("planPositionId").GetInt32()));
        Assert.Equal([3, 0, 0], tasks.Select(t => t.GetProperty("dueReviews").GetInt32()));
        Assert.Equal([0, 0, 2], tasks.Select(t => t.GetProperty("newWords").GetInt32()));
        Assert.Equal((int)TestStage.LetterBoxes, tasks[1].GetProperty("stage").GetInt32());
        Assert.Equal(JsonValueKind.Null, tasks[0].GetProperty("stage").ValueKind);
        Assert.Equal("3 fällige Wiederholungen", tasks[0].GetProperty("reason").GetString());
        Assert.StartsWith("am letzten Lerntag 2 von 2 falsch – eine Stufe leichter", tasks[1].GetProperty("reason").GetString());
        Assert.Equal("2 neue Wörter (Boden 2, Tempo 0 pro Lerntag)", tasks[2].GetProperty("reason").GetString());
    }

    /// <summary>
    /// AK 1 and 6: before 6 a.m. no plan exists; from 6 a.m. exactly one, and a second read returns the same plan although the
    /// child's progress changed in between. Red if the 6 o'clock rule is dropped, or a read composes the plan again.
    /// </summary>
    [Fact]
    public async Task Plan_entsteht_ab_sechs_Uhr_einmal_und_bleibt_eingefroren()
    {
        factory.Clock.FreezeAt(At(Today, 5));
        var s = await SetupAsync();

        Assert.Empty((await OkJsonAsync(await s.Father.GetAsync(s.Url))).EnumerateArray());

        factory.Clock.FreezeAt(At(Today, 6));
        var first = await TodayPlanAsync(s);
        // The child plays: A's due words move on, so a plan composed now would differ.
        Write(db =>
        {
            foreach (var p in db.PositionItemProgress.Where(p => p.PlanPositionId == s.A)) p.DueOn = Today.AddDays(5);
        });
        factory.Clock.FreezeAt(At(Today, 9));
        var second = await TodayPlanAsync(s);

        Assert.Equal(first.GetRawText(), second.GetRawText());
        Assert.Equal(1, Read(db => db.DailyPlans.Count(p => p.ChildId == s.ChildId)));
    }

    /// <summary>
    /// AK 1: the timer and a first request race - the loser of the unique index returns the winner's plan instead of failing,
    /// and no second plan exists. Red if the unique index on (child, day) is missing or the loser does not read the winner.
    /// </summary>
    [Fact]
    public async Task Verlierer_des_Wettlaufs_liest_den_Gewinner()
    {
        var s = await SetupAsync();
        Write(db => db.DailyPlans.Add(new DailyPlan
        {
            ChildId = s.ChildId,
            Day = Today,
            Mode = PlannerMode.Shadow,
            NewWordsBudget = 99,
            CreatedAt = DateTime.UtcNow,
        }));

        using var scope = factory.Services.CreateScope();
        var planner = scope.ServiceProvider.GetRequiredService<DailyPlanner>();
        var stored = await planner.SaveOrLoadAsync(new DailyPlan
        {
            ChildId = s.ChildId,
            Day = Today,
            Mode = PlannerMode.Shadow,
            NewWordsBudget = 2,
            CreatedAt = DateTime.UtcNow,
        }, CancellationToken.None);

        Assert.Equal(99, stored.NewWordsBudget);
        Assert.Equal(1, Read(db => db.DailyPlans.Count(p => p.ChildId == s.ChildId)));
    }

    /// <summary>
    /// Tower-game answers are no failure of the last learning day: the game counts every miss but only the first hit per
    /// word and day. Red if the planner's tally of that day reads rows of source <c>Game</c> (B comes back one stage easier).
    /// </summary>
    [Fact]
    public async Task Spiel_Antworten_sind_kein_Fehlschlag_des_Vortags()
    {
        var s = await SetupAsync(switchOn: false);
        Write(db =>
        {
            foreach (var e in db.ItemReviewEvents.Where(e => e.PlanPositionId == s.B)) e.Source = ItemReviewSource.Game;
        });
        await SwitchAsync(s, "Shadow");

        var tasks = (await TodayPlanAsync(s)).GetProperty("tasks").EnumerateArray().ToList();

        Assert.Contains(tasks, t => t.GetProperty("planPositionId").GetInt32() == s.A);
        Assert.All(tasks, t => Assert.Equal(JsonValueKind.Null, t.GetProperty("stage").ValueKind));
        Assert.DoesNotContain(tasks, t => t.GetProperty("reason").GetString()!.Contains("am letzten Lerntag"));
    }

    /// <summary>
    /// AK 1: many callers at once - the child's login and day view and the supervisor's list - leave one plan. Red if a
    /// parallel caller fails at the index instead of returning the stored plan.
    /// </summary>
    [Fact]
    public async Task Gleichzeitige_Ausloeser_legen_einen_Plan_an()
    {
        var s = await SetupAsync();
        var child = await TestApi.ChildAsync(factory, s.ChildId, ChildPin);
        Write(db => db.DailyPlans.Where(p => p.ChildId == s.ChildId).ExecuteDelete());

        // The child's login makes the plan too; it answers with a token, so it is folded into a status here.
        async Task<HttpResponseMessage> LoginAsync()
        {
            await TestApi.ChildAsync(factory, s.ChildId, ChildPin);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
        var responses = await Task.WhenAll(
            s.Father.GetAsync(s.Url), child.GetAsync($"/api/v1/student/children/{s.ChildId}/today"),
            LoginAsync(),
            s.Father.GetAsync(s.Url));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(1, Read(db => db.DailyPlans.Count(p => p.ChildId == s.ChildId && p.Day == Today)));
    }

    /// <summary>
    /// AK 7: in shadow mode the child's login and day view make the plan, yet the day view answers exactly as with the planner
    /// off, and no coins move. Red if a shadow plan reaches the student's day view or books anything.
    /// </summary>
    [Fact]
    public async Task Schattenplan_bleibt_dem_Sohn_verborgen_und_bucht_nichts()
    {
        var s = await SetupAsync(switchOn: false);
        var child = await TestApi.ChildAsync(factory, s.ChildId, ChildPin);
        var todayUrl = $"/api/v1/student/children/{s.ChildId}/today";
        var off = (await OkJsonAsync(await child.GetAsync(todayUrl))).GetRawText();
        var points = Read(db => db.ChildPointsEntries.Count(p => p.ChildId == s.ChildId));

        await SwitchAsync(s, "Shadow");
        child = await TestApi.ChildAsync(factory, s.ChildId, ChildPin);
        var shadow = (await OkJsonAsync(await child.GetAsync(todayUrl))).GetRawText();

        Assert.Equal(1, Read(db => db.DailyPlans.Count(p => p.ChildId == s.ChildId && p.Day == Today)));
        Assert.Equal(off, shadow);
        Assert.Equal(points, Read(db => db.ChildPointsEntries.Count(p => p.ChildId == s.ChildId)));
    }

    /// <summary>
    /// AK 9: a pause and a Sunday get no plan - the planner never lifts a pause - and the planner switched off makes none.
    /// Red if the duty-day check is dropped or the mode is not read.
    /// </summary>
    [Fact]
    public async Task Pause_Sonntag_und_ausgeschalteter_Planer_ergeben_keinen_Plan()
    {
        var s = await SetupAsync();
        (await s.Father.PostAsJsonAsync($"/api/v1/supervisor/children/{s.ChildId}/pauses",
            new { from = Today, until = Today })).EnsureSuccessStatusCode();
        Assert.Empty((await OkJsonAsync(await s.Father.GetAsync(s.Url))).EnumerateArray());

        factory.Clock.FreezeAt(At(Sunday, 10));
        var sunday = await SetupAsync();
        Assert.Empty((await OkJsonAsync(await sunday.Father.GetAsync(sunday.Url))).EnumerateArray());

        factory.Clock.FreezeAt(At(Today, 7));
        var off = await SetupAsync(switchOn: false);
        Assert.Empty((await OkJsonAsync(await off.Father.GetAsync(off.Url))).EnumerateArray());
    }

    /// <summary>
    /// AK 8: the planner on since Wednesday, a plan stored for Thursday, none for Friday (server off), Saturday a pause, Sunday
    /// free: the supervisor is told about Friday and Wednesday - not about the pause, the Sunday, today (its plan is made on the
    /// read) or Tuesday before the switch. No penalty is booked for a missing day. Red if the report skips the duty-day check,
    /// counts days before the switch, or is missing.
    /// </summary>
    [Fact]
    public async Task Tag_ohne_Plan_wird_gemeldet_und_kostet_nichts()
    {
        var s = await SetupAsync();
        (await s.Father.PostAsJsonAsync($"/api/v1/supervisor/children/{s.ChildId}/pauses",
            new { from = Saturday, until = Saturday })).EnsureSuccessStatusCode();
        Write(db =>
        {
            db.Children.Single(c => c.Id == s.ChildId).PlannerSince = Wednesday;
            db.DailyPlans.Add(new DailyPlan
            {
                ChildId = s.ChildId,
                Day = Thursday,
                Mode = PlannerMode.Shadow,
                CreatedAt = DateTime.UtcNow,
            });
        });

        var missing = await OkJsonAsync(await s.Father.GetAsync($"{s.Url}/missing?from={Today.AddDays(-6):yyyy-MM-dd}"));

        Assert.Equal(["2026-11-27", "2026-11-25"], missing.EnumerateArray().Select(d => d.GetString()));
        Assert.Equal(0, Read(db => db.ChildPointsEntries.Count(p => p.ChildId == s.ChildId && p.Kind == PointKind.GoalPenalty)));
    }

    /// <summary>
    /// AK 8: today is never a missing day - any access makes its plan, so a running server cannot have missed it; even with no
    /// plan stored yet (before 6 a.m.) it is not reported. Red if the report reaches into today.
    /// </summary>
    [Fact]
    public async Task Heute_fehlt_nie()
    {
        factory.Clock.FreezeAt(At(Today, 9));
        var s = await SetupAsync();

        var missing = await OkJsonAsync(await s.Father.GetAsync($"{s.Url}/missing"));

        Assert.Empty(missing.EnumerateArray());
    }

    /// <summary>
    /// The switch: the planner needs all four limits - on without them, or clearing them while it runs, is a validation error;
    /// switching on stamps today, switching off clears it. Red if the check or the stamp is missing.
    /// </summary>
    [Fact]
    public async Task Einschalten_verlangt_die_Leitplanken_und_stempelt_den_Tag()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Planer-Schalter"), pin = ChildPin }));
        var url = $"/api/v1/supervisor/children/{childId}";

        var bare = await father.PatchAsJsonAsync(url, new { plannerMode = "Shadow" });
        Assert.Equal(HttpStatusCode.BadRequest, bare.StatusCode);
        Assert.Equal("validation_error", (await bare.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        var on = await OkJsonAsync(await father.PatchAsJsonAsync(url, new
        {
            dailyTasksMin = 1,
            dailyTasksMax = 3,
            dailyNewWordsMin = 1,
            dailyNewWordsMax = 4,
            plannerMode = "Shadow",
        }));
        Assert.Equal("Shadow", on.GetProperty("plannerMode").GetString());
        Assert.Equal("2026-11-30", on.GetProperty("plannerSince").GetString());

        Assert.Equal(HttpStatusCode.BadRequest,
            (await father.PatchAsJsonAsync(url, new { clearPlannerLimits = true })).StatusCode);

        var off = await OkJsonAsync(await father.PatchAsJsonAsync(url, new { plannerMode = "Off" }));
        Assert.Equal(JsonValueKind.Null, off.GetProperty("plannerSince").ValueKind);
    }

    /// <summary>
    /// AK 10: both paths take the supervisor claim and ownership - a creator-only account gets 403, a foreign father 404, the
    /// child 403 - and a reversed range is 400. Red if a gate is missing.
    /// </summary>
    [Fact]
    public async Task Pfade_pruefen_Rolle_Eigentum_und_Zeitraum()
    {
        var s = await SetupAsync();
        var teacher = await TestApi.AdultAsync(factory, id: 2, pin: "9999");
        var stranger = await TestApi.AdultAsync(factory, id: 3, pin: "0001");
        var child = await TestApi.ChildAsync(factory, s.ChildId, ChildPin);
        foreach (var path in new[] { s.Url, $"{s.Url}/missing" })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await teacher.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await child.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,
                (await s.Father.GetAsync($"{path}?from={Friday:yyyy-MM-dd}&to={Thursday:yyyy-MM-dd}")).StatusCode);
        }
    }

    /// <summary>
    /// AK 9: no planner endpoint takes an amount - the actions read only the child, the period and the page. Red if a
    /// parameter such as a task or word count is added.
    /// </summary>
    [Fact]
    public void Kein_Planer_Endpunkt_nimmt_eine_Menge_an()
    {
        string[] allowed = ["childId", "from", "to", "skip", "take", "ct"];
        var parameters = typeof(Controllers.Supervisor.ChildDailyPlansController).GetMethods()
            .Where(m => m.DeclaringType == typeof(Controllers.Supervisor.ChildDailyPlansController) && m.IsPublic)
            .SelectMany(m => m.GetParameters()).Select(p => p.Name!).Distinct();

        Assert.All(parameters, p => Assert.Contains(p, allowed));
    }

    /// <summary>
    /// The timer: one pass makes today's plan for every child whose planner is on, through the same method as the first
    /// request; the test host does not register it. Red if the pass skips the children or the host runs the timer in tests.
    /// </summary>
    [Fact]
    public async Task Zeitgeber_legt_den_Plan_an_und_ist_im_Test_Host_aus()
    {
        var s = await SetupAsync();
        Write(db => db.DailyPlans.Where(p => p.ChildId == s.ChildId).ExecuteDelete());

        var timer = ActivatorUtilities.CreateInstance<DailyPlanTimer>(factory.Services);
        await timer.RunOnceAsync(CancellationToken.None);

        Assert.Equal(1, Read(db => db.DailyPlans.Count(p => p.ChildId == s.ChildId && p.Day == Today)));
        Assert.DoesNotContain(factory.Services.GetServices<IHostedService>(), h => h is DailyPlanTimer);
    }

    /// <summary>
    /// Review finding: new words exist only where a type introduces its cards. A cloze position with unplayed gaps brings no new
    /// words, so with the vocabulary position C gone the day has none and says so. Red if new words are counted for every type.
    /// </summary>
    [Fact]
    public async Task Lueckentext_bringt_keine_neuen_Woerter()
    {
        var s = await SetupAsync(switchOn: false);
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(s.Father, await TestApi.DefaultSubjectIdAsync(s.Father));
        var clozeId = await TestApi.IdAsync(await s.Father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/cloze", new
            {
                title = "Lücken",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { text = "I fell {{1}}.", gaps = new object[] { new { index = 1, answer = "asleep", prompt = "Ich schlief ein." } } },
            }));
        var cloze = TestApi.SeedLeitnerPosition(factory, clozeId, 0, childId: s.ChildId, useLeitner: false).positionId;
        Write(db =>
        {
            db.PlanPositions.Remove(db.PlanPositions.Single(p => p.Id == s.C));
            var plan = db.PlanPositions.Where(p => p.Id == cloze).Select(p => p.StudyPlan!).Single();
            plan.StartDate = Thursday;
            plan.EndDate = Today.AddDays(30);
        });
        await SwitchAsync(s, "Shadow");

        var plan = await TodayPlanAsync(s);

        Assert.DoesNotContain(plan.GetProperty("tasks").EnumerateArray(), t => t.GetProperty("newWords").GetInt32() > 0);
        Assert.Contains(plan.GetProperty("warnings").EnumerateArray(), w => w.GetString()!.StartsWith("Nur 0 neue Wörter"));
    }

    /// <summary>
    /// Review finding: in shadow mode a failing planner must not cost the child the app - its login and day view answer as
    /// always. Red if a student path awaits the planner unguarded.
    /// </summary>
    [Fact]
    public async Task Fehler_im_Planer_laesst_Login_und_Tag_des_Sohns_stehen()
    {
        var s = await SetupAsync();
        using var host = factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
            services.AddScoped<DailyPlanner>(sp => ActivatorUtilities.CreateInstance<FailingPlanner>(sp, s.ChildId))));

        var login = await host.CreateClient().PostAsJsonAsync("/api/v1/auth/child", new { childId = s.ChildId, pin = ChildPin });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var child = host.CreateClient();
        child.DefaultRequestHeaders.Authorization = new("Bearer",
            (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString());
        Assert.Equal(HttpStatusCode.OK, (await child.GetAsync($"/api/v1/student/children/{s.ChildId}/today")).StatusCode);
        Assert.Equal(0, Read(db => db.DailyPlans.Count(p => p.ChildId == s.ChildId)));
    }

    /// <summary>
    /// Review finding: a child whose plan fails does not keep the timer from the children after it. Red if one failure ends
    /// the whole pass.
    /// </summary>
    [Fact]
    public async Task Zeitgeber_ueberspringt_ein_scheiterndes_Kind()
    {
        var failing = await SetupAsync();
        var fine = await SetupAsync();
        Write(db => db.DailyPlans.Where(p => p.ChildId == failing.ChildId || p.ChildId == fine.ChildId).ExecuteDelete());
        using var host = factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
            services.AddScoped<DailyPlanner>(sp => ActivatorUtilities.CreateInstance<FailingPlanner>(sp, failing.ChildId))));

        await ActivatorUtilities.CreateInstance<DailyPlanTimer>(host.Services).RunOnceAsync(CancellationToken.None);

        Assert.Equal(0, Read(db => db.DailyPlans.Count(p => p.ChildId == failing.ChildId)));
        Assert.Equal(1, Read(db => db.DailyPlans.Count(p => p.ChildId == fine.ChildId)));
    }

    /// <summary>
    /// Review finding: a failing plan for today does not hide the supervisor's stored plans. Red if the list awaits the planner
    /// unguarded - the failing planner then answers 500 and Saturday's plan is never shown.
    /// </summary>
    [Fact]
    public async Task Fehler_im_heutigen_Plan_laesst_die_Liste_des_Vaters_stehen()
    {
        var s = await SetupAsync();
        Write(db => db.DailyPlans.Add(new DailyPlan
        {
            ChildId = s.ChildId,
            Day = Saturday,
            Mode = PlannerMode.Shadow,
            NewWordsBudget = 2,
            CreatedAt = At(Saturday, 7).UtcDateTime,
        }));
        using var host = factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
            services.AddScoped<DailyPlanner>(sp => ActivatorUtilities.CreateInstance<FailingPlanner>(sp, s.ChildId))));
        var father = host.CreateClient();
        father.DefaultRequestHeaders.Authorization = s.Father.DefaultRequestHeaders.Authorization;

        var plans = await OkJsonAsync(await father.GetAsync(s.Url));

        Assert.Equal(["2026-11-28"], plans.EnumerateArray().Select(p => p.GetProperty("day").GetString()));
    }

    /// <summary>
    /// Review finding: a plan whose save fails for a reason other than the lost race leaves nothing queued in the context.
    /// Red if the failed plan stays Added - the next SaveChanges of the same request would insert it again and fail.
    /// </summary>
    [Fact]
    public async Task Gescheitertes_Speichern_laesst_nichts_im_Kontext_liegen()
    {
        var s = await SetupAsync(switchOn: false);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var planner = scope.ServiceProvider.GetRequiredService<DailyPlanner>();
        var plan = new DailyPlan
        {
            ChildId = s.ChildId,
            Day = Friday,
            Mode = PlannerMode.Shadow,
            CreatedAt = At(Friday, 7).UtcDateTime,
            // A position that does not exist (deleted meanwhile): the foreign key fails, not the unique index.
            Tasks = [new DailyTask { PlanPositionId = int.MaxValue, ExerciseId = 1, ExerciseTitle = "weg", Order = 1, Reason = "Test" }],
        };

        await Assert.ThrowsAsync<DbUpdateException>(() => planner.SaveOrLoadAsync(plan, CancellationToken.None));

        Assert.Empty(db.ChangeTracker.Entries<DailyPlan>());
        Assert.Empty(db.ChangeTracker.Entries<DailyTask>());
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Review finding: switching the planner on evaluates the closed days right away, so the four weeks of the first
    /// evaluation land on the supervisor's switch, not on the child's next login. Red if the switch only saves the mode.
    /// </summary>
    [Fact]
    public async Task Einschalten_wertet_die_abgeschlossenen_Tage_aus()
    {
        var s = await SetupAsync(switchOn: false);
        Assert.Equal(0, Read(db => db.EvaluatedDays.Count(d => d.ChildId == s.ChildId)));

        await SwitchAsync(s, "Shadow");

        Assert.Equal(DailyEvaluationService.WindowDays, Read(db => db.EvaluatedDays.Count(d => d.ChildId == s.ChildId)));
    }

    /// <summary>The string-enum converter takes numbers too: an undefined mode is refused. Red if the check is missing.</summary>
    [Fact]
    public async Task Undefinierter_Modus_wird_abgelehnt()
    {
        var s = await SetupAsync(switchOn: false);

        var res = await s.Father.PatchAsJsonAsync($"/api/v1/supervisor/children/{s.ChildId}", new { plannerMode = 7 });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("Off", (await OkJsonAsync(await s.Father.GetAsync($"/api/v1/supervisor/children/{s.ChildId}")))
            .GetProperty("plannerMode").GetString());
    }

    /// <summary>A planner that fails for one child, as a broken build path or a lost position would.</summary>
    private sealed class FailingPlanner(int failFor, PuglingDbContext db, LearningDay learningDay, DutyCalendar calendar,
        PositionProgressService progress, PositionPlayService play, DailyEvaluationService evaluations, TimeProvider time,
        ILogger<DailyPlanner> logger)
        : DailyPlanner(db, learningDay, calendar, progress, play, evaluations, time, logger)
    {
        public override Task<DailyPlan?> EnsureTodayAsync(int childId, CancellationToken ct) =>
            childId == failFor ? throw new InvalidOperationException("Planer kaputt (Test).") : base.EnsureTodayAsync(childId, ct);
    }
}
