using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// B-550: the position response carries its learning check, computed by the server from the rules it grades by -
/// stage schedule, position stage, the exercise's default stage, the type's own stage rule and "typed tests only".
/// The plan's detail page used to guess it from the type alone and warned "self-assessment counts" on a typed stage.
/// </summary>
public class LernkontrolleTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private sealed record Check(string Kind, int? TypedFromDay);

    private async Task<(HttpClient Father, int PlanId)> PlanAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        return (father, await TestApi.CreateEmptyPlanAsync(father));
    }

    private static async Task<Check> CheckOfAsync(HttpResponseMessage res)
    {
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var check = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("learningCheck");
        return new Check(check.GetProperty("kind").GetString()!,
            check.TryGetProperty("typedFromDay", out var d) && d.ValueKind == JsonValueKind.Number ? d.GetInt32() : null);
    }

    private static Task<HttpResponseMessage> AddAsync(HttpClient father, int planId, object position) =>
        father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions", position);

    /// <summary>
    /// The case of B-550: a fixed typed stage (4, free text) without "typed tests only" is checked typed. Red when the
    /// position's stage is ignored and the type default (self-assessment) decides.
    /// </summary>
    [Fact]
    public async Task Feste_Getippte_Stufe_Ohne_Nur_Getippt_Ist_Getippt()
    {
        var (father, planId) = await PlanAsync();
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);

        Assert.Equal(new Check("Typed", null), await CheckOfAsync(await AddAsync(father, planId,
            new { exerciseId, stage = 4, requireTypedTest = false })));
    }

    /// <summary>Without any stage the type default (vocabulary: self-assessment) applies. Red when it is reported typed.</summary>
    [Fact]
    public async Task Ohne_Stufe_Zaehlt_Die_Selbsteinschaetzung()
    {
        var (father, planId) = await PlanAsync();
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);

        Assert.Equal(new Check("SelfAssessed", null), await CheckOfAsync(await AddAsync(father, planId,
            new { exerciseId, requireTypedTest = false })));
    }

    /// <summary>
    /// The exercise's own default stage sits between the position and the type, as in <c>StageForDay</c>. Red when the
    /// rule skips the exercise default (the position would read self-assessed).
    /// </summary>
    [Fact]
    public async Task Getippte_Standardstufe_Der_Uebung_Zaehlt()
    {
        var (father, planId) = await PlanAsync();
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var detail = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}");
        var url = $"/api/v1/creator/textbook-series/{detail.GetProperty("seriesId").GetInt32()}/units/{detail.GetProperty("seriesUnitId").GetInt32()}/vocabulary/{exerciseId}";
        (await father.PutAsJsonAsync(url, new
        {
            title = "Begrüßungen",
            orderIndex = 1,
            rewardPoints = 10,
            defaultStage = 4,
            config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = new[] { new { front = "hello", back = "hallo" } } },
        })).EnsureSuccessStatusCode();

        Assert.Equal(new Check("Typed", null), await CheckOfAsync(await AddAsync(father, planId, new { exerciseId })));
    }

    /// <summary>A schedule from self-assessment to typed is a ladder, typed from its switching day. Red when the day is lost or the schedule ignored.</summary>
    [Fact]
    public async Task Stufenleiter_Ist_Leiter_Mit_Schwellentag()
    {
        var (father, planId) = await PlanAsync();
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);

        Assert.Equal(new Check("Ladder", 5), await CheckOfAsync(await AddAsync(father, planId, new
        {
            exerciseId,
            stageSchedule = new[] { new { dayNumber = 1, stage = 2 }, new { dayNumber = 5, stage = 4 } },
        })));
    }

    /// <summary>Stage 1 (show both) is a display stage: playing through meets the goal (B-96). Red when it reads as self-assessment.</summary>
    [Fact]
    public async Task Anzeigestufe_Zaehlt_Durchspielen()
    {
        var (father, planId) = await PlanAsync();
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);

        Assert.Equal(new Check("Playthrough", null), await CheckOfAsync(await AddAsync(father, planId, new { exerciseId, stage = 1 })));
    }

    /// <summary>A type without a final test (reading) counts playthrough. Red when the check-mode branch is dropped.</summary>
    [Fact]
    public async Task Typ_Ohne_Test_Ist_Kein_Test()
    {
        var (father, planId) = await PlanAsync();
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("Lesen") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/reading", new
        {
            title = "Leseverstehen",
            orderIndex = 1,
            rewardPoints = 5,
            config = new { text = "A short text.", questions = new[] { new { prompt = "What?", choices = (string[]?)null, answer = "Text" } } },
        }));

        Assert.Equal(new Check("NoTest", null), await CheckOfAsync(await AddAsync(father, planId, new { exerciseId })));
    }

    /// <summary>
    /// "Typed tests only" on a self-assessed stage: no attempt is ever graded, the goal is unreachable. Red when the
    /// switch alone is read as "typed" (the old frontend rule).
    /// </summary>
    [Fact]
    public async Task Nur_Getippt_Auf_Selbsteinschaetzung_Ist_Unerreichbar()
    {
        var (father, planId) = await PlanAsync();
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);

        Assert.Equal(new Check("Unreachable", null), await CheckOfAsync(await AddAsync(father, planId,
            new { exerciseId, requireTypedTest = true })));
    }

    /// <summary>
    /// "Typed tests only" with a schedule holding a self-assessed stage is refused, on create and on a PATCH that sets
    /// the schedule; a schedule of display and typed stages stays allowed. Red when the ladder check is dropped (201/200),
    /// or when it also counts display stages (the last create would fail).
    /// </summary>
    [Fact]
    public async Task Nur_Getippt_Mit_Selbsteinschaetzung_In_Der_Leiter_Wird_Abgelehnt()
    {
        var (father, planId) = await PlanAsync();
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var selfAssessedLadder = new[] { new { dayNumber = 1, stage = 2 }, new { dayNumber = 5, stage = 4 } };

        var refused = await AddAsync(father, planId, new { exerciseId, requireTypedTest = true, stageSchedule = selfAssessedLadder });
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        var problem = await refused.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_error", problem.GetProperty("code").GetString());
        Assert.Contains("self-assessed stage", problem.GetProperty("detail").GetString());

        var typedId = await TestApi.IdAsync(await AddAsync(father, planId, new { exerciseId, requireTypedTest = true, stage = 4 }));
        var patch = await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{typedId}",
            new { stageSchedule = selfAssessedLadder });
        Assert.Equal(HttpStatusCode.BadRequest, patch.StatusCode);

        var displayThenTyped = new[] { new { dayNumber = 1, stage = 1 }, new { dayNumber = 3, stage = 4 } };
        Assert.Equal(new Check("Ladder", 3), await CheckOfAsync(await AddAsync(father, planId,
            new { exerciseId, requireTypedTest = true, stageSchedule = displayThenTyped })));
    }
}
