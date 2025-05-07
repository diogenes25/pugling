using Microsoft.Extensions.Logging;
using PugLingTransfer.Models;
using PugLingTransfer.Models.Converter;

namespace pugling.Services;

public class VocabularyService(VocabularyFactory vocabularyFactory, ILogger<VocabularyService> logger)
{
    private readonly VocabularyFactory _vocabularyFactory = vocabularyFactory;
    private readonly ILogger<VocabularyService> _logger = logger;

    public async Task<IEnumerable<VocabularyDto>> GetAllVocabulariesAsync()
    {
        throw new NotImplementedException();
        //try
        //{
        //    return await this._vocabularyDbService.GetAllVocabulariesAsync();
        //}
        //catch (Exception ex)
        //{
        //    this._logger.LogError(ex, "Error retrieving vocabularies");
        //    throw;
        //}
    }

    public async Task<VocabularyDto> GetVocabularyByIdAsync(string srclang, string targetlang, string id)
    {
        try
        {
            var vocabEntity = await this._vocabularyFactory.GetVocabularyAsync(srclang, targetlang, id);
            return vocabEntity.ToDomain();
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error retrieving vocabulary with ID {Id}", id);
            throw;
        }
    }

    public async Task<VocabularyDto> AddVocabularyAsync(string src, string target, VocabularyDto vocabulary)
    {
        var vocabWork = this._vocabularyFactory.CreateVocabulary(src, target, vocabulary);

        try
        {
            var voc = await vocabWork.SaveAsync(CancellationToken.None);
            return voc.ToDomain();
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error adding vocabulary");
            throw;
        }
    }

    public async Task<List<VocabularyDto>> SearchVocabulariesAsync(string query)
    {
        throw new NotImplementedException();
    }
}