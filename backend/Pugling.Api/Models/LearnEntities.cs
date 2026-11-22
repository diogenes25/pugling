namespace Pugling.Api.Models;

// The shared learn catalog:
//   Subject -> ExerciseCategory (controlled vocabulary)
//   TextbookSeries -> SeriesUnit -> Exercise (typed; see CurriculumEntities.cs for the series/unit)
// The catalog is maintained ONCE (not per child) and assigned to children later. Subject is reached
// from an Exercise only transitively (Exercise -> SeriesUnit -> TextbookSeries -> SubjectId) - see
// B-106 T-01: a series that is meant to carry exercises must have its SubjectId set, enforced as a
// guard on exercise creation, not as a NOT NULL column (a series without any exercise may stay subject-less).

/// <summary>School subject in the catalog (e.g. English, maths).</summary>
public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The creator who opened this subject. Same rule as <see cref="TextbookSeries.OwnerAdultId"/> and
    /// <see cref="Exercise.AuthorAdultId"/>: <b>any</b> creator may read and use the subject, only the owner
    /// may rename or delete it – otherwise a stranger renames the tree a whole household plans against
    /// (B-13). <c>null</c> = seeded system subject, therefore editable by <b>nobody</b>: the comparison in
    /// <see cref="Auth.ClaimsPrincipalExtensions.IsOwnedBy"/> is fail-closed. Survives the deletion of the
    /// owner (FK → <c>SetNull</c>) so that everyone else's series and study plans keep their subject.
    /// <para>
    /// One level down the write right has a <b>second bearer</b>
    /// (<see cref="ExerciseCategory.OwnerAdultId"/>, B-170) - this column alone does not decide who may
    /// change a category.
    /// </para>
    /// </summary>
    public int? OwnerAdultId { get; set; }
    public Adult? Owner { get; set; }

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

    /// <summary>
    /// The creator who added this category. Deliberately the <b>same</b> name and the same rule as
    /// <see cref="Subject.OwnerAdultId"/> and <see cref="TextbookSeries.OwnerAdultId"/>, because it is the
    /// same rule: any creator may read and use the category, only an owner may rename or delete it.
    /// <para>
    /// The right has <b>two</b> bearers (B-170): this column and the subject's. Either suffices, and the
    /// reason is that creating a category is open to everyone (B-157, decision 2) - without a bearer of its
    /// own, a category added to a seeded subject was editable by nobody, so a typo stayed in the study-plan
    /// pre-filter forever. Symmetry is the point: whoever may add may also remove what they added.
    /// </para>
    /// <para>
    /// <c>null</c> = a seeded category, therefore renamable and deletable by nobody but a platform admin -
    /// the same fail-closed comparison as for the subject. The owner's deletion sets it to <c>null</c>
    /// (FK → <c>SetNull</c>) rather than removing the row, so the pre-filter list of every other household
    /// survives the loss of an account.
    /// </para>
    /// </summary>
    public int? OwnerAdultId { get; set; }
    public Adult? Owner { get; set; }
}

/// <summary>
/// An exercise within a textbook series unit. The shared fields are typed;
/// the type-specific part sits as JSON in <see cref="ConfigJson"/>
/// and is read/written in the API as its own schema per type.
/// </summary>
public class Exercise
{
    public int Id { get; set; }
    public int SeriesUnitId { get; set; }
    public SeriesUnit? SeriesUnit { get; set; }
    /// <summary>Exercise type key (e.g. <c>"Vocabulary"</c>) – resolved through the <see cref="ExerciseTypeRegistry"/>; determines how <see cref="ConfigJson"/> is interpreted.</summary>
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    /// <summary>
    /// Free description text (optional). It helps to recognize the exercise while composing a study plan
    /// (what it practices, for whom, what to watch out for) and feeds the catalog's full-text search.
    /// </summary>
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    /// <summary>
    /// The author's <b>recommendation</b> for the target reward, not a setting of this exercise (B-45): a new
    /// <c>PlanPosition</c> inherits it as <c>PointsGoalMet</c> when the supervisor states nothing, and stays
    /// free to override. Copied once at creation - a later change does not move plans already running.
    /// <para>
    /// Unusable values fall back to the position's default of 20: 0 (the field carries no lower bound, so an
    /// untouched 0 must not mean "no reward"), negatives, and anything above the inheritance bound.
    /// </para>
    /// </summary>
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
    // Subject = the Subject of the series (through SeriesUnit -> TextbookSeries), topic = SeriesUnit - only what adds to that lives here.

