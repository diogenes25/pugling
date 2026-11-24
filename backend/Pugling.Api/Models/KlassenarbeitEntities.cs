namespace Pugling.Api.Models;

// Tagging + class tests:
//   Tag (per child) --< ExerciseTag >-- Exercise (shared catalog)
//   Klassenarbeit (per child) --< KlassenarbeitExercise >-- Exercise
//   Klassenarbeit --< KlassenarbeitTag >-- Tag  (exercises carrying a tag count as relevant)
//
// The learn catalog (TextbookSeries -> SeriesUnit -> Exercise) stays child-neutral; assigning "which exercise is
// relevant for this child / this class test" happens exclusively through these join tables. Supervisor AND
// child may set tags, class tests are maintained by the supervisor only.

// TaggedBy lives in the contract project (Pugling.Contracts).

/// <summary>
/// A freely named keyword in the context of one child (e.g. "Unit 5", "irregular verbs").
/// Supervisor and child use it to mark catalog exercises, for instance as relevant for a certain class test.
/// </summary>
public class Tag
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Optional display color (hex, e.g. "#3b82f6") for the UI.</summary>
    public string? Color { get; set; }
    /// <summary>Who created the keyword.</summary>
    public TaggedBy CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ExerciseTag> ExerciseTags { get; set; } = new();
    public List<VocabularyTag> VocabularyTags { get; set; } = new();
}

/// <summary>Links a catalog exercise to a <see cref="Tag"/> and records who set it.</summary>
public class ExerciseTag
{
    public int Id { get; set; }
    public int TagId { get; set; }
    public Tag? Tag { get; set; }
    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Links a store <see cref="Vocabulary"/> entry to a child-scoped <see cref="Tag"/> and records who set it.
/// This is how supervisor/child mark single vocabulary entries as relevant (e.g. for a class test).
/// <para>Not to be confused with the global <see cref="VocabTag"/>/<see cref="VocabTagLink"/>: that one is
/// child-neutral (chapter/grade/topic), this link carries the child context through the <see cref="Tag"/>.</para>
/// </summary>
public class VocabularyTag
{
    public int Id { get; set; }
    public int TagId { get; set; }
    public Tag? Tag { get; set; }
    public int VocabularyId { get; set; }
    public Vocabulary? Vocabulary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// KlassenarbeitStatus lives in the contract project (Pugling.Contracts).

/// <summary>
/// A planned or already written class test of a child. The supervisor plans it, assigns relevant exercises
/// (directly or through tags) and enters the grade after it has been written.
/// </summary>
public class Klassenarbeit
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    /// <summary>Optional link to the catalog subject.</summary>
    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public string Title { get; set; } = "";
    /// <summary>Free-text topic/subject matter of the test (e.g. "Simple Past, Unit 3–4").</summary>
    public string? Topic { get; set; }
    /// <summary>Date: planned or actual day it is written.</summary>
    public DateOnly ScheduledDate { get; set; }
    public KlassenarbeitStatus Status { get; set; } = KlassenarbeitStatus.Planned;
    /// <summary>German school grade 1.0 (very good) … 6.0 (insufficient). Null while not yet entered.</summary>
    public decimal? Grade { get; set; }
    /// <summary>Optional note on the grade (e.g. "vocabulary was solid, grammar weak").</summary>
    public string? GradeComment { get; set; }
    /// <summary>
    /// Days before the test in which its preparation duty runs (B-752); <c>null</c> = the configured default, 0 = no
    /// preparation duty for this test.
    /// </summary>
    public int? PrepWindowDays { get; set; }
    /// <summary>
    /// The learning day the preparation duty was started (B-752): its bundle was frozen then, and the reconcile never
    /// starts it a second time. <c>null</c> = not started.
    /// </summary>
    public DateOnly? PrepStartedOn { get; set; }
    /// <summary>
    /// The bundle exercises the start left out for lack of an execute permission (B-947), as a JSON list. A grant given
    /// later makes exactly these a duty on the next reconcile; anything that joins the bundle after the start stays out,
    /// because the bundle is frozen (B-752 decision 3). <c>null</c> = not started, or started before this column existed.
    /// </summary>
    public List<int>? PrepLeftOutExerciseIds { get; set; }
    /// <summary>
    /// The child's grade level in the test's subject, written once on the test day (B-744) so a grade entered days later
    /// is compared with what the level predicted on that day, not with where it drifted since. <c>null</c> = no snapshot:
    /// the day has not come, nobody accessed the app on it, the test has no subject or the subject had no measured level.
    /// </summary>
    public int? PredictedLevel { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<KlassenarbeitExercise> Exercises { get; set; } = new();
    public List<KlassenarbeitTag> Tags { get; set; } = new();
}

/// <summary>Direct assignment of an exercise to a class test.</summary>
public class KlassenarbeitExercise
{
    public int Id { get; set; }
    public int KlassenarbeitId { get; set; }
    public Klassenarbeit? Klassenarbeit { get; set; }
    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Links a class test to a <see cref="Tag"/>: every exercise marked with that tag counts (in addition to
/// the directly assigned ones) as relevant for the test.
/// </summary>
public class KlassenarbeitTag
{
    public int Id { get; set; }
    public int KlassenarbeitId { get; set; }
    public Klassenarbeit? Klassenarbeit { get; set; }
    public int TagId { get; set; }
    public Tag? Tag { get; set; }
}
