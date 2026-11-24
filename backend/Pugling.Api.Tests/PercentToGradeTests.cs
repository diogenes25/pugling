using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Pugling.Api.Tests;

/// <summary>
/// B-740: the one grading key. A share on a bound earns the better grade, a tenth of a percent below it the worse one;
/// out-of-range shares are clamped; the table comes from the configuration, not from code.
/// </summary>
public class PercentToGradeTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // The values of appsettings.json, repeated here on purpose: a test against the bound options below checks that the
    // file really carries them, these cases check the rule.
    private static readonly decimal[] Standard = [92m, 81m, 67m, 50m, 30m];

    // AK 1: each bound sits in the better grade, a tenth below in the worse one. Red if a `>=` becomes `>`.
    [Theory]
    [InlineData(92, 1)]
    [InlineData(91.9, 2)]
    [InlineData(81, 2)]
    [InlineData(80.9, 3)]
    [InlineData(67, 3)]
    [InlineData(66.9, 4)]
    [InlineData(50, 4)]
    [InlineData(49.9, 5)]
    [InlineData(30, 5)]
    [InlineData(29.9, 6)]
    [InlineData(0, 6)]
    [InlineData(100, 1)]
    public void Grenze_liegt_in_der_besseren_Note(double percent, int grade) =>
        Assert.Equal(grade, GradeKey.PercentToGrade((decimal)percent, Standard));

    // AK 3: nonsense shares are clamped to the edge, never computed into a grade outside 1..6.
    [Theory]
    [InlineData(-5, 6)]
    [InlineData(130, 1)]
    public void Werte_ausserhalb_werden_auf_den_Rand_gezogen(double percent, int grade) =>
        Assert.Equal(grade, GradeKey.PercentToGrade((decimal)percent, Standard));

    // AK 1 on counts: 23 of 25 words are exactly 92 percent and earn the 1, one word less the 2. Red if the bound
    // comparison flipped to `>`.
    [Fact]
    public void Anteil_aus_Zaehlungen_trifft_die_Grenze_genau()
    {
        Assert.Equal(1, GradeKey.ShareToGrade(23, 25, Standard));
        Assert.Equal(2, GradeKey.ShareToGrade(22, 25, Standard));
        Assert.Equal(6, GradeKey.ShareToGrade(0, 200, Standard));
    }

    // The inverse (B-742 builds "words to the next level" on it) agrees with ShareToGrade at every bound: the smallest
    // part for a grade earns it, one fewer does not.
    [Theory]
    [InlineData(25)]
    [InlineData(200)]
    [InlineData(7)]
    public void Umkehr_stimmt_an_jeder_Grenze_mit_der_Note_ueberein(int total)
    {
        for (var grade = 1; grade <= 5; grade++)
        {
            var min = GradeKey.MinPartForGrade(grade, total, Standard);
            Assert.True(GradeKey.ShareToGrade(min, total, Standard) <= grade, $"{min}/{total} should earn {grade}");
            if (min > 0)
                Assert.True(GradeKey.ShareToGrade(min - 1, total, Standard) > grade, $"{min - 1}/{total} should not earn {grade}");
        }
        Assert.Equal(0, GradeKey.MinPartForGrade(6, total, Standard));
    }

    // AK 2: the table is a parameter, not baked in - a different table grades the same share differently.
    [Fact]
    public void Andere_Tabelle_ergibt_andere_Note()
    {
        decimal[] strict = [98m, 95m, 90m, 80m, 70m];
        Assert.Equal(1, GradeKey.PercentToGrade(93m, Standard));
        Assert.Equal(3, GradeKey.PercentToGrade(93m, strict));
    }

    // AK 2: the running host binds the table from appsettings.json (there is no default in code to fall back on).
    [Fact]
    public void Tabelle_kommt_aus_der_Konfiguration()
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<GradingOptions>>().Value;
        Assert.Equal(Standard, options.Thresholds);
        Assert.Empty(new GradingOptions().Thresholds);
    }

    // A broken table fails loudly instead of grading from the wrong bounds.
    [Fact]
    public void Kaputte_Tabelle_wird_abgewiesen()
    {
        Assert.Throws<InvalidOperationException>(() => GradeKey.PercentToGrade(50m, [92m, 81m, 67m, 50m]));
        Assert.Throws<InvalidOperationException>(() => GradeKey.PercentToGrade(50m, [92m, 81m, 81m, 50m, 30m]));
    }

    // AK 4: exactly one place converts a share into a grade. Red if a second method named "...ToGrade" appears
    // anywhere in the API assembly outside GradeKey.
    [Fact]
    public void Es_gibt_genau_eine_Umrechnung()
    {
        var foreign = typeof(GradeKey).Assembly.GetTypes()
            .Where(t => t != typeof(GradeKey))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => m.Name.EndsWith("ToGrade", StringComparison.Ordinal))
            .Select(m => $"{m.DeclaringType!.FullName}.{m.Name}")
            .ToList();
        Assert.True(foreign.Count == 0, "A second share-to-grade conversion exists; use GradeKey instead: " + string.Join(", ", foreign));
    }
}
