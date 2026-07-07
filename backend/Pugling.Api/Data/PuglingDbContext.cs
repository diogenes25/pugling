using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Models;

namespace Pugling.Api.Data;

public class PuglingDbContext(DbContextOptions<PuglingDbContext> options) : DbContext(options)
{
    // Zeitfenster mit Punkte-Multiplikator (Leitner-Wiederholungen, siehe PointsService).
    public DbSet<TimeSlotRule> TimeSlots => Set<TimeSlotRule>();

    // Admin-Bereich: Personen (Father -> Child) + Punkte
    public DbSet<Father> Fathers => Set<Father>();
    public DbSet<Child> Children => Set<Child>();
    public DbSet<ChildPointsEntry> ChildPoints => Set<ChildPointsEntry>();

    // Lern-Katalog: Subject -> Chapter -> Exercise (typisiert)
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ExerciseCategory> ExerciseCategories => Set<ExerciseCategory>();
    // Stabil identifizierte Items einer Vokabelübung (positionierte Referenz auf den Vokabel-Store).
    public DbSet<ExerciseItem> ExerciseItems => Set<ExerciseItem>();

    // Sprachlernen: atomarer Vokabel-Store + Lückentext-Store
    public DbSet<Vocabulary> Vocabulary => Set<Vocabulary>();
    public DbSet<ClozeText> ClozeTexts => Set<ClozeText>();
    // Kindneutrale Schlagworte für den Vokabel-Katalog (Kapitel/Klasse/Thema)
    public DbSet<VocabTag> VocabTags => Set<VocabTag>();
    public DbSet<VocabTagLink> VocabTagLinks => Set<VocabTagLink>();

    // Lehrplan (Container) + Positionen auf Katalog-Übungen, Fortschritt/Ziel-Belohnung je Position
    public DbSet<StudyPlan> StudyPlans => Set<StudyPlan>();
    public DbSet<PlanPosition> PlanPositions => Set<PlanPosition>();
    public DbSet<PositionItemProgress> PositionItemProgress => Set<PositionItemProgress>();
    public DbSet<PositionGoalReward> PositionGoalRewards => Set<PositionGoalReward>();
    public DbSet<PracticeSession> PracticeSessions => Set<PracticeSession>();
    public DbSet<ReviewEvent> ReviewEvents => Set<ReviewEvent>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    public DbSet<TestItemResult> TestItemResults => Set<TestItemResult>();
    // Plan-übergreifender Lernstand je (Kind, Item) + Antwort-Historie (stabile ItemId, denormalisierte VocabularyId).
    public DbSet<ItemProgress> ItemProgress => Set<ItemProgress>();
    public DbSet<ItemReviewEvent> ItemReviewEvents => Set<ItemReviewEvent>();

    // Stundenplan-Steuerung
    public DbSet<TimetableEntry> Timetable => Set<TimetableEntry>();

    // Gamification: Missionen (zeitgebundene Ziele) + Auszeichnungen (Badges) je Kind, mit Vergabe-Log
    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<MissionAward> MissionAwards => Set<MissionAward>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<AchievementAward> AchievementAwards => Set<AchievementAward>();

