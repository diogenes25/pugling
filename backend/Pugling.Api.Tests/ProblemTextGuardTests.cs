using System.Reflection;
using System.Text.RegularExpressions;
using Pugling.Api.Errors;

namespace Pugling.Api.Tests;

/// <summary>
/// Holds every <c>409</c> code in <see cref="ApiErrors"/> against the German texts of the supervisor web
/// (<c>GERMAN_PROBLEM_TEXT</c> in <c>frontend/src/lib/api.ts</c>, B-411).
/// <para>
/// <b>The gap this closes.</b> A lock (409) is the one error a user triggers at a button of their own and is meant to
/// understand. Without a German entry the web shows the raw English <c>detail</c> with a trace id - B-403 shipped that way,
/// because nothing connected a new code in <see cref="ApiErrors"/> with the table in the frontend.
/// </para>
/// <para>
/// <b>Every 409 code, not only the adult tiers.</b> Which tier returns a code is only readable by grepping sources, and a
/// shared service serves several tiers. A code the supervisor web never shows gets a line in <see cref="Exceptions"/> with
/// its reason instead - a deliberate line, which is the point of the gate.
/// </para>
/// </summary>
public class ProblemTextGuardTests
{
    /// <summary>
    /// 409 codes that deliberately have no entry in <c>GERMAN_PROBLEM_TEXT</c>, each with the reason. Codes only the arcade
    /// shows have an entry too (B-415): the arcade's German <c>problem.*</c> sentence must equal it word for word.
    /// </summary>
    private static readonly Dictionary<string, string> Exceptions = new()
    {
        ["conflict"] = "Generic default; no action returns it with this code.",
        ["tower_game_time_limit"] = "Only the native tower game (game/) starts runs; it has its own German text in PuglingApi.cs.",
        ["tower_game_run_closed"] = "Only the native tower game (game/) plays runs; it has its own German text in PuglingApi.cs.",
    };

    [Fact]
    public void Every_409_Code_Has_A_German_Text_Or_A_Reasoned_Exception()
    {
        var (codes, german) = Sources();

        var missing = codes.Where(c => !german.Contains(c) && !Exceptions.ContainsKey(c)).Order().ToList();
        Assert.True(missing.Count == 0,
            $"{missing.Count} 409 code(s) have neither a German text in GERMAN_PROBLEM_TEXT (frontend/src/lib/api.ts) nor an entry "
            + $"in ProblemTextGuardTests.Exceptions: {string.Join(", ", missing)}. A lock a user triggers must say in German what "
            + "locks (rule 2026-11-07: no way out beyond its own lock); a code the supervisor web never shows needs an exception with its reason.");
    }

    [Fact]
    public void Every_Exception_Is_Still_Needed()
    {
        var (codes, german) = Sources();

        var stale = Exceptions.Keys.Where(c => !codes.Contains(c) || german.Contains(c)).Order().ToList();
        Assert.True(stale.Count == 0,
            $"{stale.Count} exception(s) in ProblemTextGuardTests.Exceptions are no longer needed - the code is gone from ApiErrors, "
            + $"is no longer a 409, or now has a German text: {string.Join(", ", stale)}. Remove the line, so the list keeps meaning something.");
    }

    /// <summary>
    /// Both sources, with the self-protection against a false green: if the reflection or the table parse stops engaging,
    /// a set shrinks to nothing and every check of either fact passes vacuously.
    /// </summary>
    private static (HashSet<string> Codes, HashSet<string> German) Sources()
    {
        var codes = ConflictCodes();
        var german = GermanTableKeys();
        Assert.True(codes.Count >= 30, $"Only {codes.Count} 409 codes found in ApiErrors - did the reflection stop engaging?");
        Assert.True(german.Count >= 9, $"Only {german.Count} entries parsed from GERMAN_PROBLEM_TEXT - did the table move or change shape?");
        return (codes, german);
    }

    private static HashSet<string> ConflictCodes() =>
        typeof(ApiErrors).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(ApiError))
            .Select(f => (ApiError)f.GetValue(null)!)
            .Where(e => e.Status == 409)
            .Select(e => e.Code)
            .ToHashSet();

    /// <summary>
    /// The keys of <c>GERMAN_PROBLEM_TEXT</c>: the lines between the declaration and its closing brace that start with a
    /// two-space indented identifier and a colon. Comment and continuation lines start with <c>//</c>, <c>"</c> or <c>+</c>.
    /// </summary>
    private static HashSet<string> GermanTableKeys()
    {
        var source = File.ReadAllText(Path.Combine(ApiSurface.RepoRoot(), "frontend", "src", "lib", "api.ts"));
        var start = source.IndexOf("const GERMAN_PROBLEM_TEXT", StringComparison.Ordinal);
        Assert.True(start >= 0, "GERMAN_PROBLEM_TEXT not found in frontend/src/lib/api.ts - the gate cannot read the table.");
        var end = Regex.Match(source[start..], @"\r?\n};").Index;
        return Regex.Matches(source.Substring(start, end), @"^  ([a-z_]+):", RegexOptions.Multiline)
            .Select(m => m.Groups[1].Value)
            .ToHashSet();
    }
}
