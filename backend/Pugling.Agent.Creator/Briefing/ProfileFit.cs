namespace Pugling.Agent.Creator.Briefing;

/// <summary>
/// Whether a teacher profile may speak for an exercise filed in a series. A profile of another work, or of another
/// subject, would describe a book the material is not from, and the briefing would search word lists and grammar
/// topics in the wrong subject (B-484, B-488). The briefing's rule, for a named profile and a matched one alike: create,
/// plan and exam all pass the briefing before anything is written, so no command checks it a second time.
/// </summary>
public static class ProfileFit
{
    /// <summary>Why <paramref name="profile"/> does not fit <paramref name="series"/>, or <c>null</c> when it does.</summary>
    public static string? Problem(CreatorProfileResponse profile, TextbookSeriesResponse series)
    {
        if (profile.SeriesId is int own && own != series.Id)
            return $"Creator-Profil {profile.Id} ({profile.Name}) gehört zur Reihe {own}, die Übung entstünde in Reihe "
                   + $"{series.Id} ({series.Name}) – lass --series/--series-unit weg oder wähle ein Profil dieser Reihe.";
        // A series without a subject decides nothing here; the subject then comes from the profile.
        if (profile.SubjectId is int subject && series.SubjectId is int seriesSubject && subject != seriesSubject)
            return $"Creator-Profil {profile.Id} ({profile.Name}) unterrichtet {profile.SubjectName}, Reihe {series.Id} "
                   + $"({series.Name}) gehört zu {series.SubjectName} – wähle ein Profil für {series.SubjectName} oder "
                   + $"eine Reihe in {profile.SubjectName}.";
        return null;
    }
}
