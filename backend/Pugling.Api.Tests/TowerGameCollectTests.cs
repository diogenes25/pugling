using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// What a child collects in "Vokabel-Türme": trophies from the server's own count at the end of a run, the trophy path
/// that pays gems once per step, the tower missions settled through the gamification service, the game chest and the
/// game streak. The app only reports the wave it reached, and that counts only as far as the play time covers it.
/// </summary>
public class TowerGameCollectTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static readonly (string Front, string Back)[] Words =
        [("dog", "Hund"), ("cat", "Katze"), ("house", "Haus"), ("tree", "Baum"), ("water", "Wasser"), ("bread", "Brot")];

    private static readonly ChildDutyDays EveryWeekday = new(sundayFree: true, []);

    private static string Base(int childId = 1) => $"/api/v1/student/children/{childId}/tower-game";

    private async Task<(HttpClient child, TowerRunResponse run)> StartAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, Words);
        var (_, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, stage: 1);
        var child = await TestApi.ChildAsync(factory);
        var res = await child.PostAsJsonAsync($"{Base()}/runs", new StartTowerRunDto(positionId));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (child, (await res.Content.ReadFromJsonAsync<TowerRunResponse>())!);
    }

    // The tests of this class share one child and one database: each starts without runs, game answers and payouts.
    private void Reset()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.TowerGameRuns.Where(r => r.ChildId == 1).ExecuteDelete();
        db.TowerTrophyRewards.Where(r => r.ChildId == 1).ExecuteDelete();
        db.TowerGameChests.Where(c => c.ChildId == 1).ExecuteDelete();
        db.ItemReviewEvents.Where(e => e.ChildId == 1 && e.Source == ItemReviewSource.Game).ExecuteDelete();
        db.MissionAwards.Where(a => a.Mission!.ChildId == 1).ExecuteDelete();
        db.ChildPointsEntries.Where(e => e.ChildId == 1 && (e.Kind == PointKind.TowerGameCoins
            || e.Kind == PointKind.TowerTrophyGems || e.Kind == PointKind.TowerChestGems || e.Kind == PointKind.Mission))
            .ExecuteDelete();
    }

    private T Db<T>(Func<PuglingDbContext, T> read)
    {
        using var scope = factory.Services.CreateScope();
        return read(scope.ServiceProvider.GetRequiredService<PuglingDbContext>());
    }

    private void SetPlayedSeconds(int runId, int seconds) => Db(db =>
        db.TowerGameRuns.Where(r => r.Id == runId).ExecuteUpdate(r => r.SetProperty(x => x.PlayedSeconds, seconds)));

    private int Ledger(PointKind kind) =>
        Db(db => db.ChildPointsEntries.Where(e => e.ChildId == 1 && e.Kind == kind).Sum(e => e.Amount));

    private static async Task SolveAsync(HttpClient child, TowerRunResponse run, int words)
    {
        foreach (var (front, back) in Words.Take(words))
            Assert.True((await (await child.PostAsJsonAsync($"{Base()}/assign", new AssignTowerTokenDto(run.RunId,
                run.Tokens.Single(t => t.Word == front).TokenId, run.Towers.Single(t => t.Prompt == back).TowerId)))
                .Content.ReadFromJsonAsync<TowerAssignResult>())!.Correct);
    }

    private static async Task HitAsync(HttpClient child, TowerRunResponse run, int times)
    {
        for (var i = 0; i < times; i++)
            await child.PostAsJsonAsync($"{Base()}/assign", new AssignTowerTokenDto(run.RunId,
                run.Tokens.Single(t => t.Word == "dog").TokenId, run.Towers.Single(t => t.Prompt == "Hund").TowerId));
    }

    private static async Task<TowerRunEndResult> EndAsync(HttpClient child, TowerRunResponse run, int wave)
    {
        var res = await child.PostAsJsonAsync($"{Base()}/runs/{run.RunId}/end", new EndTowerRunDto(wave));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<TowerRunEndResult>())!;
    }

    [Theory]
    // Three new words and two cleared waves.
    [InlineData(new[] { 3, 3, 3 }, 3, 11)]
    // Red without the cap by solved words: eight cleared waves would add 8 to one word.
    [InlineData(new[] { 2 }, 9, 3)]
    // Red if waves paid on their own: a run without a single word would earn 8.
    [InlineData(new int[0], 9, 0)]
    // The first wave is not cleared yet.
    [InlineData(new[] { 1, 1 }, 1, 2)]
    public void RunTrophies_SindWortwertePlusWellen_HoechstensSoVieleWellenWieWoerter(int[] values, int wave, int expected) =>
        Assert.Equal(expected, TowerGameService.RunTrophies(values, wave));

    [Theory]
    [InlineData(9, 0, 1)]
    [InlineData(9, 99, 5)]
    [InlineData(9, 100, 6)]
    [InlineData(3, 600, 3)]
    [InlineData(-4, 600, 0)]
    public void AcceptedWave_ZaehltNurSoWeitDieSpielzeitTraegt(int reported, int playedSeconds, int expected) =>
        Assert.Equal(expected, TowerGameService.AcceptedWave(reported, playedSeconds, 20));

    [Fact]
    public void Streak_ZaehltTageInFolge_HeuteIstNochOffen_SonntagBrichtNicht()
    {
        // 2026-11-28 is a Saturday.
        var saturday = new DateOnly(2026, 11, 28);
        var perDay = new Dictionary<DateOnly, int>
        {
            [saturday.AddDays(-1)] = 10,
            [saturday.AddDays(-2)] = 12,
            [saturday.AddDays(-3)] = 9,
            [saturday.AddDays(-4)] = 30,
        };
        // Red if an open today broke the streak (0), or if nine words counted as a day (4).
        Assert.Equal(2, TowerGameService.Streak(perDay, EveryWeekday, saturday, 10, 120));
        perDay[saturday] = 10;
        Assert.Equal(3, TowerGameService.Streak(perDay, EveryWeekday, saturday, 10, 120));

        // Monday after a Sunday without play: the Sunday is stepped over; a Sunday with play counts.
        var monday = new DateOnly(2026, 11, 30);
        var overSunday = new Dictionary<DateOnly, int> { [monday] = 10, [saturday] = 10 };
        Assert.Equal(2, TowerGameService.Streak(overSunday, EveryWeekday, monday, 10, 120));
        overSunday[monday.AddDays(-1)] = 10;
        Assert.Equal(3, TowerGameService.Streak(overSunday, EveryWeekday, monday, 10, 120));
        // Red if a missed weekday were stepped over like a Sunday.
        var gap = new Dictionary<DateOnly, int> { [saturday] = 10, [saturday.AddDays(-2)] = 10 };
        Assert.Equal(1, TowerGameService.Streak(gap, EveryWeekday, saturday, 10, 120));
    }

    [Fact]
    public async Task Ende_BuchtTrophaeenNachServerZaehlung_GemeldeteWelleNurSoWeitDieZeitTraegt()
    {
        Reset();
        var (child, run) = await StartAsync();
        await SolveAsync(child, run, 3);
        SetPlayedSeconds(run.RunId, 100);

        var end = await EndAsync(child, run, wave: 50);

        // Red if the reported wave were trusted: 49 cleared waves; the play time covers wave 6.
        Assert.Equal(6, end.Wave);
        // Three never met words (value 3 each) plus three of the five cleared waves - no more waves than words.
        Assert.Equal(12, end.TrophiesAwarded);
        Assert.Equal(12, end.Trophies);
        Assert.Equal(12, Db(db => db.TowerGameRuns.Single(r => r.Id == run.RunId).Trophies));

        // Red if a second end booked again: the total would double.
        var again = await EndAsync(child, run, wave: 50);
        Assert.Equal(12, again.Trophies);
        Assert.Equal(12, again.TrophiesAwarded);
        Assert.Equal(6, again.Wave);
    }

    [Fact]
    public async Task Ende_OhneKoerper_BeendetDenLauf_UndZaehltNurDieWoerter()
    {
        Reset();
        var (child, run) = await StartAsync();
        await SolveAsync(child, run, 2);
        SetPlayedSeconds(run.RunId, 300);

        var res = await child.PostAsync($"{Base()}/runs/{run.RunId}/end", null);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var end = (await res.Content.ReadFromJsonAsync<TowerRunEndResult>())!;
        Assert.Equal(0, end.Wave);
        Assert.Equal(6, end.TrophiesAwarded);
    }

    [Fact]
    public async Task TrophaeenPfad_ZahltJedeStufeEinmalInGems()
    {
        Reset();
        var (child, first) = await StartAsync();
        await SolveAsync(child, first, 6);
        SetPlayedSeconds(first.RunId, 100);

        // 18 for the words plus 5 cleared waves: 23 trophies pass the first step (20 trophies, 5 gems).
        var end = await EndAsync(child, first, wave: 6);
        Assert.Equal(23, end.Trophies);
        Assert.Equal(5, end.GemsAwarded);
        Assert.Equal(5, Ledger(PointKind.TowerTrophyGems));
        // The repeated end reports the same gems and books nothing.
        Assert.Equal(5, (await EndAsync(child, first, wave: 6)).GemsAwarded);
        Assert.Equal(5, Ledger(PointKind.TowerTrophyGems));

        // A second run stays between the steps: red if the first step were paid again.
        var (_, second) = await StartAsync();
        await SolveAsync(child, second, 6);
        var next = await EndAsync(child, second, wave: 1);
        Assert.Equal(41, next.Trophies);
        Assert.Equal(0, next.GemsAwarded);
        Assert.Equal(5, Ledger(PointKind.TowerTrophyGems));

        // A third run passes the second step (60 trophies, 10 gems).
        var (_, third) = await StartAsync();
        await SolveAsync(child, third, 6);
        SetPlayedSeconds(third.RunId, 100);
        var last = await EndAsync(child, third, wave: 6);
        Assert.Equal(64, last.Trophies);
        Assert.Equal(10, last.GemsAwarded);
        Assert.Equal(15, Ledger(PointKind.TowerTrophyGems));

        var progress = (await child.GetFromJsonAsync<TowerGameOverview>(Base()))!.Progress;
        Assert.Equal(64, progress.Trophies);
        Assert.Equal([20, 60, 120, 250, 500, 1000, 2000], progress.TrophyPath.Select(s => s.Trophies));
        Assert.Equal([true, true, false], progress.TrophyPath.Take(3).Select(s => s.Reached));
    }

    [Fact]
    public async Task SpielAufgaben_WerdenAmLaufendeUeberDieMissionenAbgerechnet()
    {
        Reset();
        string[] Awarded() => Db(db => db.MissionAwards.Where(a => a.Mission!.ChildId == 1)
            .Select(a => a.Mission!.Metric).ToList()).Select(m => m.ToString()).Order().ToArray();
        // The seeded family carries the three tower tasks.
        Assert.Equal(3, Db(db => db.Missions.Count(m => m.ChildId == 1 && (m.Metric == ProgressMetric.TowerWordsCorrect
            || m.Metric == ProgressMetric.TowerBestWave || m.Metric == ProgressMetric.TowerNewWordsCorrect))));

        // Two new words and wave 4: no task is met.
        var (child, run) = await StartAsync();
        await SolveAsync(child, run, 2);
        SetPlayedSeconds(run.RunId, 60);
        await EndAsync(child, run, wave: 9);
        Assert.Empty(Awarded());
        Assert.Equal(0, Ledger(PointKind.Mission));

        // Three new words and wave 5: both tasks pay, the 20 words do not (5 so far).
        var (_, second) = await StartAsync();
        await SolveAsync(child, second, 3);
        SetPlayedSeconds(second.RunId, 80);
        await EndAsync(child, second, wave: 9);
        Assert.Equal(["TowerBestWave", "TowerNewWordsCorrect"], Awarded());
        Assert.Equal(20, Ledger(PointKind.Mission));

        // 15 more correct assignments make 20: the third task pays, the other two not again.
        var (_, third) = await StartAsync();
        await HitAsync(child, third, 15);
        await EndAsync(child, third, wave: 1);
        Assert.Equal(["TowerBestWave", "TowerNewWordsCorrect", "TowerWordsCorrect"], Awarded());
        Assert.Equal(30, Ledger(PointKind.Mission));
    }

    [Fact]
    public async Task NeueWoerterAufgabe_ZaehltKeineGutGelerntenWoerter()
    {
        Reset();
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, Words);
        var (_, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, stage: 1);
        var child = await TestApi.ChildAsync(factory);
        // Every word of the exercise is well learned (top box): its tower value is 1, not 3.
        Db(db =>
        {
            foreach (var item in db.ExerciseItems.Where(i => i.ExerciseId == exerciseId).ToList())
                db.ItemProgress.Add(new ItemProgress
                {
                    ChildId = 1,
                    ItemId = item.Id,
                    ExerciseId = exerciseId,
                    VocabularyId = item.VocabularyId,
                    Box = ItemProgress.MaxBox,
                });
            return db.SaveChanges();
        });
        var run = (await (await child.PostAsJsonAsync($"{Base()}/runs", new StartTowerRunDto(positionId)))
            .Content.ReadFromJsonAsync<TowerRunResponse>())!;
        await SolveAsync(child, run, 6);

        var end = await EndAsync(child, run, wave: 1);

        // Six well learned words earn one trophy each.
        Assert.Equal(6, end.TrophiesAwarded);
        // Red if every solved word counted as new: the task "3 new words" would pay.
        Assert.False(Db(db => db.MissionAwards.Any(a => a.Mission!.ChildId == 1
            && a.Mission.Metric == ProgressMetric.TowerNewWordsCorrect)));
    }

    [Fact]
    public async Task SpielTruhe_OeffnetAbFuenfzehnRichtigen_EinmalAmTag_NurDasKindSelbst()
    {
        Reset();
        var (child, run) = await StartAsync();
        await HitAsync(child, run, 14);

        // Red if the chest opened before it is full.
        var early = await child.PostAsync($"{Base()}/chest/open", null);
        Assert.Equal(HttpStatusCode.Conflict, early.StatusCode);
        Assert.Contains("tower_game_chest_none", await early.Content.ReadAsStringAsync());
        var before = (await child.GetFromJsonAsync<TowerGameOverview>(Base()))!.Progress.Chest;
        Assert.Equal((14, 15, false, false), (before.Words, before.WordsNeeded, before.Ready, before.OpenedToday));

        await HitAsync(child, run, 1);
        Assert.True((await child.GetFromJsonAsync<TowerGameOverview>(Base()))!.Progress.Chest.Ready);

        // The father may read the overview but not open the child's chest.
        var father = await TestApi.AdultAsync(factory);
        Assert.Equal(HttpStatusCode.Forbidden, (await father.PostAsync($"{Base()}/chest/open", null)).StatusCode);
        Assert.Equal(0, Ledger(PointKind.TowerChestGems));

        var opened = await (await child.PostAsync($"{Base()}/chest/open", null)).Content.ReadFromJsonAsync<TowerGameChestOpened>();
        Assert.InRange(opened!.GemsAwarded, 3, 8);
        Assert.Equal(opened.GemsAwarded, Ledger(PointKind.TowerChestGems));
        Assert.True(opened.Chest.OpenedToday);
        Assert.False(opened.Chest.Ready);

        // Red if the chest opened twice a day: the ledger would grow.
        Assert.Equal(HttpStatusCode.Conflict, (await child.PostAsync($"{Base()}/chest/open", null)).StatusCode);
        Assert.Equal(opened.GemsAwarded, Ledger(PointKind.TowerChestGems));
        var after = (await child.GetFromJsonAsync<TowerGameOverview>(Base()))!.Progress.Chest;
        Assert.True(after.OpenedToday);
        Assert.False(after.Ready);
    }

    [Fact]
    public async Task SpielSerie_ZaehltHeuteAbZehnRichtigen()
    {
        Reset();
        var (child, run) = await StartAsync();
        await HitAsync(child, run, 9);
        // A wrong assignment is an answer too, but no correct word: red if it counted as the tenth.
        var wrong = await (await child.PostAsJsonAsync($"{Base()}/assign", new AssignTowerTokenDto(run.RunId,
            run.Tokens.Single(t => t.Word == "cat").TokenId, run.Towers.Single(t => t.Prompt == "Baum").TowerId)))
            .Content.ReadFromJsonAsync<TowerAssignResult>();
        Assert.False(wrong!.Correct);
        var open = (await child.GetFromJsonAsync<TowerGameOverview>(Base()))!.Progress;
        Assert.Equal((0, 9, 10), (open.Streak, open.WordsToday, open.StreakWords));

        await HitAsync(child, run, 1);

        var met = (await child.GetFromJsonAsync<TowerGameOverview>(Base()))!.Progress;
        Assert.Equal((1, 10), (met.Streak, met.WordsToday));
    }

    [Fact]
    public async Task Run_FriertDenWertJedesWortesEin()
    {
        Reset();
        var (_, run) = await StartAsync();

        // Red if the value were not stored: the trophies of the run would have nothing to add up.
        Assert.Equal([3, 3, 3, 3, 3, 3], Db(db => db.TowerGameRunItems.Where(i => i.RunId == run.RunId).Select(i => i.Value).ToList()));
    }
}
