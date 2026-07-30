using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pugling.Client;

/// <summary>
/// The client's one serialization setting. It must match the server side:
/// web defaults (camelCase) <b>plus</b> <see cref="JsonStringEnumConverter"/> – the API outputs enums as
/// strings (<c>"Gymnasium"</c>, <c>"Owner"</c>). Without this converter, all enum fields break silently.
/// </summary>
public static class PuglingJson
{
    /// <summary>Shared options for serialization and deserialization.</summary>
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
