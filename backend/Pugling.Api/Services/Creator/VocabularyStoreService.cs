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
    /// <see cref="GetOrCreateAsync"/> for many pairs of one language pair, with one lookup for all of them. The result is
    /// parallel to <paramref name="pairs"/>; two pairs with the same <see cref="VocabKey.Identity"/> yield the same
    /// (possibly new) entry.
    /// </summary>
    public async Task<IReadOnlyList<Vocabulary>> GetOrCreateManyAsync(string sourceLanguage, string targetLanguage,
        IReadOnlyList<(string Word, string Translation)> pairs, PartOfSpeech? partOfSpeech = null, CancellationToken ct = default)
    {
        var found = await FindManyAsync(sourceLanguage, targetLanguage, pairs, ct);
        var result = new List<Vocabulary>(pairs.Count);
        foreach (var (word, translation) in pairs)
        {
            var identity = VocabKey.Identity(sourceLanguage, word, targetLanguage, translation);
            if (!found.TryGetValue(identity, out var vocab))
            {
                vocab = new Vocabulary
                {
                    // The generated key can be taken by an entry that says something else now (a stale key, B-337, or
                    // another pair joining to the same key, B-346); that entry keeps it, and this one moves on to _2.
                    Key = await UniqueKeyAsync(VocabKey.Generate(sourceLanguage, word, targetLanguage, translation), ct),
                    SourceLanguage = sourceLanguage,
                    TargetLanguage = targetLanguage,
                    Word = word,
                    Translation = translation,
                    PartOfSpeech = partOfSpeech ?? PartOfSpeech.Other,
                };
                db.Vocabularies.Add(vocab);
                found[identity] = vocab;
            }
            result.Add(vocab);
        }
        return result;
    }

    /// <summary>The entry whose <b>current</b> content folds like this word/translation, or <c>null</c>.</summary>
    public async Task<Vocabulary?> FindAsync(string sourceLanguage, string word, string targetLanguage, string translation,
        CancellationToken ct) =>
        (await FindManyAsync(sourceLanguage, targetLanguage, [(word, translation)], ct))
            .GetValueOrDefault(VocabKey.Identity(sourceLanguage, word, targetLanguage, translation));

    /// <summary>
    /// The store entries for many word/translation pairs of one language pair, keyed by
    /// <see cref="VocabKey.Identity"/>. A hit is judged by what an entry says <b>now</b>, folded completely: a PATCH
    /// changes word or translation without touching the stored key (B-337), the joined key is ambiguous (B-346), and
    /// SQL folds no more than ASCII case (B-347). So every saved entry of the language pair is a candidate, read as a
    /// narrow projection and folded in memory - a store holds hundreds of words per pair, not millions. The oldest entry
    /// wins; entries added but not yet saved in this unit of work count after the saved ones, so a word is not created
    /// twice in one batch. The candidates are fixed per call, not by what an earlier call happened to load (B-349).
    /// </summary>
    public async Task<Dictionary<VocabIdentity, Vocabulary>> FindManyAsync(string sourceLanguage, string targetLanguage,
        IEnumerable<(string Word, string Translation)> pairs, CancellationToken ct)
    {
        var wanted = pairs.Select(p => VocabKey.Identity(sourceLanguage, p.Word, targetLanguage, p.Translation)).ToHashSet();
        if (wanted.Count == 0) return [];

        // Language codes are plain ASCII, where SQL's lower() and trim() (spaces only) fold them like the identity does.
        var src = sourceLanguage.Trim().ToLowerInvariant();
        var tgt = targetLanguage.Trim().ToLowerInvariant();
        var saved = await db.Vocabularies.AsNoTracking()
            .Where(v => v.SourceLanguage.Trim().ToLower() == src && v.TargetLanguage.Trim().ToLower() == tgt)
            .Select(v => new { v.Id, v.SourceLanguage, v.Word, v.TargetLanguage, v.Translation })
            .ToListAsync(ct);
        var savedHits = saved
            .Select(v => (v.Id, Identity: VocabKey.Identity(v.SourceLanguage, v.Word, v.TargetLanguage, v.Translation)))
            .Where(x => wanted.Contains(x.Identity))
            .GroupBy(x => x.Identity)
            .ToDictionary(g => g.Key, g => g.Min(x => x.Id));
        var ids = savedHits.Values.ToList();
        var byId = await db.Vocabularies.Where(v => ids.Contains(v.Id)).ToDictionaryAsync(v => v.Id, ct);
        var found = savedHits.Where(h => byId.ContainsKey(h.Value)).ToDictionary(h => h.Key, h => byId[h.Value]);

        foreach (var pending in db.ChangeTracker.Entries<Vocabulary>().Where(e => e.State == EntityState.Added).Select(e => e.Entity))
        {
            var identity = VocabKey.Identity(pending.SourceLanguage, pending.Word, pending.TargetLanguage, pending.Translation);
            if (wanted.Contains(identity)) found.TryAdd(identity, pending);
        }
        return found;
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
