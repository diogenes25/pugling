using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// The mixed review's grading (B-765): each card moves the box of its own position by the shared Leitner rule, checked
/// hits pay coins under the cap the bonus training shares, and the round touches no duty.
/// </summary>
public class MischrundeWertungTests(PuglingWebAppFactory factory) : MischrundeTestBase(factory), IClassFixture<PuglingWebAppFactory>
{
    // The card shows one side and expects the other (free text: German shown, the learnt word written; choice: the learnt
    // word shown, the meaning picked).
    private static string RightAnswer(JsonElement card)
    {
        var prompt = card.GetProperty("card").GetProperty("prompt").GetString();
        var w = Words.Single(w => w.Front == prompt || w.Back == prompt);
        return w.Front == prompt ? w.Back : w.Front;
    }

    private static object Answer(JsonElement card, bool right) => new
    {
        cardId = card.GetProperty("cardId").GetInt32(),
        givenAnswer = right ? RightAnswer(card) : "falsch",
        wasKnown = right,
    };

    private static Task<HttpResponseMessage> SubmitAsync(Kid k, JsonElement run, params object[] answers) =>
        k.Child.PostAsJsonAsync($"{Base(k)}/runs/{run.GetProperty("runId").GetInt32()}/submit", new { answers });

    private static async Task<JsonElement> SubmitOkAsync(Kid k, JsonElement run, params object[] answers)
    {
        var res = await SubmitAsync(k, run, answers);
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static JsonElement CardOf(JsonElement run, int positionId) =>
        run.GetProperty("cards").EnumerateArray().Single(c => c.GetProperty("positionId").GetInt32() == positionId);

    private PositionItemProgress Progress(int positionId, int itemIndex) =>
        Read(db => db.PositionItemProgress.Single(p => p.PlanPositionId == positionId && p.ItemIndex == itemIndex));

    private int BonusCoins(int childId) =>
        Read(db => db.ChildPointsEntries.Where(e => e.ChildId == childId && e.Kind == PointKind.ExtraTrainingCoins).Sum(e => e.Amount));

    /// <summary>
    /// AK 1: a right card climbs one box in ITS position and gets a new due date there; the same word in another position
    /// of the same exercise keeps its box. The answer also reaches the plan-spanning item history (decision 6). Red if the
    /// round books on another position or on the item progress instead.
    /// </summary>
    [Fact]
    public async Task Richtige_Karte_Steigt_Im_Kasten_Ihrer_Position()
    {
        var k = await KidAsync();
        var (_, posA) = await PositionAsync(k);
        var exerciseId = Read(db => db.PlanPositions.Single(p => p.Id == posA).ExerciseId);
        var (_, posB) = await PositionAsync(k, exerciseId: exerciseId);
        SetDue(posA, 0, Today);
        var beforeB = Progress(posB, 0);

        var run = await StartAsync(k);
        var result = await SubmitOkAsync(k, run, Answer(CardOf(run, posA), right: true));

        var a = Progress(posA, 0);
        Assert.Equal(TestApi.ClimbedBox + 1, a.Box);
        Assert.Equal(Today.AddDays(7), a.DueOn);
        var b = Progress(posB, 0);
        Assert.Equal((beforeB.Box, beforeB.DueOn, beforeB.ReviewCount), (b.Box, b.DueOn, b.ReviewCount));
        Assert.Equal(1, result.GetProperty("correct").GetInt32());
        Assert.Equal(1, Read(db => db.ItemReviewEvents.Count(e => e.ChildId == k.ChildId && e.PlanPositionId == posA && e.WasCorrect)));
    }

    /// <summary>
    /// AK 2: a wrong card falls by its own position's rule - one box with forgiving demotion, to box 1 without - and is
    /// due again today. Red if the round brings a demotion rule of its own.
    /// </summary>
    [Fact]
    public async Task Falsche_Karte_Faellt_Nach_Der_Regel_Ihrer_Position()
    {
        var k = await KidAsync();
        var (_, forgiving) = await PositionAsync(k);
        var (_, strict) = await PositionAsync(k);
        Db(db => db.PlanPositions.Single(p => p.Id == forgiving).ForgivingDemotion = true);
        SetDue(forgiving, 0, Today);
        SetDue(strict, 0, Today);

        var run = await StartAsync(k);
        await SubmitOkAsync(k, run, Answer(CardOf(run, forgiving), right: false), Answer(CardOf(run, strict), right: false));

        Assert.Equal((TestApi.ClimbedBox - 1, (DateOnly?)Today), (Progress(forgiving, 0).Box, Progress(forgiving, 0).DueOn));
        Assert.Equal((1, (DateOnly?)Today), (Progress(strict, 0).Box, Progress(strict, 0).DueOn));
    }

    /// <summary>
    /// AK 3: checked hits pay the typed rate (3) and nothing else - no combo, no speed, no new-content points - and only up
    /// to the cap the bonus training shares: 2 bonus coins earned today and a cap of 4 leave 2 of the 6 earned. The bonus
    /// training then reads the shared total. Red if the cap counts only one source.
    /// </summary>
    [Fact]
    public async Task Muenzen_Nur_Fuer_Treffer_Bis_Zum_Gemeinsamen_Deckel()
    {
        var k = await KidAsync();
        var (_, pos) = await PositionAsync(k);
        SetDue(pos, 0, Today);
        SetDue(pos, 1, Today);
        Db(db =>
        {
            db.Children.Single(c => c.Id == k.ChildId).BonusDailyCoinCap = 4;
            var exerciseId = db.PlanPositions.Single(p => p.Id == pos).ExerciseId;
            var now = Factory.Clock.GetUtcNow().UtcDateTime;
            db.ExtraTrainingRuns.Add(new ExtraTrainingRun
            {
                ChildId = k.ChildId,
                ExerciseId = exerciseId,
                Day = Today,
                Form = ExtraForm.Typed,
                Stage = (int)TestStage.FreeText,
                StartedAt = now,
                CompletedAt = now,
                CoinsAwarded = 2,
                ItemCount = 4,
            });
        });
        var pointsBefore = Read(db => db.ChildPointsEntries.Count(e => e.ChildId == k.ChildId));

        var run = await StartAsync(k);
        Assert.Equal(2, run.GetProperty("coinsLeftToday").GetInt32());
        var answers = run.GetProperty("cards").EnumerateArray().Select(c => Answer(c, right: true)).ToArray();
        var result = await SubmitOkAsync(k, run, answers);

        Assert.Equal(6, result.GetProperty("coinsEarned").GetInt32());
        Assert.Equal(2, result.GetProperty("coinsAwarded").GetInt32());
        Assert.True(result.GetProperty("capReached").GetBoolean());
        Assert.Equal(4, result.GetProperty("coinsToday").GetInt32());
        Assert.Equal(2, BonusCoins(k.ChildId));
        // One booking, of the bonus kind: no review points, combo or speed bonus.
        Assert.Equal(pointsBefore + 1, Read(db => db.ChildPointsEntries.Count(e => e.ChildId == k.ChildId)));

        var overview = await (await k.Child.GetAsync($"/api/v1/student/children/{k.ChildId}/extra-training"))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(4, overview.GetProperty("coinsToday").GetInt32());
    }

    /// <summary>
    /// AK 3, the lock: a round started while the goals were met is not graded once a goal is open again - here a new
    /// daily duty. Red if the submit skips the lock.
    /// </summary>
    [Fact]
    public async Task Abgabe_Bei_Offener_Tagespflicht_Gesperrt()
    {
        var k = await KidAsync();
        var (_, pos) = await PositionAsync(k);
        SetDue(pos, 0, Today);
        var run = await StartAsync(k);
        await PositionAsync(k, GoalCadence.Daily);

        await AssertProblemAsync(await SubmitAsync(k, run, Answer(CardOf(run, pos), right: true)), HttpStatusCode.Conflict,
            "extra_training_locked");
        Assert.Equal(0, BonusCoins(k.ChildId));
    }

    /// <summary>
    /// AK 4 (Absichtserklärung: nothing in the round writes these): the round leaves no practice session, no goal reward
    /// and no daily chest. The mutant that would make it red - a learn session written by the round - is not in the code.
    /// </summary>
    [Fact]
    public async Task Runde_Beruehrt_Keine_Pflicht()
    {
        var k = await KidAsync();
        var (plan, pos) = await PositionAsync(k);
        SetDue(pos, 0, Today);

        var run = await StartAsync(k);
        await SubmitOkAsync(k, run, Answer(CardOf(run, pos), right: true));

        Assert.Equal(0, Read(db => db.PracticeSessions.Count(s => s.StudyPlanId == plan)));
        Assert.Equal(0, Read(db => db.PositionGoalRewards.Count(r => r.PlanPositionId == pos)));
        Assert.Equal(0, Read(db => db.DailyBoxClaims.Count(c => c.ChildId == k.ChildId)));
    }

    /// <summary>
    /// AK 5: the card is asked and graded at its position's stage of the day - a choice position asks a choice and pays
    /// the choice rate (1). Red if the round picks a stage of its own.
    /// </summary>
    [Fact]
    public async Task Karte_In_Der_Stufe_Der_Position()
    {
        var k = await KidAsync();
        var (_, pos) = await PositionAsync(k);
        Db(db => db.PlanPositions.Single(p => p.Id == pos).Stage = (int)TestStage.MultipleChoice);
        SetDue(pos, 0, Today);

        var run = await StartAsync(k);
        var card = CardOf(run, pos);
        Assert.Equal((int)TestStage.MultipleChoice, card.GetProperty("stage").GetInt32());
        Assert.Equal(JsonValueKind.Array, card.GetProperty("card").GetProperty("choices").ValueKind);
        var result = await SubmitOkAsync(k, run, Answer(card, right: true));

        Assert.Equal(1, result.GetProperty("coinsAwarded").GetInt32());
    }

    /// <summary>
    /// AK 6 (Absichtserklärung): a bonus run over one exercise still moves no box. Red if someone books the box in the
    /// bonus path.
    /// </summary>
    [Fact]
    public async Task Bonusrunde_Bewegt_Weiter_Keinen_Kasten()
    {
        var k = await KidAsync();
        var (_, pos) = await PositionAsync(k);
        var exerciseId = Read(db => db.PlanPositions.Single(p => p.Id == pos).ExerciseId);
        var before = Read(db => db.PositionItemProgress.Where(p => p.PlanPositionId == pos)
            .Select(p => new { p.ItemIndex, p.Box, p.DueOn, p.ReviewCount }).OrderBy(p => p.ItemIndex).ToList());

        var start = await k.Child.PostAsync($"/api/v1/student/children/{k.ChildId}/extra-training/{exerciseId}/runs", null);
        var bonus = await start.Content.ReadFromJsonAsync<JsonElement>();
        var answers = bonus.GetProperty("cards").EnumerateArray()
            .Select(c => new { itemIndex = c.GetProperty("itemIndex").GetInt32(), givenAnswer = (string?)null, wasKnown = true })
            .ToArray();
        (await k.Child.PostAsJsonAsync($"/api/v1/student/children/{k.ChildId}/extra-training/runs/{bonus.GetProperty("runId").GetInt32()}/submit",
            new { answers })).EnsureSuccessStatusCode();

        var after = Read(db => db.PositionItemProgress.Where(p => p.PlanPositionId == pos)
            .Select(p => new { p.ItemIndex, p.Box, p.DueOn, p.ReviewCount }).OrderBy(p => p.ItemIndex).ToList());
        Assert.Equal(before, after);
    }

    /// <summary>
    /// AK 7 (decision 5): with intervals [0, 1, 0, 0] a right card lands in box 4 with 0 days - due again today - and pays
    /// nothing, though it counts as right. Red if the pay gate is missing.
    /// </summary>
    [Fact]
    public async Task Karte_Die_Heute_Wieder_Faellig_Ist_Zahlt_Nicht()
    {
        var k = await KidAsync();
        var (_, pos) = await PositionAsync(k);
        Db(db => db.PlanPositions.Single(p => p.Id == pos).BoxIntervalDays = [0, 1, 0, 0]);
        SetDue(pos, 0, Today);

        var run = await StartAsync(k);
        var result = await SubmitOkAsync(k, run, Answer(CardOf(run, pos), right: true));

        Assert.Equal(1, result.GetProperty("correct").GetInt32());
        Assert.Equal(0, result.GetProperty("coinsAwarded").GetInt32());
        Assert.Equal(Today, Progress(pos, 0).DueOn);
        Assert.Equal(0, BonusCoins(k.ChildId));
    }

    /// <summary>
    /// AK 8: a second submit of the same round - after it or at the same time (double tap) - books nothing more, and the
    /// child's concurrency stamp moves. Red if the round pays twice or the stamp stays.
    /// </summary>
    [Fact]
    public async Task Doppelte_Abgabe_Bucht_Einmal_Und_Bumpt_Den_Stempel()
    {
        var k = await KidAsync();
        var (_, pos) = await PositionAsync(k);
        SetDue(pos, 0, Today);
        var stampBefore = Read(db => db.Children.Single(c => c.Id == k.ChildId).ConcurrencyStamp);

        var run = await StartAsync(k);
        var answer = Answer(CardOf(run, pos), right: true);
        var both = await Task.WhenAll(SubmitAsync(k, run, answer), SubmitAsync(k, run, answer));
        Assert.Equal(1, both.Count(r => r.IsSuccessStatusCode));
        await AssertProblemAsync(await SubmitAsync(k, run, answer), HttpStatusCode.Conflict, "mixed_review_completed");

        Assert.Equal(3, BonusCoins(k.ChildId));
        Assert.Equal(TestApi.ClimbedBox + 1, Progress(pos, 0).Box);
        Assert.NotEqual(stampBefore, Read(db => db.Children.Single(c => c.Id == k.ChildId).ConcurrencyStamp));
    }

    /// <summary>
    /// A card practised in its position between start and submit is no longer due: it is skipped - no box move, no coin -
    /// while the other card is graded. Red if the submit grades a card that was reviewed today already.
    /// </summary>
    [Fact]
    public async Task Inzwischen_Geuebte_Karte_Wird_Uebersprungen()
    {
        var k = await KidAsync();
        var (_, posA) = await PositionAsync(k);
        var (_, posB) = await PositionAsync(k);
        SetDue(posA, 0, Today);
        SetDue(posB, 0, Today);
        var run = await StartAsync(k);
        SetDue(posB, 0, Today.AddDays(2));

        var result = await SubmitOkAsync(k, run, Answer(CardOf(run, posA), right: true), Answer(CardOf(run, posB), right: true));

        Assert.Equal(1, result.GetProperty("total").GetInt32());
        var skipped = result.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("positionId").GetInt32() == posB);
        Assert.True(skipped.GetProperty("skipped").GetBoolean());
        Assert.Equal(0, skipped.GetProperty("coinsAwarded").GetInt32());
        Assert.Equal(TestApi.ClimbedBox, Progress(posB, 0).Box);
        Assert.Equal(3, BonusCoins(k.ChildId));
    }

