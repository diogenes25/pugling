using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Kind-/Scope-bezogene Ergebnis-Lernziele (<c>children/{}/learn-goals</c>): der Vater setzt Beherrschungs-/
/// Abdeckungsziele auf Fach/Kapitel/Übung, der Status wird live aus dem Lernstand berechnet
/// (offen/erreicht/überfällig). Deckt Auswertung, Validierung, Rollen/Ownership und CRUD ab.
/// </summary>
public class LearnGoalTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private static async Task<(int subjectId, int chapterId, int exerciseId)> VocabAsync(
        HttpClient father, string subjectName, params (string Front, string Back)[] items)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = subjectName }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit 1", orderIndex = 1 }));
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary", new
            {
                title = "Übung",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = items.Select(i => new { front = i.Front, back = i.Back }) },
            }));
        return (subjectId, chapterId, exerciseId);
    }

    private static string Url(int childId) => $"/api/v1/supervisor/children/{childId}/learn-goals";

    [Fact]
    public async Task Erstellen_UndLiveStatus_ErreichtVsOffen()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (subjectId, _, ex) = await VocabAsync(father, "Ziel-Fach", ("wallaby", "Wallaby"), ("emu", "Emu"));
        var (planId, posId) = TestApi.SeedLeitnerPosition(_factory, ex, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);

        // Ein Item richtig (Box 2 = 25 %), eines falsch (Box 1 = 0 %) -> Ø-Beherrschung > 0.
        var sid = await TestApi.StartPositionSessionAsync(child, planId, posId);
        await TestApi.PositionReviewAsync(child, planId, posId, sid, 0, givenAnswer: "Wallaby");
        await TestApi.PositionReviewAsync(child, planId, posId, sid, 1, givenAnswer: "falsch");

        // Erreichbares Ziel (niedrige Schwelle) und unerreichtes Ziel (hohe Schwelle) auf dem Fach.
        var lowRes = await father.PostAsJsonAsync(Url(1), new { subjectId, metric = "AvgMastery", targetValue = 10 });
        Assert.Equal(HttpStatusCode.Created, lowRes.StatusCode);
        var low = await lowRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("achieved", low.GetProperty("status").GetString());
        Assert.True(low.GetProperty("currentValue").GetInt32() >= 10);
        Assert.Equal("subject", low.GetProperty("scope").GetString());

        var high = await (await father.PostAsJsonAsync(Url(1), new { subjectId, metric = "AvgMastery", targetValue = 95 }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("open", high.GetProperty("status").GetString());

        // Liste + Statusfilter.
        var all = await father.GetFromJsonAsync<List<JsonElement>>($"{Url(1)}?subjectId={subjectId}");
        Assert.Equal(2, all!.Count);
        var achievedOnly = await father.GetFromJsonAsync<List<JsonElement>>($"{Url(1)}?subjectId={subjectId}&status=achieved");
        Assert.All(achievedOnly!, g => Assert.Equal("achieved", g.GetProperty("status").GetString()));
        Assert.Contains(achievedOnly!, g => g.GetProperty("id").GetInt32() == low.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task Abdeckung_UndUeberfaellig()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (subjectId, _, ex) = await VocabAsync(father, "Abdeckung-Fach", ("okapi", "Okapi"), ("dingo", "Dingo"), ("quoll", "Beutelmarder"));
        var (planId, posId) = TestApi.SeedLeitnerPosition(_factory, ex, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);

        // Nur 1 von 3 Items geübt -> Abdeckung ~33 %.
        var sid = await TestApi.StartPositionSessionAsync(child, planId, posId);
        await TestApi.PositionReviewAsync(child, planId, posId, sid, 0, givenAnswer: "Okapi");

        var coverage = await (await father.PostAsJsonAsync(Url(1), new { subjectId, metric = "Coverage", targetValue = 30 }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("achieved", coverage.GetProperty("status").GetString());
        Assert.True(coverage.GetProperty("currentValue").GetInt32() is >= 30 and < 100);

        // Unerreichtes Ziel mit Stichtag in der Vergangenheit -> überfällig.
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1).ToString("yyyy-MM-dd");
        var overdue = await (await father.PostAsJsonAsync(Url(1), new { subjectId, metric = "Coverage", targetValue = 100, dueDate = yesterday }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("overdue", overdue.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Validierung_Scope_UndZielwert()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (subjectId, chapterId, _) = await VocabAsync(father, "Valid-Fach", ("serval", "Serval"));

        // Nicht existierendes Fach -> 400.
        Assert.Equal(HttpStatusCode.BadRequest,
            (await father.PostAsJsonAsync(Url(1), new { subjectId = 999999, metric = "AvgMastery", targetValue = 50 })).StatusCode);

        // Übungs-Scope ohne Kapitel -> 400.
        Assert.Equal(HttpStatusCode.BadRequest,
            (await father.PostAsJsonAsync(Url(1), new { subjectId, exerciseId = 123, metric = "AvgMastery", targetValue = 50 })).StatusCode);

        // Zielwert außerhalb 0..100 (Prozent-Metrik) -> 400.
        Assert.Equal(HttpStatusCode.BadRequest,
            (await father.PostAsJsonAsync(Url(1), new { subjectId, metric = "AvgMastery", targetValue = 150 })).StatusCode);

        // Nicht-Vokabelübung als Scope -> 400 (nur Vokabel ist item-getrackt).
        var (aSub, aChap, aEx) = await TestApi.CreateArithmeticExerciseAsync(father);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await father.PostAsJsonAsync(Url(1), new { subjectId = aSub, chapterId = aChap, exerciseId = aEx, metric = "Coverage", targetValue = 50 })).StatusCode);
    }

    [Fact]
    public async Task RollenUndOwnership()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (subjectId, _, _) = await VocabAsync(father, "Rollen-Fach", ("gerenuk", "Giraffengazelle"));
        var child = await TestApi.ChildAsync(_factory);

        // Der Sohn darf keine Ziele anlegen (nur Vater).
        Assert.Equal(HttpStatusCode.Forbidden,
            (await child.PostAsJsonAsync(Url(1), new { subjectId, metric = "AvgMastery", targetValue = 50 })).StatusCode);

        // Der Sohn darf seine eigenen Ziele lesen.
        (await father.PostAsJsonAsync(Url(1), new { subjectId, metric = "AvgMastery", targetValue = 50 })).EnsureSuccessStatusCode();
        var mine = await child.GetAsync(Url(1));
        mine.EnsureSuccessStatusCode();

        // Fremdes/nicht existierendes Kind -> 404 (Ownership-Filter).
        Assert.Equal(HttpStatusCode.NotFound, (await father.GetAsync(Url(999))).StatusCode);
    }

    [Fact]
    public async Task Aendern_UndLoeschen()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (subjectId, _, _) = await VocabAsync(father, "CRUD-Fach", ("bilby", "Bilby"));

        var created = await (await father.PostAsJsonAsync(Url(1), new { subjectId, metric = "AvgMastery", targetValue = 50, title = "Erst" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var goalId = created.GetProperty("id").GetInt32();

        var patched = await (await father.PatchAsJsonAsync($"{Url(1)}/{goalId}", new { targetValue = 80, title = "Neu" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(80, patched.GetProperty("targetValue").GetInt32());
        Assert.Equal("Neu", patched.GetProperty("title").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync($"{Url(1)}/{goalId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await father.GetAsync($"{Url(1)}/{goalId}")).StatusCode);
    }
}
