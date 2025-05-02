namespace pugling.Services
{
    public interface ISaveableService<T>
    {
        Task<T> SaveAsync(T saveObj, CancellationToken cancellationToken);

        Task<T> UpdateAsync(T saveObj, IEnumerable<string> updatedProperties, CancellationToken cancellationToken);
    }
}