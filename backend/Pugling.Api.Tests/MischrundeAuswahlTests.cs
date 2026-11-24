using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// The mixed review's draw (B-764): which cards a round takes across the child's positions, and how a round is started,
/// resumed and kept apart from the duty.
/// </summary>
public class MischrundeAuswahlTests(PuglingWebAppFactory factory) : MischrundeTestBase(factory), IClassFixture<PuglingWebAppFactory>
{
    /// <summary>
    /// AK 1 and AK 2: one due card in each of two positions of two active plans - the round offers both, and none of the
    /// six cards due in three days. Red if the draw stays within one position, or filters by something else than the due
    /// date (the six future cards would join).
    /// </summary>
    [Fact]
    public async Task Runde_Nimmt_Faellige_Karten_Aus_Zwei_Plaenen_Und_Keine_Kuenftige()
    {
        var k = await KidAsync();
        var (_, posA) = await PositionAsync(k);
        var (_, posB) = await PositionAsync(k);
        SetDue(posA, 1, Today);
        SetDue(posB, 2, Today.AddDays(-2));

        var run = await StartAsync(k);

        // Most overdue first: B's card is two days late, A's due today.
        Assert.Equal([(posB, 2), (posA, 1)], Cards(run));
        var overview = await (await k.Child.GetAsync(Base(k))).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, overview.GetProperty("dueCards").GetInt32());
        Assert.Equal(run.GetProperty("runId").GetInt32(), overview.GetProperty("openRunId").GetInt32());
    }

    /// <summary>
    /// AK 3: a position with an open weekly goal gives no card, even a due one, and its goal stays open after the start;
    /// a position whose daily goal is met gives its due card. Red if the draw does not leave out positions with an open
    /// duty - the round would empty the weekly position's next learn round, which then counts as met.
    /// </summary>
    [Fact]
    public async Task Position_Mit_Offener_Pflicht_Gibt_Keine_Karte()
    {
        var k = await KidAsync();
        var (planDaily, posDaily) = await PositionAsync(k, GoalCadence.Daily);
        var (_, posWeekly) = await PositionAsync(k, GoalCadence.Weekly);
        SeedPassedTest(planDaily, posDaily);
        SetDue(posDaily, 0, Today);
        SetDue(posWeekly, 0, Today.AddDays(-5));

        var run = await StartAsync(k);

        Assert.Equal([(posDaily, 0)], Cards(run));
        using var scope = Factory.Services.CreateScope();
        var progress = scope.ServiceProvider.GetRequiredService<PositionProgressService>();
        var day = await progress.ComputeChildDayAsync(k.ChildId, Today, CancellationToken.None);
        Assert.False(day.Positions.Single(p => p.PositionId == posWeekly).GoalMet);
    }

    /// <summary>
    /// AK 4: another child's seat is turned away (404, like an unknown child), the supervisor reads the overview but may not start a round. Red if the
    /// route hangs without the ownership filter or the start without the student check.
    /// </summary>
    [Fact]
    public async Task Fremdes_Kind_Verboten_Vater_Liest_Nur()
    {
        var k = await KidAsync();
        var (_, pos) = await PositionAsync(k);
        SetDue(pos, 0, Today);
        var other = await KidAsync();

        // The ownership filter answers a foreign child like an unknown one.
        Assert.Equal(HttpStatusCode.NotFound, (await other.Child.PostAsync($"{Base(k)}/runs", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.Child.GetAsync(Base(k))).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await k.Father.GetAsync(Base(k))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await k.Father.PostAsync($"{Base(k)}/runs", null)).StatusCode);
    }

    /// <summary>
    /// AK 5: an inactive plan and an expired one give no card, beside an active plan that does. Red if the draw reads
    /// plans that are not playable for the child.
    /// </summary>
    [Fact]
    public async Task Inaktiver_Und_Abgelaufener_Plan_Geben_Keine_Karte()
    {
        var k = await KidAsync();
        var (_, posActive) = await PositionAsync(k);
        var (planOff, posOff) = await PositionAsync(k);
        var (planOld, posOld) = await PositionAsync(k);
        SetDue(posActive, 0, Today);
        SetDue(posOff, 0, Today);
        SetDue(posOld, 0, Today);
        Db(db =>
        {
            db.StudyPlans.Single(p => p.Id == planOff).Active = false;
            var old = db.StudyPlans.Single(p => p.Id == planOld);
            old.StartDate = Today.AddDays(-10);
            old.EndDate = Today.AddDays(-1);
        });

        var run = await StartAsync(k);

        Assert.Equal([(posActive, 0)], Cards(run));
    }

    /// <summary>
    /// AK 5 at the draw itself: the day view lists only running plans today, so the HTTP case above holds even without the
    /// draw's own check. This case hands the draw a day view that carries a switched-off plan beside the running one -
    /// red if the draw stops checking that a plan is playable for the child.
    /// </summary>
    [Fact]
    public async Task Auswahl_Prueft_Selbst_Ob_Der_Plan_Spielbar_Ist()
    {
        var k = await KidAsync();
        var (_, posActive) = await PositionAsync(k);
        var (planOff, posOff) = await PositionAsync(k);
        SetDue(posActive, 0, Today);
        SetDue(posOff, 0, Today);
        Db(db => db.StudyPlans.Single(p => p.Id == planOff).Active = false);

        using var scope = Factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var day = await services.GetRequiredService<PositionProgressService>()
            .ComputeChildDayAsync(k.ChildId, Today, CancellationToken.None);
        var off = services.GetRequiredService<PuglingDbContext>().StudyPlans.Single(p => p.Id == planOff);
        var widened = day with { Plans = [.. day.Plans, new ChildPlanDay(off, day.Plans[0].Overview)] };

        var picked = await services.GetRequiredService<MixedReviewService>().SelectAsync(widened, Today, CancellationToken.None);

        Assert.Equal([(posActive, 0)], picked.Select(c => (c.Position.Id, c.ItemIndex)));
    }

    /// <summary>
    /// AK 6 (decision 4): a card the child has never met gives no card - neither one without a progress row, nor a row
    /// without an introduction, nor a word introduced on its learn card but not imprinted (B-643). Red if the draw takes
    /// the play path's "never seen counts as due" unchanged, or drops the imprint check.
    /// </summary>
    [Fact]
    public async Task Nie_Gesehene_Karte_Kommt_Nicht_Vor()
    {
        var k = await KidAsync();
        var (_, pos) = await PositionAsync(k);
        SetDue(pos, 0, Today);
        Db(db =>
        {
            var rows = db.PositionItemProgress.Where(p => p.PlanPositionId == pos).ToDictionary(p => p.ItemIndex);
            // Card 1: a row without an introduction (due); card 2: introduced but not imprinted (due); card 3: no row.
            rows[1].IntroducedAt = null;
            rows[1].ImprintedAt = null;
            rows[1].DueOn = null;
            rows[2].ImprintedAt = null;
            rows[2].DueOn = Today;
            db.PositionItemProgress.Remove(rows[3]);
        });

        var run = await StartAsync(k);

        Assert.Equal([(pos, 0)], Cards(run));
    }

    /// <summary>
    /// AK 7: before the goals the bonus training waits for, the start is locked (<c>extra_training_locked</c>); once they
    /// are met, it starts. Red if the round bypasses the lock.
    /// </summary>
    [Fact]
    public async Task Vor_Den_Tageszielen_Gesperrt_Danach_Offen()
    {
        var k = await KidAsync();
        var (plan, pos) = await PositionAsync(k, GoalCadence.Daily);
        var (_, free) = await PositionAsync(k);
        SetDue(free, 0, Today);

        await AssertProblemAsync(await k.Child.PostAsync($"{Base(k)}/runs", null), HttpStatusCode.Conflict, "extra_training_locked");
        var locked = await (await k.Child.GetAsync(Base(k))).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(locked.GetProperty("unlocked").GetBoolean());
        Assert.Equal("duty_open", locked.GetProperty("lockReason").GetString());

        SeedPassedTest(plan, pos);
        Assert.Equal([(free, 0)], Cards(await StartAsync(k)));
    }

    /// <summary>
    /// AK 8: an open round is resumed, not drawn again - a second start (and two at once, a double tap) answers the same
    /// round with the same cards. Red if every start draws a new round.
    /// </summary>
    [Fact]
    public async Task Offene_Runde_Wird_Fortgesetzt()
    {
        var k = await KidAsync();
        var (_, pos) = await PositionAsync(k);
        SetDue(pos, 0, Today);
        SetDue(pos, 1, Today);

        var first = await StartAsync(k);
        var again = await Task.WhenAll(StartAsync(k), StartAsync(k));

        var cardIds = first.GetProperty("cards").EnumerateArray().Select(c => c.GetProperty("cardId").GetInt32()).ToList();
        foreach (var run in again)
        {
            Assert.Equal(first.GetProperty("runId").GetInt32(), run.GetProperty("runId").GetInt32());
            Assert.Equal(cardIds, run.GetProperty("cards").EnumerateArray().Select(c => c.GetProperty("cardId").GetInt32()));
        }
        Assert.Equal(1, Read(db => db.MixedReviewRuns.Count(r => r.ChildId == k.ChildId)));
    }

    /// <summary>
    /// A round left open from an earlier day is drawn afresh: its cards were due then. Red if a start resumes a round of
    /// another day.
    /// </summary>
    [Fact]
    public async Task Offene_Runde_Von_Gestern_Wird_Neu_Gezogen()
    {
        var k = await KidAsync();
        var (_, pos) = await PositionAsync(k);
        SetDue(pos, 0, Today);
        var first = await StartAsync(k);
        Db(db => db.MixedReviewRuns.Single(r => r.Id == first.GetProperty("runId").GetInt32()).Day = Today.AddDays(-1));

        var fresh = await StartAsync(k);

        Assert.NotEqual(first.GetProperty("runId").GetInt32(), fresh.GetProperty("runId").GetInt32());
        Assert.Equal(1, Read(db => db.MixedReviewRuns.Count(r => r.ChildId == k.ChildId)));
    }

    /// <summary>
    /// AK 9 (decision 3): the start creates no practice session. Red if it does - a learn session would count in the
    /// duty evaluation.
    /// </summary>
    [Fact]
    public async Task Start_Legt_Keine_Lernsitzung_An()
    {
        var k = await KidAsync();
        var (plan, pos) = await PositionAsync(k);
        SetDue(pos, 0, Today);

        await StartAsync(k);

        Assert.Equal(0, Read(db => db.PracticeSessions.Count(s => s.StudyPlanId == plan)));
    }

    /// <summary>
    /// Risk 3: a word two positions share is asked once, from the more overdue position; the other one's card stays due.
    /// Red if the draw does not deduplicate by exercise and item.
    /// </summary>
    [Fact]
    public async Task Dasselbe_Wort_In_Zwei_Positionen_Kommt_Einmal()
    {
        var k = await KidAsync();
        var (_, posA) = await PositionAsync(k);
        var exerciseId = Read(db => db.PlanPositions.Single(p => p.Id == posA).ExerciseId);
        var (_, posB) = await PositionAsync(k, exerciseId: exerciseId);
        SetDue(posA, 0, Today);
        SetDue(posB, 0, Today.AddDays(-1));

        var run = await StartAsync(k);

        Assert.Equal([(posB, 0)], Cards(run));
    }

    /// <summary>Nothing due anywhere: the start answers <c>mixed_review_empty</c> and the overview counts 0 cards.</summary>
    [Fact]
    public async Task Nichts_Faellig_Leere_Runde_Wird_Abgewiesen()
    {
        var k = await KidAsync();
        await PositionAsync(k);

        await AssertProblemAsync(await k.Child.PostAsync($"{Base(k)}/runs", null), HttpStatusCode.Conflict, "mixed_review_empty");
        var overview = await (await k.Child.GetAsync(Base(k))).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, overview.GetProperty("dueCards").GetInt32());
        Assert.True(overview.GetProperty("unlocked").GetBoolean());
    }
}
