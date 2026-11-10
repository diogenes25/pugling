using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace Pugling.Agent.Creator;

/// <summary>A photographed textbook page as read from disk: its file name (for the console) and its bytes.</summary>
/// <param name="FileName">The file name without directory - what the human compares the extract against.</param>
/// <param name="Bytes">The image bytes; they go to the local model only, never to the Pugling API (B-375).</param>
/// <param name="MediaType">The image media type derived from the extension.</param>
public sealed record PageImage(string FileName, byte[] Bytes, string MediaType)
{
    /// <summary>The formats a local vision model takes. HEIC (iPhone default) is not among them - export as JPEG.</summary>
    private static readonly Dictionary<string, string> MediaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
    };

    /// <summary>
    /// Loads the pages behind the given paths: a file is one page, a directory contributes all supported images in it,
    /// sorted by name (camera names count up, so the order is the page order). Every mistake is a usage error that
    /// names the path - a wrong photo found after minutes of model time would be the expensive way to learn it.
    /// </summary>
    public static async Task<IReadOnlyList<PageImage>> LoadAsync(IReadOnlyList<string> paths, CancellationToken ct)
    {
        var files = new List<string>();
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                var images = InPageOrder(Directory.EnumerateFiles(path)
                    .Where(f => MediaTypes.ContainsKey(Path.GetExtension(f))));
                if (images.Count == 0)
                    throw new AgentUsageException($"Im Ordner '{path}' liegt kein Bild (.jpg, .jpeg, .png, .webp).");
                files.AddRange(images);
                continue;
            }
            // A mistyped folder has no extension: name the real mistake, not the format.
            if (!File.Exists(path))
                throw new AgentUsageException($"'{path}' gibt es nicht (weder Datei noch Ordner).");
            if (!MediaTypes.ContainsKey(Path.GetExtension(path)))
                throw new AgentUsageException($"'{path}' ist kein unterstütztes Bild – erlaubt sind .jpg, .jpeg, .png und .webp "
                                              + "(HEIC vom iPhone vorher als JPEG exportieren).");
            files.Add(path);
        }

        // A file named next to its own folder is one page, not two - each page costs minutes of model time on CPU.
        var distinct = files.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var pages = new List<PageImage>(distinct.Count);
        foreach (var file in distinct)
        {
            byte[] bytes;
            try
            {
                bytes = await File.ReadAllBytesAsync(file, ct);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new AgentUsageException($"Das Bild '{file}' ist nicht lesbar: {ex.Message}");
            }
            if (bytes.Length == 0)
                throw new AgentUsageException($"Das Bild '{file}' ist leer.");
            pages.Add(new PageImage(Path.GetFileName(file), bytes, MediaTypes[Path.GetExtension(file)]));
        }
        return pages;
    }

    /// <summary>
    /// The page order of a folder: by file name, numbers compared as numbers - "seite-2" before "seite-10", the way a
    /// human names photos without leading zeros. The file system's own order is not guaranteed and not used.
    /// </summary>
    public static IReadOnlyList<string> InPageOrder(IEnumerable<string> files) =>
        [.. files.OrderBy(f => Regex.Replace(Path.GetFileName(f), @"\d+", m => m.Value.PadLeft(20, '0')),
            StringComparer.OrdinalIgnoreCase)];
}

/// <summary>What the vision model recognised on one page: the material in its own words, not a transcription.</summary>
/// <param name="FileName">The page's file name.</param>
/// <param name="Text">The material extract of the page.</param>
public sealed record PageExtract(string FileName, string Text);

/// <summary>Which models the local model server has installed - asked before the first page is read.</summary>
public interface IModelInventory
{
    /// <summary>Whether the named model is installed locally.</summary>
    Task<bool> HasModelAsync(string model, CancellationToken ct);
}

