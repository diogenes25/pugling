using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// "Vokabel-Türme" (B-851): runs from a position's words with opaque ids, server-side grading of every assignment,
/// rejection of ids from another run, the dampened Leitner step (first hit per word and day), coins for a won level by
/// the server's count under the daily cap, the clamped heartbeat and the per-child minute limit with grace time.
/// </summary>
public class TowerGameTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static readonly (string Front, string Back)[] Words =
        [("dog", "Hund"), ("cat", "Katze"), ("house", "Haus"), ("tree", "Baum"), ("water", "Wasser"), ("bread", "Brot")];

    private static string Base(int childId = 1) => $"/api/v1/student/children/{childId}/tower-game";

    private async Task<(HttpClient child, int positionId, TowerRunResponse run)> StartAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father, Words);
        var (_, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, stage: 1);
        var child = await TestApi.ChildAsync(factory);
        var res = await child.PostAsJsonAsync($"{Base()}/runs", new StartTowerRunDto(positionId));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (child, positionId, (await res.Content.ReadFromJsonAsync<TowerRunResponse>())!);
    }

    private static string TokenOf(TowerRunResponse run, string word) => run.Tokens.Single(t => t.Word == word).TokenId;
    private static string TowerOf(TowerRunResponse run, string meaning) => run.Towers.Single(t => t.Prompt == meaning).TowerId;

    [Fact]
    public async Task Run_TraegtAlleWoerter_OhneGleicheIdsZwischenTurmUndToken()
    {
        var (child, positionId, run) = await StartAsync();

        Assert.Equal(Words.Length, run.Towers.Count);
        Assert.Equal(Words.Select(w => w.Back).Order(), run.Towers.Select(t => t.Prompt).Order());
        Assert.Equal(Words.Select(w => w.Front).Order(), run.Tokens.Select(t => t.Word).Order());
        // Red if ids were plain item ids: tower and token of the same word would then be matchable by equality.
        Assert.Empty(run.Towers.Select(t => t.TowerId).Intersect(run.Tokens.Select(t => t.TokenId)));

        var overview = await child.GetFromJsonAsync<TowerGameOverview>(Base());
        Assert.Contains(overview!.Positions, p => p.PlanPositionId == positionId && p.WordCount == Words.Length);
    }

    [Fact]
    public async Task Assign_BewertetRichtigUndFalsch_UndSchreibtLernstandMitQuelleGame()
    {
        var (child, positionId, run) = await StartAsync();

        var right = await (await child.PostAsJsonAsync($"{Base()}/assign",
            new AssignTowerTokenDto(run.RunId, TokenOf(run, "dog"), TowerOf(run, "Hund"))))
            .Content.ReadFromJsonAsync<TowerAssignResult>();
        Assert.True(right!.Correct);

        var wrong = await (await child.PostAsJsonAsync($"{Base()}/assign",
            new AssignTowerTokenDto(run.RunId, TokenOf(run, "cat"), TowerOf(run, "Baum"))))
            .Content.ReadFromJsonAsync<TowerAssignResult>();
        Assert.False(wrong!.Correct);
        Assert.Equal("tree", wrong.Expected);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var events = await db.ItemReviewEvents.Where(e => e.PlanPositionId == positionId).ToListAsync();
        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal(ItemReviewSource.Game, e.Source));
        Assert.Single(events, e => e.WasCorrect);
    }

    [Fact]
    public async Task Assign_MitIdAusAnderemRunOderGefaelscht_Liefert404()
    {
        var (child, _, run) = await StartAsync();
        var (_, _, otherRun) = await StartAsync();

        // Red if the run bound into the id were not checked: a tower of another run would then be gradable here.
        var foreign = await child.PostAsJsonAsync($"{Base()}/assign",
            new AssignTowerTokenDto(run.RunId, TokenOf(run, "dog"), TowerOf(otherRun, "Hund")));
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);

        var forged = await child.PostAsJsonAsync($"{Base()}/assign",
            new AssignTowerTokenDto(otherRun.RunId, "forged", TowerOf(otherRun, "Hund")));
        Assert.Equal(HttpStatusCode.NotFound, forged.StatusCode);
    }

    [Fact]
    public async Task Box_SteigtNurBeimErstenTrefferJeTag()
    {
        var (child, positionId, run) = await StartAsync();
        int BoxOfDog()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var itemId = db.ItemReviewEvents.Where(e => e.PlanPositionId == positionId).Select(e => e.ItemId).First();
            return db.ItemProgress.Single(p => p.ChildId == 1 && p.ItemId == itemId).Box;
        }

        for (var i = 0; i < 3; i++)
            await child.PostAsJsonAsync($"{Base()}/assign",
                new AssignTowerTokenDto(run.RunId, TokenOf(run, "dog"), TowerOf(run, "Hund")));

        // Red if every hit counted for mastery: three hits would lift the box to 4.
        Assert.Equal(2, BoxOfDog());
    }

    // The tests of this class share one child and one database: coin and time tests start from a clean slate.
    private void ResetTowerGame()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.TowerGameRuns.Where(r => r.ChildId == 1).ExecuteDelete();
        db.ChildPointsEntries.Where(e => e.ChildId == 1 && e.Kind == PointKind.TowerGameCoins).ExecuteDelete();
        db.Children.Where(c => c.Id == 1).ExecuteUpdate(c => c.SetProperty(x => x.TowerGameMinutesPerDay, (int?)null));
    }

    private void SetRun(int runId, Action<TowerGameRun> change)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        change(db.TowerGameRuns.Single(r => r.Id == runId));
        db.SaveChanges();
    }

    private static async Task SolveAllAsync(HttpClient child, TowerRunResponse run)
    {
        foreach (var (front, back) in Words)
            Assert.True((await (await child.PostAsJsonAsync($"{Base()}/assign",
                new AssignTowerTokenDto(run.RunId, TokenOf(run, front), TowerOf(run, back))))
                .Content.ReadFromJsonAsync<TowerAssignResult>())!.Correct);
    }

    private static async Task<TowerRunEndResult> EndAsync(HttpClient child, TowerRunResponse run)
    {
        var res = await child.PostAsync($"{Base()}/runs/{run.RunId}/end", null);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<TowerRunEndResult>())!;
    }

    private int LedgerCoins()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return db.ChildPointsEntries.Where(e => e.ChildId == 1 && e.Kind == PointKind.TowerGameCoins).Sum(e => e.Amount);
    }

    [Fact]
    public async Task Gewonnen_ZahltMuenzenBisZumDeckel_UndZweitesEndeZahltNicht()
    {
        ResetTowerGame();
        var results = new List<TowerRunEndResult>();
        for (var level = 0; level < 4; level++)
        {
            var (child, _, run) = await StartAsync();
            await SolveAllAsync(child, run);
            results.Add(await EndAsync(child, run));
            if (level == 0)
            {
                // Red if a second end booked again: the ledger would then hold 20 coins after one level.
                var again = await EndAsync(child, run);
                Assert.Equal(10, again.CoinsAwarded);
                Assert.Equal(10, LedgerCoins());
            }
        }

        Assert.All(results, r => Assert.True(r.Won));
        // Red without the cap: the fourth level would pay 10 more.
        Assert.Equal([10, 10, 10, 0], results.Select(r => r.CoinsAwarded));
        Assert.Equal(30, results[^1].CoinsToday);
        Assert.Equal(30, LedgerCoins());
    }

    [Fact]
    public async Task DerselbeTreffer_ZaehltNurEinmal_UndVerlorenZahltNichts()
    {
        ResetTowerGame();
        var (child, _, run) = await StartAsync();
        for (var i = 0; i < Words.Length; i++)
            await child.PostAsJsonAsync($"{Base()}/assign",
                new AssignTowerTokenDto(run.RunId, TokenOf(run, "dog"), TowerOf(run, "Hund")));

        var end = await EndAsync(child, run);

        // Red if the server counted hits instead of solved towers: six hits on one tower would win the level.
        Assert.False(end.Won);
        Assert.Equal(1, end.Solved);
        Assert.Equal(0, end.CoinsAwarded);
        Assert.Equal(0, LedgerCoins());
    }

    [Fact]
    public async Task Assign_NachDemEnde_Liefert409()
    {
        ResetTowerGame();
        var (child, _, run) = await StartAsync();
        await EndAsync(child, run);

        // Red if an ended run still graded: the assignment would return 200.
        var late = await child.PostAsJsonAsync($"{Base()}/assign",
            new AssignTowerTokenDto(run.RunId, TokenOf(run, "dog"), TowerOf(run, "Hund")));

        Assert.Equal(HttpStatusCode.Conflict, late.StatusCode);
        Assert.Contains("tower_game_run_closed", await late.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Heartbeat_ZaehltHoechstensDieKlammerJeSchlag()
    {
        ResetTowerGame();
        var (child, _, run) = await StartAsync();
        // The app was in the background for five minutes since the last beat.
        SetRun(run.RunId, r => r.LastBeatAt = r.LastBeatAt.AddMinutes(-5));

        var beat = await (await child.PostAsync($"{Base()}/runs/{run.RunId}/heartbeat", null))
            .Content.ReadFromJsonAsync<TowerHeartbeatResult>();

        // Red without the clamp: the five minutes would count as 300 seconds of play.
        Assert.Equal(20, beat!.PlayedSecondsToday);
        Assert.Equal(20 * 60, beat.LimitSeconds);
        Assert.False(beat.TimeUp);
    }

    [Fact]
    public async Task Minutenlimit_SperrtDenStart_LaesstDasLaufendeLevelNochDieNachspielzeit()
    {
        ResetTowerGame();
        try
        {
            var father = await TestApi.AdultAsync(factory);
            var patch = await father.PatchAsJsonAsync("/api/v1/supervisor/children/1", new { towerGameMinutesPerDay = 1 });
            Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

            var (child, positionId, run) = await StartAsync();
            SetRun(run.RunId, r => r.PlayedSeconds = 60);

            // Red without the limit check: a new level would start.
            var start = await child.PostAsJsonAsync($"{Base()}/runs", new StartTowerRunDto(positionId));
            Assert.Equal(HttpStatusCode.Conflict, start.StatusCode);
            Assert.Contains("tower_game_time_limit", await start.Content.ReadAsStringAsync());

            // Red if the limit closed the running level at once: within the grace time it is still graded.
            var inGrace = await child.PostAsJsonAsync($"{Base()}/assign",
                new AssignTowerTokenDto(run.RunId, TokenOf(run, "dog"), TowerOf(run, "Hund")));
            Assert.Equal(HttpStatusCode.OK, inGrace.StatusCode);

            // Red if the grace time were not bounded: after five more minutes the level must close.
            SetRun(run.RunId, r => r.PlayedSeconds = 60 + 5 * 60);
            var beat = await (await child.PostAsync($"{Base()}/runs/{run.RunId}/heartbeat", null))
                .Content.ReadFromJsonAsync<TowerHeartbeatResult>();
            Assert.True(beat!.TimeUp);
            var late = await child.PostAsJsonAsync($"{Base()}/assign",
                new AssignTowerTokenDto(run.RunId, TokenOf(run, "cat"), TowerOf(run, "Katze")));
            Assert.Equal(HttpStatusCode.Conflict, late.StatusCode);
        }
        finally
        {
            ResetTowerGame();
        }
    }
}
