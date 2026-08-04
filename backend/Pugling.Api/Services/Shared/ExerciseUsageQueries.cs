using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// How many usages <b>prevent an exercise from being deleted</b> – split into the ones the caller
/// can see, and the ones that lie outside their supervision.
/// </summary>
/// <param name="OwnPlans">Study plan positions for children the caller supervises.</param>
/// <param name="HiddenPlans">Study plan positions for children they do <b>not</b> supervise – invisible to them.</param>
/// <param name="OwnClassTests">Directly assigned class tests of their own children.</param>
/// <param name="HiddenClassTests">Directly assigned class tests of children supervised by someone else.</param>
/// <param name="OwnGoals">Milestones of big goals (<c>KeyResult</c>) of their own children pointing at the exercise.</param>
/// <param name="HiddenGoals">The same milestones for children supervised by someone else.</param>
/// <param name="HiddenLearners">
/// How many <b>different children</b> stand behind the hidden usages. A separate number, because
/// it answers a different question than <see cref="Hidden"/>: the usage count says "how many
/// places would someone need to clean up", this one says "how many children are learning my material". For
/// a creator without children of their own, the second is the only one that matters to them – and three
/// positions in the plans of the same child are not three users.
/// </param>
public readonly record struct BlockingUsage(
    int OwnPlans, int HiddenPlans, int OwnClassTests, int HiddenClassTests, int HiddenLearners,
    int OwnGoals = 0, int HiddenGoals = 0)
{
    /// <summary>Usages the caller finds in the usage display.</summary>
    public int Own => OwnPlans + OwnClassTests + OwnGoals;

    /// <summary>Usages that remain hidden from them – the number without which a 409 is a mystery.</summary>
    public int Hidden => HiddenPlans + HiddenClassTests + HiddenGoals;

    /// <summary>Does anything block the deletion at all?</summary>
    public bool Any => Own + Hidden > 0;
}

/// <summary>
/// The <b>one</b> answer to "where is this exercise used".
///
/// It lives here because this exact question used to be answered differently in two places: the
/// usage display filtered on the caller's own children, the deletion check looked globally. If an
/// exercise was embedded in the plan of a child supervised by someone else, the display reported
/// "nowhere" – and deletion still failed with <c>409</c>, without the author being able to find the
/// reason (remark 14).
///
/// <para>
/// Deliberately only the <b>FK-relevant</b> usages: study plan positions and <i>directly</i> assigned
/// class tests. A class test that only collects the exercise via a shared tag does not reference it
/// and therefore does not block deletion either – it belongs in the display, but not in this count.
/// Anyone who mixes the two builds the next contradiction of the same kind.
/// </para>
/// </summary>
public static class ExerciseUsageQueries
{
    /// <summary>
    /// Counts what blocks deletion, split into "visible to <paramref name="fid"/>" and "hidden".
    /// Without <paramref name="fid"/> (creator without an adult profile) everything is hidden – they supervise no child.
    /// </summary>
    public static async Task<BlockingUsage> CountBlockingAsync(
        PuglingDbContext db, int exerciseId, int? fid, CancellationToken ct = default)
    {
        // Four plain counts instead of one GroupBy projection: SQLite would group over a subquery expression
        // here, and the gain would be one round trip on a path that runs once per delete attempt.
        // Readability beats that.
        var planTotal = await db.PlanPositions.AsNoTracking()
            .CountAsync(p => p.ExerciseId == exerciseId, ct);
        var planMine = await db.PlanPositions.AsNoTracking()
            .CountAsync(p => p.ExerciseId == exerciseId
                && p.StudyPlan!.Child!.SupervisorLinks.Any(l => l.SupervisorId == fid), ct);
        var testTotal = await db.KlassenarbeitExercises.AsNoTracking()
            .CountAsync(x => x.ExerciseId == exerciseId, ct);
        var testMine = await db.KlassenarbeitExercises.AsNoTracking()
            .CountAsync(x => x.ExerciseId == exerciseId
                && x.Klassenarbeit!.Child!.SupervisorLinks.Any(l => l.SupervisorId == fid), ct);
        // Since the scope foreign key (Restrict), a goal milestone blocks the delete too. Without this count
        // the 409 would be a 500 - exactly the kind of gap this class exists for.
        var goalTotal = await db.KeyResults.AsNoTracking()
            .CountAsync(k => k.ExerciseId == exerciseId, ct);
        var goalMine = await db.KeyResults.AsNoTracking()
            .CountAsync(k => k.ExerciseId == exerciseId
                && k.Objective!.Child!.SupervisorLinks.Any(l => l.SupervisorId == fid), ct);

        // How many distinct CHILDREN are behind the hidden usages - not how many places. Three positions in the
        // plans of the same child are one user, not three; and for a creator without children of their own it
        // is the only number that says anything.
        var hiddenLearners = await db.PlanPositions.AsNoTracking()
            .Where(p => p.ExerciseId == exerciseId
                && !p.StudyPlan!.Child!.SupervisorLinks.Any(l => l.SupervisorId == fid))
            .Select(p => p.StudyPlan!.ChildId)
            .Union(db.KlassenarbeitExercises.AsNoTracking()
                .Where(x => x.ExerciseId == exerciseId
                    && !x.Klassenarbeit!.Child!.SupervisorLinks.Any(l => l.SupervisorId == fid))
                .Select(x => x.Klassenarbeit!.ChildId))
            .Union(db.KeyResults.AsNoTracking()
                .Where(k => k.ExerciseId == exerciseId
                    && !k.Objective!.Child!.SupervisorLinks.Any(l => l.SupervisorId == fid))
                .Select(k => k.Objective!.ChildId))
            .Distinct()
            .CountAsync(ct);

        return new BlockingUsage(
            OwnPlans: planMine, HiddenPlans: planTotal - planMine,
            OwnClassTests: testMine, HiddenClassTests: testTotal - testMine,
            HiddenLearners: hiddenLearners,
            OwnGoals: goalMine, HiddenGoals: goalTotal - goalMine);
    }

