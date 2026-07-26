using Pugling.Agent.Creator.Drafting;
using Pugling.Client;

namespace Pugling.Agent.Creator;

/// <summary>
/// Der Auftrag für eine Übungsklausur: dasselbe Briefing wie eine Einzelübung, aber über <b>mehrere</b>
/// Übungstypen hinweg.
/// </summary>
/// <param name="Base">Der gemeinsame Auftrag (Kind/Profil/Ort/Unit); Typ und Umfang setzt der Planer je Teil.</param>
/// <param name="Types">Die Übungstypen, aus denen die Klausur besteht – in dieser Reihenfolge.</param>
/// <param name="PerType">Aufgaben je Typ.</param>
/// <param name="ScheduledDate">Termin der Klassenarbeit; ohne Angabe in einer Woche.</param>
/// <param name="Title">Titel der Klausur; ohne Angabe aus Unit bzw. Thema gebildet.</param>
public sealed record ExamRequest(
    GenerationRequest Base,
    IReadOnlyList<string> Types,
    int PerType,
    DateOnly? ScheduledDate,
    string? Title);

/// <summary>Ein Teil der Klausur: der Lauf eines Übungstyps mit seinem Ergebnis.</summary>
/// <param name="TypeKey">Der Übungstyp.</param>
/// <param name="Outcome">Das Ergebnis des Laufs, oder <c>null</c>, wenn er scheiterte.</param>
/// <param name="Error">Die Fehlermeldung des gescheiterten Laufs.</param>
public sealed record ExamPart(string TypeKey, GenerationOutcome? Outcome, string? Error);

/// <summary>Das Ergebnis einer Klausur-Planung.</summary>
/// <param name="Title">Der verwendete Titel.</param>
/// <param name="Parts">Alle Teile in Auftragsreihenfolge.</param>
/// <param name="ExerciseIds">Die tatsächlich angelegten Übungen.</param>
/// <param name="ClassTestId">Die angelegte Klassenarbeit (nur mit Kind und ohne Trockenlauf).</param>
/// <param name="TagName">Der Tag, der das Bündel zusammenhält (nur mit Kind).</param>
public sealed record ExamOutcome(
    string Title,
    IReadOnlyList<ExamPart> Parts,
    IReadOnlyList<int> ExerciseIds,
    int? ClassTestId,
    string? TagName)
{
    /// <summary>Ist jeder Teil durchgelaufen und hat seinen Selbsttest bestanden?</summary>
    public bool Complete => Parts.All(p => p.Outcome is { DraftAccepted: true, RolledBack: false });
}

/// <summary>
/// Plant eine <b>Übungsklausur</b>: mehrere Übungen verschiedener Typen zum gleichen Stoff, gebündelt zu
/// einer geplanten Klassenarbeit. Der Ablauf ist bewusst deterministisch und in C# verdrahtet – jeder Teil
/// läuft durch dieselbe Pipeline (samt eigenem Selbsttest), und erst danach entstehen Tag und
/// Klassenarbeit. So bleibt eine halb gelungene Klausur <i>sichtbar</i> halb gelungen, statt als
/// vollständige Arbeit im Kalender des Kindes zu landen: gescheiterte Teile brechen den Lauf nicht ab,
/// werden aber gemeldet.
/// </summary>
public sealed class ExamPlanner(CreatorPipeline pipeline, CreatorApi creator, SupervisorApi supervisor)
{
    /// <summary>Ohne Termin liegt die Klausur eine Woche in der Zukunft – Zeit zum Üben.</summary>
    private const int DefaultDaysAhead = 7;

