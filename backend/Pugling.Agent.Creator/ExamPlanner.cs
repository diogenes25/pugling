using Pugling.Agent.Creator.Drafting;
using Pugling.Client;

namespace Pugling.Agent.Creator;

/// <summary>
/// The request for an exercise-based class test: the same briefing as a single exercise, but spanning
/// <b>several</b> exercise types.
/// </summary>
/// <param name="Base">The shared request (child/profile/location/unit); the planner sets type and scope per part.</param>
/// <param name="Types">The exercise types the class test consists of - in this order.</param>
/// <param name="PerType">Tasks per type.</param>
/// <param name="ScheduledDate">Date of the class test; one week ahead if not given.</param>
/// <param name="Title">Title of the class test; formed from the unit or topic if not given.</param>
public sealed record ExamRequest(
    GenerationRequest Base,
    IReadOnlyList<string> Types,
    int PerType,
    DateOnly? ScheduledDate,
    string? Title);

/// <summary>One part of the class test: the run of one exercise type with its result.</summary>
/// <param name="TypeKey">The exercise type.</param>
/// <param name="Outcome">The result of the run, or <c>null</c> if it failed.</param>
/// <param name="Error">The error message of the failed run.</param>
public sealed record ExamPart(string TypeKey, GenerationOutcome? Outcome, string? Error);

/// <summary>The result of a class test planning run.</summary>
/// <param name="Title">The title used.</param>
/// <param name="Parts">All parts in request order.</param>
/// <param name="ExerciseIds">The exercises actually created.</param>
/// <param name="ClassTestId">The created class test (only with a child and without a dry run).</param>
/// <param name="TagName">The tag holding the bundle together (only with a child).</param>
public sealed record ExamOutcome(
    string Title,
    IReadOnlyList<ExamPart> Parts,
    IReadOnlyList<int> ExerciseIds,
    int? ClassTestId,
    string? TagName)
{
    /// <summary>Did every part run through and pass its self-test?</summary>
    public bool Complete => Parts.All(p => p.Outcome is { DraftAccepted: true, RolledBack: false });
}

/// <summary>
/// Plans an <b>exercise-based class test</b>: several exercises of different types on the same material,
/// bundled into a planned class test. The flow is deliberately deterministic and wired in C# - every part
/// runs through the same pipeline (including its own self-test), and only afterward do the tag and class
/// test come into being. This keeps a half-successful class test <i>visibly</i> half-successful, instead
/// of landing as a complete assignment in the child's calendar: failed parts do not abort the run, but
/// are reported.
/// </summary>
public sealed class ExamPlanner(CreatorPipeline pipeline, CreatorApi creator, SupervisorApi supervisor)
{
    /// <summary>Without a date, the class test lies one week in the future - time to practice.</summary>
    private const int DefaultDaysAhead = 7;

    /// <summary>Generates the exercises and, with a child, the class test to go with them.</summary>
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
    /// The title from the material: the request's unit, otherwise the topic. Deliberately queried via the
    /// briefing rather than guessed - the unit label should be the same one that appears in the source.
    /// </summary>
    private async Task<string> DeriveTitleAsync(ExamRequest request, CancellationToken ct)
    {
        var briefing = await pipeline.BriefAsync(request.Base, ct);
        var subject = briefing.Profile?.Unit?.Label ?? request.Base.Topic ?? briefing.ChapterName;
        return $"Übungsklausur: {subject}";
    }

    /// <summary>
    /// Finds the class test's tag or creates it. The name is unique per child; a second run with the
    /// same title should extend the existing bundle, not fail on a name conflict.
    /// </summary>
    private async Task<TagResponse> EnsureTagAsync(int childId, string title, CancellationToken ct)
    {
        var existing = await creator.ListTagsAsync(childId, ct);
        if (existing.FirstOrDefault(t => string.Equals(t.Name, title, StringComparison.OrdinalIgnoreCase)) is { } found)
            return found;

        return await creator.CreateTagAsync(new CreateTagDto(childId, title, null), ct);
    }
}
