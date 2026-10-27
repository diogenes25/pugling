namespace Pugling.Contracts.Creator;

// Grammar topics (B-155): a shared, subject-bound vocabulary that exercises AND units carry, so a plan can be filled
// by topic across series and grades. The free text on the unit (`SeriesUnitResponse.Grammar`) stays untouched.

/// <summary>
/// A grammar topic of the shared vocabulary. <paramref name="IsOwn"/> = the caller may change and delete it;
/// a seeded topic (<paramref name="OwnerAdultId"/> <c>null</c>) is changeable by nobody. The two counts show
/// whether a topic is in use - a used topic cannot be deleted.
/// </summary>
/// <param name="Id">Topic id.</param>
/// <param name="Slug">Stable, subject-qualified key ("englisch-present-perfect"). Immutable.</param>
/// <param name="Label">Display name.</param>
/// <param name="Synonyms">Alternative spellings, a search aid.</param>
/// <param name="Color">Optional display color (hex).</param>
/// <param name="SubjectId">The subject the topic belongs to (mandatory).</param>
/// <param name="OwnerAdultId">Creator who owns the topic; <c>null</c> = seeded.</param>
/// <param name="IsOwn">Whether the caller may change or delete the topic.</param>
/// <param name="ExerciseCount">How many exercises carry the topic.</param>
/// <param name="UnitCount">How many units carry the topic.</param>
/// <param name="CreatedAt">Creation time (UTC).</param>
public record GrammarTopicResponse(int Id, string Slug, string Label, IReadOnlyList<string> Synonyms, string? Color,
    int SubjectId, int? OwnerAdultId, bool IsOwn, int ExerciseCount, int UnitCount, DateTime CreatedAt);

/// <summary>
/// Creating a topic. The slug is derived from the subject and the label; if it already exists, the endpoint
/// returns the existing entry (idempotent), so a label typed twice never becomes two topics.
/// </summary>
/// <param name="Label">Display name, e.g. "Present perfect".</param>
/// <param name="SubjectId">The subject the topic belongs to.</param>
/// <param name="Synonyms">Alternative spellings.</param>
/// <param name="Color">Optional display color (hex).</param>
public record CreateGrammarTopicDto(string Label, int SubjectId, List<string>? Synonyms = null, string? Color = null);

/// <summary>Only fields that are set are changed; <c>Synonyms</c> replaces the list entirely. The slug and the subject never change.</summary>
/// <param name="Label">New display name.</param>
/// <param name="Synonyms">New list of alternative spellings.</param>
/// <param name="Color">New display color (hex); empty clears it.</param>
public record UpdateGrammarTopicDto(string? Label = null, List<string>? Synonyms = null, string? Color = null);
