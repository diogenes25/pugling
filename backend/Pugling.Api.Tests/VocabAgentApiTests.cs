using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Die Agenten-tauglichen Store-Fähigkeiten: „einfach" (nur Word) anlegen, unfertige Vokabeln filtern,
/// per Lookup/Batch massenhaft anlegen/nachtragen, die Formen-Familie navigieren und Vokabeln taggen.
/// Isoliert über ein erfundenes Sprachpaar (fa/fb), damit die Filter nicht mit Seed-Daten kollidieren.
/// </summary>
public class VocabAgentApiTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private static async Task<JsonElement> CreateAsync(HttpClient c, object body)
    {
        var res = await c.PostAsJsonAsync("/api/v1/learn/vocabulary", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<List<string>> KeysAsync(HttpClient c, string query)
    {
        var arr = await c.GetFromJsonAsync<JsonElement>($"/api/v1/learn/vocabulary?{query}");
        return arr.EnumerateArray().Select(v => v.GetProperty("key").GetString()!).ToList();
    }

    [Fact]
    public async Task WortOhneUebersetzung_WirdAngelegt_TranslationLeer()
    {
        var father = await TestApi.FatherAsync(_factory);

        var res = await father.PostAsJsonAsync("/api/v1/learn/vocabulary",
            new { sourceLanguage = "fa", targetLanguage = "fb", word = "solo" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var v = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("", v.GetProperty("translation").GetString());
        Assert.Equal("fa_solo_fb", v.GetProperty("key").GetString());
    }

    [Fact]
    public async Task Filter_Untranslated_Incomplete_Linked()
    {
        var father = await TestApi.FatherAsync(_factory);
        const string sl = "flt";

        // ohne Übersetzung → untranslated + incomplete
        await CreateAsync(father, new { sourceLanguage = sl, targetLanguage = "fb", word = "raw" });
        // vollständig (Noun mit Details) → weder noch
        await CreateAsync(father, new
        {
            sourceLanguage = sl,
            targetLanguage = "fb",
            word = "full",
            translation = "voll",
            partOfSpeech = "Noun",
            noun = new { article = "das" },
        });
        // übersetzt, aber Wortart Other → incomplete, nicht untranslated
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
        var father = await TestApi.FatherAsync(_factory);
        const string sl = "lnk";

        var baseForm = await CreateAsync(father, new { sourceLanguage = sl, targetLanguage = "fb", word = "swim", translation = "schwimmen" });
        var baseKey = baseForm.GetProperty("key").GetString();
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
        var father = await TestApi.FatherAsync(_factory);
        await CreateAsync(father, new { sourceLanguage = "lkp", targetLanguage = "fb", word = "banana", translation = "Banane" });

        var res = await father.PostAsJsonAsync("/api/v1/learn/vocabulary/lookup",
            new { sourceLanguage = "lkp", words = new[] { "banana", "missing" } });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();

        var words = body.GetProperty("words").EnumerateArray().ToList();
        var banana = words.Single(w => w.GetProperty("word").GetString() == "banana");
        var missing = words.Single(w => w.GetProperty("word").GetString() == "missing");
        Assert.True(banana.GetProperty("exists").GetBoolean());
        Assert.Single(banana.GetProperty("matches").EnumerateArray());
        Assert.False(missing.GetProperty("exists").GetBoolean());
    }

    [Fact]
    public async Task Batch_IstIdempotent_ExplizitenKeys()
    {
        var father = await TestApi.FatherAsync(_factory);
        object batch = new[]
        {
            new { key = "batch_alpha", sourceLanguage = "bat", targetLanguage = "fb", word = "alpha", translation = "a" },
        };

        var first = await (await father.PostAsJsonAsync("/api/v1/learn/vocabulary/batch", batch)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("created", first[0].GetProperty("status").GetString());

        var second = await (await father.PostAsJsonAsync("/api/v1/learn/vocabulary/batch", batch)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("existing", second[0].GetProperty("status").GetString());
        Assert.Equal(first[0].GetProperty("id").GetInt32(), second[0].GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task BatchPatch_TraegtUebersetzungenNach()
    {
        var father = await TestApi.FatherAsync(_factory);
        var a = await CreateAsync(father, new { sourceLanguage = "bpa", targetLanguage = "fb", word = "uno" });
        var b = await CreateAsync(father, new { sourceLanguage = "bpa", targetLanguage = "fb", word = "due" });

        var patch = new[]
        {
            new { id = a.GetProperty("id").GetInt32(), translation = "eins" },
            new { id = b.GetProperty("id").GetInt32(), translation = "zwei" },
        };
        var res = await (await father.PatchAsJsonAsync("/api/v1/learn/vocabulary/batch", patch)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.All(res.EnumerateArray(), r => Assert.Equal("updated", r.GetProperty("status").GetString()));

        var remaining = await KeysAsync(father, "sourceLanguage=bpa&untranslated=true");
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task Forms_LiefertGrundformFamilieMitLabel()
    {
        var father = await TestApi.FatherAsync(_factory);
        const string sl = "fam";
        var baseForm = await CreateAsync(father, new { sourceLanguage = sl, targetLanguage = "fb", word = "go", translation = "gehen" });
        var baseKey = baseForm.GetProperty("key").GetString();
        var went = await CreateAsync(father, new { sourceLanguage = sl, targetLanguage = "fb", word = "went", translation = "ging", baseFormKey = baseKey, baseFormRelation = "Präteritum" });
        await CreateAsync(father, new { sourceLanguage = sl, targetLanguage = "fb", word = "gone", translation = "gegangen", baseFormKey = baseKey, baseFormRelation = "Partizip II" });

        // Abfrage über eine flektierte Form liefert die ganze Familie, Grundform zuerst.
        var forms = await father.GetFromJsonAsync<JsonElement>($"/api/v1/learn/vocabulary/{went.GetProperty("id").GetInt32()}/forms");
        var list = forms.EnumerateArray().ToList();
        Assert.Equal(3, list.Count);
        Assert.Equal(baseKey, list[0].GetProperty("key").GetString());
        var wentEntry = list.Single(f => f.GetProperty("word").GetString() == "went");
        Assert.Equal("Präteritum", wentEntry.GetProperty("baseFormRelation").GetString());
    }

    [Fact]
    public async Task Tags_AnlegenFilternUndOder_Loeschen()
    {
        var father = await TestApi.FatherAsync(_factory);
        const string sl = "tag";
        var k5 = Uri.EscapeDataString("Kapitel 5");
        var k7 = Uri.EscapeDataString("Klasse 7");

        // Zwei Vokabeln taggen: eine mit beiden Tags, eine nur mit „Kapitel 5".
        var both = await CreateAsync(father, new { sourceLanguage = sl, targetLanguage = "fb", word = "both", translation = "beide", tags = new[] { "Kapitel 5", "Klasse 7" } });
        await CreateAsync(father, new { sourceLanguage = sl, targetLanguage = "fb", word = "one", translation = "eins", tags = new[] { "Kapitel 5" } });

        Assert.Contains("Kapitel 5", both.GetProperty("tags").EnumerateArray().Select(t => t.GetString()));

        var kapitel5 = await KeysAsync(father, $"sourceLanguage={sl}&tag={k5}");
        Assert.Equal(2, kapitel5.Count);

        var orBoth = await KeysAsync(father, $"sourceLanguage={sl}&tag={k5}&tag={k7}");
        Assert.Equal(2, orBoth.Count); // ODER

        var andBoth = await KeysAsync(father, $"sourceLanguage={sl}&tag={k5}&tag={k7}&matchAll=true");
        Assert.Equal(["tag_both_fb_beide"], andBoth); // UND → nur die doppelt getaggte

        // Tag löschen entfernt die Verknüpfungen.
        var tags = await father.GetFromJsonAsync<JsonElement>("/api/v1/learn/vocabulary/tags");
        var kapitelId = tags.EnumerateArray().Single(t => t.GetProperty("name").GetString() == "Kapitel 5").GetProperty("id").GetInt32();
        var del = await father.DeleteAsync($"/api/v1/learn/vocabulary/tags/{kapitelId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var afterDelete = await KeysAsync(father, $"sourceLanguage={sl}&tag={k5}");
        Assert.Empty(afterDelete);
    }

    [Fact]
    public async Task List_SetztTotalCountHeader()
    {
        var father = await TestApi.FatherAsync(_factory);
        await CreateAsync(father, new { sourceLanguage = "hdr", targetLanguage = "fb", word = "x", translation = "x" });

        var res = await father.GetAsync("/api/v1/learn/vocabulary?sourceLanguage=hdr");
        res.EnsureSuccessStatusCode();
        Assert.True(res.Headers.TryGetValues("X-Total-Count", out var values));
        Assert.Equal("1", values!.First());
    }
}
