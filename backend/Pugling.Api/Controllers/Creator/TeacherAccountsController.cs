using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Registration of a <b>teacher account</b>: an adult who creates content and <b>supervises no
/// child</b>.
///
/// <para>
/// Why this is not a new entity type: the three tiers are <i>roles</i>, decoupled from the login
/// (docs/grundprinzip.md). An account carries one <see cref="AccountProfile"/> per role; a father gets
/// Creator <b>and</b> Supervisor, a teacher only Creator. This means their token lacks the supervisor claim, and
/// all supervision endpoints reject them via their existing <c>[Authorize(Roles = Roles.Supervisor)]</c>
/// – without a single special-case rule. Authorship (<c>Exercise.AuthorAdultId</c>) and RWX permissions
/// (<c>ExerciseGrant.CreatorId</c>) still hang off the same <see cref="Adult"/> row, which is why creating,
/// granting permissions, publishing, and withdrawing continue to work unchanged.
/// </para>
/// <para>
/// Not to be confused with the <c>CreatorProfile</c> ("subject teacher") under
/// <c>api/v1/creator/profiles</c>: that is the <i>subject-matter</i> description (subject, school type, didactics) for
/// the AI creator. Here, the <i>identity</i> used to log in is created.
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
    /// Creates a teacher account (registration, reachable without login – like the father registration).
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
        var teacher = new Adult
        {
            Name = dto.Name.Trim(),
            Email = dto.Email,
            Pin = string.IsNullOrEmpty(dto.Pin) ? "" : PinHasher.Hash(dto.Pin),
        };
        db.Adults.Add(teacher);
        await db.SaveChangesAsync(ct);

        var account = await accounts.EnsureForTeacherAsync(teacher, ct);
        var roles = account.Profiles.Select(p => p.Role.ToString()).Distinct().ToList();
        return CreatedAtAction(nameof(Get), new { creatorId = teacher.Id },
            new TeacherAccountResponse(teacher.Id, account.Id, teacher.Name, teacher.Email, roles));
    }

    /// <summary>
    /// The teacher's own account. Owner only – the route id must match the <c>fid</c> in the token,
    /// otherwise a creator could query the accounts of others.
    /// </summary>
    [HttpGet("{creatorId:int}")]
    [Authorize(Roles = Roles.Creator)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeacherAccountResponse>> Get(int creatorId, CancellationToken ct)
    {
        if (User.AdultId() != creatorId) return Forbid();

        var teacher = await db.Adults.AsNoTracking().FirstOrDefaultAsync(f => f.Id == creatorId, ct);
        if (teacher is null) return NotFound();
        var account = await db.Accounts.AsNoTracking().Include(a => a.Profiles)
            .FirstOrDefaultAsync(a => a.Profiles.Any(p => p.AdultId == creatorId), ct);
        if (account is null) return NotFound();

        var roles = account.Profiles.Select(p => p.Role.ToString()).Distinct().ToList();
        return new TeacherAccountResponse(teacher.Id, account.Id, teacher.Name, teacher.Email, roles);
    }
}
