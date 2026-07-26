using System.ComponentModel.DataAnnotations;

namespace Pugling.Client;

/// <summary>
/// Verbindungs- und Anmeldedaten für die Pugling-API. Wird typischerweise aus der Konfiguration
/// (Abschnitt <c>Pugling</c>) bzw. aus User-Secrets gebunden – die PIN gehört nicht in appsettings.json.
/// </summary>
public sealed class PuglingClientOptions
{
    /// <summary>Konfigurationsabschnitt, aus dem <c>AddPuglingClient</c> standardmäßig bindet.</summary>
    public const string SectionName = "Pugling";

    /// <summary>Basis-URL der API <b>ohne</b> Versionssegment, z. B. <c>http://localhost:5200</c>.</summary>
    [Required]
    public string BaseUrl { get; set; } = "http://localhost:5200";

    /// <summary>
    /// Konto-Id für den konto-zentrischen Login (<c>POST api/v1/auth/login</c>). Ein Konto trägt
    /// mehrere Rollen; der Creator-Agent braucht ein Konto mit Creator-, der Supervisor-Agent eines
    /// mit Supervisor-Rolle.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int AccountId { get; set; }

    /// <summary>PIN des Kontos.</summary>
    [Required]
    public string Pin { get; set; } = "";

    /// <summary>
    /// Sicherheitsabstand, mit dem das Token <b>vor</b> seinem Ablauf erneuert wird. Verhindert, dass
    /// ein Aufruf mit einem Token losläuft, das während der Übertragung abläuft.
    /// </summary>
    public TimeSpan RefreshSkew { get; set; } = TimeSpan.FromMinutes(1);
}
