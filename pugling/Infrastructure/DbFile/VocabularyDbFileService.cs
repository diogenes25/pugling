using pugling.Infrastructure.DbServices;
using pugling.Infrastructure.DbServices.DbModels;
using System.Text.Json;

namespace pugling.Infrastructure.DbFile
{
    public class VocabularyDbFileService : IVocabularyDbService
    {
        private readonly string _filePath;

        public VocabularyDbFileService()
        {
            // Combine the application path with the file name
            _filePath = Path.Combine(AppContext.BaseDirectory, "vocabularies");
        }


        public async Task<VocabularyEntity> AddVocabularyAsync(VocabularyEntity vocabulary)
        {
            var vocabularies = await ReadVocabulariesFromFileAsync();
            vocabularies.Add(vocabulary);
            await WriteVocabulariesToFileAsync(vocabularies);
            return vocabulary;
        }

        public async Task DeleteVocabularyAsync(int id)
        {
            var vocabularies = await ReadVocabulariesFromFileAsync();
            var vocabularyToRemove = vocabularies.FirstOrDefault(v => v.Id == id.ToString());
            if (vocabularyToRemove != null)
            {
                vocabularies.Remove(vocabularyToRemove);
                await WriteVocabulariesToFileAsync(vocabularies);
            }
        }

        public async Task<IEnumerable<VocabularyEntity>> GetAllVocabulariesAsync()
        {
            return await ReadVocabulariesFromFileAsync();
        }

        public async Task<VocabularyEntity> GetVocabularyByIdAsync(int id)
        {
            var vocabularies = await ReadVocabulariesFromFileAsync();
            return vocabularies.FirstOrDefault(v => v.Id == id.ToString());
        }

        public async Task<VocabularyEntity> UpdateVocabularyAsync(VocabularyEntity vocabulary)
        {
            var vocabularies = await ReadVocabulariesFromFileAsync();
            var existingVocabulary = vocabularies.FirstOrDefault(v => v.Id == vocabulary.Id);
            if (existingVocabulary != null)
            {
                vocabularies.Remove(existingVocabulary);
                vocabularies.Add(vocabulary);
                await WriteVocabulariesToFileAsync(vocabularies);
            }
            return vocabulary;
        }

        private async Task<List<VocabularyEntity>> ReadVocabulariesFromFileAsync()
        {
            if (!File.Exists(_filePath))
            {
                return [];
            }

            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<List<VocabularyEntity>>(json) ?? [];
        }

        private async Task WriteVocabulariesToFileAsync(List<VocabularyEntity> vocabularies)
        {
            var json = JsonSerializer.Serialize(vocabularies, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json);
        }
    }
}
