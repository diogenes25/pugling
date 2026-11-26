using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-732: why a typed answer was wrong - accent, typo, synonym, wrong word - is decided where the answer is graded and
/// stored with it, on the answer history (<see cref="ItemReviewEvent"/>) and on the test row (<see cref="TestItemResult"/>);
/// a right answer, a pick and a self-assessment carry none. One case per write path. The history also keeps the cross-plan
/// box before the answer. Each case names the change that turns it red.
/// </summary>
public class FehlerartTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private sealed record Setup(HttpClient Father, HttpClient Child, int ChildId, int PlanId, int PositionId, int ExerciseId,
        string[] German);

    // ---- The rule itself (AK 2, order) ----

    /// <summary>
    /// The order is the rule: a missing accent is also one letter off, so "cafe" is an accent slip and never a typo. Red when
    /// <see cref="AnswerGrader.SpellingSlip"/> checks the typo first (the first two rows turn Typo).
    /// </summary>
    [Theory]
    [InlineData("café", "cafe", AnswerErrorKind.Accent)]
    [InlineData("école", "ecole", AnswerErrorKind.Accent)]
    [InlineData("forêt", "forêtt", AnswerErrorKind.Typo)]
    [InlineData("height", "heiht", AnswerErrorKind.Typo)]
    [InlineData("Schule", "Haus", null)]
    [InlineData("Schule", "", null)]
    [InlineData("Schule", "Schule", null)]
    public void Rechtschreibart_Akzent_Vor_Tippfehler(string solution, string given, AnswerErrorKind? expected)
    {
        var item = new Services.Shared.ContentItem(0, "?", solution, [solution]);
        Assert.Equal(expected, new AnswerGrader().SpellingSlip(given, item, [item]));
    }

    /// <summary>
    /// AK 2: one distance rule. Over the same data the kind is Typo exactly where <see cref="AnswerGrader.IsMisspelled"/>
    /// holds and the answer is no accent slip - including the neighbouring word, which IsMisspelled refuses. Red when the
    /// classification measures distance on its own (the "racist" row would turn Typo).
    /// </summary>
    [Theory]
    [InlineData("racism", "racist")]
    [InlineData("racism", "racsim")]
    [InlineData("house", "hous")]
    [InlineData("house", "hose")]
    [InlineData("house", "horse")]
    [InlineData("école", "ecole")]
    [InlineData("forêt", "ferêt")]
    public void Tippfehler_Ist_Die_Regel_Von_IsMisspelled(string solution, string given)
    {
        var grader = new AnswerGrader();
        var item = new Services.Shared.ContentItem(0, "?", solution, [solution]);
        IReadOnlyList<Services.Shared.ContentItem> siblings = [item, new Services.Shared.ContentItem(1, "?", "racist", ["racist"])];
        var typo = grader.SpellingSlip(given, item, siblings) == AnswerErrorKind.Typo;
        Assert.Equal(grader.IsMisspelled(given, item, siblings) && !grader.IsAccentSlip(given, item), typo);
    }

    // ---- Practice ----

    /// <summary>
    /// AK 1, 3, 7, 8 in practice: "ecole" is Accent, "forêtt" Typo, the neighbouring word "racist" for "racism" WrongWord,
    /// each still wrong; the history carries the cross-plan box before the answer. Red when the practice review passes no
    /// kind (all null), when the neighbour guard is lost (racist → Typo), or when BoxBefore is not taken (null).
    /// </summary>
    [Fact]
    public async Task Ueben_Speichert_Akzent_Tippfehler_Und_Falsches_Wort()
    {
        var s = await SetupAsync([("école", "Schule"), ("forêt", "Wald"), ("racism", "Rassismus"), ("racist", "rassistisch")],
            TestStage.FreeText, introduced: true);
        SeedCrossPlanBox(s, itemIndex: 0, box: 4);
        var session = await TestApi.StartPositionSessionAsync(s.Child, s.PlanId, s.PositionId);

        foreach (var (index, answer) in new[] { (0, "ecole"), (1, "forêtt"), (2, "racist") })
            JsonAssert.False(await ReviewAsync(s, session, index, answer), "wasCorrect");

        var rows = History(s);
        Assert.Equal(AnswerErrorKind.Accent, rows["ecole"].ErrorKind);
        Assert.Equal(AnswerErrorKind.Typo, rows["forêtt"].ErrorKind);
        Assert.Equal(AnswerErrorKind.WrongWord, rows["racist"].ErrorKind);
        Assert.Equal(4, rows["ecole"].BoxBefore);
        Assert.Equal(1, rows["forêtt"].BoxBefore);
    }

    /// <summary>
    /// AK 4: the learnt word of another store entry with the same meaning is a synonym, not a wrong word. Red when the store
    /// check is gone (WrongWord) or matches on the word instead of the shared meaning.
    /// </summary>
    [Fact]
    public async Task Ueben_Erkennt_Das_Synonym_Aus_Dem_Speicher()
    {
        var s = await SetupAsync([("glimmerfox", "Funkelfuchs"), ("dullhound", "Mattehund")], TestStage.FreeText, introduced: true);
        await TestApi.CreateStoreVocabAsync(s.Father, "shimmervixen", s.German[0]);
        await TestApi.CreateStoreVocabAsync(s.Father, "shimmerhound", TestApi.UniqueName("Anderes"));
        var session = await TestApi.StartPositionSessionAsync(s.Child, s.PlanId, s.PositionId);

        await ReviewAsync(s, session, 0, "shimmervixen");
        await ReviewAsync(s, session, 1, "shimmerhound");

        var rows = History(s);
        Assert.Equal(AnswerErrorKind.Synonym, rows["shimmervixen"].ErrorKind);
        Assert.Equal(AnswerErrorKind.WrongWord, rows["shimmerhound"].ErrorKind);
    }

    /// <summary>
    /// AK 4 past the candidate cap (review finding): fifty-five longer store words contain the typed one, the synonym
    /// itself was entered last. Red when the capped query does not take the closest words first (the synonym falls out
    /// of the fifty and the answer is stored as WrongWord).
    /// </summary>
    [Fact]
    public async Task Synonym_Bleibt_Auch_Hinter_Vielen_Aehnlichen_Woertern()
    {
        var s = await SetupAsync([("glimmerfox", "Funkelfuchs"), ("dullhound", "Mattehund")], TestStage.FreeText, introduced: true);
        var core = "qv" + Guid.NewGuid().ToString("N")[..6];
        for (var i = 0; i < 55; i++)
            await TestApi.CreateStoreVocabAsync(s.Father, $"{core}x{i:D2}", TestApi.UniqueName("Fremd"));
        await TestApi.CreateStoreVocabAsync(s.Father, core, s.German[0]);
        var session = await TestApi.StartPositionSessionAsync(s.Child, s.PlanId, s.PositionId);

        await ReviewAsync(s, session, 0, core);

        Assert.Equal(AnswerErrorKind.Synonym, History(s)[core].ErrorKind);
    }

    /// <summary>
    /// AK 5: a right answer, a pick on the choice stage and a self-assessment carry no kind. Red when the classifier does not
    /// ask whether the card offered options (the pick turns WrongWord) or a right answer gets one.
    /// </summary>
    [Fact]
    public async Task Richtig_Auswahl_Und_Selbsteinschaetzung_Tragen_Keine_Art()
    {
        var typed = await SetupAsync([("école", "Schule"), ("forêt", "Wald")], TestStage.FreeText, introduced: true);
        await ReviewAsync(typed, await TestApi.StartPositionSessionAsync(typed.Child, typed.PlanId, typed.PositionId), 0, "école");
        var right = Assert.Single(History(typed).Values);
        Assert.True(right.WasCorrect);
        Assert.Null(right.ErrorKind);

        var choice = await SetupAsync([("école", "Schule"), ("forêt", "Wald"), ("plage", "Strand")], TestStage.MultipleChoice,
            introduced: true);
        var picked = await ReviewAsync(choice, await TestApi.StartPositionSessionAsync(choice.Child, choice.PlanId, choice.PositionId),
            0, "forêt");
        JsonAssert.False(picked, "wasCorrect");
        Assert.Null(Assert.Single(History(choice).Values).ErrorKind);

        var claim = await SetupAsync([("école", "Schule"), ("forêt", "Wald")], TestStage.SelfAssess, introduced: true);
        var res = await TestApi.PositionReviewAsync(claim.Child, claim.PlanId, claim.PositionId,
            await TestApi.StartPositionSessionAsync(claim.Child, claim.PlanId, claim.PositionId), 0, wasKnown: false);
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
        Assert.Null(Db(db => db.ItemReviewEvents.Single(e => e.PlanPositionId == claim.PositionId).ErrorKind));
    }

    /// <summary>
    /// A wrong solution set carries no kind: the kinds name what went wrong with a word, and "2;5" for {2, 4} is no other
    /// word. Typed stage: red when the classifier loses its <see cref="Services.Shared.ContentItem.IsNumber"/> gate (the row
    /// turns WrongWord). Choice stage: a pick carries none either way - kept so the two stages of a number task are pinned
    /// together, not as a probe of the gate.
    /// </summary>
    [Theory]
    [InlineData((int)QuadraticEquationStage.Direct)]
    [InlineData((int)QuadraticEquationStage.Distractors)]
    public async Task Falsche_Loesungsmenge_Traegt_Keine_Art(int stage)
    {
        var type = new Exercises.QuadraticEquationExerciseType();
        var config = System.Text.Json.JsonSerializer.Serialize(
            new QuadraticEquationConfig { Equations = [new QuadraticEquation(1, -6, 8)] },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var items = type.ItemsOf(config);

        using var scope = factory.Services.CreateScope();
        var classifier = scope.ServiceProvider.GetRequiredService<Services.Shared.AnswerErrorClassifier>();
        var kind = await classifier.ClassifyAsync(type, config, items, items[0], stage, "2;5", wasCorrect: false,
            CancellationToken.None);

        Assert.Null(kind);
    }

    /// <summary>
    /// Learn card path: a recall written with a slip is a typo, a recall given up is a word not known; neither row counts for
    /// mastery, so neither has a box before. Red when the learn card's recall row passes no kind, or a box is taken for it.
    /// </summary>
    [Fact]
    public async Task Lernkarte_Speichert_Die_Art_Des_Abrufs()
    {
        var s = await SetupAsync([("la gare", "der Bahnhof"), ("le train", "der Zug")], TestStage.FreeText, introduced: false,
            lang: "fr");
        var session = await TestApi.IdAsync(await s.Child.PostAsJsonAsync(TestApi.PracticeBase(s.PlanId, s.PositionId),
            new { mode = "Lern" }));

        await PostReviewAsync(s, session, new { itemIndex = 0, gaveUp = true });
        await PostReviewAsync(s, session, new { itemIndex = 1, givenAnswer = "le tren" });

        var rows = Db(db => db.ItemReviewEvents.Where(e => e.PlanPositionId == s.PositionId && e.StageValue == (int)TestStage.LearnCard)
            .OrderBy(e => e.Id).Select(e => new { e.GivenAnswer, e.ErrorKind, e.BoxBefore }).ToList());
        Assert.Equal(2, rows.Count);
        Assert.Equal(AnswerErrorKind.WrongWord, rows[0].ErrorKind);
        Assert.Equal(AnswerErrorKind.Typo, rows[1].ErrorKind);
        Assert.All(rows, r => Assert.Null(r.BoxBefore));
    }

    // ---- Test ----

    /// <summary>
    /// AK 1 on the test, both ways of answering: the step-wise answer and the bulk submission each store Accent on the test
    /// row and hand it to the history; the right answer has none; the box before is the new word's box 1. Red when either
    /// test path writes no kind, or the submission does not pass the row's kind on.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Test_Speichert_Die_Art_An_Testzeile_Und_Verlauf(bool bulk)
    {
        var s = await SetupAsync([("école", "Schule"), ("forêt", "Wald")], TestStage.FreeText, introduced: true);
        var answers = new Dictionary<int, string> { [0] = "ecole", [1] = "forêt" };

        var attemptId = await StartTestAsync(s);
        if (bulk) await SubmitAsync(s, attemptId, answers);
        else
        {
            await AnswerStepwiseAsync(s, attemptId, answers);
            await SubmitAsync(s, attemptId, null);
        }

        var results = Db(db => db.Set<TestItemResult>().Where(r => r.TestAttemptId == attemptId)
            .ToDictionary(r => r.ItemIndex!.Value, r => r.ErrorKind));
        Assert.Equal(AnswerErrorKind.Accent, results[0]);
        Assert.Null(results[1]);
        var rows = History(s);
        Assert.Equal(AnswerErrorKind.Accent, rows["ecole"].ErrorKind);
        Assert.Null(rows["forêt"].ErrorKind);
        Assert.Equal(1, rows["ecole"].BoxBefore);
    }

    /// <summary>
    /// AK 6: the kind is fixed with the answer. The solution changes between the step-wise answer and the submission; the
    /// stored Accent stays on the test row and reaches the history unchanged. Red when the submission classifies again (the
    /// new solution makes "ecole" a WrongWord).
    /// </summary>
    [Fact]
    public async Task Art_Bleibt_Wenn_Die_Loesung_Sich_Danach_Aendert()
    {
        var s = await SetupAsync([("école", "Schule"), ("forêt", "Wald")], TestStage.FreeText, introduced: true);
        var attemptId = await StartTestAsync(s);
        await AnswerStepwiseAsync(s, attemptId, new Dictionary<int, string> { [0] = "ecole", [1] = "forêt" });
        Db(db =>
        {
            var vocabularyId = db.ExerciseItems.Where(i => i.ExerciseId == s.ExerciseId).OrderBy(i => i.OrderIndex).First().VocabularyId;
            db.Vocabularies.Single(v => v.Id == vocabularyId).Word = "collège";
            return db.SaveChanges();
        });

        await SubmitAsync(s, attemptId, null);

        Assert.Equal(AnswerErrorKind.Accent, History(s)["ecole"].ErrorKind);
        Assert.Equal(AnswerErrorKind.Accent, Db(db => db.Set<TestItemResult>()
            .Single(r => r.TestAttemptId == attemptId && r.ItemIndex == 0).ErrorKind));
    }

    /// <summary>
    /// A wrong mention in a set-graded list is typed and wrong: one letter off an open entry is a typo, anything else a
    /// wrong word. Red when the wrong mention is written without a kind, or measured against no entry (Bayren → WrongWord).
    /// </summary>
    [Fact]
    public async Task Fehlnennung_In_Der_Liste_Traegt_Eine_Art()
    {
        var father = await TestApi.AdultAsync(factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Erdkunde-Fehlerart") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/list", new
            {
                title = TestApi.UniqueName("Drei Länder"),
                orderIndex = 1,
                rewardPoints = 5,
                config = new
                {
                    instruction = "Nenne drei Bundesländer.",
                    ordered = false,
                    items = new object[] { new { value = "Bayern" }, new { value = "Hessen" }, new { value = "Berlin" } },
                },
            }));
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, useLeitner: false);
        var child = await TestApi.ChildAsync(factory);
        var url = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(url, new { }), "attemptId");
        foreach (var answer in new[] { "Bayren", "Paris", "Hessen" })
        {
            var next = await child.GetFromJsonAsync<JsonElement>($"{url}/{attemptId}/next");
            (await child.PostAsJsonAsync($"{url}/{attemptId}/answer", new
            {
                itemIndex = next.GetProperty("item").GetProperty("itemIndex").GetInt32(),
                givenAnswer = answer,
            })).EnsureSuccessStatusCode();
        }

        var mentions = Db(db => db.Set<TestItemResult>().Where(r => r.TestAttemptId == attemptId && r.ItemIndex == null)
            .ToDictionary(r => r.GivenAnswer!, r => r.ErrorKind));
        Assert.Equal(AnswerErrorKind.Typo, mentions["Bayren"]);
        Assert.Equal(AnswerErrorKind.WrongWord, mentions["Paris"]);
    }

    // ---- Migration (decision 3) ----

    /// <summary>
    /// The migration adds the three columns empty: an answer recorded before it keeps its verdict and gets no kind and no box
    /// (old answers are not classified afterwards). Taken back one step and forward again. Red when the migration fills them.
    /// </summary>
    [Fact]
    public async Task Migration_Laesst_Bestehende_Antworten_Ohne_Art()
    {
        const string before = "20261125100246_AccentLeniency";
        const string target = "20261125213822_AnswerErrorKind";
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling-fehlerart-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";
        try
        {
            var options = new DbContextOptionsBuilder<PuglingDbContext>().UseSqlite(connectionString).Options;
            await using (var db = new PuglingDbContext(options))
            {
                await db.Database.MigrateAsync();
                var child = new Child { Name = "Bestand" };
                db.Children.Add(child);
                await db.SaveChangesAsync();
                db.ItemReviewEvents.Add(new ItemReviewEvent
                {
                    ChildId = child.Id,
                    ExerciseId = 1,
                    VocabularyId = 1,
                    GivenAnswer = "ecole",
                    WasCorrect = false,
                    ErrorKind = AnswerErrorKind.Accent,
                    BoxBefore = 3,
                });
                await db.SaveChangesAsync();

                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(before);
                await migrator.MigrateAsync(target);
            }

            await using (var db = new PuglingDbContext(options))
            {
                // Projected: the database stands at this migration, before later columns (B-1028's Day) existed.
                var row = await db.ItemReviewEvents.AsNoTracking()
                    .Select(e => new { e.GivenAnswer, e.WasCorrect, e.ErrorKind, e.BoxBefore }).SingleAsync();
                Assert.Equal(("ecole", false), (row.GivenAnswer, row.WasCorrect));
                Assert.Null(row.ErrorKind);
                Assert.Null(row.BoxBefore);
            }
        }
        finally
        {
            using (var own = new SqliteConnection(connectionString)) SqliteConnection.ClearPool(own);
            File.Delete(dbPath);
        }
    }

    // ---- Setup ----

    // A fresh child per case: the cases read its history. The German side is made unique, so store entries of other cases
    // never share a meaning with these words (the synonym check reads the whole store).
    private async Task<Setup> SetupAsync((string Word, string German)[] words, TestStage stage, bool introduced,
        string lang = "en")
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "7" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Fehlerart-Kind"), pin }));
        var german = words.Select(w => TestApi.UniqueName(w.German)).ToArray();
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, lang, "de",
            [.. words.Select((w, i) => (w.Word, german[i]))]);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)stage, childId: childId,
            introduced: introduced);
        return new Setup(father, await TestApi.ChildAsync(factory, childId, pin), childId, planId, positionId, exerciseId, german);
    }

    private void SeedCrossPlanBox(Setup s, int itemIndex, int box) => Db(db =>
    {
        var item = db.ExerciseItems.Where(i => i.ExerciseId == s.ExerciseId).OrderBy(i => i.OrderIndex).Skip(itemIndex).First();
        db.ItemProgress.Add(new ItemProgress
        {
            ChildId = s.ChildId,
            ItemId = item.Id,
            ExerciseId = s.ExerciseId,
            VocabularyId = item.VocabularyId,
            Box = box,
        });
        return db.SaveChanges();
    });

    private static async Task<JsonElement> ReviewAsync(Setup s, int sessionId, int itemIndex, string answer)
    {
        var res = await TestApi.PositionReviewAsync(s.Child, s.PlanId, s.PositionId, sessionId, itemIndex, givenAnswer: answer);
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task PostReviewAsync(Setup s, int sessionId, object body)
    {
        var res = await s.Child.PostAsJsonAsync($"{TestApi.PracticeBase(s.PlanId, s.PositionId)}/{sessionId}/review", body);
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    private static string TestUrl(Setup s) => $"/api/v1/student/study-plans/{s.PlanId}/positions/{s.PositionId}/tests";

    private static async Task<int> StartTestAsync(Setup s) =>
        await TestApi.IdWithKeyAsync(await s.Child.PostAsJsonAsync(TestUrl(s), new { }), "attemptId");

    private static async Task AnswerStepwiseAsync(Setup s, int attemptId, Dictionary<int, string> answers)
    {
        for (var i = 0; i < answers.Count; i++)
        {
            var next = await s.Child.GetFromJsonAsync<JsonElement>($"{TestUrl(s)}/{attemptId}/next");
            var index = next.GetProperty("item").GetProperty("itemIndex").GetInt32();
            var res = await s.Child.PostAsJsonAsync($"{TestUrl(s)}/{attemptId}/answer", new { itemIndex = index, givenAnswer = answers[index] });
            Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
        }
    }

    private static async Task SubmitAsync(Setup s, int attemptId, Dictionary<int, string>? answers)
    {
        var res = await s.Child.PostAsJsonAsync($"{TestUrl(s)}/{attemptId}/submit", new
        {
            answers = answers?.Select(a => new { itemIndex = a.Key, givenAnswer = a.Value }),
        });
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    // The position's history rows keyed by the given answer (each case types distinct answers).
    private Dictionary<string, ItemReviewEvent> History(Setup s) => Db(db => db.ItemReviewEvents.AsNoTracking()
        .Where(e => e.PlanPositionId == s.PositionId && e.GivenAnswer != null && e.StageValue != (int)TestStage.LearnCard)
        .ToDictionary(e => e.GivenAnswer!));

    private T Db<T>(Func<PuglingDbContext, T> work)
    {
        using var scope = factory.Services.CreateScope();
        return work(scope.ServiceProvider.GetRequiredService<PuglingDbContext>());
    }
}

/// <summary>
/// B-732, the mixed review's write path: a checked miss in the round stores its kind on the history like a position's
/// practice. Red when the round passes no kind to the history.
/// </summary>
public class FehlerartMischrundeTests(PuglingWebAppFactory factory) : MischrundeTestBase(factory), IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Mischrunde_Speichert_Die_Art()
    {
        var k = await KidAsync();
        var (_, pos) = await PositionAsync(k);
        SetDue(pos, 0, Today);

        var run = await StartAsync(k);
        var card = run.GetProperty("cards").EnumerateArray().Single(c => c.GetProperty("positionId").GetInt32() == pos);
        var res = await k.Child.PostAsJsonAsync($"{Base(k)}/runs/{run.GetProperty("runId").GetInt32()}/submit", new
        {
            answers = new[] { new { cardId = card.GetProperty("cardId").GetInt32(), givenAnswer = "dgo", wasKnown = (bool?)null } },
        });
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());

        var row = Read(db => db.ItemReviewEvents.Single(e => e.ChildId == k.ChildId && e.PlanPositionId == pos));
        Assert.Equal(("dgo", false, AnswerErrorKind.Typo), (row.GivenAnswer, row.WasCorrect, row.ErrorKind));
    }
}
