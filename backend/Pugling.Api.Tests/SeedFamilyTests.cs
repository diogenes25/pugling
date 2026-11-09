using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-429: the seeded family is the first thing a developer, tester or role walk sees, so it has to use the catalog it ships
/// with. Fresh database per class fixture - these are assertions about the seed, not about data other tests created.
/// </summary>
public class SeedFamilyTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // AK 1 (book part). Red if the seeded book stays free text - the child hub then says "nicht katalogisiert" for a series
    // that lies in the same seed.
    [Fact]
    public async Task SeedBuch_HaengtAnReiheUndUnit()
    {
        var father = await TestApi.AdultAsync(factory);
        var books = await father.GetFromJsonAsync<JsonElement>("/api/v1/supervisor/children/1/textbooks");
        var book = books.EnumerateArray().Single(b => b.GetProperty("title").GetString() == "Green Line 1");

        Assert.Equal("Green Line 1", book.GetProperty("seriesName").GetString());
        Assert.Equal("Unit 1 – Greetings", book.GetProperty("currentUnitLabel").GetString());
    }

    // AK 2. Red if the starting balance is booked as a learning kind again ("Base" reads as "Wiederholung", "Achievement"
    // as "Auszeichnung") - it is a parent's gift, and the categories are what the history shows.
    [Fact]
    public async Task Startguthaben_IstEinePapaBuchung()
    {
        var father = await TestApi.AdultAsync(factory);
        var list = await father.GetFromJsonAsync<JsonElement>("/api/v1/supervisor/children/1/points?take=500");
        var entries = list.GetProperty("entries").EnumerateArray().ToList();

        Assert.Equal("Manual", entries.Single(e => e.GetProperty("reason").GetString() == "Startguthaben (Münzen)").GetProperty("kind").GetString());
        Assert.Equal("ManualGems", entries.Single(e => e.GetProperty("reason").GetString() == "Willkommens-Gems").GetProperty("kind").GetString());
    }
}
