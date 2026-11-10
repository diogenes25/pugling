using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pugling.Client;

namespace Pugling.Agent.Creator;

/// <summary>What the <c>fill</c> verb was asked to do: which unit, from which human-supplied source, how.</summary>
/// <param name="SeriesId">The series the unit belongs to.</param>
/// <param name="UnitId">The unit whose notes are drafted.</param>
/// <param name="SourceText">The source the model structures - a table of contents, a chapter overview. Mandatory unless pages are given.</param>
/// <param name="Force">Overwrite fields that already carry a value; without it only empty fields are filled.</param>
/// <param name="DryRun">Show the draft, write nothing.</param>
/// <param name="Pages">Photographed textbook pages (B-375); each is read by the vision model into a material extract first.</param>
public sealed record UnitFillRequest(int SeriesId, int UnitId, string? SourceText, bool Force, bool DryRun,
    IReadOnlyList<PageImage>? Pages = null);

/// <summary>
/// The model's structured draft of a unit's notes (B-22). Three fields, nothing else: the unit's <c>Topics</c>
/// list, its grammar and its vocabulary note - the same three the AI creator reads back as "the material".
/// </summary>
/// <param name="Topics">Topics of the unit, one entry per topic.</param>
/// <param name="Grammar">Grammar the unit introduces or practices.</param>
/// <param name="VocabularyNotes">Word fields or concrete words of the unit.</param>
public sealed record UnitNotesDraft(List<string>? Topics, string? Grammar, string? VocabularyNotes);

/// <summary>
/// The result: the unit before, the draft, which fields were written and which were kept because they already
/// carried a value (and <c>--force</c> was not given), and the unit after the write (<c>null</c> on a dry run).
/// </summary>
/// <param name="Before">The unit as it was read before drafting.</param>
/// <param name="Draft">The model's draft, already trimmed and clamped to the API's limits.</param>
/// <param name="Written">Field names that were (or, on a dry run, would be) written.</param>
/// <param name="Kept">Field names left untouched because they already carried a value.</param>
/// <param name="After">The unit after the PATCH; <c>null</c> on a dry run or when nothing was to write.</param>
/// <param name="Dropped">Topics of the draft that were left out - over the API's 200 characters (never cut) or beyond the 50 the unit takes (B-292).</param>
/// <param name="Shortened">Free-text fields cut at the column length (2000), at a word boundary - named so the human sees it.</param>
/// <param name="Extracts">What the vision model recognised per page (B-375) - empty without pages; shown so the human can hold it against the photo.</param>
public sealed record UnitFillOutcome(SeriesUnitResponse Before, UnitNotesDraft Draft,
    IReadOnlyList<string> Written, IReadOnlyList<string> Kept, SeriesUnitResponse? After,
    IReadOnlyList<string> Dropped, IReadOnlyList<string> Shortened, IReadOnlyList<PageExtract> Extracts);

