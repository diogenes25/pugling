namespace Pugling.Agent.Creator.Drafting;

/// <summary>
/// Was bei einem Generierungslauf herauskam – Erfolg wie Misserfolg in derselben Form, damit die
/// Ausgabe (und ein Test) beides gleich behandeln kann.
/// </summary>
/// <param name="TypeKey">Der erzeugte Übungstyp.</param>
/// <param name="Title">Titel des Entwurfs (auch im Trockenlauf gefüllt).</param>
/// <param name="DraftJson">Der Entwurf als JSON – die Ausgabe von <c>--dry-run</c> und das Beweisstück im Fehlerfall.</param>
/// <param name="Violations">Regelverstöße, die auch die Reparatur-Runde nicht beheben konnte (leer = sauber).</param>
/// <param name="ExerciseId">Id der angelegten Übung; <c>null</c> im Trockenlauf oder bei Verstößen.</param>
/// <param name="SelfTestPercent">Ergebnis des nebenwirkungsfreien Selbsttests; erwartet werden 100 %.</param>
/// <param name="RolledBack">Ob die Übung wegen eines misslungenen Selbsttests wieder gelöscht wurde.</param>
public sealed record GenerationOutcome(
    string TypeKey,
    string Title,
    string DraftJson,
    IReadOnlyList<string> Violations,
    int? ExerciseId,
    int? SelfTestPercent,
    bool RolledBack)
{
    /// <summary>Der Entwurf hat alle Regeln bestanden.</summary>
    public bool DraftAccepted => Violations.Count == 0;

    /// <summary>Es steht eine spielbare, selbstgetestete Übung im Katalog.</summary>
    public bool Published => ExerciseId is not null && !RolledBack && SelfTestPercent == 100;
}
