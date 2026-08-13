using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Models;

namespace Pugling.Api.Data;

/// <summary>
/// Seeds the demo/development data at startup: time slots, the admin family, the learning catalog
/// (English/maths/geography), the textbook profile, the vocabulary store, French content, class tests,
/// gamification, a teacher library, the family shop and one complete demo study plan. Every sub-routine is
/// additive and idempotent so that a restart on an already populated DB duplicates nothing.
/// </summary>
public static class Seed
{
    /// <summary>
    /// Runs all seed routines one after another. <b>The order is part of the contract</b> – the resulting ids
    /// are wired up outside this repository (Playwright, skills, the checked-in API examples); new things
    /// therefore go to the end.
    /// <para>
    /// The second block is the <b>follow-up</b>: it needs the rows seeded above plus one service each.
    /// These four routines used to be called "backfill" and hung behind <c>Seed.Run</c> as their own classes in
    /// <c>Program.cs</c>. The name was wrong: there was no legacy data to fill – without them a <i>fresh</i> DB
    /// has adults without a login, vocabulary exercises without items and children without referenced
    /// interests. So they are seed, and here they stand where their order is visible.
    /// </para>
    /// <para>
    /// <b>The idempotency of every routine is a condition, not decoration:</b> startup calls this on <i>every</i>
    /// boot. The "does the child already have entries?" and "has the config already been reduced?" checks are
    /// not migration artifacts but the reason why a restart duplicates nothing.
    /// </para>
    /// </summary>
    public static async Task RunAsync(PuglingDbContext db, ExerciseItemService items,
        AccountService accounts, InterestTagService tags, CancellationToken ct)
    {
        SeedAdmin(db);
        SeedCatalog(db);
        SeedStudentProfile(db);
        SeedVocabulary(db);
        SeedFrench(db);
        SeedKlassenarbeiten(db);
        SeedGamification(db);
        SeedTeacherLibrary(db);
        SeedShop(db);
        SeedDemoPlan(db);

        // Follow-up (see the documentation above). Order: rights before content, content before login.
        await SeedExerciseGrantsAsync(db, ct);
        await SeedExerciseItemsAsync(db, items, ct);
        await SeedAccountsAsync(db, accounts, ct);
        await SeedChildInterestsAsync(db, tags, ct);
    }

