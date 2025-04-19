using pugling.Infrastructure.DbServices.DbModels;

namespace pugling.Infrastructure.DbServices
{
    public interface IVocabularyDbService
    {
        Task<IEnumerable<VocabularyEntity>> GetAllVocabulariesAsync();
        Task<VocabularyEntity> GetVocabularyByIdAsync(int id);
        Task<VocabularyEntity> AddVocabularyAsync(VocabularyEntity vocabulary);
        Task<VocabularyEntity> UpdateVocabularyAsync(VocabularyEntity vocabulary);
        Task DeleteVocabularyAsync(int id);
    }


}
