using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pugling.Api.Models;

namespace Pugling.Api.Data;

public class PuglingDbContext(DbContextOptions<PuglingDbContext> options) : DbContext(options)
{
    // Zeitfenster mit Punkte-Multiplikator (Leitner-Wiederholungen, siehe PointsService).
    public DbSet<TimeSlotRule> TimeSlots => Set<TimeSlotRule>();

    // Identität: Login-Konto mit einer/mehreren Rollen (Creator/Supervisor/Student), entkoppelt von Adult/Child.
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountProfile> AccountProfiles => Set<AccountProfile>();

    // Admin-Bereich: Personen (Supervisor >-< Student über SupervisorLink) + Punkte
    public DbSet<Adult> Adults => Set<Adult>();
    public DbSet<Child> Children => Set<Child>();
    // Vom Kind verwendete Lehrbücher (übungsunabhängiges Profil, Grundlage für einen späteren Lehrplan-Generator).
    public DbSet<Textbook> Textbooks => Set<Textbook>();
    public DbSet<SupervisorLink> SupervisorLinks => Set<SupervisorLink>();
    public DbSet<ChildPointsEntry> ChildPoints => Set<ChildPointsEntry>();

    // Unterrichts-Seite des Katalogs: Lehrwerk-Reihe -> Unit, dazu die Creator-Profile („Fachlehrer").
    public DbSet<TextbookSeries> TextbookSeries => Set<TextbookSeries>();
    public DbSet<SeriesUnit> SeriesUnits => Set<SeriesUnit>();
    public DbSet<CreatorProfile> CreatorProfiles => Set<CreatorProfile>();

    // Lern-Katalog: Subject -> Chapter -> Exercise (typisiert)
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ExerciseCategory> ExerciseCategories => Set<ExerciseCategory>();
    // RWX-Rechte einzelner Creator auf eine Übung (Owner/Write/Execute).
    public DbSet<ExerciseGrant> ExerciseGrants => Set<ExerciseGrant>();
    // Stabil identifizierte Items einer Vokabelübung (positionierte Referenz auf den Vokabel-Store).
    public DbSet<ExerciseItem> ExerciseItems => Set<ExerciseItem>();

    // Sprachlernen: atomarer Vokabel-Store + Lückentext-Store
    public DbSet<Vocabulary> Vocabulary => Set<Vocabulary>();
    public DbSet<ClozeText> ClozeTexts => Set<ClozeText>();
    // Kindneutrale Schlagworte für den Vokabel-Katalog (Kapitel/Klasse/Thema)
    public DbSet<VocabTag> VocabTags => Set<VocabTag>();
    public DbSet<VocabTagLink> VocabTagLinks => Set<VocabTagLink>();

    // Medien-Store: Asset = eine Darstellung eines Motivs, Variant = dieselbe Darstellung in einer
    // Auflösung/einem Format. Getaggt mit derselben Taxonomie, die auch die Kind-Interessen nutzen.
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<MediaVariant> MediaVariants => Set<MediaVariant>();
    public DbSet<MediaTagLink> MediaTagLinks => Set<MediaTagLink>();
    // Zuordnung Bild ⇢ Träger (Vokabel / Übungs-Item / Übung) – n:m in beide Richtungen.
    public DbSet<MediaLink> MediaLinks => Set<MediaLink>();
    // Eingefrorene Bildwahl je (Kind, Träger) – Bildkonstanz ist beim Vokabellernen der Merkeffekt.
    public DbSet<ChildMediaPick> ChildMediaPicks => Set<ChildMediaPick>();

    // Geteilte Interessen-/Stil-Taxonomie (Kind ⇢ Tag ⇠ Bild) – Grundlage der individualisierten Bildauswahl.
    public DbSet<InterestTag> InterestTags => Set<InterestTag>();
    public DbSet<ChildInterest> ChildInterests => Set<ChildInterest>();

    // Lehrplan (Container) + Positionen auf Katalog-Übungen, Fortschritt/Ziel-Belohnung je Position
    public DbSet<StudyPlan> StudyPlans => Set<StudyPlan>();
    public DbSet<PlanPosition> PlanPositions => Set<PlanPosition>();
    public DbSet<PositionItemProgress> PositionItemProgress => Set<PositionItemProgress>();
    public DbSet<PositionGoalReward> PositionGoalRewards => Set<PositionGoalReward>();
    public DbSet<PositionGoalPenalty> PositionGoalPenalties => Set<PositionGoalPenalty>();
    public DbSet<PracticeSession> PracticeSessions => Set<PracticeSession>();
    public DbSet<ReviewEvent> ReviewEvents => Set<ReviewEvent>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    // Kein DbSet für TestItemResult: die Tabelle existiert (über die Beziehung unten), wird aber
    // ausschließlich über TestAttempt.Results erreicht – ein eigenes Set wäre ein zweiter Zugang, den
    // niemand nutzt und der zur Umgehung des Versuchs-Kontexts einlädt.
    // Plan-übergreifender Lernstand je (Kind, Item) + Antwort-Historie (stabile ItemId, denormalisierte VocabularyId).
    public DbSet<ItemProgress> ItemProgress => Set<ItemProgress>();
    public DbSet<ItemReviewEvent> ItemReviewEvents => Set<ItemReviewEvent>();
    // Kind-/Scope-bezogene Ergebnis-Lernziele (Beherrschung/Abdeckung), live gegen den Lernstand ausgewertet.
    public DbSet<LearnGoal> LearnGoals => Set<LearnGoal>();
    // „Große Ziele" (OKR-Kern): Objective als Container über messbaren KeyResults + idempotenter Belohnungs-Log.
    public DbSet<Objective> Objectives => Set<Objective>();
    public DbSet<KeyResult> KeyResults => Set<KeyResult>();
    public DbSet<ObjectiveReward> ObjectiveRewards => Set<ObjectiveReward>();

    // Stundenplan-Steuerung
    public DbSet<TimetableEntry> Timetable => Set<TimetableEntry>();

    // Gamification: Missionen (zeitgebundene Ziele) + Auszeichnungen (Badges) je Kind, mit Vergabe-Log
    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<MissionAward> MissionAwards => Set<MissionAward>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<AchievementAward> AchievementAwards => Set<AchievementAward>();

    // Familien-Shop: Vater-Katalog (Artikel + Angebote), kindbezogenes aggregiertes Inventar,
    // Kaufhistorie und Aktivierungsanfragen
    public DbSet<ShopArticle> ShopArticles => Set<ShopArticle>();
    public DbSet<ShopListing> ShopListings => Set<ShopListing>();
    public DbSet<ShopPurchase> ShopPurchases => Set<ShopPurchase>();
    public DbSet<ChildInventory> ChildInventories => Set<ChildInventory>();
    public DbSet<ActivationRequest> ActivationRequests => Set<ActivationRequest>();

    // Tagging + Klassenarbeiten
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ExerciseTag> ExerciseTags => Set<ExerciseTag>();
    public DbSet<VocabularyTag> VocabularyTags => Set<VocabularyTag>();
    public DbSet<Klassenarbeit> Klassenarbeiten => Set<Klassenarbeit>();
    public DbSet<KlassenarbeitExercise> KlassenarbeitExercises => Set<KlassenarbeitExercise>();
    public DbSet<KlassenarbeitTag> KlassenarbeitTags => Set<KlassenarbeitTag>();

