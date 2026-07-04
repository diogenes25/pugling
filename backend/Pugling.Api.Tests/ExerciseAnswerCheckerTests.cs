using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Tests;

/// <summary>Unit-Tests der Antwort-Bewertung (zustandslos, ohne DB/HTTP).</summary>
public class ExerciseAnswerCheckerTests
{
    private readonly ExerciseAnswerChecker _checker = new();

    // ---- Arithmetik (feste Aufgaben) ----

    [Fact]
    public void CheckArithmetic_AlleRichtig_100Prozent()
    {
        var config = new ArithmeticConfig { Problems = { new("7 × 6", 42m), new("63 ÷ 9", 7m) } };

        var result = _checker.CheckArithmetic(config, [new(0, "42"), new(1, "7")]);

        Assert.Equal(2, result.Total);
        Assert.Equal(2, result.Correct);
        Assert.Equal(100, result.ScorePercent);
    }

    [Fact]
    public void CheckArithmetic_FehlendeUndFalscheAntwort_ZaehltAlsFalsch()
    {
        var config = new ArithmeticConfig { Problems = { new("7 × 6", 42m), new("63 ÷ 9", 7m) } };

        // Index 1 fehlt komplett, Index 0 ist falsch.
        var result = _checker.CheckArithmetic(config, [new(0, "41")]);

        Assert.Equal(0, result.Correct);
        Assert.Equal(0, result.ScorePercent);
    }

    [Theory]
    [InlineData("3.33", true)]   // exakt innerhalb Toleranz 0.01
    [InlineData("3.34", true)]   // Differenz 0.01 == Toleranz
    [InlineData("3.35", false)]  // Differenz 0.02 > Toleranz
    public void CheckArithmetic_AchtetAufToleranz(string given, bool expectedCorrect)
    {
        var config = new ArithmeticConfig { Problems = { new("10 ÷ 3", 3.33m, 0.01m) } };

        var result = _checker.CheckArithmetic(config, [new(0, given)]);

        Assert.Equal(expectedCorrect, result.Items[0].Correct);
    }

    [Fact]
    public void CheckArithmetic_SpaeterGleicherIndexGewinnt()
    {
        var config = new ArithmeticConfig { Problems = { new("2 + 2", 4m) } };

        var result = _checker.CheckArithmetic(config, [new(0, "1"), new(0, "4")]);

        Assert.Equal(1, result.Correct);
    }

    // ---- Generierte Aufgaben (Seed-basiert) ----

    [Fact]
    public void CheckGenerated_GanzzahlStrikt_Dezimalzahl_MitToleranz()
    {
        var problems = new List<GeneratedProblem> { new("2 + 2", 4m), new("10 ÷ 3", 3.33m) };

        Assert.Equal(100, _checker.CheckGenerated(problems, [new(0, "4"), new(1, "3.33")]).ScorePercent);
        // Ganzzahl-Ergebnis wird strikt geprüft (Toleranz 0):
        Assert.False(_checker.CheckGenerated([new("2 + 2", 4m)], [new(0, "5")]).Items[0].Correct);
        // Dezimalergebnis toleriert kleine Rundung (<= 0.005):
        Assert.True(_checker.CheckGenerated([new("10 ÷ 3", 3.33m)], [new(0, "3.332")]).Items[0].Correct);
    }

    // ---- Zuordnung ----

    [Fact]
    public void CheckMatching_NormalisiertGrossKleinUndLeerzeichen()
    {
        var config = new MatchingConfig { Pairs = { new("Bayern", "München"), new("Hessen", "Wiesbaden") } };

        var result = _checker.CheckMatching(config, [new(0, "münchen"), new(1, "  WIESBADEN ")]);

        Assert.Equal(100, result.ScorePercent);
    }

    [Fact]
    public void CheckMatching_TeilweiseRichtig_50Prozent()
    {
        var config = new MatchingConfig { Pairs = { new("Bayern", "München"), new("Hessen", "Wiesbaden") } };

        var result = _checker.CheckMatching(config, [new(0, "München"), new(1, "Falsch")]);

        Assert.Equal(50, result.ScorePercent);
    }

    // ---- Liste ----

    [Fact]
    public void CheckList_Ungeordnet_ReihenfolgeEgal()
    {
        var config = new ListConfig { Items = { new("Bayern"), new("Hessen"), new("Berlin") } };

        var result = _checker.CheckList(config, ["berlin", "bayern", "hessen"]);

        Assert.Equal(100, result.ScorePercent);
    }

    [Fact]
    public void CheckList_Ungeordnet_Alternative_Zaehlt()
    {
        var config = new ListConfig { Items = { new("Nordrhein-Westfalen", new() { "NRW" }) } };

        Assert.True(_checker.CheckList(config, ["nrw"]).Items[0].Correct);
    }

    [Fact]
    public void CheckList_Ungeordnet_DoppelteNennungZaehltNichtDoppelt()
    {
        var config = new ListConfig { Items = { new("Bayern"), new("Hessen") } };

        // Zweimal "Bayern" darf nicht auch "Hessen" erfüllen.
        var result = _checker.CheckList(config, ["bayern", "bayern"]);

        Assert.Equal(1, result.Correct);
        Assert.Equal(50, result.ScorePercent);
    }

    [Fact]
    public void CheckList_Geordnet_AchtetAufPosition()
    {
        var config = new ListConfig { Ordered = true, Items = { new("A"), new("B") } };

        Assert.Equal(100, _checker.CheckList(config, ["A", "B"]).ScorePercent);
        Assert.Equal(0, _checker.CheckList(config, ["B", "A"]).ScorePercent);
    }

    // ---- Aggregation ----

    [Fact]
    public void LeereAufgabenliste_LiefertNull_KeineDivisionDurchNull()
    {
        var result = _checker.CheckArithmetic(new ArithmeticConfig(), []);

        Assert.Equal(0, result.Total);
        Assert.Equal(0, result.ScorePercent);
    }
}
