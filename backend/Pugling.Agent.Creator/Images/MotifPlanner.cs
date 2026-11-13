using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Pugling.Agent.Creator.Images;

/// <summary>The model's answer for one word: can it be drawn, and if so, which scene shows its meaning.</summary>
/// <param name="Imageable">Whether a single picture can show the meaning without help.</param>
/// <param name="Motif">Short English scene (a few words) – only when <paramref name="Imageable"/>.</param>
public sealed record MotifDraft(bool Imageable, string? Motif);

/// <summary>The model's answer for one word and one interest: the motif woven into the interest's world.</summary>
/// <param name="Scene">Short English scene – the word's meaning stays the subject, the interest shows clearly.</param>
public sealed record SceneDraft(string? Scene);

/// <summary>
/// Asks the local text model the questions that need language (B-485): whether a word can be drawn and which scene
/// shows it, and – per interest – how that scene looks in the interest's world (decision 10: an appended setting is
/// ignored by diffusion models). C# checks every answer and builds the prompt. The model sees only the tag's
/// brand-neutral image prompt – never its label (a brand name) and never anything about a child.
/// </summary>
public sealed class MotifPlanner(IChatClient chat, ILogger<MotifPlanner> logger)
{
    /// <summary>A motif longer than this is a description, not a scene – it would drown the interest and the style.</summary>
    public const int MaxMotifLength = 100;

    /// <summary>
    /// A woven scene may be a little longer than a motif – it names two things – but the same limit applies for the same
    /// reason: past it the style at the end of the prompt loses its weight.
    /// </summary>
    public const int MaxSceneLength = 140;

    private const string SceneSystem = """
        Du verbindest für EIN Vokabelbild die Bedeutung eines Worts mit einem Interesse des Kindes.
        Gegeben sind das Wort, seine Bedeutung, eine Szene, die die Bedeutung zeigt, und eine Beschreibung des Interesses.
        Schreib EINE neue Szene, in der beides deutlich zu sehen ist: Die Bedeutung des Worts bleibt die Hauptsache,
        das Interesse erscheint als Person, Gegenstand oder Ort im selben Bild.
        Verbinde beides in EINEM zusammenhängenden Satzteil, der mit dem Interesse handelt – keine Aufzählung, keine
        zwei Bildteile mit Komma; ein Bildmodell zeichnet nur den ersten Teil einer Aufzählung.
        Beispiel: Wort "la rue", Interesse "on a soccer pitch" → "a soccer player kicking a ball down a city street".
        Die Szene ist ENGLISCH, höchstens 15 Wörter, enthält keine Schrift, keine Marken- und keine Figurennamen,
        keinen Stil und keine Verneinung.
        Das Bild ist für Kinder: Personen sind Kinder (a boy, a girl, a kid), freundlich und in Alltagskleidung –
        keine Kämpfer, keine Waffen, keine Gewalt; ein Wettkampf ist ein Spiel, kein Kampf.
        Antworte nur mit JSON: {"scene": "..."}.
        """;

    private const string System = """
        Du entscheidest für EIN Vokabelwort, ob ein einzelnes kleines Bild seine Bedeutung eindeutig zeigen kann,
        und beschreibst dann die Szene.
        Bebilderbar sind konkrete Dinge, Tiere, Personen, sichtbare Tätigkeiten und sichtbare Eigenschaften.
        NICHT bebilderbar sind Funktionswörter (Artikel, Präpositionen, Konjunktionen, Pronomen), Abstrakta,
        Redewendungen und alles, was ein Bild nur mehrdeutig zeigen könnte.
        Die Szene ist ENGLISCH, höchstens zwölf Wörter, zeigt genau die Bedeutung des Worts und enthält keine Schrift.
        Nenne keinen Stil und kein Thema – beides wird später ergänzt.
        Das Bild ist für Kinder: Personen sind Kinder (a boy, a girl, a kid), freundlich und in Alltagskleidung –
        keine Waffen, keine Gewalt.
        Antworte nur mit JSON: {"imageable": true|false, "motif": "..."}; bei false ist motif leer.
        """;

