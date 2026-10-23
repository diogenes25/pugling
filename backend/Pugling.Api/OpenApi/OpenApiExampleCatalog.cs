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

    /// <summary>How often <see cref="Load"/> tries to read the catalog before it gives up.</summary>
    public const int ReadAttempts = 5;

    /// <summary>Loads the generated catalog, if it is present in the build output.</summary>
    public static OpenApiExampleCatalog Load(string contentRootPath)
    {
        var path = Path.Combine(contentRootPath, "OpenApi", "openapi-examples.generated.json");
        if (!File.Exists(path))
            return Empty;

        return LoadWithRetry(path, File.OpenRead, ReadAttempts, TimeSpan.FromMilliseconds(50));
    }

    /// <summary>
    /// Reads <paramref name="path"/> and retries a bounded number of times when the read fails (B-181).
    /// The catalog is rewritten while hosts start (DocsCaptureTests), and which failure a concurrent reader
    /// sees depends on the platform: Windows refuses to open a file that is being written
    /// (<see cref="IOException"/>), Linux lets the reader in and hands it half-written JSON
    /// (<see cref="JsonException"/>). Both are transient, so both are retried - and so is
    /// <see cref="UnauthorizedAccessException"/>, which Windows raises for a file caught in the "delete
    /// pending" moment of the writer's atomic replace (the same window DocsCaptureTests' writer retries).
    ///
    /// A catalog that stays unreadable is not swallowed: after the last attempt the failure is thrown with the
    /// path and the attempt count, the last exception as its cause. A retry on <see cref="JsonException"/>
    /// would otherwise hide a permanently broken file behind a slower start.
    ///
    /// <paramref name="open"/> is the seam the tests use to make a failure deterministic instead of racing
    /// a real writer.
    /// </summary>
    public static OpenApiExampleCatalog LoadWithRetry(string path, Func<string, Stream> open, int attempts, TimeSpan pause)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(attempts, 1);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var stream = open(path);
                var entries = JsonSerializer.Deserialize<List<OpenApiExampleEntry>>(stream, SerializerOptions) ?? [];
                return new OpenApiExampleCatalog(entries);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                if (attempt >= attempts)
                    throw new InvalidOperationException(
                        $"Beispielkatalog {path} nach {attempts} Versuchen nicht lesbar: {ex.Message}", ex);
                Thread.Sleep(pause);
            }
        }
    }
}
