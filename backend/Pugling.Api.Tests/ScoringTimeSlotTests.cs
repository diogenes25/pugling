using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Prüft den Zeitfenster-Multiplikator der Basispunkte (<see cref="ScoringService"/>): eine richtige
/// Wiederholung wird mit dem Faktor des zur Uhrzeit aktiven <see cref="TimeSlotRule"/> gewichtet;
/// außerhalb aller Fenster gilt Faktor 1,0. Direkter Service-Test mit FIXER Uhrzeit – kein Wanduhr-Bezug,
/// darum reihenfolge-/zeitunabhängig.
/// </summary>
public class ScoringTimeSlotTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // Neuer Inhalt (reviewCount 0) bringt genau NewContentPoints als Basis – bequem zum Nachrechnen.
    private static readonly ScoringService.ScoreConfig Cfg = new("Test", NewContentPoints: 10, 0, 0, 0, 0);

    private async Task<int> BasePointsAtAsync(DateTime nowLocal)
    {
        using var scope = factory.Services.CreateScope();
        var scoring = scope.ServiceProvider.GetRequiredService<ScoringService>();
        var score = await scoring.ScoreReviewAsync(Cfg, reviewCount: 0, box: 1, postBox: 2,
            wasCorrect: true, combo: 0, nowLocal, elapsedSeconds: null);
        return score.BasePoints;
    }

    [Fact]
    public async Task Basispunkte_ImDoppelfenster_WerdenVerdoppelt_SonstUnveraendert()
    {
        // Eigenes 2×-Fenster anlegen: Die Test-Factory löscht die geseedeten Fenster (Wanduhr-Neutralisierung),
        // wer den Multiplikator prüfen will, bringt sein Fenster also selbst mit. Die Nachtzeiten bleiben
        // trotzdem gewählt – so trifft der Test auch dann, wenn irgendwann wieder Fenster vorhanden sind.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            db.TimeSlots.Add(new TimeSlotRule { Name = "Test-Nacht", StartTime = new(2, 0), EndTime = new(4, 0), Multiplier = 2.0 });
            await db.SaveChangesAsync();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var inSlot = today.ToDateTime(new TimeOnly(3, 0));   // im 2×-Fenster
        var noSlot = today.ToDateTime(new TimeOnly(22, 30)); // außerhalb aller Fenster

        Assert.Equal(20, await BasePointsAtAsync(inSlot)); // 10 × 2,0
        Assert.Equal(10, await BasePointsAtAsync(noSlot)); // 10 × 1,0 (kein Fenster)
    }

    /// <summary>
    /// Überlappende Fenster sind erlaubt (das Anlegen verbietet sie nicht) – dann muss die Auswahl trotzdem
    /// **festliegen**: das engste, also am spätesten beginnende Fenster gewinnt. Ohne diese Ordnung entschied
    /// die Datenbank-Laune, und dieselbe richtige Antwort brachte mal 30, mal 50 Punkte.
    /// </summary>
    [Fact]
    public async Task Bei_ueberlappenden_Fenstern_gewinnt_deterministisch_das_engste()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            // Absichtlich das weite Fenster ZUERST einfügen: ohne OrderBy läge es als Erstes in der
            // Einfüge-Reihenfolge und der Test würde den falschen Faktor sehen.
            db.TimeSlots.Add(new TimeSlotRule { Name = "Weit", StartTime = new(4, 0), EndTime = new(6, 0), Multiplier = 3.0 });
            db.TimeSlots.Add(new TimeSlotRule { Name = "Eng", StartTime = new(4, 30), EndTime = new(5, 0), Multiplier = 5.0 });
            await db.SaveChangesAsync();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // 04:45 liegt in beiden Fenstern → das engere („Eng", 5,0) muss gelten.
        Assert.Equal(50, await BasePointsAtAsync(today.ToDateTime(new TimeOnly(4, 45))));
        // 04:15 liegt nur im weiten Fenster.
        Assert.Equal(30, await BasePointsAtAsync(today.ToDateTime(new TimeOnly(4, 15))));
    }
}
