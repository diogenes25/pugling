using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-71: equally valid translations entered on an exercise item (<c>vocabulary/{id}/items</c>) land on the
/// item's store entry – the one list per word that grading reads – instead of a second, exercise-local list.
/// </summary>
public class VocabItemAlternativesTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    /// <summary>A vocabulary exercise (en → de) with one seed item; returns the item collection URL.</summary>
    private static async Task<(string itemsUrl, int exerciseId)> ExerciseAsync(HttpClient father)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Alternativen-Fach") }));
        var (s, c) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{s}/units/{c}/vocabulary", new
            {
                title = "Alternativen-Übung",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = new[] { new { front = Word("seed"), back = "Saat" } } },
            }));
        return ($"/api/v1/creator/textbook-series/{s}/units/{c}/vocabulary/{exerciseId}/items", exerciseId);
    }

    // Store keys are global across the shared test database, so every test word is unique.
    private static string Word(string stem) => $"{stem}-{Guid.NewGuid():N}"[..20];

    private static async Task<string[]> StoreAlternativesAsync(HttpClient father, int vocabularyId)
    {
        var entry = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/vocabulary/{vocabularyId}");
        var field = entry.GetProperty("translationAlternatives");
        // The store normalises "none" to null; empty here means exactly that.
        return field.ValueKind == JsonValueKind.Null ? [] : [.. field.EnumerateArray().Select(a => a.GetString()!)];
    }

    /// <summary>AK 1 + AK 4: sent inline, cleaned like in the store, visible on the store entry and read back on the item.</summary>
    [Fact]
    public async Task AddItem_Inline_MitAlternativen_LandenAmStoreEintrag_UndKommenZurueck()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (itemsUrl, _) = await ExerciseAsync(father);

        var res = await father.PostAsJsonAsync(itemsUrl, new
        {
            front = Word("huge"),
            back = "riesig",
            // Same cleaning as the store: trimmed, blanks/duplicates/the primary translation dropped.
            translationAlternatives = new[] { " sehr groß ", "", "Sehr groß", "riesig", "gewaltig" },
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var item = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(["sehr groß", "gewaltig"],
            item.GetProperty("translationAlternatives").EnumerateArray().Select(a => a.GetString()));

        var vocabularyId = item.GetProperty("vocabularyId").GetInt32();
        Assert.Equal(["sehr groß", "gewaltig"], await StoreAlternativesAsync(father, vocabularyId));

        // Round trip: the single item and the list both carry them.
        var one = await father.GetFromJsonAsync<JsonElement>($"{itemsUrl}/{item.GetProperty("id").GetInt32()}");
        Assert.Equal(2, one.GetProperty("translationAlternatives").GetArrayLength());
        var list = await father.GetFromJsonAsync<List<JsonElement>>(itemsUrl);
        Assert.Equal(2, list!.Single(i => i.GetProperty("vocabularyId").GetInt32() == vocabularyId)
            .GetProperty("translationAlternatives").GetArrayLength());
    }

    /// <summary>
    /// AK 3: the inline word already exists in the store (found by its key) – the sent alternatives still land
    /// there, they are not dropped because <c>GetOrCreateAsync</c> only fills a NEW entry. Plus the risk named
    /// in the story: a second exercise using the same word without the field leaves the list alone.
    /// </summary>
    [Fact]
    public async Task AddItem_TrifftBestehendenEintrag_UebernimmtAlternativen_OhneFeldBleibtListeStehen()
    {
        var father = await TestApi.AdultAsync(_factory);
        var word = Word("dog");
        var (storeId, _) = await TestApi.CreateStoreVocabAsync(father, word, "Hund");
        var (firstUrl, _) = await ExerciseAsync(father);
        var (secondUrl, _) = await ExerciseAsync(father);

        var first = await (await father.PostAsJsonAsync(firstUrl, new { front = word, back = "Hund", translationAlternatives = new[] { "Köter" } }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(storeId, first.GetProperty("vocabularyId").GetInt32());
        Assert.Equal(["Köter"], await StoreAlternativesAsync(father, storeId));

        // Same word in another exercise, field omitted: "not specified", the shared list stays.
        var second = await father.PostAsJsonAsync(secondUrl, new { front = word, back = "Hund" });
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(["Köter"], await StoreAlternativesAsync(father, storeId));
    }

    /// <summary>A rejected item (word already in the exercise) must not have changed the shared store entry.</summary>
    [Fact]
    public async Task AddItem_Abgelehnt_AendertDenStoreEintragNicht()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (itemsUrl, _) = await ExerciseAsync(father);
        var word = Word("cat");
        var created = await (await father.PostAsJsonAsync(itemsUrl, new { front = word, back = "Katze", translationAlternatives = new[] { "Mieze" } }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var vocabularyId = created.GetProperty("vocabularyId").GetInt32();

        // Inline, not by id: only the inline path saves inside ResolveVocabularyIdAsync before the 409 check, so a
        // write step slipped in there would persist - the by-id path would stay green under that regression.
        var again = await father.PostAsJsonAsync(itemsUrl, new { front = word, back = "Katze", translationAlternatives = new[] { "Kater" } });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(["Mieze"], await StoreAlternativesAsync(father, vocabularyId));
    }

    /// <summary>AK 2: PATCH adds/replaces, an omitted field keeps the list, an explicitly empty list deletes it.</summary>
    [Fact]
    public async Task PatchItem_Ergaenzt_LaesstOhneFeldStehen_UndLoeschtMitLeererListe()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (itemsUrl, _) = await ExerciseAsync(father);
        var created = await (await father.PostAsJsonAsync(itemsUrl, new { front = Word("big"), back = "groß" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var itemUrl = $"{itemsUrl}/{created.GetProperty("id").GetInt32()}";
        var vocabularyId = created.GetProperty("vocabularyId").GetInt32();
        Assert.Empty(await StoreAlternativesAsync(father, vocabularyId));

        var added = await father.PatchAsJsonAsync(itemUrl, new { translationAlternatives = new[] { "dick" } });
        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        Assert.Equal(["dick"], await StoreAlternativesAsync(father, vocabularyId));

        // Another field only: the list must survive (null = unchanged).
        (await father.PatchAsJsonAsync(itemUrl, new { hint = "Größe" })).EnsureSuccessStatusCode();
        Assert.Equal(["dick"], await StoreAlternativesAsync(father, vocabularyId));

        (await father.PatchAsJsonAsync(itemUrl, new { translationAlternatives = new[] { "mächtig", "stark" } })).EnsureSuccessStatusCode();
        Assert.Equal(["mächtig", "stark"], await StoreAlternativesAsync(father, vocabularyId));

        var cleared = await (await father.PatchAsJsonAsync(itemUrl, new { translationAlternatives = Array.Empty<string>() }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, cleared.GetProperty("translationAlternatives").ValueKind);
        Assert.Empty(await StoreAlternativesAsync(father, vocabularyId));
    }

    /// <summary>A word swap plus alternatives in one PATCH writes them onto the NEW entry, not the replaced one.</summary>
    [Fact]
    public async Task PatchItem_WortTausch_SchreibtAufDenNeuenEintrag()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (itemsUrl, _) = await ExerciseAsync(father);
        var created = await (await father.PostAsJsonAsync(itemsUrl, new { front = Word("old"), back = "alt" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var oldId = created.GetProperty("vocabularyId").GetInt32();

        var swapped = await (await father.PatchAsJsonAsync($"{itemsUrl}/{created.GetProperty("id").GetInt32()}",
            new { front = Word("new"), back = "neu", translationAlternatives = new[] { "frisch" } }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var newId = swapped.GetProperty("vocabularyId").GetInt32();

        Assert.NotEqual(oldId, newId);
        Assert.Equal(["frisch"], await StoreAlternativesAsync(father, newId));
        Assert.Empty(await StoreAlternativesAsync(father, oldId));
    }

    /// <summary>
    /// AK 6: an alternative entered on this second way counts as correct in the position test – the grading
    /// path is B-65's, reached here through the item endpoint instead of the store. Asked on the choice, where the German
    /// side is the answer; a writing stage asks the learnt word (B-597).
    /// </summary>
    [Fact]
    public async Task Test_UeberDasItemHinterlegteAlternative_WirdAlsRichtigGewertet()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (itemsUrl, exerciseId) = await ExerciseAsync(father);
        var seed = (await father.GetFromJsonAsync<List<JsonElement>>(itemsUrl))!.Single();
        // The seed item is the only word, so item index 0 in the test is this one.
        (await father.PatchAsJsonAsync($"{itemsUrl}/{seed.GetProperty("id").GetInt32()}",
            new { translationAlternatives = new[] { "Samen" } })).EnsureSuccessStatusCode();

        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.MultipleChoice);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var submit = await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "Samen" } },
        });

        var res = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, res.GetProperty("correctItems").GetInt32());
    }

    /// <summary>AK 7: a misspelt field name is still rejected – no silent special case for the new one.</summary>
    [Fact]
    public async Task AddItem_FalschBenanntesFeld_Liefert400UnknownField()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (itemsUrl, _) = await ExerciseAsync(father);

        var res = await father.PostAsJsonAsync(itemsUrl, new { front = Word("typo"), back = "Tippfehler", translationAlternative = new[] { "Vertipper" } });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("unknown_field", problem.GetProperty("code").GetString());
    }
}
