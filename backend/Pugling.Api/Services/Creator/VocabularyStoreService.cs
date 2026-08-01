using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Creator;

/// <summary>
/// Ensures that a used vocabulary entry lives in the central store: finds an entry via its
/// stable key (language + word + translation, see <see cref="VocabKey"/>) or creates it. This way
/// every vocabulary entry used in an exercise gets a store ID and can be linked across multiple exercises.
/// </summary>
public class VocabularyStoreService(PuglingDbContext db)
{
    /// <summary>
    /// Returns the existing store entry for the word/translation or creates it (not yet with
    /// <c>SaveChanges</c> – the caller saves, so multiple vocabulary entries of an exercise land in one go).
    /// Entries already created in the same unit of work are detected via <see cref="DbSet{T}.Local"/>,
    /// so the same vocabulary entry is not created twice (key uniqueness).
    /// </summary>
    public async Task<Vocabulary> GetOrCreateAsync(string sourceLanguage, string word, string targetLanguage,
        string translation, PartOfSpeech? partOfSpeech = null, CancellationToken ct = default)
    {
        var key = VocabKey.Generate(sourceLanguage, word, targetLanguage, translation);

        // Already created within this unit of work (several items of the same vocabulary entry in one exercise)?
        var local = db.Vocabularies.Local.FirstOrDefault(v => v.Key == key);
        if (local is not null) return local;

        var existing = await db.Vocabularies.FirstOrDefaultAsync(v => v.Key == key, ct);
        if (existing is not null) return existing;

        var vocab = new Vocabulary
        {
            Key = key,
            SourceLanguage = sourceLanguage,
            TargetLanguage = targetLanguage,
            Word = word,
            Translation = translation,
            PartOfSpeech = partOfSpeech ?? PartOfSpeech.Other,
        };
        db.Vocabularies.Add(vocab);
        return vocab;
    }
}
