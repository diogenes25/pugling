using System.ComponentModel.DataAnnotations;

namespace Pugling.Agent.Creator;

/// <summary>
/// Einstellungen des lokalen Sprachmodells (Abschnitt <c>Agent</c>). Bewusst getrennt von
/// <c>PuglingClientOptions</c>: das eine beschreibt, <b>womit</b> generiert wird, das andere,
/// <b>wohin</b> das Ergebnis geschrieben wird.
/// </summary>
public sealed class AgentOptions
{
    /// <summary>Konfigurationsabschnitt, aus dem gebunden wird.</summary>
    public const string SectionName = "Agent";

    /// <summary>Basis-URL des Ollama-Servers.</summary>
    [Required]
    public string Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Modellname wie in <c>ollama list</c>. Es muss ein <b>Instruct</b>-Modell sein, das verlässlich JSON
    /// nach Schema liefert (z. B. <c>qwen2.5:14b-instruct</c> oder <c>llama3.1:8b</c>); Roleplay-Finetunes
    /// oder sehr kleine Modelle scheitern an der strukturierten Ausgabe.
    /// </summary>
    [Required]
    public string Model { get; set; } = "qwen2.5:14b-instruct";

    /// <summary>Kreativität. Niedrig halten – wir wollen korrekte Aufgaben, keine Überraschungen.</summary>
    [Range(0d, 2d)]
    public double Temperature { get; set; } = 0.4;

    /// <summary>Zeitlimit eines einzelnen Modellaufrufs; lokale Modelle brauchen auf CPU spürbar lange.</summary>
    [Range(10, 3600)]
    public int TimeoutSeconds { get; set; } = 180;

    /// <summary>
    /// Wie oft ein Entwurf, der die deterministischen Regeln verletzt, mit den konkreten Verstößen
    /// zurück ans Modell geht. 0 = keine Reparatur (Abbruch beim ersten Verstoß).
    /// </summary>
    [Range(0, 3)]
    public int RepairAttempts { get; set; } = 1;
}
