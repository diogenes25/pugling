using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Exercises;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Playing a cloze position (B-76). Until now the cloze was only covered while authoring and while
/// resolving its content – nobody had ever played one, and that is exactly where it was broken: every gap
/// of the same text produced a byte-identical card, so the child could not tell which gap was being asked.
/// </summary>
public class ClozePlayTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    /// <summary>A two-gap cloze with a word bank – the smallest shape in which the defect is visible.</summary>
    private static async Task<int> CreateClozeAsync(HttpClient father, object? config = null)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Englisch-Cloze") }));
        var seriesId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            "/api/v1/creator/textbook-series", new { name = TestApi.UniqueName("Englisch-Cloze-Reihe"), subjectId }));
        var seriesUnitId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Unit 1", orderIndex = 1 }));
        return await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/cloze", new
            {
                // Deliberately NOT the seeded title: the seed test below looks its exercise up by name, and
                // a fixture sharing that name would make the lookup depend on the query plan.
                title = TestApi.UniqueName("Lückentext (Fixture)"),
                orderIndex = 1,
                rewardPoints = 15,
                config = config ?? new
                {
                    text = "A: {{1}}, how are you? B: I'm {{2}}, thank you.",
                    gaps = new[]
                    {
                        new { index = 1, answer = "Hello", alternatives = new[] { "Hi" } },
                        new { index = 2, answer = "fine", alternatives = new[] { "good" } },
                    },
                    wordBank = new[] { "Hello", "Hi", "fine", "good" },
                },
            }));
    }

    private static async Task<JsonElement> CardsAsync(HttpClient child, int planId, int positionId)
    {
        var baseUrl = TestApi.PracticeBase(planId, positionId);
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { mode = "Info" }));
        return await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{sessionId}/cards");
    }

    // ---- The defect: two gaps, two indistinguishable cards ---- ----

    [Fact]
    public async Task Lueckentext_KartenWeisenIhreLueckeAus()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await CreateClozeAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)ClozeStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);

        var cards = await CardsAsync(child, planId, positionId);

        Assert.Equal(2, cards.GetArrayLength());
        // Both cards carry the whole text as their prompt - that is by design, it is one text. What must
        // differ is which gap of it is being asked.
        var gapIndices = cards.EnumerateArray().Select(c => c.GetProperty("gapIndex").GetInt32()).ToList();
        Assert.Equal([1, 2], gapIndices.Order());
    }

    // ---- E4: the word-bank stage delivers its word bank ---- ----

    [Fact]
    public async Task Wortbankstufe_LiefertDieGanzeWortbank_FreitextstufeKeine()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await CreateClozeAsync(father);
        var child = await TestApi.ChildAsync(_factory);

        var (wordBankPlan, wordBankPos) =
            TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)ClozeStage.TranslationWordBank);
        var withBank = await CardsAsync(child, wordBankPlan, wordBankPos);
        foreach (var card in withBank.EnumerateArray())
        {
            var choices = card.GetProperty("choices").EnumerateArray().Select(c => c.GetString()).ToList();
            // The whole pool, unshortened (E4) - but sorted, so the authoring order (gap by gap) cannot
            // give the mapping away through position alone.
            Assert.Equal(["fine", "good", "Hello", "Hi"], choices);
            // R1: the stage is typed, so the solution stays behind - otherwise the buttons would be decoration.
            JsonAssert.Null(card, "reveal");
        }

        var (freePlan, freePos) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)ClozeStage.FreeText);
        var freeText = await CardsAsync(child, freePlan, freePos);
        Assert.All(freeText.EnumerateArray(), card => JsonAssert.Null(card, "choices"));
    }

    // ---- E6: the word-bank stage is graded by the server ---- ----

    /// <summary>
    /// The behaviour change behind the visible one: the word-bank stage used to be self-assessment, so a
    /// child clicking "I knew it" advanced without ever matching a word. Now the pick is an answer and the
    /// server judges it. That is what the seeded daily duty hangs on – and what no test watched before.
    /// </summary>
    [Fact]
    public async Task Wortbankstufe_WirdVomServerBewertet_NichtSelbstEingeschaetzt()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await CreateClozeAsync(father);
        var (planId, positionId) =
            TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)ClozeStage.TranslationWordBank);
        var child = await TestApi.ChildAsync(_factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);

        // The right word out of the bank counts, and the alternative counts too (B-65).
        var right = await (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "Hi"))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(right, "wasCorrect");
        Assert.Equal(2, right.GetProperty("box").GetInt32());   // graded => the Leitner box moves

        var wrong = await (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 1, givenAnswer: "Hello"))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.False(wrong, "wasCorrect");                   // "Hello" belongs to gap 1, not gap 2
        Assert.Equal("fine", wrong.GetProperty("expected").GetString());

        // And self-assessment no longer buys anything: without a typed answer the stage grades it wrong.
        var claimed = await (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 1, wasKnown: true))
            .Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.False(claimed, "wasCorrect");
    }

    // ---- E5: the exam pulls along ---- ----

    [Fact]
    public async Task Klausur_WeistDieLueckeGenauSoAus()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await CreateClozeAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(
            _factory, exerciseId, (int)ClozeStage.FreeText, requireTypedTest: true);
        var child = await TestApi.ChildAsync(_factory);

        var testUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(
            await child.PostAsJsonAsync(testUrl, new { stage = (int)ClozeStage.FreeText }), "attemptId");

        // One at a time through the attempt cursor - and each question names its gap.
        var seen = new List<int>();
        for (var i = 0; i < 2; i++)
        {
            var next = await child.GetFromJsonAsync<JsonElement>($"{testUrl}/{attemptId}/next");
            var item = next.GetProperty("item");
            seen.Add(item.GetProperty("gapIndex").GetInt32());
            await child.PostAsJsonAsync($"{testUrl}/{attemptId}/answer",
                new { itemIndex = item.GetProperty("itemIndex").GetInt32(), givenAnswer = "x", wasKnown = (bool?)null });
        }

        Assert.Equal([1, 2], seen.Order());
    }

    // ---- A type without gaps says so, instead of inventing one ---- ----

    [Fact]
    public async Task Vokabelkarte_TraegtKeineLueckennummer()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("a", "1"));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);

        var cards = await CardsAsync(child, planId, positionId);

        Assert.All(cards.EnumerateArray(), card => JsonAssert.Null(card, "gapIndex"));
    }

    /// <summary>
    /// The seeded exercise is the case that motivated the story – the real dialogue with its two gaps and
    /// its five-word bank, not only a fixture written for this test. Played on an own position, because the
    /// seeded plan's ownership is the seed's business and would make this test fail for another reason.
    /// </summary>
    [Fact]
    public async Task GeseedeterLueckentext_SpieltMitLueckeUndWortbank()
    {
        int exerciseId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var seeded = await db.Exercises.AsNoTracking().FirstOrDefaultAsync(e =>
                e.Type == ExerciseTypeKeys.Cloze && e.Title == "Lückentext: A short dialogue");
            Assert.NotNull(seeded);
            exerciseId = seeded.Id;
        }

        var (planId, positionId) =
            TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)ClozeStage.TranslationWordBank);
        var child = await TestApi.ChildAsync(_factory);

        var cards = await CardsAsync(child, planId, positionId);

        Assert.Equal(2, cards.GetArrayLength());
        Assert.Equal([1, 2], cards.EnumerateArray().Select(c => c.GetProperty("gapIndex").GetInt32()).Order());
        Assert.All(cards.EnumerateArray(), c =>
            Assert.Equal(["fine", "good", "Hello", "Hi", "well"],
                c.GetProperty("choices").EnumerateArray().Select(x => x.GetString())));
    }

    // ---- B-487: a bank without a gap's solution ---- ----

    private async Task<List<List<string?>>> BankChoicesAsync(int exerciseId)
    {
        var (planId, positionId) =
            TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)ClozeStage.TranslationWordBank);
        var cards = await CardsAsync(await TestApi.ChildAsync(_factory), planId, positionId);
        return [.. cards.EnumerateArray().Select(c => c.GetProperty("choices").EnumerateArray().Select(x => x.GetString()).ToList())];
    }

    /// <summary>
    /// B-487: a hand-written bank that lacks a gap's solution still offers it, on every card the same pool. Red if
    /// the bank is passed through as written (no "went", no "saw"), or if only the asked gap's solution is added
    /// (the two cards then differ and each names its own solution).
    /// </summary>
    [Fact]
    public async Task Wortbank_ohne_Loesung_bekommt_sie_auf_jeder_Karte()
    {
        var exerciseId = await CreateClozeAsync(await TestApi.AdultAsync(_factory), new
        {
            text = "I {{1}} home and {{2}} a film.",
            gaps = new[] { new { index = 1, answer = "went" }, new { index = 2, answer = "saw" } },
            wordBank = new[] { "go", "gone" },
        });

        var pools = await BankChoicesAsync(exerciseId);

        Assert.Equal(2, pools.Count);
        Assert.All(pools, pool => Assert.Equal(["go", "gone", "saw", "went"], pool));
    }

    /// <summary>
    /// B-487: a tile the grader accepts already solves the gap - in another case, with a closing mark or as an
    /// alternative - and nothing is added. Red if the bank is compared by exact spelling ("Went." would get "went"
    /// next to it) or only against the main solution ("did go" would get "went" next to it).
    /// </summary>
    [Fact]
    public async Task Wortbank_mit_einer_gueltigen_Schreibweise_bleibt_wie_geschrieben()
    {
        var exerciseId = await CreateClozeAsync(await TestApi.AdultAsync(_factory), new
        {
            text = "I {{1}} home and {{2}} a film.",
            gaps = new object[]
            {
                new { index = 1, answer = "went", alternatives = new[] { "did go" } },
                new { index = 2, answer = "saw", alternatives = Array.Empty<string>() },
            },
            wordBank = new[] { "did go", "go", "Saw." },
        });

        var pools = await BankChoicesAsync(exerciseId);

        Assert.All(pools, pool => Assert.Equal(["did go", "go", "Saw."], pool));
    }

    /// <summary>
    /// B-487, review: the added tile is a solution the grader accepts, never a blank one, and two gaps sharing a missing
    /// solution get one tile. Red if the inline answer is added as written (a blank "  " tile instead of "went"), or if
    /// the pool is not rechecked after each addition ("went" twice).
    /// </summary>
    [Fact]
    public async Task Wortbank_ergaenzt_eine_angenommene_Loesung_und_jede_nur_einmal()
    {
        var exerciseId = await CreateClozeAsync(await TestApi.AdultAsync(_factory), new
        {
            text = "I {{1}} home and she {{2}} too.",
            gaps = new[]
            {
                new { index = 1, answer = "  ", alternatives = new[] { "went" } },
                new { index = 2, answer = "went", alternatives = Array.Empty<string>() },
            },
            wordBank = new[] { "go" },
        });

        var pools = await BankChoicesAsync(exerciseId);

        Assert.All(pools, pool => Assert.Equal(["go", "went"], pool));
    }

    /// <summary>
    /// B-487: a store-backed gap takes its solution from the store, which can change after the bank was written. The
    /// pool follows the store. Red if the completion reads the config's inline answer (empty for a store gap) instead
    /// of the resolved solution.
    /// </summary>
    [Fact]
    public async Task Wortbank_folgt_einem_umbenannten_Wort_aus_dem_Speicher()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (vocabId, key) = await TestApi.CreateStoreVocabAsync(father, TestApi.UniqueName("castle"), "die Burg");
        var exerciseId = await CreateClozeAsync(father, new
        {
            text = "The {{1}} is old.",
            gaps = new[] { new { index = 1, answer = "", vocabKey = key } },
            wordBank = new[] { "tower" },
        });
        string renamed;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var vocab = await db.Vocabularies.SingleAsync(v => v.Id == vocabId);
            renamed = vocab.Word = TestApi.UniqueName("fortress");
            await db.SaveChangesAsync();
        }

        var pool = Assert.Single(await BankChoicesAsync(exerciseId));

        Assert.Equal(new[] { renamed, "tower" }.Order(StringComparer.OrdinalIgnoreCase), pool);
    }

    // ---- B-491: the bank's own entries are cleaned before it counts as a bank ----

    /// <summary>
    /// B-491: a bank of blank entries is no bank. The stage then stays free text, like a bank that was left out. Red if
    /// blank entries count as a bank: the completion fills it with the solutions alone ("saw", "went") next to an
    /// unlabelled button.
    /// </summary>
    [Fact]
    public async Task Wortbank_aus_leeren_Eintraegen_ist_keine_Wortbank()
    {
        var exerciseId = await CreateClozeAsync(await TestApi.AdultAsync(_factory), new
        {
            text = "I {{1}} home and {{2}} a film.",
            gaps = new[] { new { index = 1, answer = "went" }, new { index = 2, answer = "saw" } },
            wordBank = new[] { "", "   " },
        });
        var (planId, positionId) =
            TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)ClozeStage.TranslationWordBank);

        var cards = await CardsAsync(await TestApi.ChildAsync(_factory), planId, positionId);

        Assert.Equal(2, cards.GetArrayLength());
        Assert.All(cards.EnumerateArray(), card => JsonAssert.Null(card, "choices"));
    }

    /// <summary>
    /// B-491: a blank entry next to real ones drops out and does not reach the child as an unlabelled tile, and a kept
    /// entry arrives without the spaces around it. Red if the bank's entries are passed through as written (the pool
    /// then starts with ""), or if kept entries are not trimmed (" go " instead of "go").
    /// </summary>
    [Fact]
    public async Task Wortbank_laesst_leere_Eintraege_weg()
    {
        var exerciseId = await CreateClozeAsync(await TestApi.AdultAsync(_factory), new
        {
            text = "I {{1}} home.",
            gaps = new[] { new { index = 1, answer = "went" } },
            wordBank = new[] { " go ", "", "gone" },
        });

        var pool = Assert.Single(await BankChoicesAsync(exerciseId));

        Assert.Equal(["go", "gone", "went"], pool);
    }

    /// <summary>
    /// B-491: entries the grader cannot tell apart are one tile, the first one written. "went" and "Went." would be two
    /// right tiles, "qu’il" and "qu'il" too (the KI-Creator compared its distractors only by case). Red if duplicates are
    /// kept, or if they are compared by exact spelling or case alone ("Went." and "qu’il" stay).
    /// </summary>
    [Fact]
    public async Task Wortbank_legt_ununterscheidbare_Eintraege_zusammen()
    {
        var exerciseId = await CreateClozeAsync(await TestApi.AdultAsync(_factory), new
        {
            text = "I {{1}} home, {{2}} vient.",
            gaps = new[] { new { index = 1, answer = "went" }, new { index = 2, answer = "qu'il" } },
            wordBank = new[] { "went", "Went.", "go", "go", "qu’il", "qu'il" },
        });

        var pools = await BankChoicesAsync(exerciseId);

        Assert.All(pools, pool => Assert.Equal(["go", "qu’il", "went"], pool));
    }
}
