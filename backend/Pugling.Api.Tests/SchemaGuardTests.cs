using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// Gates for the <b>shape of the database schema</b> (docs/codequalitaet-gates-plan.md). They check the
/// EF model itself, not its behavior – which is why they need no host and no database:
/// the model and the migration snapshot both live in the assembly.
/// <para>
/// Every test carries a <b>self-protection against false-green</b>: if the reflection doesn't catch
/// anything (an empty-built context, a moved migrations assembly), it would see nothing and pass vacuously.
/// </para>
/// </summary>
public class SchemaGuardTests
{
    // No host, no migration: `HasPendingModelChanges` and `GetMigrations` compare the model with the snapshot
    // sitting in the assembly - the connection is never opened.
    private static PuglingDbContext Context() =>
        new(new DbContextOptionsBuilder<PuglingDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options);

    /// <summary>
    /// <b>G1 – no model drift.</b> If the model diverges from the snapshot of the last migration, a
    /// migration is missing. This has gone unnoticed <i>anywhere</i> so far: the tests run <c>Migrate()</c>,
    /// and a column that only exists in the model is simply not found by SQLite when reading – the error
    /// surfaces as an apparently domain-specific test failure at a completely different spot.
    /// </summary>
    [Fact]
    public void Modell_Und_Migrationen_Stimmen_Ueberein()
    {
        using var db = Context();

        // Self-protection: a context built empty would have no model and no migrations - the assurance below
        // would then be worthless.
        Assert.True(db.Model.GetEntityTypes().Count() >= 55,
            $"Too few entity types in the model ({db.Model.GetEntityTypes().Count()}) - does the reflection bite?");
        var migrations = db.Database.GetMigrations().ToList();
        Assert.True(migrations.Count >= 1, "No migrations found in the assembly - wrong context?");

        Assert.False(db.Database.HasPendingModelChanges(),
            "The EF model deviates from the snapshot of the last migration. Create a migration "
            + "(see CLAUDE.md → Befehle) - do not fall back to EnsureCreated.");
    }

