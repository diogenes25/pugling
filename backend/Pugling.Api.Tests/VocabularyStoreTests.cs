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
        var father = await TestApi.AdultAsync(factory);
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
        var father = await TestApi.AdultAsync(factory);
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
    /// B-277: <c>reuseExisting</c> lets a quick-add hit the entry the server key already names instead of creating a
    /// <c>_2</c> copy. The two spellings differ in an accented capital, which SQLite's NOCASE does not fold - only the
    /// server key does, so a client-side comparison could not get this case right. Without the flag the old behaviour
    /// stays (a second entry), which is what keeps the flag from being decoration.
    /// </summary>
    [Fact]
    public async Task ReuseExisting_TrifftDenEintragDesServerSchluessels_UndErgaenztVarianten()
    {
        var father = await TestApi.AdultAsync(factory);
        var u = Guid.NewGuid().ToString("N")[..8];
        var original = await father.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            sourceLanguage = "fr",
            targetLanguage = "de",
            word = $"École{u}",
            translation = "Schule",
            translationAlternatives = new[] { "Lehranstalt" },
        });
        var originalId = await TestApi.IdAsync(original);

        var reused = await father.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            sourceLanguage = "fr",
            targetLanguage = "de",
            word = $"école{u}",
            translation = "schule",
            translationAlternatives = new[] { "Bildungsstätte", "lehranstalt" },
            tags = new[] { $"B277-{u}" },
            reuseExisting = true,
        });

        Assert.Equal(HttpStatusCode.OK, reused.StatusCode);
        var body = await reused.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(originalId, body.GetProperty("id").GetInt32());
        // Merged, not replaced; the repeated variant in another case is not listed twice.
        Assert.Equal(["Bildungsstätte", "Lehranstalt"], (await VariantenAsync(father, originalId))!.Order());
        Assert.Equal($"École{u}", body.GetProperty("word").GetString());
        // Additive only: the sent spelling of the translation does not overwrite the stored one, tags join in.
        Assert.Equal("Schule", body.GetProperty("translation").GetString());
        Assert.Contains($"B277-{u}", body.GetProperty("tags").EnumerateArray().Select(t => t.GetString()));

        var copy = await father.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            sourceLanguage = "fr",
            targetLanguage = "de",
            word = $"école{u}",
            translation = "schule",
        });
        Assert.Equal(HttpStatusCode.Created, copy.StatusCode);
        Assert.EndsWith("_2", (await copy.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("key").GetString());
    }

    /// <summary>
    /// The flag must not change what is valid. With an explicit key it would be ignored without a word - the caller
    /// would get a 409 duplicate_key it did not ask about; on a hit it used to skip the base-form check that a create
    /// runs, and the unknown base form came back as a 200.
    /// </summary>
    [Fact]
    public async Task ReuseExisting_UmgehtKeinePruefung()
    {
        var father = await TestApi.AdultAsync(factory);
        var u = Guid.NewGuid().ToString("N")[..8];
        var key = await (await father.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            sourceLanguage = "en",
            targetLanguage = "de",
            word = $"tree{u}",
            translation = "Baum",
        })).Content.ReadFromJsonAsync<JsonElement>();

        var withKey = await father.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            key = key.GetProperty("key").GetString(),
            sourceLanguage = "en",
            targetLanguage = "de",
            word = $"tree{u}",
            translation = "Baum",
            reuseExisting = true,
        });
        Assert.Equal(HttpStatusCode.BadRequest, withKey.StatusCode);
        Assert.Equal("validation_error", (await withKey.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        var unknownBase = await father.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            sourceLanguage = "en",
            targetLanguage = "de",
            word = $"tree{u}",
            translation = "Baum",
            baseFormKey = $"gibt-es-nicht-{u}",
            reuseExisting = true,
        });
        Assert.Equal(HttpStatusCode.BadRequest, unknownBase.StatusCode);
    }

    /// <summary>The batch reports a reused entry as <c>existing</c> with its id - not as <c>created</c>.</summary>
    [Fact]
    public async Task ReuseExisting_ImBatch_MeldetExisting()
    {
        var father = await TestApi.AdultAsync(factory);
        var u = Guid.NewGuid().ToString("N")[..8];
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            sourceLanguage = "en",
            targetLanguage = "de",
            word = $"river{u}",
            translation = "Fluss",
        }));

        var batch = await father.PostAsJsonAsync("/api/v1/creator/vocabulary/batch", new[]
        {
            new { sourceLanguage = "en", targetLanguage = "de", word = $"River{u}", translation = "fluss", reuseExisting = true },
        });

        var result = (await batch.Content.ReadFromJsonAsync<List<JsonElement>>())!.Single();
        Assert.Equal("Existing", result.GetProperty("status").GetString());
        Assert.Equal(id, result.GetProperty("id").GetInt32());
    }

    /// <summary>
    /// B-65: the equally valid translations survive POST, PATCH and both batch routes. The single PATCH is
    /// covered by <c>PatchSemanticsTests</c>; what needs its own test here is the batch pair – it maps the
    /// fields by hand and would silently drop a forgotten one.
    /// </summary>
    [Fact]
    public async Task Uebersetzungsvarianten_UeberlebenAlleSchreibwege()
    {
        var father = await TestApi.AdultAsync(factory);

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

    /// <summary>
    /// A PATCH that only moves the translation onto one of its own alternatives must not leave that word
    /// listed twice - the entry would read "riesig, also: riesig".
    /// </summary>
    [Fact]
    public async Task Uebersetzung_AufEineVarianteGesetzt_LaesstSieNichtDoppeltStehen()
    {
        var father = await TestApi.AdultAsync(factory);
        var (id, _) = await TestApi.CreateStoreVocabAsync(father, "enormous", "riesig",
            translationAlternatives: ["sehr groß"]);

        (await father.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{id}",
            new { translation = "sehr groß" })).EnsureSuccessStatusCode();

        Assert.Null(await VariantenAsync(father, id));
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
