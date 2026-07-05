using Pugling.Api.Models;

namespace Pugling.Api.Services;

/// <summary>
/// Verfahrensneutrale Stufen-/Antwort-Mechanik, die der Positions-Lern-Motor und der Antwort-Vergleich
/// teilen: welche Stufe „getippt" (objektiv prüfbar) ist und wie eine Antwort für den Vergleich normalisiert
/// wird. Bewusst zustandslose Statics – die einzige Wahrheit dieser kleinen Regeln.
/// </summary>
public static class StageMechanics
{
    /// <summary>
    /// Objektiv (serverseitig gegen die Lösung) prüfbare Vokabel-Stufen – im Gegensatz zu reiner
    /// Selbsteinschätzung. Multiple-Choice zählt dazu: die Auswahl wird gegen die richtige Option geprüft.
    /// </summary>
    public static bool IsTyped(TestStage stage) =>
        stage is TestStage.LetterBoxes or TestStage.FreeText or TestStage.Audio or TestStage.MultipleChoice;

    /// <summary>Freitext-Stufen des Lückentexts (echtes Schreiben statt Auswahl).</summary>
    public static bool IsTyped(ClozeStage stage) =>
        stage is ClozeStage.TranslationFreeText or ClozeStage.FreeText;

    /// <summary>Normalisiert eine Antwort für den Vergleich (trim, klein, Mehrfach-Leerzeichen zusammenfassen).</summary>
    public static string Normalize(string? s) =>
        string.Join(' ', (s ?? "").Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
