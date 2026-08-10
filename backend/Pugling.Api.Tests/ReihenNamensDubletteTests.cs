using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-133. The guard from B-124 compares slug against slug, but the slug freezes on rename - so after one
/// rename two series could carry the same display name, which is exactly what that guard's own comment
/// promises to prevent.
/// </summary>
public class ReihenNamensDubletteTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static string Eindeutig(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..16];

    private static async Task<(HttpClient Client, int Id)> ReiheAsync(PuglingWebAppFactory f, string name)
    {
        var client = await TestApi.AdultAsync(f);
        var id = await TestApi.IdAsync(await client.PostAsJsonAsync("/api/v1/creator/textbook-series", new { name }));
        return (client, id);
    }

    private static async Task<string?> CodeOfAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();

    /// <summary>The four steps from the story, in order - this is the path that produced the duplicate.</summary>
    [Fact]
    public async Task Nach_Umbenennen_Ist_Der_Alte_Anzeigename_Nicht_Neu_Vergebbar()
    {
        var alt = Eindeutig("Access");
        var neu = Eindeutig("GreenLine");
        var (client, id) = await ReiheAsync(factory, alt);

        var umbenannt = await client.PatchAsJsonAsync($"/api/v1/creator/textbook-series/{id}", new { name = neu });
        umbenannt.EnsureSuccessStatusCode();

        // The slug still reads like the old name, so creating under the NEW name would miss it and add a
        // second series carrying the same display name.
        var zweite = await client.PostAsJsonAsync("/api/v1/creator/textbook-series", new { name = neu });

        Assert.Equal(HttpStatusCode.Conflict, zweite.StatusCode);
        Assert.Equal("duplicate_textbook_series", await CodeOfAsync(zweite));
    }

    [Fact]
    public async Task Umbenennen_Auf_Einen_Vergebenen_Anzeigenamen_Wird_Abgewiesen()
    {
        var ersterName = Eindeutig("Erste");
        var (client, _) = await ReiheAsync(factory, ersterName);
        var zweiteId = await TestApi.IdAsync(await client.PostAsJsonAsync(
            "/api/v1/creator/textbook-series", new { name = Eindeutig("Zweite") }));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/creator/textbook-series/{zweiteId}", new { name = ersterName });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("duplicate_textbook_series", await CodeOfAsync(response));
    }

    /// <summary>
    /// The NOCASE collation from B-128 carries here: two spellings are ONE display name.
    /// <para>
    /// Slug and name have to be decoupled first, and that is the whole point of the setup. A plain
    /// upper-cased name derives the <b>same</b> slug (slugs are lowercased), so the older slug guard would
    /// answer and the name comparison would never run - the case would be green with or without the
    /// collation and would prove nothing.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Auch_Eine_Andere_Schreibweise_Gilt_Als_Derselbe_Anzeigename()
    {
        var client = await TestApi.AdultAsync(factory);

        // Erste Reihe: unter einem Namen anlegen, dann umbenennen - danach ist ihr Slug eingefroren und
        // zeigt nicht mehr auf ihren Anzeigenamen.
        var tarnName = Eindeutig("Tarnung");
        var zielName = Eindeutig("Zielname");
        var ersteId = await TestApi.IdAsync(await client.PostAsJsonAsync(
            "/api/v1/creator/textbook-series", new { name = tarnName }));
        (await client.PatchAsJsonAsync($"/api/v1/creator/textbook-series/{ersteId}",
            new { name = zielName })).EnsureSuccessStatusCode();

        var zweiteId = await TestApi.IdAsync(await client.PostAsJsonAsync(
            "/api/v1/creator/textbook-series", new { name = Eindeutig("Andere") }));

        // Der Slug von `zielName` ist frei - nur der Namensvergleich kann das noch abfangen, und nur
        // dank NOCASE trotz anderer Schreibweise.
        var response = await client.PatchAsJsonAsync(
            $"/api/v1/creator/textbook-series/{zweiteId}", new { name = zielName.ToUpperInvariant() });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("duplicate_textbook_series", await CodeOfAsync(response));
    }

    /// <summary>
    /// The mirror image of the four-step path, and the one the first review of this sprint found missing:
    /// a slug hit whose display name has since changed must NOT hand the row back. Without the guard this
    /// answers 200 with a series of a different name, and a catalog agent hangs its units off the wrong one.
    /// </summary>
    [Fact]
    public async Task Slug_Treffer_Mit_Abweichendem_Namen_Liefert_Nicht_Still_Die_Falsche_Reihe()
    {
        var alt = Eindeutig("Access");
        var neu = Eindeutig("GreenLine");
        var (client, id) = await ReiheAsync(factory, alt);
        (await client.PatchAsJsonAsync($"/api/v1/creator/textbook-series/{id}",
            new { name = neu })).EnsureSuccessStatusCode();

        // Der ALTE Name: sein Slug trifft die Reihe weiter, ihr Anzeigename ist aber inzwischen ein anderer.
        var response = await client.PostAsJsonAsync("/api/v1/creator/textbook-series", new { name = alt });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("duplicate_textbook_series", await CodeOfAsync(response));
    }

    /// <summary>
    /// Pins decision 2 of B-133: the display name is unique <b>across creators</b>, not per owner. Without
    /// a second account a switch to `s.OwnerAdultId == fid` would not turn a single test red.
    /// </summary>
    [Fact]
    public async Task Ein_Fremder_Creator_Kann_Den_Namen_Nicht_Erneut_Vergeben()
    {
        var (eigner, ersteId) = await ReiheAsync(factory, Eindeutig("Tarnung"));
        var zielName = Eindeutig("Geteilt");

        // Umbenennen entkoppelt Name und Slug. Ohne das griffe beim gleichen Namen der idempotente
        // Slug-Treffer und antwortete zu Recht mit 200 - die Namensprüfung waere nie befragt, und der
        // Fall belegte nichts ueber ihre Reichweite.
        (await eigner.PatchAsJsonAsync($"/api/v1/creator/textbook-series/{ersteId}",
            new { name = zielName })).EnsureSuccessStatusCode();

        var fremdeId = await TestApi.IdAsync(await eigner.PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = "Fremder Creator", pin = "6502" }));
        var fremder = await TestApi.AdultAsync(factory, fremdeId, "6502");

        var response = await fremder.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = zielName });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("duplicate_textbook_series", await CodeOfAsync(response));
    }

    /// <summary>
    /// The counter-check, and it is the more important half: creating stays idempotent over the slug.
    /// The same name returns the same series - no 409, no duplicate.
    /// </summary>
    [Fact]
    public async Task Zweimal_Derselbe_Name_Liefert_Weiterhin_Dieselbe_Reihe()
    {
        var name = Eindeutig("Idempotent");
        var (client, id) = await ReiheAsync(factory, name);

        var nochmal = await client.PostAsJsonAsync("/api/v1/creator/textbook-series", new { name });

        Assert.Equal(HttpStatusCode.OK, nochmal.StatusCode);
        Assert.Equal(id, await TestApi.IdAsync(nochmal));
    }
}
