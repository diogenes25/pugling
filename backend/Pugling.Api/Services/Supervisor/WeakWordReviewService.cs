using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Supervisor;

/// <summary>
/// Turns a child's weak words into a daily review duty with one call (B-557): a new vocabulary exercise that references the
/// existing store entries, plus a position for it in the given plan.
/// <para>
/// The exercise is built here and not through the creator path on purpose - the father bundles material the child already
/// had, he writes none. That is why the request carries word ids only (never a config): the language pair comes from the
/// words, the unit from where the child learned them, and every word must have learning state of this child. Without those
/// three rules the endpoint were a side door to build arbitrary exercises past the creator's checks.
/// </para>
/// </summary>
public class WeakWordReviewService(PuglingDbContext db, ExerciseItemService items, TimeProvider time)
{
    /// <summary>Most words one review takes - one page of the father's word list.</summary>
    public const int MaxWords = 25;

    /// <summary>Outcome: the new position, or the error to answer with.</summary>
    public record Outcome(PlanPosition? Position, ApiError? Error = null, string? Detail = null);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Creates the review exercise and its daily position in <paramref name="plan"/> for the plan's child, or explains why
    /// not. Every check runs before the first write; the writes share one transaction, so a refusal leaves no empty
    /// exercise in the catalog.
    /// </summary>
    public async Task<Outcome> CreateAsync(StudyPlan plan, IReadOnlyList<int> vocabularyIds, int? adultId, CancellationToken ct)
    {
        // Duplicates in the request are one word: the same id twice is a slip, not a second card.
        var ids = vocabularyIds.Distinct().ToList();
        if (ids.Count is 0 or > MaxWords)
            return Fail(ApiErrors.ValidationError, $"A review takes 1 to {MaxWords} words.");

        var progress = await db.ItemProgress.AsNoTracking()
            .Where(p => p.ChildId == plan.ChildId && ids.Contains(p.VocabularyId))
            .Select(p => new { p.VocabularyId, p.ExerciseId })
            .ToListAsync(ct);
        // Only words the child has already met: a duty on a word never seen would be unfair, and this is no way to
        // smuggle new material into the plan.
        var unknown = ids.Where(id => progress.All(p => p.VocabularyId != id)).ToList();
        if (unknown.Count > 0)
            return Fail(ApiErrors.InvalidReference,
                $"The child has no learning state for vocabulary {string.Join(", ", unknown)}; a review takes only words the child has met.");

        var languages = await db.Vocabularies.AsNoTracking()
            .Where(v => ids.Contains(v.Id))
            .Select(v => new { v.SourceLanguage, v.TargetLanguage })
            .Distinct()
            .ToListAsync(ct);
        if (languages.Count != 1)
            return Fail(ApiErrors.ValidationError, "All words of a review must share one language pair.");

        // The unit the child learned most of these words in; on a tie the smallest id, so the same request always files the
        // exercise in the same place. Progress of a since-deleted exercise does not vote.
        var exerciseIds = progress.Select(p => p.ExerciseId).Distinct().ToList();
        var unitOf = await db.Exercises.AsNoTracking()
            .Where(e => exerciseIds.Contains(e.Id))
            .Select(e => new { e.Id, e.SeriesUnitId })
            .ToDictionaryAsync(e => e.Id, e => e.SeriesUnitId, ct);
        // One vote per word: the unit of its earliest origin exercise. A word learned in several exercises (a review is one
        // itself) would otherwise vote by whatever row the database returns first.
        var unitVotes = progress
            .Where(p => unitOf.ContainsKey(p.ExerciseId))
            .GroupBy(p => p.VocabularyId)
            .Select(g => unitOf[g.Min(p => p.ExerciseId)])
            .GroupBy(u => u)
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
            .Select(g => (int?)g.Key)
            .FirstOrDefault();
        if (unitVotes is not { } unitId)
            return Fail(ApiErrors.ValidationError, "The exercises these words were learned in no longer exist.");

        var unitSubject = await db.SeriesUnits.AsNoTracking()
            .Where(u => u.Id == unitId)
            .Select(u => u.Series!.SubjectId)
            .FirstAsync(ct);
        // The creator path forbids an exercise in a series without a subject (B-106); this side path must not create one.
        if (unitSubject is null)
            return Fail(ApiErrors.ValidationError, "These words come from a textbook series without a subject.");
        if (plan.SubjectId is { } planSubject && unitSubject != planSubject)
            return Fail(ApiErrors.ReviewSubjectMismatch, "These words belong to another subject than this plan.");

        // The very same set of words as a position in this plan already is a duty twice over - a review or the exercise the
        // words came from; an overlapping set is a new choice of the father and allowed. Checked before the writes, not under
        // a lock: two simultaneous identical requests can both pass, the father's double click is caught by the client.
        var wanted = ids.ToHashSet();
        var candidates = await db.PlanPositions.AsNoTracking()
            .Where(p => p.StudyPlanId == plan.Id && p.Exercise!.Type == ExerciseTypeKeys.Vocabulary)
            .Select(p => new { Position = p, Words = db.ExerciseItems.Where(i => i.ExerciseId == p.ExerciseId).Select(i => i.VocabularyId).ToList() })
            .ToListAsync(ct);
        if (candidates.FirstOrDefault(c => c.Words.ToHashSet().SetEquals(wanted)) is { } same)
            return new Outcome(same.Position, ApiErrors.ReviewAlreadyInPlan,
                $"This plan already practises exactly these words (position {same.Position.Id}).");

        var childName = await db.Children.AsNoTracking().Where(c => c.Id == plan.ChildId).Select(c => c.Name).FirstAsync(ct);
        var now = time.GetUtcNow().UtcDateTime;
        var config = new VocabularyConfig { SourceLang = languages[0].SourceLanguage, TargetLang = languages[0].TargetLanguage };

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var exercise = new Exercise
        {
            SeriesUnitId = unitId,
            Type = ExerciseTypeKeys.Vocabulary,
            Title = $"Wiederholung: schwache Wörter ({childName}, {now:dd.MM.yyyy})",
            RewardPoints = GoalPointsPolicy.Fallback,
            ConfigJson = JsonSerializer.Serialize(config, JsonOptions),
            DefaultUseLeitner = true,
            // Cut to one child: readable in the catalog like everything, but no other creator should put it into foreign
            // plans.
            ExecutePublic = false,
            AuthorAdultId = adultId,
        };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync(ct);
        if (adultId is { } owner)
            db.ExerciseGrants.Add(new ExerciseGrant
            {
                ExerciseId = exercise.Id,
                CreatorId = owner,
                Permission = GrantPermission.Owner,
                GrantedByAdultId = owner,
            });
        await items.ReconcileAsync(exercise.Id, [.. vocabularyIds.Distinct().Select(id => new DesiredItem(id, null))], ct);

        var order = ((await db.PlanPositions.Where(p => p.StudyPlanId == plan.Id).MaxAsync(p => (int?)p.Order, ct)) ?? -1) + 1;
        var position = new PlanPosition
        {
            CreatedAt = now,
            StudyPlanId = plan.Id,
            ExerciseId = exercise.Id,
            Order = order,
            Scope = ItemScope.All,
            // The point of it: the gap becomes a duty. The penalty stays opt-in per position, as everywhere.
            Cadence = GoalCadence.Daily,
            OrderStrategy = PracticeOrder.WeakestFirst,
            UseLeitner = true,
            MaxBox = 5,
            PointsGoalMet = GoalPointsPolicy.Effective(exercise.RewardPoints),
            PenaltyCoins = 0,
            NewContentPoints = 10,
            ComboThreshold = 5,
            ComboBonusPoints = 5,
        };
        db.PlanPositions.Add(position);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        position.Exercise = exercise;
        return new Outcome(position);
    }

    private static Outcome Fail(ApiError error, string detail) => new(null, error, detail);
}
