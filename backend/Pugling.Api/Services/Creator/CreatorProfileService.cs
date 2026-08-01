using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Creator;

/// <summary>
/// Finds the subject-matter-competent creator profiles for a child. The selection is deliberately
/// <b>deterministic</b> (hard exclusions, then score, ties broken by id – no <c>Random</c>, no
/// <c>string.GetHashCode</c>, cf. <c>MediaSelector</c>): the same data state must produce the same
/// teacher, otherwise the origin of a generated exercise would not be traceable.
/// </summary>
public class CreatorProfileService(PuglingDbContext db)
{
    // The weights of the match. The series weighs heaviest because only it reveals whether the creator knows
    // the concrete material - subject and grade only hit the shelf, not the book.
    private const int WeightSeries = 8;
    private const int WeightSubject = 4;
    private const int WeightGrade = 2;
    private const int WeightSchoolType = 1;

    /// <summary>Stable reason codes (no free text – the UI does the wording, see i18n rule).</summary>
    public const string ReasonSeries = "series_match";
    /// <summary>The profile is fixed to the same subject as the request.</summary>
    public const string ReasonSubject = "subject_match";
    /// <summary>The child's grade lies within the profile's grade range.</summary>
    public const string ReasonGrade = "grade_in_range";
    /// <summary>The profile's school type matches the child's school type.</summary>
    public const string ReasonSchoolType = "school_type_match";

    /// <summary>
    /// The matching profiles, best first. <paramref name="subjectId"/> hard-restricts to one subject
    /// (profiles for other subjects fall out, subject-neutral ones remain); <paramref name="supervisorId"/>
    /// only serves the <c>IsOwn</c> display.
    /// </summary>
    public async Task<IReadOnlyList<CreatorProfileMatch>> MatchAsync(int childId, int? subjectId,
        int? supervisorId, CancellationToken ct = default)
    {
        var child = await db.Children.AsNoTracking()
            .Where(c => c.Id == childId)
            .Select(c => new { c.Grade, c.SchoolType })
            .FirstOrDefaultAsync(ct);
        if (child is null) return [];

        // The series the child actually works with - the strongest hint at the right teacher.
        var seriesIds = await db.Textbooks.AsNoTracking()
            .Where(t => t.ChildId == childId && t.SeriesId != null
                        && (subjectId == null || t.SubjectId == subjectId))
            .Select(t => t.SeriesId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var profiles = await db.CreatorProfiles.AsNoTracking()
            .Where(p => p.Active)
            // Subject filter in the DB: subject-neutral profiles (SubjectId == null) deliberately stay in.
            .Where(p => subjectId == null || p.SubjectId == null || p.SubjectId == subjectId)
            .Include(p => p.Series)
            .OrderBy(p => p.Id)
            .ToListAsync(ct);

        var matches = new List<CreatorProfileMatch>();
        foreach (var profile in profiles)
        {
            if (!Fits(profile, child.Grade, child.SchoolType)) continue;

            var reasons = new List<string>();
            var score = 0;

            if (profile.SeriesId is int series && seriesIds.Contains(series))
            {
                score += WeightSeries;
                reasons.Add(ReasonSeries);
            }
            if (subjectId is not null && profile.SubjectId == subjectId)
            {
                score += WeightSubject;
                reasons.Add(ReasonSubject);
            }
            // Only a real narrowing counts: a profile without grade bounds always fits and earns no points
            // for it, otherwise the generalist would beat the subject teacher.
            if (child.Grade is not null && (profile.GradeMin is not null || profile.GradeMax is not null))
            {
                score += WeightGrade;
                reasons.Add(ReasonGrade);
            }
            if (child.SchoolType != SchoolTypes.None && profile.SchoolTypes != SchoolTypes.None)
            {
                score += WeightSchoolType;
                reasons.Add(ReasonSchoolType);
            }

            matches.Add(new CreatorProfileMatch(Map(profile, supervisorId), score, reasons));
        }

        // Points descending, then the id ascending: the order is reproducible.
        return [.. matches.OrderByDescending(m => m.Score).ThenBy(m => m.Profile.Id)];
    }

    /// <summary>
    /// Hard exclusions: a profile that doesn't teach the grade or is meant for a different
    /// school type is not a worse match – it is not a match at all.
    /// </summary>
    private static bool Fits(CreatorProfile profile, int? grade, SchoolTypes schoolType)
    {
        if (grade is int g && (profile.GradeMin > g || profile.GradeMax < g)) return false;
        // None means "not specified" on both sides and therefore excludes nothing.
        if (profile.SchoolTypes != SchoolTypes.None && schoolType != SchoolTypes.None
            && (profile.SchoolTypes & schoolType) == 0) return false;
        return true;
    }

    /// <summary>The single entity → contract mapping; the controller uses it too.</summary>
    public static CreatorProfileResponse Map(CreatorProfile p, int? supervisorId) =>
        new(p.Id, p.Name, p.OwnerAdultId, ClaimsPrincipalExtensions.IsOwnedBy(p.OwnerAdultId, supervisorId),
            p.SubjectName, p.SubjectId, p.SchoolTypes, p.GradeMin, p.GradeMax,
            p.SeriesId, p.Series?.Name, p.SourceLang, p.TargetLang,
            p.Persona, p.Didactics, p.DefaultTypes, p.Active, p.CreatedAt);
}
