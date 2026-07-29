using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Pugling.Client;

/// <summary>
/// Hängt jedem Aufruf das JWT an und hält es aktuell: Login beim ersten Aufruf (konto-zentrisch über
/// <c>POST api/v1/auth/login</c>), proaktive Erneuerung kurz vor Ablauf und ein einmaliger Retry, falls
/// der Server trotzdem mit <c>401</c> antwortet (z. B. weil der Serverneustart den Signaturschlüssel wechselte).
/// Der Token-Zustand liegt im geteilten <see cref="PuglingTokenStore"/>; der Handler selbst ist
/// zustandslos und gehört – wie jeder <see cref="DelegatingHandler"/> – in genau eine Handler-Kette.
/// </summary>
public sealed class AuthHandler(PuglingTokenStore tokens) : DelegatingHandler
{
    /// <summary>
    /// Weg ohne DI (Tests, kleine Werkzeuge): Handler mit eigenem Token-Speicher. Bewusst eine
    /// Fabrikmethode und kein zweiter Konstruktor – der DI-Container lehnt mehrdeutige Konstruktoren ab.
    /// </summary>
    public static AuthHandler Standalone(PuglingClientOptions options) =>
        new(new PuglingTokenStore(Options.Create(options)));

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // Der Login selbst darf nicht authentifiziert werden – sonst Endlosschleife über sich selbst.
        if (IsLogin(request)) return await base.SendAsync(request, ct);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            await tokens.GetTokenAsync(base.SendAsync, force: false, ct));
        var response = await base.SendAsync(request, ct);
        if (response.StatusCode != HttpStatusCode.Unauthorized) return response;

        // Einmalig neu anmelden und denselben Aufruf wiederholen. Der ursprüngliche Response wird
        // verworfen (und entsorgt), der Request für den zweiten Versuch geklont – ein
        // HttpRequestMessage darf nicht zweimal gesendet werden.
        response.Dispose();
        var fresh = await tokens.GetTokenAsync(base.SendAsync, force: true, ct);
        var retry = await CloneAsync(request, ct);
        retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", fresh);
        return await base.SendAsync(retry, ct);
    }

    /// <summary>
    /// Die Anmelde-Endpunkte – und <b>nur</b> die. Ein <c>Contains("/auth/")</c> träfe auch die
    /// authentifizierten Auth-Routen (<c>auth/me</c>): die gingen dann ohne Token hinaus, und weil der
    /// Kurzschluss auch den 401-Retry überspringt, bekäme der Aufrufer immer <c>unauthorized</c>.
    /// </summary>
    private static readonly string[] LoginPaths = ["/auth/login", "/auth/adult", "/auth/child"];

    private static bool IsLogin(HttpRequestMessage request) =>
        request.RequestUri?.AbsolutePath is { } path
        && LoginPaths.Any(p => path.EndsWith(p, StringComparison.OrdinalIgnoreCase));

    // Nur das, was der Client tatsächlich sendet: Methode, URI, Inhalt und Nicht-Auth-Header.
    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };
        if (request.Content is not null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync(ct);
            var content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            clone.Content = content;
        }
        foreach (var header in request.Headers)
            if (!string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        return clone;
    }
}
