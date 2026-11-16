using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-446: the hint derived from a store entry is its article, which the contract defines as the target language's
/// article - a clue to the answer. An entry whose article is the one the asked word already starts with describes the
/// question side; its hint would repeat the prompt, so none is derived.
/// </summary>
public class VokabelTippTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>Hint of the first card of a one-word exercise over a store entry with the given article.</summary>
    private async Task<string?> HintAsync(string word, string translation, string src, string article, string direction = "front-to-back")
    {
        var father = await TestApi.AdultAsync(factory);
        var (id, _) = await TestApi.CreateStoreVocabAsync(father, word, translation, src: src, tgt: TestApi.UniqueName("t"));
        (await father.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{id}",
            new { partOfSpeech = "Noun", noun = new { article } })).EnsureSuccessStatusCode();
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Tipp") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary", new
            {
                title = "Tipp",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { direction, refs = new[] { new { vocabularyId = id } } },
            }));
        // The choice: there the German side is the answer the article hints at. A writing stage asks the learnt word and
        // drops the German article with the turn (B-597).
        var data = await father.GetFromJsonAsync<JsonElement>(
            $"/api/v1/creator/exercises/{exerciseId}/preview?stage={(int)TestStage.MultipleChoice}");
        var hint = data.GetProperty("items").EnumerateArray().First().GetProperty("hint");
        return hint.ValueKind == JsonValueKind.Null ? null : hint.GetString();
    }

    // Red if the derived article is dropped altogether: the target-language article is a real clue.
    [Fact]
    public async Task Artikel_Der_Zielsprache_Bleibt_Tipp() =>
        Assert.Equal("das", await HintAsync("house", "Haus", "en", "das"));

    // Red if the guard is removed: "la" for "la ville" would repeat the prompt.
    [Fact]
    public async Task Artikel_Den_Das_Fragewort_Traegt_Ist_Kein_Tipp() =>
        Assert.Null(await HintAsync("la ville", "die Stadt", "fr", "la"));

    // Red if the elided form is not recognized: "l'école" starts with "l'" without a space.
    [Fact]
    public async Task Elidierter_Artikel_Des_Fragewortes_Ist_Kein_Tipp() =>
        Assert.Null(await HintAsync("l'école", "die Schule", "fr", "l'"));

    // Red if the guard only compares the first letters: "le" is not the article of "lecteur".
    [Fact]
    public async Task Gleicher_Wortanfang_Ohne_Grenze_Bleibt_Tipp() =>
        Assert.Equal("le", await HintAsync("lecteur", "Leser", "fr", "le"));

    // Red if the typographic apostrophe is dropped from the guard: "l’" is how a photographed textbook spells it.
    [Fact]
    public async Task Typografisch_Elidierter_Artikel_Ist_Kein_Tipp() =>
        Assert.Null(await HintAsync("l’école", "die Schule", "fr", "l’"));

    // Red if the whole-word branch goes: a word equal to its article would index past its end (500).
    [Fact]
    public async Task Wort_Gleich_Artikel_Ist_Kein_Tipp() =>
        Assert.Null(await HintAsync("la", "die", "fr", "la"));

    // Red if the seed writes the French article again: NounInfo.Article is the target language's ("die", not "la").
    [Fact]
    public async Task Franzoesischer_Seed_Traegt_Den_Artikel_Der_Zielsprache()
    {
        var father = await TestApi.AdultAsync(factory);
        var rows = await father.GetFromJsonAsync<JsonElement>("/api/v1/creator/vocabulary?sourceLanguage=fr&word=la%20ville");
        // Other tests in this class fixture store "la ville" too, without a key of their own; the seed's entry is the
        // one under the seed key.
        var entry = rows.EnumerateArray().Single(v => v.GetProperty("key").GetString() == "fr_la_ville_de_die_stadt");
        Assert.Equal("die", entry.GetProperty("noun").GetProperty("article").GetString());
    }
}
