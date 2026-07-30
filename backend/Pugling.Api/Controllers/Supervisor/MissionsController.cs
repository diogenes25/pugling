using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Supervisor;

/// <summary>
/// Manage missions of a child (father only, own children only): time-bound goals with a reward.
/// Ownership is secured by the <see cref="ChildOwnershipFilter"/>; progress/status is read for the child via
/// <c>GET api/v1/student/me/missions</c>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/{childId:int}/missions")]
[Tags("Supervisor – Missions")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class MissionsController(PuglingDbContext db) : ControllerBase
{
    static MissionDto Map(Mission m) => new(m.Id, m.Title, m.Metric, m.Target, m.Period, m.RewardPoints, m.Active);

    /// <summary>All missions of the child (definitions for management).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MissionDto>>> List(int childId, CancellationToken ct = default) =>
        await db.Missions.AsNoTracking().Where(m => m.ChildId == childId)
            .OrderBy(m => m.Period).ThenBy(m => m.Id)
            .Select(m => Map(m)).ToListAsync(ct);

    /// <summary>Creates a mission for the child.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MissionDto>> Create(int childId, CreateMissionDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) return this.ProblemWithCode(ApiErrors.ValidationError, "Title is required.");
        if (dto.Target <= 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Target must be positive.");

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
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { childId }, Map(mission));
    }

    /// <summary>Changes a mission (partial).</summary>
    [HttpPatch("{missionId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MissionDto>> Update(int childId, int missionId, UpdateMissionDto dto, CancellationToken ct = default)
    {
        var mission = await db.Missions.FirstOrDefaultAsync(m => m.Id == missionId && m.ChildId == childId, ct);
        if (mission is null) return NotFound();

        if (dto.Title is not null) mission.Title = dto.Title.Trim();
        if (dto.Target is > 0) mission.Target = dto.Target.Value;
        if (dto.RewardPoints is not null) mission.RewardPoints = Math.Max(0, dto.RewardPoints.Value);
        if (dto.Active is not null) mission.Active = dto.Active.Value;
        await db.SaveChangesAsync(ct);
        return Map(mission);
    }

    /// <summary>Deletes a mission (together with the award log via cascade).</summary>
    [HttpDelete("{missionId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int childId, int missionId, CancellationToken ct = default)
    {
        var mission = await db.Missions.FirstOrDefaultAsync(m => m.Id == missionId && m.ChildId == childId, ct);
        if (mission is null) return NotFound();
        db.Missions.Remove(mission);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

/// <summary>
/// Manage awards (badges) of a child (father only, own children only): permanent
/// milestones. Ownership is secured by the <see cref="ChildOwnershipFilter"/>; the status is read for the child
/// via <c>GET api/v1/student/me/achievements</c>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Supervisor + "/children/{childId:int}/achievements")]
[Tags("Supervisor – Achievements")]
[Produces("application/json")]
[Authorize(Roles = Roles.Supervisor)]
[ServiceFilter(typeof(ChildOwnershipFilter))]
public class AchievementsController(PuglingDbContext db) : ControllerBase
{
    static AchievementDto Map(Achievement a) =>
        new(a.Id, a.Title, a.Icon, a.Metric, a.Threshold, a.RewardPoints, a.Active);

    /// <summary>All awards of the child (definitions for management).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AchievementDto>>> List(int childId, CancellationToken ct = default) =>
        await db.Achievements.AsNoTracking().Where(a => a.ChildId == childId)
            .OrderBy(a => a.Metric).ThenBy(a => a.Threshold)
            .Select(a => Map(a)).ToListAsync(ct);

    /// <summary>Creates an award for the child.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AchievementDto>> Create(int childId, CreateAchievementDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) return this.ProblemWithCode(ApiErrors.ValidationError, "Title is required.");
        if (dto.Threshold <= 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Threshold must be positive.");

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
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { childId }, Map(achievement));
    }

    /// <summary>Changes an award (partial).</summary>
    [HttpPatch("{achievementId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AchievementDto>> Update(int childId, int achievementId, UpdateAchievementDto dto, CancellationToken ct = default)
    {
        var achievement = await db.Achievements.FirstOrDefaultAsync(a => a.Id == achievementId && a.ChildId == childId, ct);
        if (achievement is null) return NotFound();

        if (dto.Title is not null) achievement.Title = dto.Title.Trim();
        if (dto.Icon is not null) achievement.Icon = dto.Icon;
        if (dto.Threshold is > 0) achievement.Threshold = dto.Threshold.Value;
        if (dto.RewardPoints is not null) achievement.RewardPoints = Math.Max(0, dto.RewardPoints.Value);
        if (dto.Active is not null) achievement.Active = dto.Active.Value;
        await db.SaveChangesAsync(ct);
        return Map(achievement);
    }

    /// <summary>Deletes an award (together with the award log via cascade).</summary>
    [HttpDelete("{achievementId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int childId, int achievementId, CancellationToken ct = default)
    {
        var achievement = await db.Achievements.FirstOrDefaultAsync(a => a.Id == achievementId && a.ChildId == childId, ct);
        if (achievement is null) return NotFound();
        db.Achievements.Remove(achievement);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
