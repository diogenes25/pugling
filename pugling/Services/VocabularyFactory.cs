using pugling.Application.Vocabularies;
using pugling.Infrastructure.Persistance.DbModels.Vocabularies;
using pugling.Models;

namespace pugling.Services;

public class VocabularyFactory(
ISaveableService<Vocabulary> SaveableService,
IReadableService<IVocabularyEntity> ReadableService)
{
    public Vocabulary CreateVocabulary(string src, string target, IVocabulary<IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails> vocabulary)
    {
        var newVocabulary = Vocabulary.Create(src, target, vocabulary, SaveableService);
        // newVocabulary.SaveableService = SaveableService;
        return newVocabulary;
    }

    public async Task<IVocabularyEntity> GetVocabularyAsync(string srclang, string targetlang, string id)
    {
        var vocabularyEntity = await ReadableService.GetById(srclang, targetlang, id);

        return vocabularyEntity ?? throw new KeyNotFoundException($"Vocabulary with ID {id} not found.");
    }
}