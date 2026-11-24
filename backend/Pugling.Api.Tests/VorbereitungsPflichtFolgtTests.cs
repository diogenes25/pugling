using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-758: the preparation duty follows its class test - moved, window changed, cancelled, written or deleted - right after
/// the supervisor's change and again on the child's next login. Closing uses the rule of B-659: a day already met counts,
/// an open one does not, and nothing is owed afterwards. Same frozen Monday as <see cref="VorbereitungsPflichtTests"/>.
/// </summary>
public class VorbereitungsPflichtFolgtTests : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory factory;

    private static readonly DateTimeOffset Start = new(2030, 3, 4, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly D = DateOnly.FromDateTime(Start.UtcDateTime);

    public VorbereitungsPflichtFolgtTests(PuglingWebAppFactory factory)
    {
        this.factory = factory;
        factory.Clock.FreezeAt(Start);
    }

    private sealed record Kind(HttpClient Father, int ChildId, string Pin, int SubjectId, int PlanId);

    private async Task<Kind> KindAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "7" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Folge-Kind"), pin }));
        var subjectId = await TestApi.DefaultSubjectIdAsync(father);
        var planId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/study-plans",
            new { childId, title = "Englisch-Plan", subjectId, durationDays = 30 }));
        return new Kind(father, childId, pin, subjectId, planId);
    }

    private async Task<int> ArbeitAsync(Kind k, DateOnly date, int exerciseId)
    {
        var res = await k.Father.PostAsJsonAsync("/api/v1/supervisor/class-tests", new
        {
            childId = k.ChildId,
            title = "Vokabeltest",
            scheduledDate = date.ToString("yyyy-MM-dd"),
            subjectId = k.SubjectId,
            exerciseIds = new[] { exerciseId },
        });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("klassenarbeit").GetProperty("id").GetInt32();
    }

    /// <summary>A started duty: one vocabulary exercise, a test on <paramref name="date"/>, the child logged in once.</summary>
    private async Task<(Kind Kind, int Arbeit, int Exercise)> GestartetAsync(DateOnly date)
    {
        var k = await KindAsync();
        var ex = await TestApi.CreateVocabExerciseAsync(k.Father);
        var arbeit = await ArbeitAsync(k, date, ex);
        await LoginAsync(k);
        return (k, arbeit, ex);
    }

    private Task<HttpClient> LoginAsync(Kind k) => TestApi.ChildAsync(factory, k.ChildId, k.Pin);

    private Task<HttpResponseMessage> AendernAsync(Kind k, int arbeit, object patch) =>
        k.Father.PatchAsJsonAsync($"/api/v1/supervisor/class-tests/{arbeit}", patch);

    private void Db(Action<PuglingDbContext> act)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        act(db);
        db.SaveChanges();
    }

    private T Read<T>(Func<PuglingDbContext, T> read)
    {
        using var scope = factory.Services.CreateScope();
        return read(scope.ServiceProvider.GetRequiredService<PuglingDbContext>());
    }

    private PlanPosition Position(int planId) => Read(db => db.PlanPositions.Single(p => p.StudyPlanId == planId));

    /// <summary>A test attempt on <paramref name="day"/> - passed meets the day's goal, unpassed only starts the penalty clock (B-535).</summary>
    private void Versuch(int planId, int positionId, DateOnly day, bool passed) => Db(db => db.TestAttempts.Add(new TestAttempt
    {
        StudyPlanId = planId,
        PlanPositionId = positionId,
        Day = day,
        Passed = passed,
        Graded = true,
        StartedAt = day.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc),
        CompletedAt = passed ? day.ToDateTime(new TimeOnly(10, 5), DateTimeKind.Utc) : null,
    }));

    private int Strafen(int childId) => Read(db => db.ChildPointsEntries
        .Where(e => e.ChildId == childId && e.Kind == PointKind.GoalPenalty).Sum(e => (int?)e.Amount) ?? 0);

    [Fact]
    public async Task Verschobener_Termin_Zieht_Das_Ende_Sofort_Nach()
    {
        var (k, arbeit, _) = await GestartetAsync(D.AddDays(3));
        Assert.Equal(D.AddDays(2), Position(k.PlanId).DutyUntil);

        (await AendernAsync(k, arbeit, new { scheduledDate = D.AddDays(5).ToString("yyyy-MM-dd") })).EnsureSuccessStatusCode();

        Assert.Equal(D.AddDays(4), Position(k.PlanId).DutyUntil); // synchronously after the PATCH, no login in between
    }

    [Fact]
    public async Task Aus_Dem_Fenster_Geschoben_Pausiert_Und_Beginnt_Beim_Login_Im_Neuen_Fenster_Neu()
    {
        var (k, arbeit, _) = await GestartetAsync(D.AddDays(3));
        (await AendernAsync(k, arbeit, new { scheduledDate = D.AddDays(20).ToString("yyyy-MM-dd") })).EnsureSuccessStatusCode();
        Assert.Equal(D.AddDays(-1), Position(k.PlanId).DutyUntil); // today open: today does not count

        factory.Clock.Advance(TimeSpan.FromDays(13)); // the window of D+20 begins on D+13
        await LoginAsync(k);

        var pos = Position(k.PlanId);
        Assert.Equal(D.AddDays(19), pos.DutyUntil);
        Assert.Equal(D.AddDays(13), pos.DutySince); // the paused days cannot have been missed
    }

    [Fact]
    public async Task Fenster_Null_Schliesst_Die_Pflicht()
    {
        var (k, arbeit, _) = await GestartetAsync(D.AddDays(3));

        (await AendernAsync(k, arbeit, new { prepWindowDays = 0 })).EnsureSuccessStatusCode();

        Assert.Equal(D.AddDays(-1), Position(k.PlanId).DutyUntil);
        Assert.Single(Read(db => db.PlanPositions.Where(p => p.StudyPlanId == k.PlanId).ToList()));
    }

    [Theory]
    [InlineData("Cancelled", true)]
    [InlineData("Cancelled", false)]
    [InlineData("Written", true)]
    [InlineData("Written", false)]
    public async Task Abgesagt_Oder_Geschrieben_Schliesst_Nach_Der_Regel_Von_B659(string status, bool heuteErfuellt)
    {
        var (k, arbeit, _) = await GestartetAsync(D.AddDays(3));
        var pos = Position(k.PlanId);
        Versuch(k.PlanId, pos.Id, D, passed: heuteErfuellt);

        (await AendernAsync(k, arbeit, new { status })).EnsureSuccessStatusCode();

        // Met today: today counts and closes the duty; open: the duty ended yesterday.
        Assert.Equal(heuteErfuellt ? D : D.AddDays(-1), Position(k.PlanId).DutyUntil);
        factory.Clock.Advance(TimeSpan.FromDays(3));
        await LoginAsync(k);
        Assert.Equal(0, Strafen(k.ChildId)); // neither today (open) nor the days after are owed
    }

    [Fact]
    public async Task Zweite_Arbeit_Mit_Derselben_Uebung_Haelt_Die_Pflicht()
    {
        var k = await KindAsync();
        var ex = await TestApi.CreateVocabExerciseAsync(k.Father);
        var erste = await ArbeitAsync(k, D.AddDays(3), ex);
        await ArbeitAsync(k, D.AddDays(5), ex);
        await LoginAsync(k);
        Assert.Equal(D.AddDays(4), Position(k.PlanId).DutyUntil);

        (await AendernAsync(k, erste, new { status = "Cancelled" })).EnsureSuccessStatusCode();

        Assert.Equal(D.AddDays(4), Position(k.PlanId).DutyUntil);
    }

    [Fact]
    public async Task Geloeschte_Arbeit_Schliesst_Die_Pflicht_Wie_Eine_Abgesagte()
    {
        var (k, arbeit, _) = await GestartetAsync(D.AddDays(3));

        (await k.Father.DeleteAsync($"/api/v1/supervisor/class-tests/{arbeit}")).EnsureSuccessStatusCode();

        var pos = Position(k.PlanId);
        Assert.Equal(D.AddDays(-1), pos.DutyUntil);
        Assert.Null(pos.SourceKlassenarbeitId);
    }

    /// <summary>
    /// AK 5, an intent rather than a probe: a re-save without a change of date, window or status moves nothing. The reconcile is
    /// idempotent, so this case stays green even if the PATCH reconciled on every save - it pins the outcome, not the guard.
    /// </summary>
    [Fact]
    public async Task Speichern_Ohne_Aenderung_Verschiebt_Nichts()
    {
        var (k, arbeit, _) = await GestartetAsync(D.AddDays(3));
        var vorher = Position(k.PlanId);

        (await AendernAsync(k, arbeit, new { title = "Vokabeltest Unit 2", status = "Planned" })).EnsureSuccessStatusCode();

        var nachher = Position(k.PlanId);
        Assert.Equal(vorher.DutyUntil, nachher.DutyUntil);
        Assert.Equal(vorher.DutySince, nachher.DutySince);
    }
}
