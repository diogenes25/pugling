using System.Text;
using System.Text.RegularExpressions;
using Microsoft.OpenApi;

namespace Pugling.Api.OpenApi;

/// <summary>
/// Turns the XML doc prose that the OpenAPI source generator copies into <c>summary</c>/<c>description</c>
/// into text a reader can use (B-147, B-138). Runs as the last document transformer, so it sees every text
/// the generator and the other transformers produced, and a comment written tomorrow is covered without
/// anyone touching it.
/// <para>
/// Two defects of the generator (Microsoft.AspNetCore.OpenApi 10.0.10) are repaired here, not in 106
/// comments: it keeps the source indentation of a comment inside a <c>para</c> (four leading spaces are a
/// code block in Markdown), and it passes <c>b</c>/<c>i</c>/<c>em</c>/<c>strong</c> through as raw text,
/// which Swagger UI prints verbatim in a summary.
/// </para>
/// </summary>
public static partial class OpenApiProseNormalizer
{
    /// <summary>Normalizes every summary and description in the document in place.</summary>
    public static void Apply(OpenApiDocument doc)
    {
        if (doc.Info is { } info)
            info.Description = Description(info.Description);
        foreach (var tag in Each(doc.Tags))
            tag.Description = Description(tag.Description);

        foreach (var pathItem in Each(doc.Paths?.Values))
        {
            if (pathItem is not OpenApiPathItem item) continue;
            item.Summary = Summary(item.Summary);
            item.Description = Description(item.Description);
            foreach (var parameter in Each(item.Parameters)) Visit(parameter);
            foreach (var operation in Each(item.Operations?.Values)) Visit(operation);
        }

        var components = doc.Components;
        if (components is null) return;
        foreach (var schema in Each(components.Schemas?.Values)) Visit(schema);
        foreach (var parameter in Each(components.Parameters?.Values)) Visit(parameter);
        foreach (var response in Each(components.Responses?.Values)) Visit(response);
        foreach (var body in Each(components.RequestBodies?.Values)) Visit(body);
        foreach (var scheme in Each(components.SecuritySchemes?.Values))
            if (scheme is OpenApiSecurityScheme s) s.Description = Description(s.Description);
    }

    /// <summary>
    /// A summary is shown as plain text (Swagger UI's operation line, Scalar's heading), so emphasis tags
    /// are dropped rather than translated - <c>**all**</c> would be just as visible as <c>&lt;b&gt;</c>.
    /// </summary>
    public static string? Summary(string? text) => text is null ? null : Dedent(text, markdown: false);

    /// <summary>A description is rendered as Markdown, so emphasis survives as <c>**…**</c> / <c>*…*</c>.</summary>
    public static string? Description(string? text) => text is null ? null : Dedent(text, markdown: true);

    private static IEnumerable<T> Each<T>(IEnumerable<T>? items) => items ?? [];

    private static void Visit(OpenApiOperation operation)
    {
        operation.Summary = Summary(operation.Summary);
        operation.Description = Description(operation.Description);
        foreach (var parameter in Each(operation.Parameters)) Visit(parameter);
        if (operation.RequestBody is { } body) Visit(body);
        foreach (var response in Each(operation.Responses?.Values)) Visit(response);
    }

    private static void Visit(IOpenApiParameter parameter)
    {
        if (parameter is not OpenApiParameter p) return;
        p.Description = Description(p.Description);
        if (p.Schema is { } schema) Visit(schema);
    }

    private static void Visit(IOpenApiRequestBody body)
    {
        if (body is not OpenApiRequestBody b) return;
        b.Description = Description(b.Description);
        foreach (var media in Each(b.Content?.Values)) if (media.Schema is { } schema) Visit(schema);
    }

