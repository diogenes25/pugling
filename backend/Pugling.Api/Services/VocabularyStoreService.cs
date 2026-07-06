using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services;

/// <summary>
/// Sorgt dafür, dass eine genutzte Vokabel im zentralen Store liegt: findet einen Eintrag über seinen
/// stabilen Key (Sprache + Wort + Übersetzung, siehe <see cref="VocabKey"/>) oder legt ihn an. So bekommt
/// jede in einer Übung verwendete Vokabel eine Store-ID und ist über mehrere Übungen hinweg verknüpfbar.
/// </summary>
public class VocabularyStoreService(PuglingDbContext db)
{
    /// <summary>
    /// Liefert den vorhandenen Store-Eintrag zum Wort/zur Übersetzung oder legt ihn an (noch ohne
    /// <c>SaveChanges</c> – der Aufrufer speichert, damit mehrere Vokabeln einer Übung in einem Zug landen).
    /// Bereits in derselben Unit-of-Work angelegte Einträge werden über <see cref="DbSet{T}.Local"/> erkannt,
    /// damit dieselbe Vokabel nicht doppelt entsteht (Key-Eindeutigkeit).
    /// </summary>
    public async Task<Vocabulary> GetOrCreateAsync(string sourceLanguage, string word, string targetLanguage,
        string translation, PartOfSpeech? partOfSpeech = null, CancellationToken ct = default)
    {
        var key = VocabKey.Generate(sourceLanguage, word, targetLanguage, translation);

        // Schon in dieser Unit-of-Work angelegt (mehrere Items derselben Vokabel in einer Übung)?
        var local = db.Vocabulary.Local.FirstOrDefault(v => v.Key == key);
        if (local is not null) return local;

        var existing = await db.Vocabulary.FirstOrDefaultAsync(v => v.Key == key, ct);
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
        db.Vocabulary.Add(vocab);
        return vocab;
    }
}
