namespace Pugling.Api.Models;

// Admin area: people management.
//   Supervisor (Adult) >-< Student (Child) through SupervisorLink (a student can have several supervisors)
//   + points per child (one shared wallet across all supervisors).
// The learning content (Subject -> Chapter -> Exercise) sits separately in the shared learn catalog
// (see LearnEntities.cs).

// SupervisorRelation/Gender/PointKind live in the contract project (Pugling.Contracts).

/// <summary>
/// Supervision relation supervisor↔student. A student can have several supervisors (father, mother,
/// grandmother …); each of them runs their own family shop. The wallet stays <b>shared</b> – who redeems
/// is decided by the purchase (see <see cref="ShopPurchase"/> with its issuer snapshot), not by the money.
/// Replaces the former 1:1 binding <c>Child.AdultId</c>.
/// </summary>
public class SupervisorLink
{
    public int Id { get; set; }
    /// <summary>The supervising adult (today an <see cref="Adult"/> profile).</summary>
    public int SupervisorId { get; set; }
    public Adult? Supervisor { get; set; }
    /// <summary>The supervised learner (a <see cref="Child"/> profile).</summary>
    public int StudentId { get; set; }
    public Child? Student { get; set; }
    public SupervisorRelation Relation { get; set; } = SupervisorRelation.Father;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// An <b>adult</b>: the domain row behind every non-child role. Authorship
/// (<see cref="Exercise.AuthorAdultId"/>) and the RWX rights (<see cref="ExerciseGrant.CreatorId"/>)
/// hang on it.
///
/// <para>
/// The same row also carries a <b>teacher account</b> that supervises no child (see
/// docs/lehrer-konto-plan.md). Whether somebody supervises is not decided by the type but by the role of
/// their account (<see cref="AccountProfile"/>) and the <see cref="SupervisorLink"/>s. "Father" stays
/// correct where a father is actually meant – for instance in
/// <see cref="SupervisorRelation.Father"/> as the kinship.
/// </para>
/// </summary>
public class Adult
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    /// <summary>Simple PIN login. To be replaced by real auth later.</summary>
    public string Pin { get; set; } = "";
    /// <summary>
    /// Platform superuser (break-glass): carries the additional <see cref="Auth.Roles.Admin"/> claim on
    /// login and thereby bypasses the RWX rights check on exercises (e.g. to edit orphaned, ownerless
    /// exercises). Deliberately not settable through the API – only via DB/seed (no self rights escalation).
    /// </summary>
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Students supervised by this supervisor (through <see cref="SupervisorLink"/>).</summary>
    public List<SupervisorLink> SupervisedLinks { get; set; } = new();
}

/// <summary>A learning child (student profile). Can have several supervisors (<see cref="SupervisorLinks"/>).</summary>
public class Child
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? BirthYear { get; set; }
    /// <summary>Current grade (1–13). Drives the pre-filtering of matching exercises in the study plan assistant.</summary>
    public int? Grade { get; set; }
    /// <summary>The child's school type – filters out exercises in the assistant that are not meant for every school type.</summary>
    public SchoolTypes SchoolType { get; set; } = SchoolTypes.None;

    // --- Exercise-independent personal profile ---
    // These values describe the child, not its subject matter. They deliberately sit here (on the child) and
    // not on an exercise/plan, so that a later AI generator can derive an individual study plan from them:
    // hit the subject matter (Grade/SchoolType/textbooks) and embed it in topics the child cares about
    // (Interests). See wiki/09-llm-kochbuch.md.

    /// <summary>Gender (purely descriptive; persisted as a string, readable/stable).</summary>
    public Gender Gender { get; set; } = Gender.None;

    /// <summary>
    /// The child's free-form interests/preferences ("Brawl Stars", "Pokémon", "football"). They serve a
    /// later generator as themes the (fixed) subject matter is embedded into – they never change <i>what</i>
    /// is learned, only its dressing (cloze sentences, word problems, contexts). Stored as a JSON list
    /// (reassign in the controller, no in-place mutation – a missing ValueComparer is a pitfall otherwise).
    /// </summary>
    public List<string> Interests { get; set; } = [];

    /// <summary>Optional free text for everything unstructured a generator should know
    /// (learning difficulties, motivation hints). Deliberately free-form so it can be added to without a schema change.</summary>
    public string? ProfileNotes { get; set; }

    /// <summary>
    /// Weighted interests from the controlled taxonomy (including dislikes at a negative weight).
    /// Complements the free-form <see cref="Interests"/>, it does not replace it: free text is the language
    /// of the AI creator, referenced tags are the basis of the machine-driven image selection.
    /// </summary>
    public List<ChildInterest> InterestTags { get; set; } = [];

    /// <summary>
    /// Upper bound of image suitability for this child – the selection never returns an asset above it.
    /// Only the supervisor may raise it; the default is the strictest level.
    /// </summary>
    public ContentRating AllowedContentRating { get; set; } = ContentRating.Everyone;
    /// <summary>The child's simple PIN login. To be replaced by real auth later.</summary>
    public string Pin { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The child's currently equipped skin (character). Persisted server-side so that the choice applies
    /// across devices instead of hanging on one device's localStorage.
    /// </summary>
    public string SelectedSkin { get; set; } = SkinCatalog.Default;

    /// <summary>
    /// Unlocked skins. The server is the source of truth for ownership so that a skin can only be
    /// unlocked after a real coin redemption (no client-side cheating).
    /// Stored as a JSON list (reassign in the controller, no in-place mutation).
    /// </summary>
    public List<string> OwnedSkins { get; set; } = [SkinCatalog.Default];

    /// <summary>
    /// Concurrency stamp: re-set on every skin purchase/equip and checked as the EF concurrency token.
    /// Prevents two parallel purchases (double click/retry) from both passing the funds check and
    /// debiting twice or overwriting the skin list – the loser then runs into a
    /// <c>DbUpdateConcurrencyException</c> (→ 409) instead of duplicating.
    /// </summary>
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    public List<ChildPointsEntry> PointsEntries { get; set; } = new();

    /// <summary>Supervisors of this student (through <see cref="SupervisorLink"/>).</summary>
    public List<SupervisorLink> SupervisorLinks { get; set; } = new();

    /// <summary>Textbooks used by the child (exercise-independent profile, see <see cref="Textbook"/>).</summary>
    public List<Textbook> Textbooks { get; set; } = new();
}

