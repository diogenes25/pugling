using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Creator;

/// <summary>
/// Which vocabulary entries still lack images (B-485) – the work list of the image generator. Images are drawn per
/// vocabulary entry and interest tag, never per child: the child only decides <i>which</i> tags are wanted, and the
/// answer carries no child data. Which image a child then sees stays the <see cref="MediaSelector"/>'s decision.
/// <para>
/// Two deliberate simplifications against the selector: only vocabulary links count (an exercise item with images of
/// its own shows only those, so images ordered for its word stay unseen in that exercise), and an existing image counts
/// as present without checking the child's rating or dislikes. Both only matter for hand-maintained images; everything
/// the generator draws is rated for everyone and linked at the vocabulary entry.
/// </para>
/// </summary>
public class MediaDemandService(PuglingDbContext db, TimeProvider time)
{
    /// <summary>
    /// How many interests of a child get images of their own (decision 3 of B-485): the two strongest keep the volume
    /// and the cost bounded, the neutral image covers every other child.
    /// </summary>
    public const int TagsPerChild = 2;

    /// <summary>The weight from which an interest counts as clear enough to draw for (scale −3…+3).</summary>
    public const int MinWeight = 2;

    /// <summary>
    /// The demand of the vocabulary in the child's playable plans: neutral image plus the child's strongest
    /// theme interests. Style interests are left out – all generated images share one fixed style (decision 5).
    /// </summary>
    public async Task<IReadOnlyList<MediaDemandEntry>> ForChildAsync(int childId, int take, CancellationToken ct)
    {
        var tags = await db.ChildInterests.AsNoTracking()
            .Where(i => i.ChildId == childId && i.Weight >= MinWeight && i.InterestTag!.Facet != InterestFacet.Style)
            // Ties broken by tag id: the same data state must name the same two tags on every run.
            .OrderByDescending(i => i.Weight).ThenBy(i => i.InterestTagId)
            .Take(TagsPerChild)
            .Select(i => i.InterestTag!)
            .ToListAsync(ct);

        // The injected clock, as in PositionPracticeController: a faked "today" must mean the same day in both places.
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        // The same rule as PositionPlayService.PlanPlayableForChild: only what the child can play today needs images now.
        var exerciseIds = db.PlanPositions
            .Where(p => p.StudyPlan!.ChildId == childId && p.StudyPlan.Active
                && p.StudyPlan.StartDate <= today && today <= p.StudyPlan.EndDate)
            .Select(p => p.ExerciseId);
        var vocabularyIds = await db.ExerciseItems.AsNoTracking()
            .Where(i => exerciseIds.Contains(i.ExerciseId))
            .Select(i => i.VocabularyId)
            .Distinct()
            .ToListAsync(ct);

        return await DemandAsync(vocabularyIds, tags, take, ct);
    }

    /// <summary>
    /// The demand of a whole unit, independent of any child: neutral image, plus <paramref name="tag"/> when given –
    /// for producing a unit ahead of the children who will learn it.
    /// </summary>
    public async Task<IReadOnlyList<MediaDemandEntry>> ForUnitAsync(int seriesUnitId, InterestTag? tag, int take,
        CancellationToken ct)
    {
        var vocabularyIds = await db.ExerciseItems.AsNoTracking()
            .Where(i => i.Exercise!.SeriesUnitId == seriesUnitId)
            .Select(i => i.VocabularyId)
            .Distinct()
            .ToListAsync(ct);

        return await DemandAsync(vocabularyIds, tag is null ? [] : [tag], take, ct);
    }

    private async Task<IReadOnlyList<MediaDemandEntry>> DemandAsync(List<int> vocabularyIds, List<InterestTag> tags,
        int take, CancellationToken ct)
    {
        if (vocabularyIds.Count == 0) return [];

        // Loaded and matched in memory instead of grouped in SQL: SQLite cannot translate the nested aggregates, and
        // the vocabulary of one plan or unit is small.
        var links = await db.MediaLinks.AsNoTracking()
            .Where(l => l.VocabularyId != null && vocabularyIds.Contains(l.VocabularyId.Value))
            .Select(l => new { VocabularyId = l.VocabularyId!.Value, TagIds = l.MediaAsset!.TagLinks.Select(t => t.InterestTagId).ToList() })
            .ToListAsync(ct);
        var linksByVocabulary = links.ToLookup(l => l.VocabularyId);

        var vocabulary = await db.Vocabularies.AsNoTracking()
            .Where(v => vocabularyIds.Contains(v.Id))
            .OrderBy(v => v.Word).ThenBy(v => v.Id)
            .Select(v => new { v.Id, v.Word, v.Translation, v.SourceLanguage, v.TargetLanguage, v.PartOfSpeech })
            .ToListAsync(ct);

        var result = new List<MediaDemandEntry>();
        foreach (var v in vocabulary)
        {
            var own = linksByVocabulary[v.Id].ToList();
            // Neutral = an image without any tag: the selector scores it 0 and offers it to every child that has no
            // better match, which is exactly the base image of decision 3.
            var needsNeutral = !own.Any(l => l.TagIds.Count == 0);
            var wanted = tags
                .Where(t => !own.Any(l => l.TagIds.Contains(t.Id)))
                .Select(t => new MediaDemandTag(t.Slug, t.Label, t.ImagePrompt))
                .ToList();
            if (!needsNeutral && wanted.Count == 0) continue;

            result.Add(new MediaDemandEntry(v.Id, v.Word, v.Translation, v.SourceLanguage, v.TargetLanguage,
                v.PartOfSpeech, needsNeutral, wanted));
            if (result.Count == take) break;
        }
        return result;
    }
}
