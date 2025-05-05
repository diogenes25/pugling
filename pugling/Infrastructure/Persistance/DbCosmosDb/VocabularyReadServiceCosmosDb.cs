using Microsoft.Azure.Cosmos;
using pugling.Application.Vocabularies;
using pugling.Infrastructure.Persistance.DbModels.Vocabularies;
using pugling.Services;

namespace pugling.Infrastructure.Persistance.DbCosmosDb;

public class VocabularyReadServiceCosmosDb : ACosmosDbBase, IReadableService<IVocabularyEntity>
{
    public VocabularyReadServiceCosmosDb(CosmosDbContainerFactory cosmosDbSettings, ILogger<VocabularyReadServiceCosmosDb> logger) : base(cosmosDbSettings, "Vocabulary", logger)
    {
    }

    public async Task<IVocabularyEntity> GetById(string id)
    {
        try
        {
            //    var response = await _container.ReadItemAsync<VocabularyCosmosEntity>(
            //        id,
            //        new PartitionKey("vocabulary")
            //    );
            //    return response.Resource;
            //}
            //catch (CosmosException ex)
            //{
            //    _logger.LogError(ex, "Error retrieving item with id {Id} and partition key {PartitionKey}", id, "vocabulary");
            //    throw;
            //}

            var response = await _container.ReadItemAsync<VocabularyCosmosEntity>(id, new PartitionKey("vocabulary"))
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    _logger.LogError(task.Exception, "Error retrieving item with id {Id}", id);
                    throw new ItemNotFoundException(id);
                }
                if (task.IsCanceled)
                {
                    _logger.LogWarning("Task was canceled while retrieving item with id {Id}", id);
                    throw new TaskCanceledException(task);
                }
                if (task.IsCompletedSuccessfully == false)
                {
                    _logger.LogWarning("Task was not completed successfully while retrieving item with id {Id}", id);
                    throw new Exception("Task was not completed successfully");
                }
                var response = task.Result;
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return response.Resource;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new ItemNotFoundException(id);
                }
                else
                {
                    throw new Exception($"Could not find item {id}");
                }
            });

            return response;
        }
        catch (CosmosException ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new ItemNotFoundException(id);
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error");
            throw;
        }
    }
}