using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Creator-Profile: der <b>Fachlehrer</b> hinter einer Übung – ein Fach, ein Schulzweig, ein
/// Klassenstufen-Bereich, optional eine Buchreihe, dazu Persona und Didaktik. Ein KI-Creator lädt sich
/// daraus seine Rolle, statt für jedes Fach denselben Generalisten zu spielen; der Match-Endpunkt
/// beantwortet die eigentliche Frage: <i>welcher Lehrer kennt den Stoff dieses Kindes?</i>
/// Lesen darf jeder Creator (der Katalog ist geteilt), ändern nur der Owner.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/profiles")]
[Tags("Creator – Profiles")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class CreatorProfilesController(PuglingDbContext db, CreatorProfileService profiles, AuthAccess access)
    : ControllerBase
{
    /// <summary>Alle Profile (alphabetisch), optional gefiltert.</summary>
    /// <param name="subjectId">Nur Profile zu diesem Katalog-Fach.</param>
    /// <param name="seriesId">Nur Profile zu dieser Lehrwerk-Reihe.</param>
    /// <param name="mineOnly">true = nur eigene Profile.</param>
    /// <param name="includeInactive">true = auch stillgelegte Profile.</param>
    /// <param name="ct">Abbruch-Token.</param>
    [HttpGet]
    public async Task<IReadOnlyList<CreatorProfileResponse>> List(
        [FromQuery] int? subjectId, [FromQuery] int? seriesId, [FromQuery] bool? mineOnly,
        [FromQuery] bool? includeInactive, CancellationToken ct)
    {
        var fid = User.CreatorId();
        var query = db.CreatorProfiles.AsNoTracking().Include(p => p.Series).AsQueryable();

        if (subjectId is int sid) query = query.Where(p => p.SubjectId == sid);
        if (seriesId is int serId) query = query.Where(p => p.SeriesId == serId);
        if (mineOnly is true) query = query.Where(p => p.OwnerAdultId == fid);
        if (includeInactive is not true) query = query.Where(p => p.Active);

        var found = await query.OrderBy(p => p.Name).ThenBy(p => p.Id).ToListAsync(ct);
        return [.. found.Select(p => CreatorProfileService.Map(p, fid))];
    }

    /// <summary>Ein Profil per Id.</summary>
    [HttpGet("{profileId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreatorProfileResponse>> Get(int profileId, CancellationToken ct)
    {
        var profile = await db.CreatorProfiles.AsNoTracking().Include(p => p.Series)
            .FirstOrDefaultAsync(p => p.Id == profileId, ct);
        return profile is null ? NotFound() : CreatorProfileService.Map(profile, User.CreatorId());
    }

    /// <summary>
    /// Die zu einem Kind passenden Profile, bestes zuerst (Reihen-Treffer wiegt am schwersten).
    /// Der Endpunkt liest Kind-Daten und ist darum an die <b>Betreuung</b> gebunden: ein Creator, der
    /// dieses Kind nicht betreut, bekommt <c>403</c> – sonst wäre die Profil-Suche ein Seitenkanal auf
    /// fremde Kind-Profile.
    /// </summary>
    /// <param name="childId">Das Kind, zu dem der passende Creator gesucht wird.</param>
    /// <param name="subjectId">Optional auf ein Fach verengen (fachneutrale Profile bleiben enthalten).</param>
    /// <param name="ct">Abbruch-Token.</param>
    [HttpGet("match")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CreatorProfileMatch>>> Match(
        [FromQuery] int childId, [FromQuery] int? subjectId, CancellationToken ct)
    {
        if (!await db.Children.AnyAsync(c => c.Id == childId, ct)) return NotFound();
        if (!await access.FatherOwnsChildAsync(User, childId))
            return this.ProblemWithCode(ApiErrors.Forbidden, "You do not supervise this child.");

        return Ok(await profiles.MatchAsync(childId, subjectId, User.CreatorId(), ct));
    }

    /// <summary>Legt ein Profil an (Owner = das aufrufende Konto).</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatorProfileResponse>> Create(CreateCreatorProfileDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return this.ProblemWithCode(ApiErrors.ValidationError, "Name is required.");
        if (await ReferenceProblemAsync(dto.SubjectId, dto.SeriesId, ct) is { } problem) return problem;
        if (dto.GradeMin > dto.GradeMax)
            return this.ProblemWithCode(ApiErrors.ValidationError, "GradeMin must not be greater than GradeMax.");
        if (await NameTakenAsync(dto.Name.Trim(), User.CreatorId(), ct))
            return this.ProblemWithCode(ApiErrors.DuplicateProfileName, "A profile with this name already exists.");

        var profile = new CreatorProfile
        {
            Name = dto.Name.Trim(),
            OwnerAdultId = User.CreatorId(),
            SubjectName = Trimmed(dto.SubjectName),
            SubjectId = dto.SubjectId,
            SchoolTypes = dto.SchoolTypes ?? SchoolTypes.None,
            GradeMin = dto.GradeMin,
            GradeMax = dto.GradeMax,
            SeriesId = dto.SeriesId,
            SourceLang = Trimmed(dto.SourceLang) ?? "en",
            TargetLang = Trimmed(dto.TargetLang) ?? "de",
            Persona = Trimmed(dto.Persona),
            Didactics = Trimmed(dto.Didactics),
            DefaultTypes = Clean(dto.DefaultTypes),
            Active = dto.Active ?? true,
        };
        db.CreatorProfiles.Add(profile);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { profileId = profile.Id },
            CreatorProfileService.Map(await LoadWithSeriesAsync(profile.Id, ct), User.CreatorId()));
    }

    /// <summary>Ändert ein Profil (partiell, nur Owner).</summary>
    [HttpPatch("{profileId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreatorProfileResponse>> Update(int profileId, UpdateCreatorProfileDto dto,
        CancellationToken ct)
    {
        var profile = await db.CreatorProfiles.FirstOrDefaultAsync(p => p.Id == profileId, ct);
        if (profile is null) return NotFound();
        var fid = User.CreatorId();
        if (!ClaimsPrincipalExtensions.IsOwnedBy(profile.OwnerAdultId, fid))
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only the owner may change this creator profile.");
        if (await ReferenceProblemAsync(dto.SubjectId, dto.SeriesId, ct) is { } problem) return problem;

        if (dto.Name is not null)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Name must not be empty.");
            if (await NameTakenAsync(name, fid, ct, exceptProfileId: profileId))
                return this.ProblemWithCode(ApiErrors.DuplicateProfileName, "A profile with this name already exists.");
            profile.Name = name;
        }
        if (dto.SubjectName is not null) profile.SubjectName = Trimmed(dto.SubjectName);
        // Reihenfolge: erst der Wert, dann der Clear-Schalter – so gewinnt „leeren" auch, wenn ein Client
        // (etwa ein Formular, das immer alle Felder schickt) beides mitsendet.
        if (dto.SubjectId.HasValue) profile.SubjectId = dto.SubjectId;
        if (dto.ClearSubject) { profile.SubjectId = null; profile.SubjectName = null; }
        if (dto.SchoolTypes.HasValue) profile.SchoolTypes = dto.SchoolTypes.Value;
        if (dto.GradeMin.HasValue) profile.GradeMin = dto.GradeMin;
        if (dto.ClearGradeMin) profile.GradeMin = null;
        if (dto.GradeMax.HasValue) profile.GradeMax = dto.GradeMax;
        if (dto.ClearGradeMax) profile.GradeMax = null;
        if (dto.SeriesId.HasValue) profile.SeriesId = dto.SeriesId;
        if (dto.ClearSeries) profile.SeriesId = null;
        if (Trimmed(dto.SourceLang) is { } src) profile.SourceLang = src;
        if (Trimmed(dto.TargetLang) is { } tgt) profile.TargetLang = tgt;
        if (dto.Persona is not null) profile.Persona = Trimmed(dto.Persona);
        if (dto.Didactics is not null) profile.Didactics = Trimmed(dto.Didactics);
        // Neue Liste zuweisen (kein In-Place-Mutieren – JSON-Spalten-Fallstrick).
        if (dto.DefaultTypes is not null) profile.DefaultTypes = Clean(dto.DefaultTypes);
        if (dto.Active.HasValue) profile.Active = dto.Active.Value;

        if (profile.GradeMin > profile.GradeMax)
            return this.ProblemWithCode(ApiErrors.ValidationError, "GradeMin must not be greater than GradeMax.");

        await db.SaveChangesAsync(ct);
        return CreatorProfileService.Map(await LoadWithSeriesAsync(profileId, ct), fid);
    }

    /// <summary>
    /// Löscht ein Profil (nur Owner). Bereits erzeugte Übungen bleiben unberührt – das Profil ist die
    /// Werkbank, nicht der Besitzer des Ergebnisses.
    /// </summary>
    [HttpDelete("{profileId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int profileId, CancellationToken ct)
    {
        var profile = await db.CreatorProfiles.FirstOrDefaultAsync(p => p.Id == profileId, ct);
        if (profile is null) return NotFound();
        if (!ClaimsPrincipalExtensions.IsOwnedBy(profile.OwnerAdultId, User.CreatorId()))
            return this.ProblemWithCode(ApiErrors.NotOwner, "Only the owner may delete this creator profile.");

        db.CreatorProfiles.Remove(profile);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Trägt schon ein anderes Profil desselben Creators diesen Namen? Der Name ist je Creator eindeutig
    /// (Unique-Index <c>CreatorProfile(OwnerAdultId, Name)</c>) – ohne diese Vorprüfung quittierte das
    /// Anlegen eines zweiten „Frau Meier" mit <b>500</b> statt mit einem Fachfehler am Namensfeld.
    /// </summary>
    private Task<bool> NameTakenAsync(string name, int? ownerAdultId, CancellationToken ct, int? exceptProfileId = null) =>
        ownerAdultId is null
            ? Task.FromResult(false) // Ohne Owner greift der gefilterte Index nicht.
            : db.CreatorProfiles.AsNoTracking().AnyAsync(p => p.OwnerAdultId == ownerAdultId
                && p.Name == name && p.Id != exceptProfileId, ct);

    /// <summary>Fach und Reihe müssen existieren – ein Profil, das ins Leere zeigt, findet nie ein Kind.</summary>
    private async Task<ObjectResult?> ReferenceProblemAsync(int? subjectId, int? seriesId, CancellationToken ct)
    {
        if (subjectId is int sid && !await db.Subjects.AnyAsync(s => s.Id == sid, ct))
            return this.ProblemWithCode(ApiErrors.InvalidReference, "SubjectId does not reference an existing subject.");
        if (seriesId is int serId && !await db.TextbookSeries.AnyAsync(s => s.Id == serId, ct))
            return this.ProblemWithCode(ApiErrors.InvalidReference, "SeriesId does not reference an existing textbook series.");
        return null;
    }

    private Task<CreatorProfile> LoadWithSeriesAsync(int profileId, CancellationToken ct) =>
        db.CreatorProfiles.AsNoTracking().Include(p => p.Series).FirstAsync(p => p.Id == profileId, ct);

    private static string? Trimmed(string? value) => value?.Trim() is { Length: > 0 } v ? v : null;

    /// <summary>Trimmt, verwirft Leereinträge und dedupliziert die bevorzugten Übungstypen.</summary>
    private static List<string> Clean(List<string>? values) =>
        [.. (values ?? []).Select(v => v.Trim()).Where(v => v.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)];
}
