using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Covers the teaching side of the catalog: textbook series with units, creator profiles
/// ("subject teacher") and - the actual point - the <b>matching</b> of child → profile. The series is the
/// pivot: only because the child's textbook and the profile name the same record is the question "who knows
/// this child's material?" more than a free-text comparison.
/// </summary>
public class CreatorProfileTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string SeriesRoot = "/api/v1/creator/textbook-series";
    private const string ProfileRoot = "/api/v1/creator/profiles";

    /// <summary>Creates a series and returns its id (name unique per run - the slug is global).</summary>
    private static async Task<int> CreateSeriesAsync(HttpClient creator, string name, string? publisher = "Cornelsen",
        int? subjectId = null, string? schoolTypes = "Gymnasium") =>
        await TestApi.IdAsync(await creator.PostAsJsonAsync(SeriesRoot, new
        {
            name = $"{name} {Guid.NewGuid():N}",
            publisher,
            subjectName = "Englisch",
            subjectId,
            schoolTypes,
            sourceLanguage = "en",
            targetLanguage = "de",
        }));

    [Fact]
    public async Task Reihe_und_Units_koennen_angelegt_gelesen_und_geaendert_werden()
    {
        var creator = await TestApi.AdultAsync(factory);
        var seriesId = await CreateSeriesAsync(creator, "Access");

        var unitId = await TestApi.IdAsync(await creator.PostAsJsonAsync($"{SeriesRoot}/{seriesId}/units", new
        {
            label = "Unit 3 – Growing up",
            grade = 8,
            topics = "Familie, Freundschaft",
            grammar = "Present perfect",
            vocabularyNotes = "to grow up, responsibility",
        }));

        // Without an orderIndex it is appended at the end - the caller need not know the order.
        var second = await (await creator.PostAsJsonAsync($"{SeriesRoot}/{seriesId}/units", new
        {
            label = "Unit 4 – School life",
            grade = 8,
        })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, second.GetProperty("orderIndex").GetInt32());

        var units = await creator.GetFromJsonAsync<JsonElement>($"{SeriesRoot}/{seriesId}/units");
        Assert.Equal(2, units.GetArrayLength());

        var series = await creator.GetFromJsonAsync<JsonElement>($"{SeriesRoot}/{seriesId}");
        Assert.Equal(2, series.GetProperty("unitCount").GetInt32());
        Assert.True(series.GetProperty("isOwn").GetBoolean());

        // The unit's subject matter is the reason for this table - it has to come back unchanged.
        var unit = await creator.GetFromJsonAsync<JsonElement>($"{SeriesRoot}/{seriesId}/units/{unitId}");
        Assert.Equal("Present perfect", unit.GetProperty("grammar").GetString());

        var patched = await (await creator.PatchAsJsonAsync($"{SeriesRoot}/{seriesId}/units/{unitId}", new
        {
            grammar = "Present perfect vs. simple past",
        })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Present perfect vs. simple past", patched.GetProperty("grammar").GetString());
        // What is not in the payload stays as it is.
        Assert.Equal("Familie, Freundschaft", patched.GetProperty("topics").GetString());
    }

    /// <summary>
    /// The slug makes creation idempotent: an agent may repeat the same catalog setup without
    /// writing "Access" twice into the shared catalog.
    /// </summary>
    [Fact]
    public async Task Dieselbe_Reihe_zweimal_angelegt_liefert_dieselbe_Reihe()
    {
        var creator = await TestApi.AdultAsync(factory);
        var name = $"Green Line {Guid.NewGuid():N}";

        var first = await creator.PostAsJsonAsync(SeriesRoot, new { name, publisher = "Klett" });
        var again = await creator.PostAsJsonAsync(SeriesRoot, new { name, publisher = "Klett" });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(await TestApi.IdAsync(first), await TestApi.IdAsync(again));
    }

    /// <summary>
    /// The catalog is shared: any creator may read it, only the owner may change it. Otherwise a father
    /// could rename the series that other families' profiles and textbooks depend on.
    /// </summary>
    [Fact]
    public async Task Ein_fremder_Creator_darf_lesen_aber_nicht_aendern()
    {
        var owner = await TestApi.AdultAsync(factory);
        var seriesId = await CreateSeriesAsync(owner, "Lighthouse");

        var strangerId = await TestApi.IdAsync(await owner.PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = $"Fremder {Guid.NewGuid():N}", pin = "4321" }));
        var stranger = await TestApi.AdultAsync(factory, strangerId, "4321");

        Assert.Equal(HttpStatusCode.OK, (await stranger.GetAsync($"{SeriesRoot}/{seriesId}")).StatusCode);

        var patch = await stranger.PatchAsJsonAsync($"{SeriesRoot}/{seriesId}", new { name = "Geklaut" });
        Assert.Equal(HttpStatusCode.Forbidden, patch.StatusCode);
        Assert.Equal("not_owner", await CodeOfAsync(patch));

        var unit = await stranger.PostAsJsonAsync($"{SeriesRoot}/{seriesId}/units", new { label = "Fremde Unit" });
        Assert.Equal(HttpStatusCode.Forbidden, unit.StatusCode);
        Assert.Equal("not_owner", await CodeOfAsync(unit));

        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.DeleteAsync($"{SeriesRoot}/{seriesId}")).StatusCode);
    }

    [Fact]
    public async Task Profil_CRUD_prueft_Fach_und_Reihe()
    {
        var creator = await TestApi.AdultAsync(factory);
        var seriesId = await CreateSeriesAsync(creator, "Access");

        var profile = await (await creator.PostAsJsonAsync(ProfileRoot, new
        {
            name = $"Englisch 8 Gymnasium {Guid.NewGuid():N}",
            subjectName = "Englisch",
            schoolTypes = "Gymnasium",
            gradeMin = 7,
            gradeMax = 8,
            seriesId,
            persona = "Du bist Englischlehrer an einem Gymnasium.",
            didactics = "Kurze Sätze.",
            defaultTypes = new[] { "Vocabulary", "Cloze", "Vocabulary" },
        })).Content.ReadFromJsonAsync<JsonElement>();
        var profileId = profile.GetProperty("id").GetInt32();

        Assert.True(profile.GetProperty("isOwn").GetBoolean());
        Assert.True(profile.GetProperty("active").GetBoolean());
        // Duplicates in the preferred types are discarded.
        Assert.Equal(2, profile.GetProperty("defaultTypes").GetArrayLength());
        // The series name comes along so that a UI can show the assignment without a second load.
        Assert.False(string.IsNullOrEmpty(profile.GetProperty("seriesName").GetString()));

        // References pointing nowhere are rejected - a profile without a series never finds a child.
        var bad = await creator.PostAsJsonAsync(ProfileRoot, new { name = "Kaputt", seriesId = 999999 });
        Assert.Equal("invalid_reference", await CodeOfAsync(bad));
        var badSubject = await creator.PostAsJsonAsync(ProfileRoot, new { name = "Kaputt", subjectId = 999999 });
        Assert.Equal("invalid_reference", await CodeOfAsync(badSubject));

        var swapped = await creator.PostAsJsonAsync(ProfileRoot,
            new { name = "Verdreht", gradeMin = 9, gradeMax = 5 });
        Assert.Equal("validation_error", await CodeOfAsync(swapped));

        // Deactivating: the profile disappears from the default list but stays retrievable.
        await creator.PatchAsJsonAsync($"{ProfileRoot}/{profileId}", new { active = false });
        var listed = await creator.GetFromJsonAsync<JsonElement>(ProfileRoot);
        Assert.DoesNotContain(profileId, Ids(listed));
        var withInactive = await creator.GetFromJsonAsync<JsonElement>($"{ProfileRoot}?includeInactive=true");
        Assert.Contains(profileId, Ids(withInactive));

        Assert.Equal(HttpStatusCode.NoContent, (await creator.DeleteAsync($"{ProfileRoot}/{profileId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await creator.GetAsync($"{ProfileRoot}/{profileId}")).StatusCode);
    }

    /// <summary>
    /// The core piece: the profile matching the <b>child's book series</b> beats one that only matches
    /// subject and grade. A profile for a different grade is not a weaker match, it is no match at all.
    /// </summary>
    [Fact]
    public async Task Das_Matching_stellt_den_Reihen_Treffer_nach_vorn()
    {
        var creator = await TestApi.AdultAsync(factory);
        var subjectId = await TestApi.IdAsync(
            await creator.PostAsJsonAsync("/api/v1/creator/subjects", new { name = $"Englisch {Guid.NewGuid():N}" }));
        var seriesId = await CreateSeriesAsync(creator, "Access", subjectId: subjectId);
        var otherSeriesId = await CreateSeriesAsync(creator, "Green Line", subjectId: subjectId);

        var childId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/supervisor/children", new
        {
            name = "Match-Kind",
            pin = "8210",
            grade = 8,
            schoolType = "Gymnasium",
        }));
        // The child works with "Access" - that is exactly what should decide the selection.
        await creator.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/textbooks", new
        {
            title = "Access 8",
            subjectName = "Englisch",
            subjectId,
            seriesId,
        });

        var withSeries = await CreateProfileAsync(creator, "Mit Reihe", subjectId, seriesId, 7, 9);
        var otherSeries = await CreateProfileAsync(creator, "Andere Reihe", subjectId, otherSeriesId, 7, 9);
        var wrongGrade = await CreateProfileAsync(creator, "Falsche Stufe", subjectId, seriesId, 5, 6);
        var inactive = await CreateProfileAsync(creator, "Stillgelegt", subjectId, seriesId, 7, 9);
        await creator.PatchAsJsonAsync($"{ProfileRoot}/{inactive}", new { active = false });

        var matches = await creator.GetFromJsonAsync<JsonElement>(
            $"{ProfileRoot}/match?childId={childId}&subjectId={subjectId}");
        var ranked = matches.EnumerateArray().ToList();

        // Best first: the shared textbook series.
        Assert.Equal(withSeries, ranked[0].GetProperty("profile").GetProperty("id").GetInt32());
        Assert.Contains("series_match",
            ranked[0].GetProperty("reasons").EnumerateArray().Select(r => r.GetString()));
        Assert.True(ranked[0].GetProperty("score").GetInt32()
                    > ranked[1].GetProperty("score").GetInt32());

        var ids = ranked.Select(m => m.GetProperty("profile").GetProperty("id").GetInt32()).ToList();
        Assert.Contains(otherSeries, ids);
        // Hard exclusions: the wrong grade and deactivated profiles do not show up at all.
        Assert.DoesNotContain(wrongGrade, ids);
        Assert.DoesNotContain(inactive, ids);
    }

    /// <summary>
    /// The <b>weight ranking</b> - series 8 &gt; subject 4 &gt; grade 2 &gt; school type 1 - is the
    /// actual business statement of the matching, but was nowhere pinned down: only <i>that</i>
    /// a profile wins was checked, not <i>why</i>. The weights could therefore be flattened without any
    /// test failing (docs/testplan.md, injection D15).
    /// <para>
    /// The core is not "8 is greater than 4", but: <b>the series alone beats everything else combined</b>
    /// (8 &gt; 4 + 2 + 1). Only the series reveals whether the creator knows the concrete material; subject, grade
    /// and school type only match the shelf. By exactly one point - whoever tweaks the weights tips this.
    /// </para>
    /// The weaker profile is always created <b>first</b>: on a tie the id decides
    /// ascending, so a flattened weight would flip the order and the test would fail. Were it
    /// the other way round, it would stay green even on a tie.
    /// </summary>
    [Fact]
    public async Task Das_Matching_haelt_die_Rangfolge_der_Gewichte_ein()
    {
        var creator = await TestApi.AdultAsync(factory);
        var subjectId = await TestApi.IdAsync(
            await creator.PostAsJsonAsync("/api/v1/creator/subjects", new { name = $"Englisch {Guid.NewGuid():N}" }));
        var seriesId = await CreateSeriesAsync(creator, "Access", subjectId: subjectId);

        var childId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/supervisor/children", new
        {
            name = "Gewichte-Kind",
            pin = "8214",
            grade = 8,
            schoolType = "Gymnasium",
        }));
        (await creator.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/textbooks", new
        {
            title = "Access 8",
            subjectName = "Englisch",
            subjectId,
            seriesId,
        })).EnsureSuccessStatusCode();

        // Subject + grade + school type, but the wrong series: 4 + 2 + 1 = 7.
        var allesAusserReihe = await CreateWeightedProfileAsync(creator, "Fach+Stufe+Schulart",
            subjectId: subjectId, seriesId: null, gradeMin: 7, gradeMax: 9, schoolTypes: "Gymnasium");
        // The series alone: 8. It still has to come first.
        var nurReihe = await CreateWeightedProfileAsync(creator, "Nur Reihe",
            subjectId: null, seriesId: seriesId, gradeMin: null, gradeMax: null, schoolTypes: null);

        // Grade (2) against school type (1) - the same pattern one level down.
        var nurSchulart = await CreateWeightedProfileAsync(creator, "Nur Schulart",
            subjectId: null, seriesId: null, gradeMin: null, gradeMax: null, schoolTypes: "Gymnasium");
        var nurStufe = await CreateWeightedProfileAsync(creator, "Nur Stufe",
            subjectId: null, seriesId: null, gradeMin: 7, gradeMax: 9, schoolTypes: null);

        var ranked = (await creator.GetFromJsonAsync<JsonElement>(
            $"{ProfileRoot}/match?childId={childId}&subjectId={subjectId}")).EnumerateArray().ToList();
        var ids = ranked.Select(m => m.GetProperty("profile").GetProperty("id").GetInt32()).ToList();
        int ScoreOf(int id) => ranked[ids.IndexOf(id)].GetProperty("score").GetInt32();

        Assert.Equal(8, ScoreOf(nurReihe));
        Assert.Equal(7, ScoreOf(allesAusserReihe));
        Assert.Equal(2, ScoreOf(nurStufe));
        Assert.Equal(1, ScoreOf(nurSchulart));

        // And the points really have to carry the sorting (the list comes "best first").
        Assert.True(ids.IndexOf(nurReihe) < ids.IndexOf(allesAusserReihe));
        Assert.True(ids.IndexOf(nurStufe) < ids.IndexOf(nurSchulart));
    }

    /// <summary>
    /// Like <c>CreateProfileAsync</c>, but every scoring property can be deselected individually - only this way
    /// can a profile be built that scores <b>exclusively</b> through one weight.
    /// </summary>
    private static async Task<int> CreateWeightedProfileAsync(HttpClient creator, string name,
        int? subjectId, int? seriesId, int? gradeMin, int? gradeMax, string? schoolTypes) =>
        await TestApi.IdAsync(await creator.PostAsJsonAsync(ProfileRoot, new
        {
            name = $"{name} {Guid.NewGuid():N}",
            subjectId,
            seriesId,
            gradeMin,
            gradeMax,
            schoolTypes,
        }));

    /// <summary>
    /// The match endpoint reads child data. A creator who does not supervise the child must not use it as a
    /// side channel onto other children's profiles.
    /// </summary>
    [Fact]
    public async Task Ein_fremdes_Kind_wird_beim_Matching_abgewiesen()
    {
        var supervisor = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await supervisor.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Fremd-Kind", pin = "8211", grade = 8 }));

        var strangerId = await TestApi.IdAsync(await supervisor.PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = $"Fremder {Guid.NewGuid():N}", pin = "4322" }));
        var stranger = await TestApi.AdultAsync(factory, strangerId, "4322");

        var response = await stranger.GetAsync($"{ProfileRoot}/match?childId={childId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("forbidden", await CodeOfAsync(response));

        Assert.Equal(HttpStatusCode.NotFound,
            (await stranger.GetAsync($"{ProfileRoot}/match?childId=999999")).StatusCode);
    }

    /// <summary>
    /// The unit on the child must belong to the series on the child. Otherwise the creator would get the
    /// material of a book the child does not use - wrong in substance, but technically unremarkable.
    /// </summary>
    [Fact]
    public async Task Eine_Unit_aus_fremder_Reihe_am_Lehrbuch_wird_abgewiesen()
    {
        var creator = await TestApi.AdultAsync(factory);
        var seriesId = await CreateSeriesAsync(creator, "Access");
        var otherSeriesId = await CreateSeriesAsync(creator, "Green Line");
        var foreignUnitId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"{SeriesRoot}/{otherSeriesId}/units", new { label = "Fremde Unit", grade = 8 }));
        var unitId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"{SeriesRoot}/{seriesId}/units", new { label = "Unit 3", grade = 8 }));

        var childId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Buch-Kind", pin = "8212", grade = 8 }));
        var books = $"/api/v1/supervisor/children/{childId}/textbooks";

        var wrong = await creator.PostAsJsonAsync(books,
            new { title = "Access 8", seriesId, currentUnitId = foreignUnitId });
        Assert.Equal("validation_error", await CodeOfAsync(wrong));

        // A unit without a series would be just as unresolvable.
        var orphan = await creator.PostAsJsonAsync(books, new { title = "Access 8", currentUnitId = unitId });
        Assert.Equal("validation_error", await CodeOfAsync(orphan));

        var created = await (await creator.PostAsJsonAsync(books,
            new { title = "Access 8", seriesId, currentUnitId = unitId })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(unitId, created.GetProperty("currentUnitId").GetInt32());
        Assert.Equal("Unit 3", created.GetProperty("currentUnitLabel").GetString());

        // Adding only the unit: the check runs against the target state, not against the payload.
        var textbookId = created.GetProperty("id").GetInt32();
        var patched = await creator.PatchAsJsonAsync($"{books}/{textbookId}", new { currentUnitId = foreignUnitId });
        Assert.Equal("validation_error", await CodeOfAsync(patched));
    }

    /// <summary>
    /// Deletion is deliberately not blocked: the child's textbook and the profile only lose the
    /// association (SetNull) and remain usable with their free text.
    /// </summary>
    [Fact]
    public async Task Eine_geloeschte_Reihe_leert_nur_die_Zuordnungen()
    {
        var creator = await TestApi.AdultAsync(factory);
        var seriesId = await CreateSeriesAsync(creator, "Access");
        var unitId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"{SeriesRoot}/{seriesId}/units", new { label = "Unit 1", grade = 8 }));
        var profileId = await CreateProfileAsync(creator, "Mit Reihe", null, seriesId, 7, 9);

        var childId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Reihen-Kind", pin = "8213", grade = 8 }));
        var textbookId = await TestApi.IdAsync(await creator.PostAsJsonAsync(
            $"/api/v1/supervisor/children/{childId}/textbooks",
            new { title = "Access 8", seriesId, currentUnitId = unitId }));

        Assert.Equal(HttpStatusCode.NoContent, (await creator.DeleteAsync($"{SeriesRoot}/{seriesId}")).StatusCode);

        var book = await creator.GetFromJsonAsync<JsonElement>(
            $"/api/v1/supervisor/children/{childId}/textbooks/{textbookId}");
        Assert.Equal(JsonValueKind.Null, book.GetProperty("seriesId").ValueKind);
        Assert.Equal(JsonValueKind.Null, book.GetProperty("currentUnitId").ValueKind);
        Assert.Equal("Access 8", book.GetProperty("title").GetString());

        var profile = await creator.GetFromJsonAsync<JsonElement>($"{ProfileRoot}/{profileId}");
        Assert.Equal(JsonValueKind.Null, profile.GetProperty("seriesId").ValueKind);
    }

    private static async Task<int> CreateProfileAsync(HttpClient creator, string name, int? subjectId,
        int? seriesId, int gradeMin, int gradeMax) =>
        await TestApi.IdAsync(await creator.PostAsJsonAsync(ProfileRoot, new
        {
            name = $"{name} {Guid.NewGuid():N}",
            subjectName = "Englisch",
            subjectId,
            schoolTypes = "Gymnasium",
            gradeMin,
            gradeMax,
            seriesId,
        }));

    /// <summary>The machine-readable error code from the ProblemDetails body.</summary>
    private static async Task<string?> CodeOfAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("code", out var code)
            ? code.GetString()
            : null;

    private static IEnumerable<int> Ids(JsonElement array) =>
        array.EnumerateArray().Select(e => e.GetProperty("id").GetInt32());
}
