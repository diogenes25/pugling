using System.Collections;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;

namespace Pugling.Api.Tests;

/// <summary>
/// Builds a <b>just barely valid</b> JSON payload from a contract DTO – the supplier for the
/// ownership matrix test (<see cref="OwnershipMatrixTests"/>).
/// <para>
/// Why this is needed: <c>ChildOwnershipFilter</c>/<c>PlanOwnershipFilter</c> are
/// <c>IAsyncActionFilter</c> and thus run <b>after</b> model binding – and after the
/// <c>ModelStateInvalidFilter</c> (order −2000). A <c>POST</c> with an empty body would therefore get
/// <b>400</b> before the ownership check even runs, and the matrix could not say anything about
/// write actions. Only a bindable body turns the 403/404 into a meaningful result.
/// </para>
/// <para>
/// Only the <b>required fields</b> are filled: optional ones (nullable or with a default value) are
/// left out – that keeps the payload small and sidesteps validation attributes on fields nobody needs.
/// The payload does not need to make business sense and does not have to: it just must not fail
/// binding.
/// </para>
/// </summary>
internal static class SampleJson
{
    private static readonly NullabilityInfoContext Nullability = new();

    /// <summary>
    /// Payload for a DTO type; <c>null</c> if none can be built (e.g. file uploads).
    /// </summary>
    public static JsonNode? ForType(Type type) => Build(type, depth: 0);

    private static JsonNode? Build(Type type, int depth)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string)) return JsonValue.Create("x");
        if (underlying == typeof(bool)) return JsonValue.Create(false);
        if (underlying == typeof(Guid)) return JsonValue.Create(Guid.Empty.ToString());
        if (underlying.IsEnum) return JsonValue.Create(Enum.GetNames(underlying).FirstOrDefault() ?? "");
        // Date types arrive as ISO strings; a fixed value keeps the payload free of the wall clock.
        if (underlying == typeof(DateOnly)) return JsonValue.Create("2026-01-01");
        if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset)) return JsonValue.Create("2026-01-01T00:00:00Z");
        if (underlying == typeof(TimeOnly) || underlying == typeof(TimeSpan)) return JsonValue.Create("12:00:00");
        if (underlying.IsPrimitive || underlying == typeof(decimal)) return JsonValue.Create(1);

        // A file upload cannot be built as JSON - the caller has to recognize that.
        if (typeof(IFormFile).IsAssignableFrom(underlying) || typeof(IFormFileCollection).IsAssignableFrom(underlying))
            return null;

        // Collections empty: a filled list would drag the validation of its elements along, and all that is
        // needed here is a bindable body.
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
    /// Is the field allowed to be missing? A default value or a nullable annotation means "not specified" –
    /// and that is exactly the PATCH semantics in this project, so it belongs left out.
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
