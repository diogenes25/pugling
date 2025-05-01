using pugling.Application;
using pugling.Infrastructure.DbServices.DbModels;
using pugling.Infrastructure.Persistance.DbModels;
using pugling.Services;
using System.Text.Json;

namespace pugling.Infrastructure.DbFile
{
    public class VocabularySaveServiceFile : ISaveableService<Vocabulary>, IReadableService<IVocabularyEntity>
    {
        private readonly ILogger<VocabularySaveServiceFile> _logger;

        public VocabularySaveServiceFile(ILogger<VocabularySaveServiceFile> logger)
        {
            _logger = logger;
            // Combine the application path with the file name
            //this._filePath = Path.Combine(AppContext.BaseDirectory, "vocabulariesDB.json");
        }

        public async Task<Vocabulary> SaveCreateAsync(Vocabulary vacabulary, CancellationToken cancellationToken)
        {
            var vocabularyEntity = new VocabularyEntity().FillAndValidate(vacabulary);
            var vocabularyJson = JsonSerializer.Serialize(vocabularyEntity, new JsonSerializerOptions { WriteIndented = true });
            var filePath = Path.Combine(AppContext.BaseDirectory, $"{vocabularyEntity.Id}.json");
            await File.WriteAllTextAsync(filePath, vocabularyJson);
            _logger.LogInformation("Vocabulary saved to file: {FilePath}", filePath);
            return Vocabulary.Create(vocabularyEntity, this);
        }

        public Task<Vocabulary> DeleteAsync(string id, CancellationToken cancellationToken)
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, $"{id}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Vocabulary deleted from file: {FilePath}", filePath);
            }
            else
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
            }
            return Task.FromResult<Vocabulary>(null);
        }

        public Task<Vocabulary> SaveUpdateAsync(Vocabulary vacabulary, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Vocabulary> UpdateAsync(Vocabulary vacabulary, IEnumerable<string> updatedProperties, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<IVocabularyEntity> GetById(string id)
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, $"{id}.json");
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                var vocabularyEntity = JsonSerializer.Deserialize<VocabularyEntity>(json);
                return vocabularyEntity;
            }
            else
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }
        }
    }
}