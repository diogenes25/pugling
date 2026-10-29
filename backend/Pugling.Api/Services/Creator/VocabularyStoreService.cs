using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Creator;

/// <summary>
/// Ensures that a used vocabulary entry lives in the central store: finds the entry that says this word and
/// translation (folded like <see cref="VocabKey"/>) or creates it. This way every vocabulary entry used in an
/// exercise gets a store ID and can be linked across multiple exercises.
/// </summary>
public class VocabularyStoreService(PuglingDbContext db)
{
    /// <summary>
    /// Returns the existing store entry for the word/translation or creates it (not yet with
    /// <c>SaveChanges</c> – the caller saves, so multiple vocabulary entries of an exercise land in one go).
    /// </summary>
    public async Task<Vocabulary> GetOrCreateAsync(string sourceLanguage, string word, string targetLanguage,
        string translation, PartOfSpeech? partOfSpeech = null, CancellationToken ct = default) =>
        (await GetOrCreateManyAsync(sourceLanguage, targetLanguage, [(word, translation)], partOfSpeech, ct))[0];

    /// <summary>
    /// <see cref="GetOrCreateAsync"/> for many pairs of one language pair, with a single lookup query. The result is
    /// parallel to <paramref name="pairs"/>; two pairs that fold to the same key yield the same (possibly new) entry.
    /// </summary>
    public async Task<IReadOnlyList<Vocabulary>> GetOrCreateManyAsync(string sourceLanguage, string targetLanguage,
        IReadOnlyList<(string Word, string Translation)> pairs, PartOfSpeech? partOfSpeech = null, CancellationToken ct = default)
    {
        var found = await FindManyAsync(sourceLanguage, targetLanguage, pairs, ct);
        // FindManyAsync loaded every row whose stored key is one of the generated ones, so the local view knows
        // which of them are taken - a stale key (B-337) keeps its slot, and the new entry moves on to _2.
        var takenKeys = db.Vocabularies.Local.Select(v => v.Key).ToHashSet(StringComparer.Ordinal);
        var result = new List<Vocabulary>(pairs.Count);
        foreach (var (word, translation) in pairs)
        {
            var key = VocabKey.Generate(sourceLanguage, word, targetLanguage, translation);
            if (!found.TryGetValue(key, out var vocab))
            {
                vocab = new Vocabulary
                {
                    Key = takenKeys.Contains(key) ? await UniqueKeyAsync(key, ct) : key,
                    SourceLanguage = sourceLanguage,
                    TargetLanguage = targetLanguage,
                    Word = word,
                    Translation = translation,
                    PartOfSpeech = partOfSpeech ?? PartOfSpeech.Other,
                };
                db.Vocabularies.Add(vocab);
                takenKeys.Add(vocab.Key);
                found[key] = vocab;
            }
            result.Add(vocab);
        }
        return result;
    }

    /// <summary>The entry whose <b>current</b> content folds to the key of this word/translation, or <c>null</c>.</summary>
    public async Task<Vocabulary?> FindAsync(string sourceLanguage, string word, string targetLanguage, string translation,
        CancellationToken ct) =>
        (await FindManyAsync(sourceLanguage, targetLanguage, [(word, translation)], ct))
            .GetValueOrDefault(VocabKey.Generate(sourceLanguage, word, targetLanguage, translation));

    /// <summary>
    /// The store entries for many word/translation pairs, keyed by the key each pair generates. A hit is judged by
    /// what an entry says <b>now</b>: a PATCH changes word or translation without touching the stored key, so the key
    /// alone can name an entry that says something else - or miss one whose typo was fixed (B-337). Candidates are the
    /// key matches plus the entries with the same word (NOCASE, the index); language case and surrounding whitespace
    /// fold in memory, the same way the key folds them. The oldest entry wins; entries added but not yet saved in this
    /// unit of work count as well (after the saved ones), so a word is not created twice in one batch.
    /// </summary>
    public async Task<Dictionary<string, Vocabulary>> FindManyAsync(string sourceLanguage, string targetLanguage,
        IEnumerable<(string Word, string Translation)> pairs, CancellationToken ct)
    {
        var list = pairs.ToList();
        var keys = list.Select(p => VocabKey.Generate(sourceLanguage, p.Word, targetLanguage, p.Translation)).Distinct().ToList();
        if (keys.Count == 0) return new(StringComparer.Ordinal);
        var words = list.Select(p => p.Word.Trim()).Distinct().ToList();

        // Loaded tracked into the local view, which also holds the entries of this unit of work that are not saved yet.
        await db.Vocabularies.Where(v => keys.Contains(v.Key) || words.Contains(v.Word)).LoadAsync(ct);
        var wanted = keys.ToHashSet(StringComparer.Ordinal);
        return db.Vocabularies.Local
            .Select(v => (Vocab: v, Key: VocabKey.Generate(v.SourceLanguage, v.Word, v.TargetLanguage, v.Translation)))
            .Where(x => wanted.Contains(x.Key))
            .OrderBy(x => x.Vocab.Id > 0 ? x.Vocab.Id : int.MaxValue)
            .GroupBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.First().Vocab, StringComparer.Ordinal);
    }

    /// <summary>Makes a generated base key unique by appending _2, _3 … on collision (saved or pending in this unit of work).</summary>
    public async Task<string> UniqueKeyAsync(string baseKey, CancellationToken ct)
    {
        var key = string.IsNullOrWhiteSpace(baseKey) ? "vokabel" : baseKey;
        if (!await KeyTakenAsync(key, ct)) return key;
        for (var n = 2; ; n++)
        {
            var candidate = $"{key}_{n}";
            if (!await KeyTakenAsync(candidate, ct)) return candidate;
        }
    }

    private async Task<bool> KeyTakenAsync(string key, CancellationToken ct) =>
        db.Vocabularies.Local.Any(v => v.Key == key) || await db.Vocabularies.AnyAsync(v => v.Key == key, ct);

    /// <summary>
    /// Writes equally valid translations that an exercise item sent explicitly onto its store entry –
    /// whether the entry was just created or found by its key. Kept apart from
    /// <see cref="GetOrCreateAsync"/> on purpose: that method must not change a found entry, otherwise two
    /// items of the same word in one batch would overwrite each other silently. Here the caller asked for
    /// the change. <c>null</c> leaves the entry untouched; an empty list clears it.
    /// </summary>
    public static void ApplyTranslationAlternatives(Vocabulary vocab, IReadOnlyList<string>? alternatives)
    {
        if (alternatives is null) return;
        vocab.TranslationAlternatives = CleanAlternatives([.. alternatives], vocab.Translation);
    }

    /// <summary>
    /// Trims the equally valid translations and drops blanks, duplicates and the primary
    /// <paramref name="translation"/> itself – it already counts, listing it again would show up as a
    /// redundant "also: riesig" in the UI. An empty result becomes <c>null</c>, so "none declared" has
    /// exactly one representation in the store – otherwise <c>[]</c> and <c>null</c> would be two ways of
    /// saying the same thing, and only one of them would answer "does this entry have alternatives?" the
    /// way a reader expects.
    /// </summary>
    public static List<string>? CleanAlternatives(List<string>? values, string translation)
    {
        if (values is null) return null;
        var cleaned = values.Select(v => v.Trim())
            .Where(v => v.Length > 0 && !string.Equals(v, translation.Trim(), StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return cleaned.Count > 0 ? cleaned : null;
    }
}
