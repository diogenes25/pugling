using System.Net;

namespace Pugling.Client;

/// <summary>
/// Fehler der Pugling-API, aufgelöst aus der <c>ProblemDetails</c>-Antwort (RFC 7807).
/// Der maschinenlesbare <see cref="Code"/> ist stabiler Vertragsbestandteil (z. B. <c>not_owner</c>,
/// <c>exercise_not_executable</c>) und damit das, worauf ein Agent verzweigen sollte – nicht auf den Text.
/// </summary>
public sealed class PuglingApiException : Exception
{
    /// <summary>Erzeugt einen Fehler aus den Feldern einer ProblemDetails-Antwort.</summary>
    public PuglingApiException(string code, HttpStatusCode statusCode, string? title, string? detail,
        IReadOnlyDictionary<string, string[]>? errors = null, string? requestUri = null)
        : base(BuildMessage(code, statusCode, title, detail, requestUri))
    {
        Code = code;
        StatusCode = statusCode;
        Title = title;
        Detail = detail;
        Errors = errors ?? new Dictionary<string, string[]>();
        RequestUri = requestUri;
    }

    /// <summary>Maschinenlesbarer Fehlercode aus <c>ApiErrors</c> (leerer String, wenn die Antwort keinen trug).</summary>
    public string Code { get; }

    /// <summary>HTTP-Status der Antwort.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>Kurztitel des Fehlers.</summary>
    public string? Title { get; }

    /// <summary>Englischer Fehlertext (i18n – nicht zum Verzweigen verwenden, dafür ist <see cref="Code"/> da).</summary>
    public string? Detail { get; }

    /// <summary>Feldbezogene Validierungsfehler bei <c>validation_error</c>; sonst leer.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>Aufgerufene URI – für Diagnose/Logging.</summary>
    public string? RequestUri { get; }

    private static string BuildMessage(string code, HttpStatusCode status, string? title, string? detail, string? uri)
    {
        var text = detail ?? title ?? status.ToString();
        var where = uri is null ? "" : $" [{uri}]";
        return $"{(int)status} {code}: {text}{where}";
    }
}
