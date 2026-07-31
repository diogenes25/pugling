using Pugling.Client;

namespace Pugling.Agent.Creator.Briefing;

/// <summary>
/// Assembles the <see cref="CreatorBriefing"/>: the <b>profile</b> and the textbook units from the
/// <b>creator</b>, catalog and vocabulary also from the creator, child profile and textbooks from the
/// <b>supervisor</b>, learning progress from the <b>student</b> read views.
/// <para>
/// The child part is <b>optional</b>, and a permission question hangs on that: only someone who names a
/// child needs an account that supervises this child. General catalog exercises need only the creator
/// role - so supervisor and student views are queried here only when a child is set.
/// </para>
/// </summary>
public sealed class BriefingBuilder(CreatorApi creator, SupervisorApi supervisor, StudentApi student)
{
    /// <summary>At most this many weak words go into the prompt - more would overload local models.</summary>
    private const int MaxWeakWords = 15;

    /// <summary>Builds the briefing for a request.</summary>
    public async Task<CreatorBriefing> BuildAsync(GenerationRequest request, CancellationToken ct = default)
    {
        var subject = await creator.GetSubjectAsync(request.SubjectId, ct);
        var chapter = (await creator.ListChaptersAsync(request.SubjectId, ct))
            .FirstOrDefault(c => c.Id == request.ChapterId)
            ?? throw new AgentUsageException(
                $"Kapitel {request.ChapterId} gehört nicht zu Fach {request.SubjectId} ({subject.Name}).");

        var child = request.ChildId is int childId ? await LoadChildAsync(childId, request, ct) : null;
        var profile = await ResolveProfileAsync(request, ct);
        var (series, unit) = await ResolveMaterialAsync(request, profile, child, subject.Id, subject.Name, ct);

        // Vorhandene Titel gehen als „nicht wiederholen" in den Prompt – das verhindert Beinahe-Dubletten.
        var existing = await creator.SearchExercisesAsync(subjectId: request.SubjectId,
            chapterId: request.ChapterId, take: 50, ct: ct);

        // Wortschatz-Priorität: ausdrücklich vorgegeben > schwache Wörter des Kindes > (leer, dann wählt das Modell).
        IReadOnlyList<string> requiredWords = request.Words.Count > 0
            ? request.Words
            : [.. (child?.WeakWords ?? []).Select(w => w.Word)];

        return new CreatorBriefing(
            Profile: profile is null ? null : Facts(profile, series, unit),
            // `--general` behält Reihe und Unit des Kindes, lässt aber die Person weg: der Stoff stimmt,
            // die Übung bleibt für den gemeinsamen Katalog brauchbar.
            Child: request.General ? null : child,
            SubjectId: subject.Id,
            SubjectName: subject.Name,
            ChapterId: chapter.Id,
            ChapterName: chapter.Name,
            Topic: request.Topic,
            // Ausdrücklich gesetzte Sprachen schlagen das Profil; ohne beides die üblichen Vorgaben.
            SourceLang: request.SourceLang ?? profile?.SourceLang ?? "en",
            TargetLang: request.TargetLang ?? profile?.TargetLang ?? "de",
            ExistingExerciseTitles: [.. existing.Select(e => e.Title)],
            RequiredWords: requiredWords);
    }

