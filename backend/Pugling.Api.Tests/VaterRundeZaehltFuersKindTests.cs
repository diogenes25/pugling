using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-542 (decided: stays as it is): a round the supervisor plays in the child's position is the documented catch-up
/// ("Nachtrag") - a graded hit writes the child's history, its plan-wide item progress and the points, whether the round
/// is backdated or today's. Trying an exercise without side effects is the preview (`creator/exercises/{id}/preview`).
/// The anti-cheat walls face the child, not the parent; only the first-contact stamp stays child-only (B-433).
/// </summary>
public class VaterRundeZaehltFuersKindTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    /// <summary>
    /// AK 1 (backdated) and AK 2 (today): one typed hit by the father on an introduced, due card. Red when a role check
    /// (<c>User.IsStudent()</c>) is put around <c>itemProgress.RecordAsync</c> (no history, no item progress) or around
    /// the points booking (no <c>Base</c> entry), for either day.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task Gewertete_Vaterrunde_Schreibt_Verlauf_Lernstand_Und_Punkte_Des_Kindes(int dayOffset)
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "5" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Nachtrag-Kind"), pin }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father); // hello→hallo, goodbye→tschüss
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText,
            childId: childId, orderStrategy: PracticeOrder.Serial);
        var today = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);
        using (var scope = factory.Services.CreateScope())
        {
            // The plan starts yesterday, so a backdated round lies inside it.
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            db.StudyPlans.Single(p => p.Id == planId).StartDate = today.AddDays(-1);
            db.SaveChanges();
        }

        var start = await father.PostAsJsonAsync(TestApi.PracticeBase(planId, positionId),
            new { day = today.AddDays(dayOffset) });
        Assert.True(start.IsSuccessStatusCode, await start.Content.ReadAsStringAsync());
        var sessionId = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var review = await TestApi.PositionReviewAsync(father, planId, positionId, sessionId, 0, givenAnswer: "hello");
        Assert.True(review.IsSuccessStatusCode, await review.Content.ReadAsStringAsync());
        Assert.True((await review.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("wasCorrect").GetBoolean());

        using var check = factory.Services.CreateScope();
        var read = check.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var history = read.ItemReviewEvents.Where(e => e.ChildId == childId && e.PlanPositionId == positionId).ToList();
        Assert.Single(history);
        Assert.True(history[0].WasCorrect);
        Assert.Equal(1, read.ItemProgress.Single(p => p.ChildId == childId && p.ExerciseId == exerciseId
            && p.ItemId == history[0].ItemId).CorrectCount);
        Assert.True(read.ChildPointsEntries.Any(e => e.ChildId == childId && e.Kind == PointKind.Base && e.Amount > 0));
    }
}
