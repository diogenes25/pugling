using System.ComponentModel.DataAnnotations;

namespace Pugling.Client;

/// <summary>
/// Connection and login data for the Pugling API. Typically bound from configuration
/// (section <c>Pugling</c>) or from user secrets – the PIN does not belong in appsettings.json.
/// </summary>
public sealed class PuglingClientOptions
{
    /// <summary>Configuration section that <c>AddPuglingClient</c> binds from by default.</summary>
    public const string SectionName = "Pugling";

    /// <summary>Base URL of the API <b>without</b> version segment, e.g. <c>http://localhost:5200</c>.</summary>
    [Required]
    public string BaseUrl { get; set; } = "http://localhost:5200";

    /// <summary>
    /// Account id for the account-centric login (<c>POST api/v1/auth/login</c>). An account carries
    /// multiple roles; the creator agent needs an account with the Creator role, the supervisor agent
    /// one with the Supervisor role.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int AccountId { get; set; }

    /// <summary>PIN of the account.</summary>
    [Required]
    public string Pin { get; set; } = "";

    /// <summary>
    /// Safety margin by which the token is renewed <b>before</b> its expiry. Prevents a call from
    /// starting out with a token that expires during transmission.
    /// </summary>
    public TimeSpan RefreshSkew { get; set; } = TimeSpan.FromMinutes(1);
}
