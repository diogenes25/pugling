using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;
using Pugling.Api.Services;

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
        // Eigenes 2×-Fenster zu einer vom Seed NICHT belegten Uhrzeit anlegen (der Seed deckt 08–21 Uhr ab).
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
}
