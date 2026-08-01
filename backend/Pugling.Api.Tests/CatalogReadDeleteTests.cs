using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// The read and delete path of the creator catalog: single views, lists, and removing chapter/category/
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
    public async Task Kapitel_Einzelansicht_Zeigt_Das_Kapitel_Eines_Fremden_Fachs_Nicht()
    {
        var creator = await TestApi.FatherAsync(factory);
        var subjectId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects", new { name = Eindeutig("Fach") }));
        var chapterId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit 3", orderIndex = 3 }));

        var kapitel = await Json(await creator.GetAsync($"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}"));
        Assert.Equal("Unit 3", kapitel.GetProperty("name").GetString());
        Assert.Equal(subjectId, kapitel.GetProperty("subjectId").GetInt32());

        // Under a different subject this chapter does not exist - otherwise the subject in the path would be
        // pure decoration and every chapter id globally guessable.
        var anderesFach = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects", new { name = Eindeutig("Fach") }));
        Assert.Equal(HttpStatusCode.NotFound,
            (await creator.GetAsync($"/api/v1/creator/subjects/{anderesFach}/chapters/{chapterId}")).StatusCode);
    }

    [Fact]
    public async Task Uebungs_Kategorien_Lesen_Und_Loeschen()
    {
        var creator = await TestApi.FatherAsync(factory);
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
        var creator = await TestApi.FatherAsync(factory);
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
        var creator = await TestApi.FatherAsync(factory);
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
        var creator = await TestApi.FatherAsync(factory);
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

    [Fact]
    public async Task Vokabel_Item_Einzeln_Lesen()
    {
        var creator = await TestApi.FatherAsync(factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(creator, Eindeutig("hedgehog"), "Igel");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(creator, key);
        // The exercise route needs subject and chapter; both sit on the exercise itself.
        var uebung = await Json(await creator.GetAsync($"/api/v1/creator/exercises/{exerciseId}"));
        var basis = $"/api/v1/creator/subjects/{uebung.GetProperty("subjectId").GetInt32()}"
            + $"/chapters/{uebung.GetProperty("chapterId").GetInt32()}/vocabulary/{exerciseId}/items";

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
        var creator = await TestApi.FatherAsync(factory);
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
        var owner = await TestApi.FatherAsync(factory);
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
