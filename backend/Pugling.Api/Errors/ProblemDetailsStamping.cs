using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Pugling.Api.Errors;

/// <summary>
/// Shared helpers that stamp the error code + <c>type</c> URI + <c>traceId</c> onto a
/// <see cref="ProblemDetails"/>. A single place for the rules, so the three
/// emit paths (domain-specific <c>ProblemWithCode</c>, the <see cref="CodeStampingProblemDetailsFactory"/>,
/// and the <c>CustomizeProblemDetails</c> hook) don't drift apart.
/// </summary>
public static class ProblemDetailsStamping
{
    /// <summary>Sets the <c>traceId</c> extension like the <c>DefaultProblemDetailsFactory</c> (log correlation).</summary>
    public static void ApplyTraceId(ProblemDetails problem, HttpContext httpContext)
    {
        if ((Activity.Current?.Id ?? httpContext.TraceIdentifier) is { } traceId)
            problem.Extensions["traceId"] = traceId;
    }

    /// <summary>
    /// Stamps a <b>specific</b> error authoritatively (status, title, <c>type</c> URI, <c>code</c>).
    /// </summary>
    public static void StampSpecific(ProblemDetails problem, ApiError error)
    {
        problem.Status = error.Status;
        problem.Title = error.Title;
        problem.Type = error.TypeUri;
        problem.Extensions["code"] = error.Code;
    }

    /// <summary>
    /// Stamps a status-based default – but only if <b>no</b> <c>code</c> is set yet
    /// (specific codes win). Deliberately normalizes the <c>type</c> to the pugling error URI.
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
