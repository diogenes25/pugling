using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Errors;

namespace Pugling.Api.Auth;

/// <summary>
/// Action filter for all endpoints below a study plan (route parameter <c>planId</c>):
/// centrally ensures that the plan exists and belongs to the logged-in user
/// (student = own plan, supervisor = plan of one of their own children). Otherwise 404 or 403.
/// Attach via <c>[ServiceFilter(typeof(PlanOwnershipFilter))]</c> on the study controllers.
/// </summary>
public class PlanOwnershipFilter(PuglingDbContext db, AuthAccess access) : IAsyncActionFilter
{
    /// <summary>Checks existence and ownership of the plan for the current action and aborts with 404 or 403.</summary>
    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        if (ctx.ActionArguments.TryGetValue("planId", out var v) && v is int planId)
        {
            var ct = ctx.HttpContext.RequestAborted;
            var plan = await db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId, ct);
            if (plan is null) { ctx.Result = ControllerBaseErrorExtensions.ProblemResult(ctx.HttpContext, ApiErrors.NotFound, "Study plan not found."); return; }
            if (!await access.OwnsPlanAsync(ctx.HttpContext.User, plan, ct)) { ctx.Result = new ForbidResult(); return; }
        }
        await next();
    }
}
