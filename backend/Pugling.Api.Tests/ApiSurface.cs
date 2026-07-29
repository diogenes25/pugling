using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Pugling.Api.Tests;

/// <summary>
/// Die API-Fläche per Reflexion: Controller, Actions, Routen-Vorlagen. Geteilte Grundlage der reflexiven
/// Wächter (<see cref="ConventionGuardTests"/>, <see cref="OwnershipMatrixTests"/>,
/// <see cref="EndpointCoverage"/>) – dieselbe Definition von „was ist eine Action" für alle, sonst
/// bewachen drei Tests drei verschiedene Flächen und keiner merkt es.
/// </summary>
internal static class ApiSurface
{
    /// <summary>Alle instanziierbaren Controller der API.</summary>
    public static IEnumerable<Type> Controllers() =>
        typeof(Program).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

    /// <summary>Die öffentlichen Actions eines Controllers (deklariert, nicht geerbt).</summary>
    public static IEnumerable<MethodInfo> Actions(Type controller) =>
        controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName && m.GetCustomAttributes<HttpMethodAttribute>().Any());

    /// <summary>Schlüssel einer Action – <c>Controller.Action</c>, die Sprache aller Wächter-Meldungen.</summary>
    public static string Key(string controller, string action) => $"{controller}.{action}";

    /// <inheritdoc cref="Key(string,string)"/>
    public static string Key(Type controller, MethodInfo action) => Key(controller.Name, action.Name);

    /// <summary>
    /// Route-Vorlage einer Action: Controller-<c>[Route]</c> + Vorlage des HTTP-Verbs.
    /// <para>
    /// Eine mit <c>~/</c> (oder <c>/</c>) beginnende Action-Vorlage <b>ersetzt</b> das Controller-Präfix,
    /// sie hängt nicht daran – so führt etwa <c>ShopController</c> unter seinem Shop-Präfix zugleich
    /// kindgebundene Routen. Naives Verketten ergäbe Unsinn wie
    /// <c>api/v1/supervisor/shop/~/api/v1/supervisor/children/3/…</c>; die Wächter prüften dann eine Route,
    /// die es nicht gibt.
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

    /// <summary>Das HTTP-Verb einer Action (<c>GET</c>, <c>POST</c>, …).</summary>
    public static string MethodOf(MethodInfo action) =>
        action.GetCustomAttributes<HttpMethodAttribute>().First().HttpMethods.First();

    /// <summary>Ein Route-Platzhalter samt Constraint: <c>{childId:int}</c>, <c>{version:apiVersion}</c>.</summary>
    private static readonly Regex Placeholder = new(@"\{(?<name>[A-Za-z0-9_]+)(?::[^}]*)?\}", RegexOptions.Compiled);

    /// <summary>Die Platzhalter-Namen einer Route-Vorlage, ohne Constraint und ohne das Versionssegment.</summary>
    public static IEnumerable<string> RouteParameters(string template) =>
        Placeholder.Matches(template)
            .Select(m => m.Groups["name"].Value)
            .Where(n => !n.Equals("version", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Setzt eine Route-Vorlage zu einer konkreten URL zusammen. <c>{version:apiVersion}</c> wird zu
    /// <c>1</c>, alle übrigen Platzhalter kommen aus <paramref name="values"/>; ein fehlender Wert ist ein
    /// Fehler und keine leere Stelle – sonst entstünde eine URL, die nichts trifft, und der Test wäre grün.
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
                    $"Kein Wert für Route-Platzhalter '{name}' in '{template}'.");
        });
        // Doppelte und abschließende Schrägstriche entstehen, wo eine Action keine eigene Vorlage trägt.
        return Regex.Replace(url, "/{2,}", "/").TrimEnd('/');
    }

    /// <summary>Repo-Wurzel: von <see cref="AppContext.BaseDirectory"/> aufwärts bis <c>backend</c>+<c>docs</c> bzw. <c>.git</c>.</summary>
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
        throw new InvalidOperationException("Repo-Wurzel (backend + docs bzw. .git) nicht gefunden.");
    }
}
