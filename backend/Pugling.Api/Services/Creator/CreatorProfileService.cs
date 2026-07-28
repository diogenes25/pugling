using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Creator;

/// <summary>
/// Findet zu einem Kind die fachkundigen Creator-Profile. Die Auswahl ist bewusst <b>deterministisch</b>
/// (harte Ausschlüsse, dann Punkte, Gleichstand über die Id – kein <c>Random</c>, kein
/// <c>string.GetHashCode</c>, vgl. <c>MediaSelector</c>): derselbe Datenstand muss denselben Lehrer
/// liefern, sonst wäre die Herkunft einer generierten Übung nicht nachvollziehbar.
/// </summary>
public class CreatorProfileService(PuglingDbContext db)
{
    // Die Gewichte der Passung. Die Reihe wiegt am schwersten, weil nur sie verrät, ob der Creator das
    // konkrete Material kennt – Fach und Klassenstufe treffen bloß das Regal, nicht das Buch.
    private const int WeightSeries = 8;
    private const int WeightSubject = 4;
    private const int WeightGrade = 2;
    private const int WeightSchoolType = 1;

    /// <summary>Stabile Begründungs-Codes (kein Fließtext – die Oberfläche formuliert, siehe i18n-Regel).</summary>
    public const string ReasonSeries = "series_match";
    public const string ReasonSubject = "subject_match";
    public const string ReasonGrade = "grade_in_range";
    public const string ReasonSchoolType = "school_type_match";

    /// <summary>
    /// Die passenden Profile, bestes zuerst. <paramref name="subjectId"/> verengt hart auf ein Fach
    /// (fachfremde Profile fallen heraus, fachneutrale bleiben); <paramref name="fatherId"/> dient nur
    /// der <c>IsOwn</c>-Anzeige.
    /// </summary>
    public async Task<IReadOnlyList<CreatorProfileMatch>> MatchAsync(int childId, int? subjectId,
        int? fatherId, CancellationToken ct = default)
    {
        var child = await db.Children.AsNoTracking()
            .Where(c => c.Id == childId)
            .Select(c => new { c.Grade, c.SchoolType })
            .FirstOrDefaultAsync(ct);
        if (child is null) return [];

        // Die Reihen, mit denen das Kind tatsächlich arbeitet – der stärkste Hinweis auf den richtigen Lehrer.
        var seriesIds = await db.Textbooks.AsNoTracking()
            .Where(t => t.ChildId == childId && t.SeriesId != null
                        && (subjectId == null || t.SubjectId == subjectId))
            .Select(t => t.SeriesId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var profiles = await db.CreatorProfiles.AsNoTracking()
            .Where(p => p.Active)
            // Fachfilter in der DB: fachneutrale Profile (SubjectId == null) bleiben bewusst drin.
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
            // Nur eine echte Eingrenzung zählt: ein Profil ohne Klassenstufen-Grenzen passt immer und
            // verdient dafür keine Punkte, sonst schlüge der Generalist den Fachlehrer.
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

            matches.Add(new CreatorProfileMatch(Map(profile, fatherId), score, reasons));
        }

        // Punkte absteigend, danach die Id aufsteigend: die Reihenfolge ist reproduzierbar.
        return [.. matches.OrderByDescending(m => m.Score).ThenBy(m => m.Profile.Id)];
    }

    /// <summary>
    /// Harte Ausschlüsse: ein Profil, das die Klassenstufe nicht unterrichtet oder für eine andere
    /// Schulart gedacht ist, ist kein schlechterer Treffer – es ist keiner.
    /// </summary>
    private static bool Fits(CreatorProfile profile, int? grade, SchoolTypes schoolType)
    {
        if (grade is int g && (profile.GradeMin > g || profile.GradeMax < g)) return false;
        // None heißt auf beiden Seiten „keine Angabe" und schließt darum nichts aus.
        if (profile.SchoolTypes != SchoolTypes.None && schoolType != SchoolTypes.None
            && (profile.SchoolTypes & schoolType) == 0) return false;
        return true;
    }

    /// <summary>Die eine Abbildung Entität → Vertrag; auch der Controller nutzt sie.</summary>
    public static CreatorProfileResponse Map(CreatorProfile p, int? fatherId) =>
        new(p.Id, p.Name, p.OwnerAdultId, ClaimsPrincipalExtensions.IsOwnedBy(p.OwnerAdultId, fatherId),
            p.SubjectName, p.SubjectId, p.SchoolTypes, p.GradeMin, p.GradeMax,
            p.SeriesId, p.Series?.Name, p.SourceLang, p.TargetLang,
            p.Persona, p.Didactics, p.DefaultTypes, p.Active, p.CreatedAt);
}