    /// <summary>
    /// The request's profile: explicitly named, otherwise the <b>best fit</b> for the child. Without
    /// either, it stays empty - the agent then works as a generalist as before, instead of aborting with
    /// an error message (a catalog without profiles must remain usable).
    /// </summary>
    public async Task<CreatorProfileResponse?> ResolveProfileAsync(GenerationRequest request, CancellationToken ct = default)
    {
        if (request.ProfileId is int profileId)
        {
            try
            {
                return await creator.GetProfileAsync(profileId, ct);
            }
            catch (PuglingApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
            {
                throw new AgentUsageException($"Creator-Profil {profileId} gibt es nicht – 'pugling-creator profiles' zeigt die vorhandenen.");
            }
        }

        if (request.ChildId is not int childId) return null;

        var matches = await creator.MatchProfilesAsync(childId, request.SubjectId, ct);
        return matches.Count > 0 ? matches[0].Profile : null;
    }

    /// <summary>
    /// The request's material: the series (from the profile, otherwise from the child's textbook) and
    /// the unit - explicitly named, otherwise the child's current unit. The unit is <b>checked against
    /// the series</b>: a unit from an unrelated work would be worse than none, because the model would
    /// then treat its material as confirmed.
    /// </summary>
    private async Task<(TextbookSeriesResponse? Series, SeriesUnitResponse? Unit)> ResolveMaterialAsync(
        GenerationRequest request, CreatorProfileResponse? profile, ChildFacts? child,
        int subjectId, string subjectName, CancellationToken ct)
    {
        var childBook = child?.PrimaryTextbook(subjectId, subjectName);
        if ((profile?.SeriesId ?? childBook?.SeriesId) is not int seriesId)
        {
            if (request.UnitId is not null)
                throw new AgentUsageException(
                    "--unit setzt eine Lehrwerk-Reihe voraus: hinterlege sie am Creator-Profil oder am Lehrbuch des Kindes.");
            return (null, null);
        }

        var series = await creator.GetSeriesAsync(seriesId, ct);
        var units = await creator.ListUnitsAsync(seriesId, ct: ct);

        if (request.UnitId is int wanted)
            return (series, units.FirstOrDefault(u => u.Id == wanted)
                            ?? throw new AgentUsageException(
                                $"Unit {wanted} gehört nicht zur Reihe '{series.Name}' – " +
                                "'pugling-creator profiles' zeigt die Reihe des Profils."));

        // Die Unit des Kindes zählt nur, wenn sie aus derselben Reihe kommt (Profil und Kind können auf
        // verschiedene Werke zeigen).
        var current = childBook is { SeriesId: { } bookSeries, CurrentUnitId: int currentId } && bookSeries == seriesId
            ? units.FirstOrDefault(u => u.Id == currentId)
            : null;
        return (series, current);
    }

    /// <summary>Condenses profile, series and unit into the prompt facts.</summary>
    private static ProfileFacts Facts(CreatorProfileResponse profile, TextbookSeriesResponse? series,
        SeriesUnitResponse? unit) =>
        new(profile.Id, profile.Name, profile.SubjectName, profile.SchoolTypes, profile.GradeMin, profile.GradeMax,
            profile.SourceLang, profile.TargetLang, profile.Persona, profile.Didactics,
            // Die Reihe steht am Profil; ist sie dort nicht gesetzt, stammt sie aus dem Buch des Kindes
            // und wird trotzdem gemeldet – der Stoff zählt, nicht die Herkunft der Angabe.
            series?.Id ?? profile.SeriesId, series?.Name ?? profile.SeriesName, series?.Publisher, series?.Notes, unit);

    /// <summary>
    /// The child with everything that drives the tailoring. A pure creator account sees no children; the
    /// exception points this out explicitly instead of passing through a bare 403.
    /// </summary>
    private async Task<ChildFacts> LoadChildAsync(int childId, GenerationRequest request, CancellationToken ct)
    {
        ChildResponse child;
        try
        {
            child = await supervisor.GetChildAsync(childId, ct);
        }
        catch (PuglingApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Forbidden
                                                 or System.Net.HttpStatusCode.NotFound)
        {
            throw new AgentUsageException(
                $"Kind {childId} ist für dieses Konto nicht sichtbar. Für eine individuelle Übung braucht der Agent " +
                $"ein Konto mit Creator-Rolle, das dieses Kind betreut (Supervisor). Prüfe Pugling:AccountId/Pugling:Pin – " +
                $"oder erzeuge mit --profile eine allgemeine Übung ohne Kind.");
        }

        var weighted = await LoadInterestsAsync(childId, ct);
        var textbooks = await LoadTextbooksAsync(childId, ct);
        var weakWords = request.UseWeakWords
            ? await student.ListWordMasteryAsync(childId, onlyWeak: true, take: MaxWeakWords, ct: ct)
            : [];

        return new ChildFacts(
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
            WeakWords: weakWords);
    }

    /// <summary>
    /// The weighted interests from the shared taxonomy. For the prompt, the <b>negative</b> ones matter
    /// most: a task about spiders is factually correct and still unusable if the child cannot stand
    /// spiders. If they are missing (freshly created child), the free text is all that remains.
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
