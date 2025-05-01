using pugling.Models;
using pugling.Models.Converter;

namespace pugling.Services
{
    public class VocabularyService
    {
        private readonly VocabularyFactory _vocabularyFactory;
        private readonly ILogger<VocabularyService> _logger;

        public VocabularyService(VocabularyFactory vocabularyFactory, ILogger<VocabularyService> logger)
        {
            _vocabularyFactory = vocabularyFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<VocabularyDto>> GetAllVocabulariesAsync()
        {
            throw new NotImplementedException();
            //try
            //{
            //    return await _vocabularyDbService.GetAllVocabulariesAsync();
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Error retrieving vocabularies");
            //    throw;
            //}
        }

        public async Task<VocabularyDto> GetVocabularyByIdAsync(string id)
        {
            try
            {
                var vocabEntity = await _vocabularyFactory.GetVocabularyAsync(id);
                return vocabEntity.ToDomain();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vocabulary with ID {Id}", id);
                throw;
            }
        }

        public async Task<VocabularyDto> AddVocabularyAsync(VocabularyDto vocabulary)
        {
            var vocabWork = _vocabularyFactory.CreateVocabulary(vocabulary);

            try
            {
                var voc = await vocabWork.SaveAsync(CancellationToken.None);
                return voc.ToDomain();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding vocabulary");
                throw;
            }
        }

        //public async Task<VocabularyDto> UpdateVocabularyAsync(VocabularyDto vocabulary)
        //{
        //    var vocabWork = Vocabulary.Create(vocabulary);

        //    try
        //    {
        //        return await _vocabularyDbService.UpdateVocabularyAsync(vocabWork);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error updating vocabulary");
        //        throw;
        //    }
        //}

        //public async Task DeleteVocabularyAsync(string id)
        //{
        //    try
        //    {
        //        await _vocabularyDbService.DeleteVocabularyAsync(id);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error deleting vocabulary with ID {Id}", id);
        //        throw;
        //    }
        //}

        public async Task<List<VocabularyDto>> SearchVocabulariesAsync(string query)
        {
            throw new NotImplementedException();
        }
    }
}