using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Wie viele Verwendungen eine Übung <b>am Löschen hindern</b> – aufgeteilt in die, die der Aufrufer
/// sehen kann, und die, die außerhalb seiner Betreuung liegen.
/// </summary>
/// <param name="OwnPlans">Lehrplan-Positionen bei Kindern, die der Aufrufer betreut.</param>
/// <param name="HiddenPlans">Lehrplan-Positionen bei Kindern, die er <b>nicht</b> betreut – für ihn unsichtbar.</param>
/// <param name="OwnClassTests">Direkt zugewiesene Klassenarbeiten eigener Kinder.</param>
/// <param name="HiddenClassTests">Direkt zugewiesene Klassenarbeiten fremd betreuter Kinder.</param>
/// <param name="OwnGoals">Etappen großer Ziele (<c>KeyResult</c>) eigener Kinder, die auf die Übung zeigen.</param>
/// <param name="HiddenGoals">Dieselben Etappen bei fremd betreuten Kindern.</param>
/// <param name="HiddenLearners">
/// Wie viele <b>verschiedene Kinder</b> hinter den verborgenen Verwendungen stehen. Eine eigene Zahl, weil
/// sie eine andere Frage beantwortet als <see cref="Hidden"/>: die Verwendungs-Zahl sagt „an wie vielen
/// Stellen müsste jemand aufräumen", diese sagt „von wie vielen Kindern wird mein Material gelernt". Für
/// einen Creator ohne eigene Kinder ist die zweite die einzige, die ihn interessiert – und drei Positionen
/// in den Plänen desselben Kindes sind eben nicht drei Nutzer.
/// </param>
public readonly record struct BlockingUsage(
    int OwnPlans, int HiddenPlans, int OwnClassTests, int HiddenClassTests, int HiddenLearners,
    int OwnGoals = 0, int HiddenGoals = 0)
{
    /// <summary>Verwendungen, die der Aufrufer in der Verwendungs-Anzeige findet.</summary>
    public int Own => OwnPlans + OwnClassTests + OwnGoals;

    /// <summary>Verwendungen, die ihm verborgen bleiben – die Zahl, ohne die ein 409 ein Rätsel ist.</summary>
    public int Hidden => HiddenPlans + HiddenClassTests + HiddenGoals;

    /// <summary>Blockiert überhaupt etwas das Löschen?</summary>
    public bool Any => Own + Hidden > 0;
}

/// <summary>
/// Die <b>eine</b> Antwort auf „wo wird diese Übung verwendet".
///
/// Sie steht hier, weil genau diese Frage vorher an zwei Stellen unterschiedlich beantwortet wurde: die
/// Verwendungs-Anzeige filterte auf die eigenen Kinder, die Löschprüfung schaute global. Steckte eine Übung
/// im Plan eines fremd betreuten Kindes, meldete die Anzeige „nirgends" – und das Löschen scheiterte
/// trotzdem mit <c>409</c>, ohne dass der Autor den Grund finden konnte (Anmerkung 14).
///
/// <para>
/// Bewusst nur die <b>FK-relevanten</b> Verwendungen: Lehrplan-Positionen und <i>direkt</i> zugewiesene
/// Klassenarbeiten. Eine Klassenarbeit, die die Übung nur über einen gemeinsamen Tag einsammelt, verweist
/// nicht auf sie und hindert das Löschen darum auch nicht – sie gehört in die Anzeige, aber nicht in diese
/// Zählung. Wer beides vermischt, baut den nächsten Widerspruch derselben Art.
/// </para>
/// </summary>
public static class ExerciseUsageQueries
{
    /// <summary>
    /// Zählt, was das Löschen blockiert, getrennt nach „sichtbar für <paramref name="fid"/>" und „verborgen".
    /// Ohne <paramref name="fid"/> (Creator ohne Vater-Profil) ist alles verborgen – er betreut kein Kind.
    /// </summary>
    public static async Task<BlockingUsage> CountBlockingAsync(
        PuglingDbContext db, int exerciseId, int? fid, CancellationToken ct = default)
    {
        // Vier schlichte Zählungen statt einer GroupBy-Projektion: SQLite gruppiert hier über einen
        // Unterabfrage-Ausdruck, und der Gewinn wäre ein Roundtrip auf einem Pfad, der einmal pro
        // Lösch-Versuch läuft. Lesbarkeit schlägt das.
        var planTotal = await db.PlanPositions.AsNoTracking()
            .CountAsync(p => p.ExerciseId == exerciseId, ct);
        var planMine = await db.PlanPositions.AsNoTracking()
            .CountAsync(p => p.ExerciseId == exerciseId
                && p.StudyPlan!.Child!.SupervisorLinks.Any(l => l.SupervisorId == fid), ct);
        var testTotal = await db.KlassenarbeitExercises.AsNoTracking()
            .CountAsync(x => x.ExerciseId == exerciseId, ct);
        var testMine = await db.KlassenarbeitExercises.AsNoTracking()
            .CountAsync(x => x.ExerciseId == exerciseId
                && x.Klassenarbeit!.Child!.SupervisorLinks.Any(l => l.SupervisorId == fid), ct);
        // Seit dem Scope-Fremdschlüssel (Restrict) hindert auch eine Ziel-Etappe das Löschen. Ohne diese
        // Zählung wäre der 409 ein 500 – genau die Sorte Lücke, für die es diese Klasse gibt.
        var goalTotal = await db.KeyResults.AsNoTracking()
            .CountAsync(k => k.ExerciseId == exerciseId, ct);
        var goalMine = await db.KeyResults.AsNoTracking()
            .CountAsync(k => k.ExerciseId == exerciseId
                && k.Objective!.Child!.SupervisorLinks.Any(l => l.SupervisorId == fid), ct);

        // Wie viele verschiedene KINDER hinter den verborgenen Verwendungen stehen – nicht wie viele
        // Stellen. Drei Positionen in den Plänen desselben Kindes sind ein Nutzer, nicht drei; und für
        // einen Creator ohne eigene Kinder ist das die einzige Zahl, die etwas aussagt.
        var hiddenLearners = await db.PlanPositions.AsNoTracking()
            .Where(p => p.ExerciseId == exerciseId
                && !p.StudyPlan!.Child!.SupervisorLinks.Any(l => l.SupervisorId == fid))
            .Select(p => p.StudyPlan!.ChildId)
            .Union(db.KlassenarbeitExercises.AsNoTracking()
                .Where(x => x.ExerciseId == exerciseId
                    && !x.Klassenarbeit!.Child!.SupervisorLinks.Any(l => l.SupervisorId == fid))
                .Select(x => x.Klassenarbeit!.ChildId))
            .Union(db.KeyResults.AsNoTracking()
                .Where(k => k.ExerciseId == exerciseId
                    && !k.Objective!.Child!.SupervisorLinks.Any(l => l.SupervisorId == fid))
                .Select(k => k.Objective!.ChildId))
            .Distinct()
            .CountAsync(ct);

        return new BlockingUsage(
            OwnPlans: planMine, HiddenPlans: planTotal - planMine,
            OwnClassTests: testMine, HiddenClassTests: testTotal - testMine,
            HiddenLearners: hiddenLearners,
            OwnGoals: goalMine, HiddenGoals: goalTotal - goalMine);
    }

