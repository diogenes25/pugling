using System.Text.Json;

namespace Pugling.Api.OpenApi;

/// <summary>Loads the Swagger examples verified by integration tests.</summary>
public sealed class OpenApiExampleCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IReadOnlyList<OpenApiExampleEntry> _entries;

    private OpenApiExampleCatalog(IReadOnlyList<OpenApiExampleEntry> entries) => _entries = entries;

    /// <summary>All verified examples.</summary>
    public IReadOnlyList<OpenApiExampleEntry> Entries => _entries;

    /// <summary>Loads the generated catalog, if it is present in the build output.</summary>
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
