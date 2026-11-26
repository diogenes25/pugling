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
    /// card shows options, so a choice stage that fell back to letter boxes is typed). The caller calls this only for an
    /// answer the server checked as text; a self-assessment has no text to classify. An empty answer is
    /// <see cref="AnswerErrorKind.WrongWord"/>: the child did not know the word.
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
        if (wasCorrect || type.Choices(configJson, staged, item, stage) is { Count: > 0 }) return null;
        if (grader.SpellingSlip(given, item, staged) is { } slip) return slip;
        return await IsSynonymAsync(given, item, ct) ? AnswerErrorKind.Synonym : AnswerErrorKind.WrongWord;
    }

    /// <summary>
    /// The kind of a wrong mention in a set-graded exercise (an answer that credits no open entry): measured against each
    /// entry still open, the most specific kind wins - one letter off any entry is a typo, not another word. A blank one is no mention.
    /// </summary>
    public async Task<AnswerErrorKind?> ClassifyMentionAsync(IReadOnlyList<ContentItem> items, IEnumerable<int> open,
        string? given, CancellationToken ct)
    {
        AnswerErrorKind? best = null;
        foreach (var index in open)
        {
            var kind = grader.SpellingSlip(given, items[index], items)
                ?? (await IsSynonymAsync(given, items[index], ct) ? AnswerErrorKind.Synonym : AnswerErrorKind.WrongWord);
            if (best is null || kind < best) best = kind;
        }
        return best ?? AnswerErrorKind.WrongWord;
    }

    // B-732, assumption 2: the answer is the learnt word of another store entry that shares a meaning with the asked one
    // ("big" asked as "groß", "large" typed). Only on a card whose answer is the learnt word: the store declares meanings
    // for words, so it knows no German synonyms - and two entries sharing a word are homonyms, not synonyms (see
    // Vocabulary.TranslationAlternatives).
    private async Task<bool> IsSynonymAsync(string? given, ContentItem item, CancellationToken ct)
    {
        if (!item.Reversed || item.VocabularyId is not { } vocabularyId) return false;
        var core = StageMechanics.Normalize(given).Split(' ').MaxBy(w => w.Length) ?? "";
        if (core.Length == 0) return false;
        var asked = await db.Vocabularies.AsNoTracking().Where(v => v.Id == vocabularyId)
            .Select(v => new { v.SourceLanguage, v.TargetLanguage, v.Translation, v.TranslationAlternatives })
            .FirstOrDefaultAsync(ct);
        if (asked is null) return false;
        var meanings = Meanings(asked.Translation, asked.TranslationAlternatives);
        // SQLite's LIKE folds ASCII case only; a word stored with a capital umlaut is missed, which leaves it a wrong word.
        var pattern = "%" + core.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        var candidates = await db.Vocabularies.AsNoTracking()
            .Where(v => v.Id != vocabularyId && v.SourceLanguage == asked.SourceLanguage
                && v.TargetLanguage == asked.TargetLanguage && EF.Functions.Like(v.Word, pattern, "\\"))
            .Select(v => new { v.Word, v.Translation, v.TranslationAlternatives })
            // Shortest first: past the cap, the words closest to the typed one stay in (review finding - an unordered cap
            // let SQLite pick which fifty, and the kind is fixed once stored).
            .OrderBy(v => v.Word.Length).ThenBy(v => v.Word)
            .Take(MaxSynonymCandidates)
            .ToListAsync(ct);
        return candidates.Any(c =>
            grader.Matches(given, new ContentItem(0, "", c.Word, [c.Word], ArticleOptionalIn: item.ArticleOptionalIn,
                MeaningsSeparable: true))
            && Meanings(c.Translation, c.TranslationAlternatives).Overlaps(meanings));
    }

    // Every single meaning of a store entry, normalized: "laufen, rennen" is two.
    private static HashSet<string> Meanings(string translation, List<string>? alternatives) =>
        [.. (alternatives ?? []).Prepend(translation).SelectMany(AnswerVariants.Of).Select(m => StageMechanics.Normalize(m))
            .Where(m => m.Length > 0)];
}
