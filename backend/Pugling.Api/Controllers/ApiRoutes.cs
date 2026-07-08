namespace Pugling.Api.Controllers;

/// <summary>
/// Zentrale Routen-Bausteine. Das Versionssegment steckt nur hier – ein künftiger Versionswechsel
/// (bzw. das Parallelführen einer v2) berührt damit die Controller nicht flächig.
/// </summary>
public static class ApiRoutes
{
    /// <summary>Präfix aller versionierten Routen; <c>{version:apiVersion}</c> wird von Asp.Versioning ersetzt.</summary>
    public const string V1 = "api/v{version:apiVersion}";

    // Die drei fachlichen Ebenen (siehe docs/grundprinzip.md) sind der erste Pfadbaustein nach der Version.
    // Das Präfix ist Ressourcen-Taxonomie, nicht die Auth-Wand: der eigentliche Zugriff bleibt die
    // Method-Level-[Authorize]. Einzelne Routen (z. B. Reports) sind bewusst dual – ein Supervisor liest
    // dann eine Student-getaggte Route und umgekehrt.

    /// <summary>Ebene 1 – Creator: Inhalte/Übungen erstellen (Fach → Kapitel → Übung, Stores, Tags).</summary>
    public const string Creator = V1 + "/creator";

    /// <summary>Ebene 2 – Supervisor: Lehrpläne, Ziele/Punkte, Shop/Angebote, Kind-Verwaltung.</summary>
    public const string Supervisor = V1 + "/supervisor";

    /// <summary>Ebene 3 – Student: Spielen, Verdienen, Kaufen/Aktivieren, eigener Fortschritt.</summary>
    public const string Student = V1 + "/student";
}
