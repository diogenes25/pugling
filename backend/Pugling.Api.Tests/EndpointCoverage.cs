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
/// Sammelt prozessweit, <b>welche</b> Controller-Actions die Testsuite tatsächlich erfolgreich aufgerufen
/// hat – die Datenquelle des Abdeckungs-Wächters (<see cref="EndpointCoverageGuard"/>,
/// docs/codequalitaet-gates-plan.md, C4).
/// <para>
/// Warum nicht die Zeilen-/Zweigabdeckung? Weil sie lügt: 97,9 % Zeilen bei 57 nie aufgerufenen Actions.
/// Coverlet zählt async-State-Machines und kleine <c>Get</c>-Rümpfe mit, und eine hohe Quote entsteht auch
/// dann, wenn ein ganzer Endpunkt nie über HTTP bedient wurde. Gezählt wird darum die Einheit, die das
/// Produkt nach außen gibt: die <b>Action</b>.
/// </para>
/// <para>
/// Prozessweit statisch, weil jede Testklasse ihren eigenen Host samt Wegwerf-SQLite fährt
/// (<see cref="PuglingWebAppFactory"/>) – die Summe über alle Hosts ist die Aussage, nicht der einzelne.
/// </para>
/// </summary>
internal static class EndpointCoverage
{
    private static readonly ConcurrentDictionary<string, byte> TouchedActions = new(StringComparer.Ordinal);

    /// <summary>
    /// Hält eine Action als „berührt" fest – <b>nur bei Status &lt; 400</b>.
    /// <para>
    /// Der Statuscode ist hier die eigentliche Regel. „Die Route wurde einmal angesprochen" wäre wertlos:
    /// der Ownership-Matrix-Test (C1) ruft jede kindes-/plan-gebundene Action mit einem fremden Vater auf
    /// und bekäme 403/404 – damit gälte jede dieser Actions als abgedeckt, ohne dass ihr Rumpf je gelaufen
    /// ist. Ein 2xx/3xx dagegen belegt, dass die Action <em>ausgeführt</em> und ihr Ergebnis geprüft werden
    /// konnte. Genau das verlangt C3 („Happy Path + der eine fachlich interessante Fehlerfall").
    /// </para>
    /// </summary>
    public static void RecordSuccess(string controller, string action) =>
        TouchedActions.TryAdd(Key(controller, action), 0);

    /// <summary>Schlüssel einer Action – <c>Controller.Action</c>, wie in den Fehlermeldungen der Wächter.</summary>
    public static string Key(string controller, string action) => $"{controller}.{action}";

    /// <summary>Alle Actions aller Controller der API – das Soll.</summary>
    public static IReadOnlyList<string> Inventory() =>
        [.. typeof(Program).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && m.GetCustomAttributes<HttpMethodAttribute>().Any())
                .Select(m => Key(t.Name, m.Name)))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(k => k, StringComparer.Ordinal)];

    /// <summary>Die Actions, die kein Test erfolgreich aufgerufen hat.</summary>
    public static IReadOnlyList<string> Untouched() =>
        [.. Inventory().Where(k => !TouchedActions.ContainsKey(k))];

    /// <summary>
    /// Anzahl der erfolgreich berührten Actions <b>aus dem Soll</b> – der Selbstschutz der Wächter gegen
    /// falsch-grün.
    /// <para>
    /// Der Schnitt mit dem Soll ist nötig, nicht kosmetisch: <see cref="Inventory"/> zählt wie die übrigen
    /// Wächter nur <c>DeclaredOnly</c>, die Middleware sieht dagegen auch die aus
    /// <c>ExerciseControllerBase</c> <b>geerbten</b> CRUD-Actions der typisierten Übungs-Controller. Ohne
    /// Schnitt käme „berührt" über „Soll" hinaus und die gemeldete Quote wäre falsch.
    /// </para>
    /// </summary>
    public static int TouchedCount => Inventory().Count(TouchedActions.ContainsKey);
}

/// <summary>
/// Hängt den Zähler von <see cref="EndpointCoverage"/> in die Pipeline des Test-Hosts.
/// <para>
/// Als <see cref="IStartupFilter"/> und nicht per <c>IWebHostBuilder.Configure</c>, weil letzteres die
/// Pipeline von <c>Program.cs</c> <b>ersetzen</b> würde. Die Middleware sitzt ganz vorn und liest den
/// Endpunkt erst <em>nach</em> <c>next()</c>: gesetzt wird er weiter hinten vom Routing, und er bleibt
/// danach am <c>HttpContext</c> stehen.
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
