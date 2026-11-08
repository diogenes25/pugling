namespace Pugling.Api.Tests;

/// <summary>
/// B-421: a leading article of the answer's language may be left out when typing - "Stadt" for "die Stadt" - so points
/// and the penalty measure the word, not typing discipline. Only leaving it out is forgiven.
/// </summary>
public class AnswerGraderTests
{
    private readonly AnswerGrader _grader = new();

    // AK 1 and 2: the article may be left out, in each of the three languages; case stays irrelevant.
    [Theory]
    [InlineData("die Stadt", "Stadt", "de")]
    [InlineData("die Stadt", "Die Stadt", "de")]
    [InlineData("das Geschäft", "geschäft", "de")]
    [InlineData("eine Katze", "Katze", "de")]
    [InlineData("the house", "house", "en")]
    [InlineData("an apple", "apple", "en")]
    [InlineData("la ville", "ville", "fr")]
    [InlineData("les enfants", "enfants", "fr")]
    [InlineData("l'école", "école", "fr")]
    [InlineData("l’école", "école", "fr")]
    [InlineData("l' école", "école", "fr")]
    [InlineData("die Stadt", "Stadt", "de-DE")]
    public void Weggelassener_Artikel_Zaehlt(string expected, string given, string lang) =>
        Assert.True(_grader.Matches(given, expected, lang));

    // AK 1: a different article is not a left-out one; AK 3: an added article is invented, not forgiven; and a first word
    // that is no article (an adjective) is never dropped.
    [Theory]
    [InlineData("die Stadt", "der Stadt", "de")]
    [InlineData("Stadt", "die Stadt", "de")]
    [InlineData("the house", "a house", "en")]
    [InlineData("la ville", "le ville", "fr")]
    [InlineData("großes Haus", "Haus", "de")]
    public void Anderer_Oder_Hinzugefuegter_Artikel_Bleibt_Falsch(string expected, string given, string lang) =>
        Assert.False(_grader.Matches(given, expected, lang));

    // AK 4: an answer that is only an article has nothing to leave the article off. Statement of intent, not a guard:
    // an empty answer is refused before any article rule and the exact match holds anyway, so no change to the rule turns
    // this red - it documents the boundary.
    [Theory]
    [InlineData("die", "", "de")]
    [InlineData("die", "die", "de")]
    [InlineData("l'", "", "fr")]
    public void Nur_Artikel_Gilt_Nur_Exakt(string expected, string given, string lang) =>
        Assert.Equal(given.Length > 0, _grader.Matches(given, expected, lang));

    // AK 5 at the grader: without a language (the exercise trains the article, or another type) it stays exact.
    [Fact]
    public void Ohne_Sprache_Bleibt_Der_Vergleich_Exakt() =>
        Assert.False(_grader.Matches("Stadt", "die Stadt", null));

    // The article table is per language: a German article is no article in English, and an unknown language forgives
    // nothing (Latin has no articles).
    [Theory]
    [InlineData("die Stadt", "Stadt", "en")]
    [InlineData("the house", "house", "de")]
    [InlineData("die Stadt", "Stadt", "la")]
    public void Artikel_Gilt_Nur_In_Seiner_Sprache(string expected, string given, string lang) =>
        Assert.False(_grader.Matches(given, expected, lang));

    // Review finding: with the tolerance, a neighbour's bare "Stadt" is a correct answer to "die Stadt" - it must not stand
    // among the multiple-choice options as a distractor (two right buttons).
    [Fact]
    public void Auswahl_Bietet_Das_Wort_Ohne_Artikel_Nicht_Als_Falsche_Option_An()
    {
        ContentItem Item(int i, string answer) => new(i, $"q{i}", answer, [answer], ArticleOptionalIn: "de");
        var items = new[] { Item(0, "die Stadt"), Item(1, "Stadt"), Item(2, "der Hund"), Item(3, "das Haus") };
        var choices = StageMechanics.DistractorPool(items, items[0], 3)!;
        Assert.Contains("die Stadt", choices);
        Assert.DoesNotContain("Stadt", choices);
        Assert.Contains("der Hund", choices);
    }

    // The content atom carries the tolerance: every accepted alternative gets it, not just the main solution.
    [Fact]
    public void Atom_Traegt_Die_Toleranz_Fuer_Jede_Alternative()
    {
        var item = new ContentItem(0, "la ville", "die Stadt", ["die Stadt", "die Großstadt"], ArticleOptionalIn: "de");
        Assert.True(_grader.Matches("Großstadt", item));
        Assert.False(_grader.Matches("Großstadt", item with { ArticleOptionalIn = null }));
    }
}
