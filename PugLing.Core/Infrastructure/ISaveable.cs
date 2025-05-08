namespace PugLing.Core.Infrastructure;

public interface ISaveable<T>
{
    Task<T> SaveAsync(CancellationToken cancellationToken);

    bool HasUnsavedChanges { get; }
}