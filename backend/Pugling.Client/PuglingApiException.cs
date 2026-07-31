using System.Net;

namespace Pugling.Client;

/// <summary>
/// Error of the Pugling API, resolved from the <c>ProblemDetails</c> response (RFC 7807).
/// The machine-readable <see cref="Code"/> is a stable part of the contract (e.g. <c>not_owner</c>,
/// <c>exercise_not_executable</c>) and thus what an agent should branch on – not the text.
/// </summary>
public sealed class PuglingApiException : Exception
{
    /// <summary>Creates an error from the fields of a ProblemDetails response.</summary>
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

    /// <summary>Machine-readable error code from <c>ApiErrors</c> (empty string if the response carried none).</summary>
    public string Code { get; }

    /// <summary>HTTP status of the response.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>Short title of the error.</summary>
    public string? Title { get; }

    /// <summary>English error text (i18n – do not use for branching, that's what <see cref="Code"/> is for).</summary>
    public string? Detail { get; }

    /// <summary>Field-related validation errors for <c>validation_error</c>; otherwise empty.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>URI that was called – for diagnostics/logging.</summary>
    public string? RequestUri { get; }

    private static string BuildMessage(string code, HttpStatusCode status, string? title, string? detail, string? uri)
    {
        var text = detail ?? title ?? status.ToString();
        var where = uri is null ? "" : $" [{uri}]";
        return $"{(int)status} {code}: {text}{where}";
    }
}
