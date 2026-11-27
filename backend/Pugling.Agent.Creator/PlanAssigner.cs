using System.Globalization;
using System.Net;
using Pugling.Client;

namespace Pugling.Agent.Creator;

/// <summary>
/// The request of the <c>assign</c> verb (B-20): which exercises go into which running plan of a child, and
/// which position fields the caller overrides. Every override left <c>null</c> inherits the server default.
/// </summary>
/// <param name="ChildId">The child whose plan receives the positions.</param>
/// <param name="PlanId">An explicit plan of that child; <c>null</c> = its playable plan.</param>
/// <param name="ExerciseIds">Exercises named by id; empty when <paramref name="TagName"/> is used.</param>
/// <param name="TagName">A child-scoped tag whose exercises are assigned; <c>null</c> when ids are named.</param>
/// <param name="Points">Override for the points booked when the goal is met.</param>
/// <param name="GoalThreshold">Override for the passing threshold of the goal.</param>
/// <param name="Cadence">Override for the goal rhythm.</param>
/// <param name="DryRun">Resolve and report only; no write.</param>
public sealed record AssignRequest(
    int ChildId,
    int? PlanId,
    IReadOnlyList<int> ExerciseIds,
    string? TagName,
    int? Points,
    int? GoalThreshold,
    GoalCadence? Cadence,
    bool DryRun)
{
    /// <summary>
    /// Reads the request from the command line. Exactly one source of exercises - repeated <c>--exercise-id</c>
    /// or <c>--tag</c> - is required; both or neither is a misuse, not a domain failure.
    /// </summary>
    public static AssignRequest Parse(CommandLine command)
    {
        var childId = command.RequiredInt("child");
        // Every occurrence must name an id: a bare `--exercise-id` next to a valid one is a typo, not "one fewer".
        if (command.Values("exercise-id").Any(v => string.IsNullOrWhiteSpace(v?.Replace(",", ""))))
            throw new AgentUsageException("--exercise-id ohne Wert – jede Angabe braucht eine Übungs-Id.");
        var ids = command.Values("exercise-id")
            .SelectMany(v => (v ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(v => int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
                ? id
                : throw new AgentUsageException($"--exercise-id erwartet eine Zahl, bekam '{v}'."))
            .Distinct()
            .ToList();
        var tag = command.Value("tag");
        var hasIds = command.Has("exercise-id");
        var hasTag = command.Has("tag");
        if (hasIds == hasTag)
            throw new AgentUsageException("assign braucht genau eine Quelle: --exercise-id (wiederholbar) ODER --tag <name>.");
        if (hasTag && string.IsNullOrWhiteSpace(tag))
            throw new AgentUsageException("--tag fehlt ein Name.");

        // None is no goal at all - a position that never counts is not what assigning into a running plan is for.
        GoalCadence? cadence = command.Value("cadence") is { } raw
            ? Enum.TryParse<GoalCadence>(raw, ignoreCase: true, out var parsed) && parsed is GoalCadence.Daily or GoalCadence.Weekly
                ? parsed
                : throw new AgentUsageException($"--cadence erwartet Daily oder Weekly, bekam '{raw}'.")
            : null;

        return new AssignRequest(
            childId,
            command.Has("plan-id") ? command.RequiredInt("plan-id") : null,
            ids,
            hasTag ? tag!.Trim() : null,
            command.Has("points") ? command.RequiredInt("points") : null,
            command.Has("goal-threshold") ? command.RequiredInt("goal-threshold") : null,
            cadence,
            command.Flag("dry-run"));
    }
}

/// <summary>One exercise of an assign run and what happened to it.</summary>
/// <param name="ExerciseId">The catalog exercise.</param>
/// <param name="Title">Its title, for the report.</param>
/// <param name="AlreadyPresent">It is already a position of the plan and was skipped.</param>
/// <param name="PositionId">The position created for it; <c>null</c> when skipped, not yet written or refused.</param>
/// <param name="Assignable">May this account put it into a plan (<c>ExerciseDetail.CanAssign</c>)?</param>
public sealed record AssignItem(int ExerciseId, string Title, bool AlreadyPresent, int? PositionId, bool Assignable = true);

/// <summary>The result of an assign run.</summary>
/// <param name="Request">The request as resolved.</param>
/// <param name="Plan">The target plan.</param>
/// <param name="Items">Every exercise in request order, with its state.</param>
/// <param name="Rejected">The exercise the server refused, which stopped the run; <c>null</c> otherwise.</param>
/// <param name="TagTotal">With <c>--tag</c>: how many exercises the tag holds (never more than were read, see <see cref="PlanAssigner.TagWindow"/>).</param>
public sealed record AssignOutcome(
    AssignRequest Request,
    PlanResponse Plan,
    IReadOnlyList<AssignItem> Items,
    FailedPosition? Rejected,
    int? TagTotal)
{
    /// <summary>The exercises that were new to the plan (to be written, or written).</summary>
    public IReadOnlyList<AssignItem> New => [.. Items.Where(i => !i.AlreadyPresent)];

    /// <summary>New exercises this account may not assign - any of them stops the run before the first write.</summary>
    public IReadOnlyList<AssignItem> NotAssignable => [.. New.Where(i => !i.Assignable)];

    /// <summary>The positions actually created.</summary>
    public IReadOnlyList<int> CreatedPositionIds => [.. Items.Where(i => i.PositionId is not null).Select(i => i.PositionId!.Value)];

    /// <summary>
    /// 0 = done, 1 = the server refused a position (the ones before it stay, a rerun skips them),
    /// <see cref="NothingNewExitCode"/> = nothing new to assign. Not 2: that code means "invoked incorrectly"
    /// across the console, and a rerun that finds everything in place was invoked correctly.
    /// </summary>
    public int ExitCode => Rejected is not null || NotAssignable.Count > 0 ? 1 : New.Count == 0 ? NothingNewExitCode : 0;

    /// <summary>Exit code of a run in which every exercise already was a position of the plan.</summary>
    public const int NothingNewExitCode = 3;

    /// <summary>The report the console prints - child, plan, each exercise's state and the overrides.</summary>
    public IReadOnlyList<string> Report()
    {
        var lines = new List<string>
        {
            $"Kind {Request.ChildId}, Plan {Plan.Id} „{Plan.Title}“ ({Plan.StartDate:yyyy-MM-dd} bis {(Plan.EndDate is { } end ? end.ToString("yyyy-MM-dd") : "offen")}"
            + (Plan.IsPlayable ? ", spielbar)" : ", nicht spielbar)"),
            "Vorgaben: " + Overrides(),
        };
        foreach (var item in Items)
        {
            var state = item.AlreadyPresent ? "schon vorhanden – übersprungen"
                : !item.Assignable ? "nicht zuweisbar – privat, ohne Ausführungsrecht für dieses Konto"
                : item.PositionId is { } pid ? $"angelegt (Position {pid})"
                : Rejected?.ExerciseId == item.ExerciseId ? $"abgelehnt: {Rejected.Error}"
                : Request.DryRun ? "neu"
                : "nicht angelegt (Lauf abgebrochen)";
            lines.Add($"  #{item.ExerciseId} {item.Title}: {state}");
        }
        if (NotAssignable.Count > 0)
            lines.Add($"Abgebrochen vor dem Schreiben: {NotAssignable.Count} Übung(en) nicht zuweisbar – nichts geschrieben.");
        else if (Request.DryRun) lines.Add("Trockenlauf – nichts geschrieben.");
        else if (Rejected is not null)
            lines.Add($"Abgebrochen: {CreatedPositionIds.Count} Position(en) angelegt, die bleiben stehen; ein erneuter Lauf überspringt sie.");
        else if (New.Count == 0) lines.Add("Nichts Neues: alle Übungen stehen schon im Plan.");
        return lines;
    }

    private string Overrides()
    {
        var parts = new List<string>();
        if (Request.Points is { } p) parts.Add($"Punkte {p}");
        if (Request.GoalThreshold is { } g) parts.Add($"Schwelle {g}");
        if (Request.Cadence is { } c) parts.Add($"Rhythmus {c}");
        return parts.Count == 0 ? "Server-Vorgaben" : string.Join(", ", parts);
    }
}

/// <summary>
/// Adds existing catalog exercises to a child's running plan as positions (B-20) - deterministic, no language model.
/// <para>
/// Read everything first, then write: the plan, its positions and every exercise are resolved before the first
/// <c>AddPositionAsync</c>, so a dry run, a wrong id or a foreign plan never leaves half a run behind. Duplicates are
/// skipped here because the server accepts a second position on the same exercise. The first refused write stops the
/// run; what was created stays, and a rerun is safe because it skips those.
/// </para>
/// </summary>
public sealed class PlanAssigner(CreatorApi creator, SupervisorApi supervisor)
{
    /// <summary>
    /// The window read from a tag - by default the server's <c>MaxTake</c>. A tag holding more is refused before
    /// anything is written, never assigned in part. Settable so a test can provoke the case with a small tag.
    /// </summary>
    public int TagWindow { get; init; } = 500;

    /// <summary>Resolves, then (unless a dry run) writes; see the class remarks for the order.</summary>
    public async Task<AssignOutcome> RunAsync(AssignRequest request, CancellationToken ct)
    {
        var plan = await ResolvePlanAsync(request, ct);
        var (exercises, tagTotal) = await ResolveExercisesAsync(request, ct);
        var present = (await supervisor.ListPositionsAsync(plan.Id, ct)).Select(p => p.ExerciseId).ToHashSet();

        var items = exercises.Select(e => new AssignItem(e.Id, e.Title, present.Contains(e.Id), null, e.CanAssign)).ToList();
        // A non-assignable exercise would fail at its POST, after the ones before it were written - refuse the run first.
        if (request.DryRun || items.Any(i => !i.AlreadyPresent && !i.Assignable))
            return new AssignOutcome(request, plan, items, null, tagTotal);

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].AlreadyPresent) continue;
            try
            {
                var position = await supervisor.AddPositionAsync(plan.Id, new CreatePositionDto(
                    ExerciseId: items[i].ExerciseId, Order: null, Stage: null, ItemCount: null, Scope: null,
                    Cadence: request.Cadence, OrderStrategy: null, GoalThreshold: request.GoalThreshold,
                    RequireTypedTest: null, UseLeitner: null, MaxBox: null, BoxIntervalDays: null, StageSchedule: null,
                    PointsGoalMet: request.Points, PenaltyCoins: null, NewContentPoints: null, ComboThreshold: null,
                    ComboBonusPoints: null, SpeedThresholdSeconds: null, SpeedBonusPoints: null), ct);
                items[i] = items[i] with { PositionId = position.Id };
            }
            catch (PuglingApiException ex)
            {
                return new AssignOutcome(request, plan, items,
                    new FailedPosition(items[i].ExerciseId, $"{(int)ex.StatusCode} {ex.Code}: {ex.Message}"), tagTotal);
            }
        }
        return new AssignOutcome(request, plan, items, null, tagTotal);
    }

    /// <summary>
    /// The explicit plan (must belong to the child) or the child's only playable one (<c>IsPlayable</c>); several playable
    /// plans are a usage error that lists them.
    /// </summary>
    private async Task<PlanResponse> ResolvePlanAsync(AssignRequest request, CancellationToken ct)
    {
        if (request.PlanId is { } planId)
        {
            PlanResponse plan;
            try
            {
                plan = await supervisor.GetPlanAsync(planId, ct);
            }
            // Unknown and foreign read alike to the caller: this account cannot write into that plan.
            catch (PuglingApiException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
            {
                throw new AgentUsageException($"Plan {planId} ist für dieses Konto nicht sichtbar.");
            }
            return plan.ChildId == request.ChildId
                ? plan
                : throw new AgentUsageException($"Plan {planId} gehört nicht zu Kind {request.ChildId}, sondern zu Kind {plan.ChildId}.");
        }

        // IsPlayable is the server's own rule (active AND within its run) - an active but expired plan is the wrong target.
        var playable = (await supervisor.ListPlansAsync(request.ChildId, ct)).Where(p => p.IsPlayable).ToList();
        // One active plan per subject: several can be playable, and guessing one would put the exercises into another
        // subject's plan. The caller names the target; the agent never picks by subject.
        if (playable.Count > 1)
            throw new AgentUsageException(
                $"Kind {request.ChildId} hat {playable.Count} spielbare Pläne – nenne den Zielplan mit --plan-id: "
                + string.Join("; ", playable.Select(p => $"{p.Id} „{p.Title}“ (Fach {p.SubjectId?.ToString() ?? "–"})"))
                + ".");
        // Not "create one with plan": that verb creates the plan inactive, so the hint would lead into the next lock.
        return playable.Count == 1 ? playable[0] : throw new AgentUsageException(
            $"Kind {request.ChildId} hat keinen spielbaren Plan. Nenne einen seiner Pläne mit --plan-id, oder schalte einen "
            + "unter /vater/plaene scharf (ein mit plan erzeugter Plan entsteht inaktiv).");
    }

    /// <summary>The exercises to assign, in request order; every id is checked before anything is written.</summary>
    private async Task<(List<(int Id, string Title, bool CanAssign)> Exercises, int? TagTotal)> ResolveExercisesAsync(
        AssignRequest request, CancellationToken ct)
    {
        if (request.TagName is { } tagName)
        {
            // Tags are child-scoped: the same name on another child is another tag with other exercises.
            var tag = PickTag(await creator.ListTagsAsync(request.ChildId, ct), tagName, request.ChildId);
            var page = await creator.ListTagExercisesAsync(tag.Id, 0, TagWindow, ct);
            if (page.Items.Count == 0)
                throw new AgentUsageException($"Der Tag „{tagName}“ trägt keine Übung.");
            if (page.Truncated)
                throw new AgentException($"Der Tag „{tagName}“ trägt {page.TotalCount} Übungen, gelesen werden höchstens "
                    + $"{TagWindow} – nichts zugewiesen. Teile den Tag oder nenne die Übungen mit --exercise-id.");
            // The tag list is lean (ExerciseBrief, no grants): the detail carries the assign rule.
            var tagged = new List<(int, string, bool)>();
            foreach (var brief in page.Items)
                tagged.Add((brief.Id, brief.Title, (await creator.GetExerciseDetailAsync(brief.Id, ct)).CanAssign));
            return (tagged, page.TotalCount);
        }

        var exercises = new List<(int, string, bool)>();
        foreach (var id in request.ExerciseIds)
        {
            try
            {
                var detail = await creator.GetExerciseDetailAsync(id, ct);
                exercises.Add((detail.Id, detail.Title, detail.CanAssign));
            }
            catch (PuglingApiException ex) when (ex.StatusCode is HttpStatusCode.NotFound)
            {
                throw new AgentUsageException($"Übung {id} gibt es nicht.");
            }
        }
        return (exercises, null);
    }

    /// <summary>
    /// The exact name wins; otherwise a single case-insensitive hit. Several case-insensitive hits without an exact one
    /// ("Klausur" and "KLAUSUR" for "klausur") are ambiguous - picking one would assign another bundle than meant.
    /// </summary>
    private static TagResponse PickTag(IReadOnlyList<TagResponse> tags, string name, int childId)
    {
        if (tags.FirstOrDefault(t => t.Name == name) is { } exact) return exact;
        var hits = tags.Where(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
        return hits.Count switch
        {
            1 => hits[0],
            0 => throw new AgentUsageException($"Kind {childId} hat keinen Tag „{name}“."),
            _ => throw new AgentUsageException($"„{name}“ passt auf mehrere Tags von Kind {childId} "
                + $"({string.Join(", ", hits.Select(t => $"„{t.Name}“"))}) – nenne ihn in genauer Schreibweise."),
        };
    }
}
