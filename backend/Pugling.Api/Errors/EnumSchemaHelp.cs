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
        if (property.AttributeProvider is not PropertyInfo member) return false;
        // Werttyp: Pflicht, sofern kein Nullable<T>. Referenztyp: über die NRT-Annotation entscheiden.
        return Nullable.GetUnderlyingType(member.PropertyType) is null
            && (member.PropertyType.IsValueType
                || nullability.Create(member).WriteState == NullabilityState.NotNull);
    }

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
            if (raw == "$") continue;
            // Array-Indexer abtrennen (z. B. „gaps[0]" → Property „gaps", danach Elementtyp).
            var bracket = raw.IndexOf('[');
            var name = bracket >= 0 ? raw[..bracket] : raw;

            var property = FindProperty(current, name);
            if (property is null) return null;
            current = property.PropertyType;
            if (bracket >= 0 && ElementType(current) is { } element) current = element;
            current = Nullable.GetUnderlyingType(current) ?? current;
        }
        return current;
    }

    // Property-Suche wie System.Text.Json im Web-Modus: explizites [JsonPropertyName] zuerst, sonst der
    // CLR-Name case-insensitiv (der Converter matcht camelCase-JSON gegen PascalCase-Properties).
    private static PropertyInfo? FindProperty(Type type, string jsonName)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return Array.Find(properties, p =>
                   p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == jsonName)
               ?? Array.Find(properties, p => string.Equals(p.Name, jsonName, StringComparison.OrdinalIgnoreCase));
    }

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
