using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pugling.Api.Models;

namespace Pugling.Api.Data;

public class PuglingDbContext(DbContextOptions<PuglingDbContext> options) : DbContext(options)
{
    // Identity: a login account with one or more roles (creator/supervisor/student), decoupled from Adult/Child.
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountProfile> AccountProfiles => Set<AccountProfile>();

    // Admin area: people (supervisor >-< student through SupervisorLink) + points
    public DbSet<Adult> Adults => Set<Adult>();
    public DbSet<Child> Children => Set<Child>();
    // Textbooks used by the child (exercise-independent profile, the basis for a later study plan generator).
    public DbSet<Textbook> Textbooks => Set<Textbook>();
    public DbSet<SupervisorLink> SupervisorLinks => Set<SupervisorLink>();
    public DbSet<ChildPointsEntry> ChildPointsEntries => Set<ChildPointsEntry>();

    // The teaching side of the catalog: textbook series -> unit, plus the creator profiles ("subject teachers").
    public DbSet<Publisher> Publishers => Set<Publisher>();
    public DbSet<TextbookSeries> TextbookSeries => Set<TextbookSeries>();
    public DbSet<SeriesUnit> SeriesUnits => Set<SeriesUnit>();
    public DbSet<CreatorProfile> CreatorProfiles => Set<CreatorProfile>();

    // Learn catalog: Subject holds ExerciseCategory; Exercise hangs off TextbookSeries -> SeriesUnit (typed)
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ExerciseCategory> ExerciseCategories => Set<ExerciseCategory>();
    // RWX rights of individual creators on an exercise (owner/write/execute).
    public DbSet<ExerciseGrant> ExerciseGrants => Set<ExerciseGrant>();
    // Stably identified items of a vocabulary exercise (a positioned reference into the vocabulary store).
    public DbSet<ExerciseItem> ExerciseItems => Set<ExerciseItem>();

    // Language learning: the atomic vocabulary store + the cloze store
    public DbSet<Vocabulary> Vocabularies => Set<Vocabulary>();
    public DbSet<ClozeText> ClozeTexts => Set<ClozeText>();
    // Child-neutral keywords for the vocabulary catalog (chapter/grade/topic)
    public DbSet<VocabTag> VocabTags => Set<VocabTag>();
    public DbSet<VocabTagLink> VocabTagLinks => Set<VocabTagLink>();

    // Media store: an asset is one rendition of a motif, a variant is that same rendition in one
    // resolution/format. Tagged with the same taxonomy the child interests use.
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<MediaVariant> MediaVariants => Set<MediaVariant>();
    public DbSet<MediaTagLink> MediaTagLinks => Set<MediaTagLink>();
    // Image ⇢ carrier assignment (vocabulary / exercise item / exercise) - n:m in both directions.
    public DbSet<MediaLink> MediaLinks => Set<MediaLink>();
    // Frozen image choice per (child, carrier) - image constancy is the retention effect when learning vocabulary.
    public DbSet<ChildMediaPick> ChildMediaPicks => Set<ChildMediaPick>();

    // Shared interest/style taxonomy (child ⇢ tag ⇠ image) - the basis of the individualized image selection.
    public DbSet<InterestTag> InterestTags => Set<InterestTag>();
    public DbSet<ChildInterest> ChildInterests => Set<ChildInterest>();

    // Study plan (container) + positions on catalog exercises, progress/goal reward per position
    public DbSet<StudyPlan> StudyPlans => Set<StudyPlan>();
    public DbSet<PlanPosition> PlanPositions => Set<PlanPosition>();
    public DbSet<PositionItemProgress> PositionItemProgress => Set<PositionItemProgress>();
    public DbSet<PositionGoalReward> PositionGoalRewards => Set<PositionGoalReward>();
    public DbSet<PositionGoalPenalty> PositionGoalPenalties => Set<PositionGoalPenalty>();
    public DbSet<PracticeSession> PracticeSessions => Set<PracticeSession>();
    public DbSet<ReviewEvent> ReviewEvents => Set<ReviewEvent>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    // No DbSet for TestItemResult: the table exists (through the relationship below) but is reached
    // exclusively through TestAttempt.Results - a set of its own would be a second entrance that nobody uses
    // and that invites bypassing the attempt context.
    // Cross-plan learning state per (child, item) + answer history (stable ItemId, denormalized VocabularyId).
    public DbSet<ItemProgress> ItemProgress => Set<ItemProgress>();
    public DbSet<ItemReviewEvent> ItemReviewEvents => Set<ItemReviewEvent>();
    // "Big goals" (the OKR core): an objective as a container over measurable key results + an idempotent reward log.
    public DbSet<Objective> Objectives => Set<Objective>();
    public DbSet<KeyResult> KeyResults => Set<KeyResult>();
    public DbSet<ObjectiveReward> ObjectiveRewards => Set<ObjectiveReward>();

    // Timetable control
    public DbSet<TimetableEntry> TimetableEntries => Set<TimetableEntry>();

    // Gamification: missions (time-bound goals) + awards (badges) per child, with an award log
    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<MissionAward> MissionAwards => Set<MissionAward>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<AchievementAward> AchievementAwards => Set<AchievementAward>();

    /// <summary>Daily reward box: one claim per (child, day) - the positive counterpart to <see cref="PositionGoalPenalty"/>.</summary>
    public DbSet<DailyBoxClaim> DailyBoxClaims => Set<DailyBoxClaim>();

    // Family shop: the supervisor's catalog (articles + listings), the child's aggregated inventory,
    // purchase history and activation requests
    public DbSet<ShopArticle> ShopArticles => Set<ShopArticle>();
    public DbSet<ShopListing> ShopListings => Set<ShopListing>();
    public DbSet<ShopPurchase> ShopPurchases => Set<ShopPurchase>();
    public DbSet<ChildInventory> ChildInventories => Set<ChildInventory>();
    public DbSet<ActivationRequest> ActivationRequests => Set<ActivationRequest>();

    // Tagging + class tests
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ExerciseTag> ExerciseTags => Set<ExerciseTag>();
    public DbSet<VocabularyTag> VocabularyTags => Set<VocabularyTag>();
    public DbSet<Klassenarbeit> Klassenarbeiten => Set<Klassenarbeit>();
    public DbSet<KlassenarbeitExercise> KlassenarbeitExercises => Set<KlassenarbeitExercise>();
    public DbSet<KlassenarbeitTag> KlassenarbeitTags => Set<KlassenarbeitTag>();