    /// <summary>
    /// <b>G1b – the chain stays at exactly one migration.</b> As long as the app is unpublished and
    /// legacy data is dispensable, it gets refolded before every stage is completed
    /// (delete <c>Data/Migrations</c> + <c>migrations add InitialCreate</c>). This makes
    /// column renames and type changes free – no generated SQLite table rebuild that
    /// someone has to sign off on.
    /// <para>
    /// This assertion is <b>deliberately finite</b>: the first release will need a
    /// real upgrade path, and then it gets removed. That this is a visible decision rather than
    /// a silent erosion is its actual purpose.
    /// </para>
    /// </summary>
    [Fact]
    public void Migrationskette_Besteht_Aus_Genau_Einer_Migration()
    {
        using var db = Context();
        var migrations = db.Database.GetMigrations().ToList();

        Assert.True(migrations.Count == 1,
            $"Expected exactly one migration, found {migrations.Count}: {string.Join(", ", migrations)}. "
            + "Fold the chain anew (delete Data/Migrations, `dotnet dotnet-ef migrations add InitialCreate "
            + "--project backend/Pugling.Api --output-dir Data/Migrations`) - or remove this rule "
            + "once the app is published and needs a real upgrade path.");
        Assert.EndsWith("InitialCreate", migrations[0], StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>G9 – only deliberate DB defaults.</b> A SQL <c>DEFAULT</c> is a promise to writers
    /// <i>outside</i> of EF; EF itself names all mapped properties in every <c>INSERT</c> and
    /// never consults it. Before the squash, 15 columns carried such a clause without it appearing
    /// anywhere in the model: they were a byproduct of having been attached via <c>AddColumn(defaultValue:…)</c>.
    /// Two of them were even harmful – a <c>ConcurrencyStamp</c> with a default value
    /// renders the optimistic lock ineffective for any row not inserted via EF, and that on
    /// money-relevant tables.
    /// <para>
    /// This guard prevents them from growing back over the next migrations. A new
    /// default is allowed – but only as <c>HasDefaultValue</c> in the model and with an entry here,
    /// i.e. as a decision instead of a side effect.
    /// </para>
    /// </summary>
    [Fact]
    public void Nur_Bewusste_Datenbank_Defaults()
    {
        // Justified exceptions: property → why the default has to sit in the DB.
        var erlaubt = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Keys are table/column names (the relational model), not entity/property names.
            ["Exercises.ExecutePublic"] =
                "Fail-safe: an exercise without an explicit value stays executable for every creator "
                + "(the previous behavior). A missing value must not turn into 'blocked' here.",
        };

        using var db = Context();

        // What is asked is the *relational* model (tables/columns), not the property metadata:
        // `IProperty.GetDefaultValue()` also returns a value where EF writes no DEFAULT clause at all (on every
        // `CreatedAt`, say) - the test would have reported columns by the dozen that have no default in the
        // generated DDL. `IColumn.DefaultValue`, by contrast, is exactly what the DDL generator emits;
        // cross-checked against the migrated file (which holds exactly one DEFAULT).
        var tables = db.Model.GetRelationalModel().Tables.ToList();
        var mitDefault = tables
            .SelectMany(t => t.Columns
                .Where(col => col.DefaultValue is not null || col.DefaultValueSql is not null)
                .Select(col => $"{t.Name}.{col.Name}"))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        // Self-protection: if the reflection does not bite, the set would be empty and the test would pass
        // vacuously - and on top of that would not notice that the one intended default is missing.
        Assert.True(tables.Count >= 55, $"Zu wenige Tabellen im relationalen Modell ({tables.Count}).");

        Assert.Equal(erlaubt.Keys.OrderBy(n => n, StringComparer.Ordinal), mitDefault);
    }

    /// <summary>
    /// <b>G4 – every persisted enum is stored as a string in the DB.</b> The contract speaks strings
    /// externally anyway (<c>JsonStringEnumConverter</c>); if they were numbers internally, there would be
    /// two representations of the same value and a silent coupling to member order – an inserted
    /// enum value would have reinterpreted stored data.
    /// <para>
    /// Allowed exceptions are <c>[Flags]</c> (a bit combination has no name) and the enums compared for
    /// ordering that are listed by name with a reason in the DbContext. This test reads exactly
    /// that list, so the rule and the exception don't have to be maintained in two places.
    /// </para>
    /// </summary>
    [Fact]
    public void Persistierte_Enums_Sind_Strings()
    {
        using var db = Context();

        var alleEnums = new List<string>();
        var alsZahl = new List<string>();
        foreach (var entity in db.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (!type.IsEnum) continue;

                var name = $"{entity.ClrType.Name}.{property.Name}";
                alleEnums.Add(name);
                if (type.IsDefined(typeof(FlagsAttribute), inherit: false)) continue;

                // The DbContext's exception list is the source - it is only read here.
                if (PuglingDbContext.IntEnumErlaubt(name)) continue;

                if (property.GetProviderClrType() != typeof(string)
                    && property.GetValueConverter()?.ProviderClrType != typeof(string))
                    alsZahl.Add(name);
            }
        }

        // Self-protection: if the reflection finds no enums, the test would pass vacuously.
        Assert.True(alleEnums.Count >= 30, $"Zu wenige Enum-Properties gefunden ({alleEnums.Count}).");

        Assert.True(alsZahl.Count == 0,
            "These enum columns sit in the DB as numbers. Either the convention does not bite, or they "
            + "need a justified entry in PuglingDbContext.IntEnumsByDesign:\n  "
            + string.Join("\n  ", alsZahl.OrderBy(n => n, StringComparer.Ordinal)));
    }