    /// <summary>Lowest suitable grade (inclusive); null = no lower bound.</summary>
    public int? GradeMin { get; set; }
    /// <summary>Highest suitable grade (inclusive); null = no upper bound.</summary>
    public int? GradeMax { get; set; }
    /// <summary>Suitable school types; <see cref="SchoolTypes.None"/> = for all of them.</summary>
    public SchoolTypes SchoolTypes { get; set; } = SchoolTypes.None;
    /// <summary>Source of the exercise (e.g. the textbook "Green Line 3, Unit 4"); optional.</summary>
    public string? Source { get; set; }
    /// <summary>
    /// Textbook page(s) the exercise comes from, as printed ("42", "42–43", "WB 12"; at most
    /// <see cref="BookPagesMaxLength"/> characters); optional. Separate from <see cref="Source"/>, which is a
    /// catalog search field: this one reaches the child, so it can look the exercise up in its book.
    /// </summary>
    public string? BookPages { get; set; }
    /// <summary>Upper bound of <see cref="BookPages"/>: a page reference, not a note.</summary>
    public const int BookPagesMaxLength = 20;
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

    /// <summary>
    /// What other creators think of this exercise (B-12) - one row per rating creator. The catalog search
    /// aggregates it into an average and a count so that proven material can be told apart while browsing.
    /// </summary>
    public List<ExerciseRating> Ratings { get; set; } = [];

    /// <summary>
    /// The interests this exercise is <b>dressed</b> for (B-46) – the same taxonomy the child's interests
    /// and the images already hang on. It is a pre-filter axis like <see cref="GradeMin"/> or
    /// <see cref="SchoolTypes"/>, not a restriction: an exercise dressed in unicorns stays readable and
    /// usable for everybody, it merely becomes distinguishable from the robot version. Empty = dressed for
    /// nothing in particular, which is the case for every exercise written before this became recordable.
    /// </summary>
    public List<ExerciseInterestLink> InterestLinks { get; set; } = [];

    /// <summary>
    /// The grammar topics this exercise practices (B-155) - the eighth search facet. Pre-filled from the unit's
    /// topics when the exercise is created, never inherited: in a mixed unit an exercise on the passive must not
    /// silently count as "present perfect" too.
    /// </summary>
    public List<ExerciseGrammarTopic> GrammarTopicLinks { get; set; } = [];

    /// <summary>
    /// Build level of a <b>prerequisite</b> exercise on the way to <see cref="BuildTargetExerciseId"/> (B-700):
    /// 1 = recognize, 2 = fill in with a choice. <c>null</c> on every exercise that is no prerequisite - a target
    /// exercise counts as the top level (<see cref="BuildLevelTarget"/>) without carrying a value. Named "build
    /// level" because "stage" already means the play form inside one exercise.
    /// </summary>
    public int? BuildLevel { get; set; }

    /// <summary>
    /// The target exercise this one is a prerequisite of (B-700); set together with <see cref="BuildLevel"/>.
    /// Same unit, one level deep: a prerequisite is never itself a target. Deleting the target is refused while a
    /// prerequisite points at it, so a learning path never vanishes silently.
    /// </summary>
    public int? BuildTargetExerciseId { get; set; }
    public Exercise? BuildTarget { get; set; }

    /// <summary>Highest build level a prerequisite may carry; the target exercise itself is the level above.</summary>
    public const int BuildLevelMaxPrerequisite = 2;

    /// <summary>The level a target exercise stands for (it carries no <see cref="BuildLevel"/> of its own).</summary>
    public const int BuildLevelTarget = 3;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Links an <see cref="Exercise"/> to an <see cref="InterestTag"/> (n:m). Deliberately built exactly like
/// <see cref="MediaTagLink"/>, including the absence of a weight: dressing is yes/no, and a ranking would
/// invent a precision the author does not have (the child's side carries the weights, and that is where a
/// ranking belongs).
/// </summary>
public class ExerciseInterestLink
{
    public int Id { get; set; }

    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    public int InterestTagId { get; set; }
    public InterestTag? InterestTag { get; set; }
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

/// <summary>
/// A creator's rating of somebody else's exercise (B-12): 1-5 stars and an optional short comment.
/// Deliberately subjective - it is not derived from children's results, because a good exercise on an
/// unmotivated day would look bad. At most one row per (exercise, creator): rating again overwrites it,
/// and <see cref="UpdatedAt"/> is the only trace of the change - a quality signal, not an audit trail.
/// </summary>
public class ExerciseRating
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }
    /// <summary>The rating creator (= <see cref="Adult.Id"/>); never an owner of the exercise.</summary>
    public int CreatorId { get; set; }
    public Adult? Creator { get; set; }
    /// <summary>1 to 5.</summary>
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
