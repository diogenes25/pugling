using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Forgiving demotion per position (B-219): with the option a first miss drops an item one box, a second miss in a row
/// resets it to box 1; without it (the default) every miss resets. The cross-plan learning state stays hard either way.
/// Own fixture, because every case moves the shared test clock across several days.
/// </summary>
public class VerzeihendeAbstufungTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private sealed record Setup(HttpClient Child, int PlanId, int PositionId, int ExerciseId, DateTimeOffset Start);

    // AK 1: box 4 → miss → box 3, due today; the next graded miss → box 1. Red if ApplyReview ignores the option (box 1
    // at once) or never reads the marker (box 2 on the second miss).
    [Fact]
    public async Task Mild_Erster_Fehler_Eine_Box_Zweiter_In_Folge_Box_1()
    {
        var s = await SetupAsync(forgiving: true, startBox: 4, ("ember", "Glut"), ("thorn", "Dorn"));

        Assert.Equal(3, await ReviewOnDayAsync(s, 0, "falsch"));
        var prog = Progress(s.PositionId);
        Assert.Equal(DateOnly.FromDateTime(s.Start.UtcDateTime), prog.DueOn);

        Assert.Equal(1, await ReviewOnDayAsync(s, 1, "falsch"));
    }

    // AK 2: a correct answer in between breaks the run: 4 → miss (3) → hit (4) → miss → 3, not 1. Red if a hit does not
    // clear LastMissed. The hit sends the card to box 4, due seven days later (default intervals), so the third grading
    // is on day 8.
    [Fact]
    public async Task Mild_Treffer_Unterbricht_Die_Folge()
    {
        var s = await SetupAsync(forgiving: true, startBox: 4, ("ridge", "Grat"), ("marsh", "Sumpf"));

        Assert.Equal(3, await ReviewOnDayAsync(s, 0, "falsch"));
        Assert.Equal(4, await ReviewOnDayAsync(s, 1, "Grat"));
        Assert.Equal(3, await ReviewOnDayAsync(s, 8, "falsch"));
    }

    // AK 3: the default (option off) keeps the hard reset - box 4 → miss → box 1. Red if the default were on or the
    // forgiving branch ran without the option.
    [Fact]
    public async Task Hart_Ohne_Option_Ein_Fehler_Box_1()
    {
        var s = await SetupAsync(forgiving: false, startBox: 4, ("cliff", "Klippe"), ("brook", "Bach"));

        Assert.Equal(1, await ReviewOnDayAsync(s, 0, "falsch"));
    }

    // AK 4: the cross-plan learning state per word stays hard (user decision 2026-10-23) - a seeded box 4 drops to 1
    // while the position only drops to 3. Red if the forgiving rule is copied into ItemProgressService.
    [Fact]
    public async Task Mild_Gilt_Nicht_Fuer_Den_Planuebergreifenden_Lernstand()
    {
        var s = await SetupAsync(forgiving: true, startBox: 4, ("grove", "Hain"), ("shore", "Ufer"));
        int itemId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var item = db.ExerciseItems.Where(i => i.ExerciseId == s.ExerciseId).OrderBy(i => i.OrderIndex).First();
            itemId = item.Id;
            db.ItemProgress.Add(new ItemProgress
            {
                ChildId = 1,
                ItemId = item.Id,
                ExerciseId = s.ExerciseId,
                VocabularyId = item.VocabularyId,
                Box = 4,
            });
            db.SaveChanges();
        }

        Assert.Equal(3, await ReviewOnDayAsync(s, 0, "falsch"));

        var father = await TestApi.AdultAsync(factory);
        var crossPlan = await father.GetFromJsonAsync<JsonElement>($"/api/v1/student/children/1/vocabulary-progress/{itemId}");
        Assert.Equal(1, crossPlan.GetProperty("box").GetInt32());
    }

    // AK 5: forgiving never drops below box 1. Red without the Max(1, …) floor (box 0).
    [Fact]
    public async Task Mild_Box_1_Bleibt_Box_1()
    {
        var s = await SetupAsync(forgiving: true, startBox: 1, ("crest", "Kamm"), ("vale", "Tal"));

        Assert.Equal(1, await ReviewOnDayAsync(s, 0, "falsch"));
    }

    // AK 2, self-claim (decision 11): the miss marker follows checked answers only. A word introduced yesterday in box 2
    // is asked as a choice; a wrong choice drops it to box 1 and sets the marker. Next day the ladder turns it over
    // (box 1), the child claims "known" unchecked - box 2, but the marker must stay, or the claim would pardon the next
    // checked miss. Red if the self-claim path lets ApplyReview clear LastMissed.
    [Fact]
    public async Task Mild_Selbstauskunft_Gewusst_Loescht_Den_Fehler_Merker_Nicht()
    {
        var s = await SetupAsync(forgiving: true, startBox: 2, ("flint", "Feuerstein"), ("moss", "Moos"), ("fern", "Farn"),
            ("reed", "Schilf"));
        IntroducedYesterday(s);

        Assert.Equal(1, await ReviewOnDayAsync(s, 0, "falsch"));
        Assert.True(Progress(s.PositionId).LastMissed);

        Assert.Equal(2, await SelfClaimOnDayAsync(s, 1, known: true));
        Assert.True(Progress(s.PositionId).LastMissed);
    }

    // Decision 11, the other direction: an unchecked "not known" does not set the marker either. Red if the
    // self-claim path lets ApplyReview set LastMissed.
    [Fact]
    public async Task Mild_Selbstauskunft_Nicht_Gewusst_Setzt_Den_Fehler_Merker_Nicht()
    {
        var s = await SetupAsync(forgiving: true, startBox: 1, ("heath", "Heide"), ("dune", "Duene"));
        IntroducedYesterday(s);

        Assert.Equal(1, await SelfClaimOnDayAsync(s, 0, known: false));
        Assert.False(Progress(s.PositionId).LastMissed);
    }

    private async Task<Setup> SetupAsync(bool forgiving, int startBox, params (string Front, string Back)[] words)
    {
        // Noon UTC of the real day, so no day boundary sits inside a case.
        var start = new DateTimeOffset(DateTime.UtcNow.Date.AddHours(12), TimeSpan.Zero);
        factory.Clock.FreezeAt(start);
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, words);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText);
        var today = DateOnly.FromDateTime(start.UtcDateTime);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var plan = db.StudyPlans.Single(p => p.Id == planId);
            plan.StartDate = today.AddDays(-30);
            plan.EndDate = today.AddDays(30);
            db.PlanPositions.Single(p => p.Id == positionId).ForgivingDemotion = forgiving;
            // Introduced a month ago: past the ladder deadline, so the card is asked at the position's typed stage
            // whatever its box (B-476) - the case measures the box, not the rung.
            var prog = db.PositionItemProgress.Single(p => p.PlanPositionId == positionId && p.ItemIndex == 0);
            prog.Box = startBox;
            prog.DueOn = today;
            prog.IntroducedAt = today.AddDays(-30);
            prog.NewBonusPaidAt = start.UtcDateTime.AddDays(-30);
            db.SaveChanges();
        }
        return new Setup(await TestApi.ChildAsync(factory), planId, positionId, exerciseId, start);
    }

    // One graded review of item 0 on plan day `day` (relative to the start) in a fresh session; returns the box.
    private async Task<int> ReviewOnDayAsync(Setup s, int day, string answer)
    {
        factory.Clock.FreezeAt(s.Start.AddDays(day));
        var sessionId = await TestApi.StartPositionSessionAsync(s.Child, s.PlanId, s.PositionId);
        var res = await TestApi.PositionReviewAsync(s.Child, s.PlanId, s.PositionId, sessionId, 0, givenAnswer: answer);
        res.EnsureSuccessStatusCode();
        var outcome = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(answer != "falsch", outcome.GetProperty("wasCorrect").GetBoolean());
        var box = outcome.GetProperty("box").GetInt32();
        Assert.Equal(box, Progress(s.PositionId).Box);
        return box;
    }

    // One self-assessed review (the ladder's turned-over card) of item 0 on plan day `day`; returns the box.
    private async Task<int> SelfClaimOnDayAsync(Setup s, int day, bool known)
    {
        factory.Clock.FreezeAt(s.Start.AddDays(day));
        var sessionId = await TestApi.StartPositionSessionAsync(s.Child, s.PlanId, s.PositionId);
        var res = await TestApi.PositionReviewAsync(s.Child, s.PlanId, s.PositionId, sessionId, 0, wasKnown: known);
        res.EnsureSuccessStatusCode();
        var outcome = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, outcome.GetProperty("awarded").GetInt32()); // a claim pays nothing: proves the self-claim path
        var box = outcome.GetProperty("box").GetInt32();
        Assert.Equal(box, Progress(s.PositionId).Box);
        return box;
    }

    // Back onto the ladder (B-476): a word introduced yesterday is asked by its box - box 1 turned over, box 2 as a choice.
    private void IntroducedYesterday(Setup s)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.PositionItemProgress.Single(p => p.PlanPositionId == s.PositionId && p.ItemIndex == 0).IntroducedAt =
            DateOnly.FromDateTime(s.Start.UtcDateTime).AddDays(-1);
        db.SaveChanges();
    }

    private PositionItemProgress Progress(int positionId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return db.PositionItemProgress.Single(p => p.PlanPositionId == positionId && p.ItemIndex == 0);
    }
}