    /// <summary>
    /// A round drawn yesterday is not graded today: <c>mixed_review_stale</c>, the round is gone and no box moved. Red if
    /// the submit grades a round of another day.
    /// </summary>
    [Fact]
    public async Task Runde_Von_Gestern_Wird_Verworfen()
    {
        var k = await KidAsync();
        var (_, pos) = await PositionAsync(k);
        SetDue(pos, 0, Today);
        var run = await StartAsync(k);
        Db(db => db.MixedReviewRuns.Single(r => r.Id == run.GetProperty("runId").GetInt32()).Day = Today.AddDays(-1));

        await AssertProblemAsync(await SubmitAsync(k, run, Answer(CardOf(run, pos), right: true)), HttpStatusCode.Conflict,
            "mixed_review_stale");

        Assert.Equal(0, Read(db => db.MixedReviewRuns.Count(r => r.ChildId == k.ChildId)));
        Assert.Equal(TestApi.ClimbedBox, Progress(pos, 0).Box);
    }

    /// <summary>
    /// A self-assessed card (position on the self-assessment stage) moves its box like the practice's claim - up for
    /// "known", without counting as a graded review - and pays nothing: only checked hits pay (decision 3). Red if a claim
    /// pays or counts as a review.
    /// </summary>
    [Fact]
    public async Task Selbsteinschaetzung_Bewegt_Den_Kasten_Und_Zahlt_Nichts()
    {
        var k = await KidAsync();
        var (_, pos) = await PositionAsync(k);
        Db(db => db.PlanPositions.Single(p => p.Id == pos).Stage = (int)TestStage.SelfAssess);
        SetDue(pos, 0, Today);
        var reviewsBefore = Progress(pos, 0).ReviewCount;

        var run = await StartAsync(k);
        var card = CardOf(run, pos);
        Assert.Equal(0, card.GetProperty("card").GetProperty("coinsPerHit").GetInt32());
        var result = await SubmitOkAsync(k, run, Answer(card, right: true));

        Assert.Equal(0, result.GetProperty("coinsAwarded").GetInt32());
        Assert.Equal(TestApi.ClimbedBox + 1, Progress(pos, 0).Box);
        Assert.Equal(reviewsBefore, Progress(pos, 0).ReviewCount);
        Assert.Equal(0, BonusCoins(k.ChildId));
    }

