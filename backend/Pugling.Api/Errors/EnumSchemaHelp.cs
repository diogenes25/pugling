using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Pugling.Api.Errors;

/// <summary>
/// Liefert die erlaubten Werte von Enums – geteilt von zwei Stellen: (1) die Modell-Validierung übersetzt
/// einen rohen System.Text.Json-Konvertierungsfehler in eine hilfreiche „allowed values"-Meldung,
/// (2) der OpenAPI-Schema-Transformer schreibt dieselben Werte in die Beschreibung, damit Swagger/Scalar
/// die zulässigen Werte ausweisen.
/// </summary>
public static class EnumSchemaHelp
{
    /// <summary>Die erlaubten Werte eines Enums, so wie der <c>JsonStringEnumConverter</c> sie erwartet (per Name).</summary>
    public static string[] AllowedValues(Type enumType) => Enum.GetNames(enumType);

    /// <summary>
    /// Die tatsächlich <b>pflicht</b>-Felder eines DTOs als JSON-Namen. Der .NET-OpenAPI-Generator markiert
    /// jeden Positional-Record-Konstruktorparameter als <c>required</c> – auch nullbare (optionale) wie
    /// <c>string?</c>/<c>TEnum?</c>. Wir rechnen die Liste anhand der <b>Nullbarkeit</b> neu: nicht-nullbare
    /// Referenztypen und nicht-nullbare Werttypen sind Pflicht, alles Nullbare ist optional. Explizit als
    /// <c>required</c>/<c>[JsonRequired]</c> deklarierte Member bleiben Pflicht (auch wenn nullbar).
    /// </summary>
    public static IReadOnlyList<string> RequiredJsonPropertyNames(JsonTypeInfo typeInfo)
    {
        var nullability = new NullabilityInfoContext();
        var required = new List<string>();
        foreach (var property in typeInfo.Properties)
        {
            if (property.IsRequired || IsNonNullable(property, nullability))
                required.Add(property.Name);
        }
        return required;
    }

    private static bool IsNonNullable(JsonPropertyInfo property, NullabilityInfoContext nullability)
    {
        // Werttyp: Pflicht, sofern kein Nullable<T>. Referenztyp: über die NRT-Annotation entscheiden.
        // Bei get-only/expression-bodied Membern ist WriteState = Unknown – daher ReadState ODER WriteState
        // werten (sonst gälten nicht-nullbare, nur lesbare Referenz-Properties fälschlich als optional).
        // Serialisierte Felder ([JsonInclude]) genauso behandeln, nicht nur Properties.
        return property.AttributeProvider switch
        {
            PropertyInfo p => Nullable.GetUnderlyingType(p.PropertyType) is null
                && (p.PropertyType.IsValueType || IsNotNull(nullability.Create(p))),
            FieldInfo f => Nullable.GetUnderlyingType(f.FieldType) is null
                && (f.FieldType.IsValueType || IsNotNull(nullability.Create(f))),
            _ => false,
        };
    }

    private static bool IsNotNull(NullabilityInfo info) =>
        info.ReadState == NullabilityState.NotNull || info.WriteState == NullabilityState.NotNull;

    /// <summary>
    /// Ermittelt zu einem fehlgeschlagenen JSON-Feld den zugehörigen Enum-Typ – <c>null</c>, wenn das Feld
    /// kein Enum ist (z. B. „String statt int"). System.Text.Json nennt in der Fehlermeldung nur den DTO-Typ,
    /// nicht den Enum-Typ; verlässlich ist allein der JSON-Pfad (Model-State-Key, z. B. <c>$.unitType</c>).
    /// Der Pfad wird daher gegen die Parameter-Typen der Action aufgelöst (inkl. verschachtelter Objekte
    /// und Listen), was den passenden Enum-Typ liefert.
    /// </summary>
    public static Type? EnumTypeForJsonPath(IEnumerable<Type> rootTypes, string jsonPath)
    {
        foreach (var root in rootTypes)
            if (Resolve(root, jsonPath) is { IsEnum: true } enumType)
                return enumType;
        return null;
    }

    // Läuft die Pfadsegmente (nach dem führenden „$") ab und steigt Property für Property in den Typgraphen.
    private static Type? Resolve(Type rootType, string jsonPath)
    {
        if (!jsonPath.StartsWith('$')) return null;
        var current = rootType;
        foreach (var raw in jsonPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            // Array-Indexer abtrennen (z. B. „gaps[0]" → Property „gaps", danach Elementtyp).
            var bracket = raw.IndexOf('[');
            var name = bracket >= 0 ? raw[..bracket] : raw;

            // Wurzel („$", auch mit Indexer „$[0]"): keine Property; nur ein evtl. Element-Abstieg unten.
            if (name is not "$" && name.Length > 0)
            {
                // Ein Dictionary-Segment ist ein Schlüssel → auf den Werttyp absteigen (nicht als Property suchen).
                if (DictionaryValueType(current) is { } dictValue)
                    current = dictValue;
                else if (FindProperty(current, name) is { } property)
                    current = property.PropertyType;
                else
                    return null;
            }

            if (bracket >= 0 && ElementType(current) is { } element) current = element;
            current = Nullable.GetUnderlyingType(current) ?? current;
        }
        return current;
    }

    // Property-Suche wie System.Text.Json im Web-Modus (PropertyNameCaseInsensitive): explizites
    // [JsonPropertyName] zuerst (case-insensitiv), sonst der CLR-Name case-insensitiv.
    private static PropertyInfo? FindProperty(Type type, string jsonName)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return Array.Find(properties, p =>
                   string.Equals(p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name, jsonName, StringComparison.OrdinalIgnoreCase))
               ?? Array.Find(properties, p => string.Equals(p.Name, jsonName, StringComparison.OrdinalIgnoreCase));
    }

    // Werttyp eines Dictionary&lt;string, V&gt; (bzw. IDictionary&lt;string, V&gt;) – sonst null.
    private static Type? DictionaryValueType(Type type) =>
        type.GetInterfaces().Prepend(type)
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            ?.GetGenericArguments()[1];

    // Elementtyp einer Sammlung (T[] bzw. IEnumerable<T>) – string ausgenommen (ist selbst IEnumerable<char>).
    private static Type? ElementType(Type type)
    {
        if (type == typeof(string)) return null;
        if (type.IsArray) return type.GetElementType();
        if (!typeof(IEnumerable).IsAssignableFrom(type)) return null;
        return type.GetInterfaces().Prepend(type)
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];
    }
}
