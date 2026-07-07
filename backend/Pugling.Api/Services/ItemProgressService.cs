using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services;

/// <summary>
/// Schreibt den plan-übergreifenden Lernstand je (Kind, Item) fort und protokolliert jede Antwort. Wird an den
/// server-autoritativen Bewertungsstellen (Üben/Test) aufgerufen – ausschließlich für Vokabel-Items, die eine
/// stabile <see cref="ContentItem.ItemId"/> tragen. Aktualisiert <see cref="ItemProgress"/> (Box/Beherrschung,
/// Zähler, letzte Antwort) und hängt einen <see cref="ItemReviewEvent"/> an die Historie. Bewusst ohne eigenes
/// <c>SaveChanges</c>: die aufrufenden Controller speichern gebündelt mit ihren übrigen Schreibvorgängen.
/// </summary>
public class ItemProgressService(PuglingDbContext db)
{
    /// <summary>Beherrschung in Prozent aus der Leitner-Box (Box 1 = 0 % … MaxBox = 100 %; wie im Positions-Report).</summary>
    private static int MasteryOf(int box) =>
        (int)Math.Round(100.0 * (Math.Clamp(box, 1, ItemProgress.MaxBox) - 1) / (ItemProgress.MaxBox - 1));

    /// <summary>
    /// Protokolliert eine bewertete Antwort zu einem Item. Trägt der Inhalt keine stabile Item-/Store-Identität
    /// (Nicht-Vokabel-Typen), passiert nichts. Die Antwort-Historie (<see cref="ItemReviewEvent"/>) wird immer
    /// geschrieben; der aggregierte Lernstand (<see cref="ItemProgress"/>: Box/Beherrschung/Zähler) wird nur
    /// fortgeschrieben, wenn die Antwort <paramref name="countsForMastery"/> ist – sonst ließe sich die Box durch
    /// Wiederholen derselben Karte in einer Sitzung hochtreiben (Anti-Farming, wie beim Positions-Motor).
    /// Kein <c>SaveChanges</c> – siehe Klassen-Doku.
    /// </summary>
    public async Task RecordAsync(int childId, int exerciseId, ContentItem item, bool wasCorrect, int stageValue,
        string? givenAnswer, ItemReviewSource source, int? planPositionId, DateOnly today, bool countsForMastery,
        CancellationToken ct = default)
    {
        if (item.ItemId is not { } itemId || item.VocabularyId is not { } vocabId) return;

        var now = DateTime.UtcNow;

        // Historie protokolliert jede Antwort – auch nicht gewertete Wiederholungen (das ist genuine Historie).
        db.ItemReviewEvents.Add(new ItemReviewEvent
        {
            ChildId = childId,
            ItemId = itemId,
            ExerciseId = exerciseId,
            VocabularyId = vocabId,
            PlanPositionId = planPositionId,
            Source = source,
            StageValue = stageValue,
            GivenAnswer = givenAnswer,
            WasCorrect = wasCorrect,
            At = now,
        });

        if (!countsForMastery) return;

        var prog = await db.ItemProgress.FirstOrDefaultAsync(p => p.ChildId == childId && p.ItemId == itemId, ct);
        if (prog is null)
        {
            prog = new ItemProgress { ChildId = childId, ItemId = itemId, IntroducedAt = today };
            db.ItemProgress.Add(prog);
        }
        // Denormalisierte Bezüge aktuell halten (das Item könnte einer anderen Vokabel zugeordnet worden sein).
        prog.ExerciseId = exerciseId;
        prog.VocabularyId = vocabId;
        prog.IntroducedAt ??= today;
        prog.SeenCount++;
        if (wasCorrect) prog.CorrectCount++;
        // Leitner-Schritt: richtig → eine Box höher (gedeckelt), falsch → zurück auf Box 1 (wie der Positions-Motor).
        prog.Box = wasCorrect ? Math.Min(ItemProgress.MaxBox, Math.Max(1, prog.Box) + 1) : 1;
        prog.MasteryPercent = MasteryOf(prog.Box);
        prog.LastAnswerAt = now;
        prog.LastCorrect = wasCorrect;
    }
}
