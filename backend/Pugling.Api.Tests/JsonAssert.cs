using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Small assertions for boolean JSON properties. Fail with a descriptive message (property name
/// + actual JSON) instead of the bare "Assert.True() Failure", so a red test immediately shows
/// which flag was wrong in which payload.
/// </summary>
internal static class JsonAssert
{
    /// <summary>Expects that the bool property <paramref name="property"/> of <paramref name="el"/> is true.</summary>
    public static void True(JsonElement el, string property) =>
        Assert.True(el.GetProperty(property).GetBoolean(), $"'{property}' sollte true sein – JSON: {el}");

    /// <summary>Expects that the bool property <paramref name="property"/> of <paramref name="el"/> is false.</summary>
    public static void False(JsonElement el, string property) =>
        Assert.False(el.GetProperty(property).GetBoolean(), $"'{property}' sollte false sein – JSON: {el}");

    /// <summary>
    /// Expects that <paramref name="property"/> exists and is JSON <c>null</c>. Deliberately not
    /// "missing or null": a facet the contract promises must be present and empty, not absent.
    /// </summary>
    public static void Null(JsonElement el, string property) =>
        Assert.Equal(JsonValueKind.Null, el.GetProperty(property).ValueKind);

    /// <summary>Expects that <paramref name="property"/> exists and carries a value.</summary>
    public static void NotNull(JsonElement el, string property) =>
        Assert.NotEqual(JsonValueKind.Null, el.GetProperty(property).ValueKind);
}
