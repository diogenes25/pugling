using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Pugling.Api.Errors;

/// <summary>
/// Reports a <c>null</c> element in a list whose element type is declared non-nullable (B-397) as a model error, so the
/// request ends as <c>400 validation_error</c> with the path in <c>errors</c> - the same answer a <c>null</c> member gets.
/// </summary>
/// <remarks>
/// The implicit <c>[Required]</c> of non-nullable members covers members only; System.Text.Json accepts <c>[null]</c> for a
/// <c>List&lt;T&gt;</c>, and model validation never checks elements. Every handler then dereferenced the element and threw.
/// One rule for every body instead of a check per controller: the contract already states "never null", the filter only
/// enforces it. It runs before <c>ModelStateInvalidFilter</c> (order -2000), so its errors join the automatic 400.
/// A list directly inside a list (<c>List&lt;List&lt;T&gt;&gt;</c>) is not looked into - no contract has one.
/// </remarks>
public sealed class NullElementGuardFilter : IActionFilter, IOrderedFilter
{
    // Deep enough for every contract graph; a guard against cycles, not a limit anyone reaches. A list element costs two
    // levels (the list, then its members), so through lists the effective object depth is about eight.
    private const int MaxDepth = 16;

    private static readonly Assembly ContractsAssembly = typeof(ClozeConfig).Assembly;
    private static readonly ConcurrentDictionary<Type, Member[]> Members = new();
    private static readonly ConcurrentDictionary<ParameterInfo, bool> BodyElementNotNull = new();

    /// <inheritdoc />
    public int Order => -2001;

    /// <inheritdoc />
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            if (parameter.BindingInfo?.BindingSource != BindingSource.Body) continue;
            if (!context.ActionArguments.TryGetValue(parameter.Name, out var body) || body is null) continue;
            // A body that is itself a list (the batch endpoints) takes its element nullability from the parameter.
            var elementNotNull = parameter is ControllerParameterDescriptor { ParameterInfo: { } info }
                && BodyElementNotNull.GetOrAdd(info, static p => ElementNotNull(new NullabilityInfoContext().Create(p)));
            Visit(body, "", elementNotNull, context.ModelState, 0);
        }
    }

    /// <inheritdoc />
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    // One value at one path: a list is checked element by element, a contract object member by member.
    private static void Visit(object value, string path, bool elementNotNull, ModelStateDictionary modelState, int depth)
    {
        if (depth > MaxDepth) return;
        if (value is IEnumerable list and not string and not IDictionary)
        {
            var index = 0;
            foreach (var element in list)
            {
                var elementPath = $"{path}[{index++}]";
                if (element is null)
                {
                    if (elementNotNull) modelState.AddModelError(elementPath, $"The element {elementPath} must not be null.");
                    continue;
                }
                if (IsContract(element.GetType())) VisitMembers(element, elementPath, modelState, depth + 1);
            }
            return;
        }
        if (IsContract(value.GetType())) VisitMembers(value, path, modelState, depth);
    }

    private static void VisitMembers(object node, string path, ModelStateDictionary modelState, int depth)
    {
        foreach (var member in MembersOf(node.GetType()))
        {
            if (member.Property.GetValue(node) is not { } value) continue;
            var memberPath = path.Length == 0 ? member.Property.Name : $"{path}.{member.Property.Name}";
            Visit(value, memberPath, member.ElementNotNull, modelState, depth + 1);
        }
    }

    private static bool IsContract(Type type) => type.Assembly == ContractsAssembly && !type.IsEnum && !type.IsPrimitive;

    private static Member[] MembersOf(Type type) => Members.GetOrAdd(type, static t =>
    {
        // NullabilityInfoContext is not thread-safe - one per type inside the cache factory.
        var nullability = new NullabilityInfoContext();
        return t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .Select(p => new Member(p, ElementNotNull(nullability.Create(p))))
            .ToArray();
    });

    // Arrays carry the element's nullability in ElementType, generic lists in their single type argument.
    private static bool ElementNotNull(NullabilityInfo info) =>
        (info.ElementType ?? (info.GenericTypeArguments.Length == 1 ? info.GenericTypeArguments[0] : null))
            is { ReadState: NullabilityState.NotNull } element
        && !element.Type.IsValueType;

    private sealed record Member(PropertyInfo Property, bool ElementNotNull);
}
