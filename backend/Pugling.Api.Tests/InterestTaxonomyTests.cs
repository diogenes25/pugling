using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Interest taxonomy (stage 2). The core these tests secure: image tags and child interests draw from
/// <b>one</b> vocabulary. If this fell apart, the later image selection would run into a void – that's
/// why the tests mainly check that different spellings hit the same tag.
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
    /// The backfill migrates the existing children's free-text interests into the taxonomy at startup –
    /// without it, the image selection would start at zero for every already-maintained child. Verified
    /// against the seeded demo child ("Minecraft", "Basketball"), which none of the other tests touch.
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

    /// <summary>
    /// Two spellings of the same slug in <b>one</b> call. The indexed slug lookup alone is not enough
    /// for this: the tag just created still hangs unsaved in the ChangeTracker and is invisible to any
    /// query – both inputs would each create a row and saving would violate the unique index. The same
    /// hit the <c>InterestTagBackfill</c> at startup, for every existing child with two spellings in the
    /// free text.
    /// </summary>
    [Fact]
    public async Task ZweiSchreibweisenInEinemAufruf_TreffenDenselbenTag()
    {
        var father = await TestApi.FatherAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Slug-Kollision", pin = "4711" }));

        // Beide Schreibweisen fallen auf strassenhockey (ß → ss) – und der Tag ist hier wirklich neu
        // (nicht wie „Fußball" schon vom Seed über den Backfill angelegt).
        await SetInterestsAsync(father, [
            new { label = "Straßenhockey", weight = 3 },
            new { label = "Strassenhockey", weight = 1 },
        ], childId);

        var interests = await GetAsync(father, $"/api/v1/supervisor/children/{childId}/interests");
        Assert.Equal(1, interests.GetArrayLength());
        Assert.Equal("strassenhockey", interests[0].GetProperty("slug").GetString());
        // Dubletten in der Eingabe verhalten sich wie eine Zuweisung: der letzte Eintrag gewinnt.
        Assert.Equal(1, interests[0].GetProperty("weight").GetInt32());

        // Und es ist wirklich ein Tag im Katalog, keine zweite Zeile daneben.
        var tags = await GetAsync(father, "/api/v1/creator/interest-tags?search=strassenhockey");
        Assert.Equal(1, tags.GetArrayLength());
    }

    // ─────────────────────────────── Einzelnes Gewicht setzen, Interesse und Tag löschen (C3-Lücke)

    [Fact]
    public async Task Gewicht_Einzeln_Setzen_Und_Interesse_Wieder_Entfernen()
    {
        var father = await TestApi.FatherAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Interessen-Kind", pin = "6401" }));
        var tagId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/interest-tags",
            new { label = $"Skaten-{Guid.NewGuid():N}"[..14] }));
        var url = $"/api/v1/supervisor/children/{childId}/interests";

        // Ein Gewicht außerhalb -3…3 ist der Fehlerfall der Route.
        Assert.Equal(HttpStatusCode.BadRequest,
            (await father.PutAsJsonAsync($"{url}/{tagId}", new { weight = 4 })).StatusCode);

        // Upsert: das einzelne Gewicht legt die Zuordnung an, wenn es sie noch nicht gibt.
        var gesetzt = await father.PutAsJsonAsync($"{url}/{tagId}", new { weight = 3 });
        gesetzt.EnsureSuccessStatusCode();
        Assert.Equal(3, (await gesetzt.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("weight").GetInt32());

        // Und derselbe Aufruf ändert es, statt eine zweite Zeile anzulegen.
        (await father.PutAsJsonAsync($"{url}/{tagId}", new { weight = 2 })).EnsureSuccessStatusCode();
        var liste = await GetAsync(father, url);
        Assert.Equal(1, liste.GetArrayLength());
        Assert.Equal(2, liste[0].GetProperty("weight").GetInt32());

        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync($"{url}/{tagId}")).StatusCode);
        Assert.Empty((await GetAsync(father, url)).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await father.DeleteAsync($"{url}/{tagId}")).StatusCode);
    }

    [Fact]
    public async Task Interessen_Tag_Laesst_Sich_Aus_Dem_Katalog_Loeschen()
    {
        var father = await TestApi.FatherAsync(factory);
        var tagId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/interest-tags",
            new { label = $"Einhorn-{Guid.NewGuid():N}"[..16] }));

        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync($"/api/v1/creator/interest-tags/{tagId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await father.GetAsync($"/api/v1/creator/interest-tags/{tagId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await father.DeleteAsync($"/api/v1/creator/interest-tags/{tagId}")).StatusCode);
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
