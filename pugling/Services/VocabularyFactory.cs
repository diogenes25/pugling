using pugling.Application;
using pugling.Infrastructure.Persistance.DbModels;
using pugling.Models;

namespace pugling.Services
{
    public class VocabularyFactory(
    ISaveableService<Vocabulary> SaveableService,
    IReadableService<IVocabularyEntity> ReadableService)
    {
        public Vocabulary CreateVocabulary(IVocabulary<IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails> vocabulary)
        {
            var newVocabulary = Vocabulary.Create(vocabulary, SaveableService);
            // newVocabulary.SaveableService = SaveableService;
            return newVocabulary;
        }

        public async Task<IVocabularyEntity> GetVocabularyAsync(string id)
        {
            var vocabulary = await ReadableService.GetById(id);

            return vocabulary ?? throw new KeyNotFoundException($"Vocabulary with ID {id} not found.");
        }
    }
}