    // Remarks captured while testing (entered in the UI widget, answered by Claude Code)
    public DbSet<Remark> Remarks => Set<Remark>();
    public DbSet<RemarkComment> RemarkComments => Set<RemarkComment>();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Identity: Account -> AccountProfile(role -> Adult/Child). The role as a string (readable/stable).
        // Filtered unique indexes prevent duplicate profiles on a (repeated) backfill.
        modelBuilder.Entity<AccountProfile>(e =>
        {
            // Exactly one of AdultId/ChildId - so far only claimed in a comment on the entity. Both set would
            // be one login with two identities behind it, neither a role pointing at nothing (AuthAccess would
            // then check silently into the void). Same construction as MediaLink/ChildMediaPick.
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
            // An account carries every role at most once. The two indexes above only prevent the same *profile*
            // from hanging in one role twice - not an account getting two creator profiles on different adults.
            // That would be exactly a second identity behind one login.
            e.HasIndex(p => new { p.AccountId, p.Role }).IsUnique();
        });
        modelBuilder.Entity<Account>()
            .HasIndex(a => a.Email).IsUnique().HasFilter("[Email] IS NOT NULL");

        // Issuer attribution of the economy (a snapshot): the filter "my cases only" per (child, supervisor).
        modelBuilder.Entity<ShopPurchase>().HasIndex(p => new { p.ChildId, p.SupervisorId });
        modelBuilder.Entity<ActivationRequest>().HasIndex(r => new { r.ChildId, r.SupervisorId });
        modelBuilder.Entity<ChildPointsEntry>(e =>
        {
            // Wallet sums and ledger lists: filter by child/kind plus paging "newest first".
            e.HasIndex(p => new { p.ChildId, p.Kind });
            e.HasIndex(p => new { p.ChildId, p.CreatedAt, p.Id });
        });

        // Supervision supervisor >-< student. A student can have several supervisors; a pair is unique.
        // A leaf on two independent roots (like ItemProgress) - both FKs cascade, no SQLite diamond.
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

