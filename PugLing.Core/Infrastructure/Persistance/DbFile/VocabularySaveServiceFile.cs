using Microsoft.Extensions.Logging;
using PugLing.Core.Application.Vocabularies;
using PugLing.Core.Infrastructure.Persistance.DbModels.Vocabularies;
using PugLing.Core.Services;
using System.Text.Json;

namespace PugLing.Core.Infrastructure.Persistance.DbFile;

public class VocabularySaveServiceFile : ISaveableService<Vocabulary>, IReadableService<IVocabularyEntity>
{
    private readonly ILogger<VocabularySaveServiceFile> _logger;

    public VocabularySaveServiceFile(ILogger<VocabularySaveServiceFile> logger)
    {
        this._logger = logger;
        // Combine the application path with the file name
        //this._filePath = Path.Combine(AppContext.BaseDirectory, "vocabulariesDB.json");
    }

    public async Task<Vocabulary> SaveAsync(Vocabulary vacabulary, CancellationToken cancellationToken)
    {
        var vocabularyEntity = new VocabularyEntity().FillAndValidate(vacabulary);
        var vocabularyJson = JsonSerializer.Serialize(vocabularyEntity, new JsonSerializerOptions { WriteIndented = true });
        var filePath = Path.Combine(AppContext.BaseDirectory, $"{vocabularyEntity.Id}.json");
        await File.WriteAllTextAsync(filePath, vocabularyJson);
        this._logger.LogInformation("Vocabulary saved to file: {FilePath}", filePath);
        return Vocabulary.Create(vacabulary.SourceLanguage, vacabulary.TargetLanguage, vocabularyEntity, this);
    }

    public Task<Vocabulary> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, $"{id}.json");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            this._logger.LogInformation("Vocabulary deleted from file: {FilePath}", filePath);
        }
        else
        {
            this._logger.LogWarning("File not found: {FilePath}", filePath);
        }
        return Task.FromResult<Vocabulary>(null);
    }

    public Task<Vocabulary> UpdateAsync(Vocabulary vacabulary, IEnumerable<string> updatedProperties, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<IVocabularyEntity> GetById(string srclang, string targetlang, string id)
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
            this._logger.LogWarning("File not found: {FilePath}", filePath);
            return null;
        }
    }
}