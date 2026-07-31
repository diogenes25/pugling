using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// The complete ownership surface instead of individual examples (docs/codequalitaet-gates-plan.md, C1):
/// <b>every</b> action under <c>{childId}</c> or <c>{planId}</c> is called with a foreign credential
/// and must reject it.
/// <para>
/// The difference to <see cref="OwnershipTests"/> (six hand-picked cases) and to
/// <see cref="ConventionGuardTests.Actions_Unter_ChildId_Oder_PlanId_Tragen_Den_Ownership_Filter"/>
/// (only checks that the <c>[ServiceFilter]</c> attribute <em>is present</em>) is the effect: here the
/// filter actually runs. For generated code this is the single most valuable test – a new route without
/// an ownership check stands out immediately, without anyone having to remember to add it.
/// </para>
/// </summary>
public class OwnershipMatrixTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>What a foreign credential is allowed to see: nothing. 404 hides the existence, 403 denies it.</summary>
    private static bool IsRejection(HttpStatusCode status) =>
        status is HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized;

    [Fact]
    public async Task Fremder_Supervisor_Kommt_An_Keine_Kindes_Oder_Plan_Gebundene_Action()
    {
        var victim = await FremdeWeltAsync();
        var attacker = await TestApi.FatherAsync(factory); // der geseedete Vater 1 – ihm gehört nichts davon
        await PruefeMatrixAsync(attacker, victim, "fremder Supervisor");
    }

    [Fact]
    public async Task Fremdes_Kind_Kommt_An_Keine_Kindes_Oder_Plan_Gebundene_Action()
    {
        // Zweiter Durchgang mit einem **Kind**-Token, und zwar nicht aus Symmetrie: ein fremder Supervisor
        // scheitert an einer Student-Route womöglich schon an der Rolle, und ein Rollen-403 sähe genauso aus
        // wie ein Eigentums-403. Das Kind trägt die Student-Rolle – es kommt bis zur Eigentumsprüfung.
        var victim = await FremdeWeltAsync();
        var attacker = await TestApi.ChildAsync(factory); // der geseedete Sohn 1
        await PruefeMatrixAsync(attacker, victim, "fremdes Kind");
    }

    /// <summary>Child, plan and position of a <b>different</b> adult – the targets of the attack.</summary>
    private async Task<(int ChildId, int PlanId, int PositionId)> FremdeWeltAsync()
    {
        var registered = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = "Fremder Papa", pin = "2222" });
        var otherAdultId = await TestApi.IdAsync(registered);
        var owner = await TestApi.FatherAsync(factory, otherAdultId, "2222");

        var childId = await TestApi.IdAsync(await owner.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Fremdes Kind", pin = "3333" }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(owner);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.SelfAssess, childId: childId);
        return (childId, planId, positionId);
    }

    private async Task PruefeMatrixAsync(HttpClient attacker, (int ChildId, int PlanId, int PositionId) victim, string wer)
    {
        var offenders = new List<string>();
        var inconclusive = new List<string>();
        var checkedActions = 0;

        foreach (var controller in ApiSurface.Controllers())
            foreach (var action in ApiSurface.Actions(controller))
            {
                var template = ApiSurface.RouteOf(controller, action);
                var parameters = ApiSurface.RouteParameters(template).ToList();
                if (!parameters.Contains("childId") && !parameters.Contains("planId"))
                    continue;

                var key = ApiSurface.Key(controller, action);
                if (Ausnahmen.Contains(key))
                    continue;

                var url = ApiSurface.BuildUrl(template, RouteWerte(action, parameters, victim));
                var method = ApiSurface.MethodOf(action);
                using var request = new HttpRequestMessage(new HttpMethod(method), url);
                if (Rumpf(action) is { } body)
                    request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
                else if (BrauchtRumpf(method) && HatRumpfParameter(action))
                {
                    inconclusive.Add($"{key}: Rumpf nicht baubar (Datei-Upload?) – Ausnahme mit Grund eintragen");
                    continue;
                }

                checkedActions++;
                var response = await attacker.SendAsync(request);
                if (IsRejection(response.StatusCode))
                    continue;

                // 400 ist **kein** Beleg: es kann heißen „Rumpf unbindbar", und dann hat die
                // Eigentumsprüfung nie gefeuert. Getrennt melden, sonst tarnt sich eine echte Lücke als
                // Validierungsfehler.
                var zeile = $"{key} [{method} {url}] → {(int)response.StatusCode}";
                if (response.StatusCode == HttpStatusCode.BadRequest)
                    inconclusive.Add($"{zeile}: {await response.Content.ReadAsStringAsync()}");
                else
                    offenders.Add(zeile);
            }

        // Selbstschutz: greift die Routen-Auflösung nicht, prüft die Matrix nichts und wäre grün.
        Assert.True(checkedActions >= 60,
            $"Zu wenige kindes-/plan-gebundene Actions geprüft ({checkedActions}) – Routen-Auflösung greift nicht.");
        Assert.True(inconclusive.Count == 0,
            $"Unentschieden ({wer}) – diese Actions ließen sich nicht bis zur Eigentumsprüfung treiben:\n"
            + string.Join("\n", inconclusive));
        Assert.True(offenders.Count == 0,
            $"IDOR: ein {wer} kommt an fremde Daten (erwartet 403/404):\n" + string.Join("\n", offenders));
    }

    /// <summary>
    /// Values for the route placeholders. <c>childId</c>/<c>planId</c> point into the foreign world, all
    /// other ids stay arbitrary: the ownership check must fire <b>before</b> them – if it didn't,
    /// the existence of a sub-resource would decide access instead.
    /// </summary>
    private static Dictionary<string, string> RouteWerte(MethodInfo action, IEnumerable<string> parameters,
        (int ChildId, int PlanId, int PositionId) victim)
    {
        var werte = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in parameters)
            werte[name] = name switch
            {
                "childId" => victim.ChildId.ToString(),
                "planId" => victim.PlanId.ToString(),
                "positionId" => victim.PositionId.ToString(),
                // Zeichenketten-Platzhalter (Schlüssel, Slugs) brauchen etwas Nicht-Numerisches, sonst
                // scheitert schon die Route-Constraint und der Aufruf käme als 404 durch, ohne zu prüfen.
                _ => IstZeichenkette(action, name) ? "x" : "1",
            };
        return werte;
    }

    private static bool IstZeichenkette(MethodInfo action, string routeParameter) =>
        action.GetParameters()
            .FirstOrDefault(p => string.Equals(p.Name, routeParameter, StringComparison.OrdinalIgnoreCase))
            ?.ParameterType == typeof(string);

    private static bool BrauchtRumpf(string method) =>
        method is "POST" or "PUT" or "PATCH";

    /// <summary>The parameter bound from the body: a complex type with no other binding source.</summary>
    private static ParameterInfo? RumpfParameter(MethodInfo action) =>
        action.GetParameters().FirstOrDefault(p =>
            p.ParameterType != typeof(CancellationToken)
            && !p.ParameterType.IsPrimitive
            && p.ParameterType != typeof(string)
            && !p.ParameterType.IsEnum
            && Nullable.GetUnderlyingType(p.ParameterType) is null
            && !p.GetCustomAttributes().Any(a => a.GetType().Name is "FromRouteAttribute" or "FromQueryAttribute"
                or "FromServicesAttribute" or "FromHeaderAttribute" or "FromFormAttribute"));

    private static bool HatRumpfParameter(MethodInfo action) => RumpfParameter(action) is not null;

    private static JsonNode? Rumpf(MethodInfo action) =>
        RumpfParameter(action) is { } p ? SampleJson.ForType(p.ParameterType) : null;

    /// <summary>
    /// Deliberate exceptions – <b>not</b> a catch-all. Every entry needs a reason; without a reason
    /// the gap should be closed instead.
    /// </summary>
    private static readonly HashSet<string> Ausnahmen = new(StringComparer.Ordinal);
}
