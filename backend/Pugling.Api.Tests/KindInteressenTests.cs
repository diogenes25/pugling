using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// The child describes itself (B-50). Two boundaries carry this feature, and both are here:
/// the child may <b>pick from</b> the taxonomy but never extend it, and a changed profile must leave an
/// already frozen image choice alone – image constancy is the memory effect the freeze was built for.
/// </summary>
public class KindInteressenTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>PIN of the children created here – these tests call the student endpoints themselves.</summary>
    private const string ChildPin = "5150";

    [Fact]
    public async Task DasKindSetztSeineInteressenSelbst_UndLiestSieZurueck()
    {
        var father = await TestApi.AdultAsync(factory);
        var (childId, child) = await ChildAsync(father, "selbst-setzen");
        var einhorn = await TagAsync(father, "Einhorn", "Animal");
        var comic = await TagAsync(father, "Comic", "Style");

        var put = await child.PutAsJsonAsync("/api/v1/student/me/interests", new
        {
            interests = new[] { new { tagId = einhorn, weight = 3 }, new { tagId = comic, weight = -1 } },
        });
        put.EnsureSuccessStatusCode();

        var profile = await JsonAsync(child, "/api/v1/student/me/interests");
        var interests = profile.GetProperty("interests");
        Assert.Equal(2, interests.GetArrayLength());
        // Strongest preference first, the dislike last - the order the supervisor sees as well.
        Assert.Equal(einhorn, interests[0].GetProperty("tagId").GetInt32());
        Assert.Equal(3, interests[0].GetProperty("weight").GetInt32());
        Assert.Equal(comic, interests[1].GetProperty("tagId").GetInt32());
        Assert.Equal(-1, interests[1].GetProperty("weight").GetInt32());
    }

    /// <summary>
    /// The supervisor keeps seeing what the child set, at the place they already use – that is what makes
    /// the direct write way (no approval queue) safe.
    /// </summary>
    [Fact]
    public async Task WasDasKindSetzt_SiehtDerSupervisorAnDerGewohntenStelle()
    {
        var father = await TestApi.AdultAsync(factory);
        var (childId, child) = await ChildAsync(father, "vater-sieht-es");
        var tagId = await TagAsync(father, "Skateboard", "Sport");

        (await child.PutAsJsonAsync("/api/v1/student/me/interests", new
        {
            interests = new[] { new { tagId, weight = 2 } },
        })).EnsureSuccessStatusCode();

        var seenByFather = await JsonAsync(father, $"/api/v1/supervisor/children/{childId}/interests");
        Assert.Equal(1, seenByFather.GetArrayLength());
        Assert.Equal(tagId, seenByFather[0].GetProperty("tagId").GetInt32());
        Assert.Equal(2, seenByFather[0].GetProperty("weight").GetInt32());
    }

    /// <summary>
    /// The core boundary of the story: a child must not grow the taxonomy. An invented keyword would match
    /// no image at all, and the shared vocabulary is exactly what makes the selection computable.
    /// <para>
    /// Checked at the contract, not at a tag count: the count is global and other test classes write to the
    /// same taxonomy in parallel, so it could only ever be measured racily.
    /// </para>
    /// <para>
    /// Red probe: add a <c>Label</c> to <c>MyInterestInput</c> and resolve it through
    /// <c>InterestTagService.EnsureAsync</c> as the supervisor does ⇒ the first assertion falls; replace
    /// the reference check with that same <c>EnsureAsync</c> ⇒ the second and third fall.
    /// </para>
    /// </summary>
    [Fact]
    public async Task EinUnbekanntesSchlagwort_WirdAbgelehnt_StattStillAngelegtZuWerden()
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, child) = await ChildAsync(father, "keine-taxonomie");
        var known = await TagAsync(father, "Dinosaurier", "Animal");

        (await child.PutAsJsonAsync("/api/v1/student/me/interests", new
        {
            interests = new[] { new { tagId = known, weight = 2 } },
        })).EnsureSuccessStatusCode();

        // The child's contract offers no text path at all - naming a tag is how the supervisor creates one.
        var byLabel = await child.PutAsJsonAsync("/api/v1/student/me/interests", new
        {
            interests = new[] { new { label = "Erfundenes Stichwort", facet = "Hobby", weight = 3 } },
        });
        Assert.Equal(HttpStatusCode.BadRequest, byLabel.StatusCode);
        Assert.Equal("unknown_field", await CodeOf(byLabel));

        // And an id nobody issued is a rejected reference, not an invitation to create one.
        var byUnknownId = await child.PutAsJsonAsync("/api/v1/student/me/interests", new
        {
            interests = new[] { new { tagId = 999_999, weight = 3 } },
        });
        Assert.Equal(HttpStatusCode.BadRequest, byUnknownId.StatusCode);
        Assert.Equal("invalid_reference", await CodeOf(byUnknownId));

        // A rejected request leaves the existing set untouched - it is resolved before anything is removed.
        var profile = await JsonAsync(child, "/api/v1/student/me/interests");
        Assert.Equal(1, profile.GetProperty("interests").GetArrayLength());
        Assert.Equal(known, profile.GetProperty("interests")[0].GetProperty("tagId").GetInt32());
    }

    [Fact]
    public async Task GewichtAusserhalbDerSkala_Liefert400()
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, child) = await ChildAsync(father, "gewicht-skala");
        var tagId = await TagAsync(father, "Angeln", "Hobby");

        // Both ends, and both accepted edges: a check shortened to "> Max" would stay green on the upper
        // bound alone - that is the "rule tested, edge case open" class from docs/testplan.md.
        foreach (var weight in new[] { 9, -4 })
        {
            var res = await child.PutAsJsonAsync("/api/v1/student/me/interests", new
            {
                interests = new[] { new { tagId, weight } },
            });
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            Assert.Equal("validation_error", await CodeOf(res));
        }

        foreach (var weight in new[] { 3, -3 })
            (await child.PutAsJsonAsync("/api/v1/student/me/interests", new
            {
                interests = new[] { new { tagId, weight } },
            })).EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Two entries for the same tag in one payload would violate the unique index
    /// <c>(ChildId, InterestTagId)</c> and answer 500. The last one wins, as in an assignment.
    /// <para>Red probe: drop the <c>GroupBy</c> in <c>Replace</c> ⇒ this test turns 500.</para>
    /// </summary>
    [Fact]
    public async Task DerselbeTagZweimal_GewinntMitDemLetztenEintrag_StattDenIndexZuVerletzen()
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, child) = await ChildAsync(father, "doppelter-tag");
        var tagId = await TagAsync(father, "Segeln", "Hobby");

        (await child.PutAsJsonAsync("/api/v1/student/me/interests", new
        {
            interests = new[] { new { tagId, weight = 1 }, new { tagId, weight = -2 } },
        })).EnsureSuccessStatusCode();

        var profile = await JsonAsync(child, "/api/v1/student/me/interests");
        Assert.Equal(1, profile.GetProperty("interests").GetArrayLength());
        Assert.Equal(-2, profile.GetProperty("interests")[0].GetProperty("weight").GetInt32());
    }

    [Fact]
    public async Task LeeresPut_EntferntAlleInteressen()
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, child) = await ChildAsync(father, "leeres-put");
        var tagId = await TagAsync(father, "Schach", "Hobby");

        (await child.PutAsJsonAsync("/api/v1/student/me/interests", new
        {
            interests = new[] { new { tagId, weight = 1 } },
        })).EnsureSuccessStatusCode();

        (await child.PutAsJsonAsync("/api/v1/student/me/interests", new { interests = Array.Empty<object>() }))
            .EnsureSuccessStatusCode();

        var profile = await JsonAsync(child, "/api/v1/student/me/interests");
        Assert.Equal(0, profile.GetProperty("interests").GetArrayLength());
    }

    /// <summary>
    /// The guided process is offered on the first visit and never again by itself. Deriving that from an
    /// empty profile is what keeps this story free of a schema change.
    /// </summary>
    [Fact]
    public async Task NeedsIntro_GiltNurSolangeDasProfilLeerIst()
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, child) = await ChildAsync(father, "erstbesuch");
        var tagId = await TagAsync(father, "Weltraum", "Nature");

        Assert.True((await JsonAsync(child, "/api/v1/student/me/interests")).GetProperty("needsIntro").GetBoolean());

        (await child.PutAsJsonAsync("/api/v1/student/me/interests", new
        {
            interests = new[] { new { tagId, weight = 2 } },
        })).EnsureSuccessStatusCode();

        Assert.False((await JsonAsync(child, "/api/v1/student/me/interests")).GetProperty("needsIntro").GetBoolean());
    }

    [Fact]
    public async Task EinFreitextWunsch_LandetSichtbarBeimSupervisor()
    {
        var father = await TestApi.AdultAsync(factory);
        var (childId, child) = await ChildAsync(father, "freitext-wunsch");

        var res = await child.PostAsJsonAsync("/api/v1/student/me/interests/wishes", new { text = "Bassgitarre" });
        res.EnsureSuccessStatusCode();
        var profile = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Bassgitarre", profile.GetProperty("wishes")[0].GetString());

        // The same field the supervisor already edits on the child profile - no separate channel.
        var seenByFather = await JsonAsync(father, $"/api/v1/supervisor/children/{childId}");
        Assert.Contains("Bassgitarre", seenByFather.GetProperty("interests").EnumerateArray().Select(i => i.GetString()));
    }

    /// <summary>
    /// The wish writes into a JSON column with no schema limit, and nothing else prunes it – so length and
    /// list size are capped server-side. Without the cap a child could grow the column without bound.
    /// </summary>
    [Fact]
    public async Task DerFreitextWunsch_IstInLaengeUndMengeGedeckelt()
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, child) = await ChildAsync(father, "wunsch-deckel");

        var tooLong = await child.PostAsJsonAsync("/api/v1/student/me/interests/wishes",
            new { text = new string('x', 61) });
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        Assert.Equal("validation_error", await CodeOf(tooLong));

        for (var i = 0; i < 20; i++)
            (await child.PostAsJsonAsync("/api/v1/student/me/interests/wishes", new { text = $"Wunsch {i}" }))
                .EnsureSuccessStatusCode();

        var full = await child.PostAsJsonAsync("/api/v1/student/me/interests/wishes", new { text = "Einer zu viel" });
        Assert.Equal(HttpStatusCode.Conflict, full.StatusCode);
        Assert.Equal("interest_wish_limit_reached", await CodeOf(full));
    }

    /// <summary>
    /// The wish is a read-modify-write of a whole JSON list. <c>Child.ConcurrencyStamp</c> is an EF
    /// concurrency token, so without bumping it two parallel wishes both match the unchanged token and the
    /// second silently overwrites the first - a lost update reported as 200.
    /// <para>
    /// Red probe: drop the <c>ConcurrencyStamp</c> assignment in <c>AddWish</c> ⇒ this test falls. The 500
    /// that the missing <c>TrySaveAsync</c> would cause has no deterministic test - real concurrency inside
    /// one request - and is deliberately not faked here.
    /// </para>
    /// </summary>
    [Fact]
    public async Task EinWunsch_BumptDasNebenlaeufigkeitsZeichen()
    {
        var father = await TestApi.AdultAsync(factory);
        var (childId, child) = await ChildAsync(father, "wunsch-stempel");

        var before = await StampOfAsync(childId);
        (await child.PostAsJsonAsync("/api/v1/student/me/interests/wishes", new { text = "Kanufahren" }))
            .EnsureSuccessStatusCode();

        Assert.NotEqual(before, await StampOfAsync(childId));
    }

    /// <summary>
    /// The self-service way is the child's alone: a supervisor token carries no <c>cid</c>, so
    /// <c>me/interests</c> is not its seat – it edits the child under <c>supervisor/children/{id}</c>.
    /// </summary>
    [Fact]
    public async Task OhneKindToken_BleibtDerSelbstbedienungswegVerschlossen()
    {
        var father = await TestApi.AdultAsync(factory);
        var anonymous = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/student/me/interests")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await father.GetAsync("/api/v1/student/me/interests")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await father.GetAsync("/api/v1/student/interest-tags")).StatusCode);
    }

    /// <summary>
    /// The picking list the guided process shows. It deliberately carries no usage counts: they would tell
    /// one child how many others picked a keyword.
    /// </summary>
    [Fact]
    public async Task DieAuswahlliste_ZeigtDieTaxonomieOhneNutzungszaehler()
    {
        var father = await TestApi.AdultAsync(factory);
        var (_, child) = await ChildAsync(father, "auswahlliste");
        var styleTag = await TagAsync(father, TestApi.UniqueName("Pixelkunst"), "Style");

        // Über die Facette geholt statt über den vollen Bestand: die Taxonomie ist global und wächst mit
        // jedem Testlauf, ein `take=500` über alles liefe irgendwann in den Deckel und der Fund wäre weg.
        var all = await JsonAsync(child, "/api/v1/student/interest-tags?facet=Style&take=500");
        var entry = all.EnumerateArray().Single(t => t.GetProperty("id").GetInt32() == styleTag);
        Assert.Equal("Style", entry.GetProperty("facet").GetString());
        Assert.False(entry.TryGetProperty("childCount", out _), "The picking list reveals no usage counts.");

        // The facet filter is what lets the process show topics and styles as separate steps.
        Assert.All(all.EnumerateArray(), t => Assert.Equal("Style", t.GetProperty("facet").GetString()));

        // Alphabetical by slug, not by label: SQLite compares the label byte-wise, so "Zebra" would sort
        // before "apfel" and the child's list would look shuffled.
        var frueh = await TagAsync(father, TestApi.UniqueName("apfelsorte"), "Nature");
        var spaet = await TagAsync(father, TestApi.UniqueName("Zebrastreifen"), "Nature");
        var natur = await JsonAsync(child, "/api/v1/student/interest-tags?facet=Nature&take=500");
        var slugs = natur.EnumerateArray().Select(t => t.GetProperty("slug").GetString()!).ToList();
        Assert.True(slugs.IndexOf(SlugOf(natur, frueh)) < slugs.IndexOf(SlugOf(natur, spaet)),
            $"Expected alphabetical order by slug, got: {string.Join(", ", slugs)}");
    }

    /// <summary>
    /// Acceptance criterion 4 of the story: a profile change affects <b>future</b> picks, never
    /// retroactively. Without that rule a child who likes to fiddle with its profile would learn a new
    /// image for the same word every week - and the memory effect the freeze was built for would be gone.
    /// <para>
    /// Red probe: make <c>MediaSelector</c> re-evaluate the frozen row before handing it out (instead of
    /// returning it) ⇒ the assertion on the unchanged asset id falls.
    /// </para>
    /// </summary>
    [Fact]
    public async Task EineProfilaenderung_LaesstDieEingefroreneBildwahlStehen()
    {
        var father = await TestApi.AdultAsync(factory);
        var scenario = await MediaScenarioAsync(father, TestApi.UniqueName("einfrieren").Replace(" ", "-").ToLowerInvariant());
        var child = await TestApi.ChildAsync(factory, scenario.ChildId, ChildPin);

        var einhorn = await TagAsync(father, "Einhorn", "Animal");
        var superhelden = await TagAsync(father, "Superhelden", "Franchise");
        (await child.PutAsJsonAsync("/api/v1/student/me/interests", new
        {
            interests = new[] { new { tagId = einhorn, weight = 3 } },
        })).EnsureSuccessStatusCode();

        var before = (await FirstCardAsync(father, scenario)).GetProperty("imageUrl").GetString();
        Assert.Contains("unicorn", before);
        var frozen = PicksOf(scenario.ChildId).Single();

        // The child now prefers something else entirely - and still gets its unicorn back.
        (await child.PutAsJsonAsync("/api/v1/student/me/interests", new
        {
            interests = new[] { new { tagId = superhelden, weight = 3 }, new { tagId = einhorn, weight = 0 } },
        })).EnsureSuccessStatusCode();

        Assert.Equal(before, (await FirstCardAsync(father, scenario)).GetProperty("imageUrl").GetString());
        var after = PicksOf(scenario.ChildId).Single();
        Assert.Equal(frozen.MediaAssetId, after.MediaAssetId);
        Assert.Equal(frozen.PickedAt, after.PickedAt);
    }

    // ---- Helpers -----------------------------------------------------------------------------------

    /// <summary>A child of its own per test plus a logged-in client for it – the tests run against one DB.</summary>
    private async Task<(int ChildId, HttpClient Client)> ChildAsync(HttpClient father, string marker)
    {
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName(marker), pin = ChildPin }));
        return (childId, await TestApi.ChildAsync(factory, childId, ChildPin));
    }

    /// <summary>
    /// Creates (or finds) a taxonomy keyword and releases it for children – the creator owns the vocabulary, the child
    /// only picks from what is released (B-207). Released by PATCH, not only on create: creating is idempotent, and a
    /// keyword another test already made hidden would come back unchanged.
    /// </summary>
    private static async Task<int> TagAsync(HttpClient father, string label, string facet)
    {
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/interest-tags", new { label, facet }));
        (await father.PatchAsJsonAsync($"/api/v1/creator/interest-tags/{id}", new { visibleToChildren = true }))
            .EnsureSuccessStatusCode();
        return id;
    }

    /// <summary>The child's frozen image picks – the rows AC4 requires to stay untouched.</summary>
    private List<ChildMediaPick> PicksOf(int childId)
    {
        using var scope = factory.Services.CreateScope();
        return [.. scope.ServiceProvider.GetRequiredService<PuglingDbContext>()
            .ChildMediaPicks.AsNoTracking().Where(p => p.ChildId == childId)];
    }

    private sealed record MediaScenario(int ChildId, int PlanId, int PositionId);

    /// <summary>
    /// A minimal but complete playing scenario: own child, one vocabulary exercise with one word, two
    /// competing motifs in the store, and a running plan with one position on the self-assessment stage
    /// (the stage where an image is allowed at all).
    /// </summary>
    private async Task<MediaScenario> MediaScenarioAsync(HttpClient father, string marker)
    {
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = marker, pin = ChildPin }));

        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = marker }));
        var seriesId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = $"{marker}-Reihe", subjectId }));
        var seriesUnitId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Unit" }));
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary", new
            {
                title = $"{marker}-Uebung",
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    direction = "front-to-back",
                    sourceLang = "en",
                    targetLang = "de",
                    // A word of its own: the vocabulary store is find-or-create, and a shared word would
                    // collect the image assignments of every scenario on the same row.
                    items = new[] { new { front = $"run-{marker}", back = $"laufen-{marker}" } },
                },
            }));

        var items = await JsonAsync(father,
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{exerciseId}/items");
        var vocabularyId = items[0].GetProperty("vocabularyId").GetInt32();

        foreach (var (key, description, tags) in new (string, string, string[])[]
        {
            ($"{marker}_unicorn", "Ein Einhorn laeuft", ["Einhorn"]),
            ($"{marker}_flash", "Flash rennt", ["Superhelden"]),
        })
        {
            var assetId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/media", new
            {
                key,
                description,
                rating = "Everyone",
                tags,
                variants = new[] { new { purpose = "Card", url = $"https://cdn.test/{key}.webp", width = 512, height = 512 } },
            }));
            (await father.PostAsJsonAsync($"/api/v1/creator/vocabulary/{vocabularyId}/media",
                new { mediaAssetId = assetId })).EnsureSuccessStatusCode();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var planId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/study-plans", new
        {
            childId,
            subjectId = await TestApi.DefaultSubjectIdAsync(father),
            title = $"{marker}-Plan",
            startDate = today.AddDays(-1).ToString("yyyy-MM-dd"),
            durationDays = 31,
        }));
        var positionId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions",
            // Stage 2 = SelfAssess: the solution is visible anyway, so an image is allowed there.
            new { exerciseId, order = 1, cadence = "Daily", goalThreshold = 1, pointsGoalMet = 5, stage = 2 }));

        return new MediaScenario(childId, planId, positionId);
    }

    /// <summary>Starts a practice session as the supervisor (preview) and returns the first card.</summary>
    private static async Task<JsonElement> FirstCardAsync(HttpClient father, MediaScenario s)
    {
        var start = await father.PostAsJsonAsync(
            $"/api/v1/student/study-plans/{s.PlanId}/positions/{s.PositionId}/practice-sessions",
            new { mode = "Lern" });
        start.EnsureSuccessStatusCode();
        var sessionId = await TestApi.IdAsync(start);

        return (await JsonAsync(father,
            $"/api/v1/student/study-plans/{s.PlanId}/positions/{s.PositionId}/practice-sessions/{sessionId}/next"))
            .GetProperty("card");
    }

    private static async Task<JsonElement> JsonAsync(HttpClient client, string url)
    {
        var res = await client.GetAsync(url);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>The child's concurrency token straight from the DB - what a lost update would leave alone.</summary>
    private async Task<Guid> StampOfAsync(int childId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<PuglingDbContext>()
            .Children.AsNoTracking().Where(c => c.Id == childId)
            .Select(c => c.ConcurrencyStamp).FirstAsync();
    }

    /// <summary>The slug the picking list reports for a tag id - the value its ordering is built on.</summary>
    private static string SlugOf(JsonElement list, int tagId) =>
        list.EnumerateArray().Single(t => t.GetProperty("id").GetInt32() == tagId).GetProperty("slug").GetString()!;

    private static async Task<string?> CodeOf(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
}
