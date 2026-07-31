using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Verifies the <c>Clear…</c> switches of the PATCH endpoints.
///
/// Background: in a PATCH, <c>null</c> means "not specified" – a field that was set could therefore
/// never be <b>cleared</b> again, the server would skip over the <c>null</c> and silently keep the old
/// value. A UI offering "no value" would then be a click into the void <em>with</em> a success message.
/// The switches (following the pattern of <c>ClearGrade</c> on the class test) make the intent to clear
/// explicit; these tests check both directions: setting stays setting, clearing actually clears.
/// </summary>
public class PatchClearFieldTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static async Task<JsonElement> JsonAsync(HttpResponseMessage res)
    {
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static int? IntOrNull(JsonElement e, string prop) =>
        e.GetProperty(prop) is { ValueKind: JsonValueKind.Number } v ? v.GetInt32() : null;

    [Fact]
    public async Task Kind_Geburtsjahr_und_Klasse_lassen_sich_leeren()
    {
        var father = await TestApi.FatherAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = $"Clear-Kind {Guid.NewGuid():N}", birthYear = 2013, grade = 6, pin = "1111" }));

        // Gegenprobe: ein `null` allein ändert nichts (das ist die Semantik, auf der die Schalter aufsetzen).
        var untouched = await JsonAsync(await father.PatchAsJsonAsync($"/api/v1/supervisor/children/{childId}",
            new { birthYear = (int?)null, grade = (int?)null }));
        Assert.Equal(2013, IntOrNull(untouched, "birthYear"));
        Assert.Equal(6, IntOrNull(untouched, "grade"));

        var cleared = await JsonAsync(await father.PatchAsJsonAsync($"/api/v1/supervisor/children/{childId}",
            new { clearBirthYear = true, clearGrade = true }));
        Assert.Null(IntOrNull(cleared, "birthYear"));
        Assert.Null(IntOrNull(cleared, "grade"));

        // Setzen funktioniert weiterhin – der Schalter ist ein Zusatz, kein Ersatz.
        var reset = await JsonAsync(await father.PatchAsJsonAsync($"/api/v1/supervisor/children/{childId}",
            new { grade = 7 }));
        Assert.Equal(7, IntOrNull(reset, "grade"));
    }

    [Fact]
    public async Task Lueckentext_Uebersetzung_und_Wortpool_lassen_sich_leeren()
    {
        var creator = await TestApi.FatherAsync(factory);
        var id = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/cloze-texts", new
        {
            key = $"cz_clear_{Guid.NewGuid():N}",
            title = "Begrüßungen",
            sourceLanguage = "en",
            targetLanguage = "de",
            text = "Good {{1}}!",
            gaps = new[] { new { index = 1, answer = "morning" } },
            translation = "Guten Morgen!",
            wordBank = new[] { "morning", "evening" },
        }));

        // Gegenprobe: das geräumte Formularfeld käme als `null` an – und ließe beides stehen.
        var untouched = await JsonAsync(await creator.PatchAsJsonAsync($"/api/v1/creator/cloze-texts/{id}",
            new { translation = (string?)null, wordBank = (string[]?)null }));
        Assert.Equal("Guten Morgen!", untouched.GetProperty("translation").GetString());
        Assert.Equal(2, untouched.GetProperty("wordBank").GetArrayLength());

        var cleared = await JsonAsync(await creator.PatchAsJsonAsync($"/api/v1/creator/cloze-texts/{id}",
            new { clearTranslation = true, clearWordBank = true }));
        Assert.Equal(JsonValueKind.Null, cleared.GetProperty("translation").ValueKind);
        Assert.Equal(JsonValueKind.Null, cleared.GetProperty("wordBank").ValueKind);

        Assert.Equal("Guten Abend!", (await JsonAsync(await creator.PatchAsJsonAsync(
            $"/api/v1/creator/cloze-texts/{id}", new { translation = "Guten Abend!" })))
            .GetProperty("translation").GetString());

        // Schickt eine Oberfläche Wert *und* Schalter, gewinnt „leeren" (Reihenfolge im Controller).
        var both = await JsonAsync(await creator.PatchAsJsonAsync($"/api/v1/creator/cloze-texts/{id}",
            new { translation = "Egal", clearTranslation = true }));
        Assert.Equal(JsonValueKind.Null, both.GetProperty("translation").ValueKind);
    }

    [Fact]
    public async Task Fachlehrer_Profil_wird_wieder_fachneutral_und_werkunabhaengig()
    {
        var creator = await TestApi.FatherAsync(factory);
        var seriesId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = $"Clear-Reihe {Guid.NewGuid():N}", subjectName = "Englisch", sourceLanguage = "en", targetLanguage = "de" }));
        var subjectId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = $"Clear-Fach {Guid.NewGuid():N}" }));

        var profileId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/profiles", new
        {
            name = "Frau Meier",
            subjectId,
            subjectName = "Englisch",
            gradeMin = 5,
            gradeMax = 8,
            seriesId,
            schoolTypes = "Gymnasium",
        }));

        var cleared = await JsonAsync(await creator.PatchAsJsonAsync($"/api/v1/creator/profiles/{profileId}", new
        {
            clearSubject = true,
            clearSeries = true,
            clearGradeMin = true,
            clearGradeMax = true,
            schoolTypes = "None",
        }));
        Assert.Null(IntOrNull(cleared, "subjectId"));
        Assert.Null(IntOrNull(cleared, "seriesId"));
        Assert.Null(IntOrNull(cleared, "gradeMin"));
        Assert.Null(IntOrNull(cleared, "gradeMax"));
        // Der Fach-Name gehört zur Fach-Bindung und geht mit – sonst behauptete das Profil weiter ein Fach.
        Assert.Equal(JsonValueKind.Null, cleared.GetProperty("subjectName").ValueKind);
        Assert.Equal("None", cleared.GetProperty("schoolTypes").GetString());
    }

    [Fact]
    public async Task Lehrbuch_Reihenwechsel_verwirft_die_Unit_der_alten_Reihe()
    {
        var father = await TestApi.FatherAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = $"Buch-Kind {Guid.NewGuid():N}", pin = "1111" }));

        var oldSeries = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = $"Alt {Guid.NewGuid():N}", sourceLanguage = "en", targetLanguage = "de" }));
        var oldUnit = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{oldSeries}/units", new { label = "Unit 1", grade = 5 }));
        // Die neue Reihe hat bewusst KEINE Units: genau dann war der Wechsel vorher unmöglich, weil die
        // alte Unit gegen die neue Reihe geprüft wurde und dabei zwangsläufig durchfiel.
        var newSeries = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = $"Neu {Guid.NewGuid():N}", sourceLanguage = "en", targetLanguage = "de" }));

        var bookId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/children/{childId}/textbooks",
            new { title = "Access 5", seriesId = oldSeries, currentUnitId = oldUnit }));

        var switched = await JsonAsync(await father.PatchAsJsonAsync(
            $"/api/v1/supervisor/children/{childId}/textbooks/{bookId}",
            new { seriesId = newSeries, currentUnitId = (int?)null }));
        Assert.Equal(newSeries, IntOrNull(switched, "seriesId"));
        Assert.Null(IntOrNull(switched, "currentUnitId"));

        // Eine Unit aus einer fremden Reihe bleibt verboten – die Prüfung ist gelockert, nicht abgeschafft.
        var wrong = await father.PatchAsJsonAsync(
            $"/api/v1/supervisor/children/{childId}/textbooks/{bookId}", new { currentUnitId = oldUnit });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
    }

    [Fact]
    public async Task Lehrbuch_laesst_sich_aus_dem_Katalog_loesen()
    {
        var father = await TestApi.FatherAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = $"Katalog-Kind {Guid.NewGuid():N}", pin = "1111" }));
        var seriesId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = $"Reihe {Guid.NewGuid():N}", sourceLanguage = "en", targetLanguage = "de" }));
        var unitId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Unit 2" }));
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = $"Buch-Fach {Guid.NewGuid():N}" }));

        var bookId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/children/{childId}/textbooks",
            new { title = "Access 6", subjectId, subjectName = "Englisch", grade = 6, seriesId, currentUnitId = unitId }));

        var cleared = await JsonAsync(await father.PatchAsJsonAsync(
            $"/api/v1/supervisor/children/{childId}/textbooks/{bookId}",
            new { clearSeries = true, clearSubject = true, clearGrade = true }));
        Assert.Null(IntOrNull(cleared, "seriesId"));
        // Die Unit fällt mit der Reihe weg – ohne sie bezeichnet sie nichts.
        Assert.Null(IntOrNull(cleared, "currentUnitId"));
        Assert.Null(IntOrNull(cleared, "subjectId"));
        Assert.Null(IntOrNull(cleared, "grade"));
        Assert.Equal(JsonValueKind.Null, cleared.GetProperty("subjectName").ValueKind);
    }
}
