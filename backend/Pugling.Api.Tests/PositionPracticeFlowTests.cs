using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// End-to-end of the new position-based practicing (stage 3): a catalog exercise is played via a
/// study plan position, content comes from the exercise config, Leitner progress runs through
/// <see cref="PositionItemProgress"/>. The position is seeded directly (position CRUD follows in stage 5).
/// </summary>
public class PositionPracticeFlowTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    [Fact]
    public async Task Vokabel_Position_RichtigGetippt_BringtPunkteUndBoxAufstieg()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions";

        // Start the session
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));

        // Cards: both items are new → due; a typed stage → no solution included.
        var cards = await (await child.GetAsync($"{baseUrl}/{sessionId}/cards"))
            .Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.Equal(2, cards!.Count);
        Assert.Equal("hello", cards[0].GetProperty("prompt").GetString());
        Assert.Equal(JsonValueKind.Null, cards[0].GetProperty("reveal").ValueKind);

        // A correct answer on item 0 → graded
        var review = await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review",
            new { itemIndex = 0, givenAnswer = "hallo" });
        var outcome = await review.Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(outcome, "wasCorrect");
        Assert.True(outcome.GetProperty("awarded").GetInt32() > 0);
        Assert.Equal(2, outcome.GetProperty("box").GetInt32()); // box 1 → 2

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var prog = db.PositionItemProgress.Single(p => p.PlanPositionId == positionId && p.ItemIndex == 0);
        Assert.Equal(2, prog.Box);
        Assert.NotNull(prog.IntroducedAt);
        Assert.True(db.ChildPointsEntries.Where(e => e.ChildId == 1 && e.Kind == PointKind.Base).Sum(e => e.Amount) > 0);
    }

    /*
     * Since B-65 every declared translation counts on the typed stages. On self-assessment the CHILD decides,
     * though - and whoever thought of "sehr groß" and is shown only "riesig" marks themselves wrong. The same
     * damage as the original defect, this time self-inflicted (B-70).
     * The direction swap is covered by `PositionTestFlowTests.Rueckwaerts_DecktKeineAlternativeAuf` - the helper
     * that flips the direction lives there.
     */
    [Fact]
    public async Task Selbsteinschaetzung_DecktJedeGleichwertigeUebersetzungAuf()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "huge", "riesig",
            translationAlternatives: ["sehr groß"]);
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var child = await TestApi.ChildAsync(_factory);

        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.SelfAssess);
        var karte = await ErsteKarteAsync(child, planId, positionId);
        Assert.Equal("riesig", karte.GetProperty("reveal").GetString());
        Assert.Equal(new[] { "sehr groß" },
            karte.GetProperty("revealAlternatives").EnumerateArray().Select(a => a.GetString()).ToArray());

        // The typed stage keeps withholding both - the alternative is no side door into the solution.
        var (typedPlanId, typedPositionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var getippt = await ErsteKarteAsync(child, typedPlanId, typedPositionId);
        Assert.Equal(JsonValueKind.Null, getippt.GetProperty("reveal").ValueKind);
        Assert.Equal(JsonValueKind.Null, getippt.GetProperty("revealAlternatives").ValueKind);
    }

    /// <summary>The first card of a fresh practice session – shared by the reveal tests.</summary>
    private static async Task<JsonElement> ErsteKarteAsync(HttpClient child, int planId, int positionId)
    {
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        var cards = await child.GetFromJsonAsync<List<JsonElement>>($"{baseUrl}/{sessionId}/cards");
        return cards![0];
    }

    // ─────────────────────────────────── B-66: the letter-box mask fixes punctuation/spacing

    [Fact]
    public async Task LetterBoxes_MehrteiligeLoesung_TraegtDieMaskeMitFestenTrennzeichen()
    {
        var father = await TestApi.AdultAsync(_factory);
        // Front/back swapped on purpose: direction is front-to-back, and the ANSWER (the typed side) is the
        // one that needs a space to test the mask - "to grow up" as a translation, not the prompt.
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ("aufwachsen", "to grow up"));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.LetterBoxes);
        var child = await TestApi.ChildAsync(_factory);
        var karte = await ErsteKarteAsync(child, planId, positionId);

        Assert.Equal(10, karte.GetProperty("answerLength").GetInt32()); // "to grow up".Length
        Assert.Equal("__ ____ __", karte.GetProperty("answerPattern").GetString());
        // Still withheld like the length - a typed stage never reveals the solution itself.
        Assert.Equal(JsonValueKind.Null, karte.GetProperty("reveal").ValueKind);
    }

    // ─────────────────────────────────── B-96: ShowBoth is a free display stage, not self-assessment

    [Fact]
    public async Task ShowBoth_ZeigtBeideSeitenSofort_UndIstAlsAnzeigenurMarkiert()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.ShowBoth);
        var child = await TestApi.ChildAsync(_factory);

        var karte = await ErsteKarteAsync(child, planId, positionId);
        // Both sides at once - front (prompt) AND back (reveal), like self-assessment, but flagged distinctly.
        Assert.Equal("hello", karte.GetProperty("prompt").GetString());
        Assert.Equal("hallo", karte.GetProperty("reveal").GetString());
        Assert.True(karte.GetProperty("displayOnly").GetBoolean());
    }

    [Fact]
    public async Task ShowBoth_ZaehltAlsGeuebtAberNichtAlsTrefferOderBoxbewegung()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        // A fresh child: an isolated wallet, so the coin count below is not contaminated by other tests
        // sharing the default child (id 1) in this fixture's database.
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "ShowBoth-Kind", pin = "7501" }));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.ShowBoth, childId: childId);
        var child = await TestApi.ChildAsync(_factory, childId, "7501");
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));

        // Even a client claiming "wasKnown: true" must not be scored or move the box - the server enforces
        // this from the stage, not from trusting the client's self-report (there is none on this stage).
        var review = await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review",
            new { itemIndex = 0, wasKnown = true });
        var outcome = await review.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, outcome.GetProperty("awarded").GetInt32());
        Assert.Equal(1, outcome.GetProperty("box").GetInt32()); // unmoved (default box 1, never applied)

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var prog = db.PositionItemProgress.Single(p => p.PlanPositionId == positionId && p.ItemIndex == 0);
        Assert.Equal(1, prog.Box);
        Assert.Equal(0, prog.ReviewCount); // ApplyReview never ran - "practiced" is not "reviewed"
        // But it IS counted as practiced: the introduction is stamped, same as any other first contact.
        Assert.NotNull(prog.IntroducedAt);
        Assert.Equal(0, db.ChildPointsEntries.Count(e => e.ChildId == childId && e.Kind == PointKind.Base));
        // The cross-plan history must not carry a verdict either - a spoofed "wasKnown: true" must not turn
        // into a "correctly answered" row on a stage that never judges anything (pugling-reviewer finding).
        var historyRow = db.ItemReviewEvents.Single(e => e.ChildId == childId && e.PlanPositionId == positionId);
        Assert.False(historyRow.WasCorrect);
    }

    [Fact]
    public async Task ShowBoth_AlsKlausurstufe_WirdAbgelehnt()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.ShowBoth);
        var child = await TestApi.ChildAsync(_factory);

        var res = await child.PostAsJsonAsync(
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests", new { });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("stage_not_testable", (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    /// <summary>
    /// A display-only day must leave exactly one playable way in, and that way has to discharge the duty.
    /// Otherwise the position is a dead end: the daily mission offers only the test button, the test answers
    /// <c>stage_not_testable</c>, and a mandatory cadence books the coin penalty for a duty the product blocks.
    /// </summary>
    [Fact]
    public async Task ShowBoth_OhneLeitner_IstNichtPruefbar_UndDieGespielteRundeErfuelltDiePflicht()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Kennenlern-Kind", pin = "7502" }));
        // Exactly the shape the seed and the father's wizard produce: a display stage, no Leitner, daily duty.
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.ShowBoth,
            childId: childId, cadence: GoalCadence.Daily, useLeitner: false);
        var child = await TestApi.ChildAsync(_factory, childId, "7502");

        var vorher = (await child.GetFromJsonAsync<JsonElement>($"/api/v1/student/study-plans/{planId}/overview"))
            .GetProperty("today").GetProperty("positions").EnumerateArray().Single();
        // Not testable TODAY although the type has a check mode - the client must not offer the test button.
        Assert.Equal("StudyPlanTest", vorher.GetProperty("checkMode").GetString());
        Assert.False(vorher.GetProperty("testable").GetBoolean());
        Assert.False(vorher.GetProperty("goalMet").GetBoolean());

        // Play the whole round: two cards, "Weiter" each time (no verdict on this stage).
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        foreach (var itemIndex in new[] { 0, 1 })
            Assert.Equal(HttpStatusCode.OK,
                (await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review", new { itemIndex })).StatusCode);

        var nachher = (await child.GetFromJsonAsync<JsonElement>($"/api/v1/student/study-plans/{planId}/overview"))
            .GetProperty("today").GetProperty("positions").EnumerateArray().Single();
        JsonAssert.True(nachher, "goalMet");
    }

    /// <summary>
    /// B-117 (found in a browser rollengang of B-114): the position CARD already knew it could not be
    /// tested (<see cref="ShowBoth_OhneLeitner_IstNichtPruefbar_UndDieGespielteRundeErfuelltDiePflicht"/>),
    /// but the SESSION did not - <c>SohnPractice.tsx</c>'s "Weiter zum Test"/"Zum Test" buttons after a
    /// round (or when nothing is due) had no field to gate on and always offered the exam, which then
    /// answered <c>stage_not_testable</c> and rendered its raw English detail text straight to the child.
    /// </summary>
    [Fact]
    public async Task ShowBoth_PracticeSessionTraegtTestableFalse()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Kennenlern-Kind-2", pin = "7503" }));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.ShowBoth,
            childId: childId, cadence: GoalCadence.Daily, useLeitner: false);
        var child = await TestApi.ChildAsync(_factory, childId, "7503");
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions";

        var start = await (await child.PostAsJsonAsync(baseUrl, new { })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(start.GetProperty("testable").GetBoolean());

        // Re-reading the same session (as the "done"/"empty" screens do) must agree - not just the start response.
        var sessionId = start.GetProperty("id").GetInt32();
        var reread = await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{sessionId}");
        Assert.False(reread.GetProperty("testable").GetBoolean());
    }

    /// <summary>Gegenprobe zu <see cref="ShowBoth_PracticeSessionTraegtTestableFalse"/>: eine normale,
    /// getippte Stufe bleibt testbar.</summary>
    [Fact]
    public async Task GetippteStufe_PracticeSessionTraegtTestableTrue()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions";

        var start = await (await child.PostAsJsonAsync(baseUrl, new { })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(start.GetProperty("testable").GetBoolean());
    }

    [Fact]
    public async Task Vokabel_Position_ZweiteWertungAmSelbenTag_WirdNichtGewertet()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));

        // The first grading: 200 + a result
        var first = await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review", new { itemIndex = 0, givenAnswer = "hallo" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Grading the same card again on the same day: only recorded, no further points (anti-farming).
        // The cursor moves on (200), but no points flow.
        var second = await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review", new { itemIndex = 0, givenAnswer = "hallo" });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondOutcome = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, secondOutcome.GetProperty("awarded").GetInt32());
    }

    [Fact]
    public async Task Vokabel_Position_FalscheAntwort_BleibtInBox1UndFaellig()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));

        var review = await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review", new { itemIndex = 0, givenAnswer = "falsch" });
        var outcome = await review.Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.False(outcome, "wasCorrect");
        Assert.Equal(0, outcome.GetProperty("awarded").GetInt32());
        Assert.Equal(1, outcome.GetProperty("box").GetInt32());
    }

    [Fact]
    public async Task Position_UnbekanntFuerDenPlan_LiefertNotFound()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);

        // A position that does not exist (in this plan) → the start must return 404, not play into the void.
        var res = await child.PostAsJsonAsync(
            $"/api/v1/student/study-plans/{planId}/positions/{positionId + 999}/practice-sessions", new { });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Sitzung_Wird_Einzeln_Gelesen()
    {
        // The single view of the session (a C3 coverage gap): the client fetches it after a reload to find
        // cursor and mode again without starting a second session.
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);

        var sitzung = await (await child.GetAsync($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}"))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(sessionId, sitzung.GetProperty("id").GetInt32());
        Assert.Equal(positionId, sitzung.GetProperty("positionId").GetInt32());
        Assert.Equal(0, sitzung.GetProperty("reviewCount").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound,
            (await child.GetAsync($"{TestApi.PracticeBase(planId, positionId)}/{sessionId + 999}")).StatusCode);
    }
}
