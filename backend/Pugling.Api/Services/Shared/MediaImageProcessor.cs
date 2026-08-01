using SkiaSharp;

namespace Pugling.Api.Services.Shared;

/// <summary>A generated resolution: the finished bytes plus the dimensions that belong to the variant.</summary>
public record RenderedVariant(MediaPurpose Purpose, int Width, int Height, string Format, byte[] Content);

/// <summary>Result of processing an upload.</summary>
/// <param name="Variants">The generated resolutions (at least one).</param>
/// <param name="Placeholder">Dominant color as hex – the client can color the area before the image arrives.</param>
public record ProcessedImage(IReadOnlyList<RenderedVariant> Variants, string Placeholder);

/// <summary>
/// Generates the resolutions of the media store from an uploaded image file.
/// <para>
/// Three decisions are baked in here that can't be changed cheaply later:
/// <list type="bullet">
/// <item><b>No upscaling.</b> If the source is smaller than a target size, it is not blown up –
/// that would only produce blurry, larger files. The variant is then created at source size.</item>
/// <item><b>No cropping.</b> Scaling always preserves the aspect ratio into a box. Cropping to a
/// fixed format could cut off the head on a motif like "running unicorn" – only a human can make
/// such a call. That's why the upload also does <b>not</b> generate a
/// <see cref="MediaPurpose.Hero"/>: the wide hero format requires editorial cropping.</item>
/// <item><b>WebP.</b> One format for all slots – noticeably smaller than PNG/JPEG at the same
/// quality and universally supported. Anyone wanting AVIF alongside it can add the variant via the API.</item>
/// </list>
/// </para>
/// </summary>
public class MediaImageProcessor
{
    /// <summary>Target sizes per purpose (longest edge in pixels).</summary>
    private static readonly (MediaPurpose Purpose, int LongestEdge)[] Targets =
    [
        (MediaPurpose.Thumb, 128),
        (MediaPurpose.Card, 512),
        (MediaPurpose.Full, 1024),
    ];

    private const int WebpQuality = 82;
    private const string Format = "webp";

    /// <summary>
    /// Decodes the upload and renders the resolutions. Throws <see cref="ArgumentException"/> if the
    /// file is not a decodable image – the controller turns that into a clean 400.
    /// </summary>
    public ProcessedImage Process(ReadOnlySpan<byte> source)
    {
        using var original = SKBitmap.Decode(source)
            ?? throw new ArgumentException("The file could not be decoded as an image.");

        var longest = Math.Max(original.Width, original.Height);
        var variants = new List<RenderedVariant>();
        var emittedSizes = new HashSet<(int, int)>();

        foreach (var (purpose, edge) in Targets)
        {
            // Never upscale: the source is the upper bound.
            var scale = Math.Min(1.0, (double)edge / longest);
            var width = Math.Max(1, (int)Math.Round(original.Width * scale));
            var height = Math.Max(1, (int)Math.Round(original.Height * scale));

            // If the same size came out twice (a small source), one file is enough - the selection falls back
            // to the next best purpose anyway when the requested one is missing.
            if (!emittedSizes.Add((width, height))) continue;

            variants.Add(new RenderedVariant(purpose, width, height, Format, Encode(original, width, height)));
        }

        return new ProcessedImage(variants, DominantColor(original));
    }

    private static byte[] Encode(SKBitmap original, int width, int height)
    {
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var scaled = original.Resize(info, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
            ?? throw new ArgumentException("The image could not be resized.");
        using var image = SKImage.FromBitmap(scaled);
        using var data = image.Encode(SKEncodedImageFormat.Webp, WebpQuality);
        return data.ToArray();
    }

    /// <summary>
    /// Dominant color as <c>#rrggbb</c> – determined by downscaling the image to 1×1
    /// (that's the average, and precise enough for a placeholder).
    /// </summary>
    private static string DominantColor(SKBitmap original)
    {
        var info = new SKImageInfo(1, 1, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var one = original.Resize(info, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        if (one is null) return "#cccccc";
        var c = one.GetPixel(0, 0);
        return $"#{c.Red:x2}{c.Green:x2}{c.Blue:x2}";
    }
}
