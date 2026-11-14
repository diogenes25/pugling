using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Pugling.Agent.Creator;
using Pugling.Agent.Creator.Briefing;
using Pugling.Api.Data;
using Pugling.Client;

namespace Pugling.Api.Tests;

/// <summary>
/// B-20: the agent's <c>assign</c> verb adds existing exercises to a child's running plan as positions - no language
/// model, read first then write, duplicates skipped in the agent because the server accepts them.
/// </summary>
public class AssignAgentTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // Account 1 = the father: creator (tags, exercises) AND supervisor (plans, positions) of the children created here.
    private HttpClient Authenticated() =>
        factory.CreateDefaultClient(AuthHandler.Standalone(new PuglingClientOptions
        {
            BaseUrl = "http://localhost",
            AccountId = 1,
            Pin = "0000",
        }));

    private CreatorApi Creator => new(Authenticated());
    private SupervisorApi Supervisor => new(Authenticated());
    private PlanAssigner Assigner => new(Creator, Supervisor);

    private static AssignRequest Parse(params string[] args) => AssignRequest.Parse(CommandLine.Parse(["assign", .. args]));

    private async Task<int> ChildAsync(string name) =>
        (await Supervisor.CreateChildAsync(new CreateChildDto(TestApi.UniqueName(name), null, null, null, null))).Id;

    /// <summary>The child's playable plan: active, starting today.</summary>
    private async Task<int> PlayablePlanAsync(int childId) =>
        (await Supervisor.CreatePlanAsync(new CreatePlanDto(childId, "Laufender Plan", null, null, 14))).Id;

    /// <summary>
    /// An ACTIVE plan whose run is over, created after the playable one so it lists first (newest first) - the trap for a
    /// selection by <c>Active</c> instead of <c>IsPlayable</c>. Flipped in the DB, because the API would not keep two active.
    /// </summary>
    private async Task<int> ExpiredActivePlanAsync(int childId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var id = (await Supervisor.CreatePlanAsync(new CreatePlanDto(childId, "Abgelaufen", null, today.AddDays(-30), 5, Active: false))).Id;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.StudyPlans.Single(p => p.Id == id).Active = true;
        db.SaveChanges();
        return id;
    }

    private async Task<int> ExerciseAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        return (await TestApi.CreateArithmeticExerciseAsync(father)).exerciseId;
    }

    private async Task<IReadOnlyList<PositionResponse>> PositionsAsync(int planId) => await Supervisor.ListPositionsAsync(planId);

    /// <summary>
    /// AK 1. Red when the plan is chosen by <c>Active</c> instead of <c>IsPlayable</c>: the expired active plan lists first
    /// and would receive the positions. All override fields stay null, so the server defaults apply.
    /// </summary>
    [Fact]
    public async Task Ids_Landen_Im_Spielbaren_Plan()
    {
        var childId = await ChildAsync("Assign-Ids");
        var playable = await PlayablePlanAsync(childId);
        var expired = await ExpiredActivePlanAsync(childId);
        var (a, b) = (await ExerciseAsync(), await ExerciseAsync());

        var outcome = await Assigner.RunAsync(Parse("--child", $"{childId}", "--exercise-id", $"{a}", "--exercise-id", $"{b}"), default);

        Assert.Equal(0, outcome.ExitCode);
        Assert.Equal(playable, outcome.Plan.Id);
        Assert.Equal([a, b], (await PositionsAsync(playable)).Select(p => p.ExerciseId).Order().ToArray());
        Assert.Empty(await PositionsAsync(expired));
        // Server defaults: the agent sends no figures of its own (Decision 7).
        var position = (await PositionsAsync(playable)).First();
        var serverDefault = await Supervisor.AddPositionAsync(await PlayablePlanAsync(await ChildAsync("Vergleich")),
            new CreatePositionDto(a, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null));
        Assert.Equal(serverDefault.PointsGoalMet, position.PointsGoalMet);
        Assert.Equal(serverDefault.Cadence, position.Cadence);
        Assert.Equal(serverDefault.GoalThreshold, position.GoalThreshold);
    }

    /// <summary>
    /// AK 2. Red when the tag name is not resolved through the child: the second child carries a tag of the same name with
    /// another exercise, which would land in the plan.
    /// </summary>
    [Fact]
    public async Task Tag_Wird_Ueber_Das_Kind_Aufgeloest()
    {
        var childId = await ChildAsync("Assign-Tag");
        var other = await ChildAsync("Assign-Geschwister-Tag");
        var plan = await PlayablePlanAsync(childId);
        var (x, y, z) = (await ExerciseAsync(), await ExerciseAsync(), await ExerciseAsync());
        // The sibling's tag first and the sibling's name sorting first, so a lookup across all children finds it first.
        var foreignTag = await Creator.CreateTagAsync(new CreateTagDto(other, "Klausur", null));
        await Creator.TagExercisesAsync(foreignTag.Id, new TagExercisesDto([z]));
        var ownTag = await Creator.CreateTagAsync(new CreateTagDto(childId, "Klausur", null));
        await Creator.TagExercisesAsync(ownTag.Id, new TagExercisesDto([x, y]));

        var outcome = await Assigner.RunAsync(Parse("--child", $"{childId}", "--tag", "Klausur"), default);

        Assert.Equal(0, outcome.ExitCode);
        Assert.Equal([x, y], (await PositionsAsync(plan)).Select(p => p.ExerciseId).Order().ToArray());
        Assert.Equal(2, outcome.TagTotal);
    }

    /// <summary>
    /// AK 3. Red when the pre-check against the plan's positions is missing: the server would add a duplicate, and the
    /// second run would report exit 0 instead of 3. Exit 3, not the story's 2: 2 means "invoked incorrectly" across the
    /// console (Stakeholder-Entscheidung im Auftrag des Nutzers, Story-Verlauf 2026-11-14).
    /// </summary>
    [Fact]
    public async Task Vorhandene_Position_Wird_Uebersprungen_Und_Nichts_Neues_Ist_Exit_3()
    {
        var childId = await ChildAsync("Assign-Doppelt");
        var plan = await PlayablePlanAsync(childId);
        var (a, b) = (await ExerciseAsync(), await ExerciseAsync());
        await Assigner.RunAsync(Parse("--child", $"{childId}", "--exercise-id", $"{a}"), default);

        var mixed = await Assigner.RunAsync(Parse("--child", $"{childId}", "--exercise-id", $"{a},{b}"), default);
        Assert.Equal(0, mixed.ExitCode);
        Assert.True(mixed.Items.Single(i => i.ExerciseId == a).AlreadyPresent);
        Assert.Contains(mixed.Report(), l => l.Contains($"#{a}") && l.Contains("schon vorhanden"));

        var again = await Assigner.RunAsync(Parse("--child", $"{childId}", "--exercise-id", $"{a}", "--exercise-id", $"{b}"), default);
        Assert.Equal(3, again.ExitCode);
        Assert.Empty(again.CreatedPositionIds);
        Assert.Equal([a, b], (await PositionsAsync(plan)).Select(p => p.ExerciseId).Order().ToArray());
    }

    /// <summary>
    /// AK 4. Red when the missing playable plan falls through to a write (or another plan), or when a <c>--plan-id</c>
    /// of another child is accepted. Exit 2 is the <see cref="AgentUsageException"/> path of <c>Program</c>.
    /// </summary>
    [Fact]
    public async Task Ohne_Spielbaren_Plan_Oder_Mit_Fremdem_Plan_Ist_Fehlbedienung()
    {
        var childId = await ChildAsync("Assign-Ohne-Plan");
        var sibling = await ChildAsync("Assign-Geschwister");
        var siblingPlan = await PlayablePlanAsync(sibling);
        var exercise = await ExerciseAsync();

        var none = await Assert.ThrowsAsync<AgentUsageException>(() =>
            Assigner.RunAsync(Parse("--child", $"{childId}", "--exercise-id", $"{exercise}"), default));
        Assert.Contains("--plan-id", none.Message);

        await Assert.ThrowsAsync<AgentUsageException>(() =>
            Assigner.RunAsync(Parse("--child", $"{childId}", "--plan-id", $"{siblingPlan}", "--exercise-id", $"{exercise}"), default));
        Assert.Empty(await PositionsAsync(siblingPlan));
        Assert.DoesNotContain(await Supervisor.ListPlansAsync(childId), p => p.PositionCount > 0);
    }

    /// <summary>
    /// AK 5. Red when the dry-run branch only takes effect after the first <c>AddPositionAsync</c>: the position count
    /// would grow. The report names child, plan, each exercise's state and the overrides.
    /// </summary>
    [Fact]
    public async Task Trockenlauf_Berichtet_Und_Schreibt_Nichts()
    {
        var childId = await ChildAsync("Assign-Trocken");
        var plan = await PlayablePlanAsync(childId);
        var (a, b) = (await ExerciseAsync(), await ExerciseAsync());
        await Assigner.RunAsync(Parse("--child", $"{childId}", "--exercise-id", $"{a}"), default);
        var before = (await PositionsAsync(plan)).Count;

        var outcome = await Assigner.RunAsync(Parse("--child", $"{childId}", "--exercise-id", $"{a}", "--exercise-id", $"{b}",
            "--points", "30", "--dry-run"), default);

        Assert.Equal(before, (await PositionsAsync(plan)).Count);
        Assert.Empty(outcome.CreatedPositionIds);
        var report = outcome.Report();
        Assert.Contains($"Kind {childId}, Plan {plan}", report[0]);
        Assert.Contains("Punkte 30", report[1]);
        Assert.Contains(report, l => l.Contains($"#{a}") && l.Contains("schon vorhanden"));
        Assert.Contains(report, l => l.Contains($"#{b}") && l.EndsWith(": neu"));
    }

    /// <summary>AK 6. Red when an override is parsed but not put into the position DTO.</summary>
    [Fact]
    public async Task Overrides_Landen_In_Der_Position()
    {
        var childId = await ChildAsync("Assign-Overrides");
        var plan = await PlayablePlanAsync(childId);
        var exercise = await ExerciseAsync();

        await Assigner.RunAsync(Parse("--child", $"{childId}", "--exercise-id", $"{exercise}", "--points", "30",
            "--cadence", "Weekly", "--goal-threshold", "70"), default);

        var position = (await PositionsAsync(plan)).Single();
        Assert.Equal(30, position.PointsGoalMet);
        Assert.Equal(GoalCadence.Weekly, position.Cadence);
        Assert.Equal(70, position.GoalThreshold);
    }

    /// <summary>
    /// Review finding 4: a bare <c>--exercise-id</c> next to a valid one is a misuse, not silently one fewer exercise.
    /// Red when the empty-occurrence check in <c>AssignRequest.Parse</c> is dropped (the parser then keeps only <c>4</c>).
    /// </summary>
    [Fact]
    public void Leeres_ExerciseId_Neben_Gueltigem_Ist_Fehlbedienung()
    {
        Assert.Throws<AgentUsageException>(() => Parse("--child", "1", "--exercise-id", "4", "--exercise-id"));
        Assert.Throws<AgentUsageException>(() => Parse("--child", "1", "--exercise-id", "--exercise-id", "4"));
        Assert.Throws<AgentUsageException>(() => Parse("--child", "1", "--exercise-id", ","));
    }

    /// <summary>
    /// Review finding 2: a tag holding more exercises than the window is refused before anything is written - never a
    /// partial assignment with exit 0. Red when the <c>page.Truncated</c> guard is dropped (one position would land).
    /// </summary>
    [Fact]
    public async Task Tag_Ueber_Dem_Lesefenster_Bricht_Vor_Dem_Schreiben_Ab()
    {
        var childId = await ChildAsync("Assign-Fenster");
        var plan = await PlayablePlanAsync(childId);
        var (x, y) = (await ExerciseAsync(), await ExerciseAsync());
        var tag = await Creator.CreateTagAsync(new CreateTagDto(childId, "Gross", null));
        await Creator.TagExercisesAsync(tag.Id, new TagExercisesDto([x, y]));

        var ex = await Assert.ThrowsAsync<AgentException>(() =>
            new PlanAssigner(Creator, Supervisor) { TagWindow = 1 }.RunAsync(Parse("--child", $"{childId}", "--tag", "Gross"), default));

        Assert.Contains("2 Übungen", ex.Message);
        Assert.Empty(await PositionsAsync(plan));
    }

    /// <summary>
    /// Review finding 3: tag names that differ only in case are ambiguous unless one matches exactly. Red when the lookup
    /// takes the first case-insensitive hit (it would assign "Klausur"'s exercise for "klausur"), or when the exact
    /// match no longer wins (the "KLAUSUR" call would then be refused as ambiguous).
    /// </summary>
    [Fact]
    public async Task Tag_Nur_In_Schreibweise_Verschieden_Ist_Mehrdeutig()
    {
        var childId = await ChildAsync("Assign-Schreibweise");
        var plan = await PlayablePlanAsync(childId);
        var (x, y) = (await ExerciseAsync(), await ExerciseAsync());
        var lower = await Creator.CreateTagAsync(new CreateTagDto(childId, "Klausur", null));
        await Creator.TagExercisesAsync(lower.Id, new TagExercisesDto([x]));
        var upper = await Creator.CreateTagAsync(new CreateTagDto(childId, "KLAUSUR", null));
        await Creator.TagExercisesAsync(upper.Id, new TagExercisesDto([y]));

        await Assert.ThrowsAsync<AgentUsageException>(() =>
            Assigner.RunAsync(Parse("--child", $"{childId}", "--tag", "klausur"), default));
        Assert.Empty(await PositionsAsync(plan));

        await Assigner.RunAsync(Parse("--child", $"{childId}", "--tag", "KLAUSUR"), default);
        Assert.Equal([y], (await PositionsAsync(plan)).Select(p => p.ExerciseId).ToArray());
    }

    /// <summary>AK 7. Red when the "exactly one source" check accepts both or neither.</summary>
    [Fact]
    public void Ids_Und_Tag_Zusammen_Oder_Keins_Ist_Fehlbedienung()
    {
        Assert.Throws<AgentUsageException>(() => Parse("--child", "1", "--exercise-id", "4", "--tag", "Klausur"));
        Assert.Throws<AgentUsageException>(() => Parse("--child", "1"));
        // Counter-check: the repeated option really collects every value (the parser kept only the last one before).
        Assert.Equal([4, 9], Parse("--child", "1", "--exercise-id", "4", "--exercise-id", "9").ExerciseIds);
    }

    /// <summary>
    /// Decision 8: the first refused write stops the run; what was created stays, and a rerun skips it. The foreign
    /// exercise is not executable for this account (<c>executePublic: false</c>). Red when the agent swallows the refusal
    /// and carries on (the third exercise would be written) or reports exit 0.
    /// </summary>
    [Fact]
    public async Task Abgelehnte_Position_Bricht_Ab_Und_Laesst_Die_Angelegten_Stehen()
    {
        var childId = await ChildAsync("Assign-Abgelehnt");
        var plan = await PlayablePlanAsync(childId);
        var (own, after) = (await ExerciseAsync(), await ExerciseAsync());
        var foreign = await ForeignPrivateExerciseAsync();

        var outcome = await Assigner.RunAsync(Parse("--child", $"{childId}", "--exercise-id", $"{own},{foreign},{after}"), default);

        Assert.Equal(1, outcome.ExitCode);
        Assert.Equal(foreign, outcome.Rejected!.ExerciseId);
        Assert.Contains("exercise_not_executable", outcome.Rejected.Error);
        Assert.Equal([own], (await PositionsAsync(plan)).Select(p => p.ExerciseId).ToArray());
    }

    private async Task<int> ForeignPrivateExerciseAsync()
    {
        var pin = "7231";
        var adultId = await TestApi.IdAsync(await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Fremd-Autor"), pin }));
        var stranger = await TestApi.AdultAsync(factory, adultId, pin);
        var subjectId = await TestApi.IdAsync(await stranger.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("Fremd-Fach") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(stranger, subjectId);
        return await TestApi.IdAsync(await stranger.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/arithmetic", new
        {
            title = "Privat",
            orderIndex = 1,
            rewardPoints = 10,
            executePublic = false,
            config = new { problems = new[] { new { prompt = "1 + 1", answer = 2, tolerance = 0 } } },
        }));
    }

    /// <summary>
    /// AK 8. The verb through the console's own dispatcher, wired like the host but with a model that counts its calls.
    /// Statement of intent - green on arrival, since no path of the verb reaches the model today. Red when <c>assign</c>
    /// touches the chat client (e.g. <c>AssignAsync</c> routing through <c>BuildRequestAsync</c>/the pipeline).
    /// </summary>
    [Fact]
    public async Task Assign_Braucht_Kein_Sprachmodell()
    {
        var childId = await ChildAsync("Assign-Ohne-Modell");
        var plan = await PlayablePlanAsync(childId);
        var exercise = await ExerciseAsync();
        var chat = new FakeChatClient("{}");
        var creator = Creator;
        var supervisor = Supervisor;
        var pipeline = new CreatorPipeline(new BriefingBuilder(creator, supervisor, new StudentApi(Authenticated())), []);
        var options = Options.Create(new AgentOptions());
        var filler = new UnitNotesFiller(chat, creator,
            new PageReader(chat, new FakeModelInventory(options.Value.VisionModel), options, NullLogger<PageReader>.Instance),
            options, NullLogger<UnitNotesFiller>.Instance);
        var commands = new AgentCommands(creator, supervisor, pipeline, new ExamPlanner(pipeline, creator, supervisor),
            new PlanPlanner(pipeline, supervisor), filler, ImageAgentTests.Run(creator, chat, new FakeImageGenerator()),
            new PlanAssigner(creator, supervisor));

        var exit = await commands.RunAsync(CommandLine.Parse(["assign", "--child", $"{childId}", "--exercise-id", $"{exercise}"]), default);

        Assert.Equal(0, exit);
        Assert.Equal(0, chat.Calls);
        Assert.Single(await PositionsAsync(plan));
    }
}
