using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>Regression tests for the verified examples selectable in Swagger.</summary>
public class OpenApiExampleTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task OpenApi_EnthaeltVerifizierteRequestUndResponseBeispiele()
    {
        var client = factory.CreateClient();
        var doc = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        Assert.Contains("auth-vater-login", RequestExampleKeys(doc, "/api/v1/auth/adult", "post"));
        Assert.Contains("auth-login-mit-falscher-pin", RequestExampleKeys(doc, "/api/v1/auth/adult", "post"));
        Assert.Contains("auth-login-mit-falscher-pin", ResponseExampleKeys(doc, "/api/v1/auth/adult", "post", "401"));
        Assert.Contains("study-plans-lehrplan-anlegen", RequestExampleKeys(doc, "/api/v1/supervisor/study-plans", "post"));
        AssertDocumentContainsExample(doc, "children-einzelnes-kind-lesen");
        AssertDocumentContainsExample(doc, "study-plans-position-anlegen");
        AssertDocumentContainsExample(doc, "shop-shop-angebot-kaufen");
    }

    [Fact]
    public async Task OpenApi_Schema_BeschreibtEnumWerteUndPflichtOptionaleFelder()
    {
        var client = factory.CreateClient();
        var doc = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var schemas = doc.GetProperty("components").GetProperty("schemas");

        // Enums: documented as a string with the full value list (not as a bare integer).
        var unitType = schemas.GetProperty("UnitType");
        Assert.Equal("string", unitType.GetProperty("type").GetString());
        var allowed = unitType.GetProperty("enum").EnumerateArray().Select(v => v.GetString()).ToList();
        Assert.Equal(Enum.GetNames<UnitType>(), allowed);

        // Required vs. optional correctly: Create names the non-nullable fields as required, but NOT the
        // optional "description" (string?). All properties are still described in the object.
        var create = schemas.GetProperty("CreateShopArticleDto");
        var createRequired = create.GetProperty("required").EnumerateArray().Select(v => v.GetString()).ToList();
        Assert.Contains("articleNumber", createRequired);
        Assert.Contains("unitType", createRequired);
        Assert.DoesNotContain("description", createRequired);
        Assert.Contains("description", create.GetProperty("properties").EnumerateObject().Select(p => p.Name));

        // A partial-update DTO: all fields nullable → no required at all (the generator would have marked all).
        var update = schemas.GetProperty("UpdateShopArticleDto");
        Assert.False(update.TryGetProperty("required", out var upd) && upd.GetArrayLength() > 0,
            "UpdateShopArticleDto must declare no required fields (all fields are optional).");
    }

    private static IReadOnlyList<string> RequestExampleKeys(JsonElement doc, string path, string method) =>
        ExampleKeys(doc.GetProperty("paths").GetProperty(path).GetProperty(method)
            .GetProperty("requestBody").GetProperty("content"));

    private static IReadOnlyList<string> ResponseExampleKeys(JsonElement doc, string path, string method, string status) =>
        ExampleKeys(doc.GetProperty("paths").GetProperty(path).GetProperty(method)
            .GetProperty("responses").GetProperty(status).GetProperty("content"));

    private static IReadOnlyList<string> ExampleKeys(JsonElement content) =>
        content.EnumerateObject()
            .Where(mediaType => mediaType.Value.TryGetProperty("examples", out _))
            .SelectMany(mediaType => mediaType.Value.GetProperty("examples").EnumerateObject().Select(example => example.Name))
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private static void AssertDocumentContainsExample(JsonElement doc, string expectedKey)
    {
        var found = doc.GetProperty("paths").EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject())
            .Any(operation => OperationContainsExample(operation.Value, expectedKey));

        Assert.True(found, $"OpenAPI example '{expectedKey}' was not found.");
    }

    private static bool OperationContainsExample(JsonElement operation, string expectedKey)
    {
        if (operation.TryGetProperty("requestBody", out var requestBody)
            && requestBody.TryGetProperty("content", out var requestContent)
            && ExampleKeys(requestContent).Contains(expectedKey, StringComparer.Ordinal))
            return true;

        if (!operation.TryGetProperty("responses", out var responses))
            return false;

        return responses.EnumerateObject()
            .Where(response => response.Value.TryGetProperty("content", out _))
            .SelectMany(response => ExampleKeys(response.Value.GetProperty("content")))
            .Contains(expectedKey, StringComparer.Ordinal);
    }
}
