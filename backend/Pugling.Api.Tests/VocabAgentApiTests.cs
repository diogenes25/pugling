using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// The agent-friendly store capabilities: creating "simple" entries (word only), filtering incomplete
/// vocabulary, bulk creating/backfilling via lookup/batch, navigating the form family and tagging
/// vocabulary. Isolated via a made-up language pair (fa/fb) so the filters don't collide with seed data.
/// </summary>
public class VocabAgentApiTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private static async Task<JsonElement> CreateAsync(HttpClient c, object body)
    {
        var res = await c.PostAsJsonAsync("/api/v1/creator/vocabulary", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<List<string>> KeysAsync(HttpClient c, string query)
    {
        var arr = await c.GetFromJsonAsync<JsonElement>($"/api/v1/creator/vocabulary?{query}");
        return arr.EnumerateArray().Select(v => v.GetProperty("key").GetString()!).ToList();
    }

    [Fact]
    public async Task WortOhneUebersetzung_WirdAngelegt_TranslationLeer()
    {
        var father = await TestApi.AdultAsync(_factory);

        var res = await father.PostAsJsonAsync("/api/v1/creator/vocabulary",
            new { sourceLanguage = "fa", targetLanguage = "fb", word = "solo" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var v = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("", v.GetProperty("translation").GetString());
        Assert.Equal("fa_solo_fb", v.GetProperty("key").GetString());
    }

    [Fact]
    public async Task Filter_Untranslated_Incomplete_Linked()
    {
        var father = await TestApi.AdultAsync(_factory);
        const string sl = "flt";

        // without a translation → untranslated + incomplete
        await CreateAsync(father, new { sourceLanguage = sl, targetLanguage = "fb", word = "raw" });
        // complete (a noun with details) → neither
        await CreateAsync(father, new
        {
            sourceLanguage = sl,
            targetLanguage = "fb",
            word = "full",
            translation = "voll",
            partOfSpeech = "Noun",
            noun = new { article = "das" },
        });
        // translated, but part of speech Other → incomplete, not untranslated
        await CreateAsync(father, new { sourceLanguage = sl, targetLanguage = "fb", word = "half", translation = "halb" });

        var untranslated = await KeysAsync(father, $"sourceLanguage={sl}&untranslated=true");
        Assert.Equal(["flt_raw_fb"], untranslated);

        var incomplete = await KeysAsync(father, $"sourceLanguage={sl}&incomplete=true");
        Assert.Contains("flt_raw_fb", incomplete);
        Assert.Contains("flt_half_fb_halb", incomplete);
        Assert.DoesNotContain("flt_full_fb_voll", incomplete);
    }

    [Fact]
    public async Task Linked_Filter_TrenntGrundformVonFlektierterForm()
    {
        var father = await TestApi.AdultAsync(_factory);
        const string sl = "lnk";

        var baseForm = await CreateAsync(father, new { sourceLanguage = sl, targetLanguage = "fb", word = "swim", translation = "schwimmen" });
        var baseKey = baseForm.GetProperty("key").GetString()!;
        await CreateAsync(father, new
        {
            sourceLanguage = sl,
            targetLanguage = "fb",
            word = "swam",
            translation = "schwamm",
            baseFormKey = baseKey,
            baseFormRelation = "Präteritum",
        });

        var linked = await KeysAsync(father, $"sourceLanguage={sl}&linked=true");
        Assert.Equal(["lnk_swam_fb_schwamm"], linked);

        var unlinked = await KeysAsync(father, $"sourceLanguage={sl}&linked=false");
        Assert.Equal([baseKey], unlinked);
    }

    [Fact]
    public async Task Lookup_FindetVorhandene_MeldetFehlende()
    {
        var father = await TestApi.AdultAsync(_factory);
        await CreateAsync(father, new { sourceLanguage = "lkp", targetLanguage = "fb", word = "banana", translation = "Banane" });

        var res = await father.PostAsJsonAsync("/api/v1/creator/vocabulary/lookup",
            new { sourceLanguage = "lkp", words = new[] { "banana", "missing" } });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();

        var words = body.GetProperty("words").EnumerateArray().ToList();
        var banana = words.Single(w => w.GetProperty("word").GetString() == "banana");
        var missing = words.Single(w => w.GetProperty("word").GetString() == "missing");
        JsonAssert.True(banana, "exists");
        Assert.Single(banana.GetProperty("matches").EnumerateArray());
        JsonAssert.False(missing, "exists");
    }

    [Fact]
    public async Task Batch_IstIdempotent_ExplizitenKeys()
    {
        var father = await TestApi.AdultAsync(_factory);
        object batch = new[]
        {
            new { key = "batch_alpha", sourceLanguage = "bat", targetLanguage = "fb", word = "alpha", translation = "a" },
        };

        var first = await (await father.PostAsJsonAsync("/api/v1/creator/vocabulary/batch", batch)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("created", first[0].GetProperty("status").GetString());

        var second = await (await father.PostAsJsonAsync("/api/v1/creator/vocabulary/batch", batch)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("existing", second[0].GetProperty("status").GetString());
        Assert.Equal(first[0].GetProperty("id").GetInt32(), second[0].GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task BatchPatch_TraegtUebersetzungenNach()
    {
        var father = await TestApi.AdultAsync(_factory);
        var a = await CreateAsync(father, new { sourceLanguage = "bpa", targetLanguage = "fb", word = "uno" });
        var b = await CreateAsync(father, new { sourceLanguage = "bpa", targetLanguage = "fb", word = "due" });

        var patch = new[]
        {
            new { id = a.GetProperty("id").GetInt32(), translation = "eins" },
            new { id = b.GetProperty("id").GetInt32(), translation = "zwei" },
        };
        var res = await (await father.PatchAsJsonAsync("/api/v1/creator/vocabulary/batch", patch)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.All(res.EnumerateArray(), r => Assert.Equal("updated", r.GetProperty("status").GetString()));

        var remaining = await KeysAsync(father, "sourceLanguage=bpa&untranslated=true");
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task Forms_LiefertGrundformFamilieMitLabel()
    {
        var father = await TestApi.AdultAsync(_factory);
        const string sl = "fam";
        var baseForm = await CreateAsync(father, new { sourceLanguage = sl, targetLanguage = "fb", word = "go", translation = "gehen" });
        var baseKey = baseForm.GetProperty("key").GetString();
        var went = await CreateAsync(father, new { sourceLanguage = sl, targetLanguage = "fb", word = "went", translation = "ging", baseFormKey = baseKey, baseFormRelation = "Präteritum" });
        await CreateAsync(father, new { sourceLanguage = sl, targetLanguage = "fb", word = "gone", translation = "gegangen", baseFormKey = baseKey, baseFormRelation = "Partizip II" });

        // A query through an inflected form returns the whole family, base form first.
        var forms = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/vocabulary/{went.GetProperty("id").GetInt32()}/forms");
        var list = forms.EnumerateArray().ToList();
        Assert.Equal(3, list.Count);
        Assert.Equal(baseKey, list[0].GetProperty("key").GetString());
        var wentEntry = list.Single(f => f.GetProperty("word").GetString() == "went");
        Assert.Equal("Präteritum", wentEntry.GetProperty("baseFormRelation").GetString());
    }

    [Fact]
    public async Task Tags_AnlegenFilternUndOder_Loeschen()
    {
        var father = await TestApi.AdultAsync(_factory);
        const string sl = "tag";
        var k5 = Uri.EscapeDataString("Kapitel 5");
        var k7 = Uri.EscapeDataString("Klasse 7");

        // Tag two entries: one with both tags, one with "Kapitel 5" only.
        var both = await CreateAsync(father, new { sourceLanguage = sl, targetLanguage = "fb", word = "both", translation = "beide", tags = new[] { "Kapitel 5", "Klasse 7" } });
        await CreateAsync(father, new { sourceLanguage = sl, targetLanguage = "fb", word = "one", translation = "eins", tags = new[] { "Kapitel 5" } });

        Assert.Contains("Kapitel 5", both.GetProperty("tags").EnumerateArray().Select(t => t.GetString()));

        var kapitel5 = await KeysAsync(father, $"sourceLanguage={sl}&tag={k5}");
        Assert.Equal(2, kapitel5.Count);

        var orBoth = await KeysAsync(father, $"sourceLanguage={sl}&tag={k5}&tag={k7}");
        Assert.Equal(2, orBoth.Count); // OR

        var andBoth = await KeysAsync(father, $"sourceLanguage={sl}&tag={k5}&tag={k7}&matchAll=true");
        Assert.Equal(["tag_both_fb_beide"], andBoth); // AND → only the doubly tagged one

        // Deleting a tag removes the links.
        var tags = await father.GetFromJsonAsync<JsonElement>("/api/v1/creator/vocabulary/tags");
        var kapitelId = tags.EnumerateArray().Single(t => t.GetProperty("name").GetString() == "Kapitel 5").GetProperty("id").GetInt32();
        var del = await father.DeleteAsync($"/api/v1/creator/vocabulary/tags/{kapitelId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var afterDelete = await KeysAsync(father, $"sourceLanguage={sl}&tag={k5}");
        Assert.Empty(afterDelete);
    }

    [Fact]
    public async Task Tag_ErneutAnlegen_LiefertDenEchtenVerlinkungszaehler()
    {
        var father = await TestApi.AdultAsync(_factory);
        const string sl = "tagcount";
        var tagName = "Zaehl-Tag";

        // Two entries carry the tag - created through the vocabulary attach path (the same VocabTagLink rows
        // a real usage would produce).
        await CreateAsync(father, new { sourceLanguage = sl, targetLanguage = "fb", word = "one", translation = "eins", tags = new[] { tagName } });
        await CreateAsync(father, new { sourceLanguage = sl, targetLanguage = "fb", word = "two", translation = "zwei", tags = new[] { tagName } });

        // Re-creating the SAME tag name is the idempotent "already exists" branch (B-98): it must report the
        // REAL link count, not the always-0 value of an unloaded navigation.
        var again = await father.PostAsJsonAsync("/api/v1/creator/vocabulary/tags", new { name = tagName });
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        var body = await again.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, body.GetProperty("vocabCount").GetInt32());

        // Matches what the listing endpoint reports for the same tag - one source of truth.
        var list = await father.GetFromJsonAsync<JsonElement>("/api/v1/creator/vocabulary/tags");
        var listedCount = list.EnumerateArray().Single(t => t.GetProperty("name").GetString() == tagName).GetProperty("vocabCount").GetInt32();
        Assert.Equal(listedCount, body.GetProperty("vocabCount").GetInt32());
    }

    [Fact]
    public async Task List_SetztTotalCountHeader()
    {
        var father = await TestApi.AdultAsync(_factory);
        await CreateAsync(father, new { sourceLanguage = "hdr", targetLanguage = "fb", word = "x", translation = "x" });

        var res = await father.GetAsync("/api/v1/creator/vocabulary?sourceLanguage=hdr");
        res.EnsureSuccessStatusCode();
        Assert.True(res.Headers.TryGetValues("X-Total-Count", out var values));
        Assert.Equal("1", values!.First());
    }
}
