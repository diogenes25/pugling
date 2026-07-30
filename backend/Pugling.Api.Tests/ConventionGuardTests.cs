using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;

namespace Pugling.Api.Tests;

/// <summary>
/// Macht aus vier Konventionen aus CLAUDE.md ein Tor (docs/codequalitaet-gates-plan.md, B4). Sie wurden
/// bisher lückenlos befolgt – aber von nichts erzwungen: ein generierter Controller mit
/// <c>return BadRequest("…")</c> kompiliert, läuft, liefert ein <c>ProblemDetails</c> ohne <c>code</c>,
/// und kein Test bemerkt es.
/// <para>
/// Jeder Test trägt einen <b>Selbstschutz gegen falsch-grün</b>: greift die Reflexion bzw. der Quell-Scan
/// nicht (umbenannter Namespace, verschobener Ordner, falsches Attribut), findet er nichts und bestünde
/// inhaltsleer. Darum prüft jeder Test zusätzlich, dass er überhaupt genug gesehen hat.
/// </para>
/// </summary>
public class ConventionGuardTests
{
    // Fläche und Routen-Auflösung kommen aus `ApiSurface`, damit dieser Wächter, die Ownership-Matrix (C1)
    // und der Abdeckungs-Wächter (C4) dieselbe Definition von „Action" und dieselbe Route sehen.
    private static IEnumerable<Type> Controllers() => ApiSurface.Controllers();

    private static IEnumerable<MethodInfo> Actions(Type controller) => ApiSurface.Actions(controller);

    // ─────────────────────────────────────────────────────── (a) Fehler nur über ProblemWithCode

