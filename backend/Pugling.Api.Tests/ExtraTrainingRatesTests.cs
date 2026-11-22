using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Pugling.Api.Tests;

/// <summary>
/// B-785: the warm-up hint of a self-assessed bonus run (<c>bonus.hintSelf</c> in the Sohn app) promises "typing pays
/// more". The text is fixed, the rates are configuration - these tests hold the premise the text rests on, so a changed
/// rate turns red here instead of turning the hint into a lie.
/// </summary>
public class ExtraTrainingRatesTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>The rates the running app binds from <c>appsettings.json</c>. Red if the typed rate drops to the others.</summary>
    [Fact]
    public void Schreiben_Zahlt_Mehr_Als_Karteikarte_Und_Auswahl_In_Der_Konfiguration()
    {
        var options = factory.Services.GetRequiredService<IOptions<ExtraTrainingOptions>>().Value;
        Assert.True(options.CoinsPerTypedHit > options.CoinsPerSelfAssessHit,
            $"typed {options.CoinsPerTypedHit} must pay more than self-assess {options.CoinsPerSelfAssessHit}");
        Assert.True(options.CoinsPerTypedHit > options.CoinsPerChoiceHit,
            $"typed {options.CoinsPerTypedHit} must pay more than choice {options.CoinsPerChoiceHit}");
    }

    /// <summary>The defaults that apply without a configuration section. Red if a default breaks the same order.</summary>
    [Fact]
    public void Schreiben_Zahlt_Mehr_In_Den_Vorgaben()
    {
        var defaults = new ExtraTrainingOptions();
        Assert.True(defaults.CoinsPerTypedHit > defaults.CoinsPerSelfAssessHit);
        Assert.True(defaults.CoinsPerTypedHit > defaults.CoinsPerChoiceHit);
    }
}
