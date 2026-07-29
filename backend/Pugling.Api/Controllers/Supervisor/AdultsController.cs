using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>Verwaltung der Erwachsenen (oberste Ebene des Admin-Bereichs).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/adults")]
[Tags("Supervisor – Adults")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
public class AdultsController(PuglingDbContext db, AccountService accounts) : ControllerBase, IActionFilter
{
    /// <summary>Ein Erwachsener darf nur seinen eigenen Datensatz lesen/ändern/löschen (Route-adultId == Token-fid).</summary>
    [NonAction]
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ActionArguments.TryGetValue("adultId", out var v) && v is int aid && User.AdultId() != aid)
            context.Result = Forbid();
    }
    [NonAction]
    public void OnActionExecuted(ActionExecutedContext context) { }

    IQueryable<AdultResponse> Project(IQueryable<Adult> q) =>
        q.Select(a => new AdultResponse(a.Id, a.Name, a.Email, a.CreatedAt, a.SupervisedLinks.Count));

    /// <summary>Der eigene Datensatz (Selbstauskunft).</summary>
    [HttpGet]
    public async Task<IEnumerable<AdultResponse>> List() =>
        await Project(db.Adults.Where(a => a.Id == User.AdultId())).ToListAsync();

    /// <summary>Ein einzelner Erwachsener.</summary>
    [HttpGet("{adultId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdultResponse>> Get(int adultId)
    {
        var adult = await Project(db.Adults.Where(a => a.Id == adultId)).FirstOrDefaultAsync();
        return adult is null ? NotFound() : adult;
    }

    /// <summary>Erstellt einen neuen Vater (Registrierung, ohne Anmeldung erreichbar).</summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdultResponse>> Create(CreateAdultDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return this.ProblemWithCode(ApiErrors.ValidationError, "Name is required.");

        var adult = new Adult { Name = dto.Name.Trim(), Email = dto.Email, Pin = string.IsNullOrEmpty(dto.Pin) ? "" : PinHasher.Hash(dto.Pin) };
        db.Adults.Add(adult);
        await db.SaveChangesAsync();
        // Login-Konto (Creator+Supervisor) sofort anlegen, damit der neue Vater sich einloggen kann.
        await accounts.EnsureForFatherAsync(adult);

        var response = new AdultResponse(adult.Id, adult.Name, adult.Email, adult.CreatedAt, 0);
        return CreatedAtAction(nameof(Get), new { adultId = adult.Id }, response);
    }

    /// <summary>Ändert einen Erwachsenen (partiell).</summary>
    [HttpPatch("{adultId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdultResponse>> Update(int adultId, UpdateAdultDto dto)
    {
        var adult = await db.Adults.FirstOrDefaultAsync(a => a.Id == adultId);
        if (adult is null) return NotFound();

        if (dto.Name is not null) adult.Name = dto.Name.Trim();
        if (dto.Email is not null) adult.Email = dto.Email;
        if (dto.Pin is not null)
        {
            adult.Pin = string.IsNullOrEmpty(dto.Pin) ? "" : PinHasher.Hash(dto.Pin);
            // PIN-Hash auf das Login-Konto spiegeln, damit der konto-zentrische Login (/auth/login) synchron bleibt.
            (await accounts.EnsureForFatherAsync(adult)).PinHash = adult.Pin;
        }
        await db.SaveChangesAsync();

        return (await Project(db.Adults.Where(a => a.Id == adultId)).FirstAsync());
    }

    /// <summary>Löscht einen Erwachsenen samt aller Kinder, Fächer, Kapitel und Lektionen.</summary>
    [HttpDelete("{adultId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int adultId)
    {
        var adult = await db.Adults.FindAsync(adultId);
        if (adult is null) return NotFound();
        db.Adults.Remove(adult);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
