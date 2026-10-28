using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-258. An id that points at nothing is one rule - "unknown reference" - and a client branching on the code
/// must not have to know which resource it talked to. The series already answered <c>invalid_reference</c>;
/// these sites answered <c>validation_error</c>. Every request below is valid except for the one unknown id,
/// so an earlier validation cannot answer in its place; each site is named, so reverting one shows which.
/// </summary>
public class UnbekannteReferenzTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const int Unknown = 999_999;

    [Fact]
    public async Task Unbekannte_Id_Meldet_Ueberall_InvalidReference()
    {
        var adult = await TestApi.AdultAsync(factory);
        var subjectId = await TestApi.IdAsync(await adult.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("Fach") }));
        var seriesId = await TestApi.IdAsync(await adult.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Reihe"), subjectId }));
        var bookId = await TestApi.IdAsync(await adult.PostAsJsonAsync("/api/v1/supervisor/children/1/textbooks",
            new { title = TestApi.UniqueName("Buch") }));

        var sites = new (string Site, HttpResponseMessage Response)[]
        {
            ("textbook POST subjectId", await adult.PostAsJsonAsync("/api/v1/supervisor/children/1/textbooks",
                new { title = TestApi.UniqueName("Buch"), subjectId = Unknown })),
            ("textbook PATCH subjectId", await adult.PatchAsJsonAsync($"/api/v1/supervisor/children/1/textbooks/{bookId}",
                new { subjectId = Unknown })),
            ("textbook POST seriesId", await adult.PostAsJsonAsync("/api/v1/supervisor/children/1/textbooks",
                new { title = TestApi.UniqueName("Buch"), seriesId = Unknown })),
            ("textbook POST currentUnitId", await adult.PostAsJsonAsync("/api/v1/supervisor/children/1/textbooks",
                new { title = TestApi.UniqueName("Buch"), seriesId, currentUnitId = Unknown })),
            ("grammar topic POST subjectId", await adult.PostAsJsonAsync("/api/v1/creator/grammar-topics",
                new { label = TestApi.UniqueName("Thema"), subjectId = Unknown })),
            ("series unit POST grammarTopicIds", await adult.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units",
                new { label = "Unit", grammarTopicIds = new[] { Unknown } })),
        };

        var wrong = new List<string>();
        foreach (var (site, response) in sites)
        {
            var code = response.StatusCode == HttpStatusCode.BadRequest
                ? (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString()
                : $"HTTP {(int)response.StatusCode}";
            if (code != "invalid_reference") wrong.Add($"{site}: {code}");
        }
        Assert.Empty(wrong);
    }
}
