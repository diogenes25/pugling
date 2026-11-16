using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Pugling.Api.Errors;

namespace Pugling.Api.Tests;

/// <summary>
/// B-410: "every list in every request body answers a <c>null</c> element with 400" as an enumeration instead of a belief.
/// The guard lists every body parameter through the ApiExplorer, builds one value per body type in which every list holds a
/// built element and a <c>null</c>, runs the <see cref="NullElementGuardFilter"/> registered on that action, and expects a
/// model error at exactly the paths whose element type is declared non-nullable - no more, no fewer.
/// <para>
/// The walk is independent of the filter: it follows every non-framework type to any depth, so a list behind a type outside
/// the contracts assembly or beyond the filter's depth turns up as a missing path. <c>UnknownReferenceCodeTests</c> keeps the
/// HTTP cut-through (whether System.Text.Json lets <c>[null]</c> reach the filter at all), which this guard does not see.
/// </para>
/// </summary>
public class NullElementGuardTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // Self-protection against a green guard that saw nothing: measured on 2026-11-16 at 128 body parameters and 90 lists (B-410). A drop below these
    // floors means the enumeration or the walk broke, not that the API shrank by half.
    private const int MinBodyParameters = 100;
    private const int MinCheckedLists = 75;

    /// <summary>
    /// Shapes the filter deliberately does not look into, each with its reason. A contract that carries one turns the
    /// guard red: the exception is only true while no contract needs it.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> KnownBlindShapes = new Dictionary<string, string>
    {
        ["List<List<T>>"] = "NullElementGuardFilter does not look into a list directly inside a list - no contract has one.",
    };

    [Fact]
    public void Jede_Body_Liste_Meldet_Ein_Null_Element()
    {
        var bodies = BodyParameters();
        var fromBodyAttributes = bodies.Count(b => b.Parameter.ParameterInfo.GetCustomAttribute<FromBodyAttribute>() is not null);

        // AK 1: [ApiController] infers most bodies without the attribute, so counting the attribute would see a handful.
        Assert.True(bodies.Count > fromBodyAttributes,
            $"The guard found {bodies.Count} body parameters, no more than the {fromBodyAttributes} carrying [FromBody] - "
            + "it must enumerate through the ApiExplorer (BindingSource.Body), not through the attribute.");
        Assert.True(bodies.Count >= MinBodyParameters,
            $"The guard found only {bodies.Count} body parameters (floor {MinBodyParameters}) - the ApiExplorer enumeration broke.");

        var violations = new List<string>();
        var checkedLists = 0;
        foreach (var (action, parameter) in bodies)
        {
            var type = parameter.ParameterInfo.ParameterType;
            var where = $"{action.DisplayName} ({TypeName(type)})";
            var probe = new ProbeBuilder();
            var value = probe.BuildBody(parameter.ParameterInfo);

            violations.AddRange(probe.Unbuildable.Select(u => $"{where}: cannot build {u} - the guard would be blind there."));
            violations.AddRange(probe.NestedLists.Select(p =>
                $"{where}: '{p}' is a list inside a list, a shape the filter does not look into ({KnownBlindShapes["List<List<T>>"]})"));
            checkedLists += probe.Expected.Count + probe.Tolerated.Count;

            var filter = action.FilterDescriptors.Select(f => f.Filter).OfType<NullElementGuardFilter>().SingleOrDefault();
            if (filter is null)
            {
                violations.Add($"{where}: NullElementGuardFilter is not registered for this action.");
                continue;
            }

            var reported = RunFilter(filter, action, parameter, value);
            violations.AddRange(probe.Expected.Where(p => !reported.Contains(p))
                .Select(p => $"{where}: a null element at '{p}' raises no model error (expected 400 with this path)."));
            violations.AddRange(reported.Where(p => !probe.Expected.Contains(p))
                .Select(p => $"{where}: model error at '{p}', but its element type is nullable or it is no list element."));
        }

        Assert.True(violations.Count == 0,
            $"{violations.Count} null-element violation(s) across {bodies.Count} body parameters and {checkedLists} lists:\n"
            + string.Join("\n", violations.Distinct().Take(60)));
        Assert.True(checkedLists >= MinCheckedLists,
            $"The guard checked only {checkedLists} lists (floor {MinCheckedLists}) - the type walk stopped seeing lists.");
    }

    /// <summary>AK 2 is a statement about the filter's place too: it must run before the automatic 400 to join it.</summary>
    [Fact]
    public void Null_Element_Filter_Laeuft_Vor_Der_Automatischen_400()
    {
        var filter = BodyParameters()
            .SelectMany(b => b.Action.FilterDescriptors).Select(f => f.Filter).OfType<NullElementGuardFilter>().First();
        var automatic400 = new ModelStateInvalidFilter(
            factory.Services.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value, NullLogger.Instance).Order;
        Assert.True(filter.Order < automatic400,
            $"NullElementGuardFilter runs at order {filter.Order}, not before ModelStateInvalidFilter ({automatic400}) - its errors would miss the automatic 400.");
    }

    /// <summary>
    /// AK 3: no contract carries a <c>List&lt;T?&gt;</c> today, so the nullable case is pinned on a body that is itself a list,
    /// where the filter reads the element nullability from the parameter. Red if the filter ignores the declared nullability.
    /// </summary>
    [Theory]
    [InlineData(nameof(NullableElements), false)]
    [InlineData(nameof(NonNullableElements), true)]
    public void Nullbare_Elementliste_Meldet_Keinen_Fehler(string method, bool expectError)
    {
        var parameter = typeof(NullElementGuardTests).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static)!.GetParameters()[0];
        var descriptor = new ControllerParameterDescriptor
        {
            Name = parameter.Name!,
            ParameterType = parameter.ParameterType,
            ParameterInfo = parameter,
            BindingInfo = new BindingInfo { BindingSource = BindingSource.Body },
        };
        var action = new ControllerActionDescriptor { Parameters = [descriptor] };

        var reported = RunFilter(new NullElementGuardFilter(), action, descriptor, new List<string?> { "x", null });

        Assert.Equal(expectError ? ["[1]"] : [], reported.ToArray());
    }

    /// <summary>AK 4: red if the walk stops reporting a list directly inside a list, so the exception could go unnoticed.</summary>
    [Fact]
    public void Liste_In_Liste_Wird_Gemeldet()
    {
        var probe = new ProbeBuilder();
        probe.Build(typeof(NestedListProbe), "", 0);

        Assert.Equal(["Rows"], probe.NestedLists);
        Assert.Equal(["Tags[1]"], probe.Expected);
    }

    private static void NullableElements(List<string?> items) => _ = items;

    private static void NonNullableElements(List<string> items) => _ = items;

    private sealed record NestedListProbe(List<List<string>> Rows, List<string> Tags);

    private List<(ActionDescriptor Action, ControllerParameterDescriptor Parameter)> BodyParameters()
    {
        var provider = factory.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        return provider.ApiDescriptionGroups.Items
            .SelectMany(g => g.Items)
            .SelectMany(d => d.ParameterDescriptions
                .Where(p => p.Source == BindingSource.Body)
                .Select(p => (Action: d.ActionDescriptor, Parameter: p.ParameterDescriptor as ControllerParameterDescriptor)))
            .Where(b => b.Parameter is not null)
            .Select(b => (b.Action, b.Parameter!))
            .DistinctBy(b => (b.Action.Id, b.Item2.Name))
            .ToList();
    }

    private static HashSet<string> RunFilter(IActionFilter filter, ActionDescriptor action, ParameterDescriptor parameter, object? value)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), action, new ModelStateDictionary());
        var context = new ActionExecutingContext(actionContext, [], new Dictionary<string, object?> { [parameter.Name] = value }, new object());
        filter.OnActionExecuting(context);
        return context.ModelState.Where(e => e.Value is { Errors.Count: > 0 }).Select(e => e.Key).ToHashSet(StringComparer.Ordinal);
    }

    private static string TypeName(Type type) => type.IsGenericType
        ? $"{type.Name[..type.Name.IndexOf('`')]}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>"
        : type.Name;

    /// <summary>
    /// Builds one value per body type: every list holds a built element at [0] (so the walk continues inside) and a
    /// <c>null</c> at [1]. Records the paths where the filter must report and where it must stay silent.
    /// </summary>
    private sealed class ProbeBuilder
    {
        private readonly NullabilityInfoContext _nullability = new();
        private readonly HashSet<Type> _onStack = [];

        public List<string> Expected { get; } = [];
        public List<string> Tolerated { get; } = [];
        public List<string> NestedLists { get; } = [];
        public List<string> Unbuildable { get; } = [];

        public object? BuildBody(ParameterInfo parameter) =>
            BuildValue(parameter.ParameterType, _nullability.Create(parameter), "", 0);

        public object? Build(Type type, string path, int depth) => BuildValue(type, null, path, depth);

        private object? BuildValue(Type type, NullabilityInfo? info, string path, int depth)
        {
            if (type == typeof(string)) return "x";
            if (type.IsValueType) return Nullable.GetUnderlyingType(type) is null ? Activator.CreateInstance(type) : null;
            if (ElementTypeOf(type) is { } element) return BuildList(type, element, info, path, depth);
            if (IsFramework(type)) return null; // object, Uri, JsonNode …: no contract list can hide behind them.
            if (type.IsAbstract || type.IsInterface)
            {
                Unbuildable.Add($"'{path}' of abstract type {TypeName(type)}");
                return null;
            }
            return BuildObject(type, path, depth);
        }

        private object? BuildList(Type listType, Type element, NullabilityInfo? info, string path, int depth)
        {
            if (ElementTypeOf(element) is not null && element != typeof(string))
            {
                NestedLists.Add(path);
                return CreateList(listType, element, [], path);
            }

            var items = new List<object?> { BuildValue(element, null, $"{path}[0]", depth + 1) };
            if (!element.IsValueType)
            {
                items.Add(null);
                var elementInfo = info?.ElementType ?? (info?.GenericTypeArguments.Length == 1 ? info.GenericTypeArguments[0] : null);
                (elementInfo is { ReadState: NullabilityState.NotNull } ? Expected : Tolerated).Add($"{path}[1]");
            }
            return CreateList(listType, element, items, path);
        }

        private object? CreateList(Type listType, Type element, List<object?> items, string path)
        {
            if (listType.IsArray)
            {
                var array = Array.CreateInstance(element, items.Count);
                for (var i = 0; i < items.Count; i++) array.SetValue(items[i], i);
                return array;
            }
            var concrete = listType.IsInterface ? typeof(List<>).MakeGenericType(element) : listType;
            var collection = typeof(ICollection<>).MakeGenericType(element);
            if (!listType.IsAssignableFrom(concrete) || !collection.IsAssignableFrom(concrete) || concrete.GetConstructor(Type.EmptyTypes) is null)
            {
                Unbuildable.Add($"'{path}' of list type {TypeName(listType)}");
                return null;
            }
            var list = Activator.CreateInstance(concrete)!;
            var add = collection.GetMethod("Add")!;
            foreach (var item in items) add.Invoke(list, [item]);
            return list;
        }

        private object? BuildObject(Type type, string path, int depth)
        {
            // A type that contains itself is built once per branch; deeper, the member stays null.
            if (!_onStack.Add(type)) return null;
            var node = RuntimeHelpers.GetUninitializedObject(type);
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead || property.GetIndexParameters().Length > 0) continue;
                var memberPath = path.Length == 0 ? property.Name : $"{path}.{property.Name}";
                var value = BuildValue(property.PropertyType, _nullability.Create(property), memberPath, depth + 1);
                if (property.SetMethod is not null) property.SetValue(node, value);
                else if (type.GetField($"<{property.Name}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance) is { } field)
                    field.SetValue(node, value);
                else if (ElementTypeOf(property.PropertyType) is not null)
                    Unbuildable.Add($"'{memberPath}' (a computed list without setter or backing field)");
            }
            _onStack.Remove(type);
            return node;
        }

        // Any IEnumerable except string and dictionaries is a list to the filter; returns its element type.
        private static Type? ElementTypeOf(Type type)
        {
            if (type == typeof(string) || typeof(IDictionary).IsAssignableFrom(type) || !typeof(IEnumerable).IsAssignableFrom(type)) return null;
            if (type.IsArray) return type.GetElementType();
            var enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                ? type
                : type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            return enumerable?.GetGenericArguments()[0] ?? typeof(object);
        }

        private static bool IsFramework(Type type) =>
            type.Namespace is { } ns && (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal)
                || ns.StartsWith("Microsoft.", StringComparison.Ordinal));
    }
}
