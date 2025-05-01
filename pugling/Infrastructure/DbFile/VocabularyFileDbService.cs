using pugling.Infrastructure.DbServices;
using pugling.Infrastructure.DbServices.DbModels;
using pugling.Infrastructure.Persistance.DbModels;
using System.Text.Json;

namespace pugling.Infrastructure.DbFile
{
    public class VocabularyFileDbService : IInputOutputConverter<VocabularyEntity>
    {
        private readonly string _filePath;

        public VocabularyFileDbService()
        {
            // Combine the application path with the file name
            this._filePath = Path.Combine(AppContext.BaseDirectory, "vocabulariesDB.json");
        }

        public async Task<VocabularyEntity> AddVocabularyAsync(VocabularyEntity vocabulary)
        {
            var vocabularies = await ReadVocabulariesFromFileAsync();
            vocabularies.Add(vocabulary);
            await WriteVocabulariesToFileAsync(vocabularies);
            return vocabulary;
        }

        public Task<VocabularyEntity> AddVocabularyAsync(IVocabularyEntity vocabulary)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteVocabularyAsync(string id)
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

        public async Task<VocabularyEntity> GetVocabularyByIdAsync(string id)
        {
            var vocabularies = await ReadVocabulariesFromFileAsync();
            return vocabularies.FirstOrDefault(v => v.Id == id.ToString());
        }

        public async Task<VocabularyEntity> UpdateVocabularyAsync(IVocabularyEntity vocabulary)
        {
            var vocabularies = await ReadVocabulariesFromFileAsync();
            var existingVocabulary = vocabularies.FirstOrDefault(v => v.Id == vocabulary.Id);

            var convertVocabular = vocabulary as VocabularyEntity;

            if (existingVocabulary != null)
            {
                vocabularies.Remove(existingVocabulary);
                vocabularies.Add(convertVocabular);
                await WriteVocabulariesToFileAsync(vocabularies);
            }
            return convertVocabular;
        }

        private async Task<List<VocabularyEntity>> ReadVocabulariesFromFileAsync()
        {
            if (!File.Exists(this._filePath))
            {
                return [];
            }

            var json = await File.ReadAllTextAsync(this._filePath);
            return JsonSerializer.Deserialize<List<VocabularyEntity>>(json) ?? [];
        }

        private async Task WriteVocabulariesToFileAsync(List<VocabularyEntity> vocabularies)
        {
            var json = JsonSerializer.Serialize(vocabularies, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(this._filePath, json);
        }
    }
}