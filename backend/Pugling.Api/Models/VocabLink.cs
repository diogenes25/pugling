using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pugling.Api.Models;

/// <summary>
/// Baut den HATEOAS-Selbstlink auf einen Vokabel-Store-Eintrag. Eine Stelle für den Pfad, damit
/// alle Übungstypen denselben <c>_self</c> liefern. Der Pfad ist bis zur Publikation stabil (v1);
/// bewusst als String (kein <c>LinkGenerator</c>), da der Link rein aus der ID ableitbar ist.
/// </summary>
public static class VocabLink
{
    /// <summary>Basis-Pfad des Vokabel-Store-Eintrags.</summary>
    public const string Path = "/api/v1/creator/vocabulary/";

    /// <summary>Selbstlink zur ID; <c>null</c> für fehlende/unbekannte IDs (0 = Alt-Referenz ohne aufgelöste ID).</summary>
    public static string? Self(int? id) => id is null or 0 ? null : Path + id;
}

/// <summary>
/// Liest <see cref="VocabRef"/> abwärtskompatibel: sowohl die Alt-Form (nackter String = Store-Key, ID noch
/// unbekannt → 0, der Resolver löst per Key auf) als auch die neue Objekt-Form <c>{ vocabularyId, key, _self }</c>.
/// So bleiben bestehende <c>ConfigJson</c>-Zeilen ohne Daten-Migration lesbar. Beim Schreiben wird stets die
/// Objekt-Form ausgegeben; <c>_self</c> nur, wenn (für Antworten) gesetzt – gespeicherte Configs bleiben linkfrei.
/// </summary>
public sealed class VocabRefJsonConverter : JsonConverter<VocabRef>
{
    public override VocabRef Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Alt-Form: nackter String = Store-Key (ID noch unbekannt → 0; Resolver löst per Key auf).
        if (reader.TokenType == JsonTokenType.String)
            return new VocabRef(0, reader.GetString());

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("VocabRef erwartet einen String (Legacy-Key) oder ein Objekt.");

        var vocabularyId = 0;
        string? key = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return new VocabRef(vocabularyId, key);
            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;
            var prop = reader.GetString();
            reader.Read();
            if (string.Equals(prop, "vocabularyId", StringComparison.OrdinalIgnoreCase))
                vocabularyId = reader.TokenType == JsonTokenType.Number ? reader.GetInt32() : 0;
            else if (string.Equals(prop, "key", StringComparison.OrdinalIgnoreCase))
                key = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
            else
                reader.Skip(); // abgeleitete/unbekannte Felder (z. B. _self) ignorieren
        }
        throw new JsonException("Unerwartetes Ende beim Lesen eines VocabRef.");
    }

    public override void Write(Utf8JsonWriter writer, VocabRef value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("vocabularyId", value.VocabularyId);
        if (value.Key is not null)
            writer.WriteString("key", value.Key);
        // _self ist rein abgeleitet: nur in Antworten gesetzt, in gespeicherten Configs null → nicht ausgegeben.
        if (value.Self is not null)
            writer.WriteString("_self", value.Self);
        writer.WriteEndObject();
    }
}
