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
    /// <summary>Ungenutzter Teil des Filter-Paars (die Prüfung sitzt vollständig in <see cref="OnActionExecuting"/>).</summary>
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
        if (await EmailTakenAsync(dto.Email)) return this.ProblemWithCode(ApiErrors.DuplicateEmail, "Email already in use.");

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

        if (dto.Email is not null && await EmailTakenAsync(dto.Email, exceptAdultId: adultId))
            return this.ProblemWithCode(ApiErrors.DuplicateEmail, "Email already in use.");

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

    /// <summary>
    /// Ist die E-Mail-Adresse schon von einem <b>anderen</b> Konto belegt?
    /// <para>
    /// Geprüft wird an <c>Account.Email</c>, nicht an <c>Adult.Email</c>: dort sitzt der (gefilterte)
    /// Unique-Index. Ohne diese Vorprüfung lief die Registrierung auf halbem Weg auf: <c>Adult</c> war
    /// schon gespeichert, das Anlegen des Kontos scheiterte am Index, und der Aufrufer bekam <b>500</b> –
    /// zurück blieb ein Erwachsener ohne Login.
    /// </para>
    /// </summary>
    /// <param name="email">Die gewünschte Adresse; leer heißt „keine", das kollidiert nie (Index ist gefiltert).</param>
    /// <param name="exceptAdultId">Beim Ändern der eigene Erwachsene – seine eigene Adresse ist keine Kollision.</param>
    private Task<bool> EmailTakenAsync(string? email, int? exceptAdultId = null) =>
        string.IsNullOrWhiteSpace(email)
            ? Task.FromResult(false)
            : db.Accounts.AsNoTracking().AnyAsync(a => a.Email == email
                && (exceptAdultId == null || !a.Profiles.Any(p => p.AdultId == exceptAdultId)));

    /// <summary>
    /// Löscht einen Erwachsenen – samt der Kinder, die dadurch <b>ihren letzten Supervisor</b> verlieren,
    /// und samt seines Login-Kontos.
    /// <para>
    /// Ein von mehreren betreutes Kind (Vater <i>und</i> Mutter) bleibt bestehen; es verliert nur diesen
    /// einen Betreuer. Nur das Kind, dem niemand bleibt, geht mit – denn seit dem Multi-Supervisor-Umbau
    /// hängt ein <see cref="Child"/> <b>nicht</b> mehr per Fremdschlüssel am Erwachsenen, sondern über
    /// <see cref="SupervisorLink"/>. Die Kaskade der Datenbank räumt darum nur die Verknüpfung ab und ließ
    /// das Kind als <b>Waise</b> zurück: von keinem Erwachsenen mehr sichtbar oder löschbar, aber mit
    /// weiterhin funktionierendem PIN-Login. Die frühere Zusicherung „samt aller Kinder, Fächer, Kapitel"
    /// war seit dem Umbau falsch – Fächer gehören ohnehin keinem Erwachsenen.
    /// </para>
    /// </summary>
    [HttpDelete("{adultId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int adultId, CancellationToken ct)
    {
        var adult = await db.Adults.FindAsync([adultId], ct);
        if (adult is null) return NotFound();

        // Die Kinder, für die dieser Erwachsene der einzige Betreuer ist.
        var verwaisende = await db.Children
            .Where(c => c.SupervisorLinks.Any(l => l.SupervisorId == adultId)
                && c.SupervisorLinks.All(l => l.SupervisorId == adultId))
            .ToListAsync(ct);
        db.Children.RemoveRange(verwaisende);

        // Das Login-Konto verliert mit dem Erwachsenen sein letztes Profil und bliebe sonst als leere Hülle
        // zurück – mitsamt seiner E-Mail, die den (eindeutigen) Adressraum dauerhaft blockiert hätte.
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
