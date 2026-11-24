using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-752: in the window before a planned class test, every exercise of its bundle becomes a daily duty in the child's
/// active plan of the test's subject - created on the child's login, owing from then on, ending the day before the test,
/// with one daily penalty per test spread over its positions and no goal reward. The clock is frozen on a Monday so the
/// days are inputs, and every case uses a fresh child.
/// </summary>
public class VorbereitungsPflichtTests : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory factory;

    /// <summary>A Monday, 10:00 UTC - every case starts here and advances the clock itself.</summary>
    private static readonly DateTimeOffset Start = new(2030, 3, 4, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly D = DateOnly.FromDateTime(Start.UtcDateTime);

    public VorbereitungsPflichtTests(PuglingWebAppFactory factory)
    {
        this.factory = factory;
        factory.Clock.FreezeAt(Start);
    }

    private sealed record Kind(HttpClient Father, int ChildId, string Pin, int SubjectId, int PlanId);

    /// <summary>A fresh child with an active, long-running plan in the default subject (Englisch).</summary>
    private async Task<Kind> KindAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "7" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Vorbereitungs-Kind"), pin }));
        var subjectId = await TestApi.DefaultSubjectIdAsync(father);
        var planId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/study-plans",
            new { childId, title = "Englisch-Plan", subjectId, durationDays = 30 }));
        return new Kind(father, childId, pin, subjectId, planId);
    }

    /// <summary>A vocabulary exercise with <paramref name="words"/> words whose author suggests the Leitner box.</summary>
    private async Task<int> VokabelnAsync(HttpClient father, int words = 2, bool leitner = true)
    {
        var id = await TestApi.CreateVocabExerciseAsync(father,
            [.. Enumerable.Range(1, words).Select(i => ($"word{i}-{Guid.NewGuid():N}"[..14], $"Wort{i}"))]);
        if (leitner) Db(db => db.Exercises.Single(e => e.Id == id).DefaultUseLeitner = true);
        return id;
    }

    private async Task<int> ArbeitAsync(Kind k, DateOnly date, int[] exerciseIds, int? subjectId = -1, int? window = null)
    {
        var res = await k.Father.PostAsJsonAsync("/api/v1/supervisor/class-tests", new
        {
            childId = k.ChildId,
            title = "Vokabeltest",
            scheduledDate = date.ToString("yyyy-MM-dd"),
            subjectId = subjectId == -1 ? k.SubjectId : subjectId,
            exerciseIds,
            prepWindowDays = window,
        });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("klassenarbeit").GetProperty("id").GetInt32();
    }

    /// <summary>The child logs in - the one trigger of the reconcile (and of the penalty settlement).</summary>
    private Task<HttpClient> LoginAsync(Kind k) => TestApi.ChildAsync(factory, k.ChildId, k.Pin);

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

    private List<PlanPosition> Positionen(int planId) =>
        Read(db => db.PlanPositions.Where(p => p.StudyPlanId == planId).OrderBy(p => p.Order).ThenBy(p => p.Id).ToList());

    /// <summary>B-535: penalties start with the child's first play in the plan - an unpassed attempt on the day.</summary>
    private void GespieltAm(int planId, int positionId, DateOnly day) => Db(db => db.TestAttempts.Add(new TestAttempt
    {
        StudyPlanId = planId,
        PlanPositionId = positionId,
        Day = day,
        StartedAt = day.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc),
    }));

    private int Strafen(int childId) => Read(db => db.ChildPointsEntries
        .Where(e => e.ChildId == childId && e.Kind == PointKind.GoalPenalty).Sum(e => (int?)e.Amount) ?? 0);

    private void Vorspulen(int days) => factory.Clock.Advance(TimeSpan.FromDays(days));

    [Fact]
    public async Task Pflicht_Entsteht_Erst_Im_Fenster_Ab_Dem_Anlegetag()
    {
        var k = await KindAsync();
        var ex = await VokabelnAsync(k.Father);
        var arbeit = await ArbeitAsync(k, D.AddDays(10), [ex]); // window 7: begins D+3

        await LoginAsync(k);
        Assert.Empty(Positionen(k.PlanId));
        Assert.Null(Read(db => db.Klassenarbeiten.Single(a => a.Id == arbeit).PrepStartedOn));

        Vorspulen(3);
        await LoginAsync(k);
        var pos = Assert.Single(Positionen(k.PlanId));
        Assert.Equal(ex, pos.ExerciseId);
        Assert.Equal(arbeit, pos.SourceKlassenarbeitId);
        Assert.Equal(GoalCadence.Daily, pos.Cadence);
        Assert.Equal(D.AddDays(3), pos.DutySince);
        Assert.Equal(D.AddDays(9), pos.DutyUntil);
        Assert.Equal(GoalCadence.Daily, pos.CadenceUntil);
        Assert.Equal(5, pos.PenaltyCoins);
        Assert.Equal(0, pos.PointsGoalMet);
        Assert.Equal(D.AddDays(3), Read(db => db.Klassenarbeiten.Single(a => a.Id == arbeit).PrepStartedOn));
    }

    [Fact]
    public async Task Spaeter_Erster_Login_Im_Fenster_Bucht_Nichts_Rueckwirkend()
    {
        // AK 1's red condition: the child first logs in three days into the window. The duty owes from that day, not from the
        // window's start - although the child already played in the plan before (so B-535's first-play clamp does not hide it).
        var k = await KindAsync();
        var andere = await VokabelnAsync(k.Father);
        var frei = 0;
        Db(db =>
        {
            var p = new PlanPosition { StudyPlanId = k.PlanId, ExerciseId = andere, Cadence = GoalCadence.None };
            db.PlanPositions.Add(p);
            db.SaveChanges();
            frei = p.Id;
        });
        GespieltAm(k.PlanId, frei, D.AddDays(3));
        var ex = await VokabelnAsync(k.Father);
        await ArbeitAsync(k, D.AddDays(10), [ex]); // window begins D+3

        Vorspulen(6);
        await LoginAsync(k);
        var pos = Positionen(k.PlanId).Single(p => p.ExerciseId == ex);
        Assert.Equal(D.AddDays(6), pos.DutySince);

        Vorspulen(2);
        await LoginAsync(k);
        Assert.Equal(-10, Strafen(k.ChildId)); // D+6 and D+7 - never D+3 to D+5
    }

    [Fact]
    public async Task Speichern_Der_Position_Nach_Dem_Termin_Macht_Keine_Endlose_Pflicht()
    {
        // Review B-752 (red): the father's form sends the cadence with every save; a save after the test day must not clear
        // the duty's end and leave a daily duty with penalty that never stops.
        var k = await KindAsync();
        var ex = await VokabelnAsync(k.Father);
        await ArbeitAsync(k, D.AddDays(3), [ex]);
        await LoginAsync(k);
        var pos = Assert.Single(Positionen(k.PlanId));
        GespieltAm(k.PlanId, pos.Id, D);

        Vorspulen(5);
        await LoginAsync(k);
        Assert.Equal(-15, Strafen(k.ChildId)); // D, D+1, D+2 missed
        (await k.Father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{k.PlanId}/positions/{pos.Id}",
            new { cadence = "Daily", penaltyCoins = 3 })).EnsureSuccessStatusCode();

        Vorspulen(2);
        await LoginAsync(k);
        Assert.Equal(-15, Strafen(k.ChildId));
        Assert.Equal(D.AddDays(2), Assert.Single(Positionen(k.PlanId)).DutyUntil);
    }

    [Fact]
    public async Task Kauf_Gleicht_Ab_Wie_Der_Login()
    {
        // The second settlement seam (MeController purchase): a child that stays logged in still gets its duty in the window.
        var k = await KindAsync();
        var ex = await VokabelnAsync(k.Father);
        await ArbeitAsync(k, D.AddDays(10), [ex]); // window begins D+3
        var child = await LoginAsync(k);
        (await k.Father.PostAsJsonAsync($"/api/v1/supervisor/children/{k.ChildId}/points", new { amount = 5, reason = "Start" }))
            .EnsureSuccessStatusCode();
        var listing = await TestApi.CreateShopListingAsync(k.Father, $"KA-{Guid.NewGuid():N}"[..12], coinPrice: 1,
            unitsPerPurchase: 1, stock: 5);

        Vorspulen(3);
        (await child.PostAsJsonAsync($"/api/v1/student/me/shop/listings/{listing}/purchase", new { })).EnsureSuccessStatusCode();

        Assert.Equal(D.AddDays(3), Assert.Single(Positionen(k.PlanId)).DutySince);
    }

    [Fact]
    public async Task Termintag_Und_Danach_Schulden_Nichts_Auch_Spaet_Abgerechnet()
    {
        var k = await KindAsync();
        var ex = await VokabelnAsync(k.Father);
        await ArbeitAsync(k, D.AddDays(3), [ex]);
        await LoginAsync(k);
        var pos = Assert.Single(Positionen(k.PlanId));
        GespieltAm(k.PlanId, pos.Id, D);

        // Back six days later: D, D+1, D+2 were owed and missed; the test day D+3 and the days after owe nothing.
        Vorspulen(6);
        await LoginAsync(k);

        Assert.Equal(-15, Strafen(k.ChildId));
        Assert.Equal([D, D.AddDays(1), D.AddDays(2)],
            Read(db => db.PositionGoalPenalties.Where(p => p.PlanPositionId == pos.Id).OrderBy(p => p.PeriodStart)
                .Select(p => p.PeriodStart).ToList()));
    }

    [Fact]
    public async Task Zweiter_Abgleich_Und_Nachgetaggte_Uebung_Legen_Nichts_An()
    {
        var k = await KindAsync();
        var ex = await VokabelnAsync(k.Father);
        var arbeit = await ArbeitAsync(k, D.AddDays(4), [ex]);
        await LoginAsync(k);
        await LoginAsync(k);
        Assert.Single(Positionen(k.PlanId));

        // A tag linked after the start brings a new exercise into the bundle - the frozen bundle makes no duty of it.
        var spaet = await VokabelnAsync(k.Father);
        var tagId = await TestApi.IdAsync(await k.Father.PostAsJsonAsync("/api/v1/creator/tags",
            new { childId = k.ChildId, name = TestApi.UniqueName("Nachtrag") }));
        (await k.Father.PostAsJsonAsync($"/api/v1/creator/tags/{tagId}/exercises", new { exerciseIds = new[] { spaet } })).EnsureSuccessStatusCode();
        (await k.Father.PostAsJsonAsync($"/api/v1/supervisor/class-tests/{arbeit}/tags/{tagId}", new { })).EnsureSuccessStatusCode();
        Vorspulen(1);
        await LoginAsync(k);

        Assert.Equal([ex], Positionen(k.PlanId).Select(p => p.ExerciseId));

        // The other half of the frozen bundle: an exercise taken out of the test after the start keeps its duty.
        (await k.Father.DeleteAsync($"/api/v1/supervisor/class-tests/{arbeit}/exercises/{ex}")).EnsureSuccessStatusCode();
        Vorspulen(1);
        await LoginAsync(k);
        Assert.Equal(D.AddDays(3), Assert.Single(Positionen(k.PlanId)).DutyUntil);
    }

    [Fact]
    public async Task Inaktiver_Plan_Im_Fach_Zaehlt_Nicht()
    {
        var k = await KindAsync();
        Db(db => db.StudyPlans.Single(p => p.Id == k.PlanId).Active = false);
        var ex = await VokabelnAsync(k.Father);
        var arbeit = await ArbeitAsync(k, D.AddDays(3), [ex]);

        await LoginAsync(k);

        Assert.Empty(Positionen(k.PlanId));
        var liste = await k.Father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/class-tests?childId={k.ChildId}");
        Assert.Equal("no_active_plan", liste.EnumerateArray().Single(a => a.GetProperty("id").GetInt32() == arbeit)
            .GetProperty("prepBlockedReason").GetString());
    }

    [Fact]
    public async Task Leeres_Buendel_Friert_Nicht_Ein_Und_Zuweisen_Legt_Die_Pflicht_An()
    {
        // Review: the father plans the test first and assigns its exercises afterwards. Red when a reconcile in between
        // freezes the empty bundle (PrepStartedOn set, no position ever), or when assigning does not reconcile.
        var k = await KindAsync();
        var arbeit = await ArbeitAsync(k, D.AddDays(3), []);
        await LoginAsync(k);
        Assert.Null(Read(db => db.Klassenarbeiten.Single(a => a.Id == arbeit).PrepStartedOn));

        var ex = await VokabelnAsync(k.Father);
        (await k.Father.PostAsJsonAsync($"/api/v1/supervisor/class-tests/{arbeit}/exercises", new { exerciseIds = new[] { ex } }))
            .EnsureSuccessStatusCode();

        var pos = Assert.Single(Positionen(k.PlanId));
        Assert.Equal(ex, pos.ExerciseId);
        Assert.Equal(arbeit, pos.SourceKlassenarbeitId);
        Assert.Equal(D, Read(db => db.Klassenarbeiten.Single(a => a.Id == arbeit).PrepStartedOn));
    }

    [Fact]
    public async Task Anlegen_Im_Fenster_Legt_Die_Pflicht_Ohne_Login_An()
    {
        // Review: a child that stays logged in must not lose the days until its next login. Red when planning a test
        // inside its window leaves the plan without the position.
        var k = await KindAsync();
        var ex = await VokabelnAsync(k.Father);
        await ArbeitAsync(k, D.AddDays(3), [ex]);

        var pos = Assert.Single(Positionen(k.PlanId));
        Assert.Equal(D, pos.DutySince);
        Assert.Equal(D.AddDays(2), pos.DutyUntil);
    }

    [Fact]
    public async Task Beendete_Pflicht_Laesst_Sich_Ueber_Kuer_Neu_Beginnen()
    {
        // Review: after the test day the position still reads "daily" but owes nothing. Freeing it must keep the old end
        // (red when the close stretches the span to today), and a duty again begins today without an end (red when the span
        // survives the switch back).
        var k = await KindAsync();
        var ex = await VokabelnAsync(k.Father);
        await ArbeitAsync(k, D.AddDays(3), [ex]);
        var pos = Assert.Single(Positionen(k.PlanId));
        var url = $"/api/v1/supervisor/study-plans/{k.PlanId}/positions/{pos.Id}";

        Vorspulen(6);
        (await k.Father.PatchAsJsonAsync(url, new { cadence = "None" })).EnsureSuccessStatusCode();
        Assert.Equal(D.AddDays(2), Assert.Single(Positionen(k.PlanId)).DutyUntil);

        (await k.Father.PatchAsJsonAsync(url, new { cadence = "Daily" })).EnsureSuccessStatusCode();
        var neu = Assert.Single(Positionen(k.PlanId));
        Assert.Null(neu.DutyUntil);
        Assert.Equal(GoalCadence.Daily, neu.Cadence);
        Assert.Equal(D.AddDays(6), neu.DutySince);
    }

    [Fact]
    public async Task Leere_Uebung_Wird_Keine_Pflicht()
    {
        // An exercise with nothing to play would be a duty nobody can meet - and a penalty every day.
        var k = await KindAsync();
        var leer = await VokabelnAsync(k.Father);
        var voll = await VokabelnAsync(k.Father);
        Db(db => db.ExerciseItems.RemoveRange(db.ExerciseItems.Where(i => i.ExerciseId == leer)));
        await ArbeitAsync(k, D.AddDays(3), [leer, voll]);

        await LoginAsync(k);

        Assert.Equal([voll], Positionen(k.PlanId).Select(p => p.ExerciseId));
    }

    [Fact]
    public async Task Vom_Vater_Freigegebene_Position_Bleibt_Frei()
    {
        // Decision 8: after the start the position is the father's. Freeing it must not be undone by the next reconcile.
        var k = await KindAsync();
        var ex = await VokabelnAsync(k.Father);
        await ArbeitAsync(k, D.AddDays(5), [ex]);
        await LoginAsync(k);
        var pos = Assert.Single(Positionen(k.PlanId));
        (await k.Father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{k.PlanId}/positions/{pos.Id}", new { cadence = "None" }))
            .EnsureSuccessStatusCode();
        var frei = Assert.Single(Positionen(k.PlanId));

        Vorspulen(1);
        await LoginAsync(k);

        var danach = Assert.Single(Positionen(k.PlanId));
        Assert.Equal(GoalCadence.None, danach.Cadence);
        Assert.Equal(frei.DutyUntil, danach.DutyUntil);
    }

    [Fact]
    public async Task Uebung_Schon_Im_Plan_Bekommt_Keine_Zweite_Position()
    {
        var k = await KindAsync();
        var ex = await VokabelnAsync(k.Father);
        Db(db => db.PlanPositions.Add(new PlanPosition
        {
            StudyPlanId = k.PlanId,
            ExerciseId = ex,
            Cadence = GoalCadence.None,
            PenaltyCoins = 0,
            PointsGoalMet = 7,
        }));
        await ArbeitAsync(k, D.AddDays(3), [ex]);

        await LoginAsync(k);

        var pos = Assert.Single(Positionen(k.PlanId));
        Assert.Equal(GoalCadence.None, pos.Cadence);
        Assert.Null(pos.SourceKlassenarbeitId);
        Assert.Null(pos.DutyUntil);
        Assert.Equal(7, pos.PointsGoalMet);
    }

    [Fact]
    public async Task Malus_Eines_Tages_Ist_Der_Standard_Verteilt_Nicht_Vervielfacht()
    {
        var k = await KindAsync();
        var uebungen = new List<int>();
        for (var i = 0; i < 8; i++) uebungen.Add(await VokabelnAsync(k.Father));
        await ArbeitAsync(k, D.AddDays(3), [.. uebungen]);
        await LoginAsync(k);

        var positionen = Positionen(k.PlanId);
        Assert.Equal(8, positionen.Count);
        Assert.Equal(5, positionen.Sum(p => p.PenaltyCoins));
        Assert.Equal(5, positionen[0].PenaltyCoins); // 5 / 8 = 0, the rest on the first
        Assert.All(positionen, p => Assert.Equal(0, p.PointsGoalMet));

        GespieltAm(k.PlanId, positionen[0].Id, D);
        Vorspulen(1);
        await LoginAsync(k);
        Assert.Equal(-5, Strafen(k.ChildId)); // one missed day, all eight missed: still the standard once
    }

    [Fact]
    public async Task Ohne_Fach_Oder_Ohne_Aktiven_Plan_Keine_Pflicht_Und_Die_Liste_Nennt_Den_Grund()
    {
        var k = await KindAsync();
        var ex = await VokabelnAsync(k.Father);
        var ohneFach = await ArbeitAsync(k, D.AddDays(3), [ex], subjectId: null);
        var fremdesFach = await TestApi.IdAsync(await k.Father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Erdkunde") }));
        var ohnePlan = await ArbeitAsync(k, D.AddDays(3), [ex], subjectId: fremdesFach);
        var mitPlan = await ArbeitAsync(k, D.AddDays(20), [ex]);

        await LoginAsync(k);
        Assert.Empty(Positionen(k.PlanId));
        Assert.Equal(1, Read(db => db.StudyPlans.Count(p => p.ChildId == k.ChildId)));

        var liste = await k.Father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/class-tests?childId={k.ChildId}");
        string? Grund(int id) => liste.EnumerateArray().Single(a => a.GetProperty("id").GetInt32() == id)
            .GetProperty("prepBlockedReason").GetString();
        Assert.Equal("no_subject", Grund(ohneFach));
        Assert.Equal("no_active_plan", Grund(ohnePlan));
        Assert.Null(Grund(mitPlan));
    }

    [Fact]
    public async Task Zwei_Arbeiten_Dieselbe_Uebung_Eine_Position_Bis_Zum_Spaeteren_Termin()
    {
        var k = await KindAsync();
        var ex = await VokabelnAsync(k.Father);
        var frueh = await ArbeitAsync(k, D.AddDays(3), [ex]);
        await ArbeitAsync(k, D.AddDays(5), [ex]);

        await LoginAsync(k);

        var pos = Assert.Single(Positionen(k.PlanId));
        Assert.Equal(frueh, pos.SourceKlassenarbeitId);
        Assert.Equal(D.AddDays(4), pos.DutyUntil);
    }

    [Fact]
    public async Task Spaetere_Arbeit_Verlaengert_Eine_Schon_Laufende_Pflicht()
    {
        // The second test's window begins only after the first one started: its start must extend, not duplicate.
        var k = await KindAsync();
        var ex = await VokabelnAsync(k.Father);
        await ArbeitAsync(k, D.AddDays(3), [ex]);
        await ArbeitAsync(k, D.AddDays(9), [ex]); // window begins D+2
        await LoginAsync(k);
        Assert.Equal(D.AddDays(2), Assert.Single(Positionen(k.PlanId)).DutyUntil);

        Vorspulen(2);
        await LoginAsync(k);
        Assert.Equal(D.AddDays(8), Assert.Single(Positionen(k.PlanId)).DutyUntil);
    }

    [Fact]
    public async Task Fenster_Null_Schaltet_Die_Pflicht_Ab()
    {
        var k = await KindAsync();
        var ex = await VokabelnAsync(k.Father);
        await ArbeitAsync(k, D.AddDays(3), [ex], window: 0);

        await LoginAsync(k);

        Assert.Empty(Positionen(k.PlanId));
    }

    [Fact]
    public async Task Dosis_Ist_Aufgerundet_Je_Tag_Und_Gedeckelt_Ohne_Tagesdosis_Traegt_ItemCount_Den_Deckel()
    {
        var k = await KindAsync();
        var zehn = await VokabelnAsync(k.Father, words: 10);
        var vierzig = await VokabelnAsync(k.Father, words: 40);
        var (_, _, rechnen) = await TestApi.CreateArithmeticExerciseAsync(k.Father);
        await ArbeitAsync(k, D.AddDays(3), [zehn, vierzig, rechnen]); // three days to go

        await LoginAsync(k);

        var byExercise = Positionen(k.PlanId).ToDictionary(p => p.ExerciseId);
        Assert.Equal(4, byExercise[zehn].NewItemsPerDay);     // ceil(10 / 3), not 3
        Assert.Null(byExercise[zehn].ItemCount);
        Assert.True(byExercise[zehn].UseLeitner);
        Assert.Equal(10, byExercise[vierzig].NewItemsPerDay); // ceil(40 / 3) = 14, capped at 10
        Assert.Equal(0, byExercise[rechnen].NewItemsPerDay);  // no daily dose without the Leitner box
        Assert.Equal(10, byExercise[rechnen].ItemCount);
    }

    [Fact]
    public async Task Liste_Nennt_Fenster_Standard_Und_Karten_Je_Tag()
    {
        var k = await KindAsync();
        var ex = await VokabelnAsync(k.Father, words: 14);
        var standard = await ArbeitAsync(k, D.AddDays(20), [ex]);
        var eigenes = await ArbeitAsync(k, D.AddDays(20), [ex], window: 2);
        var aus = await ArbeitAsync(k, D.AddDays(20), [ex], window: 0);

        var liste = await k.Father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/class-tests?childId={k.ChildId}");
        JsonElement Arbeit(int id) => liste.EnumerateArray().Single(a => a.GetProperty("id").GetInt32() == id);

        Assert.Equal(JsonValueKind.Null, Arbeit(standard).GetProperty("prepWindowDays").ValueKind);
        Assert.Equal(7, Arbeit(standard).GetProperty("prepWindowDefault").GetInt32());
        Assert.Equal(2, Arbeit(standard).GetProperty("prepCardsPerDay").GetInt32()); // ceil(14 / 7)
        Assert.Equal(2, Arbeit(eigenes).GetProperty("prepWindowDays").GetInt32());
        Assert.Equal(7, Arbeit(eigenes).GetProperty("prepCardsPerDay").GetInt32());  // ceil(14 / 2)
        Assert.Equal(JsonValueKind.Null, Arbeit(aus).GetProperty("prepCardsPerDay").ValueKind);
    }

    [Fact]
    public async Task Fenster_Ausserhalb_Des_Bereichs_Wird_Abgewiesen()
    {
        var k = await KindAsync();
        var res = await k.Father.PostAsJsonAsync("/api/v1/supervisor/class-tests", new
        {
            childId = k.ChildId,
            title = "X",
            scheduledDate = D.AddDays(9).ToString("yyyy-MM-dd"),
            prepWindowDays = -1,
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ---- B-941: only executable material becomes a duty ----

    /// <summary>Leaves <paramref name="exerciseId"/> with a grant only to <paramref name="grantTo"/> (<c>null</c> a stranger
    /// adult, otherwise that adult) and withdraws it unless <paramref name="executePublic"/>.</summary>
    private void Zurueckgezogen(int exerciseId, int? grantTo, bool executePublic = false)
    {
        Db(db =>
        {
            var ex = db.Exercises.Single(e => e.Id == exerciseId);
            ex.ExecutePublic = executePublic;
            db.ExerciseGrants.RemoveRange(db.ExerciseGrants.Where(g => g.ExerciseId == exerciseId));
            var creator = grantTo;
            if (creator is null)
            {
                var fremd = new Adult { Name = TestApi.UniqueName("Fremd") };
                db.Adults.Add(fremd);
                db.SaveChanges();
                creator = fremd.Id;
            }
            db.ExerciseGrants.Add(new ExerciseGrant { ExerciseId = exerciseId, CreatorId = creator.Value, Permission = GrantPermission.Owner });
        });
    }

    /// <summary>Puts <paramref name="exerciseId"/> into the bundle of <paramref name="arbeit"/> through a tag of the child.</summary>
    private async Task UeberSchlagwortAsync(Kind k, int arbeit, int exerciseId)
    {
        var tagId = await TestApi.IdAsync(await k.Father.PostAsJsonAsync("/api/v1/creator/tags",
            new { childId = k.ChildId, name = TestApi.UniqueName("Arbeit") }));
        (await k.Father.PostAsJsonAsync($"/api/v1/creator/tags/{tagId}/exercises", new { exerciseIds = new[] { exerciseId } }))
            .EnsureSuccessStatusCode();
        Db(db => db.KlassenarbeitTags.Add(new KlassenarbeitTag { KlassenarbeitId = arbeit, TagId = tagId }));
    }

    private async Task<JsonElement> ArbeitLesenAsync(Kind k, int arbeit) =>
        (await k.Father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/class-tests?childId={k.ChildId}"))
            .EnumerateArray().Single(a => a.GetProperty("id").GetInt32() == arbeit);

    // AK 1, 3, 4. Red when the sink lets a non-public exercise with only a stranger's grant in (any grant counts, or no
    // filter at all), when the count before the start includes it, or when the supervisor's count stays 0.
    [Fact]
    public async Task Zurueckgezogenes_Material_Ueber_Schlagwort_Wird_Keine_Pflicht()
    {
        var k = await KindAsync();
        var offen = await VokabelnAsync(k.Father);
        var fremd = await VokabelnAsync(k.Father);
        var arbeit = await ArbeitAsync(k, D.AddDays(10), [offen]);
        await UeberSchlagwortAsync(k, arbeit, fremd);
        Zurueckgezogen(fremd, grantTo: null);

        var vorher = await ArbeitLesenAsync(k, arbeit);
        Assert.Equal(1, vorher.GetProperty("prepCardsPerDay").GetInt32()); // ceil(2 / 7) for the public one only
        Assert.Equal(1, vorher.GetProperty("prepNotExecutableCount").GetInt32());

        Vorspulen(3);
        await LoginAsync(k);
        var pos = Assert.Single(Positionen(k.PlanId));
        Assert.Equal(offen, pos.ExerciseId);
    }

    // Review finding (Sprint 135): a bundle of withdrawn material only must not freeze the test. Red when the start claims
    // on an unfiltered bundle - the later grant would then never start the duty.
    [Fact]
    public async Task Nur_Zurueckgezogenes_Friert_Die_Arbeit_Nicht_Ein()
    {
        var k = await KindAsync();
        var ex = await VokabelnAsync(k.Father);
        var arbeit = await ArbeitAsync(k, D.AddDays(10), [ex]);
        Zurueckgezogen(ex, grantTo: null);

        Vorspulen(3);
        await LoginAsync(k);
        Assert.Empty(Positionen(k.PlanId));
        Assert.Null(Read(db => db.Klassenarbeiten.Single(a => a.Id == arbeit).PrepStartedOn));

        var vater = Read(db => db.SupervisorLinks.Where(l => l.StudentId == k.ChildId).Select(l => l.SupervisorId).First());
        Zurueckgezogen(ex, grantTo: vater);
        await LoginAsync(k);
        Assert.Equal(ex, Assert.Single(Positionen(k.PlanId)).ExerciseId);
    }

    // The count's bounds. Red when it counts an exercise already in the plan (it is played there), or keeps counting on and
    // after the test day, or for a test that is not planned.
    [Fact]
    public async Task Ausgelassen_Zaehlt_Nur_Was_Der_Pflicht_Wirklich_Fehlt()
    {
        var k = await KindAsync();
        var imPlan = await VokabelnAsync(k.Father);
        var fehlt = await VokabelnAsync(k.Father);
        Db(db => db.PlanPositions.Add(new PlanPosition { StudyPlanId = k.PlanId, ExerciseId = imPlan, Cadence = GoalCadence.None }));
        var arbeit = await ArbeitAsync(k, D.AddDays(10), [imPlan, fehlt]);
        Zurueckgezogen(imPlan, grantTo: null);
        Zurueckgezogen(fehlt, grantTo: null);
        Assert.Equal(1, (await ArbeitLesenAsync(k, arbeit)).GetProperty("prepNotExecutableCount").GetInt32());

        Vorspulen(10); // the test day: nothing is owed any more
        Assert.Equal(0, (await ArbeitLesenAsync(k, arbeit)).GetProperty("prepNotExecutableCount").GetInt32());
        Vorspulen(-10);
        Db(db => db.Klassenarbeiten.Single(a => a.Id == arbeit).Status = KlassenarbeitStatus.Cancelled);
        Assert.Equal(0, (await ArbeitLesenAsync(k, arbeit)).GetProperty("prepNotExecutableCount").GetInt32());
    }

    // AK 2. Red when the sink drops the grant clause (a supervisor's own withdrawn-but-granted material would vanish) or
    // the public clause (the public exercise, granted only to a stranger, would vanish).
    [Fact]
    public async Task Freigabe_Eines_Betreuers_Laesst_Die_Uebung_Pflicht_Werden()
    {
        var k = await KindAsync();
        var offen = await VokabelnAsync(k.Father);
        var freigegeben = await VokabelnAsync(k.Father);
        var arbeit = await ArbeitAsync(k, D.AddDays(10), [offen]);
        await UeberSchlagwortAsync(k, arbeit, freigegeben);
        var vater = Read(db => db.SupervisorLinks.Where(l => l.StudentId == k.ChildId).Select(l => l.SupervisorId).First());
        Zurueckgezogen(freigegeben, grantTo: vater);
        // Public, but no supervisor of the child holds a grant: only the public clause lets it in.
        Zurueckgezogen(offen, grantTo: null, executePublic: true);

        Assert.Equal(0, (await ArbeitLesenAsync(k, arbeit)).GetProperty("prepNotExecutableCount").GetInt32());
        Vorspulen(3);
        await LoginAsync(k);
        Assert.Equal([offen, freigegeben], Positionen(k.PlanId).Select(p => p.ExerciseId).Order().ToArray());
    }
}
