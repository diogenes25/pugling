namespace Pugling.Api.Tests;

/// <summary>
/// B-624 decision 2: only diacritics count milder - an answer that is right once the marks on and under the letters are
/// folded away. A wrong, missing or swapped letter stays a full miss. Pure unit tests of
/// <see cref="AnswerGrader.IsAccentSlip"/>; the review path is pinned in <c>AkzentMildePfadTests</c>.
/// </summary>
public class AkzentMildeTests
{
    private readonly AnswerGrader _grader = new();

    private static ContentItem Item(string answer, string? articleOptionalIn = null) =>
        new(0, "…", answer, [answer], ArticleOptionalIn: articleOptionalIn);

    // AK 1 and AK 6: each true case is red when the folding is gone (IsAccentSlip always false) or keeps the marks.
    [Theory]
    [InlineData("ecole", "école")]
    [InlineData("eleve", "élève")]
    [InlineData("francais", "français")]
    [InlineData("foret", "forêt")]
    [InlineData("Mädchen", "Madchen")]
    [InlineData("Madchen", "Mädchen")]
    [InlineData("manana", "mañana")]
    [InlineData("élève", "elève")]
    public void Nur_Akzent_Falsch_Ist_Ein_Akzentfehler(string given, string solution) =>
        Assert.True(_grader.IsAccentSlip(given, Item(solution)));

    // AK 2 and the letters that do not decompose (risk 4): red when the folding also drops or maps letters.
    [Theory]
    [InlineData("ecolle", "école")]
    [InlineData("ecol", "école")]
    [InlineData("Strase", "Straße")]
    [InlineData("Strasse", "Straße")]
    [InlineData("coeur", "cœur")]
    [InlineData("maison", "école")]
    public void Ein_Buchstabe_Falsch_Ist_Kein_Akzentfehler(string given, string solution) =>
        Assert.False(_grader.IsAccentSlip(given, Item(solution)));

    // A right answer is no slip: red when IsAccentSlip stops checking Matches first (the review would then count a
    // hit as half).
    [Fact]
    public void Richtige_Antwort_Ist_Kein_Akzentfehler() =>
        Assert.False(_grader.IsAccentSlip("école", Item("école")));

    // The full Matches rule applies before folding: the article may be left out, as for a right answer. Red when the
    // slip is measured against the bare solution only.
    [Fact]
    public void Artikel_Toleranz_Gilt_Auch_Fuer_Den_Akzentfehler()
    {
        Assert.True(_grader.IsAccentSlip("ecole", Item("l'école", "fr")));
        Assert.True(_grader.IsAccentSlip("la foret", Item("la forêt", "fr")));
    }

    // Empty is never anything.
    [Fact]
    public void Leere_Antwort_Ist_Kein_Akzentfehler() => Assert.False(_grader.IsAccentSlip("", Item("é")));
}
