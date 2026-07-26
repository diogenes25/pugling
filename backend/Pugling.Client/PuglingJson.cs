using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pugling.Client;

/// <summary>
/// Die eine Serialisierungs-Einstellung des Clients. Sie muss der Server-Seite entsprechen:
/// Web-Defaults (camelCase) <b>plus</b> <see cref="JsonStringEnumConverter"/> – die API gibt Enums als
/// String aus (<c>"Gymnasium"</c>, <c>"Owner"</c>). Ohne diesen Converter brechen still alle Enum-Felder.
/// </summary>
public static class PuglingJson
{
    /// <summary>Gemeinsame Optionen für Serialisierung und Deserialisierung.</summary>
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
