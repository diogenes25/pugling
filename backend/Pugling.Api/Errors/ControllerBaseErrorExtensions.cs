using Microsoft.AspNetCore.Mvc;

namespace Pugling.Api.Errors;

/// <summary>
/// Creates RFC-7807 error responses with a machine-readable <c>code</c> from the central
/// <see cref="ApiErrors"/> registry. Replaces the raw <c>Problem(statusCode:, detail:)</c> in the
/// controllers – status, title, and <c>type</c> URI come from the <see cref="ApiError"/>.
/// </summary>
public static class ControllerBaseErrorExtensions
{
    /// <summary>
    /// Builds an <c>application/problem+json</c> with <c>Extensions["code"]</c> and a canonical
    /// <c>type</c> URI. The optional <paramref name="detail"/> is the freely worded plain text.
    /// </summary>
    public static ObjectResult ProblemWithCode(this ControllerBase controller, ApiError error, string? detail = null) =>
        ProblemResult(controller.HttpContext, error, detail);

    /// <summary>
    /// Like <see cref="ProblemWithCode"/>, but without a <see cref="ControllerBase"/> – for action filters
    /// (e.g. ownership filters) that set an <see cref="ObjectResult"/> directly.
    /// </summary>
    public static ObjectResult ProblemResult(HttpContext httpContext, ApiError error, string? detail = null)
    {
        // Direkt bauen (kein Umweg über die Factory): der spezifische Code wird autoritativ gestempelt,
        // traceId wie überall gesetzt. Kein Stamp-then-Repair mehr.
        var problem = new ProblemDetails { Detail = detail };
        ProblemDetailsStamping.StampSpecific(problem, error);
        ProblemDetailsStamping.ApplyTraceId(problem, httpContext);
        return new ObjectResult(problem)
        {
            StatusCode = error.Status,
            ContentTypes = { "application/problem+json" },
        };
    }
}