    /// <summary>Erzeugt die Übungen und (mit Kind) die Klassenarbeit dazu.</summary>
    public async Task<ExamOutcome> RunAsync(ExamRequest request, CancellationToken ct = default)
    {
        if (request.Types.Count == 0)
            throw new AgentUsageException("Eine Klausur braucht mindestens einen Übungstyp (--types).");

        var title = request.Title?.Trim() is { Length: > 0 } given ? given : await DeriveTitleAsync(request, ct);
        var parts = new List<ExamPart>();

        foreach (var (typeKey, index) in request.Types.Select((t, i) => (t, i)))
        {
            var part = request.Base with
            {
                TypeKey = typeKey,
                ItemCount = request.PerType,
                // Der Teil-Titel entsteht im Modell; die Zuordnung zur Klausur trägt die Quelle. Das Thema
                // nennt sie ausdrücklich, damit die Aufgaben zum selben Stoff entstehen.
                Topic = $"{request.Base.Topic ?? title} (Übungsklausur, Teil {index + 1}: {typeKey})",
            };

            try
            {
                var (_, outcome) = await pipeline.CreateAsync(part, ct);
                parts.Add(new ExamPart(typeKey, outcome, null));
            }
            catch (Exception ex) when (ex is AgentException or AgentUsageException or PuglingApiException)
            {
                // Ein gescheiterter Typ kostet die Klausur einen Teil, nicht alle: die übrigen laufen weiter.
                parts.Add(new ExamPart(typeKey, null, ex.Message));
            }
        }

        var exerciseIds = parts
            .Select(p => p.Outcome)
            .Where(o => o is { RolledBack: false })
            .Select(o => o!.ExerciseId)
            .OfType<int>()
            .ToList();

        // Ohne Kind gibt es keinen Tag und keine Klassenarbeit: Tags sind kind-skopiert, und eine
        // Klassenarbeit ohne Kind wäre sinnlos. Das Bündel hält dann allein die Quelle zusammen.
        if (request.Base.ChildId is not int childId || request.Base.DryRun || exerciseIds.Count == 0)
            return new ExamOutcome(title, parts, exerciseIds, null, null);

        var tag = await EnsureTagAsync(childId, title, ct);
        await creator.TagExercisesAsync(tag.Id, new TagExercisesDto([.. exerciseIds]), ct);

        var classTest = await supervisor.CreateClassTestAsync(new CreateClassTestDto(
            ChildId: childId,
            Title: title,
            Topic: request.Base.Topic,
            SubjectId: request.Base.SubjectId,
            ScheduledDate: request.ScheduledDate ?? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(DefaultDaysAhead),
            Status: KlassenarbeitStatus.Planned,
            Grade: null,
            GradeComment: null,
            ExerciseIds: [.. exerciseIds],
            TagIds: [tag.Id]), ct);

        return new ExamOutcome(title, parts, exerciseIds, classTest.Klassenarbeit.Id, tag.Name);
    }

    /// <summary>
    /// Der Titel aus dem Material: die Unit des Auftrags, sonst das Thema. Bewusst über das Briefing
    /// erfragt und nicht geraten – die Unit-Bezeichnung soll dieselbe sein, die auch in der Quelle steht.
    /// </summary>
    private async Task<string> DeriveTitleAsync(ExamRequest request, CancellationToken ct)
    {
        var briefing = await pipeline.BriefAsync(request.Base, ct);
        var subject = briefing.Profile?.Unit?.Label ?? request.Base.Topic ?? briefing.ChapterName;
        return $"Übungsklausur: {subject}";
    }

    /// <summary>
    /// Findet den Tag der Klausur oder legt ihn an. Der Name ist je Kind eindeutig; ein zweiter Lauf mit
    /// demselben Titel soll das bestehende Bündel erweitern, nicht an einem Namenskonflikt scheitern.
    /// </summary>
    private async Task<TagResponse> EnsureTagAsync(int childId, string title, CancellationToken ct)
    {
        var existing = await creator.ListTagsAsync(childId, ct);
        if (existing.FirstOrDefault(t => string.Equals(t.Name, title, StringComparison.OrdinalIgnoreCase)) is { } found)
            return found;

        return await creator.CreateTagAsync(new CreateTagDto(childId, title, null), ct);
    }
}
