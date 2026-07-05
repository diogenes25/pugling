using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Integrationstests für den Birkenbihl-Übungstyp: das geerbte CRUD (typisiertes Zurücklesen der Config)
/// sowie die vokabel-gestützte Automatik – automatisches Dekodieren eines Satzes gegen den Vokabelspeicher,
/// einzelnes Austauschen von Wörtern (Homonym-Korrektur), Kandidaten mehrdeutiger Wörter, die zustandslose
/// Vorschau und die Rollen-Absicherung der neuen Endpunkte.
/// </summary>
public class BirkenbihlExerciseTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<HttpClient> FatherClientAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/father", new { fatherId = 1, pin = "0000" });
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<HttpClient> ChildClientAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/child", new { childId = 1, pin = "1111" });
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Legt Fach + Kapitel an und gibt die Basis-Route der Birkenbihl-Übungen zurück.</summary>
    private static async Task<string> CreateChapterAsync(HttpClient father)
    {
        var subjectRes = await father.PostAsJsonAsync("/api/v1/learn/subjects", new { name = "Birkenbihl-Test" });
        var subjectId = (await subjectRes.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var chapterRes = await father.PostAsJsonAsync($"/api/v1/learn/subjects/{subjectId}/chapters",
            new { name = "Lektion 1", orderIndex = 1 });
        var chapterId = (await chapterRes.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        return $"/api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/birkenbihl";
    }

    /// <summary>Legt eine leere Birkenbihl-Übung mit Sprachpaar an und gibt (Basis-Route, ExerciseId) zurück.</summary>
    private static async Task<(string Route, int ExerciseId)> CreateExerciseAsync(
        HttpClient father, string learningLang = "en", string nativeLang = "de")
    {
        var route = await CreateChapterAsync(father);
        var res = await father.PostAsJsonAsync(route, new
        {
            title = "Birkenbihl",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { learningLang, nativeLang },
        });
        res.EnsureSuccessStatusCode();
        var id = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return (route, id);
    }

    /// <summary>Legt eine Vokabel an und gibt ihre Id zurück (Vater-Katalog, global).</summary>
    private static async Task<int> CreateVocabAsync(HttpClient father, string word, string translation,
        string src = "en", string tgt = "de", string partOfSpeech = "Other")
    {
        var res = await father.PostAsJsonAsync("/api/v1/learn/vocabulary", new
        {
            sourceLanguage = src,
            targetLanguage = tgt,
            word,
            translation,
            partOfSpeech,
        });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private static bool IsNull(JsonElement e, string prop) =>
        e.GetProperty(prop).ValueKind == JsonValueKind.Null;

    [Fact]
    public async Task CrudRoundtrip_ConfigBleibtTypisiertErhalten()
    {
        var father = await FatherClientAsync();
        var route = await CreateChapterAsync(father);

        var payload = new
        {
            title = "Getting to know each other",
            orderIndex = 1,
            rewardPoints = 10,
            config = new
            {
                learningLang = "en",
                nativeLang = "de",
                nextSentenceId = 2,
                nextWordId = 5,
                sentences = new[]
                {
                    new
                    {
                        sentenceId = 1,
                        learningSentence = "What is your name?",
                        naturalTranslation = "Wie heißt du?",
                        decoding = new[]
                        {
                            new { wordId = 1, learningWord = "What", gloss = "Was", vocabularyId = (int?)null },
                            new { wordId = 2, learningWord = "is", gloss = "ist", vocabularyId = (int?)null },
                            new { wordId = 3, learningWord = "your", gloss = "dein", vocabularyId = (int?)null },
                            new { wordId = 4, learningWord = "name", gloss = "Name", vocabularyId = (int?)null },
                        },
                    },
                },
            },
        };

        var createRes = await father.PostAsJsonAsync(route, payload);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var exerciseId = (await createRes.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var body = await (await father.GetAsync($"{route}/{exerciseId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Birkenbihl", body.GetProperty("type").GetString());
        var config = body.GetProperty("config");
        Assert.Equal("en", config.GetProperty("learningLang").GetString());

        var sentence = config.GetProperty("sentences").EnumerateArray().Single();
        Assert.Equal(1, sentence.GetProperty("sentenceId").GetInt32());
        Assert.Equal("What is your name?", sentence.GetProperty("learningSentence").GetString());
        Assert.Equal("Wie heißt du?", sentence.GetProperty("naturalTranslation").GetString());

        var decoding = sentence.GetProperty("decoding").EnumerateArray().ToArray();
        Assert.Equal(4, decoding.Length);
        Assert.Equal("What", decoding[0].GetProperty("learningWord").GetString());
        Assert.Equal("Was", decoding[0].GetProperty("gloss").GetString());
        Assert.Equal(4, decoding[3].GetProperty("wordId").GetInt32());
    }

    /// <summary>
    /// Das Anlege-Formular liefert Sätze OHNE ids/Zähler (so wie der Frontend-Client). Der Server muss beim
    /// Speichern übungsweit eindeutige sentenceId/wordId vergeben (NormalizeConfig), damit die Wort-Endpunkte
    /// funktionieren – und ein späteres AddSentence darf keine bereits vergebene wordId erneut ausgeben.
    /// </summary>
    [Fact]
    public async Task Create_OhneIds_VergibtEindeutigeIds_UndAddSentenceKollidiertNicht()
    {
        var father = await FatherClientAsync();
        var route = await CreateChapterAsync(father);

        // Genau die Form, die das Vater-Formular schickt: keine sentenceId/wordId, keine Zähler.
        var payload = new
        {
            title = "Ohne IDs",
            orderIndex = 1,
            rewardPoints = 10,
            config = new
            {
                learningLang = "en",
                nativeLang = "de",
                sentences = new[]
                {
                    new
                    {
                        learningSentence = "What is your name?",
                        naturalTranslation = "Wie heißt du?",
                        decoding = new[]
                        {
                            new { learningWord = "What", gloss = "Was" },
                            new { learningWord = "is", gloss = "ist" },
                        },
                    },
                },
            },
        };

        var createRes = await father.PostAsJsonAsync(route, payload);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var exerciseId = (await createRes.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var config = (await (await father.GetAsync($"{route}/{exerciseId}")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("config");
        var sentence = config.GetProperty("sentences").EnumerateArray().Single();
        // Satz-Text erhalten (nicht null) + eindeutige, positive IDs vergeben.
        Assert.Equal("What is your name?", sentence.GetProperty("learningSentence").GetString());
        Assert.True(sentence.GetProperty("sentenceId").GetInt32() > 0);
        var words = sentence.GetProperty("decoding").EnumerateArray()
            .Select(w => w.GetProperty("wordId").GetInt32()).ToArray();
        Assert.All(words, id => Assert.True(id > 0));
        Assert.Equal(words.Length, words.Distinct().Count());

        // Neuen Satz ergänzen – seine wordIds dürfen sich nicht mit den bestehenden überschneiden.
        var addRes = await father.PostAsJsonAsync($"{route}/{exerciseId}/sentences",
            new { learningSentence = "Good morning", naturalTranslation = "Guten Morgen" });
        Assert.Equal(HttpStatusCode.Created, addRes.StatusCode);

        var after = (await (await father.GetAsync($"{route}/{exerciseId}")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("config");
        var allWordIds = after.GetProperty("sentences").EnumerateArray()
            .SelectMany(s => s.GetProperty("decoding").EnumerateArray())
            .Select(w => w.GetProperty("wordId").GetInt32()).ToArray();
        Assert.Equal(allWordIds.Length, allWordIds.Distinct().Count());
        var sentenceIds = after.GetProperty("sentences").EnumerateArray()
            .Select(s => s.GetProperty("sentenceId").GetInt32()).ToArray();
        Assert.Equal(sentenceIds.Length, sentenceIds.Distinct().Count());
    }

    [Fact]
    public async Task AddSentence_VerlinktBekannteWoerter_LaesstUnbekannteLeer()
    {
        var father = await FatherClientAsync();
        var (route, exerciseId) = await CreateExerciseAsync(father);
        var howId = await CreateVocabAsync(father, "How", "Wie", partOfSpeech: "Adverb");
        var areId = await CreateVocabAsync(father, "are", "bist", partOfSpeech: "Verb");

        var res = await father.PostAsJsonAsync($"{route}/{exerciseId}/sentences",
            new { learningSentence = "How are you?", naturalTranslation = "Wie geht es dir?" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1, body.GetProperty("sentenceId").GetInt32());
        var result = body.GetProperty("result").EnumerateArray().ToArray();
        Assert.Equal(3, result.Length);

        Assert.Equal("How", result[0].GetProperty("learningWord").GetString());
        Assert.Equal("Wie", result[0].GetProperty("gloss").GetString());
        Assert.Equal(howId, result[0].GetProperty("vocabularyId").GetInt32());
        Assert.Equal($"/api/v1/learn/vocabulary/{howId}", result[0].GetProperty("vocabularySrc").GetString());

        Assert.Equal("bist", result[1].GetProperty("gloss").GetString());
        Assert.Equal(areId, result[1].GetProperty("vocabularyId").GetInt32());

        // "you" ist nicht im Speicher → alles null.
        Assert.Equal("you", result[2].GetProperty("learningWord").GetString());
        Assert.True(IsNull(result[2], "gloss"));
        Assert.True(IsNull(result[2], "vocabularyId"));
        Assert.True(IsNull(result[2], "vocabularySrc"));

        // Wort-Ids sind übungsweit eindeutig vergeben.
        Assert.Equal([1, 2, 3], result.Select(w => w.GetProperty("wordId").GetInt32()).ToArray());
    }

    [Fact]
    public async Task Homonym_LiefertKandidaten_UndWirdPerWortEndpunktKorrigiert()
    {
        var father = await FatherClientAsync();
        var (route, exerciseId) = await CreateExerciseAsync(father);
        var geldId = await CreateVocabAsync(father, "bank", "Bank", partOfSpeech: "Noun");
        var uferId = await CreateVocabAsync(father, "bank", "Ufer", partOfSpeech: "Noun");

        var add = await (await father.PostAsJsonAsync($"{route}/{exerciseId}/sentences",
            new { learningSentence = "the bank", naturalTranslation = "das Ufer" })).Content.ReadFromJsonAsync<JsonElement>();
        var bank = add.GetProperty("result").EnumerateArray().Single(w => w.GetProperty("learningWord").GetString() == "bank");
        var wordId = bank.GetProperty("wordId").GetInt32();

        // Mehrdeutig → beide Karten als Kandidaten.
        var candidates = bank.GetProperty("candidates").EnumerateArray().ToArray();
        Assert.Equal(2, candidates.Length);
        Assert.Contains(candidates, c => c.GetProperty("vocabularyId").GetInt32() == geldId);
        Assert.Contains(candidates, c => c.GetProperty("vocabularyId").GetInt32() == uferId);

        // Auch der dedizierte Kandidaten-Endpunkt liefert beide.
        var epCandidates = await (await father.GetAsync($"{route}/{exerciseId}/words/{wordId}/candidates"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, epCandidates.EnumerateArray().Count());

        // Falsche Bedeutung gezielt auf "Ufer" korrigieren.
        var put = await father.PutAsJsonAsync($"{route}/{exerciseId}/words/{wordId}", new { vocabularyId = uferId });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var updated = await put.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Ufer", updated.GetProperty("gloss").GetString());
        Assert.Equal(uferId, updated.GetProperty("vocabularyId").GetInt32());

        // Persistiert: erneutes Lesen der Übung zeigt die Korrektur.
        var config = (await (await father.GetAsync($"{route}/{exerciseId}")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("config");
        var stored = config.GetProperty("sentences").EnumerateArray().Single()
            .GetProperty("decoding").EnumerateArray().Single(w => w.GetProperty("wordId").GetInt32() == wordId);
        Assert.Equal("Ufer", stored.GetProperty("gloss").GetString());
        Assert.Equal(uferId, stored.GetProperty("vocabularyId").GetInt32());
    }

    [Fact]
    public async Task GleichesWort_InZweiSaetzen_HatEigeneWordId_UndWirdUnabhaengigGeaendert()
    {
        var father = await FatherClientAsync();
        var (route, exerciseId) = await CreateExerciseAsync(father);
        await CreateVocabAsync(father, "red", "rot", partOfSpeech: "Adjective");

        var s1 = await (await father.PostAsJsonAsync($"{route}/{exerciseId}/sentences",
            new { learningSentence = "red apple", naturalTranslation = "roter Apfel" })).Content.ReadFromJsonAsync<JsonElement>();
        var s2 = await (await father.PostAsJsonAsync($"{route}/{exerciseId}/sentences",
            new { learningSentence = "red car", naturalTranslation = "rotes Auto" })).Content.ReadFromJsonAsync<JsonElement>();

        int RedId(JsonElement s) => s.GetProperty("result").EnumerateArray()
            .Single(w => w.GetProperty("learningWord").GetString() == "red").GetProperty("wordId").GetInt32();
        var red1 = RedId(s1);
        var red2 = RedId(s2);
        Assert.NotEqual(red1, red2);

        // Nur das "red" im zweiten Satz auf eine freie Glosse (ohne Karte) setzen.
        await father.PutAsJsonAsync($"{route}/{exerciseId}/words/{red2}", new { gloss = "ROT-FREI" });

        var config = (await (await father.GetAsync($"{route}/{exerciseId}")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("config");
        var allWords = config.GetProperty("sentences").EnumerateArray()
            .SelectMany(s => s.GetProperty("decoding").EnumerateArray()).ToArray();

        var w1 = allWords.Single(w => w.GetProperty("wordId").GetInt32() == red1);
        var w2 = allWords.Single(w => w.GetProperty("wordId").GetInt32() == red2);
        Assert.Equal("rot", w1.GetProperty("gloss").GetString());
        Assert.False(IsNull(w1, "vocabularyId"));
        Assert.Equal("ROT-FREI", w2.GetProperty("gloss").GetString());
        Assert.True(IsNull(w2, "vocabularyId"));
    }

    [Fact]
    public async Task Decode_Zustandslos_LiefertTupelOhnePersistenz()
    {
        var father = await FatherClientAsync();
        var dogId = await CreateVocabAsync(father, "dog", "Hund", partOfSpeech: "Noun");

        var res = await father.PostAsJsonAsync("/api/v1/learn/birkenbihl/decode", new
        {
            learningLang = "en",
            nativeLang = "de",
            learningSentence = "dog cat",
            naturalTranslation = "Hund Katze",
        });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();

        // Ephemer: keine gespeicherten IDs.
        Assert.Equal(0, body.GetProperty("sentenceId").GetInt32());
        var result = body.GetProperty("result").EnumerateArray().ToArray();
        Assert.All(result, w => Assert.Equal(0, w.GetProperty("wordId").GetInt32()));

        Assert.Equal("Hund", result[0].GetProperty("gloss").GetString());
        Assert.Equal(dogId, result[0].GetProperty("vocabularyId").GetInt32());
        Assert.True(IsNull(result[1], "gloss"));
    }

    [Fact]
    public async Task Kind_KannKeinenSatzHinzufuegen_Liefert403()
    {
        var father = await FatherClientAsync();
        var (route, exerciseId) = await CreateExerciseAsync(father);

        var child = await ChildClientAsync();
        var res = await child.PostAsJsonAsync($"{route}/{exerciseId}/sentences",
            new { learningSentence = "How are you?", naturalTranslation = "Wie geht es dir?" });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
