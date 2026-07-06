using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Pugling.Api.OpenApi;

/// <summary>Hängt verifizierte Request/Response-Beispiele an passende OpenAPI-Operationen.</summary>
public sealed partial class OpenApiExamplesOperationTransformer(OpenApiExampleCatalog catalog) : IOpenApiOperationTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var method = context.Description.HttpMethod;
        var pathTemplate = NormalizeApiDescriptionPath(context.Description.RelativePath);
        if (method is null || pathTemplate is null || catalog.Entries.Count == 0)
            return Task.CompletedTask;

        var examples = catalog.Entries
            .Where(e => string.Equals(e.Method, method, StringComparison.OrdinalIgnoreCase)
                && Matches(pathTemplate, e.Path))
            .ToList();

        if (examples.Count == 0)
            return Task.CompletedTask;

        AddRequestExamples(operation, examples);
        AddResponseExamples(operation, examples);
        return Task.CompletedTask;
    }

    private static string? NormalizeApiDescriptionPath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        var path = relativePath.Split('?', 2)[0]
            .Replace("v{version:apiVersion}", "v1", StringComparison.OrdinalIgnoreCase)
            .Replace("{version:apiVersion}", "1", StringComparison.OrdinalIgnoreCase)
            .TrimStart('/');
        return $"/{path}";
    }

    private static bool Matches(string pathTemplate, string concretePath)
    {
        var path = concretePath.Split('?', 2)[0];
        var escapedTemplate = Regex.Escape(pathTemplate);
        var pattern = "^" + EscapedRouteParameterRegex().Replace(escapedTemplate, "[^/]+") + "$";
        return Regex.IsMatch(path, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static void AddRequestExamples(OpenApiOperation operation, IReadOnlyList<OpenApiExampleEntry> examples)
    {
        if (operation.RequestBody?.Content is not { Count: > 0 } content)
            return;

        var requestExamples = examples.Where(e => !string.IsNullOrWhiteSpace(e.RequestBodyJson)).ToList();
        if (requestExamples.Count == 0)
            return;

        foreach (var mediaType in JsonMediaTypes(content))
            AddExamples(mediaType, requestExamples, e => e.RequestBodyJson);
    }

    private static void AddResponseExamples(OpenApiOperation operation, IReadOnlyList<OpenApiExampleEntry> examples)
    {
        if (operation.Responses is null)
            return;

        foreach (var group in examples.Where(e => !string.IsNullOrWhiteSpace(e.ResponseBodyJson)).GroupBy(e => e.ExpectedStatus))
        {
            var statusCode = group.Key.ToString();
            if (!operation.Responses.TryGetValue(statusCode, out var response))
            {
                response = new OpenApiResponse
                {
                    Description = ResponseDescription(group.Key),
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new(),
                    },
                };
                operation.Responses[statusCode] = response;
            }

            if (response.Content is null)
                continue;

            foreach (var mediaType in JsonMediaTypes(response.Content))
                AddExamples(mediaType, [.. group], e => e.ResponseBodyJson);
        }
    }

    private static IEnumerable<OpenApiMediaType> JsonMediaTypes(IDictionary<string, OpenApiMediaType> content) =>
        content.Where(kvp => kvp.Key.Contains("json", StringComparison.OrdinalIgnoreCase)).Select(kvp => kvp.Value);

    private static void AddExamples(OpenApiMediaType mediaType, IReadOnlyList<OpenApiExampleEntry> examples,
        Func<OpenApiExampleEntry, string?> jsonSelector)
    {
        mediaType.Examples ??= new Dictionary<string, IOpenApiExample>(StringComparer.Ordinal);

        foreach (var example in examples)
        {
            var json = jsonSelector(example);
            if (string.IsNullOrWhiteSpace(json))
                continue;

            mediaType.Examples[example.Key] = new OpenApiExample
            {
                Summary = example.IsError ? $"{example.Title} (Fehlerfall)" : example.Title,
                Description = ExampleDescription(example),
                Value = JsonNode.Parse(json),
            };
        }
    }

    private static string ExampleDescription(OpenApiExampleEntry example)
    {
        var code = example.ExpectedCode is null ? null : $", Code `{example.ExpectedCode}`";
        return $"Verifiziert durch DocsCaptureTests: Rolle `{example.Role}`, erwartet HTTP {example.ExpectedStatus}{code}.";
    }

    private static string ResponseDescription(int statusCode) => statusCode switch
    {
        200 => "OK",
        201 => "Created",
        204 => "No Content",
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        _ => $"HTTP {statusCode}",
    };

    [GeneratedRegex(@"\\\{[^}]+\}", RegexOptions.CultureInvariant)]
    private static partial Regex EscapedRouteParameterRegex();
}