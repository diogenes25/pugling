using System.ComponentModel.DataAnnotations;

namespace Pugling.Agent.Creator.Images;

/// <summary>
/// Settings of the image generator (section <c>Images</c>, B-485). Separate from <see cref="AgentOptions"/>: the text
/// model runs locally, the image model is a cloud service with its own key and its own cost.
/// </summary>
public sealed class ImageOptions
{
    /// <summary>Configuration section this is bound from.</summary>
    public const string SectionName = "Images";

    /// <summary>
    /// OpenAI API key. Not required for any other verb and not for <c>images --dry-run</c>; belongs in user secrets,
    /// never in <c>appsettings.json</c>.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>Image model (decision 7 of B-485: small and cheap is enough for card-sized pictures).</summary>
    [Required]
    public string Model { get; set; } = "gpt-image-1-mini";

    /// <summary>Rendering quality of the model: <c>low</c>, <c>medium</c> or <c>high</c>.</summary>
    [Required]
    public string Quality { get; set; } = "low";

    /// <summary>Edge length of the square image in pixels; the server scales the card variant down from it.</summary>
    [Range(256, 2048)]
    public int Size { get; set; } = 1024;

    /// <summary>
    /// The one fixed style of every generated image (decision 5). Calm and uniform, so cards look alike and the
    /// motif – not the drawing – carries the word.
    /// </summary>
    [Required]
    public string Style { get; set; } = "simple flat sticker illustration, soft colors, plain white background";

    /// <summary>Approximate price of one image in euros – only for the cost estimate of a dry run.</summary>
    [Range(0d, 1d)]
    public double CostPerImage { get; set; } = 0.01;
}
