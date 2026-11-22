namespace Pugling.Api.Tests;

/// <summary>
/// Unit tests of the letter-box mask (<see cref="StageMechanics.LetterBoxPattern"/>, B-66): stateless,
/// without DB/HTTP. Secures that only letters/digits become typeable underscores - every other character
/// (space, punctuation, hyphen, an accented/foreign letter) stays fixed and literal.
/// </summary>
public class StageMechanicsTests
{
    [Fact]
    public void MehrteiligeLoesung_MaskiertNurBuchstaben_LeerzeichenBleibenFest()
    {
        Assert.Equal("__ ____ __", StageMechanics.LetterBoxPattern("to grow up"));
    }

    [Fact]
    public void Satzzeichen_UndBindestrich_BleibenFest()
    {
        Assert.Equal("____-__, ___?", StageMechanics.LetterBoxPattern("well-it, huh?"));
    }

    [Fact]
    public void UnicodeBuchstabe_ZaehltAlsBuchstabe_WirdMaskiert()
    {
        // char.IsLetterOrDigit treats accented/foreign letters as letters too (French "œ", German "ß") -
        // they get typed like any other letter; only the space between the two words stays fixed.
        Assert.Equal("___ ___", StageMechanics.LetterBoxPattern("aœb aßb"));
    }

    [Fact]
    public void EinzelnesWort_OhneTrennzeichen_IstVollstaendigMaskiert()
    {
        Assert.Equal("_____", StageMechanics.LetterBoxPattern("hallo"));
    }

    /// <summary>
    /// B-472: the right answer's button must not give the card's direction away. With "both" a word's forward card has
    /// an even index and its backward card an odd one; a shift of <c>Index % 4</c> put the right answer of every forward
    /// card on button 0 or 2 and of every backward card on 1 or 3 - a child guessing by direction halves the choice.
    /// Red as long as the shift follows the index parity.
    /// </summary>
    [Fact]
    public void Die_Richtige_Antwort_Verraet_Die_Richtung_Nicht()
    {
        var cards = Enumerable.Range(0, 8).SelectMany(w => new[]
        {
            new ContentItem(2 * w, $"word {w}", $"Wort {w}", [$"Wort {w}"]),
            new ContentItem(2 * w + 1, $"Wort {w}", $"word {w}", [$"word {w}"], Reversed: true),
        }).ToList();

        int[] Positions(bool reversed) => [.. cards.Where(c => c.Reversed == reversed)
            .Select(c => StageMechanics.DistractorPool(cards, c, 3)!.ToList().IndexOf(c.Answer)).Distinct().Order()];

        Assert.Equal([0, 1, 2, 3], Positions(reversed: false));
        Assert.Equal([0, 1, 2, 3], Positions(reversed: true));
    }

    // B-626 AK 2/3/4: only the core of a word-list entry is typed; brackets and slots stay fixed text.
    [Theory]
    [InlineData("(to) point sth. out (to sb.)", "(to) _____ sth. ___ (to sb.)")]
    [InlineData("(to) be home to sth.", "(to) __ ____ __ sth.")]
    [InlineData("marcher sur qc", "_______ ___ qc")]
    [InlineData("imaginer qn/qc", "________ qn/qc")]
    [InlineData("something", "_________")]
    [InlineData("to grow up", "__ ____ __")]
    public void Eintrag_Maske_Setzt_Beiwerk_Fest(string entry, string expected) =>
        Assert.Equal(expected, StageMechanics.EntryLetterBoxPattern(entry));
}
