using Microsoft.Extensions.AI;
using SkiaSharp;

namespace Pugling.Api.Tests;

/// <summary>
/// Stand-in for the image service (B-485): returns a real, decodable PNG – the server scales it into variants, so a
/// dummy byte string would fail at the upload, not at the step under test. Records every prompt.
/// </summary>
public sealed class FakeImageGenerator : IImageGenerator
{
    /// <summary>Every prompt in call order.</summary>
    public List<string> Prompts { get; } = [];

    /// <summary>Prompts containing this text fail – to show that one failed image does not stop the run.</summary>
    public string? FailWhenPromptContains { get; init; }

    public Task<ImageGenerationResponse> GenerateAsync(ImageGenerationRequest request, ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var prompt = request.Prompt ?? "";
        Prompts.Add(prompt);
        if (FailWhenPromptContains is { } marker && prompt.Contains(marker, StringComparison.Ordinal))
            throw new InvalidOperationException("Bild-Dienst nicht erreichbar (Testfall).");

        // The real service's size: the server never upscales, so a tiny fake would lack the card variant.
        using var bitmap = new SKBitmap(1024, 1024);
        using (var canvas = new SKCanvas(bitmap)) canvas.Clear(SKColors.Orange);
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return Task.FromResult(new ImageGenerationResponse([new DataContent(data.ToArray(), "image/png")]));
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
