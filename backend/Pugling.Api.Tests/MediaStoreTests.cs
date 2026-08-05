using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Media store (stage 1): one motif, many representations – and per representation multiple
/// resolutions. The tests keep the two axes apart and secure the suitability filtering that the later
/// audience separation relies on.
/// </summary>
public class MediaStoreTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Anlegen_MitVariantenUndTags_LiefertBeideAchsen()
    {
        var father = await TestApi.AdultAsync(factory);

        var create = await father.PostAsJsonAsync("/api/v1/creator/media", new
        {
            key = "run_unicorn_comic",
            description = "Ein Einhorn läuft im Comic-Stil",
            rating = "Everyone",
            origin = "Generated",
            source = "sdxl: running unicorn, comic",
            tags = new[] { "Einhorn", "Comic" },
            variants = new object[]
            {
                new { purpose = "Thumb", url = "https://cdn.test/run-unicorn-128.webp", width = 128, height = 128 },
                new { purpose = "Card", url = "https://cdn.test/run-unicorn-512.webp", width = 512, height = 512 },
            },
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var asset = await create.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, asset.GetProperty("variants").GetArrayLength());
        // Tags come back as slugs: the taxonomy normalizes so that a child's interest can hit them.
        var tags = asset.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ToList();
        Assert.Contains("einhorn", tags);
        Assert.Contains("comic", tags);
    }

    [Fact]
    public async Task OhneKey_WirdEindeutigerKeyAusBeschreibungErzeugt()
    {
        var father = await TestApi.AdultAsync(factory);
        var body = new { description = "Flash rennt sehr schnell" };

        var first = await father.PostAsJsonAsync("/api/v1/creator/media", body);
        var second = await father.PostAsJsonAsync("/api/v1/creator/media", body);

        var firstKey = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("key").GetString();
        var secondKey = (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("key").GetString();

        Assert.Equal("flash-rennt-sehr-schnell", firstKey);
        Assert.Equal("flash-rennt-sehr-schnell_2", secondKey);
    }

    [Fact]
    public async Task DoppelterKey_Liefert409()
    {
        var father = await TestApi.AdultAsync(factory);
        var dto = new { key = "dupe-media-key", description = "Irgendein Motiv" };

        Assert.Equal(HttpStatusCode.Created, (await father.PostAsJsonAsync("/api/v1/creator/media", dto)).StatusCode);

        var again = await father.PostAsJsonAsync("/api/v1/creator/media", dto);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("duplicate_key", await CodeOf(again));
    }

    [Fact]
    public async Task BeschreibungIstPflicht_SieIstZugleichDerAltText()
    {
        var father = await TestApi.AdultAsync(factory);
        var res = await father.PostAsJsonAsync("/api/v1/creator/media", new { description = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("validation_error", await CodeOf(res));
    }

    [Fact]
    public async Task ZweiteVarianteMitGleichemZweckUndFormat_Liefert409()
    {
        var father = await TestApi.AdultAsync(factory);
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/media",
            new { description = "Ein Hund schläft" }));

        var first = await father.PostAsJsonAsync($"/api/v1/creator/media/{id}/variants",
            new { purpose = "Card", url = "https://cdn.test/dog.webp", width = 512, height = 512, format = "webp" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var duplicate = await father.PostAsJsonAsync($"/api/v1/creator/media/{id}/variants",
            new { purpose = "Card", url = "https://cdn.test/dog-2.webp", width = 512, height = 512, format = "webp" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("media_variant_exists", await CodeOf(duplicate));

        // Another format for the same purpose, by contrast, is wanted (<picture>/srcset).
        var avif = await father.PostAsJsonAsync($"/api/v1/creator/media/{id}/variants",
            new { purpose = "Card", url = "https://cdn.test/dog.avif", width = 512, height = 512, format = "avif" });
        Assert.Equal(HttpStatusCode.Created, avif.StatusCode);
    }

    /// <summary>
    /// "By purpose" means <b>semantic</b> ordering (Thumb → Card → Full → Hero), not alphabetical.
    /// Because <c>Purpose</c> is persisted as a string, an <c>OrderBy</c> in SQL sorted by letters
    /// (Card, Full, Hero, Thumb) – and thereby contradicted the same list on the asset, which sorts
    /// in-memory by enum value. Two endpoints, two orderings for the same data.
    /// </summary>
    [Fact]
    public async Task Varianten_SindNachZweckSortiert_NichtAlphabetisch()
    {
        var father = await TestApi.AdultAsync(factory);
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/media",
            new { description = "Ein Fuchs springt" }));

        // Deliberately created in the "wrong" order; the purposes are alphabetically twisted at the same time.
        foreach (var purpose in new[] { "Full", "Thumb", "Hero", "Card" })
            (await father.PostAsJsonAsync($"/api/v1/creator/media/{id}/variants", new
            {
                purpose,
                url = $"https://cdn.test/fox-{purpose}.webp",
                width = 128,
                height = 128,
            })).EnsureSuccessStatusCode();

        const string expected = "Thumb,Card,Full,Hero";
        var listed = await GetAsync(father, $"/api/v1/creator/media/{id}/variants");
        Assert.Equal(expected, Purposes(listed));

        // … and the asset itself returns exactly the same order.
        var asset = await GetAsync(father, $"/api/v1/creator/media/{id}");
        Assert.Equal(expected, Purposes(asset.GetProperty("variants")));

        static string Purposes(JsonElement variants) =>
            string.Join(',', variants.EnumerateArray().Select(v => v.GetProperty("purpose").GetString()));
    }

    [Fact]
    public async Task FremdeVariante_LiefertEigenenFehlercode()
    {
        var father = await TestApi.AdultAsync(factory);
        var assetA = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/media", new { description = "Motiv A" }));
        var assetB = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/media", new { description = "Motiv B" }));

        var variantId = await TestApi.IdAsync(await father.PostAsJsonAsync($"/api/v1/creator/media/{assetA}/variants",
            new { purpose = "Card", url = "https://cdn.test/a.webp", width = 100, height = 100 }));

        var res = await father.DeleteAsync($"/api/v1/creator/media/{assetB}/variants/{variantId}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal("media_variant_not_found", await CodeOf(res));
    }

    [Fact]
    public async Task MaxRating_FiltertNichtKindgerechteDarstellungenAus()
    {
        var father = await TestApi.AdultAsync(factory);
        var marker = "rating-filter-motiv";

        await father.PostAsJsonAsync("/api/v1/creator/media",
            new { description = $"{marker} als Einhorn", rating = "Everyone" });
        await father.PostAsJsonAsync("/api/v1/creator/media",
            new { description = $"{marker} freizuegig", rating = "Mature" });

        var all = await ListAsync(father, $"/api/v1/creator/media?search={marker}");
        Assert.Equal(2, all.GetArrayLength());

        // The cut that the later automatic selection applies hard per child.
        var kidSafe = await ListAsync(father, $"/api/v1/creator/media?search={marker}&maxRating=Everyone");
        Assert.Equal(1, kidSafe.GetArrayLength());
        Assert.Equal("Everyone", kidSafe[0].GetProperty("rating").GetString());
    }

    [Fact]
    public async Task TagFilter_FindetDarstellungenUeberDieGeteilteTaxonomie()
    {
        var father = await TestApi.AdultAsync(factory);
        var marker = "tagfilter-motiv";

        await father.PostAsJsonAsync("/api/v1/creator/media", new
        {
            description = $"{marker} eins",
            tags = new[] { "Pokémon", "Comic" },
        });
        await father.PostAsJsonAsync("/api/v1/creator/media", new
        {
            description = $"{marker} zwei",
            tags = new[] { "Comic" },
        });

        // Diacritics/capitalization are normalized onto the same slug.
        var byFranchise = await ListAsync(father, $"/api/v1/creator/media?search={marker}&tag=pokemon");
        Assert.Equal(1, byFranchise.GetArrayLength());

        var byStyle = await ListAsync(father, $"/api/v1/creator/media?search={marker}&tag=comic");
        Assert.Equal(2, byStyle.GetArrayLength());

        // matchAll = AND over both axes (topic + style).
        var both = await ListAsync(father, $"/api/v1/creator/media?search={marker}&tag=pokemon&tag=comic&matchAll=true");
        Assert.Equal(1, both.GetArrayLength());
    }

    [Fact]
    public async Task TagLoesen_LaesstDasSchlagwortImKatalog()
    {
        var father = await TestApi.AdultAsync(factory);
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/media",
            new { description = "Ein Fussballspieler schiesst", tags = new[] { "Fußball" } }));

        var tagId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/interest-tags",
            new { label = "Fußball" }));

        Assert.Equal(HttpStatusCode.NoContent,
            (await father.DeleteAsync($"/api/v1/creator/media/{id}/tags/{tagId}")).StatusCode);

        var asset = await GetAsync(father, $"/api/v1/creator/media/{id}");
        Assert.Empty(asset.GetProperty("tags").EnumerateArray());

        // The tag itself survives - it may hang on other images and on child profiles.
        Assert.Equal(HttpStatusCode.OK, (await father.GetAsync($"/api/v1/creator/interest-tags/{tagId}")).StatusCode);
    }

    [Fact]
    public async Task NurCreator_DarfDenStorePflegen()
    {
        var child = await TestApi.ChildAsync(factory);
        var res = await child.GetAsync("/api/v1/creator/media");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    private static async Task<JsonElement> ListAsync(HttpClient client, string url) => await GetAsync(client, url);

    private static async Task<JsonElement> GetAsync(HttpClient client, string url)
    {
        var res = await client.GetAsync(url);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<string?> CodeOf(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
}
