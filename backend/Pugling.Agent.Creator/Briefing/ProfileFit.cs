namespace Pugling.Agent.Creator.Briefing;

/// <summary>
/// Whether a teacher profile may speak for an exercise filed in a series. A profile of another work, or of another
/// subject, would describe a book the material is not from, and the briefing would search word lists and grammar
/// topics in the wrong subject (B-484, B-488). The briefing's rule, for a named profile and a matched one alike: create,
/// plan and exam all pass the briefing before anything is written, so no command checks it a second time.
/// </summary>
public static class ProfileFit
{
    /// <summary>
    /// Why <paramref name="profile"/> does not fit <paramref name="series"/> and the exercise's
    /// <paramref name="subject"/>, or <c>null</c> when it does. The subject is the request's, which the briefing has
    /// already held to the series' own where the series has one - so a series without a subject is checked too (B-495).
    /// </summary>
    public static string? Problem(CreatorProfileResponse profile, TextbookSeriesResponse series, SubjectResponse subject)
    {
        if (profile.SeriesId is int own && own != series.Id)
            return $"Creator-Profil {profile.Id} ({profile.Name}) gehört zur Reihe {own}, die Übung entstünde in Reihe "
                   + $"{series.Id} ({series.Name}) – lass --series/--series-unit weg oder wähle ein Profil dieser Reihe.";
        if (TeachesAnother(profile, subject) is not { } taught) return null;
        // Bound to this very series, the profile contradicts itself: another series fails the check above, another
        // profile is the only way out besides fixing this one - never "a series in its subject" (the circle of B-495).
        return profile.SeriesId == series.Id
            ? $"Creator-Profil {profile.Id} ({profile.Name}) ist an Reihe {series.Id} ({series.Name}) gebunden, "
              + $"unterrichtet aber {taught} statt {subject.Name} – wähle ein anderes Profil oder korrigiere das Fach "
              + "dieses Profils (Vater-Web, Fachlehrer), oder seine Reihe, wenn es zu einem anderen Werk gehört."
            : $"Creator-Profil {profile.Id} ({profile.Name}) unterrichtet {taught}, die Übung gehört zu {subject.Name} "
              + $"(Reihe {series.Id}, {series.Name}) – wähle ein Profil für {subject.Name} oder eine Reihe in {taught}.";
    }

    /// <summary>
    /// The profile's subject when it is another than <paramref name="subject"/>, else <c>null</c>. A profile without a
    /// subject id may still name one as free text (B-142) - that name counts, case-insensitively; one without either
    /// teaches every subject.
    /// </summary>
    private static string? TeachesAnother(CreatorProfileResponse profile, SubjectResponse subject)
    {
        if (profile.SubjectId is int id) return id == subject.Id ? null : profile.SubjectName ?? $"Fach {id}";
        var name = profile.SubjectName?.Trim();
        return string.IsNullOrEmpty(name) || string.Equals(name, subject.Name.Trim(), StringComparison.OrdinalIgnoreCase)
            ? null
            : name;
    }
}
