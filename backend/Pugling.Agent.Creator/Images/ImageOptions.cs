using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.AI;

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

    /// <summary>
    /// Base URL of an OpenAI-compatible image service; empty means OpenAI itself. Lets a run point at another provider
    /// with the same API – or at a local stand-in that answers in OpenAI's format, to walk the whole path without cost.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// How the service returns the picture; empty means "not sent". OpenAI's GPT image models reject the parameter and
    /// return bytes anyway, while local OpenAI-compatible servers (LocalAI) default to a URL the run cannot use – those
    /// need <c>Data</c>.
    /// </summary>
    public ImageGenerationResponseFormat? ResponseFormat { get; set; }

    /// <summary>
    /// Separator between prompt and negative prompt, for services that read one from the prompt (<c>|</c> for LocalAI);
    /// empty means the service has none and the no-text rule stays as text in the prompt (OpenAI).
    /// </summary>
    public string? NegativeSeparator { get; set; }

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
    /// motif – not the drawing – carries the word. "For children" and "fully clothed" belong to the positive side: the
    /// negative prompt only steers away, this steers towards what a child's card should show.
    /// </summary>
    [Required]
    public string Style { get; set; } =
        "simple flat sticker illustration for children, friendly, fully clothed, soft colors, plain white background";

    /// <summary>Approximate price of one image in euros – only for the cost estimate of a dry run.</summary>
    [Range(0d, 1d)]
    public double CostPerImage { get; set; } = 0.01;
}
