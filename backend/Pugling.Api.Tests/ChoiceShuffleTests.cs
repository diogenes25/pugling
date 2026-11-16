using Pugling.Api.Exercises;

namespace Pugling.Api.Tests;

/// <summary>
/// The options of a choice question reach the child shuffled per card and session (<c>PositionPlayService.CardFacets</c>).
/// The case behind it: a reading exercise whose author listed the solution first in every question - in a fixed order
/// the child learned "always the top button", not the simple-past form.
/// </summary>
public class ChoiceShuffleTests
{
    private static readonly ReadingExerciseType Reading = new();

    // Every question with its solution first, as the author wrote them.
    private const string Config = """
        {"text":"Wähle die richtige simple-past-Form des Verbs.","questions":[
          {"prompt":"go","choices":["went","goed","gone","wend","wented"],"answer":"went"},
          {"prompt":"see","choices":["saw","seed","seen","sawed","sew"],"answer":"saw"},
          {"prompt":"take","choices":["took","taked","taken","tooked","teak"],"answer":"took"}]}
        """;

    private static IReadOnlyList<string> Options(int itemIndex, int seed)
    {
        var items = Reading.ItemsOf(Config);
        return PositionPlayService.CardFacets(Config, items, items[itemIndex], Reading, stage: 1, typed: true, seed).Choices!;
    }

    // Red if the options come in the author's order again: then "went" stands first in every session.
    [Fact]
    public void Die_Loesung_Steht_Nicht_Immer_An_Derselben_Stelle()
    {
        var positions = Enumerable.Range(1, 20).Select(seed => Options(0, seed).ToList().IndexOf("went")).ToHashSet();
        Assert.True(positions.Count >= 3, $"Solution only ever at positions {string.Join(", ", positions)}.");
    }

    // Red if the shuffle drops or duplicates an option - the button set must stay the author's.
    [Fact]
    public void Gemischt_Werden_Nur_Die_Optionen_Selbst()
    {
        Assert.Equal(new[] { "went", "goed", "gone", "wend", "wented" }.Order(), Options(0, seed: 42).Order());
    }

    // Red if the shuffle ignores the seed (Random.Shared): a reload within the session would move the buttons.
    [Fact]
    public void Innerhalb_Einer_Sitzung_Bleibt_Die_Reihenfolge()
    {
        Assert.Equal(Options(1, seed: 7), Options(1, seed: 7));
    }

    // Red if only the seed feeds the shuffle: questions with equally many options would share one permutation, and the
    // solution - first in every question - would again sit at the same place on every card of the session.
    [Fact]
    public void Fragen_Einer_Sitzung_Werden_Unterschiedlich_Gemischt()
    {
        var places = Enumerable.Range(1, 10).Count(seed =>
            Options(0, seed).ToList().IndexOf("went") != Options(1, seed).ToList().IndexOf("saw")
            || Options(1, seed).ToList().IndexOf("saw") != Options(2, seed).ToList().IndexOf("took"));
        Assert.True(places >= 5, $"Only {places} of 10 sessions placed the solutions differently.");
    }
}
