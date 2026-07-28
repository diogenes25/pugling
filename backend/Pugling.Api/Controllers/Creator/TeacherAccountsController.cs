using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Registrierung eines <b>Lehrer-Kontos</b>: ein Erwachsener, der Inhalte erstellt und <b>kein Kind
/// betreut</b>.
///
/// <para>
/// Warum das kein neuer Entitätstyp ist: Die drei Ebenen sind <i>Rollen</i>, entkoppelt vom Login
/// (docs/grundprinzip.md). Ein Konto trägt je Rolle ein <see cref="AccountProfile"/>; ein Vater bekommt
/// Creator <b>und</b> Supervisor, ein Lehrer nur Creator. Damit fehlt seinem Token der Supervisor-Claim, und
/// alle Betreuungs-Endpunkte weisen ihn über ihr vorhandenes <c>[Authorize(Roles = Roles.Supervisor)]</c> ab
/// – ohne eine einzige Sonderregel. Autorschaft (<c>Exercise.AuthorFatherId</c>) und RWX-Rechte
/// (<c>ExerciseGrant.CreatorId</c>) hängen weiter an derselben <see cref="Father"/>-Zeile, weshalb Anlegen,
/// Rechtevergabe, Freigabe und Rücknahme unverändert funktionieren.
/// </para>
/// <para>
/// Nicht zu verwechseln mit dem <c>CreatorProfile</c> („Fachlehrer") unter
/// <c>api/v1/creator/profiles</c>: das ist die <i>fachliche</i> Beschreibung (Fach, Schulart, Didaktik) für
/// den KI-Creator. Hier entsteht die <i>Identität</i>, mit der man sich anmeldet.
/// </para>
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/teacher-accounts")]
[Tags("Creator – Teacher Accounts")]
[Produces("application/json")]
public class TeacherAccountsController(PuglingDbContext db, AccountService accounts) : ControllerBase
{
    /// <summary>
    /// Legt ein Lehrer-Konto an (Registrierung, ohne Anmeldung erreichbar – wie die Vater-Registrierung).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeacherAccountResponse>> Create(CreateTeacherDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return this.ProblemWithCode(ApiErrors.ValidationError, "Name is required.");

        // Die PIN wird gehasht (PinHasher) und auf das Konto gespiegelt – sonst liefe der konto-zentrische
        // Login aus dem Takt. Genau dieselbe Regel wie bei Vater und Kind.
        var teacher = new Father
        {
            Name = dto.Name.Trim(),
            Email = dto.Email,
            Pin = string.IsNullOrEmpty(dto.Pin) ? "" : PinHasher.Hash(dto.Pin),
        };
        db.Fathers.Add(teacher);
        await db.SaveChangesAsync(ct);

        var account = await accounts.EnsureForTeacherAsync(teacher, ct);
        var roles = account.Profiles.Select(p => p.Role.ToString()).Distinct().ToList();
        return CreatedAtAction(nameof(Get), new { creatorId = teacher.Id },
            new TeacherAccountResponse(teacher.Id, account.Id, teacher.Name, teacher.Email, roles));
    }

    /// <summary>
    /// Das eigene Lehrer-Konto. Nur der Inhaber – die Route-Id muss der <c>fid</c> im Token entsprechen,
    /// sonst könnte ein Creator die Konten anderer abfragen.
    /// </summary>
    [HttpGet("{creatorId:int}")]
    [Authorize(Roles = Roles.Creator)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeacherAccountResponse>> Get(int creatorId, CancellationToken ct)
    {
        if (User.FatherId() != creatorId) return Forbid();

        var teacher = await db.Fathers.AsNoTracking().FirstOrDefaultAsync(f => f.Id == creatorId, ct);
        if (teacher is null) return NotFound();
        var account = await db.Accounts.AsNoTracking().Include(a => a.Profiles)
            .FirstOrDefaultAsync(a => a.Profiles.Any(p => p.FatherId == creatorId), ct);
        if (account is null) return NotFound();

        var roles = account.Profiles.Select(p => p.Role.ToString()).Distinct().ToList();
        return new TeacherAccountResponse(teacher.Id, account.Id, teacher.Name, teacher.Email, roles);
    }
}
