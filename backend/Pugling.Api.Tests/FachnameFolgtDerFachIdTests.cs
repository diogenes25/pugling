using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-142. Three resources carry the pair <c>SubjectId</c> + <c>SubjectName</c>, and each of them used to
/// set the two independently - so a <c>PATCH</c> that only moved the id left the old name standing, and
/// the row claimed one subject by id and another by name.
/// <para>
/// <c>SubjectName</c> is the fallback for <em>uncatalogued</em> works. The moment an id is present the
/// catalog is the truth, so the name follows the id rather than travelling on its own. Each case here
/// switches the subject and asserts the name moved along; the last one guards the other direction - a row
/// without an id keeps its free text.
/// </para>
/// </summary>
public class FachnameFolgtDerFachIdTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>Two subjects to switch between, uniquely named (the catalog is shared across the class).</summary>
    private static async Task<(int alt, string altName, int neu, string neuName)> ZweiFaecherAsync(HttpClient creator)
    {
        var altName = TestApi.UniqueName("Englisch");
        var neuName = TestApi.UniqueName("Französisch");
        var alt = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects", new { name = altName }));
        var neu = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects", new { name = neuName }));
        return (alt, altName, neu, neuName);
    }

    private static async Task<string?> FachnameAsync(HttpClient client, string url) =>
        (await client.GetFromJsonAsync<JsonElement>(url)).GetProperty("subjectName").GetString();

    [Fact]
    public async Task Lehrwerkreihe_Fachname_Folgt_Der_Fach_Id()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (alt, altName, neu, neuName) = await ZweiFaecherAsync(creator);

        var id = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Reihe"), subjectId = alt }));
        Assert.Equal(altName, await FachnameAsync(creator, $"/api/v1/creator/textbook-series/{id}"));

        // Only the id travels - exactly the request the client library and the agent send, and the one the
        // frontend used to compensate for.
        (await creator.PatchAsJsonAsync($"/api/v1/creator/textbook-series/{id}", new { subjectId = neu }))
            .EnsureSuccessStatusCode();
        Assert.Equal(neuName, await FachnameAsync(creator, $"/api/v1/creator/textbook-series/{id}"));

        // The case the controller comment names as its whole reason for deriving against the RESULT rather
        // than the payload: a free-text name sent onto a row that already carries an id. A payload-shaped
        // guard would let this through, and this assertion is what keeps anyone from rewriting it that way.
        (await creator.PatchAsJsonAsync($"/api/v1/creator/textbook-series/{id}", new { subjectName = "Handgemalt" }))
            .EnsureSuccessStatusCode();
        Assert.Equal(neuName, await FachnameAsync(creator, $"/api/v1/creator/textbook-series/{id}"));

        // And the switch still beats the value when a form sends both - "clear" wins, and the derivation
        // running afterwards must not refill what it just emptied.
        (await creator.PatchAsJsonAsync($"/api/v1/creator/textbook-series/{id}",
            new { subjectId = alt, clearSubject = true })).EnsureSuccessStatusCode();
        Assert.Null(await FachnameAsync(creator, $"/api/v1/creator/textbook-series/{id}"));
    }

    [Fact]
    public async Task Fachlehrer_Profil_Fachname_Folgt_Der_Fach_Id()
    {
        var creator = await TestApi.AdultAsync(factory);
        var (alt, altName, neu, neuName) = await ZweiFaecherAsync(creator);

        var id = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/profiles",
            new { name = TestApi.UniqueName("Fachlehrer"), subjectId = alt }));
        Assert.Equal(altName, await FachnameAsync(creator, $"/api/v1/creator/profiles/{id}"));

        (await creator.PatchAsJsonAsync($"/api/v1/creator/profiles/{id}", new { subjectId = neu }))
            .EnsureSuccessStatusCode();
        Assert.Equal(neuName, await FachnameAsync(creator, $"/api/v1/creator/profiles/{id}"));
    }

    [Fact]
    public async Task Lehrbuch_Des_Kindes_Fachname_Folgt_Der_Fach_Id()
    {
        var father = await TestApi.AdultAsync(factory);
        var (alt, altName, neu, neuName) = await ZweiFaecherAsync(father);

        var id = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children/1/textbooks",
            new { title = TestApi.UniqueName("Buch"), subjectId = alt }));
        Assert.Equal(altName, await FachnameAsync(father, $"/api/v1/supervisor/children/1/textbooks/{id}"));

        (await father.PatchAsJsonAsync($"/api/v1/supervisor/children/1/textbooks/{id}", new { subjectId = neu }))
            .EnsureSuccessStatusCode();
        Assert.Equal(neuName, await FachnameAsync(father, $"/api/v1/supervisor/children/1/textbooks/{id}"));
    }

    /// <summary>
    /// B-145, the same invariant from the subject's side: renaming the subject has to reach every copy of
    /// its name, not wait for the next PATCH of each row to heal it. Red if any of the three tables is left
    /// out (its own assertion names which), if the update runs without the id filter (the bystander on the
    /// other subject would be renamed too), or if it matches on the old NAME instead of the id (the
    /// free-text twin would follow).
    /// </summary>
    [Fact]
    public async Task Umbenanntes_Fach_Zieht_Den_Namen_In_Alle_Drei_Kopien_Nach()
    {
        var adult = await TestApi.AdultAsync(factory);
        var (fach, altName, anderes, anderesName) = await ZweiFaecherAsync(adult);

        var reihe = await TestApi.IdAsync(await adult.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Reihe"), subjectId = fach }));
        var profil = await TestApi.IdAsync(await adult.PostAsJsonAsync("/api/v1/creator/profiles",
            new { name = TestApi.UniqueName("Fachlehrer"), subjectId = fach }));
        var buch = await TestApi.IdAsync(await adult.PostAsJsonAsync("/api/v1/supervisor/children/1/textbooks",
            new { title = TestApi.UniqueName("Buch"), subjectId = fach }));
        var unbeteiligt = await TestApi.IdAsync(await adult.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Reihe"), subjectId = anderes }));
        var freitextZwilling = await TestApi.IdAsync(await adult.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Reihe"), subjectName = altName }));
        // A series of ANOTHER creator on the same subject: the name is a cached catalog value, not the row
        // owner's data. Red if the update is narrowed to the renaming creator's rows.
        var fremder = await FremderCreatorAsync();
        var fremdeReihe = await TestApi.IdAsync(await fremder.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Reihe"), subjectId = fach }));

        var neuName = TestApi.UniqueName("Französisch (Gymnasium)");
        (await adult.PatchAsJsonAsync($"/api/v1/creator/subjects/{fach}", new { name = neuName }))
            .EnsureSuccessStatusCode();

        Assert.Equal(neuName, await FachnameAsync(adult, $"/api/v1/creator/textbook-series/{reihe}"));
        Assert.Equal(neuName, await FachnameAsync(adult, $"/api/v1/creator/profiles/{profil}"));
        Assert.Equal(neuName, await FachnameAsync(adult, $"/api/v1/supervisor/children/1/textbooks/{buch}"));
        Assert.Equal(anderesName, await FachnameAsync(adult, $"/api/v1/creator/textbook-series/{unbeteiligt}"));
        Assert.Equal(altName, await FachnameAsync(adult, $"/api/v1/creator/textbook-series/{freitextZwilling}"));
        Assert.Equal(neuName, await FachnameAsync(fremder, $"/api/v1/creator/textbook-series/{fremdeReihe}"));
    }

    /// <summary>A second, freshly registered creator (same route as FachEigentumTests uses).</summary>
    private async Task<HttpClient> FremderCreatorAsync()
    {
        const string pin = "3131";
        var res = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Fremder"), pin });
        res.EnsureSuccessStatusCode();
        var id = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return await TestApi.AdultAsync(factory, id, pin);
    }

    /// <summary>
    /// The other direction, and the reason the rule is "follows the id" and not "always comes from the
    /// catalog": a row <b>without</b> an id is exactly the uncatalogued case the free text exists for. It
    /// must survive untouched - otherwise this fix would delete the fallback it is built around.
    /// </summary>
    [Fact]
    public async Task Ohne_Fach_Id_Bleibt_Der_Freitext_Stehen()
    {
        var creator = await TestApi.AdultAsync(factory);
        var freitext = TestApi.UniqueName("Nur-Freitext");

        var id = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Reihe"), subjectName = freitext }));
        Assert.Equal(freitext, await FachnameAsync(creator, $"/api/v1/creator/textbook-series/{id}"));

        // A PATCH on something else must not touch it either.
        (await creator.PatchAsJsonAsync($"/api/v1/creator/textbook-series/{id}", new { notes = "Randnotiz" }))
            .EnsureSuccessStatusCode();
        Assert.Equal(freitext, await FachnameAsync(creator, $"/api/v1/creator/textbook-series/{id}"));

        // And clearing still clears both halves together.
        (await creator.PatchAsJsonAsync($"/api/v1/creator/textbook-series/{id}", new { clearSubject = true }))
            .EnsureSuccessStatusCode();
        Assert.Null(await FachnameAsync(creator, $"/api/v1/creator/textbook-series/{id}"));
    }
}
