namespace Pugling.Agent.Creator.Briefing;

/// <summary>
/// Whether a teacher profile may speak for an exercise filed in a series. A profile of another work, or of another
/// subject, would describe a book the material is not from, and the briefing would search word lists and grammar
/// topics in the wrong subject (B-484, B-488). The briefing's rule, for a named profile and a matched one alike: create,
/// plan and exam all pass the briefing before anything is written. The console applies the same rule, never another one:
/// <see cref="FitsSubjectOf"/> picks a book or a profile it would otherwise guess (B-498, B-500, B-506, B-510).
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
        if (TeachesAnother(profile, subject.Id, subject.Name) is not { } taught) return null;
        if (profile.SeriesId != series.Id)
            return $"Creator-Profil {profile.Id} ({profile.Name}) unterrichtet {taught}, die Übung gehört zu {subject.Name} "
                   + $"(Reihe {series.Id}, {series.Name}) – wähle ein Profil für {subject.Name} oder eine Reihe in {taught}.";
        // Bound to this very series, the profile contradicts itself: another series fails the check above, so "a series
        // in its subject" would be a circle (B-495). Correcting the profile is a way out only for its owner (B-500).
        var bound = $"Creator-Profil {profile.Id} ({profile.Name}) ist an Reihe {series.Id} ({series.Name}) gebunden, "
                    + $"unterrichtet aber {taught} statt {subject.Name}";
        return profile.IsOwn
            ? bound + " – wähle ein anderes Profil oder korrigiere das Fach dieses Profils (Vater-Web, Fachlehrer), oder "
                    + "seine Reihe, wenn es zu einem anderen Werk gehört."
            : bound + " – es gehört nicht diesem Konto; wähle ein anderes Profil (--profile <id>).";
    }

    /// <summary>
    /// Why no exercise can be filed in <paramref name="series"/>: it has no subject, and the server files none there
    /// (<c>series_without_subject</c>). The way out depends on who may set one: the owner can, nobody can change a series of
    /// the base stock (no owner), and anyone else can only ask the owner (B-508). Another series is a way out only when the
    /// named profile does not hold the user to this one (<paramref name="boundByNamedProfile"/>, B-511) - then it is
    /// another profile.
    /// </summary>
    public static string NoSubject(TextbookSeriesResponse series, bool boundByNamedProfile = false)
    {
        var elsewhere = boundByNamedProfile
            ? "wähle ein Profil einer anderen Reihe (--profile <id>), das gewählte ist an diese gebunden"
            : "wähle mit --series eine Reihe mit Fach";
        return $"Reihe {series.Id} ({series.Name}) hat kein Fach zugeordnet, und ohne Fach legt der Server keine Übung an – "
               + (series.IsOwn
                   ? $"setze eines im Vater-Web (Lehrwerke → Reihe bearbeiten) oder {elsewhere}."
                   : series.OwnerAdultId is null
                       ? $"sie gehört zum Grundbestand, niemand kann sie ändern; {elsewhere}."
                       : $"sie gehört nicht diesem Konto; bitte ihren Eigentümer, ein Fach zu setzen, oder {elsewhere}.");
    }

    /// <summary>
    /// The subject an exercise in <paramref name="series"/> gets: the series' own, the profile's only where the series has
    /// none (B-488). <c>null</c> when neither has one - the console stops there before the briefing. The one place this
    /// is decided, so the console's pick and the briefing's check cannot drift apart (B-506).
    /// </summary>
    public static int? SubjectIdOf(TextbookSeriesResponse series, CreatorProfileResponse? profile) =>
        series.SubjectId ?? profile?.SubjectId;

    /// <summary>
    /// Whether <paramref name="profile"/> teaches the subject an exercise in <paramref name="series"/> would get - the
    /// subject comparison of <see cref="Problem"/> on the subject of <see cref="SubjectIdOf"/>. A book's own subject is not
    /// bound to its series' and would decide differently (B-506).
    /// </summary>
    public static bool FitsSubjectOf(CreatorProfileResponse profile, TextbookSeriesResponse series) =>
        SubjectIdOf(series, profile) is int subjectId
        && TeachesAnother(profile, subjectId, subjectId == series.SubjectId ? series.SubjectName : profile.SubjectName) is null;

    /// <summary>
    /// The profile's subject when it is another than the given one, else <c>null</c>. A profile without a subject id may
    /// still name one as free text (B-142) - that name counts, trimmed and case-insensitively; one without either
    /// teaches every subject.
    /// </summary>
    private static string? TeachesAnother(CreatorProfileResponse profile, int? subjectId, string? subjectName)
    {
        if (profile.SubjectId is int id) return id == subjectId ? null : profile.SubjectName ?? $"Fach {id}";
        var name = profile.SubjectName?.Trim();
        return string.IsNullOrEmpty(name)
               || string.Equals(name, subjectName?.Trim(), StringComparison.OrdinalIgnoreCase)
            ? null
            : name;
    }
}
