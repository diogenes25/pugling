using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// The one rule for "which exercises belong to a class test" (B-751): the directly assigned ones plus every exercise
/// carrying a tag linked to the test, without duplicates. The supervisor's practice/repeat views and the child's readiness
/// read the bundle here, and every further reader must too - a copy of the rule drifts from it. The reverse question
/// ("which class tests use this exercise", <c>ExerciseCatalogController</c>'s usage view) still spells the rule out itself.
/// </summary>
public static class ClassTestBundle
{
    /// <summary>The distinct exercise ids relevant to the given class tests (direct assignments and linked tags).</summary>
    public static async Task<List<int>> ExerciseIdsAsync(PuglingDbContext db, IReadOnlyCollection<int> klassenarbeitIds,
        CancellationToken ct)
    {
        var directIds = await db.KlassenarbeitExercises.AsNoTracking()
            .Where(x => klassenarbeitIds.Contains(x.KlassenarbeitId))
            .Select(x => x.ExerciseId).ToListAsync(ct);
        var tagged = await db.ExerciseTags.AsNoTracking()
            .Where(et => db.KlassenarbeitTags.Any(kt => klassenarbeitIds.Contains(kt.KlassenarbeitId) && kt.TagId == et.TagId))
            .Select(et => et.ExerciseId).ToListAsync(ct);
        return directIds.Concat(tagged).Distinct().Order().ToList();
    }
}
