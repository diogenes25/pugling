using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pugling.Contracts;

/// <summary>
/// Reads <see cref="VocabRef"/> backward-compatibly: both the legacy form (bare string = store key, ID
/// still unknown → 0, the resolver resolves by key) and the new object form <c>{ vocabularyId, key, _self }</c>.
/// This keeps existing <c>ConfigJson</c> rows readable without a data migration. When writing, the
/// object form is always emitted; <c>_self</c> only when set (for responses) – stored configs remain link-free.
/// </summary>
public sealed class VocabRefJsonConverter : JsonConverter<VocabRef>
{
    /// <summary>Reads both forms: bare string (legacy form, key) or object <c>{ vocabularyId, key, _self }</c>.</summary>
    public override VocabRef Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Legacy form: a bare string is the store key (ID still unknown → 0; the resolver resolves by key).
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
                reader.Skip(); // ignore derived/unknown fields (e.g. _self)
        }
        throw new JsonException("Unerwartetes Ende beim Lesen eines VocabRef.");
    }

    /// <summary>Always writes the object form; <c>_self</c> only when set (stored configs remain link-free).</summary>
    public override void Write(Utf8JsonWriter writer, VocabRef value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("vocabularyId", value.VocabularyId);
        if (value.Key is not null)
            writer.WriteString("key", value.Key);
        // _self is purely derived: set in responses only, null in stored configs → not written out.
        if (value.Self is not null)
            writer.WriteString("_self", value.Self);
        writer.WriteEndObject();
    }
}