    /// <summary>
    /// A position that carries an open duty by the time the round is handed in (the father set a weekly goal meanwhile)
    /// gives a skipped card: graded, it would empty the position's next learn round, which then counts as met. The daily
    /// goal of another plan keeps the round open. Red if the submit does not check the duty again.
    /// </summary>
    [Fact]
    public async Task Karte_Einer_Inzwischen_Pflichtigen_Position_Wird_Uebersprungen()
    {
        var k = await KidAsync();
        var (planDaily, posDaily) = await PositionAsync(k, GoalCadence.Daily);
        SeedPassedTest(planDaily, posDaily);
        var (_, pos) = await PositionAsync(k);
        SetDue(pos, 0, Today);
        var run = await StartAsync(k);
        Db(db => db.PlanPositions.Single(p => p.Id == pos).Cadence = GoalCadence.Weekly);

        var result = await SubmitOkAsync(k, run, Answer(CardOf(run, pos), right: true));

        Assert.True(result.GetProperty("items")[0].GetProperty("skipped").GetBoolean());
        Assert.Equal(TestApi.ClimbedBox, Progress(pos, 0).Box);
    }

    /// <summary>
    /// Review finding (Sprint 130): a card missed in a first round shows its answer in the result and is due again at once.
    /// Asked again in a second round the same day, the hit moves its box but pays nothing - otherwise a word the child does
    /// not know turns into coins by copying the answer just shown. Red if the pay ignores an earlier review that day.
    /// </summary>
    [Fact]
    public async Task Zweite_Runde_Desselben_Tages_Zahlt_Die_Gezeigte_Antwort_Nicht()
    {
        var k = await KidAsync();
        var (_, pos) = await PositionAsync(k);
        SetDue(pos, 0, Today);

        var first = await StartAsync(k);
        var missed = await SubmitOkAsync(k, first, Answer(CardOf(first, pos), right: false));
        Assert.False(string.IsNullOrEmpty(missed.GetProperty("items")[0].GetProperty("expected").GetString()));
        var second = await StartAsync(k);
        var result = await SubmitOkAsync(k, second, Answer(CardOf(second, pos), right: true));

        Assert.Equal(1, result.GetProperty("correct").GetInt32());
        Assert.Equal(0, result.GetProperty("coinsAwarded").GetInt32());
        Assert.Equal(0, BonusCoins(k.ChildId));
        Assert.Equal(2, Progress(pos, 0).Box);
    }

