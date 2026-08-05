using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Pugling.Api.Errors;

/// <summary>
/// Provides the allowed values of enums – shared by two places: (1) model validation translates
/// a raw System.Text.Json conversion error into a helpful "allowed values" message,
/// (2) the OpenAPI schema transformer writes the same values into the description, so Swagger/Scalar
/// show the permitted values.
/// </summary>
public static class EnumSchemaHelp
{
    /// <summary>The allowed values of an enum, as the <c>JsonStringEnumConverter</c> expects them (by name).</summary>
    public static string[] AllowedValues(Type enumType) => Enum.GetNames(enumType);

    /// <summary>
    /// The actually <b>required</b> fields of a DTO as JSON names. The .NET OpenAPI generator marks
    /// every positional record constructor parameter as <c>required</c> – even nullable (optional) ones like
    /// <c>string?</c>/<c>TEnum?</c>. We recompute the list based on <b>nullability</b>: non-nullable
    /// reference types and non-nullable value types are required, everything nullable is optional. Members
    /// explicitly declared as <c>required</c>/<c>[JsonRequired]</c> remain required (even when nullable).
    /// </summary>
    public static IReadOnlyList<string> RequiredJsonPropertyNames(JsonTypeInfo typeInfo)
    {
        var nullability = new NullabilityInfoContext();
        var required = new List<string>();
        foreach (var property in typeInfo.Properties)
        {
            // Extension-data properties (`[JsonExtensionData]`, e.g. ProblemDetails.Extensions) are a
            // catch-all for arbitrary extra keys, not a named field - the generator itself already leaves
            // them out of `properties`. A get-only reference type like this one is otherwise non-nullable
            // and would land in `required` without ever being described (B-56): the document would demand a
            // field it does not itself know.
            if (property.IsExtensionData) continue;
            if (property.IsRequired || IsNonNullable(property, nullability))
                required.Add(property.Name);
        }
        return required;
    }

    private static bool IsNonNullable(JsonPropertyInfo property, NullabilityInfoContext nullability)
    {
        // Value type: required unless Nullable<T>. Reference type: decided by the NRT annotation.
        // For get-only/expression-bodied members WriteState is Unknown - so evaluate ReadState OR WriteState
        // (otherwise non-nullable, read-only reference properties would wrongly count as optional).
        // Treat serialized fields ([JsonInclude]) the same way, not only properties.
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
    /// Determines the associated enum type for a failed JSON field – <c>null</c> if the field is
    /// not an enum (e.g. "string instead of int"). System.Text.Json's error message only names the DTO type,
    /// not the enum type; the only reliable thing is the JSON path (model-state key, e.g. <c>$.unitType</c>).
    /// The path is therefore resolved against the action's parameter types (incl. nested objects
    /// and lists), which yields the matching enum type.
    /// </summary>
    public static Type? EnumTypeForJsonPath(IEnumerable<Type> rootTypes, string jsonPath)
    {
        foreach (var root in rootTypes)
            if (Resolve(root, jsonPath) is { IsEnum: true } enumType)
                return enumType;
        return null;
    }

    // Walks the path segments (after the leading "$") and descends the type graph property by property.
    private static Type? Resolve(Type rootType, string jsonPath)
    {
        if (!jsonPath.StartsWith('$')) return null;
        var current = rootType;
        foreach (var raw in jsonPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            // Strip the array indexer (e.g. "gaps[0]" → property "gaps", then the element type).
            var bracket = raw.IndexOf('[');
            var name = bracket >= 0 ? raw[..bracket] : raw;

            // Root ("$", also with an indexer "$[0]"): no property; only a possible element descent below.
            if (name is not "$" && name.Length > 0)
            {
                // A dictionary segment is a key → descend into the value type (do not look it up as a property).
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

    // Property lookup as System.Text.Json does it in web mode (PropertyNameCaseInsensitive): an explicit
    // [JsonPropertyName] first (case-insensitive), otherwise the CLR name case-insensitively.
    private static PropertyInfo? FindProperty(Type type, string jsonName)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return Array.Find(properties, p =>
                   string.Equals(p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name, jsonName, StringComparison.OrdinalIgnoreCase))
               ?? Array.Find(properties, p => string.Equals(p.Name, jsonName, StringComparison.OrdinalIgnoreCase));
    }

    // Value type of a Dictionary<string, V> (or IDictionary<string, V>) - null otherwise.
    private static Type? DictionaryValueType(Type type) =>
        type.GetInterfaces().Prepend(type)
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            ?.GetGenericArguments()[1];

    // Element type of a collection (T[] or IEnumerable<T>) - string excluded (it is an IEnumerable<char> itself).
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
