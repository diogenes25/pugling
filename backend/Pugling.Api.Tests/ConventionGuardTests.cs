using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