    // Einlösbare Prämien (reale Belohnungen wie Fernseh-/Spielzeit) + Einlöse-Anfragen mit Vater-Freigabe
    public DbSet<Reward> Rewards => Set<Reward>();
    public DbSet<RewardRedemption> RewardRedemptions => Set<RewardRedemption>();

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

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Freigeschaltete Skins des Kindes als JSON-Liste (Neuzuweisung im Controller, kein In-Place-Mutieren).
        modelBuilder.Entity<Child>(e =>
        {
            e.Property(c => c.OwnedSkins).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<string>>(s, JsonOptions) ?? new())
                .Metadata.SetValueComparer(JsonValueComparer.For<List<string>>());
            // Concurrency-Token: schützt Skin-Kauf/Ausrüsten vor parallelen Doppelbuchungen.
            e.Property(c => c.ConcurrencyStamp).IsConcurrencyToken();
        });

        modelBuilder.Entity<Vocabulary>(e =>
        {
            e.HasIndex(v => v.Key).IsUnique();

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
            .HasForeignKey(e => e.AuthorFatherId)
            .OnDelete(DeleteBehavior.SetNull);

        // Vokabel-Item: gehört einer Übung (Cascade – verschwindet mit ihr) und referenziert eine Store-Vokabel.
        // Die Vokabel darf nicht gelöscht werden, solange ein Item sie nutzt (Restrict, wie beim Übungs-Store-Bezug);
        // der Controller fängt das vorher als sauberen 409 ab. OrderIndex ist reiner Sortierschlüssel (bewusst NICHT
        // unique): der Lehrplan-Motor leitet den stabilen Item-Index aus der Listenposition (sortiert nach OrderIndex,
        // Id) ab, sodass Umsortieren ohne transiente Unique-Kollisionen (SQLite prüft je Statement) auskommt.
        modelBuilder.Entity<ExerciseItem>(e =>
        {
            e.HasIndex(i => new { i.ExerciseId, i.OrderIndex });
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
        modelBuilder.Entity<PositionGoalReward>(e =>
        {
            e.HasIndex(r => new { r.PlanPositionId, r.PeriodKey }).IsUnique();
            e.HasOne(r => r.PlanPosition).WithMany().HasForeignKey(r => r.PlanPositionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Übungssitzung/Test optional an eine Position gekoppelt (neues Modell). Beide hängen bereits über
        // StudyPlanId am Plan (Cascade); der Positions-Verweis nutzt daher SetNull, um in SQLite keine
        // zweiten Cascade-Pfade (Plan → Position → Session/Test) neben Plan → Session/Test zu erzeugen.
        // Die eingefrorene Ausspiel-Reihenfolge (Cursor-Modell) liegt als JSON-Spalte (Neuzuweisung im Controller).
        modelBuilder.Entity<PracticeSession>(e =>
        {
            e.HasOne(s => s.PlanPosition).WithMany().HasForeignKey(s => s.PlanPositionId)
                .OnDelete(DeleteBehavior.SetNull);
            e.Property(s => s.Order).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<int>>(s, JsonOptions) ?? new())
                .Metadata.SetValueComparer(JsonValueComparer.For<List<int>>());
        });
        modelBuilder.Entity<TestAttempt>(e =>
        {
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
        modelBuilder.Entity<Mission>()
            .HasOne(m => m.Child).WithMany().HasForeignKey(m => m.ChildId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MissionAward>(e =>
        {
            e.HasIndex(a => new { a.MissionId, a.PeriodKey }).IsUnique();
            e.HasOne(a => a.Mission).WithMany().HasForeignKey(a => a.MissionId).OnDelete(DeleteBehavior.Cascade);
        });

        // Auszeichnung gehört einem Kind (Cascade); wird genau einmal verliehen.
        modelBuilder.Entity<Achievement>()
            .HasOne(a => a.Child).WithMany().HasForeignKey(a => a.ChildId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AchievementAward>(e =>
        {
            e.HasIndex(a => a.AchievementId).IsUnique();
            e.HasOne(a => a.Achievement).WithMany().HasForeignKey(a => a.AchievementId).OnDelete(DeleteBehavior.Cascade);
        });

        // Prämie gehört einem Kind (Cascade). Einlöse-Anfrage gehört einem Kind (Cascade); die Prämie-
        // Referenz wird beim Löschen der Prämie auf null gesetzt, damit die Einlöse-Historie erhalten bleibt.
        modelBuilder.Entity<Reward>(e =>
        {
            e.HasOne(r => r.Child).WithMany().HasForeignKey(r => r.ChildId).OnDelete(DeleteBehavior.Cascade);
            // Optionaler Plan-/Übungs-Kontext: SetNull, damit Löschen von Plan/Übung das Angebot nicht bricht.
            e.HasOne(r => r.StudyPlan).WithMany().HasForeignKey(r => r.StudyPlanId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(r => r.Exercise).WithMany().HasForeignKey(r => r.ExerciseId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<RewardRedemption>(e =>
        {
            e.HasOne(r => r.Child).WithMany().HasForeignKey(r => r.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Reward).WithMany().HasForeignKey(r => r.RewardId).OnDelete(DeleteBehavior.SetNull);
        });

        // Shop-Artikel: familieninterne Artikelnummer eindeutig; gehört zum Vater (Cascade).
        // Angebote (ShopListing): gehören zum Artikel (Cascade).
        // Käufe (ShopPurchase): gehören zum Kind (Cascade); Angebots-Referenz wird auf null gesetzt,
        //   wenn das Angebot gelöscht wird, damit die Kaufhistorie erhalten bleibt.
        // Inventar (ChildInventory): Kind-Artikel-Kombination eindeutig; gehören zum Kind (Cascade);
        //   Artikel-Referenz Cascade (Inventar verschwindet mit Artikel).
        // Aktivierungsanfragen: gehören zum Kind (Cascade); Artikel-Referenz SetNull für Histor stabil.
        modelBuilder.Entity<ShopArticle>(e =>
        {
            e.HasIndex(a => new { a.FatherId, a.ArticleNumber }).IsUnique();
            e.HasOne(a => a.Father).WithMany().HasForeignKey(a => a.FatherId).OnDelete(DeleteBehavior.Cascade);
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
            e.HasIndex(i => new { i.ChildId, i.ShopArticleId }).IsUnique();
            e.Property(i => i.ConcurrencyStamp).IsConcurrencyToken();
            e.HasOne(i => i.Child).WithMany().HasForeignKey(i => i.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.ShopArticle).WithMany().HasForeignKey(i => i.ShopArticleId).OnDelete(DeleteBehavior.Cascade);
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
    }
}