    /// <summary>
    /// Rollengang finding (Sprint 130): a word introduced today - its learn card showed it - and a word a test of today
    /// asked - its result showed the solution - pay nothing when hit in the mixed review; a word met only before today pays.
    /// Red if the pay gate looks at the review stamp alone.
    /// </summary>
    [Fact]
    public async Task Heute_Gelernte_Oder_Getestete_Karte_Zahlt_Nicht()
    {
        var k = await KidAsync();
        var (plan, pos) = await PositionAsync(k);
        SetDue(pos, 0, Today);
        SetDue(pos, 1, Today);
        SetDue(pos, 2, Today);
        Db(db =>
        {
            db.PositionItemProgress.Single(p => p.PlanPositionId == pos && p.ItemIndex == 0).IntroducedAt = Today;
            var at = Today.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc);
            var attempt = new TestAttempt
            {
                StudyPlanId = plan,
                PlanPositionId = pos,
                Day = Today,
                StageValue = (int)TestStage.FreeText,
                StartedAt = at,
                CompletedAt = at,
                TotalItems = 1,
            };
            attempt.Results.Add(new TestItemResult { ItemIndex = 1, StageValue = (int)TestStage.FreeText, GivenAnswer = "x" });
            db.TestAttempts.Add(attempt);
        });

