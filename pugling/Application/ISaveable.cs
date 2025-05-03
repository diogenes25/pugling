namespace pugling.Application
{
    public interface ISaveable<T>
    {
        Task<T> SaveAsync(CancellationToken cancellationToken);

        bool HasUnsavedChanges { get; }
    }
}