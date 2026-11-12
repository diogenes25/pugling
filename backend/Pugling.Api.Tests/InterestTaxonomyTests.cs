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
        var father = await TestApi.AdultAsync(factory);

        var first = await father.PostAsJsonAsync("/api/v1/creator/interest-tags",
            new { label = "Rocket League", facet = "Franchise" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var created = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("rocket-league", created.GetProperty("slug").GetString());

        // A second call: 200 instead of 409 - an agent may repeat the same catalog setup.
        var again = await father.PostAsJsonAsync("/api/v1/creator/interest-tags", new { label = "Rocket League" });
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(created.GetProperty("id").GetInt32(), await TestApi.IdAsync(again));
    }

    [Fact]
    public async Task Diakritika_TreffenDenselbenTag()
    {
        var father = await TestApi.AdultAsync(factory);

        var withAccent = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/interest-tags",
            new { label = "Pokémon", facet = "Franchise" }));
        var plain = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/interest-tags",
            new { label = "pokemon" }));

        Assert.Equal(withAccent, plain);
    }

    [Fact]
    public async Task Synonym_VerhindertEineDublette()
    {
        var father = await TestApi.AdultAsync(factory);
        var canonical = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/interest-tags", new
        {
            label = "Lego Technic",
            facet = "Hobby",
            synonyms = new[] { "Lego-Technik", "Technikbaukasten" },
        }));

        // The supervisor types a synonym - it must not create a second tag.
        await SetInterestsAsync(father, new object[] { new { label = "Lego-Technik", weight = 2 } });

        var interests = await GetAsync(father, "/api/v1/supervisor/children/1/interests");
        Assert.Equal(canonical, interests[0].GetProperty("tagId").GetInt32());
        Assert.Equal("lego-technic", interests[0].GetProperty("slug").GetString());
    }

    [Fact]
    public async Task KindInteressen_TreffenDieselbenTagsWieBildSchlagworte()
    {
        var father = await TestApi.AdultAsync(factory);

        // The creator tags an image …
        await father.PostAsJsonAsync("/api/v1/creator/media", new
        {
            description = "Ein Dinosaurier rennt",
            tags = new[] { "Dinosaurier" },
        });

        // … the supervisor maintains the same interest on the child, in a different spelling.
        await SetInterestsAsync(father, new object[] { new { label = "dinosaurier", weight = 3 } });

        var tags = await GetAsync(father, "/api/v1/creator/interest-tags?search=dinosaurier");
        Assert.Equal(1, tags.GetArrayLength());
        // Both sides hang on the same tag - that is exactly what makes the selection computable later.
        Assert.Equal(1, tags[0].GetProperty("mediaCount").GetInt32());
        Assert.Equal(1, tags[0].GetProperty("childCount").GetInt32());
    }

    /// <summary>
    /// Aus dem Review zu B-46. Die Taxonomie hat seit dieser Story <b>drei</b> Referenzierer, und die
    /// Aufräum-Sicht kannte zwei. Ein Stichwort, das nur eine Übung einkleidet, sah damit „unbenutzt" aus —
    /// und das Löschen ist bewusst ohne Riegel, die Einkleidung wäre also lautlos per Cascade weg.
    /// <para>
    /// Rote Probe: <c>ExerciseLinks.Count == 0</c> aus dem <c>unused</c>-Filter nehmen ⇒ die zweite
    /// Zusicherung fällt; <c>ExerciseCount</c> aus der Projektion nehmen ⇒ die erste.
    /// </para>
    /// </summary>
    [Fact]
    public async Task EineUebungAlsEinzigerNutzer_MachtDasStichwortNichtUnbenutzt()
    {
        var father = await TestApi.AdultAsync(factory);
        var slug = TestApi.UniqueName("einhorn").ToLowerInvariant();

        var tag = await father.PostAsJsonAsync("/api/v1/creator/interest-tags", new { label = slug });
        var tagId = await TestApi.IdAsync(tag);

        // Weder Bild noch Kind zeigen darauf - nur die Übung. Genau der Fall, den die alte Fassung übersah.
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Einkleidung-Fach") }));
        var (seriesId, seriesUnitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var angelegt = await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/arithmetic",
            new
            {
                title = TestApi.UniqueName("Eingekleidet"),
                orderIndex = 1,
                rewardPoints = 10,
                config = new { problems = new[] { new { prompt = "1 + 1", answer = 2, tolerance = 0 } } },
                interestTagIds = new[] { tagId },
            });
        Assert.Equal(HttpStatusCode.Created, angelegt.StatusCode);

        var gefunden = await GetAsync(father, $"/api/v1/creator/interest-tags?search={slug}");
        Assert.Equal(1, gefunden.GetArrayLength());
        Assert.Equal(0, gefunden[0].GetProperty("mediaCount").GetInt32());
        Assert.Equal(0, gefunden[0].GetProperty("childCount").GetInt32());
        Assert.Equal(1, gefunden[0].GetProperty("exerciseCount").GetInt32());

        // Und die Aufräum-Sicht bietet es nicht mehr zum Wegwerfen an.
        var unbenutzt = await GetAsync(father, $"/api/v1/creator/interest-tags?unused=true&search={slug}");
        Assert.Equal(0, unbenutzt.GetArrayLength());
    }

    [Fact]
    public async Task NegativesGewicht_BildetEineAbneigungAb()
    {
        var father = await TestApi.AdultAsync(factory);
        await SetInterestsAsync(father, new object[]
        {
            new { label = "Weltraum", weight = 3 },
            new { label = "Spinnen", weight = -3 },
        });

        var interests = await GetAsync(father, "/api/v1/supervisor/children/1/interests");
        // Sorting: the strongest preference first, the dislike last.
        Assert.Equal("weltraum", interests[0].GetProperty("slug").GetString());
        Assert.Equal(3, interests[0].GetProperty("weight").GetInt32());
        var last = interests[interests.GetArrayLength() - 1];
        Assert.Equal("spinnen", last.GetProperty("slug").GetString());
        Assert.Equal(-3, last.GetProperty("weight").GetInt32());
    }

    [Fact]
    public async Task GewichtAusserhalbDerSkala_Liefert400()
    {
        var father = await TestApi.AdultAsync(factory);
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
        var father = await TestApi.AdultAsync(factory);
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
        var father = await TestApi.AdultAsync(factory);
        await SetInterestsAsync(father, new object[] { new { label = "Schach", weight = 1 } });

        await SetInterestsAsync(father, []);

        Assert.Empty((await GetAsync(father, "/api/v1/supervisor/children/1/interests")).EnumerateArray());
    }

    [Fact]
    public async Task AllowedContentRating_IstDefaultStrengUndNurVomSupervisorHebbar()
    {
        var father = await TestApi.AdultAsync(factory);
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
        // The demo adult from the seed (created after the father and the teacher, hence id 3).
        var demoSupervisor = await TestApi.AdultAsync(factory, id: 3, pin: "0001");
        var res = await demoSupervisor.GetAsync("/api/v1/supervisor/children/1/interests");
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
        // The demo adult from the seed (created after the father and the teacher, hence id 3).
        var demoSupervisor = await TestApi.AdultAsync(factory, id: 3, pin: "0001");
        var children = await GetAsync(demoSupervisor, "/api/v1/supervisor/children");
        var demoChildId = children.EnumerateArray()
            .First(c => c.GetProperty("name").GetString() == "Demo-Kind").GetProperty("id").GetInt32();

        var interests = await GetAsync(demoSupervisor, $"/api/v1/supervisor/children/{demoChildId}/interests");
        var slugs = interests.EnumerateArray().Select(i => i.GetProperty("slug").GetString()).ToList();

        Assert.Contains("minecraft", slugs);
        Assert.Contains("basketball", slugs);
        // A clear but not dominant preference - the supervisor should adjust it, not create it in the first place.
        Assert.All(interests.EnumerateArray(), i => Assert.Equal(2, i.GetProperty("weight").GetInt32()));

        // The free text is preserved: the AI creator lives on it.
        var child = await GetAsync(demoSupervisor, $"/api/v1/supervisor/children/{demoChildId}");
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
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Slug-Kollision", pin = "4711" }));

        // Both spellings fall onto strassenhockey (ß → ss) - and the tag is really new here (unlike "Fußball",
        // which the seed already created through the backfill).
        await SetInterestsAsync(father, [
            new { label = "Straßenhockey", weight = 3 },
            new { label = "Strassenhockey", weight = 1 },
        ], childId);

        var interests = await GetAsync(father, $"/api/v1/supervisor/children/{childId}/interests");
        Assert.Equal(1, interests.GetArrayLength());
        Assert.Equal("strassenhockey", interests[0].GetProperty("slug").GetString());
        // Duplicates in the input behave like an assignment: the last entry wins.
        Assert.Equal(1, interests[0].GetProperty("weight").GetInt32());

        // And it really is one tag in the catalog, not a second row beside it.
        var tags = await GetAsync(father, "/api/v1/creator/interest-tags?search=strassenhockey");
        Assert.Equal(1, tags.GetArrayLength());
    }

    // ─────────────────────────────── Setting a single weight, deleting an interest and a tag (C3 gap)

    [Fact]
    public async Task Gewicht_Einzeln_Setzen_Und_Interesse_Wieder_Entfernen()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Interessen-Kind", pin = "6401" }));
        var tagId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/interest-tags",
            new { label = $"Skaten-{Guid.NewGuid():N}"[..14] }));
        var url = $"/api/v1/supervisor/children/{childId}/interests";

        // A weight outside -3…3 is the route's error case.
        Assert.Equal(HttpStatusCode.BadRequest,
            (await father.PutAsJsonAsync($"{url}/{tagId}", new { weight = 4 })).StatusCode);

        // Upsert: the single weight creates the assignment if it does not exist yet.
        var gesetzt = await father.PutAsJsonAsync($"{url}/{tagId}", new { weight = 3 });
        gesetzt.EnsureSuccessStatusCode();
        Assert.Equal(3, (await gesetzt.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("weight").GetInt32());

        // And the same call changes it instead of creating a second row.
        (await father.PutAsJsonAsync($"{url}/{tagId}", new { weight = 2 })).EnsureSuccessStatusCode();
        var liste = await GetAsync(father, url);
        Assert.Equal(1, liste.GetArrayLength());
        Assert.Equal(2, liste[0].GetProperty("weight").GetInt32());

        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync($"{url}/{tagId}")).StatusCode);
        Assert.Empty((await GetAsync(father, url)).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await father.DeleteAsync($"{url}/{tagId}")).StatusCode);
    }

    /// <summary>
    /// B-206. A tag that dresses an exercise carries <b>authored information</b> that nothing else records -
    /// a cascade would erase it silently and unrecoverably. The lock is therefore on the exercises only, and
    /// it has a door: take the dressing off, then the delete goes through.
    /// <para>
    /// Red probe: drop the <c>ExerciseInterestLinks</c> pre-check in <c>Delete</c> ⇒ the first assertion
    /// falls (204 instead of 409).
    /// </para>
    /// </summary>
    [Fact]
    public async Task EinStichwortDasEineUebungEinkleidet_LaesstSichNichtLoeschen()
    {
        var father = await TestApi.AdultAsync(factory);
        var tagId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/interest-tags",
            new { label = TestApi.UniqueName("Rennwagen") }));

        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Riegel-Fach") }));
        var (seriesId, seriesUnitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/arithmetic",
            new
            {
                title = TestApi.UniqueName("Eingekleidet"),
                orderIndex = 1,
                rewardPoints = 10,
                config = new { problems = new[] { new { prompt = "2 + 2", answer = 4, tolerance = 0 } } },
                interestTagIds = new[] { tagId },
            }));

        var gesperrt = await father.DeleteAsync($"/api/v1/creator/interest-tags/{tagId}");
        Assert.Equal(HttpStatusCode.Conflict, gesperrt.StatusCode);
        Assert.Equal("interest_tag_in_use",
            (await gesperrt.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // And the tag is still there afterwards - a rejected delete must not have half-finished anything.
        Assert.Equal(HttpStatusCode.OK, (await father.GetAsync($"/api/v1/creator/interest-tags/{tagId}")).StatusCode);

        // The door: take the dressing off, and the same DELETE goes through.
        var detail = await GetAsync(father,
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/arithmetic/{exerciseId}");
        (await father.PutAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/arithmetic/{exerciseId}",
            new
            {
                title = detail.GetProperty("title").GetString(),
                orderIndex = detail.GetProperty("orderIndex").GetInt32(),
                rewardPoints = detail.GetProperty("rewardPoints").GetInt32(),
                config = detail.GetProperty("config"),
                interestTagIds = Array.Empty<int>(),
            })).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.NoContent,
            (await father.DeleteAsync($"/api/v1/creator/interest-tags/{tagId}")).StatusCode);
    }

    /// <summary>
    /// The counterpart to the lock: image and child links do <b>not</b> block. Their loss is recoverable -
    /// and a broader lock would make the tag catalog effectively append-only, because nearly every tag
    /// carries an image or a child.
    /// <para>Red probe: widen the lock to <c>MediaLinks</c> ⇒ this assertion falls.</para>
    /// </summary>
    [Fact]
    public async Task BildUndKindVerknuepfungen_SperrenDasLoeschenNicht()
    {
        var father = await TestApi.AdultAsync(factory);
        var label = TestApi.UniqueName("Seifenkiste");

        await father.PostAsJsonAsync("/api/v1/creator/media", new
        {
            description = $"Eine {label} rollt",
            tags = new[] { label },
        });
        await SetInterestsAsync(father, new object[] { new { label, weight = 2 } });

        var tags = await GetAsync(father, $"/api/v1/creator/interest-tags?search={Uri.EscapeDataString(label)}");
        var tagId = tags[0].GetProperty("id").GetInt32();
        Assert.True(tags[0].GetProperty("mediaCount").GetInt32() > 0);
        Assert.True(tags[0].GetProperty("childCount").GetInt32() > 0);

        Assert.Equal(HttpStatusCode.NoContent,
            (await father.DeleteAsync($"/api/v1/creator/interest-tags/{tagId}")).StatusCode);
    }

    [Fact]
    public async Task Interessen_Tag_Laesst_Sich_Aus_Dem_Katalog_Loeschen()
    {
        var father = await TestApi.AdultAsync(factory);
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

    /// <summary>
    /// The image prompt (B-485) is what the generator sends instead of the label, so a brand-named tag never reaches
    /// it by name. Turns red if the field is not stored, not trimmed, if a blank value is kept as an empty prompt
    /// (the agent would then illustrate the tag with nothing), or if an over-long prompt is silently accepted.
    /// </summary>
    [Fact]
    public async Task ImagePrompt_WirdGespeichertGetrimmtUndBegrenzt()
    {
        var father = await TestApi.AdultAsync(factory);

        var created = await father.PostAsJsonAsync("/api/v1/creator/interest-tags",
            new { label = "Bildprompt-Tag", imagePrompt = "  on a soccer pitch  " });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("on a soccer pitch", body.GetProperty("imagePrompt").GetString());
        var id = body.GetProperty("id").GetInt32();

        var blank = await father.PatchAsJsonAsync($"/api/v1/creator/interest-tags/{id}", new { imagePrompt = "   " });
        Assert.Equal(HttpStatusCode.OK, blank.StatusCode);
        Assert.Equal(JsonValueKind.Null,
            (await blank.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("imagePrompt").ValueKind);

        var tooLong = await father.PatchAsJsonAsync($"/api/v1/creator/interest-tags/{id}",
            new { imagePrompt = new string('x', 201) });
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        Assert.Equal("validation_error", (await tooLong.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }
}
