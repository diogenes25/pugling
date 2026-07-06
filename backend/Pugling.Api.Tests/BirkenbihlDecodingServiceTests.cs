using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Services;

namespace Pugling.Api.Tests;

/// <summary>
/// Tests der Birkenbihl-Dekodierung: der Tokenizer (<see cref="BirkenbihlDecodingService.Tokenize"/>)
/// arbeitet zustandslos; der Speicher-Nachschlag (<see cref="BirkenbihlDecodingService.LookupAsync"/>)
/// vergleicht die Wort-Oberfläche case-insensitiv, die Sprachcodes aber bewusst EXAKT – eine dokumentierte
/// Zusage (fail-closed), die bislang ungeprüft war.
/// </summary>
public class BirkenbihlDecodingServiceTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public void Tokenize_HaeltWortInnerenApostroph_UndWirftSatzzeichenWeg()
    {
        // Apostroph mitten im Wort bleibt ("Don't" = ein Token), Satzzeichen trennen, Ziffern zählen nur
        // WORT-intern ("R2D2"); ein allein stehendes Zahl-Token beginnt nicht mit Buchstabe und fällt weg.
        var tokens = BirkenbihlDecodingService.Tokenize("Don't run, R2D2! 42 times.");
        Assert.Equal(new[] { "Don't", "run", "R2D2", "times" }, tokens);
    }

    [Fact]
    public void Tokenize_LeererSatz_LiefertNichts() => Assert.Empty(BirkenbihlDecodingService.Tokenize(""));

    [Fact]
    public async Task Lookup_WortCaseInsensitiv_AberSprachcodeExakt()
    {
        // Vokabel im Speicher unter den Codes "en"/"de" anlegen …
        var father = await TestApi.FatherAsync(factory);
        await TestApi.CreateStoreVocabAsync(father, "house", "Haus", src: "en", tgt: "de");

        using var scope = factory.Services.CreateScope();
        var decoder = scope.ServiceProvider.GetRequiredService<BirkenbihlDecodingService>();

        // … Groß-/Kleinschreibung des WORTES ist egal → Treffer …
        var hit = await decoder.LookupAsync("en", "de", "House");
        Assert.NotNull(hit[0].Best);

        // … der SPRACHCODE wird dagegen exakt verglichen: "EN" ≠ "en" → kein Treffer.
        var miss = await decoder.LookupAsync("EN", "de", "house");
        Assert.Null(miss[0].Best);
    }
}
