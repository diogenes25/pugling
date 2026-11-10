namespace Pugling.Api.Tests;

/// <summary>
/// B-440: the apostrophe has one spelling for the grader. A tablet types the typographic one, the store may hold
/// either - the full answer must not be graded more strictly than the shortened one.
/// </summary>
public class ApostrophTests
{
    private readonly AnswerGrader _grader = new();

    [Theory]
    [InlineData("l’école", "l'école")]
    [InlineData("l'école", "l’école")]
    [InlineData("l‘école", "l'école")]
    [InlineData("lʼécole", "l'école")]
    [InlineData("l´école", "l'école")]
    [InlineData("l`école", "l'école")]
    public void Apostroph_Varianten_Sind_Dieselbe_Antwort(string given, string expected) =>
        Assert.True(_grader.Matches(given, expected, articleOptionalIn: null));

    [Fact]
    public void Ohne_Apostroph_Bleibt_Falsch() =>
        Assert.False(_grader.Matches("lécole", "l'école", articleOptionalIn: null));

    [Fact]
    public void Elision_Darf_Auch_Bei_Typografisch_Gespeichertem_Wort_Fehlen() =>
        Assert.True(_grader.Matches("école", "l’école", articleOptionalIn: "fr"));
}
