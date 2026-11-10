using Pugling.Client;

namespace Pugling.Agent.Creator;

/// <summary>
/// How hard the generated plan is: passing threshold, points per met goal and the coin penalty for a
/// missed obligation. German values on purpose - they are the same three words the study-plan wizard
/// offers the father, and the option is typed by a human.
/// </summary>
public enum PlanIntensity
{
    /// <summary>Gentle start: pure reward, no penalty at all.</summary>
    Locker,
    /// <summary>The product's default balance.</summary>
    Normal,
    /// <summary>High bar and a noticeable penalty.</summary>
    Intensiv,
}

/// <summary>
/// The request for a generated study plan: the same briefing as a single exercise, spanning several
/// exercise types, plus the container's own frame (run and intensity).
/// </summary>
/// <param name="Base">The shared request (child/profile/location/unit); the planner sets type and topic per part.</param>
/// <param name="Types">The exercise types the plan consists of - in this order.</param>
/// <param name="PerType">
/// <b>Exercises</b> per type, one position each. Not to be confused with the class test's option of the
/// same name, which counts tasks: a plan's tasks per exercise come from the request's own item count.
/// </param>
/// <param name="Intensity">Passing threshold, points and penalty of every position.</param>
/// <param name="StartDate">First day of the run; today if not given.</param>
/// <param name="DurationDays">Length of the run in days.</param>
/// <param name="Title">Title of the plan; formed from the unit or topic if not given.</param>
public sealed record PlanRequest(
    GenerationRequest Base,
    IReadOnlyList<string> Types,
    int PerType,
    PlanIntensity Intensity,
    DateOnly? StartDate,
    int DurationDays,
    string? Title);

/// <summary>An exercise that was created but did not make it into the plan as a position.</summary>
/// <param name="ExerciseId">The exercise; it stays in the catalog and can be assigned by hand.</param>
/// <param name="Error">Why the server refused the position.</param>
public sealed record FailedPosition(int ExerciseId, string Error);

/// <summary>The result of a plan-generation run.</summary>
/// <param name="Title">The title used.</param>
/// <param name="Parts">All parts in request order.</param>
/// <param name="ExerciseIds">The exercises actually created.</param>
/// <param name="PlanId">The created study plan (only without a dry run and with at least one exercise).</param>
/// <param name="PositionIds">The positions created in that plan, in plan order.</param>
/// <param name="FailedPositions">Created exercises the plan refused; empty in the normal case.</param>
public sealed record PlanOutcome(
    string Title,
    IReadOnlyList<GeneratedPart> Parts,
    IReadOnlyList<int> ExerciseIds,
    int? PlanId,
    IReadOnlyList<int> PositionIds,
    IReadOnlyList<FailedPosition> FailedPositions)
{
    /// <summary>
    /// Did every part come out clean <b>and</b> reach the plan?
    /// <para>
    /// The self-test share is part of the question, and that is not pedantry: without <c>--strict</c> an
    /// exercise whose own solutions only match 60 % is <i>not</i> withdrawn, and here it would become a
    /// <b>daily obligation with a coin penalty</b>. Reporting that as a finished plan would be worse than
    /// what <c>create</c> does for the very same exercise - there it is already an error.
    /// </para>
    /// <para>
    /// This is <see cref="Drafting.GenerationOutcome.Published"/> plus one exemption: a dry run produces no
    /// exercise id and no self-test, and must still count as complete. Hence the condition is spelled out
    /// rather than delegated.
    /// </para>
    /// </summary>
    public bool Complete =>
        FailedPositions.Count == 0
        && Parts.All(p => p.Outcome is { DraftAccepted: true, RolledBack: false } outcome
                          && (outcome.ExerciseId is null || outcome.SelfTestPercent == 100));
}

/// <summary>What one intensity means for every position of the plan.</summary>
/// <param name="Pass">Passing threshold of the position test, in percent.</param>
/// <param name="Points">Points booked when the daily goal is met.</param>
/// <param name="Penalty">Coins withdrawn for a missed obligation - the product's "stick"; 0 = pure reward.</param>
public readonly record struct PlanSettings(int Pass, int Points, int Penalty);

