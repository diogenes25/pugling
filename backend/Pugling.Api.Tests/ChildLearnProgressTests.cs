using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Child-centric drill-down view of vocabulary learning progress along the catalog hierarchy
/// (subject → series unit → exercise → item). "Assigned" is derived from the study plans - even exercises not
/// yet practiced appear (zero progress), progress comes from server-authoritative practicing.
/// </summary>
public class ChildLearnProgressTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    // Creates subject → textbook series (with the subject attached) → unit → one vocabulary exercise and
    // returns all ids (the catalog does not otherwise reveal subject/series/series unit). The series id is
    // carried along only so VocabInAsync can add a second exercise to the same unit; the student routes built
    // from this tuple only ever use subjectId/seriesUnitId.
    private static async Task<(int subjectId, int seriesId, int seriesUnitId, int exerciseId)> VocabAsync(
        HttpClient father, string subjectName, string title, params (string Front, string Back)[] items)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = subjectName }));
        var seriesId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            "/api/v1/creator/textbook-series", new { name = TestApi.UniqueName($"{subjectName}-Reihe"), subjectId }));
        var seriesUnitId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Unit 1", orderIndex = 1 }));
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary", new
            {
                title,
                orderIndex = 1,
                rewardPoints = 10,
                config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = items.Select(i => new { front = i.Front, back = i.Back }) },
            }));
        return (subjectId, seriesId, seriesUnitId, exerciseId);
    }

    // Creates another vocabulary exercise in an EXISTING series/unit and returns its id.
    private static async Task<int> VocabInAsync(HttpClient father, int seriesId, int seriesUnitId, string title, params (string Front, string Back)[] items) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary", new
            {
                title,
                orderIndex = 1,
                rewardPoints = 10,
                config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = items.Select(i => new { front = i.Front, back = i.Back }) },
            }));

    // Bundles two existing exercises as positions in ONE active plan (one active plan per child).
    private (int planId, int pos1, int pos2) SeedPlanWithTwoPositions(int exercise1, int exercise2, int childId = 1)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var plan = new StudyPlan { ChildId = childId, Title = "Progress-Plan", StartDate = today, EndDate = today.AddDays(5), Active = true };
        var p1 = new PlanPosition { ExerciseId = exercise1, Order = 0, Stage = (int)TestStage.FreeText, Cadence = GoalCadence.Daily, UseLeitner = true };
        var p2 = new PlanPosition { ExerciseId = exercise2, Order = 1, Stage = (int)TestStage.FreeText, Cadence = GoalCadence.Daily, UseLeitner = true };
        plan.Positions.Add(p1);
        plan.Positions.Add(p2);
        db.StudyPlans.Add(plan);
        db.SaveChanges();
        return (plan.Id, p1.Id, p2.Id);
    }

    // Creates an (active or inactive) plan with positions on the given exercises; returns the plan id.
    private int SeedPlan(bool active, params int[] exerciseIds)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var plan = new StudyPlan { ChildId = 1, Title = "Flag-Plan", StartDate = today, EndDate = today.AddDays(5), Active = active };
        var order = 0;
        foreach (var id in exerciseIds)
            plan.Positions.Add(new PlanPosition { ExerciseId = id, Order = order++, Stage = (int)TestStage.FreeText, Cadence = GoalCadence.Daily, UseLeitner = true });
        db.StudyPlans.Add(plan);
        db.SaveChanges();
        return plan.Id;
    }

    private void SetPlanActive(int planId, bool active)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var plan = db.StudyPlans.Find(planId)!;
        plan.Active = active;
        db.SaveChanges();
    }

    [Fact]
    public async Task Hierarchie_AggregiertFortschritt_ZeigtAbdeckung_UndBlattItems()
    {
        var father = await TestApi.FatherAsync(_factory);
        // Unique words, so that the per-child shared progress/store does not collide with other tests.
        var (subjectId, _, seriesUnitId, ex1) = await VocabAsync(father, "Progress-Fach", "Geübt", ("quokka", "Kurzschwanzkänguru"), ("axolotl", "Axolotl"));
        var (_, _, _, ex2) = await VocabAsync(father, "Progress-Fach-B", "Ungeübt", ("pangolin", "Schuppentier"), ("tapir", "Tapir"));

        // Both exercises lie in the SAME subject/series unit (ex2 only moved over for the position): we take ex1+ex2 into one plan.
        var (planId, pos1, _) = SeedPlanWithTwoPositions(ex1, ex2);
        var child = await TestApi.ChildAsync(_factory);

        // Practice ex1 only: one correct, one wrong → 2 of 4 items in total introduced.
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, pos1);
        await TestApi.PositionReviewAsync(child, planId, pos1, sessionId, 0, givenAnswer: "Kurzschwanzkänguru"); // correct
        await TestApi.PositionReviewAsync(child, planId, pos1, sessionId, 1, givenAnswer: "daneben");             // wrong

        var basePath = "/api/v1/student/children/1/learn";

        // The subject list: both assigned subjects appear; the practiced subject shows coverage (total > introduced).
        var subjects = await father.GetFromJsonAsync<List<JsonElement>>($"{basePath}/subjects");
        var geubtesFach = subjects!.First(s => s.GetProperty("subjectId").GetInt32() == subjectId);
        var prog = geubtesFach.GetProperty("progress");
        Assert.Equal(1, geubtesFach.GetProperty("exerciseCount").GetInt32());
        Assert.Equal(2, prog.GetProperty("totalItems").GetInt32());       // both words of the exercise
        Assert.Equal(2, prog.GetProperty("introducedItems").GetInt32());  // both answered
        Assert.True(prog.GetProperty("avgMasteryPercent").GetInt32() > 0);
        Assert.Equal(2, prog.GetProperty("seenCount").GetInt32());
        Assert.Equal(1, prog.GetProperty("correctCount").GetInt32());

        // A single subject: the identical aggregate.
        var subject = await father.GetFromJsonAsync<JsonElement>($"{basePath}/subjects/{subjectId}");
        Assert.Equal(2, subject.GetProperty("progress").GetProperty("totalItems").GetInt32());

        // The series-unit level.
        var seriesUnits = await father.GetFromJsonAsync<List<JsonElement>>($"{basePath}/subjects/{subjectId}/series-units");
        Assert.Single(seriesUnits!);
        Assert.Equal(seriesUnitId, seriesUnits![0].GetProperty("seriesUnitId").GetInt32());

        // The exercise level: the practiced exercise with its progress.
        var exercises = await father.GetFromJsonAsync<List<JsonElement>>($"{basePath}/subjects/{subjectId}/series-units/{seriesUnitId}/vocabulary");
        var ex1Row = exercises!.First(e => e.GetProperty("exerciseId").GetInt32() == ex1);
        Assert.Equal(2, ex1Row.GetProperty("progress").GetProperty("introducedItems").GetInt32());

        // The leaf level: item progress, weakest first.
        var itemsRes = await father.GetAsync($"{basePath}/subjects/{subjectId}/series-units/{seriesUnitId}/vocabulary/{ex1}/items");
        itemsRes.EnsureSuccessStatusCode();
        var leaf = await itemsRes.Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.Equal(2, leaf!.Count);
        Assert.Equal("2", itemsRes.Headers.GetValues("X-Total-Count").First());
        Assert.True(leaf![0].GetProperty("masteryPercent").GetInt32() <= leaf![1].GetProperty("masteryPercent").GetInt32());
    }

    [Fact]
    public async Task UngeübteAberZugewieseneÜbung_ErscheintMitNullFortschritt()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, _, _, ex1) = await VocabAsync(father, "Null-Fach", "Geübt", ("okapi", "Okapi"));
        var (subjectId, _, seriesUnitId, ex2) = await VocabAsync(father, "Null-Fach-B", "Nie geübt", ("numbat", "Ameisenbeutler"), ("dugong", "Dugong"));
        SeedPlanWithTwoPositions(ex1, ex2);

        var exercises = await father.GetFromJsonAsync<List<JsonElement>>(
            $"/api/v1/student/children/1/learn/subjects/{subjectId}/series-units/{seriesUnitId}/vocabulary");
        var ex2Row = exercises!.First(e => e.GetProperty("exerciseId").GetInt32() == ex2);
        var prog = ex2Row.GetProperty("progress");
        Assert.Equal(2, prog.GetProperty("totalItems").GetInt32());       // the exercise has 2 items …
        Assert.Equal(0, prog.GetProperty("introducedItems").GetInt32());  // … but none of them practiced
        Assert.Equal(0, prog.GetProperty("avgMasteryPercent").GetInt32());
        Assert.True(prog.GetProperty("lastActivityAt").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task NichtZugewiesenesFach_Und_NichtZugewieseneÜbung_Liefern404()
    {
        var father = await TestApi.FatherAsync(_factory);
        // A subject with an exercise but NO plan → not assigned to the child.
        var (subjectId, _, seriesUnitId, exerciseId) = await VocabAsync(father, "Waise-Fach", "Ohne Plan", ("caracal", "Karakal"));

        Assert.Equal(HttpStatusCode.NotFound,
            (await father.GetAsync($"/api/v1/student/children/1/learn/subjects/{subjectId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await father.GetAsync($"/api/v1/student/children/1/learn/subjects/{subjectId}/series-units")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await father.GetAsync($"/api/v1/student/children/1/learn/subjects/{subjectId}/series-units/{seriesUnitId}/vocabulary/{exerciseId}/items")).StatusCode);
    }

    [Fact]
    public async Task FremdesKind_Liefert404_SohnSiehtEigenen()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, _, _, ex1) = await VocabAsync(father, "Ownership-Fach", "Geübt", ("serval", "Serval"));
        var (_, _, _, ex2) = await VocabAsync(father, "Ownership-Fach-B", "Ungeübt", ("gerenuk", "Giraffengazelle"));
        SeedPlanWithTwoPositions(ex1, ex2);

        // Another/non-existent child → the ownership filter returns 404 (no enumeration).
        Assert.Equal(HttpStatusCode.NotFound,
            (await father.GetAsync("/api/v1/student/children/999/learn/subjects")).StatusCode);

        // The child may read its own state.
        var child = await TestApi.ChildAsync(_factory);
        var self = await child.GetAsync("/api/v1/student/children/1/learn/subjects");
        self.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AbgehängterPlan_MachtÜbungInaktiv_FortschrittBleibt()
    {
        var father = await TestApi.FatherAsync(_factory);
        // Both exercises in the SAME subject/series unit, so that the vocabulary list shows both.
        var (subjectId, seriesId, seriesUnitId, ex1) = await VocabAsync(father, "Retention-Fach", "Geübt", ("wombat", "Wombat"), ("kakapo", "Kakapo"));
        var ex2 = await VocabInAsync(father, seriesId, seriesUnitId, "Ungeübt", ("quoll", "Beutelmarder"));
        var (planId, pos1, _) = SeedPlanWithTwoPositions(ex1, ex2);
        var child = await TestApi.ChildAsync(_factory);

        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, pos1);
        await TestApi.PositionReviewAsync(child, planId, pos1, sessionId, 0, givenAnswer: "Wombat");
        await TestApi.PositionReviewAsync(child, planId, pos1, sessionId, 1, givenAnswer: "Kakapo");

        var url = $"/api/v1/student/children/1/learn/subjects/{subjectId}/series-units/{seriesUnitId}/vocabulary";

        // While the plan is active: ex1 is active, with progress.
        var before = await father.GetFromJsonAsync<List<JsonElement>>(url);
        var ex1Before = before!.First(e => e.GetProperty("exerciseId").GetInt32() == ex1);
        Assert.True(ex1Before.GetProperty("active").GetBoolean());
        Assert.Equal(2, ex1Before.GetProperty("progress").GetProperty("introducedItems").GetInt32());

        // Deactivate the plan → the exercise becomes inactive, the progress is preserved (it does not disappear).
        SetPlanActive(planId, false);
        var after = await father.GetFromJsonAsync<List<JsonElement>>(url);
        var ex1After = after!.First(e => e.GetProperty("exerciseId").GetInt32() == ex1);
        Assert.False(ex1After.GetProperty("active").GetBoolean());
        Assert.Equal(2, ex1After.GetProperty("progress").GetProperty("introducedItems").GetInt32());
        // The unpracticed second exercise stays visible as inactive (0 % progress).
        var ex2After = after!.First(e => e.GetProperty("exerciseId").GetInt32() == ex2);
        Assert.False(ex2After.GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task ActiveFilter_TrenntAktivVonInaktiv()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (subjectId, seriesId, seriesUnitId, exAktiv) = await VocabAsync(father, "Filter-Fach", "Aktiv", ("dingo", "Dingo"));
        var exInaktiv = await VocabInAsync(father, seriesId, seriesUnitId, "Inaktiv", ("bilby", "Bilby"));
        SeedPlan(active: true, exAktiv);
        SeedPlan(active: false, exInaktiv);

        var url = $"/api/v1/student/children/1/learn/subjects/{subjectId}/series-units/{seriesUnitId}/vocabulary";

        var aktiv = await father.GetFromJsonAsync<List<JsonElement>>($"{url}?active=true");
        Assert.Contains(aktiv!, e => e.GetProperty("exerciseId").GetInt32() == exAktiv);
        Assert.DoesNotContain(aktiv!, e => e.GetProperty("exerciseId").GetInt32() == exInaktiv);

        var inaktiv = await father.GetFromJsonAsync<List<JsonElement>>($"{url}?active=false");
        Assert.Contains(inaktiv!, e => e.GetProperty("exerciseId").GetInt32() == exInaktiv);
        Assert.DoesNotContain(inaktiv!, e => e.GetProperty("exerciseId").GetInt32() == exAktiv);
    }

    [Fact]
    public async Task SucheUndSortierung_AufÜbungen()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (subjectId, seriesId, seriesUnitId, exTiere) = await VocabAsync(father, "Sort-Fach", "Tiere", ("emu", "Emu"));
        var exFarben = await VocabInAsync(father, seriesId, seriesUnitId, "Farben", ("mauve", "Malvenfarben"));
        SeedPlan(active: true, exTiere, exFarben);

        var url = $"/api/v1/student/children/1/learn/subjects/{subjectId}/series-units/{seriesUnitId}/vocabulary";

        // Search by title (substring, case-insensitive).
        var suche = await father.GetFromJsonAsync<List<JsonElement>>($"{url}?search=tier");
        Assert.Single(suche!);
        Assert.Equal("Tiere", suche![0].GetProperty("title").GetString());

        // Sorting by title descending: "Tiere" before "Farben".
        var desc = await father.GetFromJsonAsync<List<JsonElement>>($"{url}?sort=title&dir=desc");
        var titlesDesc = desc!.Select(e => e.GetProperty("title").GetString()).ToList();
        Assert.True(titlesDesc.IndexOf("Tiere") < titlesDesc.IndexOf("Farben"));
    }
}
