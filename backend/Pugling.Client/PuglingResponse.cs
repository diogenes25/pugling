using System.Net;
using System.Text.Json;

namespace Pugling.Client;

/// <summary>
/// Translates an error response from the API into a <see cref="PuglingApiException"/>. Public so that
/// custom calls not covered by <see cref="CreatorApi"/>/<see cref="SupervisorApi"/> also get
/// the same error semantics.
/// </summary>
public static class PuglingResponse
{
    /// <summary>Throws if the response is not a 2xx; otherwise it returns unchanged.</summary>
    public static async Task<HttpResponseMessage> EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct = default)
    {
        if (response.IsSuccessStatusCode) return response;
        throw await ToExceptionAsync(response, ct);
    }

    /// <summary>
    /// Reads the ProblemDetails body and builds the exception from it. Responses without a (valid) JSON body –
    /// e.g. an empty 403 – get a substitute code derived from the status, so that
    /// <see cref="PuglingApiException.Code"/> is never empty.
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
                // No ProblemDetails (e.g. HTML from a reverse proxy) - keep the raw text as the detail.
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

    // Mirrors the default codes of the server-side CodeStampingProblemDetailsFactory.
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