/// <summary>
/// Plans a <b>playable study plan</b>: several exercises on the same material, each becoming a position
/// with its own daily goal, points and penalty. The counterpart to <see cref="ExamPlanner"/> - same flow,
/// different container: a study plan with positions instead of a class test.
/// <para>
/// Two properties are the whole reason this is wired in C# rather than left to the model. A failed type
/// costs the plan one position, not all of them, so a half-successful run stays <i>visibly</i>
/// half-successful. And the plan is created <b>inactive</b>: machine-written content must not become an
/// obligation - and therefore a source of penalty coins - before a human has looked at it.
/// </para>
/// </summary>
public sealed class PlanPlanner(CreatorPipeline pipeline, SupervisorApi supervisor)
{
    /// <summary>
    /// Passing threshold, points per met goal and coin penalty per intensity.
    /// <para>
    /// <b>Deliberately the same figures as the study-plan wizard's <c>INTENSITY</c></b>
    /// (<c>frontend/src/vater/VaterWizard.tsx</c>): one rule, two languages. The console cannot import the
    /// TypeScript constant, so it is ported rather than shared - a second, diverging heuristic for the same
    /// question would be a second place to forget when the balance changes.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Public so that the console can <b>report</b> the figures it is about to apply without holding a
    /// second copy of them - that copy is exactly what the note above rules out.
    /// </remarks>
    public static IReadOnlyDictionary<PlanIntensity, PlanSettings> Intensities { get; } =
        new Dictionary<PlanIntensity, PlanSettings>
        {
            [PlanIntensity.Locker] = new(70, 10, 0),
            [PlanIntensity.Normal] = new(80, 20, 5),
            [PlanIntensity.Intensiv] = new(90, 30, 10),
        };

