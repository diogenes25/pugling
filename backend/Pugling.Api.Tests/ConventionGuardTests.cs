using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pugling.Api.Auth;

namespace Pugling.Api.Tests;

/// <summary>
/// Turns four conventions from CLAUDE.md into a gate (docs/codequalitaet-gates-plan.md, B4). They have
/// been followed without exception so far – but enforced by nothing: a generated controller with
/// <c>return BadRequest("…")</c> compiles, runs, returns a <c>ProblemDetails</c> without a <c>code</c>,
/// and no test notices.
/// <para>
/// Every test carries a <b>self-protection against a false green</b>: if the reflection or the source
/// scan does not engage (renamed namespace, moved folder, wrong attribute), it finds nothing and would
/// pass with an empty check. That's why every test additionally verifies that it actually saw enough.
/// </para>
/// </summary>
public class ConventionGuardTests
{
    // The surface and the route resolution come from `ApiSurface`, so that this guard, the ownership matrix
    // (C1) and the coverage guard (C4) see the same definition of "action" and the same route.
    private static IEnumerable<Type> Controllers() => ApiSurface.Controllers();

    private static IEnumerable<MethodInfo> Actions(Type controller) => ApiSurface.Actions(controller);

    // ─────────────────────────────────────────────────────── (a) errors only through ProblemWithCode

    [Fact]
    public void Actions_Melden_Fehler_Nur_Ueber_ProblemWithCode()
    {
        // A source scan instead of reflection: the convention concerns the method *body*, which reflection does not see.
        var controllerDir = Path.Combine(RepoRoot(), "backend", "Pugling.Api", "Controllers");
        var files = Directory.GetFiles(controllerDir, "*.cs", SearchOption.AllDirectories);

        // `BadRequest(` and a *raw* `Problem(`. The negative lookbehind separates `ProblemWithCode(` and
        // `ValidationProblem(`: there "Problem" is not followed by "(", or a word character precedes it.
        var forbidden = new Regex(@"\bBadRequest\s*\(|(?<!\w)Problem\s*\(", RegexOptions.Compiled);
        var blessed = new Regex(@"\bProblemWithCode\s*\(", RegexOptions.Compiled);

        var offenders = new List<string>();
        var blessedHits = 0;
        foreach (var file in files)
        {
            var lineNo = 0;
            foreach (var raw in File.ReadLines(file))
            {
                lineNo++;
                var line = raw.TrimStart();
                if (line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith('*'))
                    continue; // comments quote the rule, they do not violate it.
                if (blessed.IsMatch(line))
                    blessedHits++;
                if (forbidden.IsMatch(line))
                    offenders.Add($"{Path.GetFileName(file)}:{lineNo}: {line}");
            }
        }

        // Self-protection: the scan must really have read the folder - there are plenty of controllers and
        // three-digit numbers of `ProblemWithCode` calls.
        Assert.True(files.Length >= 30, $"Zu wenige Controller-Dateien gefunden ({files.Length}) – Pfad falsch?");
        Assert.True(blessedHits >= 100, $"Too few ProblemWithCode calls found ({blessedHits}) - the scan does not bite.");
        Assert.True(offenders.Count == 0,
            "Errors have to go through this.ProblemWithCode(ApiErrors.…) (RFC 7807 + a machine-readable code):\n"
            + string.Join("\n", offenders));
    }

    // ─────────────────────────────────────────────────────── (e) no generic Conflict in a controller

    [Fact]
    public void Controller_Nennt_Keinen_Generischen_Conflict_Code()
    {
        // ApiErrors.Conflict is the framework/middleware safety net (ApiErrors.ForStatus) for a 409 that
        // reaches no more specific handling - a controller that reaches for it directly always had a more
        // specific business condition available (B-101: AuthController had DuplicateEmail two files away,
        // ExerciseCategoriesController's sibling chapter check already had its own code). An empty exception
        // list is the point: the moment a fourth generic use appears, it gets its own code before this test
        // is touched, not an entry added to a list that would otherwise only grow.
        var controllerDir = Path.Combine(RepoRoot(), "backend", "Pugling.Api", "Controllers");
        var files = Directory.GetFiles(controllerDir, "*.cs", SearchOption.AllDirectories);
        var forbidden = new Regex(@"\bApiErrors\.Conflict\b", RegexOptions.Compiled);

        var offenders = new List<string>();
        foreach (var file in files)
        {
            var lineNo = 0;
            foreach (var raw in File.ReadLines(file))
            {
                lineNo++;
                var line = raw.TrimStart();
                if (line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith('*'))
                    continue;
                if (forbidden.IsMatch(line))
                    offenders.Add($"{Path.GetFileName(file)}:{lineNo}: {line}");
            }
        }

        Assert.True(files.Length >= 30, $"Zu wenige Controller-Dateien gefunden ({files.Length}) – Pfad falsch?");
        Assert.True(offenders.Count == 0,
            "Ein Controller meldet einen fachlichen Konflikt nicht generisch, sondern über einen eigenen "
            + "ApiErrors-Code (siehe ApiErrors.Conflict selbst - es bleibt der Fallback von ApiErrors.ForStatus):\n"
            + string.Join("\n", offenders));
    }