/// <summary>The model list of the local Ollama server.</summary>
public sealed class OllamaModelInventory(IOllamaApiClient ollama) : IModelInventory
{
    /// <inheritdoc />
    public async Task<bool> HasModelAsync(string model, CancellationToken ct)
    {
        var installed = await ollama.ListLocalModelsAsync(ct);
        // Ollama lists an untagged pull as "name:latest"; the configuration usually names it without the tag.
        return installed.Any(m => string.Equals(m.Name, model, StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(m.Name, model + ":latest", StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Reads photographed textbook pages with a local vision model and returns the <b>material</b> of each page (B-375):
/// topics, grammar rules in its own words, word pairs. The exercises, texts and example sentences of the book are
/// deliberately not taken over - the page becomes material for the unit notes, and the exercises are drafted
/// afterwards from those notes only, so the book's own tasks never reach the catalog or the child. One page per call:
/// small vision models in Ollama take one image per request.
/// </summary>
public sealed class PageReader(IChatClient chat, IModelInventory models, IOptions<AgentOptions> options,
    ILogger<PageReader> logger)
{
    /// <summary>
    /// The copyright rule is the core of this prompt. It asks for an extract, not a transcription: what the page
    /// teaches, never how the book phrases its tasks. Plain text, not JSON - small vision models fail at JSON, and the
    /// text model structures the extracts afterwards anyway.
    /// </summary>
    public const string System = """
        Du liest die fotografierte Seite eines Schulbuchs und hältst fest, welchen STOFF sie vermittelt.

        Regeln:
        1. Schreibe den Stoff in drei Abschnitten auf: "Themen:", "Grammatik:", "Wörter:".
        2. Themen: kurze Stichworte, worum es auf der Seite geht.
        3. Grammatik: die Regel IN EIGENEN WORTEN, knapp. Keine Erklärtexte des Buches abschreiben.
        4. Wörter: Vokabeln aus Wortlisten oder Kästen als "Wort – Übersetzung", eines je Zeile.
        5. Übernimm NICHTS davon wörtlich: Aufgabenstellungen, Aufgabennummern, Lese- und Hörtexte, Dialoge,
           Beispielsätze, Lückentexte, Bildbeschreibungen. Diese Inhalte sind urheberrechtlich geschützt und werden
           nicht gebraucht – aus dem Stoff entstehen später eigene Übungen.
        6. Was auf der Seite nicht steht, lässt du weg. Erfinde nichts dazu. Ist ein Abschnitt leer, schreibe "–".
        """;

    /// <summary>
    /// Checks the vision model is installed, then reads page by page. A missing model is a usage error before any
    /// model call - pulling it is the fix, and the message names the command.
    /// </summary>
    public async Task<IReadOnlyList<PageExtract>> ReadAsync(IReadOnlyList<PageImage> pages, CancellationToken ct)
    {
        var model = options.Value.VisionModel;
        if (!await models.HasModelAsync(model, ct))
            throw new AgentUsageException($"Das Vision-Modell '{model}' ist lokal nicht installiert. Hol es mit "
                                          + $"'ollama pull {model}' oder nenne ein vorhandenes über Agent:VisionModel.");

        var extracts = new List<PageExtract>(pages.Count);
        for (var i = 0; i < pages.Count; i++)
        {
            var page = pages[i];
            logger.LogInformation("Lese Seite {Index}/{Count}: {File} mit {Model}", i + 1, pages.Count, page.FileName, model);
            List<ChatMessage> messages =
            [
                new(ChatRole.System, System),
                new(ChatRole.User, [new TextContent("Welchen Stoff vermittelt diese Seite?"),
                    new DataContent(page.Bytes, page.MediaType)]),
            ];
            var response = await chat.GetResponseAsync(messages,
                new ChatOptions { ModelId = model, Temperature = (float)options.Value.Temperature }, ct);
            var text = response.Text.Trim();
            if (text.Length == 0)
                throw new AgentException($"Das Vision-Modell '{model}' hat zu '{page.FileName}' nichts erkannt – "
                                         + "ist die Seite scharf und gerade fotografiert?");
            extracts.Add(new PageExtract(page.FileName, text));
        }
        return extracts;
    }

    /// <summary>
    /// The extracts as one source for the structuring call, page by page under the file name - and the human's own
    /// source text after them, when one was given too.
    /// </summary>
    public static string AsSource(IReadOnlyList<PageExtract> extracts, string? sourceText)
    {
        var parts = extracts.Select(e => $"### Seite {e.FileName}\n{e.Text}").ToList();
        if (!string.IsNullOrWhiteSpace(sourceText)) parts.Add($"### Ergänzende Quelle\n{sourceText.Trim()}");
        return string.Join("\n\n", parts);
    }
}
