using Microsoft.AspNetCore.Mvc.Filters;
using Pugling.Api.Errors;

namespace Pugling.Api.Auth;

/// <summary>
/// Action-Filter für alle Endpunkte unterhalb eines Kindes (Route-Parameter <c>childId</c>):
/// stellt zentral sicher, dass der angemeldete Nutzer auf dieses Kind zugreifen darf
/// (Vater = eigenes Kind, Sohn = er selbst). Andernfalls 404 – bewusst kein 403, um das
/// Enumerieren fremder Kind-Ids zu verhindern. Per <c>[ServiceFilter(typeof(ChildOwnershipFilter))]</c>
/// an den kindbezogenen Controllern anzubringen (Gegenstück zu <see cref="PlanOwnershipFilter"/>).
/// </summary>
public class ChildOwnershipFilter(AuthAccess access) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        if (ctx.ActionArguments.TryGetValue("childId", out var v) && v is int childId
            && !await access.OwnsChildAsync(ctx.HttpContext.User, childId))
        {
            ctx.Result = ControllerBaseErrorExtensions.ProblemResult(ctx.HttpContext, ApiErrors.NotFound, "Child not found.");
            return;
        }
        await next();
    }
}
