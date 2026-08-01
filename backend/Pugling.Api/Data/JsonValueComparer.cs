using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Pugling.Api.Data;

/// <summary>
/// Value comparer for JSON columns: it compares, hashes and copies through JSON serialization. That makes EF
/// detect changes to converted lists/objects correctly EVEN on in-place mutation (not only on reassignment)
/// and take a deep copy for the snapshot. It closes the well-known pitfall of the missing value comparers for
/// the JSON columns (Gaps/WordBank/StageSchedule/Noun/Verb …). Purely model metadata – no schema change,
/// no migration.
/// </summary>
public static class JsonValueComparer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>A deep, serialization-based comparer for the JSON column type <typeparamref name="T"/>.</summary>
    public static ValueComparer<T> For<T>() => new(
        (a, b) => JsonSerializer.Serialize(a, Options) == JsonSerializer.Serialize(b, Options),
        v => v == null ? 0 : JsonSerializer.Serialize(v, Options).GetHashCode(),
        v => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, Options), Options)!);
}
