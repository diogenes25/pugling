using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Errors;

namespace Pugling.Api.Tests;

/// <summary>
/// Secures the machine-readable error code system: every emit path (validation, domain-specific
/// <c>ProblemWithCode</c>, framework/middleware, ownership filter) returns a stable <c>code</c>, the
/// <c>type</c> URI matches it, <c>traceId</c> is preserved, and the OpenAPI <c>enum</c> matches the
/// registry (drift protection).
/// </summary>
public class ErrorCodeTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static async Task<JsonElement> BodyAsync(HttpResponseMessage res) =>
        await res.Content.ReadFromJsonAsync<JsonElement>();

    private static string? Code(JsonElement body) =>
        body.TryGetProperty("code", out var c) ? c.GetString() : null;

    [Fact]
    public async Task Validierung_LiefertValidationErrorCode()
    {
        // Path (b): the InvalidModelStateResponseFactory (a non-int adultId → a parse error).
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/v1/auth/adult", new { adultId = "1a", pin = "0000" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await BodyAsync(res);
        Assert.Equal("validation_error", Code(body));
        Assert.True(body.TryGetProperty("errors", out _));
        // Regression: a validation 400 must carry a traceId like every other error.
        Assert.False(string.IsNullOrEmpty(body.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Validierung_ErzeugtKeineLeerenSchluessel()
    {
        // Regression: a root parse error (path "$") must not turn into an empty errors key.
        var client = factory.CreateClient();
        var res = await client.PostAsync("/api/v1/auth/adult",
            new StringContent("{ not valid json", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await BodyAsync(res);
        Assert.Equal("validation_error", Code(body));
        foreach (var field in body.GetProperty("errors").EnumerateObject())
            Assert.False(string.IsNullOrEmpty(field.Name));
    }

    [Fact]
    public void HttpError_FallbackCode_IstImKatalog()
    {
        // Regression: the ForStatus catch-all code must be a declared code (otherwise it is missing from the
        // OpenAPI enum and from the drift test) - e.g. for 415 Unsupported Media Type.
        Assert.Equal("http_error", ApiErrors.ForStatus(415).Code);
        Assert.Contains("http_error", ApiErrors.AllCodes);
    }

    [Fact]
    public async Task FalschePin_LiefertInvalidCredentials_MitTypUndTraceId()
    {
        // Path (a): a domain ProblemWithCode; it also checks the type URI shape and that the traceId survives.
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/v1/auth/adult", new { adultId = 1, pin = "9998" });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        var body = await BodyAsync(res);
        Assert.Equal("invalid_credentials", Code(body));
        Assert.Equal("https://pugling.app/errors/invalid_credentials", body.GetProperty("type").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task OhneToken_LiefertUnauthorized()
    {
        // Path (c): an empty 401 from the JWT middleware, via UseStatusCodePages + CustomizeProblemDetails.
        var client = factory.CreateClient();
        var res = await client.GetAsync("/api/v1/student/me/points");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Equal("unauthorized", Code(await BodyAsync(res)));
    }

    [Fact]
    public async Task FalscheRolle_LiefertForbidden()
    {
        // Path (c): a supervisor token on a child-only route (me/*) → 403 forbidden.
        var father = await TestApi.FatherAsync(factory);
        var res = await father.GetAsync("/api/v1/student/me/points");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("forbidden", Code(await BodyAsync(res)));
    }

    [Fact]
    public async Task UnbekannteRessource_LiefertNotFound()
    {
        // Path: a bare NotFound() from a controller → the [ApiController] auto-conversion through the factory.
        var father = await TestApi.FatherAsync(factory);
        var res = await father.GetAsync("/api/v1/creator/subjects/999999");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal("not_found", Code(await BodyAsync(res)));
    }

    [Fact]
    public async Task FremderPlan_OwnershipFilter_LiefertProblemDetailsMitCode()
    {
        // Regression guard: the PlanOwnershipFilter used to return a raw German string.
        var father = await TestApi.FatherAsync(factory);
        var res = await father.GetAsync("/api/v1/supervisor/study-plans/999999/positions");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        var body = await BodyAsync(res);
        Assert.Equal("not_found", Code(body));
        Assert.False(string.IsNullOrEmpty(body.GetProperty("detail").GetString())); // a structured body, not an empty/raw string
    }

    [Fact]
    public async Task SkinDoppeltKaufen_LiefertSkinAlreadyUnlocked()
    {
        // Path (a): the starter skin "pug" is already unlocked → buying it again gives 409.
        var father = await TestApi.FatherAsync(factory);
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Code-Kind", pin = "7401" }));
        var child = await TestApi.ChildAsync(factory, childId, "7401");

        var res = await child.PostAsJsonAsync("/api/v1/student/me/skins/pug/purchase", new { });

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("skin_already_unlocked", Code(await BodyAsync(res)));
    }

    [Fact]
    public async Task SkinOhneGems_LiefertInsufficientGems()
    {
        var father = await TestApi.FatherAsync(factory);
        var childId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Code-Kind", pin = "7402" }));
        var child = await TestApi.ChildAsync(factory, childId, "7402");

        var res = await child.PostAsJsonAsync("/api/v1/student/me/skins/fox/purchase", new { });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("insufficient_gems", Code(await BodyAsync(res)));
    }

    [Fact]
    public async Task OpenApi_CodeEnum_DecktSichMitRegistry()
    {
        // Drift guard: the enum documented in the OpenAPI document must be exactly ApiErrors.AllCodes.
        var client = factory.CreateClient();
        var doc = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        var enumValues = doc.GetProperty("components").GetProperty("schemas")
            .GetProperty("ProblemDetails").GetProperty("properties").GetProperty("code")
            .GetProperty("enum").EnumerateArray().Select(e => e.GetString()!).ToHashSet();

        // Self-protection against a false green - the only reflective guard in the suite that lacked it
        // (docs/testplan.md, stage 1c): BOTH sides of the comparison come from ApiErrors.AllCodes. If the
        // reflection stops biting there (fields renamed, visibility changed), on paper it would read
        // `empty == empty` - and the drift test would pass vacuously. That it does not in practice is an
        // accident: with an empty list the generator omits the `enum` property entirely, and only the
        // KeyNotFoundException above topples it. This line makes the protection deliberate.
        Assert.True(ApiErrors.AllCodes.Count >= 40,
            $"Too few error codes found ({ApiErrors.AllCodes.Count}) - the reflection in ApiErrors does not bite.");
        Assert.Equal(ApiErrors.AllCodes.ToHashSet(), enumValues);
    }
}
