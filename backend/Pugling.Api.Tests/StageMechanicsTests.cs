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
}
