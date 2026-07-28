using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Wie viele Verwendungen eine Übung <b>am Löschen hindern</b> – aufgeteilt in die, die der Aufrufer
/// sehen kann, und die, die außerhalb seiner Betreuung liegen.
/// </summary>
/// <param name="OwnPlans">Lehrplan-Positionen bei Kindern, die der Aufrufer betreut.</param>
/// <param name="HiddenPlans">Lehrplan-Positionen bei Kindern, die er <b>nicht</b> betreut – für ihn unsichtbar.</param>
/// <param name="OwnClassTests">Direkt zugewiesene Klassenarbeiten eigener Kinder.</param>
/// <param name="HiddenClassTests">Direkt zugewiesene Klassenarbeiten fremd betreuter Kinder.</param>
public readonly record struct BlockingUsage(int OwnPlans, int HiddenPlans, int OwnClassTests, int HiddenClassTests)
{
    /// <summary>Verwendungen, die der Aufrufer in der Verwendungs-Anzeige findet.</summary>
    public int Own => OwnPlans + OwnClassTests;

    /// <summary>Verwendungen, die ihm verborgen bleiben – die Zahl, ohne die ein 409 ein Rätsel ist.</summary>
    public int Hidden => HiddenPlans + HiddenClassTests;

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

        return new BlockingUsage(
            OwnPlans: planMine, HiddenPlans: planTotal - planMine,
            OwnClassTests: testMine, HiddenClassTests: testTotal - testMine);
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

        var parts = new List<string>();
        if (own.Count > 0) parts.Add($"{string.Join(" and ", own)} of yours");
        if (usage.Hidden > 0)
            parts.Add($"{Plural(usage.Hidden, "usage")} outside your care (children you do not supervise, "
                + "so they are not listed under usage)");

        return $"Cannot delete: the exercise is still used – {string.Join("; ", parts)}. Remove it there first.";
    }

    private static string Plural(int count, string noun) => $"{count} {noun}{(count == 1 ? "" : "s")}";
}
