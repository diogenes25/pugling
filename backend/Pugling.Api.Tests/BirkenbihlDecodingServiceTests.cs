using Microsoft.Extensions.DependencyInjection;

namespace Pugling.Api.Tests;

/// <summary>
/// Tests for the Birkenbihl decoding: the tokenizer (<see cref="BirkenbihlDecodingService.Tokenize"/>)
/// works statelessly; the store lookup (<see cref="BirkenbihlDecodingService.LookupAsync"/>)
/// compares the word surface case-insensitively, but the language codes deliberately EXACTLY - a documented
/// guarantee (fail-closed) that had been untested so far.
/// </summary>
public class BirkenbihlDecodingServiceTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public void Tokenize_HaeltWortInnerenApostroph_UndWirftSatzzeichenWeg()
    {
        // An apostrophe inside a word stays ("Don't" = one token), punctuation separates, digits only count
        // WITHIN a word ("R2D2"); a standalone number token does not start with a letter and drops out.
        var tokens = BirkenbihlDecodingService.Tokenize("Don't run, R2D2! 42 times.");
        Assert.Equal(new[] { "Don't", "run", "R2D2", "times" }, tokens);
    }

    [Fact]
    public void Tokenize_LeererSatz_LiefertNichts() => Assert.Empty(BirkenbihlDecodingService.Tokenize(""));

    [Fact]
    public async Task Lookup_WortCaseInsensitiv_AberSprachcodeExakt()
    {
        // Create the vocabulary entry in the store under the codes "en"/"de" …
        var father = await TestApi.AdultAsync(factory);
        await TestApi.CreateStoreVocabAsync(father, "house", "Haus", src: "en", tgt: "de");

        using var scope = factory.Services.CreateScope();
        var decoder = scope.ServiceProvider.GetRequiredService<BirkenbihlDecodingService>();

        // … the capitalization of the WORD does not matter → a hit …
        var hit = await decoder.LookupAsync("en", "de", "House");
        Assert.NotNull(hit[0].Best);

        // … the LANGUAGE CODE, by contrast, is compared exactly: "EN" ≠ "en" → no hit.
        var miss = await decoder.LookupAsync("EN", "de", "house");
        Assert.Null(miss[0].Best);
    }
}
