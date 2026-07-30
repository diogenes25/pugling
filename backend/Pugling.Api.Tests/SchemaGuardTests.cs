using Microsoft.EntityFrameworkCore;
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
    // Kein Host, keine Migration: `HasPendingModelChanges` und `GetMigrations` vergleichen das Modell
    // mit dem in der Assembly liegenden Snapshot – die Verbindung wird dabei nie geöffnet.
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

        // Selbstschutz: ein leer gebauter Kontext hätte kein Modell und keine Migrationen – dann wäre
        // die Zusicherung unten wertlos.
        Assert.True(db.Model.GetEntityTypes().Count() >= 55,
            $"Zu wenige Entity-Typen im Modell ({db.Model.GetEntityTypes().Count()}) – greift die Reflexion?");
        var migrations = db.Database.GetMigrations().ToList();
        Assert.True(migrations.Count >= 1, "Keine Migrationen in der Assembly gefunden – falscher Kontext?");

        Assert.False(db.Database.HasPendingModelChanges(),
            "Das EF-Modell weicht vom Snapshot der letzten Migration ab. Erzeuge eine Migration "
            + "(siehe CLAUDE.md → Befehle) – nicht auf EnsureCreated zurückfallen.");
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
            $"Erwartet genau eine Migration, gefunden {migrations.Count}: {string.Join(", ", migrations)}. "
            + "Falte die Kette neu (Data/Migrations löschen, `dotnet dotnet-ef migrations add InitialCreate "
            + "--project backend/Pugling.Api --output-dir Data/Migrations`) – oder entferne diese Regel, "
            + "wenn die App veröffentlicht ist und einen echten Upgrade-Pfad braucht.");
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
        // Begründete Ausnahmen: Property → warum der Default in der DB stehen muss.
        var erlaubt = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Schlüssel sind Tabellen-/Spaltennamen (relationales Modell), nicht Entity-/Property-Namen.
            ["Exercises.ExecutePublic"] =
                "Fail-Safe: eine Übung ohne ausdrückliche Angabe bleibt für alle Creator ausführbar "
                + "(bisheriges Verhalten). Ein fehlender Wert darf hier nicht zu 'gesperrt' werden.",
        };

        using var db = Context();

        // Gefragt wird das *relationale* Modell (Tabellen/Spalten), nicht die Property-Metadaten:
        // `IProperty.GetDefaultValue()` liefert auch dort einen Wert, wo EF gar keine DEFAULT-Klausel
        // schreibt (z. B. an jedem `CreatedAt`) – der Test hätte reihenweise Spalten gemeldet, die im
        // erzeugten DDL keinen Default haben. `IColumn.DefaultValue` ist dagegen genau das, was der
        // DDL-Generator ausgibt; gegengeprüft an der migrierten Datei (dort steht exakt ein DEFAULT).
        var tables = db.Model.GetRelationalModel().Tables.ToList();
        var mitDefault = tables
            .SelectMany(t => t.Columns
                .Where(col => col.DefaultValue is not null || col.DefaultValueSql is not null)
                .Select(col => $"{t.Name}.{col.Name}"))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        // Selbstschutz: greift die Reflexion nicht, wäre die Menge leer und der Test bestünde inhaltsleer –
        // obendrein wüsste er dann nicht, dass ihm der eine gewollte Default fehlt.
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

                // Die Ausnahmeliste des DbContext ist die Quelle – hier wird sie nur gelesen.
                if (PuglingDbContext.IntEnumErlaubt(name)) continue;

                if (property.GetProviderClrType() != typeof(string)
                    && property.GetValueConverter()?.ProviderClrType != typeof(string))
                    alsZahl.Add(name);
            }
        }

        // Selbstschutz: findet die Reflexion keine Enums, bestünde der Test inhaltsleer.
        Assert.True(alleEnums.Count >= 30, $"Zu wenige Enum-Properties gefunden ({alleEnums.Count}).");

        Assert.True(alsZahl.Count == 0,
            "Diese Enum-Spalten liegen als Zahl in der DB. Entweder greift die Konvention nicht, oder sie "
            + "brauchen einen begründeten Eintrag in PuglingDbContext.IntEnumsByDesign:\n  "
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
        // Abnahme-Tabelle: "Entity.FkProperty" → beabsichtigtes Verhalten. Der Kommentar nennt das Ziel.
        // Kommt eine FK dazu, gehört sie hier eingetragen – bewusst, nicht durch Erben einer Konvention.
        var abgenommen = new Dictionary<string, DeleteBehavior>(StringComparer.Ordinal)
        {
            ["AccountProfile.AccountId"] = DeleteBehavior.Cascade, // -> Account
            ["AccountProfile.AdultId"] = DeleteBehavior.Cascade, // -> Adult
            ["AccountProfile.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["Achievement.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["AchievementAward.AchievementId"] = DeleteBehavior.Cascade, // -> Achievement
            ["ActivationRequest.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["ActivationRequest.ShopArticleId"] = DeleteBehavior.SetNull, // -> ShopArticle
            ["Chapter.SubjectId"] = DeleteBehavior.Cascade, // -> Subject
            ["ChildInterest.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["ChildInterest.InterestTagId"] = DeleteBehavior.Cascade, // -> InterestTag
            ["ChildInventory.ChildId"] = DeleteBehavior.Cascade, // -> Child
            // Der Fund: bezahlte Einheiten sind Geld und dürfen nicht mit dem Katalogeintrag verschwinden.
            ["ChildInventory.ShopArticleId"] = DeleteBehavior.SetNull, // -> ShopArticle
            ["ChildMediaPick.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["ChildMediaPick.ExerciseItemId"] = DeleteBehavior.Cascade, // -> ExerciseItem
            ["ChildMediaPick.MediaAssetId"] = DeleteBehavior.Cascade, // -> MediaAsset
            ["ChildMediaPick.VocabularyId"] = DeleteBehavior.Cascade, // -> Vocabulary
            ["ChildPointsEntry.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["CreatorProfile.OwnerAdultId"] = DeleteBehavior.SetNull, // -> Adult
            ["CreatorProfile.SeriesId"] = DeleteBehavior.SetNull, // -> TextbookSeries
            ["CreatorProfile.SubjectId"] = DeleteBehavior.SetNull, // -> Subject
            ["Exercise.AuthorAdultId"] = DeleteBehavior.SetNull, // -> Adult
            ["Exercise.CategoryId"] = DeleteBehavior.SetNull, // -> ExerciseCategory
            ["Exercise.ChapterId"] = DeleteBehavior.Cascade, // -> Chapter
            ["ExerciseCategory.SubjectId"] = DeleteBehavior.Cascade, // -> Subject
            ["ExerciseGrant.CreatorId"] = DeleteBehavior.Cascade, // -> Adult
            ["ExerciseGrant.ExerciseId"] = DeleteBehavior.Cascade, // -> Exercise
            ["ExerciseItem.ExerciseId"] = DeleteBehavior.Cascade, // -> Exercise
            // Restrict: eine Store-Vokabel, die eine Übung benutzt, wird nicht mitgelöscht (409 statt Verlust).
            ["ExerciseItem.VocabularyId"] = DeleteBehavior.Restrict, // -> Vocabulary
            ["ExerciseTag.ExerciseId"] = DeleteBehavior.Cascade, // -> Exercise
            ["ExerciseTag.TagId"] = DeleteBehavior.Cascade, // -> Tag
            ["ItemProgress.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["ItemProgress.ItemId"] = DeleteBehavior.Cascade, // -> ExerciseItem
            ["ItemReviewEvent.ChildId"] = DeleteBehavior.Cascade, // -> Child
            // Die Historie überlebt das Item: die Aussage „richtig beantwortet" gilt weiter.
            ["ItemReviewEvent.ItemId"] = DeleteBehavior.SetNull, // -> ExerciseItem
            ["KeyResult.ObjectiveId"] = DeleteBehavior.Cascade, // -> Objective
            ["Klassenarbeit.ChildId"] = DeleteBehavior.Cascade, // -> Child
            ["Klassenarbeit.SubjectId"] = DeleteBehavior.SetNull, // -> Subject
            ["KlassenarbeitExercise.ExerciseId"] = DeleteBehavior.Cascade, // -> Exercise
            ["KlassenarbeitExercise.KlassenarbeitId"] = DeleteBehavior.Cascade, // -> Klassenarbeit
            ["KlassenarbeitTag.KlassenarbeitId"] = DeleteBehavior.Cascade, // -> Klassenarbeit
            ["KlassenarbeitTag.TagId"] = DeleteBehavior.Cascade, // -> Tag
            ["LearnGoal.ChildId"] = DeleteBehavior.Cascade, // -> Child (fällt mit E13 weg)
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
            // Restrict: eine Übung, die in einem Lehrplan steckt, wird nicht mitgelöscht (409 statt Verlust).
            ["PlanPosition.ExerciseId"] = DeleteBehavior.Restrict, // -> Exercise
            ["PlanPosition.StudyPlanId"] = DeleteBehavior.Cascade, // -> StudyPlan
            ["PositionGoalPenalty.PlanPositionId"] = DeleteBehavior.Cascade, // -> PlanPosition
            ["PositionGoalReward.PlanPositionId"] = DeleteBehavior.Cascade, // -> PlanPosition
            ["PositionItemProgress.PlanPositionId"] = DeleteBehavior.Cascade, // -> PlanPosition
            // SetNull, damit in SQLite kein zweiter Cascade-Pfad (Plan → Position → Sitzung/Test) neben
            // Plan → Sitzung/Test entsteht; am Plan hängen beide schon.
            ["PracticeSession.PlanPositionId"] = DeleteBehavior.SetNull, // -> PlanPosition
            ["PracticeSession.StudyPlanId"] = DeleteBehavior.Cascade, // -> StudyPlan
            ["Remark.AccountId"] = DeleteBehavior.Cascade, // -> Account
            // Jeder Kontext-Bezug der Anmerkung darf verblassen; die Beobachtung bleibt.
            ["Remark.ChildId"] = DeleteBehavior.SetNull, // -> Child
            ["Remark.ExerciseId"] = DeleteBehavior.SetNull, // -> Exercise
            ["Remark.ParentRemarkId"] = DeleteBehavior.SetNull, // -> Remark
            ["Remark.PlanPositionId"] = DeleteBehavior.SetNull, // -> PlanPosition
            ["Remark.StudyPlanId"] = DeleteBehavior.SetNull, // -> StudyPlan
            ["RemarkComment.AuthorAccountId"] = DeleteBehavior.SetNull, // -> Account
            ["RemarkComment.RemarkId"] = DeleteBehavior.Cascade, // -> Remark
            ["ReviewEvent.PracticeSessionId"] = DeleteBehavior.Cascade, // -> PracticeSession
            ["SeriesUnit.SeriesId"] = DeleteBehavior.Cascade, // -> TextbookSeries
            // Cascade bleibt Absicht: ein Vater mit Artikeln muss sich selbst löschen können.
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
            // Restrict: die Grundform darf nicht verschwinden, solange eine Beugung auf sie zeigt.
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

        // Selbstschutz: greift die Reflexion nicht, wäre die Menge leer und der Vergleich inhaltsleer.
        Assert.True(tatsaechlich.Count >= 90,
            $"Zu wenige Fremdschlüssel gefunden ({tatsaechlich.Count}) – greift die Reflexion?");

        // Ein sortierter Zeilenvergleich statt Mengendifferenzen: die Fehlermeldung zeigt dann direkt,
        // welche Zeile fehlt, überzählig ist oder ein anderes Verhalten trägt.
        static string[] Zeilen(IDictionary<string, DeleteBehavior> d) =>
            d.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key} = {p.Value}").ToArray();

        Assert.Equal(Zeilen(abgenommen), Zeilen(tatsaechlich));

        // Der Konventions-Default für optionale Beziehungen räumt nur im geladenen ChangeTracker auf und
        // lässt die DB-Seite offen – als *Absicht* ist er nie richtig.
        Assert.DoesNotContain(DeleteBehavior.ClientSetNull, tatsaechlich.Values);
    }
}
