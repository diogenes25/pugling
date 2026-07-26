using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Interessen-Taxonomie (Etappe 2). Der Kern, den diese Tests absichern: Bild-Tags und Kind-Interessen
/// schöpfen aus <b>einem</b> Vokabular. Fiele das auseinander, liefe die spätere Bildauswahl ins Leere –
/// deshalb prüfen die Tests vor allem, dass verschiedene Schreibweisen denselben Tag treffen.
/// </summary>
public class InterestTaxonomyTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Anlegen_IstIdempotent_UndLeitetDenSlugAusDemLabelAb()
    {
        var father = await TestApi.FatherAsync(factory);

        var first = await father.PostAsJsonAsync("/api/v1/creator/interest-tags",
            new { label = "Rocket League", facet = "Franchise" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var created = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("rocket-league", created.GetProperty("slug").GetString());

        // Zweiter Aufruf: 200 statt 409 – ein Agent darf denselben Katalog-Aufbau wiederholen.
        var again = await father.PostAsJsonAsync("/api/v1/creator/interest-tags", new { label = "Rocket League" });
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(created.GetProperty("id").GetInt32(), await TestApi.IdAsync(again));
    }

    [Fact]
    public async Task Diakritika_TreffenDenselbenTag()
    {
        var father = await TestApi.FatherAsync(factory);

        var withAccent = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/interest-tags",
            new { label = "Pokémon", facet = "Franchise" }));
        var plain = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/interest-tags",
            new { label = "pokemon" }));

        Assert.Equal(withAccent, plain);
    }

    [Fact]
    public async Task Synonym_VerhindertEineDublette()
    {
        var father = await TestApi.FatherAsync(factory);
        var canonical = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/interest-tags", new
        {
            label = "Lego Technic",
            facet = "Hobby",
            synonyms = new[] { "Lego-Technik", "Technikbaukasten" },
        }));

        // Der Supervisor tippt ein Synonym – es darf keinen zweiten Tag erzeugen.
        await SetInterestsAsync(father, new object[] { new { label = "Lego-Technik", weight = 2 } });

        var interests = await GetAsync(father, "/api/v1/supervisor/children/1/interests");
        Assert.Equal(canonical, interests[0].GetProperty("tagId").GetInt32());
        Assert.Equal("lego-technic", interests[0].GetProperty("slug").GetString());
    }

    [Fact]
    public async Task KindInteressen_TreffenDieselbenTagsWieBildSchlagworte()
    {
        var father = await TestApi.FatherAsync(factory);

        // Creator taggt ein Bild …
        await father.PostAsJsonAsync("/api/v1/creator/media", new
        {
            description = "Ein Dinosaurier rennt",
            tags = new[] { "Dinosaurier" },
        });

        // … der Supervisor pflegt dasselbe Interesse am Kind, in anderer Schreibweise.
        await SetInterestsAsync(father, new object[] { new { label = "dinosaurier", weight = 3 } });

        var tags = await GetAsync(father, "/api/v1/creator/interest-tags?search=dinosaurier");
        Assert.Equal(1, tags.GetArrayLength());
        // Beide Seiten hängen am selben Tag – genau das macht die Auswahl später berechenbar.
        Assert.Equal(1, tags[0].GetProperty("mediaCount").GetInt32());
        Assert.Equal(1, tags[0].GetProperty("childCount").GetInt32());
    }

    [Fact]
    public async Task NegativesGewicht_BildetEineAbneigungAb()
    {
        var father = await TestApi.FatherAsync(factory);
        await SetInterestsAsync(father, new object[]
        {
            new { label = "Weltraum", weight = 3 },
            new { label = "Spinnen", weight = -3 },
        });

        var interests = await GetAsync(father, "/api/v1/supervisor/children/1/interests");
        // Sortierung: stärkste Vorliebe zuerst, Abneigung zuletzt.
        Assert.Equal("weltraum", interests[0].GetProperty("slug").GetString());
        Assert.Equal(3, interests[0].GetProperty("weight").GetInt32());
        var last = interests[interests.GetArrayLength() - 1];
        Assert.Equal("spinnen", last.GetProperty("slug").GetString());
        Assert.Equal(-3, last.GetProperty("weight").GetInt32());
    }

    [Fact]
    public async Task GewichtAusserhalbDerSkala_Liefert400()
    {
        var father = await TestApi.FatherAsync(factory);
        var res = await father.PutAsJsonAsync("/api/v1/supervisor/children/1/interests", new
        {
            interests = new object[] { new { label = "Angeln", weight = 9 } },
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("validation_error", (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Put_ErsetztDieMengeVollstaendig()
    {
        var father = await TestApi.FatherAsync(factory);
        await SetInterestsAsync(father, new object[]
        {
            new { label = "Segeln", weight = 2 },
            new { label = "Klettern", weight = 1 },
        });

        await SetInterestsAsync(father, new object[] { new { label = "Klettern", weight = 3 } });

        var interests = await GetAsync(father, "/api/v1/supervisor/children/1/interests");
        Assert.Equal(1, interests.GetArrayLength());
        Assert.Equal("klettern", interests[0].GetProperty("slug").GetString());
        Assert.Equal(3, interests[0].GetProperty("weight").GetInt32());
    }

    [Fact]
    public async Task LeeresPut_EntferntAlleInteressen()
    {
        var father = await TestApi.FatherAsync(factory);
        await SetInterestsAsync(father, new object[] { new { label = "Schach", weight = 1 } });

        await SetInterestsAsync(father, []);

        Assert.Empty((await GetAsync(father, "/api/v1/supervisor/children/1/interests")).EnumerateArray());
    }

    [Fact]
    public async Task AllowedContentRating_IstDefaultStrengUndNurVomSupervisorHebbar()
    {
        var father = await TestApi.FatherAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Rating-Kind", pin = "4321" }));

        var created = await GetAsync(father, $"/api/v1/supervisor/children/{childId}");
        Assert.Equal("Everyone", created.GetProperty("allowedContentRating").GetString());

        var patch = await father.PatchAsJsonAsync($"/api/v1/supervisor/children/{childId}",
            new { allowedContentRating = "Teen" });
        patch.EnsureSuccessStatusCode();
        Assert.Equal("Teen", (await patch.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("allowedContentRating").GetString());
    }

    [Fact]
    public async Task FremdesKind_BleibtVerschlossen()
    {
        // Demo-Vater aus dem Seed (angelegt nach Papa und dem Lehrer, daher Id 3).
        var demoFather = await TestApi.FatherAsync(factory, id: 3, pin: "0001");
        var res = await demoFather.GetAsync("/api/v1/supervisor/children/1/interests");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    /// <summary>
    /// Der Backfill überführt die vorhandenen Freitext-Interessen der Bestandskinder beim Start in die
    /// Taxonomie – ohne ihn stünde die Bildauswahl für jedes bereits gepflegte Kind bei null. Geprüft am
    /// geseedeten Demo-Kind („Minecraft", „Basketball"), das keiner der übrigen Tests anfasst.
    /// </summary>
    [Fact]
    public async Task Backfill_UebernimmtFreitextInteressenDerBestandskinder()
    {
        // Demo-Vater aus dem Seed (angelegt nach Papa und dem Lehrer, daher Id 3).
        var demoFather = await TestApi.FatherAsync(factory, id: 3, pin: "0001");
        var children = await GetAsync(demoFather, "/api/v1/supervisor/children");
        var demoChildId = children.EnumerateArray()
            .First(c => c.GetProperty("name").GetString() == "Demo-Kind").GetProperty("id").GetInt32();

        var interests = await GetAsync(demoFather, $"/api/v1/supervisor/children/{demoChildId}/interests");
        var slugs = interests.EnumerateArray().Select(i => i.GetProperty("slug").GetString()).ToList();

        Assert.Contains("minecraft", slugs);
        Assert.Contains("basketball", slugs);
        // Klare, aber nicht dominante Vorliebe – der Vater soll nachjustieren, nicht erst anlegen.
        Assert.All(interests.EnumerateArray(), i => Assert.Equal(2, i.GetProperty("weight").GetInt32()));

        // Der Freitext bleibt erhalten: der KI-Creator lebt davon.
        var child = await GetAsync(demoFather, $"/api/v1/supervisor/children/{demoChildId}");
        Assert.Contains("Minecraft", child.GetProperty("interests").EnumerateArray().Select(i => i.GetString()));
    }

    private static async Task SetInterestsAsync(HttpClient father, object[] interests, int childId = 1)
    {
        var res = await father.PutAsJsonAsync($"/api/v1/supervisor/children/{childId}/interests", new { interests });
        res.EnsureSuccessStatusCode();
    }

    private static async Task<JsonElement> GetAsync(HttpClient client, string url)
    {
        var res = await client.GetAsync(url);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }
}
