namespace pugling.Application.Vocabularies;

public interface ISaveable<T>
{
    Task<T> SaveAsync(CancellationToken cancellationToken);

    bool HasUnsavedChanges { get; }
}