namespace Pugling.Api.Models;

// Lückentext-Lernverfahren: erststrangiger Lückentext-Store (Lerngrundlage, vom Vater gepflegt),
// analog zum Vokabel-Store. Sätze mit Platzhaltern {{1}}, {{2}} … + Lösungen je Lücke.

/// <summary>Stufe des Lückentext-Verfahrens (steigende Schwierigkeit / weniger Hilfe).</summary>
public enum ClozeStage
{
    /// <summary>Auswahl möglicher Wörter (Wortpool), ohne Übersetzung.</summary>
    WordBank = 1,
    /// <summary>Übersetzung + Auswahl möglicher Wörter.</summary>
    TranslationWordBank = 2,
    /// <summary>Übersetzung + Freitexteingabe.</summary>
    TranslationFreeText = 3,
    /// <summary>Nur Freitexteingabe.</summary>
    FreeText = 4,
}

/// <summary>Ein Lückentext als Lerngrundlage (Referenz-Basis für Lehrpläne/Übungen).</summary>
public class ClozeText
{
    public int Id { get; set; }
    /// <summary>Stabiler, eindeutiger Referenz-Key (z. B. "cz_greetings_1").</summary>
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public string SourceLanguage { get; set; } = "";
    public string TargetLanguage { get; set; } = "";
    /// <summary>Text mit Platzhaltern {{1}}, {{2}} … an den Lücken.</summary>
    public string Text { get; set; } = "";
    /// <summary>Optionale Übersetzung des Gesamtsatzes (Hilfe in Stufe 2/3).</summary>
    public string? Translation { get; set; }
    /// <summary>Lösungen je Lücke (JSON-Spalte).</summary>
    public List<Gap> Gaps { get; set; } = new();
    /// <summary>Optionaler Wortpool zur Auswahl (JSON-Spalte).</summary>
    public List<string>? WordBank { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
