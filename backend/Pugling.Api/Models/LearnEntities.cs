namespace Pugling.Api.Models;

// The shared learn catalog:
//   Subject -> Chapter -> Exercise (typed)
// The catalog is maintained ONCE (not per child) and assigned to children later.

/// <summary>School subject in the catalog (e.g. English, maths).</summary>
public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Chapter> Chapters { get; set; } = new();

    /// <summary>Subject-dependent exercise categories (e.g. grammar/vocabulary for English).</summary>
    public List<ExerciseCategory> Categories { get; set; } = new();
}

/// <summary>
/// Subject-dependent "category" of an exercise (e.g. grammar/vocabulary for languages,
/// basic arithmetic/algebra for maths). A child-neutral, controlled vocabulary per subject –
/// it serves the pre-filtering of exercises while a study plan is being put together.
/// </summary>
public class ExerciseCategory
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Chapter within a subject.</summary>
public class Chapter
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public string Name { get; set; } = "";
    public int OrderIndex { get; set; }

    public List<Exercise> Exercises { get; set; } = new();
}

/// <summary>
/// An exercise within a chapter. The shared fields are typed;
/// the type-specific part sits as JSON in <see cref="ConfigJson"/>
/// and is read/written in the API as its own schema per type.
/// </summary>
public class Exercise
{
    public int Id { get; set; }
    public int ChapterId { get; set; }
    public Chapter? Chapter { get; set; }
    /// <summary>Exercise type key (e.g. <c>"Vocabulary"</c>) – resolved through the <see cref="ExerciseTypeRegistry"/>; determines how <see cref="ConfigJson"/> is interpreted.</summary>
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    /// <summary>
    /// Free description text (optional). It helps to recognize the exercise while composing a study plan
    /// (what it practices, for whom, what to watch out for) and feeds the catalog's full-text search.
    /// </summary>
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    /// <summary>Points the child receives for completing it.</summary>
    public int RewardPoints { get; set; }
    /// <summary>Type-specific configuration as JSON (see the *Config classes).</summary>
    public string ConfigJson { get; set; } = "{}";
    /// <summary>Optional bonus suggestion of the author (a template, copied when the plan is created).</summary>
    public SuggestedBonus? SuggestedBonus { get; set; }

    // Suggested defaults for a study plan position (hybrid principle: the position inherits them as long as
    // it does not override them itself - see PlanPosition.Stage/ItemCount).
    /// <summary>Recommended test stage (interpreted per method); null = the method's default.</summary>
    public int? DefaultStage { get; set; }
    /// <summary>Recommended number of contents used per position; null = all of them.</summary>
    public int? DefaultItemCount { get; set; }
    /// <summary>Default for the Leitner box (the exercise's suggestion; a position may override it).</summary>
    public bool DefaultUseLeitner { get; set; }
    /// <summary>Default for "only typed/graded tests count" (the exercise's suggestion; a position may override it).</summary>
    public bool DefaultRequireTypedTest { get; set; }

    // Structured metadata for pre-filtering while a study plan is composed.
    // Subject = Subject (through Chapter), topic = Chapter - only what adds to that lives here.

    /// <summary>Lowest suitable grade (inclusive); null = no lower bound.</summary>
    public int? GradeMin { get; set; }
    /// <summary>Highest suitable grade (inclusive); null = no upper bound.</summary>
    public int? GradeMax { get; set; }
    /// <summary>Suitable school types; <see cref="SchoolTypes.None"/> = for all of them.</summary>
    public SchoolTypes SchoolTypes { get; set; } = SchoolTypes.None;
    /// <summary>Source of the exercise (e.g. the textbook "Green Line 3, Unit 4"); optional.</summary>
    public string? Source { get; set; }
    /// <summary>Subject-dependent category (FK to <see cref="ExerciseCategory"/>); optional.</summary>
    public int? CategoryId { get; set; }
    public ExerciseCategory? Category { get; set; }

    /// <summary>
    /// Author of the exercise (the adult who created it). The catalog is deliberately <b>global</b>:
    /// every adult may <i>find and use</i> every exercise, but only the author may <i>change or delete</i>
    /// it – that keeps an exercise created by a teacher protected while other adults take it into their
    /// study plans. <c>null</c> = seeded system exercise (owned by nobody, therefore not editable).
    /// Survives the deletion of the author (FK → <c>SetNull</c>) so that other people's study plans
    /// referencing it do not break.
    /// </summary>
    public int? AuthorAdultId { get; set; }
    public Adult? Author { get; set; }

    /// <summary>
    /// Whether the exercise is executable <b>for every</b> creator (can be taken into study plans/class tests).
    /// <c>true</c> (default) = the previous behavior (anyone may assign it). If an owner sets it to <c>false</c>,
    /// only owners and creators holding an execute/write <see cref="ExerciseGrant"/> may assign the exercise.
    /// It only affects <i>new</i> assignments – plans already running stay untouched.
    /// </summary>
    public bool ExecutePublic { get; set; } = true;

    /// <summary>RWX rights granted to individual creators (owner/write/execute) – see <see cref="ExerciseGrant"/>.</summary>
    public List<ExerciseGrant> Grants { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A right on an exercise granted to a single creator. It replaces the former single-author model:
/// the original <see cref="Exercise.AuthorAdultId"/> becomes the first <see cref="GrantPermission.Owner"/>;
/// further owner/write/execute rights are added through this table (co-authoring, controlled sharing).
/// Pattern analogous to <see cref="SupervisorLink"/> (surrogate PK + unique composite index, both FKs cascade).
/// </summary>
public class ExerciseGrant
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }
    /// <summary>The creator the right is granted to (= <see cref="Adult.Id"/>).</summary>
    public int CreatorId { get; set; }
    public Adult? Creator { get; set; }
    public GrantPermission Permission { get; set; }
    /// <summary>Audit: which adult granted the right (null for the seeded grant).</summary>
    public int? GrantedByAdultId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