    private static void Visit(IOpenApiResponse response)
    {
        if (response is not OpenApiResponse r) return;
        r.Description = Description(r.Description);
        foreach (var header in Each(r.Headers?.Values))
            if (header is OpenApiHeader h) h.Description = Description(h.Description);
        foreach (var media in Each(r.Content?.Values)) if (media.Schema is { } schema) Visit(schema);
    }

    private static void Visit(IOpenApiSchema schema)
    {
        switch (schema)
        {
            // A $ref carries its own description sibling (a property's text next to the referenced type);
            // the referenced schema itself is visited once, through components. Only the reference's OWN
            // text: its Description getter falls back to the target's, and writing that back would copy
            // the type's description onto every $ref in the document.
            case OpenApiSchemaReference reference:
                reference.Reference.Description = Description(reference.Reference.Description);
                return;
            case OpenApiSchema s:
                s.Description = Description(s.Description);
                foreach (var property in Each(s.Properties?.Values)) Visit(property);
                if (s.Items is { } items) Visit(items);
                if (s.AdditionalProperties is { } extra) Visit(extra);
                foreach (var part in Each(s.AllOf)) Visit(part);
                foreach (var part in Each(s.OneOf)) Visit(part);
                foreach (var part in Each(s.AnyOf)) Visit(part);
                return;
        }
    }

    private static string Dedent(string text, bool markdown)
    {
        var lines = text.ReplaceLineEndings("\n").Split('\n');
        var output = new StringBuilder(text.Length);
        var previousIndented = false;
        var previousBlank = true;
        foreach (var raw in lines)
        {
            var indented = raw.Length > 0 && char.IsWhiteSpace(raw[0]);
            var line = raw.Trim();
            if (line.Length == 0)
            {
                // Collapse runs of blank lines: one blank line is a paragraph break, more say nothing extra.
                if (!previousBlank) output.Append("\n\n");
                previousBlank = true;
                previousIndented = false;
                continue;
            }

            if (!previousBlank)
            {
                // The generator indents only the FIRST line of a <para>, its continuation lines start at
                // column 0. So an indented line after an unindented one is the start of a paragraph - the
                // one signal that survives into the generated string. The reverse case (a <para> right
                // after indented text, or two single-line paragraphs in a row) cannot be told apart from a
                // wrapped line and stays a soft break; a blank /// line instead of <para> makes it explicit.
                output.Append(indented && !previousIndented ? "\n\n" : "\n");
            }

            output.Append(line);
            previousBlank = false;
            previousIndented = indented;
        }

        var result = output.ToString().TrimEnd();
        return markdown
            ? Italic().Replace(Bold().Replace(result, m => Wrap(m, "**")), m => Wrap(m, "*"))
            : Italic().Replace(Bold().Replace(result, m => m.Groups["inner"].Value), m => m.Groups["inner"].Value);
    }

    /// <summary>
    /// Markdown emphasis must hug its text (<c>** x **</c> is not bold), so the whitespace inside the tag
    /// moves outside it.
    /// </summary>
    private static string Wrap(Match match, string marker)
    {
        var inner = match.Groups["inner"].Value;
        var trimmed = inner.Trim();
        if (trimmed.Length == 0) return inner;
        var lead = inner[..(inner.Length - inner.TrimStart().Length)];
        var trail = inner[inner.TrimEnd().Length..];
        return $"{lead}{marker}{trimmed}{marker}{trail}";
    }

    // Emphasis never spans a paragraph break: "**a" + blank line + "b**" is no emphasis in Markdown. Such a
    // tag stays raw, so the contract test reports it loudly instead of the document losing it silently.
    [GeneratedRegex(@"<(?<tag>b|strong)>(?<inner>(?:(?!\n\n).)*?)</\k<tag>>", RegexOptions.Singleline)]
    private static partial Regex Bold();

    [GeneratedRegex(@"<(?<tag>i|em)>(?<inner>(?:(?!\n\n).)*?)</\k<tag>>", RegexOptions.Singleline)]
    private static partial Regex Italic();
}