    // Anmerkungen beim Testen (Erfassung im UI-Widget, Beantwortung durch Claude Code)
    public DbSet<Remark> Remarks => Set<Remark>();
    public DbSet<RemarkComment> RemarkComments => Set<RemarkComment>();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Identität: Account -> AccountProfile(Rolle -> Adult/Child). Rolle als String (lesbar/stabil).
        // Gefilterte Unique-Indizes verhindern doppelte Profile beim (wiederholten) Backfill.
        modelBuilder.Entity<AccountProfile>(e =>
        {
            // Genau eines von AdultId/ChildId – bisher nur als Kommentar an der Entity behauptet. Beide
            // gesetzt wäre ein Login mit zwei Identitäten dahinter, keines eine Rolle, die auf nichts zeigt
            // (AuthAccess prüfte dann stumm ins Leere). Gleiche Bauart wie bei MediaLink/ChildMediaPick.
            e.ToTable(t => t.HasCheckConstraint("CK_AccountProfile_SingleProfile",
                """
                (CASE WHEN "AdultId" IS NULL THEN 0 ELSE 1 END
                 + CASE WHEN "ChildId" IS NULL THEN 0 ELSE 1 END) = 1
                """));

            e.Property(p => p.Role).HasConversion<string>();
            e.HasOne(p => p.Account).WithMany(a => a.Profiles)
                .HasForeignKey(p => p.AccountId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Adult).WithMany()
                .HasForeignKey(p => p.AdultId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Child).WithMany()
                .HasForeignKey(p => p.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => new { p.Role, p.AdultId }).IsUnique().HasFilter("[AdultId] IS NOT NULL");
            e.HasIndex(p => new { p.Role, p.ChildId }).IsUnique().HasFilter("[ChildId] IS NOT NULL");
            // Ein Konto trägt jede Rolle höchstens einmal. Die zwei Indizes oben verhindern nur, dass
            // dasselbe *Profil* zweimal in einer Rolle hängt – nicht, dass ein Konto zwei Creator-Profile
            // auf verschiedene Adults bekommt. Genau das wäre eine zweite Identität hinter einem Login.
            e.HasIndex(p => new { p.AccountId, p.Role }).IsUnique();
        });
        modelBuilder.Entity<Account>()
            .HasIndex(a => a.Email).IsUnique().HasFilter("[Email] IS NOT NULL");

        // Aussteller-Attribution der Ökonomie (Momentaufnahme): Filter „nur meine Vorgänge" je (Kind, Supervisor).
        modelBuilder.Entity<ShopPurchase>().HasIndex(p => new { p.ChildId, p.SupervisorId });
        modelBuilder.Entity<ActivationRequest>().HasIndex(r => new { r.ChildId, r.SupervisorId });
        modelBuilder.Entity<ChildPointsEntry>(e =>
        {
            // Wallet-Summen und Buchungslisten: Filter nach Kind/Art sowie Paging „neueste zuerst".
            e.HasIndex(p => new { p.ChildId, p.Kind });
            e.HasIndex(p => new { p.ChildId, p.CreatedAt, p.Id });
        });

