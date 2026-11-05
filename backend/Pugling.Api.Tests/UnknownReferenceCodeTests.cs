using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-329: a payload that names an id or key which does not exist answers <c>invalid_reference</c> on the exercise path
/// and in the vocabulary store - the same code B-258 gave the six other places, so a client can branch on it. A rule
/// break next to it stays <c>validation_error</c>. Status (400) and detail are unchanged; only the code moved.
/// Each case is red if its validator goes back to the plain <c>validation_error</c>.
/// </summary>
public class UnknownReferenceCodeTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const int Unknown = 987_654;

    private static async Task<string> UnitPathAsync(HttpClient creator)
    {
        var subjectId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Referenz-Fach") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(creator, subjectId);
        return $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}";
    }

    private static async Task AssertProblemAsync(HttpResponseMessage res, string code)
    {
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal(code, (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    private static object Vocabulary(object config, int[]? grammarTopicIds = null) => new
    {
        title = TestApi.UniqueName("Referenz-Übung"),
        orderIndex = 1,
        rewardPoints = 10,
        config,
        grammarTopicIds = grammarTopicIds ?? [],
    };

    [Fact]
    public async Task Unbekannte_Vokabel_in_refs_ist_eine_unbekannte_Referenz_ein_Id_0_bleibt_Regelbruch()
    {
        var creator = await TestApi.AdultAsync(factory);
        var unit = await UnitPathAsync(creator);

        await AssertProblemAsync(await creator.PostAsJsonAsync($"{unit}/vocabulary",
            Vocabulary(new { direction = "front-to-back", refs = new[] { new { vocabularyId = Unknown } } })), "invalid_reference");
        // The rule "an id must be > 0" is malformed input, not a missing row.
        await AssertProblemAsync(await creator.PostAsJsonAsync($"{unit}/vocabulary",
            Vocabulary(new { direction = "front-to-back", refs = new[] { new { vocabularyId = 0 } } })), "validation_error");
    }

    [Fact]
    public async Task Unbekannte_Vokabel_an_einem_Inline_Eintrag_ist_eine_unbekannte_Referenz()
    {
        var creator = await TestApi.AdultAsync(factory);
        var unit = await UnitPathAsync(creator);

        await AssertProblemAsync(await creator.PostAsJsonAsync($"{unit}/vocabulary", Vocabulary(new
        {
            direction = "front-to-back",
            sourceLang = "en",
            targetLang = "de",
            items = new[] { new { vocabularyId = (int?)Unknown } },
        })), "invalid_reference");
        // B-388: an id of 0 stays malformed input here too. Red if the inline validator reports it as an unknown reference.
        await AssertProblemAsync(await creator.PostAsJsonAsync($"{unit}/vocabulary", Vocabulary(new
        {
            direction = "front-to-back",
            sourceLang = "en",
            targetLang = "de",
            items = new[] { new { vocabularyId = (int?)0 } },
        })), "validation_error");
    }

    /// <summary>
    /// The item routes (review finding, Sprint 4): <c>POST/PATCH …/items</c> with an id that does not exist is the same
    /// missing row as in inline <c>items</c>. An id of 0 stays malformed input. Red if either route reports the unknown
    /// id as <c>validation_error</c> again.
    /// </summary>
    [Fact]
    public async Task Unbekannte_Vokabel_an_den_Item_Routen_ist_eine_unbekannte_Referenz()
    {
        var creator = await TestApi.AdultAsync(factory);
        var unit = await UnitPathAsync(creator);
        var exerciseId = await TestApi.IdAsync(await creator.PostAsJsonAsync($"{unit}/vocabulary", Vocabulary(new
        {
            direction = "front-to-back",
            sourceLang = "en",
            targetLang = "de",
            items = new[] { new { front = TestApi.UniqueName("tree"), back = "Baum" } },
        })));
        var items = $"{unit}/vocabulary/{exerciseId}/items";

        await AssertProblemAsync(await creator.PostAsJsonAsync(items, new { vocabularyId = Unknown }), "invalid_reference");
        await AssertProblemAsync(await creator.PostAsJsonAsync(items, new { vocabularyId = 0 }), "validation_error");

        var itemId = (await creator.GetFromJsonAsync<JsonElement>(items)).EnumerateArray().First().GetProperty("id").GetInt32();
        await AssertProblemAsync(await creator.PatchAsJsonAsync($"{items}/{itemId}", new { vocabularyId = Unknown }), "invalid_reference");
        // B-388: red if the item PATCH reports an id of 0 as an unknown reference.
        await AssertProblemAsync(await creator.PatchAsJsonAsync($"{items}/{itemId}", new { vocabularyId = 0 }), "validation_error");
    }

    private static object Translation(int? vocabularyId) => new
    {
        title = TestApi.UniqueName("Referenz-Übersetzung"),
        orderIndex = 1,
        rewardPoints = 10,
        config = new
        {
            sourceLang = "de",
            targetLang = "en",
            items = new[] { new { source = "Der Baum ist grün.", target = "The tree is green.", vocabularyId } },
        },
    };

    /// <summary>
    /// B-386: a translation pair with a set <c>vocabularyId</c> skips the store, so an unknown id used to be saved silently
    /// and left the pair unlinked. Red if the translation validator stops checking the id on create or on replace; the
    /// existing id is the counter-check that a real link still saves.
    /// </summary>
    [Fact]
    public async Task Unbekannte_Vokabel_an_einem_Uebersetzungspaar_ist_eine_unbekannte_Referenz()
    {
        var creator = await TestApi.AdultAsync(factory);
        var unit = await UnitPathAsync(creator);
        var (vocabId, _) = await TestApi.CreateStoreVocabAsync(creator, TestApi.UniqueName("Baum"), "tree", "de", "en");

        await AssertProblemAsync(await creator.PostAsJsonAsync($"{unit}/translation", Translation(Unknown)), "invalid_reference");
        await AssertProblemAsync(await creator.PostAsJsonAsync($"{unit}/translation", Translation(0)), "validation_error");

        var created = await creator.PostAsJsonAsync($"{unit}/translation", Translation(vocabId));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var exerciseId = await TestApi.IdAsync(created);
        await AssertProblemAsync(await creator.PutAsJsonAsync($"{unit}/translation/{exerciseId}", Translation(Unknown)), "invalid_reference");
        // B-393: statement of intent, not a separate guard - create and replace share one validator, so the POST cases above
        // already catch a loosened check. It pins AK 1 of B-386 ("create AND change") should replace ever get its own path.
        await AssertProblemAsync(await creator.PutAsJsonAsync($"{unit}/translation/{exerciseId}", Translation(0)), "validation_error");
    }

    private static object Birkenbihl(int? vocabularyId) => new
    {
        title = TestApi.UniqueName("Referenz-Birkenbihl"),
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
                    learningSentence = "The tree.",
                    naturalTranslation = "Der Baum.",
                    decoding = new[] { new { learningWord = "tree", gloss = "Baum", vocabularyId } },
                },
            },
        },
    };

    /// <summary>
    /// B-387: a decoded word with a set <c>vocabularyId</c> was saved without a check on create and replace, while the word endpoint
    /// <c>PUT …/words/{wordId}</c> answered <c>invalid_reference</c> for the same field. Red if the Birkenbihl validator stops checking the id
    /// on create or replace, or reports 0 as an unknown reference; the existing id is the counter-check.
    /// </summary>
    [Fact]
    public async Task Unbekannte_Vokabel_an_einem_Birkenbihl_Wort_ist_eine_unbekannte_Referenz()
    {
        var creator = await TestApi.AdultAsync(factory);
        var unit = await UnitPathAsync(creator);
        var (vocabId, _) = await TestApi.CreateStoreVocabAsync(creator, TestApi.UniqueName("tree"), "Baum", "en", "de");

        await AssertProblemAsync(await creator.PostAsJsonAsync($"{unit}/birkenbihl", Birkenbihl(Unknown)), "invalid_reference");
        await AssertProblemAsync(await creator.PostAsJsonAsync($"{unit}/birkenbihl", Birkenbihl(0)), "validation_error");

        var created = await creator.PostAsJsonAsync($"{unit}/birkenbihl", Birkenbihl(vocabId));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var exerciseId = await TestApi.IdAsync(created);
        await AssertProblemAsync(await creator.PutAsJsonAsync($"{unit}/birkenbihl/{exerciseId}", Birkenbihl(Unknown)), "invalid_reference");
    }

    /// <summary>
    /// B-394: the word endpoint answers like create and replace of the exercise - an id ≤ 0 is malformed input, an unknown id a
    /// missing row. Red if the word endpoint looks an id of 0 up again (then it reports <c>invalid_reference</c>); the unknown id
    /// is the counter-check that the lookup still runs.
    /// </summary>
    [Fact]
    public async Task Birkenbihl_Wort_mit_Id_0_ist_ein_Formfehler()
    {
        var creator = await TestApi.AdultAsync(factory);
        var unit = await UnitPathAsync(creator);
        var created = await creator.PostAsJsonAsync($"{unit}/birkenbihl", Birkenbihl(null));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        var exerciseId = body.GetProperty("id").GetInt32();
        var wordId = body.GetProperty("config").GetProperty("sentences")[0].GetProperty("decoding")[0].GetProperty("wordId").GetInt32();
        var word = $"{unit}/birkenbihl/{exerciseId}/words/{wordId}";

        await AssertProblemAsync(await creator.PutAsJsonAsync(word, new { vocabularyId = 0 }), "validation_error");
        await AssertProblemAsync(await creator.PutAsJsonAsync(word, new { vocabularyId = -3 }), "validation_error");
        await AssertProblemAsync(await creator.PutAsJsonAsync(word, new { vocabularyId = Unknown }), "invalid_reference");
    }

    [Fact]
    public async Task Unbekannter_Schluessel_in_einer_Luecke_ist_eine_unbekannte_Referenz()
    {
        var creator = await TestApi.AdultAsync(factory);
        var unit = await UnitPathAsync(creator);

        await AssertProblemAsync(await creator.PostAsJsonAsync($"{unit}/cloze", new
        {
            title = TestApi.UniqueName("Referenz-Lücke"),
            orderIndex = 1,
            rewardPoints = 10,
            config = new { text = "A {{1}}.", gaps = new[] { new { index = 1, answer = "", vocabKey = $"gibt-es-nicht-{Guid.NewGuid():N}" } } },
        }), "invalid_reference");
    }

    [Fact]
    public async Task Unbekanntes_Grammatikthema_ist_eine_unbekannte_Referenz()
    {
        var creator = await TestApi.AdultAsync(factory);
        var unit = await UnitPathAsync(creator);

        await AssertProblemAsync(await creator.PostAsJsonAsync($"{unit}/vocabulary", Vocabulary(
            new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = new[] { new { front = "hello", back = "hallo" } } },
            [Unknown])), "invalid_reference");
    }

    [Fact]
    public async Task Unbekannte_Grundform_im_Vokabelspeicher_ist_eine_unbekannte_Referenz()
    {
        var creator = await TestApi.AdultAsync(factory);
        var missing = $"gibt-es-nicht-{Guid.NewGuid():N}";

        await AssertProblemAsync(await creator.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            sourceLanguage = "en",
            targetLanguage = "de",
            word = TestApi.UniqueName("went"),
            translation = "ging",
            baseFormKey = missing,
        }), "invalid_reference");

        var (id, _) = await TestApi.CreateStoreVocabAsync(creator, TestApi.UniqueName("gone"), "gegangen");
        await AssertProblemAsync(await creator.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{id}", new { baseFormKey = missing }),
            "invalid_reference");
    }

    /// <summary>
    /// B-389: a base-form key of only blanks is no key. Create already treats it as "not given"; a PATCH treats it like the empty
    /// string, which removes the link. Red if the PATCH looks the blanks up as a key (then <c>invalid_reference</c>). The link is
    /// set before the PATCH, so "removed" differs from the starting state.
    /// </summary>
    [Fact]
    public async Task Leere_Grundform_im_Patch_entfernt_die_Verknuepfung()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (grundId, grundKey) = await TestApi.CreateStoreVocabAsync(creator, TestApi.UniqueName("go"), "gehen");
        var formId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            sourceLanguage = "en",
            targetLanguage = "de",
            word = TestApi.UniqueName("went"),
            translation = "ging",
            baseFormKey = grundKey,
            baseFormRelation = "past",
        }));
        var vorher = await creator.GetFromJsonAsync<JsonElement>($"/api/v1/creator/vocabulary/{formId}");
        Assert.Equal(grundId, vorher.GetProperty("baseFormId").GetInt32());
        Assert.Equal("past", vorher.GetProperty("baseFormRelation").GetString());

        var res = await creator.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{formId}", new { baseFormKey = "   " });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var nachher = await creator.GetFromJsonAsync<JsonElement>($"/api/v1/creator/vocabulary/{formId}");
        Assert.Equal(JsonValueKind.Null, nachher.GetProperty("baseFormId").ValueKind);
        // The decision removes the label with the link; red if the blank branch clears only the id.
        Assert.Equal(JsonValueKind.Null, nachher.GetProperty("baseFormRelation").ValueKind);
    }

    /// <summary>
    /// B-391: an explicit <c>null</c> where a config's contract says "never null" (a list, a required string) is malformed input.
    /// Model validation already answers it (the implicit <c>[Required]</c> of non-nullable members), with the path as the key of
    /// <c>errors</c> - B-391 assumed a 500 and was refuted here, but only for null MEMBERS. A null ELEMENT of a list
    /// (<c>[null]</c>) passes model validation and was a 500 (B-397); <c>NullElementGuardFilter</c> answers it with the same shape.
    /// Red if the implicit check or the filter is switched off, or a member loses its annotation.
    /// </summary>
    [Theory]
    [InlineData("birkenbihl", "{\"learningLang\":\"en\",\"nativeLang\":\"de\",\"sentences\":null}", "config.sentences")]
    [InlineData("birkenbihl", "{\"learningLang\":\"en\",\"nativeLang\":\"de\",\"sentences\":[{\"learningSentence\":\"Hi.\",\"naturalTranslation\":\"Hallo.\",\"decoding\":null}]}", "config.sentences[0].decoding")]
    [InlineData("translation", "{\"sourceLang\":\"de\",\"targetLang\":\"en\",\"items\":null}", "config.items")]
    [InlineData("translation", "{\"sourceLang\":\"de\",\"targetLang\":\"en\",\"items\":[{\"source\":null,\"target\":\"x\"}]}", "config.items[0].source")]
    [InlineData("vocabulary", "{\"direction\":\"front-to-back\",\"items\":null}", "config.items")]
    // B-397: a null ELEMENT of a list is not covered by the implicit [Required] above - model validation never sees it.
    [InlineData("translation", "{\"sourceLang\":\"de\",\"targetLang\":\"en\",\"items\":[null]}", "config.items[0]")]
    [InlineData("birkenbihl", "{\"learningLang\":\"en\",\"nativeLang\":\"de\",\"sentences\":[null]}", "config.sentences[0]")]
    [InlineData("cloze", "{\"text\":\"Ich {{1}}.\",\"gaps\":[null]}", "config.gaps[0]")]
    [InlineData("cloze", "{\"text\":\"Ich {{1}}.\",\"gaps\":[{\"index\":1,\"answer\":\"bin\"}],\"wordBank\":[\"bin\",null]}", "config.wordBank[1]")]
    public async Task Null_in_einer_Config_ist_ein_Formfehler(string type, string configJson, string path)
    {
        var creator = await TestApi.AdultAsync(factory);
        var unit = await UnitPathAsync(creator);
        var payload = $"{{\"title\":\"{TestApi.UniqueName("Null")}\",\"orderIndex\":1,\"rewardPoints\":10,\"config\":{configJson}}}";

        await AssertNullProblemAsync(await creator.PostAsync($"{unit}/{type}", Json(payload)), path);
    }

    /// <summary>B-391 on the replace path: red if replace binds its config without model validation.</summary>
    [Fact]
    public async Task Null_in_einer_Config_ist_auch_beim_Ersetzen_ein_Formfehler()
    {
        var creator = await TestApi.AdultAsync(factory);
        var unit = await UnitPathAsync(creator);
        var exerciseId = await TestApi.IdAsync(await creator.PostAsJsonAsync($"{unit}/translation", Translation(null)));
        var payload = $"{{\"title\":\"Null\",\"orderIndex\":1,\"rewardPoints\":10,\"config\":{{\"sourceLang\":\"de\",\"targetLang\":\"en\",\"items\":null}}}}";

        await AssertNullProblemAsync(await creator.PutAsync($"{unit}/translation/{exerciseId}", Json(payload)), "config.items");
    }

    private static StringContent Json(string payload) => new(payload, System.Text.Encoding.UTF8, "application/json");

    private static async Task AssertNullProblemAsync(HttpResponseMessage res, string path)
    {
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_error", problem.GetProperty("code").GetString());
        Assert.Contains(problem.GetProperty("errors").EnumerateObject(), e => string.Equals(e.Name, path, StringComparison.OrdinalIgnoreCase));
    }
}
