using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-614: a position reports the subject of its plan, not the subject of its exercise's textbook series - the start page
/// groups the child's day by it, and the grouping must be the father's filing. A plan without a subject keeps the
/// series' name as before.
/// </summary>
public class PlanFachTagessichtTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private sealed record Setup(HttpClient Child, int ChildId, int PlanId, int ForeignPos, int FreeTextPos, int BarePos,
        string ForeignSubject);

    // One plan of the seeded subject "Englisch" with three positions whose series name their subject differently:
    // a catalog subject of another name, only the free text "English", and nothing at all.
    private async Task<Setup> SetupAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "7" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Plan-Fach-Kind"), pin }));
        var foreignEx = await TestApi.CreateVocabExerciseAsync(father);
        var freeTextEx = await TestApi.CreateVocabExerciseAsync(father);
        var bareEx = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, foreignPos) = TestApi.SeedLeitnerPosition(factory, foreignEx, (int)TestStage.FreeText, childId: childId);
        var (planB, freeTextPos) = TestApi.SeedLeitnerPosition(factory, freeTextEx, (int)TestStage.FreeText, childId: childId);
        var (planC, barePos) = TestApi.SeedLeitnerPosition(factory, bareEx, (int)TestStage.FreeText, childId: childId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        foreach (var pos in db.PlanPositions.Where(p => p.Id == freeTextPos || p.Id == barePos))
            pos.StudyPlanId = planId;
        db.StudyPlans.RemoveRange(db.StudyPlans.Where(p => p.Id == planB || p.Id == planC));
        var foreignSubject = TestApi.UniqueName("Mathe-x");
        var mathe = new Subject { Name = foreignSubject };
        db.Subjects.Add(mathe);
        SeriesOf(db, foreignEx).Subject = mathe;
        var freeText = SeriesOf(db, freeTextEx);
        freeText.SubjectId = null;
        freeText.SubjectName = "English";
        var bare = SeriesOf(db, bareEx);
        bare.SubjectId = null;
        bare.SubjectName = null;
        db.SaveChanges();
        return new Setup(await TestApi.ChildAsync(factory, childId, pin), childId, planId, foreignPos, freeTextPos, barePos,
            foreignSubject);
    }

    private static TextbookSeries SeriesOf(PuglingDbContext db, int exerciseId) =>
        db.TextbookSeries.Single(s => s.Id == db.Exercises.Where(e => e.Id == exerciseId)
            .Select(e => e.SeriesUnit!.SeriesId).Single());

    private void ClearPlanSubject(int planId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.StudyPlans.Single(p => p.Id == planId).SubjectId = null;
        db.SaveChanges();
    }

    private static async Task<Dictionary<int, string?>> SubjectsAsync(HttpClient c, string url, Func<JsonElement, JsonElement> day)
    {
        var res = await c.GetAsync(url);
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return day(body).GetProperty("positions").EnumerateArray()
            .ToDictionary(p => p.GetProperty("positionId").GetInt32(), p => p.GetProperty("subjectName").GetString());
    }

    private static Task<Dictionary<int, string?>> TodaySubjectsAsync(Setup s) =>
        SubjectsAsync(s.Child, $"/api/v1/student/children/{s.ChildId}/today", b => b);

    private static Task<Dictionary<int, string?>> OverviewSubjectsAsync(Setup s) =>
        SubjectsAsync(s.Child, $"/api/v1/student/study-plans/{s.PlanId}/overview", b => b.GetProperty("today"));

    /// <summary>
    /// AK 1 + 2: a series of another catalog subject and a series with only the free text "English" both report the plan's
    /// subject. Red while the name comes from the series ("Mathe-x…" and "English").
    /// </summary>
    [Fact]
    public async Task Startseite_Meldet_Das_Fach_Des_Plans()
    {
        var s = await SetupAsync();

        var subjects = await TodaySubjectsAsync(s);

        Assert.Equal("Englisch", subjects[s.ForeignPos]);
        Assert.Equal("Englisch", subjects[s.FreeTextPos]);
        Assert.Equal("Englisch", subjects[s.BarePos]);
    }

    /// <summary>
    /// AK 3: a plan without a subject keeps the series' name (catalog name, else free text, else empty). Red without the
    /// fallback (all three empty).
    /// </summary>
    [Fact]
    public async Task Plan_Ohne_Fach_Faellt_Auf_Die_Reihe_Zurueck()
    {
        var s = await SetupAsync();
        ClearPlanSubject(s.PlanId);

        var subjects = await TodaySubjectsAsync(s);

        Assert.Equal(s.ForeignSubject, subjects[s.ForeignPos]);
        Assert.Equal("English", subjects[s.FreeTextPos]);
        Assert.Equal("", subjects[s.BarePos]);
    }

    /// <summary>
    /// AK 4: the plan overview loads its plan without the subject navigation and still names the plan's subject, the same
    /// as the start page. Red when the name is only read from a loaded <c>plan.Subject</c> and falls back otherwise.
    /// </summary>
    [Fact]
    public async Task Planuebersicht_Meldet_Dasselbe_Fach_Wie_Die_Startseite()
    {
        var s = await SetupAsync();

        var overview = await OverviewSubjectsAsync(s);

        Assert.Equal("Englisch", overview[s.ForeignPos]);
        Assert.Equal("Englisch", overview[s.FreeTextPos]);
        Assert.Equal(await TodaySubjectsAsync(s), overview);
    }

    /// <summary>
    /// Decision 3: a caller that hands in the plan without its subject navigation still gets the plan's subject - the sink
    /// looks it up instead of falling back to the series. Red when the name is only read from a loaded <c>plan.Subject</c>
    /// (every known caller loads it today, so only this direct call reaches the lookup).
    /// </summary>
    [Fact]
    public async Task Plan_Ohne_Geladenes_Fach_Meldet_Trotzdem_Das_Plan_Fach()
    {
        var s = await SetupAsync();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var plan = db.StudyPlans.AsNoTracking().Single(p => p.Id == s.PlanId);
        Assert.Null(plan.Subject);
        var day = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);
        var overview = await scope.ServiceProvider.GetRequiredService<PositionProgressService>()
            .ComputeDayAsync(plan, day, CancellationToken.None);

        Assert.All(overview.Positions, p => Assert.Equal("Englisch", p.SubjectName));
    }
}