    // ─────────────────────────────────────────────────────── (f) one placeholder name per collection segment

    /// <summary>
    /// Pinned red list (B-101/B-121): every entry is a literal path segment that today carries more than one
    /// placeholder name in different routes. Each tuple is either a real debt (two names for the SAME entity,
    /// never unified) or a different entity reached through the same literal segment (correct, not debt) -
    /// the reason column carries that distinction, the pinning mechanism itself does not need to. A new entry
    /// here is new scope, not decoration: it means either a genuinely new inconsistency was introduced, or an
    /// existing one was found and not yet fixed - either way it is meant to draw attention when it grows.
    /// </summary>
    private static readonly (string Segment, string Name, string Reason)[] PlaceholderRedList =
    [
        ("exercises", "exerciseId", "debt: ExerciseGrantsController/ExerciseMediaController vs. `id` in ExerciseCatalogController/ExercisePreviewController - same Exercise entity"),
        ("media", "assetId", "debt: MediaVariantsController vs. `id` in MediaAssetsController - same MediaAsset entity"),
        ("media", "linkId", "correct: a MediaLink is a different entity, reached through the same literal 'media' segment"),
        ("vocabulary", "vocabularyId", "debt: VocabularyMediaController vs. `id` in VocabularyStoreController - same Vocabulary store entry"),
        ("vocabulary", "exerciseId", "correct: the vocabulary EXERCISE type route (textbook-series/…/units/…/vocabulary/{exerciseId}) is a different entity"),
        ("tags", "id", "debt: VocabularyTagsController's nested tags/{id} vs. `tagId` in TagsController - same Tag entity"),
        ("units", "seriesUnitId", "debt: ExerciseRoutes.Base vs. `unitId` in SeriesUnitsController - same SeriesUnit entity"),
    ];

    private static readonly Regex PlaceholderSegment = new(@"^\{(?<name>[A-Za-z0-9_]+)(?::[^}]*)?\}$", RegexOptions.Compiled);

    [Fact]
    public void Sammlungs_Segment_Traegt_Hoechstens_Einen_Platzhalternamen()
    {
        // Walk every route template pairwise: a literal segment immediately followed by a placeholder segment
        // is the pair this guard cares about ("exercises/{exerciseId}" -> ("exercises", "exerciseId")).
        var observed = new List<(string Segment, string Name)>();
        foreach (var controller in ApiSurface.Controllers())
            foreach (var action in ApiSurface.ActionsIncludingInherited(controller))
            {
                var segments = ApiSurface.RouteOf(controller, action).Split('/');
                for (var i = 0; i < segments.Length - 1; i++)
                {
                    if (PlaceholderSegment.IsMatch(segments[i])) continue;
                    var next = PlaceholderSegment.Match(segments[i + 1]);
                    if (next.Success)
                        observed.Add((segments[i], next.Groups["name"].Value));
                }
            }

        var redListBysegment = PlaceholderRedList.ToLookup(r => r.Segment, r => r.Name);
        var offenders = new List<string>();
        foreach (var group in observed.GroupBy(o => o.Segment))
        {
            var remaining = group.Select(o => o.Name).Distinct()
                .Where(n => !redListBysegment[group.Key].Contains(n))
                .ToList();
            if (remaining.Count > 1)
                offenders.Add($"{group.Key}: {string.Join(", ", remaining)} (nicht in der Rot-Liste)");
        }

        Assert.True(observed.Count >= 150, $"Zu wenige Segment/Platzhalter-Paare gefunden ({observed.Count}) - die Reflexion greift nicht.");
        Assert.True(offenders.Count == 0,
            "Ein Sammlungs-Segment trägt mehr als einen Platzhalternamen, ohne in der Rot-Liste zu stehen "
            + "(neuer Fund oder neue Route mit einem zweiten Namen):\n" + string.Join("\n", offenders));
    }

