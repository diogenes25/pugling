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

    // B-990: a word-list entry as the store keeps it; the meanings and slot variants come from AnswerVariants.
    private static ContentItem Entry(string entry) =>
        new(0, "q", entry, [entry], ArticleOptionalIn: "en", MeaningsSeparable: true);

    // B-990 AK 5: each of the six abbreviations stands for its slot, with or without the dot and in any case. Each row is
    // red without the rule; "turn sth up/down" (no dot) also, since the dot before a word is kept by Normalize.
    [Theory]
    [InlineData("(to) turn to sb.", "turn to smb")]
    [InlineData("(to) turn to sb.", "turn to smb.")]
    [InlineData("(to) turn to sb.", "turn to SMBD")]
    [InlineData("(to) turn to sb.", "turn to sbd.")]
    [InlineData("(to) turn sth. up/down", "turn smt. up/down")]
    [InlineData("(to) turn sth. up/down", "turn smth up/down")]
    [InlineData("(to) turn sth. up/down", "turn s.th. up/down")]
    [InlineData("(to) turn sth. up/down", "turn sth up/down")]
    [InlineData("(to) dig (for sth.), dug, dug", "(to) dig (for smth.), dug, dug")]
    // A person slot mid-phrase, where Normalize keeps the dot: red without the bare "sb" of the rule, and red without
    // "smb" (review of B-990: every row above ends in the slot, whose closing dot Normalize strips anyway).
    [InlineData("(to) give sb. a hand", "give sb a hand")]
    [InlineData("(to) give sb. a hand", "give smb a hand")]
    public void Kuerzel_Des_Kindes_Zaehlt_Fuer_Seinen_Platzhalter(string entry, string given) =>
        Assert.True(_grader.Matches(given, Entry(entry)));

    // B-990 AK 2, 3, 5: the kind has to fit (a thing for a person and the other way round), an abbreviation does not rescue
    // the word in front of it, and "so" / "s.o." are no abbreviation. All green in the starting state on purpose: rows 1-2
    // turn red with a rule that maps every abbreviation to one slot, rows 4-5 with one that takes "so" in. Row 3 is a
    // statement of intent (AK 3) - no plausible rule turns it red, it documents the boundary from the real answer.
    [Theory]
    [InlineData("(to) turn to sb.", "turn to smth")]
    [InlineData("(to) turn sth. up/down", "turn smb. up/down")]
    [InlineData("(to) hold onto sth.", "(to) hold on smth.")]
    [InlineData("(to) turn to sb.", "turn to so")]
    [InlineData("(to) turn to sb.", "turn to s.o.")]
    public void Falsches_Oder_Fremdes_Kuerzel_Bleibt_Falsch(string entry, string given) =>
        Assert.False(_grader.Matches(given, Entry(entry)));

    // B-990 AK 6: the French and German slots stay as they are - a guessed school abbreviation does not count. Statement
    // of intent: green in the starting state, red only if the rule grows beyond English.
    [Theory]
    [InlineData("marcher sur qc", "marcher sur qqch")]
    [InlineData("jdm. helfen", "jmdm. helfen")]
    public void Andere_Sprachen_Bleiben_Wie_Heute(string entry, string given) =>
        Assert.False(_grader.Matches(given, Entry(entry)));

    // B-990: an abbreviation counts only as a whole word. "Gasmthaus" would become "Gasthaus" if "smth" inside a word were
    // mapped - red without the regex's word guards. German words with "sth" (Gasthaus, Dienstherr) make the guard real.
    [Fact]
    public void Kuerzel_Im_Wortinneren_Zaehlt_Nicht() =>
        Assert.False(_grader.Matches("Gasmthaus", Entry("Gasthaus")));

    // B-990: "falsch geschrieben" measures against the same form Matches judges - a typo next to an abbreviated slot is a
    // spelling slip like the same typo next to the printed slot. Row 1 is red when the child's side skips the mapping
    // ("trun smt." is then three edits off "turn sth"), row 2 when the solutions' side does ("gvie sb a hnad" is then three
    // edits off "give sb. a hand", the dot counting as one).
    [Theory]
    [InlineData("(to) turn sth. up/down", "trun smt. up/down")]
    [InlineData("(to) give sb. a hand", "gvie smb a hnad")]
    public void Tippfehler_Neben_Kuerzel_Ist_Falsch_Geschrieben(string entry, string given)
    {
        var item = Entry(entry);
        Assert.True(_grader.IsMisspelled(given, item, [item]));
    }
}
