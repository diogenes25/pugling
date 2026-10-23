using System.Text;
using System.Text.Json;
using Pugling.Api.OpenApi;

namespace Pugling.Api.Tests;

/// <summary>
/// B-181: <see cref="OpenApiExampleCatalog.LoadWithRetry"/> retries BOTH failure modes a concurrent reader
/// can see - which one appears depends on the platform (see <see cref="OpenApiExampleCatalogConcurrencyTests"/>)
/// - and gives up loudly instead of swallowing a catalog that stays broken. The opener is scripted, so each
/// case is deterministic instead of a race against a real writer. Each case says which change turns it red.
/// </summary>
public class OpenApiExampleCatalogRetryTests
{
    private const string Path = "catalog.json";
    private static readonly string Valid = JsonSerializer.Serialize(new[]
    {
        new { method = "GET", path = "/api/v1/x", status = 200, requestBodyJson = (string?)null, responseBodyJson = "{}" },
    });

    /// <summary>An opener that fails with the given exceptions first, then serves the valid catalog.</summary>
    private static Func<string, Stream> Scripted(params Func<Stream>[] steps)
    {
        var calls = 0;
        return _ => calls < steps.Length ? steps[calls++]() : new MemoryStream(Encoding.UTF8.GetBytes(Valid));
    }

    private static Stream Locked() => throw new IOException("The process cannot access the file because it is being used by another process.");
    private static Stream ReplacePending() => throw new UnauthorizedAccessException("Access to the path is denied.");
    private static Stream Torn() => new MemoryStream(Encoding.UTF8.GetBytes(Valid[..(Valid.Length / 2)]));

    // Windows: the open is refused while the writer holds the file. Red if IOException is not retried.
    [Fact]
    public void Gesperrte_Datei_Wird_Erneut_Gelesen()
    {
        var catalog = OpenApiExampleCatalog.LoadWithRetry(Path, Scripted(Locked, Locked), 5, TimeSpan.Zero);
        Assert.Single(catalog.Entries);
    }

    // Linux: the reader gets half-written JSON. Red if only IOException is retried (the B-181 trap).
    [Fact]
    public void Zerrissener_Inhalt_Wird_Erneut_Gelesen()
    {
        var catalog = OpenApiExampleCatalog.LoadWithRetry(Path, Scripted(Torn), 5, TimeSpan.Zero);
        Assert.Single(catalog.Entries);
    }

    // A file that stays broken is a real defect, not a race. Red if the last failure is swallowed (an empty
    // catalog instead of an exception), if the cause is dropped, or if the attempt count is off.
    [Fact]
    public void Dauerhaft_Kaputter_Katalog_Wirft_Mit_Diagnose()
    {
        var calls = 0;
        Stream Always() { calls++; return Torn(); }

        var ex = Assert.Throws<InvalidOperationException>(
            () => OpenApiExampleCatalog.LoadWithRetry(Path, _ => Always(), 3, TimeSpan.Zero));

        Assert.Equal(3, calls);
        Assert.IsAssignableFrom<JsonException>(ex.InnerException);
        Assert.Contains(Path, ex.Message, StringComparison.Ordinal);
    }

    // Windows, mid-replace: the target is "delete pending" for an instant. Red if UnauthorizedAccessException
    // is not retried (DocsCaptureTests' writer retries exactly this case on its side).
    [Fact]
    public void Datei_Mitten_Im_Ersetzen_Wird_Erneut_Gelesen()
    {
        var catalog = OpenApiExampleCatalog.LoadWithRetry(Path, Scripted(ReplacePending), 5, TimeSpan.Zero);
        Assert.Single(catalog.Entries);
    }

    // A programming error is not a race and must not be retried. Red if the filter widens to every exception.
    [Fact]
    public void Andere_Fehler_Schlagen_Sofort_Durch()
    {
        var calls = 0;
        Stream Broken() { calls++; throw new ArgumentException("bug"); }

        Assert.Throws<ArgumentException>(() => OpenApiExampleCatalog.LoadWithRetry(Path, _ => Broken(), 5, TimeSpan.Zero));
        Assert.Equal(1, calls);
    }
}
