using pugling.Infrastructure.DbServices.DbModels;
using pugling.Infrastructure.Persistance.DbModels;

namespace pugling.Infrastructure.DbServices
{
    /// <summary>
    /// Interface for managing vocabulary database operations.
    /// </summary>
    public interface IInputOutputConverter<TOut>
    {
        /// <summary>
        /// Retrieves all vocabulary entities asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of <see cref="VocabularyEntity"/>.</returns>
        Task<IEnumerable<TOut>> GetAllVocabulariesAsync();

        /// <summary>
        /// Retrieves a vocabulary entity by its unique identifier asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the vocabulary entity.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="VocabularyEntity"/>.</returns>
        Task<TOut> GetVocabularyByIdAsync(string id);

        /// <summary>
        /// Adds a new vocabulary entity to the database asynchronously.
        /// </summary>
        /// <param name="vocabulary">The vocabulary entity to add.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the added <see cref="VocabularyEntity"/>.</returns>
        Task<TOut> AddVocabularyAsync(IVocabularyEntity vocabulary);

        /// <summary>
        /// Updates an existing vocabulary entity in the database asynchronously.
        /// </summary>
        /// <param name="vocabulary">The vocabulary entity with updated values.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated <see cref="VocabularyEntity"/>.</returns>
        Task<TOut> UpdateVocabularyAsync(IVocabularyEntity vocabulary);

        /// <summary>
        /// Deletes a vocabulary entity from the database by its unique identifier asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the vocabulary entity to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteVocabularyAsync(string id);
    }
}