using SkiaSharp;

namespace Pugling.Api.Services.Shared;

/// <summary>Eine erzeugte Auflösung: fertige Bytes plus die Maße, die in die Variante gehören.</summary>
public record RenderedVariant(MediaPurpose Purpose, int Width, int Height, string Format, byte[] Content);

/// <summary>Ergebnis der Aufbereitung eines Uploads.</summary>
/// <param name="Variants">Die erzeugten Auflösungen (mindestens eine).</param>
/// <param name="Placeholder">Dominante Farbe als Hex – der Client kann die Fläche färben, bevor das Bild da ist.</param>
public record ProcessedImage(IReadOnlyList<RenderedVariant> Variants, string Placeholder);

/// <summary>
/// Erzeugt aus einer hochgeladenen Bilddatei die Auflösungen des Medien-Stores.
/// <para>
/// Drei Entscheidungen stecken hier drin, die man später nicht mehr billig ändern kann:
/// <list type="bullet">
/// <item><b>Kein Hochskalieren.</b> Ist die Quelle kleiner als eine Zielgröße, wird sie nicht aufgeblasen –
/// das ergäbe nur unscharfe, größere Dateien. Die Variante entsteht dann in Quellgröße.</item>
/// <item><b>Kein Beschnitt.</b> Skaliert wird immer seitenverhältnis-erhaltend in eine Box. Ein Zuschnitt
/// auf ein festes Format würde bei einem Motiv wie „laufendes Einhorn" den Kopf abschneiden – solche
/// Entscheidungen kann nur ein Mensch treffen. Deshalb erzeugt der Upload auch <b>kein</b>
/// <see cref="MediaPurpose.Hero"/>: das breite Aufmacherformat verlangt redaktionellen Beschnitt.</item>
/// <item><b>WebP.</b> Ein Format für alle Slots – deutlich kleiner als PNG/JPEG bei gleicher Qualität und
/// überall unterstützt. Wer AVIF danebenlegen will, reicht die Variante über die API nach.</item>
/// </list>
/// </para>
/// </summary>
public class MediaImageProcessor
{
    /// <summary>Zielgrößen je Zweck (längste Kante in Pixeln).</summary>
    private static readonly (MediaPurpose Purpose, int LongestEdge)[] Targets =
    [
        (MediaPurpose.Thumb, 128),
        (MediaPurpose.Card, 512),
        (MediaPurpose.Full, 1024),
    ];

    private const int WebpQuality = 82;
    private const string Format = "webp";

    /// <summary>
    /// Dekodiert den Upload und rendert die Auflösungen. Wirft <see cref="ArgumentException"/>, wenn die
    /// Datei kein dekodierbares Bild ist – der Controller macht daraus einen sauberen 400.
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
            // Nie hochskalieren: die Quelle ist die Obergrenze.
            var scale = Math.Min(1.0, (double)edge / longest);
            var width = Math.Max(1, (int)Math.Round(original.Width * scale));
            var height = Math.Max(1, (int)Math.Round(original.Height * scale));

            // Käme dieselbe Größe zweimal heraus (kleine Quelle), reicht eine Datei – die Auswahl fällt
            // ohnehin auf den nächstbesten Zweck zurück, wenn der gefragte fehlt.
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
    /// Dominante Farbe als <c>#rrggbb</c> – ermittelt, indem das Bild auf 1×1 heruntergerechnet wird
    /// (das ist der Mittelwert und für einen Platzhalter genau genug).
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
