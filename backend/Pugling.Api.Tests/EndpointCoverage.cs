using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Pugling.Api.Tests;

/// <summary>
/// Collects process-wide <b>which</b> controller actions the test suite has actually invoked
/// successfully – the data source for the coverage guard (<see cref="EndpointCoverageGuard"/>,
/// docs/codequalitaet-gates-plan.md, C4).
/// <para>
/// Why not line/branch coverage? Because it lies: 97.9% lines with 57 never-invoked actions.
/// Coverlet counts async state machines and small <c>Get</c> bodies too, and a high ratio arises even
/// when an entire endpoint was never served over HTTP. What is counted instead is the unit that the
/// product exposes externally: the <b>action</b>.
/// </para>
/// <para>
/// Process-wide static, because every test class runs its own host with a disposable SQLite
/// (<see cref="PuglingWebAppFactory"/>) – the sum across all hosts is what matters, not the individual one.
/// </para>
/// </summary>
internal static class EndpointCoverage
{
    private static readonly ConcurrentDictionary<string, byte> TouchedActions = new(StringComparer.Ordinal);

    /// <summary>
    /// Marks an action as "touched" – <b>only for status &lt; 400</b>.
    /// <para>
    /// The status code is the actual rule here. "The route was hit once" would be worthless:
    /// the ownership matrix test (C1) calls every child-/plan-bound action with a different supervisor
    /// and gets 403/404 – that would count every such action as covered without its body ever having
    /// run. A 2xx/3xx, in contrast, proves that the action was <em>executed</em> and its result could be
    /// checked. That is exactly what C3 demands ("happy path + the one business-relevant error case").
    /// </para>
    /// </summary>
    public static void RecordSuccess(string controller, string action) =>
        TouchedActions.TryAdd(Key(controller, action), 0);

    /// <summary>Key of an action – <c>Controller.Action</c>, as in the guard's error messages.</summary>
    public static string Key(string controller, string action) => $"{controller}.{action}";

    /// <summary>All actions of all controllers in the API – the target set.</summary>
    public static IReadOnlyList<string> Inventory() =>
        [.. typeof(Program).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && m.GetCustomAttributes<HttpMethodAttribute>().Any())
                .Select(m => Key(t.Name, m.Name)))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(k => k, StringComparer.Ordinal)];

    /// <summary>The actions that no test has successfully invoked.</summary>
    public static IReadOnlyList<string> Untouched() =>
        [.. Inventory().Where(k => !TouchedActions.ContainsKey(k))];

    /// <summary>
    /// Number of successfully touched actions <b>from the target set</b> – the guard's self-protection
    /// against false-green.
    /// <para>
    /// The intersection with the target set is necessary, not cosmetic: <see cref="Inventory"/>, like the
    /// other guards, counts only <c>DeclaredOnly</c>, whereas the middleware also sees the CRUD actions of
    /// the typed exercise controllers <b>inherited</b> from <c>ExerciseControllerBase</c>. Without the
    /// intersection, "touched" would exceed the "target set" and the reported ratio would be wrong.
    /// </para>
    /// </summary>
    public static int TouchedCount => Inventory().Count(TouchedActions.ContainsKey);
}

/// <summary>
/// Hooks the counter from <see cref="EndpointCoverage"/> into the test host's pipeline.
/// <para>
/// As an <see cref="IStartupFilter"/> and not via <c>IWebHostBuilder.Configure</c>, because the latter
/// would <b>replace</b> the pipeline from <c>Program.cs</c>. The middleware sits right at the front and
/// only reads the endpoint <em>after</em> <c>next()</c>: it gets set further downstream by routing, and
/// stays attached to the <c>HttpContext</c> afterwards.
/// </para>
/// </summary>
internal sealed class EndpointCoverageStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (ctx, nextMiddleware) =>
        {
            await nextMiddleware(ctx);
            if (ctx.Response.StatusCode >= 400)
                return;
            if (ctx.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>() is { } descriptor)
                EndpointCoverage.RecordSuccess(descriptor.ControllerTypeInfo.Name, descriptor.ActionName);
        });
        next(app);
    };
}
