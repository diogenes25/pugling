using pugling.Application;
using pugling.Infrastructure.DbServices;
using pugling.Infrastructure.DbServices.DbModels;
using pugling.Infrastructure.Persistance.DbModels;
using pugling.Models;
using pugling.Models.Converter;

namespace pugling.Infrastructure.Persistance
{
    public class VocabularyPersit<T>
    where T : IVocabularyEntity, new()
    {
        private readonly IInputOutputConverter<T> _vocabularyDbService;
        private readonly ILogger<VocabularyPersit<T>> _logger;

        public VocabularyPersit(IInputOutputConverter<T> vocabularyDbService, ILogger<VocabularyPersit<T>> logger)
        {
            _vocabularyDbService = vocabularyDbService;
            _logger = logger;
        }

        //(IVocabularyDbService<VocabularyEntity, Q> _vocabularyDbService, ILogger<VocabularyPersit<Q>> _logger) : IVocabularyDbService<VocabularyDto, Vocabulary>

        public async Task<VocabularyDto> AddVocabularyAsync(Vocabulary vocabulary)
        {
            var vocabWork = Vocabulary.Create(vocabulary, null);
            var entity = new T();
            entity.FillAndValidate(vocabWork);
            var result = await _vocabularyDbService.AddVocabularyAsync(entity);
            return result.ToDomain();
        }

        public async Task DeleteVocabularyAsync(string id)
        {
            await _vocabularyDbService.DeleteVocabularyAsync(id);
        }

        public async Task<IEnumerable<VocabularyDto>> GetAllVocabulariesAsync()
        {
            var vocabularies = await _vocabularyDbService.GetAllVocabulariesAsync();
            return vocabularies.Select(v => v.ToDomain());
        }

        public async Task<VocabularyDto> GetVocabularyByIdAsync(string id)
        {
            var vocabulary = await _vocabularyDbService.GetVocabularyByIdAsync(id) ?? throw new KeyNotFoundException($"Vocabulary with ID '{id}' was not found.");
            return vocabulary.ToDomain();
        }

        public async Task<VocabularyDto> UpdateVocabularyAsync(Vocabulary vocabulary)
        {
            var vocabWork = Vocabulary.Create(vocabulary, null);
            var entity = new VocabularyEntity();
            entity.FillAndValidate(vocabWork);
            var result = await _vocabularyDbService.UpdateVocabularyAsync(entity);
            return result.ToDomain();
        }
    }
}