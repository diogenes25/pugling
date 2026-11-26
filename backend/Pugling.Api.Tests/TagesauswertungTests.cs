using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-733: the supervisor's evaluation per child, day and subject, the profile over 7 and 28 days and the problem words.
/// Today is Monday 2026-11-30 (frozen); the plan runs since Thursday the 26th with a daily duty, missed on Thursday and met
/// on Friday; Saturday is a pause, Sunday is free. Past answers are seeded in the database (the API only plays today). Each
/// case names the change that turns it red.
/// </summary>
public class TagesauswertungTests : IClassFixture<LerntageWebAppFactory>
{
    private static readonly DateOnly Today = new(2026, 11, 30);
    private static readonly DateOnly Sunday = Today.AddDays(-1);
    private static readonly DateOnly Saturday = Today.AddDays(-2);
    private static readonly DateOnly Friday = Today.AddDays(-3);
    private static readonly DateOnly Thursday = Today.AddDays(-4);

    private const string ChildPin = "7531";

    private readonly LerntageWebAppFactory factory;

    public TagesauswertungTests(LerntageWebAppFactory factory)
    {
        this.factory = factory;
        factory.Clock.FreezeAt(new DateTimeOffset(2026, 11, 30, 12, 0, 0, TimeSpan.Zero));
    }

    private sealed record Setup(HttpClient Father, int ChildId, int PlanId, int PositionId, int SubjectId,
        (int Id, int VocabularyId) A, (int Id, int VocabularyId) B)
    {
        public string Url => $"/api/v1/supervisor/children/{ChildId}/evaluations";
    }

