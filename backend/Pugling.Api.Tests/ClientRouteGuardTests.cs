using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Exercises;

namespace Pugling.Api.Tests;

/// <summary>
/// Holds the routes that <c>Pugling.Client</c> writes against the routes the API actually offers
/// (docs/backlog/B-40-client-routen-waechter.md).
/// <para>
/// <b>The gap this closes.</b> Of the ~120 call sites in the three facades, 18 tests drive a part; a typo in
/// the route string of an untested method is invisible today and only breaks at runtime inside the AI agent –
/// as a 404, not as a test failure. And because the API stands at <c>v1</c> and may change <b>freely</b> until
/// publication, every renamed segment in the backend silently breaks every call site that carries it.
/// </para>
/// <para>
/// <b>Client → API only.</b> The opposite direction ("every endpoint has a client method") is deliberately not
/// checked: the client is an <i>excerpt</i> (263 actions against ~120 call sites), so that gate would need an
/// exception list of more than a hundred entries.
/// </para>
/// <para>
/// <b>The document is read live</b> from the test host, not from the artifact checked in by B-42: a guard that
/// compares against a copy can stay green while the real API drifts away – the copy is only as fresh as the
/// last run.
/// </para>
/// </summary>
public class ClientRouteGuardTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>
    /// The path helpers of the client that the resolver follows. Deliberately a <b>closed list</b>: a fifth
    /// helper is not resolved, its paths drop out of the collection, and the lower bound below turns red. That
    /// is the point – the guard should fail <i>loudly</i> rather than silently check less.
    /// </summary>
    private static readonly string[] PathHelpers = ["ExercisePath", "ItemsPath"];

    /// <summary>
    /// The one parameter whose value is not knowable statically: the exercise type segment comes from the
    /// server manifest at runtime ("exercise types come from the server manifest"). A path containing it is
    /// multiplied over all manifest segments instead of being skipped.
    /// </summary>
    private const string ManifestParameter = "authoringRoute";

    /// <summary>Marker for the manifest-driven segment inside a resolved path.</summary>
    private const string TypeMarker = "<type>";

    /// <summary>
    /// Self-protection of the call-site counter itself: if <see cref="CallSitePattern"/> stops matching, the
    /// comparison below would read <c>0 &gt;= 0</c> and pass on an empty scan. Measured 2026-08-01: 138.
    /// </summary>
    private const int MinimumCallSites = 130;

    /// <summary>
    /// Every HTTP call in the facades goes through the <c>Http</c> property, so counting these is a second,
    /// <b>dumb</b> measure of how many routes there have to be – independent of the resolver.
    /// </summary>
    private const string CallSitePattern = @"\bHttp\.(Get|Post|Patch|Put|Send|PostContent)Async";

    // ─────────────────────────────────────────────────────── The gate

    [Fact]
    public async Task Jede_Client_Route_Steht_Im_OpenApi_Dokument()
    {
        var known = await OpenApiPathsAsync();
        var segments = ManifestSegments();
        var (routes, callSites) = CollectClientRoutes();

        // Self-protection against a vacuous green - the failure mode of every scanning guard (see
        // ConventionGuardTests). A fixed lower bound was the obvious choice and is the wrong one: it is only
        // tight on the day it is written, and every client method added afterwards buys one route's worth of
        // blindness. Counting the call sites instead keeps the bound tight **by itself**: one resolved route
        // per HTTP call, forever. Renaming a path helper drops its call sites out of the resolution and lands
        // here, no matter how large the client has grown by then.
        Assert.True(callSites >= MinimumCallSites,
            $"Only {callSites} HTTP call sites found in Pugling.Client (expected at least {MinimumCallSites}) - "
            + "the call-site count does not bite, so it cannot secure the comparison below.");
        Assert.True(routes.Count >= callSites,
            $"{callSites} HTTP call sites, but only {routes.Count} routes resolved - {callSites - routes.Count} "
            + $"call site(s) are silently unchecked. Known path helpers: {string.Join(", ", PathHelpers)}. "
            + "Was one of them renamed, or is there a new one?");

        var offenders = new List<string>();
        foreach (var route in routes)
            foreach (var path in Expand(route.Path, segments))
                if (!known.Contains(path))
                    offenders.Add($"{route.File}:{route.Line} {route.Member} → {path}");

        Assert.True(offenders.Count == 0,
            $"{offenders.Count} client route(s) do not exist in /openapi/v1.json. Either the client writes a "
            + "wrong segment, or the API was renamed and the client was not pulled along. A `{}` where the "
            + "client has a real segment means the resolver could not evaluate that hole:\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public async Task Jedes_Typ_Segment_Des_Manifests_Ergibt_Einen_Gueltigen_Pfad()
    {
        // The other half of the manifest decision: the four exercise methods take their segment from the
        // manifest, so a *new* exercise type must be covered without touching this guard. That only holds if
        // every segment the manifest offers really is an authoring route.
        var known = await OpenApiPathsAsync();
        var segments = ManifestSegments();

        var offenders = segments
            .Select(s => $"/api/v1/creator/textbook-series/{{}}/units/{{}}/{s}")
            .Where(p => !known.Contains(p))
            .ToList();

        Assert.True(offenders.Count == 0,
            "Manifest segment(s) without an authoring endpoint - the client would post them into the void:\n"
            + string.Join("\n", offenders));
    }

    // ─────────────────────────────────────────────────────── The two sources

    /// <summary>The paths of the live document, normalized (every <c>{…}</c> placeholder collapsed to <c>{}</c>).</summary>
    private async Task<HashSet<string>> OpenApiPathsAsync()
    {
        var doc = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var paths = doc.GetProperty("paths").EnumerateObject().Select(p => Normalize(p.Name)).ToHashSet(StringComparer.Ordinal);

        // Self-protection: a document without paths would make every comparison below pass.
        Assert.True(paths.Count >= 100, $"Too few paths in the OpenAPI document ({paths.Count}) - wrong document?");
        return paths;
    }

    /// <summary>The authoring route segments of the live type manifest.</summary>
    private List<string> ManifestSegments()
    {
        // Straight from the registry rather than over HTTP: the manifest endpoint is covered elsewhere, and a
        // call here would move EndpointCoverageGuard.FullRunTouchedActions - a constant that belongs to B-41.
        var segments = factory.Services.GetRequiredService<ExerciseTypeRegistry>()
            .Manifests.Select(m => m.AuthoringRoute).ToList();

        Assert.True(segments.Count >= 5, $"Too few exercise types found ({segments.Count}) - the registry is empty?");
        return segments;
    }

    // ─────────────────────────────────────────────────────── The source scan

    /// <summary>One route as the client writes it, with the location needed to fix it.</summary>
    private sealed record ClientRoute(string File, int Line, string Member, string Path);

    /// <summary>A resolvable path helper: its parameter names and the interpolated string it returns.</summary>
    private sealed record PathHelper(IReadOnlyList<string> Parameters, string Body);

    private static (List<ClientRoute> Routes, int CallSites) CollectClientRoutes()
    {
        var dir = Path.Combine(ApiSurface.RepoRoot(), "backend", "Pugling.Client");
        // Hand-written sources only: `obj/` carries generated files (AssemblyInfo, GlobalUsings), which would
        // make the count below depend on the build state instead of on the client.
        var files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToList();
        Assert.True(files.Count >= 11, $"Too few client source files found ({files.Count}) - wrong path?");

        var routes = new List<ClientRoute>();
        var unreadable = new List<string>();
        var callSites = 0;

        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            var (root, rootLine) = FindRoot(lines);
            var (helpers, helperLines) = FindHelpers(lines);
            var name = Path.GetFileName(file);
            var member = "(file scope)";

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("//", StringComparison.Ordinal) || trimmed.StartsWith('*'))
                    continue; // XML docs quote routes with an ellipsis; they are not call sites.
                if (MethodName(line) is { } declared) member = declared;
                callSites += Regex.Matches(line, CallSitePattern).Count;
                // The helper bodies are templates the resolver substitutes into - not call sites of their own.
                // Their unbound type segment would otherwise look like a path with a parameter where the API
                // has a literal.
                if (i == rootLine || helperLines.Contains(i))
                    continue;

                if (UnreadableShape(line) is { } shape) unreadable.Add($"{name}:{i + 1}: {shape}");

                var found = new List<string>();
                var masked = new StringBuilder(line);

                foreach (var s in InterpolatedStrings(line))
                {
                    if (!s.Terminated)
                    {
                        unreadable.Add($"{name}:{i + 1}: interpolated string not closed on this line");
                        continue;
                    }
                    // Blank out what has been read, so the bare-helper scan below cannot see the same path twice.
                    for (var k = s.Start; k <= s.End && k < masked.Length; k++) masked[k] = ' ';
                    found.Add(Resolve(s.Raw, root, helpers, Unbound));
                }

                // A path helper handed over **bare** - `Http.PostAsync(ExercisePath(a, b, route), …)` in
                // CreateExerciseAsync is the only one today. Without this it is the single call site of the
                // four exercise methods the guard would miss, and it would miss it silently.
                foreach (Match m in Regex.Matches(masked.ToString(), $@"\b({string.Join("|", PathHelpers)})\s*\("))
                    if (BalancedCall(masked.ToString(), m.Index) is { } call)
                        found.Add(ResolveHole(call, root, helpers, Unbound, depth: 0));

                foreach (Match m in Regex.Matches(masked.ToString(), "\"(api/v[0-9]+/[^\"]*)\""))
                    found.Add(m.Groups[1].Value);

                // `api/v` and not `api/v1/`: CLAUDE.md plans the break after publication as a parallel `v2`.
                // Anchored on `v1` alone, the first v2 client method would drop out of the scan without a sound.
                foreach (var path in found.Where(p => p.StartsWith("api/v", StringComparison.Ordinal)))
                    routes.Add(new ClientRoute(name, i + 1, MemberOnLine(line) ?? member, Normalize(path)));
            }
        }

        // A shape the scanner cannot read must be **loud**. It is legal C# and would otherwise resolve to
        // nothing or to a fragment - and a fragment is worse than nothing here: `$"{Root}/children/{childId}"
        // + "/points"` yields the prefix, the prefix exists in the document, and the composed path is never
        // checked. The count stays right in every one of these cases, so no lower bound would notice.
        Assert.True(unreadable.Count == 0,
            "The route scanner cannot read these lines - it has to learn the shape rather than quietly check "
            + "less:\n" + string.Join("\n", unreadable));
        return (routes, callSites);
    }

    /// <summary>
    /// Names the shape of a line the scanner cannot read, or <c>null</c> if it can. Deliberately conservative:
    /// what is not listed here is read, and what is listed turns the gate red instead of vanishing.
    /// </summary>
    private static string? UnreadableShape(string line)
    {
        // A path glued together from two literals: the scanner would see only the first part.
        if (Regex.IsMatch(line, @"""\s*\+\s*[$@]{0,2}"""))
            return "path composed from two string literals with `+`";
        // Verbatim and raw string literals: `InterpolatedStrings` expects `$` directly followed by `"`.
        if (Regex.IsMatch(line, @"\$@""|@\$""|\$""""""") && Regex.IsMatch(line, @"api/v|\{Root\}"))
            return "verbatim or raw string literal carrying a route";
        return null;
    }

    /// <summary>The <c>Root</c> constant of a facade and the line it sits on.</summary>
    private static (string? Root, int Line) FindRoot(string[] lines)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var m = Regex.Match(lines[i], @"const\s+string\s+Root\s*=\s*""([^""]+)""");
            if (m.Success) return (m.Groups[1].Value, i);
        }
        return (null, -1);
    }

    /// <summary>The path helpers from <see cref="PathHelpers"/> plus the line indices their bodies occupy.</summary>
    private static (Dictionary<string, PathHelper> Helpers, HashSet<int> Lines) FindHelpers(string[] lines)
    {
        var helpers = new Dictionary<string, PathHelper>(StringComparer.Ordinal);
        var used = new HashSet<int>();

        for (var i = 0; i < lines.Length; i++)
        {
            var m = Regex.Match(lines[i], @"private\s+static\s+string\s+(\w+)\s*\(([^)]*)\)\s*=>\s*(.*)$");
            if (!m.Success || !PathHelpers.Contains(m.Groups[1].Value, StringComparer.Ordinal))
                continue;

            // Expression body on the same line or - as in the client today - on the next one.
            var bodyLine = m.Groups[3].Value.Trim().Length > 0 ? i : i + 1;
            if (bodyLine >= lines.Length) continue;
            if (InterpolatedStrings(lines[bodyLine]).FirstOrDefault(s => s.Terminated) is not { } body) continue;

            var parameters = m.Groups[2].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => p.Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1]).ToList();
            helpers[m.Groups[1].Value] = new PathHelper(parameters, body.Raw);
            // Both lines of the declaration: the signature carries the helper name and would otherwise be read
            // as a bare call - with its own parameter list as the arguments.
            used.Add(i);
            used.Add(bodyLine);
        }
        return (helpers, used);
    }

    // ─────────────────────────────────────────────────────── Resolving an interpolated string

    /// <summary>
    /// Turns an interpolated string into a route: known holes become their value, everything else becomes the
    /// neutral placeholder <c>{}</c>.
    /// <para>
    /// <b>Why parameter names are not compared.</b> The client writes <c>{childId}</c> where the route may say
    /// <c>{id}</c>. Normalizing both sides makes the comparison deliberately blind to renamed <i>parameters</i>
    /// and sharp on wrong <i>segments</i> – and only the latter is the defect.
    /// </para>
    /// </summary>
    private static string Resolve(string raw, string? root, IReadOnlyDictionary<string, PathHelper> helpers,
        IReadOnlyDictionary<string, string> bindings, int depth = 0)
    {
        var sb = new StringBuilder();
        foreach (var (isHole, text) in Parts(raw))
            sb.Append(isHole ? ResolveHole(text.Trim(), root, helpers, bindings, depth) : text);
        return sb.ToString();
    }

    /// <summary>
    /// Resolves one hole of an interpolated string.
    /// <para>
    /// <b>Why the depth counter.</b> Bindings can form a cycle – swapped arguments at a path helper
    /// (<c>ItemsPath(seriesUnitId, seriesId, …)</c>) map <c>a → b</c> and <c>b → a</c>, and that is exactly the
    /// defect class this guard exists for. A <see cref="StackOverflowException"/> cannot be caught: it kills
    /// the test host, and all 600-plus tests fall without a cause. Degrading to a placeholder instead lets the
    /// comparison report the wrong path.
    /// </para>
    /// </summary>
    private static string ResolveHole(string expr, string? root, IReadOnlyDictionary<string, PathHelper> helpers,
        IReadOnlyDictionary<string, string> bindings, int depth)
    {
        if (depth > 16) return "{}";

        // A helper parameter: continue with the argument the call site passed in.
        if (bindings.TryGetValue(expr, out var bound) && !string.Equals(bound, expr, StringComparison.Ordinal))
            return ResolveHole(bound.Trim(), root, helpers, bindings, depth + 1);

        if (expr == "Root" && root is not null) return root;
        if (expr.Length > 1 && expr[0] == '"' && expr[^1] == '"') return expr[1..^1];
        if (expr == ManifestParameter) return TypeMarker;

        var call = Regex.Match(expr, @"^(\w+)\((.*)\)$", RegexOptions.Singleline);
        if (call.Success && helpers.TryGetValue(call.Groups[1].Value, out var helper))
        {
            var args = SplitArguments(call.Groups[2].Value);
            var inner = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var k = 0; k < helper.Parameters.Count && k < args.Count; k++)
                // Resolve the argument through the *current* bindings first, so a helper calling a helper
                // carries the outer call site's values through instead of its own parameter names.
                inner[helper.Parameters[k]] = bindings.TryGetValue(args[k], out var v) ? v : args[k];
            return Resolve(helper.Body, root, helpers, inner, depth + 1);
        }

        return "{}";
    }

    /// <summary>Splits an interpolated string into literal text and hole expressions.</summary>
    private static IEnumerable<(bool IsHole, string Text)> Parts(string raw)
    {
        var literal = new StringBuilder();
        for (var i = 0; i < raw.Length; i++)
        {
            if (raw[i] != '{')
            {
                literal.Append(raw[i]);
                continue;
            }
            if (literal.Length > 0)
            {
                yield return (false, literal.ToString());
                literal.Clear();
            }

            var depth = 1;
            var hole = new StringBuilder();
            var j = i + 1;
            while (j < raw.Length)
            {
                if (raw[j] == '{') depth++;
                else if (raw[j] == '}' && --depth == 0) break;
                hole.Append(raw[j]);
                j++;
            }
            yield return (true, hole.ToString());
            i = j;
        }
        if (literal.Length > 0) yield return (false, literal.ToString());
    }

    /// <summary>Splits an argument list at the commas that are not nested inside brackets or a string.</summary>
    private static List<string> SplitArguments(string args)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var depth = 0;
        var inString = false;

        foreach (var c in args)
        {
            if (c == '"') inString = !inString;
            if (!inString)
            {
                if (c is '(' or '[' or '<') depth++;
                else if (c is ')' or ']' or '>') depth--;
                else if (c == ',' && depth == 0)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                    continue;
                }
            }
            current.Append(c);
        }
        if (current.Length > 0) result.Add(current.ToString().Trim());
        return result;
    }

    /// <summary>
    /// The interpolated strings of a line, with their content unescaped of nothing – the brace depth is tracked
    /// so a <b>nested string literal inside a hole</b> does not end the scan. That is not academic: with
    /// <c>ExercisePath(seriesId, seriesUnitId, "birkenbihl")</c> the client has exactly this shape, and it is what
    /// a line-wise regex would trip over.
    /// </summary>
    private static IEnumerable<Interpolated> InterpolatedStrings(string line)
    {
        for (var i = 0; i + 1 < line.Length; i++)
        {
            if (line[i] != '$' || line[i + 1] != '"') continue;

            var content = new StringBuilder();
            var depth = 0;
            var terminated = false;
            var j = i + 2;
            while (j < line.Length)
            {
                var c = line[j];
                if (c == '{') depth++;
                else if (c == '}') depth--;
                else if (c == '"' && depth == 0)
                {
                    terminated = true;
                    break;
                }
                content.Append(c);
                j++;
            }
            yield return new Interpolated(content.ToString(), terminated, i, j);
            i = j;
        }
    }

    /// <summary>One interpolated string of a line, with the span it occupies so the line can be masked.</summary>
    private sealed record Interpolated(string Raw, bool Terminated, int Start, int End);

    /// <summary>
    /// The full text of a call starting at <paramref name="start"/>, up to its balanced closing parenthesis;
    /// <c>null</c> if it does not close on this line.
    /// </summary>
    private static string? BalancedCall(string line, int start)
    {
        var depth = 0;
        for (var i = start; i < line.Length; i++)
        {
            if (line[i] == '(') depth++;
            else if (line[i] == ')' && --depth == 0) return line[start..(i + 1)];
        }
        return null;
    }

    /// <summary>A call site binds nothing – only a helper body does.</summary>
    private static readonly Dictionary<string, string> Unbound = new(StringComparer.Ordinal);

    // ─────────────────────────────────────────────────────── Small helpers

    /// <summary>Both sides of the comparison in one shape: a leading slash, every placeholder as <c>{}</c>.</summary>
    private static string Normalize(string path)
    {
        var normalized = Regex.Replace(path, @"\{[^{}]*\}", "{}");
        return normalized.StartsWith('/') ? normalized : "/" + normalized;
    }

    /// <summary>Multiplies a manifest-driven path over all type segments; a static path stays as it is.</summary>
    private static IEnumerable<string> Expand(string path, IEnumerable<string> segments) =>
        path.Contains(TypeMarker, StringComparison.Ordinal)
            ? segments.Select(s => path.Replace(TypeMarker, s, StringComparison.Ordinal))
            : [path];

    /// <summary>The name of the method declared on this line, if any – so an offender can be located.</summary>
    private static string? MethodName(string line)
    {
        var m = Regex.Match(line, @"^\s*public\s+.*?\b(\w+)\s*(?:<[^<>]*>)?\s*\(");
        return m.Success ? m.Groups[1].Value : null;
    }

    /// <summary>The member assigned on this line – for constants such as <c>LoginPath</c>, which are no method.</summary>
    private static string? MemberOnLine(string line)
    {
        var m = Regex.Match(line, @"const\s+string\s+(\w+)\s*=");
        return m.Success ? m.Groups[1].Value : null;
    }

}
