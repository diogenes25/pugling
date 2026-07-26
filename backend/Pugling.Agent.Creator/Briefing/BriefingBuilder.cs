using Pugling.Client;

namespace Pugling.Agent.Creator.Briefing;

/// <summary>
/// Trägt das <see cref="ChildBriefing"/> aus drei Ebenen zusammen: Profil und Lehrbücher vom
/// <b>Supervisor</b>, Katalog und Wortschatz vom <b>Creator</b>, Lernstand von den <b>Student</b>-Lesesichten.
/// Das Konto des Agenten braucht dafür Creator <i>und</i> Supervisor – ein reines Creator-Konto kennt
/// keine Kinder; darauf weist die Ausnahme unten ausdrücklich hin, statt einen nackten 403 durchzureichen.
/// </summary>
public sealed class BriefingBuilder(CreatorApi creator, SupervisorApi supervisor, StudentApi student)
{
    /// <summary>Maximal so viele schwache Wörter gehen in den Prompt – mehr überfrachtet lokale Modelle.</summary>
    private const int MaxWeakWords = 15;

    /// <summary>Baut das Briefing für einen Auftrag.</summary>
    public async Task<ChildBriefing> BuildAsync(GenerationRequest request, CancellationToken ct = default)
    {
        var child = await LoadChildAsync(request.ChildId, ct);
        var textbooks = await LoadTextbooksAsync(request.ChildId, ct);
        var weighted = await LoadInterestsAsync(request.ChildId, ct);

        var subject = await creator.GetSubjectAsync(request.SubjectId, ct);
        var chapter = (await creator.ListChaptersAsync(request.SubjectId, ct))
            .FirstOrDefault(c => c.Id == request.ChapterId)
            ?? throw new AgentUsageException(
                $"Kapitel {request.ChapterId} gehört nicht zu Fach {request.SubjectId} ({subject.Name}).");

        // Vorhandene Titel gehen als „nicht wiederholen" in den Prompt – das verhindert Beinahe-Dubletten.
        var existing = await creator.SearchExercisesAsync(subjectId: request.SubjectId,
            chapterId: request.ChapterId, take: 50, ct: ct);

        var weakWords = request.UseWeakWords
            ? await student.ListWordMasteryAsync(request.ChildId, onlyWeak: true, take: MaxWeakWords, ct: ct)
            : [];

        // Wortschatz-Priorität: ausdrücklich vorgegeben > schwache Wörter des Kindes > (leer, dann wählt das Modell).
        IReadOnlyList<string> requiredWords = request.Words.Count > 0
            ? request.Words
            : [.. weakWords.Select(w => w.Word)];

        return new ChildBriefing(
            ChildId: child.Id,
            Name: child.Name,
            Age: child.BirthYear is { } year ? DateTime.UtcNow.Year - year : null,
            Grade: child.Grade,
            SchoolType: child.SchoolType,
            Gender: child.Gender,
            // Freitext und gewichtete Tags nebeneinander: der Freitext trägt Nuancen, die die Taxonomie
            // nicht kennt, die Tags dafür die Rangfolge – und vor allem die Abneigungen.
            Interests: child.Interests,
            WeightedInterests: [.. weighted.Where(i => i.Weight > 0).OrderByDescending(i => i.Weight)],
            Dislikes: [.. weighted.Where(i => i.Weight < 0).OrderBy(i => i.Weight)],
            ProfileNotes: child.ProfileNotes,
            Textbooks: textbooks,
            SubjectId: subject.Id,
            SubjectName: subject.Name,
            ChapterId: chapter.Id,
            ChapterName: chapter.Name,
            Topic: request.Topic,
            ExistingExerciseTitles: [.. existing.Select(e => e.Title)],
            RequiredWords: requiredWords,
            WeakWords: weakWords);
    }

    private async Task<ChildResponse> LoadChildAsync(int childId, CancellationToken ct)
    {
        try
        {
            return await supervisor.GetChildAsync(childId, ct);
        }
        catch (PuglingApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Forbidden
                                                 or System.Net.HttpStatusCode.NotFound)
        {
            throw new AgentUsageException(
                $"Kind {childId} ist für dieses Konto nicht sichtbar. Der Agent braucht ein Konto, das die " +
                $"Creator-Rolle hat UND dieses Kind betreut (Supervisor). Prüfe Pugling:AccountId/Pugling:Pin.");
        }
    }

    /// <summary>
    /// Die gewichteten Interessen aus der geteilten Taxonomie. Für den Prompt zählen vor allem die
    /// <b>negativen</b>: eine Aufgabe über Spinnen ist fachlich korrekt und trotzdem unbrauchbar, wenn das
    /// Kind Spinnen nicht erträgt. Fehlen sie (frisch angelegtes Kind), bleibt es beim Freitext.
    /// </summary>
    private async Task<IReadOnlyList<ChildInterestResponse>> LoadInterestsAsync(int childId, CancellationToken ct)
    {
        try
        {
            return await supervisor.ListInterestsAsync(childId, ct);
        }
        catch (PuglingApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            return [];
        }
    }

    private async Task<IReadOnlyList<TextbookResponse>> LoadTextbooksAsync(int childId, CancellationToken ct)
    {
        try
        {
            return await supervisor.ListTextbooksAsync(childId, ct);
        }
        catch (PuglingApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            // Ohne Lehrbuch lässt sich trotzdem generieren – dann trägt nur das Thema den Stoff.
            return [];
        }
    }
}
