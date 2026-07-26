namespace Pugling.Api.Models;

/// <summary>
/// Baut den HATEOAS-Selbstlink auf einen Vokabel-Store-Eintrag. Eine Stelle für den Pfad, damit
/// alle Übungstypen denselben <c>_self</c> liefern. Der Pfad ist bis zur Publikation stabil (v1);
/// bewusst als String (kein <c>LinkGenerator</c>), da der Link rein aus der ID ableitbar ist.
/// </summary>
public static class VocabLink
{
    /// <summary>Basis-Pfad des Vokabel-Store-Eintrags.</summary>
    public const string Path = "/api/v1/creator/vocabulary/";

    /// <summary>Selbstlink zur ID; <c>null</c> für fehlende/unbekannte IDs (0 = Alt-Referenz ohne aufgelöste ID).</summary>
    public static string? Self(int? id) => id is null or 0 ? null : Path + id;
}
