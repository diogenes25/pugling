// Assembly-Fixture, weil die Prüfung erst dann etwas weiß, wenn **alle** Tests durch sind. Ein `[Fact]`
// käme dafür nicht in Frage: xUnit parallelisiert über Collections, eine Reihenfolge „zuletzt" gibt es
// nicht. Das Aufräumen des Assembly-Fixtures läuft dagegen garantiert nach dem letzten Test.
[assembly: AssemblyFixture(typeof(Pugling.Api.Tests.EndpointCoverageGuard))]

namespace Pugling.Api.Tests;

/// <summary>
/// The coverage guard from docs/codequalitaet-gates-plan.md (C4): no controller action may go without
/// a test that invokes it <b>successfully</b>.
/// <para>
/// It makes the work of C3 permanent. Before this, the gap was invisible: 57 of 295 actions were
/// touched by no test at all, while line coverage reported 97.9%. What was affected was almost always
/// <c>Update</c>/<c>Delete</c> – the CRUD tail, and thus exactly the places where PATCH semantics
/// and ownership checks live.
/// </para>
/// <para>
/// Only a call with status &lt; 400 is counted (see <see cref="EndpointCoverage.RecordSuccess"/>) –
/// otherwise the ownership matrix test from C1 would have faked coverage with its 403/404s.
/// </para>
/// </summary>
public sealed class EndpointCoverageGuard : IAsyncDisposable
{
    /// <summary>
    /// Actions successfully touched in a complete run (measured 2026-07-29). Serves as
    /// <b>partial-run detection</b>, not a ratio: the actual check is the list of untouched
    /// actions against <see cref="Exceptions"/>. If coverage increases, the countermeasure below
    /// requires updating this value accordingly.
    /// </summary>
    private const int FullRunTouchedActions = 263;

    /// <summary>
    /// From what percentage of the full inventory the guard judges at all.
    /// <para>
    /// A <c>dotnet test --filter</c> on a single test class naturally touches almost nothing; there, a
    /// report of 290 untouched actions would just be noise. The threshold is deliberately set <b>far</b>
    /// below the full inventory: if coverage in a full run drops by a few tests, the guard still judges
    /// and the newly exposed action stands out. The gates where it matters (CI and the stop hook) always
    /// run the whole solution.
    /// </para>
    /// </summary>
    private const double PartialRunThreshold = 0.6;

    /// <summary>
    /// Deliberately uncovered actions – <b>not</b> a catch-all. Every entry needs a reason;
    /// without a reason, a test should be written instead.
    /// </summary>
    private static readonly HashSet<string> Exceptions = new(StringComparer.Ordinal);

    /// <summary>Checks after the last test; an exception here makes the run go red.</summary>
    public ValueTask DisposeAsync()
    {
        var inventory = EndpointCoverage.Inventory();
        var touched = EndpointCoverage.TouchedCount;

        // Selbstschutz gegen falsch-grün: greift die Reflexion nicht (umbenannter Controller-Basistyp,
        // andere Assembly), ist das Soll leer und der Wächter bestünde inhaltsleer.
        Assert.True(inventory.Count >= 250,
            $"Zu wenige Actions im Soll gefunden ({inventory.Count}) – Reflexion greift nicht.");

        var untouched = EndpointCoverage.Untouched().Where(a => !Exceptions.Contains(a)).ToList();
        Bericht(untouched, touched, inventory.Count);

        if (touched < FullRunTouchedActions * PartialRunThreshold)
            return ValueTask.CompletedTask; // Teil-Lauf (z. B. --filter): kein Urteil.

        Assert.True(untouched.Count == 0,
            $"{untouched.Count} Controller-Action(s) werden von keinem Test erfolgreich aufgerufen "
            + $"(berührt: {touched}/{inventory.Count}). Je Action ein Test: Happy Path plus der eine "
            + "fachlich interessante Fehlerfall – siehe docs/codequalitaet-gates-plan.md (C3/C4):\n"
            + string.Join("\n", untouched));

        // Gegenrichtung wie bei der CancellationToken-Sperre: steigt die Abdeckung, muss die Messzahl
        // mitwachsen, sonst rutscht die Teil-Lauf-Erkennung nach unten und der Wächter wird stumpf.
        Assert.False(touched > FullRunTouchedActions,
            $"Erfreulich: {touched} Actions abgedeckt statt {FullRunTouchedActions}. "
            + $"Bitte {nameof(FullRunTouchedActions)} auf diesen Wert setzen.");
        return ValueTask.CompletedTask;
    }

    /// <summary>Where the report ends up – relative to the repo root, <c>TestResults/</c> is gitignored.</summary>
    public const string BerichtPfad = "TestResults/endpoint-coverage.txt";

    /// <summary>
    /// Writes the report to a file, <b>because the console swallows it.</b>
    /// <para>
    /// An exception from cleaning up an assembly fixture does make the run fail (exit code 1, and the
    /// <c>.trx</c> carries the full message), but the console summary still reports "Passed!" and shows
    /// only <c>Xunit.Sdk.TestPipelineException</c> – without the reason. Even <c>Console.WriteLine</c>
    /// from the fixture does not come through. So that a red gate says <em>what</em> is missing, the
    /// report is kept ready as a file: the stop hook prints it, CI uploads it as an artifact.
    /// </para>
    /// </summary>
    private static void Bericht(IReadOnlyList<string> untouched, int touched, int inventar)
    {
        try
        {
            var pfad = Path.Combine(ApiSurface.RepoRoot(), BerichtPfad);
            Directory.CreateDirectory(Path.GetDirectoryName(pfad)!);
            File.WriteAllLines(pfad,
                [$"# Endpunkt-Abdeckung: {touched}/{inventar} Actions erfolgreich aufgerufen, {untouched.Count} offen",
                 "# Erzeugt von EndpointCoverageGuard (docs/codequalitaet-gates-plan.md, C4).",
                 .. untouched]);
        }
        catch (IOException)
        {
            // Der Bericht ist Diagnose-Komfort; sein Fehlen darf das Urteil nicht verhindern.
        }
    }
}
