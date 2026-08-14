using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-13: a subject used to be the only catalog level without an owner - any creator could rename or delete
/// anyone else's "English". It now follows the textbook series' rule: reading and using stays open to every
/// creator, renaming and deleting belong to the owner. A seeded subject has no owner and is therefore
/// editable by <b>nobody</b> (fail-closed), which is the deliberate tightening of this story, not a gap.
/// </summary>
public class FachEigentumTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>Registers a second creator and logs them in – the stranger every check below needs.</summary>
    private async Task<HttpClient> ZweiterCreatorAsync(string pin)
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Fremder"), pin });
        res.EnsureSuccessStatusCode();
        var id = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return await TestApi.AdultAsync(factory, id, pin);
    }

    private static async Task<int> FachAsync(HttpClient creator) =>
        await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Eigentum-Fach") }));

    private static async Task AssertCodeAsync(HttpResponseMessage res, string code)
    {
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, body.GetProperty("code").GetString());
    }

    /// <summary>Reads the id of a seeded subject – it carries no owner and is the fail-closed case.</summary>
    private static async Task<int> SeedFachIdAsync(HttpClient creator)
    {
        var list = await creator.GetFromJsonAsync<JsonElement>("/api/v1/creator/subjects");
        var seed = list.EnumerateArray().First(s => s.GetProperty("name").GetString() == "Englisch");
        Assert.Equal(JsonValueKind.Null, seed.GetProperty("ownerAdultId").ValueKind);
        return seed.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Anlegen_MachtDenAufrufer_ZumEigentuemer()
    {
        var adult = await TestApi.AdultAsync(factory);

        var created = await (await adult.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Eigentum-Fach") })).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1, created.GetProperty("ownerAdultId").GetInt32());
        Assert.True(created.GetProperty("isMine").GetBoolean());
    }

    [Fact]
    public async Task Eigentuemer_DarfWeiterhinUmbenennenUndLoeschen()
    {
        var adult = await TestApi.AdultAsync(factory);
        var subjectId = await FachAsync(adult);

        var patch = await adult.PatchAsJsonAsync($"/api/v1/creator/subjects/{subjectId}",
            new { name = TestApi.UniqueName("Umbenannt") });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        Assert.True((await patch.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("isMine").GetBoolean());

        var delete = await adult.DeleteAsync($"/api/v1/creator/subjects/{subjectId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task FremderCreator_BekommtNotOwner_BeimUmbenennenUndLoeschen()
    {
        var adult = await TestApi.AdultAsync(factory);
        var subjectId = await FachAsync(adult);
        var fremder = await ZweiterCreatorAsync("2113");

        var patch = await fremder.PatchAsJsonAsync($"/api/v1/creator/subjects/{subjectId}", new { name = "Gekapert" });
        Assert.Equal(HttpStatusCode.Forbidden, patch.StatusCode);
        await AssertCodeAsync(patch, "not_owner");

        var delete = await fremder.DeleteAsync($"/api/v1/creator/subjects/{subjectId}");
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        await AssertCodeAsync(delete, "not_owner");

        // And nothing happened on the way: the name is the one the owner gave it.
        var after = await adult.GetFromJsonAsync<JsonElement>($"/api/v1/creator/subjects/{subjectId}");
        Assert.NotEqual("Gekapert", after.GetProperty("name").GetString());
    }

    [Fact]
    public async Task FremderCreator_DarfDasFachWeiterhinLesen()
    {
        var adult = await TestApi.AdultAsync(factory);
        var subjectId = await FachAsync(adult);
        var fremder = await ZweiterCreatorAsync("2114");

        // Reading stays global - that is the half of the catalog this story deliberately does not touch.
        var single = await fremder.GetAsync($"/api/v1/creator/subjects/{subjectId}");
        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
        var body = await single.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("ownerAdultId").GetInt32());
        Assert.False(body.GetProperty("isMine").GetBoolean());

        var list = await fremder.GetFromJsonAsync<JsonElement>("/api/v1/creator/subjects");
        Assert.Contains(list.EnumerateArray(), s => s.GetProperty("id").GetInt32() == subjectId);
    }

    /// <summary>
    /// The owner check runs <b>before</b> the in-use check, and that order is a privacy decision rather
    /// than a matter of taste: the 409 <c>subject_in_use</c> names a child's objectives and timetable, so
    /// answering it to a stranger would leak that another household plans against this subject. Swapping
    /// the two blocks in <c>SubjectsController.Delete</c> keeps every other test green, which is why this
    /// one exists.
    /// </summary>
    [Fact]
    public async Task FremderCreator_BekommtNotOwner_AuchWennDasFachBenutztIst()
    {
        var adult = await TestApi.AdultAsync(factory);
        var subjectId = await FachAsync(adult);

        // Make the subject undeletable for its own owner first - a timetable entry is the cheapest of the
        // two mandatory uses (pattern: FachLoeschenSperreTests).
        var entry = await adult.PostAsJsonAsync("/api/v1/supervisor/children/1/timetable",
            new { subjectId, dayOfWeek = "Wednesday", timeOfDay = "09:00" });
        Assert.Equal(HttpStatusCode.Created, entry.StatusCode);

        // The owner now gets the 409 - that is the proof the subject really is in use.
        var ownerDelete = await adult.DeleteAsync($"/api/v1/creator/subjects/{subjectId}");
        Assert.Equal(HttpStatusCode.Conflict, ownerDelete.StatusCode);
        await AssertCodeAsync(ownerDelete, "subject_in_use");

        // The stranger must not learn any of that: 403 first, and no word about the child's rows.
        var fremder = await ZweiterCreatorAsync("2116");
        var strangerDelete = await fremder.DeleteAsync($"/api/v1/creator/subjects/{subjectId}");
        Assert.Equal(HttpStatusCode.Forbidden, strangerDelete.StatusCode);
        await AssertCodeAsync(strangerDelete, "not_owner");
    }

    /// <summary>Creates a category under a subject and returns its id.</summary>
    private static async Task<int> ArtAsync(HttpClient creator, int subjectId) =>
        await TestApi.IdAsync(await creator.PostAsJsonAsync($"/api/v1/creator/subjects/{subjectId}/categories",
            new { name = TestApi.UniqueName("Art") }));

    /// <summary>
    /// B-157: a category has no owner of its own, it belongs to whoever owns its subject. B-13 closed the
    /// subject and left the level below it open - a stranger could rename the pre-filter list inside my
    /// subject.
    /// </summary>
    [Fact]
    public async Task FremderCreator_DarfDieArten_EinesFremdenFachs_NichtAendern()
    {
        var adult = await TestApi.AdultAsync(factory);
        var subjectId = await FachAsync(adult);
        var categoryId = await ArtAsync(adult, subjectId);
        var fremder = await ZweiterCreatorAsync("2117");

        var patch = await fremder.PatchAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/categories/{categoryId}", new { name = "Gekapert" });
        Assert.Equal(HttpStatusCode.Forbidden, patch.StatusCode);
        await AssertCodeAsync(patch, "not_owner");

        var delete = await fremder.DeleteAsync($"/api/v1/creator/subjects/{subjectId}/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        await AssertCodeAsync(delete, "not_owner");

        // Nothing happened on the way: the name is the one the owner gave it.
        var after = await adult.GetFromJsonAsync<JsonElement>(
            $"/api/v1/creator/subjects/{subjectId}/categories/{categoryId}");
        Assert.NotEqual("Gekapert", after.GetProperty("name").GetString());

        // Reading stays open - the counterpart to FremderCreator_DarfDasFachWeiterhinLesen. Without this,
        // pulling `List`/`Get` onto the owner later ("make it symmetric") would keep the suite green and
        // leave the study-plan pre-filter empty for every other creator.
        var single = await fremder.GetAsync($"/api/v1/creator/subjects/{subjectId}/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
        var list = await fremder.GetFromJsonAsync<JsonElement>($"/api/v1/creator/subjects/{subjectId}/categories");
        Assert.Contains(list.EnumerateArray(), c => c.GetProperty("id").GetInt32() == categoryId);

        // And the missing category under a foreign subject stays a 404: hoisting the owner check above the
        // category lookup ("pattern B-13") would silently turn this into a 403, and nothing else would fail.
        var fehlt = await fremder.PatchAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/categories/999999", new { name = "X" });
        Assert.Equal(HttpStatusCode.NotFound, fehlt.StatusCode);
    }

    /// <summary>
    /// The other half of decision 2, and the one a later "let us make it symmetric" refactor would break:
    /// <b>creating</b> a category stays open to everyone. Gating it would leave the seeded subjects - the
    /// only ones an ordinary user has - with a category axis nobody can extend, because their owner is null
    /// and <c>IsOwnedBy</c> is fail-closed.
    /// </summary>
    [Fact]
    public async Task JederCreator_DarfEineArt_AuchImFremdenUndImSeedFach_Anlegen()
    {
        var adult = await TestApi.AdultAsync(factory);
        var fremdesFach = await FachAsync(adult);
        var seedFach = await SeedFachIdAsync(adult);
        var fremder = await ZweiterCreatorAsync("2118");

        foreach (var subjectId in new[] { fremdesFach, seedFach })
        {
            var res = await fremder.PostAsJsonAsync($"/api/v1/creator/subjects/{subjectId}/categories",
                new { name = TestApi.UniqueName("Fremd-Art") });
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        }
    }

    /// <summary>
    /// The seeded subject has no owner, so its categories are editable by <b>nobody</b> - the same
    /// fail-closed rule as for the subject itself, one level down. This is the deliberate tightening of
    /// B-157 and it reaches further than B-13's four subjects: it covers every seeded category.
    /// </summary>
    [Fact]
    public async Task Arten_EinesSeedFachs_SindFuerJedenGesperrt()
    {
        var adult = await TestApi.AdultAsync(factory);
        var seedId = await SeedFachIdAsync(adult);
        var artId = (await adult.GetFromJsonAsync<JsonElement>($"/api/v1/creator/subjects/{seedId}/categories"))
            .EnumerateArray().First().GetProperty("id").GetInt32();
        var fremder = await ZweiterCreatorAsync("2119");

        foreach (var client in new[] { adult, fremder })
        {
            var patch = await client.PatchAsJsonAsync(
                $"/api/v1/creator/subjects/{seedId}/categories/{artId}", new { name = "Umbenannt" });
            Assert.Equal(HttpStatusCode.Forbidden, patch.StatusCode);
            await AssertCodeAsync(patch, "not_owner");

            var delete = await client.DeleteAsync($"/api/v1/creator/subjects/{seedId}/categories/{artId}");
            Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
            await AssertCodeAsync(delete, "not_owner");
        }
    }

    /// <summary>
    /// The owner keeps working unchanged - without this case the story would read as "categories are
    /// read-only now".
    /// </summary>
    [Fact]
    public async Task Eigentuemer_DarfSeineArten_WeiterhinAendernUndLoeschen()
    {
        var adult = await TestApi.AdultAsync(factory);
        var subjectId = await FachAsync(adult);
        var categoryId = await ArtAsync(adult, subjectId);

        var patch = await adult.PatchAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/categories/{categoryId}", new { name = TestApi.UniqueName("Neu") });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        var delete = await adult.DeleteAsync($"/api/v1/creator/subjects/{subjectId}/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    /// <summary>
    /// Acceptance criterion 8: deleting a category does <b>not</b> conflict, it only takes the assignment
    /// away - <c>Exercise.CategoryId</c> is optional. The class doc promises this since B-157 and the
    /// contract document carries the sentence, so it needs a test rather than a claim.
    /// </summary>
    [Fact]
    public async Task Eine_Benutzte_Art_Zu_Loeschen_Nimmt_Der_Uebung_Nur_Die_Zuordnung()
    {
        var adult = await TestApi.AdultAsync(factory);
        var subjectId = await FachAsync(adult);
        var categoryId = await ArtAsync(adult, subjectId);

        var seriesId = await TestApi.IdAsync(await adult.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Reihe"), subjectId }));
        var unitId = await TestApi.IdAsync(await adult.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = TestApi.UniqueName("Unit") }));
        // `arithmetic` with the config shape ExerciseMetadataTests already proves - the point here is the
        // category assignment, not the exercise type.
        var basePath = $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/arithmetic";
        var exerciseId = await TestApi.IdAsync(await adult.PostAsJsonAsync(basePath, new
        {
            title = TestApi.UniqueName("Uebung"),
            orderIndex = 1,
            rewardPoints = 10,
            categoryId,
            config = new { problems = new[] { new { prompt = "1 + 1", answer = 2, tolerance = 0 } } },
        }));

        // The assignment has to EXIST before its loss means anything (B-171). Drop `CategoryId` in the
        // creation path and the check below stays green on its own - it would then be a statement about the
        // starting state. Reading the element rather than calling GetInt32() only buys the message: a typed
        // accessor on null is red too, but as an exception instead of an expected/actual pair.
        var vorher = await adult.GetFromJsonAsync<JsonElement>($"{basePath}/{exerciseId}");
        var zuordnung = vorher.GetProperty("categoryId");
        Assert.Equal(JsonValueKind.Number, zuordnung.ValueKind);
        Assert.Equal(categoryId, zuordnung.GetInt32());

        var delete = await adult.DeleteAsync($"/api/v1/creator/subjects/{subjectId}/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var exercise = await adult.GetFromJsonAsync<JsonElement>($"{basePath}/{exerciseId}");
        Assert.Equal(JsonValueKind.Null, exercise.GetProperty("categoryId").ValueKind);
    }

    /// <summary>
    /// The trap the estimate named: a helper that reads only <c>OwnerAdultId</c> answers the same for "no
    /// such subject" and "nobody owns it". A write against a missing subject must stay a 404, not become a
    /// 403 - otherwise the API tells a stranger that a subject id is merely forbidden rather than absent.
    /// </summary>
    [Fact]
    public async Task Ein_Nicht_Existierendes_Fach_Bleibt_404_Und_Wird_Nicht_Zu_403()
    {
        var adult = await TestApi.AdultAsync(factory);

        var patch = await adult.PatchAsJsonAsync("/api/v1/creator/subjects/999999/categories/1", new { name = "X" });
        Assert.Equal(HttpStatusCode.NotFound, patch.StatusCode);

        var delete = await adult.DeleteAsync("/api/v1/creator/subjects/999999/categories/1");
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    /// <summary>
    /// The seeded subject belongs to nobody, so nobody may change it - not even the seeded father, who is
    /// the creator closest to it. Without this case the fail-closed rule would only be a comment.
    /// </summary>
    [Fact]
    public async Task SeedFach_IstFuerJedenCreator_Gesperrt()
    {
        var adult = await TestApi.AdultAsync(factory);
        var seedId = await SeedFachIdAsync(adult);
        var fremder = await ZweiterCreatorAsync("2115");

        foreach (var client in new[] { adult, fremder })
        {
            var patch = await client.PatchAsJsonAsync($"/api/v1/creator/subjects/{seedId}", new { name = "Englisch neu" });
            Assert.Equal(HttpStatusCode.Forbidden, patch.StatusCode);
            await AssertCodeAsync(patch, "not_owner");

            var delete = await client.DeleteAsync($"/api/v1/creator/subjects/{seedId}");
            Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
            await AssertCodeAsync(delete, "not_owner");
        }

        // isMine must not become true just because the caller has no owner either (null == null would).
        var seed = await adult.GetFromJsonAsync<JsonElement>($"/api/v1/creator/subjects/{seedId}");
        Assert.False(seed.GetProperty("isMine").GetBoolean());
    }
}
