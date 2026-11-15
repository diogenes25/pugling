using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

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
    /// B-552 AK 8 and 5: a position already stored with "typed tests only" on a self-assessed base stage (the API no
    /// longer creates one, so it is written straight to the database) still reads as unreachable, and a PATCH that only
    /// changes its points is allowed. Red when <c>LearningCheckOf</c> loses the case, or when the combination check runs
    /// on every PATCH (the points PATCH would be refused).
    /// </summary>
    [Fact]
    public async Task Bestand_Nur_Getippt_Auf_Selbsteinschaetzung_Bleibt_Unerreichbar_Und_Aenderbar()
    {
        var (father, planId) = await PlanAsync();
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var id = await TestApi.IdAsync(await AddAsync(father, planId, new { exerciseId, requireTypedTest = true, stage = 4 }));
        await WithDbAsync(db => db.PlanPositions.Where(p => p.Id == id).ExecuteUpdateAsync(u => u.SetProperty(p => p.Stage, (int?)null)));

        var res = await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{id}", new { pointsGoalMet = 7 });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var check = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("learningCheck");
        Assert.Equal("Unreachable", check.GetProperty("kind").GetString());

        // "Back to default" on a stage that is already default changes nothing either. Red when clearStage alone counts.
        Assert.Equal(HttpStatusCode.OK, (await father.PatchAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions/{id}", new { clearStage = true, pointsGoalMet = 8 })).StatusCode);

        // The father's form sends requireTypedTest and the stage with every save (both unchanged here). Red when the check
        // runs on a sent field instead of a changed one - the father could then change nothing else on this position.
        await WithDbAsync(db => db.PlanPositions.Where(p => p.Id == id).ExecuteUpdateAsync(u => u.SetProperty(p => p.Stage, (int?)2)));
        var fromForm = await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{id}",
            new { requireTypedTest = true, stage = 2, pointsGoalMet = 9 });
        Assert.Equal(HttpStatusCode.OK, fromForm.StatusCode);
    }

    /// <summary>
    /// B-552 meets B-547: a typed-only position whose schedule covers day 1 has no stage of its own; emptying the schedule
    /// (back to the method default) leaves the self-assessed base stage, so it is refused. Red when an empty schedule
    /// skipped the check.
    /// </summary>
    [Fact]
    public async Task Leerer_Stufenplan_Bei_Nur_Getippt_Ohne_Stufe_Wird_Abgelehnt()
    {
        var (father, planId) = await PlanAsync();
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var id = await TestApi.IdAsync(await AddAsync(father, planId,
            new { exerciseId, requireTypedTest = true, stageSchedule = new[] { new { dayNumber = 1, stage = 4 } } }));

        await AssertRefusedAsync(await father.PatchAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions/{id}", new { stageSchedule = Array.Empty<object>() }));
    }

    /// <summary>
    /// B-552 AK 1: "typed tests only" without a stage falls on the type default (self-assessment) and is refused, as is a
    /// fixed self-assessed stage; with a typed stage it is accepted. Red without the base-stage check on create (the
    /// first call would be 201).
    /// </summary>
    [Fact]
    public async Task Nur_Getippt_Ohne_Getippte_Basisstufe_Wird_Abgelehnt()
    {
        var (father, planId) = await PlanAsync();
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);

        await AssertRefusedAsync(await AddAsync(father, planId, new { exerciseId, requireTypedTest = true }));
        await AssertRefusedAsync(await AddAsync(father, planId, new { exerciseId, requireTypedTest = true, stage = 2 }));
        Assert.Equal(new Check("Typed", null), await CheckOfAsync(await AddAsync(father, planId,
            new { exerciseId, requireTypedTest = true, stage = 4 })));
    }

    /// <summary>
    /// B-552 AK 2: "typed tests only" inherited from the exercise default (set in the database, the creator path is
    /// closed by AK 6) with a position that says nothing is refused too. Red when only <c>dto.RequireTypedTest</c> is
    /// judged instead of the effective value.
    /// </summary>
    [Fact]
    public async Task Geerbtes_Nur_Getippt_Ohne_Getippte_Stufe_Wird_Abgelehnt()
    {
        var (father, planId) = await PlanAsync();
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        await WithDbAsync(db => db.Exercises.Where(e => e.Id == exerciseId)
            .ExecuteUpdateAsync(u => u.SetProperty(e => e.DefaultRequireTypedTest, true)));

        await AssertRefusedAsync(await AddAsync(father, planId, new { exerciseId }));
    }

    /// <summary>
    /// B-552 AK 3: a schedule that starts on day 1 replaces the base stage, so a self-assessed base stage does not
    /// matter there. Red when the check judges the base stage although the schedule covers day 1 (it would be 400).
    /// </summary>
    [Fact]
    public async Task Stufenplan_Ab_Tag_Eins_Ersetzt_Die_Basisstufe()
    {
        var (father, planId) = await PlanAsync();
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);

        Assert.Equal(new Check("Typed", null), await CheckOfAsync(await AddAsync(father, planId,
            new { exerciseId, requireTypedTest = true, stageSchedule = new[] { new { dayNumber = 1, stage = 4 } } })));
    }

    /// <summary>
    /// B-552 AK 4: a PATCH that tips the combination is refused and leaves the position as it was - "stage back to
    /// default" and a self-assessed stage on a typed-only position, and "typed only" switched on for a position without a
    /// stage. Red without the check on <c>stage</c>/<c>clearStage</c>, or without the base stage in the PATCH.
    /// </summary>
    [Fact]
    public async Task Patch_Der_Die_Kombination_Kippt_Wird_Abgelehnt()
    {
        var (father, planId) = await PlanAsync();
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var typedId = await TestApi.IdAsync(await AddAsync(father, planId, new { exerciseId, requireTypedTest = true, stage = 4 }));
        var plainId = await TestApi.IdAsync(await AddAsync(father, planId, new { exerciseId }));

        await AssertRefusedAsync(await father.PatchAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions/{typedId}", new { clearStage = true }));
        await AssertRefusedAsync(await father.PatchAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions/{typedId}", new { stage = 2 }));
        await AssertRefusedAsync(await father.PatchAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions/{plainId}", new { requireTypedTest = true }));

        var typed = await father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/study-plans/{planId}/positions/{typedId}");
        Assert.Equal(4, typed.GetProperty("stage").GetInt32());
        var plain = await father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/study-plans/{planId}/positions/{plainId}");
        Assert.False(plain.GetProperty("requireTypedTest").GetBoolean());
    }

    /// <summary>
    /// B-552 AK 6: the creator's default pair is judged the same way - "typed tests only" with no default stage or a
    /// self-assessed one is refused on create and on PUT, a typed default stage is accepted. Red without the creator check.
    /// </summary>
    [Fact]
    public async Task Creator_Vorgabe_Nur_Getippt_Braucht_Getippte_Vorgabestufe()
    {
        var father = await TestApi.AdultAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("Englisch-B552") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var url = $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary";
        object Body(int? defaultStage) => new
        {
            title = "Begrüßungen",
            orderIndex = 1,
            rewardPoints = 10,
            defaultRequireTypedTest = true,
            defaultStage,
            config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = new[] { new { front = "hello", back = "hallo" } } },
        };

        await AssertRefusedAsync(await father.PostAsJsonAsync(url, Body(null)), "defaultRequireTypedTest");
        await AssertRefusedAsync(await father.PostAsJsonAsync(url, Body(2)), "defaultRequireTypedTest");
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(url, Body(4)));

        await AssertRefusedAsync(await father.PutAsJsonAsync($"{url}/{exerciseId}", Body(null)), "defaultRequireTypedTest");
        Assert.Equal(HttpStatusCode.OK, (await father.PutAsJsonAsync($"{url}/{exerciseId}", Body(5))).StatusCode);
    }

    private static async Task AssertRefusedAsync(HttpResponseMessage res, string field = "requireTypedTest")
    {
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_error", problem.GetProperty("code").GetString());
        Assert.StartsWith(field + " cannot be combined with a self-assessed", problem.GetProperty("detail").GetString());
    }

    private async Task WithDbAsync(Func<PuglingDbContext, Task> change)
    {
        using var scope = factory.Services.CreateScope();
        await change(scope.ServiceProvider.GetRequiredService<PuglingDbContext>());
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
