namespace Pugling.Api.Controllers;

/// <summary>
/// Central route building blocks. The version segment lives only here – a future version change
/// (or running a v2 in parallel) therefore doesn't touch the controllers across the board.
/// </summary>
public static class ApiRoutes
{
    /// <summary>Prefix of all versioned routes; <c>{version:apiVersion}</c> is replaced by Asp.Versioning.</summary>
    public const string V1 = "api/v{version:apiVersion}";

    // Die drei fachlichen Ebenen (siehe docs/grundprinzip.md) sind der erste Pfadbaustein nach der Version.
    // Das Präfix ist Ressourcen-Taxonomie, nicht die Auth-Wand: der eigentliche Zugriff bleibt die
    // Method-Level-[Authorize]. Einzelne Routen (z. B. Reports) sind bewusst dual – ein Supervisor liest
    // dann eine Student-getaggte Route und umgekehrt.

    /// <summary>Tier 1 – Creator: create content/exercises (subject → chapter → exercise, stores, tags).</summary>
    public const string Creator = V1 + "/creator";

    /// <summary>Tier 2 – Supervisor: study plans, goals/points, shop/offers, child management.</summary>
    public const string Supervisor = V1 + "/supervisor";

    /// <summary>Tier 3 – Student: play, earn, buy/activate, own progress.</summary>
    public const string Student = V1 + "/student";
}
