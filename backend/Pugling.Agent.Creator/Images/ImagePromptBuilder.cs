namespace Pugling.Agent.Creator.Images;

/// <summary>
/// Builds everything that goes with one generated image – prompt, store key, trace and alt text – deterministically in
/// C# (B-485). The model only supplies the motif; how interest and style are added is fixed here, so the interest stays
/// the setting and never becomes the subject.
/// </summary>
public static class ImagePromptBuilder
{
    /// <summary>
    /// Appended to every prompt: a word painted into the picture would hand the child the answer, and image models
    /// like to label things.
    /// </summary>
    public const string NoText = "no text, no letters, no words, no numbers";

    /// <summary>Column length of <c>MediaAsset.Source</c>; the server rejects anything longer.</summary>
    public const int MaxSourceLength = 200;

    /// <summary>Slug part of the key and the trace for the image without interest tag.</summary>
    public const string Neutral = "neutral";

    /// <summary>The prompt: motif first (it is the word), then the interest as setting, then the fixed style.</summary>
    /// <param name="motif">English scene that shows the meaning of the word.</param>
    /// <param name="tagPrompt">The tag's brand-neutral scene fragment; <c>null</c> for the neutral image.</param>
    /// <param name="style">The one style of all generated images.</param>
    public static string Prompt(string motif, string? tagPrompt, string style) =>
        string.Join(", ", new[] { motif.Trim(), tagPrompt?.Trim(), style.Trim(), NoText }
            .Where(part => !string.IsNullOrEmpty(part)));

    /// <summary>
    /// Store key of the image: one per vocabulary entry and tag. Stable, so a rerun after a failed link finds the
    /// uploaded image again instead of drawing (and paying for) it twice.
    /// </summary>
    public static string Key(int vocabularyId, string? tagSlug) => $"gen-v{vocabularyId}-{tagSlug ?? Neutral}";

    /// <summary>
    /// The trace stored with the image (<c>model: motif; tag</c>) – together with the fixed style it reproduces the
    /// prompt. The motif is shortened so the whole fits the column; model and tag are never cut.
    /// </summary>
    public static string Source(string model, string motif, string? tagSlug)
    {
        var tail = $"; {tagSlug ?? Neutral}";
        var head = $"{model}: ";
        var room = MaxSourceLength - head.Length - tail.Length;
        var trimmed = motif.Trim();
        if (trimmed.Length > room) trimmed = trimmed[..Math.Max(0, room - 1)] + "…";
        return head + trimmed + tail;
    }

    /// <summary>
    /// Alt text of the image. It deliberately names neither the word nor its translation: the image also shows on the
    /// self-assessment stage, where the answer is still hidden, and a screen reader would read it out (B-485).
    /// </summary>
    /// <param name="tagLabel">Display name of the interest; <c>null</c> for the neutral image.</param>
    public static string AltText(string? tagLabel) =>
        tagLabel is null ? "Bild zur Vokabel" : $"Bild zur Vokabel ({tagLabel})";
}
