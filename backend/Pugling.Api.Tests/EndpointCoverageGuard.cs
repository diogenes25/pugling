// Assembly-Fixture, weil die Prüfung erst dann etwas weiß, wenn **alle** Tests durch sind. Ein `[Fact]`
// käme dafür nicht in Frage: xUnit parallelisiert über Collections, eine Reihenfolge „zuletzt" gibt es
// nicht. Das Aufräumen des Assembly-Fixtures läuft dagegen garantiert nach dem letzten Test.
[assembly: AssemblyFixture(typeof(Pugling.Api.Tests.EndpointCoverageGuard))]

namespace Pugling.Api.Tests;

/// <summary>
/// Der Abdeckungs-Wächter aus docs/codequalitaet-gates-plan.md (C4): keine Controller-Action darf ohne
/// einen Test bleiben, der sie <b>erfolgreich</b> aufruft.
/// <para>
/// Er macht die Arbeit von C3 dauerhaft. Vorher war die Lücke unsichtbar: 57 von 295 Actions wurden von
/// keinem Test berührt, während die Zeilenabdeckung 97,9 % meldete. Betroffen war fast immer
/// <c>Update</c>/<c>Delete</c> – der CRUD-Schwanz, und damit genau die Stellen, an denen PATCH-Semantik
/// und Eigentumsprüfung sitzen.
/// </para>
/// <para>
/// Gezählt wird nur ein Aufruf mit Status &lt; 400 (siehe <see cref="EndpointCoverage.RecordSuccess"/>) –
/// sonst hätte der Ownership-Matrix-Test aus C1 die Abdeckung mit seinen 403/404 vorgetäuscht.
/// </para>
/// </summary>
public sealed class EndpointCoverageGuard : IAsyncDisposable
{
    /// <summary>
    /// Erfolgreich berührte Actions bei einem vollständigen Lauf (gemessen 2026-07-29). Dient der
    /// <b>Teil-Lauf-Erkennung</b>, nicht als Quote: die eigentliche Prüfung ist die Liste der unberührten
    /// Actions gegen <see cref="Exceptions"/>. Steigt die Abdeckung, verlangt die Gegenrichtung unten,
    /// diesen Wert mitzuziehen.
    /// </summary>
    private const int FullRunTouchedActions = 263;

    /// <summary>
    /// Ab wie viel Prozent des Vollbestands der Wächter überhaupt urteilt.
    /// <para>
    /// Ein <c>dotnet test --filter</c> auf eine einzelne Testklasse berührt naturgemäß fast nichts; dort
    /// wäre eine Meldung über 290 unberührte Actions nur Lärm. Der Schwellwert liegt bewusst <b>weit</b>
    /// unter dem Vollbestand: fällt die Abdeckung im vollen Lauf um einige Tests, urteilt der Wächter
    /// weiterhin und die neu entblößte Action fällt auf. Die Tore, an denen es zählt (CI und der
    /// Stop-Hook), fahren immer die ganze Solution.
    /// </para>
    /// </summary>
    private const double PartialRunThreshold = 0.6;

    /// <summary>
    /// Bewusst unabgedeckte Actions – <b>kein</b> Sammelbecken. Jeder Eintrag braucht einen Grund;
    /// ohne Grund gehört stattdessen ein Test geschrieben.
    /// </summary>
    private static readonly HashSet<string> Exceptions = new(StringComparer.Ordinal);

    /// <summary>Prüft nach dem letzten Test; eine Ausnahme hier lässt den Lauf rot werden.</summary>
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

    /// <summary>Wo der Befund landet – relativ zur Repo-Wurzel, <c>TestResults/</c> ist gitignored.</summary>
    public const string BerichtPfad = "TestResults/endpoint-coverage.txt";

    /// <summary>
    /// Schreibt den Befund in eine Datei, <b>weil die Konsole ihn verschluckt.</b>
    /// <para>
    /// Eine Ausnahme aus dem Aufräumen eines Assembly-Fixtures lässt den Lauf zwar scheitern (Exit-Code 1,
    /// und die <c>.trx</c> trägt die vollständige Meldung), aber der Konsolen-Zusammenzug meldet trotzdem
    /// „Passed!" und zeigt nur <c>Xunit.Sdk.TestPipelineException</c> – ohne den Grund. Auch
    /// <c>Console.WriteLine</c> aus dem Fixture kommt nicht durch. Damit ein rotes Tor sagt, <em>was</em>
    /// fehlt, liegt der Befund als Datei bereit: der Stop-Hook gibt sie aus, CI lädt sie als Artefakt hoch.
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
