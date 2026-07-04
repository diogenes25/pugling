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
        SeedKlassenarbeiten(db);
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

        var englisch = new Subject
        {
            Name = "Englisch",
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

        var mathe = new Subject
        {
            Name = "Mathe",
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
