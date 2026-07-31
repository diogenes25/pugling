using Microsoft.AspNetCore.Diagnostics;

namespace Pugling.Api.Errors;

/// <summary>
/// Catches an <b>abort by the client</b> before it appears as a server error.
///
/// <para>
/// Since the <c>CancellationToken</c> is passed through into every EF/service call, an
/// aborted request (tab closed, connection lost) throws an <see cref="OperationCanceledException"/>
/// out of the action. Without this handler, <c>UseExceptionHandler</c> logs it as an
/// unhandled exception and responds 500 – a user who navigates away would thus look like a
/// server error (also in the remark widget's error capture). Instead: status 499
/// (non-standard, but widely used for "Client Closed Request"), no error log, no body –
/// nobody is reading along anymore anyway.
/// </para>
/// </summary>
public sealed class ClientAbortExceptionHandler(ILogger<ClientAbortExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>Non-standard status "Client Closed Request" (nginx convention).</summary>
    public const int ClientClosedRequest = 499;

    /// <summary>
    /// Handles the exception precisely when it is an abort <b>by the client</b>. An
    /// <see cref="OperationCanceledException"/> from another source (e.g. a timeout token in a service)
    /// remains a real error and continues into the 500 path – otherwise this handler would swallow
    /// server-side aborts that should be visible.
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
