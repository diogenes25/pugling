using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Pugling.Client;

/// <summary>
/// Holds the JWT for <b>one</b> login session – shared across all facades (Creator/Supervisor/Student).
/// Deliberately separate from <see cref="AuthHandler"/>: a <c>DelegatingHandler</c> instance may hang in
/// exactly one handler chain, but the token state must specifically <i>not</i> be duplicated per client –
/// otherwise every facade would log in independently.
/// </summary>
public sealed class PuglingTokenStore(IOptions<PuglingClientOptions> options) : IDisposable
{
    private const string LoginPath = "api/v1/auth/login";

    /// <summary>Send path for the login – the handler passes in its own chain.</summary>
    public delegate Task<HttpResponseMessage> Send(HttpRequestMessage request, CancellationToken ct);

    private readonly PuglingClientOptions _options = options.Value;
    private readonly SemaphoreSlim _gate = new(1, 1);

    // Absolut, nicht relativ: Die Auflösung gegen HttpClient.BaseAddress passiert *vor* der
    // Handler-Kette – ein hier erzeugter relativer Login-Request käme nie beim Server an.
    private readonly Uri _loginUri = new(new Uri(options.Value.BaseUrl.TrimEnd('/') + "/"), LoginPath);

    private string? _token;
    private DateTime _expiresAtUtc;

    /// <summary>
    /// Returns a valid token; logs in if none is present, it is about to expire, or the
    /// caller forces renewal (after a <c>401</c>).
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

    /// <summary>Releases the login gate. As a DI singleton, the container handles this on shutdown.</summary>
    public void Dispose() => _gate.Dispose();
}