    /// <summary>
    /// The checked motif, or <c>null</c> when the word is not to be drawn – because the model says so or because its
    /// answer does not pass the checks. Never draw on a doubtful answer: no image beats a misleading one.
    /// </summary>
    public async Task<string?> PlanAsync(MediaDemandEntry entry, CancellationToken ct)
    {
        var user = new StringBuilder();
        user.AppendLine($"Wort ({entry.SourceLanguage}): {entry.Word}");
        user.AppendLine($"Bedeutung ({entry.TargetLanguage}): {entry.Translation}");
        user.AppendLine($"Wortart: {entry.PartOfSpeech}");
        List<ChatMessage> messages = [new(ChatRole.System, System), new(ChatRole.User, user.ToString())];
        // Temperature 0, not the agent's setting: "drawable or not" is a decision, and at 0.4 the same word flipped
        // between runs (measured: "pollution" rejected, then accepted with a scene).
        var chatOptions = new ChatOptions { Temperature = 0f };

        MotifDraft? draft = null;
        try
        {
            var response = await chat.GetResponseAsync<MotifDraft>(messages, chatOptions,
                useJsonSchemaResponseFormat: true, cancellationToken: ct);
            if (!response.TryGetResult(out draft))
                logger.LogWarning("Kein schema-konformes JSON für '{Word}' – das Wort bleibt ohne Bild.", entry.Word);
        }
        // Only the caller's cancellation ends the run: an HTTP timeout is also an OperationCanceledException, and it
        // means "no answer for this word", not "stop".
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Motiv für '{Word}' nicht ermittelbar – das Wort bleibt ohne Bild.", entry.Word);
        }
        return Check(draft);
    }

    /// <summary>
    /// The checked scene of the word in the interest's world, or <c>null</c> when there is none – then the interest image
    /// is left out (decision 10): falling back to the appended setting would file an image without the interest under it.
    /// </summary>
    /// <param name="entry">The word.</param>
    /// <param name="motif">The word's checked motif from <see cref="PlanAsync"/>.</param>
    /// <param name="imagePrompt">The tag's brand-neutral image prompt.</param>
    /// <param name="ct">Cancellation of the run.</param>
    public async Task<string?> PlanSceneAsync(MediaDemandEntry entry, string motif, string imagePrompt, CancellationToken ct)
    {
        var user = new StringBuilder();
        user.AppendLine($"Wort ({entry.SourceLanguage}): {entry.Word}");
        user.AppendLine($"Bedeutung ({entry.TargetLanguage}): {entry.Translation}");
        user.AppendLine($"Szene der Bedeutung: {motif}");
        user.AppendLine($"Interesse: {imagePrompt}");
        List<ChatMessage> messages = [new(ChatRole.System, SceneSystem), new(ChatRole.User, user.ToString())];
        // Temperature 0 like the motif: a rerun after a failed upload must ask for the same picture.
        var chatOptions = new ChatOptions { Temperature = 0f };

        SceneDraft? draft = null;
        try
        {
            var response = await chat.GetResponseAsync<SceneDraft>(messages, chatOptions,
                useJsonSchemaResponseFormat: true, cancellationToken: ct);
            if (!response.TryGetResult(out draft))
                logger.LogWarning("Keine schema-konforme Szene für '{Word}' – das Interessen-Bild entfällt.", entry.Word);
        }
        // Only the caller's cancellation ends the run: an HTTP timeout is also an OperationCanceledException, and it
        // means "no answer for this word", not "stop".
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Szene für '{Word}' nicht ermittelbar – das Interessen-Bild entfällt.", entry.Word);
        }
        return CheckScene(draft);
    }

    /// <summary>The deterministic check of the model's answer: drawable, a motif, short enough, free of line breaks.</summary>
    public static string? Check(MotifDraft? draft) =>
        draft is { Imageable: true } ? OneLine(draft.Motif, MaxMotifLength) : null;

    /// <summary>The same check for a woven scene: present, short enough, one line.</summary>
    public static string? CheckScene(SceneDraft? draft) => OneLine(draft?.Scene, MaxSceneLength);

    private static string? OneLine(string? text, int maxLength)
    {
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > maxLength) return null;
        return trimmed.Contains('\n') || trimmed.Contains('\r') ? null : trimmed;
    }
}
