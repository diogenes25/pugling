using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// "Vokabel-Türme" (B-851 core): runs from a position's words with opaque ids, server-side grading of every
/// assignment, rejection of ids from another run context, and the dampened Leitner step (first hit per word and day).
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
            new AssignTowerTokenDto(positionId, TokenOf(run, "dog"), TowerOf(run, "Hund"))))
            .Content.ReadFromJsonAsync<TowerAssignResult>();
        Assert.True(right!.Correct);

        var wrong = await (await child.PostAsJsonAsync($"{Base()}/assign",
            new AssignTowerTokenDto(positionId, TokenOf(run, "cat"), TowerOf(run, "Baum"))))
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
    public async Task Assign_MitIdAusAndererPositionOderGefaelscht_Liefert404()
    {
        var (child, positionId, run) = await StartAsync();
        var (_, otherPositionId, otherRun) = await StartAsync();

        // Red if the position bound into the id were not checked: a tower of another run would then be gradable here.
        var foreign = await child.PostAsJsonAsync($"{Base()}/assign",
            new AssignTowerTokenDto(positionId, TokenOf(run, "dog"), TowerOf(otherRun, "Hund")));
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);

        var forged = await child.PostAsJsonAsync($"{Base()}/assign",
            new AssignTowerTokenDto(otherPositionId, "forged", TowerOf(otherRun, "Hund")));
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
                new AssignTowerTokenDto(positionId, TokenOf(run, "dog"), TowerOf(run, "Hund")));

        // Red if every hit counted for mastery: three hits would lift the box to 4.
        Assert.Equal(2, BoxOfDog());
    }
}
