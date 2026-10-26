using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-05: the scramble stage. The server hands out the answer's letters in a shuffled order, frozen per session or
/// attempt; the child sends the word it builds as an ordinary <c>GivenAnswer</c>, so grading does not change. An
/// answer that is not one short word is played as letter boxes, card by card.
/// </summary>
public class LetterScrambleTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // Twelve letters (e and t twice each): two shuffles with different seeds coincide with a chance of 1 in 12!/4 -
    // so "a new session shuffles anew" is checked against the word itself, not against luck. Capitalized like a
    // German noun, so the lower-case rule shows.
    private const string Word = "Schmetterlin";
    private static readonly string Lower = Word.ToLowerInvariant();

    [Fact]
    public void Mischen_Behaelt_Die_Buchstaben_Und_Ist_Je_Seed_Fest()
    {
        var a = StageMechanics.ScrambleLetters(Word, 41);
        Assert.Equal(Lower.Order(), string.Concat(a).Order());
        // Lower case: the capital S of the noun would otherwise name the first tile.
        Assert.DoesNotContain("S", a);
        Assert.Equal(a, StageMechanics.ScrambleLetters(Word, 41));
        Assert.NotEqual(a, StageMechanics.ScrambleLetters(Word, 42));
        // Never already the solution - also for a two-letter word, where a shuffle hits it half of the time.
        foreach (var seed in Enumerable.Range(0, 50))
        {
            Assert.NotEqual("ab", string.Concat(StageMechanics.ScrambleLetters("ab", seed)));
            // Case-blind: "Ab" must not come back as "ab" either.
            Assert.NotEqual("ab", string.Concat(StageMechanics.ScrambleLetters("Ab", seed)));
        }
    }

    [Theory]
    [InlineData("ab", true)]
    [InlineData("a", false)]
    [InlineData("abcdefghijkl", true)]
    [InlineData("abcdefghijklm", false)]
    [InlineData("ice cream", false)]
    [InlineData("T-Shirt", false)]
    public void Nur_Ein_Kurzes_Wort_Wird_Gemischt(string answer, bool scramblable) =>
        Assert.Equal(scramblable, StageMechanics.IsScramblable(answer));

    [Fact]
    public void Die_Stufe_Zaehlt_Als_Getippt() => Assert.True(StageMechanics.IsTyped(TestStage.LetterScramble));

    /// <summary>
    /// AC 2/4 in practice: two fetches of the card in one session give the same tiles, a new session other tiles,
    /// and the word built from them is graded correct. Red if the shuffle ignores the session (same tiles in both
    /// sessions), reshuffles per fetch, or if the tiles are not the answer's letters.
    /// </summary>
    [Fact]
    public async Task Uebung_Liefert_Kacheln_Fest_Je_Sitzung_Und_Wertet_Das_Gelegte_Wort()
    {
        var father = await TestApi.AdultAsync(factory);
        var child = await TestApi.ChildAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("butterfly (B-05)", Word));
        // A second position on the same exercise for "a new session": on the first one the card is no longer due
        // after it was answered, so a second session there the same day would carry no card at all.
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.LetterScramble);
        var (otherPlanId, otherPositionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.LetterScramble);
        var basis = TestApi.PracticeBase(planId, positionId);

        async Task<(int sessionId, JsonElement card)> StartAsync(int plan, int position)
        {
            var sid = await TestApi.StartPositionSessionAsync(child, plan, position);
            return (sid, (await child.GetFromJsonAsync<JsonElement>($"{TestApi.PracticeBase(plan, position)}/{sid}/next")).GetProperty("card"));
        }
        static string[] Tiles(JsonElement card) => [.. card.GetProperty("letterTiles").EnumerateArray().Select(t => t.GetString()!)];

        var (first, card) = await StartAsync(planId, positionId);
        var tiles = Tiles(card);
        Assert.Equal(Lower.Order(), string.Concat(tiles).Order());
        Assert.NotEqual(Lower, string.Concat(tiles));
        // The solution stays withheld, and no letter-box facets ride along.
        Assert.Equal(JsonValueKind.Null, card.GetProperty("reveal").ValueKind);
        Assert.Equal(JsonValueKind.Null, card.GetProperty("answerLength").ValueKind);

        var again = (await child.GetFromJsonAsync<JsonElement>($"{basis}/{first}/next")).GetProperty("card");
        Assert.Equal(tiles, Tiles(again));

        var review = await (await TestApi.PositionReviewAsync(child, planId, positionId, first, card.GetProperty("itemIndex").GetInt32(), givenAnswer: Word))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(review, "wasCorrect");

        var (_, other) = await StartAsync(otherPlanId, otherPositionId);
        Assert.NotEqual(tiles, Tiles(other));
    }

    /// <summary>
    /// "Two cards of one round differ": the seed mixes in the item index. Two answers with the same letters in one
    /// session would otherwise get the same permutation, and a child who solved one could apply it to the other.
    /// Red if the item index is dropped from the seed.
    /// </summary>
    [Fact]
    public async Task Zwei_Karten_Einer_Runde_Sind_Verschieden_Gemischt()
    {
        var father = await TestApi.AdultAsync(factory);
        var child = await TestApi.ChildAsync(factory);
        // Same letters on purpose: the same permutation would then give the same tiles.
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("one (B-05)", Word), ("two (B-05)", Word));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.LetterScramble);
        var sid = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        var cards = (await child.GetFromJsonAsync<JsonElement>($"{TestApi.PracticeBase(planId, positionId)}/{sid}/cards"))
            .EnumerateArray().Select(c => string.Concat(c.GetProperty("letterTiles").EnumerateArray().Select(t => t.GetString()))).ToList();
        Assert.Equal(2, cards.Count);
        Assert.NotEqual(cards[0], cards[1]);
    }

    /// <summary>AC 2 in the exam: the test item carries the tiles too, frozen per attempt.</summary>
    [Fact]
    public async Task Pruefung_Liefert_Kacheln_Fest_Je_Versuch()
    {
        var father = await TestApi.AdultAsync(factory);
        var child = await TestApi.ChildAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("moth (B-05)", Word));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.LetterScramble);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var item = (await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{attemptId}/next")).GetProperty("item");
        var again = (await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{attemptId}/next")).GetProperty("item");
        var tiles = item.GetProperty("letterTiles").EnumerateArray().Select(t => t.GetString()).ToList();
        Assert.Equal(Lower.Order(), string.Concat(tiles).Order());
        Assert.Equal(tiles, again.GetProperty("letterTiles").EnumerateArray().Select(t => t.GetString()).ToList());
    }

    /// <summary>
    /// AC 3: an answer of several words falls back to letter boxes for that card only - next to a scrambled card in
    /// the same round. Red if the fallback is missing (no tiles and no boxes) or applies to the whole round.
    /// </summary>
    [Fact]
    public async Task Mehrwortige_Antwort_Kommt_Als_Buchstabenkaestchen()
    {
        var father = await TestApi.AdultAsync(factory);
        var child = await TestApi.ChildAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("ice cream (B-05)", "das Speiseeis"), ("moon (B-05)", "Mond"));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.LetterScramble);
        var sid = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        var cards = (await child.GetFromJsonAsync<JsonElement>($"{TestApi.PracticeBase(planId, positionId)}/{sid}/cards"))
            .EnumerateArray().ToList();

        var boxes = Assert.Single(cards, c => c.GetProperty("prompt").GetString() == "ice cream (B-05)");
        Assert.Equal(JsonValueKind.Null, boxes.GetProperty("letterTiles").ValueKind);
        Assert.Equal("___ _________", boxes.GetProperty("answerPattern").GetString());
        // The length too: the Sohn app switches to letter boxes on `answerLength`, not on the pattern.
        Assert.Equal(13, boxes.GetProperty("answerLength").GetInt32());

        var tiles = Assert.Single(cards, c => c.GetProperty("prompt").GetString() == "moon (B-05)");
        Assert.Equal(4, tiles.GetProperty("letterTiles").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, tiles.GetProperty("answerPattern").ValueKind);
    }
}
