using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Kleine Assertions für boolesche JSON-Properties. Schlagen mit sprechender Meldung fehl (Property-Name
/// + tatsächliches JSON) statt mit dem nackten „Assert.True() Failure", damit ein roter Test sofort zeigt,
/// welches Flag im welchem Payload nicht stimmte.
/// </summary>
internal static class JsonAssert
{
    /// <summary>Erwartet, dass die bool-Property <paramref name="property"/> von <paramref name="el"/> true ist.</summary>
    public static void True(JsonElement el, string property) =>
        Assert.True(el.GetProperty(property).GetBoolean(), $"'{property}' sollte true sein – JSON: {el}");

    /// <summary>Erwartet, dass die bool-Property <paramref name="property"/> von <paramref name="el"/> false ist.</summary>
    public static void False(JsonElement el, string property) =>
        Assert.False(el.GetProperty(property).GetBoolean(), $"'{property}' sollte false sein – JSON: {el}");
}
