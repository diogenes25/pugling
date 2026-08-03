namespace Pugling.Contracts.Creator;

/// <summary>
/// Lean, type-spanning view of a catalog exercise – for lists in which exercises of
/// different types appear together (tagged exercises, exercises of a class test).
/// <para>
/// Deliberately WITHOUT the type-specific configuration: it carries the solutions, alternatives and
/// the listening transcript. Endpoints listing exercises are reachable for a student token (tags, class
/// tests), so a config field here would hand the child the answer to every assigned exercise – the very
/// assurance the play path upholds by withholding <c>reveal</c>. Whoever needs the configuration reads
/// the creator-gated type detail (<c>creator/subjects/{}/chapters/{}/&lt;type&gt;/{id}</c>).
/// </para>
/// </summary>
public record ExerciseBrief(
    int Id, int ChapterId, string ChapterName, int? SubjectId, string SubjectName,
    string Type, string Title, int RewardPoints);
