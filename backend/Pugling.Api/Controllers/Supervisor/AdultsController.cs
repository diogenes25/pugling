using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>Management of adults (top tier of the admin area).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/adults")]
[Tags("Supervisor – Adults")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
public class AdultsController(PuglingDbContext db, AccountService accounts) : ControllerBase, IActionFilter
{
    /// <summary>An adult may only read/change/delete their own record (route adultId == token fid).</summary>
    [NonAction]
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ActionArguments.TryGetValue("adultId", out var v) && v is int aid && User.AdultId() != aid)
            context.Result = Forbid();
    }
    /// <summary>Unused part of the filter pair (the check sits entirely in <see cref="OnActionExecuting"/>).</summary>
    [NonAction]
    public void OnActionExecuted(ActionExecutedContext context) { }

    IQueryable<AdultResponse> Project(IQueryable<Adult> q) =>
        q.Select(a => new AdultResponse(a.Id, a.Name, a.Email, a.CreatedAt, a.SupervisedLinks.Count));

    /// <summary>The caller's own record (self-service lookup).</summary>
    [HttpGet]
    public async Task<IEnumerable<AdultResponse>> List(CancellationToken ct = default) =>
        await Project(db.Adults.Where(a => a.Id == User.AdultId())).ToListAsync(ct);

    /// <summary>A single adult.</summary>
    [HttpGet("{adultId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdultResponse>> Get(int adultId, CancellationToken ct = default)
    {
        var adult = await Project(db.Adults.Where(a => a.Id == adultId)).FirstOrDefaultAsync(ct);
        return adult is null ? NotFound() : adult;
    }

    /// <summary>Creates a new father (registration, reachable without login).</summary>
    // Same throttle as the login (B-48): anonymous and writing is the one combination a script can abuse
    // without limit - unbounded accounts, or squatting the e-mail addresses of real people via the
    // uniqueness check. The registration itself stays open on purpose (bootstrap, E2E, several families
    // and teachers per instance); only the missing brake is closed, with the policy that already exists.
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AdultResponse>> Create(CreateAdultDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return this.ProblemWithCode(ApiErrors.ValidationError, "Name is required.");
        if (await EmailTakenAsync(dto.Email, ct: ct)) return this.ProblemWithCode(ApiErrors.DuplicateEmail, "Email already in use.");

        var adult = new Adult { Name = dto.Name.Trim(), Email = dto.Email, Pin = string.IsNullOrEmpty(dto.Pin) ? "" : PinHasher.Hash(dto.Pin) };
        db.Adults.Add(adult);
        await db.SaveChangesAsync(ct);
        // Create the login account (creator+supervisor) right away so the new adult can log in.
        await accounts.EnsureForAdultAsync(adult, ct);

        var response = new AdultResponse(adult.Id, adult.Name, adult.Email, adult.CreatedAt, 0);
        return CreatedAtAction(nameof(Get), new { adultId = adult.Id }, response);
    }

    /// <summary>Changes an adult (partial).</summary>
    [HttpPatch("{adultId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdultResponse>> Update(int adultId, UpdateAdultDto dto, CancellationToken ct = default)
    {
        var adult = await db.Adults.FirstOrDefaultAsync(a => a.Id == adultId, ct);
        if (adult is null) return NotFound();

        if (dto.Email is not null && await EmailTakenAsync(dto.Email, exceptAdultId: adultId, ct: ct))
            return this.ProblemWithCode(ApiErrors.DuplicateEmail, "Email already in use.");

        if (dto.Name is not null) adult.Name = dto.Name.Trim();
        if (dto.Email is not null) adult.Email = dto.Email;
        if (dto.Pin is not null) adult.Pin = string.IsNullOrEmpty(dto.Pin) ? "" : PinHasher.Hash(dto.Pin);
        // Mirror name, address and PIN hash onto the login account - in the SAME commit. Only the PIN used to
        // travel along and the address drifted: the collision check above reads the account, but the unique
        // index also sits on the adult. See AccountService.MirrorAsync.
        await accounts.MirrorAsync(adult, ct);
        await db.SaveChangesAsync(ct);

        return (await Project(db.Adults.Where(a => a.Id == adultId)).FirstAsync(ct));
    }

    /// <summary>
    /// Is the email address already taken by <b>another</b> account?
    /// <para>
    /// Checked against <c>Account.Email</c>, not <c>Adult.Email</c>: that's where the (filtered)
    /// unique index sits. Without this pre-check, registration used to run aground halfway: <c>Adult</c> was
    /// already saved, creating the account failed on the index, and the caller got <b>500</b> –
    /// leaving behind an adult with no login.
    /// </para>
    /// </summary>
    /// <param name="email">The desired address; empty means "none", which never collides (the index is filtered).</param>
    /// <param name="exceptAdultId">The adult's own record when updating – its own address is not a collision.</param>
    /// <param name="ct">Cancellation token.</param>
    private Task<bool> EmailTakenAsync(string? email, int? exceptAdultId = null, CancellationToken ct = default) =>
        string.IsNullOrWhiteSpace(email)
            ? Task.FromResult(false)
            : db.Accounts.AsNoTracking().AnyAsync(a => a.Email == email
                && (exceptAdultId == null || !a.Profiles.Any(p => p.AdultId == exceptAdultId)), ct);

    /// <summary>
    /// Deletes an adult – together with any children who thereby lose <b>their last supervisor</b>,
    /// and together with its login account.
    /// <para>
    /// A child supervised by several people (father <i>and</i> mother) continues to exist; it only loses this
    /// one caregiver. Only the child left with no one remaining is removed as well – because since the
    /// multi-supervisor restructuring, a <see cref="Child"/> is <b>no longer</b> attached to the adult via a
    /// foreign key, but via <see cref="SupervisorLink"/>. The database cascade therefore only clears the
    /// link and would leave the child behind as an <b>orphan</b>: no longer visible or deletable by any adult,
    /// but with a still-functioning PIN login.
    /// </para>
    /// </summary>
    [HttpDelete("{adultId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int adultId, CancellationToken ct)
    {
        var adult = await db.Adults.FindAsync([adultId], ct);
        if (adult is null) return NotFound();

        // The children this adult is the only supervisor for.
        var verwaisende = await db.Children
            .Where(c => c.SupervisorLinks.Any(l => l.SupervisorId == adultId)
                && c.SupervisorLinks.All(l => l.SupervisorId == adultId))
            .ToListAsync(ct);
        db.Children.RemoveRange(verwaisende);

        // With the adult, the login account loses its last profile and would remain as an empty shell -
        // together with its e-mail, which would block the (unique) address space forever.
        var konten = await db.Accounts
            .Where(a => a.Profiles.All(p => p.AdultId == adultId))
            .Where(a => a.Profiles.Any(p => p.AdultId == adultId))
            .ToListAsync(ct);
        db.Accounts.RemoveRange(konten);

        db.Adults.Remove(adult);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
