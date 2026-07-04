using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;

namespace Pugling.Api.Controllers;

/// <summary>
/// Selbstauskunft für den angemeldeten Sohn: eigener Punktestand (Wallet) und Kurzprofil.
/// Schließt die Lücke, dass der kontoübergreifende Punktestand sonst nur der Vater lesen kann
/// (<see cref="Admin.ChildrenController"/> ist <c>Vater</c>-only).
/// </summary>
[ApiController]
[Route("api/me")]
[Tags("Me")]
[Produces("application/json")]
[Authorize(Roles = Roles.Sohn)]
public class MeController(PuglingDbContext db) : ControllerBase
{
    /// <summary>Eine einzelne Punkte-Buchung (Gutschrift positiv, Abzug negativ).</summary>
    public record PointsEntryResponse(int Id, int Amount, string Reason, DateTime CreatedAt);
    /// <summary>Punktestand (Wallet) des Kindes samt der letzten Buchungen.</summary>
    public record WalletResponse(int ChildId, int Balance, IReadOnlyList<PointsEntryResponse> Entries);

    /// <summary>Eigener Punktestand (Wallet) samt der letzten Buchungen.</summary>
    [HttpGet("points")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WalletResponse>> Points()
    {
        var cid = User.ChildId();
        if (cid is null) return Forbid();

        var entries = await db.ChildPoints
            .AsNoTracking()
            .Where(p => p.ChildId == cid)
            .OrderByDescending(p => p.CreatedAt)
            .Take(50)
            .Select(p => new PointsEntryResponse(p.Id, p.Amount, p.Reason, p.CreatedAt))
            .ToListAsync();

        var balance = await db.ChildPoints.Where(p => p.ChildId == cid).SumAsync(p => (int?)p.Amount) ?? 0;
        return new WalletResponse(cid.Value, balance, entries);
    }
}