    [Fact]
    public void Actions_Melden_Fehler_Nur_Ueber_ProblemWithCode()
    {
        // Quell-Scan statt Reflexion: die Konvention betrifft den Methoden*körper*, den Reflexion nicht sieht.
        var controllerDir = Path.Combine(RepoRoot(), "backend", "Pugling.Api", "Controllers");
        var files = Directory.GetFiles(controllerDir, "*.cs", SearchOption.AllDirectories);

        // `BadRequest(` und ein *rohes* `Problem(`. Der negative Lookbehind trennt `ProblemWithCode(`
        // und `ValidationProblem(` ab: dort folgt auf „Problem" kein „(" bzw. steht ein Wortzeichen davor.
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
                    continue; // Kommentare zitieren die Regel, sie verletzen sie nicht.
                if (blessed.IsMatch(line))
                    blessedHits++;
                if (forbidden.IsMatch(line))
                    offenders.Add($"{Path.GetFileName(file)}:{lineNo}: {line}");
            }
        }

        // Selbstschutz: der Scan muss den Ordner wirklich gelesen haben – es gibt reichlich Controller
        // und dreistellig viele `ProblemWithCode`-Aufrufe.
        Assert.True(files.Length >= 30, $"Zu wenige Controller-Dateien gefunden ({files.Length}) – Pfad falsch?");
        Assert.True(blessedHits >= 100, $"Zu wenige ProblemWithCode-Aufrufe gefunden ({blessedHits}) – Scan greift nicht.");
        Assert.True(offenders.Count == 0,
            "Fehler müssen über this.ProblemWithCode(ApiErrors.…) laufen (RFC 7807 + maschinenlesbarer code):\n"
            + string.Join("\n", offenders));
    }

    // ─────────────────────────────────────────────────────── (c) Vertrag lebt in Pugling.Contracts

    [Fact]
    public void Vertragstypen_Sind_Global_Namens_Eindeutig()
    {
        // Der OpenAPI-Generator schlüsselt Schemas über den *einfachen* Typnamen: zwei gleichnamige
        // Records in verschiedenen Namespaces verschmelzen still zu einem Schema. Der Fehler ist im
        // Vertrag unsichtbar und fällt erst beim generierten Client auf.
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
        // „DTOs als record projizieren – nie EF-Entities zurückgeben" (CLAUDE.md), mechanisch gefasst:
        // was eine Action als Nutzlast liefert, muss aus Pugling.Contracts kommen. Eine zurückgegebene
        // Entity zöge Navigationen und interne Felder mit in die Antwort.
        var contracts = typeof(PointKind).Assembly;
        var apiAssembly = typeof(Program).Assembly;
        var offenders = new List<string>();
        var checkedActions = 0;

        foreach (var controller in Controllers())
            foreach (var action in Actions(controller))
            {
                var payload = PayloadType(action.ReturnType);
                if (payload is null)
                    continue; // IActionResult ohne Typparameter – nichts zu prüfen
                checkedActions++;
                foreach (var leaf in LeafTypes(payload))
                    if (leaf.Assembly == apiAssembly)
                        offenders.Add($"{controller.Name}.{action.Name} → {leaf.Name} (liegt in Pugling.Api, nicht im Vertrag)");
            }

        Assert.True(checkedActions >= 100, $"Zu wenige typisierte Actions gefunden ({checkedActions}) – Reflexion greift nicht.");
        Assert.True(contracts.GetTypes().Length > 0);
        Assert.True(offenders.Count == 0,
            "Antworttypen gehören ins Vertrags-Projekt (nie EF-Entities):\n" + string.Join("\n", offenders));
    }

    // ─────────────────────────────────────────────────────── (d) CancellationToken durchreichen

    [Fact]
    public void Async_Actions_Nehmen_Einen_CancellationToken()
    {
        // „CancellationToken durchreichen" (CLAUDE.md) beginnt an der Action: ohne Parameter gibt es
        // nichts weiterzureichen, und ein abgebrochener Request läuft serverseitig weiter.
        //
        // War bis 2026-07-30 eine Zuwachs-Sperre mit Baseline, weil die Konvention im Bestand *nicht*
        // befolgt war (189 von 337 async Actions ohne Token, gemessen am 2026-07-29) – eine harte Regel
        // wäre damals nur abschaltbar gewesen. Die Altlast ist abgearbeitet, also gilt sie jetzt hart
        // wie die übrigen drei dieser Klasse.
        //
        // Was dieser Wächter *nicht* prüft: dass der Token auch ankommt. Er sieht die **Signatur** der
        // Action, nicht die Kette dahinter. Ein Helfer ohne Token-Parameter verbirgt jeden Aufruf in
        // seinem Rumpf vor CA2016, und in Lambdas schweigt der Analyzer ohnehin – beim Abarbeiten der
        // Altlast war genau das die Fehlerklasse (ein Abbruch-Leak in `MediaAssetsController.Upload`
        // saß hinter einer Action, die den Token längst hatte, und war darum hier nie sichtbar).
        var offenders = new List<string>();
        var checkedActions = 0;

        foreach (var controller in Controllers())
            foreach (var action in Actions(controller))
            {
                // Nur asynchrone Actions: eine synchrone hat keine abbrechbare Arbeit.
                if (!typeof(Task).IsAssignableFrom(action.ReturnType))
                    continue;
                checkedActions++;
                if (!action.GetParameters().Any(p => p.ParameterType == typeof(CancellationToken)))
                    offenders.Add($"{controller.Name}.{action.Name}");
            }

        Assert.True(checkedActions >= 150, $"Zu wenige async Actions gefunden ({checkedActions}) – Reflexion greift nicht.");
        Assert.True(offenders.Count == 0,
            $"{offenders.Count} async Action(s) ohne CancellationToken. Der Token gehört als letzter "
            + "Parameter an die Action (`CancellationToken ct = default` – der Vorgabewert ist nötig, "
            + "weil C# keinen erforderlichen Parameter nach den optionalen `[FromQuery]`-Werten erlaubt) "
            + "und von dort in jeden EF-/Service-Aufruf:\n" + string.Join("\n", offenders));
    }

    // ─────────────────────────────────────────────────────── (b) Eigentum über die geteilten Filter

    [Fact]
    public void Actions_Unter_ChildId_Oder_PlanId_Tragen_Den_Ownership_Filter()
    {
        // „Für Endpunkte unter {planId} den PlanOwnershipFilter, unter {childId} den ChildOwnershipFilter
        // nutzen (nicht inline wiederholen)" – CLAUDE.md. Eine neue Route ohne Filter ist eine IDOR-Lücke.
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
                    offenders.Add($"{controller.Name}.{action.Name} ({route}) ohne ChildOwnershipFilter");
                if (needsPlan && !filters.Contains(typeof(PlanOwnershipFilter)))
                    offenders.Add($"{controller.Name}.{action.Name} ({route}) ohne PlanOwnershipFilter");
            }
        }

        Assert.True(checkedActions >= 50, $"Zu wenige kindes-/plan-gebundene Actions gefunden ({checkedActions}) – Routen-Auflösung greift nicht.");
        Assert.True(offenders.Count == 0,
            "Kindes-/plan-gebundene Actions brauchen den geteilten Ownership-Filter:\n" + string.Join("\n", offenders));
    }

    /// <summary>
    /// Bewusste Ausnahmen von (b) – **kein** Sammelbecken, sondern Entscheidungen mit Grund. Wächst diese
    /// Liste, gehört der Grund dazu, sonst höhlt sie das Tor aus.
    /// </summary>
    private static readonly HashSet<string> OwnershipExceptions = [];

    // ─────────────────────────────────────────────────────── Hilfsmittel

    /// <summary>Die Service-Filter-Typen einer Attribut-Menge.</summary>
    private static IEnumerable<Type> ServiceFilterTypes(IEnumerable<ServiceFilterAttribute> attributes) =>
        attributes.Select(a => a.ServiceType);

    private static string RouteOf(Type controller, MethodInfo action) => ApiSurface.RouteOf(controller, action);

    /// <summary>Die Nutzlast hinter <c>Task&lt;ActionResult&lt;T&gt;&gt;</c> &amp; Co.; <c>null</c>, wenn untypisiert.</summary>
    private static Type? PayloadType(Type returnType)
    {
        var t = returnType;
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>))
            t = t.GetGenericArguments()[0];
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ActionResult<>))
            return t.GetGenericArguments()[0];
        return null;
    }

    /// <summary>Zerlegt Sammlungs-/Nullable-Hüllen bis zu den tatsächlich übertragenen Typen.</summary>
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

    /// <summary>Repo-Wurzel: von <see cref="AppContext.BaseDirectory"/> aufwärts bis <c>backend</c>+<c>docs</c> bzw. <c>.git</c>.</summary>
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
        throw new InvalidOperationException("Repo-Wurzel (backend + docs bzw. .git) nicht gefunden.");
    }
}
