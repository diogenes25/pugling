using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Pugling.Api.Errors;

/// <summary>
/// Geteilte Helfer, die den Fehler-Code + <c>type</c>-URI + <c>traceId</c> auf ein
/// <see cref="ProblemDetails"/> stempeln. Eine einzige Stelle für die Regeln, damit die drei
/// Emit-Pfade (fachliches <c>ProblemWithCode</c>, die <see cref="CodeStampingProblemDetailsFactory"/>
/// und der <c>CustomizeProblemDetails</c>-Hook) nicht auseinanderdriften.
/// </summary>
public static class ProblemDetailsStamping
{
    /// <summary>Setzt die <c>traceId</c>-Extension wie die <c>DefaultProblemDetailsFactory</c> (Log-Korrelation).</summary>
    public static void ApplyTraceId(ProblemDetails problem, HttpContext httpContext)
    {
        if ((Activity.Current?.Id ?? httpContext.TraceIdentifier) is { } traceId)
            problem.Extensions["traceId"] = traceId;
    }

    /// <summary>
    /// Stempelt einen <b>spezifischen</b> Fehler autoritativ (Status, Titel, <c>type</c>-URI, <c>code</c>).
    /// </summary>
    public static void StampSpecific(ProblemDetails problem, ApiError error)
    {
        problem.Status = error.Status;
        problem.Title = error.Title;
        problem.Type = error.TypeUri;
        problem.Extensions["code"] = error.Code;
    }

    /// <summary>
    /// Stempelt einen status-basierten Default – aber nur, wenn noch <b>kein</b> <c>code</c> gesetzt ist
    /// (spezifische Codes gewinnen). Normalisiert den <c>type</c> bewusst auf den pugling-Fehler-URI.
    /// </summary>
    public static void StampFallback(ProblemDetails problem, int status)
    {
        if (problem.Extensions.ContainsKey("code")) return;
        var error = ApiErrors.ForStatus(status);
        problem.Extensions["code"] = error.Code;
        problem.Type = error.TypeUri;
        if (string.IsNullOrEmpty(problem.Title)) problem.Title = error.Title;
    }
}
