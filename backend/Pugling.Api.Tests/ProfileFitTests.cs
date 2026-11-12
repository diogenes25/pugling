using Pugling.Agent.Creator.Briefing;

namespace Pugling.Api.Tests;

/// <summary>
/// The console picks a book or a profile with <see cref="ProfileFit.FitsSubjectOf"/>; the briefing then judges the same
/// profile with <see cref="ProfileFit.Problem"/> on the subject <see cref="ProfileFit.SubjectIdOf"/> gives. Two functions
/// for one decision escaped twice (B-498, B-500, B-506): the console offered what the briefing then rejected. This holds
/// them to one answer over every kind of series and profile, so a change to one side alone turns red.
/// </summary>
public class ProfileFitTests
{
    private const int English = 1;
    private const int Latin = 2;
    private static readonly Dictionary<int, string> Catalog = new() { [English] = "Englisch", [Latin] = "Latein" };

    private static TextbookSeriesResponse Series(int? subjectId) =>
        new(10, "Access", "access", null, null, subjectId is int id ? Catalog[id] : null, subjectId, SchoolTypes.Gymnasium,
            "en", "de", null, null, IsOwn: true, UnitCount: 1, null, null, DateTime.UnixEpoch);

    private static CreatorProfileResponse Profile(int? subjectId, string? subjectName) =>
        new(20, "Lehrer", null, IsOwn: true, subjectName, subjectId, SchoolTypes.Gymnasium, null, null, SeriesId: null,
            SeriesName: null, "en", "de", null, null, [], Active: true, DateTime.UnixEpoch);

    [Fact]
    public void Konsole_und_Briefing_entscheiden_das_Fach_gleich()
    {
        int?[] seriesSubjects = [English, null];
        (int? Id, string? Name)[] profiles =
        [
            (English, "Englisch"), (Latin, "Latein"), (null, " englisch "), (null, "Latein"), (null, null),
        ];
        var mismatches = new List<string>();
        foreach (var seriesSubject in seriesSubjects)
            foreach (var (id, name) in profiles)
            {
                var series = Series(seriesSubject);
                var profile = Profile(id, name);
                // The briefing's verdict: no subject at all stops the console before the briefing, which counts as a misfit.
                var sinkAccepts = ProfileFit.SubjectIdOf(series, profile) is int subjectId
                                  && ProfileFit.Problem(profile, series,
                                      new SubjectResponse(subjectId, Catalog[subjectId], DateTime.UnixEpoch, 0, null, true)) is null;
                if (ProfileFit.FitsSubjectOf(profile, series) != sinkAccepts)
                    mismatches.Add($"series subject {seriesSubject?.ToString() ?? "none"}, profile ({id?.ToString() ?? "-"}, "
                                   + $"'{name}'): console {!sinkAccepts}, briefing {sinkAccepts}");
            }

        Assert.True(mismatches.Count == 0, string.Join(Environment.NewLine, mismatches));
    }
}
