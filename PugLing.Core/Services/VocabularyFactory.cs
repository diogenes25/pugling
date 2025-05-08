using PugLing.Core.Infrastructure.Persistance.DbModels.Vocabularies;
using PugLing.Core.Application.Vocabularies;
using PugLing.Model.Models;

namespace PugLing.Core.Services;

public class VocabularyFactory(
    ISaveableService<Vocabulary> SaveableService,
    IReadableService<IVocabularyEntity> ReadableService
    )
{
    public Vocabulary CreateVocabulary(string sourceLang, string targetLang, IVocabulary<IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails> vocabulary)
    {
        var newVocabulary = Vocabulary.Create(sourceLanguage: sourceLang, targetLanguage: targetLang, vocabulary: vocabulary, vocabularySaveServiceFile: SaveableService);
        // newVocabulary.SaveableService = SaveableService;
        return newVocabulary;
    }

    public async Task<IVocabularyEntity> GetVocabularyAsync(string srclang, string targetlang, string id)
    {
        var vocabularyEntity = await ReadableService.GetById(srclang: srclang, targetlang: targetlang, id: id);

        return vocabularyEntity ?? throw new KeyNotFoundException($"Vocabulary with ID {id} not found.");
    }
}