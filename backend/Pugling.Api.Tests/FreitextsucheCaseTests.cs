using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-135. The six free-text searches B-128 did not reach. Same defect class as
/// <see cref="KatalogSucheCaseTests"/>: EF maps <c>string.Contains</c> to SQLite's byte-exact
/// <c>instr()</c>, so a search box only finds what the caller spells the way somebody else typed it.
/// <para>
/// Every case searches with <b>upper case</b> for a row created in mixed case, and asserts by
/// <em>name</em> rather than by count - the fixture database is shared across the class, so a count
/// would make each case depend on its siblings (the lesson already written down in
/// <see cref="KatalogSucheCaseTests"/>).
/// </para>
/// </summary>
public class FreitextsucheCaseTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>A distinctive mixed-case term; upper-casing it is what the searches are probed with.</summary>
    private static string Begriff(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..12];

    private static async Task<JsonElement> ListeAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>Collects one string property out of a JSON array response.</summary>
    private static async Task<List<string>> TrefferAsync(HttpClient client, string url, string feld) =>
        [.. (await ListeAsync(client, url)).EnumerateArray().Select(e => e.GetProperty(feld).GetString() ?? "")];

    [Fact]
    public async Task Lueckentext_Suche_Findet_Unabhaengig_Von_Der_Schreibweise()
    {
        var creator = await TestApi.AdultAsync(factory);
        var titel = Begriff("Wetter");
        (await creator.PostAsJsonAsync("/api/v1/creator/cloze-texts", new
        {
            key = $"k-{Guid.NewGuid():N}"[..14],
            title = titel,
            sourceLanguage = "en",
            targetLanguage = "de",
            text = "It is {{1}} today.",
            gaps = new[] { new { index = 1, answer = "sunny" } },
        })).EnsureSuccessStatusCode();

        Assert.Contains(titel, await TrefferAsync(creator,
            $"/api/v1/creator/cloze-texts?take=500&search={titel.ToUpperInvariant()}", "title"));
    }

    [Fact]
    public async Task Uebungssuche_Findet_Unabhaengig_Von_Der_Schreibweise()
    {
        var creator = await TestApi.AdultAsync(factory);
        var titel = Begriff("Rechnen");
        // Built here rather than via the shared helper: that one fixes the title, and the search has to
        // find THIS row by a term nothing else in the shared fixture database carries.
        var subjectId = await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Suchfach") }));
        var (seriesId, seriesUnitId) = await TestApi.CreateSeriesAndUnitAsync(creator, subjectId);
        (await creator.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/arithmetic", new
            {
                title = titel,
                orderIndex = 1,
                rewardPoints = 10,
                config = new { problems = new[] { new { prompt = "7 × 6", answer = 42, tolerance = 0 } } },
            })).EnsureSuccessStatusCode();

        Assert.Contains(titel, await TrefferAsync(creator,
            $"/api/v1/creator/exercises?take=500&search={titel.ToUpperInvariant()}", "title"));
    }

    [Fact]
    public async Task Interessen_Tag_Suche_Findet_Unabhaengig_Von_Der_Schreibweise()
    {
        var creator = await TestApi.AdultAsync(factory);
        var label = Begriff("Dino");
        (await creator.PostAsJsonAsync("/api/v1/creator/interest-tags", new { label })).EnsureSuccessStatusCode();

        Assert.Contains(label, await TrefferAsync(creator,
            $"/api/v1/creator/interest-tags?take=500&search={label.ToUpperInvariant()}", "label"));
    }

    [Fact]
    public async Task Mediensuche_Findet_Unabhaengig_Von_Der_Schreibweise()
    {
        var creator = await TestApi.AdultAsync(factory);
        var beschreibung = Begriff("Pferd");
        (await creator.PostAsJsonAsync("/api/v1/creator/media", new { description = beschreibung }))
            .EnsureSuccessStatusCode();

        Assert.Contains(beschreibung, await TrefferAsync(creator,
            $"/api/v1/creator/media?take=500&search={beschreibung.ToUpperInvariant()}", "description"));
    }

    /// <summary>
    /// The vocabulary store is the case that looks fixed and is not: <c>Word</c> and <c>Translation</c>
    /// carry the <c>NOCASE</c> collation, which a substring search never consults (B-128's measurement).
    /// </summary>
    [Fact]
    public async Task Vokabelsuche_Findet_Unabhaengig_Von_Der_Schreibweise()
    {
        var creator = await TestApi.AdultAsync(factory);
        var wort = Begriff("Horse");
        (await creator.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            sourceLanguage = "en",
            targetLanguage = "de",
            word = wort,
            translation = "Pferd",
        })).EnsureSuccessStatusCode();

        Assert.Contains(wort, await TrefferAsync(creator,
            $"/api/v1/creator/vocabulary?take=500&search={wort.ToUpperInvariant()}", "word"));
    }

    /// <summary>
    /// The two narrow filters of the same endpoint. They fold case for the same reason <c>search</c> does -
    /// the contract calls them "substring filter" while only <c>partOfSpeech</c> says "exact", so the line
    /// runs between substring and exact, not between this parameter and that one. Their own case, because
    /// without it a revert of exactly these two blocks would stay green.
    /// </summary>
    [Fact]
    public async Task Vokabel_Wort_Und_Uebersetzungsfilter_Falten_Die_Schreibweise()
    {
        var creator = await TestApi.AdultAsync(factory);
        var wort = Begriff("Zebra");
        var uebersetzung = Begriff("Zebratier");
        (await creator.PostAsJsonAsync("/api/v1/creator/vocabulary", new
        {
            sourceLanguage = "en",
            targetLanguage = "de",
            word = wort,
            translation = uebersetzung,
        })).EnsureSuccessStatusCode();

        Assert.Contains(wort, await TrefferAsync(creator,
            $"/api/v1/creator/vocabulary?take=500&word={wort.ToUpperInvariant()}", "word"));
        Assert.Contains(uebersetzung, await TrefferAsync(creator,
            $"/api/v1/creator/vocabulary?take=500&translation={uebersetzung.ToUpperInvariant()}", "translation"));
    }

    /// <summary>
    /// The article number is user free text, not a system key - its own help text calls it "dein eigenes
    /// Kürzel, um den Artikel wiederzufinden". That is why it folds case like every other field here
    /// (B-135, decision 1).
    /// </summary>
    [Fact]
    public async Task Shop_Artikelsuche_Findet_Unabhaengig_Von_Der_Schreibweise()
    {
        var father = await TestApi.AdultAsync(factory);
        var nummer = Begriff("TvNr");
        (await father.PostAsJsonAsync("/api/v1/supervisor/shop/articles", new
        {
            articleNumber = nummer,
            title = "Fernsehen",
            unitType = "Minute",
            actionType = "TV",
        })).EnsureSuccessStatusCode();

        Assert.Contains(nummer, await TrefferAsync(father,
            $"/api/v1/supervisor/shop/articles?take=500&search={nummer.ToUpperInvariant()}", "articleNumber"));
    }
}
