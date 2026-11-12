using Microsoft.Extensions.AI;
using OpenAI.Images;
using MeaiImageOptions = Microsoft.Extensions.AI.ImageGenerationOptions;
using OpenAiImageOptions = OpenAI.Images.ImageGenerationOptions;

namespace Pugling.Agent.Creator.Images;

/// <summary>
/// The OpenAI image model behind <see cref="IImageGenerator"/> (decision 7 of B-485). The only OpenAI-specific place:
/// a different provider is a different class here, <see cref="ImageRun"/> does not change.
/// </summary>
public static class OpenAiImageGenerator
{
    /// <summary>
    /// Creates the generator. Called lazily on the first image, so a missing key only stops a run that would really
    /// draw – and stops it as a usage error that says where the key goes.
    /// </summary>
    public static IImageGenerator Create(ImageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new AgentUsageException(
                "Images:ApiKey fehlt – setze den OpenAI-Schlüssel als Secret (im Ordner backend/Pugling.Agent.Creator):\n"
                + "  dotnet user-secrets set \"Images:ApiKey\" \"sk-…\"\n"
                + "Ein Trockenlauf (--dry-run) braucht keinen Schlüssel.");
        return new QualityGenerator(new ImageClient(options.Model, options.ApiKey).AsIImageGenerator(), options.Quality);
    }

    /// <summary>
    /// Adds the rendering quality, which the provider-neutral options do not know: without it the model renders at its
    /// default quality, several times the price of <c>low</c>.
    /// </summary>
    private sealed class QualityGenerator(IImageGenerator inner, string quality) : DelegatingImageGenerator(inner)
    {
        public override Task<ImageGenerationResponse> GenerateAsync(ImageGenerationRequest request,
            MeaiImageOptions? options = null, CancellationToken cancellationToken = default)
        {
            var configured = options?.Clone() ?? new MeaiImageOptions();
            configured.RawRepresentationFactory = _ => new OpenAiImageOptions { Quality = new GeneratedImageQuality(quality) };
            return base.GenerateAsync(request, configured, cancellationToken);
        }
    }
}
