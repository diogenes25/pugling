using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-848: a position on a build path plays the prerequisite of its current level - content, type, config and learning
/// state follow the played exercise, the father sets the start level, and the views name played and target exercise.
/// </summary>
public class AufbauStufePlayTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static string Unit(int seriesId, int unitId, string type) =>
        $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/{type}";

    private static object Vocab(string title, (string Front, string Back)[] items, int? buildLevel = null, int? target = null,
        string? bookPages = null) => new
        {
            title,
            orderIndex = 1,
            rewardPoints = 10,
            bookPages,
            config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de", items = items.Select(i => new { front = i.Front, back = i.Back }) },
            buildLevel,
            buildTargetExerciseId = target,
        };

    // A vocabulary target with a prerequisite of the given kind on level 1 in the same unit, and an empty plan for child 1.
    private async Task<(HttpClient Father, int PlanId, int TargetId, int PrereqId, string PrereqUrl)> PathAsync(
        bool arithmeticPrereq)
    {
        var father = await TestApi.AdultAsync(factory);
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, await TestApi.DefaultSubjectIdAsync(father));
        var targetId = await TestApi.IdAsync(await father.PostAsJsonAsync(Unit(seriesId, unitId, "vocabulary"),
            Vocab("Ziel: Tiere", [("dog", "Hund"), ("cat", "Katze")], bookPages: "S. 20")));
        var prereqId = arithmeticPrereq
            ? await TestApi.IdAsync(await father.PostAsJsonAsync(Unit(seriesId, unitId, "arithmetic"), new
            {
                title = "Vorstufe: Rechnen",
                orderIndex = 2,
                rewardPoints = 10,
                bookPages = "S. 8",
                config = new { problems = new[] { new { prompt = "2 + 3", answer = 5, tolerance = 0 } } },
                buildLevel = 1,
                buildTargetExerciseId = targetId,
            }))
            : await TestApi.IdAsync(await father.PostAsJsonAsync(Unit(seriesId, unitId, "vocabulary"),
                Vocab("Vorstufe: Farben", [("red", "rot"), ("blue", "blau")], 1, targetId, "S. 8")));
        var planId = await TestApi.CreateEmptyPlanAsync(father);
        var prereqUrl = $"{Unit(seriesId, unitId, arithmeticPrereq ? "arithmetic" : "vocabulary")}/{prereqId}";
        return (father, planId, targetId, prereqId, prereqUrl);
    }

    private static string Positions(int planId) => $"/api/v1/supervisor/study-plans/{planId}/positions";

    private static async Task<int> PositionAsync(HttpClient father, int planId, int exerciseId, int? level,
        string cadence = "None") =>
        await TestApi.IdAsync(await father.PostAsJsonAsync(Positions(planId),
            new { exerciseId, currentBuildLevel = level, cadence, useLeitner = true }));

    private static async Task<JsonElement> NextCardAsync(HttpClient child, int planId, int positionId)
    {
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        var next = await child.GetFromJsonAsync<JsonElement>($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/next");
        return next.GetProperty("card");
    }

    private static async Task AssertProblemAsync(HttpResponseMessage res, HttpStatusCode status, string code)
    {
        Assert.Equal(status, res.StatusCode);
        Assert.Equal(code, (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    private T Db<T>(Func<PuglingDbContext, T> read)
    {
        using var scope = factory.Services.CreateScope();
        return read(scope.ServiceProvider.GetRequiredService<PuglingDbContext>());
    }

    /// <summary>
    /// AC 1 and 3, practice: level 1 plays the prerequisite - its content, its type (arithmetic under a vocabulary
    /// target) and its grading. Red: an <c>ItemsOfAsync</c> or a <c>TypeOf</c> that reads <c>pos.Exercise</c>.
    /// </summary>
    [Fact]
    public async Task Ueben_Spielt_Die_Vorstufe_Mit_Ihrem_Typ()
    {
        var (father, planId, targetId, _, _) = await PathAsync(arithmeticPrereq: true);
        var positionId = await PositionAsync(father, planId, targetId, 1);
        var child = await TestApi.ChildAsync(factory);

        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        var next = await child.GetFromJsonAsync<JsonElement>($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/next");
        var card = next.GetProperty("card");
        Assert.Equal("Arithmetic", card.GetProperty("type").GetString());
        Assert.Equal("2 + 3", card.GetProperty("prompt").GetString());

        var review = await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "5");
        Assert.True((await review.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("wasCorrect").GetBoolean());
    }

    /// <summary>
    /// AC 1 and 3, test: start, next, answer and submit run on the prerequisite. Red: a test path that reads type or
    /// config of the target (the vocabulary grader would reject "5").
    /// </summary>
    [Fact]
    public async Task Test_Spielt_Die_Vorstufe_Bis_Zur_Abgabe()
    {
        var (father, planId, targetId, _, _) = await PathAsync(arithmeticPrereq: true);
        var positionId = await PositionAsync(father, planId, targetId, 1);
        var child = await TestApi.ChildAsync(factory);
        var tests = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var started = await child.PostAsJsonAsync(tests, new { });
        Assert.True(started.IsSuccessStatusCode, await started.Content.ReadAsStringAsync());
        var attemptId = await TestApi.IdWithKeyAsync(started, "attemptId");
        var item = (await child.GetFromJsonAsync<JsonElement>($"{tests}/{attemptId}/next")).GetProperty("item");
        Assert.Equal("Arithmetic", item.GetProperty("type").GetString());
        Assert.Equal("2 + 3", item.GetProperty("prompt").GetString());
        (await child.PostAsJsonAsync($"{tests}/{attemptId}/answer", new { itemIndex = 0, givenAnswer = "5" })).EnsureSuccessStatusCode();
        var submit = await child.PostAsJsonAsync($"{tests}/{attemptId}/submit", new { });
        submit.EnsureSuccessStatusCode();
        var result = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, result.GetProperty("totalItems").GetInt32());
        Assert.True(result.GetProperty("passed").GetBoolean());
    }

    /// <summary>
    /// AC 3, the bulk submission: answers sent with the submit are graded with the prerequisite's type. Red: the submit
    /// path reading the target's type (the vocabulary grader would not take "5" on the arithmetic stage).
    /// </summary>
    [Fact]
    public async Task Sammelabgabe_Wertet_Mit_Dem_Typ_Der_Vorstufe()
    {
        var (father, planId, targetId, _, _) = await PathAsync(arithmeticPrereq: true);
        var positionId = await PositionAsync(father, planId, targetId, 1);
        var child = await TestApi.ChildAsync(factory);
        var tests = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";

        var attemptId = await TestApi.IdWithKeyAsync(await child.PostAsJsonAsync(tests, new { }), "attemptId");
        var submit = await child.PostAsJsonAsync($"{tests}/{attemptId}/submit",
            new { answers = new[] { new { itemIndex = 0, givenAnswer = "5", wasKnown = (bool?)null } } });
        submit.EnsureSuccessStatusCode();
        var result = await submit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, result.GetProperty("correctItems").GetInt32());
    }

    /// <summary>
    /// The position's own stage was set and checked against the target's type; on a prerequisite of another type it does
    /// not apply - the prerequisite plays its own default. Red: <c>StageForDay</c> reading <c>pos.Stage</c> on a
    /// prerequisite (the arithmetic card would come at the vocabulary choice stage 6).
    /// </summary>
    [Fact]
    public async Task Stufe_Der_Position_Gilt_Nicht_Fuer_Die_Vorstufe()
    {
        var (father, planId, targetId, _, _) = await PathAsync(arithmeticPrereq: true);
        var positionId = await TestApi.IdAsync(await father.PostAsJsonAsync(Positions(planId),
            new { exerciseId = targetId, currentBuildLevel = 1, stage = 6, useLeitner = true }));
        var child = await TestApi.ChildAsync(factory);

        var onPrereq = await NextCardAsync(child, planId, positionId);
        Assert.Equal("Arithmetic", onPrereq.GetProperty("type").GetString());
        Assert.NotEqual(6, onPrereq.GetProperty("stage").GetInt32());
    }

    /// <summary>
    /// AC 2: without a level, and on level 3 (the target), the position plays its own exercise. Red: <c>null</c> or the
    /// target level read as a prerequisite level.
    /// </summary>
    [Fact]
    public async Task Ohne_Stufe_Und_Auf_Der_Zielstufe_Spielt_Die_Zieluebung()
    {
        var (father, planId, targetId, _, _) = await PathAsync(arithmeticPrereq: true);
        var child = await TestApi.ChildAsync(factory);
        foreach (int? level in new int?[] { null, 3 })
        {
            var positionId = await PositionAsync(father, planId, targetId, level);
            var card = await NextCardAsync(child, planId, positionId);
            Assert.Equal("Vocabulary", card.GetProperty("type").GetString());
        }
    }

    /// <summary>
    /// AC 4 and 5: the learning state of one position lies apart per played exercise - a word learnt on the prerequisite
    /// is new on the target (its first card is an introduction again), and the cross-plan state is written for the
    /// prerequisite. Red: a progress read without the exercise filter, or a record under <c>pos.ExerciseId</c>.
    /// </summary>
    [Fact]
    public async Task Lernstand_Liegt_Je_Gespielter_Uebung_Getrennt()
    {
        var (father, planId, targetId, prereqId, _) = await PathAsync(arithmeticPrereq: false);
        var positionId = await PositionAsync(father, planId, targetId, 1);
        var child = await TestApi.ChildAsync(factory);

        // The first card of the prerequisite is its learn card ("rot" shown, "red" written); recalled right, the word is
        // introduced and imprinted - known, so on a shared state the target's item 0 would be no introduction.
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        var first = (await child.GetFromJsonAsync<JsonElement>($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/next"))
            .GetProperty("card");
        // A learn card shows the learnt word: the prerequisite's, not the target's "Hund".
        Assert.Equal("rot", first.GetProperty("prompt").GetString());
        Assert.True(first.GetProperty("introduction").GetBoolean());
        var recall = await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "red");
        Assert.True((await recall.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("wasCorrect").GetBoolean());

        var rows = Db(db => db.PositionItemProgress.AsNoTracking().Where(p => p.PlanPositionId == positionId)
            .Select(p => new { p.ExerciseId, p.ItemIndex, p.ImprintedAt }).ToList());
        var row = Assert.Single(rows);
        Assert.Equal(prereqId, row.ExerciseId);
        Assert.NotNull(row.ImprintedAt);
        // The cross-plan record of the answer (the learn card writes history only, no mastery) names the prerequisite.
        Assert.Equal(prereqId, Db(db => db.ItemReviewEvents.AsNoTracking().Where(e => e.PlanPositionId == positionId)
            .Select(e => e.ExerciseId).Single()));

        // Up to the target (the round ended first: an open one holds the level): item 0 there is still unseen, whatever
        // the prerequisite's item 0 holds.
        (await child.PostAsJsonAsync($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/end", new { })).EnsureSuccessStatusCode();
        (await father.PatchAsJsonAsync($"{Positions(planId)}/{positionId}", new { currentBuildLevel = 3 })).EnsureSuccessStatusCode();
        var targetCard = await NextCardAsync(child, planId, positionId);
        Assert.Equal("Hund", targetCard.GetProperty("prompt").GetString());
        Assert.True(targetCard.GetProperty("introduction").GetBoolean());
    }

    /// <summary>
    /// AC 3, the easier stage: a learn card's recall made easier on the prerequisite stays the prerequisite's card. Red:
    /// the eased card built from the target's items or config.
    /// </summary>
    [Fact]
    public async Task Leichtere_Stufe_Bleibt_Auf_Der_Vorstufe()
    {
        var (father, planId, targetId, _, _) = await PathAsync(arithmeticPrereq: false);
        var positionId = await PositionAsync(father, planId, targetId, 1);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        (await child.GetFromJsonAsync<JsonElement>($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/next")).GetProperty("card");

        var eased = await child.PostAsJsonAsync($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/easier", new { itemIndex = 0 });
        eased.EnsureSuccessStatusCode();
        var card = await eased.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("rot", card.GetProperty("prompt").GetString());
        Assert.True(card.GetProperty("eased").GetBoolean());
    }

    /// <summary>
    /// AC 6: the start level is set and read back, moved by PATCH, and refused without a matching prerequisite, on an
    /// exercise without a path, and together with a daily or weekly duty - on create and on a later cadence change.
    /// </summary>
    [Fact]
    public async Task Startstufe_Wird_Gesetzt_Gelesen_Und_Geprueft()
    {
        var (father, planId, targetId, prereqId, _) = await PathAsync(arithmeticPrereq: false);

        var created = await father.PostAsJsonAsync(Positions(planId), new { exerciseId = targetId, currentBuildLevel = 1 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        var positionId = body.GetProperty("id").GetInt32();
        Assert.Equal(1, body.GetProperty("currentBuildLevel").GetInt32());
        var read = await father.GetFromJsonAsync<JsonElement>($"{Positions(planId)}/{positionId}");
        Assert.Equal(1, read.GetProperty("currentBuildLevel").GetInt32());
        Assert.Equal(prereqId, read.GetProperty("build").GetProperty("playedExerciseId").GetInt32());

        var moved = await father.PatchAsJsonAsync($"{Positions(planId)}/{positionId}", new { currentBuildLevel = 3 });
        Assert.Equal(3, (await moved.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("currentBuildLevel").GetInt32());

        await AssertProblemAsync(await father.PostAsJsonAsync(Positions(planId), new { exerciseId = targetId, currentBuildLevel = 2 }),
            HttpStatusCode.BadRequest, "validation_error");
        await AssertProblemAsync(await father.PostAsJsonAsync(Positions(planId), new { exerciseId = targetId, currentBuildLevel = 4 }),
            HttpStatusCode.BadRequest, "validation_error");
        await AssertProblemAsync(await father.PostAsJsonAsync(Positions(planId), new { exerciseId = prereqId, currentBuildLevel = 3 }),
            HttpStatusCode.BadRequest, "validation_error");
        foreach (var cadence in new[] { "Daily", "Weekly" })
        {
            await AssertProblemAsync(await father.PostAsJsonAsync(Positions(planId),
                    new { exerciseId = targetId, currentBuildLevel = 1, cadence }),
                HttpStatusCode.BadRequest, "validation_error");
            await AssertProblemAsync(await father.PatchAsJsonAsync($"{Positions(planId)}/{positionId}", new { cadence }),
                HttpStatusCode.BadRequest, "validation_error");
        }
        // Without a level the same duties stay possible: the lock is the path's, not the exercise's.
        Assert.Equal(HttpStatusCode.Created, (await father.PostAsJsonAsync(Positions(planId),
            new { exerciseId = targetId, cadence = "Daily" })).StatusCode);
    }

    /// <summary>
    /// AC 7: the child's day shows title and book pages of the prerequisite; the father's position, the plan overview and
    /// the report keep the target and name "level n of m" with the exercise played now. Red: a view that reads the
    /// target where the child plays the prerequisite, or the other way round.
    /// </summary>
    [Fact]
    public async Task Kind_Sieht_Die_Vorstufe_Vater_Das_Ziel_Mit_Stufe()
    {
        var (father, planId, targetId, prereqId, _) = await PathAsync(arithmeticPrereq: false);
        var positionId = await PositionAsync(father, planId, targetId, 1);
        var child = await TestApi.ChildAsync(factory);

        var today = await child.GetFromJsonAsync<JsonElement>("/api/v1/student/children/1/today");
        var status = today.GetProperty("positions").EnumerateArray().Single(p => p.GetProperty("positionId").GetInt32() == positionId);
        Assert.Equal("Vorstufe: Farben", status.GetProperty("exerciseTitle").GetString());
        Assert.Equal("S. 8", status.GetProperty("bookPages").GetString());
        Assert.Equal(prereqId, status.GetProperty("exerciseId").GetInt32());
        AssertBuild(status.GetProperty("build"), 1, 2, prereqId, targetId);

        var position = await father.GetFromJsonAsync<JsonElement>($"{Positions(planId)}/{positionId}");
        Assert.Equal("Ziel: Tiere", position.GetProperty("exerciseTitle").GetString());
        AssertBuild(position.GetProperty("build"), 1, 2, prereqId, targetId);

        var report = await father.GetFromJsonAsync<JsonElement>($"{Positions(planId)}/{positionId}/report");
        Assert.Equal("Ziel: Tiere", report.GetProperty("exerciseTitle").GetString());
        AssertBuild(report.GetProperty("build"), 1, 2, prereqId, targetId);
        Assert.Equal("red", report.GetProperty("items")[0].GetProperty("prompt").GetString());

        // On the target the path reads "level 2 of 2" and the child sees the target again.
        (await father.PatchAsJsonAsync($"{Positions(planId)}/{positionId}", new { currentBuildLevel = 3 })).EnsureSuccessStatusCode();
        var onTarget = await father.GetFromJsonAsync<JsonElement>($"{Positions(planId)}/{positionId}");
        AssertBuild(onTarget.GetProperty("build"), 2, 2, targetId, targetId);
    }

    /// <summary>
    /// Review finding (B-848): while the child has a round or a test of today open, the level cannot change - its frozen
    /// cards belong to the old level's exercise. Once the round is ended the change goes through. Red: the PATCH without
    /// the in-play check, or a check that never lets go.
    /// </summary>
    [Fact]
    public async Task Stufe_Wechselt_Nicht_Unter_Einer_Offenen_Runde()
    {
        var (father, planId, targetId, _, _) = await PathAsync(arithmeticPrereq: false);
        var positionId = await PositionAsync(father, planId, targetId, 1);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);

        await AssertProblemAsync(await father.PatchAsJsonAsync($"{Positions(planId)}/{positionId}", new { currentBuildLevel = 3 }),
            HttpStatusCode.Conflict, "build_level_in_play");
        // The same level sent again is no change, and other settings stay editable during the round.
        Assert.Equal(HttpStatusCode.OK, (await father.PatchAsJsonAsync($"{Positions(planId)}/{positionId}",
            new { currentBuildLevel = 1, pointsGoalMet = 30 })).StatusCode);

        (await child.PostAsJsonAsync($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/end", new { })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await father.PatchAsJsonAsync($"{Positions(planId)}/{positionId}",
            new { currentBuildLevel = 3 })).StatusCode);

        // An open test holds the level the same way.
        var tests = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests";
        (await child.PostAsJsonAsync(tests, new { })).EnsureSuccessStatusCode();
        await AssertProblemAsync(await father.PatchAsJsonAsync($"{Positions(planId)}/{positionId}", new { currentBuildLevel = 1 }),
            HttpStatusCode.Conflict, "build_level_in_play");
    }

    /// <summary>
    /// Review finding (B-848): the child plays the prerequisite, so its execute gate counts - a public target does not
    /// open a private prerequisite of another author. Red: Create checking the target only.
    /// </summary>
    [Fact]
    public async Task Private_Vorstufe_Braucht_Das_Ausfuehrungsrecht()
    {
        var (author, _, targetId, prereqId, _) = await PathAsync(arithmeticPrereq: false);
        (await author.PatchAsJsonAsync($"/api/v1/creator/exercises/{prereqId}/sharing", new { executePublic = false }))
            .EnsureSuccessStatusCode();

        var otherId = await TestApi.IdAsync(await factory.CreateClient()
            .PostAsJsonAsync("/api/v1/supervisor/adults", new { name = TestApi.UniqueName("Vater"), pin = "4848" }));
        var other = await TestApi.AdultAsync(factory, otherId, "4848");
        var childId = await TestApi.IdAsync(await other.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Kind", pin = "7777" }));
        var planId = await TestApi.CreateEmptyPlanAsync(other, childId);

        await AssertProblemAsync(await other.PostAsJsonAsync(Positions(planId), new { exerciseId = targetId, currentBuildLevel = 1 }),
            HttpStatusCode.Forbidden, "exercise_not_executable");
        // The target itself stays assignable to him.
        Assert.Equal(HttpStatusCode.Created, (await other.PostAsJsonAsync(Positions(planId),
            new { exerciseId = targetId, currentBuildLevel = 3 })).StatusCode);
    }

    /// <summary>
    /// Review finding (B-848): remediation runs on the exercise played, so it is checked there - a cloze target that
    /// supports it does not vouch for an arithmetic prerequisite that cannot be made easier. On create and on a later
    /// level change. Red: the remediation check on the target only.
    /// </summary>
    [Fact]
    public async Task Nacharbeit_Wird_An_Der_Gespielten_Vorstufe_Geprueft()
    {
        var father = await TestApi.AdultAsync(factory);
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, await TestApi.DefaultSubjectIdAsync(father));
        var targetId = await TestApi.IdAsync(await father.PostAsJsonAsync(Unit(seriesId, unitId, "cloze"), new
        {
            title = "Ziel: Lückentext",
            orderIndex = 1,
            rewardPoints = 10,
            config = new { text = "I am {{1}}.", gaps = new[] { new { index = 1, answer = "fine", alternatives = Array.Empty<string>() } } },
        }));
        (await father.PostAsJsonAsync(Unit(seriesId, unitId, "arithmetic"), new
        {
            title = "Vorstufe: Rechnen",
            orderIndex = 2,
            rewardPoints = 10,
            config = new { problems = new[] { new { prompt = "2 + 3", answer = 5, tolerance = 0 } } },
            buildLevel = 1,
            buildTargetExerciseId = targetId,
        })).EnsureSuccessStatusCode();
        var planId = await TestApi.CreateEmptyPlanAsync(father);

        await AssertProblemAsync(await father.PostAsJsonAsync(Positions(planId),
                new { exerciseId = targetId, currentBuildLevel = 1, remediation = true }),
            HttpStatusCode.BadRequest, "validation_error");
        // On the target remediation fits; moving such a position down to the prerequisite is refused the same way.
        var positionId = await TestApi.IdAsync(await father.PostAsJsonAsync(Positions(planId),
            new { exerciseId = targetId, currentBuildLevel = 3, remediation = true }));
        await AssertProblemAsync(await father.PatchAsJsonAsync($"{Positions(planId)}/{positionId}", new { currentBuildLevel = 1 }),
            HttpStatusCode.BadRequest, "validation_error");
    }

    private static void AssertBuild(JsonElement build, int level, int of, int playedId, int targetId)
    {
        Assert.Equal(level, build.GetProperty("level").GetInt32());
        Assert.Equal(of, build.GetProperty("of").GetInt32());
        Assert.Equal(playedId, build.GetProperty("playedExerciseId").GetInt32());
        Assert.Equal(targetId, build.GetProperty("targetExerciseId").GetInt32());
    }

    /// <summary>
    /// A prerequisite the child played cannot be deleted - the progress on it is the child's record (Restrict). The
    /// delete answers 409 like any exercise in use, not a 500 from the foreign key. Red: the usage count without the
    /// build path.
    /// </summary>
    [Fact]
    public async Task Gespielte_Vorstufe_Ist_Nicht_Loeschbar()
    {
        var (father, planId, targetId, prereqId, prereqUrl) = await PathAsync(arithmeticPrereq: false);
        var positionId = await PositionAsync(father, planId, targetId, 1);
        var child = await TestApi.ChildAsync(factory);
        var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
        (await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "red")).EnsureSuccessStatusCode();
        // Moved on to the target: only the progress row still ties the prerequisite to the position.
        (await child.PostAsJsonAsync($"{TestApi.PracticeBase(planId, positionId)}/{sessionId}/end", new { })).EnsureSuccessStatusCode();
        (await father.PatchAsJsonAsync($"{Positions(planId)}/{positionId}", new { currentBuildLevel = 3 })).EnsureSuccessStatusCode();

        await AssertProblemAsync(await father.DeleteAsync(prereqUrl),
            HttpStatusCode.Conflict, "exercise_in_use");
        // The usage display names the plan the 409 counts (review finding: it listed the position's own exercise only).
        var usage = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{prereqId}/usage");
        Assert.Contains(usage.GetProperty("plans").EnumerateArray(), p => p.GetProperty("planId").GetInt32() == planId);
    }

    /// <summary>
    /// The seam fails loudly: a position on a build path read without <c>Exercise.Prerequisites</c> throws instead of
    /// playing the target. Red: a fallback to <c>pos.Exercise</c> when the prerequisites are not loaded.
    /// </summary>
    [Fact]
    public void Gespielte_Uebung_Ohne_Geladene_Vorstufen_Wirft()
    {
        var pos = new PlanPosition { Id = 7, CurrentBuildLevel = 1, Exercise = new Exercise { Id = 1, Title = "Ziel" } };
        Assert.Throws<InvalidOperationException>(() => PositionPlayService.PlayedExercise(pos, default));
        // Without a level nothing needs loading.
        pos.CurrentBuildLevel = null;
        Assert.Same(pos.Exercise, PositionPlayService.PlayedExercise(pos, default));
    }
}
