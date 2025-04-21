using pugling.Infrastructure.DbServices.DbModels;
using pugling.Models;

namespace pugling.Infrastructure.DbServices
{
    /// <summary>
    /// Interface for managing vocabulary database operations.
    /// </summary>
    public interface IVocabularyDbService<T, Q>
    where T : IVocabulary<IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails>
    where Q : IVocabulary<IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails>
    {
        /// <summary>
        /// Retrieves all vocabulary entities asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of <see cref="VocabularyEntity"/>.</returns>
        Task<IEnumerable<T>> GetAllVocabulariesAsync();

        /// <summary>
        /// Retrieves a vocabulary entity by its unique identifier asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the vocabulary entity.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="VocabularyEntity"/>.</returns>
        Task<T> GetVocabularyByIdAsync(string id);

        /// <summary>
        /// Adds a new vocabulary entity to the database asynchronously.
        /// </summary>
        /// <param name="vocabulary">The vocabulary entity to add.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the added <see cref="VocabularyEntity"/>.</returns>
        Task<T> AddVocabularyAsync(Q vocabulary);

        /// <summary>
        /// Updates an existing vocabulary entity in the database asynchronously.
        /// </summary>
        /// <param name="vocabulary">The vocabulary entity with updated values.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated <see cref="VocabularyEntity"/>.</returns>
        Task<T> UpdateVocabularyAsync(Q vocabulary);

        /// <summary>
        /// Deletes a vocabulary entity from the database by its unique identifier asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the vocabulary entity to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteVocabularyAsync(string id);
    }
}