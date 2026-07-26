using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Pugling.Client;

/// <summary>
/// Hält das JWT <b>eine</b> Anmeldung lang – geteilt über alle Fassaden (Creator/Supervisor/Student).
/// Bewusst getrennt vom <see cref="AuthHandler"/>: eine <c>DelegatingHandler</c>-Instanz darf nur in
/// genau einer Handler-Kette hängen, der Token-Zustand soll aber gerade <i>nicht</i> je Client
/// verdoppelt werden – sonst meldete sich jede Fassade eigenständig an.
/// </summary>
public sealed class PuglingTokenStore(IOptions<PuglingClientOptions> options)
{
    private const string LoginPath = "api/v1/auth/login";

    /// <summary>Sendeweg für den Login – der Handler reicht seine eigene Kette herein.</summary>
    public delegate Task<HttpResponseMessage> Send(HttpRequestMessage request, CancellationToken ct);

    private readonly PuglingClientOptions _options = options.Value;
    private readonly SemaphoreSlim _gate = new(1, 1);

    // Absolut, nicht relativ: Die Auflösung gegen HttpClient.BaseAddress passiert *vor* der
    // Handler-Kette – ein hier erzeugter relativer Login-Request käme nie beim Server an.
    private readonly Uri _loginUri = new(new Uri(options.Value.BaseUrl.TrimEnd('/') + "/"), LoginPath);

    private string? _token;
    private DateTime _expiresAtUtc;

    /// <summary>
    /// Liefert ein gültiges Token; meldet sich an, wenn keines da ist, es demnächst abläuft oder der
    /// Aufrufer die Erneuerung erzwingt (nach einem <c>401</c>).
    /// </summary>
    public async Task<string> GetTokenAsync(Send send, bool force, CancellationToken ct)
    {
        if (!force && IsFresh()) return _token!;

        await _gate.WaitAsync(ct);
        try
        {
            // Zweite Prüfung im Lock: Bei parallelen Aufrufen hat u. U. schon jemand angemeldet.
            if (!force && IsFresh()) return _token!;

            var login = new HttpRequestMessage(HttpMethod.Post, _loginUri)
            {
                Content = JsonContent.Create(new AccountLoginDto(_options.AccountId, _options.Pin),
                    options: PuglingJson.Options),
            };
            using var response = await send(login, ct);
            if (!response.IsSuccessStatusCode)
                throw await PuglingResponse.ToExceptionAsync(response, ct);

            var body = await response.Content.ReadFromJsonAsync<LoginResponse>(PuglingJson.Options, ct)
                       ?? throw new PuglingApiException("invalid_login_response", response.StatusCode,
                           "Login failed", "The login endpoint returned an empty body.");

            _token = body.Token;
            _expiresAtUtc = body.ExpiresAt;
            return _token;
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool IsFresh() => _token is not null && DateTime.UtcNow < _expiresAtUtc - _options.RefreshSkew;
}
