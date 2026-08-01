using Serilog.Context;

namespace Pugling.Api.Auth;

/// <summary>
/// Enriches every request's log context with the identity (<c>Fid</c>/<c>Cid</c>/<c>Role</c>) and the
/// <c>TraceId</c>. That way <em>every</em> log line within a request carries the same TraceId
/// that also goes to the client in the <c>problem+json</c> error – so a reported reference
/// (e.g. from the frontend) can be traced directly to the corresponding server logs.
/// </summary>
public sealed class RequestLogContextMiddleware(RequestDelegate next)
{
    /// <summary>Enriches the request's log context and then calls the next middleware.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // The same traceId that AddProblemDetails writes into the error response.
        var traceId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;
        var fid = context.User.FindFirst("fid")?.Value;
        var cid = context.User.FindFirst("cid")?.Value;
        var role = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("Fid", fid))
        using (LogContext.PushProperty("Cid", cid))
        using (LogContext.PushProperty("Role", role))
        {
            await next(context);
        }
    }
}
