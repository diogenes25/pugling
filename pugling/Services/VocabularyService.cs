using pugling.Application;
using pugling.Infrastructure.DbServices;
using pugling.Models;

namespace pugling.Services
{
    public class VocabularyService(IVocabularyDbService<VocabularyDto, Vocabulary> _vocabularyDbService, ILogger<VocabularyService> _logger)
    {
        public async Task<IEnumerable<VocabularyDto>> GetAllVocabulariesAsync()
        {
            try
            {
                return await _vocabularyDbService.GetAllVocabulariesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vocabularies");
                throw;
            }
        }

        public async Task<VocabularyDto> GetVocabularyByIdAsync(string id)
        {
            try
            {
                return await _vocabularyDbService.GetVocabularyByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vocabulary with ID {Id}", id);
                throw;
            }
        }

        public async Task<VocabularyDto> AddVocabularyAsync(VocabularyDto vocabulary)
        {
            var vocabWork = Vocabulary.Create(vocabulary);

            try
            {
                return await _vocabularyDbService.AddVocabularyAsync(vocabWork);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding vocabulary");
                throw;
            }
        }

        public async Task<VocabularyDto> UpdateVocabularyAsync(VocabularyDto vocabulary)
        {
            var vocabWork = Vocabulary.Create(vocabulary);

            try
            {
                return await _vocabularyDbService.UpdateVocabularyAsync(vocabWork);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vocabulary");
                throw;
            }
        }

        public async Task DeleteVocabularyAsync(string id)
        {
            try
            {
                await _vocabularyDbService.DeleteVocabularyAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vocabulary with ID {Id}", id);
                throw;
            }
        }

        public async Task<List<VocabularyDto>> SearchVocabulariesAsync(string query)
        {
            throw new NotImplementedException();
        }
    }
}