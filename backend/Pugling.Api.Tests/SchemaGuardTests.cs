using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// Tore für die <b>Form des Datenbankschemas</b> (docs/codequalitaet-gates-plan.md). Sie prüfen das
/// EF-Modell selbst, nicht sein Verhalten – deshalb brauchen sie keinen Host und keine Datenbank:
/// Modell und Migrations-Snapshot liegen beide in der Assembly.
/// <para>
/// Jeder Test trägt einen <b>Selbstschutz gegen falsch-grün</b>: greift die Reflexion nicht (leer
/// gebauter Kontext, verschobene Migrations-Assembly), sähe sie nichts und bestünde inhaltsleer.
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
    /// <b>G1 – kein Modell-Drift.</b> Weicht das Modell vom Snapshot der letzten Migration ab, fehlt eine
    /// Migration. Das fiel bisher <i>nirgends</i> auf: die Tests fahren <c>Migrate()</c>, und eine Spalte,
    /// die nur im Modell existiert, wird von SQLite beim Lesen einfach nicht gefunden – der Fehler landet
    /// als scheinbar fachlicher Testfehler an ganz anderer Stelle.
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
    /// <b>G1b – die Kette bleibt bei genau einer Migration.</b> Solange die App unveröffentlicht ist und
    /// Altdaten verzichtbar sind, wird vor jedem Etappenabschluss neu gefaltet
    /// (<c>Data/Migrations</c> löschen + <c>migrations add InitialCreate</c>). Das macht
    /// Spaltenumbenennungen und Typwechsel kostenlos – kein generierter SQLite-Tabellen-Neubau, den
    /// jemand abnehmen muss.
    /// <para>
    /// Diese Zusicherung ist <b>bewusst endlich</b>: mit der ersten Veröffentlichung braucht es einen
    /// echten Upgrade-Pfad, und dann wird sie entfernt. Dass das eine sichtbare Entscheidung ist statt
    /// einer stillen Erosion, ist ihr eigentlicher Zweck.
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
    /// <b>G9 – nur bewusste DB-Defaults.</b> Ein SQL-<c>DEFAULT</c> ist eine Zusage an Schreiber
    /// <i>außerhalb</i> von EF; EF selbst benennt in jedem <c>INSERT</c> alle gemappten Properties und
    /// konsultiert ihn nie. Vor dem Squash trugen 15 Spalten eine solche Klausel, ohne dass sie irgendwo
    /// im Modell stand: sie waren ein Nebenprodukt davon, per <c>AddColumn(defaultValue:…)</c> angehängt
    /// worden zu sein. Zwei davon waren sogar schädlich – ein <c>ConcurrencyStamp</c> mit Vorgabewert
    /// macht die optimistische Sperre für jede nicht über EF eingefügte Zeile wirkungslos, und das an
    /// geldrelevanten Tabellen.
    /// <para>
    /// Dieser Wächter verhindert, dass sie über die nächsten Migrationen wieder nachwachsen. Ein neuer
    /// Default ist erlaubt – aber nur als <c>HasDefaultValue</c> im Modell und mit einem Eintrag hier,
    /// also als Entscheidung statt als Nebenwirkung.
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
    /// <b>G4 – jedes persistierte Enum liegt als String in der DB.</b> Der Vertrag spricht nach außen
    /// ohnehin Strings (<c>JsonStringEnumConverter</c>); waren es innen Zahlen, gab es zwei Darstellungen
    /// desselben Werts und eine stille Kopplung an die Mitglieder-Reihenfolge – ein eingeschobener
    /// Enum-Wert hätte gespeicherte Daten umgedeutet.
    /// <para>
    /// Erlaubte Ausnahmen sind <c>[Flags]</c> (eine Bit-Kombination hat keinen Namen) und die ordnend
    /// verglichenen Enums, die im DbContext namentlich mit Grund gelistet sind. Dieser Test liest genau
    /// jene Liste, damit Regel und Ausnahme nicht an zwei Orten gepflegt werden müssen.
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
    /// <b>G2 – jeder Fremdschlüssel hat ein abgenommenes Löschverhalten.</b> Ein Löschverhalten ist die
    /// gefährlichste stille Entscheidung im Modell: es entscheidet, ob eine Zeile <i>Daten mitnimmt</i>.
    /// Genau dort saß der teuerste Fund dieses Umbaus – <c>Adult→ShopArticle→ChildInventory</c> war
    /// durchgehend <c>Cascade</c>, sodass das Löschen eines Supervisors bezahltes Kind-Inventar vernichtete,
    /// während die Kaufbelege daneben stehenblieben.
    /// <para>
    /// Warum eine <b>literal gepinnte Tabelle</b> und keine schlaue Regel: Reflexion kann „ausdrücklich
    /// gesetzt" nicht von „von der EF-Konvention geerbt" unterscheiden. Eine Regel könnte also nur die
    /// <i>Werte</i> prüfen, nicht die Absicht – und wäre bei jeder neuen FK stumm. Die Tabelle ist der
    /// ehrliche Ersatz: sie erzwingt bei jeder neuen Beziehung <b>eine bewusste Zeile</b>, und sie schlägt
    /// in beide Richtungen an (auch bei einer FK, die verschwindet).
    /// </para>
    /// <para>
    /// Zusätzlich verboten ist <see cref="DeleteBehavior.ClientSetNull"/> – der Konventions-Default für
    /// optionale Beziehungen. Er räumt nur im <i>geladenen</i> ChangeTracker auf und lässt die DB-Seite
    /// offen. Dieselbe Zusicherung fing beim Ausschreiben der Konventions-Cascades einen echten Fehler:
    /// ein <c>WithMany()</c> ohne die vorhandene Gegen-Navigation ließ EF eine <b>zweite</b> Beziehung
    /// (<c>ChildId1</c>) anlegen, und die trug genau diesen Wert.
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

    /// <summary>
    /// <b>G6 – kein Zeitpunkt und kein Zeitraum als Text.</b> Eine <c>string</c>-Spalte, deren Name eine
    /// Zeitangabe verspricht, ist der teuerste Schema-Fehler dieses Modells gewesen: <c>PeriodKey</c> trug
    /// <b>drei</b> Formate (<c>2026-07-04</c>, <c>2026-W27</c>, <c>once</c>) in <b>vier</b> Tabellen, und alle
    /// vier waren Teil eines Unique-Index – also idempotenz-tragend. Ein Tippfehler im Format hätte doppelt
    /// gezahlt, ohne dass irgendetwas auffällt.
    /// <para>
    /// Der Wächter greift über den <b>Namen</b>, weil das die einzige reflektierbare Spur einer
    /// Zeit-Bedeutung ist. Namen wie <c>Key</c>/<c>Slug</c>, die einen fachlichen <i>Natur</i>schlüssel
    /// bezeichnen, sind erlaubt – aber nur namentlich und mit Grund, damit die nächste <c>…Key</c>-Spalte
    /// eine Entscheidung erzwingt statt sich einzuschleichen. Der Vergleich ist <b>case-sensitiv</b>: er soll
    /// <c>PeriodKey</c> treffen, nicht jedes Wort, das auf „on" endet.
    /// </para>
    /// </summary>
    [Fact]
    public void Keine_Zeitangabe_Als_Text()
    {
        // Verdächtige Endungen: alles, was nach Zeitpunkt oder Zeitraum klingt.
        string[] verdaechtig = ["Key", "Period", "Day", "Date", "On", "At", "Time", "Week", "Month", "Year"];

        // Begründete Ausnahmen: Property → warum der Text hier keine Zeitangabe ist.
        var erlaubt = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Vocabulary.Key"] = "Fachlicher Naturschlüssel der Store-Vokabel (en:hello:de), slug-idempotent.",
            ["MediaAsset.Key"] = "Fachlicher Naturschlüssel des Motivs, Grundlage der Wiedererkennung.",
            ["ClozeText.Key"] = "Fachlicher Naturschlüssel des Lückentexts.",
            // Der einzige echte Fund dieses Wächters beim ersten Lauf – und ein berechtigter:
            ["TimetableEntry.TimeOfDay"] =
                "Freitext und ausdrücklich KEINE Uhrzeit: der Vater schreibt 'Nachmittag' oder "
                + "'1./2. Stunde' hinein. Ein Zeittyp könnte das nicht abbilden, und das Feld steht im "
                + "Vertrag (EntryResponse/CreateEntryDto) – Typisieren wäre ein Bruch ohne Gewinn.",
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

        // Selbstschutz: findet die Reflexion keine String-Spalten, bestünde der Test inhaltsleer.
        Assert.True(alleStrings.Count >= 100, $"Zu wenige String-Spalten gefunden ({alleStrings.Count}).");

        Assert.True(treffer.Count == 0,
            "Diese String-Spalten tragen dem Namen nach eine Zeitangabe. Nimm einen echten Typ "
            + "(DateOnly/DateTime/Enum) – oder trage sie mit Grund in die Ausnahmeliste ein, falls es ein "
            + "fachlicher Naturschlüssel ist:\n  "
            + string.Join("\n  ", treffer.OrderBy(n => n, StringComparer.Ordinal)));
    }

    /// <summary>
    /// <b>G8 – „genau eines von N" steht in der Datenbank, nicht im Kommentar.</b> Drei Tabellen tragen
    /// dieselbe Frage: welcher der mehreren optionalen Fremdschlüssel ist der gültige? Zwei davon
    /// (<c>MediaLink</c>, <c>ChildMediaPick</c>) beantworteten sie vorbildlich per Check-Constraint, die
    /// dritte (<c>AccountProfile</c>) behauptete sie nur im XML-Kommentar – und ein Profil mit beiden
    /// Zielen wäre ein Login mit zwei Identitäten gewesen.
    /// <para>
    /// Ein Wächter auf einer <i>Menge</i> statt auf „mindestens diese drei": eine verschwundene
    /// Invariante ist genauso ein Fund wie eine neue ohne Eintrag. Die gefilterten Unique-Indizes daneben
    /// hängen an derselben Bauart – wer den Constraint entfernt, entfernt auch ihre Voraussetzung.
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
        // Nicht `db.Model`: das laufzeit-optimierte Modell wirft für Check-Constraints ausdrücklich
        // („not stored in the read-optimized model"). EF wirft sie weg, weil zur Laufzeit niemand sie
        // liest – gefragt ist darum das Design-Time-Modell, dasselbe, aus dem die Migration entsteht.
        var entities = db.GetService<IDesignTimeModel>().Model.GetEntityTypes().ToList();

        // Selbstschutz: greift die Reflexion nicht, wäre die Menge leer – und der Vergleich meldete
        // fälschlich „alle drei fehlen" statt „der Test ist kaputt".
        Assert.True(entities.Count >= 55, $"Zu wenige Entity-Typen im Modell ({entities.Count}).");

        // Entity-Name statt Tabellenname als Schlüssel: E11 zieht Tabellennamen auf die DbSet-Namen, und
        // dieser Wächter prüft eine Invariante, nicht die Benennung.
        var gefunden = entities
            .SelectMany(e => e.GetCheckConstraints().Select(c => $"{e.ClrType.Name}.{c.Name}"))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(erwartet, gefunden);
    }
}
