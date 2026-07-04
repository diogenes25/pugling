using Serilog.Context;

namespace Pugling.Api.Auth;

/// <summary>
/// Reichert jeden Request-Log-Kontext um die Identität (<c>Fid</c>/<c>Cid</c>/<c>Role</c>) und die
/// <c>TraceId</c> an. Damit trägt <em>jede</em> Log-Zeile innerhalb eines Requests dieselbe TraceId,
/// die auch im <c>problem+json</c>-Fehler an den Client geht – so lässt sich eine gemeldete Referenz
/// (z. B. aus dem Frontend) direkt auf die zugehörigen Server-Logs zurückführen.
/// </summary>
public sealed class RequestLogContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Dieselbe TraceId, die AddProblemDetails in die Fehlerantwort schreibt.
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
