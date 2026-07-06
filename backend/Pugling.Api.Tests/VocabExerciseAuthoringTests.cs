using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Store-verknüpftes Erstellen von Übungen: Lückentext bezieht die Lösung aus dem Vokabel-Store (P1),
/// Vokabel-Refs aus Tags materialisieren (P2), Ref-Validierung + Vokabel-Usage/Lösch-Schutz (P3).
/// </summary>
public class VocabExerciseAuthoringTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private static async Task<(int subjectId, int chapterId)> ChapterAsync(HttpClient f, string name)
    {
        var s = await TestApi.IdAsync(await f.PostAsJsonAsync("/api/v1/learn/subjects", new { name }));
        var c = await TestApi.IdAsync(await f.PostAsJsonAsync($"/api/v1/learn/subjects/{s}/chapters",
            new { name = "Unit", orderIndex = 1 }));
        return (s, c);
    }

    private static async Task<JsonElement> CreateVocabAsync(HttpClient f, object body)
    {
        var res = await f.PostAsJsonAsync("/api/v1/learn/vocabulary", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    // ---- P1: Cloze-Lücke ↔ Vokabel-Store -----------------------------------------------------------

    [Fact]
    public async Task Cloze_MitVocabKey_LoestLoesungAusStoreAuf_UndReagiertAufAenderung()
    {
        var father = await TestApi.FatherAsync(_factory);
        var vocab = await CreateVocabAsync(father,
            new { sourceLanguage = "en", targetLanguage = "de", word = "opportunity", translation = "Gelegenheit" });
        var vocabId = vocab.GetProperty("id").GetInt32();
        var key = vocab.GetProperty("key").GetString();

        var (s, c) = await ChapterAsync(father, "Cloze-Store");
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{s}/chapters/{c}/cloze", new
            {
                title = "Lückentext Unit",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { text = "The American Dream promises {{1}}.", gaps = new[] { new { index = 1, answer = "", vocabKey = key } } },
            }));

        // Positions-Test auf FreeText-Stufe (getippt): die Lösung kommt aus dem Store-Wort.
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)ClozeStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/study-plans/{planId}/positions/{positionId}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        var outcome = await (await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review",
            new { itemIndex = 0, givenAnswer = "opportunity" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(outcome.GetProperty("wasCorrect").GetBoolean());
        Assert.Equal("opportunity", outcome.GetProperty("expected").GetString());

        // Zentrale Korrektur im Store schlägt in der Lücke durch. Frische Position, weil dasselbe Item
        // am selben Tag nur einmal gewertet wird (Anti-Farming → sonst 204).
        await father.PatchAsJsonAsync($"/api/v1/learn/vocabulary/{vocabId}", new { word = "chance" });
        var (planId2, positionId2) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)ClozeStage.FreeText);
        var base2 = $"/api/v1/study-plans/{planId2}/positions/{positionId2}/practice-sessions";
        var s2 = await TestApi.IdAsync(await child.PostAsJsonAsync(base2, new { }));
        var out2 = await (await child.PostAsJsonAsync($"{base2}/{s2}/review",
            new { itemIndex = 0, givenAnswer = "chance" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(out2.GetProperty("wasCorrect").GetBoolean());
    }

    [Fact]
    public async Task Cloze_MitUnbekanntemVocabKey_Liefert400()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (s, c) = await ChapterAsync(father, "Cloze-Bad");
        var res = await father.PostAsJsonAsync($"/api/v1/learn/subjects/{s}/chapters/{c}/cloze", new
        {
            title = "Kaputt",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { text = "x {{1}}", gaps = new[] { new { index = 1, answer = "", vocabKey = "gibt_es_nicht" } } },
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ---- P2: Refs aus Tags materialisieren ---------------------------------------------------------

    [Fact]
    public async Task RefsFromTags_NurGrundformen_SchreibtSnapshotInRefs()
    {
        var father = await TestApi.FatherAsync(_factory);
        var walk = await CreateVocabAsync(father, new { sourceLanguage = "en", targetLanguage = "de", word = "walk", translation = "gehen", tags = new[] { "UnitP2" } });
        var walkKey = walk.GetProperty("key").GetString();
        await CreateVocabAsync(father, new { sourceLanguage = "en", targetLanguage = "de", word = "walked", translation = "ging", baseFormKey = walkKey, baseFormRelation = "Simple Past", tags = new[] { "UnitP2" } });
        await CreateVocabAsync(father, new { sourceLanguage = "en", targetLanguage = "de", word = "jump", translation = "springen", tags = new[] { "UnitP2" } });

        var (s, c) = await ChapterAsync(father, "Refs-Tags");
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{s}/chapters/{c}/vocabulary",
            new { title = "Unit-Vokabeln", orderIndex = 1, rewardPoints = 10, config = new { direction = "front-to-back", refs = Array.Empty<string>() } }));

        var updated = await (await father.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{s}/chapters/{c}/vocabulary/{exerciseId}/refs-from-tags",
            new { tags = new[] { "UnitP2" }, baseFormsOnly = true })).Content.ReadFromJsonAsync<JsonElement>();

        var refs = updated.GetProperty("config").GetProperty("refs").EnumerateArray()
            .Select(r => r.GetProperty("key").GetString()).ToList();
        Assert.Equal(2, refs.Count); // walk + jump, NICHT walked (flektiert)
        Assert.Contains(walkKey, refs);
        Assert.DoesNotContain("en_walked_de_ging", refs);
    }

    // ---- P3: Ref-Validierung + Vokabel-Usage + Lösch-Schutz ----------------------------------------

    [Fact]
    public async Task VocabExercise_MitUnbekanntemRef_Liefert400()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (s, c) = await ChapterAsync(father, "Ref-Bad");
        var res = await father.PostAsJsonAsync($"/api/v1/learn/subjects/{s}/chapters/{c}/vocabulary",
            new { title = "Kaputt", orderIndex = 1, rewardPoints = 10, config = new { direction = "front-to-back", refs = new[] { "gibt_es_nicht" } } });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task VocabUsage_ListetReferenzierendeUebung_UndLoeschenIst409()
    {
        var father = await TestApi.FatherAsync(_factory);
        var v = await CreateVocabAsync(father, new { sourceLanguage = "en", targetLanguage = "de", word = "kite", translation = "Drachen" });
        var vocabId = v.GetProperty("id").GetInt32();
        var key = v.GetProperty("key").GetString();
        await TestApi.CreateVocabRefExerciseAsync(father, key!);

        var usage = await father.GetFromJsonAsync<List<JsonElement>>($"/api/v1/learn/vocabulary/{vocabId}/usage");
        Assert.Single(usage!);
        Assert.Equal("Vocabulary", usage![0].GetProperty("type").GetString());

        var del = await father.DeleteAsync($"/api/v1/learn/vocabulary/{vocabId}");
        Assert.Equal(HttpStatusCode.Conflict, del.StatusCode);
    }

    // ---- Inline-Vokabeln werden automatisch im Store angelegt und verlinkt --------------------------

    [Fact]
    public async Task VocabExercise_InlineItemsOhneId_WerdenImStoreAngelegtUndVerlinkt()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (s, c) = await ChapterAsync(father, "Inline-Autolink");

        var created = await (await father.PostAsJsonAsync(
            $"/api/v1/learn/subjects/{s}/chapters/{c}/vocabulary", new
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
            })).Content.ReadFromJsonAsync<JsonElement>();

        // Jedes Inline-Item bekommt eine Store-Id und einen _self-Link.
        var items = created.GetProperty("config").GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        foreach (var it in items)
        {
            var id = it.GetProperty("vocabularyId").GetInt32();
            Assert.True(id > 0);
            Assert.Equal($"/api/v1/learn/vocabulary/{id}", it.GetProperty("_self").GetString());
        }

        // Und die Wörter liegen jetzt tatsächlich im Store (Store-Membership).
        var berg = await father.GetFromJsonAsync<List<JsonElement>>("/api/v1/learn/vocabulary?word=mountain");
        Assert.Contains(berg!, v => v.GetProperty("translation").GetString() == "Berg");
    }

    // ---- Task 1: getrennte Suchparameter word/translation ------------------------------------------

    [Fact]
    public async Task VocabularyList_FiltertNachWordUndTranslation()
    {
        var father = await TestApi.FatherAsync(_factory);
        await CreateVocabAsync(father, new { sourceLanguage = "en", targetLanguage = "de", word = "elephant", translation = "Elefant" });
        await CreateVocabAsync(father, new { sourceLanguage = "en", targetLanguage = "de", word = "mouse", translation = "Maus" });

        var byWord = await father.GetFromJsonAsync<List<JsonElement>>("/api/v1/learn/vocabulary?word=elephant");
        Assert.All(byWord!, v => Assert.Contains("elephant", v.GetProperty("word").GetString()!));
        Assert.Contains(byWord!, v => v.GetProperty("translation").GetString() == "Elefant");

        var byTranslation = await father.GetFromJsonAsync<List<JsonElement>>("/api/v1/learn/vocabulary?translation=Maus");
        Assert.All(byTranslation!, v => Assert.Contains("Maus", v.GetProperty("translation").GetString()!));
        Assert.DoesNotContain(byTranslation!, v => v.GetProperty("word").GetString() == "elephant");
    }
}
