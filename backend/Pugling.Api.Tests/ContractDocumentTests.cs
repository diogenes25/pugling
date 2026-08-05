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

    /// <summary>
    /// Four statements about the document that were <b>false</b> until B-42 step 2 - and whose breach the diff
    /// gate cannot report. That gate catches a change to the document; it waves through a newly added endpoint
    /// or DTO that repeats one of these defects, because then the document changes "correctly". Each assertion
    /// names its defect instead of showing a five-thousand line diff.
    /// </summary>
    [Fact]
    public async Task Vertragsdokument_BeschreibtDieLeitungWahrheitsgemaess()
    {
        var doc = JsonNode.Parse(await GenerateAsync())!;
        var schemas = doc["components"]!["schemas"]!.AsObject();
        var methods = new[] { "get", "post", "put", "patch", "delete" };

        // 1. Every operation names a success. Without it a renamed response field moves nothing in the
        // document - the gate of step 1 was blind to response shapes for half the API.
        var withoutSuccess = doc["paths"]!.AsObject()
            .SelectMany(path => path.Value!.AsObject()
                .Where(op => methods.Contains(op.Key))
                .Where(op => !(op.Value!["responses"]?.AsObject() ?? []).Any(r => r.Key.StartsWith('2')))
                .Select(op => $"{op.Key.ToUpperInvariant()} {path.Key}"))
            .ToList();
        Assert.True(withoutSuccess.Count == 0,
            $"{withoutSuccess.Count} operations document no 2xx response. An explicit [ProducesResponseType] "
            + $"replaces the inferred set - SuccessResponseConvention restores it, but only where the return "
            + $"type names a payload:\n  {string.Join("\n  ", withoutSuccess.Take(10))}");

        // 2. No enum arrives as a bare integer. Nullable<TEnum> is not IsEnum, so an enum the generator reaches
        // through a "TEnum?" field first slips past the transformer - and a generated client gets `number`.
        var bareIntegers = schemas
            .Where(s => s.Value!["type"]?.GetValue<string>() == "integer"
                && s.Value!["enum"] is null && s.Value!["properties"] is null)
            .Select(s => s.Key)
            .ToList();
        Assert.True(bareIntegers.Count == 0,
            $"Schemas without a value list, presumably nullable enums: {string.Join(", ", bareIntegers)}. "
            + "The API sends the NAME; a bare integer makes the document lie.");

        // 3. A property with a default value is not required - omitting it is legal, the server fills it in.
        // This hit every `clear<Field>` switch of the PATCH semantics: the document demanded what makes the
        // difference between "leave as is" and "clear".
        var requiredDespiteDefault = schemas
            .SelectMany(s => (s.Value!["required"]?.AsArray() ?? [])
                .Select(r => r!.GetValue<string>())
                .Where(name => s.Value!["properties"]?[name]?.AsObject().ContainsKey("default") == true)
                .Select(name => $"{s.Key}.{name}"))
            .ToList();
        Assert.True(requiredDespiteDefault.Count == 0,
            $"Required despite a default value: {string.Join(", ", requiredDespiteDefault)}");

        // 4. No `clear<Field>` switch is required anywhere. Follows from 3 today, but it is the rule the root
        // CLAUDE.md states, and it deserves to be nailed down in the CONTRACT, not only in the DTO.
        var requiredClearSwitches = schemas
            .SelectMany(s => (s.Value!["required"]?.AsArray() ?? [])
                .Select(r => r!.GetValue<string>())
                .Where(name => name.StartsWith("clear", StringComparison.Ordinal))
                .Select(name => $"{s.Key}.{name}"))
            .ToList();
        Assert.True(requiredClearSwitches.Count == 0,
            $"Clear switches declared as required: {string.Join(", ", requiredClearSwitches)}. A form that "
            + "changes one field would have to send them along in order to clear nothing.");

        // 5. A [Flags] enum travels as a COMMA-SEPARATED COMBINATION (e.g. "Realschule, Gymnasium"), never one
        // of the single names - an `enum` list of just the individual names would reject exactly the values the
        // server actually sends (B-60). Reflective over the [Flags] TYPES themselves, not over a type name
        // like "SchoolTypes" - otherwise a second [Flags] type introduced later would slip past this gate
        // silently, exactly the blind spot this closes.
        var flagsWithEnumList = typeof(PointKind).Assembly.GetTypes()
            .Where(t => t.IsEnum && t.IsDefined(typeof(FlagsAttribute), inherit: false))
            .Where(t => schemas.ContainsKey(t.Name) && schemas[t.Name]!["enum"] is not null)
            .Select(t => t.Name)
            .ToList();
        Assert.True(flagsWithEnumList.Count == 0,
            $"[Flags] enums with an `enum` value list in the document (rejects valid combinations): "
            + $"{string.Join(", ", flagsWithEnumList)}");

        // 6. No schema demands a `required` field it does not itself describe under `properties` - the
        // document would be self-contradictory (ProblemDetails.Extensions, a [JsonExtensionData] catch-all,
        // was exactly this case, B-56). Generic over every schema, not hardcoded to that one type name.
        var requiredWithoutProperty = schemas
            .SelectMany(s => (s.Value!["required"]?.AsArray() ?? [])
                .Select(r => r!.GetValue<string>())
                .Where(name => s.Value!["properties"]?[name] is null)
                .Select(name => $"{s.Key}.{name}"))
            .ToList();
        Assert.True(requiredWithoutProperty.Count == 0,
            $"Required fields without a matching property (the document demands something it never describes): "
            + $"{string.Join(", ", requiredWithoutProperty)}");
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
