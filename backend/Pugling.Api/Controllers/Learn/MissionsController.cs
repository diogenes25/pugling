using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Learn;

/// <summary>
/// Missionen eines Kindes verwalten (nur Vater, nur eigene Kinder): zeitgebundene Ziele mit Belohnung.
/// Der Fortschritt/Status wird beim Kind über <c>GET api/me/missions</c> gelesen.
/// </summary>
[ApiController]
[Route("api/fathers/{fatherId:int}/children/{childId:int}/missions")]
[Tags("Admin – Missions")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
public class MissionsController(PuglingDbContext db, AuthAccess access) : ControllerBase
{
    public record MissionDto(int Id, string Title, ProgressMetric Metric, int Target, MissionPeriod Period,
        int RewardPoints, bool Active);

    static MissionDto Map(Mission m) => new(m.Id, m.Title, m.Metric, m.Target, m.Period, m.RewardPoints, m.Active);

    /// <summary>Prüft, dass Route-Vater == Token-Vater und das Kind ihm gehört; sonst 404 (kein Enumerieren).</summary>
    async Task<bool> OwnsAsync(int fatherId, int childId) =>
        User.FatherId() == fatherId && await access.FatherOwnsChildAsync(User, childId);

    /// <summary>Alle Missionen des Kindes (Definitionen zur Verwaltung).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MissionDto>>> List(int fatherId, int childId)
    {
        if (!await OwnsAsync(fatherId, childId)) return NotFound();
        return await db.Missions.AsNoTracking().Where(m => m.ChildId == childId)
            .OrderBy(m => m.Period).ThenBy(m => m.Id)
            .Select(m => Map(m)).ToListAsync();
    }

    public record CreateMissionDto(string Title, ProgressMetric Metric, int Target, MissionPeriod Period, int RewardPoints);

    /// <summary>Legt eine Mission für das Kind an.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MissionDto>> Create(int fatherId, int childId, CreateMissionDto dto)
    {
        if (!await OwnsAsync(fatherId, childId)) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("Title ist erforderlich.");
        if (dto.Target <= 0) return BadRequest("Target muss positiv sein.");

        var mission = new Mission
        {
            ChildId = childId,
            Title = dto.Title.Trim(),
            Metric = dto.Metric,
            Target = dto.Target,
            Period = dto.Period,
            RewardPoints = Math.Max(0, dto.RewardPoints),
        };
        db.Missions.Add(mission);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(List), new { fatherId, childId }, Map(mission));
    }

    public record UpdateMissionDto(string? Title, int? Target, int? RewardPoints, bool? Active);

    /// <summary>Ändert eine Mission (partiell).</summary>
    [HttpPatch("{missionId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MissionDto>> Update(int fatherId, int childId, int missionId, UpdateMissionDto dto)
    {
        if (!await OwnsAsync(fatherId, childId)) return NotFound();
        var mission = await db.Missions.FirstOrDefaultAsync(m => m.Id == missionId && m.ChildId == childId);
        if (mission is null) return NotFound();

        if (dto.Title is not null) mission.Title = dto.Title.Trim();
        if (dto.Target is > 0) mission.Target = dto.Target.Value;
        if (dto.RewardPoints is not null) mission.RewardPoints = Math.Max(0, dto.RewardPoints.Value);
        if (dto.Active is not null) mission.Active = dto.Active.Value;
        await db.SaveChangesAsync();
        return Map(mission);
    }

    /// <summary>Löscht eine Mission (samt Vergabe-Log per Cascade).</summary>
    [HttpDelete("{missionId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int fatherId, int childId, int missionId)
    {
        if (!await OwnsAsync(fatherId, childId)) return NotFound();
        var mission = await db.Missions.FirstOrDefaultAsync(m => m.Id == missionId && m.ChildId == childId);
        if (mission is null) return NotFound();
        db.Missions.Remove(mission);
        await db.SaveChangesAsync();
        return NoContent();
    }
}

/// <summary>
/// Auszeichnungen (Badges) eines Kindes verwalten (nur Vater, nur eigene Kinder): permanente
/// Meilensteine. Der Status wird beim Kind über <c>GET api/me/achievements</c> gelesen.
/// </summary>
[ApiController]
[Route("api/fathers/{fatherId:int}/children/{childId:int}/achievements")]
[Tags("Admin – Achievements")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
public class AchievementsController(PuglingDbContext db, AuthAccess access) : ControllerBase
{
    public record AchievementDto(int Id, string Title, string? Icon, ProgressMetric Metric, int Threshold,
        int RewardPoints, bool Active);

    static AchievementDto Map(Achievement a) =>
        new(a.Id, a.Title, a.Icon, a.Metric, a.Threshold, a.RewardPoints, a.Active);

    async Task<bool> OwnsAsync(int fatherId, int childId) =>
        User.FatherId() == fatherId && await access.FatherOwnsChildAsync(User, childId);

    /// <summary>Alle Auszeichnungen des Kindes (Definitionen zur Verwaltung).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AchievementDto>>> List(int fatherId, int childId)
    {
        if (!await OwnsAsync(fatherId, childId)) return NotFound();
        return await db.Achievements.AsNoTracking().Where(a => a.ChildId == childId)
            .OrderBy(a => a.Metric).ThenBy(a => a.Threshold)
            .Select(a => Map(a)).ToListAsync();
    }

    public record CreateAchievementDto(string Title, string? Icon, ProgressMetric Metric, int Threshold, int RewardPoints);

    /// <summary>Legt eine Auszeichnung für das Kind an.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AchievementDto>> Create(int fatherId, int childId, CreateAchievementDto dto)
    {
        if (!await OwnsAsync(fatherId, childId)) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("Title ist erforderlich.");
        if (dto.Threshold <= 0) return BadRequest("Threshold muss positiv sein.");

        var achievement = new Achievement
        {
            ChildId = childId,
            Title = dto.Title.Trim(),
            Icon = dto.Icon,
            Metric = dto.Metric,
            Threshold = dto.Threshold,
            RewardPoints = Math.Max(0, dto.RewardPoints),
        };
        db.Achievements.Add(achievement);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(List), new { fatherId, childId }, Map(achievement));
    }

    public record UpdateAchievementDto(string? Title, string? Icon, int? Threshold, int? RewardPoints, bool? Active);

    /// <summary>Ändert eine Auszeichnung (partiell).</summary>
    [HttpPatch("{achievementId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AchievementDto>> Update(int fatherId, int childId, int achievementId, UpdateAchievementDto dto)
    {
        if (!await OwnsAsync(fatherId, childId)) return NotFound();
        var achievement = await db.Achievements.FirstOrDefaultAsync(a => a.Id == achievementId && a.ChildId == childId);
        if (achievement is null) return NotFound();

        if (dto.Title is not null) achievement.Title = dto.Title.Trim();
        if (dto.Icon is not null) achievement.Icon = dto.Icon;
        if (dto.Threshold is > 0) achievement.Threshold = dto.Threshold.Value;
        if (dto.RewardPoints is not null) achievement.RewardPoints = Math.Max(0, dto.RewardPoints.Value);
        if (dto.Active is not null) achievement.Active = dto.Active.Value;
        await db.SaveChangesAsync();
        return Map(achievement);
    }

    /// <summary>Löscht eine Auszeichnung (samt Vergabe-Log per Cascade).</summary>
    [HttpDelete("{achievementId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int fatherId, int childId, int achievementId)
    {
        if (!await OwnsAsync(fatherId, childId)) return NotFound();
        var achievement = await db.Achievements.FirstOrDefaultAsync(a => a.Id == achievementId && a.ChildId == childId);
        if (achievement is null) return NotFound();
        db.Achievements.Remove(achievement);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
