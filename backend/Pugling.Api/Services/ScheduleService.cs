using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services;

/// <summary>
/// Stundenplan- und Wiederholungs-Steuerung: bestimmt je Tag den Modus (neuer Stoff am Unterrichtstag,
/// sonst Wiederholung – Tag davor = Vorbereitung) und wählt den passenden Item-Pool eines Lehrplans.
/// Ist am Plan <see cref="StudyPlan.UseLeitner"/> aktiv, umfasst die Wiederholung nur die nach dem
/// Karteikasten-Prinzip <em>fälligen</em> Karten (Box-Intervalle); sonst alle eingeführten Inhalte.
/// </summary>
public class ScheduleService(PuglingDbContext db)
{
    /// <summary>Standard-Leitner-Intervalle in Tagen (Index = Box; Index 0 ungenutzt).</summary>
    private static readonly int[] DefaultBoxIntervalDays = [0, 1, 2, 4, 7, 14];

    public record DaySelection(LessonDayMode? Mode, bool IsPreparationDay, string Reason, List<StudyPlanItem> Items);

    /// <summary>Wählt die für einen Tag relevanten Inhalte des Plans (Plan.Items müssen geladen sein).</summary>
    public async Task<DaySelection> SelectAsync(StudyPlan plan, DateOnly day)
    {
        var ordered = plan.Items.OrderBy(i => i.Order).ToList();

        // Kein Fach/Stundenplan hinterlegt: bei Leitner die fälligen Karten, sonst unverändert alle Inhalte.
        if (plan.SubjectId is null)
            return plan.UseLeitner
                ? new DaySelection(LessonDayMode.Review, false, "Leitner – fällige Karten.", DueForReview(plan, ordered, day))
                : new DaySelection(null, false, "Kein Stundenplan verknüpft – alle Inhalte.", ordered);

        var lessonDays = await db.Timetable
            .Where(t => t.ChildId == plan.ChildId && t.SubjectId == plan.SubjectId)
            .Select(t => t.DayOfWeek).Distinct().ToListAsync();
        if (lessonDays.Count == 0)
            return plan.UseLeitner
                ? new DaySelection(LessonDayMode.Review, false, "Kein Stundenplan-Eintrag – fällige Karten.", DueForReview(plan, ordered, day))
                : new DaySelection(null, false, "Kein Stundenplan-Eintrag für das Fach – alle Inhalte.", ordered);

        var introduced = ordered.Where(i => i.IntroducedAt != null).ToList();
        var notIntroduced = ordered.Where(i => i.IntroducedAt == null).ToList();
        var isLessonDay = lessonDays.Contains(day.DayOfWeek);
        var isPreparationDay = lessonDays.Contains(day.AddDays(1).DayOfWeek);

        if (isLessonDay)
        {
            // Unterrichtstag: neuen Stoff einführen; ist alles eingeführt, wird wiederholt.
            var batch = notIntroduced.Take(plan.NewItemsPerLesson).ToList();
            return batch.Count > 0
                ? new DaySelection(LessonDayMode.New, false, "Unterrichtstag – neuer Stoff.", batch)
                : new DaySelection(LessonDayMode.Review, false, "Unterrichtstag – bereits alles eingeführt, Wiederholung.", DueForReview(plan, introduced, day));
        }

        // Kein Unterrichtstag: Wiederholung des bereits Eingeführten; ist noch nichts eingeführt,
        // wird die nächste Charge als Vorschau gezeigt, damit der Sohn nicht leer ausgeht.
        var pool = introduced.Count > 0 ? DueForReview(plan, introduced, day) : notIntroduced.Take(plan.NewItemsPerLesson).ToList();
        var reason = isPreparationDay ? "Vorbereitungstag – Wiederholung vor dem Unterricht." : "Wiederholungstag.";
        return new DaySelection(LessonDayMode.Review, isPreparationDay, reason, pool);
    }

    /// <summary>Markiert die angegebenen (neuen) Inhalte als am Tag eingeführt.</summary>
    public async Task MarkIntroducedAsync(IEnumerable<StudyPlanItem> items, DateOnly day)
    {
        var changed = false;
        foreach (var item in items.Where(i => i.IntroducedAt is null))
        {
            item.IntroducedAt = day;
            item.DueOn ??= day;   // ab Einführung sofort fällig (Box 1)
            changed = true;
        }
        if (changed) await db.SaveChangesAsync();
    }

    /// <summary>
    /// Verbucht eine Leitner-Wiederholung: richtig → eine Box höher (längeres Intervall),
    /// falsch → zurück in Box 1 und sofort wieder fällig. Der Aufrufer speichert.
    /// </summary>
    public void ApplyReview(StudyPlan plan, StudyPlanItem item, bool correct, DateOnly today, DateTime nowUtc)
    {
        var intervals = BoxIntervals(plan);
        item.ReviewCount++;
        item.LastReviewedAt = nowUtc;

        if (correct)
        {
            item.Box = Math.Min(plan.MaxBox, Math.Max(1, item.Box) + 1);
            var days = intervals[Math.Min(item.Box, intervals.Count - 1)];
            item.DueOn = today.AddDays(days);
        }
        else
        {
            item.Box = 1;
            item.DueOn = today;   // gleicher Tag: sofort erneut üben
        }
    }

    /// <summary>Leitner-Intervalle des Plans (eigene, sonst Standard); mindestens die Standardlänge.</summary>
    public IReadOnlyList<int> BoxIntervals(StudyPlan plan) =>
        plan.BoxIntervalDays is { Count: > 1 } custom ? custom : DefaultBoxIntervalDays;

    /// <summary>Bei Leitner: nur fällige Karten (nie bewertet zählt als fällig), sortiert nach Box/Fälligkeit; sonst unverändert.</summary>
    private static List<StudyPlanItem> DueForReview(StudyPlan plan, List<StudyPlanItem> items, DateOnly day) =>
        plan.UseLeitner
            ? items.Where(i => i.DueOn is null || i.DueOn <= day)
                   .OrderBy(i => i.Box).ThenBy(i => i.DueOn ?? DateOnly.MinValue).ThenBy(i => i.Order).ToList()
            : items;
}
