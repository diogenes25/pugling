using System.Net;
using System.Text.Json;

namespace Pugling.Client;

/// <summary>
/// Übersetzt eine Fehlerantwort der API in eine <see cref="PuglingApiException"/>. Öffentlich, damit
/// auch eigene, nicht von <see cref="CreatorApi"/>/<see cref="SupervisorApi"/> abgedeckte Aufrufe
/// dieselbe Fehlersemantik bekommen.
/// </summary>
public static class PuglingResponse
{
    /// <summary>Wirft, wenn die Antwort kein 2xx trägt; sonst kehrt sie unverändert zurück.</summary>
    public static async Task<HttpResponseMessage> EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct = default)
    {
        if (response.IsSuccessStatusCode) return response;
        throw await ToExceptionAsync(response, ct);
    }

    /// <summary>
    /// Liest den ProblemDetails-Body und baut daraus die Ausnahme. Antworten ohne (gültigen) JSON-Body –
    /// etwa ein leeres 403 – bekommen einen aus dem Status abgeleiteten Ersatzcode, damit
    /// <see cref="PuglingApiException.Code"/> nie leer ist.
    /// </summary>
    public static async Task<PuglingApiException> ToExceptionAsync(HttpResponseMessage response, CancellationToken ct = default)
    {
        var uri = response.RequestMessage?.RequestUri?.ToString();
        string? title = null, detail = null, code = null;
        Dictionary<string, string[]>? errors = null;

        var raw = await SafeReadAsync(response, ct);
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    title = Text(root, "title");
                    detail = Text(root, "detail");
                    code = Text(root, "code");
                    errors = ReadErrors(root);
                }
            }
            catch (JsonException)
            {
                // Kein ProblemDetails (z. B. HTML eines Reverse-Proxy) – der Rohtext bleibt als Detail erhalten.
                detail = Truncate(raw);
            }
        }

        return new PuglingApiException(code ?? FallbackCode(response.StatusCode), response.StatusCode,
            title, detail, errors, uri);
    }

    private static async Task<string?> SafeReadAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try { return await response.Content.ReadAsStringAsync(ct); }
        catch (HttpRequestException) { return null; }
    }

    private static string? Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static Dictionary<string, string[]>? ReadErrors(JsonElement root)
    {
        if (!root.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Object) return null;
        var map = new Dictionary<string, string[]>();
        foreach (var field in errors.EnumerateObject())
        {
            if (field.Value.ValueKind != JsonValueKind.Array) continue;
            map[field.Name] = field.Value.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!)
                .ToArray();
        }
        return map.Count > 0 ? map : null;
    }

    // Spiegelt die Default-Codes der serverseitigen CodeStampingProblemDetailsFactory.
    private static string FallbackCode(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => "unauthorized",
        HttpStatusCode.Forbidden => "forbidden",
        HttpStatusCode.NotFound => "not_found",
        HttpStatusCode.Conflict => "conflict",
        HttpStatusCode.TooManyRequests => "too_many_requests",
        HttpStatusCode.BadRequest => "bad_request",
        _ => "http_error",
    };

    private static string Truncate(string text) => text.Length <= 500 ? text : text[..500] + "…";
}
