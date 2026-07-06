using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Admin;

/// <summary>Verwaltung der Väter (oberste Ebene des Admin-Bereichs).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.V1 + "/fathers")]
[Tags("Admin – Fathers")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
public class FathersController(PuglingDbContext db) : ControllerBase, IActionFilter
{
    /// <summary>Ein Vater darf nur seinen eigenen Datensatz lesen/ändern/löschen (Route-fatherId == Token-fid).</summary>
    [NonAction]
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ActionArguments.TryGetValue("fatherId", out var v) && v is int fid && User.FatherId() != fid)
            context.Result = Forbid();
    }
    [NonAction]
    public void OnActionExecuted(ActionExecutedContext context) { }

    /// <summary>Vater ohne PIN (wird nie ausgeliefert).</summary>
    public record FatherResponse(int Id, string Name, string? Email, DateTime CreatedAt, int ChildrenCount);

    IQueryable<FatherResponse> Project(IQueryable<Father> q) =>
        q.Select(f => new FatherResponse(f.Id, f.Name, f.Email, f.CreatedAt, f.Children.Count));

    /// <summary>Der eigene Vater-Datensatz (Selbstauskunft).</summary>
    [HttpGet]
    public async Task<IEnumerable<FatherResponse>> List() =>
        await Project(db.Fathers.Where(f => f.Id == User.FatherId())).ToListAsync();

    /// <summary>Ein einzelner Vater.</summary>
    [HttpGet("{fatherId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FatherResponse>> Get(int fatherId)
    {
        var father = await Project(db.Fathers.Where(f => f.Id == fatherId)).FirstOrDefaultAsync();
        return father is null ? NotFound() : father;
    }

    public record CreateFatherDto(string Name, string? Email, string? Pin);

    /// <summary>Erstellt einen neuen Vater (Registrierung, ohne Anmeldung erreichbar).</summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FatherResponse>> Create(CreateFatherDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return this.ProblemWithCode(ApiErrors.ValidationError, "Name is required.");

        var father = new Father { Name = dto.Name.Trim(), Email = dto.Email, Pin = string.IsNullOrEmpty(dto.Pin) ? "" : PinHasher.Hash(dto.Pin) };
        db.Fathers.Add(father);
        await db.SaveChangesAsync();

        var response = new FatherResponse(father.Id, father.Name, father.Email, father.CreatedAt, 0);
        return CreatedAtAction(nameof(Get), new { fatherId = father.Id }, response);
    }

    /// <summary>Nur gesetzte Felder werden geändert.</summary>
    public record UpdateFatherDto(string? Name, string? Email, string? Pin);

    /// <summary>Ändert einen Vater (partiell).</summary>
    [HttpPatch("{fatherId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FatherResponse>> Update(int fatherId, UpdateFatherDto dto)
    {
        var father = await db.Fathers.FirstOrDefaultAsync(f => f.Id == fatherId);
        if (father is null) return NotFound();

        if (dto.Name is not null) father.Name = dto.Name.Trim();
        if (dto.Email is not null) father.Email = dto.Email;
        if (dto.Pin is not null) father.Pin = string.IsNullOrEmpty(dto.Pin) ? "" : PinHasher.Hash(dto.Pin);
        await db.SaveChangesAsync();

        return (await Project(db.Fathers.Where(f => f.Id == fatherId)).FirstAsync());
    }

    /// <summary>Löscht einen Vater samt aller Kinder, Fächer, Kapitel und Lektionen.</summary>
    [HttpDelete("{fatherId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int fatherId)
    {
        var father = await db.Fathers.FindAsync(fatherId);
        if (father is null) return NotFound();
        db.Fathers.Remove(father);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
