using Microsoft.Azure.Cosmos;
using pugling.Application.Vocabularies;
using pugling.Services;

namespace pugling.Infrastructure.Persistance.DbCosmosDb;

public class VocabularySaveServiceCosmosDb : ACosmosDbBase, ISaveableService<Vocabulary>
{
    public VocabularySaveServiceCosmosDb(
    CosmosDbContainerFactory cosmosDbSettings,
    ILogger<VocabularySaveServiceCosmosDb> logger) : base(cosmosDbSettings, "Vocabulary", logger)
    {
    }

    public async Task<Vocabulary> SaveAsync(Vocabulary saveObj, CancellationToken cancellationToken)
    {
        var vocabularyEntity = new VocabularyCosmosEntity().FillAndValidate(saveObj);
        try
        {
            var response = await _container.CreateItemAsync(vocabularyEntity, new PartitionKey(vocabularyEntity.vocabularypartition), cancellationToken: cancellationToken);

            return Vocabulary.Create(vocabularyEntity.SourceLanguage, vocabularyEntity.TargetLanguage, response.Resource, this);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Error saving vocabulary to Cosmos DB");
            throw;
        }
    }

    public Task<Vocabulary> UpdateAsync(Vocabulary saveObj, IEnumerable<string> updatedProperties, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}