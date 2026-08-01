using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Pins down the contract tightening from <c>UnmappedMemberHandling.Disallow</c>: the server
/// <b>rejects</b> a field it doesn't know instead of silently discarding it.
/// <para>
/// Previously it reported <c>201 Created</c> and the caller believed its value had arrived - for an
/// API-first product with generated clients and AI agents this is the most expensive default of all,
/// because it turns a contract error into silent data loss. The evidence for this used to live in the
/// test helper <c>TestApi.CreateEmptyPlanAsync</c> (see its docs). Background:
/// docs/codequalitaet-gates-plan.md (L3/B3).
/// </para>
/// </summary>
public class UnknownFieldTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Unbekanntes_Feld_wird_abgelehnt_mit_eigenem_Code()
    {
        var father = await TestApi.FatherAsync(factory);

        // `method` belonged to the plan-wide StudyPlanItem/Method model, which was removed completely during
        // the study plan rebuild - exactly the kind of outdated field the server used to swallow.
        var res = await father.PostAsJsonAsync("/api/v1/supervisor/study-plans", new
        {
            childId = 1,
            title = "Plan mit Altfeld",
            durationDays = 5,
            method = "Vocabulary",
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        // Its own code, not `validation_error`: the cause is "the field does not exist", not "the value is wrong".
        Assert.Equal("unknown_field", body.GetProperty("code").GetString());
        // The field has to be named, otherwise the caller searches in the dark.
        var errors = body.GetProperty("errors");
        Assert.True(errors.TryGetProperty("method", out var messages), "The unknown field has to appear as a key.");
        var text = messages.EnumerateArray().Single().GetString()!;
        Assert.Contains("Unknown field", text, StringComparison.Ordinal);
        // **No type name leak.** The raw System.Text.Json message names the internal DTO type
        // ("… contained in type 'Pugling.Contracts.Supervisor.CreatePlanDto'"); that must not reach the outside.
        Assert.DoesNotContain("Pugling.", text, StringComparison.Ordinal);
        Assert.False(string.IsNullOrEmpty(body.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Gueltiger_Body_bleibt_akzeptiert()
    {
        // The counter-check (self-protection against a false green): the test above would also be green if the
        // endpoint rejected every body. The same payload without the legacy field has to go through.
        var father = await TestApi.FatherAsync(factory);

        var planId = await TestApi.CreateEmptyPlanAsync(father);

        Assert.True(planId > 0);
    }

    [Fact]
    public async Task Falscher_Wert_bleibt_validation_error()
    {
        // The delimitation: a known field with a wrong value is still `validation_error`. Otherwise the two
        // causes would merge into one code and the caller could not tell them apart.
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/v1/auth/adult", new { adultId = "1a", pin = "0000" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_error", body.GetProperty("code").GetString());
    }
}
