using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-128. The catalog search must find a publisher or a series without the caller guessing the
/// capitalization somebody else chose when creating it.
/// <para>
/// The lowercase case is deliberately included even though it passed before: the slug is lowercase by
/// derivation and caught it by accident, and that accident is what hid the gap. A test that was already
/// green has to be labelled as such, otherwise it later reads like a fixed defect.
/// </para>
/// </summary>
public class KatalogSucheCaseTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static async Task<(HttpClient Client, string Name)> VerlagAsync(PuglingWebAppFactory f)
    {
        var client = await TestApi.AdultAsync(f);
        var name = $"Klett{Guid.NewGuid():N}"[..12];
        // Checked, not fired and forgotten: a failed create would make a "does not find it" assertion
        // green for the wrong reason.
        (await client.PostAsJsonAsync("/api/v1/creator/publishers", new { name })).EnsureSuccessStatusCode();
        return (client, name);
    }

    /// <summary>
    /// The names found, not their number: the fixture database is shared across the cases of this class,
    /// and they all create "Klett…" publishers. A count would make each case depend on its siblings.
    /// </summary>
    private static async Task<List<string>> TrefferAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return [.. (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray()
            .Select(e => e.GetProperty("name").GetString() ?? "")];
    }

    [Theory]
    [InlineData(false)] // as created - was green before the fix as well
    [InlineData(true)]  // all caps - exactly the case that failed
    public async Task Verlagssuche_Findet_Unabhaengig_Von_Der_Schreibweise(bool upper)
    {
        var (client, name) = await VerlagAsync(factory);
        var suche = upper ? name.ToUpperInvariant() : name;

        Assert.Contains(name, await TrefferAsync(client, $"/api/v1/creator/publishers?search={suche}"));
    }

    [Fact]
    public async Task Verlagssuche_Findet_Auch_Mitten_Im_Wort_Mit_Grossbuchstaben()
    {
        var (client, name) = await VerlagAsync(factory);
        // "Lett" instead of "Klett": not at a word start, and the lowercase slug does not catch it.
        var suche = name[1..5].ToUpperInvariant();

        Assert.Contains(name, await TrefferAsync(client, $"/api/v1/creator/publishers?search={suche}"));
    }

    /// <summary>
    /// The only non-trivial logic in <c>SearchPattern</c>: a wildcard typed into the search box is a
    /// literal. Without the escaping "%" collapses the pattern to "%%%" and matches every row, and "_"
    /// matches any single character - and nothing else in the suite would notice, because every other
    /// search term is plain text. Both characters get a case, and the escape character is exercised by
    /// them both (a swapped replacement order turns them red).
    /// </summary>
    [Theory]
    [InlineData("%", "%25")] // %25 is the URL encoding of the percent sign
    [InlineData("_", "_")]
    public async Task Ein_Platzhalterzeichen_Im_Suchbegriff_Ist_Ein_Zeichen(string zeichen, string kodiert)
    {
        var client = await TestApi.AdultAsync(factory);
        var mitZeichen = $"Rab{zeichen}{Guid.NewGuid():N}"[..12];
        var ohneZeichen = $"Rabx{Guid.NewGuid():N}"[..12];
        (await client.PostAsJsonAsync("/api/v1/creator/publishers", new { name = mitZeichen })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/creator/publishers", new { name = ohneZeichen })).EnsureSuccessStatusCode();

        // take=500: with broken escaping the hit set is the WHOLE publisher table - exactly the situation
        // in which the default page size could hide the counter-example.
        var treffer = await TrefferAsync(client, $"/api/v1/creator/publishers?take=500&search=Rab{kodiert}");

        Assert.Contains(mitZeichen, treffer);
        Assert.DoesNotContain(ohneZeichen, treffer);
    }

    [Fact]
    public async Task Reihensuche_Findet_Unabhaengig_Von_Der_Schreibweise()
    {
        var client = await TestApi.AdultAsync(factory);
        var name = $"Green{Guid.NewGuid():N}"[..12];
        await client.PostAsJsonAsync("/api/v1/creator/textbook-series", new { name });

        Assert.Contains(name, await TrefferAsync(client,
            $"/api/v1/creator/textbook-series?search={name.ToUpperInvariant()}"));
    }
}
