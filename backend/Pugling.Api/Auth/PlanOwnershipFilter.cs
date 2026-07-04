using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;

namespace Pugling.Api.Auth;

/// <summary>
/// Action-Filter für alle Endpunkte unterhalb eines Lehrplans (Route-Parameter <c>planId</c>):
/// stellt zentral sicher, dass der Plan existiert und dem angemeldeten Nutzer gehört
/// (Sohn = eigener Plan, Vater = Plan eines eigenen Kindes). Andernfalls 404 bzw. 403.
/// Per <c>[ServiceFilter(typeof(PlanOwnershipFilter))]</c> an den Study-Controllern anzubringen.
/// </summary>
public class PlanOwnershipFilter(PuglingDbContext db, AuthAccess access) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        if (ctx.ActionArguments.TryGetValue("planId", out var v) && v is int planId)
        {
            var plan = await db.StudyPlans.FirstOrDefaultAsync(p => p.Id == planId);
            if (plan is null) { ctx.Result = new NotFoundObjectResult("Lehrplan nicht gefunden."); return; }
            if (!await access.OwnsPlanAsync(ctx.HttpContext.User, plan)) { ctx.Result = new ForbidResult(); return; }
        }
        await next();
    }
}