    /// <summary>
    /// <b>G2 – every foreign key has a signed-off delete behavior.</b> A delete behavior is the
    /// most dangerous silent decision in the model: it decides whether a row <i>takes data down with it</i>.
    /// That is exactly where the most expensive finding of this restructuring sat –
    /// <c>Adult→ShopArticle→ChildInventory</c> was <c>Cascade</c> throughout, so deleting a supervisor
    /// destroyed paid-for child inventory while the purchase receipts next to it remained standing.
    /// <para>
    /// Why a <b>literally pinned table</b> and not a clever rule: reflection cannot distinguish "explicitly
    /// set" from "inherited from the EF convention". A rule could therefore only check the
    /// <i>values</i>, not the intent – and would stay silent for every new FK. The table is the
    /// honest substitute: it forces <b>one deliberate line</b> for every new relationship, and it
    /// triggers in both directions (also for an FK that disappears).
    /// </para>
    /// <para>
    /// Additionally forbidden is <see cref="DeleteBehavior.ClientSetNull"/> – the convention default for
    /// optional relationships. It only cleans up in the <i>loaded</i> ChangeTracker and leaves the DB side
    /// open. This very assertion caught a real bug while writing out the convention cascades:
    /// a <c>WithMany()</c> without the existing counter-navigation let EF create a <b>second</b>
    /// relationship (<c>ChildId1</c>), and that one carried exactly this value.
    /// </para>
    /// </summary>
    [Fact]
    public void Jeder_Fremdschluessel_Hat_Ein_Abgenommenes_Loeschverhalten()
    {
        // Acceptance table: "Entity.FkProperty" → the intended behavior. The comment names the target.
        // If an FK is added, it belongs in here - deliberately, not by inheriting a convention.
        var abgenommen = new Dictionary<string, DeleteBehavior>(StringComparer.Ordinal)
        {
            ["AccountProfile.AccountId"] = DeleteBehavior.Cascade, // -> Account
            ["AccountProfile.AdultId"] = DeleteBehavior.Cascade, // -> Adult
            ["AccountProfile.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["Achievement.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["AchievementAward.AchievementId"] = DeleteBehavior.Cascade, // -> Achievement
            ["ActivationRequest.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["ActivationRequest.ShopArticleId"] = DeleteBehavior.SetNull, // -> ShopArticle
            ["ChildInterest.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["ChildInterest.InterestTagId"] = DeleteBehavior.Cascade, // -> InterestTag
            ["ChildInventory.ChildId"] = DeleteBehavior.Cascade, // -> Child
            // The finding: paid units are money and must not disappear with the catalog entry.
            ["ChildInventory.ShopArticleId"] = DeleteBehavior.SetNull, // -> ShopArticle
            ["ChildMediaPick.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["ChildMediaPick.ExerciseItemId"] = DeleteBehavior.Cascade, // -> ExerciseItem
            ["ChildMediaPick.MediaAssetId"] = DeleteBehavior.Cascade, // -> MediaAsset
            ["ChildMediaPick.VocabularyId"] = DeleteBehavior.Cascade, // -> Vocabulary
            ["ChildPointsEntry.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["CreatorProfile.OwnerAdultId"] = DeleteBehavior.SetNull, // -> Adult
            ["CreatorProfile.SeriesId"] = DeleteBehavior.SetNull, // -> TextbookSeries
            ["CreatorProfile.SubjectId"] = DeleteBehavior.SetNull, // -> Subject
            ["DailyBoxClaim.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["Exercise.AuthorAdultId"] = DeleteBehavior.SetNull, // -> Adult
            ["Exercise.CategoryId"] = DeleteBehavior.SetNull, // -> ExerciseCategory
            ["Exercise.SeriesUnitId"] = DeleteBehavior.Cascade, // -> SeriesUnit
            ["ExerciseCategory.SubjectId"] = DeleteBehavior.Cascade, // -> Subject
            ["ExerciseGrant.CreatorId"] = DeleteBehavior.Cascade, // -> Adult
            ["ExerciseGrant.ExerciseId"] = DeleteBehavior.Cascade, // -> Exercise
            ["ExerciseItem.ExerciseId"] = DeleteBehavior.Cascade, // -> Exercise
            // Restrict: a store entry used by an exercise is not deleted along with it (409 instead of loss).
            ["ExerciseItem.VocabularyId"] = DeleteBehavior.Restrict, // -> Vocabulary
            ["ExerciseTag.ExerciseId"] = DeleteBehavior.Cascade, // -> Exercise
            ["ExerciseTag.TagId"] = DeleteBehavior.Cascade, // -> Tag
            ["ItemProgress.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["ItemProgress.ItemId"] = DeleteBehavior.Cascade, // -> ExerciseItem
            ["ItemReviewEvent.ChildId"] = DeleteBehavior.Cascade, // -> Child
            // The history outlives the item: the statement "answered correctly" still holds.
            ["ItemReviewEvent.ItemId"] = DeleteBehavior.SetNull, // -> ExerciseItem
            ["KeyResult.ExerciseId"] = DeleteBehavior.Restrict, // -> Exercise (ditto)
            ["KeyResult.ObjectiveId"] = DeleteBehavior.Cascade, // -> Objective
            ["KeyResult.SeriesUnitId"] = DeleteBehavior.Restrict, // -> SeriesUnit (remove the goal first, then the series unit)
            // Cascade: a goal on a deleted subject is meaningless.
            ["KeyResult.SubjectId"] = DeleteBehavior.Cascade, // -> Subject
            ["Klassenarbeit.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["Klassenarbeit.SubjectId"] = DeleteBehavior.SetNull, // -> Subject
            ["KlassenarbeitExercise.ExerciseId"] = DeleteBehavior.Cascade, // -> Exercise
            ["KlassenarbeitExercise.KlassenarbeitId"] = DeleteBehavior.Cascade, // -> Klassenarbeit
            ["KlassenarbeitTag.KlassenarbeitId"] = DeleteBehavior.Cascade, // -> Klassenarbeit
            ["KlassenarbeitTag.TagId"] = DeleteBehavior.Cascade, // -> Tag
            ["MediaLink.ExerciseId"] = DeleteBehavior.Cascade, // -> Exercise
            ["MediaLink.ExerciseItemId"] = DeleteBehavior.Cascade, // -> ExerciseItem
            ["MediaLink.MediaAssetId"] = DeleteBehavior.Cascade, // -> MediaAsset
            ["MediaLink.VocabularyId"] = DeleteBehavior.Cascade, // -> Vocabulary
            ["MediaTagLink.InterestTagId"] = DeleteBehavior.Cascade, // -> InterestTag
            ["MediaTagLink.MediaAssetId"] = DeleteBehavior.Cascade, // -> MediaAsset
            ["MediaVariant.MediaAssetId"] = DeleteBehavior.Cascade, // -> MediaAsset
            ["Mission.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["MissionAward.MissionId"] = DeleteBehavior.Cascade, // -> Mission
            ["Objective.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["ObjectiveReward.ObjectiveId"] = DeleteBehavior.Cascade, // -> Objective
            // Restrict: an exercise sitting in a study plan is not deleted along with it (409 instead of loss).
            ["PlanPosition.ExerciseId"] = DeleteBehavior.Restrict, // -> Exercise
            ["PlanPosition.StudyPlanId"] = DeleteBehavior.Cascade, // -> StudyPlan
            ["PositionGoalPenalty.PlanPositionId"] = DeleteBehavior.Cascade, // -> PlanPosition
            ["PositionGoalReward.PlanPositionId"] = DeleteBehavior.Cascade, // -> PlanPosition
            ["PositionItemProgress.PlanPositionId"] = DeleteBehavior.Cascade, // -> PlanPosition
            // SetNull, so that no second cascade path (plan → position → session/test) arises in SQLite next to
            // plan → session/test; both already hang on the plan.
            ["PracticeSession.PlanPositionId"] = DeleteBehavior.SetNull, // -> PlanPosition
            ["PracticeSession.StudyPlanId"] = DeleteBehavior.Cascade, // -> StudyPlan
            ["Remark.AccountId"] = DeleteBehavior.Cascade, // -> Account
            // Every context reference of a remark may fade; the observation stays.
            ["Remark.ChildId"] = DeleteBehavior.SetNull, // -> Child
            ["Remark.ExerciseId"] = DeleteBehavior.SetNull, // -> Exercise
            ["Remark.ParentRemarkId"] = DeleteBehavior.SetNull, // -> Remark
            ["Remark.PlanPositionId"] = DeleteBehavior.SetNull, // -> PlanPosition
            ["Remark.StudyPlanId"] = DeleteBehavior.SetNull, // -> StudyPlan
            ["RemarkComment.AuthorAccountId"] = DeleteBehavior.SetNull, // -> Account
            ["RemarkComment.RemarkId"] = DeleteBehavior.Cascade, // -> Remark
            ["ReviewEvent.PracticeSessionId"] = DeleteBehavior.Cascade, // -> PracticeSession
            ["SeriesUnit.SeriesId"] = DeleteBehavior.Cascade, // -> TextbookSeries
            // Cascade stays intentional: an adult with articles must be able to delete themselves.
            ["ShopArticle.AdultId"] = DeleteBehavior.Cascade, // -> Adult
            ["ShopListing.ShopArticleId"] = DeleteBehavior.Cascade, // -> ShopArticle
            ["ShopPurchase.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["ShopPurchase.ShopListingId"] = DeleteBehavior.SetNull, // -> ShopListing
            ["StudyPlan.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["StudyPlan.SubjectId"] = DeleteBehavior.SetNull, // -> Subject
            ["SupervisorLink.StudentId"] = DeleteBehavior.Cascade, // -> Child
            ["SupervisorLink.SupervisorId"] = DeleteBehavior.Cascade, // -> Adult
            ["Tag.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["TestAttempt.PlanPositionId"] = DeleteBehavior.SetNull, // -> PlanPosition
            ["TestAttempt.StudyPlanId"] = DeleteBehavior.Cascade, // -> StudyPlan
            ["TestItemResult.TestAttemptId"] = DeleteBehavior.Cascade, // -> TestAttempt
            ["Textbook.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["Textbook.CurrentUnitId"] = DeleteBehavior.SetNull, // -> SeriesUnit
            ["Textbook.SeriesId"] = DeleteBehavior.SetNull, // -> TextbookSeries
            ["Textbook.SubjectId"] = DeleteBehavior.SetNull, // -> Subject
            ["TextbookSeries.OwnerAdultId"] = DeleteBehavior.SetNull, // -> Adult
            ["TextbookSeries.SubjectId"] = DeleteBehavior.SetNull, // -> Subject
            ["TimetableEntry.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["TimetableEntry.SubjectId"] = DeleteBehavior.Cascade, // -> Subject
            ["VocabTagLink.VocabTagId"] = DeleteBehavior.Cascade, // -> VocabTag
            ["VocabTagLink.VocabularyId"] = DeleteBehavior.Cascade, // -> Vocabulary
            // Restrict: the base form must not disappear while an inflection points at it.
            ["Vocabulary.BaseFormId"] = DeleteBehavior.Restrict, // -> Vocabulary
            ["VocabularyTag.TagId"] = DeleteBehavior.Cascade, // -> Tag
            ["VocabularyTag.VocabularyId"] = DeleteBehavior.Cascade, // -> Vocabulary
        };

        using var db = Context();

        var tatsaechlich = new Dictionary<string, DeleteBehavior>(StringComparer.Ordinal);
        foreach (var entity in db.Model.GetEntityTypes())
            foreach (var fk in entity.GetForeignKeys())
                tatsaechlich[$"{entity.ClrType.Name}.{string.Join("+", fk.Properties.Select(p => p.Name))}"]
                    = fk.DeleteBehavior;

        // Self-protection: if the reflection does not bite, the set would be empty and the comparison vacuous.
        Assert.True(tatsaechlich.Count >= 90,
            $"Too few foreign keys found ({tatsaechlich.Count}) - does the reflection bite?");

        // A sorted line comparison instead of set differences: the failure message then shows directly which
        // line is missing, superfluous or carries a different behavior.
        static string[] Zeilen(IDictionary<string, DeleteBehavior> d) =>
            d.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key} = {p.Value}").ToArray();

        Assert.Equal(Zeilen(abgenommen), Zeilen(tatsaechlich));

        // The convention default for optional relationships only cleans up in the loaded ChangeTracker and
        // leaves the DB side open - as an *intent* it is never right.
        Assert.DoesNotContain(DeleteBehavior.ClientSetNull, tatsaechlich.Values);
    }

    /// <summary>
    /// <b>G3 - every string column has a length, and a unique-indexed one MUST have one.</b> Before, <i>not a
    /// single</i> column in the whole model carried a <c>HasMaxLength</c>.
    /// <para>
    /// <b>Said honestly:</b> SQLite does not enforce the length, and EF does not validate it on
    /// <c>SaveChanges</c>. The value lies in portability - on a provider change <c>NVARCHAR(MAX)</c> would
    /// otherwise appear everywhere, and <b>no unique index can be created</b> on that in SQL Server.
    /// That hits exactly the columns carrying the idempotency. Which is why the second assurance is the sharp
    /// one: unlimited <i>and</i> unique is not allowed, not even with an entry in the exception list.
    /// </para>
    /// </summary>
    [Fact]
    public void Jede_String_Spalte_Hat_Eine_Laenge()
    {
        using var db = Context();

        var alle = new List<string>();
        var ohneLaenge = new List<string>();
        var uniqueOhneLaenge = new List<string>();
        foreach (var entity in db.Model.GetEntityTypes())
        {
            // Columns that appear in a unique index - for the second, hard assurance.
            var inUnique = entity.GetIndexes().Where(i => i.IsUnique)
                .SelectMany(i => i.Properties).Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType != typeof(string)) continue;
                var name = $"{entity.ClrType.Name}.{property.Name}";
                alle.Add(name);
                if (property.GetMaxLength() is not null) continue;

                if (inUnique.Contains(property.Name)) uniqueOhneLaenge.Add(name);
                // The DbContext's exception list is the source - it is only read here.
                else if (!PuglingDbContext.UnbegrenztErlaubt(name)) ohneLaenge.Add(name);
            }
        }

        // Self-protection: if the reflection finds no string columns, the test would pass vacuously.
        Assert.True(alle.Count >= 100, $"Too few string columns found ({alle.Count}).");

        Assert.True(uniqueOhneLaenge.Count == 0,
            "These string columns sit in a unique index and are unlimited. That is the hard rule: "
            + "a unique index on NVARCHAR(MAX) cannot be created after a provider change - and these are "
            + "the columns that carry the idempotency:\n  "
            + string.Join("\n  ", uniqueOhneLaenge.OrderBy(n => n, StringComparer.Ordinal)));

        Assert.True(ohneLaenge.Count == 0,
            "These string columns have no length. Either the convention does not bite, or they need "
            + "a justified entry in PuglingDbContext.UnlimitedByDesign:\n  "
            + string.Join("\n  ", ohneLaenge.OrderBy(n => n, StringComparer.Ordinal)));
    }

    /// <summary>
    /// <b>G7 - every JSON column has a ValueComparer.</b> A collection that goes into the DB through a string
    /// converter is compared by <b>reference</b> by EF unless it has its own comparer: an in-place mutation
    /// (<c>list.Add(...)</c>) then counts as "unchanged" and is <b>lost silently</b> on <c>SaveChanges</c>.
    /// <para>
    /// This rule sits in <c>CLAUDE.md</c> and has been followed exemplarily so far - 13 comparers, no gap.
    /// That is exactly what makes it a good candidate for a gate: it hangs on discipline, breaking it is
    /// invisible, and the next JSON column will surely come.
    /// </para>
    /// </summary>
    [Fact]
    public void Jede_Json_Spalte_Hat_Einen_ValueComparer()
    {
        using var db = Context();
        // The design-time model again, not `db.Model`: the runtime-optimized one throws the annotations away
        // (the same trap as in G8) - and the annotation is the trace we are after here.
        var jsonSpalten = new List<string>();
        var ohneComparer = new List<string>();
        foreach (var entity in db.GetService<IDesignTimeModel>().Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
            {
                // A JSON column = a collection/complex type persisted as a string. Strings themselves and the
                // enum conversions from G4 are not.
                var clr = property.ClrType;
                if (clr == typeof(string) || (Nullable.GetUnderlyingType(clr) ?? clr).IsEnum) continue;
                if (property.GetValueConverter()?.ProviderClrType != typeof(string)) continue;

                var name = $"{entity.ClrType.Name}.{property.Name}";
                jsonSpalten.Add(name);
                // The question is "was one *set*", not "is there one": `GetValueComparer()` always returns
                // something - in case of doubt the reference-comparing default, and that is exactly the bug.
                // The annotation is the only trace of the explicit configuration.
                if (!property.GetAnnotations().Any(a =>
                        a.Name is "ValueComparer" && a.Value is not null))
                    ohneComparer.Add(name);
            }

        // Self-protection: if the reflection finds no JSON columns, the test would pass vacuously.
        Assert.True(jsonSpalten.Count >= 10,
            $"Too few JSON columns found ({jsonSpalten.Count}) - does the reflection bite?");

        Assert.True(ohneComparer.Count == 0,
            "These JSON columns have no ValueComparer. EF then compares them by reference, and an "
            + "in-place mutation is lost silently on SaveChanges (see Data/JsonValueComparer.cs):\n  "
            + string.Join("\n  ", ohneComparer.OrderBy(n => n, StringComparer.Ordinal)));
    }

    /// <summary>
    /// <b>G6 - no instant and no period as text.</b> A <c>string</c> column whose name promises a time value
    /// has been the most expensive schema bug of this model: <c>PeriodKey</c> carried <b>three</b> formats
    /// (<c>2026-07-04</c>, <c>2026-W27</c>, <c>once</c>) across <b>four</b> tables, and all four were part of a
    /// unique index - i.e. carried the idempotency. A typo in the format would have paid twice without
    /// anything standing out.
    /// <para>
    /// The guard bites through the <b>name</b>, because that is the only reflectable trace of a time meaning.
    /// Names such as <c>Key</c>/<c>Slug</c> that denote a domain <i>natural</i> key are allowed - but only by
    /// name and with a reason, so that the next <c>…Key</c> column forces a decision instead of sneaking in.
    /// The comparison is <b>case-sensitive</b>: it should hit <c>PeriodKey</c>, not every word ending in "on".
    /// </para>
    /// </summary>
    [Fact]
    public void Keine_Zeitangabe_Als_Text()
    {
        // Suspicious suffixes: everything that sounds like an instant or a period.
        string[] verdaechtig = ["Key", "Period", "Day", "Date", "On", "At", "Time", "Week", "Month", "Year"];

        // Justified exceptions: property → why the text here is not a time value.
        var erlaubt = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Vocabulary.Key"] = "Domain natural key of the store entry (en:hello:de), slug-idempotent.",
            ["MediaAsset.Key"] = "Domain natural key of the motif, the basis of recognition.",
            ["ClozeText.Key"] = "Domain natural key of the cloze text.",
            // The only real finding of this guard on its first run - and a justified one:
            ["TimetableEntry.TimeOfDay"] =
                "Free text and explicitly NO time of day: the supervisor writes 'Nachmittag' or "
                + "'1./2. Stunde' into it. A time type could not represent that, and the field is part of "
                + "the contract (EntryResponse/CreateEntryDto) - typing it would be a break without a gain.",
        };

        using var db = Context();

        var alleStrings = new List<string>();
        var treffer = new List<string>();
        foreach (var entity in db.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType != typeof(string)) continue;
                var name = $"{entity.ClrType.Name}.{property.Name}";
                alleStrings.Add(name);
                if (verdaechtig.Any(s => property.Name.EndsWith(s, StringComparison.Ordinal))
                    && !erlaubt.ContainsKey(name))
                    treffer.Add(name);
            }

        // Self-protection: if the reflection finds no string columns, the test would pass vacuously.
        Assert.True(alleStrings.Count >= 100, $"Zu wenige String-Spalten gefunden ({alleStrings.Count}).");

        Assert.True(treffer.Count == 0,
            "These string columns promise a time value by their name. Use a real type "
            + "(DateOnly/DateTime/Enum) - or add them to the exception list with a reason if it is a "
            + "domain natural key:\n  "
            + string.Join("\n  ", treffer.OrderBy(n => n, StringComparer.Ordinal)));
    }

    /// <summary>
    /// <b>G8 - "exactly one of N" belongs in the database, not in a comment.</b> Three tables carry the same
    /// question: which of the several optional foreign keys is the valid one? Two of them
    /// (<c>MediaLink</c>, <c>ChildMediaPick</c>) answered it exemplarily with a check constraint, the third
    /// (<c>AccountProfile</c>) only claimed it in an XML comment - and a profile with both targets would have
    /// been one login with two identities.
    /// <para>
    /// A guard on a <i>set</i> instead of on "at least these three": a vanished invariant is just as much a
    /// finding as a new one without an entry. The filtered unique indexes next to it hang on the same
    /// construction - whoever removes the constraint removes their precondition too.
    /// </para>
    /// </summary>
    [Fact]
    public void Erwartete_Check_Constraints_Stehen_Im_Modell()
    {
        string[] erwartet =
        [
            "AccountProfile.CK_AccountProfile_SingleProfile",
            "ChildMediaPick.CK_ChildMediaPick_SingleCarrier",
            "MediaLink.CK_MediaLink_SingleCarrier",
        ];

        using var db = Context();
        // Not `db.Model`: for check constraints the runtime-optimized model explicitly throws ("not stored in
        // the read-optimized model"). EF discards them because nobody reads them at runtime - so what is asked
        // is the design-time model, the same one the migration is generated from.
        var entities = db.GetService<IDesignTimeModel>().Model.GetEntityTypes().ToList();

        // Self-protection: if the reflection does not bite, the set would be empty - and the comparison would
        // wrongly report "all three are missing" instead of "the test is broken".
        Assert.True(entities.Count >= 55, $"Zu wenige Entity-Typen im Modell ({entities.Count}).");

        // The entity name as the key instead of the table name: E11 pulls table names onto the DbSet names, and
        // this guard checks an invariant, not the naming.
        var gefunden = entities
            .SelectMany(e => e.GetCheckConstraints().Select(c => $"{e.ClrType.Name}.{c.Name}"))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(erwartet, gefunden);
    }
}
