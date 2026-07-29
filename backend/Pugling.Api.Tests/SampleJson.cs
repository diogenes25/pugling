using System.Collections;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;

namespace Pugling.Api.Tests;

/// <summary>
/// Baut aus einem Vertrags-DTO eine <b>gerade eben gültige</b> JSON-Nutzlast – der Zulieferer des
/// Ownership-Matrix-Tests (<see cref="OwnershipMatrixTests"/>).
/// <para>
/// Warum das nötig ist: <c>ChildOwnershipFilter</c>/<c>PlanOwnershipFilter</c> sind
/// <c>IAsyncActionFilter</c> und laufen damit <b>nach</b> der Modellbindung – und nach dem
/// <c>ModelStateInvalidFilter</c> (Order −2000). Ein <c>POST</c> mit leerem Rumpf bekäme also
/// <b>400</b>, bevor die Eigentumsprüfung überhaupt dran ist, und die Matrix könnte für schreibende
/// Actions nichts aussagen. Erst ein bindbarer Rumpf macht den 403/404 zur Aussage.
/// </para>
/// <para>
/// Gefüllt werden nur die <b>Pflichtfelder</b>: optionale (nullable oder mit Vorgabewert) bleiben weg –
/// das hält die Nutzlast klein und umgeht Validierungsattribute auf Feldern, die niemand braucht.
/// Fachlich sinnvoll ist die Nutzlast nicht und muss es nicht sein: sie darf nur nicht an der Bindung
/// scheitern.
/// </para>
/// </summary>
internal static class SampleJson
{
    private static readonly NullabilityInfoContext Nullability = new();

    /// <summary>
    /// Nutzlast für einen DTO-Typ; <c>null</c>, wenn sich keine bauen lässt (z. B. Datei-Uploads).
    /// </summary>
    public static JsonNode? ForType(Type type) => Build(type, depth: 0);

    private static JsonNode? Build(Type type, int depth)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string)) return JsonValue.Create("x");
        if (underlying == typeof(bool)) return JsonValue.Create(false);
        if (underlying == typeof(Guid)) return JsonValue.Create(Guid.Empty.ToString());
        if (underlying.IsEnum) return JsonValue.Create(Enum.GetNames(underlying).FirstOrDefault() ?? "");
        // Datumstypen kommen als ISO-Zeichenkette; ein fester Wert hält die Nutzlast von der Wanduhr frei.
        if (underlying == typeof(DateOnly)) return JsonValue.Create("2026-01-01");
        if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset)) return JsonValue.Create("2026-01-01T00:00:00Z");
        if (underlying == typeof(TimeOnly) || underlying == typeof(TimeSpan)) return JsonValue.Create("12:00:00");
        if (underlying.IsPrimitive || underlying == typeof(decimal)) return JsonValue.Create(1);

        // Ein Datei-Upload lässt sich nicht als JSON bauen – der Aufrufer muss das erkennen.
        if (typeof(IFormFile).IsAssignableFrom(underlying) || typeof(IFormFileCollection).IsAssignableFrom(underlying))
            return null;

        // Sammlungen leer: eine gefüllte Liste zöge die Validierung ihrer Elemente nach sich, und
        // gebraucht wird hier nur ein bindbarer Rumpf.
        if (underlying != typeof(string) && typeof(IEnumerable).IsAssignableFrom(underlying))
            return new JsonArray();

        if (depth >= 3) return new JsonObject(); // Schutz gegen zyklische Vertragsgraphen.

        var obj = new JsonObject();
        var ctor = underlying.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();
        if (ctor is not null && ctor.GetParameters().Length > 0)
        {
            foreach (var p in ctor.GetParameters())
                if (!IsOptional(p) && p.Name is { } name)
                    obj[Camel(name)] = Build(p.ParameterType, depth + 1);
            return obj;
        }

        foreach (var prop in underlying.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            if (prop.CanWrite && !IsOptional(prop))
                obj[Camel(prop.Name)] = Build(prop.PropertyType, depth + 1);
        return obj;
    }

    /// <summary>
    /// Darf das Feld fehlen? Ein Vorgabewert oder eine nullable Annotation heißt „nicht angegeben" – und
    /// genau das ist in diesem Projekt die PATCH-Semantik, also gehört es weggelassen.
    /// </summary>
    private static bool IsOptional(ParameterInfo p) =>
        p.HasDefaultValue
        || Nullable.GetUnderlyingType(p.ParameterType) is not null
        || Nullability.Create(p).WriteState == NullabilityState.Nullable;

    private static bool IsOptional(PropertyInfo p) =>
        Nullable.GetUnderlyingType(p.PropertyType) is not null
        || Nullability.Create(p).WriteState == NullabilityState.Nullable;

    private static string Camel(string name) =>
        name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
