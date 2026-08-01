using System.Reflection;
using System.Text.RegularExpressions;
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
