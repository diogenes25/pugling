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

    // Sprachlernen: atomarer Vokabel-Store + Lückentext-Store
    public DbSet<Vocabulary> Vocabulary => Set<Vocabulary>();
    public DbSet<ClozeText> ClozeTexts => Set<ClozeText>();

    // Vokabeltraining: Lehrplan, Übungssitzungen, Tests, Belohnungen
    public DbSet<StudyPlan> StudyPlans => Set<StudyPlan>();
    public DbSet<StudyPlanItem> StudyPlanItems => Set<StudyPlanItem>();
    public DbSet<PracticeSession> PracticeSessions => Set<PracticeSession>();
    public DbSet<ReviewEvent> ReviewEvents => Set<ReviewEvent>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    public DbSet<TestItemResult> TestItemResults => Set<TestItemResult>();
    public DbSet<StudyDayReward> StudyDayRewards => Set<StudyDayReward>();

    // Stundenplan-Steuerung + Inhalts-Bewertungen
    public DbSet<TimetableEntry> Timetable => Set<TimetableEntry>();
    public DbSet<ContentRating> ContentRatings => Set<ContentRating>();

    // Gamification: Missionen (zeitgebundene Ziele) + Auszeichnungen (Badges) je Kind, mit Vergabe-Log
    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<MissionAward> MissionAwards => Set<MissionAward>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<AchievementAward> AchievementAwards => Set<AchievementAward>();

    // Tagging + Klassenarbeiten
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ExerciseTag> ExerciseTags => Set<ExerciseTag>();
    public DbSet<Klassenarbeit> Klassenarbeiten => Set<Klassenarbeit>();
    public DbSet<KlassenarbeitExercise> KlassenarbeitExercises => Set<KlassenarbeitExercise>();
    public DbSet<KlassenarbeitTag> KlassenarbeitTags => Set<KlassenarbeitTag>();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Vocabulary>(e =>
        {
            e.HasIndex(v => v.Key).IsUnique();

            // noun/verb als JSON-Spalten (null bleibt DB-NULL, Converter läuft nur für Werte).
            e.Property(v => v.Noun).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<NounInfo>(s, JsonOptions));
            e.Property(v => v.Verb).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<VerbInfo>(s, JsonOptions));

            // Selbst-Referenz auf die Grundform; Löschen einer referenzierten Grundform verhindern.
            e.HasOne(v => v.BaseForm)
                .WithMany()
                .HasForeignKey(v => v.BaseFormId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Bonus-Vorschlag der Übung als JSON-Spalte (null bleibt DB-NULL; Converter läuft nur für Werte).
        modelBuilder.Entity<Exercise>()
            .Property(e => e.SuggestedBonus).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<SuggestedBonus>(s, JsonOptions));

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

        // Lückentext-Store: eindeutiger Key + Gaps/WordBank als JSON-Spalten.
        modelBuilder.Entity<ClozeText>(e =>
        {
            e.HasIndex(c => c.Key).IsUnique();
            e.Property(c => c.Gaps).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<Gap>>(s, JsonOptions) ?? new());
            e.Property(c => c.WordBank).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<string>>(s, JsonOptions));
        });

        // Stufen-Fahrplan als JSON-Spalte am Lehrplan.
        modelBuilder.Entity<StudyPlan>()
            .Property(p => p.StageSchedule).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<StageStep>>(s, JsonOptions));

        // Leitner-Intervalle je Box als JSON-Spalte (null = Standard-Intervalle).
        modelBuilder.Entity<StudyPlan>()
            .Property(p => p.BoxIntervalDays).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<int>>(s, JsonOptions));

        // Punkte nur einmal je (Plan, Tag, Art) vergeben.
        modelBuilder.Entity<StudyDayReward>()
            .HasIndex(r => new { r.StudyPlanId, r.Day, r.Kind }).IsUnique();

        // Lehrplan-Items verweisen auf Vokabel ODER Lückentext; Inhalte dürfen nicht gelöscht
        // werden, solange sie in einem Lehrplan stecken.
        modelBuilder.Entity<StudyPlanItem>(e =>
        {
            e.HasOne(i => i.Vocabulary).WithMany().HasForeignKey(i => i.VocabularyId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.ClozeText).WithMany().HasForeignKey(i => i.ClozeTextId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Lehrplan optional an ein Katalog-Fach gekoppelt (für Stundenplan-Steuerung).
        modelBuilder.Entity<StudyPlan>()
            .HasOne(p => p.Subject).WithMany().HasForeignKey(p => p.SubjectId)
            .OnDelete(DeleteBehavior.SetNull);

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
