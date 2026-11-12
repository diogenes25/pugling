using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Pugling.Agent.Creator.Images;

/// <summary>The model's answer for one word: can it be drawn, and if so, which scene shows its meaning.</summary>
/// <param name="Imageable">Whether a single picture can show the meaning without help.</param>
/// <param name="Motif">Short English scene (a few words) – only when <paramref name="Imageable"/>.</param>
public sealed record MotifDraft(bool Imageable, string? Motif);

/// <summary>
/// Asks the local text model whether a word can be drawn and which scene shows it (B-485, step 1). The model decides
/// only this one question that needs language; C# then checks the answer and builds the prompt. Interests are never
/// shown to the model – they are appended by <see cref="ImagePromptBuilder"/>, so they cannot crowd out the word.
/// </summary>
public sealed class MotifPlanner(IChatClient chat, ILogger<MotifPlanner> logger)
{
    /// <summary>A motif longer than this is a description, not a scene – it would drown the interest and the style.</summary>
    public const int MaxMotifLength = 100;

    private const string System = """
        Du entscheidest für EIN Vokabelwort, ob ein einzelnes kleines Bild seine Bedeutung eindeutig zeigen kann,
        und beschreibst dann die Szene.
        Bebilderbar sind konkrete Dinge, Tiere, Personen, sichtbare Tätigkeiten und sichtbare Eigenschaften.
        NICHT bebilderbar sind Funktionswörter (Artikel, Präpositionen, Konjunktionen, Pronomen), Abstrakta,
        Redewendungen und alles, was ein Bild nur mehrdeutig zeigen könnte.
        Die Szene ist ENGLISCH, höchstens zwölf Wörter, zeigt genau die Bedeutung des Worts und enthält keine Schrift.
        Nenne keinen Stil und kein Thema – beides wird später ergänzt.
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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Motiv für '{Word}' nicht ermittelbar – das Wort bleibt ohne Bild.", entry.Word);
        }
        return Check(draft);
    }

    /// <summary>The deterministic check of the model's answer: drawable, a motif, short enough, free of line breaks.</summary>
    public static string? Check(MotifDraft? draft)
    {
        if (draft is not { Imageable: true }) return null;
        var motif = draft.Motif?.Trim();
        if (string.IsNullOrEmpty(motif) || motif.Length > MaxMotifLength) return null;
        return motif.Contains('\n') || motif.Contains('\r') ? null : motif;
    }
}