        var run = await StartAsync(k);
        var answers = run.GetProperty("cards").EnumerateArray().Select(c => Answer(c, right: true)).ToArray();
        var result = await SubmitOkAsync(k, run, answers);

        Assert.Equal(3, result.GetProperty("correct").GetInt32());
        // Only card 2 - met before today - pays the typed rate.
        Assert.Equal(3, result.GetProperty("coinsAwarded").GetInt32());
        var paid = result.GetProperty("items").EnumerateArray()
            .Where(i => i.GetProperty("coinsAwarded").GetInt32() > 0).Select(i => i.GetProperty("cardId").GetInt32()).ToList();
        var card2 = run.GetProperty("cards").EnumerateArray().Single(c => c.GetProperty("card").GetProperty("itemIndex").GetInt32() == 2);
        Assert.Equal([card2.GetProperty("cardId").GetInt32()], paid);
    }

    /// <summary>
    /// Review finding (Sprint 130, 🟢): the father switches Leitner off between start and submit - the card is skipped and
    /// no box moves. Red if the submit does not check Leitner again.
    /// </summary>
    [Fact]
    public async Task Position_Ohne_Leitner_Bei_Der_Abgabe_Wird_Uebersprungen()
    {
        var k = await KidAsync();
        var (_, pos) = await PositionAsync(k);
        SetDue(pos, 0, Today);
        var run = await StartAsync(k);
        Db(db => db.PlanPositions.Single(p => p.Id == pos).UseLeitner = false);

        var result = await SubmitOkAsync(k, run, Answer(CardOf(run, pos), right: true));

        Assert.True(result.GetProperty("items")[0].GetProperty("skipped").GetBoolean());
        Assert.Equal(TestApi.ClimbedBox, Progress(pos, 0).Box);
    }

    /// <summary>A card whose exercise changed since the start is skipped unpaid (risk 5, pattern of B-545).</summary>
    [Fact]
    public async Task Geaenderte_Uebung_Wird_Uebersprungen()
    {
        var k = await KidAsync();
        var (_, pos) = await PositionAsync(k);
        SetDue(pos, 0, Today);
        var run = await StartAsync(k);
        Db(db =>
        {
            var exercise = db.Exercises.Single(e => e.Id == db.PlanPositions.Single(p => p.Id == pos).ExerciseId);
            exercise.ConfigJson = exercise.ConfigJson.Replace("\"front-to-back\"", "\"back-to-front\"");
        });

        var result = await SubmitOkAsync(k, run, Answer(CardOf(run, pos), right: true));

        Assert.True(result.GetProperty("items")[0].GetProperty("skipped").GetBoolean());
        Assert.Equal(0, BonusCoins(k.ChildId));
        Assert.Equal(TestApi.ClimbedBox, Progress(pos, 0).Box);
    }
    /// <summary>
    /// B-624, review finding: the round writes the position's own box, so it follows the position's accent leniency. Item 0
    /// of each position is turned into "café" in the store; "cafe" holds box 3 on a lenient position (due after box 3's
    /// interval, flagged in the result and the history) and drops to box 1 on a strict one. Red when the round ignores the
    /// leniency (lenient box 1) or the switch (strict box 3).
    /// </summary>
    [Fact]
    public async Task Akzentfehler_Haelt_Den_Kasten_Nur_Auf_Milder_Position()
    {
        var k = await KidAsync();
        var (_, lenient) = await PositionAsync(k);
        var (_, strict) = await PositionAsync(k);
        Db(db =>
        {
            db.PlanPositions.Single(p => p.Id == strict).AccentLenient = false;
            foreach (var positionId in new[] { lenient, strict })
            {
                var exerciseId = db.PlanPositions.Single(p => p.Id == positionId).ExerciseId;
                var item = db.ExerciseItems.Where(i => i.ExerciseId == exerciseId).OrderBy(i => i.OrderIndex).First();
                db.Vocabularies.Single(v => v.Id == item.VocabularyId).Word = "café";
            }
        });
        SetDue(lenient, 0, Today);
        SetDue(strict, 0, Today);

        var run = await StartAsync(k);
        object Cafe(int positionId) => new { cardId = CardOf(run, positionId).GetProperty("cardId").GetInt32(), givenAnswer = "cafe", wasKnown = (bool?)null };
        var result = await SubmitOkAsync(k, run, Cafe(lenient), Cafe(strict));

        Assert.Equal((TestApi.ClimbedBox, (DateOnly?)Today.AddDays(4)), (Progress(lenient, 0).Box, Progress(lenient, 0).DueOn));
        Assert.Equal((1, (DateOnly?)Today), (Progress(strict, 0).Box, Progress(strict, 0).DueOn));
        var items = result.GetProperty("items").EnumerateArray().ToDictionary(i => i.GetProperty("positionId").GetInt32());
        Assert.True(items[lenient].GetProperty("accentSlip").GetBoolean());
        Assert.False(items[lenient].GetProperty("wasCorrect").GetBoolean());
        Assert.False(items[strict].GetProperty("accentSlip").GetBoolean());
        Assert.Equal(0, result.GetProperty("coinsAwarded").GetInt32());
        Assert.True(Read(db => db.ItemReviewEvents.Any(e => e.PlanPositionId == lenient && e.AccentSlip)));
        Assert.False(Read(db => db.ItemReviewEvents.Any(e => e.PlanPositionId == strict && e.AccentSlip)));
    }
}