    /// <summary>
    /// Does <b>any</b> exercise from <paramref name="scope"/> block deletion? For the level above the
    /// exercise: a series unit cascades to its exercises, and <c>PlanPosition→Exercise</c> is
    /// <c>Restrict</c> – without this pre-check, deletion dies as an FK violation in a bare 500,
    /// instead of saying what is in the way.
    /// <para>
    /// It lives here and not in the callers, because the answer to "which tables block
    /// the deletion of an exercise" needs <b>one</b> place. Previously the line was written out three
    /// times verbatim; a fourth referencing table would have had to be found in all three places – and
    /// the one forgotten would again be a 500. The <i>message texts</i> stay with the callers: they name
    /// the level ("in this subject" / "in this unit") and are not the same statement.
    /// </para>
    /// </summary>
    public static async Task<bool> AnyBlockingAsync(
        PuglingDbContext db, IQueryable<Exercise> scope, IQueryable<SeriesUnit> seriesUnitScope, CancellationToken ct)
    {
        var ids = scope.Select(x => x.Id);
        var seriesUnitIds = seriesUnitScope.Select(u => u.Id);
        return await db.PlanPositions.AsNoTracking().AnyAsync(p => ids.Contains(p.ExerciseId), ct)
            || await db.KlassenarbeitExercises.AsNoTracking().AnyAsync(x => ids.Contains(x.ExerciseId), ct)
            // Goal milestones point at the exercise OR directly at the series unit - both FKs are Restrict, and a
            // unit goal hangs on no exercise. Checking only the exercise scope would let deleting a unit
            // with a unit goal run into the FK violation.
            || await db.KeyResults.AsNoTracking().AnyAsync(k =>
                (k.ExerciseId != null && ids.Contains(k.ExerciseId.Value))
                || (k.SeriesUnitId != null && seriesUnitIds.Contains(k.SeriesUnitId.Value)), ct);
    }

    /// <summary>
    /// The sentence that explains on <c>409</c> <b>why</b> deletion is not possible – and that names the
    /// hidden usages as a <i>number</i>. Deliberately without the names of plans or children: those
    /// belong to a different supervisor, and the author only needs to know that they exist.
    /// </summary>
    public static string Explain(BlockingUsage usage)
    {
        // Only what actually occurs, and with the right plural: a message enumerating "0 class test(s)" reads
        // like a machine and distracts from the actual hint.
        var own = new List<string>();
        if (usage.OwnPlans > 0) own.Add(Plural(usage.OwnPlans, "study plan"));
        if (usage.OwnClassTests > 0) own.Add(Plural(usage.OwnClassTests, "class test"));
        if (usage.OwnGoals > 0) own.Add(Plural(usage.OwnGoals, "objective milestone"));

        var parts = new List<string>();
        if (own.Count > 0) parts.Add($"{string.Join(" and ", own)} of yours");
        if (usage.Hidden > 0)
            parts.Add($"{Plural(usage.Hidden, "usage")} outside your care (children you do not supervise, "
                + "so they are not listed under usage)");

        return $"Cannot delete: the exercise is still used – {string.Join("; ", parts)}. Remove it there first.";
    }

    private static string Plural(int count, string noun) => $"{count} {noun}{(count == 1 ? "" : "s")}";
}
