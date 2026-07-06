using Microsoft.AspNetCore.Mvc;

namespace Pugling.Api.Errors;

/// <summary>
/// Erzeugt RFC-7807-Fehlerantworten mit maschinenlesbarem <c>code</c> aus der zentralen
/// <see cref="ApiErrors"/>-Registry. Ersetzt das rohe <c>Problem(statusCode:, detail:)</c> in den
/// Controllern – Status, Titel und <c>type</c>-URI kommen aus dem <see cref="ApiError"/>.
/// </summary>
public static class ControllerBaseErrorExtensions
{
    /// <summary>
    /// Baut ein <c>application/problem+json</c> mit <c>Extensions["code"]</c> und kanonischem
    /// <c>type</c>-URI. Der optionale <paramref name="detail"/> ist der frei formulierte Klartext.
    /// </summary>
    public static ObjectResult ProblemWithCode(this ControllerBase controller, ApiError error, string? detail = null) =>
        ProblemResult(controller.HttpContext, error, detail);

    /// <summary>
    /// Wie <see cref="ProblemWithCode"/>, aber ohne <see cref="ControllerBase"/> – für Action-Filter
    /// (z. B. Ownership-Filter), die direkt ein <see cref="ObjectResult"/> setzen.
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
