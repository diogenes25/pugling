using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Sichert die Unterrichts-Seite des Katalogs ab: Lehrwerk-Reihen samt Units, die Creator-Profile
/// („Fachlehrer") und – der eigentliche Zweck – das <b>Matching</b> Kind → Profil. Die Reihe ist der
/// Angelpunkt: nur weil Kind-Lehrbuch und Profil denselben Datensatz nennen, ist die Frage „wer kennt
/// das Material dieses Kindes?" mehr als ein Freitext-Vergleich.
/// </summary>
public class CreatorProfileTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string SeriesRoot = "/api/v1/creator/textbook-series";
    private const string ProfileRoot = "/api/v1/creator/profiles";

    /// <summary>Legt eine Reihe an und liefert ihre Id (Name je Lauf eindeutig – der Slug ist global).</summary>
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
        var creator = await TestApi.FatherAsync(factory);
        var seriesId = await CreateSeriesAsync(creator, "Access");

        var unitId = await TestApi.IdAsync(await creator.PostAsJsonAsync($"{SeriesRoot}/{seriesId}/units", new
        {
            label = "Unit 3 – Growing up",
            grade = 8,
            topics = "Familie, Freundschaft",
            grammar = "Present perfect",
            vocabularyNotes = "to grow up, responsibility",
        }));

        // Ohne orderIndex wird hinten angehängt – der Aufrufer muss die Reihenfolge nicht kennen.
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

        // Der Stoff der Unit ist der Grund für diese Tabelle – er muss unverändert zurückkommen.
        var unit = await creator.GetFromJsonAsync<JsonElement>($"{SeriesRoot}/{seriesId}/units/{unitId}");
        Assert.Equal("Present perfect", unit.GetProperty("grammar").GetString());

        var patched = await (await creator.PatchAsJsonAsync($"{SeriesRoot}/{seriesId}/units/{unitId}", new
        {
            grammar = "Present perfect vs. simple past",
        })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Present perfect vs. simple past", patched.GetProperty("grammar").GetString());
        // Was nicht im Payload steht, bleibt stehen.
        Assert.Equal("Familie, Freundschaft", patched.GetProperty("topics").GetString());
    }

    /// <summary>
    /// Der Slug macht das Anlegen idempotent: ein Agent darf denselben Katalog-Aufbau wiederholen, ohne
    /// „Access" zweimal in den geteilten Katalog zu schreiben.
    /// </summary>
    [Fact]
    public async Task Dieselbe_Reihe_zweimal_angelegt_liefert_dieselbe_Reihe()
    {
        var creator = await TestApi.FatherAsync(factory);
        var name = $"Green Line {Guid.NewGuid():N}";

        var first = await creator.PostAsJsonAsync(SeriesRoot, new { name, publisher = "Klett" });
        var again = await creator.PostAsJsonAsync(SeriesRoot, new { name, publisher = "Klett" });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(await TestApi.IdAsync(first), await TestApi.IdAsync(again));
    }

    /// <summary>
    /// Der Katalog ist geteilt: lesen darf jeder Creator, ändern nur der Owner. Sonst könnte ein Vater
    /// die Reihe umbenennen, an der die Profile und Lehrbücher anderer Familien hängen.
    /// </summary>
    [Fact]
    public async Task Ein_fremder_Creator_darf_lesen_aber_nicht_aendern()
    {
        var owner = await TestApi.FatherAsync(factory);
        var seriesId = await CreateSeriesAsync(owner, "Lighthouse");

        var strangerId = await TestApi.IdAsync(await owner.PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = $"Fremder {Guid.NewGuid():N}", pin = "4321" }));
        var stranger = await TestApi.FatherAsync(factory, strangerId, "4321");

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
        var creator = await TestApi.FatherAsync(factory);
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
        // Dubletten in den bevorzugten Typen werden verworfen.
        Assert.Equal(2, profile.GetProperty("defaultTypes").GetArrayLength());
        // Der Reihenname kommt mit, damit ein UI die Zuordnung ohne Nachladen zeigen kann.
        Assert.False(string.IsNullOrEmpty(profile.GetProperty("seriesName").GetString()));

        // Ins Leere zeigende Verweise werden abgewiesen – ein Profil ohne Reihe findet nie ein Kind.
        var bad = await creator.PostAsJsonAsync(ProfileRoot, new { name = "Kaputt", seriesId = 999999 });
        Assert.Equal("invalid_reference", await CodeOfAsync(bad));
        var badSubject = await creator.PostAsJsonAsync(ProfileRoot, new { name = "Kaputt", subjectId = 999999 });
        Assert.Equal("invalid_reference", await CodeOfAsync(badSubject));

        var swapped = await creator.PostAsJsonAsync(ProfileRoot,
            new { name = "Verdreht", gradeMin = 9, gradeMax = 5 });
        Assert.Equal("validation_error", await CodeOfAsync(swapped));

        // Stilllegen: das Profil verschwindet aus der Standard-Liste, bleibt aber abrufbar.
        await creator.PatchAsJsonAsync($"{ProfileRoot}/{profileId}", new { active = false });
        var listed = await creator.GetFromJsonAsync<JsonElement>(ProfileRoot);
        Assert.DoesNotContain(profileId, Ids(listed));
        var withInactive = await creator.GetFromJsonAsync<JsonElement>($"{ProfileRoot}?includeInactive=true");
        Assert.Contains(profileId, Ids(withInactive));

        Assert.Equal(HttpStatusCode.NoContent, (await creator.DeleteAsync($"{ProfileRoot}/{profileId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await creator.GetAsync($"{ProfileRoot}/{profileId}")).StatusCode);
    }

    /// <summary>
    /// Das Herzstück: das Profil zur <b>Buchreihe des Kindes</b> schlägt das, das nur Fach und
    /// Klassenstufe trifft. Ein Profil für eine andere Klassenstufe ist kein schlechterer Treffer,
    /// sondern keiner.
    /// </summary>
    [Fact]
    public async Task Das_Matching_stellt_den_Reihen_Treffer_nach_vorn()
    {
        var creator = await TestApi.FatherAsync(factory);
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
        // Das Kind arbeitet mit „Access" – genau das soll die Auswahl entscheiden.
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

        // Bestes zuerst: die gemeinsame Buchreihe.
        Assert.Equal(withSeries, ranked[0].GetProperty("profile").GetProperty("id").GetInt32());
        Assert.Contains("series_match",
            ranked[0].GetProperty("reasons").EnumerateArray().Select(r => r.GetString()));
        Assert.True(ranked[0].GetProperty("score").GetInt32()
                    > ranked[1].GetProperty("score").GetInt32());

        var ids = ranked.Select(m => m.GetProperty("profile").GetProperty("id").GetInt32()).ToList();
        Assert.Contains(otherSeries, ids);
        // Harte Ausschlüsse: falsche Klassenstufe und stillgelegte Profile tauchen gar nicht auf.
        Assert.DoesNotContain(wrongGrade, ids);
        Assert.DoesNotContain(inactive, ids);
    }

    /// <summary>
    /// Der Match-Endpunkt liest Kind-Daten. Ein Creator, der das Kind nicht betreut, darf ihn nicht als
    /// Seitenkanal auf fremde Kind-Profile benutzen.
    /// </summary>
    [Fact]
    public async Task Ein_fremdes_Kind_wird_beim_Matching_abgewiesen()
    {
        var supervisor = await TestApi.FatherAsync(factory);
        var childId = await TestApi.IdAsync(await supervisor.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Fremd-Kind", pin = "8211", grade = 8 }));

        var strangerId = await TestApi.IdAsync(await supervisor.PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = $"Fremder {Guid.NewGuid():N}", pin = "4322" }));
        var stranger = await TestApi.FatherAsync(factory, strangerId, "4322");

        var response = await stranger.GetAsync($"{ProfileRoot}/match?childId={childId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("forbidden", await CodeOfAsync(response));

        Assert.Equal(HttpStatusCode.NotFound,
            (await stranger.GetAsync($"{ProfileRoot}/match?childId=999999")).StatusCode);
    }

    /// <summary>
    /// Die Unit am Kind muss zur Reihe am Kind gehören. Sonst bekäme der Creator den Stoff eines Buchs,
    /// das das Kind nicht benutzt – fachlich falsch, aber technisch unauffällig.
    /// </summary>
    [Fact]
    public async Task Eine_Unit_aus_fremder_Reihe_am_Lehrbuch_wird_abgewiesen()
    {
        var creator = await TestApi.FatherAsync(factory);
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

        // Eine Unit ohne Reihe wäre ebenso wenig auflösbar.
        var orphan = await creator.PostAsJsonAsync(books, new { title = "Access 8", currentUnitId = unitId });
        Assert.Equal("validation_error", await CodeOfAsync(orphan));

        var created = await (await creator.PostAsJsonAsync(books,
            new { title = "Access 8", seriesId, currentUnitId = unitId })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(unitId, created.GetProperty("currentUnitId").GetInt32());
        Assert.Equal("Unit 3", created.GetProperty("currentUnitLabel").GetString());

        // Nur die Unit nachtragen: geprüft wird gegen den Zielzustand, nicht gegen den Payload.
        var textbookId = created.GetProperty("id").GetInt32();
        var patched = await creator.PatchAsJsonAsync($"{books}/{textbookId}", new { currentUnitId = foreignUnitId });
        Assert.Equal("validation_error", await CodeOfAsync(patched));
    }

    /// <summary>
    /// Löschen ist bewusst nicht gesperrt: Kind-Lehrbuch und Profil verlieren nur die Zuordnung
    /// (SetNull) und bleiben mit ihrem Freitext arbeitsfähig.
    /// </summary>
    [Fact]
    public async Task Eine_geloeschte_Reihe_leert_nur_die_Zuordnungen()
    {
        var creator = await TestApi.FatherAsync(factory);
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

    /// <summary>Der maschinenlesbare Fehlercode aus dem ProblemDetails-Körper.</summary>
    private static async Task<string?> CodeOfAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("code", out var code)
            ? code.GetString()
            : null;

    private static IEnumerable<int> Ids(JsonElement array) =>
        array.EnumerateArray().Select(e => e.GetProperty("id").GetInt32());
}
