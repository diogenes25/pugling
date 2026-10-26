using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// Verifies the exercise-independent student profile (foundation for a later study plan generator):
/// the personal fields on the child (gender/interests/note) and the textbook sub-resource
/// (CRUD + ownership + cascade on child deletion).
/// </summary>
public class StudentProfileTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Vater_KannProfilfelder_AnlegenUndAendern()
    {
        var father = await TestApi.AdultAsync(factory);

        var created = await (await father.PostAsJsonAsync("/api/v1/supervisor/children", new
        {
            name = "Profil-Kind",
            pin = "8100",
            grade = 9,
            gender = "Male",
            interests = new[] { "Brawl Stars", "Pokémon" },
            profileNotes = "Mag kurze Aufgaben.",
        })).Content.ReadFromJsonAsync<JsonElement>();
        var childId = created.GetProperty("id").GetInt32();

        Assert.Equal("Male", created.GetProperty("gender").GetString());
        Assert.Equal("Mag kurze Aufgaben.", created.GetProperty("profileNotes").GetString());
        Assert.Equal(["Brawl Stars", "Pokémon"],
            created.GetProperty("interests").EnumerateArray().Select(i => i.GetString()!).ToArray());

        // GET returns the same profile fields.
        var fetched = await father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/children/{childId}");
        Assert.Equal("Male", fetched.GetProperty("gender").GetString());
        Assert.Equal(2, fetched.GetProperty("interests").GetArrayLength());

        // PATCH replaces the interest list (reassignment, no in-place mutation).
        var patched = await (await father.PatchAsJsonAsync($"/api/v1/supervisor/children/{childId}", new
        {
            interests = new[] { "Fußball" },
            gender = "Diverse",
        })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Diverse", patched.GetProperty("gender").GetString());
        Assert.Equal(["Fußball"],
            patched.GetProperty("interests").EnumerateArray().Select(i => i.GetString()!).ToArray());
    }

    [Fact]
    public async Task Vater_KannLehrbuch_Anlegen_Lesen_Aendern_Loeschen()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Buch-Kind", pin = "8101" }));

        // Create
        var created = await (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/textbooks", new
        {
            title = "Green Line 3",
            subjectName = "Englisch",
            grade = 9,
            publisher = "Klett",
            currentChapter = "Unit 4 – Past Tense",
        })).Content.ReadFromJsonAsync<JsonElement>();
        var bookId = created.GetProperty("id").GetInt32();
        Assert.Equal("Green Line 3", created.GetProperty("title").GetString());

        // The list contains the book
        var list = await father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/children/{childId}/textbooks");
        Assert.Contains(bookId, list.EnumerateArray().Select(b => b.GetProperty("id").GetInt32()));

        // Read a single one
        var single = await father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/children/{childId}/textbooks/{bookId}");
        Assert.Equal("Unit 4 – Past Tense", single.GetProperty("currentChapter").GetString());

        // PATCH updates the position in the book
        var patched = await (await father.PatchAsJsonAsync(
            $"/api/v1/supervisor/children/{childId}/textbooks/{bookId}", new { currentChapter = "Unit 5 – Future" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unit 5 – Future", patched.GetProperty("currentChapter").GetString());

        // Delete → deleting again is a 404
        (await father.DeleteAsync($"/api/v1/supervisor/children/{childId}/textbooks/{bookId}")).EnsureSuccessStatusCode();
        var again = await father.DeleteAsync($"/api/v1/supervisor/children/{childId}/textbooks/{bookId}");
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
    }

    /// <summary>
    /// A series with a publisher and two units: one with a grade, one without. Returns the ids the B-64
    /// tests link a book to.
    /// </summary>
    private static async Task<(int seriesId, int unitWithGrade, int unitWithoutGrade, string publisherName)> CatalogAsync(HttpClient father)
    {
        var publisherName = TestApi.UniqueName("Katalog-Verlag");
        var publisherId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/publishers", new { name = publisherName }));
        var seriesId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/textbook-series", new
        {
            name = TestApi.UniqueName("Katalog-Reihe"),
            publisherId,
            subjectName = (string?)null,
            subjectId = (int?)null,
            schoolTypes = (object?)null,
            sourceLanguage = (string?)null,
            targetLanguage = (string?)null,
            notes = (string?)null,
        }));
        var withGrade = await TestApi.IdAsync(await father.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units",
            new { label = "Band 7, Unit 1", grade = 7, orderIndex = 1 }));
        var withoutGrade = await TestApi.IdAsync(await father.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units",
            new { label = "Werkstatt", orderIndex = 2 }));
        return (seriesId, withGrade, withoutGrade, publisherName);
    }

    /// <summary>
    /// B-64 AK 4: once the book points into the catalog, publisher and grade come from there – not from the
    /// free text typed next to it. A unit without a grade leaves the typed grade standing.
    /// </summary>
    [Fact]
    public async Task Lehrbuch_MitReiheUndUnit_ZeigtVerlagUndKlasseAusDemKatalog()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Katalog-Kind", pin = "8102" }));
        var (seriesId, withGrade, withoutGrade, publisherName) = await CatalogAsync(father);
        var url = $"/api/v1/supervisor/children/{childId}/textbooks";

        var linked = await (await father.PostAsJsonAsync(url, new
        {
            title = "Access 7",
            subjectName = (string?)null,
            grade = 9,
            publisher = "Freitext-Verlag",
            seriesId,
            currentUnitId = withGrade,
        })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(publisherName, linked.GetProperty("publisher").GetString());
        Assert.Equal(7, linked.GetProperty("grade").GetInt32());

        // The list projects the same way (one projection for all reads).
        var listed = (await father.GetFromJsonAsync<JsonElement>(url)).EnumerateArray()
            .Single(b => b.GetProperty("id").GetInt32() == linked.GetProperty("id").GetInt32());
        Assert.Equal(publisherName, listed.GetProperty("publisher").GetString());

        // Unit without a grade: the catalog has no value, so the typed one is the only information and stays.
        var noGrade = await (await father.PostAsJsonAsync(url, new
        {
            title = "Access Werkstatt",
            subjectName = (string?)null,
            grade = 9,
            publisher = "Freitext-Verlag",
            seriesId,
            currentUnitId = withoutGrade,
        })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(9, noGrade.GetProperty("grade").GetInt32());
        Assert.Equal(publisherName, noGrade.GetProperty("publisher").GetString());

        // Series without a publisher: same rule for the publisher - no catalog value, the typed one stays.
        var bareSeries = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/textbook-series", new
        {
            name = TestApi.UniqueName("Reihe-ohne-Verlag"),
            publisherId = (int?)null,
            subjectName = (string?)null,
            subjectId = (int?)null,
            schoolTypes = (object?)null,
            sourceLanguage = (string?)null,
            targetLanguage = (string?)null,
            notes = (string?)null,
        }));
        var noPublisher = await (await father.PostAsJsonAsync(url, new
        {
            title = "Ohne Verlag",
            subjectName = (string?)null,
            publisher = "Freitext-Verlag",
            seriesId = bareSeries,
        })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Freitext-Verlag", noPublisher.GetProperty("publisher").GetString());
    }

    /// <summary>
    /// B-64 AK 5: unlinking must not lose what was typed. The catalog value only overlays the stored free text
    /// while the link exists; <c>clearUnit</c> brings back the grade, <c>clearSeries</c> the publisher.
    /// </summary>
    [Fact]
    public async Task Lehrbuch_Entkoppeln_BringtDenFreitextZurueck()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Entkoppel-Kind", pin = "8103" }));
        var (seriesId, withGrade, _, publisherName) = await CatalogAsync(father);
        var url = $"/api/v1/supervisor/children/{childId}/textbooks";

        // Typed first, linked afterwards - the usual order when a father catalogues an existing entry.
        var bookId = await TestApi.IdAsync(await father.PostAsJsonAsync(url, new
        {
            title = "Mein Buch",
            subjectName = (string?)null,
            grade = 9,
            publisher = "Freitext-Verlag",
        }));
        var linked = await (await father.PatchAsJsonAsync($"{url}/{bookId}", new { seriesId, currentUnitId = withGrade }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(publisherName, linked.GetProperty("publisher").GetString());
        Assert.Equal(7, linked.GetProperty("grade").GetInt32());

        // Writing the grade while linked changes the stored fallback, not the shown catalog value - and the
        // write must not be dropped: it is what shows once the unit is gone.
        var whileLinked = await (await father.PatchAsJsonAsync($"{url}/{bookId}", new { grade = 8 }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(7, whileLinked.GetProperty("grade").GetInt32());

        var unitCleared = await (await father.PatchAsJsonAsync($"{url}/{bookId}", new { clearUnit = true }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(8, unitCleared.GetProperty("grade").GetInt32());
        Assert.Equal(publisherName, unitCleared.GetProperty("publisher").GetString());

        var seriesCleared = await (await father.PatchAsJsonAsync($"{url}/{bookId}", new { clearSeries = true }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Freitext-Verlag", seriesCleared.GetProperty("publisher").GetString());
        Assert.Equal(8, seriesCleared.GetProperty("grade").GetInt32());
    }

    [Fact]
    public async Task Lehrbuch_MitUngueltigemFach_Ist400()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Fach-Kind", pin = "8102" }));

        var res = await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/textbooks",
            new { title = "Irgendein Buch", subjectId = 999999 });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Vater_KommtNichtAnLehrbuecherFremderKinder_403Oder404()
    {
        var father = await TestApi.AdultAsync(factory);

        var res = await father.GetAsync("/api/v1/supervisor/children/999999/textbooks");

        Assert.True(res.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LoeschenDesKindes_RaeumtLehrbuecherMitAb()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Cascade-Kind", pin = "8103" }));
        var bookId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/children/{childId}/textbooks", new { title = "Wegwerf-Buch" }));

        (await father.DeleteAsync($"/api/v1/supervisor/children/{childId}")).EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.False(db.Textbooks.Any(t => t.Id == bookId));
    }
}
