using Microsoft.AspNetCore.Mvc.Filters;
using Pugling.Api.Errors;

namespace Pugling.Api.Auth;

/// <summary>
/// Action filter for all endpoints below a child (route parameter <c>childId</c>):
/// centrally ensures that the logged-in user may access this child
/// (supervisor = own child, student = themselves). Otherwise 404 – deliberately not 403, to
/// prevent enumerating other children's ids. Attach via <c>[ServiceFilter(typeof(ChildOwnershipFilter))]</c>
/// on the child-related controllers (counterpart to <see cref="PlanOwnershipFilter"/>).
/// </summary>
public class ChildOwnershipFilter(AuthAccess access) : IAsyncActionFilter
{
    /// <summary>Checks child ownership for the current action and aborts with 404 if it is missing.</summary>
    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        if (ctx.ActionArguments.TryGetValue("childId", out var v) && v is int childId
            && !await access.OwnsChildAsync(ctx.HttpContext.User, childId, ctx.HttpContext.RequestAborted))
        {
            ctx.Result = ControllerBaseErrorExtensions.ProblemResult(ctx.HttpContext, ApiErrors.NotFound, "Child not found.");
            return;
        }
        await next();
    }
}
