using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Vokabel-Übung, die den Store per Key referenziert (Block 3): Inhalt/Karten kommen aus dem Komplextyp,
/// und eine zentrale Store-Änderung wirkt in der Übung (Verknüpfung über Übungen hinweg).
/// </summary>
public class PositionVocabRefTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    [Fact]
    public async Task VokabelUebung_MitStoreRefs_LiefertKartenUndBewertetAusDemStore()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, k1) = await TestApi.CreateStoreVocabAsync(father, "hello", "hallo");
        var (_, k2) = await TestApi.CreateStoreVocabAsync(father, "bye", "tschüss");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, k1, k2);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions";

        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        var cards = await (await child.GetAsync($"{baseUrl}/{sessionId}/cards"))
            .Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.Equal(2, cards!.Count);
        Assert.Equal("hello", cards[0].GetProperty("prompt").GetString());

        var outcome = await (await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review",
            new { itemIndex = 0, givenAnswer = "hallo" })).Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(outcome, "wasCorrect");
        Assert.Equal("hallo", outcome.GetProperty("expected").GetString());
    }

    [Fact]
    public async Task StoreAenderung_WirktSofortInDerUebung()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (id, key) = await TestApi.CreateStoreVocabAsync(father, "cat", "Katze");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions";

        // Zentrale Korrektur der Übersetzung im Store …
        await father.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{id}", new { translation = "Mieze" });

        // … schlägt sofort in der referenzierenden Übung durch: die neue Lösung ist maßgeblich.
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        var outcome = await (await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review",
            new { itemIndex = 0, givenAnswer = "Mieze" })).Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(outcome, "wasCorrect");
    }
}
