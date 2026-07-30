using Microsoft.AspNetCore.Diagnostics;

namespace Pugling.Api.Errors;

/// <summary>
/// Fängt den <b>Abbruch durch den Client</b> ab, bevor er als Serverfehler erscheint.
///
/// <para>
/// Seit der <c>CancellationToken</c> in jeden EF-/Service-Aufruf durchgereicht wird, wirft eine
/// abgebrochene Anfrage (Tab geschlossen, Verbindung weg) eine <see cref="OperationCanceledException"/>
/// aus der Action heraus. Ohne diesen Handler protokolliert <c>UseExceptionHandler</c> sie als
/// unbehandelte Exception und antwortet 500 – ein Nutzer, der wegnavigiert, sähe damit wie ein
/// Serverfehler aus (auch im Fehler-Mitschnitt des Anmerkungs-Widgets). Stattdessen: Status 499
/// (nicht-standardisiert, aber verbreitet für „Client Closed Request"), kein Fehler-Log, kein Body –
/// es liest ja niemand mehr mit.
/// </para>
/// </summary>
public sealed class ClientAbortExceptionHandler(ILogger<ClientAbortExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>Nicht-standardisierter Status „Client Closed Request" (nginx-Konvention).</summary>
    public const int ClientClosedRequest = 499;

    /// <summary>
    /// Behandelt die Exception genau dann, wenn sie ein Abbruch <b>des Clients</b> ist. Ein
    /// <see cref="OperationCanceledException"/> aus anderer Quelle (z. B. ein Timeout-Token im Service)
    /// bleibt ein echter Fehler und läuft weiter in den 500er-Pfad – sonst verschluckte dieser Handler
    /// serverseitige Abbrüche, die man sehen will.
    /// </summary>
    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not OperationCanceledException || !httpContext.RequestAborted.IsCancellationRequested)
            return ValueTask.FromResult(false);

        logger.LogDebug("Request {Method} {Path} vom Client abgebrochen.",
            httpContext.Request.Method, httpContext.Request.Path);
        if (!httpContext.Response.HasStarted) httpContext.Response.StatusCode = ClientClosedRequest;
        return ValueTask.FromResult(true);
    }
}
