using System.Globalization;
using System.Net;
using System.Net.Http.Json;

namespace Pugling.Client;

/// <summary>
/// Dünne Sende-/Lese-Helfer, auf denen <see cref="CreatorApi"/> und <see cref="SupervisorApi"/> aufsetzen:
/// eine Stelle für Serialisierung, Fehlerabbildung und Query-Bau – die Wrapper bleiben dadurch einzeilig.
/// </summary>
internal static class PuglingHttp
{
    internal static async Task<T> GetAsync<T>(this HttpClient http, string uri, CancellationToken ct)
    {
        using var response = await http.GetAsync(uri, ct);
        return await ReadAsync<T>(response, ct);
    }

    internal static async Task<T> PostAsync<T>(this HttpClient http, string uri, object? body, CancellationToken ct)
    {
        using var response = await http.PostAsync(uri, Body(body), ct);
        return await ReadAsync<T>(response, ct);
    }

    /// <summary>
    /// POST mit vorgefertigtem Inhalt statt JSON – für Datei-Uploads (multipart). Der Aufrufer baut den
    /// <see cref="MultipartFormDataContent"/>; Fehlerbehandlung und Deserialisierung bleiben hier, damit
    /// die Upload-Fassade kein eigenes HTTP-Plumbing braucht.
    /// </summary>
    internal static async Task<T> PostContentAsync<T>(this HttpClient http, string uri, HttpContent content,
        CancellationToken ct)
    {
        using var response = await http.PostAsync(uri, content, ct);
        return await ReadAsync<T>(response, ct);
    }

    internal static async Task<T> PatchAsync<T>(this HttpClient http, string uri, object? body, CancellationToken ct)
    {
        using var response = await http.PatchAsync(uri, Body(body), ct);
        return await ReadAsync<T>(response, ct);
    }

    internal static async Task<T> PutAsync<T>(this HttpClient http, string uri, object? body, CancellationToken ct)
    {
        using var response = await http.PutAsync(uri, Body(body), ct);
        return await ReadAsync<T>(response, ct);
    }

    /// <summary>Für Endpunkte ohne Nutzlast in der Antwort (204) – prüft nur den Status.</summary>
    internal static async Task SendAsync(this HttpClient http, HttpMethod method, string uri, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, uri) { Content = Body(body) };
        using var response = await http.SendAsync(request, ct);
        await PuglingResponse.EnsureSuccessAsync(response, ct);
    }

    private static HttpContent? Body(object? body) =>
        body is null ? null : JsonContent.Create(body, body.GetType(), options: PuglingJson.Options);

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        await PuglingResponse.EnsureSuccessAsync(response, ct);

        // 204/leerer Body ist für einen Aufrufer, der ein T erwartet, ein Vertragsbruch – klar melden,
        // statt null zu liefern und den Fehler erst beim Zugriff auftauchen zu lassen.
        if (response.StatusCode == HttpStatusCode.NoContent)
            throw new PuglingApiException("empty_response", response.StatusCode, "Empty response",
                $"Expected a {typeof(T).Name} body but the server returned 204.",
                requestUri: response.RequestMessage?.RequestUri?.ToString());

        return await response.Content.ReadFromJsonAsync<T>(PuglingJson.Options, ct)
               ?? throw new PuglingApiException("empty_response", response.StatusCode, "Empty response",
                   $"Expected a {typeof(T).Name} body but the response was empty.",
                   requestUri: response.RequestMessage?.RequestUri?.ToString());
    }

    /// <summary>
    /// Baut einen Query-String aus optionalen Parametern; <c>null</c> wird weggelassen. Werte werden
    /// invariant formatiert (Enums als Name, <c>bool</c> klein, <see cref="DateOnly"/> als ISO-Datum),
    /// damit sie zur String-Enum-Bindung des Servers passen.
    /// </summary>
    internal static string Query(params (string Name, object? Value)[] parameters)
    {
        var parts = new List<string>();
        foreach (var (name, value) in parameters)
        {
            if (value is null) continue;
            if (value is System.Collections.IEnumerable list and not string)
            {
                foreach (var item in list)
                    if (item is not null)
                        parts.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(Format(item))}");
                continue;
            }
            parts.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(Format(value))}");
        }
        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }

    private static string Format(object value) => value switch
    {
        bool b => b ? "true" : "false",
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };
}
