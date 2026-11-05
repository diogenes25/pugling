using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// The read and delete path of the creator catalog: single views, lists, and removing series-unit/category/
/// cloze/unit/vocabulary resources along with their links.
/// <para>
/// Added while closing the coverage gap (docs/codequalitaet-gates-plan.md, C3). The pattern there was
/// clear: what a walkthrough touches (create → play → evaluate) was tested; almost everywhere <c>Get</c>
/// and <c>Delete</c> remained untested. The delete path is the most expensive blind spot, because it
/// only becomes noticeable once data is gone – or, precisely, fails to go away.
/// </para>
/// </summary>
public class CatalogReadDeleteTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static string Eindeutig(string präfix) => $"{präfix}-{Guid.NewGuid():N}"[..20];

    private static async Task<JsonElement> Json(HttpResponseMessage res)
    {
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Unit_Einzelansicht_Zeigt_Die_Unit_Einer_Fremden_Reihe_Nicht()
    {
        var creator = await TestApi.AdultAsync(factory);
        var seriesId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/textbook-series", new { name = Eindeutig("Reihe") }));
        var unitId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Unit 3", orderIndex = 3 }));

        var unit = await Json(await creator.GetAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}"));
        Assert.Equal("Unit 3", unit.GetProperty("label").GetString());
        Assert.Equal(seriesId, unit.GetProperty("seriesId").GetInt32());

        // Under a different series this unit does not exist - otherwise the series in the path would be
        // pure decoration and every unit id globally guessable.
        var andereReihe = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/textbook-series", new { name = Eindeutig("Reihe") }));
        Assert.Equal(HttpStatusCode.NotFound,
            (await creator.GetAsync($"/api/v1/creator/textbook-series/{andereReihe}/units/{unitId}")).StatusCode);
    }

    [Fact]
    public async Task Uebungs_Kategorien_Lesen_Und_Loeschen()
    {
        var creator = await TestApi.AdultAsync(factory);
        var subjectId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects", new { name = Eindeutig("Fach") }));
        var url = $"/api/v1/creator/subjects/{subjectId}/categories";
        var categoryId = await TestApi.IdAsync(await creator.PostAsJsonAsync(url, new { name = "Grammatik" }));

        Assert.Equal("Grammatik", (await Json(await creator.GetAsync($"{url}/{categoryId}"))).GetProperty("name").GetString());
        Assert.Equal(1, (await Json(await creator.GetAsync(url))).GetArrayLength());

        Assert.Equal(HttpStatusCode.NoContent, (await creator.DeleteAsync($"{url}/{categoryId}")).StatusCode);
        Assert.Empty((await Json(await creator.GetAsync(url))).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await creator.DeleteAsync($"{url}/{categoryId}")).StatusCode);
    }

    [Fact]
    public async Task Lueckentexte_Lesen_Per_Id_Und_Key_Und_Loeschen()
    {
        var creator = await TestApi.AdultAsync(factory);
        var key = Eindeutig("cz");
        var id = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/cloze-texts", new
        {
            key,
            title = "Wetter",
            sourceLanguage = "en",
            targetLanguage = "de",
            text = "It is {{1}} today.",
            gaps = new[] { new { index = 1, answer = "sunny" } },
        }));

        Assert.Equal(key, (await Json(await creator.GetAsync($"/api/v1/creator/cloze-texts/{id}"))).GetProperty("key").GetString());
        // The key is the stable domain identifier - authors reference it, not the id.
        Assert.Equal(id, (await Json(await creator.GetAsync($"/api/v1/creator/cloze-texts/by-key/{key}"))).GetProperty("id").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await creator.GetAsync("/api/v1/creator/cloze-texts/by-key/gibt-es-nicht")).StatusCode);

        var liste = await Json(await creator.GetAsync("/api/v1/creator/cloze-texts"));
        Assert.Contains(id, liste.EnumerateArray().Select(c => c.GetProperty("id").GetInt32()));

        Assert.Equal(HttpStatusCode.NoContent, (await creator.DeleteAsync($"/api/v1/creator/cloze-texts/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await creator.GetAsync($"/api/v1/creator/cloze-texts/{id}")).StatusCode);
    }

    [Fact]
    public async Task Buchreihen_Liste_Und_Unit_Loeschen()
    {
        var creator = await TestApi.AdultAsync(factory);
        var seriesId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = Eindeutig("Access"), sourceLanguage = "en", targetLanguage = "de" }));
        var unitId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Unit 1", grade = 5 }));

        // Series are a *shared* catalog: every creator may read them, which is why the list is not empty.
        var liste = await Json(await creator.GetAsync("/api/v1/creator/textbook-series"));
        Assert.Contains(seriesId, liste.EnumerateArray().Select(s => s.GetProperty("id").GetInt32()));

        Assert.Equal(HttpStatusCode.NoContent,
            (await creator.DeleteAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}")).StatusCode);
        Assert.Empty((await Json(await creator.GetAsync($"/api/v1/creator/textbook-series/{seriesId}/units"))).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound,
            (await creator.DeleteAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}")).StatusCode);
    }

    [Fact]
    public async Task Store_Vokabel_Loeschen_Nur_Wenn_Keine_Uebung_Sie_Nutzt()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (unbenutztId, _) = await TestApi.CreateStoreVocabAsync(creator, Eindeutig("lonely"), "einsam");
        var (_, benutztKey) = await TestApi.CreateStoreVocabAsync(creator, Eindeutig("used"), "benutzt");
        await TestApi.CreateVocabRefExerciseAsync(creator, benutztKey);
        var benutztId = await TestApi.ResolveVocabIdAsync(creator, benutztKey);

        // A referenced vocabulary entry must not disappear - otherwise a silent "(vocabulary missing)"
        // placeholder would sit in the exercise.
        var verweigert = await creator.DeleteAsync($"/api/v1/creator/vocabulary/{benutztId}");
        Assert.Equal(HttpStatusCode.Conflict, verweigert.StatusCode);
        Assert.Equal("vocabulary_in_use",
            (await verweigert.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // The unused one goes.
        Assert.Equal(HttpStatusCode.NoContent, (await creator.DeleteAsync($"/api/v1/creator/vocabulary/{unbenutztId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await creator.DeleteAsync($"/api/v1/creator/vocabulary/{unbenutztId}")).StatusCode);
    }

    /// <summary>
    /// B-386: a translation links its pairs by id in the config, not in the item table. Red if the usage lookup ignores
    /// translations - then the entry could be deleted, and the editor would send the dead id back on the next save.
    /// Counter-check: the id of an unrelated entry stands in the sentence text, so the SQL pre-filter matches it - only the
    /// exact id comparison keeps it deletable. Red if the pre-filter alone decides.
    /// </summary>
    [Fact]
    public async Task Store_Vokabel_An_Einer_Uebersetzung_Ist_Benutzt()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (verlinktId, _) = await TestApi.CreateStoreVocabAsync(creator, Eindeutig("Brücke"), "bridge", "de", "en");
        var (fremdId, _) = await TestApi.CreateStoreVocabAsync(creator, Eindeutig("Fluss"), "river", "de", "en");
        var subjectId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Übersetzungs-Fach") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(creator, subjectId);
        var exerciseId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/translation", new
            {
                title = TestApi.UniqueName("Brücken-Übersetzung"),
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    sourceLang = "de",
                    targetLang = "en",
                    items = new[] { new { source = $"Die Brücke Nr. {fremdId} ist alt.", target = "The bridge is old.", vocabularyId = (int?)verlinktId } },
                },
            }));

        var usage = await creator.GetFromJsonAsync<List<JsonElement>>($"/api/v1/creator/vocabulary/{verlinktId}/usage");
        Assert.Contains(usage!, u => u.GetProperty("exerciseId").GetInt32() == exerciseId);

        var verweigert = await creator.DeleteAsync($"/api/v1/creator/vocabulary/{verlinktId}");
        Assert.Equal(HttpStatusCode.Conflict, verweigert.StatusCode);
        Assert.Equal("vocabulary_in_use",
            (await verweigert.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await creator.DeleteAsync($"/api/v1/creator/vocabulary/{fremdId}")).StatusCode);
    }

    /// <summary>
    /// B-392: the editor drops a pair's id as soon as its text was edited and expects the server to link it again. Replace
    /// used to save such a pair unlinked. Red if replace skips the store normalization: the new pair would then have no id,
    /// no <c>_self</c> and no usage. The old entry is the counter-check that the stale link is really gone.
    /// </summary>
    [Fact]
    public async Task Geaenderte_Uebersetzung_Legt_Ihr_Neues_Paar_Im_Speicher_An()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (altId, _) = await TestApi.CreateStoreVocabAsync(creator, Eindeutig("Brücke alt"), "The bridge is old.", "de", "en");
        var subjectId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Übersetzungs-Fach") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(creator, subjectId);
        var route = $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/translation";
        object Payload(string source, string target, int? vocabularyId) => new
        {
            title = "Brücken-Übersetzung",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { sourceLang = "de", targetLang = "en", items = new[] { new { source, target, vocabularyId } } },
        };
        var exerciseId = await TestApi.IdAsync(await creator.PostAsJsonAsync(route, Payload("Die Brücke ist alt.", "The bridge is old.", altId)));

        var neuerSatz = Eindeutig("Fluss breit");
        var res = await creator.PutAsJsonAsync($"{route}/{exerciseId}", Payload(neuerSatz, "The river is wide.", null));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var paar = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("config").GetProperty("items")[0];
        var neuId = paar.GetProperty("vocabularyId").GetInt32();
        Assert.NotEqual(altId, neuId);
        Assert.Equal($"/api/v1/creator/vocabulary/{neuId}", paar.GetProperty("_self").GetString());

        var usage = await creator.GetFromJsonAsync<List<JsonElement>>($"/api/v1/creator/vocabulary/{neuId}/usage");
        Assert.Contains(usage!, u => u.GetProperty("exerciseId").GetInt32() == exerciseId);
        Assert.Equal(HttpStatusCode.NoContent, (await creator.DeleteAsync($"/api/v1/creator/vocabulary/{altId}")).StatusCode);
    }

    /// <summary>
    /// B-395: the editor's click path sends the WHOLE list on replace - unchanged pairs with their id, the edited one with
    /// <c>null</c>. Three cases, each with its own red mutant:
    /// the unchanged pair keeps its id (red if normalization re-resolves every pair - its text differs from its entry's word,
    /// so it would get a new id - or if the result lands on the wrong index, visible because the edited pair comes last); the
    /// edited pair gets the entry that already carries its new text (red if the store lookup is skipped and a duplicate is
    /// created); the edited pair's former entry is locked before and free after, while the unchanged pair's entry stays locked
    /// (counter-check, also green without B-392).
    /// </summary>
    [Fact]
    public async Task Uebersetzung_Ersetzen_Mit_Gemischter_Liste_Behaelt_Und_Findet()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (bleibtId, _) = await TestApi.CreateStoreVocabAsync(creator, Eindeutig("Brücke"), "bridge", "de", "en");
        var (altId, _) = await TestApi.CreateStoreVocabAsync(creator, Eindeutig("Turm"), "tower", "de", "en");
        var vorhandenerSatz = Eindeutig("Fluss breit");
        var (vorhandenId, _) = await TestApi.CreateStoreVocabAsync(creator, vorhandenerSatz, "The river is wide.", "de", "en");
        var subjectId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Übersetzungs-Fach") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(creator, subjectId);
        var route = $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/translation";
        object Payload(params (string Source, string Target, int? VocabularyId)[] pairs) => new
        {
            title = "Gemischte Übersetzung",
            orderIndex = 1,
            rewardPoints = 10,
            config = new
            {
                sourceLang = "de",
                targetLang = "en",
                items = pairs.Select(p => new { source = p.Source, target = p.Target, vocabularyId = p.VocabularyId }).ToArray(),
            },
        };
        var exerciseId = await TestApi.IdAsync(await creator.PostAsJsonAsync(route, Payload(
            ("Die Brücke ist alt.", "The bridge is old.", bleibtId), ("Der Turm ist hoch.", "The tower is high.", altId))));
        // Before: the edited pair's entry is in use - so "free" after the PUT is a change, not the starting state.
        Assert.Equal(HttpStatusCode.Conflict, (await creator.DeleteAsync($"/api/v1/creator/vocabulary/{altId}")).StatusCode);

        var res = await creator.PutAsJsonAsync($"{route}/{exerciseId}", Payload(
            ("Die Brücke ist alt.", "The bridge is old.", bleibtId), (vorhandenerSatz, "The river is wide.", null)));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var items = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("config").GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal(bleibtId, items[0].GetProperty("vocabularyId").GetInt32());
        Assert.Equal(vorhandenId, items[1].GetProperty("vocabularyId").GetInt32());

        Assert.Equal(HttpStatusCode.Conflict, (await creator.DeleteAsync($"/api/v1/creator/vocabulary/{bleibtId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await creator.DeleteAsync($"/api/v1/creator/vocabulary/{altId}")).StatusCode);
    }

    /// <summary>
    /// B-387: a Birkenbihl decoding links its words by id in the config, like a translation. Red if the usage lookup ignores
    /// Birkenbihl. Counter-check as in B-386: an unrelated entry's id stands in the sentence text and must not lock it.
    /// </summary>
    [Fact]
    public async Task Store_Vokabel_An_Einem_Birkenbihl_Wort_Ist_Benutzt()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (verlinktId, _) = await TestApi.CreateStoreVocabAsync(creator, Eindeutig("bridge"), "Brücke", "en", "de");
        var (fremdId, _) = await TestApi.CreateStoreVocabAsync(creator, Eindeutig("river"), "Fluss", "en", "de");
        var subjectId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Birkenbihl-Fach") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(creator, subjectId);
        var exerciseId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/birkenbihl", new
            {
                title = TestApi.UniqueName("Brücken-Birkenbihl"),
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
                            learningSentence = $"Bridge {fremdId}.",
                            naturalTranslation = $"Brücke {fremdId}.",
                            decoding = new[] { new { learningWord = "Bridge", gloss = "Brücke", vocabularyId = (int?)verlinktId } },
                        },
                    },
                },
            }));

        var usage = await creator.GetFromJsonAsync<List<JsonElement>>($"/api/v1/creator/vocabulary/{verlinktId}/usage");
        Assert.Contains(usage!, u => u.GetProperty("exerciseId").GetInt32() == exerciseId);

        var verweigert = await creator.DeleteAsync($"/api/v1/creator/vocabulary/{verlinktId}");
        Assert.Equal(HttpStatusCode.Conflict, verweigert.StatusCode);
        Assert.Equal("vocabulary_in_use",
            (await verweigert.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await creator.DeleteAsync($"/api/v1/creator/vocabulary/{fremdId}")).StatusCode);
    }

    [Fact]
    public async Task Vokabel_Item_Einzeln_Lesen()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(creator, Eindeutig("hedgehog"), "Igel");
        var vocabId = await TestApi.ResolveVocabIdAsync(creator, key);

        // The exercise route needs the series and the unit; the exercise detail no longer carries a series
        // id (only seriesUnitId/subjectId), so the fixture is built directly instead of read back.
        var subjectId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects", new { name = Eindeutig("Fach") }));
        var seriesId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            "/api/v1/creator/textbook-series", new { name = Eindeutig("Reihe"), subjectId }));
        var seriesUnitId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Unit 1", orderIndex = 1 }));
        var basis = $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary";
        var exerciseId = await TestApi.IdAsync(await creator.PostAsJsonAsync(basis, new
        {
            title = "Vokabeln (Store)",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { direction = "front-to-back", refs = new[] { new { vocabularyId = vocabId } } },
        }));
        basis = $"{basis}/{exerciseId}/items";

        var items = await Json(await creator.GetAsync(basis));
        var itemId = items[0].GetProperty("id").GetInt32();

        var item = await Json(await creator.GetAsync($"{basis}/{itemId}"));
        Assert.Equal(itemId, item.GetProperty("id").GetInt32());
        // Front/back come live from the store - the item is only a positioned reference.
        Assert.False(string.IsNullOrEmpty(item.GetProperty("front").GetString()));
        Assert.Equal(HttpStatusCode.NotFound, (await creator.GetAsync($"{basis}/999999")).StatusCode);
    }

    [Fact]
    public async Task Vokabel_Tags_Anhaengen_Und_Wieder_Loesen()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (vocabId, _) = await TestApi.CreateStoreVocabAsync(creator, Eindeutig("otter"), "Otter");
        var url = $"/api/v1/creator/vocabulary/{vocabId}/tags";

        // Create-if-missing by name: the author types topics, not ids.
        var angehaengt = await Json(await creator.PostAsJsonAsync(url, new { tags = new[] { "Tiere", "Wasser" } }));
        Assert.Equal(2, angehaengt.GetArrayLength());
        var tagId = angehaengt.EnumerateArray().First(t => t.GetProperty("name").GetString() == "Tiere")
            .GetProperty("id").GetInt32();

        Assert.Equal(HttpStatusCode.NoContent, (await creator.DeleteAsync($"{url}/{tagId}")).StatusCode);
        // Attaching again is idempotent and returns the *full* state - "Tiere" is gone now.
        var danach = await Json(await creator.PostAsJsonAsync(url, new { tags = new[] { "Wasser" } }));
        Assert.Equal("Wasser", Assert.Single(danach.EnumerateArray()).GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await creator.DeleteAsync($"{url}/{tagId}")).StatusCode);
    }

    [Fact]
    public async Task Uebungs_Recht_Laesst_Sich_Zurueckziehen()
    {
        var owner = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(owner);
        var fremderId = await TestApi.IdAsync(await factory.CreateClient().PostAsJsonAsync(
            "/api/v1/supervisor/adults", new { name = "Kollege", pin = "6501" }));
        var url = $"/api/v1/creator/exercises/{exerciseId}/grants";
        (await owner.PostAsJsonAsync(url, new { creatorId = fremderId, permission = "Write" })).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"{url}/{fremderId}/Write")).StatusCode);
        Assert.DoesNotContain(fremderId, (await Json(await owner.GetAsync(url)))
            .EnumerateArray().Select(g => g.GetProperty("creatorId").GetInt32()));
        Assert.Equal(HttpStatusCode.NotFound, (await owner.DeleteAsync($"{url}/{fremderId}/Write")).StatusCode);
    }
}
