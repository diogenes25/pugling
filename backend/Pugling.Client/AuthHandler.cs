using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace Pugling.Client;

/// <summary>
/// Attaches the JWT to every call and keeps it current: login on first use (account-centric via
/// <c>POST api/v1/auth/login</c>), proactive renewal shortly before expiry, and a single retry if
/// the server still responds with <c>401</c> (e.g. because a server restart changed the signing key).
/// Token state lives in the shared <see cref="PuglingTokenStore"/>; the handler itself is
/// stateless and belongs — like every <see cref="DelegatingHandler"/> — in exactly one handler chain.
/// </summary>
public sealed class AuthHandler(PuglingTokenStore tokens) : DelegatingHandler
{
    /// <summary>
    /// Path without DI (tests, small tools): a handler with its own token store. Deliberately a
    /// factory method rather than a second constructor — the DI container rejects ambiguous constructors.
    /// </summary>
    public static AuthHandler Standalone(PuglingClientOptions options) =>
        new(new PuglingTokenStore(Options.Create(options)));

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // The login call itself must not be authenticated - that would recurse into itself.
        if (IsLogin(request)) return await base.SendAsync(request, ct);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            await tokens.GetTokenAsync(base.SendAsync, force: false, ct));
        var response = await base.SendAsync(request, ct);
        if (response.StatusCode != HttpStatusCode.Unauthorized) return response;

        // Log in once more and repeat the same call. The original response is discarded (and disposed),
        // the request is cloned for the second attempt - an HttpRequestMessage must not be sent twice.
        response.Dispose();
        var fresh = await tokens.GetTokenAsync(base.SendAsync, force: true, ct);
        var retry = await CloneAsync(request, ct);
        retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", fresh);
        return await base.SendAsync(retry, ct);
    }

    /// <summary>
    /// The login endpoints — and <b>only</b> those. A <c>Contains("/auth/")</c> would also match the
    /// authenticated auth routes (<c>auth/me</c>): those would then go out without a token, and because the
    /// short-circuit also skips the 401 retry, the caller would always get <c>unauthorized</c>.
    /// </summary>
    private static readonly string[] LoginPaths = ["/auth/login", "/auth/adult", "/auth/child"];

    private static bool IsLogin(HttpRequestMessage request) =>
        request.RequestUri?.AbsolutePath is { } path
        && LoginPaths.Any(p => path.EndsWith(p, StringComparison.OrdinalIgnoreCase));

    // Only what the client actually sends: method, URI, content and non-auth headers.
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