    /// <summary>Generates the exercises and, from them, the study plan with one position each.</summary>
    public async Task<PlanOutcome> RunAsync(PlanRequest request, CancellationToken ct = default)
    {
        // All five checks come before the first model call: a run that cannot end in a plan should not
        // cost minutes of generation first.
        if (request.Types.Count == 0)
            throw new AgentUsageException("Ein Lehrplan braucht mindestens einen Übungstyp (--types).");
        pipeline.EnsureSupported(request.Types);
        if (request.PerType <= 0)
            throw new AgentUsageException($"--per-type erwartet mindestens 1, bekam {request.PerType}.");
        // The run is not left to the server: it silently turns a non-positive value into 10 days, while
        // help and README promise 14 - a typo would produce a plan whose length nobody stated.
        if (request.DurationDays <= 0)
            throw new AgentUsageException($"--days erwartet mindestens 1, bekam {request.DurationDays}.");
        if (request.Base.ChildId is null)
            throw new AgentUsageException(
                "Ein Lehrplan gehört immer einem Kind – gib --child <id> an. "
                + "(--profile allein genügt nur für den gemeinsamen Katalog, also bei 'create' und 'exam'.)");

        // Briefed once before the parts, title given or not: a wrong series, unit, child, profile or interest is a
        // usage error of the whole request (exit 2), not a failed part (B-308).
        var briefing = await pipeline.BriefAsync(request.Base, ct);
        var derived = DeriveTitle(request, briefing);
        var title = request.Title?.Trim() is { Length: > 0 } given ? given : derived;
        var parts = new List<GeneratedPart>();
        var grammarPart = 0;

        foreach (var typeKey in request.Types)
        {
            for (var n = 1; n <= request.PerType; n++)
            {
                var part = request.Base with
                {
                    TypeKey = typeKey,
                    Topic = PartTopic(request.Base.Topic ?? title, typeKey, n, request.PerType, briefing.GrammarTopicLabels,
                        GrammarTypes.Contains(typeKey) ? grammarPart++ : -1),
                    // Each exercise of a type takes the next slice of the unit's word list, so two vocabulary
                    // exercises practice twenty words instead of the same ten twice.
                    Part = n - 1,
                };

                try
                {
                    var (_, outcome) = await pipeline.CreateAsync(part, ct);
                    parts.Add(new GeneratedPart(typeKey, outcome, null));
                }
                catch (Exception ex) when (ex is AgentException or AgentUsageException or PuglingApiException)
                {
                    // A failed exercise costs the plan one position, not all of them: the others keep running.
                    parts.Add(new GeneratedPart(typeKey, null, ex.Message));
                }
            }
        }

        var exerciseIds = parts
            .Select(p => p.Outcome)
            .Where(o => o is { RolledBack: false })
            .Select(o => o!.ExerciseId)
            .OfType<int>()
            .ToList();

        // A plan without a single position would be an empty container the father has to clean up.
        if (request.Base.DryRun || exerciseIds.Count == 0)
            return new PlanOutcome(title, parts, exerciseIds, null, [], []);

        var plan = await supervisor.CreatePlanAsync(new CreatePlanDto(
            ChildId: request.Base.ChildId.Value,
            Title: title,
            SubjectId: request.Base.SubjectId,
            StartDate: request.StartDate,
            DurationDays: request.DurationDays,
            Description: $"Vom KI-Creator erzeugt ({string.Join(", ", request.Types)}, Intensität {request.Intensity}). "
                         + "Bitte prüfen und dann aktivieren.",
            // The run must not take over the child's evening: an unreviewed plan stays off until a human
            // switches it on. Created active, it would additionally deactivate the plan being played today.
            Active: false), ct);

        var settings = Intensities[request.Intensity];
        var positionIds = new List<int>();
        var failedPositions = new List<FailedPosition>();
        foreach (var exerciseId in exerciseIds)
        {
            try
            {
                var position = await supervisor.AddPositionAsync(plan.Id, new CreatePositionDto(
                    ExerciseId: exerciseId,
                    Order: null,
                    // No stage: the number is the vocabulary scale, and every other type reads it differently.
                    // Left out, the server resolves the exercise's own default instead of a misread figure.
                    Stage: null,
                    ItemCount: null,
                    Scope: null,
                    Cadence: GoalCadence.Daily,
                    OrderStrategy: null,
                    GoalThreshold: settings.Pass,
                    RequireTypedTest: null,
                    UseLeitner: null,
                    MaxBox: null,
                    BoxIntervalDays: null,
                    StageSchedule: null,
                    PointsGoalMet: settings.Points,
                    PenaltyCoins: settings.Penalty,
                    // Everything below stays empty on purpose: those fields inherit the exercise's own bonus
                    // suggestion (the hybrid principle), and the agent knows nothing better about them.
                    NewContentPoints: null,
                    ComboThreshold: null,
                    ComboBonusPoints: null,
                    SpeedThresholdSeconds: null,
                    SpeedBonusPoints: null), ct);
                positionIds.Add(position.Id);
            }
            catch (PuglingApiException ex)
            {
                // The same rule as in the generation loop, and here the more important half of it: the plan
                // already exists at this point. Letting the exception out would leave the father with a
                // half-filled plan whose id he never got to see.
                failedPositions.Add(new FailedPosition(exerciseId, ex.Message));
            }
        }

        return new PlanOutcome(title, parts, exerciseIds, plan.Id, positionIds, failedPositions);
    }

    /// <summary>The types whose exercises practice a grammar point rather than words - each gets one of the unit's topics.</summary>
    private static readonly HashSet<string> GrammarTypes = new(["Cloze", "Grammar"], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The topic of one part. A grammar-practicing part names one of the unit's grammar topics, in turn: with
    /// "question tags" and "past progressive" in the unit, two cloze texts become one on each instead of two
    /// texts that mix everything. The rest keeps the part number, so that several exercises of the same type do
    /// not come back as the same exercise n times.
    /// </summary>
    public static string PartTopic(string subject, string typeKey, int n, int perType,
        IReadOnlyList<string> grammarTopics, int grammarPart) =>
        grammarPart >= 0 && grammarTopics.Count > 0
            ? $"{subject} – Schwerpunkt: {grammarTopics[grammarPart % grammarTopics.Count]}"
              // Fewer topics than parts: the second round on a topic must still read differently from the first.
              + (grammarPart >= grammarTopics.Count ? $" (Teil {grammarPart / grammarTopics.Count + 1})" : "")
            : $"{subject} (Lehrplan, {typeKey} {n}/{perType})";

    /// <summary>
    /// The title from the material: the request's unit, otherwise the topic. Taken from the briefing
    /// rather than guessed - the unit label should be the one the exercises name as their source.
    /// </summary>
    private static string DeriveTitle(PlanRequest request, Briefing.CreatorBriefing briefing) =>
        $"Lehrplan: {briefing.Profile?.Unit?.Label ?? request.Base.Topic ?? briefing.SeriesUnitLabel}";
}
