using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// The work list of the image generator (B-485, <c>creator/media/demand</c>). What it must get right: exactly the two
/// strongest theme interests count (never a weak one, a third one, or a style), an image already there takes its slot
/// out of the list, and the child scope is bound to supervision like every other endpoint that reads a child.
/// </summary>
public class MediaDemandTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string Demand = "/api/v1/creator/media/demand";

    /// <summary>
    /// Red if the weight bound is dropped (the weight-1 tag appears), if the cut at two is dropped (the third weight-2
    /// tag appears), if style interests are not filtered, or if the tiebreak is not by tag id (B and C swap).
    /// </summary>
    [Fact]
    public async Task Gewuenscht_sind_die_zwei_staerksten_Themen_Interessen_und_ein_neutrales_Bild()
    {
        var father = await TestApi.AdultAsync(factory);
        var s = await ScenarioAsync(father, "dem1");

        var entries = await GetAsync(father, $"{Demand}?childId={s.ChildId}");

        Assert.Equal(2, entries.GetArrayLength());
        foreach (var entry in entries.EnumerateArray())
        {
            Assert.True(entry.GetProperty("needsNeutral").GetBoolean());
            Assert.Equal([s.TagA, s.TagB], Slugs(entry));
        }
        // The prompt travels with the tag; a tag without one arrives with null so the generator can report it.
        var tags = entries[0].GetProperty("wantedTags");
        Assert.Equal("on a soccer pitch", tags[0].GetProperty("imagePrompt").GetString());
        Assert.Equal(JsonValueKind.Null, tags[1].GetProperty("imagePrompt").ValueKind);
    }

    /// <summary>
    /// Red if an existing image does not take its slot out (the generator would draw the same thing on every run), or
    /// if a tagged image is miscounted as the neutral one.
    /// </summary>
    [Fact]
    public async Task Ein_vorhandenes_Bild_nimmt_seinen_Platz_aus_der_Liste()
    {
        var father = await TestApi.AdultAsync(factory);
        var s = await ScenarioAsync(father, "dem2");

        await LinkAsync(father, s.VocabularyIds[0], "dem2_a", [s.TagA]);
        var afterTagged = await EntryAsync(father, s, s.VocabularyIds[0]);
        Assert.True(afterTagged!.Value.GetProperty("needsNeutral").GetBoolean());
        Assert.Equal([s.TagB], Slugs(afterTagged.Value));

        await LinkAsync(father, s.VocabularyIds[0], "dem2_neutral", []);
        await LinkAsync(father, s.VocabularyIds[0], "dem2_b", [s.TagB]);
        Assert.Null(await EntryAsync(father, s, s.VocabularyIds[0]));
        // The other word is untouched.
        Assert.NotNull(await EntryAsync(father, s, s.VocabularyIds[1]));
    }

    /// <summary>Red if the unit scope ignores the tag or leaks the child's interests into a child-free list.</summary>
    [Fact]
    public async Task Die_Unit_wird_ohne_Kind_fuer_ein_genanntes_Interesse_abgefragt()
    {
        var father = await TestApi.AdultAsync(factory);
        var s = await ScenarioAsync(father, "dem3");

        var neutralOnly = await GetAsync(father, $"{Demand}?seriesUnitId={s.SeriesUnitId}");
        Assert.Equal(2, neutralOnly.GetArrayLength());
        Assert.All(neutralOnly.EnumerateArray(), e => Assert.Empty(Slugs(e)));

        var withTag = await GetAsync(father, $"{Demand}?seriesUnitId={s.SeriesUnitId}&tag={s.TagB}");
        Assert.All(withTag.EnumerateArray(), e => Assert.Equal([s.TagB], Slugs(e)));

        var unknownTag = await father.GetAsync($"{Demand}?seriesUnitId={s.SeriesUnitId}&tag=gibt-es-nicht-dem3");
        Assert.Equal(HttpStatusCode.NotFound, unknownTag.StatusCode);
    }

    /// <summary>Red if the scope check is dropped: both or neither scope, or a tag for a child, must not silently pick one.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("?childId=1&seriesUnitId=1")]
    [InlineData("?childId=1&tag=x")]
    [InlineData("?childId=1&take=0")]
    public async Task Unklarer_Umfang_ist_ein_Validierungsfehler(string query)
    {
        var father = await TestApi.AdultAsync(factory);
        var res = await father.GetAsync(Demand + query);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("validation_error", (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    /// <summary>
    /// Red if the supervision check is dropped (200 with the child's vocabulary) or answers 403 – which would tell a
    /// stranger which child ids exist (B-310).
    /// </summary>
    [Fact]
    public async Task Ein_fremdes_Kind_ist_wie_ein_unbekanntes()
    {
        var father = await TestApi.AdultAsync(factory);
        var s = await ScenarioAsync(father, "dem4");
        var strangerId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = $"Fremder {Guid.NewGuid():N}", pin = "4323" }));
        var stranger = await TestApi.AdultAsync(factory, strangerId, "4323");

        var foreign = await stranger.GetAsync($"{Demand}?childId={s.ChildId}");
        var unknown = await stranger.GetAsync($"{Demand}?childId=999999");

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal((await unknown.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString(),
            (await foreign.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString());
    }

    // ---- Setup ----------------------------------------------------------------------------------------

    private sealed record Scenario(int ChildId, int SeriesUnitId, int[] VocabularyIds, string TagA, string TagB);

    /// <summary>
    /// A child with a running plan on a two-word vocabulary exercise and five interests: A (3) and B (2) are the two
    /// strongest themes; C (2) loses the tie to B by its later id, D (1) is too weak, the style tag (3) is a style.
    /// </summary>
    private static async Task<Scenario> ScenarioAsync(HttpClient father, string marker)
    {
        var tagA = await TagAsync(father, $"{marker} Fussball", "Sport", "on a soccer pitch");
        var tagB = await TagAsync(father, $"{marker} Roboter", "Other", null);
        await TagAsync(father, $"{marker} Pferde", "Animal", "on a farm");
        await TagAsync(father, $"{marker} Raketen", "Vehicle", "in space");
        await TagAsync(father, $"{marker} Comicstil", "Style", "comic style");

        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = $"Kind {marker}", pin = "9871" }));
        (await father.PutAsJsonAsync($"/api/v1/supervisor/children/{childId}/interests", new
        {
            interests = new[]
            {
                new { label = $"{marker} Fussball", weight = 3 },
                new { label = $"{marker} Roboter", weight = 2 },
                new { label = $"{marker} Pferde", weight = 2 },
                new { label = $"{marker} Raketen", weight = 1 },
                new { label = $"{marker} Comicstil", weight = 3 },
            },
        })).EnsureSuccessStatusCode();

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
                    items = new[]
                    {
                        new { front = $"dog-{marker}", back = $"Hund-{marker}" },
                        new { front = $"jump-{marker}", back = $"springen-{marker}" },
                    },
                },
            }));
        var items = await GetAsync(father,
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{exerciseId}/items");
        int[] vocabularyIds = [.. items.EnumerateArray().Select(i => i.GetProperty("vocabularyId").GetInt32())];

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var planId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/study-plans", new
        {
            childId,
            title = $"{marker}-Plan",
            startDate = today.AddDays(-1).ToString("yyyy-MM-dd"),
            durationDays = 31,
        }));
        (await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, order = 1, cadence = "Daily", goalThreshold = 1, pointsGoalMet = 5 })).EnsureSuccessStatusCode();

        return new Scenario(childId, seriesUnitId, vocabularyIds, tagA, tagB);
    }

    private static async Task<string> TagAsync(HttpClient father, string label, string facet, string? imagePrompt)
    {
        var res = await father.PostAsJsonAsync("/api/v1/creator/interest-tags", new { label, facet, imagePrompt });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("slug").GetString()!;
    }

    private static async Task LinkAsync(HttpClient father, int vocabularyId, string key, string[] tags)
    {
        var assetId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/media", new
        {
            key,
            description = key,
            tags,
            variants = new[] { new { purpose = "Card", url = $"https://cdn.test/{key}.webp", width = 512, height = 512 } },
        }));
        (await father.PostAsJsonAsync($"/api/v1/creator/vocabulary/{vocabularyId}/media",
            new { mediaAssetId = assetId })).EnsureSuccessStatusCode();
    }

    private static async Task<JsonElement?> EntryAsync(HttpClient father, Scenario s, int vocabularyId)
    {
        var entries = await GetAsync(father, $"{Demand}?childId={s.ChildId}");
        foreach (var e in entries.EnumerateArray())
            if (e.GetProperty("vocabularyId").GetInt32() == vocabularyId) return e;
        return null;
    }

    private static string[] Slugs(JsonElement entry) =>
        [.. entry.GetProperty("wantedTags").EnumerateArray().Select(t => t.GetProperty("slug").GetString()!)];

    private static async Task<JsonElement> GetAsync(HttpClient client, string url)
    {
        var res = await client.GetAsync(url);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }
}
