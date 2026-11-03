using Microsoft.OpenApi;
using Pugling.Api.OpenApi;

namespace Pugling.Api.Tests;

/// <summary>
/// The rules of <see cref="OpenApiProseNormalizer"/> as a pure function (B-147, B-138). The contract test
/// (<see cref="ContractDocumentTests"/>) proves that nothing indented or tagged reaches the document; these
/// cases pin down HOW, so that a regression names the rule it broke instead of a location in 900 KB.
/// Each case says which change turns it red.
/// </summary>
public class OpenApiProseNormalizerTests
{
    // Red if the per-line TrimStart goes: four leading spaces stay, which Markdown reads as a code block.
    [Fact]
    public void Einrueckung_Faellt_Je_Zeile_Weg()
        => Assert.Equal("Deletes a subject\nwithout it.", OpenApiProseNormalizer.Description("    Deletes a subject\n    without it."));

    // The generator indents only a <para>'s first line. Red if the "indented after unindented" signal is
    // ignored (the two paragraphs run together) or applied to every indented line (the wrapped intro splits).
    [Fact]
    public void Eingerueckte_Zeile_Nach_Buendiger_Beginnt_Einen_Absatz()
        => Assert.Equal(
            "Intro line\nstill intro\nFirst para\ncontinued\n\nSecond para\ncontinued",
            OpenApiProseNormalizer.Description("    Intro line\n    still intro\n    First para\ncontinued\n    Second para\ncontinued"));

    // B-260: a <param> whose text starts on the tag line arrives unindented first, indented after - the reverse of a
    // <para>. Red if every "indented after unindented" counts as a paragraph: the sentence then breaks after "can".
    [Fact]
    public void Text_Auf_Der_Tag_Zeile_Bricht_Nicht_In_Einen_Absatz()
        => Assert.Equal("New login PIN, so an account can\nbe deliberately deactivated.",
            OpenApiProseNormalizer.Description("New login PIN, so an account can\n            be deliberately deactivated."));

    // B-260: the generator writes the first list item indented and every later one at column 0, each continued
    // indented. Red if the continuation of a later item counts as a paragraph (measured: "both\n\nlists").
    [Fact]
    public void Folgezeile_Eines_Listenpunkts_Bricht_Nicht_In_Einen_Absatz()
        => Assert.Equal("Two reasons:\n* First item\nwraps here.\n* Second item, both\nlists empty.",
            OpenApiProseNormalizer.Description(
                "            Two reasons:\n            * First item\n            wraps here.\n* Second item, both\n            lists empty."));

    // Red if blank runs are kept (three blank lines say nothing a single one does not) or the outer
    // blank lines survive the trim.
    [Fact]
    public void Leerzeilen_Werden_Zu_Einem_Absatz_Und_Raender_Fallen_Weg()
        => Assert.Equal("a\n\nb", OpenApiProseNormalizer.Description("\n\na\r\n\r\n\r\n\nb\n\n"));

    // A summary is plain text in Swagger UI: red if summaries get Markdown markers (then **all** is just as
    // visible as <b>all</b>) or keep the tags.
    [Fact]
    public void Summary_Laesst_Betonung_Fallen()
        => Assert.Equal("carries all roles of the one account",
            OpenApiProseNormalizer.Summary("carries <b>all</b> roles of the <i>one</i> account"));

    // A description is Markdown: red if any of the four tags stays raw or maps to the wrong marker.
    [Fact]
    public void Description_Uebersetzt_Betonung_Nach_Markdown()
        => Assert.Equal("for **every** role, *one* list, *really* **must**",
            OpenApiProseNormalizer.Description("for <b>every</b> role, <i>one</i> list, <em>really</em> <strong>must</strong>"));

    // "** x **" is not emphasis in Markdown. Red if the inner whitespace stays inside the markers.
    [Fact]
    public void Leerraum_Im_Tag_Wandert_Nach_Aussen()
        => Assert.Equal("a **b** c", OpenApiProseNormalizer.Description("a<b> b </b>c"));

    // Emphasis across a paragraph break would be "**a\n\nb**", which Markdown does not render. Red if the
    // translation ignores the break: the tag must stay raw so the contract test names it.
    [Fact]
    public void Betonung_Ueber_Einen_Absatz_Bleibt_Roh()
        => Assert.Equal("<b>a\n\nb</b>", OpenApiProseNormalizer.Description("<b>a\n\nb</b>"));

    // A $ref's Description getter falls back to the referenced schema's text. Red if Apply writes the getter
    // back: then every property pointing at a type carries a copy of that type's description (measured once:
    // 920 extra texts in the document).
    [Fact]
    public void Referenz_Bekommt_Keine_Kopie_Der_Typbeschreibung()
    {
        var doc = new OpenApiDocument { Components = new OpenApiComponents { Schemas = new Dictionary<string, IOpenApiSchema>() } };
        var reference = new OpenApiSchemaReference("Target", doc);
        doc.Components.Schemas["Target"] = new OpenApiSchema { Description = "    The type." };
        doc.Components.Schemas["Holder"] = new OpenApiSchema
        {
            Properties = new Dictionary<string, IOpenApiSchema> { ["target"] = reference },
        };
        // Without a registered workspace the reference resolves nothing, its getter returns null, and the
        // case could not fail - the generated document always has one.
        doc.RegisterComponents();
        Assert.Equal("    The type.", reference.Description);

        OpenApiProseNormalizer.Apply(doc);

        Assert.Null(reference.Reference.Description);
        Assert.Equal("The type.", doc.Components.Schemas["Target"].Description);
    }
}
