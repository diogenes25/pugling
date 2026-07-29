using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Nagelt die Vertragsverschärfung aus <c>UnmappedMemberHandling.Disallow</c> fest: der Server
/// <b>lehnt</b> ein Feld ab, das er nicht kennt, statt es still zu verwerfen.
/// <para>
/// Vorher meldete er <c>201 Created</c> und der Aufrufer glaubte, sein Wert sei angekommen – für ein
/// API-First-Produkt mit generierten Clients und KI-Agenten die teuerste Voreinstellung überhaupt, weil
/// sie einen Vertragsfehler in stillen Datenverlust verwandelt. Der eigene Beleg dafür stand im
/// Testhelfer <c>TestApi.CreateEmptyPlanAsync</c> (siehe dessen Doku). Hintergrund:
/// docs/codequalitaet-gates-plan.md (L3/B3).
/// </para>
/// </summary>
public class UnknownFieldTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Unbekanntes_Feld_wird_abgelehnt_mit_eigenem_Code()
    {
        var father = await TestApi.FatherAsync(factory);

        // `method` gehörte zum plan-weiten StudyPlanItem/Method-Modell, das beim Lehrplan-Umbau
        // vollständig entfernt wurde – genau die Art veralteten Felds, die der Server früher schluckte.
        var res = await father.PostAsJsonAsync("/api/v1/supervisor/study-plans", new
        {
            childId = 1,
            title = "Plan mit Altfeld",
            durationDays = 5,
            method = "Vocabulary",
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        // Eigener Code, nicht `validation_error`: die Ursache ist „Feld existiert nicht", nicht „Wert falsch".
        Assert.Equal("unknown_field", body.GetProperty("code").GetString());
        // Das Feld muss benannt werden, sonst sucht der Aufrufer im Dunkeln.
        var errors = body.GetProperty("errors");
        Assert.True(errors.TryGetProperty("method", out var messages), "Das unbekannte Feld muss als Schlüssel auftauchen.");
        var text = messages.EnumerateArray().Single().GetString()!;
        Assert.Contains("Unknown field", text, StringComparison.Ordinal);
        // **Kein Typnamen-Leak.** Die Rohmeldung von System.Text.Json nennt den internen DTO-Typ
        // („… contained in type 'Pugling.Contracts.Supervisor.CreatePlanDto'"); der darf nicht nach außen.
        Assert.DoesNotContain("Pugling.", text, StringComparison.Ordinal);
        Assert.False(string.IsNullOrEmpty(body.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Gueltiger_Body_bleibt_akzeptiert()
    {
        // Gegenprobe (Selbstschutz gegen falsch-grün): der Test oben wäre auch grün, wenn der Endpunkt
        // jeden Body ablehnte. Derselbe Payload ohne das Altfeld muss durchgehen.
        var father = await TestApi.FatherAsync(factory);

        var planId = await TestApi.CreateEmptyPlanAsync(father);

        Assert.True(planId > 0);
    }

    [Fact]
    public async Task Falscher_Wert_bleibt_validation_error()
    {
        // Abgrenzung: ein bekanntes Feld mit falschem Wert ist weiterhin `validation_error`. Sonst
        // verschmölzen die beiden Ursachen zu einem Code und der Aufrufer könnte sie nicht trennen.
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/v1/auth/adult", new { adultId = "1a", pin = "0000" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_error", body.GetProperty("code").GetString());
    }
}
