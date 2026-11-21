using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// End-to-end of the position-based final test (stage 3): content from the exercise config,
/// type-neutral scoring against the item solution, passing based on <see cref="PlanPosition.GoalThreshold"/>.
/// </summary>
public class PositionTestFlowTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    [Fact]
    public async Task Test_AlleRichtig_Bestanden_100Prozent()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var submit = await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[]
            {
                new { itemIndex = 0, givenAnswer = "hello" },
                new { itemIndex = 1, givenAnswer = "goodbye" },
            },
        });
        var res = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, res.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, res.GetProperty("correctItems").GetInt32());
        Assert.Equal(100, res.GetProperty("scorePercent").GetInt32());
        JsonAssert.True(res, "passed");
    }

    [Fact]
    public async Task Test_HalbRichtig_UnterStandardgrenze_NichtBestanden()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var submit = await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "hello" }, new { itemIndex = 1, givenAnswer = "falsch" } },
        });
        var res = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(50, res.GetProperty("scorePercent").GetInt32());
        Assert.Equal(80, res.GetProperty("passPercent").GetInt32());
        JsonAssert.False(res, "passed");
    }

    [Fact]
    public async Task Test_EigeneZielSchwelle_WirdRespektiert()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        // A position with a milder pass threshold (40 %): 50 % then suffice.
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText, goalThreshold: 40);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var submit = await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "hello" }, new { itemIndex = 1, givenAnswer = "falsch" } },
        });
        var res = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(50, res.GetProperty("scorePercent").GetInt32());
        Assert.Equal(40, res.GetProperty("passPercent").GetInt32());
        JsonAssert.True(res, "passed");
    }

    /// <summary>
    /// B-639 AK 1 and 4 through the child's test route: a task with the solution 2.5 takes "2,5", one with 2.33 and
    /// tolerance 0.05 takes "2,3", a third with 25 rejects "2,5" - 2 of 3. Red while playing compares text (0 of 3: the
    /// comma never matches and the tolerance is ignored) and red if "2,5" reads as 25 again (the third would count).
    /// </summary>
    [Fact]
    public async Task Test_Rechenaufgabe_Mit_Komma_Und_Toleranz()
    {
        var father = await TestApi.AdultAsync(_factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("Komma-Mathe") }));
        var (seriesId, seriesUnitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId, "Kapitel IV");
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/arithmetic", new
            {
                title = "Dezimalzahlen",
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    problems = new[]
                    {
                        new { prompt = "5 : 2", answer = 2.5m, tolerance = 0m },
                        new { prompt = "7 : 3 (gerundet)", answer = 2.33m, tolerance = 0.05m },
                        new { prompt = "5 · 5", answer = 25m, tolerance = 0m },
                    },
                },
            }));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText, useLeitner: false);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var res = await (await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[]
            {
                new { itemIndex = 0, givenAnswer = "2,5" },
                new { itemIndex = 1, givenAnswer = "2,3" },
                new { itemIndex = 2, givenAnswer = "2,5" },
            },
        })).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(3, res.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, res.GetProperty("correctItems").GetInt32());
    }

    /*
     * Die Einheit der Schwelle ist auch bei einem Katalog-Check-Verfahren PROZENT – nicht die Anzahl
     * richtiger Aufgaben. Das stand einmal anders in der Doku (und der Seed setzte danach Trefferzahlen),
     * war aber nie implementiert: ein TestAttempt entsteht nur hier, und hier wird verglichen.
     *
     * Der Test nagelt beide Richtungen fest, weil die falsche Lesart lautlos schadet: als Prozentwert
     * gelesen macht eine „3" die Pflicht wirkungslos, statt sie zu verschärfen.
     */
    [Theory]
    [InlineData(3, true)]    // if the number were a hit count, 2 of 4 would be too few - as 3 % it suffices
    [InlineData(90, false)]  // 50 % miss a strict percent threshold
    public async Task Test_KatalogCheck_SchwelleIstProzent(int goalThreshold, bool expectPassed)
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, _, exerciseId) = await TestApi.CreateArithmeticExerciseAsync(
            father, ("1 + 1", 2), ("2 + 2", 4), ("3 + 3", 6), ("4 + 4", 8));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(
            _factory, exerciseId, (int)TestStage.FreeText, goalThreshold: goalThreshold, useLeitner: false);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        // Two of four correct = 50 %.
        var submit = await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[]
            {
                new { itemIndex = 0, givenAnswer = "2" },
                new { itemIndex = 1, givenAnswer = "4" },
                new { itemIndex = 2, givenAnswer = "999" },
                new { itemIndex = 3, givenAnswer = "999" },
            },
        });
        var res = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(4, res.GetProperty("totalItems").GetInt32());
        Assert.Equal(50, res.GetProperty("scorePercent").GetInt32());
        Assert.Equal(goalThreshold, res.GetProperty("passPercent").GetInt32());
        Assert.Equal(expectPassed, res.GetProperty("passed").GetBoolean());
    }

    /// <summary>
    /// The edge case of the pass threshold: a result <b>exactly at</b> the threshold counts as passed
    /// (<c>ScorePercent &gt;= passPercent</c>), not only above it.
    /// <para>
    /// Why this needs its own test: the other tests in this class check 100 against 80, 50 against 80,
    /// 50 against 40, 50 against 3, and 50 against 90 – always genuinely above or genuinely below. Turning
    /// <c>&gt;=</c> into a <c>&gt;</c> therefore stayed fully green (docs/testplan.md, injection D01 – the most
    /// expensive gap of the measurement). The bug costs real balance in <b>both</b> directions:
    /// <see cref="TestAttempt.Passed"/> decides, via <c>IsGoalMetAsync</c>, both the goal points
    /// (<see cref="PointKind.Goal"/>) and whether the coin penalty is skipped – a child with exactly the
    /// required rate would lose the reward <i>and</i> get the deduction.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Test_ErgebnisGenauAufDerSchwelle_IstBestanden()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father); // hello→hallo, goodbye→tschüss
        // A threshold of exactly 50 %: with two tasks one correct answer is exactly the bound.
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText,
            goalThreshold: 50);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var submit = await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "hello" }, new { itemIndex = 1, givenAnswer = "falsch" } },
        });

        var res = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(50, res.GetProperty("scorePercent").GetInt32());
        Assert.Equal(50, res.GetProperty("passPercent").GetInt32());
        JsonAssert.True(res, "passed"); // exactly reached IS reached

        // And the money effect behind it: the daily goal is met and the goal points are booked.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.Equal(1, db.PositionGoalRewards.Count(r => r.PlanPositionId == positionId));
    }

    /// <summary>
    /// B-65: a vocabulary entry may carry several equally valid translations, and <b>each</b> of them counts
    /// as correct where the German side is the answer. Before that, an entry held exactly one translation – a child
    /// answering "sehr groß" for "huge" was marked wrong although the father had entered that very wording as valid.
    /// <para>
    /// Since B-597 the German side is the answer only where the child recognises, not where it writes: on every writing
    /// stage the card asks the learnt word. So the checked stage left for the alternative is the choice, and there a
    /// declared alternative must never stand as a wrong option either - which is what the choice's pool guards
    /// (<c>ExerciseChoicesTests</c>). The typed answer here is graded on that stage.
    /// </para>
    /// </summary>
    [Theory]
    [InlineData((int)TestStage.MultipleChoice, "huge", "riesig", "sehr groß")]
    [InlineData((int)TestStage.MultipleChoice, "nice", "nett", "lieb")]
    public async Task Test_GleichwertigeUebersetzung_WirdAlsRichtigGewertet(
        int stage, string word, string translation, string alternative)
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, word, translation,
            translationAlternatives: [alternative]);
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, stage);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var submit = await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            // Not the primary translation - the second declared one.
            answers = new[] { new { itemIndex = 0, givenAnswer = alternative } },
        });

        var res = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, res.GetProperty("correctItems").GetInt32());
        Assert.Equal(100, res.GetProperty("scorePercent").GetInt32());
        JsonAssert.True(res, "passed");
    }

    /// <summary>
    /// B-462: the store keeps a word-list entry as the book prints it ("dès que, aussitôt que", "(to) reply (to)"). In
    /// the child's test one typed meaning is the right answer. Since B-597 the free-text card asks the learnt word in
    /// either direction, so both directions grade the learnt side - and the front-to-back card does so through its
    /// reversed form built in the resolver, which must carry the rule as well. Through the real test path on purpose:
    /// the rule only applies where the resolver marks the atom.
    /// </summary>
    [Theory]
    [InlineData("dès que, aussitôt que", "sobald", "front-to-back", "aussitôt que")]
    [InlineData("(to) reply (to)", "antworten (auf); erwidern, entgegnen", "front-to-back", "reply")]
    [InlineData("(to) reply (to)", "antworten (auf); erwidern, entgegnen", "back-to-front", "reply")]
    public async Task Test_EineBedeutungDesEintrags_WirdAlsRichtigGewertet(
        string word, string translation, string direction, string given)
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, word, translation);
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        await SetDirectionAsync(father, _factory, exerciseId, direction);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var submit = await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = given } },
        });

        var res = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, res.GetProperty("correctItems").GetInt32());
        JsonAssert.True(res, "passed");
    }

    /// <summary>
    /// B-462, the two ends of the rule at the supervisor's preview ("Ausprobieren" must grade like the child's
    /// test): a single meaning counts for a vocabulary entry, and a part of a sentence does <b>not</b> count in a
    /// translation exercise - there the comma is part of the one answer.
    /// </summary>
    [Fact]
    public async Task Vorschau_EineBedeutungZaehlt_AberKeinSatzteilDerUebersetzung()
    {
        var father = await TestApi.AdultAsync(_factory);
        // B-597: free text asks the learnt word, so the word-list entry sits on that side.
        var meaning = TestApi.UniqueName("aussitot que");
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, $"dès que, {meaning}", "sobald");
        var vocabId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Satz-Fach") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var translationId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/translation", new
            {
                title = TestApi.UniqueName("Wegbeschreibung"),
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    sourceLang = "de",
                    targetLang = "en",
                    items = new[] { new { source = "Entschuldigung, wie komme ich zum Bahnhof", target = "Excuse me, how can I get to the station" } },
                },
            }));

        async Task<int> CorrectAsync(int exerciseId, string given, int? stage) =>
            (await (await father.PostAsJsonAsync($"/api/v1/creator/exercises/{exerciseId}/preview/check",
                new { answers = new[] { new { itemIndex = 0, givenAnswer = given, wasKnown = (bool?)null } }, stage }))
                .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("correct").GetInt32();

        Assert.Equal(1, await CorrectAsync(vocabId, meaning, (int)TestStage.FreeText));
        Assert.Equal(0, await CorrectAsync(translationId, "Excuse me", null));
        // Self-protection against a vacuous red: the translation exercise does grade - the whole sentence counts.
        Assert.Equal(1, await CorrectAsync(translationId, "Excuse me, how can I get to the station", null));
    }

    /// <summary>
    /// The alternatives belong to the <b>target</b> side only – hence the name
    /// <c>TranslationAlternatives</c>. After the direction swap the translation is the question, so they
    /// answer nothing and are dropped (<c>ExerciseContentProvider.Swap</c>). Pinned down because it is the
    /// rule, not a side effect: asked backwards, only the word itself counts.
    /// </summary>
    [Fact]
    public async Task Test_Rueckwaerts_AkzeptiertDieAlternativeNicht()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (id, key) = await TestApi.CreateStoreVocabAsync(father, "vast", "weit",
            translationAlternatives: ["ausgedehnt"]);
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        // Backwards: the card asks "weit → ?", the expected answer is "vast".
        await SetDirectionAsync(father, _factory, exerciseId, "back-to-front");
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var submit = await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "ausgedehnt" } },
        });

        var res = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, res.GetProperty("correctItems").GetInt32());
        // Self-protection against a vacuous green: the entry must really carry the alternative. (That the
        // direction really flipped is proven by the assertion above - forwards "ausgedehnt" would count.)
        var stored = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/vocabulary/{id}");
        Assert.Equal(1, stored.GetProperty("translationAlternatives").GetArrayLength());
    }

    /// <summary>Switches the query direction of a vocabulary exercise (the config is a full replacement).</summary>
    private static async Task SetDirectionAsync(HttpClient father, PuglingWebAppFactory factory, int exerciseId, string direction)
    {
        var ex = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}");
        var seriesUnitId = ex.GetProperty("seriesUnitId").GetInt32();

        // The catalog exercise detail only carries the unit, not its series - resolved via the DB the same
        // way the seed itself knows it (there is no lookup route for a unit's series without already knowing it).
        using var scope = factory.Services.CreateScope();
        var seriesId = await scope.ServiceProvider.GetRequiredService<PuglingDbContext>()
            .SeriesUnits.Where(u => u.Id == seriesUnitId).Select(u => u.SeriesId).FirstAsync();

        var res = await father.PutAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{exerciseId}", new
            {
                title = ex.GetProperty("title").GetString(),
                orderIndex = 1,
                rewardPoints = 10,
                config = new { direction },
            });
        res.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// The counter-test to <see cref="Test_GleichwertigeUebersetzung_WirdAlsRichtigGewertet"/>, and the reason
    /// equivalence has to be <b>declared</b>: two entries sharing the same word are homonyms
    /// (<c>bank → Bank</c> / <c>bank → Ufer</c>), not synonyms. Deriving equivalence from the shared word
    /// would have turned the visible defect ("right answer marked wrong") into an invisible one – the child
    /// gets credit for a meaning that does not fit the unit, and nobody sees it.
    /// </summary>
    [Fact]
    public async Task Test_HomonymeAkzeptierenSichNichtGegenseitig()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, geld) = await TestApi.CreateStoreVocabAsync(father, "bank", "Bank");
        var (_, ufer) = await TestApi.CreateStoreVocabAsync(father, "bank", "Ufer");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, geld, ufer);
        // Asked on the choice, where the German side is the answer (B-597): on a writing stage both cards ask "bank".
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.MultipleChoice);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var submit = await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            // "Ufer" is the other entry's translation - for this card it stays wrong.
            answers = new[]
            {
                new { itemIndex = 0, givenAnswer = "Ufer" },
                new { itemIndex = 1, givenAnswer = "Ufer" },
            },
        });

        var res = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, res.GetProperty("correctItems").GetInt32());
        Assert.Equal(50, res.GetProperty("scorePercent").GetInt32());
    }

    [Fact]
    public async Task Versuch_Wird_Einzeln_Mit_Ergebnissen_Gelesen()
    {
        // The single view of the attempt (a C3 coverage gap): it is the evaluation the child sees after
        // submitting - and the only place where the per-item results can be read.
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");

        // Before submitting, the attempt is already readable - only without a completion.
        var offen = await (await child.GetAsync($"{baseUrl}/{attemptId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(attemptId, offen.GetProperty("id").GetInt32());
        Assert.Equal(JsonValueKind.Null, offen.GetProperty("completedAt").ValueKind);

        (await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "hello" }, new { itemIndex = 1, givenAnswer = "falsch" } },
        })).EnsureSuccessStatusCode();

        var fertig = await (await child.GetAsync($"{baseUrl}/{attemptId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(JsonValueKind.Null, fertig.GetProperty("completedAt").ValueKind);
        Assert.Equal(1, fertig.GetProperty("correctItems").GetInt32());
        Assert.Equal(2, fertig.GetProperty("results").GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await child.GetAsync($"{baseUrl}/{attemptId + 999}")).StatusCode);
    }

    // ─────────────────────────────────── B-66: the exam question carries the same letter-box mask

    [Fact]
    public async Task LetterBoxes_ImTest_TraegtDieselbeMaskeWieDieUebung()
    {
        var father = await TestApi.AdultAsync(_factory);
        // The learnt word is written (B-597), so it carries the separators the mask keeps.
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("to grow up", "aufwachsen"));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.LetterBoxes);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");

        var next = await (await child.GetAsync($"{baseUrl}/{attemptId}/next")).Content.ReadFromJsonAsync<JsonElement>();
        var item = next.GetProperty("item");
        Assert.Equal(10, item.GetProperty("answerLength").GetInt32());
        Assert.Equal("__ ____ __", item.GetProperty("answerPattern").GetString());
    }

    // ─────────────────────────────────── B-62: SubmitResponse.AttemptsRemaining

    /// <summary>
    /// <c>Submit</c> reports the same daily count <c>Start</c> already uses for the
    /// <c>TestAttemptsExhausted</c> cap - after the first of two daily attempts one remains, after the
    /// second none does. Without this field the result screen could only guess whether "try again" would
    /// actually work, and the child's second click landed as a rejection it had no way to see coming.
    /// </summary>
    [Fact]
    public async Task Submit_MeldetVerbleibendeVersuche_ProTag()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var firstAttemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var firstSubmit = await (await child.PostAsJsonAsync($"{baseUrl}/{firstAttemptId}/submit", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "hello" }, new { itemIndex = 1, givenAnswer = "goodbye" } },
        })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, firstSubmit.GetProperty("attemptsRemaining").GetInt32());

        var secondAttemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var secondSubmit = await (await child.PostAsJsonAsync($"{baseUrl}/{secondAttemptId}/submit", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "hello" }, new { itemIndex = 1, givenAnswer = "goodbye" } },
        })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, secondSubmit.GetProperty("attemptsRemaining").GetInt32());

        // The cap itself (already covered by DocsCaptureTests) still applies - a third start is rejected.
        Assert.Equal(HttpStatusCode.Conflict, (await child.PostAsJsonAsync(baseUrl, new { })).StatusCode);
    }

    /// <summary>
    /// A supervisor attempt never counts against the child's daily cap (<see cref="TestAttempt.BySupervisor"/>),
    /// so the field is constant filler there, not a real count - regardless of how many child attempts
    /// already ran that same day.
    /// </summary>
    [Fact]
    public async Task Submit_SupervisorVersuch_LiefertKonstantenFuellwert()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        // Exhaust the child's two daily attempts first - the supervisor's own attempt below must stay
        // unaffected by that count.
        for (var i = 0; i < 2; i++)
        {
            var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
            (await child.PostAsJsonAsync($"{baseUrl}/{attemptId}/submit", new
            {
                answers = new[] { new { itemIndex = 0, givenAnswer = "hello" }, new { itemIndex = 1, givenAnswer = "goodbye" } },
            })).EnsureSuccessStatusCode();
        }

        var supervisorAttemptId = await TestApi.IdWithKeyAsync(
            await father.PostAsJsonAsync(baseUrl, new { stage = (int)TestStage.FreeText }), "attemptId");
        var supervisorSubmit = await (await father.PostAsJsonAsync($"{baseUrl}/{supervisorAttemptId}/submit", new
        {
            answers = new[] { new { itemIndex = 0, givenAnswer = "hello" }, new { itemIndex = 1, givenAnswer = "goodbye" } },
        })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, supervisorSubmit.GetProperty("attemptsRemaining").GetInt32());
    }

    /// <summary>
    /// B-544: the exam measures, so its card carries no hint – not even an always-null field – while the practice
    /// card of the very same exercise keeps it. AK 1 turns red when <c>f.Hint</c> goes back into the exam card
    /// (the field reappears); AK 2 turns red when the hint is dropped from practice as well.
    /// </summary>
    [Fact]
    public async Task Abschlusstest_Liefert_Keinen_Tipp_Das_Ueben_Schon()
    {
        var father = await TestApi.AdultAsync(_factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("Tipp-Fach") }));
        var (seriesId, seriesUnitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary", new
            {
                title = "Mit Tipp",
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    direction = "front-to-back",
                    sourceLang = "en",
                    targetLang = "de",
                    items = new[] { new { front = "hello", back = "hallo", hint = "beginnt mit h" }, new { front = "goodbye", back = "tschüss", hint = "beginnt mit t" } },
                },
            }));
        // The choice keeps the author's hint; a writing stage asks the learnt word and drops it with the turn (B-597).
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.MultipleChoice);
        var child = await TestApi.ChildAsync(_factory);

        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        var cards = await child.GetFromJsonAsync<JsonElement>($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/cards");
        Assert.Contains(cards.EnumerateArray(), c => c.GetProperty("hint").GetString() is { Length: > 0 });

        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(baseUrl, new { }), "attemptId");
        var next = await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{attemptId}/next");
        var item = next.GetProperty("item");
        Assert.Equal(JsonValueKind.Object, item.ValueKind);
        Assert.False(item.TryGetProperty("hint", out _), "The exam card must not carry a hint field (B-544).");
    }
}
