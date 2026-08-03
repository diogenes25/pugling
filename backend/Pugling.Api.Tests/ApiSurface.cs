using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Pugling.Api.Tests;

/// <summary>
/// The API surface via reflection: controllers, actions, route templates. Shared foundation of the
/// reflective guards (<see cref="ConventionGuardTests"/>, <see cref="OwnershipMatrixTests"/>,
/// <see cref="EndpointCoverage"/>) – the same definition of "what is an action" for all of them, otherwise
/// three tests guard three different surfaces and nobody notices.
/// </summary>
internal static class ApiSurface
{
    /// <summary>All instantiable controllers of the API.</summary>
    public static IEnumerable<Type> Controllers() =>
        typeof(Program).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

    /// <summary>The public actions of a controller (declared, not inherited).</summary>
    public static IEnumerable<MethodInfo> Actions(Type controller) =>
        controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName && m.GetCustomAttributes<HttpMethodAttribute>().Any());

    /// <summary>
    /// The public actions of a controller <b>including inherited ones</b>. Use this where a guard judges what
    /// an endpoint returns or who may call it: the thirteen exercise controllers declare no CRUD action of
    /// their own, they inherit <c>List/Get/Create/Update</c> from <c>ExerciseControllerBase</c> – and those
    /// carry the sharpest solution fields of the API (<c>Gap.Answer</c>, <c>Question.Answer</c>). A guard built
    /// on <see cref="Actions"/> alone leaves roughly fifty endpoints outside its scope, the same blind spot
    /// <c>EndpointCoverage</c> documents for itself.
    /// <para>
    /// Deduplicated by name and signature: a <c>new</c>-hidden action would otherwise appear twice, once per
    /// declaring type, and every count built on it would be inflated.
    /// </para>
    /// </summary>
    public static IEnumerable<MethodInfo> ActionsIncludingInherited(Type controller) =>
        controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName && m.GetCustomAttributes<HttpMethodAttribute>().Any())
            .GroupBy(m => (m.Name, Signature: string.Join(",", m.GetParameters().Select(p => p.ParameterType.FullName))))
            .Select(g => g.First());

    /// <summary>Key of an action – <c>Controller.Action</c>, the language of all guard messages.</summary>
    public static string Key(string controller, string action) => $"{controller}.{action}";

    /// <inheritdoc cref="Key(string,string)"/>
    public static string Key(Type controller, MethodInfo action) => Key(controller.Name, action.Name);

    /// <summary>
    /// Route template of an action: controller <c>[Route]</c> + template of the HTTP verb.
    /// <para>
    /// An action template starting with <c>~/</c> (or <c>/</c>) <b>replaces</b> the controller prefix,
    /// it doesn't append to it – this is how, for instance, <c>ShopController</c> also serves
    /// child-scoped routes under its shop prefix. Naive concatenation would produce nonsense like
    /// <c>api/v1/supervisor/shop/~/api/v1/supervisor/children/3/…</c>; the guards would then check a route
    /// that doesn't exist.
    /// </para>
    /// </summary>
    public static string RouteOf(Type controller, MethodInfo action)
    {
        var prefix = controller.GetCustomAttributes<RouteAttribute>(inherit: true).FirstOrDefault()?.Template ?? "";
        var suffix = action.GetCustomAttributes<HttpMethodAttribute>().FirstOrDefault()?.Template ?? "";
        if (suffix.StartsWith("~/", StringComparison.Ordinal))
            return suffix[2..];
        if (suffix.StartsWith('/'))
            return suffix[1..];
        return $"{prefix}/{suffix}";
    }

    /// <summary>The HTTP verb of an action (<c>GET</c>, <c>POST</c>, …).</summary>
    public static string MethodOf(MethodInfo action) =>
        action.GetCustomAttributes<HttpMethodAttribute>().First().HttpMethods.First();

    /// <summary>A route placeholder including constraint: <c>{childId:int}</c>, <c>{version:apiVersion}</c>.</summary>
    private static readonly Regex Placeholder = new(@"\{(?<name>[A-Za-z0-9_]+)(?::[^}]*)?\}", RegexOptions.Compiled);

    /// <summary>The placeholder names of a route template, without constraint and without the version segment.</summary>
    public static IEnumerable<string> RouteParameters(string template) =>
        Placeholder.Matches(template)
            .Select(m => m.Groups["name"].Value)
            .Where(n => !n.Equals("version", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Assembles a route template into a concrete URL. <c>{version:apiVersion}</c> becomes
    /// <c>1</c>, all other placeholders come from <paramref name="values"/>; a missing value is an
    /// error, not an empty slot – otherwise a URL would result that matches nothing, and the test would pass.
    /// </summary>
    public static string BuildUrl(string template, IReadOnlyDictionary<string, string> values)
    {
        var url = Placeholder.Replace(template, m =>
        {
            var name = m.Groups["name"].Value;
            if (name.Equals("version", StringComparison.OrdinalIgnoreCase))
                return "1";
            return values.TryGetValue(name, out var value)
                ? value
                : throw new InvalidOperationException(
                    $"No value for route placeholder '{name}' in '{template}'.");
        });
        // Double and trailing slashes arise where an action carries no template of its own.
        return Regex.Replace(url, "/{2,}", "/").TrimEnd('/');
    }

    /// <summary>Repo root: upward from <see cref="AppContext.BaseDirectory"/> until <c>backend</c>+<c>docs</c> or <c>.git</c>.</summary>
    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if ((Directory.Exists(Path.Combine(dir.FullName, "backend")) && Directory.Exists(Path.Combine(dir.FullName, "docs")))
                || Directory.Exists(Path.Combine(dir.FullName, ".git")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Repository root (backend + docs, or .git) not found.");
    }
}
