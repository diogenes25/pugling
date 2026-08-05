using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Store-linked authoring of exercises: cloze draws its solution from the vocabulary store (P1),
/// materializing vocabulary refs from tags (P2), ref validation + vocabulary usage/delete protection (P3).
/// </summary>
public class VocabExerciseAuthoringTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    /// <summary>Creates subject → series (with the subject set) → unit; returns their ids.</summary>
    private static async Task<(int subjectId, int seriesId, int seriesUnitId)> SeriesUnitAsync(HttpClient f, string name)
    {
        var s = await TestApi.IdAsync(await f.PostAsJsonAsync("/api/v1/creator/subjects", new { name }));
        var sr = await TestApi.IdAsync(await f.PostAsJsonAsync("/api/v1/creator/textbook-series", new
        {
            name = $"{name} Reihe",
            publisher = (string?)null,
            subjectName = (string?)null,
            subjectId = s,
            schoolTypes = (object?)null,
            sourceLanguage = (string?)null,
            targetLanguage = (string?)null,
            notes = (string?)null,
        }));
        var c = await TestApi.IdAsync(await f.PostAsJsonAsync($"/api/v1/creator/textbook-series/{sr}/units",
            new { label = "Unit", grade = (int?)null, orderIndex = 1, topics = (string?)null, grammar = (string?)null, vocabularyNotes = (string?)null }));
        return (s, sr, c);
    }

    private static async Task<JsonElement> CreateVocabAsync(HttpClient f, object body)
    {
        var res = await f.PostAsJsonAsync("/api/v1/creator/vocabulary", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    // ---- P1: cloze gap ↔ vocabulary store -----------------------------------------------------------

    [Fact]
    public async Task Cloze_MitVocabKey_LoestLoesungAusStoreAuf_UndReagiertAufAenderung()
    {
        var father = await TestApi.AdultAsync(_factory);
        var vocab = await CreateVocabAsync(father,
            new { sourceLanguage = "en", targetLanguage = "de", word = "opportunity", translation = "Gelegenheit" });
        var vocabId = vocab.GetProperty("id").GetInt32();
        var key = vocab.GetProperty("key").GetString();

        var (_, sr, c) = await SeriesUnitAsync(father, "Cloze-Store");
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{sr}/units/{c}/cloze", new
            {
                title = "Lückentext Unit",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { text = "The American Dream promises {{1}}.", gaps = new[] { new { index = 1, answer = "", vocabKey = key } } },
            }));

        // A position test on the free-text stage (typed): the solution comes from the store word.
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)ClozeStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        var outcome = await (await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review",
            new { itemIndex = 0, givenAnswer = "opportunity" })).Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(outcome, "wasCorrect");
        Assert.Equal("opportunity", outcome.GetProperty("expected").GetString());

        // A central correction in the store shows through in the gap. A fresh position, because the same item
        // is graded only once per day (anti-farming → 204 otherwise).
        await father.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{vocabId}", new { word = "chance" });
        var (planId2, positionId2) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)ClozeStage.FreeText);
        var base2 = $"/api/v1/student/study-plans/{planId2}/positions/{positionId2}/practice-sessions";
        var s2 = await TestApi.IdAsync(await child.PostAsJsonAsync(base2, new { }));
        var out2 = await (await child.PostAsJsonAsync($"{base2}/{s2}/review",
            new { itemIndex = 0, givenAnswer = "chance" })).Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(out2, "wasCorrect");
    }

    [Fact]
    public async Task Cloze_MitUnbekanntemVocabKey_Liefert400()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, sr, c) = await SeriesUnitAsync(father, "Cloze-Bad");
        var res = await father.PostAsJsonAsync($"/api/v1/creator/textbook-series/{sr}/units/{c}/cloze", new
        {
            title = "Kaputt",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { text = "x {{1}}", gaps = new[] { new { index = 1, answer = "", vocabKey = "gibt_es_nicht" } } },
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ---- P2: materialize refs from tags ---------------------------------------------------------

    [Fact]
    public async Task RefsFromTags_NurGrundformen_SchreibtSnapshotInRefs()
    {
        var father = await TestApi.AdultAsync(_factory);
        var walk = await CreateVocabAsync(father, new { sourceLanguage = "en", targetLanguage = "de", word = "walk", translation = "gehen", tags = new[] { "UnitP2" } });
        var walkKey = walk.GetProperty("key").GetString();
        await CreateVocabAsync(father, new { sourceLanguage = "en", targetLanguage = "de", word = "walked", translation = "ging", baseFormKey = walkKey, baseFormRelation = "Simple Past", tags = new[] { "UnitP2" } });
        await CreateVocabAsync(father, new { sourceLanguage = "en", targetLanguage = "de", word = "jump", translation = "springen", tags = new[] { "UnitP2" } });

        var (_, sr, c) = await SeriesUnitAsync(father, "Refs-Tags");
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{sr}/units/{c}/vocabulary",
            new { title = "Unit-Vokabeln", orderIndex = 1, rewardPoints = 10, config = new { direction = "front-to-back", refs = Array.Empty<string>() } }));

        (await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{sr}/units/{c}/vocabulary/{exerciseId}/refs-from-tags",
            new { tags = new[] { "UnitP2" }, baseFormsOnly = true })).EnsureSuccessStatusCode();

        // The snapshot materializes the words as items (one level deeper), no longer in the config.
        var items = await father.GetFromJsonAsync<List<JsonElement>>(
            $"/api/v1/creator/textbook-series/{sr}/units/{c}/vocabulary/{exerciseId}/items");
        var fronts = items!.Select(i => i.GetProperty("front").GetString()).ToList();
        Assert.Equal(2, fronts.Count); // walk + jump, NOT walked (inflected)
        Assert.Contains("walk", fronts);
        Assert.Contains("jump", fronts);
        Assert.DoesNotContain("walked", fronts);
        _ = walkKey;
    }

    // ---- P3: ref validation + vocabulary usage + delete protection ----------------------------------------

    [Fact]
    public async Task VocabExercise_MitUnbekanntemRef_Liefert400()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, sr, c) = await SeriesUnitAsync(father, "Ref-Bad");
        var res = await father.PostAsJsonAsync($"/api/v1/creator/textbook-series/{sr}/units/{c}/vocabulary",
            new { title = "Kaputt", orderIndex = 1, rewardPoints = 10, config = new { direction = "front-to-back", refs = new[] { "gibt_es_nicht" } } });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task VocabUsage_ListetReferenzierendeUebung_UndLoeschenIst409()
    {
        var father = await TestApi.AdultAsync(_factory);
        var v = await CreateVocabAsync(father, new { sourceLanguage = "en", targetLanguage = "de", word = "kite", translation = "Drachen" });
        var vocabId = v.GetProperty("id").GetInt32();
        var key = v.GetProperty("key").GetString();
        await TestApi.CreateVocabRefExerciseAsync(father, key!);

        var usage = await father.GetFromJsonAsync<List<JsonElement>>($"/api/v1/creator/vocabulary/{vocabId}/usage");
        Assert.Single(usage!);
        Assert.Equal("Vocabulary", usage![0].GetProperty("type").GetString());

        var del = await father.DeleteAsync($"/api/v1/creator/vocabulary/{vocabId}");
        Assert.Equal(HttpStatusCode.Conflict, del.StatusCode);
    }

    // ---- Inline vocabulary is created in the store and linked automatically --------------------------

    [Fact]
    public async Task VocabExercise_InlineItemsOhneId_WerdenImStoreAngelegtUndVerlinkt()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, sr, c) = await SeriesUnitAsync(father, "Inline-Autolink");

        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{sr}/units/{c}/vocabulary", new
            {
                title = "Inline-Vokabeln",
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    direction = "front-to-back",
                    sourceLang = "en",
                    targetLang = "de",
                    items = new[] { new { front = "mountain", back = "Berg" }, new { front = "river", back = "Fluss" } },
                },
            }));

        // The inline items are materialized as items of their own (one level deeper) and linked to the store.
        var items = await father.GetFromJsonAsync<List<JsonElement>>(
            $"/api/v1/creator/textbook-series/{sr}/units/{c}/vocabulary/{exerciseId}/items");
        Assert.Equal(2, items!.Count);
        foreach (var it in items!)
        {
            var id = it.GetProperty("vocabularyId").GetInt32();
            Assert.True(id > 0);
            Assert.Equal($"/api/v1/creator/vocabulary/{id}", it.GetProperty("vocabulary").GetString());
        }

        // And the words really sit in the store now (store membership).
        var berg = await father.GetFromJsonAsync<List<JsonElement>>("/api/v1/creator/vocabulary?word=mountain");
        Assert.Contains(berg!, v => v.GetProperty("translation").GetString() == "Berg");
    }

    // ---- Item input: VocabularyId only (front/back from the store) + validation -------------------

    [Fact]
    public async Task InlineItem_NurVocabularyId_ZiehtFrontBackAusStore()
    {
        var father = await TestApi.AdultAsync(_factory);
        var v = await CreateVocabAsync(father, new { sourceLanguage = "en", targetLanguage = "de", word = "bridge", translation = "Brücke" });
        var vocabId = v.GetProperty("id").GetInt32();
        var (_, sr, c) = await SeriesUnitAsync(father, "Inline-IdOnly");

        // An inline item without front/back - only the store id. Front/back come from the linked store entry.
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{sr}/units/{c}/vocabulary", new
            {
                title = "Nur-Id",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { direction = "front-to-back", items = new[] { new { vocabularyId = vocabId } } },
            }));

        var items = await father.GetFromJsonAsync<List<JsonElement>>(
            $"/api/v1/creator/textbook-series/{sr}/units/{c}/vocabulary/{exerciseId}/items");
        var item = Assert.Single(items!);
        Assert.Equal(vocabId, item.GetProperty("vocabularyId").GetInt32());
        Assert.Equal("bridge", item.GetProperty("front").GetString());
        Assert.Equal("Brücke", item.GetProperty("back").GetString());
    }

    [Fact]
    public async Task ItemEndpunkt_NurVocabularyId_LiefertFrontBackAusStore()
    {
        var father = await TestApi.AdultAsync(_factory);
        var v = await CreateVocabAsync(father, new { sourceLanguage = "en", targetLanguage = "de", word = "castle", translation = "Schloss" });
        var vocabId = v.GetProperty("id").GetInt32();
        var (_, sr, c) = await SeriesUnitAsync(father, "ItemEP-IdOnly");
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{sr}/units/{c}/vocabulary",
            new { title = "Hülle", orderIndex = 1, rewardPoints = 10, config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de" } }));

        // An item through the item endpoint with the VocabularyId only (front/back empty → from the store).
        var res = await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{sr}/units/{c}/vocabulary/{exerciseId}/items", new { vocabularyId = vocabId });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var item = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(vocabId, item.GetProperty("vocabularyId").GetInt32());
        Assert.Equal("castle", item.GetProperty("front").GetString());
        Assert.Equal("Schloss", item.GetProperty("back").GetString());
    }

    [Fact]
    public async Task InlineItem_OhneIdUndOhneFrontBack_Liefert400()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, sr, c) = await SeriesUnitAsync(father, "Inline-Leer");
        var res = await father.PostAsJsonAsync($"/api/v1/creator/textbook-series/{sr}/units/{c}/vocabulary", new
        {
            title = "Leeres Item",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = new[] { new { hint = "nur ein Hinweis" } } },
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ---- Task 1: separate search parameters word/translation ------------------------------------------

    [Fact]
    public async Task VocabularyList_FiltertNachWordUndTranslation()
    {
        var father = await TestApi.AdultAsync(_factory);
        await CreateVocabAsync(father, new { sourceLanguage = "en", targetLanguage = "de", word = "elephant", translation = "Elefant" });
        await CreateVocabAsync(father, new { sourceLanguage = "en", targetLanguage = "de", word = "mouse", translation = "Maus" });

        var byWord = await father.GetFromJsonAsync<List<JsonElement>>("/api/v1/creator/vocabulary?word=elephant");
        Assert.All(byWord!, v => Assert.Contains("elephant", v.GetProperty("word").GetString()!));
        Assert.Contains(byWord!, v => v.GetProperty("translation").GetString() == "Elefant");

        var byTranslation = await father.GetFromJsonAsync<List<JsonElement>>("/api/v1/creator/vocabulary?translation=Maus");
        Assert.All(byTranslation!, v => Assert.Contains("Maus", v.GetProperty("translation").GetString()!));
        Assert.DoesNotContain(byTranslation!, v => v.GetProperty("word").GetString() == "elephant");
    }
}
