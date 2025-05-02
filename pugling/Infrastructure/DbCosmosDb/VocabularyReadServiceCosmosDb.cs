using pugling.Infrastructure.Persistance.DbModels;
using pugling.Services;

namespace pugling.Infrastructure.DbCosmosDb
{
    public class VocabularyReadServiceCosmosDb : ACosmosDbBase, IReadableService<IVocabularyEntity>
    {
        public VocabularyReadServiceCosmosDb(CosmosDbSettings cosmosDbSettings, ILogger<VocabularyReadServiceCosmosDb> logger) : base(cosmosDbSettings, "vocabulary", logger)
        {
        }

        public Task<IVocabularyEntity> GetById(string id)
        {
            throw new NotImplementedException();
        }
    }
}