/// <summary>
/// Drafts a unit's notes (<c>Topics</c>/<c>Grammar</c>/<c>VocabularyNotes</c>) from a source the human supplies and
/// writes them back through the existing unit PATCH (B-22). The pipeline stays deterministic: C# owns the flow,
/// the model only turns the source into the three fields. Two rules carry the trust in those fields: the model
/// invents nothing beyond the source (the system prompt says so, and the source is the only material it gets),
/// and a field that already carries a value is never overwritten unless <c>--force</c> says so - hand-written notes
/// are the basis of trust, and a silent overwrite would be data loss exactly there.
/// </summary>
public sealed class UnitNotesFiller(IChatClient chat, CreatorApi creator, PageReader pages, IOptions<AgentOptions> options,
    ILogger<UnitNotesFiller> logger)
{
    /// <summary>
    /// The unit's limits: topics 50 × 200 characters (the controller rejects more), free text 2000 (the column
    /// length - SQLite does not enforce it, the clamp keeps the data within the schema anyway).
    /// </summary>
    private const int MaxTopics = 50;
    private const int MaxTopicLength = 200;
    private const int MaxFreeTextLength = 2000;

    /// <summary>
    /// The one rule that distinguishes this verb from the exercise drafts: the source is the material, the model
    /// only shapes it. It is phrased as a prohibition, not a wish - a title like "Unit 3 – Growing up" must not be
    /// embellished with grammar the book never brings.
    /// </summary>
    public const string System = """
        Du bist der Creator der Lern-App Pugling und erfasst den Stoff einer Lehrbuch-Unit.

        Regeln:
        1. Antworte ausschließlich mit JSON nach dem vorgegebenen Schema. Kein Fließtext, keine
           Erklärungen, keine Markdown-Codezäune.
        2. Nutze AUSSCHLIESSLICH die mitgelieferte Quelle. Füge keine Themen, keine Grammatik und keine
           Wörter hinzu, die dort nicht stehen. Was die Quelle nicht nennt, bleibt leer (null bzw. leere Liste).
        3. "topics": die Themen der Unit als kurze Einträge (ein Thema je Eintrag, höchstens 50).
           "grammar": die Grammatik, die die Unit einführt oder übt, als knapper Text.
           "vocabularyNotes": Wortfelder oder konkrete Wörter der Unit, kommagetrennt; nennt die Quelle eine
           Übersetzung, bleibt sie dabei ("to grow up – aufwachsen").
        4. Deutsch für Themen und Grammatik-Beschreibung; Wörter in der Sprache der Quelle.
        5. Übernimm keine Aufgabenstellungen, Texte, Dialoge oder Beispielsätze wörtlich – nur den Stoff:
           Themen, Regeln in eigenen Worten, Wörter.
        """;

    /// <summary>Reads the unit, drafts its notes from the source, decides field by field, and writes (unless dry run).</summary>
    public async Task<UnitFillOutcome> RunAsync(UnitFillRequest request, CancellationToken ct = default)
    {
        // Defense in depth: the command already refuses a missing source; a direct caller gets the same answer.
        var photographed = request.Pages ?? [];
        if (string.IsNullOrWhiteSpace(request.SourceText) && photographed.Count == 0)
            throw new AgentUsageException("Ohne Quelle (--images, --source-file oder Stdin) gibt es nichts zu erfassen – "
                                          + "das Modell darf den Stoff einer Unit nicht erfinden.");

        // Ownership before the model call: only the series owner may change its units, and a seeded series belongs
        // to nobody. Found on the first live walk - a minute of model time ended in the API's 403.
        TextbookSeriesResponse series;
        try
        {
            series = await creator.GetSeriesAsync(request.SeriesId, ct);
        }
        catch (PuglingApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new AgentUsageException($"Reihe {request.SeriesId} gibt es nicht – prüfe die Id im Vater-Web.");
        }
        if (!series.IsOwn)
            throw new AgentUsageException($"Reihe {request.SeriesId} ({series.Name}) gehört dir nicht – nur der Besitzer "
                                          + "einer Reihe darf ihre Units ändern"
                                          + (series.OwnerAdultId is null ? " (geseedete Reihe, niemandem gehörend)." : "."));

        SeriesUnitResponse unit;
        try
        {
            unit = await creator.GetUnitAsync(request.SeriesId, request.UnitId, ct);
        }
        catch (PuglingApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new AgentUsageException($"Unit {request.UnitId} gehört nicht zu Reihe {request.SeriesId}.");
        }

        // The photos stop here: they become a material extract, and only the extract reaches the structuring call
        // and, through the notes, the exercise drafts later on (B-375) - the book's own tasks are never passed on.
        IReadOnlyList<PageExtract> extracts = photographed.Count > 0 ? await pages.ReadAsync(photographed, ct) : [];
        var source = extracts.Count > 0 ? PageReader.AsSource(extracts, request.SourceText) : request.SourceText!;

        var (draft, dropped, shortened) = Clamp(await DraftAsync(unit, source, ct));

        // Field by field: a value already there wins over the draft unless --force says otherwise. Kept fields go
        // to the PATCH as null ("not specified"), so the write touches exactly the fields it names.
        var written = new List<string>();
        var kept = new List<string>();
        var topics = Decide("topics", unit.Topics.Count > 0, draft.Topics is { Count: > 0 }, request.Force, written, kept) ? draft.Topics : null;
        var grammar = Decide("grammar", !string.IsNullOrWhiteSpace(unit.Grammar), !string.IsNullOrWhiteSpace(draft.Grammar), request.Force, written, kept) ? draft.Grammar : null;
        var vocabulary = Decide("vocabularyNotes", !string.IsNullOrWhiteSpace(unit.VocabularyNotes), !string.IsNullOrWhiteSpace(draft.VocabularyNotes), request.Force, written, kept) ? draft.VocabularyNotes : null;

        if (request.DryRun || written.Count == 0)
            return new UnitFillOutcome(unit, draft, written, kept, null, dropped, shortened, extracts);

        var after = await creator.UpdateUnitAsync(request.SeriesId, request.UnitId,
            new UpdateSeriesUnitDto(null, null, null, null, topics, grammar, vocabulary), ct);
        return new UnitFillOutcome(unit, draft, written, kept, after, dropped, shortened, extracts);
    }

    /// <summary>
    /// Whether a field gets written: only if the draft has something for it, and only if it is empty or
    /// <c>--force</c> is set. Records the decision under its field name so the console can say what happened.
    /// </summary>
    private static bool Decide(string field, bool hasValue, bool draftHasValue, bool force,
        List<string> written, List<string> kept)
    {
        if (!draftHasValue) return false;
        if (hasValue && !force) { kept.Add(field); return false; }
        written.Add(field);
        return true;
    }

    /// <summary>
    /// Trims the draft and keeps it within the unit's limits - the way the API would, not milder (B-292): a topic
    /// over 200 characters is <b>left out and named</b>, never cut, because the controller rejects a cut topic on
    /// purpose (a half topic reads as a wrong one to the AI creator). Free text is cut at the column length (2000)
    /// at a word boundary and the field is named, so the human sees it before trusting the result.
    /// </summary>
    private static (UnitNotesDraft Draft, IReadOnlyList<string> Dropped, IReadOnlyList<string> Shortened) Clamp(UnitNotesDraft draft)
    {
        var dropped = new List<string>();
        var shortened = new List<string>();
        var cleaned = (draft.Topics ?? [])
            // A model in JSON mode is not held to the item type: a null topic is left out like an empty one.
            .Select(t => t?.Trim() ?? "")
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        // Every loss is named: the over-long ones, and everything beyond the 50 the unit takes - a 51st topic
        // that vanished unnamed would be the same silent cut as a truncated one.
        dropped.AddRange(cleaned.Where(t => t.Length > MaxTopicLength));
        var fitting = cleaned.Where(t => t.Length <= MaxTopicLength).ToList();
        dropped.AddRange(fitting.Skip(MaxTopics));
        var topics = fitting.Take(MaxTopics).ToList();
        return (new UnitNotesDraft(topics, Text(draft.Grammar, "grammar"), Text(draft.VocabularyNotes, "vocabularyNotes")),
            dropped, shortened);

        string? Text(string? value, string field)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrEmpty(trimmed)) return null;
            if (trimmed.Length <= MaxFreeTextLength) return trimmed;
            shortened.Add(field);
            // At the last word boundary before the limit: a cut mid-word would read as a wrong word. If the limit
            // itself falls on whitespace, the text before it already ends on a whole word. A text without any
            // whitespace is cut hard (a surrogate pair may split there - accepted).
            var head = trimmed[..MaxFreeTextLength];
            if (char.IsWhiteSpace(trimmed[MaxFreeTextLength])) return head.TrimEnd();
            // The same notion of whitespace as the check above: a non-breaking space from a PDF copy is a word
            // boundary too, not a reason for the hard cut.
            var boundary = head.Length - 1;
            while (boundary > 0 && !char.IsWhiteSpace(head[boundary])) boundary--;
            return (boundary > 0 ? head[..boundary] : head).TrimEnd();
        }
    }

    /// <summary>Asks the model once; the strict JSON schema first, JSON mode as the fallback (the strategies' pattern).</summary>
    private async Task<UnitNotesDraft> DraftAsync(SeriesUnitResponse unit, string source, CancellationToken ct)
    {
        var user = new StringBuilder();
        user.AppendLine($"Unit: {unit.Label}{(unit.Grade is { } g ? $" (Band für Klasse {g})" : "")}");
        user.AppendLine();
        user.AppendLine("## Quelle (einzige Grundlage)");
        user.AppendLine(source.Trim());
        List<ChatMessage> messages = [new(ChatRole.System, System), new(ChatRole.User, user.ToString())];
        var chatOptions = new ChatOptions { Temperature = (float)options.Value.Temperature };

        try
        {
            var response = await chat.GetResponseAsync<UnitNotesDraft>(messages, chatOptions,
                useJsonSchemaResponseFormat: true, cancellationToken: ct);
            if (response.TryGetResult(out var result) && result is not null) return result;
            logger.LogWarning("Das Modell lieferte kein schema-konformes JSON – Rückfall auf den JSON-Modus.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Strukturierte Ausgabe per JSON-Schema fehlgeschlagen – Rückfall auf den JSON-Modus.");
        }

        var fallback = await chat.GetResponseAsync<UnitNotesDraft>(messages, chatOptions,
            useJsonSchemaResponseFormat: false, cancellationToken: ct);
        return fallback.TryGetResult(out var value) && value is not null
            ? value
            : throw new AgentException(
                $"Das Modell '{options.Value.Model}' hat keinen verwertbaren JSON-Entwurf geliefert. " +
                "Nutze ein Instruct-Modell mit verlässlicher JSON-Ausgabe (z. B. qwen2.5:14b-instruct).");
    }

    /// <summary>The draft as indented JSON for the console.</summary>
    /// <remarks>
    /// Relaxed escaping: the draft is read by a human next to the photo (B-375), and "für" for "für" hides exactly
    /// the text to be checked. The output goes to a console, never into HTML.
    /// </remarks>
    public static string Print(UnitNotesDraft draft) =>
        JsonSerializer.Serialize(draft, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
}
