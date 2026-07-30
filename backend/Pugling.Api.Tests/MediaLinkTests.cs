using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Link between an image and its carrier (stage 3). The core of these tests is the <b>n:m property in both
/// directions</b> - it's the reason the link is its own table rather than a column on the carrier
/// (the way pronunciation audio does it 1:1). Plus the specificity cascade item ⊐ vocabulary and the
/// permission split: the child-neutral store is freely editable, tampering with an exercise is not.
/// </summary>
public class MediaLinkTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task EineVokabel_TraegtMehrereDarstellungen()
    {
        var father = await TestApi.FatherAsync(factory);
        var (vocabId, _) = await TestApi.CreateStoreVocabAsync(father, "run", "laufen");

        foreach (var (key, description, weight) in new[]
                 {
                     ("link_run_unicorn", "Ein Einhorn laeuft im Comic-Stil", 0),
                     ("link_run_flash", "Der Superheld Flash rennt", 5),
                     ("link_run_photo", "Eine joggende Person, Foto", 0),
                 })
        {
            var assetId = await CreateAssetAsync(father, key, description);
            var res = await father.PostAsJsonAsync($"/api/v1/creator/vocabulary/{vocabId}/media",
                new { mediaAssetId = assetId, weight });
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        }

        var links = await GetAsync(father, $"/api/v1/creator/vocabulary/{vocabId}/media");
        Assert.Equal(3, links.GetArrayLength());
        // Bester redaktioneller Rang zuerst – er entscheidet später nur bei Gleichstand der Interessen.
        Assert.Equal("link_run_flash", links[0].GetProperty("asset").GetProperty("key").GetString());
        // Die Antwort trägt das Asset mit, damit eine Liste ohne Nachladen darstellbar ist.
        Assert.Equal("Der Superheld Flash rennt", links[0].GetProperty("asset").GetProperty("description").GetString());
    }

    [Fact]
    public async Task EinBild_DientMehrerenVokabeln()
    {
        var father = await TestApi.FatherAsync(factory);
        var assetId = await CreateAssetAsync(father, "link_shared_running", "Laufendes Einhorn");

        // „run" (en→de) und „laufen" (de→en) sind getrennte Store-Zeilen – dasselbe Bild dient beiden.
        var (enId, _) = await TestApi.CreateStoreVocabAsync(father, "run", "laufen", "en", "de");
        var (deId, _) = await TestApi.CreateStoreVocabAsync(father, "laufen", "run", "de", "en");

        foreach (var vocabId in new[] { enId, deId })
            Assert.Equal(HttpStatusCode.Created,
                (await father.PostAsJsonAsync($"/api/v1/creator/vocabulary/{vocabId}/media",
                    new { mediaAssetId = assetId })).StatusCode);

        var usage = await GetAsync(father, $"/api/v1/creator/media/{assetId}/usage");
        Assert.Equal(2, usage.GetArrayLength());
        Assert.All(usage.EnumerateArray(), u => Assert.Equal("vocabulary", u.GetProperty("carrier").GetString()));
    }

    [Fact]
    public async Task DasselbeBild_ZweimalAmSelbenTraeger_Liefert409()
    {
        var father = await TestApi.FatherAsync(factory);
        var assetId = await CreateAssetAsync(father, "link_dupe", "Ein Motiv");
        var (vocabId, _) = await TestApi.CreateStoreVocabAsync(father, "dupe-word", "Dublette");

        await father.PostAsJsonAsync($"/api/v1/creator/vocabulary/{vocabId}/media", new { mediaAssetId = assetId });
        var again = await father.PostAsJsonAsync($"/api/v1/creator/vocabulary/{vocabId}/media", new { mediaAssetId = assetId });

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("media_already_linked", await CodeOf(again));
    }

    [Fact]
    public async Task ZuordnungPerKey_FuerAgentenDieUeberSprechendeKeysArbeiten()
    {
        var father = await TestApi.FatherAsync(factory);
        await CreateAssetAsync(father, "link_by_key_motiv", "Per Key zugeordnet");
        var (vocabId, _) = await TestApi.CreateStoreVocabAsync(father, "keyword", "Schluesselwort");

        var res = await father.PostAsJsonAsync($"/api/v1/creator/vocabulary/{vocabId}/media",
            new { key = "link_by_key_motiv" });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var link = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("link_by_key_motiv", link.GetProperty("asset").GetProperty("key").GetString());
    }

    [Fact]
    public async Task WederIdNochKey_Liefert400()
    {
        var father = await TestApi.FatherAsync(factory);
        var (vocabId, _) = await TestApi.CreateStoreVocabAsync(father, "leer-ref", "Ohne Referenz");

        var res = await father.PostAsJsonAsync($"/api/v1/creator/vocabulary/{vocabId}/media", new { weight = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("invalid_reference", await CodeOf(res));
    }

    [Fact]
    public async Task ItemZuordnung_StehtNebenDerStoreZuordnung()
    {
        var father = await TestApi.FatherAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("cat", "Katze"));
        var itemId = await FirstItemIdAsync(father, exerciseId);
        var vocabId = await FirstItemVocabIdAsync(father, exerciseId);

        var storeAsset = await CreateAssetAsync(father, "link_cat_store", "Eine Katze, allgemein");
        var itemAsset = await CreateAssetAsync(father, "link_cat_item", "Eine Katze im Comic-Stil");

        await father.PostAsJsonAsync($"/api/v1/creator/vocabulary/{vocabId}/media", new { mediaAssetId = storeAsset });
        var itemLink = await father.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/items/{itemId}/media",
            new { mediaAssetId = itemAsset });
        Assert.Equal(HttpStatusCode.Created, itemLink.StatusCode);

        // Beide Ebenen bleiben getrennt sichtbar – die Kaskade (Item schlägt Vokabel) zieht erst der
        // Resolver in Etappe 4; hier darf nichts stillschweigend überschrieben werden.
        var store = await GetAsync(father, $"/api/v1/creator/vocabulary/{vocabId}/media");
        var item = await GetAsync(father, $"/api/v1/creator/exercises/{exerciseId}/items/{itemId}/media");
        Assert.Equal("link_cat_store", Single(store).GetProperty("asset").GetProperty("key").GetString());
        Assert.Equal("link_cat_item", Single(item).GetProperty("asset").GetProperty("key").GetString());
    }

    [Fact]
    public async Task Titelbild_HaengtAnDerUebung_OhneWortbezug()
    {
        var father = await TestApi.FatherAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("story", "Geschichte"));
        var assetId = await CreateAssetAsync(father, "link_cover", "Aufmacher der Lektion");

        Assert.Equal(HttpStatusCode.Created,
            (await father.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/media",
                new { mediaAssetId = assetId })).StatusCode);

        var usage = await GetAsync(father, $"/api/v1/creator/media/{assetId}/usage");
        Assert.Equal("exercise", Single(usage).GetProperty("carrier").GetString());
        Assert.Equal(exerciseId, Single(usage).GetProperty("carrierId").GetInt32());
    }

    [Fact]
    public async Task FremdesItem_UeberFremdeUebung_WirdNichtGetroffen()
    {
        var father = await TestApi.FatherAsync(factory);
        var exerciseA = await TestApi.CreateVocabExerciseAsync(father, ("alpha", "Alpha"));
        var exerciseB = await TestApi.CreateVocabExerciseAsync(father, ("beta", "Beta"));
        var itemOfA = await FirstItemIdAsync(father, exerciseA);
        var assetId = await CreateAssetAsync(father, "link_cross_item", "Motiv");

        var res = await father.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseB}/items/{itemOfA}/media",
            new { mediaAssetId = assetId });

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal("item_not_found", await CodeOf(res));
    }

    [Fact]
    public async Task OhneSchreibrecht_BleibtDieUebungZu_DerStoreAberOffen()
    {
        var owner = await TestApi.FatherAsync(factory);
        // Herr Schmidt (Seed-Konto 2) ist Creator, aber nicht Autor dieser Übung.
        var stranger = await TestApi.FatherAsync(factory, id: 2, pin: "9999");

        var exerciseId = await TestApi.CreateVocabExerciseAsync(owner, ("locked", "gesperrt"));
        var assetId = await CreateAssetAsync(stranger, "link_stranger", "Motiv eines Fremden");

        var onExercise = await stranger.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/media",
            new { mediaAssetId = assetId });
        Assert.Equal(HttpStatusCode.Forbidden, onExercise.StatusCode);
        Assert.Equal("not_author", await CodeOf(onExercise));

        // Der Vokabel-Store ist dagegen kindneutral und gemeinsam – dort darf jeder Creator zuordnen.
        var (vocabId, _) = await TestApi.CreateStoreVocabAsync(owner, "shared-word", "geteilt");
        var onStore = await stranger.PostAsJsonAsync($"/api/v1/creator/vocabulary/{vocabId}/media",
            new { mediaAssetId = assetId });
        Assert.Equal(HttpStatusCode.Created, onStore.StatusCode);
    }

    [Fact]
    public async Task Loesen_LaesstDasBildImStore()
    {
        var father = await TestApi.FatherAsync(factory);
        var assetId = await CreateAssetAsync(father, "link_survives", "Bleibt im Store");
        var (vocabId, _) = await TestApi.CreateStoreVocabAsync(father, "detach-word", "loesen");

        var linkId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/vocabulary/{vocabId}/media", new { mediaAssetId = assetId }));

        Assert.Equal(HttpStatusCode.NoContent,
            (await father.DeleteAsync($"/api/v1/creator/vocabulary/{vocabId}/media/{linkId}")).StatusCode);

        Assert.Empty((await GetAsync(father, $"/api/v1/creator/vocabulary/{vocabId}/media")).EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, (await father.GetAsync($"/api/v1/creator/media/{assetId}")).StatusCode);
    }

    [Fact]
    public async Task FremdeZuordnung_UeberFremdenTraeger_Liefert404()
    {
        var father = await TestApi.FatherAsync(factory);
        var assetId = await CreateAssetAsync(father, "link_wrong_carrier", "Motiv");
        var (vocabA, _) = await TestApi.CreateStoreVocabAsync(father, "carrier-a", "Traeger A");
        var (vocabB, _) = await TestApi.CreateStoreVocabAsync(father, "carrier-b", "Traeger B");

        var linkId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/vocabulary/{vocabA}/media", new { mediaAssetId = assetId }));

        var res = await father.DeleteAsync($"/api/v1/creator/vocabulary/{vocabB}/media/{linkId}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal("media_link_not_found", await CodeOf(res));
    }

    [Fact]
    public async Task RangAendern_SortiertDieAuswahlNeu()
    {
        var father = await TestApi.FatherAsync(factory);
        var (vocabId, _) = await TestApi.CreateStoreVocabAsync(father, "rank-word", "Rang");
        var first = await CreateAssetAsync(father, "link_rank_a", "Motiv A");
        var second = await CreateAssetAsync(father, "link_rank_b", "Motiv B");

        var linkA = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/vocabulary/{vocabId}/media", new { mediaAssetId = first, weight = 0 }));
        await father.PostAsJsonAsync($"/api/v1/creator/vocabulary/{vocabId}/media",
            new { mediaAssetId = second, weight = 3 });

        Assert.Equal("link_rank_b",
            (await GetAsync(father, $"/api/v1/creator/vocabulary/{vocabId}/media"))[0]
                .GetProperty("asset").GetProperty("key").GetString());

        var patch = await father.PatchAsJsonAsync($"/api/v1/creator/vocabulary/{vocabId}/media/{linkA}", new { weight = 9 });
        patch.EnsureSuccessStatusCode();

        Assert.Equal("link_rank_a",
            (await GetAsync(father, $"/api/v1/creator/vocabulary/{vocabId}/media"))[0]
                .GetProperty("asset").GetProperty("key").GetString());
    }

    [Fact]
    public async Task BildLoeschen_RaeumtSeineZuordnungenAb()
    {
        var father = await TestApi.FatherAsync(factory);
        var assetId = await CreateAssetAsync(father, "link_cascade", "Verschwindet gleich");
        var (vocabId, _) = await TestApi.CreateStoreVocabAsync(father, "cascade-word", "Kaskade");
        await father.PostAsJsonAsync($"/api/v1/creator/vocabulary/{vocabId}/media", new { mediaAssetId = assetId });

        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync($"/api/v1/creator/media/{assetId}")).StatusCode);

        // Kein Platzhalter, keine Sperre: die Auswahl schrumpft, die Vokabel bleibt unversehrt.
        Assert.Empty((await GetAsync(father, $"/api/v1/creator/vocabulary/{vocabId}/media")).EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, (await father.GetAsync($"/api/v1/creator/vocabulary/{vocabId}")).StatusCode);
    }

    // ---- Helfer -------------------------------------------------------------------------------------

    private static async Task<int> CreateAssetAsync(HttpClient father, string key, string description) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/media", new
        {
            key,
            description,
            variants = new[] { new { purpose = "Card", url = $"https://cdn.test/{key}.webp", width = 512, height = 512 } },
        }));

    private static async Task<int> FirstItemIdAsync(HttpClient father, int exerciseId) =>
        (await ItemsAsync(father, exerciseId))[0].GetProperty("id").GetInt32();

    private static async Task<int> FirstItemVocabIdAsync(HttpClient father, int exerciseId) =>
        (await ItemsAsync(father, exerciseId))[0].GetProperty("vocabularyId").GetInt32();

    /// <summary>Reads items via the exercise's HATEOAS link - without repeating subject/chapter in the test.</summary>
    private static async Task<JsonElement> ItemsAsync(HttpClient father, int exerciseId)
    {
        var exercise = await GetAsync(father, $"/api/v1/creator/exercises/{exerciseId}");
        var subjectId = exercise.GetProperty("subjectId").GetInt32();
        var chapterId = exercise.GetProperty("chapterId").GetInt32();
        return await GetAsync(father,
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}/items");
    }

    private static JsonElement Single(JsonElement array)
    {
        Assert.Equal(1, array.GetArrayLength());
        return array[0];
    }

    private static async Task<JsonElement> GetAsync(HttpClient client, string url)
    {
        var res = await client.GetAsync(url);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<string?> CodeOf(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
}
