using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Decides why a typed answer was wrong (B-732) - the one rule every writer of an answer row calls, at the moment it grades
/// the answer, so the kind is stored and never recomputed from a solution that may have changed since. The checks run in a
/// fixed order: accent, typo (both <see cref="AnswerGrader.SpellingSlip"/>), synonym, wrong word. Grading stays binary:
/// the kind explains a miss, it never turns one into a hit.
/// </summary>
public class AnswerErrorClassifier(PuglingDbContext db, AnswerGrader grader)
{
    // Candidates fetched for the synonym check. A typed word is contained in only a handful of store words; the cap keeps a
    // one-letter answer from loading the whole language.
    private const int MaxSynonymCandidates = 50;

    /// <summary>
    /// The kind of a graded answer, or <c>null</c> when it carries none: a right answer, and a pick from the options the
    /// card offered at <paramref name="stage"/> (<see cref="IExerciseType.Choices"/> - the same test that decides whether the
    /// card shows options, so a choice stage that fell back to letter boxes is typed). A number or a solution set
    /// (<see cref="ContentItem.IsNumber"/>) carries none either: the kinds name what went wrong with a word, and a wrong
    /// number is no accent, no typo and no other word. The caller calls this only for an answer the server checked as text;
    /// a self-assessment has no text to classify. An empty answer is <see cref="AnswerErrorKind.WrongWord"/>: the child did
    /// not know the word.
    /// </summary>
    /// <param name="type">The exercise's type.</param>
    /// <param name="configJson">The played exercise's config, which the type's choices are built from.</param>
    /// <param name="staged">All cards of the exercise in the form they were asked at <paramref name="stage"/>.</param>
    /// <param name="item">The card the answer was given for, as asked.</param>
    /// <param name="stage">The stage the card was asked at.</param>
    /// <param name="given">The child's answer.</param>
    /// <param name="wasCorrect">The verdict the caller stored for the answer.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<AnswerErrorKind?> ClassifyAsync(IExerciseType type, string configJson, IReadOnlyList<ContentItem> staged,
        ContentItem item, int stage, string? given, bool wasCorrect, CancellationToken ct)
    {
        if (wasCorrect || item.IsNumber || type.Choices(configJson, staged, item, stage) is { Count: > 0 }) return null;
        if (grader.SpellingSlip(given, item, staged) is { } slip) return slip;
        return await IsSynonymOfAnyAsync(given, [item], ct) ? AnswerErrorKind.Synonym : AnswerErrorKind.WrongWord;
    }

    /// <summary>
    /// The kind of a wrong mention in a set-graded exercise (an answer that credits no open entry): measured against each
    /// entry still open, the most specific kind wins - one letter off any entry is a typo, not another word. A blank one is no mention.
    /// The spelling check is pure and runs first; the store is asked only when no entry is a spelling slip, and then once for
    /// all open entries, not once per entry.
    /// </summary>
    public async Task<AnswerErrorKind?> ClassifyMentionAsync(IReadOnlyList<ContentItem> items, IEnumerable<int> open,
        string? given, CancellationToken ct)
    {
        var entries = open.Select(i => items[i]).ToList();
        AnswerErrorKind? best = null;
        foreach (var entry in entries)
        {
            var slip = grader.SpellingSlip(given, entry, items);
            // Accent is the most specific kind there is: no further entry can beat it.
            if (slip == AnswerErrorKind.Accent) return slip;
            if (slip is not null && (best is null || slip < best)) best = slip;
        }
        if (best is not null) return best;
        return await IsSynonymOfAnyAsync(given, entries, ct) ? AnswerErrorKind.Synonym : AnswerErrorKind.WrongWord;
    }

    // B-732, assumption 2: the answer is the learnt word of another store entry that shares a meaning with an asked one
    // ("big" asked as "groß", "large" typed). Only on a card whose answer is the learnt word: the store declares meanings
    // for words, so it knows no German synonyms - and two entries sharing a word are homonyms, not synonyms (see
    // Vocabulary.TranslationAlternatives). One query for the asked entries and one per language pair among them.
    private async Task<bool> IsSynonymOfAnyAsync(string? given, IReadOnlyList<ContentItem> asked, CancellationToken ct)
    {
        var askedIds = asked.Where(i => i.Reversed && i.VocabularyId is not null).Select(i => i.VocabularyId!.Value)
            .Distinct().ToList();
        if (askedIds.Count == 0) return false;
        var core = StageMechanics.Normalize(given).Split(' ').MaxBy(w => w.Length) ?? "";
        if (core.Length == 0) return false;
        var askedRows = await db.Vocabularies.AsNoTracking().Where(v => askedIds.Contains(v.Id))
            .Select(v => new { v.Id, v.SourceLanguage, v.TargetLanguage, v.Translation, v.TranslationAlternatives })
            .ToListAsync(ct);
        // SQLite's LIKE folds ASCII case only; a word stored with a capital umlaut is missed, which leaves it a wrong word.
        var pattern = "%" + core.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        foreach (var pair in askedRows.GroupBy(r => (r.SourceLanguage, r.TargetLanguage)))
        {
            var ids = pair.Select(r => r.Id).ToList();
            var candidates = await db.Vocabularies.AsNoTracking()
                .Where(v => v.SourceLanguage == pair.Key.SourceLanguage && v.TargetLanguage == pair.Key.TargetLanguage
                    && EF.Functions.Like(v.Word, pattern, "\\"))
                .Select(v => new { v.Id, v.Word, v.Translation, v.TranslationAlternatives })
                // Shortest first: past the cap, the words closest to the typed one stay in (review finding - an unordered
                // cap let SQLite pick which fifty, and the kind is fixed once stored). The asked entries ride along in the
                // cap and are skipped below, so the cap grows by their number.
                .OrderBy(v => v.Word.Length).ThenBy(v => v.Word)
                .Take(MaxSynonymCandidates + ids.Count)
                .ToListAsync(ct);
            foreach (var row in pair)
            {
                var item = asked.First(i => i.VocabularyId == row.Id);
                var meanings = Meanings(row.Translation, row.TranslationAlternatives);
                if (candidates.Where(c => c.Id != row.Id).Take(MaxSynonymCandidates).Any(c =>
                        grader.Matches(given, new ContentItem(0, "", c.Word, [c.Word], ArticleOptionalIn: item.ArticleOptionalIn,
                            MeaningsSeparable: true))
                        && Meanings(c.Translation, c.TranslationAlternatives).Overlaps(meanings)))
                    return true;
            }
        }
        return false;
    }

    // Every single meaning of a store entry, normalized: "laufen, rennen" is two.
    private static HashSet<string> Meanings(string translation, List<string>? alternatives) =>
        [.. (alternatives ?? []).Prepend(translation).SelectMany(AnswerVariants.Of).Select(m => StageMechanics.Normalize(m))
            .Where(m => m.Length > 0)];
}