        // Betreuung Supervisor >-< Student. Ein Student kann mehrere Supervisor haben; ein Paar ist eindeutig.
        // Leaf auf zwei unabhängige Roots (wie ItemProgress) – beide FKs Cascade, kein SQLite-Diamant.
        modelBuilder.Entity<SupervisorLink>(e =>
        {
            e.Property(l => l.Relation).HasConversion<string>();
            e.HasOne(l => l.Supervisor).WithMany(f => f.SupervisedLinks)
                .HasForeignKey(l => l.SupervisorId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Student).WithMany(c => c.SupervisorLinks)
                .HasForeignKey(l => l.StudentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(l => new { l.SupervisorId, l.StudentId }).IsUnique();
            e.HasIndex(l => l.StudentId);
        });

        // Freigeschaltete Skins des Kindes als JSON-Liste (Neuzuweisung im Controller, kein In-Place-Mutieren).
        modelBuilder.Entity<Child>(e =>
        {
            e.Property(c => c.OwnedSkins).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<string>>(s, JsonOptions) ?? new())
                .Metadata.SetValueComparer(JsonValueComparer.For<List<string>>());
            // Interessen des Kindes ebenfalls als JSON-Liste (gleicher ValueComparer-Fallstrick wie OwnedSkins).
            e.Property(c => c.Interests).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<string>>(s, JsonOptions) ?? new())
                .Metadata.SetValueComparer(JsonValueComparer.For<List<string>>());
            // Geschlecht als String (lesbar/stabil, wie SupervisorLink.Relation).
            e.Property(c => c.Gender).HasConversion<string>();
            // Eignungsgrenze bewusst als int (NICHT als String wie die übrigen Enums): der Medien-Selektor
            // vergleicht sie ordnend (Rating <= Erlaubtes). Als String liefe der Vergleich alphabetisch
            // ("Everyone" < "Mature" < "Teen") und wäre schlicht falsch.
            e.Property(c => c.AllowedContentRating).HasConversion<int>();
            // Concurrency-Token: schützt Skin-Kauf/Ausrüsten vor parallelen Doppelbuchungen.
            e.Property(c => c.ConcurrencyStamp).IsConcurrencyToken();
        });

        // Lehrbuch: gehört einem Kind (Cascade – verschwindet mit dem Kind). Der optionale Katalog-Link auf
        // ein Fach nutzt SetNull, damit ein Fach-Löschen die Buch-Zuordnung nicht mitreißt (nur die FK leert).
        modelBuilder.Entity<Textbook>(e =>
        {
            e.HasIndex(t => t.ChildId);
            e.HasOne(t => t.Child).WithMany(c => c.Textbooks).HasForeignKey(t => t.ChildId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Subject).WithMany().HasForeignKey(t => t.SubjectId)
                .OnDelete(DeleteBehavior.SetNull);
            // Reihe und Unit sind Verweise in den geteilten Katalog: eine gelöschte Reihe leert nur die
            // Zuordnung (SetNull) – das Buch des Kindes bleibt mit Titel/Kapitel als Freitext bestehen.
            e.HasOne(t => t.Series).WithMany().HasForeignKey(t => t.SeriesId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.CurrentUnit).WithMany().HasForeignKey(t => t.CurrentUnitId)
                .OnDelete(DeleteBehavior.SetNull);
            // Der heiße Weg des Profil-Matchings: „welche Reihe benutzt dieses Kind?"
            e.HasIndex(t => t.SeriesId);
        });

        // Lehrwerk-Reihe: global eindeutiger Slug (kindneutral wie der Vokabel-Store, Muster InterestTag).
        // Owner nur als Editier-/Löschrecht – ein gelöschter Vater leert die FK, die Reihe bleibt nutzbar.
        modelBuilder.Entity<TextbookSeries>(e =>
        {
            e.HasIndex(s => s.Slug).IsUnique();
            e.HasOne(s => s.Subject).WithMany().HasForeignKey(s => s.SubjectId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(s => s.Owner).WithMany().HasForeignKey(s => s.OwnerAdultId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Unit: gehört der Reihe (Cascade). Der Index bedient die einzige Sortierung, in der Units je
        // gelesen werden – Band, dann Reihenfolge im Band.
        modelBuilder.Entity<SeriesUnit>(e =>
        {
            e.HasIndex(u => new { u.SeriesId, u.Grade, u.OrderIndex });
            e.HasOne(u => u.Series).WithMany(s => s.Units).HasForeignKey(u => u.SeriesId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Creator-Profil: je Owner ein eindeutiger Name; Fach und Reihe sind die beiden Achsen, über die
        // das Matching filtert. Die bevorzugten Übungstypen liegen als JSON-Liste (ValueComparer wie bei Child).
        modelBuilder.Entity<CreatorProfile>(e =>
        {
            e.HasIndex(p => new { p.OwnerAdultId, p.Name }).IsUnique().HasFilter("[OwnerAdultId] IS NOT NULL");
            e.HasIndex(p => new { p.SubjectId, p.SeriesId });
            e.Property(p => p.DefaultTypes).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<string>>(s, JsonOptions) ?? new())
                .Metadata.SetValueComparer(JsonValueComparer.For<List<string>>());
            e.HasOne(p => p.Subject).WithMany().HasForeignKey(p => p.SubjectId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.Series).WithMany().HasForeignKey(p => p.SeriesId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.Owner).WithMany().HasForeignKey(p => p.OwnerAdultId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Vocabulary>(e =>
        {
            e.HasIndex(v => v.Key).IsUnique();

            // Wort/Übersetzung sind der heißeste Lesepfad des Katalogs (Dubletten-Lookup beim Anlegen,
            // Freitextsuche) und hatten keinen Index. Ein Index allein hätte aber nichts gebracht: die
            // Suche verglich `LOWER(Word)`, und über einen Ausdruck greift kein Spaltenindex. Erst die
            // Collation NOCASE macht den Vergleich selbst groß-/kleinschreibungsunabhängig – dann darf
            // das `ToLower()` im Query entfallen und der Index wird benutzt.
            // Folge, die man wissen muss: `Word == "march"` findet ab jetzt auch „March". Für einen
            // Vokabelspeicher ist das gewollt (Groß-/Kleinschreibung ist keine eigene Vokabel), und
            // die Eindeutigkeit hängt ohnehin am `Key`, nicht am Wort.
            e.Property(v => v.Word).UseCollation("NOCASE");
            e.Property(v => v.Translation).UseCollation("NOCASE");
            e.HasIndex(v => v.Word);
            e.HasIndex(v => v.Translation);

            // noun/verb als JSON-Spalten (null bleibt DB-NULL, Converter läuft nur für Werte).
            e.Property(v => v.Noun).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<NounInfo>(s, JsonOptions))
                .Metadata.SetValueComparer(JsonValueComparer.For<NounInfo?>());
            e.Property(v => v.Verb).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<VerbInfo>(s, JsonOptions))
                .Metadata.SetValueComparer(JsonValueComparer.For<VerbInfo?>());

            // Selbst-Referenz auf die Grundform; Löschen einer referenzierten Grundform verhindern.
            e.HasOne(v => v.BaseForm)
                .WithMany()
                .HasForeignKey(v => v.BaseFormId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Vokabel-Tag: global eindeutiger Name (kindneutral, wie der Vokabel-Store).
        modelBuilder.Entity<VocabTag>()
            .HasIndex(t => t.Name).IsUnique();

        // Vokabel <-> Tag: jede Vokabel höchstens einmal je Tag; Links verschwinden mit Tag oder Vokabel.
        modelBuilder.Entity<VocabTagLink>(e =>
        {
            e.HasIndex(x => new { x.VocabTagId, x.VocabularyId }).IsUnique();
            e.HasOne(x => x.VocabTag).WithMany(t => t.Links).HasForeignKey(x => x.VocabTagId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Vocabulary).WithMany(v => v.TagLinks).HasForeignKey(x => x.VocabularyId).OnDelete(DeleteBehavior.Cascade);
        });

        // Geteilte Interessen-/Stil-Taxonomie: global eindeutiger Slug (kindneutral wie der Vokabel-Store).
        // Die Facette bleibt als String lesbar – sie wird nur verglichen, nie geordnet.
        modelBuilder.Entity<InterestTag>(e =>
        {
            e.HasIndex(t => t.Slug).IsUnique();
            e.Property(t => t.Facet).HasConversion<string>();
            e.Property(t => t.Synonyms).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<string>>(s, JsonOptions) ?? new())
                .Metadata.SetValueComparer(JsonValueComparer.For<List<string>>());
        });

        // Kind <-> Interesse: höchstens ein Gewicht je (Kind, Tag). Leaf auf zwei unabhängige Roots
        // (Child, InterestTag) – beide Cascade, kein SQLite-Diamant (Muster wie SupervisorLink).
        modelBuilder.Entity<ChildInterest>(e =>
        {
            e.HasIndex(x => new { x.ChildId, x.InterestTagId }).IsUnique();
            e.HasOne(x => x.Child).WithMany(c => c.InterestTags).HasForeignKey(x => x.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.InterestTag).WithMany(t => t.ChildInterests).HasForeignKey(x => x.InterestTagId).OnDelete(DeleteBehavior.Cascade);
        });

        // Medien-Asset: eindeutiger Key (wie Vocabulary). Das Rating liegt – anders als Kind/Origin –
        // als int in der DB, weil der Selektor ordnend darauf filtert (siehe Kommentar bei Child).
        modelBuilder.Entity<MediaAsset>(e =>
        {
            e.HasIndex(a => a.Key).IsUnique();
            // Der Selektor filtert immer zuerst Art + Eignung, bevor er nach Interessen sortiert.
            e.HasIndex(a => new { a.Kind, a.Rating });
            e.Property(a => a.Kind).HasConversion<string>();
            e.Property(a => a.Origin).HasConversion<string>();
            e.Property(a => a.Rating).HasConversion<int>();
        });

        // Variante: gehört dem Asset (Cascade). Je Asset höchstens eine Datei pro (Zweck, Format) –
        // sonst müsste die Auslieferung zwischen gleichwertigen Kandidaten willkürlich wählen.
        modelBuilder.Entity<MediaVariant>(e =>
        {
            e.HasIndex(v => new { v.MediaAssetId, v.Purpose, v.Format }).IsUnique();
            e.Property(v => v.Purpose).HasConversion<string>();
            e.HasOne(v => v.MediaAsset).WithMany(a => a.Variants).HasForeignKey(v => v.MediaAssetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Bild <-> Tag: jedes Asset höchstens einmal je Tag. Der zusätzliche Index auf den Tag bedient
        // die heiße Richtung der späteren Auswahl („welche Assets tragen dieses Interesse?").
        modelBuilder.Entity<MediaTagLink>(e =>
        {
            e.HasIndex(x => new { x.MediaAssetId, x.InterestTagId }).IsUnique();
            e.HasIndex(x => x.InterestTagId);
            e.HasOne(x => x.MediaAsset).WithMany(a => a.TagLinks).HasForeignKey(x => x.MediaAssetId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.InterestTag).WithMany(t => t.MediaLinks).HasForeignKey(x => x.InterestTagId).OnDelete(DeleteBehavior.Cascade);
        });

        // Bild ⇢ Träger. Genau eine der drei FKs ist gesetzt – als Check-Constraint in der DB, nicht nur
        // im Controller: eine Zeile ohne Träger wäre unsichtbar, eine mit zweien mehrdeutig auflösbar.
        // Je Träger ein eigener gefilterter Unique-Index (dasselbe Bild nicht zweimal am selben Objekt)
        // – ein gemeinsamer Index über alle drei Spalten griffe nicht, weil NULLs in SQLite als
        // verschieden gelten. Alle FKs Cascade: ein gelöschtes Bild/Objekt lässt keine Zuordnung zurück.
        // Kein Diamant trotz Exercise → ExerciseItem → MediaLink, weil eine Zeile per Constraint immer
        // nur an EINEM Träger hängt (die anderen Spalten sind NULL und werden nie mitgelöscht).
        modelBuilder.Entity<MediaLink>(e =>
        {
            e.ToTable(t => t.HasCheckConstraint("CK_MediaLink_SingleCarrier",
                """
                (CASE WHEN "VocabularyId" IS NULL THEN 0 ELSE 1 END
                 + CASE WHEN "ExerciseItemId" IS NULL THEN 0 ELSE 1 END
                 + CASE WHEN "ExerciseId" IS NULL THEN 0 ELSE 1 END) = 1
                """));

            e.HasIndex(l => new { l.MediaAssetId, l.VocabularyId }).IsUnique().HasFilter("[VocabularyId] IS NOT NULL");
            e.HasIndex(l => new { l.MediaAssetId, l.ExerciseItemId }).IsUnique().HasFilter("[ExerciseItemId] IS NOT NULL");
            e.HasIndex(l => new { l.MediaAssetId, l.ExerciseId }).IsUnique().HasFilter("[ExerciseId] IS NOT NULL");
            // Die Gegenrichtung: „welche Verknüpfungen hat dieses Asset?" (Aufräumen beim Löschen).
            // Die drei gefilterten Uniques oben beginnen mit MediaAssetId, können diese Query aber
            // nicht bedienen: ohne Einschränkung auf einen Träger ist ihr Filter nicht impliziert.
            e.HasIndex(l => l.MediaAssetId);
            // Die heiße Richtung der Auswahl: „welche Bilder hängen an dieser Vokabel / diesem Item?"
            e.HasIndex(l => l.VocabularyId);
            e.HasIndex(l => l.ExerciseItemId);
            e.HasIndex(l => l.ExerciseId);

            e.HasOne(l => l.MediaAsset).WithMany(a => a.Links).HasForeignKey(l => l.MediaAssetId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Vocabulary).WithMany().HasForeignKey(l => l.VocabularyId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.ExerciseItem).WithMany().HasForeignKey(l => l.ExerciseItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Exercise).WithMany().HasForeignKey(l => l.ExerciseId).OnDelete(DeleteBehavior.Cascade);
        });

        // Eingefrorene Bildwahl. Eine Zeile je Kandidat (nicht je Träger): die aktive Wahl ist die Zeile
        // ohne Rejected, abgelehnte bleiben als Ausschluss stehen. Genau ein Träger je Zeile – gleiche
        // Begründung und gleiche Bauart wie beim MediaLink (Check-Constraint + gefilterte Unique-Indizes).
        modelBuilder.Entity<ChildMediaPick>(e =>
        {
            e.ToTable(t => t.HasCheckConstraint("CK_ChildMediaPick_SingleCarrier",
                """
                (CASE WHEN "VocabularyId" IS NULL THEN 0 ELSE 1 END
                 + CASE WHEN "ExerciseItemId" IS NULL THEN 0 ELSE 1 END) = 1
                """));

            e.HasIndex(p => new { p.ChildId, p.VocabularyId, p.MediaAssetId }).IsUnique().HasFilter("[VocabularyId] IS NOT NULL");
            e.HasIndex(p => new { p.ChildId, p.ExerciseItemId, p.MediaAssetId }).IsUnique().HasFilter("[ExerciseItemId] IS NOT NULL");
            // Kein zusätzlicher Index auf (ChildId, VocabularyId)/(ChildId, ExerciseItemId): per
            // EXPLAIN QUERY PLAN gemessen wählt SQLite für „was ist für dieses Kind an diesem Träger
            // gewählt?" die gefilterten Unique-Indizes oben – eine Gleichheit auf der Trägerspalte
            // impliziert deren `IS NOT NULL`-Filter. Die früheren Zusatzindizes wurden nie benutzt.

            e.HasOne(p => p.Child).WithMany().HasForeignKey(p => p.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Vocabulary).WithMany().HasForeignKey(p => p.VocabularyId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.ExerciseItem).WithMany().HasForeignKey(p => p.ExerciseItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.MediaAsset).WithMany().HasForeignKey(p => p.MediaAssetId).OnDelete(DeleteBehavior.Cascade);
        });

        ApplyExplicitCascades(modelBuilder);

        // Kapitelnamen sind je Fach eindeutig: zwei „Unit 1" im selben Fach sind eine Dublette, und
        // Auswahllisten wie die Zuordnung durch den Agenten hängen am Namen.
        modelBuilder.Entity<Chapter>().HasIndex(c => new { c.SubjectId, c.Name }).IsUnique();
        // Immerhin ein Index auf den Fach-Namen: `Subjects` hatte außer dem Primärschlüssel keinen,
        // obwohl jede Katalog-Ansicht danach sucht und sortiert.
        modelBuilder.Entity<Subject>().HasIndex(s => s.Name);
        // BEWUSST NICHT eindeutig. Naheliegend wäre es – „Englisch" zweimal ist unschön. Aber `Subject`
        // trägt keinen Owner: ein globaler Unique machte den wichtigsten Namensraum des Katalogs
        // first-come-first-served über alle Creator hinweg, und jeder weitere Lehrer müsste seine Kapitel
        // an ein Fach hängen, das ihm nicht gehört. Das ist eine Produktentscheidung über Katalog-Eigentum
        // (und ein Vertragsbruch: POST /subjects antwortete dann 409), nicht das Schließen einer
        // Strukturlücke. Erst entscheiden, wem ein Fach gehört – dann eindeutig machen.

        // Die E-Mail des Erwachsenen ist ein Login-Merkmal und war frei duplizierbar, während der
        // gefilterte Unique-Index nur am Konto hing. Gefiltert, weil die Adresse optional bleibt
        // (ein Kind-betreuender Vater braucht keine).
        modelBuilder.Entity<Adult>().HasIndex(a => a.Email).IsUnique().HasFilter("[Email] IS NOT NULL");

        // Bonus-Vorschlag der Übung als JSON-Spalte (null bleibt DB-NULL; Converter läuft nur für Werte).
        modelBuilder.Entity<Exercise>()
            .Property(e => e.SuggestedBonus).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<SuggestedBonus>(s, JsonOptions))
                .Metadata.SetValueComparer(JsonValueComparer.For<SuggestedBonus?>());

        // Fachabhängige Übungs-Arten: Name je Fach eindeutig, Löschen des Fachs entfernt die Arten.
        modelBuilder.Entity<ExerciseCategory>(e =>
        {
            e.HasIndex(c => new { c.SubjectId, c.Name }).IsUnique();
            e.HasOne(c => c.Subject)
                .WithMany(s => s.Categories)
                .HasForeignKey(c => c.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Übung → Art (optional): Löschen einer Art setzt nur die FK auf null, löscht die Übung NICHT.
        modelBuilder.Entity<Exercise>()
            .HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Übung → Autor (optional): Der Katalog ist global; der Autor schützt nur das Editier-/Löschrecht.
        // Löschen des Autors setzt die FK auf null (Übung bleibt für fremde Lehrpläne nutzbar), löscht sie NICHT.
        modelBuilder.Entity<Exercise>()
            .HasOne(e => e.Author)
            .WithMany()
            .HasForeignKey(e => e.AuthorAdultId)
            .OnDelete(DeleteBehavior.SetNull);

        /*
         * Der DB-Default gehört ins Modell, nicht nur in eine alte Migration.
         *
         * `ExecutePublic` bekam seinen `DEFAULT 1` einst über `AddColumn(defaultValue: true)`. SQLite baut
         * eine Tabelle bei jeder Spaltenumbenennung neu – und ein Neubau folgt dem **Modell**. Beim Umbau
         * Father→Adult wäre der Default darum lautlos verschwunden; ein Fremd-`INSERT` ohne die Spalte
         * scheiterte plötzlich. Hier steht er als Absicht: neue Übungen sind öffentlich zuweisbar, solange
         * niemand widerspricht.
         */
        modelBuilder.Entity<Exercise>().Property(e => e.ExecutePublic).HasDefaultValue(true);

        // Bislang gab es keinen Index auf den Autor; die neuen Grant-Joins und der `mineOnly`-Filter profitieren.
        modelBuilder.Entity<Exercise>().HasIndex(e => e.AuthorAdultId);
        // Der Übungstyp ist der häufigste Katalogfilter (Typ-Listen, ExerciseControllerBase).
        modelBuilder.Entity<Exercise>().HasIndex(e => e.Type);

        // RWX-Grant: Recht eines Creator auf eine Übung. Leaf auf zwei unabhängige Roots (Exercise, Adult) –
        // beide FKs Cascade, kein SQLite-Diamant (Muster wie SupervisorLink). Paar+Recht eindeutig (Idempotenz).
        modelBuilder.Entity<ExerciseGrant>(e =>
        {
            e.Property(g => g.Permission).HasConversion<string>();
            e.HasOne(g => g.Exercise).WithMany(x => x.Grants)
                .HasForeignKey(g => g.ExerciseId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(g => g.Creator).WithMany()
                .HasForeignKey(g => g.CreatorId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(g => new { g.ExerciseId, g.CreatorId, g.Permission }).IsUnique();
            e.HasIndex(g => g.CreatorId);
        });

        // Vokabel-Item: gehört einer Übung (Cascade – verschwindet mit ihr) und referenziert eine Store-Vokabel.
        // Die Vokabel darf nicht gelöscht werden, solange ein Item sie nutzt (Restrict, wie beim Übungs-Store-Bezug);
        // der Controller fängt das vorher als sauberen 409 ab. OrderIndex ist reiner Sortierschlüssel (bewusst NICHT
        // unique): der Lehrplan-Motor leitet den stabilen Item-Index aus der Listenposition (sortiert nach OrderIndex,
        // Id) ab, sodass Umsortieren ohne transiente Unique-Kollisionen (SQLite prüft je Statement) auskommt.
        modelBuilder.Entity<ExerciseItem>(e =>
        {
            e.HasIndex(i => new { i.ExerciseId, i.OrderIndex });
            // Dieselbe Store-Vokabel darf in einer Übung nur einmal vorkommen. Ohne diese Zusicherung
            // entstehen zwei Items für dasselbe Wort und damit zwei konkurrierende ItemProgress-Zeilen –
            // der Lernstand desselben Worts liefe innerhalb einer Übung auseinander.
            e.HasIndex(i => new { i.ExerciseId, i.VocabularyId }).IsUnique();
            e.HasOne(i => i.Exercise).WithMany().HasForeignKey(i => i.ExerciseId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Vocabulary).WithMany().HasForeignKey(i => i.VocabularyId).OnDelete(DeleteBehavior.Restrict);
        });

        // Lückentext-Store: eindeutiger Key + Gaps/WordBank als JSON-Spalten.
        modelBuilder.Entity<ClozeText>(e =>
        {
            e.HasIndex(c => c.Key).IsUnique();
            e.Property(c => c.Gaps).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<Gap>>(s, JsonOptions) ?? new())
                .Metadata.SetValueComparer(JsonValueComparer.For<List<Gap>>());
            e.Property(c => c.WordBank).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<string>>(s, JsonOptions))
                .Metadata.SetValueComparer(JsonValueComparer.For<List<string>?>());
        });

        // Lehrplan optional an ein Katalog-Fach gekoppelt (für Stundenplan-Steuerung).
        modelBuilder.Entity<StudyPlan>()
            .HasOne(p => p.Subject).WithMany().HasForeignKey(p => p.SubjectId)
            .OnDelete(DeleteBehavior.SetNull);

        // Lehrplan-Position (neues Modell): gehört einem Plan (Cascade) und verweist auf eine Katalog-Übung.
        // Die Übung darf nicht gelöscht werden, solange sie in einer Position steckt (Restrict, wie bei
        // Vokabeln/Lückentexten). Leitner-Intervalle und Stufen-Fahrplan liegen als JSON-Spalten an der Position.
        modelBuilder.Entity<PlanPosition>(e =>
        {
            // Plan-Ladevorgänge filtern nach StudyPlanId und sortieren nach Order/Id.
            e.HasIndex(p => new { p.StudyPlanId, p.Order, p.Id });
            e.HasOne(p => p.StudyPlan).WithMany(s => s.Positions).HasForeignKey(p => p.StudyPlanId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Exercise).WithMany().HasForeignKey(p => p.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(p => p.BoxIntervalDays).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<int>>(s, JsonOptions))
                .Metadata.SetValueComparer(JsonValueComparer.For<List<int>?>());
            e.Property(p => p.StageSchedule).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<StageStep>>(s, JsonOptions))
                .Metadata.SetValueComparer(JsonValueComparer.For<List<StageStep>?>());
        });

        // Fortschritt je Inhalts-Atom einer Position: verschwindet mit der Position (Cascade);
        // je Position höchstens ein Fortschritts-Satz pro Item-Index.
        modelBuilder.Entity<PositionItemProgress>(e =>
        {
            e.HasIndex(p => new { p.PlanPositionId, p.ItemIndex }).IsUnique();
            e.HasOne(p => p.PlanPosition).WithMany(pos => pos.ItemProgress).HasForeignKey(p => p.PlanPositionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Plan-übergreifender Lernstand je (Kind, Item): genau eine Zeile pro (Kind, Item); Index (Kind, Vokabel)
        // für das Wort-Rollup. Verschwindet mit dem Kind ODER dem Item (beide Cascade; keine Diamant-Pfade, da
        // Kind und Item unabhängige Wurzeln sind).
        modelBuilder.Entity<ItemProgress>(e =>
        {
            e.HasIndex(p => new { p.ChildId, p.ItemId }).IsUnique();
            e.HasIndex(p => new { p.ChildId, p.VocabularyId });
            e.HasIndex(p => new { p.ChildId, p.ExerciseId });
            e.HasOne(p => p.Child).WithMany().HasForeignKey(p => p.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Item).WithMany().HasForeignKey(p => p.ItemId).OnDelete(DeleteBehavior.Cascade);
        });

        // Antwort-Historie je (Kind, Item): gehört dem Kind (Cascade). Die Item-Referenz wird beim Löschen des
        // Items auf null gesetzt (SetNull), damit die Wort-Historie (VocabularyId denormalisiert) erhalten bleibt.
        modelBuilder.Entity<ItemReviewEvent>(e =>
        {
            e.HasIndex(x => new { x.ChildId, x.ItemId, x.At });
            e.HasIndex(x => new { x.ChildId, x.VocabularyId });
            e.HasOne(x => x.Child).WithMany().HasForeignKey(x => x.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.SetNull);
        });

        // Ziel-Belohnung je Position/Periode: höchstens eine Buchung pro (Position, Periode) – die
        // Idempotenz-Garantie der Ziel-Punkte. Verschwindet mit der Position (Cascade).
        // Die Taktung gehört in den Schlüssel: sie ist auf der Buchung eine Momentaufnahme, und nach einem
        // Wechsel Tag→Woche bezeichnet derselbe Perioden-Anfang zwei verschiedene Perioden.
        modelBuilder.Entity<PositionGoalReward>(e =>
        {
            e.HasIndex(r => new { r.PlanPositionId, r.Cadence, r.PeriodStart }).IsUnique();
            e.HasOne(r => r.PlanPosition).WithMany().HasForeignKey(r => r.PlanPositionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Ziel-Malus je Position/Periode: höchstens ein Abzug pro (Position, Periode) – die Idempotenz-Garantie
        // gegen doppelte Bestrafung, wenn das Lazy Settlement mehrfach über dieselbe Periode läuft. Cascade mit der Position.
        modelBuilder.Entity<PositionGoalPenalty>(e =>
        {
            e.HasIndex(r => new { r.PlanPositionId, r.Cadence, r.PeriodStart }).IsUnique();
            e.HasOne(r => r.PlanPosition).WithMany().HasForeignKey(r => r.PlanPositionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Objective (großes Ziel) gehört einem Kind (Cascade); seine KeyResults hängen am Objective (Cascade).
        // Der Katalog-Scope eines KeyResults ist bewusst NICHT als FK modelliert (nur Ids), wie beim LearnGoal –
        // die Auswertung läuft über den Lernstand-Snapshot, nicht über Navigationspfade.
        modelBuilder.Entity<Objective>(e =>
        {
            e.HasOne(o => o.Child).WithMany().HasForeignKey(o => o.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(o => o.KeyResults).WithOne(k => k.Objective!).HasForeignKey(k => k.ObjectiveId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Objective-Belohnungs-Log: höchstens eine Buchung je (Objective, Anlass) – die Idempotenz-Garantie
        // gegen doppelte Auszahlung, wenn das Lazy Settlement mehrfach läuft. Cascade mit dem Objective.
        // Zwei GEFILTERTE Uniques statt einem, weil der Anlass zwei Ausprägungen hat und SQLite NULLs als
        // verschieden behandelt: ein einzelner Unique über die nullable Spalte ließe beliebig viele
        // Abschluss-Buchungen zu – und das ist der große Batzen, also Geld.
        modelBuilder.Entity<ObjectiveReward>(e =>
        {
            e.HasIndex(r => new { r.ObjectiveId, r.PaidKeyResultId }).IsUnique()
                .HasFilter("[PaidKeyResultId] IS NOT NULL");
            e.HasIndex(r => r.ObjectiveId, "IX_ObjectiveRewards_ObjectiveId_Complete").IsUnique()
                .HasFilter("[PaidKeyResultId] IS NULL");
            // Der Fremdschlüssel-Index von Hand, weil die Konvention ihn nur anlegt, solange die Spalte
            // *keinen* Index hat – die zwei gefilterten oben zählen für sie mit, taugen aber nicht: ein
            // partieller Index bedient ein blankes `WHERE ObjectiveId IN (…)` nicht. Und genau das ist der
            // heiße Lesepfad (ObjectiveRewardService lädt bei jedem Kind-Login die gebuchten Anlässe).
            e.HasIndex(r => r.ObjectiveId, "IX_ObjectiveRewards_ObjectiveId");
            e.HasOne(r => r.Objective).WithMany().HasForeignKey(r => r.ObjectiveId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Übungssitzung/Test optional an eine Position gekoppelt (neues Modell). Beide hängen bereits über
        // StudyPlanId am Plan (Cascade); der Positions-Verweis nutzt daher SetNull, um in SQLite keine
        // zweiten Cascade-Pfade (Plan → Position → Session/Test) neben Plan → Session/Test zu erzeugen.
        // Die eingefrorene Ausspiel-Reihenfolge (Cursor-Modell) liegt als JSON-Spalte (Neuzuweisung im Controller).
        modelBuilder.Entity<PracticeSession>(e =>
        {
            // Ziel-/Metrik-Queries: Position+Tag(+Modus) sowie Child-Rollups über StudyPlan+Tag.
            e.HasIndex(s => new { s.PlanPositionId, s.Day, s.Mode });
            e.HasIndex(s => new { s.StudyPlanId, s.Day });
            e.HasOne(s => s.PlanPosition).WithMany().HasForeignKey(s => s.PlanPositionId)
                .OnDelete(DeleteBehavior.SetNull);
            e.Property(s => s.Order).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<int>>(s, JsonOptions) ?? new())
                .Metadata.SetValueComparer(JsonValueComparer.For<List<int>>());
        });
        modelBuilder.Entity<TestAttempt>(e =>
        {
            // Ziel-/Metrik-Queries: Position+Tag mit Abschlussstatus sowie Child-Rollups über StudyPlan+Tag.
            e.HasIndex(t => new { t.PlanPositionId, t.Day, t.CompletedAt, t.Passed });
            e.HasIndex(t => new { t.StudyPlanId, t.Day });
            e.HasOne(t => t.PlanPosition).WithMany().HasForeignKey(t => t.PlanPositionId)
                .OnDelete(DeleteBehavior.SetNull);
            e.Property(t => t.Order).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<int>>(s, JsonOptions) ?? new())
                .Metadata.SetValueComparer(JsonValueComparer.For<List<int>>());
        });

        // Stundenplan-Eintrag: Kind + Fach; ein Fach je Kind/Wochentag höchstens einmal.
        modelBuilder.Entity<TimetableEntry>(e =>
        {
            e.HasIndex(t => new { t.ChildId, t.SubjectId, t.DayOfWeek }).IsUnique();
            e.HasOne(t => t.Child).WithMany().HasForeignKey(t => t.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Subject).WithMany().HasForeignKey(t => t.SubjectId).OnDelete(DeleteBehavior.Cascade);
        });

        // Mission gehört einem Kind (Cascade); jede Mission wird je Zeitraum höchstens einmal belohnt.
        // Der Lesepfad ist immer „die aktiven Missionen dieses Kindes". Bewusst *kein* Unique auf
        // (ChildId, Metric): „20 Wörter täglich" und „100 Wörter wöchentlich" sind zwei legitime
        // Missionen auf derselben Metrik.
        modelBuilder.Entity<Mission>().HasIndex(m => new { m.ChildId, m.Active });
        modelBuilder.Entity<Mission>()
            .HasOne(m => m.Child).WithMany().HasForeignKey(m => m.ChildId).OnDelete(DeleteBehavior.Cascade);
        // Zwei GEFILTERTE Uniques wie beim ObjectiveReward: `OneOff` hat keinen Zeitraum (PeriodStart NULL),
        // und SQLite behandelt NULLs als verschieden – ein einzelner Unique über die nullable Spalte ließe
        // beliebig viele Einmal-Belohnungen zu. Genau dieser Fallstrick machte den Text-Schlüssel attraktiv.
        // Anders als dort braucht es hier KEINEN zusätzlichen Fremdschlüssel-Index: jede Abfrage auf
        // MissionAwards nennt (MissionId, Period, PeriodStart) vollständig, es gibt keinen Lesepfad auf
        // MissionId allein. Nur die Kaskade sucht so – auf einer Tabelle mit einer Handvoll Zeilen je Mission.
        modelBuilder.Entity<MissionAward>(e =>
        {
            e.HasIndex(a => new { a.MissionId, a.Period, a.PeriodStart }).IsUnique()
                .HasFilter("[PeriodStart] IS NOT NULL");
            e.HasIndex(a => new { a.MissionId, a.Period }).IsUnique().HasFilter("[PeriodStart] IS NULL");
            e.HasOne(a => a.Mission).WithMany().HasForeignKey(a => a.MissionId).OnDelete(DeleteBehavior.Cascade);
        });

        // Auszeichnung gehört einem Kind (Cascade); wird genau einmal verliehen. Dieselbe Schwelle
        // derselben Metrik zweimal anzulegen wäre eine Dublette – die Badge käme doppelt.
        modelBuilder.Entity<Achievement>().HasIndex(a => new { a.ChildId, a.Metric, a.Threshold }).IsUnique();
        modelBuilder.Entity<Achievement>()
            .HasOne(a => a.Child).WithMany().HasForeignKey(a => a.ChildId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AchievementAward>(e =>
        {
            e.HasIndex(a => a.AchievementId).IsUnique();
            e.HasOne(a => a.Achievement).WithMany().HasForeignKey(a => a.AchievementId).OnDelete(DeleteBehavior.Cascade);
        });

        // Shop-Artikel: familieninterne Artikelnummer eindeutig; gehört zum Vater (Cascade).
        // Angebote (ShopListing): gehören zum Artikel (Cascade).
        // Käufe (ShopPurchase): gehören zum Kind (Cascade); Angebots-Referenz wird auf null gesetzt,
        //   wenn das Angebot gelöscht wird, damit die Kaufhistorie erhalten bleibt.
        // Inventar (ChildInventory): gehört zum Kind (Cascade); Artikel-Referenz SetNull (s. u.).
        // Aktivierungsanfragen: gehören zum Kind (Cascade); Artikel-Referenz SetNull für Histor stabil.
        modelBuilder.Entity<ShopArticle>(e =>
        {
            e.HasIndex(a => new { a.AdultId, a.ArticleNumber }).IsUnique();
            e.HasOne(a => a.Adult).WithMany().HasForeignKey(a => a.AdultId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ShopListing>(e =>
        {
            e.Property(l => l.ConcurrencyStamp).IsConcurrencyToken();
            e.HasOne(l => l.ShopArticle).WithMany(a => a.Listings).HasForeignKey(l => l.ShopArticleId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ShopPurchase>(e =>
        {
            e.Property(p => p.ConcurrencyStamp).IsConcurrencyToken();
            e.HasOne(p => p.Child).WithMany().HasForeignKey(p => p.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.ShopListing).WithMany().HasForeignKey(p => p.ShopListingId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<ChildInventory>(e =>
        {
            // GEFILTERT eindeutig – und der Filter ist keine Kosmetik: SQLite behandelt NULLs als
            // verschieden, ein Unique über die nullable Spalte hielte die Invariante also nur für Zeilen
            // mit Artikel. Genau so ist es gewollt: je lebendem Artikel höchstens eine Zeile, während
            // zwei verschiedene gelöschte Artikel zwei verwaiste Bestände hinterlassen dürfen, die nicht
            // miteinander kollidieren (und die der Upsert-Lookup `== article.Id` nie wieder trifft).
            e.HasIndex(i => new { i.ChildId, i.ShopArticleId }).IsUnique()
                .HasFilter("[ShopArticleId] IS NOT NULL");
            // Der Vater-Filter läuft seit der Momentaufnahme über SupervisorId statt über die Navigation.
            e.HasIndex(i => new { i.ChildId, i.SupervisorId });
            e.Property(i => i.ConcurrencyStamp).IsConcurrencyToken();
            e.HasOne(i => i.Child).WithMany().HasForeignKey(i => i.ChildId).OnDelete(DeleteBehavior.Cascade);
            // SetNull statt Cascade: bezahlte Einheiten sind Geld und dürfen nicht mit dem Katalogeintrag
            // verschwinden. Die Kaufbelege standen schon so daneben – das Inventar nicht, sodass Löschen
            // eines Artikels den Gegenwert vernichtete und nur den Beleg übrigließ. Der Artikel selbst
            // bleibt bewusst Cascade unter dem Erwachsenen: ein Vater mit Artikeln muss sich löschen können.
            e.HasOne(i => i.ShopArticle).WithMany().HasForeignKey(i => i.ShopArticleId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<ActivationRequest>(e =>
        {
            e.HasOne(r => r.Child).WithMany().HasForeignKey(r => r.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.ShopArticle).WithMany().HasForeignKey(r => r.ShopArticleId).OnDelete(DeleteBehavior.SetNull);
        });

        // Tag: pro Kind eindeutiger Name; löscht das Kind, verschwinden seine Tags.
        modelBuilder.Entity<Tag>(e =>
        {
            e.HasIndex(t => new { t.ChildId, t.Name }).IsUnique();
            e.HasOne(t => t.Child).WithMany().HasForeignKey(t => t.ChildId).OnDelete(DeleteBehavior.Cascade);
        });

        // Übung <-> Tag: jede Übung höchstens einmal je Tag; Links verschwinden mit Tag oder Übung.
        modelBuilder.Entity<ExerciseTag>(e =>
        {
            e.HasIndex(x => new { x.TagId, x.ExerciseId }).IsUnique();
            e.HasOne(x => x.Tag).WithMany(t => t.ExerciseTags).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Cascade);
        });

        // Vokabel <-> Kind-Tag: jede Vokabel höchstens einmal je Tag; Links verschwinden mit Tag oder Vokabel.
        modelBuilder.Entity<VocabularyTag>(e =>
        {
            e.HasIndex(x => new { x.TagId, x.VocabularyId }).IsUnique();
            e.HasOne(x => x.Tag).WithMany(t => t.VocabularyTags).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Vocabulary).WithMany().HasForeignKey(x => x.VocabularyId).OnDelete(DeleteBehavior.Cascade);
        });

        // Klassenarbeit: gehört einem Kind (Cascade), optional an ein Fach gekoppelt (SetNull).
        modelBuilder.Entity<Klassenarbeit>(e =>
        {
            e.HasOne(k => k.Child).WithMany().HasForeignKey(k => k.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(k => k.Subject).WithMany().HasForeignKey(k => k.SubjectId).OnDelete(DeleteBehavior.SetNull);
            e.Property(k => k.Grade).HasPrecision(3, 1);
        });

        // Klassenarbeit <-> Übung: jede Übung höchstens einmal je Arbeit.
        modelBuilder.Entity<KlassenarbeitExercise>(e =>
        {
            e.HasIndex(x => new { x.KlassenarbeitId, x.ExerciseId }).IsUnique();
            e.HasOne(x => x.Klassenarbeit).WithMany(k => k.Exercises).HasForeignKey(x => x.KlassenarbeitId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Cascade);
        });

        // Klassenarbeit <-> Tag: jeder Tag höchstens einmal je Arbeit.
        modelBuilder.Entity<KlassenarbeitTag>(e =>
        {
            e.HasIndex(x => new { x.KlassenarbeitId, x.TagId }).IsUnique();
            e.HasOne(x => x.Klassenarbeit).WithMany(k => k.Tags).HasForeignKey(x => x.KlassenarbeitId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
        });

        // Anmerkung: Der Autor bindet (Cascade – ohne Konto gibt es niemanden, dem die Notiz gehört).
        // Jeder Kontext-Bezug dagegen SetNull: Ein gelöschtes Kind, eine gelöschte Übung oder eine
        // gelöschte Vorgänger-Anmerkung darf das Löschen nicht blockieren – der Kontext darf verblassen,
        // die Beobachtung bleibt. Rolle als String (lesbar/stabil, wie beim AccountProfile).
        modelBuilder.Entity<Remark>(e =>
        {
            e.Property(r => r.AuthorRole).HasConversion<string>();
            e.HasOne(r => r.Account).WithMany().HasForeignKey(r => r.AccountId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Child).WithMany().HasForeignKey(r => r.ChildId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(r => r.Exercise).WithMany().HasForeignKey(r => r.ExerciseId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(r => r.StudyPlan).WithMany().HasForeignKey(r => r.StudyPlanId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(r => r.PlanPosition).WithMany().HasForeignKey(r => r.PlanPositionId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(r => r.ParentRemark).WithMany().HasForeignKey(r => r.ParentRemarkId).OnDelete(DeleteBehavior.SetNull);
            // Die beiden Wege, auf denen gelesen wird: die eigene Liste im Widget (neueste zuerst)
            // und der Export/Nachbereitungs-Skill, der die offenen Anmerkungen holt.
            e.HasIndex(r => new { r.AccountId, r.CreatedAt });
            e.HasIndex(r => r.Status);
        });

        // Verlauf einer Anmerkung. Die Anmerkung bindet (Cascade): ein Beitrag ohne Vorgang ist sinnlos –
        // anders als der Kontext, der verblassen darf. Das Autor-Konto dagegen SetNull, denn die fachliche
        // Aussage des Beitrags gilt weiter, auch wenn das Konto verschwindet.
        // Herkunft als String wie die `AuthorRole` daneben: An dieser Tabelle wird von Hand nachgesehen
        // (Werkzeug für die Entwicklung), und „Assistant" liest sich dabei besser als „1".
        modelBuilder.Entity<RemarkComment>(e =>
        {
            e.Property(c => c.Author).HasConversion<string>();
            e.HasOne(c => c.Remark).WithMany(r => r.Comments).HasForeignKey(c => c.RemarkId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.AuthorAccount).WithMany().HasForeignKey(c => c.AuthorAccountId).OnDelete(DeleteBehavior.SetNull);
            // Der einzige Lesepfad: der Verlauf einer Anmerkung, chronologisch.
            e.HasIndex(c => new { c.RemarkId, c.CreatedAt });
        });

        ApplyEnumConvention(modelBuilder);
    }

    /// <summary>
    /// Schreibt die Löschverhalten aus, die bisher nur aus der EF-<b>Konvention</b> kamen (Pflicht-FK ⇒
    /// <c>Cascade</c>). Das Verhalten ändert sich dadurch nicht – sichtbar wird die <b>Absicht</b>.
    /// <para>
    /// Warum das nicht kosmetisch ist: Reflexion kann „ausdrücklich gesetzt" nicht von „von der Konvention
    /// geerbt" unterscheiden, ein Wächter kann die Regel also nicht am Modell prüfen. Erst wenn jede FK
    /// eine Zeile hat, ist die Zusicherungs-Tabelle in <c>SchemaGuardTests</c> (G2) vollständig – und ein
    /// Konventionswechsel in einer künftigen EF-Version verschiebt hier nichts mehr lautlos.
    /// </para>
    /// <para>
    /// Es sind Kompositions-Beziehungen: das Kind gehört dem Eltern-Datensatz und hat ohne ihn keine
    /// Bedeutung (ein Kapitel ohne Fach, ein Testergebnis ohne Testversuch). Die Gegenprobe ist die Suite:
    /// sie muss <b>unverändert</b> grün bleiben – jede Abweichung heißt, die ausgeschriebene Absicht war
    /// nicht die gelebte.
    /// </para>
    /// </summary>
    private static void ApplyExplicitCascades(ModelBuilder modelBuilder)
    {
        // Katalog: Fach ⇒ Kapitel ⇒ Übung. Löscht ein Fach, fällt der ganze Ast (der Restrict-Guard auf
        // PlanPosition→Exercise fängt vorher ab, was noch in einem Lehrplan steckt).
        modelBuilder.Entity<Chapter>()
            .HasOne(c => c.Subject).WithMany(s => s.Chapters).HasForeignKey(c => c.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Exercise>()
            .HasOne(x => x.Chapter).WithMany(c => c.Exercises).HasForeignKey(x => x.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Alles, was am Kind hängt und ohne es sinnlos ist: Lehrpläne, das Kassenbuch, die Lernziele.
        modelBuilder.Entity<StudyPlan>()
            .HasOne(p => p.Child).WithMany().HasForeignKey(p => p.ChildId)
            .OnDelete(DeleteBehavior.Cascade);
        // Die Gegen-Navigation MUSS benannt werden, wo sie existiert: `WithMany()` ohne sie lässt EF die
        // vorhandene, per Konvention gefundene Beziehung nicht wiedererkennen und legt eine ZWEITE an –
        // hier wuchs so eine Spalte `ChildId1` nach. Der Wächter G2 hat genau das gefangen.
        modelBuilder.Entity<ChildPointsEntry>()
            .HasOne(p => p.Child).WithMany(c => c.PointsEntries).HasForeignKey(p => p.ChildId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LearnGoal>()
            .HasOne(g => g.Child).WithMany().HasForeignKey(g => g.ChildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ausspiel-Historie: Sitzung/Test gehören dem Plan, ihre Einzelantworten der Sitzung bzw. dem
        // Versuch. Der Positions-Verweis daneben ist bewusst SetNull (kein zweiter Cascade-Pfad in SQLite).
        modelBuilder.Entity<PracticeSession>()
            .HasOne(s => s.StudyPlan).WithMany().HasForeignKey(s => s.StudyPlanId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TestAttempt>()
            .HasOne(t => t.StudyPlan).WithMany().HasForeignKey(t => t.StudyPlanId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ReviewEvent>()
            .HasOne(r => r.PracticeSession).WithMany(s => s.Reviews).HasForeignKey(r => r.PracticeSessionId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TestItemResult>()
            .HasOne(r => r.TestAttempt).WithMany(t => t.Results).HasForeignKey(r => r.TestAttemptId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Enums, die in einer Ausnahmeliste dieser Klasse stehen, bleiben <c>int</c> – jeweils mit Grund.
    /// Der Schlüssel ist <c>Entity.Property</c>.
    /// </summary>
    private static readonly Dictionary<string, string> IntEnumsByDesign = new(StringComparer.Ordinal)
    {
        ["Child.AllowedContentRating"] =
            "Wird ORDNEND verglichen (asset.Rating <= child.AllowedContentRating) – als Text wäre der "
            + "Vergleich lexikografisch und die Altersfreigabe stillschweigend falsch.",
        ["MediaAsset.Rating"] =
            "Gegenstück zu Child.AllowedContentRating: dieselbe Ordnung, dieselbe Begründung.",
    };

    /// <summary>
    /// Ob <paramref name="entityDotProperty"/> (Form <c>Entity.Property</c>) bewusst als <c>int</c>
    /// gespeichert wird. Der Wächter <c>SchemaGuardTests</c> liest diese Liste, statt eine zweite zu
    /// führen – sonst wären Regel und Ausnahme an zwei Orten zu pflegen.
    /// </summary>
    public static bool IntEnumErlaubt(string entityDotProperty) =>
        IntEnumsByDesign.ContainsKey(entityDotProperty);

    /// <summary>
    /// <b>Eine Regel statt 32 Einzelfällen:</b> jedes persistierte Enum wird als <b>String</b> gespeichert.
    /// <para>
    /// Vorher waren 12 Enums per <c>HasConversion&lt;string&gt;()</c> konvertiert und ~20 implizit <c>int</c> –
    /// ohne erkennbare Regel, in <c>Remarks</c> sogar beides in derselben Tabelle (<c>AuthorRole</c> als Text
    /// neben <c>Status</c>/<c>Category</c> als Zahl). String ist die richtige Seite, weil der Vertrag nach
    /// außen ohnehin Strings spricht (<c>JsonStringEnumConverter</c>): damit entfällt die Übersetzungsstufe
    /// zwischen dem, was in der DB steht, und dem, was die API sagt – und der gespeicherte Wert wird
    /// unabhängig von der Mitglieder-Reihenfolge, was das Entfernen toter Enum-Werte erst gefahrlos macht.
    /// </para>
    /// <para>
    /// Zwei Arten von Ausnahmen: <see cref="IntEnumsByDesign"/> (ordnend verglichen) und <c>[Flags]</c>.
    /// Eine Flags-Kombination hat keinen Namen – <c>HasConversion&lt;string&gt;</c> erzeugte
    /// <c>"Gymnasium, Realschule"</c> und machte jede bitweise Mengenabfrage kaputt.
    /// </para>
    /// </summary>
    private static void ApplyEnumConvention(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                // Nullable entpacken: `GoalCadence?` ist so zu behandeln wie `GoalCadence`.
                var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (!type.IsEnum) continue;
                if (type.IsDefined(typeof(FlagsAttribute), inherit: false)) continue;
                if (IntEnumsByDesign.ContainsKey($"{entity.ClrType.Name}.{property.Name}")) continue;

                // Nur setzen, wo noch nichts steht: eine ausdrückliche Konfiguration weiter oben
                // (oder ein eigener Converter) gewinnt gegen die Konvention.
                if (property.GetValueConverter() is not null) continue;
                property.SetValueConverter(
                    typeof(EnumToStringConverter<>).MakeGenericType(type));
            }
        }
    }
}