        // The child's unlocked skins as a JSON list (reassign in the controller, no in-place mutation).
        modelBuilder.Entity<Child>(e =>
        {
            e.Property(c => c.OwnedSkins).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<string>>(s, JsonOptions) ?? new())
                .Metadata.SetValueComparer(JsonValueComparer.For<List<string>>());
            // The child's interests as a JSON list too (the same ValueComparer pitfall as OwnedSkins).
            e.Property(c => c.Interests).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<string>>(s, JsonOptions) ?? new())
                .Metadata.SetValueComparer(JsonValueComparer.For<List<string>>());
            // Gender as a string (readable/stable, like SupervisorLink.Relation).
            e.Property(c => c.Gender).HasConversion<string>();
            // The suitability bound deliberately as an int (NOT as a string like the other enums): the media
            // selector compares it by order (rating <= allowed). As a string the comparison would run
            // alphabetically ("Everyone" < "Mature" < "Teen") and simply be wrong.
            e.Property(c => c.AllowedContentRating).HasConversion<int>();
            // Concurrency token: protects skin purchase/equip against parallel double bookings.
            e.Property(c => c.ConcurrencyStamp).IsConcurrencyToken();
        });

        // Textbook: belongs to a child (cascade - it disappears with the child). The optional catalog link to a
        // subject uses SetNull so that deleting a subject does not tear the book assignment with it (it only clears the FK).
        modelBuilder.Entity<Textbook>(e =>
        {
            e.HasIndex(t => t.ChildId);
            e.HasOne(t => t.Child).WithMany(c => c.Textbooks).HasForeignKey(t => t.ChildId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Subject).WithMany().HasForeignKey(t => t.SubjectId)
                .OnDelete(DeleteBehavior.SetNull);
            // Series and unit are references into the shared catalog: a deleted series only clears the
            // assignment (SetNull) - the child's book remains with its title/chapter as free text.
            e.HasOne(t => t.Series).WithMany().HasForeignKey(t => t.SeriesId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.CurrentUnit).WithMany().HasForeignKey(t => t.CurrentUnitId)
                .OnDelete(DeleteBehavior.SetNull);
            // The hot path of the profile matching: "which series does this child use?"
            e.HasIndex(t => t.SeriesId);
        });

        // Publisher: a globally unique slug, pattern InterestTag - no owner, naming a publisher is not authorship.
        modelBuilder.Entity<Publisher>(e =>
        {
            e.HasIndex(p => p.Slug).IsUnique();

            // NOCASE acts on EQUALITY, not on the catalog search - `instr()`, which EF maps `Contains` to,
            // ignores collations entirely (measured in B-128, see Services/Shared/SearchPattern.cs). The
            // search is handled by LIKE; this collation is here so that a duplicate check on the publisher
            // name would treat "Klett" and "KLETT" as one, the way it does for TextbookSeries.Name below.
            // Today nothing compares Publisher.Name for equality, so this is a deliberate reserve rather
            // than a load-bearing line - it becomes load-bearing with B-136 (renaming a publisher can still
            // produce two identical display names). Do not read it as a fix for the search.
            e.Property(p => p.Name).UseCollation("NOCASE");
        });

        // Textbook series: a globally unique slug (child-neutral like the vocabulary store, pattern InterestTag).
        // The owner is only an edit/delete right - a deleted adult clears the FK, the series stays usable.
        modelBuilder.Entity<TextbookSeries>(e =>
        {
            e.HasIndex(s => s.Slug).IsUnique();

            // Deliberate and load-bearing: the duplicate check on create and rename compares display names
            // (B-133), and through this collation "Access" and "ACCESS" count as the same one. It does
            // NOT make the catalog search case-insensitive - that is LIKE's job, see SearchPattern.cs.
            // Uniqueness of the name is enforced in the controller only, with no unique index behind it:
            // two simultaneous POSTs could both pass the pre-check. Accepted for a catalog two adults
            // edit at a kitchen table; an index would also have to answer what to do with rows that
            // already collide.
            e.Property(s => s.Name).UseCollation("NOCASE");
            e.HasOne(s => s.Publisher).WithMany().HasForeignKey(s => s.PublisherId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(s => s.Subject).WithMany().HasForeignKey(s => s.SubjectId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(s => s.Owner).WithMany().HasForeignKey(s => s.OwnerAdultId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Unit: belongs to the series (cascade). The index serves the only ordering units are ever read in -
        // volume, then order within the volume. Topics is a JSON list (ValueComparer as with Child/CreatorProfile
        // - a missing one silently drops in-place edits on SaveChanges).
        modelBuilder.Entity<SeriesUnit>(e =>
        {
            e.HasIndex(u => new { u.SeriesId, u.Grade, u.OrderIndex });
            e.HasOne(u => u.Series).WithMany(s => s.Units).HasForeignKey(u => u.SeriesId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(u => u.BookType).HasConversion<string>();
            e.Property(u => u.Topics).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<string>>(s, JsonOptions) ?? new())
                .Metadata.SetValueComparer(JsonValueComparer.For<List<string>>());
        });

        // Creator profile: a unique name per owner; subject and series are the two axes the matching filters
        // on. The preferred exercise types sit in a JSON list (ValueComparer as with Child).
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

            // Word/translation are the hottest read path of the catalog (the duplicate lookup on create,
            // free-text search) and had no index. An index alone would not have helped, though: the search
            // compared `LOWER(Word)`, and no column index applies over an expression. Only the NOCASE collation
            // makes the comparison itself case-insensitive - then the `ToLower()` in the query can go and the
            // index is used.
            // A consequence you need to know: `Word == "march"` now also finds "March". For a vocabulary store
            // that is wanted (capitalization is not a separate word), and uniqueness hangs on the `Key` anyway,
            // not on the word.
            e.Property(v => v.Word).UseCollation("NOCASE");
            e.Property(v => v.Translation).UseCollation("NOCASE");
            e.HasIndex(v => v.Word);
            e.HasIndex(v => v.Translation);

            // noun/verb as JSON columns (null stays DB NULL, the converter runs for values only).
            e.Property(v => v.Noun).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<NounInfo>(s, JsonOptions))
                .Metadata.SetValueComparer(JsonValueComparer.For<NounInfo?>());
            e.Property(v => v.Verb).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<VerbInfo>(s, JsonOptions))
                .Metadata.SetValueComparer(JsonValueComparer.For<VerbInfo?>());
            // Equally valid translations as a JSON list (null stays DB NULL - "none declared", the state of
            // every pre-existing row).
            e.Property(v => v.TranslationAlternatives).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<string>>(s, JsonOptions))
                .Metadata.SetValueComparer(JsonValueComparer.For<List<string>?>());

            // Self-reference to the base form; prevent deleting a referenced base form.
            e.HasOne(v => v.BaseForm)
                .WithMany()
                .HasForeignKey(v => v.BaseFormId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Vocabulary tag: a globally unique name (child-neutral, like the vocabulary store).
        modelBuilder.Entity<VocabTag>()
            .HasIndex(t => t.Name).IsUnique();

        // Vocabulary <-> tag: every entry at most once per tag; links disappear with the tag or the entry.
        modelBuilder.Entity<VocabTagLink>(e =>
        {
            e.HasIndex(x => new { x.VocabTagId, x.VocabularyId }).IsUnique();
            e.HasOne(x => x.VocabTag).WithMany(t => t.Links).HasForeignKey(x => x.VocabTagId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Vocabulary).WithMany(v => v.TagLinks).HasForeignKey(x => x.VocabularyId).OnDelete(DeleteBehavior.Cascade);
        });

        // Shared interest/style taxonomy: a globally unique slug (child-neutral like the vocabulary store).
        // The facet stays a readable string - it is only compared, never ordered.
        modelBuilder.Entity<InterestTag>(e =>
        {
            e.HasIndex(t => t.Slug).IsUnique();
            e.Property(t => t.Facet).HasConversion<string>();
            e.Property(t => t.Synonyms).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<string>>(s, JsonOptions) ?? new())
                .Metadata.SetValueComparer(JsonValueComparer.For<List<string>>());
        });

        // Child <-> interest: at most one weight per (child, tag). A leaf on two independent roots
        // (Child, InterestTag) - both cascade, no SQLite diamond (the SupervisorLink pattern).
        modelBuilder.Entity<ChildInterest>(e =>
        {
            e.HasIndex(x => new { x.ChildId, x.InterestTagId }).IsUnique();
            e.HasOne(x => x.Child).WithMany(c => c.InterestTags).HasForeignKey(x => x.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.InterestTag).WithMany(t => t.ChildInterests).HasForeignKey(x => x.InterestTagId).OnDelete(DeleteBehavior.Cascade);
        });

        // Media asset: a unique key (like Vocabulary). Unlike kind/origin, the rating sits in the DB as an int,
        // because the selector filters on it by order (see the comment on Child).
        modelBuilder.Entity<MediaAsset>(e =>
        {
            e.HasIndex(a => a.Key).IsUnique();
            // The selector always filters kind + suitability first, before it sorts by interests.
            e.HasIndex(a => new { a.Kind, a.Rating });
            e.Property(a => a.Kind).HasConversion<string>();
            e.Property(a => a.Origin).HasConversion<string>();
            e.Property(a => a.Rating).HasConversion<int>();
        });

        // Variant: belongs to the asset (cascade). At most one file per (purpose, format) per asset -
        // otherwise delivery would have to choose arbitrarily between equivalent candidates.
        modelBuilder.Entity<MediaVariant>(e =>
        {
            e.HasIndex(v => new { v.MediaAssetId, v.Purpose, v.Format }).IsUnique();
            e.Property(v => v.Purpose).HasConversion<string>();
            e.HasOne(v => v.MediaAsset).WithMany(a => a.Variants).HasForeignKey(v => v.MediaAssetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Image <-> tag: every asset at most once per tag. The additional index on the tag serves the hot
        // direction of the later selection ("which assets carry this interest?").
        modelBuilder.Entity<MediaTagLink>(e =>
        {
            e.HasIndex(x => new { x.MediaAssetId, x.InterestTagId }).IsUnique();
            e.HasIndex(x => x.InterestTagId);
            e.HasOne(x => x.MediaAsset).WithMany(a => a.TagLinks).HasForeignKey(x => x.MediaAssetId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.InterestTag).WithMany(t => t.MediaLinks).HasForeignKey(x => x.InterestTagId).OnDelete(DeleteBehavior.Cascade);
        });

        // Image ⇢ carrier. Exactly one of the three FKs is set - as a check constraint in the DB, not only in
        // the controller: a row without a carrier would be invisible, one with two ambiguously resolvable.
        // One filtered unique index per carrier (the same image not twice on the same object) - a shared index
        // over all three columns would not hold, because SQLite treats NULLs as distinct. All FKs cascade: a
        // deleted image/object leaves no assignment behind. No diamond despite Exercise → ExerciseItem →
        // MediaLink, because by constraint a row always hangs on just ONE carrier (the other columns are NULL
        // and are never deleted along).
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
            // The opposite direction: "which links does this asset have?" (cleaning up on delete).
            // The three filtered uniques above start with MediaAssetId but cannot serve this query: without a
            // restriction to one carrier their filter is not implied.
            e.HasIndex(l => l.MediaAssetId);
            // The hot direction of the selection: "which images hang on this entry / this item?"
            e.HasIndex(l => l.VocabularyId);
            e.HasIndex(l => l.ExerciseItemId);
            e.HasIndex(l => l.ExerciseId);

            e.HasOne(l => l.MediaAsset).WithMany(a => a.Links).HasForeignKey(l => l.MediaAssetId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Vocabulary).WithMany().HasForeignKey(l => l.VocabularyId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.ExerciseItem).WithMany().HasForeignKey(l => l.ExerciseItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Exercise).WithMany().HasForeignKey(l => l.ExerciseId).OnDelete(DeleteBehavior.Cascade);
        });

        // Frozen image choice. One row per candidate (not per carrier): the active choice is the row without
        // Rejected, rejected ones remain as an exclusion. Exactly one carrier per row - same rationale and same
        // construction as MediaLink (check constraint + filtered unique indexes).
        modelBuilder.Entity<ChildMediaPick>(e =>
        {
            e.ToTable(t => t.HasCheckConstraint("CK_ChildMediaPick_SingleCarrier",
                """
                (CASE WHEN "VocabularyId" IS NULL THEN 0 ELSE 1 END
                 + CASE WHEN "ExerciseItemId" IS NULL THEN 0 ELSE 1 END) = 1
                """));

            e.HasIndex(p => new { p.ChildId, p.VocabularyId, p.MediaAssetId }).IsUnique().HasFilter("[VocabularyId] IS NOT NULL");
            e.HasIndex(p => new { p.ChildId, p.ExerciseItemId, p.MediaAssetId }).IsUnique().HasFilter("[ExerciseItemId] IS NOT NULL");
            // No additional index on (ChildId, VocabularyId)/(ChildId, ExerciseItemId): measured with EXPLAIN
            // QUERY PLAN, SQLite picks the filtered uniques above for "what is chosen for this child on this
            // carrier?" - an equality on the carrier column implies their `IS NOT NULL` filter. The former
            // extra indexes were never used.

            e.HasOne(p => p.Child).WithMany().HasForeignKey(p => p.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Vocabulary).WithMany().HasForeignKey(p => p.VocabularyId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.ExerciseItem).WithMany().HasForeignKey(p => p.ExerciseItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.MediaAsset).WithMany().HasForeignKey(p => p.MediaAssetId).OnDelete(DeleteBehavior.Cascade);
        });

        ApplyExplicitCascades(modelBuilder);

        // At least an index on the subject name: `Subjects` had none besides the primary key, although every
        // catalog view searches and sorts by it.
        modelBuilder.Entity<Subject>().HasIndex(s => s.Name);
        // DELIBERATELY NOT unique. It would be the obvious thing - "English" twice is ugly. But `Subject`
        // carries no owner: a global unique would make the catalog's most important namespace
        // first-come-first-served across all creators, and every further teacher would have to hang their
        // chapters on a subject they do not own. That is a product decision about catalog ownership (and a
        // contract break: POST /subjects would then answer 409), not the closing of a structural gap. First
        // decide who owns a subject - then make it unique.

        // The adult's e-mail is a login attribute and was freely duplicable, while the filtered unique index
        // hung on the account only. Filtered, because the address stays optional (an adult supervising a child
        // needs none).
        modelBuilder.Entity<Adult>().HasIndex(a => a.Email).IsUnique().HasFilter("[Email] IS NOT NULL");

        // The exercise's bonus suggestion as a JSON column (null stays DB NULL; the converter runs for values only).
        modelBuilder.Entity<Exercise>()
            .Property(e => e.SuggestedBonus).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<SuggestedBonus>(s, JsonOptions))
                .Metadata.SetValueComparer(JsonValueComparer.For<SuggestedBonus?>());

        // Subject-dependent exercise categories: the name is unique per subject, deleting the subject removes them.
        modelBuilder.Entity<ExerciseCategory>(e =>
        {
            e.HasIndex(c => new { c.SubjectId, c.Name }).IsUnique();
            e.HasOne(c => c.Subject)
                .WithMany(s => s.Categories)
                .HasForeignKey(c => c.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Exercise → category (optional): deleting a category only sets the FK to null, it does NOT delete the exercise.
        modelBuilder.Entity<Exercise>()
            .HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Exercise → author (optional): the catalog is global; the author only protects the edit/delete right.
        // Deleting the author sets the FK to null (the exercise stays usable for other people's plans), it does NOT delete it.
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

        // There used to be no index on the author; the new grant joins and the `mineOnly` filter benefit from it.
        modelBuilder.Entity<Exercise>().HasIndex(e => e.AuthorAdultId);
        // The exercise type is the most frequent catalog filter (type lists, ExerciseControllerBase).
        modelBuilder.Entity<Exercise>().HasIndex(e => e.Type);

        // RWX grant: a creator's right on an exercise. A leaf on two independent roots (Exercise, Adult) -
        // both FKs cascade, no SQLite diamond (the SupervisorLink pattern). Pair+right unique (idempotency).
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

        // Vocabulary item: belongs to an exercise (cascade - it disappears with it) and references a store entry.
        // The entry must not be deleted while an item uses it (Restrict, as with the exercise store reference);
        // the controller catches that beforehand as a clean 409. OrderIndex is a pure sort key (deliberately NOT
        // unique): the study plan engine derives the stable item index from the list position (ordered by
        // OrderIndex, Id), so reordering works without transient unique collisions (SQLite checks per statement).
        modelBuilder.Entity<ExerciseItem>(e =>
        {
            e.HasIndex(i => new { i.ExerciseId, i.OrderIndex });
            // The same store entry may appear only once per exercise. Without this assurance two items for the
            // same word arise and with them two competing ItemProgress rows - the progress of that same word
            // would drift apart within one exercise.
            e.HasIndex(i => new { i.ExerciseId, i.VocabularyId }).IsUnique();
            e.HasOne(i => i.Exercise).WithMany().HasForeignKey(i => i.ExerciseId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Vocabulary).WithMany().HasForeignKey(i => i.VocabularyId).OnDelete(DeleteBehavior.Restrict);
        });

        // Cloze store: a unique key + gaps/word bank as JSON columns.
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

        // The study plan is optionally coupled to a catalog subject (for timetable control).
        modelBuilder.Entity<StudyPlan>()
            .HasOne(p => p.Subject).WithMany().HasForeignKey(p => p.SubjectId)
            .OnDelete(DeleteBehavior.SetNull);

        // Study plan position: belongs to a plan (cascade) and references a catalog exercise. The exercise must
        // not be deleted while it sits in a position (Restrict, as with vocabulary/cloze texts). Leitner
        // intervals and the stage schedule sit on the position as JSON columns.
        modelBuilder.Entity<PlanPosition>(e =>
        {
            // Plan loads filter by StudyPlanId and sort by Order/Id.
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
            e.Property(p => p.TimeSlots).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<ScoringTimeSlot>>(s, JsonOptions))
                .Metadata.SetValueComparer(JsonValueComparer.For<List<ScoringTimeSlot>?>());
        });

        // Progress per content atom of a position: disappears with the position (cascade);
        // at most one progress row per item index per position.
        modelBuilder.Entity<PositionItemProgress>(e =>
        {
            e.HasIndex(p => new { p.PlanPositionId, p.ItemIndex }).IsUnique();
            e.HasOne(p => p.PlanPosition).WithMany(pos => pos.ItemProgress).HasForeignKey(p => p.PlanPositionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Cross-plan learning state per (child, item): exactly one row per (child, item); index (child, entry)
        // for the word rollup. Disappears with the child OR the item (both cascade; no diamond paths, since
        // child and item are independent roots).
        modelBuilder.Entity<ItemProgress>(e =>
        {
            e.HasIndex(p => new { p.ChildId, p.ItemId }).IsUnique();
            e.HasIndex(p => new { p.ChildId, p.VocabularyId });
            e.HasIndex(p => new { p.ChildId, p.ExerciseId });
            e.HasOne(p => p.Child).WithMany().HasForeignKey(p => p.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Item).WithMany().HasForeignKey(p => p.ItemId).OnDelete(DeleteBehavior.Cascade);
        });

        // Answer history per (child, item): belongs to the child (cascade). The item reference is set to null
        // when the item is deleted (SetNull) so that the word history (denormalized VocabularyId) is preserved.
        modelBuilder.Entity<ItemReviewEvent>(e =>
        {
            e.HasIndex(x => new { x.ChildId, x.ItemId, x.At });
            e.HasIndex(x => new { x.ChildId, x.VocabularyId });
            e.HasOne(x => x.Child).WithMany().HasForeignKey(x => x.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.SetNull);
        });

        // Goal reward per position/period: at most one entry per (position, period) - the idempotency guarantee
        // of the goal points. Disappears with the position (cascade).
        // The cadence belongs in the key: on the entry it is a snapshot, and after a switch from daily to
        // weekly the same period start denotes two different periods.
        modelBuilder.Entity<PositionGoalReward>(e =>
        {
            e.HasIndex(r => new { r.PlanPositionId, r.Cadence, r.PeriodStart }).IsUnique();
            e.HasOne(r => r.PlanPosition).WithMany().HasForeignKey(r => r.PlanPositionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Goal penalty per position/period: at most one deduction per (position, period) - the idempotency
        // guarantee against double punishment when the lazy settlement runs over the same period several times. Cascade with the position.
        modelBuilder.Entity<PositionGoalPenalty>(e =>
        {
            e.HasIndex(r => new { r.PlanPositionId, r.Cadence, r.PeriodStart }).IsUnique();
            e.HasOne(r => r.PlanPosition).WithMany().HasForeignKey(r => r.PlanPositionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // An objective (big goal) belongs to a child (cascade); its key results hang on the objective (cascade).
        modelBuilder.Entity<Objective>(e =>
        {
            e.HasOne(o => o.Child).WithMany().HasForeignKey(o => o.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(o => o.KeyResults).WithOne(k => k.Objective!).HasForeignKey(k => k.ObjectiveId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // The catalog scope of a milestone is now a REAL foreign key. Before, they were only ids - `SubjectId` a
        // required field without a relationship, i.e. a zombie: nothing stopped it from pointing at a deleted
        // subject, and the evaluation then silently returned 0 %.
        //
        // Subject = cascade: a goal on a deleted subject is meaningless. (Two independent roots - Subject and
        // Objective -, no diamond; the same construction as ItemProgress.)
        //
        // SeriesUnit/exercise = **Restrict**, deliberately not SetNull: SetNull would silently widen a unit
        // goal into a subject goal, i.e. secretly move the bar. Restrict means: remove the goal first, then the
        // unit. So that this does not become a bare 500, `ExerciseUsageQueries` knows about the milestones -
        // as it already does about the study plan positions.
        modelBuilder.Entity<KeyResult>(e =>
        {
            e.HasOne<Subject>().WithMany().HasForeignKey(k => k.SubjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<SeriesUnit>().WithMany().HasForeignKey(k => k.SeriesUnitId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Exercise>().WithMany().HasForeignKey(k => k.ExerciseId).OnDelete(DeleteBehavior.Restrict);

            // The same milestone twice in the same goal would be a duplicate - and `RewardPerKeyResult` would pay
            // twice. Three FILTERED uniques, because SQLite treats NULLs as distinct: a single index over the
            // nullable scope columns would not hold the invariant.
            e.HasIndex(k => new { k.ObjectiveId, k.SubjectId, k.Metric }).IsUnique()
                .HasFilter("[SeriesUnitId] IS NULL AND [ExerciseId] IS NULL");
            e.HasIndex(k => new { k.ObjectiveId, k.SeriesUnitId, k.Metric }).IsUnique()
                .HasFilter("[SeriesUnitId] IS NOT NULL AND [ExerciseId] IS NULL");
            e.HasIndex(k => new { k.ObjectiveId, k.ExerciseId, k.Metric }).IsUnique()
                .HasFilter("[ExerciseId] IS NOT NULL");
        });

        // Objective reward log: at most one entry per (objective, occasion) - the idempotency guarantee against
        // a double payout when the lazy settlement runs several times. Cascade with the objective.
        // Two FILTERED uniques instead of one, because the occasion has two shapes and SQLite treats NULLs as
        // distinct: a single unique over the nullable column would allow any number of completion entries - and
        // that is the big chunk, i.e. money.
        modelBuilder.Entity<ObjectiveReward>(e =>
        {
            e.HasIndex(r => new { r.ObjectiveId, r.PaidKeyResultId }).IsUnique()
                .HasFilter("[PaidKeyResultId] IS NOT NULL");
            e.HasIndex(r => r.ObjectiveId, "IX_ObjectiveRewards_ObjectiveId_Complete").IsUnique()
                .HasFilter("[PaidKeyResultId] IS NULL");
            // The foreign key index by hand, because the convention only creates it while the column has *no*
            // index - the two filtered ones above count for it but are no good: a partial index does not serve a
            // plain `WHERE ObjectiveId IN (…)`. And that is exactly the hot read path (ObjectiveRewardService
            // loads the booked occasions on every child login).
            e.HasIndex(r => r.ObjectiveId, "IX_ObjectiveRewards_ObjectiveId");
            e.HasOne(r => r.Objective).WithMany().HasForeignKey(r => r.ObjectiveId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Practice session/test optionally coupled to a position. Both already hang on the plan through
        // StudyPlanId (cascade); the position reference therefore uses SetNull, to avoid creating second cascade
        // paths in SQLite (plan → position → session/test) next to plan → session/test.
        // The frozen play-out order (the cursor model) sits in a JSON column (reassign in the controller).
        modelBuilder.Entity<PracticeSession>(e =>
        {
            // Goal/metric queries: position+day(+mode) as well as child rollups over StudyPlan+day.
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
            // Goal/metric queries: position+day with completion status as well as child rollups over StudyPlan+day.
            e.HasIndex(t => new { t.PlanPositionId, t.Day, t.CompletedAt, t.Passed });
            e.HasIndex(t => new { t.StudyPlanId, t.Day });
            e.HasOne(t => t.PlanPosition).WithMany().HasForeignKey(t => t.PlanPositionId)
                .OnDelete(DeleteBehavior.SetNull);
            e.Property(t => t.Order).HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                s => JsonSerializer.Deserialize<List<int>>(s, JsonOptions) ?? new())
                .Metadata.SetValueComparer(JsonValueComparer.For<List<int>>());
        });

        // Timetable entry: child + subject; one subject at most once per child/weekday.
        modelBuilder.Entity<TimetableEntry>(e =>
        {
            e.HasIndex(t => new { t.ChildId, t.SubjectId, t.DayOfWeek }).IsUnique();
            e.HasOne(t => t.Child).WithMany().HasForeignKey(t => t.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Subject).WithMany().HasForeignKey(t => t.SubjectId).OnDelete(DeleteBehavior.Cascade);
        });

        // A mission belongs to a child (cascade); every mission is rewarded at most once per period.
        // The read path is always "the active missions of this child". Deliberately *no* unique on
        // (ChildId, Metric): "20 words daily" and "100 words weekly" are two legitimate missions on the same
        // metric.
        modelBuilder.Entity<Mission>().HasIndex(m => new { m.ChildId, m.Active });
        modelBuilder.Entity<Mission>()
            .HasOne(m => m.Child).WithMany().HasForeignKey(m => m.ChildId).OnDelete(DeleteBehavior.Cascade);
        // Two FILTERED uniques as with ObjectiveReward: `OneOff` has no period (PeriodStart NULL), and SQLite
        // treats NULLs as distinct - a single unique over the nullable column would allow any number of one-off
        // rewards. Exactly that pitfall made the text key attractive.
        // Unlike there, NO additional foreign key index is needed here: every query on MissionAwards names
        // (MissionId, Period, PeriodStart) in full, there is no read path on MissionId alone. Only the cascade
        // searches that way - on a table with a handful of rows per mission.
        modelBuilder.Entity<MissionAward>(e =>
        {
            e.HasIndex(a => new { a.MissionId, a.Period, a.PeriodStart }).IsUnique()
                .HasFilter("[PeriodStart] IS NOT NULL");
            e.HasIndex(a => new { a.MissionId, a.Period }).IsUnique().HasFilter("[PeriodStart] IS NULL");
            e.HasOne(a => a.Mission).WithMany().HasForeignKey(a => a.MissionId).OnDelete(DeleteBehavior.Cascade);
        });

        // An award belongs to a child (cascade); it is granted exactly once. Creating the same threshold of the
        // same metric twice would be a duplicate - the badge would come twice.
        modelBuilder.Entity<Achievement>().HasIndex(a => new { a.ChildId, a.Metric, a.Threshold }).IsUnique();
        modelBuilder.Entity<Achievement>()
            .HasOne(a => a.Child).WithMany().HasForeignKey(a => a.ChildId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AchievementAward>(e =>
        {
            e.HasIndex(a => a.AchievementId).IsUnique();
            e.HasOne(a => a.Achievement).WithMany().HasForeignKey(a => a.AchievementId).OnDelete(DeleteBehavior.Cascade);
        });

        // Daily reward box: at most one per (child, day) - the idempotency guarantee for the lazy award at
        // the practice/test-completion seams (mirrors PositionGoalPenalty for the negative side). Cascade
        // with the child, like the other per-child gamification logs.
        modelBuilder.Entity<DailyBoxClaim>(e =>
        {
            e.HasIndex(c => new { c.ChildId, c.Day }).IsUnique();
            e.HasOne(c => c.Child).WithMany().HasForeignKey(c => c.ChildId).OnDelete(DeleteBehavior.Cascade);
        });

        // Shop article: a family-internal article number, unique; belongs to the adult (cascade).
        // Listings (ShopListing): belong to the article (cascade).
        // Purchases (ShopPurchase): belong to the child (cascade); the listing reference is set to null when the
        //   listing is deleted, so that the purchase history is preserved.
        // Inventory (ChildInventory): belongs to the child (cascade); the article reference SetNull (see below).
        // Activation requests: belong to the child (cascade); the article reference SetNull to keep history stable.
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
            // FILTERED unique - and the filter is not cosmetics: SQLite treats NULLs as distinct, so a unique
            // over the nullable column would hold the invariant only for rows with an article. That is exactly
            // what is wanted: at most one row per living article, while two different deleted articles may leave
            // two orphaned stocks behind that do not collide with each other (and that the upsert lookup
            // `== article.Id` never hits again).
            e.HasIndex(i => new { i.ChildId, i.ShopArticleId }).IsUnique()
                .HasFilter("[ShopArticleId] IS NOT NULL");
            // Since the snapshot, the supervisor filter runs through SupervisorId instead of the navigation.
            e.HasIndex(i => new { i.ChildId, i.SupervisorId });
            e.Property(i => i.ConcurrencyStamp).IsConcurrencyToken();
            e.HasOne(i => i.Child).WithMany().HasForeignKey(i => i.ChildId).OnDelete(DeleteBehavior.Cascade);
            // SetNull instead of cascade: paid units are money and must not disappear with the catalog entry.
            // The purchase records already stood beside it that way - the inventory did not, so deleting an
            // article destroyed the value and left only the receipt. The article itself deliberately stays
            // cascade under the adult: an adult with articles must be able to delete themselves.
            e.HasOne(i => i.ShopArticle).WithMany().HasForeignKey(i => i.ShopArticleId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<ActivationRequest>(e =>
        {
            e.HasOne(r => r.Child).WithMany().HasForeignKey(r => r.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.ShopArticle).WithMany().HasForeignKey(r => r.ShopArticleId).OnDelete(DeleteBehavior.SetNull);
        });

        // Tag: a unique name per child; if the child is deleted, its tags disappear.
        modelBuilder.Entity<Tag>(e =>
        {
            e.HasIndex(t => new { t.ChildId, t.Name }).IsUnique();
            e.HasOne(t => t.Child).WithMany().HasForeignKey(t => t.ChildId).OnDelete(DeleteBehavior.Cascade);
        });

        // Exercise <-> tag: every exercise at most once per tag; links disappear with the tag or the exercise.
        modelBuilder.Entity<ExerciseTag>(e =>
        {
            e.HasIndex(x => new { x.TagId, x.ExerciseId }).IsUnique();
            e.HasOne(x => x.Tag).WithMany(t => t.ExerciseTags).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Cascade);
        });

        // Vocabulary <-> child tag: every entry at most once per tag; links disappear with the tag or the entry.
        modelBuilder.Entity<VocabularyTag>(e =>
        {
            e.HasIndex(x => new { x.TagId, x.VocabularyId }).IsUnique();
            e.HasOne(x => x.Tag).WithMany(t => t.VocabularyTags).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Vocabulary).WithMany().HasForeignKey(x => x.VocabularyId).OnDelete(DeleteBehavior.Cascade);
        });

        // Class test: belongs to a child (cascade), optionally coupled to a subject (SetNull).
        modelBuilder.Entity<Klassenarbeit>(e =>
        {
            e.HasOne(k => k.Child).WithMany().HasForeignKey(k => k.ChildId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(k => k.Subject).WithMany().HasForeignKey(k => k.SubjectId).OnDelete(DeleteBehavior.SetNull);
            e.Property(k => k.Grade).HasPrecision(3, 1);
        });

        // Class test <-> exercise: every exercise at most once per test.
        modelBuilder.Entity<KlassenarbeitExercise>(e =>
        {
            e.HasIndex(x => new { x.KlassenarbeitId, x.ExerciseId }).IsUnique();
            e.HasOne(x => x.Klassenarbeit).WithMany(k => k.Exercises).HasForeignKey(x => x.KlassenarbeitId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Cascade);
        });

        // Class test <-> tag: every tag at most once per test.
        modelBuilder.Entity<KlassenarbeitTag>(e =>
        {
            e.HasIndex(x => new { x.KlassenarbeitId, x.TagId }).IsUnique();
            e.HasOne(x => x.Klassenarbeit).WithMany(k => k.Tags).HasForeignKey(x => x.KlassenarbeitId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
        });

        // Remark: the author binds it (cascade - without an account there is nobody the note belongs to).
        // Every context reference, by contrast, is SetNull: a deleted child, a deleted exercise or a deleted
        // parent remark must not block the delete - the context may fade, the observation stays.
        // The role as a string (readable/stable, as on AccountProfile).
        modelBuilder.Entity<Remark>(e =>
        {
            e.Property(r => r.AuthorRole).HasConversion<string>();
            e.HasOne(r => r.Account).WithMany().HasForeignKey(r => r.AccountId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Child).WithMany().HasForeignKey(r => r.ChildId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(r => r.Exercise).WithMany().HasForeignKey(r => r.ExerciseId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(r => r.StudyPlan).WithMany().HasForeignKey(r => r.StudyPlanId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(r => r.PlanPosition).WithMany().HasForeignKey(r => r.PlanPositionId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(r => r.ParentRemark).WithMany().HasForeignKey(r => r.ParentRemarkId).OnDelete(DeleteBehavior.SetNull);
            // The two ways it is read: the own list in the widget (newest first) and the export/follow-up skill,
            // which fetches the open remarks.
            e.HasIndex(r => new { r.AccountId, r.CreatedAt });
            e.HasIndex(r => r.Status);
        });

        // History of a remark. The remark binds it (cascade): an entry without its case is pointless - unlike
        // the context, which may fade. The author account, by contrast, SetNull, because the entry's domain
        // statement still holds even when the account disappears.
        // The origin as a string like the `AuthorRole` next to it: this table is looked at by hand (a
        // development tool), and "Assistant" reads better there than "1".
        modelBuilder.Entity<RemarkComment>(e =>
        {
            e.Property(c => c.Author).HasConversion<string>();
            e.HasOne(c => c.Remark).WithMany(r => r.Comments).HasForeignKey(c => c.RemarkId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.AuthorAccount).WithMany().HasForeignKey(c => c.AuthorAccountId).OnDelete(DeleteBehavior.SetNull);
            // The only read path: the history of one remark, chronologically.
            e.HasIndex(c => new { c.RemarkId, c.CreatedAt });
        });

        ApplyEnumConvention(modelBuilder);
        ApplyStringLengthConvention(modelBuilder);
    }

    /// <summary>
    /// Spells out the delete behaviors that so far only came from the EF <b>convention</b> (required FK ⇒
    /// <c>Cascade</c>). The behavior does not change through this – what becomes visible is the <b>intent</b>.
    /// <para>
    /// Why that is not cosmetic: reflection cannot tell "explicitly set" from "inherited from the convention",
    /// so a guard cannot check the rule against the model. Only once every FK has its line is the assurance
    /// table in <c>SchemaGuardTests</c> (G2) complete – and a change of convention in a future EF version
    /// no longer shifts anything here silently.
    /// </para>
    /// <para>
    /// These are composition relations: the child belongs to the parent record and has no meaning without it
    /// (a chapter without a subject, a test result without a test attempt). The counter-check is the suite:
    /// it must stay green <b>unchanged</b> – any deviation means the intent written out was not the one
    /// actually lived.
    /// </para>
    /// </summary>
    private static void ApplyExplicitCascades(ModelBuilder modelBuilder)
    {
        // Catalog: series ⇒ unit ⇒ exercise. Deleting a unit drops its exercises (the Restrict guard
        // on PlanPosition→Exercise catches beforehand whatever still sits in a study plan).
        modelBuilder.Entity<Exercise>()
            .HasOne(x => x.SeriesUnit).WithMany().HasForeignKey(x => x.SeriesUnitId)
            .OnDelete(DeleteBehavior.Cascade);

        // Everything hanging on the child that is pointless without it: study plans, the ledger, the goals.
        modelBuilder.Entity<StudyPlan>()
            .HasOne(p => p.Child).WithMany().HasForeignKey(p => p.ChildId)
            .OnDelete(DeleteBehavior.Cascade);
        // The inverse navigation MUST be named where it exists: `WithMany()` without it stops EF from
        // recognizing the existing, convention-found relationship and it creates a SECOND one - a column
        // `ChildId1` grew here that way. Guard G2 caught exactly that.
        modelBuilder.Entity<ChildPointsEntry>()
            .HasOne(p => p.Child).WithMany(c => c.PointsEntries).HasForeignKey(p => p.ChildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Play-out history: session/test belong to the plan, their individual answers to the session or the
        // attempt. The position reference next to it is deliberately SetNull (no second cascade path in SQLite).
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
    /// Enums listed in an exception list of this class stay <c>int</c> – each one with its reason.
    /// The key has the form <c>Entity.Property</c>.
    /// </summary>
    private static readonly Dictionary<string, string> IntEnumsByDesign = new(StringComparer.Ordinal)
    {
        ["Child.AllowedContentRating"] =
            "Compared BY ORDER (asset.Rating <= child.AllowedContentRating) - as text the comparison would "
            + "be lexicographic and the content rating silently wrong.",
        ["MediaAsset.Rating"] =
            "Counterpart to Child.AllowedContentRating: same ordering, same reason.",
    };

    /// <summary>
    /// Whether <paramref name="entityDotProperty"/> (form <c>Entity.Property</c>) is deliberately stored as
    /// <c>int</c>. The guard <c>SchemaGuardTests</c> reads this list instead of keeping a second one –
    /// otherwise rule and exception would have to be maintained in two places.
    /// </summary>
    public static bool IntEnumErlaubt(string entityDotProperty) =>
        IntEnumsByDesign.ContainsKey(entityDotProperty);

    /// <summary>
    /// <b>One rule instead of 32 individual cases:</b> every persisted enum is stored as a <b>string</b>.
    /// <para>
    /// Before, 12 enums were converted through <c>HasConversion&lt;string&gt;()</c> and about 20 were
    /// implicitly <c>int</c> – with no discernible rule, in <c>Remarks</c> even both within the same table
    /// (<c>AuthorRole</c> as text next to <c>Status</c>/<c>Category</c> as a number). String is the right side
    /// because the contract speaks strings to the outside anyway (<c>JsonStringEnumConverter</c>): that removes
    /// the translation step between what is in the DB and what the API says – and the stored value becomes
    /// independent of the member order, which is what makes removing dead enum values safe in the first place.
    /// </para>
    /// <para>
    /// Two kinds of exception: <see cref="IntEnumsByDesign"/> (compared by order) and <c>[Flags]</c>.
    /// A flags combination has no name – <c>HasConversion&lt;string&gt;</c> produced
    /// <c>"Gymnasium, Realschule"</c> and broke every bitwise set query.
    /// </para>
    /// </summary>
    private static void ApplyEnumConvention(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                // Unwrap nullable: `GoalCadence?` is to be treated like `GoalCadence`.
                var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (!type.IsEnum) continue;
                if (type.IsDefined(typeof(FlagsAttribute), inherit: false)) continue;
                if (IntEnumsByDesign.ContainsKey($"{entity.ClrType.Name}.{property.Name}")) continue;

                // Only set it where nothing is set yet: an explicit configuration further up (or a converter of
                // its own) beats the convention.
                if (property.GetValueConverter() is not null) continue;
                property.SetValueConverter(
                    typeof(EnumToStringConverter<>).MakeGenericType(type));
            }
        }
    }

    /// <summary>
    /// String columns that stay unlimited <b>on purpose</b>: property → why. Without exception these are
    /// serialized structures (JSON) or frozen orders – their length follows from the content, not from an
    /// input, and an upper bound would be an arbitrary cut-off in the middle of the data model.
    /// </summary>
    private static readonly Dictionary<string, string> UnlimitedByDesign = new(StringComparer.Ordinal)
    {
        ["Exercise.ConfigJson"] = "Type-specific exercise config as JSON - grows with the exercise content.",
        ["Exercise.SuggestedBonus"] = "Bonus suggestion as a JSON object.",
        ["Remark.ContextJson"] = "Automatic context capture of the remark - that IS the feature.",
        ["Remark.RecentErrorsJson"] = "Captured recent errors; the length follows the incident.",
        ["Vocabulary.Noun"] = "Noun forms as a JSON object.",
        ["Vocabulary.Verb"] = "Verb forms as a JSON object.",
        ["Vocabulary.TranslationAlternatives"] = "Equally valid translations as a JSON list.",
        ["ClozeText.Gaps"] = "Gaps of the text as a JSON list - grows with the text.",
        ["ClozeText.WordBank"] = "Word bank as a JSON list.",
        ["Child.Interests"] = "Free-text interests as a JSON list (the language of the AI creator).",
        ["Child.OwnedSkins"] = "Unlocked skins as a JSON list - grows with the play state.",
        ["CreatorProfile.DefaultTypes"] = "Preferred exercise types as a JSON list.",
        ["SeriesUnit.Topics"] = "Topics of the unit as a JSON list - grows with the material.",
        ["PlanPosition.BoxIntervalDays"] = "Leitner intervals as a JSON list.",
        ["PlanPosition.StageSchedule"] = "Stage schedule as a JSON list.",
        ["PlanPosition.TimeSlots"] = "Points time slots of this obligation as a JSON list.",
        ["PracticeSession.Order"] = "Frozen play-out order as a JSON list - as long as the pool.",
        ["TestAttempt.Order"] = "Frozen test order as a JSON list - as long as the pool.",
    };

    /// <summary>Whether the column deliberately stays unlimited. Guard G3 reads this list instead of keeping a second one.</summary>
    public static bool UnbegrenztErlaubt(string entityDotProperty) =>
        UnlimitedByDesign.ContainsKey(entityDotProperty);

    /// <summary>Default length of a string column unless something else is said.</summary>
    private const int DefaultLength = 200;
    /// <summary>Length for free-text fields (description, notes, remark text).</summary>
    private const int FreeTextLength = 2000;
    /// <summary>Length for slugs/keys – short, because they appear in unique indexes.</summary>
    private const int KeyLength = 128;

    /// <summary>Name suffixes that give away a free-text field (a more generous length).</summary>
    private static readonly string[] FreeTextSuffixes =
        ["Description", "Notes", "Text", "Reason", "Persona", "Didactics", "Comment", "Message", "Answer"];

    /// <summary>Name suffixes that give away a slug/key (short length, often unique-indexed).</summary>
    private static readonly string[] KeySuffixes = ["Key", "Slug"];

    /// <summary>
    /// <b>One rule instead of 143 individual decisions:</b> every string column gets a length – 200 by
    /// default, 2000 for free text, 128 for slugs/keys. Before, <b>not a single</b> column in the whole model
    /// carried a <c>HasMaxLength</c>.
    /// <para>
    /// <b>Said honestly:</b> SQLite does not enforce the length, and EF does not validate it on
    /// <c>SaveChanges</c> either. The value lies elsewhere: on a provider change, <c>NVARCHAR(MAX)</c> would
    /// otherwise appear everywhere – and no unique index can be created on that in SQL Server, which hits
    /// exactly those columns that carry the idempotency. That is why the following also holds <b>hard</b>: a
    /// unique-indexed string column MUST be bounded. Enforcing the input stays the job of the DTO validation
    /// and is not part of this rule.
    /// </para>
    /// <para>
    /// The exceptions (<see cref="UnlimitedByDesign"/>) are without exception serialized structures: their
    /// length follows the content, an upper bound would be an arbitrary cut-off in the middle of the data model.
    /// </para>
    /// </summary>
    private static void ApplyStringLengthConvention(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType != typeof(string)) continue;
                if (UnlimitedByDesign.ContainsKey($"{entity.ClrType.Name}.{property.Name}")) continue;
                // An explicit length further up beats the convention.
                if (property.GetMaxLength() is not null) continue;

                var name = property.Name;
                property.SetMaxLength(
                    KeySuffixes.Any(s => name.EndsWith(s, StringComparison.Ordinal)) ? KeyLength
                    : FreeTextSuffixes.Any(s => name.EndsWith(s, StringComparison.Ordinal)) ? FreeTextLength
                    : DefaultLength);
            }
        }
    }
}
