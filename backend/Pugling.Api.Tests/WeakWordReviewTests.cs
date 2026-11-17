using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-584: the father turns the child's weak words into a daily review with one call -
/// <c>POST supervisor/study-plans/{planId}/positions/from-words</c>. Each case names the change that makes it red.
/// </summary>
public class WeakWordReviewTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private sealed record Setup(HttpClient Father, int ChildId, int PlanId, int SubjectId, int UnitA, int UnitB,
        List<int> WordsA, List<int> WordsB, string Pin);

    // A child with two units of one subject: unit A holds an exercise with three words, unit B one with two. The child has
    // learning state for all five, attributed to the exercise it learned them in.
    private async Task<Setup> SetupAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "4" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Schwach-Kind"), pin }));
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Englisch-Schwach") }));
        var (seriesId, unitA) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var unitB = await TestApi.IdAsync(await father.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units",
            new { label = "Unit B", orderIndex = 2 }));
        var exA = await VocabAsync(father, seriesId, unitA, ("dog", "Hund"), ("cat", "Katze"), ("cow", "Kuh"));
        var exB = await VocabAsync(father, seriesId, unitB, ("pig", "Schwein"), ("hen", "Huhn"));
        var wordsA = SeedProgress(childId, exA);
        var wordsB = SeedProgress(childId, exB);
        var planId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/study-plans",
            new { childId, title = "Plan", durationDays = 7, subjectId }));
        return new Setup(father, childId, planId, subjectId, unitA, unitB, wordsA, wordsB, pin);
    }

    private static async Task<int> VocabAsync(HttpClient father, int seriesId, int unitId, params (string Front, string Back)[] words) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary", new
        {
            title = "Wörter",
            orderIndex = 1,
            rewardPoints = 10,
            config = new
            {
                direction = "front-to-back",
                sourceLang = "en",
                targetLang = "de",
                items = words.Select(w => new { front = TestApi.UniqueName(w.Front), back = w.Back }),
            },
        }));

    // Learning state of the child for every word of the exercise; returns the vocabulary ids in item order.
    private List<int> SeedProgress(int childId, int exerciseId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var rows = db.ExerciseItems.Where(i => i.ExerciseId == exerciseId).OrderBy(i => i.OrderIndex).ToList();
        foreach (var r in rows)
            db.ItemProgress.Add(new ItemProgress { ChildId = childId, ItemId = r.Id, ExerciseId = exerciseId, VocabularyId = r.VocabularyId });
        db.SaveChanges();
        return [.. rows.Select(r => r.VocabularyId)];
    }

    private static Task<HttpResponseMessage> PostAsync(Setup s, params int[] ids) =>
        s.Father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{s.PlanId}/positions/from-words", new { vocabularyIds = ids });

    private static async Task AssertProblemAsync(HttpResponseMessage res, HttpStatusCode status, string code)
    {
        Assert.Equal(status, res.StatusCode);
        Assert.Equal(code, (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    private int ExercisesOf(int adultId)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<PuglingDbContext>().Exercises.Count(e => e.AuthorAdultId == adultId);
    }

    /// <summary>
    /// AK 1: a daily position on a new vocabulary exercise whose items are exactly these store entries, in request order;
    /// author = caller with an owner grant, not executable for others, no penalty. Red when the position is created with
    /// <c>Cadence.None</c>, or the items come from inline words instead of the store entries.
    /// </summary>
    [Fact]
    public async Task Legt_Eine_Taegliche_Wiederholung_Aus_Genau_Diesen_Woertern_An()
    {
        var s = await SetupAsync();
        var ids = new[] { s.WordsB[1], s.WordsA[0], s.WordsA[2] };

        var res = await PostAsync(s, ids);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var pos = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Daily", pos.GetProperty("cadence").GetString());
        Assert.Equal(0, pos.GetProperty("penaltyCoins").GetInt32());
        Assert.True(pos.GetProperty("useLeitner").GetBoolean());
        var exerciseId = pos.GetProperty("exerciseId").GetInt32();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.Equal(ids, db.ExerciseItems.Where(i => i.ExerciseId == exerciseId).OrderBy(i => i.OrderIndex).Select(i => i.VocabularyId).ToArray());
        var exercise = db.Exercises.Single(e => e.Id == exerciseId);
        Assert.False(exercise.ExecutePublic);
        Assert.Equal("Vocabulary", exercise.Type);
        Assert.NotNull(exercise.AuthorAdultId);
        Assert.True(db.ExerciseGrants.Any(g => g.ExerciseId == exerciseId && g.CreatorId == exercise.AuthorAdultId && g.Permission == GrantPermission.Owner));
    }

    /// <summary>AK 2: the child itself may not call it. Red when the supervisor gate is missing.</summary>
    [Fact]
    public async Task Das_Kind_Darf_Es_Nicht()
    {
        var s = await SetupAsync();
        var child = await TestApi.ChildAsync(factory, s.ChildId, s.Pin);

        var res = await child.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{s.PlanId}/positions/from-words",
            new { vocabularyIds = new[] { s.WordsA[0] } });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    /// <summary>AK 2: a foreign plan is not found. Red when the plan ownership filter is missing.</summary>
    [Fact]
    public async Task Ein_Fremder_Plan_Ist_Unbekannt()
    {
        var s = await SetupAsync();
        var registered = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults", new { name = "Fremder Vater", pin = "3434" });
        registered.EnsureSuccessStatusCode();
        var strangerId = (await registered.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var stranger = await TestApi.AdultAsync(factory, strangerId, "3434");

        var res = await stranger.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{s.PlanId}/positions/from-words",
            new { vocabularyIds = new[] { s.WordsA[0] } });

        Assert.True(res.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden, res.StatusCode.ToString());
    }

    /// <summary>
    /// AK 3: a word only another child has learning state for is refused, and nothing is created. Red when the check against
    /// this child's learning state is missing.
    /// </summary>
    [Fact]
    public async Task Ein_Wort_Ohne_Lernstand_Des_Kindes_Wird_Abgelehnt()
    {
        var s = await SetupAsync();
        var other = await SetupAsync();
        var adultId = await AdultIdAsync(s.Father);
        var before = ExercisesOf(adultId);

        await AssertProblemAsync(await PostAsync(s, s.WordsA[0], other.WordsA[0]), HttpStatusCode.BadRequest, "invalid_reference");
        Assert.Equal(before, ExercisesOf(adultId));
    }

    /// <summary>AK 4: 0 or 26 words, and two language pairs, are refused. Red when the bounds or the language check are missing.</summary>
    [Fact]
    public async Task Anzahl_Und_Sprachpaar_Werden_Geprueft()
    {
        var s = await SetupAsync();
        await AssertProblemAsync(await PostAsync(s), HttpStatusCode.BadRequest, "validation_error");
        await AssertProblemAsync(await PostAsync(s, [.. Enumerable.Range(1, 26)]), HttpStatusCode.BadRequest, "validation_error");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var v = db.Vocabularies.Single(x => x.Id == s.WordsB[0]);
            v.SourceLanguage = "fr";
            db.SaveChanges();
        }
        await AssertProblemAsync(await PostAsync(s, s.WordsA[0], s.WordsB[0]), HttpStatusCode.BadRequest, "validation_error");
    }

    /// <summary>AK 5: a plan of another subject is refused with its own code (German text in the father's form). Red when the subject check is missing.</summary>
    [Fact]
    public async Task Ein_Plan_Eines_Anderen_Fachs_Wird_Abgelehnt()
    {
        var s = await SetupAsync();
        var math = await TestApi.IdAsync(await s.Father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("Mathe-Schwach") }));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            await db.StudyPlans.Where(p => p.Id == s.PlanId).ExecuteUpdateAsync(u => u.SetProperty(p => p.SubjectId, (int?)math));
        }

        await AssertProblemAsync(await PostAsync(s, s.WordsA[0]), HttpStatusCode.BadRequest, "review_subject_mismatch");
    }

    /// <summary>
    /// AK 6, found at the running product: the exercise the words came from is a position with exactly this set - the
    /// review would be the same duty twice. Red when only review exercises were compared.
    /// </summary>
    [Fact]
    public async Task Die_Herkunftsuebung_Mit_Genau_Diesen_Woertern_Ist_Ein_Konflikt()
    {
        var s = await SetupAsync();
        int sourceExercise;
        using (var scope = factory.Services.CreateScope())
            sourceExercise = scope.ServiceProvider.GetRequiredService<PuglingDbContext>().ExerciseItems
                .Where(i => i.VocabularyId == s.WordsA[0]).Select(i => i.ExerciseId).First();
        (await s.Father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{s.PlanId}/positions", new { exerciseId = sourceExercise })).EnsureSuccessStatusCode();

        await AssertProblemAsync(await PostAsync(s, [.. s.WordsA]), HttpStatusCode.Conflict, "review_already_in_plan");
    }

    /// <summary>
    /// AK 6: the same set again (reordered, with a duplicate) is a conflict and creates nothing; a set that differs by one word
    /// is a new review. Red when the set check is missing (first case) or compares by overlap (second case).
    /// </summary>
    [Fact]
    public async Task Dieselbe_Wortmenge_Ist_Ein_Konflikt_Eine_Andere_Nicht()
    {
        var s = await SetupAsync();
        Assert.Equal(HttpStatusCode.Created, (await PostAsync(s, s.WordsA[0], s.WordsA[1])).StatusCode);

        await AssertProblemAsync(await PostAsync(s, s.WordsA[1], s.WordsA[0], s.WordsA[1]), HttpStatusCode.Conflict, "review_already_in_plan");
        Assert.Equal(HttpStatusCode.Created, (await PostAsync(s, s.WordsA[0], s.WordsA[1], s.WordsA[2])).StatusCode);
    }

    /// <summary>
    /// AK 7: the exercise is filed in the unit most of the words were learned in, not the unit of the first word. Red when
    /// the first word decides (unit A here).
    /// </summary>
    [Fact]
    public async Task Die_Uebung_Haengt_An_Der_Haeufigsten_Unit()
    {
        var s = await SetupAsync();

        var res = await PostAsync(s, s.WordsA[0], s.WordsB[0], s.WordsB[1]);

        var exerciseId = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("exerciseId").GetInt32();
        using var scope = factory.Services.CreateScope();
        Assert.Equal(s.UnitB, scope.ServiceProvider.GetRequiredService<PuglingDbContext>().Exercises.Single(e => e.Id == exerciseId).SeriesUnitId);
    }

    /// <summary>
    /// The weak-word list names each word's language pair, so the father's page can offer one review per pair (review
    /// blocker: a child learning French and English got one button the server refused). Red when the fields stay empty.
    /// </summary>
    [Fact]
    public async Task Die_Wortliste_Nennt_Das_Sprachpaar()
    {
        var s = await SetupAsync();
        var words = await s.Father.GetFromJsonAsync<JsonElement>($"/api/v1/student/children/{s.ChildId}/vocabulary-progress/by-word");
        Assert.All(words.EnumerateArray(), w =>
        {
            Assert.Equal("en", w.GetProperty("sourceLanguage").GetString());
            Assert.Equal("de", w.GetProperty("targetLanguage").GetString());
        });
        Assert.Equal(5, words.GetArrayLength());
    }

    private static async Task<int> AdultIdAsync(HttpClient father) =>
        (await father.GetFromJsonAsync<JsonElement>("/api/v1/auth/me")).GetProperty("adultId").GetInt32();
}
