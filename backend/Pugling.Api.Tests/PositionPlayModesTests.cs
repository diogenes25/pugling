using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Server-driven playback modes (Info / Lern / Klausur): frozen order + cursor. Checks the
/// one-at-a-time delivery (<c>/next</c>), the "next card" feedback included in
/// <c>/review</c>, the feedback-free Info mode, and the strictly server-driven class-test flow.
/// </summary>
public class PositionPlayModesTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private static readonly (string, string)[] ThreeWords = [("a", "1"), ("b", "2"), ("c", "3")];

    // ---- Learn mode: server cursor + "next card" ---- ----

    [Fact]
    public async Task LernModus_LiefertKartenEinzelnUeberCursor_UndSchliesstAbMitDone()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);

        var start = await (await child.PostAsJsonAsync(baseUrl, new { mode = "Lern" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Lern", start.GetProperty("mode").GetString());
        Assert.Equal(3, start.GetProperty("total").GetInt32());
        var sessionId = start.GetProperty("id").GetInt32();

        // The first card comes server-driven through /next (not as a batch).
        var next = await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{sessionId}/next");
        JsonAssert.False(next, "done");
        Assert.Equal(0, next.GetProperty("cursor").GetInt32());
        var answers = new[] { "1", "2", "3" };
        var card = next.GetProperty("card");

        // The whole run through /review - every answer carries the next card or the completion signal.
        for (var i = 0; i < 3; i++)
        {
            var idx = card.GetProperty("itemIndex").GetInt32();
            var outcome = await (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, idx, givenAnswer: answers[idx]))
                .Content.ReadFromJsonAsync<JsonElement>();
            JsonAssert.True(outcome, "wasCorrect");
            var done = outcome.GetProperty("done").GetBoolean();
            if (i < 2)
            {
                Assert.False(done);
                Assert.Equal(JsonValueKind.Object, outcome.GetProperty("next").ValueKind); // the next card is included
                card = outcome.GetProperty("next");
            }
            else
            {
                Assert.True(done); // last card → the run is over
                Assert.Equal(JsonValueKind.Null, outcome.GetProperty("next").ValueKind);
            }
        }

        // The cursor is at the end; /next reports done.
        var end = await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{sessionId}/next");
        JsonAssert.True(end, "done");
        Assert.Equal(JsonValueKind.Null, end.GetProperty("card").ValueKind);
    }

    // ---- Info mode: free practice, no feedback ---- ----

    [Fact]
    public async Task InfoModus_LiefertAlleKartenAlsBatch_UndSchreibtKeinFeedback()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);

        var start = await (await child.PostAsJsonAsync(baseUrl, new { mode = "Info" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Info", start.GetProperty("mode").GetString());
        var sessionId = start.GetProperty("id").GetInt32();

        // All cards can be fetched in one go.
        var cards = await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{sessionId}/cards");
        Assert.Equal(3, cards.GetArrayLength());

        // In info mode /review writes nothing (204) - no progress, no points.
        var res = await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "1");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.Empty(db.PositionItemProgress.Where(p => p.PlanPositionId == positionId));
        Assert.Empty(db.ReviewEvents.Where(r => r.PracticeSessionId == sessionId));
    }

    [Fact]
    public async Task InfoModus_ErfuelltDasTagesziel_Nicht()
    {
        var father = await TestApi.FatherAsync(_factory);
        // A pure content exercise (reading comprehension) → the goal is "done" as soon as a real learn session exists.
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "Info-Fach" }));
        var seriesId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Info-Reihe"), subjectId }));
        var seriesUnitId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "K1" }));
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/reading",
            new { title = "Text", orderIndex = 1, rewardPoints = 5, config = new { text = "Ein kurzer Text.", questions = Array.Empty<object>() } }));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText, useLeitner: false);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);

        // An info session with real activity (a heartbeat) → must NOT fulfill the daily goal.
        // Together with LernModus_InhaltsuebungOhneInhalt_VerweilteRunde_ErfuelltDieTagespflicht it forms a
        // pair: the same (question-less) exercise, the same flow, the same dwell time - only the mode differs.
        // So the `false` here hangs SOLELY on the mode filter and the test really checks the info exclusion.
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { mode = "Info" }));
        await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/heartbeat", new { seconds = 60, active = true });
        await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/end", new { });

        var overview = await child.GetFromJsonAsync<JsonElement>($"/api/v1/student/study-plans/{planId}/overview");
        JsonAssert.False(overview.GetProperty("today"), "dutyDone");
    }

    // ---- Obligation for pure content exercises: a played round instead of mere presence ---- ----

    /// <summary>
    /// Seeds a reading position (<see cref="ExerciseCheckMode.None"/>) with
    /// <paramref name="questions"/> content atoms – the exercise kind whose duty used to be fulfilled by a
    /// single heartbeat.
    /// </summary>
    private async Task<(int planId, int positionId)> SeedReadingPositionAsync(HttpClient father, int questions,
        bool useLeitner = false)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "Lese-Fach" }));
        var seriesId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Lese-Reihe"), subjectId }));
        var seriesUnitId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "K1" }));
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/reading",
            new
            {
                title = "Text",
                orderIndex = 1,
                rewardPoints = 5,
                config = new
                {
                    text = "Ein kurzer Text.",
                    questions = Enumerable.Range(0, questions)
                        .Select(i => new { prompt = $"F{i}", answer = $"A{i}" }),
                },
            }));
        return TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText, useLeitner: useLeitner);
    }

    [Fact]
    public async Task LernModus_ReineInhaltsuebung_BlosseAnwesenheit_ErfuelltDieTagespflichtNicht()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (planId, positionId) = await SeedReadingPositionAsync(father, questions: 2);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);

        // A learn session with real activity but NO card played. That used to fulfill the obligation and
        // trigger the goal points: open a round, walk away, collect coins.
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { mode = "Lern" }));
        await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/heartbeat", new { seconds = 60, active = true });
        await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/end", new { });

        var overview = await child.GetFromJsonAsync<JsonElement>($"/api/v1/student/study-plans/{planId}/overview");
        JsonAssert.False(overview.GetProperty("today"), "dutyDone");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.Empty(db.PositionGoalRewards.Where(r => r.PlanPositionId == positionId));
    }

    [Fact]
    public async Task LernModus_ReineInhaltsuebung_GespielteRunde_ErfuelltDieTagespflicht()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (planId, positionId) = await SeedReadingPositionAsync(father, questions: 2);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);

        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { mode = "Lern" }));
        // Play both cards (the cursor advances per answered card, independent of Leitner).
        for (var i = 0; i < 2; i++)
            await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, i, givenAnswer: $"A{i}");
        await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/end", new { });

        var overview = await child.GetFromJsonAsync<JsonElement>($"/api/v1/student/study-plans/{planId}/overview");
        JsonAssert.True(overview.GetProperty("today"), "dutyDone");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.Equal(1, db.PositionGoalRewards.Count(r => r.PlanPositionId == positionId));
    }

    /// <summary>
    /// An exercise WITHOUT any content atoms (a text without questions, an essay) always freezes an empty
    /// order – there is no cursor that could prove anything. Opening and closing a round must therefore not
    /// discharge the duty, otherwise the presence rule above would have a hole exactly where it cannot be
    /// measured.
    /// </summary>
    [Fact]
    public async Task LernModus_InhaltsuebungOhneInhalt_OeffnenUndSchliessen_ErfuelltDieTagespflichtNicht()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (planId, positionId) = await SeedReadingPositionAsync(father, questions: 0);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);

        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { mode = "Lern" }));
        await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/end", new { });

        var overview = await child.GetFromJsonAsync<JsonElement>($"/api/v1/student/study-plans/{planId}/overview");
        JsonAssert.False(overview.GetProperty("today"), "dutyDone");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.Empty(db.PositionGoalRewards.Where(r => r.PlanPositionId == positionId));
    }

    /// <summary>
    /// The counterpart: a contentless exercise stays fulfillable, otherwise its duty would be an automatic
    /// <see cref="PlanPosition.PenaltyCoins"/> fine. Dwelling on the text and deliberately closing the round
    /// is the only evidence that exists there.
    /// </summary>
    [Fact]
    public async Task LernModus_InhaltsuebungOhneInhalt_VerweilteRunde_ErfuelltDieTagespflicht()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (planId, positionId) = await SeedReadingPositionAsync(father, questions: 0);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);

        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { mode = "Lern" }));
        await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/heartbeat", new { seconds = 60, active = true });
        await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/end", new { });

        var overview = await child.GetFromJsonAsync<JsonElement>($"/api/v1/student/study-plans/{planId}/overview");
        JsonAssert.True(overview.GetProperty("today"), "dutyDone");
    }

    /// <summary>
    /// The other cause of an empty order: the position HAS a pool, Leitner simply scheduled everything for
    /// later. Then there really was nothing to play and the duty stays met – the distinction the two tests
    /// above hang on.
    /// </summary>
    [Fact]
    public async Task LernModus_PoolVorhandenAberNichtsFaellig_ErfuelltDieTagespflicht()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (planId, positionId) = await SeedReadingPositionAsync(father, questions: 2, useLeitner: true);
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        using (var scope = _factory.Services.CreateScope())
        {
            // Both contents already boxed up and due tomorrow - today's learn session comes out empty.
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            for (var i = 0; i < 2; i++)
                db.PositionItemProgress.Add(new PositionItemProgress
                {
                    PlanPositionId = positionId,
                    ItemIndex = i,
                    Box = 2,
                    DueOn = tomorrow,
                    IntroducedAt = DateOnly.FromDateTime(DateTime.UtcNow),
                });
            db.SaveChanges();
        }

        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);
        var start = await (await child.PostAsJsonAsync(baseUrl, new { mode = "Lern" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, start.GetProperty("total").GetInt32()); // nothing due, so nothing frozen
        await child.PostAsJsonAsync($"{baseUrl}/{start.GetProperty("id").GetInt32()}/end", new { });

        var overview = await child.GetFromJsonAsync<JsonElement>($"/api/v1/student/study-plans/{planId}/overview");
        JsonAssert.True(overview.GetProperty("today"), "dutyDone");
    }

    // ---- Class-test mode: strictly server-driven ---- ----

    [Fact]
    public async Task KlausurModus_StartOhneAufgaben_FragenEinzelnOhneKorrektheit_AbschlussMitScorecard()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        // The start returns ONLY metadata - no tasks in bulk (strictly server-driven).
        var start = await (await child.PostAsJsonAsync(testsUrl, new { })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, start.GetProperty("totalItems").GetInt32());
        Assert.False(start.TryGetProperty("items", out _)); // no tasks up front
        var attemptId = start.GetProperty("attemptId").GetInt32();

        // Fetch the questions one by one and answer them - the answer does NOT reveal whether it was correct.
        var answersByPrompt = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2", ["c"] = "wrong" };
        for (var i = 0; i < 3; i++)
        {
            var next = await child.GetFromJsonAsync<JsonElement>($"{testsUrl}/{attemptId}/next");
            JsonAssert.False(next, "done");
            var prompt = next.GetProperty("item").GetProperty("prompt").GetString()!;
            Assert.Equal(JsonValueKind.Null, next.GetProperty("item").GetProperty("reveal").ValueKind); // typed: no solution

            var ack = await (await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/answer",
                new { givenAnswer = answersByPrompt[prompt] })).Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(ack.TryGetProperty("wasCorrect", out _)); // no feedback per question
        }

        // After the last question: /next is at the end.
        var end = await child.GetFromJsonAsync<JsonElement>($"{testsUrl}/{attemptId}/next");
        JsonAssert.True(end, "done");

        // Submission: only here does the evaluation arrive (2 out of 3 correct).
        var submit = await (await child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, submit.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, submit.GetProperty("correctItems").GetInt32());
    }

    // ---- Order strategies ---- ----

    [Fact]
    public async Task Strategie_Serial_SpieltStrengNachIndex()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, [("a", "1"), ("b", "2"), ("c", "3"), ("d", "4"), ("e", "5")]);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText,
            orderStrategy: PracticeOrder.Serial);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);

        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        var cards = await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{sessionId}/cards");
        var order = cards.EnumerateArray().Select(c => c.GetProperty("itemIndex").GetInt32()).ToList();
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, order);
    }

    // ---- Class test: recorded only on submission (no leak/double count through abort/repeat) ---- ----

    /// <summary>
    /// A left class test writes no learning progress until submitted – and re-entering does <b>not</b> start a
    /// fresh attempt: the child resumes the running one at its cursor. That is what makes the attempt cap
    /// fair, because otherwise an accidental reload would burn one of the few attempts.
    /// </summary>
    [Fact]
    public async Task KlausurModus_VerlassenerVersuch_SchreibtKeinenLernstand_UndWirdFortgesetzt()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var ans = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2", ["c"] = "3" };

        // Attempt 1: answer one question, then LEAVE (no submit).
        var a1 = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
        var first = await child.GetFromJsonAsync<JsonElement>($"{testsUrl}/{a1}/next");
        await child.PostAsJsonAsync($"{testsUrl}/{a1}/answer",
            new { givenAnswer = ans[first.GetProperty("item").GetProperty("prompt").GetString()!] });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            // The abandoned attempt must NOT have changed the cross-plan learning state.
            Assert.Empty(db.ItemReviewEvents.Where(e => e.ExerciseId == exerciseId));
            Assert.Empty(db.ItemProgress.Where(p => p.ExerciseId == exerciseId));
        }

        // Back in: the same attempt, the cursor is on question 2 of 3.
        var again = await (await child.PostAsJsonAsync(testsUrl, new { })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(a1, again.GetProperty("attemptId").GetInt32());
        var resumed = await child.GetFromJsonAsync<JsonElement>($"{testsUrl}/{a1}/next");
        Assert.Equal(1, resumed.GetProperty("cursor").GetInt32());

        // Answer the remaining questions and submit.
        for (var i = 1; i < 3; i++)
        {
            var nx = await child.GetFromJsonAsync<JsonElement>($"{testsUrl}/{a1}/next");
            var prompt = nx.GetProperty("item").GetProperty("prompt").GetString()!;
            await child.PostAsJsonAsync($"{testsUrl}/{a1}/answer", new { givenAnswer = ans[prompt] });
        }
        await child.PostAsJsonAsync($"{testsUrl}/{a1}/submit", new { });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            // Exactly ONE record per item (3) - written on submission, not per intermediate answer.
            Assert.Equal(3, db.ItemReviewEvents.Count(e => e.ExerciseId == exerciseId && e.Source == ItemReviewSource.Test));
            // And only ONE attempt, although it was started twice.
            Assert.Equal(1, db.TestAttempts.Count(t => t.PlanPositionId == positionId));
        }
    }

    [Fact]
    public async Task KlausurModus_DritterVersuchDesTages_WirdAbgewiesen_VaterNicht()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        // Consume two attempts: each is submitted so that the resume path does not kick in.
        for (var i = 0; i < 2; i++)
        {
            var id = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(testsUrl, new { }), "attemptId");
            await child.PostAsJsonAsync($"{testsUrl}/{id}/submit", new { });
        }

        // The third start is grade farming and is rejected.
        var third = await child.PostAsJsonAsync(testsUrl, new { });
        Assert.Equal(HttpStatusCode.Conflict, third.StatusCode);
        var problem = await third.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("test_attempts_exhausted", problem.GetProperty("code").GetString());

        // The supervisor is not capped (preview/catch-up).
        var fathersTry = await father.PostAsJsonAsync(testsUrl, new { });
        Assert.Equal(HttpStatusCode.Created, fathersTry.StatusCode);
    }

    /// <summary>
    /// The cap counts per day, not per goal period. On a weekly position the period bound would grant two
    /// attempts for the whole week: two Monday failures would lock the child out of its own weekly duty until
    /// Sunday – and the missed period would then cost it coins.
    /// </summary>
    [Fact]
    public async Task KlausurModus_WochenZiel_DeckelGiltProTag_NichtProWoche()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText,
            cadence: GoalCadence.Weekly);
        // Another day of the SAME week (Mon–Sun) - never today, whichever weekday the run falls on. On a
        // Monday there is no earlier day left in the week, so the neighbour is Tuesday.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var otherDayOfWeek = today.AddDays(today.DayOfWeek == DayOfWeek.Monday ? 1 : -1);
        using (var scope = _factory.Services.CreateScope())
        {
            // Two failed attempts on that day - inside the weekly period the child's duty is settled over.
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            for (var i = 0; i < 2; i++)
                db.TestAttempts.Add(new TestAttempt
                {
                    StudyPlanId = planId,
                    PlanPositionId = positionId,
                    Day = otherDayOfWeek,
                    StageValue = (int)TestStage.FreeText,
                    CompletedAt = DateTime.UtcNow,
                });
            db.SaveChanges();
        }

        var child = await TestApi.ChildAsync(_factory);
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        // Today is a fresh day, so the two attempts are there again.
        for (var i = 0; i < 2; i++)
        {
            var res = await child.PostAsJsonAsync(testsUrl, new { });
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            await child.PostAsJsonAsync(
                $"{testsUrl}/{await TestApi.IdWithKeyAsync(res, "attemptId")}/submit", new { });
        }
        Assert.Equal(HttpStatusCode.Conflict, (await child.PostAsJsonAsync(testsUrl, new { })).StatusCode);
    }

    /// <summary>
    /// A supervisor attempt belongs to a different actor under different rules (free stage, no cap). It must
    /// therefore neither eat the child's attempts nor be handed to the child for continuation – the child
    /// would then be examined at a stage the supervisor picked, with the supervisor's <c>Graded</c> flag.
    /// </summary>
    [Fact]
    public async Task KlausurModus_VorschauDesVaters_KostetDenSohnKeinenVersuch_UndWirdNichtFortgesetzt()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var testsUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        // Two father previews, both left open and both at a stage of his choosing (not ShowBoth: a free
        // display stage cannot be tested at all since B-96, by the father either).
        var fathersIds = new List<int>();
        for (var i = 0; i < 2; i++)
            fathersIds.Add(await TestApi.IdWithKeyAsync(
                await father.PostAsJsonAsync(testsUrl, new { stage = (int)TestStage.SelfAssess }), "attemptId"));

        // The child gets a NEW attempt at ITS scheduled stage, not the father's open one.
        var mine = await (await child.PostAsJsonAsync(testsUrl, new { })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(mine.GetProperty("attemptId").GetInt32(), fathersIds);
        Assert.Equal((int)TestStage.FreeText, mine.GetProperty("stage").GetInt32());
        await child.PostAsJsonAsync($"{testsUrl}/{mine.GetProperty("attemptId").GetInt32()}/submit", new { });

        // And the previews did not consume the day: the child's second attempt still works, the third does not.
        var second = await child.PostAsJsonAsync(testsUrl, new { });
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        await child.PostAsJsonAsync($"{testsUrl}/{await TestApi.IdWithKeyAsync(second, "attemptId")}/submit", new { });
        Assert.Equal(HttpStatusCode.Conflict, (await child.PostAsJsonAsync(testsUrl, new { })).StatusCode);
    }

    // ---- Info mode serves the whole pool (cards not due too), learn mode only the due ones ---- ----

    [Fact]
    public async Task InfoModus_ServiertAuchNichtFaelligeKarten_ImGegensatzZuLern()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, ThreeWords);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);

        // All 3 cards correct in one learn session → box up, DueOn = later (no longer due today).
        var lern = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { mode = "Lern" }));
        foreach (var (idx, a) in new[] { (0, "1"), (1, "2"), (2, "3") })
            (await TestApi.PositionReviewAsync(child, planId, positionId, lern, idx, givenAnswer: a)).EnsureSuccessStatusCode();

        // A new learn session: nothing due any more → empty.
        var lern2 = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { mode = "Lern" }));
        var lernCards = await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{lern2}/cards");
        Assert.Equal(0, lernCards.GetArrayLength());

        // An info session: the whole pool stays playable (free repetition), although nothing is due.
        var info = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { mode = "Info" }));
        var infoCards = await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{info}/cards");
        Assert.Equal(3, infoCards.GetArrayLength());
    }

    [Fact]
    public async Task Strategie_Random_SpieltAlleFaelligenGenauEinmal()
    {
        var father = await TestApi.FatherAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, [("a", "1"), ("b", "2"), ("c", "3"), ("d", "4"), ("e", "5")]);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(_factory, exerciseId, (int)TestStage.FreeText,
            orderStrategy: PracticeOrder.Random);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);

        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        var cards = await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{sessionId}/cards");
        var order = cards.EnumerateArray().Select(c => c.GetProperty("itemIndex").GetInt32()).ToList();
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, order.OrderBy(i => i).ToArray()); // a permutation without loss/duplicates
    }
}
