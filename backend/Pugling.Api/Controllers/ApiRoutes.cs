namespace Pugling.Api.Controllers;

/// <summary>
/// Zentrale Routen-Bausteine. Das Versionssegment steckt nur hier – ein künftiger Versionswechsel
/// (bzw. das Parallelführen einer v2) berührt damit die Controller nicht flächig.
/// </summary>
public static class ApiRoutes
{
    /// <summary>Präfix aller versionierten Routen; <c>{version:apiVersion}</c> wird von Asp.Versioning ersetzt.</summary>
    public const string V1 = "api/v{version:apiVersion}";
}
