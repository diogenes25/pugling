using System.Text.Json;

namespace Pugling.Api.OpenApi;

/// <summary>Lädt die durch Integrationstests verifizierten Swagger-Beispiele.</summary>
public sealed class OpenApiExampleCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IReadOnlyList<OpenApiExampleEntry> _entries;

    private OpenApiExampleCatalog(IReadOnlyList<OpenApiExampleEntry> entries) => _entries = entries;

    /// <summary>Alle verifizierten Beispiele.</summary>
    public IReadOnlyList<OpenApiExampleEntry> Entries => _entries;

    /// <summary>Lädt den generierten Katalog, falls er im Build-Output vorhanden ist.</summary>
    public static OpenApiExampleCatalog Load(string contentRootPath)
    {
        var path = Path.Combine(contentRootPath, "OpenApi", "openapi-examples.generated.json");
        if (!File.Exists(path))
            return new OpenApiExampleCatalog([]);

        using var stream = File.OpenRead(path);
        var entries = JsonSerializer.Deserialize<List<OpenApiExampleEntry>>(stream, SerializerOptions) ?? [];
        return new OpenApiExampleCatalog(entries);
    }
}