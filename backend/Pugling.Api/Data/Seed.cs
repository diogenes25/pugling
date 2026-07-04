using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Data;

public static class Seed
{
    public static void Run(PuglingDbContext db)
    {
        SeedTimeSlots(db);
        SeedAdmin(db);
        SeedCatalog(db);
        SeedVocabulary(db);
        SeedFrench(db);
        SeedKlassenarbeiten(db);
        SeedGamification(db);
    }

    /// <summary>
    /// Französisch-Inhalte für den typischen Einstieg „Sohn (14 J.) hat Probleme in Französisch":
    /// ein Fach mit Kapitel + Katalog-Übungen (zum Stöbern/Filtern nach Klassenstufe) UND passende
    /// Einträge im Vokabel-Store (Basis für einen Vokabel-Lehrplan). Additiv-idempotent: läuft auch
    /// auf einer bereits befüllten DB nach (prüft gezielt auf das Fach bzw. je Vokabel-Key).
    /// </summary>
    private static void SeedFrench(PuglingDbContext db)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        string Json<T>(T config) => JsonSerializer.Serialize(config, options);

        // (fr -> de) Grundwortschatz „En ville / Le quotidien" – 8./9. Klasse, Découvertes 1, Unité 2.
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

        string Slug(string s) =>
            s.ToLowerInvariant().Replace("ß", "ss").Normalize(System.Text.NormalizationForm.FormD)
                .Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch)
                    != System.Globalization.UnicodeCategory.NonSpacingMark)
                .Aggregate(new System.Text.StringBuilder(), (sb, ch) => sb.Append(ch)).ToString()
                .Replace("'", " ").Trim();
        string Key(string word, string de) =>
            $"fr_{Slug(word).Replace(' ', '_')}_de_{Slug(de).Replace(' ', '_')}"
                .Replace("__", "_");

        foreach (var w in woerter)
        {
            var key = Key(w.Word, w.De);
            if (db.Vocabulary.Any(v => v.Key == key)) continue;
            db.Vocabulary.Add(new Vocabulary
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

        // Katalog: nur anlegen, wenn das Fach noch fehlt (sonst nur Store-Vokabeln ergänzen, s. o.).
        if (db.Subjects.Any(s => s.Name == "Französisch")) return;

        var frVokabeln = new ExerciseCategory { Name = "Vokabeln" };
        var frGrammatik = new ExerciseCategory { Name = "Grammatik" };

        var franzoesisch = new Subject
        {
            Name = "Französisch",
            Categories = { frVokabeln, frGrammatik },
            Chapters =
            {
                new Chapter
                {
                    Name = "Unité 2 – En ville",
                    OrderIndex = 1,
                    Exercises =
                    {
                        new Exercise
                        {
                            Type = ExerciseType.Vocabulary,
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
                        new Exercise
                        {
                            Type = ExerciseType.Cloze,
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
                    }
                },
            }
        };

        db.Subjects.Add(franzoesisch);
        db.SaveChanges();
    }

    /// <summary>
    /// Vorlagen für Missionen (Tages-/Wochenziele) und Auszeichnungen (Duolingo-artige Badges) je Kind.
    /// Der Vater kann sie frei editieren/löschen und eigene ergänzen (siehe Missions-/Achievements-Controller).
    /// </summary>
    private static void SeedGamification(PuglingDbContext db)
    {
        var child = db.Children.FirstOrDefault();
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

        // Beispiel-Prämien zum Einlösen (reale Belohnungen; eigener Guard, damit sie auch in
        // bereits geseedeten DBs nachgezogen werden).
        if (!db.Rewards.Any())
        {
            db.Rewards.AddRange(
                new Reward { ChildId = child.Id, Title = "30 Min Fernsehen", Cost = 200 },
                new Reward { ChildId = child.Id, Title = "1 Stunde Zocken", Cost = 400 },
                new Reward { ChildId = child.Id, Title = "Kinoabend aussuchen", Cost = 1500 });
            db.SaveChanges();
        }
    }

    private static void SeedKlassenarbeiten(PuglingDbContext db)
    {
        if (db.Klassenarbeiten.Any()) return;

        var child = db.Children.FirstOrDefault();
        if (child is null) return; // ohne Kind keine kindbezogenen Daten

        var englisch = db.Subjects.FirstOrDefault(s => s.Name == "Englisch");
        var mathe = db.Subjects.FirstOrDefault(s => s.Name == "Mathe");
        var exEnglisch = db.Exercises.Where(e => e.Chapter!.Subject!.Name == "Englisch").OrderBy(e => e.Id).ToList();
        var exMathe = db.Exercises.Where(e => e.Chapter!.Subject!.Name == "Mathe").OrderBy(e => e.Id).ToList();

        // Zwei Beispiel-Tags – einer vom Vater, einer vom Sohn gesetzt.
        var tagUnit1 = new Tag { ChildId = child.Id, Name = "Unit 1", Color = "#3b82f6", CreatedBy = TaggedBy.Vater };
        var tag1x1 = new Tag { ChildId = child.Id, Name = "Einmaleins", Color = "#f59e0b", CreatedBy = TaggedBy.Sohn };
        db.Tags.AddRange(tagUnit1, tag1x1);
        db.SaveChanges();

        foreach (var e in exEnglisch)
            db.ExerciseTags.Add(new ExerciseTag { TagId = tagUnit1.Id, ExerciseId = e.Id, TaggedByRole = TaggedBy.Vater });
        foreach (var e in exMathe)
            db.ExerciseTags.Add(new ExerciseTag { TagId = tag1x1.Id, ExerciseId = e.Id, TaggedByRole = TaggedBy.Sohn });
        db.SaveChanges();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Geplante Arbeit: relevante Übungen kommen über den verknüpften Tag „Unit 1".
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

        // Geschriebene Arbeit mit schlechter Note: Übungen direkt zugewiesen → tauchen im Wiederholen-Endpunkt auf.
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
        if (db.Vocabulary.Any()) return;

        // Substantiv + Verb-Grundform
        db.Vocabulary.AddRange(
            new Vocabulary
            {
                Key = "en_house_de_haus",
                SourceLanguage = "en", TargetLanguage = "de",
                Word = "house", Translation = "Haus",
                PartOfSpeech = PartOfSpeech.Noun,
                Noun = new NounInfo { Article = "das", Genus = Genus.Neuter, Plural = "Häuser" },
            },
            new Vocabulary
            {
                Key = "en_go_de_gehen",
                SourceLanguage = "en", TargetLanguage = "de",
                Word = "go", Translation = "gehen",
                PartOfSpeech = PartOfSpeech.Verb,
                Verb = new VerbInfo { IsBaseForm = true, Infinitive = "gehen" },
            });
        db.SaveChanges();

        // Flektierte Form, die auf die Grundform verweist
        var baseId = db.Vocabulary.Where(v => v.Key == "en_go_de_gehen").Select(v => v.Id).First();
        db.Vocabulary.Add(new Vocabulary
        {
            Key = "en_goes_de_geht",
            SourceLanguage = "en", TargetLanguage = "de",
            Word = "goes", Translation = "geht",
            PartOfSpeech = PartOfSpeech.Verb,
            Verb = new VerbInfo { IsBaseForm = false, Infinitive = "gehen", Tense = "present", Person = "3", Number = "singular" },
            BaseFormId = baseId,
        });
        db.SaveChanges();
    }

    private static void SeedTimeSlots(PuglingDbContext db)
    {
        if (db.TimeSlots.Any()) return;

        // Zeitfenster mit Punkte-Multiplikator für Leitner-Wiederholungen (vom Vater änderbar).
        db.TimeSlots.AddRange(
            new TimeSlotRule { Name = "Vormittag", StartTime = new(8, 0), EndTime = new(12, 0), Multiplier = 1.5 },
            new TimeSlotRule { Name = "Nachmittag", StartTime = new(12, 0), EndTime = new(18, 0), Multiplier = 1.0 },
            new TimeSlotRule { Name = "Abend", StartTime = new(18, 0), EndTime = new(21, 0), Multiplier = 0.8 });
        db.SaveChanges();
    }

    private static void SeedAdmin(PuglingDbContext db)
    {
        if (db.Fathers.Any()) return;

        db.Fathers.Add(new Father
        {
            Name = "Papa",
            Email = "papa@example.com",
            Pin = "0000",
            Children =
            {
                new Child
                {
                    Name = "Sohn",
                    BirthYear = 2015,
                    Pin = "1111",
                    PointsEntries = { new ChildPointsEntry { Amount = 50, Reason = "Startguthaben" } }
                }
            }
        });
        db.SaveChanges();
    }

    private static void SeedCatalog(PuglingDbContext db)
    {
        if (db.Subjects.Any()) return;

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        string Json<T>(T config) => JsonSerializer.Serialize(config, options);

        // Fachabhängige Arten (kontrolliertes Vokabular) als Grundlage der Lehrplan-Vorfilterung.
        var enVokabeln = new ExerciseCategory { Name = "Vokabeln" };
        var enGrammatik = new ExerciseCategory { Name = "Grammatik" };
        var enLeseverstehen = new ExerciseCategory { Name = "Leseverstehen" };

        var englisch = new Subject
        {
            Name = "Englisch",
            Categories = { enVokabeln, enGrammatik, enLeseverstehen },
            Chapters =
            {
                new Chapter
                {
                    Name = "Unit 1 – Greetings",
                    OrderIndex = 1,
                    Exercises =
                    {
                        new Exercise
                        {
                            Type = ExerciseType.Vocabulary,
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
                                Items =
                                {
                                    new VocabItem("hello", "hallo"),
                                    new VocabItem("goodbye", "auf Wiedersehen"),
                                    new VocabItem("please", "bitte", "Höflichkeit"),
                                }
                            }),
                        },
                        new Exercise
                        {
                            Type = ExerciseType.Cloze,
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
                        // Birkenbihl: Wort-für-Wort-Dekodierung (grammatik-unabhängig) + natürliche Übersetzung.
                        new Exercise
                        {
                            Type = ExerciseType.Birkenbihl,
                            Title = "Birkenbihl: Getting to know each other",
                            OrderIndex = 3,
                            RewardPoints = 10,
                            GradeMin = 5, GradeMax = 8,
                            SchoolTypes = SchoolTypes.Gymnasium,
                            Category = enLeseverstehen,
                            ConfigJson = Json(new BirkenbihlConfig
                            {
                                LearningLang = "Englisch",
                                NativeLang = "Deutsch",
                                Sentences =
                                {
                                    new BirkenbihlSentence(
                                        "What is your name?",
                                        [new WordPair("What", "Was"), new WordPair("is", "ist"),
                                         new WordPair("your", "dein"), new WordPair("name", "Name")],
                                        "Wie heißt du?"),
                                    new BirkenbihlSentence(
                                        "Where do you live?",
                                        [new WordPair("Where", "Wo"), new WordPair("do", "tust"),
                                         new WordPair("you", "du"), new WordPair("live", "wohnen")],
                                        "Wo wohnst du?"),
                                }
                            }),
                        },
                    }
                },
                new Chapter { Name = "Unit 2 – Family", OrderIndex = 2 },
            }
        };

        var maGrundrechenarten = new ExerciseCategory { Name = "Grundrechenarten" };
        var maAlgebra = new ExerciseCategory { Name = "Algebra" };

        var mathe = new Subject
        {
            Name = "Mathe",
            Categories = { maGrundrechenarten, maAlgebra },
            Chapters =
            {
                new Chapter
                {
                    Name = "Einmaleins",
                    OrderIndex = 1,
                    Exercises =
                    {
                        // Feste Aufgaben: manuell gepflegte Liste (wie Vokabeln).
                        new Exercise
                        {
                            Type = ExerciseType.Arithmetic,
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
                        // Zufallsaufgaben: gespeichert werden die Regeln, die Aufgaben
                        // erzeugt POST …/arithmetic-drill/{id}/generate auf Abruf.
                        new Exercise
                        {
                            Type = ExerciseType.ArithmeticDrill,
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
                    }
                },
            }
        };

        // Bundesland -> Landeshauptstadt (Grundlage für Liste UND Zuordnungs-Paare).
        (string Land, string Hauptstadt)[] laender =
        [
            ("Baden-Württemberg", "Stuttgart"), ("Bayern", "München"), ("Berlin", "Berlin"),
            ("Brandenburg", "Potsdam"), ("Bremen", "Bremen"), ("Hamburg", "Hamburg"),
            ("Hessen", "Wiesbaden"), ("Mecklenburg-Vorpommern", "Schwerin"),
            ("Niedersachsen", "Hannover"), ("Nordrhein-Westfalen", "Düsseldorf"),
            ("Rheinland-Pfalz", "Mainz"), ("Saarland", "Saarbrücken"), ("Sachsen", "Dresden"),
            ("Sachsen-Anhalt", "Magdeburg"), ("Schleswig-Holstein", "Kiel"), ("Thüringen", "Erfurt"),
        ];

        var erdkunde = new Subject
        {
            Name = "Erdkunde",
            Chapters =
            {
                new Chapter
                {
                    Name = "Deutschland",
                    OrderIndex = 1,
                    Exercises =
                    {
                        // Liste: alle Bundesländer aufzählen (Reihenfolge egal).
                        new Exercise
                        {
                            Type = ExerciseType.List,
                            Title = "Die 16 Bundesländer",
                            OrderIndex = 1,
                            RewardPoints = 15,
                            ConfigJson = Json(new ListConfig
                            {
                                Instruction = "Nenne alle 16 Bundesländer.",
                                Items = laender.Select(l => new ListEntry(l.Land)).ToList(),
                            }),
                        },
                        // Zuordnung nach Karteikasten-Prinzip: Bundesland -> Landeshauptstadt.
                        new Exercise
                        {
                            Type = ExerciseType.Matching,
                            Title = "Bundesland → Landeshauptstadt",
                            OrderIndex = 2,
                            RewardPoints = 20,
                            ConfigJson = Json(new MatchingConfig
                            {
                                Instruction = "Ordne jedem Bundesland seine Landeshauptstadt zu.",
                                Pairs = laender.Select(l => new MatchPair(l.Land, l.Hauptstadt)).ToList(),
                            }),
                        },
                    }
                },
            }
        };

        db.Subjects.AddRange(englisch, mathe, erdkunde);
        db.SaveChanges();
    }
}
