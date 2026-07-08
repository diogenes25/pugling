using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Errors;

namespace Pugling.Api.Tests;

/// <summary>
/// Sichert das maschinenlesbare Fehler-Code-System ab: jeder Emit-Pfad (Validierung, fachliches
/// <c>ProblemWithCode</c>, Framework/Middleware, Ownership-Filter) liefert einen stabilen <c>code</c>,
/// der <c>type</c>-URI passt dazu, <c>traceId</c> bleibt erhalten, und das OpenAPI-<c>enum</c> deckt
/// sich mit der Registry (Drift-Schutz).
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
        // Pfad (b): der InvalidModelStateResponseFactory (nicht-int fatherId → Parse-Fehler).
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/v1/auth/father", new { fatherId = "1a", pin = "0000" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await BodyAsync(res);
        Assert.Equal("validation_error", Code(body));
        Assert.True(body.TryGetProperty("errors", out _));
        // Regression: Validierungs-400 müssen wie jeder andere Fehler eine traceId tragen.
        Assert.False(string.IsNullOrEmpty(body.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Validierung_ErzeugtKeineLeerenSchluessel()
    {
        // Regression: ein Wurzel-Parse-Fehler (Pfad „$") darf nicht zu einem leeren errors-Schlüssel werden.
        var client = factory.CreateClient();
        var res = await client.PostAsync("/api/v1/auth/father",
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
        // Regression: der ForStatus-Auffangcode muss ein deklarierter Code sein (sonst fehlt er im
        // OpenAPI-enum und im Drift-Test) – z. B. für 415 Unsupported Media Type.
        Assert.Equal("http_error", ApiErrors.ForStatus(415).Code);
        Assert.Contains("http_error", ApiErrors.AllCodes);
    }

    [Fact]
    public async Task FalschePin_LiefertInvalidCredentials_MitTypUndTraceId()
    {
        // Pfad (a): fachliches ProblemWithCode; prüft zugleich type-URI-Form und traceId-Erhalt.
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/v1/auth/father", new { fatherId = 1, pin = "9998" });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        var body = await BodyAsync(res);
        Assert.Equal("invalid_credentials", Code(body));
        Assert.Equal("https://pugling.app/errors/invalid_credentials", body.GetProperty("type").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task OhneToken_LiefertUnauthorized()
    {
        // Pfad (c): leere 401 der JWT-Middleware, via UseStatusCodePages + CustomizeProblemDetails.
        var client = factory.CreateClient();
        var res = await client.GetAsync("/api/v1/student/me/points");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Equal("unauthorized", Code(await BodyAsync(res)));
    }

    [Fact]
    public async Task FalscheRolle_LiefertForbidden()
    {
        // Pfad (c): Vater-Token auf einer nur-Sohn-Route (me/*) → 403 forbidden.
        var father = await TestApi.FatherAsync(factory);
        var res = await father.GetAsync("/api/v1/student/me/points");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("forbidden", Code(await BodyAsync(res)));
    }

    [Fact]
    public async Task UnbekannteRessource_LiefertNotFound()
    {
        // Pfad: bare NotFound() eines Controllers → [ApiController]-Auto-Wandlung über die Factory.
        var father = await TestApi.FatherAsync(factory);
        var res = await father.GetAsync("/api/v1/creator/subjects/999999");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal("not_found", Code(await BodyAsync(res)));
    }

    [Fact]
    public async Task FremderPlan_OwnershipFilter_LiefertProblemDetailsMitCode()
    {
        // Regressionsschutz: der PlanOwnershipFilter lieferte früher einen rohen deutschen String.
        var father = await TestApi.FatherAsync(factory);
        var res = await father.GetAsync("/api/v1/supervisor/study-plans/999999/positions");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        var body = await BodyAsync(res);
        Assert.Equal("not_found", Code(body));
        Assert.False(string.IsNullOrEmpty(body.GetProperty("detail").GetString())); // strukturierter Body, kein leerer/roher String
    }

    [Fact]
    public async Task SkinDoppeltKaufen_LiefertSkinAlreadyUnlocked()
    {
        // Pfad (a): der Starter-Skin "pug" ist bereits freigeschaltet → erneuter Kauf 409.
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
        // Drift-Schutz: das im OpenAPI-Dokument dokumentierte enum muss exakt ApiErrors.AllCodes sein.
        var client = factory.CreateClient();
        var doc = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        var enumValues = doc.GetProperty("components").GetProperty("schemas")
            .GetProperty("ProblemDetails").GetProperty("properties").GetProperty("code")
            .GetProperty("enum").EnumerateArray().Select(e => e.GetString()!).ToHashSet();

        Assert.Equal(ApiErrors.AllCodes.ToHashSet(), enumValues);
    }
}
