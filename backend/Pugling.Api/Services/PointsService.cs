using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services;

public class PointsService(PuglingDbContext db)
{
    /// <summary>
    /// Punkte für eine richtige Wiederholung, verfahrensneutral: erstmalige Wiederholung (<paramref name="reviewCount"/> 0)
    /// zählt am meisten, spätere je höher die <paramref name="box"/> weniger; gewichtet nach dem aktiven Zeitfenster.
    /// </summary>
    public async Task<int> PointsForReviewAsync(int reviewCount, int box, DateTime nowLocal)
    {
        int basePoints = reviewCount == 0
            ? 10                                  // neuer Inhalt
            : Math.Max(2, 8 - box);               // Wiederholung: je höher die Box, desto weniger

        var time = TimeOnly.FromDateTime(nowLocal);
        var slot = await db.TimeSlots
            .Where(s => s.StartTime <= time && time < s.EndTime)
            .FirstOrDefaultAsync();

        return (int)Math.Round(basePoints * (slot?.Multiplier ?? 1.0));
    }
}