/// <summary>
/// A textbook used by the child (exercise-independent profile). Records which work and which current
/// chapter the subject matter comes from – the basis a later generator derives "what is due right now"
/// from. <see cref="Title"/> + <see cref="CurrentChapter"/> can be matched against the free-text
/// <c>Exercise.Source</c> (e.g. "Green Line 3, Unit 4") to reuse existing exercises.
/// </summary>
public class Textbook
{
    public int Id { get; set; }
    /// <summary>The child the book is assigned to (cascade – disappears with the child).</summary>
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    /// <summary>Title of the work, e.g. "Green Line 3".</summary>
    public string Title { get; set; } = "";
    /// <summary>Subject as free text, e.g. "Englisch" – the subject need not exist in the catalog.</summary>
    public string? SubjectName { get; set; }
    /// <summary>Optional catalog link to a <see cref="Subject"/> where an exact assignment is possible.</summary>
    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    /// <summary>Which grade the book is meant for.</summary>
    public int? Grade { get; set; }
    public string? Publisher { get; set; }
    public string? Isbn { get; set; }
    /// <summary>Current position in the book, e.g. "Unit 4 – Past Tense".</summary>
    public string? CurrentChapter { get; set; }

    /// <summary>
    /// Optional link to the cataloged <see cref="TextbookSeries"/>. Only it makes the question
    /// "which creator knows this material?" decidable (series match in the profile matching);
    /// <see cref="Title"/>/<see cref="Publisher"/> remain the fallback for uncataloged works.
    /// </summary>
    public int? SeriesId { get; set; }
    public TextbookSeries? Series { get; set; }
    /// <summary>
    /// The unit the child is currently working through – the structured form of <see cref="CurrentChapter"/>.
    /// Only with it does the creator know the unit's subject matter (topics/grammar/vocabulary) instead of
    /// just its name.
    /// </summary>
    public int? CurrentUnitId { get; set; }
    public SeriesUnit? CurrentUnit { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Catalog of the purchasable skins including their cost – the <b>server-side source of truth</b>. Costs
/// are never taken from the client; the frontend only supplies the visual representation
/// (emoji/gradient). The IDs must match the frontend catalog (<c>frontend/src/lib/skins.ts</c>).
/// </summary>
public static class SkinCatalog
{
    /// <summary>Free starter, unlocked from the beginning.</summary>
    public const string Default = "pug";

    /// <summary>Purchasable skins: ID → cost in gems (0 = free).</summary>
    public static readonly IReadOnlyDictionary<string, int> Costs = new Dictionary<string, int>
    {
        ["pug"] = 0,
        ["fox"] = 300,
        ["dragon"] = 800,
        ["robot"] = 1200,
        ["ninja"] = 2000,
    };

    /// <summary>Cost of a skin, or <c>null</c> if the ID is unknown.</summary>
    public static int? CostOf(string skinId) => Costs.TryGetValue(skinId, out var c) ? c : null;
}

/// <summary>A child's points ledger entry (positive = credited, negative = redeemed).</summary>
public class ChildPointsEntry
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public int Amount { get; set; }
    /// <summary>Category of the ledger entry (for reporting/capping the bonus sources).</summary>
    public PointKind Kind { get; set; } = PointKind.Base;
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
