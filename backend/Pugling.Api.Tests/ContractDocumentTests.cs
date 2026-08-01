using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;

namespace Pugling.Api.Tests;

/// <summary>
/// Writes the <b>contract-pure</b> OpenAPI document to <c>docs/openapi/v1.json</c> on every run and proves
/// that two hosts produce the same bytes (docs/backlog/B-42-openapi-typen-generieren.md, step 1).
/// <para>
/// <b>Why check a generated file in at all.</b> The document is the contract. Checked in, every change to it
/// becomes visible in the diff of the commit that causes it – and CI turns an uncommitted change red. Until
/// now a renamed field was invisible in review: it changed a record, and nothing else in the repo moved.
/// </para>
/// <para>
/// <b>Why contract-pure.</b> The examples are documentation, not contract, and they cannot be byte-stable in
/// this suite – see <see cref="Pugling.Api.OpenApi.OpenApiExampleCatalog.Empty"/>. They stay covered by gate
/// D4, which diffs <c>docs/api-examples</c> and the generated example catalog.
/// </para>
/// </summary>
public class ContractDocumentTests
{
    /// <summary>Where the checked-in contract lives. Read by the CI gate and, from B-42 step 2, by the frontend.</summary>
    private static readonly string[] OutputPath = ["docs", "openapi", "v1.json"];

    [Fact]
    public async Task Vertragsdokument_WirdGeschrieben_UndIstZwischenZweiHostsByteGleich()
    {
        // Two separate hosts, not two requests against one: the question is whether a *run* reproduces the
        // document, and host startup is where the non-determinism would sit (catalog from the source tree,
        // reflection order, configuration). Two requests against one host would only prove the serializer.
        var first = await GenerateAsync();
        var second = await GenerateAsync();

        // Write **before** comparing. Otherwise a failing stability check leaves the developer without an
        // artifact - and with an xUnit string diff over 900 KB, which shows nothing. This way the two
        // documents lie side by side on disk and `git diff` names the difference.
        var path = Path.Combine(ApiSurface.RepoRoot(), Path.Combine(OutputPath));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, first);

        if (first != second)
        {
            var rival = Path.Combine(Path.GetDirectoryName(path)!, "v1.second-host.json");
            await File.WriteAllTextAsync(rival, second);
            Assert.Fail($"Two hosts produced two different documents ({first.Length} vs. {second.Length} "
                + $"characters). Compare {path} against {rival} - and do not commit the latter.");
        }

        // Self-protection against a vacuous green: an empty or truncated document would compare equal to
        // itself and get checked in as "the contract".
        Assert.Contains("\"/api/v1/supervisor/children\"", first, StringComparison.Ordinal);
        Assert.True(first.Length > 100_000, $"Contract document suspiciously small ({first.Length} characters).");
        // Contract-pure: the examples belong to gate D4, not here. If one shows up, the switch did not take
        // effect - and the diff gate would start flapping instead of guarding.
        Assert.DoesNotContain("\"examples\"", first, StringComparison.Ordinal);
    }

    private static async Task<string> GenerateAsync()
    {
        using var factory = new ContractDocumentFactory();
        var json = await factory.CreateClient().GetStringAsync("/openapi/v1.json");

        // Re-serialized indented, because the endpoint delivers one single line: a one-line diff over a
        // megabyte tells a reviewer nothing. Property order is preserved by JsonNode.
        var node = JsonNode.Parse(json)!;
        NormalizeLineEndings(node);
        var pretty = node.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        // Line endings explicitly, never Environment.NewLine - that exact mistake made gate D4 flap between
        // Windows and the Linux runner once already.
        return pretty.ReplaceLineEndings("\n").TrimEnd() + "\n";
    }

    /// <summary>
    /// Normalizes the line endings <b>inside</b> the string values to <c>\n</c>.
    /// <para>
    /// Not cosmetics, and not covered by normalizing the file: the <c>summary</c> fields carry the XML doc
    /// comments verbatim, including their line breaks. On Windows those are <c>\r\n</c> and land in the JSON
    /// as an escaped <c>\r\n</c>; on the Linux runner git checks the same sources out with <c>\n</c>. Without
    /// this the document would differ between the two platforms in hundreds of places and the diff gate would
    /// be red on its first CI run – on a difference that is not a contract change at all.
    /// </para>
    /// </summary>
    private static void NormalizeLineEndings(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                // Materialized, because assigning to the indexer while enumerating would throw.
                foreach (var (key, value) in obj.ToList())
                    Replace(value, v => obj[key] = v);
                break;
            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    var index = i;
                    Replace(array[index], v => array[index] = v);
                }
                break;
        }

        static void Replace(JsonNode? value, Action<JsonNode?> set)
        {
            if (value is JsonValue leaf && leaf.TryGetValue<string>(out var text))
            {
                if (text.Contains('\r', StringComparison.Ordinal)) set(JsonValue.Create(text.ReplaceLineEndings("\n")));
                return;
            }
            if (value is not null) NormalizeLineEndings(value);
        }
    }
}

/// <summary>
/// A host that serves the contract-pure document. Separate from <see cref="PuglingWebAppFactory"/> on
/// purpose: there the examples have to stay on, <see cref="OpenApiExampleTests"/> asserts them.
/// </summary>
internal sealed class ContractDocumentFactory : PuglingWebAppFactoryBase
{
    /// <inheritdoc />
    protected override string EnvironmentName => "Development";

    /// <inheritdoc />
    protected override void ConfigureFactory(IWebHostBuilder builder)
    {
        builder.UseSetting("OpenApi:ExamplesEnabled", "false");
        // The document hangs on no row of data - the seed would only cost startup time.
        builder.UseSetting("Seed:Enabled", "false");
    }
}
