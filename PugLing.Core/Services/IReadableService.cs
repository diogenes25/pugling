namespace PugLing.Core.Services;

public interface IReadableService<T>
{
    Task<T> GetById(string srclang, string targetlang, string id);
}