    // ─────────────────────────────────────────────────────── (g) unpaginated array GETs are pinned

    private static bool ReturnsCollection(MethodInfo action)
    {
        var t = action.ReturnType;
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>))
            t = t.GetGenericArguments()[0];
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ActionResult<>))
            t = t.GetGenericArguments()[0];
        return t != typeof(string) && t.IsGenericType
            && typeof(System.Collections.IEnumerable).IsAssignableFrom(t);
    }

    /// <summary>
    /// Exact pin (B-121), measured 2026-08-06 - deliberately not an upper bound: a shrinking count is just as
    /// much a deliberate line as a growing one (README, "Der Nenner ist die Falle"). It shrinks when an
    /// endpoint gains pagination (B-121 form (a), not decided/built here - that changes response shape for
    /// existing unbounded callers and is a product decision, not an "Aufräumen"); it grows when a new
    /// unpaginated array `GET` is added.
    /// </summary>
    private const int UnpaginatedArrayGetCount = 34;

    [Fact]
    public void Unpaginierte_Array_GETs_Sind_Gepinnt()
    {
        var offenders = new List<string>();
        foreach (var controller in ApiSurface.Controllers())
            foreach (var action in ApiSurface.ActionsIncludingInherited(controller))
            {
                if (ApiSurface.MethodOf(action) != "GET") continue;
                if (!ReturnsCollection(action)) continue;
                if (action.GetParameters().Any(p => p.Name!.Equals("take", StringComparison.OrdinalIgnoreCase))) continue;
                offenders.Add($"{controller.Name}.{action.Name} [{RouteOf(controller, action)}]");
            }

        Assert.True(offenders.Count == UnpaginatedArrayGetCount,
            $"{offenders.Count} unpaginierte Array-GETs gefunden, {UnpaginatedArrayGetCount} gepinnt. "
            + "Eine neue Zahl ist eine bewusste Zeile (README, \"Der Nenner ist die Falle\"), keine stille "
            + "Anpassung:\n" + string.Join("\n", offenders.OrderBy(x => x)));
    }

    // ─────────────────────────────────────────────────────── (c) the contract lives in Pugling.Contracts

    [Fact]
    public void Vertragstypen_Sind_Global_Namens_Eindeutig()
    {
        // The OpenAPI generator keys schemas by the *simple* type name: two identically named records in
        // different namespaces merge silently into one schema. The bug is invisible in the contract and only
        // surfaces in the generated client.
        var types = typeof(PointKind).Assembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsNested)
            .ToList();

        var duplicates = types.GroupBy(t => t.Name)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(t => t.Namespace))}")
            .ToList();

        Assert.True(types.Count >= 200, $"Zu wenige Vertragstypen gefunden ({types.Count}) – falsche Assembly?");
        Assert.True(duplicates.Count == 0,
            "Gleichnamige Vertragstypen verschmelzen im OpenAPI-Schema:\n" + string.Join("\n", duplicates));
    }

    [Fact]
    public void Actions_Geben_Nur_Vertragstypen_Zurueck()
    {
        // "Project DTOs as records - never return EF entities" (CLAUDE.md), captured mechanically: whatever an
        // action returns as a payload must come from Pugling.Contracts. A returned entity would drag
        // navigations and internal fields into the response.
        var contracts = typeof(PointKind).Assembly;
        var apiAssembly = typeof(Program).Assembly;
        var offenders = new List<string>();
        var checkedActions = 0;

        foreach (var controller in Controllers())
            foreach (var action in Actions(controller))
            {
                var payload = PayloadType(action.ReturnType);
                if (payload is null)
                    continue; // IActionResult without a type parameter - nothing to check
                checkedActions++;
                foreach (var leaf in LeafTypes(payload))
                    if (leaf.Assembly == apiAssembly)
                        offenders.Add($"{controller.Name}.{action.Name} → {leaf.Name} (lives in Pugling.Api, not in the contract)");
            }

        Assert.True(checkedActions >= 100, $"Too few typed actions found ({checkedActions}) - the reflection does not bite.");
        Assert.True(contracts.GetTypes().Length > 0);
        Assert.True(offenders.Count == 0,
            "Antworttypen gehören ins Vertrags-Projekt (nie EF-Entities):\n" + string.Join("\n", offenders));
    }

    // ─────────────────────────────────────────────────────── (d) pass the CancellationToken on

    [Fact]
    public void Async_Actions_Nehmen_Einen_CancellationToken()
    {
        // "Pass the CancellationToken on" (CLAUDE.md) starts at the action: without the parameter there is
        // nothing to pass on, and an aborted request keeps running server-side.
        //
        // It was a growth barrier with a baseline until 2026-07-30, because the convention was *not* followed
        // in the existing code (189 of 337 async actions without a token, measured on 2026-07-29) - a hard rule
        // would only have been switchable back then. The backlog is worked off, so it now holds hard like the
        // other three of this class.
        //
        // What this guard does *not* check: that the token actually arrives. It sees the **signature** of the
        // action, not the chain behind it. A helper without a token parameter hides every call in its body from
        // CA2016, and in lambdas the analyzer stays silent anyway - while working off the backlog that was
        // exactly the failure class (an abort leak in `MediaAssetsController.Upload` sat behind an action that
        // had the token long since, and was therefore never visible here).
        var offenders = new List<string>();
        var checkedActions = 0;

        foreach (var controller in Controllers())
            foreach (var action in Actions(controller))
            {
                // Async actions only: a synchronous one has no cancellable work.
                if (!typeof(Task).IsAssignableFrom(action.ReturnType))
                    continue;
                checkedActions++;
                if (!action.GetParameters().Any(p => p.ParameterType == typeof(CancellationToken)))
                    offenders.Add($"{controller.Name}.{action.Name}");
            }

        Assert.True(checkedActions >= 150, $"Too few async actions found ({checkedActions}) - the reflection does not bite.");
        Assert.True(offenders.Count == 0,
            $"{offenders.Count} async action(s) without a CancellationToken. The token belongs as the last "
            + "parameter on the action (`CancellationToken ct = default` - the default value is needed "
            + "because C# allows no required parameter after the optional `[FromQuery]` values) "
            + "and from there into every EF/service call:\n" + string.Join("\n", offenders));
    }

    // ─────────────────────────────────────────────────────── (b) ownership through the shared filters

    [Fact]
    public void Actions_Unter_ChildId_Oder_PlanId_Tragen_Den_Ownership_Filter()
    {
        // "For endpoints under {planId} use the PlanOwnershipFilter, under {childId} the ChildOwnershipFilter
        // (do not repeat it inline)" - CLAUDE.md. A new route without the filter is an IDOR hole.
        var offenders = new List<string>();
        var checkedActions = 0;

        foreach (var controller in Controllers())
        {
            var classFilters = ServiceFilterTypes(controller.GetCustomAttributes<ServiceFilterAttribute>(inherit: true));
            foreach (var action in Actions(controller))
            {
                var route = RouteOf(controller, action);
                var needsChild = route.Contains("{childId", StringComparison.Ordinal);
                var needsPlan = route.Contains("{planId", StringComparison.Ordinal);
                if (!needsChild && !needsPlan)
                    continue;
                if (OwnershipExceptions.Contains($"{controller.Name}.{action.Name}"))
                    continue;

                checkedActions++;
                var filters = classFilters
                    .Concat(ServiceFilterTypes(action.GetCustomAttributes<ServiceFilterAttribute>(inherit: false)))
                    .ToList();
                if (needsChild && !filters.Contains(typeof(ChildOwnershipFilter)))
                    offenders.Add($"{controller.Name}.{action.Name} ({route}) without ChildOwnershipFilter");
                if (needsPlan && !filters.Contains(typeof(PlanOwnershipFilter)))
                    offenders.Add($"{controller.Name}.{action.Name} ({route}) without PlanOwnershipFilter");
            }
        }

        Assert.True(checkedActions >= 50, $"Too few child-/plan-bound actions found ({checkedActions}) - the route resolution does not bite.");
        Assert.True(offenders.Count == 0,
            "Kindes-/plan-gebundene Actions brauchen den geteilten Ownership-Filter:\n" + string.Join("\n", offenders));
    }

    /// <summary>
    /// Deliberate exceptions to (b) – **not** a catch-all, but decisions with a reason. If this list
    /// grows, the reason belongs with it, otherwise it hollows out the gate.
    /// </summary>
    private static readonly HashSet<string> OwnershipExceptions = [];

    // ─────────────────────────────────────────────────────── (f) anonymous means throttled

    [Fact]
    public void Anonyme_Actions_Tragen_EnableRateLimiting()
    {
        // B-120: after B-48, all five anonymously reachable actions carry [EnableRateLimiting("login")] -
        // but that is five correctly set attributes, not a rule. This gate is the mechanical version: the
        // next [AllowAnonymous] action that forgets the brake turns this test red instead of staying
        // unnoticed until a public instance meets it. Empty exception list on purpose (see below).
        var offenders = new List<string>();
        var checkedActions = 0;

        foreach (var controller in Controllers())
        {
            var classHasAllowAnonymous = controller.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) is not null;
            var classHasRateLimiting = controller.GetCustomAttribute<EnableRateLimitingAttribute>(inherit: true) is not null;
            foreach (var action in Actions(controller))
            {
                var isAnonymous = classHasAllowAnonymous || action.GetCustomAttribute<AllowAnonymousAttribute>(inherit: false) is not null;
                if (!isAnonymous)
                    continue;
                if (AnonymousRateLimitExceptions.Contains($"{controller.Name}.{action.Name}"))
                    continue;

                checkedActions++;
                var hasRateLimiting = classHasRateLimiting || action.GetCustomAttribute<EnableRateLimitingAttribute>(inherit: false) is not null;
                if (!hasRateLimiting)
                    offenders.Add($"{controller.Name}.{action.Name} ([AllowAnonymous] without [EnableRateLimiting])");
            }
        }

        Assert.True(checkedActions >= 5, $"Too few anonymous actions found ({checkedActions}) - the reflection does not bite.");
        Assert.True(offenders.Count == 0,
            "Anonym erreichbare Actions brauchen eine Ratenbegrenzung:\n" + string.Join("\n", offenders));
    }

    /// <summary>
    /// Deliberately empty: every anonymous action today is a write (login/registration) and needs the
    /// brake. An anonymous **read** endpoint would be a legitimate exception - add it here with a reason
    /// when one exists, so the list stays a set of decisions instead of a catch-all.
    /// </summary>
    private static readonly HashSet<string> AnonymousRateLimitExceptions = [];

    // ─────────────────────────────────────────────────────── (e) a solution never travels to a student

    [Fact]
    public void Actions_Mit_Loesungsfeld_Sind_Vor_Dem_Studenten_Gegated()
    {
        // The reach of a *read* DTO is a property of its type, not of the endpoint that happens to return it
        // (B-80/E1). The sharpest case of that is a named solution: `ItemReport.Answer` carried the answer of
        // every card - also of cards the child had never been shown - and no ownership check catches it,
        // because the plan really is the child's own (B-82).
        //
        // So the rule follows the secret, not the folder: whatever names a solution anywhere in its payload
        // graph has to be gated to a role set *without* Student. Creator-gated satisfies it just as well as
        // supervisor-gated - an author must see the answer of the exercise they are writing.
        //
        // Why not "DTO under Contracts.Supervisor ⇒ Roles.Supervisor", which was the original plan (B-82/E3):
        // measured, that takes ten exceptions, and six of them enumerate the normal case - a child reads its
        // own study plan and its own big goals, so `PlanResponse`/`ObjectiveResponse` are dual-read *as types*
        // (`StudentPlansController` is even Student-only by design). By E4's own argument an exception list
        // that lists the normal case proves nothing. The tier folder is a proxy; the solution field is the thing.
        var offenders = new List<string>();
        var inScope = new List<string>();
        // Those a student token could actually reach - the set an exception has to be justified against.
        var reachable = new List<string>();

        foreach (var controller in Controllers())
        {
            var classGated = HidesFromStudent(controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true));
            // Inherited actions on purpose (not the shared `Actions()`): the exercise CRUD lives on
            // `ExerciseControllerBase` and returns `ExerciseResponse<TConfig>` - the sharpest solution fields
            // of the API sit there, and `DeclaredOnly` would hide all of them from this gate.
            foreach (var action in ApiSurface.ActionsIncludingInherited(controller))
            {
                var payload = PayloadType(action.ReturnType);
                if (payload is null)
                    continue; // IActionResult without a type parameter - no payload to judge
                if (SolutionFieldIn(payload, [], depth: 0) is not { } field)
                    continue;

                var key = ApiSurface.Key(controller, action);
                inScope.Add(key);
                // `[AllowAnonymous]` wins over any [Authorize] above it, so an action carrying it is open to
                // everyone - worse than open to a child. Judging the roles alone would call that gated.
                var open = action.GetCustomAttributes<AllowAnonymousAttribute>(inherit: false).Any();
                if (!open && (classGated || HidesFromStudent(action.GetCustomAttributes<AuthorizeAttribute>(inherit: false))))
                    continue;
                reachable.Add(key);
                if (!SolutionFieldExceptions.ContainsKey(key))
                    offenders.Add($"{key} ({RouteOf(controller, action)}) hands out {field} to a student token");
            }
        }

        // Self-protection against an empty green, re-measured on 2026-08-03 after `Translation` joined the name
        // list (42 actions in scope) and set just below it - a hand-guessed bound is either a red gate without a
        // defect or one that never bites. Re-measured rather than nudged: the earlier bound of 25 came from a
        // 30-action scope and would have stayed green through a third of the surface falling out of it. The
        // number is this high only because the scope includes inherited actions: declared-only it was 10 and
        // left the whole exercise CRUD outside.
        Assert.True(inScope.Count >= 38,
            $"Too few actions with a solution field found ({inScope.Count}) - the reflection does not bite.");

        // The exception list points at actions by `Controller.Action`. Rename one and the entry aims at
        // nothing: the endpoint would be ungated *and* unnoticed. So every entry has to hit an action that is
        // in scope *and* actually reachable by a student - an entry that has since been gated is dead weight
        // that reads like a permitted leak (pattern: PatchSemanticsTests holds every switch against its table).
        var stale = SolutionFieldExceptions.Keys.Where(k => !reachable.Contains(k)).ToList();
        Assert.True(stale.Count == 0,
            "Ausnahmen zeigen auf keine Action, die ein Lösungsfeld ungegated herausgibt (umbenannt oder erledigt?):\n"
            + string.Join("\n", stale));

        Assert.True(offenders.Count == 0,
            "Ein Lösungsfeld darf kein Student-Token erreichen – die Action braucht [Authorize(Roles = …)] ohne Student:\n"
            + string.Join("\n", offenders));
    }

    /// <summary>
    /// Property names that mean "the solution" – deliberately short, and the shortness is measured, not
    /// guessed. <c>Expected</c> is <b>not</b> among them: it is the reveal *after* the child answered
    /// (<c>ItemOutcome</c>, <c>ReviewOutcome</c>, <c>ItemCheck</c>), which is the point of the feedback rather
    /// than a leak – with it in the list the exception list grows from 4 to 16.
    /// <para>
    /// <c>Translation</c> joined the list with B-81, and the cost was measured for that one name alone rather
    /// than taken from the four-name figure below: it raises the scope from 30 to 42 actions and costs exactly
    /// <b>one</b> further exception, because the only other hit was a real defect
    /// (<c>TagsController.GetVocabulary</c> handed <c>TaggedVocabularyDto.Translation</c> to a child token).
    /// </para>
    /// <para>
    /// <b>The known limit of a name-based rule.</b> <c>Back</c>, <c>Target</c> and <c>Reveal</c> stay out.
    /// Measured on 2026-08-03: together with <c>Translation</c> they raise the scope from 30 to 68 actions and
    /// cost <b>10 further exceptions</b> – and those enumerate the normal case, which is exactly the argument
    /// that discarded the namespace-based draft of this gate: <c>PracticeCard.Reveal</c> and
    /// <c>TestItem.Reveal</c> are what a card is *for*, <c>MissionStatus.Target</c> is a target count and not a
    /// translation, and <c>ItemProgressResponse.Back</c> is the child's progress on words it has already
    /// answered. This is a deliberate hole in the net, not a forgotten one.
    /// </para>
    /// </summary>
    private static readonly string[] SolutionPropertyNames = ["Answer", "Solution", "CorrectAnswer", "Translation"];

    /// <summary>
    /// Deliberate exceptions to (e), each with its reason – the list is the decision, not a bypass. The four
    /// remark entries are the same collision: on a remark, <c>Answer</c> is the reply text of a dev note, not
    /// the solution of a card. A name-based rule cannot tell two meanings of one word apart, so they are
    /// named here.
    /// <para>
    /// The collision alone would also excuse handing a real solution to a child, so the second half matters:
    /// the controller blanks the field for a student token at runtime (<c>MaySeeAnswers</c>). Both reasons are
    /// written out, because an exception justified only by the name would survive the field turning into a
    /// real secret.
    /// </para>
    /// <para>
    /// The fifth entry is a different kind: the field really is a translation, but it is the child's <i>own</i>
    /// learning state over words it has already answered – so it reveals nothing it has not seen.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, string> SolutionFieldExceptions = new()
    {
        ["RemarksController.Create"] = "RemarkDto.Answer is a dev note's reply, and MaySeeAnswers blanks it for a student.",
        ["RemarksController.GetOne"] = "RemarkDto.Answer is a dev note's reply, and MaySeeAnswers blanks it for a student.",
        ["RemarksController.List"] = "RemarkDto.Answer is a dev note's reply, and MaySeeAnswers blanks it for a student.",
        ["RemarksController.Update"] = "RemarkDto.Answer is a dev note's reply, and MaySeeAnswers blanks it for a student.",
        ["ChildVocabularyProgressController.ByWord"] =
            "WordMasteryResponse.Translation is the child's own progress; the query reads ItemProgress filtered "
            + "by ChildId, so a row exists only for a word the child has already answered.",
    };

    /// <summary>
    /// Does an attribute set restrict access to roles that exclude <see cref="Roles.Student"/>? A bare
    /// <c>[Authorize]</c> does not: it only asks for *any* logged-in user, and a child is one.
    /// </summary>
    private static bool HidesFromStudent(IEnumerable<AuthorizeAttribute> attributes) =>
        attributes.Any(a => a.Roles is { Length: > 0 } roles
            && !roles.Split(',').Select(r => r.Trim()).Contains(Roles.Student, StringComparer.Ordinal));

    /// <summary>
    /// Walks the contract types of a payload (properties, collections, nesting) and returns the first
    /// solution-named field as <c>Type.Property</c>, or <c>null</c>. Depth-bounded and cycle-safe, so a
    /// self-referencing DTO cannot hang the guard.
    /// </summary>
    private static string? SolutionFieldIn(Type type, HashSet<Type> seen, int depth)
    {
        if (depth > 6)
            return null;
        foreach (var leaf in LeafTypes(type))
        {
            // Only our own contract types - walking into string/DateTime internals finds nothing and costs time.
            if (leaf.Namespace?.StartsWith("Pugling.Contracts", StringComparison.Ordinal) != true)
                continue;
            if (!seen.Add(leaf))
                continue;
            foreach (var property in leaf.GetProperties())
            {
                if (SolutionPropertyNames.Contains(property.Name, StringComparer.Ordinal))
                    return $"{leaf.Name}.{property.Name}";
                if (SolutionFieldIn(property.PropertyType, seen, depth + 1) is { } nested)
                    return nested;
            }
        }
        return null;
    }

    // ─────────────────────────────────────────────────────── Helpers

    /// <summary>The service filter types of an attribute set.</summary>
    private static IEnumerable<Type> ServiceFilterTypes(IEnumerable<ServiceFilterAttribute> attributes) =>
        attributes.Select(a => a.ServiceType);

    private static string RouteOf(Type controller, MethodInfo action) => ApiSurface.RouteOf(controller, action);

    /// <summary>The payload behind <c>Task&lt;ActionResult&lt;T&gt;&gt;</c> and friends; <c>null</c> if untyped.</summary>
    private static Type? PayloadType(Type returnType)
    {
        var t = returnType;
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>))
            t = t.GetGenericArguments()[0];
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ActionResult<>))
            return t.GetGenericArguments()[0];
        return null;
    }

    /// <summary>Unwraps collection/nullable wrappers down to the actually transmitted types.</summary>
    private static IEnumerable<Type> LeafTypes(Type type)
    {
        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
                foreach (var leaf in LeafTypes(arg))
                    yield return leaf;
            yield break;
        }
        if (type.IsArray && type.GetElementType() is { } element)
        {
            foreach (var leaf in LeafTypes(element))
                yield return leaf;
            yield break;
        }
        yield return type;
    }

    // ─────────────────────────────────────────── (x) free-text search folds case (B-135)

    /// <summary>
    /// Every free-text search goes through <see cref="Pugling.Api.Services.Shared.SearchPattern"/>, never
    /// through <c>string.Contains</c>.
    /// <para>
    /// <b>Why this needs a gate at all.</b> The rule is invisible: <c>x.Title.Contains(search)</c> is the
    /// obvious thing to write, it compiles, it returns rows - and on SQLite it silently searches
    /// byte-exact, because EF maps it to <c>instr()</c>, which ignores the column collation. Nothing in
    /// the type system, the analyzers or a passing test says so. B-128 fixed two call sites, B-135 the
    /// remaining seven; without this test the eighth arrives unnoticed.
    /// </para>
    /// <para>
    /// <b>What it does not catch, so nobody mistakes it for complete.</b> It is a text scan, not a
    /// compiler, and it is bounded twice. By <em>identifier</em>: it keys on the names this repo gives a
    /// search term - anything containing <c>search</c> or <c>term</c> (so <c>searchTerm</c> too), plus
    /// <c>word</c> and <c>translation</c>. A parameter called <c>needle</c> slips past, and so does a term
    /// reached through a property. And by <em>directory</em>: only <c>Controllers/</c> and
    /// <c>Services/</c> are scanned - a query written in <c>Data/</c> is not covered (there is none today;
    /// measured while writing this). That is the accepted price of a crude parser (the lesson from B-40) -
    /// it covers the shape that gets written here, and its exception list carries a reason per entry so it
    /// never grows by reflex.
    /// </para>
    /// </summary>
    [Fact]
    public void Freitextsuchen_Falten_Die_Schreibweise()
    {
        var apiDir = Path.Combine(RepoRoot(), "backend", "Pugling.Api");
        var files = Directory.GetFiles(Path.Combine(apiDir, "Controllers"), "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(apiDir, "Services"), "*.cs", SearchOption.AllDirectories))
            .ToArray();

        // `\w*` around the stems on purpose: `searchTerm` is the likeliest name of all, and an anchored
        // \b after `search` would let exactly it through - the one variant this rule cannot afford to miss.
        var forbidden = new Regex(@"\.Contains\s*\(\s*\w*(?i:search|term|word|translation)\w*",
            RegexOptions.Compiled);

        // The one legitimate `Contains` on a search term: it compares IN MEMORY (no SQL translation), and
        // it already folds case itself - `SearchPattern` would be wrong there, not missing. Matched on the
        // full signature rather than the bare method name, so the exemption covers that one line and not
        // every future line of the file that happens to mention `Matches`.
        var allowed = new[]
        {
            "ChildLearnProgressService.cs:private static bool Matches(",
        };

        var offenders = new List<string>();
        var blessedHits = 0;
        foreach (var file in files)
        {
            var lineNo = 0;
            foreach (var raw in File.ReadLines(file))
            {
                lineNo++;
                var line = raw.TrimStart();
                if (line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith('*'))
                    continue; // comments quote the rule, they do not violate it.
                // Must come first: the blessed call is itself spelled `SearchPattern.Contains(search)`.
                if (line.Contains("SearchPattern.Contains(", StringComparison.Ordinal))
                {
                    blessedHits++;
                    continue;
                }
                if (!forbidden.IsMatch(line))
                    continue;
                if (allowed.Any(a => file.EndsWith(a.Split(':')[0], StringComparison.Ordinal)
                                     && line.Contains(a.Split(':')[1], StringComparison.Ordinal)))
                    continue;
                offenders.Add($"{Path.GetFileName(file)}:{lineNo}: {line}");
            }
        }

        // Self-protection: without it a wrong path would make this test green by finding nothing at all.
        Assert.True(files.Length >= 40, $"Too few source files found ({files.Length}) - wrong path?");
        Assert.True(blessedHits >= 8, $"Too few SearchPattern.Contains calls found ({blessedHits}) - the scan does not bite.");
        Assert.True(offenders.Count == 0,
            "A free-text search has to go through SearchPattern + EF.Functions.Like: EF maps string.Contains "
            + "to SQLite's byte-exact instr(), which ignores the column collation (B-128/B-135).\n"
            + string.Join("\n", offenders));
    }

    /// <summary>Repo root: upward from <see cref="AppContext.BaseDirectory"/> until <c>backend</c>+<c>docs</c> or <c>.git</c>.</summary>
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if ((Directory.Exists(Path.Combine(dir.FullName, "backend")) && Directory.Exists(Path.Combine(dir.FullName, "docs")))
                || Directory.Exists(Path.Combine(dir.FullName, ".git")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Repository root (backend + docs, or .git) not found.");
    }
}
