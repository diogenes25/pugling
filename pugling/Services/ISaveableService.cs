namespace pugling.Services
{
    public interface ISaveableService<T>
    {
        Task<T> SaveCreateAsync(T saveObj, CancellationToken cancellationToken);

        Task<T> SaveUpdateAsync(T saveObj, CancellationToken cancellationToken);

        Task<T> UpdateAsync(T saveObj, IEnumerable<string> updatedProperties, CancellationToken cancellationToken);
    }
}