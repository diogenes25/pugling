using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>Happy path of the atomic vocabulary store (create/read, key uniqueness).</summary>
public class VocabularyStoreTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Create_Get_ByKey_List()
    {
        var father = await TestApi.FatherAsync(factory);
        var create = await father.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            key = "en_cat_de_katze",
            sourceLanguage = "en",
            targetLanguage = "de",
            word = "cat",
            translation = "Katze",
            partOfSpeech = "Noun",
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = await TestApi.IdAsync(create);

        Assert.Equal(HttpStatusCode.OK, (await father.GetAsync($"/api/v1/creator/vocabulary/{id}")).StatusCode);

        var byKey = await father.GetAsync("/api/v1/creator/vocabulary/by-key/en_cat_de_katze");
        Assert.Equal(HttpStatusCode.OK, byKey.StatusCode);
        Assert.Equal("cat", (await byKey.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("word").GetString());

        var list = await (await father.GetAsync("/api/v1/creator/vocabulary")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(list.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task DoppelterKey_Liefert409()
    {
        var father = await TestApi.FatherAsync(factory);
        var dto = new
        {
            key = "en_dog_de_hund",
            sourceLanguage = "en",
            targetLanguage = "de",
            word = "dog",
            translation = "Hund",
            partOfSpeech = "Noun",
        };
        Assert.Equal(HttpStatusCode.Created, (await father.PostAsJsonAsync("/api/v1/creator/vocabulary", dto)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await father.PostAsJsonAsync("/api/v1/creator/vocabulary", dto)).StatusCode);
    }

    /// <summary>
    /// B-65: the equally valid translations survive POST, PATCH and both batch routes. The single PATCH is
    /// covered by <c>PatchSemanticsTests</c>; what needs its own test here is the batch pair – it maps the
    /// fields by hand and would silently drop a forgotten one.
    /// </summary>
    [Fact]
    public async Task Uebersetzungsvarianten_UeberlebenAlleSchreibwege()
    {
        var father = await TestApi.FatherAsync(factory);

        var created = await father.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            sourceLanguage = "en",
            targetLanguage = "de",
            word = "huge",
            translation = "riesig",
            // Blanks, duplicates and the primary translation itself are cleaned out, while a translation
            // containing a comma stays intact - that is what the repeated field in the editor is for.
            translationAlternatives = new[] { "sehr groß", " ", "sehr groß", "Riesig", "groß, wirklich groß" },
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = await TestApi.IdAsync(created);
        Assert.Equal(new[] { "sehr groß", "groß, wirklich groß" }, await VariantenAsync(father, id));

        (await father.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{id}",
            new { translationAlternatives = new[] { "enorm" } })).EnsureSuccessStatusCode();
        Assert.Equal(new[] { "enorm" }, await VariantenAsync(father, id));

        var batchPatch = await father.PatchAsJsonAsync("/api/v1/creator/vocabulary/batch",
            new[] { new { id, translationAlternatives = new[] { "gigantisch" } } });
        batchPatch.EnsureSuccessStatusCode();
        Assert.Equal(new[] { "gigantisch" }, await VariantenAsync(father, id));

        var batchClear = await father.PatchAsJsonAsync("/api/v1/creator/vocabulary/batch",
            new[] { new { id, clearTranslationAlternatives = true } });
        batchClear.EnsureSuccessStatusCode();
        Assert.Null(await VariantenAsync(father, id));

        var batchCreate = await father.PostAsJsonAsync("/api/v1/creator/vocabulary/batch", new[]
        {
            new { sourceLanguage = "en", targetLanguage = "de", word = "tiny", translation = "winzig",
                translationAlternatives = new[] { "sehr klein" } },
        });
        batchCreate.EnsureSuccessStatusCode();
        var neu = (await batchCreate.Content.ReadFromJsonAsync<JsonElement>())[0].GetProperty("id").GetInt32();
        Assert.Equal(new[] { "sehr klein" }, await VariantenAsync(father, neu));
    }

    /// <summary>Reads the equally valid translations of an entry back (<c>null</c> = none declared).</summary>
    private static async Task<string[]?> VariantenAsync(HttpClient father, int id)
    {
        var entry = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/vocabulary/{id}");
        var field = entry.GetProperty("translationAlternatives");
        return field.ValueKind == JsonValueKind.Null
            ? null
            : [.. field.EnumerateArray().Select(v => v.GetString()!)];
    }
}