    /// <summary>
    /// Gives every exercise that has an author an <b>owner grant</b> – exactly what
    /// <c>ExerciseControllerBase</c> does when creating one through the API.
    /// <para>
    /// This closes a real gap: the rights run exclusively through <see cref="ExerciseGrant"/>
    /// (<c>ExercisePermissionService</c>), but they were only granted by one raw SQL line in a migration – and
    /// that is a no-op on an empty DB. <b>So the seeded teacher could not edit their own exercises.</b>
    /// </para>
    /// <para>
    /// Idempotent through "this exercise has <i>no grant at all</i>". Deliberately not through "has no owner
    /// grant for its author": after an ownership transfer (owner moved over), startup would otherwise give the
    /// old author their right back on every boot.
    /// </para>
    /// </summary>
    private static async Task SeedExerciseGrantsAsync(PuglingDbContext db, CancellationToken ct)
    {
        var ohneGrant = await db.Exercises
            .Where(e => e.AuthorAdultId != null && !db.ExerciseGrants.Any(g => g.ExerciseId == e.Id))
            .Select(e => new { e.Id, AuthorId = e.AuthorAdultId!.Value })
            .ToListAsync(ct);
        if (ohneGrant.Count == 0) return;

        foreach (var e in ohneGrant)
            db.ExerciseGrants.Add(new ExerciseGrant
            {
                ExerciseId = e.Id,
                CreatorId = e.AuthorId,
                Permission = GrantPermission.Owner,
                GrantedByAdultId = e.AuthorId,
            });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Materializes the items of the vocabulary exercises out of their <see cref="Exercise.ConfigJson"/> (inline
    /// <c>Items</c> or ID <c>Refs</c>) into the <see cref="ExerciseItem"/> table and afterwards reduces the
    /// config to pure settings (direction/languages). The seed writes items inline – without this step a fresh
    /// DB would have vocabulary exercises <b>without any content</b>.
    /// <para>
    /// Idempotent through "does the config still carry items/refs?". The reconciliation
    /// (<see cref="ExerciseItemService"/>) preserves existing item ids while doing so.
    /// </para>
    /// </summary>
    private static async Task SeedExerciseItemsAsync(PuglingDbContext db, ExerciseItemService items,
        CancellationToken ct)
    {
        foreach (var exercise in await db.Exercises.Where(e => e.Type == ExerciseTypeKeys.Vocabulary).ToListAsync(ct))
        {
            var config = string.IsNullOrWhiteSpace(exercise.ConfigJson)
                ? new VocabularyConfig()
                : JsonSerializer.Deserialize<VocabularyConfig>(exercise.ConfigJson, SeedJson) ?? new VocabularyConfig();
            if (config.Items.Count == 0 && (config.Refs is null || config.Refs.Count == 0))
                continue; // config already reduced to settings - nothing to do.

            await items.SyncFromConfigAsync(exercise.Id, config, ct);
            config.Items = [];
            config.Refs = null;
            exercise.ConfigJson = JsonSerializer.Serialize(config, SeedJson);
            await db.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Creates a login account with the matching roles for every adult and every child – without it a fresh DB
    /// would have people <b>without a login</b>.
    /// <para>
    /// An adult <b>without a supervised child is a teacher account</b> and therefore gets
    /// <see cref="AccountService.EnsureForTeacherAsync"/> (creator, <i>not</i> supervisor) – exactly the domain
    /// distinction from docs/lehrer-konto-plan.md: an adult without a supervision assignment.
    /// Before, startup called <c>EnsureForAdultAsync</c> here for <i>every</i> adult, and the seeded teacher got
    /// the supervisor role even though the creator-only variant exists precisely for them and was never reached.
    /// </para>
    /// <para>
    /// That the routine runs <b>at the end</b> is part of the rule: only then do the supervision links it
    /// queries exist. An adult who registers through the API gets their roles there anyway (and keeps them –
    /// the ensure deliberately does not retrofit).
    /// </para>
    /// </summary>
    private static async Task SeedAccountsAsync(PuglingDbContext db, AccountService accounts, CancellationToken ct)
    {
        foreach (var adult in await db.Adults.AsNoTracking().Include(a => a.SupervisedLinks).ToListAsync(ct))
        {
            if (adult.SupervisedLinks.Count > 0) await accounts.EnsureForAdultAsync(adult, ct);
            else await accounts.EnsureForTeacherAsync(adult, ct);
        }

        foreach (var child in await db.Children.AsNoTracking().ToListAsync(ct))
            await accounts.EnsureForChildAsync(child, ct);
    }

    /// <summary>
    /// Transfers the children's <b>free-text</b> interests into the referenced taxonomy
    /// (<see cref="ChildInterest"/>) so that the image selection has something to compute right away. Lossless:
    /// <c>Child.Interests</c> stays untouched – it remains the language of the AI creator.
    /// <para>
    /// Idempotent through "does the child already have entries?": a child whose interests the supervisor has
    /// already maintained is skipped. Otherwise a restart would revive deliberately deleted entries or
    /// overwrite weights.
    /// </para>
    /// </summary>
    private static async Task SeedChildInterestsAsync(PuglingDbContext db, InterestTagService tags,
        CancellationToken ct)
    {
        var mitEintraegen = await db.ChildInterests.Select(i => i.ChildId).Distinct().ToListAsync(ct);
        var offen = await db.Children
            .Where(c => !mitEintraegen.Contains(c.Id))
            .Select(c => new { c.Id, c.Interests })
            .ToListAsync(ct);

        foreach (var child in offen)
        {
            if (child.Interests.Count == 0) continue;

            // Through the shared service, so that "Pokémon" hits the same tag here as later in the UI.
            foreach (var tag in await tags.EnsureManyAsync(child.Interests, ct: ct))
            {
                // Newly created tags have no id yet - save first, then reference them.
                if (tag.Id == 0) await db.SaveChangesAsync(ct);
                db.ChildInterests.Add(new ChildInterest
                {
                    ChildId = child.Id,
                    InterestTagId = tag.Id,
                    Weight = InterestStartWeight,
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Starting weight of the transferred interests: a clear but not dominant preference.</summary>
    private const int InterestStartWeight = 2;

    private static readonly JsonSerializerOptions SeedJson = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Exercise-independent profile of the seed child: one textbook in use as the basis for a later study plan
    /// generator ("what is due right now"). Title + chapter deliberately match the
    /// <see cref="Exercise.Source"/> of the seeded English exercises ("Green Line 1, Unit 1") so that an agent
    /// finds existing exercises again instead of generating new ones. The child's interests are already set in
    /// <see cref="SeedAdmin"/>. Additive and idempotent: it only creates the book if the child has none yet.
    /// </summary>
    private static void SeedStudentProfile(PuglingDbContext db)
    {
        var child = db.Children.OrderBy(c => c.Id).FirstOrDefault();
        if (child is null) return;
        if (db.Textbooks.Any(t => t.ChildId == child.Id)) return;

        var englisch = db.Subjects.FirstOrDefault(s => s.Name == "Englisch");
        db.Textbooks.Add(new Textbook
        {
            ChildId = child.Id,
            Title = "Green Line 1",
            SubjectName = "Englisch",
            SubjectId = englisch?.Id,
            Grade = 5,
            Publisher = "Klett",
            CurrentChapter = "Unit 1 – Greetings",
        });
        db.SaveChanges();
    }

    /// <summary>
    /// A <b>complete</b> demo study plan for the seed child that makes <b>every</b> playable learning method
    /// visible as its own position – meant as a real data set for frontend development: all vocabulary stages
    /// (getting acquainted, self-assessment/"flip", multiple choice, letter boxes, free text, listening), the
    /// cloze and matching stages, one pure content exercise (Birkenbihl), fixed and generated arithmetic checks,
    /// one list and one translation. It additionally covers the goal variants (daily/weekly/free), the coin
    /// penalty, the Leitner scheduling and a stage schedule, so that the daily mission, practice, final test and
    /// reporting in the frontend run against realistic data.
    /// It deliberately hangs on its <b>own demo family</b> (demo father <c>demo-vater@example.com</c>/PIN 0001,
    /// demo child "Demo-Kind"/PIN 2222) so that the primary seed child "Sohn" stays a clean initial state.
    /// Additive and idempotent: it only creates the family/plan while the demo plan does not exist yet.
    /// </summary>
    private static void SeedDemoPlan(PuglingDbContext db)
    {
        const string planTitle = "Demo: Alle Lernarten (Frontend-Testdaten)";
        const string demoSupervisorEmail = "demo-vater@example.com";

        // Deliberately an OWN demo family instead of the primary seed child "Sohn": that keeps "Sohn" a clean
        // initial state (for the tests that build their own plans/goals there, among others), while this rich
        // frontend test data set sits isolated next to it. Get-or-create, so that a re-run on a populated DB
        // neither duplicates anything nor touches other people's accounts.
        var demoSupervisor = db.Adults.FirstOrDefault(f => f.Email == demoSupervisorEmail);
        if (demoSupervisor is null)
        {
            demoSupervisor = new Adult { Name = "Demo-Vater", Email = demoSupervisorEmail, Pin = Auth.PinHasher.Hash("0001") };
            db.Adults.Add(demoSupervisor);
            db.SaveChanges();
        }

        var child = db.Children.FirstOrDefault(c => c.Name == "Demo-Kind");
        if (child is null)
        {
            child = new Child
            {
                Name = "Demo-Kind",
                BirthYear = 2013,
                Gender = Gender.Male,
                Interests = ["Minecraft", "Basketball"],
                ProfileNotes = "Frontend-Testkind: trägt den vollständigen Lernarten-Demoplan.",
                Pin = Auth.PinHasher.Hash("2222"),
                // Starting balance, so that shop/skins can be tried out right away.
                PointsEntries =
                {
                    new ChildPointsEntry { Amount = 200, Kind = PointKind.Base, Reason = "Startguthaben (Münzen)" },
                    new ChildPointsEntry { Amount = 300, Kind = PointKind.Achievement, Reason = "Willkommens-Gems" },
                },
            };
            db.Children.Add(child);
            db.SaveChanges();
            db.SupervisorLinks.Add(new SupervisorLink { SupervisorId = demoSupervisor.Id, StudentId = child.Id, Relation = SupervisorRelation.Father });
            db.SaveChanges();
        }

        if (db.StudyPlans.Any(p => p.ChildId == child.Id && p.Title == planTitle)) return;

        // Fetch catalog exercises by title (stable within the seed). Optional ones stay null if their subject is missing.
        Exercise? ByTitle(string title) => db.Exercises.FirstOrDefault(e => e.Title == title);
        var vocab = ByTitle("Begrüßungen");
        if (vocab is null) return; // without the core vocabulary exercise the demo plan makes no sense
        int vocabId = vocab.Id;

        var cloze = ByTitle("Lückentext: A short dialogue");
        var birkenbihl = ByTitle("Birkenbihl: Getting to know each other");
        var arithmetic = ByTitle("Das kleine 1×1 (7er-Reihe)");
        var drill = ByTitle("Kopfrechnen bis 20");
        var list = ByTitle("Die 16 Bundesländer");
        var matching = ByTitle("Bundesland → Landeshauptstadt");
        var translation = ByTitle("Translation: Talking about the future");

        var englisch = db.Subjects.FirstOrDefault(s => s.Name == "Englisch");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var positions = new List<PlanPosition>();
        var order = 0;

        // ── Vocabulary: every test stage as its own position ────────────────────────
        // So that all vocabulary learning modes can be played through right away. The content is always the same
        // exercise "Begrüßungen"; only the stage per position determines the learning mode.
        void Vocab(TestStage stage, GoalCadence cadence, bool leitner, int penalty = 0)
        {
            positions.Add(new PlanPosition
            {
                ExerciseId = vocabId,
                Order = order++,
                Stage = (int)stage,
                Cadence = cadence,
                PenaltyCoins = penalty,
                UseLeitner = leitner,
                // Typed stages may only be passed "graded" (no mere clicking/revealing).
                RequireTypedTest = stage is TestStage.FreeText or TestStage.Audio,
            });
        }

        Vocab(TestStage.ShowBoth, GoalCadence.None, leitner: false); // getting acquainted (front/back visible)
        Vocab(TestStage.SelfAssess, GoalCadence.Daily, leitner: true, penalty: 5); // "flip" + self-assessment
        Vocab(TestStage.MultipleChoice, GoalCadence.Daily, leitner: true); // multiple choice with distractors
        Vocab(TestStage.LetterBoxes, GoalCadence.Weekly, leitner: true); // letter boxes
        Vocab(TestStage.Audio, GoalCadence.None, leitner: false); // listen → type (items without audio are mute but playable)

        // The free-text stage additionally with a fast-answer bonus, to exercise that points path as well.
        positions.Add(new PlanPosition
        {
            ExerciseId = vocabId,
            Order = order++,
            Stage = (int)TestStage.FreeText,
            Cadence = GoalCadence.Daily,
            PenaltyCoins = 10,
            UseLeitner = true,
            RequireTypedTest = true,
            SpeedThresholdSeconds = 8,
            SpeedBonusPoints = 3,
        });

        // One position with a stage schedule: the difficulty rises automatically over the runtime.
        positions.Add(new PlanPosition
        {
            ExerciseId = vocabId,
            Order = order++,
            Stage = (int)TestStage.SelfAssess,
            Cadence = GoalCadence.Daily,
            UseLeitner = true,
            StageSchedule =
            [
                new StageStep(1, (int)TestStage.ShowBoth),
                new StageStep(3, (int)TestStage.SelfAssess),
                new StageStep(7, (int)TestStage.MultipleChoice),
                new StageStep(14, (int)TestStage.FreeText),
            ],
        });

        // ── Cloze: two stages (word bank vs. free text) ─────────────────────
        if (cloze is not null)
        {
            positions.Add(new PlanPosition
            {
                ExerciseId = cloze.Id,
                Order = order++,
                Stage = (int)ClozeStage.TranslationWordBank,
                Cadence = GoalCadence.Daily,
                UseLeitner = true,
            });
            positions.Add(new PlanPosition
            {
                ExerciseId = cloze.Id,
                Order = order++,
                Stage = (int)ClozeStage.FreeText,
                Cadence = GoalCadence.Weekly,
                UseLeitner = true,
                RequireTypedTest = true,
            });
        }

        // ── Matching: plain vs. with distractors ────────────────────────────────
        if (matching is not null)
        {
            positions.Add(new PlanPosition
            {
                ExerciseId = matching.Id,
                Order = order++,
                Stage = (int)MatchStage.Direct,
                Cadence = GoalCadence.Daily,
                UseLeitner = true,
            });
            positions.Add(new PlanPosition
            {
                ExerciseId = matching.Id,
                Order = order++,
                Stage = (int)MatchStage.Distractors,
                Cadence = GoalCadence.None,
            });
        }

        // ── Pure content exercise (no active questioning) ──────────────────────────
        if (birkenbihl is not null)
            positions.Add(new PlanPosition { ExerciseId = birkenbihl.Id, Order = order++, Cadence = GoalCadence.None });

        // ── Catalog checks: fixed arithmetic tasks, generated drill, list ──────
        // GoalThreshold is a PERCENT pass threshold here too (see PlanPosition.GoalThreshold) - this used to
        // hold hit counts (3/8/16), which as percentages let every attempt pass and thereby made precisely the
        // obligation toothless that they were meant to set. Three different values, so that the test data also
        // shows a deviating threshold.
        if (arithmetic is not null)
            positions.Add(new PlanPosition { ExerciseId = arithmetic.Id, Order = order++, Cadence = GoalCadence.Daily, GoalThreshold = 80 });
        if (drill is not null)
            positions.Add(new PlanPosition { ExerciseId = drill.Id, Order = order++, Cadence = GoalCadence.Daily, GoalThreshold = 70 });
        if (list is not null)
            positions.Add(new PlanPosition { ExerciseId = list.Id, Order = order++, Cadence = GoalCadence.Weekly, GoalThreshold = 90 });

        // ── Translation (from the teacher library, if present) ───────────
        if (translation is not null)
            positions.Add(new PlanPosition { ExerciseId = translation.Id, Order = order++, Cadence = GoalCadence.Weekly });

        var plan = new StudyPlan
        {
            ChildId = child.Id,
            Title = planTitle,
            Description = "Automatisch geseedeter Übungs-Querschnitt: jede Lernart einmal spielbar (Frontend-Testdaten).",
            SubjectId = englisch?.Id,
            StartDate = today,
            EndDate = today.AddYears(1),
            Active = true,
            Positions = positions,
        };

        db.StudyPlans.Add(plan);
        db.SaveChanges();
    }

    /// <summary>
    /// Makes the core scenario tangible: an <b>English teacher</b> (with their own father account) creates
    /// exercises at grade 9 Gymnasium level – with <see cref="Exercise.AuthorAdultId"/> set. Because the catalog
    /// is global, other adults find these exercises through the search (subject English, grade 9, Gymnasium) and
    /// take them into their own study plans as positions; changing/deleting them, however, is only allowed to the
    /// teacher themselves. Additive and idempotent: the teacher account is created on demand, the demo content is
    /// only added while the demo chapter is still missing (even if the account already exists for other reasons).
    /// </summary>
    private static void SeedTeacherLibrary(PuglingDbContext db)
    {
        const string teacherEmail = "englischlehrer@example.com";
        const string unitLabel = "Unit 5 – Global challenges (Klasse 9)";

        var englisch = db.Subjects.FirstOrDefault(s => s.Name == "Englisch");
        if (englisch is null) return;

        var series = db.TextbookSeries.FirstOrDefault(s => s.Slug == "green-line-1");
        if (series is null) return;

        // Anchor idempotency on the content, not only on the account: if the demo unit already exists there
        // is nothing to do - otherwise a teacher account created elsewhere would silently suppress the catalog
        // content.
        if (db.SeriesUnits.Any(u => u.SeriesId == series.Id && u.Label == unitLabel)) return;

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        string Json<T>(T config) => JsonSerializer.Serialize(config, options);

        // The teacher account (login with this id + PIN 9999). Without children - they only curate the catalog.
        // Get-or-create, so that an already existing account is reused instead of duplicated.
        var teacher = db.Adults.FirstOrDefault(f => f.Email == teacherEmail);
        if (teacher is null)
        {
            teacher = new Adult { Name = "Herr Schmidt (Englischlehrer)", Email = teacherEmail, Pin = Auth.PinHasher.Hash("9999") };
            db.Adults.Add(teacher);
            db.SaveChanges();
        }

        // Reuse the subject's categories (grammar/vocabulary) if they exist.
        var grammatik = db.ExerciseCategories.FirstOrDefault(c => c.SubjectId == englisch.Id && c.Name == "Grammatik");
        var vokabeln = db.ExerciseCategories.FirstOrDefault(c => c.SubjectId == englisch.Id && c.Name == "Vokabeln");

        var unit = new SeriesUnit { SeriesId = series.Id, Grade = 9, OrderIndex = 5, Label = unitLabel };
        db.SeriesUnits.Add(unit);
        db.SaveChanges();

        const SchoolTypes gym = SchoolTypes.Gymnasium;

        var vocab = new Exercise
        {
            SeriesUnitId = unit.Id,
            AuthorAdultId = teacher.Id,
            Type = ExerciseTypeKeys.Vocabulary,
            Title = "Vocabulary: The environment",
            OrderIndex = 1,
            RewardPoints = 15,
            GradeMin = 8,
            GradeMax = 10,
            SchoolTypes = gym,
            Source = "Green Line 5, Unit 3",
            CategoryId = vokabeln?.Id,
            ConfigJson = Json(new VocabularyConfig
            {
                Direction = "front-to-back",
                SourceLang = "en",
                TargetLang = "de",
                Items =
                {
                    new VocabItem("sustainability", "Nachhaltigkeit"),
                    new VocabItem("pollution", "Umweltverschmutzung"),
                    new VocabItem("renewable energy", "erneuerbare Energie"),
                    new VocabItem("greenhouse gas", "Treibhausgas"),
                    new VocabItem("to reduce", "reduzieren, verringern"),
                    new VocabItem("waste", "Abfall, Müll"),
                },
            }),
        };

        // A grade 9 classic: if-clauses type II (conditional). A cloze text with a word bank.
        var conditionals = new Exercise
        {
            SeriesUnitId = unit.Id,
            AuthorAdultId = teacher.Id,
            Type = ExerciseTypeKeys.Cloze,
            Title = "Grammar: Conditional sentences (type II)",
            OrderIndex = 2,
            RewardPoints = 20,
            GradeMin = 9,
            GradeMax = 10,
            SchoolTypes = gym,
            Source = "Green Line 5, Unit 3",
            CategoryId = grammatik?.Id,
            ConfigJson = Json(new ClozeConfig
            {
                Text = "If everyone {{1}} public transport, cities {{2}} much cleaner.",
                Gaps =
                {
                    new Gap(1, "used", new List<string>()),
                    new Gap(2, "would be", new List<string> { "'d be" }),
                },
                WordBank = new List<string> { "used", "would be", "will be", "uses" },
            }),
        };

        var translation = new Exercise
        {
            SeriesUnitId = unit.Id,
            AuthorAdultId = teacher.Id,
            Type = ExerciseTypeKeys.Translation,
            Title = "Translation: Talking about the future",
            OrderIndex = 3,
            RewardPoints = 20,
            GradeMin = 9,
            GradeMax = 10,
            SchoolTypes = gym,
            Source = "Green Line 5, Unit 3",
            CategoryId = grammatik?.Id,
            ConfigJson = Json(new TranslationConfig
            {
                SourceLang = "de",
                TargetLang = "en",
                Items =
                {
                    new TranslationItem("Wir müssen unseren Plastikverbrauch reduzieren.", "We have to reduce our use of plastic."),
                    new TranslationItem("Wenn wir jetzt handeln, können wir den Planeten retten.", "If we act now, we can save the planet."),
                },
            }),
        };

        db.Exercises.AddRange(vocab, conditionals, translation);
        db.SaveChanges();
    }

    /// <summary>
    /// French content for the typical entry point "child (14 y/o) is struggling with French":
    /// one subject with a chapter + catalog exercises (for browsing/filtering by grade) AND matching entries in
    /// the vocabulary store (the basis for a vocabulary study plan). Additive and idempotent: it also runs on an
    /// already populated DB (it checks specifically for the subject, and per vocabulary key).
    /// </summary>
    private static void SeedFrench(PuglingDbContext db)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        string Json<T>(T config) => JsonSerializer.Serialize(config, options);

        // (fr -> de) Core vocabulary "En ville / Le quotidien" - grade 8/9, Découvertes 1, Unité 2.
        (string Word, string De, PartOfSpeech Pos, string? Article)[] woerter =
        [
            ("la ville", "die Stadt", PartOfSpeech.Noun, "la"),
            ("la rue", "die Straße", PartOfSpeech.Noun, "la"),
            ("la maison", "das Haus", PartOfSpeech.Noun, "la"),
            ("l'école", "die Schule", PartOfSpeech.Noun, "l'"),
            ("le magasin", "das Geschäft", PartOfSpeech.Noun, "le"),
            ("l'ami", "der Freund", PartOfSpeech.Noun, "l'"),
            ("acheter", "kaufen", PartOfSpeech.Verb, null),
            ("manger", "essen", PartOfSpeech.Verb, null),
            ("parler", "sprechen", PartOfSpeech.Verb, null),
            ("toujours", "immer", PartOfSpeech.Adverb, null),
            ("souvent", "oft", PartOfSpeech.Adverb, null),
            ("beaucoup", "viel", PartOfSpeech.Adverb, null),
        ];

        foreach (var w in woerter)
        {
            var key = VocabKey.Generate("fr", w.Word, "de", w.De);
            if (db.Vocabularies.Any(v => v.Key == key)) continue;
            db.Vocabularies.Add(new Vocabulary
            {
                Key = key,
                SourceLanguage = "fr",
                TargetLanguage = "de",
                Word = w.Word,
                Translation = w.De,
                PartOfSpeech = w.Pos,
                Noun = w.Article is null ? null : new NounInfo { Article = w.Article },
                Verb = w.Pos == PartOfSpeech.Verb ? new VerbInfo { IsBaseForm = true, Infinitive = w.Word } : null,
            });
        }
        db.SaveChanges();

        // Catalog: only create it if the subject is still missing (otherwise just add store entries, see above).
        if (db.Subjects.Any(s => s.Name == "Französisch")) return;

        var frVokabeln = new ExerciseCategory { Name = "Vokabeln" };
        var frGrammatik = new ExerciseCategory { Name = "Grammatik" };
        var franzoesisch = new Subject { Name = "Französisch", Categories = { frVokabeln, frGrammatik } };
        db.Subjects.Add(franzoesisch);
        db.SaveChanges();

        var decouvertes = new TextbookSeries
        {
            Name = "Découvertes 1",
            Slug = "decouvertes-1",
            SubjectId = franzoesisch.Id,
            SubjectName = "Französisch",
            SourceLanguage = "fr",
            TargetLanguage = "de",
        };
        db.TextbookSeries.Add(decouvertes);
        db.SaveChanges();

        var enVilleUnit = new SeriesUnit { SeriesId = decouvertes.Id, Grade = 8, OrderIndex = 1, Label = "Unité 2 – En ville" };
        db.SeriesUnits.Add(enVilleUnit);
        db.SaveChanges();

        var frExercises = new List<Exercise>
        {
            new()
            {
                SeriesUnitId = enVilleUnit.Id,
                Type = ExerciseTypeKeys.Vocabulary,
                Title = "Vokabeln: En ville",
                OrderIndex = 1,
                RewardPoints = 10,
                GradeMin = 7, GradeMax = 9,
                SchoolTypes = SchoolTypes.Realschule | SchoolTypes.Gymnasium,
                Source = "Découvertes 1, Unité 2",
                Category = frVokabeln,
                ConfigJson = Json(new VocabularyConfig
                {
                    Direction = "front-to-back",
                    SourceLang = "fr",
                    TargetLang = "de",
                    Items =
                    {
                        new VocabItem("la ville", "die Stadt"),
                        new VocabItem("la rue", "die Straße"),
                        new VocabItem("le magasin", "das Geschäft"),
                        new VocabItem("acheter", "kaufen"),
                        new VocabItem("manger", "essen"),
                    }
                }),
            },
            new()
            {
                SeriesUnitId = enVilleUnit.Id,
                Type = ExerciseTypeKeys.Cloze,
                Title = "Lückentext: Au magasin",
                OrderIndex = 2,
                RewardPoints = 15,
                GradeMin = 7, GradeMax = 9,
                SchoolTypes = SchoolTypes.Realschule | SchoolTypes.Gymnasium,
                Source = "Découvertes 1, Unité 2",
                Category = frGrammatik,
                ConfigJson = Json(new ClozeConfig
                {
                    Text = "Je {{1}} du pain à la {{2}}.",
                    Gaps =
                    {
                        new Gap(1, "mange", new List<string> { "achète" }),
                        new Gap(2, "boulangerie", new List<string> { "maison" }),
                    },
                    WordBank = new List<string> { "mange", "achète", "boulangerie", "maison" },
                }),
            },
        };

        db.Exercises.AddRange(frExercises);
        db.SaveChanges();
    }

    /// <summary>
    /// Templates for missions (daily/weekly goals) and awards (Duolingo-style badges) per child.
    /// The supervisor can edit/delete them freely and add their own (see the missions/achievements controllers).
    /// </summary>
    private static void SeedGamification(PuglingDbContext db)
    {
        var child = db.Children.OrderBy(c => c.Id).FirstOrDefault();
        if (child is null) return;

        if (!db.Missions.Any() && !db.Achievements.Any())
        {
            db.Missions.AddRange(
                new Mission { ChildId = child.Id, Title = "Tagesziel: 10 richtige Antworten", Metric = ProgressMetric.CorrectReviews, Target = 10, Period = MissionPeriod.Daily, RewardPoints = 15 },
                new Mission { ChildId = child.Id, Title = "Tagesziel: 15 Minuten üben", Metric = ProgressMetric.MinutesPracticed, Target = 15, Period = MissionPeriod.Daily, RewardPoints = 10 },
                new Mission { ChildId = child.Id, Title = "Wochenziel: 3 Tests bestehen", Metric = ProgressMetric.TestsPassed, Target = 3, Period = MissionPeriod.Weekly, RewardPoints = 30 },
                new Mission { ChildId = child.Id, Title = "Wochenziel: 25 neue Wörter", Metric = ProgressMetric.NewWords, Target = 25, Period = MissionPeriod.Weekly, RewardPoints = 40 });

            db.Achievements.AddRange(
                new Achievement { ChildId = child.Id, Title = "Erste Schritte", Icon = "🌱", Metric = ProgressMetric.CorrectReviews, Threshold = 50, RewardPoints = 20 },
                new Achievement { ChildId = child.Id, Title = "Wortschatz-Sammler", Icon = "📚", Metric = ProgressMetric.NewWords, Threshold = 100, RewardPoints = 50 },
                new Achievement { ChildId = child.Id, Title = "Test-Ass", Icon = "🏆", Metric = ProgressMetric.TestsPassed, Threshold = 10, RewardPoints = 40 },
                new Achievement { ChildId = child.Id, Title = "Feuer-Streak", Icon = "🔥", Metric = ProgressMetric.StreakDays, Threshold = 7, RewardPoints = 70 },
                new Achievement { ChildId = child.Id, Title = "Marathon", Icon = "⏱️", Metric = ProgressMetric.MinutesPracticed, Threshold = 300, RewardPoints = 60 });

            db.SaveChanges();
        }
    }

    private static void SeedKlassenarbeiten(PuglingDbContext db)
    {
        if (db.Klassenarbeiten.Any()) return;

        var child = db.Children.OrderBy(c => c.Id).FirstOrDefault();
        if (child is null) return; // without a child there is no child-scoped data

        var englisch = db.Subjects.FirstOrDefault(s => s.Name == "Englisch");
        var mathe = db.Subjects.FirstOrDefault(s => s.Name == "Mathe");
        var exEnglisch = db.Exercises.Where(e => e.SeriesUnit!.Series!.Subject!.Name == "Englisch").OrderBy(e => e.Id).ToList();
        var exMathe = db.Exercises.Where(e => e.SeriesUnit!.Series!.Subject!.Name == "Mathe").OrderBy(e => e.Id).ToList();

        // Two example tags - one set by the supervisor, one by the child.
        var tagUnit1 = new Tag { ChildId = child.Id, Name = "Unit 1", Color = "#3b82f6", CreatedBy = TaggedBy.Vater };
        var tag1x1 = new Tag { ChildId = child.Id, Name = "Einmaleins", Color = "#f59e0b", CreatedBy = TaggedBy.Sohn };
        db.Tags.AddRange(tagUnit1, tag1x1);
        db.SaveChanges();

        foreach (var e in exEnglisch)
            db.ExerciseTags.Add(new ExerciseTag { TagId = tagUnit1.Id, ExerciseId = e.Id });
        foreach (var e in exMathe)
            db.ExerciseTags.Add(new ExerciseTag { TagId = tag1x1.Id, ExerciseId = e.Id });
        db.SaveChanges();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // A planned test: the relevant exercises come in through the linked tag "Unit 1".
        var geplant = new Klassenarbeit
        {
            ChildId = child.Id,
            SubjectId = englisch?.Id,
            Title = "Vokabeltest Unit 1",
            Topic = "Begrüßungen & kurzer Dialog",
            ScheduledDate = today.AddDays(10),
            Status = KlassenarbeitStatus.Planned,
            Tags = { new KlassenarbeitTag { TagId = tagUnit1.Id } },
        };

        // A written test with a bad grade: exercises assigned directly → they show up in the repeat endpoint.
        var geschrieben = new Klassenarbeit
        {
            ChildId = child.Id,
            SubjectId = mathe?.Id,
            Title = "Mathe-Arbeit Einmaleins",
            Topic = "Kleines 1×1, Reihen 6–9",
            ScheduledDate = today.AddDays(-7),
            Status = KlassenarbeitStatus.Written,
            Grade = 4.5m,
            GradeComment = "7er- und 8er-Reihe saßen nicht.",
            Exercises = exMathe.Select(e => new KlassenarbeitExercise { ExerciseId = e.Id }).ToList(),
        };

        db.Klassenarbeiten.AddRange(geplant, geschrieben);
        db.SaveChanges();
    }

    private static void SeedVocabulary(PuglingDbContext db)
    {
        if (db.Vocabularies.Any()) return;

        // Noun + verb base form
        db.Vocabularies.AddRange(
            new Vocabulary
            {
                Key = "en_house_de_haus",
                SourceLanguage = "en",
                TargetLanguage = "de",
                Word = "house",
                Translation = "Haus",
                PartOfSpeech = PartOfSpeech.Noun,
                Noun = new NounInfo { Article = "das", Genus = Genus.Neuter, Plural = "Häuser" },
            },
            new Vocabulary
            {
                Key = "en_go_de_gehen",
                SourceLanguage = "en",
                TargetLanguage = "de",
                Word = "go",
                Translation = "gehen",
                PartOfSpeech = PartOfSpeech.Verb,
                Verb = new VerbInfo { IsBaseForm = true, Infinitive = "gehen" },
            });
        db.SaveChanges();

        // An inflected form pointing at its base form
        var baseId = db.Vocabularies.Where(v => v.Key == "en_go_de_gehen").Select(v => v.Id).First();
        db.Vocabularies.Add(new Vocabulary
        {
            Key = "en_goes_de_geht",
            SourceLanguage = "en",
            TargetLanguage = "de",
            Word = "goes",
            Translation = "geht",
            PartOfSpeech = PartOfSpeech.Verb,
            Verb = new VerbInfo { IsBaseForm = false, Infinitive = "gehen", Tense = "present", Person = "3", Number = "singular" },
            BaseFormId = baseId,
        });
        db.SaveChanges();
    }

    /// <summary>
    /// Demo articles and listings of the family shop. It shows all the central fields of the shop cycle:
    /// different <see cref="UnitType"/>s and <see cref="ActionType"/>s, coin and gem prices, automatic refilling
    /// (<see cref="ShopRefillKind"/>) and mixed stocks – so that new developers find real objects right away
    /// without having to create articles through the admin API first.
    /// Additive and idempotent: it only runs while no shop articles exist yet.
    /// </summary>
    private static void SeedShop(PuglingDbContext db)
    {
        if (db.ShopArticles.Any()) return;

        var father = db.Adults.OrderBy(f => f.Id).FirstOrDefault();
        if (father is null) return;

        // ── Article 1: TV time ──────────────────────────────────────────────
        // A daily allowance: refilled to MaxStock automatically every day.
        // Two listings show the "small pack vs. bulk pack" pattern.
        var tv = new ShopArticle
        {
            AdultId = father.Id,
            ArticleNumber = "TV-001",
            Title = "Fernsehzeit",
            Description = "Bildschirmzeit nach dem Lernen – täglich abrufbar.",
            UnitType = UnitType.Minute,
            ActionType = ActionType.TV,
            Listings =
            [
                new ShopListing
                {
                    Title = "10 Minuten TV",
                    CoinPrice = 50,
                    GemPrice = 0,
                    UnitsPerPurchase = 10,
                    CurrentStock = 3,
                    MaxStock = 3,
                    RefillKind = ShopRefillKind.Daily,
                },
                new ShopListing
                {
                    Title = "30 Minuten TV",
                    CoinPrice = 130,
                    GemPrice = 0,
                    UnitsPerPurchase = 30,
                    CurrentStock = 1,
                    MaxStock = 1,
                    RefillKind = ShopRefillKind.Daily,
                },
            ],
        };

        // ── Article 2: play time ───────────────────────────────────────────────
        // A weekly allowance (refilled on Mondays), higher coin cost.
        var gaming = new ShopArticle
        {
            AdultId = father.Id,
            ArticleNumber = "GAME-001",
            Title = "Spielzeit",
            Description = "Konsolen- oder PC-Spielzeit; wöchentliches Budgetmodell.",
            UnitType = UnitType.Minute,
            ActionType = ActionType.Zocken,
            Listings =
            [
                new ShopListing
                {
                    Title = "30 Minuten Zocken",
                    CoinPrice = 200,
                    GemPrice = 0,
                    UnitsPerPurchase = 30,
                    CurrentStock = 3,
                    MaxStock = 3,
                    RefillKind = ShopRefillKind.Weekly,
                    RefillDayOfWeek = DayOfWeek.Monday,
                },
                new ShopListing
                {
                    Title = "60 Minuten Zocken",
                    CoinPrice = 350,
                    GemPrice = 0,
                    UnitsPerPurchase = 60,
                    CurrentStock = 1,
                    MaxStock = 1,
                    RefillKind = ShopRefillKind.Weekly,
                    RefillDayOfWeek = DayOfWeek.Monday,
                },
            ],
        };

        // ── Article 3: sweets ─────────────────────────────────────────────
        // Gram-based; a mixed price (coins + gems), no auto refill.
        // Shows that gems can make an article more exclusive.
        var sweets = new ShopArticle
        {
            AdultId = father.Id,
            ArticleNumber = "SWEET-001",
            Title = "Süßigkeiten",
            Description = "Kleine Nascherei als Lernanreiz – z. B. Gummibären oder Schokolade.",
            UnitType = UnitType.Gramm,
            ActionType = ActionType.Suessigkeit,
            Listings =
            [
                new ShopListing
                {
                    Title = "50 g Naschpaket",
                    CoinPrice = 300,
                    GemPrice = 10,
                    UnitsPerPurchase = 50,
                    CurrentStock = 4,
                    MaxStock = 4,
                    RefillKind = ShopRefillKind.None,
                },
            ],
        };

        // ── Article 4: cinema trip ────────────────────────────────────────────
        // A unit count (times), no auto refill, a high price → a long-term savings goal.
        var cinema = new ShopArticle
        {
            AdultId = father.Id,
            ArticleNumber = "EVENT-001",
            Title = "Kino-Ausflug",
            Description = "Gemeinsam ins Kino – der Sohn sucht den Film aus.",
            UnitType = UnitType.Mal,
            ActionType = ActionType.Ausflug,
            Listings =
            [
                new ShopListing
                {
                    Title = "1 Kinoabend",
                    CoinPrice = 1500,
                    GemPrice = 0,
                    UnitsPerPurchase = 1,
                    CurrentStock = 1,
                    MaxStock = 1,
                    RefillKind = ShopRefillKind.None,
                },
            ],
        };

        db.ShopArticles.AddRange(tv, gaming, sweets, cinema);
        db.SaveChanges();
    }
    private static void SeedAdmin(PuglingDbContext db)
    {
        if (db.Adults.Any()) return;

        var father = new Adult { Name = "Papa", Email = "papa@example.com", Pin = Auth.PinHasher.Hash("0000") };
        var child = new Child
        {
            Name = "Sohn",
            BirthYear = 2015,
            // Exercise-independent profile: preferences a later generator embeds the (fixed) subject matter in,
            // plus a free-text hint. See wiki/09-llm-kochbuch.md.
            Gender = Gender.Male,
            Interests = ["Brawl Stars", "Pokémon", "Fußball"],
            ProfileNotes = "Motiviert über Spiele-Themen; braucht klare kurze Aufgaben.",
            Pin = Auth.PinHasher.Hash("1111"),
            // Start: a few coins (Base → coins) for listings and a few gems (Achievement → gems), so that a skin
            // can be tried out right away.
            PointsEntries =
            {
                new ChildPointsEntry { Amount = 50, Kind = PointKind.Base, Reason = "Startguthaben (Münzen)" },
                new ChildPointsEntry { Amount = 300, Kind = PointKind.Achievement, Reason = "Willkommens-Gems" },
            }
        };
        db.Adults.Add(father);
        db.Children.Add(child);
        db.SaveChanges();
        // Supervision father → child (replaces the former Child.AdultId binding).
        db.SupervisorLinks.Add(new SupervisorLink { SupervisorId = father.Id, StudentId = child.Id, Relation = SupervisorRelation.Father });
        db.SaveChanges();
    }

    /// <summary>
    /// Since B-106: exercises hang off a textbook series unit, not a chapter (T-01/T-03). "Green Line 1"
    /// (Klett) is the real, catalogued series for Englisch - it echoes the freetext <c>Textbook</c> entry
    /// <see cref="SeedStudentProfile"/> already creates, so the seed tells one consistent story instead of
    /// two. The other subjects get one pauschal series/unit each, just enough to carry their exercises.
    /// </summary>
    private static void SeedCatalog(PuglingDbContext db)
    {
        if (db.Subjects.Any()) return;

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        string Json<T>(T config) => JsonSerializer.Serialize(config, options);

        // Subject-dependent categories (a controlled vocabulary) as the basis of the study plan pre-filtering.
        var enVokabeln = new ExerciseCategory { Name = "Vokabeln" };
        var enGrammatik = new ExerciseCategory { Name = "Grammatik" };
        // "Lesetexte", not "Leseverstehen" (B-163): the exercise type "Leseverständnis" is a legitimate
        // competence name, so the near-collision yields on the category side. A seed change only reaches
        // fresh databases - which is no shortcoming here, because the seed IS their content.
        var enLesetexte = new ExerciseCategory { Name = "Lesetexte" };
        var englisch = new Subject { Name = "Englisch", Categories = { enVokabeln, enGrammatik, enLesetexte } };

        var maGrundrechenarten = new ExerciseCategory { Name = "Grundrechenarten" };
        var maAlgebra = new ExerciseCategory { Name = "Algebra" };
        var mathe = new Subject { Name = "Mathe", Categories = { maGrundrechenarten, maAlgebra } };

        var erdkunde = new Subject { Name = "Erdkunde" };

        db.Subjects.AddRange(englisch, mathe, erdkunde);
        db.SaveChanges();

        var klett = new Publisher { Name = "Klett", Slug = "klett" };

        // The real, catalogued series: units mirror the two chapters the catalog used to carry directly.
        var greenLine = new TextbookSeries
        {
            Name = "Green Line 1",
            Slug = "green-line-1",
            Publisher = klett,
            SubjectId = englisch.Id,
            SubjectName = "Englisch",
            SourceLanguage = "en",
            TargetLanguage = "de",
        };
        db.TextbookSeries.Add(greenLine);
        db.SaveChanges();

        var greetingsUnit = new SeriesUnit { SeriesId = greenLine.Id, Grade = 5, OrderIndex = 1, Label = "Unit 1 – Greetings" };
        var familyUnit = new SeriesUnit { SeriesId = greenLine.Id, Grade = 5, OrderIndex = 2, Label = "Unit 2 – Family" };
        db.SeriesUnits.AddRange(greetingsUnit, familyUnit);

        // Pauschal series/unit per remaining subject - just a carrier for their exercises (B-106 T-03), no
        // claim to lehrwerk-typical differentiation.
        var matheSeries = new TextbookSeries { Name = "Mathe-Sammlung", Slug = "mathe-sammlung", SubjectId = mathe.Id, SubjectName = "Mathe" };
        var erdkundeSeries = new TextbookSeries { Name = "Erdkunde-Sammlung", Slug = "erdkunde-sammlung", SubjectId = erdkunde.Id, SubjectName = "Erdkunde" };
        db.TextbookSeries.AddRange(matheSeries, erdkundeSeries);
        db.SaveChanges();

        var einmaleinsUnit = new SeriesUnit { SeriesId = matheSeries.Id, OrderIndex = 1, Label = "Einmaleins" };
        var deutschlandUnit = new SeriesUnit { SeriesId = erdkundeSeries.Id, OrderIndex = 1, Label = "Deutschland" };
        db.SeriesUnits.AddRange(einmaleinsUnit, deutschlandUnit);
        db.SaveChanges();

        var englischExercises = new List<Exercise>
        {
            new()
            {
                SeriesUnitId = greetingsUnit.Id,
                Type = ExerciseTypeKeys.Vocabulary,
                Title = "Begrüßungen",
                OrderIndex = 1,
                RewardPoints = 10,
                GradeMin = 5, GradeMax = 6,
                SchoolTypes = SchoolTypes.Realschule | SchoolTypes.Gymnasium,
                Source = "Green Line 1, Unit 1",
                Category = enVokabeln,
                ConfigJson = Json(new VocabularyConfig
                {
                    Direction = "front-to-back",
                    SourceLang = "en",
                    TargetLang = "de",
                    Items =
                    {
                        new VocabItem("hello", "hallo"),
                        new VocabItem("goodbye", "auf Wiedersehen"),
                        new VocabItem("please", "bitte", "Höflichkeit"),
                    }
                }),
            },
            new()
            {
                SeriesUnitId = greetingsUnit.Id,
                Type = ExerciseTypeKeys.Cloze,
                Title = "Lückentext: A short dialogue",
                OrderIndex = 2,
                RewardPoints = 15,
                GradeMin = 5, GradeMax = 7,
                SchoolTypes = SchoolTypes.Realschule | SchoolTypes.Gymnasium,
                Source = "Green Line 1, Unit 1",
                Category = enGrammatik,
                ConfigJson = Json(new ClozeConfig
                {
                    Text = "A: {{1}}, how are you? B: I'm {{2}}, thank you.",
                    Gaps =
                    {
                        new Gap(1, "Hello", new List<string> { "Hi" }),
                        new Gap(2, "fine", new List<string> { "good", "well" }),
                    },
                    WordBank = new List<string> { "Hello", "Hi", "fine", "good", "well" },
                }),
            },
            // Birkenbihl: word-for-word decoding (grammar-independent) + the natural translation.
            new()
            {
                SeriesUnitId = greetingsUnit.Id,
                Type = ExerciseTypeKeys.Birkenbihl,
                Title = "Birkenbihl: Getting to know each other",
                OrderIndex = 3,
                RewardPoints = 10,
                GradeMin = 5, GradeMax = 8,
                SchoolTypes = SchoolTypes.Gymnasium,
                Category = enLesetexte,
                ConfigJson = Json(new BirkenbihlConfig
                {
                    LearningLang = "en",
                    NativeLang = "de",
                    NextSentenceId = 3,
                    NextWordId = 9,
                    Sentences =
                    {
                        new BirkenbihlSentence(1, "What is your name?", "Wie heißt du?",
                            [new WordPair(1, "What", "Was", null), new WordPair(2, "is", "ist", null),
                             new WordPair(3, "your", "dein", null), new WordPair(4, "name", "Name", null)]),
                        new BirkenbihlSentence(2, "Where do you live?", "Wo wohnst du?",
                            [new WordPair(5, "Where", "Wo", null), new WordPair(6, "do", "tust", null),
                             new WordPair(7, "you", "du", null), new WordPair(8, "live", "wohnen", null)]),
                    }
                }),
            },
        };

        var matheExercises = new List<Exercise>
        {
            // Fixed tasks: a manually maintained list (like vocabulary).
            new()
            {
                SeriesUnitId = einmaleinsUnit.Id,
                Type = ExerciseTypeKeys.Arithmetic,
                Title = "Das kleine 1×1 (7er-Reihe)",
                OrderIndex = 1,
                RewardPoints = 10,
                GradeMin = 3, GradeMax = 5,
                SchoolTypes = SchoolTypes.None,
                Category = maGrundrechenarten,
                ConfigJson = Json(new ArithmeticConfig
                {
                    Problems =
                    {
                        new ArithmeticProblem("7 × 6", 42),
                        new ArithmeticProblem("7 × 8", 56),
                        new ArithmeticProblem("63 ÷ 9", 7),
                    }
                }),
            },
            // Random tasks: the rules are stored, the tasks are generated on demand by
            // POST …/arithmetic-drill/{id}/generate.
            new()
            {
                SeriesUnitId = einmaleinsUnit.Id,
                Type = ExerciseTypeKeys.ArithmeticDrill,
                Title = "Kopfrechnen bis 20",
                OrderIndex = 2,
                RewardPoints = 15,
                GradeMin = 2, GradeMax = 4,
                SchoolTypes = SchoolTypes.None,
                Category = maGrundrechenarten,
                ConfigJson = Json(new ArithmeticDrillConfig
                {
                    Operations = new() { ArithmeticOperation.Addition, ArithmeticOperation.Subtraction },
                    MinOperand = 1,
                    MaxOperand = 20,
                    ProblemCount = 10,
                    AllowNegativeResults = false,
                }),
            },
        };

        // Federal state -> capital (the basis for the list AND the matching pairs).
        (string Land, string Hauptstadt)[] laender =
        [
            ("Baden-Württemberg", "Stuttgart"), ("Bayern", "München"), ("Berlin", "Berlin"),
            ("Brandenburg", "Potsdam"), ("Bremen", "Bremen"), ("Hamburg", "Hamburg"),
            ("Hessen", "Wiesbaden"), ("Mecklenburg-Vorpommern", "Schwerin"),
            ("Niedersachsen", "Hannover"), ("Nordrhein-Westfalen", "Düsseldorf"),
            ("Rheinland-Pfalz", "Mainz"), ("Saarland", "Saarbrücken"), ("Sachsen", "Dresden"),
            ("Sachsen-Anhalt", "Magdeburg"), ("Schleswig-Holstein", "Kiel"), ("Thüringen", "Erfurt"),
        ];

        var erdkundeExercises = new List<Exercise>
        {
            // List: enumerate all federal states (order does not matter).
            new()
            {
                SeriesUnitId = deutschlandUnit.Id,
                Type = ExerciseTypeKeys.List,
                Title = "Die 16 Bundesländer",
                OrderIndex = 1,
                RewardPoints = 15,
                ConfigJson = Json(new ListConfig
                {
                    Instruction = "Nenne alle 16 Bundesländer.",
                    Items = laender.Select(l => new ListEntry(l.Land)).ToList(),
                }),
            },
            // Matching by the flashcard principle: federal state -> capital.
            new()
            {
                SeriesUnitId = deutschlandUnit.Id,
                Type = ExerciseTypeKeys.Matching,
                Title = "Bundesland → Landeshauptstadt",
                OrderIndex = 2,
                RewardPoints = 20,
                ConfigJson = Json(new MatchingConfig
                {
                    Instruction = "Ordne jedem Bundesland seine Landeshauptstadt zu.",
                    Pairs = laender.Select(l => new MatchPair(l.Land, l.Hauptstadt)).ToList(),
                }),
            },
        };

        db.Exercises.AddRange(englischExercises);
        db.Exercises.AddRange(matheExercises);
        db.Exercises.AddRange(erdkundeExercises);
        db.SaveChanges();
    }
}
