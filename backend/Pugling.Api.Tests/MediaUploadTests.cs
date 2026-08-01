using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SkiaSharp;

namespace Pugling.Api.Tests;

/// <summary>
/// Image upload (stage 5): the server accepts <b>one</b> file and generates the resolutions
/// itself. The tests mainly secure the processing rules – they are expensive to change afterwards,
/// because already generated files would then no longer match the rule.
/// </summary>
public class MediaUploadTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Upload_ErzeugtDieAufloesungenUndEinePlatzhalterfarbe()
    {
        var father = await TestApi.FatherAsync(factory);

        var res = await UploadAsync(father, Png(1000, 500, SKColors.CornflowerBlue),
            "Eine breite Stadtansicht", tags: "Stadt, Foto");
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var asset = await res.Content.ReadFromJsonAsync<JsonElement>();
        var variants = asset.GetProperty("variants").EnumerateArray()
            .ToDictionary(v => v.GetProperty("purpose").GetString()!);

        // Three purposes from one file - without the creator having to touch a graphics program.
        Assert.Equal(3, variants.Count);
        Assert.Equal(128, variants["Thumb"].GetProperty("width").GetInt32());
        Assert.Equal(512, variants["Card"].GetProperty("width").GetInt32());
        Assert.Equal(1000, variants["Full"].GetProperty("width").GetInt32()); // not upscaled

        // The aspect ratio is preserved (no cropping - a crop could behead the motif).
        Assert.Equal(64, variants["Thumb"].GetProperty("height").GetInt32());
        Assert.Equal(256, variants["Card"].GetProperty("height").GetInt32());

        Assert.All(variants.Values, v => Assert.Equal("webp", v.GetProperty("format").GetString()));
        Assert.All(variants.Values, v => Assert.True(v.GetProperty("bytes").GetInt64() > 0));

        // A placeholder color for stutter-free lazy loading - averaged from the image, not guessed.
        Assert.Matches("^#[0-9a-f]{6}$", asset.GetProperty("placeholder").GetString()!);

        // Origin with nothing given = upload; the tags run through the same taxonomy as everywhere else.
        Assert.Equal("Upload", asset.GetProperty("origin").GetString());
        Assert.Contains("stadt", asset.GetProperty("tags").EnumerateArray().Select(t => t.GetString()));
    }

    [Fact]
    public async Task HochgeladeneDatei_IstUeberIhreUrlAbrufbar()
    {
        var father = await TestApi.FatherAsync(factory);
        var asset = await (await UploadAsync(father, Png(300, 300, SKColors.Tomato), "Ein rotes Quadrat"))
            .Content.ReadFromJsonAsync<JsonElement>();

        var url = asset.GetProperty("variants").EnumerateArray()
            .First(v => v.GetProperty("purpose").GetString() == "Card").GetProperty("url").GetString()!;

        // The URL points into our own media folder - not to wwwroot, which the deploy overwrites.
        Assert.StartsWith("/media/", url);

        // Retrievable without a token: the child's app loads images as an ordinary <img> source, with no headers.
        var anonymous = factory.CreateClient();
        var file = await anonymous.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        Assert.True((await file.Content.ReadAsByteArrayAsync()).Length > 0);
    }

    [Fact]
    public async Task KleineQuelle_WirdNichtHochskaliert_UndDoppelteGroessenEntfallen()
    {
        var father = await TestApi.FatherAsync(factory);
        var asset = await (await UploadAsync(father, Png(64, 64, SKColors.Green), "Ein winziges Symbol"))
            .Content.ReadFromJsonAsync<JsonElement>();

        var variants = asset.GetProperty("variants").EnumerateArray().ToList();
        // Thumb/card/full would all come out at 64px - one file is enough, the selection falls back to the next
        // best purpose. Blowing it up would only produce blurry, larger files.
        Assert.Single(variants);
        Assert.Equal(64, variants[0].GetProperty("width").GetInt32());
    }

    [Fact]
    public async Task KeineBilddatei_Liefert400MitEigenemCode()
    {
        var father = await TestApi.FatherAsync(factory);
        var res = await UploadAsync(father, "kein Bild, nur Text"u8.ToArray(), "Kaputte Datei", fileName: "x.png");

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("media_not_an_image", await CodeOf(res));
    }

    [Fact]
    public async Task BeschreibungIstPflicht_SieIstDerAltText()
    {
        var father = await TestApi.FatherAsync(factory);
        var res = await UploadAsync(father, Png(100, 100, SKColors.Gray), description: "   ");

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("validation_error", await CodeOf(res));
    }

    [Fact]
    public async Task LoeschenRaeumtDieDateienMitWeg()
    {
        var father = await TestApi.FatherAsync(factory);
        var asset = await (await UploadAsync(father, Png(200, 200, SKColors.Purple), "Wird gleich gelöscht"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var id = asset.GetProperty("id").GetInt32();
        var url = asset.GetProperty("variants")[0].GetProperty("url").GetString()!;

        var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync(url)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync($"/api/v1/creator/media/{id}")).StatusCode);

        // Otherwise the folder would collect dead files with every discarded attempt.
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Upload_IstDemSohnVerwehrt()
    {
        var child = await TestApi.ChildAsync(factory);
        var res = await UploadAsync(child, Png(100, 100, SKColors.Black), "Vom Sohn");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ---- Helpers -------------------------------------------------------------------------------------

    /// <summary>A real, decodable PNG – the processor should work on real bytes, not a dummy.</summary>
    private static byte[] Png(int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap)) canvas.Clear(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, byte[] bytes,
        string description, string? tags = null, string fileName = "motiv.png")
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", fileName);
        form.Add(new StringContent(description), "description");
        if (tags is not null) form.Add(new StringContent(tags), "tags");
        return await client.PostAsync("/api/v1/creator/media/upload", form);
    }

    private static async Task<string?> CodeOf(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
}
