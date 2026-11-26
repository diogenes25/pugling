using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Exercises;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-710 through the API: the creator posts coefficients (never a solution), the server rejects what is not a quadratic
/// equation, the catalog check and the father's test mode grade the set, and the child plays the position with the same
/// rule. Each case names the change that turns it red.
/// </summary>
public class QuadraticEquationApiTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static string Base(int seriesId, int unitId) =>
        $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/quadratic-equation";

    private static object Body(params object[] equations) =>
        new { title = "Quadratische Gleichungen", orderIndex = 1, rewardPoints = 10, config = new { equations } };

    private async Task<(HttpClient Father, int SeriesId, int UnitId)> UnitAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Mathe") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId, "Kapitel IV");
        return (father, seriesId, unitId);
    }

    private static async Task<string?> CodeOf(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();

    // AK 1: a solution field in the config is no part of the contract. Red when the config grows an answer field (the
    // solver would have a rival) or the unknown field is silently dropped.
    [Fact]
    public async Task Loesungsfeld_In_Der_Config_Wird_Abgelehnt()
    {
        var (father, seriesId, unitId) = await UnitAsync();
        var res = await father.PostAsJsonAsync(Base(seriesId, unitId),
            Body(new { a = 1, b = -6, c = 8, tolerance = 0.01, answer = "2;4" }));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("unknown_field", await CodeOf(res));
    }

    // AK 6 at the API: a = 0 and a tolerance reaching both solutions are rejected on create and on update. Red when
    // the controller skips the validation on either verb.
    [Fact]
    public async Task Ungueltige_Gleichung_Wird_Abgelehnt()
    {
        var (father, seriesId, unitId) = await UnitAsync();
        var zero = await father.PostAsJsonAsync(Base(seriesId, unitId), Body(new { a = 0, b = -6, c = 8, tolerance = 0.01 }));
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
        Assert.Equal("validation_error", await CodeOf(zero));

        var id = await TestApi.IdAsync(await father.PostAsJsonAsync(Base(seriesId, unitId),
            Body(new { a = 1, b = -6, c = 8, tolerance = 0.01 })));
        var wide = await father.PutAsJsonAsync($"{Base(seriesId, unitId)}/{id}",
            Body(new { a = 1, b = -6, c = 8, tolerance = 1 }));
        Assert.Equal(HttpStatusCode.BadRequest, wide.StatusCode);
        Assert.Equal("validation_error", await CodeOf(wide));
    }

    // AK 2, 3: the catalog check grades the set - in any order, the empty set as its fixed text. Red when the check
    // compares text ("4;2") or misses the empty set.
    [Fact]
    public async Task Katalog_Check_Wertet_Die_Menge()
    {
        var (father, seriesId, unitId) = await UnitAsync();
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync(Base(seriesId, unitId),
            Body(new { a = 1, b = -6, c = 8, tolerance = 0.01 }, new { a = 1, b = 0, c = 1, tolerance = 0.01 })));

        var res = await (await father.PostAsJsonAsync($"{Base(seriesId, unitId)}/{id}/check", new
        {
            answers = new[] { new { index = 0, value = "4;2" }, new { index = 1, value = "keine Lösung" } },
        })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, res.GetProperty("correct").GetInt32());
        Assert.Equal("2;4", res.GetProperty("items")[0].GetProperty("expected").GetString());

        var preview = await (await father.PostAsJsonAsync($"/api/v1/creator/exercises/{id}/preview/check", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "4;2", wasKnown = (bool?)null } },
        })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, preview.GetProperty("correct").GetInt32());
    }

    // AK 2, 10: the child plays a position of the type and types the set into the text field. "4;2" carries the claim -
    // red when the play path grades past the AnswerGrader (text would reject the order); "2,4" being wrong is a statement
    // of intent (text comparison rejects it too).
    [Fact]
    public async Task Sohn_Spielt_Die_Position()
    {
        var (father, seriesId, unitId) = await UnitAsync();
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync(Base(seriesId, unitId),
            Body(new { a = 1, b = -6, c = 8, tolerance = 0.01 })));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, id, stage: (int)QuadraticEquationStage.Direct);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);

        var wrong = await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "2,4");
        Assert.False((await wrong.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("wasCorrect").GetBoolean());
        var right = await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "4;2");
        Assert.True((await right.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("wasCorrect").GetBoolean());
    }

    // AK 7 on the card, AK 9: the practice card shows the separator hint and neither the set nor the coefficients'
    // solution before the answer. Red when the card reveals the answer on the typed stage or drops the hint.
    [Fact]
    public async Task Karte_Zeigt_Hinweis_Aber_Keine_Loesung()
    {
        var (father, seriesId, unitId) = await UnitAsync();
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync(Base(seriesId, unitId),
            Body(new { a = 1, b = -6, c = 8, tolerance = 0.01 })));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, id, stage: (int)QuadraticEquationStage.Direct);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);

        var card = await child.GetFromJsonAsync<JsonElement>($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/next");
        var texts = Strings(card).ToList();
        Assert.Contains("$x^2 - 6x + 8 = 0$", texts);
        Assert.Contains(QuadraticEquationExerciseType.SeparatorHint, texts);
        Assert.DoesNotContain(texts, t => t.Contains("2;4"));
    }

    // B-711 AK 5: a stage outside the type's list is rejected when assigning; both of its own are accepted. Red when the
    // type leaves StageOptions empty (every stage would pass).
    [Fact]
    public async Task Fremde_Stufe_Wird_Beim_Zuweisen_Abgelehnt()
    {
        var (father, seriesId, unitId) = await UnitAsync();
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync(Base(seriesId, unitId),
            Body(new { a = 1, b = -6, c = 8, tolerance = 0.01 })));
        var planId = await TestApi.CreateEmptyPlanAsync(father);
        var positions = $"/api/v1/supervisor/study-plans/{planId}/positions";

        var foreign = await father.PostAsJsonAsync(positions, new { exerciseId = id, stage = (int)TestStage.FreeText });
        Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
        foreach (var stage in new[] { QuadraticEquationStage.Distractors, QuadraticEquationStage.Direct })
        {
            var own = await father.PostAsJsonAsync(positions, new { exerciseId = id, stage = (int)stage });
            Assert.True(own.IsSuccessStatusCode, await own.Content.ReadAsStringAsync());
        }
    }

    // B-711 AK 4 at the API: on the choice stage the card offers the set as the child sends it, without the separator
    // hint, and the picked option is graded like typed. Red when the card's options miss the set atom's spelling or the
    // play path grades a pick differently.
    [Fact]
    public async Task Auswahlstufe_Wertet_Die_Gewaehlte_Menge()
    {
        var (father, seriesId, unitId) = await UnitAsync();
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync(Base(seriesId, unitId),
            Body(new { a = 1, b = -6, c = 8, tolerance = 0.01 })));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, id, stage: (int)QuadraticEquationStage.Distractors);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);

        var card = await child.GetFromJsonAsync<JsonElement>($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/next");
        var texts = Strings(card).ToList();
        Assert.Contains("2;4", texts);
        Assert.Contains("-4;-2", texts);
        Assert.DoesNotContain(QuadraticEquationExerciseType.SeparatorHint, texts);

        var wrong = await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "-4;-2");
        Assert.False((await wrong.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("wasCorrect").GetBoolean());
        var right = await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "2;4");
        Assert.True((await right.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("wasCorrect").GetBoolean());
    }

    // B-712 AK 9: the typed card of an equation says it takes a set - on the practice card and the test item - and the
    // choice card and any other type's card do not. Red when the flag is set on every card, missing on the equation card,
    // or set where the child answers with a button.
    [Fact]
    public async Task Mengen_Eingabe_Nur_Auf_Der_Getippten_Gleichungskarte()
    {
        var (father, seriesId, unitId) = await UnitAsync();
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync(Base(seriesId, unitId),
            Body(new { a = 1, b = -6, c = 8, tolerance = 0.01 })));
        var child = await TestApi.ChildAsync(factory);

        async Task<bool> PracticeFlag(int exerciseId, int stage)
        {
            var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, stage);
            var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
            var card = await child.GetFromJsonAsync<JsonElement>($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/next");
            return card.GetProperty("card").GetProperty("setAnswer").GetBoolean();
        }

        Assert.True(await PracticeFlag(id, (int)QuadraticEquationStage.Direct));
        Assert.False(await PracticeFlag(id, (int)QuadraticEquationStage.Distractors));
        var (_, _, arithmeticId) = await TestApi.CreateArithmeticExerciseAsync(father);
        Assert.False(await PracticeFlag(arithmeticId, (int)TestStage.FreeText));

        var (testPlan, testPosition) = TestApi.SeedLeitnerPosition(factory, id, (int)QuadraticEquationStage.Direct);
        var tests = $"/api/v1/student/study-plans/{testPlan}/positions/{testPosition}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(tests, new { }), "attemptId");
        var next = await child.GetFromJsonAsync<JsonElement>($"{tests}/{attemptId}/next");
        Assert.True(next.GetProperty("item").GetProperty("setAnswer").GetBoolean());

        // Review: the test path grades the set too - "4;2" is right only through the set rule (text would reject it).
        var submit = await child.PostAsJsonAsync($"{tests}/{attemptId}/submit",
            new { answers = new[] { new { itemIndex = 0, givenAnswer = "4;2" } } });
        Assert.True(submit.IsSuccessStatusCode, await submit.Content.ReadAsStringAsync());
        Assert.Equal(1, (await submit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("correctItems").GetInt32());
    }

    // Every string value of a JSON tree - the card is judged by what it carries anywhere, not by one field name.
    private static IEnumerable<string> Strings(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => [e.GetString()!],
        JsonValueKind.Object => e.EnumerateObject().SelectMany(p => Strings(p.Value)),
        JsonValueKind.Array => e.EnumerateArray().SelectMany(Strings),
        _ => [],
    };
}
