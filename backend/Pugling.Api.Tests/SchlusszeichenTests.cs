using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-457: a closing <c>.</c>, <c>?</c>, <c>!</c> or <c>…</c> is not part of the answer. A child who translates the
/// sentence right and leaves off the full stop must not lose the points - the meter is the language, not typing
/// discipline (same stance as case and the apostrophe, B-440).
/// </summary>
public class SchlusszeichenTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly AnswerGrader _grader = new();

    // AK 1, 2, 4: full stop, question mark, several marks and a space before them.
    [Theory]
    [InlineData("the museum is on the corner", "The museum is on the corner.")]
    [InlineData("Excuse me, how can I get to the station", "Excuse me, how can I get to the station?")]
    [InlineData("Stop", "Stop!")]
    [InlineData("and then", "and then…")]
    [InlineData("the station ?!", "The station")]
    [InlineData("the station ? !", "The station")]
    [InlineData("is it", "Is it?")]
    public void Schlusszeichen_Zaehlt_Nicht(string given, string expected) =>
        Assert.True(_grader.Matches(given, expected, articleOptionalIn: null));

    // AK 5: another word stays wrong, and a comma inside the sentence is not dropped.
    [Theory]
    [InlineData("the museum is in the corner", "The museum is on the corner.")]
    [InlineData("yes I am", "Yes, I am.")]
    public void Anderer_Wortlaut_Bleibt_Falsch(string given, string expected) =>
        Assert.False(_grader.Matches(given, expected, articleOptionalIn: null));

    // AK 6: an answer that is nothing but a mark keeps it - stripped it would be empty, and empty is never correct.
    [Fact]
    public void Nur_Ein_Zeichen_Bleibt_Loesbar() =>
        Assert.True(_grader.Matches("?", "?", articleOptionalIn: null));

    // AK 3: the gap of a cloze follows the same rule (solution and alternatives).
    [Fact]
    public void Luecke_Ohne_Schlusszeichen_Ist_Richtig()
    {
        var gap = new Gap(0, "isn't it?", ["is it not?"]);
        Assert.True(_grader.MatchesGap(gap, "isn't it"));
        Assert.True(_grader.MatchesGap(gap, "is it not"));
    }

    // AK 3 for lists: the catalog check (entry and alternative) follows the same rule, ordered and unordered.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Liste_Ohne_Schlusszeichen_Ist_Richtig(bool ordered)
    {
        var father = await TestApi.AdultAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Erdkunde-Schlusszeichen") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var basePath = $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/list";
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(basePath, new
        {
            title = TestApi.UniqueName("Hauptstädte"),
            orderIndex = 1,
            rewardPoints = 10,
            config = new
            {
                instruction = "Nenne die Hauptstädte.",
                ordered,
                items = new object[] { new { value = "Paris." }, new { value = "Rom", alternatives = new[] { "Roma!" } } },
            },
        }));

        var res = await (await father.PostAsJsonAsync($"{basePath}/{exerciseId}/check",
            new { answers = new[] { new { index = 0, value = "paris" }, new { index = 1, value = "Roma" } } }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(100, res.GetProperty("scorePercent").GetInt32());
    }

    // AK 1 end to end: the supervisor's test mode grades a translation without its full stop as correct.
    [Fact]
    public async Task Vorschau_Wertet_Uebersetzung_Ohne_Punkt_Als_Richtig()
    {
        var father = await TestApi.AdultAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Englisch-Schlusszeichen") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/translation", new
            {
                title = TestApi.UniqueName("Museum"),
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    sourceLang = "de",
                    targetLang = "en",
                    items = new[] { new { source = "Das Museum ist an der Ecke.", target = "The museum is on the corner." } },
                },
            }));

        var res = await (await father.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/preview/check",
            new { answers = new[] { new { itemIndex = 0, givenAnswer = "the museum is on the corner", wasKnown = (bool?)null } }, stage = (int)TestStage.FreeText }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, res.GetProperty("correct").GetInt32());
    }
}
