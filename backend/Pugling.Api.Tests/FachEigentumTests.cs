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
