namespace Pugling.Api.Models;

// The teaching side of the catalog: which work covers a subject (TextbookSeries -> SeriesUnit) and who
// teaches it (CreatorProfile). Both are child-neutral and maintained ONCE - the child only points at them
// through its Textbook. Ownership works as with the exercise: globally readable, only the owner may write;
// a deleted owner only clears the FK (SetNull) so that other people's references do not break.

/// <summary>
/// A publisher ("Cornelsen", "Klett") as a <b>shared</b>, slug-idempotent vocabulary entry - pattern
/// <c>InterestTag</c>: no <c>OwnerAdultId</c>, because naming a publisher is not authorship (unlike a
/// <see cref="TextbookSeries"/>, which a creator actually builds and owns).
/// </summary>
public class Publisher
{
    public int Id { get; set; }
    /// <summary>Display name, e.g. "Cornelsen".</summary>
    public string Name { get; set; } = "";
    /// <summary>Normalized, globally unique key ("cornelsen"). Immutable.</summary>
    public string Slug { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A textbook series ("Access", "Green Line") as a <b>shared</b> entity. Only that makes the question
/// "which creator knows this child's material?" answerable by machine: the child's <see cref="Textbook"/>
/// and the <see cref="CreatorProfile"/> point at the same record instead of comparing free-text titles.
/// The <see cref="Slug"/> makes creation idempotent (pattern: <c>InterestTag</c>).
/// </summary>
public class TextbookSeries
{
    public int Id { get; set; }
    /// <summary>Display name of the series, e.g. "Access".</summary>
    public string Name { get; set; } = "";
    /// <summary>Normalized, globally unique key of the series ("access"). Immutable.</summary>
    public string Slug { get; set; } = "";
    /// <summary>Optional catalog link to the <see cref="Publisher"/> that publishes this series.</summary>
    public int? PublisherId { get; set; }
    public Publisher? Publisher { get; set; }
    /// <summary>Subject as free text ("Englisch") – the subject need not exist in the catalog.</summary>
    public string? SubjectName { get; set; }
    /// <summary>Optional catalog link to a <see cref="Subject"/> where an exact assignment is possible.</summary>
    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    /// <summary>School types the series is meant for; <see cref="SchoolTypes.None"/> = for all of them.</summary>
    public SchoolTypes SchoolTypes { get; set; } = SchoolTypes.None;
    /// <summary>For language series the language being learned (language code, e.g. <c>en</c>).</summary>
    public string? SourceLanguage { get; set; }
    /// <summary>For language series the native language (language code, e.g. <c>de</c>).</summary>
    public string? TargetLanguage { get; set; }
    /// <summary>Free-form notes on the work (structure, particularities) – context for the AI creator.</summary>
    public string? Notes { get; set; }
    /// <summary>Who created the series and may change it; <c>null</c> = seeded, owned by nobody.</summary>
    public int? OwnerAdultId { get; set; }
    public Adult? Owner { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<SeriesUnit> Units { get; set; } = [];
}

/// <summary>
/// A unit of the series, including its volume. Volume and unit deliberately live on <b>one</b> level
/// (<see cref="Grade"/> = volume): "Access 8, Unit 3" is one row, not a two-level tree.
/// <see cref="Topics"/>, <see cref="Grammar"/> and <see cref="VocabularyNotes"/> are the actual gain of
/// this table – they make the creator <i>familiar with the material</i> instead of letting it guess the
/// unit's subject matter.
/// </summary>
public class SeriesUnit
{
    public int Id { get; set; }
    public int SeriesId { get; set; }
    public TextbookSeries? Series { get; set; }
    /// <summary>Volume of the series, expressed as a grade (Access 8 → 8); null = no volume.</summary>
    public int? Grade { get; set; }
    /// <summary>Order within the volume.</summary>
    public int OrderIndex { get; set; }
    /// <summary>Label as printed in the book, e.g. "Unit 3 – Growing up".</summary>
    public string Label { get; set; } = "";
    /// <summary>Which kind of book within the series this unit belongs to; default the main textbook.</summary>
    public BookType BookType { get; set; } = BookType.Textbook;
    /// <summary>Topics/contents of the unit, one entry per topic.</summary>
    public List<string> Topics { get; set; } = [];
    /// <summary>Grammar the unit introduces or practices.</summary>
    public string? Grammar { get; set; }
    /// <summary>Vocabulary note of the unit (word fields or concrete words, comma-separated).</summary>
    public string? VocabularyNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A creator profile is the <b>teacher</b>: one subject, one school branch, one grade range and
/// optionally one textbook series – plus the didactic stance exercises are created with
/// (<see cref="Persona"/>/<see cref="Didactics"/> go into the AI creator's system prompt).
/// Its purpose is the fit: for a given child the knowledgeable creator can be <i>found</i>
/// (<c>CreatorProfileService</c>) instead of asking the same generalist every time.
/// </summary>
public class CreatorProfile
{
    public int Id { get; set; }
    /// <summary>Descriptive name, e.g. "Englisch 8 Gymnasium – Access".</summary>
    public string Name { get; set; } = "";
    /// <summary>Who created the profile and may change it; <c>null</c> = seeded.</summary>
    public int? OwnerAdultId { get; set; }
    public Adult? Owner { get; set; }
    /// <summary>Subject as free text ("Englisch") – for profiles without a catalog subject.</summary>
    public string? SubjectName { get; set; }
    /// <summary>Optional catalog link to a <see cref="Subject"/>.</summary>
    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    /// <summary>School types the profile is responsible for; <see cref="SchoolTypes.None"/> = for all of them.</summary>
    public SchoolTypes SchoolTypes { get; set; } = SchoolTypes.None;
    /// <summary>Lowest grade taught (inclusive); null = no lower bound.</summary>
    public int? GradeMin { get; set; }
    /// <summary>Highest grade taught (inclusive); null = no upper bound.</summary>
    public int? GradeMax { get; set; }
    /// <summary>The series the profile is optimized for; null = independent of any work.</summary>
    public int? SeriesId { get; set; }
    public TextbookSeries? Series { get; set; }
    /// <summary>Language being learned (language code) for language subjects.</summary>
    public string SourceLang { get; set; } = "en";
    /// <summary>Native language (language code).</summary>
    public string TargetLang { get; set; } = "de";
    /// <summary>
    /// The teacher's role description in their own words ("You are an English teacher at a Gymnasium …").
    /// It is <b>prepended</b> to the creator's fixed rule block, it never replaces it.
    /// </summary>
    public string? Persona { get; set; }
    /// <summary>Didactic requirements that hold beyond a single assignment (sentence length, progression, taboos).</summary>
    public string? Didactics { get; set; }
    /// <summary>
    /// Exercise types this profile preferably creates (keys from the type manifest). Stored as a JSON list –
    /// <b>reassign</b> it in the controller, do not mutate in place (missing ValueComparer).
    /// </summary>
    public List<string> DefaultTypes { get; set; } = [];
    /// <summary>Inactive profiles are never suggested during matching.</summary>
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
