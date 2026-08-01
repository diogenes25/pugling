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

    /// <summary>
    /// A catalog without entries – the transformer then leaves every operation untouched.
    /// <para>
    /// This exists for the <b>contract-pure</b> OpenAPI document that is checked in and diffed in CI. The
    /// examples cannot be part of it: <see cref="Load"/> reads the catalog from the content root at host
    /// startup, and <c>DocsCaptureTests</c> rewrites that same file during the very same test run. xUnit
    /// parallelizes over collections, so one host sees the old catalog and another the new one – a document
    /// carrying the examples would differ between two runs and the diff gate would flap.
    /// </para>
    /// </summary>
    public static OpenApiExampleCatalog Empty { get; } = new([]);

    /// <summary>Loads the generated catalog, if it is present in the build output.</summary>
    public static OpenApiExampleCatalog Load(string contentRootPath)
    {
        var path = Path.Combine(contentRootPath, "OpenApi", "openapi-examples.generated.json");
        if (!File.Exists(path))
            return Empty;

        using var stream = File.OpenRead(path);
        var entries = JsonSerializer.Deserialize<List<OpenApiExampleEntry>>(stream, SerializerOptions) ?? [];
        return new OpenApiExampleCatalog(entries);
    }
}