    // A fresh child with a daily duty since Thursday (missed Thursday, met Friday), a pause on Saturday and the answers of the
    // class summary: Friday two right (one into the top box), four wrong (a typed miss from before B-732, accent from box 3,
    // typo from box 1, a self-assessed miss) next to rows without a verdict (an old learn card, a shown Kennenlernen card, an
    // eased recall), one new word, 300 practice seconds; Sunday one right answer; today one wrong word.
    private async Task<Setup> SetupAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Auswertung-Kind"), pin = ChildPin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText,
            childId: childId, cadence: GoalCadence.Daily);
        // A weekly duty in the same plan: on a weekday its period is not closed, so it never counts in a day's duties.
        var weeklyExerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (weeklyPlanId, weeklyPositionId) = TestApi.SeedLeitnerPosition(factory, weeklyExerciseId, (int)TestStage.FreeText,
            childId: childId, cadence: GoalCadence.Weekly);
        Write(db =>
        {
            db.PlanPositions.Single(p => p.Id == weeklyPositionId).StudyPlanId = planId;
            db.PlanPositions.Single(p => p.Id == weeklyPositionId).CreatedAt = Thursday.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            db.StudyPlans.Remove(db.StudyPlans.Single(p => p.Id == weeklyPlanId));
        });
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/pauses",
            new { from = Saturday, until = Saturday })).EnsureSuccessStatusCode();

        var (subjectId, a, b) = Read(db =>
        {
            var subject = db.Exercises.Where(e => e.Id == exerciseId).Select(e => e.SeriesUnit!.Series!.SubjectId).Single();
            var items = db.ExerciseItems.Where(i => i.ExerciseId == exerciseId).OrderBy(i => i.OrderIndex)
                .Select(i => new { i.Id, i.VocabularyId }).ToList();
            return (subject!.Value, (items[0].Id, items[0].VocabularyId), (items[1].Id, items[1].VocabularyId));
        });
        Write(db =>
        {
            var plan = db.StudyPlans.Single(p => p.Id == planId);
            plan.StartDate = Thursday;
            plan.EndDate = Today.AddDays(30);
            plan.SubjectId = subjectId;
            db.PlanPositions.Single(p => p.Id == positionId).CreatedAt = Thursday.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            Test(db, planId, positionId, Thursday, passed: false);
            Test(db, planId, positionId, Friday, passed: true);

            // Before the first row with a box (09:00) the rows are from before B-732: a typed miss counts, a learn card not.
            Answer(db, childId, exerciseId, positionId, a, Friday, 8, false, null, null, "hallu");
            Answer(db, childId, exerciseId, positionId, b, Friday, 8, true, null, null, "goodbye", TestStage.LearnCard);
            Answer(db, childId, exerciseId, positionId, a, Friday, 9, true, 4, null, "hallo");
            Answer(db, childId, exerciseId, positionId, b, Friday, 9, true, 1, null, "goodbye");
            Answer(db, childId, exerciseId, positionId, a, Friday, 10, false, 3, AnswerErrorKind.Accent, "halo");
            Answer(db, childId, exerciseId, positionId, b, Friday, 10, false, 1, AnswerErrorKind.Typo, "godbye");
            Answer(db, childId, exerciseId, positionId, a, Friday, 11, false, 1, null, null, TestStage.SelfAssess);
            // Rows without a verdict, as the practice writes them (B-96, B-643): a shown Kennenlernen card and an eased recall.
            Answer(db, childId, exerciseId, positionId, a, Friday, 12, false, null, null, null, TestStage.ShowBoth);
            Answer(db, childId, exerciseId, positionId, a, Friday, 13, false, null, null, "hal");
            Answer(db, childId, exerciseId, positionId, b, Sunday, 10, true, 2, null, "goodbye");
            Answer(db, childId, exerciseId, positionId, a, Today, 11, false, 1, AnswerErrorKind.WrongWord, "hi");

            db.ItemProgress.Add(new ItemProgress
            {
                ChildId = childId,
                ItemId = a.Item1,
                ExerciseId = exerciseId,
                VocabularyId = a.Item2,
                Box = 1,
                MasteryPercent = 0,
                // Scored first on Thursday: the cross-plan stamp differs from the position's introduction on purpose.
                IntroducedAt = Thursday,
            });
            db.ItemProgress.Add(new ItemProgress
            {
                ChildId = childId,
                ItemId = b.Item1,
                ExerciseId = exerciseId,
                VocabularyId = b.Item2,
                Box = 2,
                MasteryPercent = 20,
                IntroducedAt = Thursday.AddDays(-30),
            });
            // New words come from the introduction at the position: word A on Friday, everything else long before.
            foreach (var p in db.PositionItemProgress.Where(p => p.PlanPositionId == positionId || p.PlanPositionId == weeklyPositionId))
                p.IntroducedAt = p.PlanPositionId == positionId && p.ItemIndex == 0 ? Friday : Thursday.AddDays(-30);
            db.PracticeSessions.Add(new PracticeSession
            {
                StudyPlanId = planId,
                PlanPositionId = positionId,
                Day = Friday,
                ActiveSeconds = 300,
                StartedAt = Friday.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc),
            });
        });
        return new Setup(father, childId, planId, positionId, subjectId, a, b);
    }

    private static void Test(PuglingDbContext db, int planId, int positionId, DateOnly day, bool passed)
    {
        var at = day.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc);
        db.TestAttempts.Add(new TestAttempt
        {
            StudyPlanId = planId,
            PlanPositionId = positionId,
            Day = day,
            StageValue = (int)TestStage.FreeText,
            Graded = true,
            StartedAt = at,
            CompletedAt = at,
            TotalItems = 2,
            CorrectItems = passed ? 2 : 0,
            ScorePercent = passed ? 100 : 0,
            Passed = passed,
        });
    }

    private static void Answer(PuglingDbContext db, int childId, int exerciseId, int positionId, (int Id, int VocabularyId) item,
        DateOnly day, int hourUtc, bool correct, int? boxBefore, AnswerErrorKind? kind, string? given,
        TestStage stage = TestStage.FreeText) =>
        db.ItemReviewEvents.Add(new ItemReviewEvent
        {
            ChildId = childId,
            ItemId = item.Id,
            ExerciseId = exerciseId,
            VocabularyId = item.VocabularyId,
            PlanPositionId = positionId,
            Source = ItemReviewSource.Practice,
            StageValue = (int)stage,
            GivenAnswer = given,
            WasCorrect = correct,
            BoxBefore = boxBefore,
            ErrorKind = kind,
            At = day.ToDateTime(new TimeOnly(hourUtc, 0), DateTimeKind.Utc),
        });

    private void Write(Action<PuglingDbContext> change)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        change(db);
        db.SaveChanges();
    }

    private T Read<T>(Func<PuglingDbContext, T> read)
    {
        using var scope = factory.Services.CreateScope();
        return read(scope.ServiceProvider.GetRequiredService<PuglingDbContext>());
    }

    private static async Task<JsonElement> OkJsonAsync(HttpResponseMessage res)
    {
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    // The figures of a day row as one comparable tuple.
    private static (int New, int Cons, int Fell, int Right, int Wrong, int Secs, string Kinds, int Met, int Missed, bool Live)
        Figures(JsonElement row) =>
        (row.GetProperty("newWords").GetInt32(), row.GetProperty("consolidatedWords").GetInt32(),
         row.GetProperty("fellBackWords").GetInt32(), row.GetProperty("correctAnswers").GetInt32(),
         row.GetProperty("wrongAnswers").GetInt32(), row.GetProperty("practicedSeconds").GetInt32(),
         Kinds(row.GetProperty("errorKinds")), row.GetProperty("dutiesMet").GetInt32(),
         row.GetProperty("dutiesMissed").GetInt32(), row.GetProperty("live").GetBoolean());

    private static string Kinds(JsonElement kinds) =>
        string.Join(",", kinds.EnumerateObject().OrderBy(k => k.Name, StringComparer.Ordinal)
            .Select(k => $"{k.Name}:{k.Value.GetInt32()}"));

    private static Dictionary<DateOnly, JsonElement> ByDay(JsonElement list) =>
        list.EnumerateArray().ToDictionary(r => DateOnly.Parse(r.GetProperty("day").GetString()!));

    // ---- AK 1, 2, 7 (gap): the rows of a day ----

    /// <summary>
    /// AK 2 and the gap rule: Friday's row follows the counting rules (new from the introduction day, consolidated = right
    /// into the top box, fell back = wrong from above box 1, error kinds only of typed misses, seconds, the met duty);
    /// Thursday has a row with only the missed duty; Saturday (pause, nothing practised) has none; Sunday (free, practised)
    /// has its answers and no duty; today is live. Red if a counting rule changes, if a free day owes a duty or gets a row
    /// without practice, or if today is stored - and (review of B-733) if a row without a verdict counts as an answer, a typed
    /// miss from before B-732 does not, or "new" is read from the cross-plan stamp instead of the position's introduction.
    /// </summary>
    [Fact]
    public async Task Tageszeilen_folgen_den_Zaehlregeln_und_freie_Tage_ohne_Uebung_sind_Luecken()
    {
        var s = await SetupAsync();
        var days = ByDay(await OkJsonAsync(await s.Father.GetAsync(s.Url)));

        Assert.Equal([Today, Sunday, Friday, Thursday], days.Keys.ToArray());
        Assert.Equal((1, 1, 1, 2, 4, 300, "Accent:1,Typo:1", 1, 0, false), Figures(days[Friday]));
        Assert.Equal((0, 0, 0, 0, 0, 0, "", 0, 1, false), Figures(days[Thursday]));
        Assert.Equal((0, 0, 0, 1, 0, 0, "", 0, 0, false), Figures(days[Sunday]));
        Assert.Equal((0, 0, 0, 0, 1, 0, "WrongWord:1", 0, 1, true), Figures(days[Today]));
        Assert.All(days.Values, r => Assert.Equal(s.SubjectId, r.GetProperty("subjectId").GetInt32()));
        Assert.False(days.ContainsKey(Saturday));
        Assert.False(Read(db => db.EvaluatedDays.Any(d => d.ChildId == s.ChildId && d.Day == Today)));
        Assert.True(Read(db => db.EvaluatedDays.Any(d => d.ChildId == s.ChildId && d.Day == Saturday)));
    }

    /// <summary>
    /// AK 1 and 3: evaluating twice adds no row and changes none, and a closed day stays as stored when its answers change
    /// afterwards. Red if a read recomputes a closed day from the answers, or if a second evaluation adds rows.
    /// </summary>
    [Fact]
    public async Task Abgeschlossene_Tage_werden_einmal_gespeichert_und_nie_neu_gerechnet()
    {
        var s = await SetupAsync();
        var first = ByDay(await OkJsonAsync(await s.Father.GetAsync(s.Url)));
        var stored = Read(db => db.DailyEvaluations.Count(d => d.ChildId == s.ChildId));
        Assert.Equal(3, stored);
        Assert.Equal(28, Read(db => db.EvaluatedDays.Count(d => d.ChildId == s.ChildId)));

        // Friday's answers change after the day was evaluated: two more misses, the practice time is gone.
        Write(db =>
        {
            var exerciseId = db.ItemReviewEvents.First(e => e.ChildId == s.ChildId).ExerciseId;
            Answer(db, s.ChildId, exerciseId, s.PositionId, s.B, Friday, 12, false, 2, AnswerErrorKind.WrongWord, "bye");
            Answer(db, s.ChildId, exerciseId, s.PositionId, s.B, Friday, 13, false, 2, AnswerErrorKind.WrongWord, "bye");
            db.PracticeSessions.RemoveRange(db.PracticeSessions.Where(p => p.PlanPositionId == s.PositionId));
        });
        var second = ByDay(await OkJsonAsync(await s.Father.GetAsync(s.Url)));

        Assert.Equal(Figures(first[Friday]), Figures(second[Friday]));
        Assert.Equal(stored, Read(db => db.DailyEvaluations.Count(d => d.ChildId == s.ChildId)));
    }

    /// <summary>
    /// AK 1: the unique indices hold one row per child, day and subject - also for the subject-less row, which a single
    /// unique over the nullable column would not (SQLite treats NULLs as distinct). Red if either index is missing.
    /// </summary>
    [Fact]
    public async Task Eindeutiger_Index_je_Kind_Tag_und_Fach_auch_ohne_Fach()
    {
        var s = await SetupAsync();
        Write(db => db.DailyEvaluations.Add(new DailyEvaluation { ChildId = s.ChildId, Day = Friday.AddDays(-10) }));
        Assert.Throws<DbUpdateException>(() =>
            Write(db => db.DailyEvaluations.Add(new DailyEvaluation { ChildId = s.ChildId, Day = Friday.AddDays(-10) })));
        Write(db => db.DailyEvaluations.Add(new DailyEvaluation
        { ChildId = s.ChildId, Day = Friday.AddDays(-11), SubjectId = s.SubjectId }));
        Assert.Throws<DbUpdateException>(() => Write(db => db.DailyEvaluations.Add(new DailyEvaluation
        { ChildId = s.ChildId, Day = Friday.AddDays(-11), SubjectId = s.SubjectId })));
    }

    // ---- AK 4: the profile ----

    /// <summary>
    /// AK 4: the profile comes from the stored rows - a stored figure changed in the database shows up, the answers do not
    /// - over 7 and 28 days: a row ten days back counts only in the long window. Red if the profile reads the answers, or a
    /// window is missing or cut wrong.
    /// </summary>
    [Fact]
    public async Task Profil_rechnet_aus_den_gespeicherten_Tagen_ueber_7_und_28_Tage()
    {
        var s = await SetupAsync();
        await OkJsonAsync(await s.Father.GetAsync(s.Url));
        Write(db =>
        {
            db.DailyEvaluations.Single(d => d.ChildId == s.ChildId && d.Day == Friday).CorrectAnswers = 40;
            db.DailyEvaluations.Add(new DailyEvaluation
            {
                ChildId = s.ChildId,
                Day = Today.AddDays(-10),
                SubjectId = s.SubjectId,
                CorrectAnswers = 10,
                ConsolidatedWords = 4,
            });
        });

        var profile = await OkJsonAsync(await s.Father.GetAsync($"{s.Url}/profile"));
        Assert.Equal(Sunday, DateOnly.Parse(profile.GetProperty("until").GetString()!));
        var subject = Assert.Single(profile.GetProperty("subjects").EnumerateArray());
        Assert.Equal(s.SubjectId, subject.GetProperty("subjectId").GetInt32());

        static (int, int, int, int, int, double, string, int, int, int?) W(JsonElement w) =>
            (w.GetProperty("learningDays").GetInt32(), w.GetProperty("correctAnswers").GetInt32(),
             w.GetProperty("wrongAnswers").GetInt32(), w.GetProperty("masteryPercent").GetInt32(),
             w.GetProperty("consolidatedWords").GetInt32(), w.GetProperty("tempoPerLearningDay").GetDouble(),
             string.Join(",", w.GetProperty("topErrorKinds").EnumerateArray()
                 .Select(k => $"{k.GetProperty("kind").GetString()}:{k.GetProperty("count").GetInt32()}")),
             w.GetProperty("dutyDays").GetInt32(), w.GetProperty("dutyDaysMet").GetInt32(),
             w.GetProperty("resiliencePercent").ValueKind == JsonValueKind.Null ? null : w.GetProperty("resiliencePercent").GetInt32());

        Assert.Equal((2, 41, 4, 91, 1, 0.5, "Accent:1,Typo:1", 2, 1, 50), W(subject.GetProperty("last7Days")));
        Assert.Equal((3, 51, 4, 93, 5, 1.7, "Accent:1,Typo:1", 2, 1, 50), W(subject.GetProperty("last28Days")));
    }

    /// <summary>
    /// Subject rule: an answer counts for the subject of its exercise's series, a duty for the subject of its plan. With
    /// the plan moved to another subject, Sunday's answers stay with the series subject and Thursday's missed duty moves.
    /// Red if the plan's subject wins over the series for an answer, or a duty is filed under the exercise.
    /// </summary>
    [Fact]
    public async Task Antwort_zaehlt_fuers_Fach_der_Reihe_Pflicht_fuers_Fach_des_Plans()
    {
        var s = await SetupAsync();
        var other = Read(db => db.Subjects.Count()) + 1000;
        Write(db =>
        {
            var subject = new Subject { Name = $"Anderes Fach {other}" };
            db.Subjects.Add(subject);
            db.SaveChanges();
            db.StudyPlans.Single(p => p.Id == s.PlanId).SubjectId = subject.Id;
        });
        var planSubject = Read(db => db.StudyPlans.Single(p => p.Id == s.PlanId).SubjectId!.Value);

        var rows = (await OkJsonAsync(await s.Father.GetAsync(s.Url))).EnumerateArray()
            .Select(r => (Day: DateOnly.Parse(r.GetProperty("day").GetString()!), Subject: r.GetProperty("subjectId").GetInt32(),
                Right: r.GetProperty("correctAnswers").GetInt32(), Missed: r.GetProperty("dutiesMissed").GetInt32()))
            .Where(r => r.Day != Today).OrderBy(r => r.Day).ThenBy(r => r.Subject == planSubject).ToList();

        // Friday splits: its answers under the series, its met duty under the plan.
        Assert.Equal([(Thursday, planSubject, 0, 1), (Friday, s.SubjectId, 2, 0), (Friday, planSubject, 0, 0), (Sunday, s.SubjectId, 1, 0)],
            rows);
    }

    /// <summary>
    /// Review of B-733: a Kennenlernen card (ShowBoth, B-96) played through the student API writes a history row without a
    /// verdict; it is neither a wrong answer of the day nor a problem word. Red if the evaluation counts every history row.
    /// </summary>
    [Fact]
    public async Task Kennenlernkarte_ueber_die_Sohn_API_ist_keine_falsche_Antwort()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Kennenlern-Kind"), pin = ChildPin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.ShowBoth, childId: childId);
        var child = await TestApi.ChildAsync(factory, childId, ChildPin);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, wasKnown: false)).EnsureSuccessStatusCode();
        (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 1, wasKnown: true)).EnsureSuccessStatusCode();
        // The setup holds: the practice wrote history rows for the shown cards.
        Assert.True(Read(db => db.ItemReviewEvents.Count(e => e.ChildId == childId)) >= 2);

        var url = $"/api/v1/supervisor/children/{childId}/evaluations";
        var today = (await OkJsonAsync(await father.GetAsync(url))).EnumerateArray()
            .Where(r => r.GetProperty("live").GetBoolean()).ToList();
        Assert.All(today, r => Assert.Equal((0, 0), (r.GetProperty("correctAnswers").GetInt32(), r.GetProperty("wrongAnswers").GetInt32())));
        Assert.Equal(0, (await OkJsonAsync(await father.GetAsync($"{url}/problem-words"))).GetArrayLength());
    }

    /// <summary>
    /// A daily goal counts only on a day it was owed: a plan switched on on Friday (running since Thursday) owed nothing on
    /// Thursday, so Thursday - not practised - is a gap, not a missed duty. Red if the day counts daily goals that were not
    /// owed (<c>OwesDuty</c>).
    /// </summary>
    [Fact]
    public async Task Nicht_geschuldete_Tagespflicht_zaehlt_nicht_als_verpasst()
    {
        var s = await SetupAsync();
        Write(db => db.StudyPlans.Single(p => p.Id == s.PlanId).ActivatedAt = Friday);

        var days = ByDay(await OkJsonAsync(await s.Father.GetAsync(s.Url)));

        Assert.False(days.ContainsKey(Thursday));
        Assert.Equal(1, days[Friday].GetProperty("dutiesMet").GetInt32());
    }

    // ---- problem words ----

    /// <summary>
    /// AK 7 (problem words): a word got wrong in the period appears with its error kinds, its last wrong typed answer (a
    /// later self-assessed miss has none, a shown card or eased recall is no miss) and its current box; most misses first. Red if the kind or the answer is missing,
    /// or if the order is not by misses.
    /// </summary>
    [Fact]
    public async Task Problemwoerter_tragen_Fehlerart_letzte_Antwort_und_Fach()
    {
        var s = await SetupAsync();
        var words = (await OkJsonAsync(await s.Father.GetAsync($"{s.Url}/problem-words?from={Thursday:yyyy-MM-dd}&to={Sunday:yyyy-MM-dd}")))
            .EnumerateArray().ToList();

        Assert.Equal([s.A.VocabularyId, s.B.VocabularyId], words.Select(w => w.GetProperty("vocabularyId").GetInt32()).ToArray());
        static (int, int, string, string?, int, int) P(JsonElement w) =>
            (w.GetProperty("wrongAnswers").GetInt32(), w.GetProperty("correctAnswers").GetInt32(),
             Kinds(w.GetProperty("errorKinds")), w.GetProperty("lastWrongAnswer").GetString(),
             w.GetProperty("box").GetInt32(), w.GetProperty("masteryPercent").GetInt32());
        Assert.Equal((3, 1, "Accent:1", "halo", 1, 0), P(words[0]));
        Assert.Equal((1, 2, "Typo:1", "godbye", 2, 20), P(words[1]));
        Assert.Equal(s.SubjectId, words[0].GetProperty("subjectId").GetInt32());
        Assert.False(string.IsNullOrEmpty(words[0].GetProperty("front").GetString()));
    }

    // ---- AK 5, 6: the gates ----

    /// <summary>
    /// AK 5 and 6: the three paths take the supervisor claim and ownership - a creator-only account gets 403, a foreign
    /// father 404, the child itself 403 - and a range ending before it starts is 400 validation_error. Red if a gate or the
    /// check is missing.
    /// </summary>
    [Fact]
    public async Task Pfade_pruefen_Rolle_Eigentum_und_Zeitraum()
    {
        var s = await SetupAsync();
        var paths = new[] { s.Url, $"{s.Url}/profile", $"{s.Url}/problem-words" };
        var teacher = await TestApi.AdultAsync(factory, id: 2, pin: "9999");
        var stranger = await TestApi.AdultAsync(factory, id: 3, pin: "0001");
        var child = await TestApi.ChildAsync(factory, s.ChildId, ChildPin);
        foreach (var path in paths)
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await teacher.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await child.GetAsync(path)).StatusCode);
        }

        foreach (var path in new[] { s.Url, $"{s.Url}/problem-words" })
        {
            var reversed = await s.Father.GetAsync($"{path}?from={Friday:yyyy-MM-dd}&to={Thursday:yyyy-MM-dd}");
            Assert.Equal(HttpStatusCode.BadRequest, reversed.StatusCode);
            Assert.Equal("validation_error",
                (await reversed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
    }
}
