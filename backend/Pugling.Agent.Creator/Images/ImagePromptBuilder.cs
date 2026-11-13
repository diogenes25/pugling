namespace Pugling.Agent.Creator.Images;

/// <summary>
/// Builds everything that goes with one generated image – prompt, store key, trace and alt text – deterministically in
/// C# (B-485). The model supplies only the scene (the motif, or the motif woven into an interest, decision 10); style
/// and the no-text rule are fixed here.
/// </summary>
public static class ImagePromptBuilder
{
    /// <summary>
    /// Ends every prompt of a service without negative prompt: a word painted into the picture would hand the child the
    /// answer, and image models like to label things.
    /// </summary>
    public const string NoText = "no text, no letters, no words, no numbers";

    /// <summary>
    /// The same rule as a negative prompt, plus the child-safety terms. Diffusion models do not understand "no X" in the
    /// prompt – naming X there draws it – but they do avoid what the negative prompt lists. The safety half is there
    /// because the first run on real data drew a near-naked figure and a low-cut warrior for a 15-year-old (B-485):
    /// community models such as <c>dreamshaper</c> lean that way, and the images reach the child without review.
    /// </summary>
    public const string NegativeTerms = "text, letters, words, numbers, watermark, logo, brand, "
        + "nsfw, nude, naked, topless, shirtless, underwear, lingerie, bikini, swimsuit, cleavage, revealing clothes, "
        + "sexy, seductive, violence, blood, weapon, gun, knife, sword, scary, horror";

    /// <summary>Column length of <c>MediaAsset.Source</c>; the server rejects anything longer.</summary>
    public const int MaxSourceLength = 200;

    /// <summary>Slug part of the key and the trace for the image without interest tag.</summary>
    public const string Neutral = "neutral";

    /// <summary>The prompt: the scene first (it carries the word), then the fixed style, then the no-text rule.</summary>
    /// <param name="scene">English scene: the motif for the neutral image, the woven scene for an interest image.</param>
    /// <param name="style">The one style of all generated images.</param>
    /// <param name="negativeSeparator">
    /// Separator of the service's negative prompt (<c>|</c> for LocalAI); <c>null</c> keeps the rule as text at the end,
    /// for services without negative prompt such as OpenAI.
    /// </param>
    public static string Prompt(string scene, string style, string? negativeSeparator)
    {
        // Blank counts as unset: a stray space would glue "text, letters" onto the positive prompt and ask for text.
        var separator = string.IsNullOrWhiteSpace(negativeSeparator) ? null : negativeSeparator;
        // The scene is model text; a separator inside it would move part of the motif into the negative prompt, and the
        // picture would avoid exactly that part.
        var cleanScene = separator is null ? scene.Trim() : scene.Replace(separator, " ", StringComparison.Ordinal).Trim();
        var positive = string.Join(", ", new[] { cleanScene, style.Trim() }.Where(part => part.Length > 0));
        return separator is null ? $"{positive}, {NoText}" : positive + separator + NegativeTerms;
    }

    /// <summary>
    /// Store key of the image: one per vocabulary entry and tag. Stable, so a rerun after a failed link finds the
    /// uploaded image again instead of drawing (and paying for) it twice.
    /// </summary>
    public static string Key(int vocabularyId, string? tagSlug) => $"gen-v{vocabularyId}-{tagSlug ?? Neutral}";

    /// <summary>
    /// The trace stored with the image (<c>model: scene; tag</c>) – together with the fixed style it reproduces the
    /// prompt. The scene is shortened so the whole fits the column; model and tag are never cut.
    /// </summary>
    public static string Source(string model, string scene, string? tagSlug)
    {
        var tail = $"; {tagSlug ?? Neutral}";
        var head = $"{model}: ";
        var room = MaxSourceLength - head.Length - tail.Length;
        var trimmed = scene.Trim();
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
