using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Auth;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// The savings goal of one child, for its supervisor (B-593): the same answer the child reads under
/// <c>student/me/savings-goal</c>. Read only - the goal is the child's own choice, the supervisor does not pin it on
/// the child's behalf; he moves it through his shop (price, stock, deactivating the listing).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/{childId:int}/savings-goal")]
[Tags("Supervisor – Shop")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class ChildSavingsGoalController(SavingsGoalService savingsGoal, TimeProvider time) : ControllerBase
{
    /// <summary>The child's savings goal; 204 when it has none, 404 for a child you do not supervise.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SavingsGoalResponse>> Get(int childId, CancellationToken ct)
    {
        var goal = await savingsGoal.ReadAsync(childId, time.GetUtcNow().UtcDateTime, ct);
        return goal is null ? NoContent() : goal;
    }
}