    /// <summary>
    /// Blockiert <b>irgendeine</b> Übung aus <paramref name="scope"/> das Löschen? Für die Ebenen über der
    /// Übung: ein Fach oder Kapitel kaskadiert auf seine Übungen, und <c>PlanPosition→Exercise</c> ist
    /// <c>Restrict</c> – ohne diese Vorprüfung stirbt das Löschen als FK-Verletzung in einer nackten 500,
    /// statt zu sagen, was im Weg steht.
    /// <para>
    /// Sie steht hier und nicht in den beiden Controllern, weil die Antwort auf „welche Tabellen hindern
    /// das Löschen einer Übung" <b>einen</b> Ort braucht. Vorher stand die Zeile dreimal wörtlich da; eine
    /// vierte verweisende Tabelle hätte man an allen drei Stellen finden müssen – und die eine vergessene
    /// wäre wieder eine 500. Die <i>Meldungstexte</i> bleiben bei den Aufrufern: sie benennen die Ebene
    /// („in this subject" / „in this chapter") und sind nicht dieselbe Aussage.
    /// </para>
    /// </summary>
    public static async Task<bool> AnyBlockingAsync(
        PuglingDbContext db, IQueryable<Exercise> scope, IQueryable<Chapter> chapterScope, CancellationToken ct)
    {
        var ids = scope.Select(x => x.Id);
        var chapterIds = chapterScope.Select(c => c.Id);
        return await db.PlanPositions.AsNoTracking().AnyAsync(p => ids.Contains(p.ExerciseId), ct)
            || await db.KlassenarbeitExercises.AsNoTracking().AnyAsync(x => ids.Contains(x.ExerciseId), ct)
            // Ziel-Etappen zeigen auf die Übung ODER direkt auf das Kapitel – beide FKs sind Restrict, und
            // ein Kapitel-Ziel hängt an keiner Übung. Wer nur den Übungs-Scope prüfte, ließe das Löschen
            // eines Kapitels mit Kapitel-Ziel in die FK-Verletzung laufen.
            || await db.KeyResults.AsNoTracking().AnyAsync(k =>
                (k.ExerciseId != null && ids.Contains(k.ExerciseId.Value))
                || (k.ChapterId != null && chapterIds.Contains(k.ChapterId.Value)), ct);
    }

    /// <summary>
    /// Der Satz, der beim <c>409</c> erklärt, <b>warum</b> nicht gelöscht werden kann – und der die
    /// verborgenen Verwendungen als <i>Zahl</i> nennt. Absichtlich ohne Namen von Plänen oder Kindern:
    /// die gehören einem anderen Betreuer, und der Autor braucht nur zu wissen, dass es sie gibt.
    /// </summary>
    public static string Explain(BlockingUsage usage)
    {
        // Nur was tatsächlich vorkommt, und mit richtigem Plural: eine Meldung, die "0 class test(s)"
        // aufzählt, wirkt maschinell und lenkt vom eigentlichen Hinweis ab.
        var own = new List<string>();
        if (usage.OwnPlans > 0) own.Add(Plural(usage.OwnPlans, "study plan"));
        if (usage.OwnClassTests > 0) own.Add(Plural(usage.OwnClassTests, "class test"));
        if (usage.OwnGoals > 0) own.Add(Plural(usage.OwnGoals, "objective milestone"));

        var parts = new List<string>();
        if (own.Count > 0) parts.Add($"{string.Join(" and ", own)} of yours");
        if (usage.Hidden > 0)
            parts.Add($"{Plural(usage.Hidden, "usage")} outside your care (children you do not supervise, "
                + "so they are not listed under usage)");

        return $"Cannot delete: the exercise is still used – {string.Join("; ", parts)}. Remove it there first.";
    }

    private static string Plural(int count, string noun) => $"{count} {noun}{(count == 1 ? "" : "s")}";
}
