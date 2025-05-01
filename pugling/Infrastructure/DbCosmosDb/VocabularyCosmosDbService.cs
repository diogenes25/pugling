using Microsoft.Azure.Cosmos;
using pugling.Infrastructure.DbServices;
using pugling.Infrastructure.DbServices.DbModels;
using pugling.Infrastructure.Persistance.DbModels;

namespace pugling.Infrastructure.DbCosmosDb
{
    public class VocabularyCosmosDbService : IInputOutputConverter<VocabularyCosmosEntity>
    {
        private readonly CosmosDbSettings _cosmosDbSettings;
        private CosmosClient _client;

        //public VocabularyCosmosDbService(IOptions<CosmosDbSettings> cosmosDbSettings)
        //{
        //    _cosmosDbSettings = cosmosDbSettings.Value;
        //}

        public VocabularyCosmosDbService()
        {
            //_cosmosDbSettings = cosmosDbSettings;
        }

        public async Task<VocabularyEntity> AddVocabularyAsync(VocabularyCosmosEntity vocabulary)
        {
            await _client.GetDatabase(_cosmosDbSettings.DatabaseName)
                 .GetContainer(_cosmosDbSettings.ContainerName)
                 .CreateItemAsync(vocabulary, new PartitionKey(vocabulary.Id));

            return vocabulary;
        }

        public Task<VocabularyCosmosEntity> AddVocabularyAsync(IVocabularyEntity vocabulary)
        {
            throw new NotImplementedException();
        }

        public void ConnectToCosmosDb()
        {
            _client = new CosmosClient(_cosmosDbSettings.AccountEndpoint, _cosmosDbSettings.AccountKey);
            // Use the client...
        }

        public Task DeleteVocabularyAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<VocabularyCosmosEntity>> GetAllVocabulariesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<VocabularyCosmosEntity> GetVocabularyByIdAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<VocabularyCosmosEntity> UpdateVocabularyAsync(IVocabularyEntity vocabulary)
        {
            throw new NotImplementedException();
        }
    }
}