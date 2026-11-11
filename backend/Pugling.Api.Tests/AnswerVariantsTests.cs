namespace Pugling.Api.Tests;

/// <summary>
/// B-462: a word-list entry packs several meanings into one string ("sobald, sowie"). Typed, each single meaning
/// counts - but only for a vocabulary atom, never for a sentence.
/// </summary>
public class AnswerVariantsTests
{
    private readonly AnswerGrader _grader = new();

    private static ContentItem Entry(string answer, bool separable = true) =>
        new(0, "q", answer, [answer], MeaningsSeparable: separable);

    // The table measured against the Access word list on 2026-11-10, plus the reverse direction ("(to) reply (to)").
    [Theory]
    [InlineData("sobald, sowie", "sobald")]
    [InlineData("sobald, sowie", "sowie")]
    [InlineData("antworten (auf); erwidern, entgegnen", "antworten")]
    [InlineData("antworten (auf); erwidern, entgegnen", "antworten auf")]
    [InlineData("antworten (auf); erwidern, entgegnen", "entgegnen")]
    [InlineData("Aha! / Verstehe.", "Verstehe")]
    [InlineData("Aha! / Verstehe.", "aha")]
    [InlineData("Walisisch; walisisch", "walisisch")]
    [InlineData("westlich, West-", "westlich")]
    [InlineData("das Beste an ...", "das Beste an")]
    [InlineData("the best thing about …", "the best thing about")]
    [InlineData("(to) reply (to)", "reply")]
    [InlineData("(to) reply (to)", "to reply")]
    [InlineData("(to) reply (to)", "reply to")]
    [InlineData("(to) reply (to)", "to reply to")]
    // Review findings: a separator inside brackets belongs to the bracket, a slash there lists choices, a closing
    // abbreviation is no full stop, and a dangling comma before the slot is not part of the phrase.
    [InlineData("eingeschaltet sein, an sein (Radio, Licht usw.); laufen", "an sein")]
    [InlineData("(Schul-, Aus-)Bildung; Erziehung", "Bildung")]
    [InlineData("(to) stay (at/with)", "stay with")]
    [InlineData("übernachten (in/bei)", "übernachten")]
    [InlineData("jdn. anrufen, jdm. etw.", "jdn. anrufen")]
    [InlineData("Und überhaupt, ...", "und überhaupt")]
    [InlineData("Es war schade, dass ...", "es war schade, dass")]
    public void Einzelne_Bedeutung_Zaehlt(string expected, string given) =>
        Assert.True(_grader.Matches(given, Entry(expected)));

    // What stays wrong: half a meaning, the bracket text on its own, a fragment cut inside a bracket, and half of a
    // pattern with a slot ("dass" is no meaning of "Es war schade, dass ...").
    [Theory]
    [InlineData("das Beste an ...", "das Beste")]
    [InlineData("antworten (auf); erwidern, entgegnen", "auf")]
    [InlineData("eingeschaltet sein, an sein (Radio, Licht usw.); laufen", "Licht usw.")]
    [InlineData("Es war schade, dass ...", "dass")]
    [InlineData("Es war schade, dass ...", "es war schade")]
    [InlineData("nicht nur ..., sondern auch ...", "sondern auch")]
    public void Anderes_Bleibt_Falsch(string expected, string given) =>
        Assert.False(_grader.Matches(given, Entry(expected)));

    // A phrase that closes like a sentence is one answer: its comma separates nothing.
    [Theory]
    [InlineData("Ja, bitte.", "ja")]
    [InlineData("Ja, bitte.", "bitte")]
    [InlineData("How are you, Tom?", "Tom")]
    public void Komma_Im_Satz_Trennt_Nicht(string expected, string given) =>
        Assert.False(_grader.Matches(given, Entry(expected)));

    // Statement of intent, not a guard: the whole entry counted before B-462 and still does.
    [Fact]
    public void Ganzer_Eintrag_Zaehlt_Weiter() =>
        Assert.True(_grader.Matches("sobald, sowie", Entry("sobald, sowie")));

    // An empty part between two separators is no answer of its own (the grader refuses an empty input anyway, so
    // this is pinned at the list).
    [Fact]
    public void Leerer_Teil_Wird_Keine_Variante() =>
        Assert.DoesNotContain(AnswerVariants.Of("a, , b; (c)"), string.IsNullOrWhiteSpace);

    // ... while the phrase itself counts without its closing mark.
    [Fact]
    public void Satz_Zaehlt_Ohne_Schlusszeichen() =>
        Assert.True(_grader.Matches("ja, bitte", Entry("Ja, bitte.")));

    // The wall: every other exercise type leaves the flag off, and there a part of the answer is not the answer.
    [Theory]
    [InlineData("sobald, sowie", "sobald")]
    [InlineData("Excuse me, how can I get to the station?", "Excuse me")]
    [InlineData("(to) reply (to)", "reply")]
    public void Ohne_Kennzeichen_Bleibt_Der_Vergleich_Exakt(string expected, string given) =>
        Assert.False(_grader.Matches(given, Entry(expected, separable: false)));

    // The article tolerance (B-421) applies to each meaning, not only to the whole entry.
    [Fact]
    public void Artikel_Darf_Je_Bedeutung_Fehlen()
    {
        var item = Entry("der Müll, der Abfall") with { ArticleOptionalIn = "de" };
        Assert.True(_grader.Matches("Abfall", item));
        Assert.False(_grader.Matches("Abfall", item with { ArticleOptionalIn = null }));
    }

    // A neighbour whose answer is one meaning of this entry would be a second right button on the choice stage.
    [Fact]
    public void Auswahl_Bietet_Eine_Einzelbedeutung_Nicht_Als_Falsche_Option_An()
    {
        ContentItem Item(int i, string answer) => new(i, $"q{i}", answer, [answer], MeaningsSeparable: true);
        var items = new[] { Item(0, "sobald, sowie"), Item(1, "sobald"), Item(2, "riesig"), Item(3, "Dom") };
        var choices = StageMechanics.DistractorPool(items, items[0], 3)!;
        Assert.DoesNotContain("sobald", choices);
        Assert.Contains("riesig", choices);

        // The other way round: for the card "sobald" the neighbour's whole entry contains the right answer.
        var reverse = StageMechanics.DistractorPool(items, items[1], 3)!;
        Assert.DoesNotContain("sobald, sowie", reverse);
        Assert.Contains("riesig", reverse);
    }

    // More bracket forms than the cap: no combinations, only "all kept" and "all dropped".
    [Fact]
    public void Viele_Klammern_Nur_Alle_Oder_Keine()
    {
        var variants = AnswerVariants.Of("(a) b (c) d (e) f (g) h (i)");
        Assert.Contains("a b c d e f g h i", variants);
        Assert.Contains("b d f h", variants);
        Assert.DoesNotContain("a b d f h", variants);
    }
}
