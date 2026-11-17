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
    // The front side: the learnt word is the one laid out on the scramble stage (B-597).
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
    [InlineData("T-Shirt", false)]
    // B-634 AK 1/3: several words count when each has 2-12 letters; the 12 holds per word, not for the whole answer.
    [InlineData("ice cream", true)]
    [InlineData("as soon as", true)]
    [InlineData("abcdefghijkl abcdefghijkl", true)]
    [InlineData("Western abcdefghijklm", false)]
    [InlineData("a family", false)]
    [InlineData("ice  cream", false)]
    [InlineData("doesn't she", false)]
    public void Nur_Kurze_Woerter_Werden_Gemischt(string answer, bool scramblable) =>
        Assert.Equal(scramblable, StageMechanics.IsScramblable(answer));

    /// <summary>
    /// B-634 AK 1/2: several words are shuffled word by word - word one's tiles first, each word's own letters, none
    /// already spelling its word - and one word is shuffled exactly as before (the first word keeps the seed). Red if
    /// the words are shuffled as one pool or the single word's arrangement changes.
    /// </summary>
    [Fact]
    public void Mehrere_Woerter_Werden_Je_Wort_Gemischt()
    {
        foreach (var seed in Enumerable.Range(0, 30))
        {
            var tiles = StageMechanics.ScrambleLetters("as soon as", seed);
            Assert.Equal(8, tiles.Count);
            Assert.Equal("as".Order(), string.Concat(tiles.Take(2)).Order());
            Assert.Equal("soon".Order(), string.Concat(tiles.Skip(2).Take(4)).Order());
            Assert.Equal("as".Order(), string.Concat(tiles.Skip(6)).Order());
            Assert.NotEqual("soon", string.Concat(tiles.Skip(2).Take(4)));
            Assert.Equal(StageMechanics.ScrambleLetters("as", seed), tiles.Take(2));
        }
        Assert.Equal("__ ____ __", StageMechanics.ScrambleWordPattern("as soon as"));
        Assert.Null(StageMechanics.ScrambleWordPattern("Schmetterlin"));
    }

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
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, (Word, "butterfly (B-05)"));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.LetterScramble);
        var basis = TestApi.PracticeBase(planId, positionId);

        async Task<(int sessionId, JsonElement card)> StartAsync()
        {
            var sid = await TestApi.StartPositionSessionAsync(child, planId, positionId);
            return (sid, (await child.GetFromJsonAsync<JsonElement>($"{basis}/{sid}/next")).GetProperty("card"));
        }
        static string[] Tiles(JsonElement card) => [.. card.GetProperty("letterTiles").EnumerateArray().Select(t => t.GetString()!)];

        var (first, card) = await StartAsync();
        var tiles = Tiles(card);
        Assert.Equal(Lower.Order(), string.Concat(tiles).Order());
        Assert.NotEqual(Lower, string.Concat(tiles));
        // The solution stays withheld, and no letter-box facets ride along.
        Assert.Equal(JsonValueKind.Null, card.GetProperty("reveal").ValueKind);
        Assert.Equal(JsonValueKind.Null, card.GetProperty("answerLength").ValueKind);

        var again = (await child.GetFromJsonAsync<JsonElement>($"{basis}/{first}/next")).GetProperty("card");
        Assert.Equal(tiles, Tiles(again));

        // The second session on the SAME position, started before the review while the card is still due: across
        // two positions a seed taken from the position id would pass as well (B-288). Practice sessions do not
        // resume, so the second start is a new session with its own seed.
        var (second, other) = await StartAsync();
        Assert.NotEqual(first, second);
        Assert.NotEqual(tiles, Tiles(other));

        var review = await (await TestApi.PositionReviewAsync(child, planId, positionId, first, card.GetProperty("itemIndex").GetInt32(), givenAnswer: Word))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(review, "wasCorrect");
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
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, (Word, "one (B-05)"), (Word, "two (B-05)"));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.LetterScramble);
        var sid = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        var cards = (await child.GetFromJsonAsync<JsonElement>($"{TestApi.PracticeBase(planId, positionId)}/{sid}/cards"))
            .EnumerateArray().Select(c => string.Concat(c.GetProperty("letterTiles").EnumerateArray().Select(t => t.GetString()))).ToList();
        Assert.Equal(2, cards.Count);
        Assert.NotEqual(cards[0], cards[1]);
    }

    /// <summary>
    /// AC 2 in the exam: the test item carries the tiles too, frozen per attempt, and a new attempt shuffles anew.
    /// Red if the seed ignores the attempt id (same tiles on both attempts). The solution and the letter-box
    /// facets stay withheld, as on the practice card.
    /// </summary>
    [Fact]
    public async Task Pruefung_Liefert_Kacheln_Fest_Je_Versuch_Und_Neu_Je_Versuch()
    {
        var father = await TestApi.AdultAsync(factory);
        var child = await TestApi.ChildAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, (Word, "moth (B-05)"));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.LetterScramble);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        async Task<(int attemptId, JsonElement item)> StartAsync()
        {
            var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
            return (attemptId, (await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{attemptId}/next")).GetProperty("item"));
        }
        static List<string?> Tiles(JsonElement item) => [.. item.GetProperty("letterTiles").EnumerateArray().Select(t => t.GetString())];

        var (attemptId, item) = await StartAsync();
        var again = (await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{attemptId}/next")).GetProperty("item");
        var tiles = Tiles(item);
        Assert.Equal(Lower.Order(), string.Concat(tiles).Order());
        Assert.Equal(tiles, Tiles(again));
        Assert.Equal(JsonValueKind.Null, item.GetProperty("reveal").ValueKind);
        Assert.Equal(JsonValueKind.Null, item.GetProperty("answerLength").ValueKind);

        // The second attempt on the SAME position, on purpose: across two positions a seed taken from the position
        // id would pass as well (B-288). A start while the first attempt is open hands back that open attempt, so
        // the first one is answered and submitted - only then does the second carry a new id.
        (await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/answer",
            new { itemIndex = item.GetProperty("itemIndex").GetInt32(), givenAnswer = Word, wasKnown = (bool?)null })).EnsureSuccessStatusCode();
        (await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new { })).EnsureSuccessStatusCode();

        var (secondId, second) = await StartAsync();
        Assert.NotEqual(attemptId, secondId);
        Assert.NotEqual(tiles, Tiles(second));
    }

    /// <summary>
    /// AC 3: an answer that cannot be scrambled (a hyphen is a tile the solution fixes) falls back to letter boxes for
    /// that card only - next to a scrambled card in the same round. Red if the fallback is missing (no tiles and no
    /// boxes) or applies to the whole round. Several plain words scramble since B-634 (see the cloze tests).
    /// </summary>
    [Fact]
    public async Task Antwort_Mit_Bindestrich_Kommt_Als_Buchstabenkaestchen()
    {
        var father = await TestApi.AdultAsync(factory);
        var child = await TestApi.ChildAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("das T-Shirt", "shirt (B-05)"), ("Mond", "moon (B-05)"));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.LetterScramble);
        var sid = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        var cards = (await child.GetFromJsonAsync<JsonElement>($"{TestApi.PracticeBase(planId, positionId)}/{sid}/cards"))
            .EnumerateArray().ToList();

        var boxes = Assert.Single(cards, c => c.GetProperty("prompt").GetString() == "shirt (B-05)");
        Assert.Equal(JsonValueKind.Null, boxes.GetProperty("letterTiles").ValueKind);
        Assert.Equal("___ _-_____", boxes.GetProperty("answerPattern").GetString());
        // The length too: the Sohn app switches to letter boxes on `answerLength`, not on the pattern.
        Assert.Equal(11, boxes.GetProperty("answerLength").GetInt32());

        var tiles = Assert.Single(cards, c => c.GetProperty("prompt").GetString() == "moon (B-05)");
        Assert.Equal(4, tiles.GetProperty("letterTiles").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, tiles.GetProperty("answerPattern").ValueKind);
    }
}
