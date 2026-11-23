using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// A study plan position written as a raw row, for migration tests whose starting point predates a column of today's
/// model (B-848 added <c>CurrentBuildLevel</c>). Fills the NOT NULL columns with plain values.
/// </summary>
internal static class LegacyPositionRow
{
    /// <summary>Inserts a free Leitner position on the exercise and returns its id.</summary>
    public static async Task<int> InsertAsync(PuglingDbContext db, int planId, int exerciseId)
    {
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO PlanPositions (StudyPlanId, ExerciseId, "Order", NewItemsPerDay, Scope, OrderStrategy, Cadence,
                RequireTypedTest, PointsGoalMet, PenaltyCoins, NewContentPoints, ComboThreshold, ComboBonusPoints,
                SpeedThresholdSeconds, SpeedBonusPoints, UseLeitner, ForgivingDemotion, Remediation, CompletionBonus,
                DutySince, MaxBox, CreatedAt)
            VALUES ({planId}, {exerciseId}, 0, 0, 'All', 'WeakestFirst', 'None', 0, 20, 0, 10, 5, 5, 0, 0, 1, 0, 0, 0,
                '2026-11-01', 5, '2026-11-01 08:00:00')
            """);
        return await db.Database.SqlQuery<int>($"SELECT MAX(Id) AS Value FROM PlanPositions").SingleAsync();
    }
}
