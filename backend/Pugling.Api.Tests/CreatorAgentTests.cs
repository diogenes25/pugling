using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Pugling.Agent.Creator;
using Pugling.Agent.Creator.Briefing;
using Pugling.Agent.Creator.Drafting;
using Pugling.Client;

namespace Pugling.Api.Tests;

/// <summary>
/// The AI Creator against the real API (in-process) with a <see cref="FakeChatClient"/> instead of Ollama.
/// The tests document the part that must be deterministic: that a clean draft turns into a playable,
/// self-tested exercise – and that an unclean one does not make it into the catalog.
/// </summary>
public class CreatorAgentTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // Konto 1 = Papa: Creator (Katalog anlegen) UND Supervisor von Kind 1 (Profil/Lernstand lesen).
    private HttpClient Authenticated() =>
        factory.CreateDefaultClient(AuthHandler.Standalone(new PuglingClientOptions
        {
            BaseUrl = "http://localhost",
            AccountId = 1,
            Pin = "0000",
        }));

    /// <summary>Builds the pipeline with all four types and a model that returns the given responses.</summary>
    private (CreatorPipeline Pipeline, CreatorApi Creator, FakeChatClient Chat) BuildAgent(params string[] responses)
    {
        var creator = new CreatorApi(Authenticated());
        var supervisor = new SupervisorApi(Authenticated());
        var student = new StudentApi(Authenticated());
        var chat = new FakeChatClient(responses);
        var options = Options.Create(new AgentOptions { RepairAttempts = 1 });

        IExerciseStrategy[] strategies =
        [
            new VocabularyStrategy(chat, creator, options, NullLogger<VocabularyStrategy>.Instance),
            new ClozeStrategy(chat, creator, options, NullLogger<ClozeStrategy>.Instance),
            new TranslationStrategy(chat, creator, options, NullLogger<TranslationStrategy>.Instance),
            new GrammarStrategy(chat, creator, options, NullLogger<GrammarStrategy>.Instance),
        ];

        return (new CreatorPipeline(new BriefingBuilder(creator, supervisor, student), strategies), creator, chat);
    }

    /// <summary>The console verbs with the same underpinning as the real agent (incl. class-test planner).</summary>
    private AgentCommands Commands(CreatorApi creator, CreatorPipeline pipeline) =>
        new(creator, pipeline, new ExamPlanner(pipeline, creator, new SupervisorApi(Authenticated())));

    /// <summary>Creates an empty subject with a chapter so that every test stands on its own.</summary>
    private static async Task<(int SubjectId, int ChapterId)> FreshChapterAsync(CreatorApi creator, string name)
    {
        var subject = await creator.CreateSubjectAsync(new CreateSubjectDto($"Agent-Test {name} {Guid.NewGuid():N}"));
        var chapter = await creator.CreateChapterAsync(subject.Id, new CreateChapterDto("Unit 1", 1));
        return (subject.Id, chapter.Id);
    }

    private static GenerationRequest Request(int subjectId, int chapterId, string type,
        int count = 3, IReadOnlyList<string>? words = null, bool dryRun = false, bool strict = true,
        int? childId = 1, int? profileId = null, int? unitId = null, bool general = false) =>
        new(ChildId: childId, ProfileId: profileId, UnitId: unitId, General: general,
            SubjectId: subjectId, ChapterId: chapterId, TypeKey: type,
            Topic: "Unit 1: Animals", ItemCount: count, Words: words ?? [], UseWeakWords: false,
            SourceLang: "en", TargetLang: "de", RewardPoints: 10, DryRun: dryRun, Strict: strict);

    private const string VocabularyJson = """
        {"title":"Tiere auf dem Bauernhof","items":[
          {"front":"the horse","back":"das Pferd","hint":"Reiten"},
          {"front":"the sheep","back":"das Schaf","hint":null},
          {"front":"the goat","back":"die Ziege","hint":null}]}
        """;

    [Fact]
    public async Task Vokabeluebung_entsteht_aus_dem_Entwurf_und_besteht_den_Selbsttest()
    {
        var (pipeline, creator, chat) = BuildAgent(VocabularyJson);
        var (subjectId, chapterId) = await FreshChapterAsync(creator, "Vokabeln");

        var (briefing, outcome) = await pipeline.CreateAsync(Request(subjectId, chapterId, "Vocabulary"));

        Assert.Empty(outcome.Violations);
        Assert.True(outcome.Published, $"Selbsttest: {outcome.SelfTestPercent} %");
        Assert.Equal(1, chat.Calls);
        Assert.Equal("Tiere auf dem Bauernhof", outcome.Title);

        // Die Wortpaare müssen als eigene Item-Ebene im Katalog stehen und im Vokabelspeicher verlinkt sein.
        var items = await creator.ListItemsAsync(subjectId, chapterId, outcome.ExerciseId!.Value);
        Assert.Equal(3, items.Count);
        Assert.All(items, i => Assert.True(i.VocabularyId > 0));
        Assert.Contains(items, i => i.Front == "the horse" && i.Back == "das Pferd");

        // Die Metadaten kommen aus dem Briefing – daran findet der Supervisor die Übung später wieder.
        var detail = await creator.GetExerciseDetailAsync(outcome.ExerciseId.Value);
        Assert.Equal(briefing.Grade, detail.GradeMin);
        Assert.Contains(briefing.Audience, detail.Description);
    }

    [Fact]
    public async Task Trockenlauf_plant_die_Uebung_ohne_sie_anzulegen()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, chapterId) = await FreshChapterAsync(creator, "Trockenlauf");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, chapterId, "Vocabulary", dryRun: true));

        Assert.True(outcome.DraftAccepted);
        Assert.Null(outcome.ExerciseId);
        Assert.Contains("das Pferd", outcome.DraftJson);
        Assert.Empty(await creator.SearchExercisesAsync(subjectId: subjectId));
    }

    [Fact]
    public async Task Ein_fehlerhafter_Entwurf_geht_mit_den_Verstoessen_zurueck_ans_Modell()
    {
        // Erster Versuch: eine Vokabel zu wenig und eine Dublette – beides deterministisch prüfbar.
        const string broken = """
            {"title":"Tiere","items":[
              {"front":"the horse","back":"das Pferd","hint":null},
              {"front":"the horse","back":"das Pferd","hint":null}]}
            """;
        var (pipeline, creator, chat) = BuildAgent(broken, VocabularyJson);
        var (subjectId, chapterId) = await FreshChapterAsync(creator, "Reparatur");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, chapterId, "Vocabulary"));

        Assert.Equal(2, chat.Calls);
        Assert.True(outcome.Published);
        // Die Reparatur-Runde muss die konkreten Verstöße genannt haben, nicht nur „nochmal".
        Assert.Contains(chat.LastMessages, m => m.Text.Contains("doppelt vor"));
    }

    [Fact]
    public async Task Ein_dauerhaft_fehlerhafter_Entwurf_landet_nicht_im_Katalog()
    {
        const string broken = """
            {"title":"Tiere","items":[{"front":"the horse","back":"the horse","hint":null}]}
            """;
        var (pipeline, creator, _) = BuildAgent(broken);
        var (subjectId, chapterId) = await FreshChapterAsync(creator, "Ablehnung");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, chapterId, "Vocabulary"));

        Assert.False(outcome.DraftAccepted);
        Assert.Null(outcome.ExerciseId);
        Assert.Contains(outcome.Violations, v => v.Contains("Zu wenige Aufgaben"));
        Assert.Contains(outcome.Violations, v => v.Contains("identisch"));
        Assert.Empty(await creator.SearchExercisesAsync(subjectId: subjectId));
    }

    [Fact]
    public async Task Der_Pflicht_Wortschatz_darf_nicht_ausgetauscht_werden()
    {
        // Das „Modell" ersetzt zwei vorgegebene Wörter durch eigene – genau der Fall, den die
        // Kernregel verbietet (Interessen kleiden ein, sie ändern den Stoff nicht).
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, chapterId) = await FreshChapterAsync(creator, "Wortschatz");

        var (_, outcome) = await pipeline.CreateAsync(
            Request(subjectId, chapterId, "Vocabulary", words: ["the horse", "the tractor", "the barn"]));

        Assert.False(outcome.DraftAccepted);
        var violation = Assert.Single(outcome.Violations, v => v.Contains("Pflicht-Wortschatz"));
        Assert.Contains("the tractor", violation);
        Assert.Contains("the barn", violation);
    }

    [Fact]
    public async Task Bekannte_Vokabeln_werden_verlinkt_statt_dupliziert()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, chapterId) = await FreshChapterAsync(creator, "Dedupe");
        await creator.CreateVocabularyAsync(
            new CreateVocabularyDto(null, "en", "de", "the horse", "das Pferd", PartOfSpeech.Noun));

        // Der Vokabelspeicher ist – anders als Fach und Kapitel – **klassenweit geteilt**: frühere Tests
        // dieser Klasse spielen denselben `VocabularyJson` durch und haben „the horse" womöglich längst
        // materialisiert. Die Aussage darf darum nicht „zeigt auf *meine* Zeile" sein (dann hängt der Test
        // an der Ausführungsreihenfolge), sondern genau die, die der Name verspricht: **keine neue Zeile,
        // Verweis auf eine bestehende.**
        var before = await KnownHorsesAsync(creator);
        Assert.NotEmpty(before);

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, chapterId, "Vocabulary"));

        Assert.Equal(before, await KnownHorsesAsync(creator));
        var items = await creator.ListItemsAsync(subjectId, chapterId, outcome.ExerciseId!.Value);
        Assert.Contains(Assert.Single(items, i => i.Front == "the horse").VocabularyId, before);
    }

    /// <summary>The ids of all "the horse" rows (en→de) in the shared store – the baseline for the dedupe check.</summary>
    private static async Task<HashSet<int>> KnownHorsesAsync(CreatorApi creator) =>
        [.. (await creator.SearchVocabularyAsync(word: "the horse", sourceLanguage: "en", targetLanguage: "de"))
            .Where(v => v.Word == "the horse")
            .Select(v => v.Id)];

    [Fact]
    public async Task Lueckentext_entsteht_und_besteht_den_Selbsttest()
    {
        const string cloze = """
            {"title":"Auf dem Bauernhof","text":"Every morning Tom feeds the {{1}} and the {{2}}. Then he cleans the {{3}}.",
             "gaps":[{"index":1,"answer":"horse","alternatives":null},
                     {"index":2,"answer":"sheep","alternatives":null},
                     {"index":3,"answer":"stable","alternatives":null}],
             "wordBank":["horse","sheep","stable","tractor","fence"]}
            """;
        var (pipeline, creator, _) = BuildAgent(cloze);
        var (subjectId, chapterId) = await FreshChapterAsync(creator, "Cloze");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, chapterId, "Cloze"));

        Assert.Empty(outcome.Violations);
        Assert.True(outcome.Published, $"Selbsttest: {outcome.SelfTestPercent} %");
    }

    [Fact]
    public async Task Ein_Lueckentext_ohne_passende_Platzhalter_wird_abgelehnt()
    {
        // Lücke 3 hat keinen Platzhalter, Platzhalter {{4}} keine Lücke, und die Lösung steht im Text.
        const string cloze = """
            {"title":"Kaputt","text":"Tom feeds the {{1}} and the horse. Then he cleans the {{4}}.",
             "gaps":[{"index":1,"answer":"horse","alternatives":null},
                     {"index":2,"answer":"sheep","alternatives":null},
                     {"index":3,"answer":"stable","alternatives":null}],
             "wordBank":["horse","sheep","stable"]}
            """;
        var (pipeline, creator, _) = BuildAgent(cloze);
        var (subjectId, chapterId) = await FreshChapterAsync(creator, "Cloze-kaputt");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, chapterId, "Cloze"));

        Assert.False(outcome.DraftAccepted);
        Assert.Contains(outcome.Violations, v => v.Contains("{{4}}"));
        Assert.Contains(outcome.Violations, v => v.Contains("keinen Platzhalter"));
        Assert.Contains(outcome.Violations, v => v.Contains("verraten"));
        Assert.Empty(await creator.SearchExercisesAsync(subjectId: subjectId));
    }

    /// <summary>
    /// A model is allowed to omit any field – <c>{"title":"Tiere"}</c> is valid JSON. The draft records
    /// declare their fields non-nullable, so the deserializer plugs in <c>null</c>. The validator must
    /// turn that into a <i>rule violation</i>: previously it crashed with a
    /// <see cref="NullReferenceException"/> and the agent died with a stack trace at exactly the
    /// point where it is supposed to trigger the repair round.
    /// </summary>
    [Fact]
    public async Task Ein_Entwurf_mit_fehlenden_Feldern_wird_abgelehnt_statt_den_Agenten_zu_sprengen()
    {
        // Erst fehlt die ganze Liste, dann fehlen in jedem Eintrag die Vorderseiten.
        var (pipeline, creator, _) = BuildAgent(
            """{"title":"Tiere"}""",
            """{"title":"Tiere","items":[{"back":"das Pferd"},{"back":"das Schaf"},{"back":"die Ziege"}]}""");
        var (subjectId, chapterId) = await FreshChapterAsync(creator, "Nullfelder");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, chapterId, "Vocabulary"));

        Assert.False(outcome.DraftAccepted);
        Assert.Contains(outcome.Violations, v => v.Contains("Vorderseite"));
        Assert.Empty(await creator.SearchExercisesAsync(subjectId: subjectId));
    }

    /// <summary>
    /// The same placeholder twice in the text. A set difference (<c>Except</c>) does not notice this
    /// because it deduplicates – but the server then renders one more field than there are solutions,
    /// and the child gets a box that can never be answered correctly.
    /// </summary>
    [Fact]
    public async Task Ein_doppelter_Platzhalter_wird_abgelehnt()
    {
        const string cloze = """
            {"title":"Doppelt","text":"Tom feeds the {{1}}, then the {{2}} and the {{2}}.",
             "gaps":[{"index":1,"answer":"horse","alternatives":null},
                     {"index":2,"answer":"sheep","alternatives":null},
                     {"index":3,"answer":"stable","alternatives":null}],
             "wordBank":["horse","sheep","stable"]}
            """;
        var (pipeline, creator, _) = BuildAgent(cloze);
        var (subjectId, chapterId) = await FreshChapterAsync(creator, "Cloze-doppelt");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, chapterId, "Cloze"));

        Assert.False(outcome.DraftAccepted);
        Assert.Contains(outcome.Violations, v => v.Contains("{{2}} steht 2×"));
        Assert.Empty(await creator.SearchExercisesAsync(subjectId: subjectId));
    }

    /// <summary>
    /// <c>--chapter</c> without <c>--subject</c> used to be silently dropped: the exercise ended up in
    /// the first chapter of the first subject. A silent reach into the wrong shelf is worse than an
    /// error message.
    /// </summary>
    [Fact]
    public async Task Ein_unbekanntes_Kapitel_wird_gemeldet_statt_still_ins_erste_zu_schreiben()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        // Es gibt Fächer mit Kapiteln – ohne die Prüfung würde genau eines davon stumm gewählt.
        await FreshChapterAsync(creator, "Kapitel-Wahl");
        var commands = Commands(creator, pipeline);

        var command = CommandLine.Parse(["briefing", "--child", "1", "--chapter", "999999"]);
        var error = await Assert.ThrowsAsync<AgentUsageException>(() => commands.RunAsync(command, default));

        Assert.Contains("999999", error.Message);
    }

    /// <summary>
    /// A typed <c>help</c> is a verb. The previous start condition of the options loop confused
    /// "no verb given" with "verb is help" and read <c>args[0]</c> again as an option –
    /// <c>pugling-creator help</c> aborted with "Unexpected argument 'help'" and exit code 2.
    /// </summary>
    [Fact]
    public void Getipptes_help_ist_ein_Verb_und_keine_Option()
    {
        Assert.Equal("help", CommandLine.Parse(["help"]).Verb);

        // Ohne Verb gilt weiterhin help – und die Optionen werden ab dem ersten Argument gelesen.
        var line = CommandLine.Parse(["--child", "7"]);
        Assert.Equal("help", line.Verb);
        Assert.Equal(7, line.Int("child", 0));
    }

    [Fact]
    public async Task Uebersetzung_und_Grammatik_entstehen_ebenfalls_selbstgetestet()
    {
        const string translation = """
            {"title":"Sätze über Tiere","items":[
              {"source":"The horse is fast.","target":"Das Pferd ist schnell.","alternatives":null},
              {"source":"I feed the sheep.","target":"Ich füttere das Schaf.","alternatives":null},
              {"source":"We clean the stable.","target":"Wir putzen den Stall.","alternatives":null}]}
            """;
        const string grammar = """
            {"title":"Simple Present","instruction":"Setze das Verb in die richtige Form.","tasks":[
              {"prompt":"He ___ (to feed) the horse.","answer":"feeds","ruleHint":"3. Person Singular: -s"},
              {"prompt":"They ___ (to clean) the stable.","answer":"clean","ruleHint":"Plural ohne -s"},
              {"prompt":"She ___ (to ride) every day.","answer":"rides","ruleHint":"3. Person Singular: -s"}]}
            """;
        var (pipeline, creator, _) = BuildAgent(translation, grammar);
        var (subjectId, chapterId) = await FreshChapterAsync(creator, "Weitere Typen");

        var (_, translated) = await pipeline.CreateAsync(Request(subjectId, chapterId, "Translation"));
        var (_, grammarOutcome) = await pipeline.CreateAsync(Request(subjectId, chapterId, "Grammar"));

        Assert.True(translated.Published, $"Übersetzung – Selbsttest: {translated.SelfTestPercent} %");
        Assert.True(grammarOutcome.Published, $"Grammatik – Selbsttest: {grammarOutcome.SelfTestPercent} %");
    }

    [Fact]
    public async Task Das_Briefing_traegt_Profil_Interessen_und_Lehrbuch_in_den_Prompt()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, chapterId) = await FreshChapterAsync(creator, "Briefing");
        var supervisor = new SupervisorApi(Authenticated());
        await supervisor.UpdateChildAsync(1, new UpdateChildDto(null, null, null, null, null,
            Interests: ["Fußball", "Minecraft"]));
        await supervisor.CreateTextbookAsync(1,
            new CreateTextbookDto("Green Line 1", "Englisch", null, 5, "Klett", null, "Unit 1"));
        // Gewichtete Taxonomie neben dem Freitext – inklusive einer Abneigung.
        await supervisor.SetInterestsAsync(1, new SetChildInterestsDto(
            [new ChildInterestInput(3, Label: "Weltraum"), new ChildInterestInput(-3, Label: "Spinnen")]));

        var briefing = await pipeline.BriefAsync(Request(subjectId, chapterId, "Vocabulary"));
        var prompt = briefing.ToPromptText();

        Assert.Contains("Fußball", prompt);
        Assert.Contains("Minecraft", prompt);
        Assert.Contains("Green Line 1", prompt);
        Assert.Contains("Unit 1: Animals", prompt);
        Assert.Contains("Green Line 1", briefing.Source);

        // Die stärkste Vorliebe steht vor dem Freitext …
        Assert.Matches(@"Interessen \(wichtigste zuerst\): Weltraum", prompt);
        // … und die Abneigung wird als Verbot geführt, nicht als weiteres Thema.
        Assert.Contains("Vermeide unbedingt (Abneigungen): Spinnen", prompt);
        Assert.DoesNotContain("Interessen (wichtigste zuerst): Weltraum, Spinnen", prompt);
    }

    /// <summary>
    /// Creates a textbook series with a unit and a Creator profile optimized for it – the
    /// "subject teacher" in whose name the agent drafts.
    /// </summary>
    private static async Task<(TextbookSeriesResponse Series, SeriesUnitResponse Unit, CreatorProfileResponse Profile)>
        FreshProfileAsync(CreatorApi creator, int? subjectId, string name)
    {
        var series = await creator.CreateSeriesAsync(new CreateTextbookSeriesDto(
            $"Access {name} {Guid.NewGuid():N}", "Cornelsen", "Englisch", subjectId,
            SchoolTypes.Gymnasium, "en", "de", "Kompetenzorientiertes Lehrwerk mit Units je Halbjahr."));
        var unit = await creator.CreateUnitAsync(series.Id, new CreateSeriesUnitDto(
            "Unit 3 – Growing up", Grade: 8, OrderIndex: 3,
            Topics: "Familie, Freundschaft, Erwachsenwerden",
            Grammar: "Present perfect vs. simple past",
            VocabularyNotes: "to grow up, responsibility, to argue"));
        var profile = await creator.CreateProfileAsync(new CreateCreatorProfileDto(
            $"Englisch 8 Gymnasium – {name} {Guid.NewGuid():N}", "Englisch", subjectId,
            SchoolTypes.Gymnasium, GradeMin: 7, GradeMax: 8, SeriesId: series.Id,
            SourceLang: "en", TargetLang: "de",
            Persona: "Du bist Englischlehrer an einem bayerischen Gymnasium.",
            Didactics: "Kurze Sätze, maximal zwölf Wörter.", DefaultTypes: ["Vocabulary"], Active: true));

        return (series, unit, profile);
    }

    /// <summary>
    /// Its own vocabulary for the profile tests. Deliberately <b>not</b> <see cref="VocabularyJson"/>: the
    /// tests share a DB, and the vocabulary store is global – whoever creates the same pairs shifts the
    /// expectation of other tests (see the dedupe test, which checks against "its own" store id).
    /// </summary>
    private const string ProfileVocabularyJson = """
        {"title":"Erwachsen werden","items":[
          {"front":"to grow up","back":"aufwachsen","hint":"Unit 3"},
          {"front":"the responsibility","back":"die Verantwortung","hint":null},
          {"front":"to argue","back":"streiten","hint":null}]}
        """;

    /// <summary>
    /// Without a child, an exercise is created for the <b>shared catalog</b>: the metadata comes from the
    /// profile (a grade-level range instead of a single grade), and the prompt must not contain a child
    /// section – otherwise the model would invent preferences that belong to no one.
    /// </summary>
    [Fact]
    public async Task Eine_allgemeine_Uebung_entsteht_ohne_Kind_mit_den_Metadaten_des_Profils()
    {
        var (pipeline, creator, chat) = BuildAgent(ProfileVocabularyJson);
        var (subjectId, chapterId) = await FreshChapterAsync(creator, "Allgemein");
        var (series, unit, profile) = await FreshProfileAsync(creator, subjectId, "Allgemein");

        var request = Request(subjectId, chapterId, "Vocabulary", childId: null, profileId: profile.Id, unitId: unit.Id);
        var (briefing, outcome) = await pipeline.CreateAsync(request);

        Assert.True(outcome.Published, $"Selbsttest: {outcome.SelfTestPercent} %");
        Assert.False(briefing.Individual);

        // Der Prompt beschreibt den Lehrer und den Stoff, aber kein Kind.
        var prompt = briefing.ToPromptText();
        Assert.DoesNotContain("## Das Kind", prompt);
        Assert.Contains("Kein bestimmtes Kind", prompt);
        Assert.Contains(profile.Name, prompt);

        // Metadaten aus dem Profil: der ganze Klassenstufen-Bereich, die Schulart und die Quelle mit Unit.
        var detail = await creator.GetExerciseDetailAsync(outcome.ExerciseId!.Value);
        Assert.Equal(7, detail.GradeMin);
        Assert.Equal(8, detail.GradeMax);
        Assert.Equal(SchoolTypes.Gymnasium, detail.SchoolTypes);
        Assert.Contains(series.Name, detail.Source);
        Assert.Contains(unit.Label, detail.Source);
        Assert.Contains("gemeinsamen Katalog", detail.Description);

        // Die Persona des Profils steht im System-Prompt – vor den festen Regeln, die sie nicht aufweicht.
        var system = Assert.Single(chat.LastMessages, m => m.Role == ChatRole.System).Text;
        Assert.StartsWith("Du bist Englischlehrer", system);
        Assert.Contains("Kurze Sätze", system);
        Assert.Contains("Der Lernstoff ist vorgegeben und unveränderlich", system);
    }

    /// <summary>
    /// The actual gain of the unit level: the unit's topics, grammar and vocabulary are in the prompt.
    /// Without it, the model would have to invent the material the child has in class.
    /// </summary>
    [Fact]
    public async Task Der_Stoff_der_Unit_steht_im_Prompt()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, chapterId) = await FreshChapterAsync(creator, "Unit-Stoff");
        var (series, unit, profile) = await FreshProfileAsync(creator, subjectId, "Unit-Stoff");

        var briefing = await pipeline.BriefAsync(
            Request(subjectId, chapterId, "Vocabulary", childId: null, profileId: profile.Id, unitId: unit.Id));
        var prompt = briefing.ToPromptText();

        Assert.Contains($"Lehrwerk: {series.Name} (Cornelsen)", prompt);
        Assert.Contains("Unit 3 – Growing up", prompt);
        Assert.Contains("Present perfect vs. simple past", prompt);
        Assert.Contains("to grow up, responsibility, to argue", prompt);
        // Die Sprachen des Profils gelten, wenn die Kommandozeile keine nennt.
        Assert.Equal("en", briefing.SourceLang);
    }

    /// <summary>
    /// A unit from a foreign series would be worse than none – the model would take its material as
    /// established fact. That is why the request aborts instead of silently dropping the value.
    /// </summary>
    [Fact]
    public async Task Eine_Unit_aus_einer_fremden_Reihe_wird_gemeldet()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, chapterId) = await FreshChapterAsync(creator, "Fremde-Unit");
        var (_, _, profile) = await FreshProfileAsync(creator, subjectId, "Fremde-Unit");
        var (_, foreignUnit, _) = await FreshProfileAsync(creator, subjectId, "Fremdwerk");

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => pipeline.BriefAsync(
            Request(subjectId, chapterId, "Vocabulary", childId: null, profileId: profile.Id, unitId: foreignUnit.Id)));

        Assert.Contains(foreignUnit.Id.ToString(), error.Message);
    }

    /// <summary>
    /// The practice class test: multiple types on the same material, bundled into a planned class test
    /// with exactly these exercises. Only this turns individual exercises into exam preparation.
    /// </summary>
    [Fact]
    public async Task Eine_Uebungsklausur_erzeugt_mehrere_Uebungen_und_eine_Klassenarbeit()
    {
        const string cloze = """
            {"title":"Klausur-Lückentext","text":"Ben has {{1}} for his sister, never wants to {{2}} and has {{3}} up fast.",
             "gaps":[{"index":1,"answer":"responsibility","alternatives":null},
                     {"index":2,"answer":"argue","alternatives":null},
                     {"index":3,"answer":"grown","alternatives":null}],
             "wordBank":["responsibility","argue","grown","promise"]}
            """;
        var (pipeline, creator, _) = BuildAgent(ProfileVocabularyJson, cloze);
        var supervisor = new SupervisorApi(Authenticated());
        var (subjectId, chapterId) = await FreshChapterAsync(creator, "Klausur");
        var (_, unit, profile) = await FreshProfileAsync(creator, subjectId, "Klausur");

        var planner = new ExamPlanner(pipeline, creator, supervisor);
        var outcome = await planner.RunAsync(new ExamRequest(
            Request(subjectId, chapterId, "Vocabulary", profileId: profile.Id, unitId: unit.Id),
            Types: ["Vocabulary", "Cloze"], PerType: 3,
            ScheduledDate: new DateOnly(2026, 9, 15), Title: null));

        Assert.True(outcome.Complete, string.Join(" | ", outcome.Parts.Select(p => p.Error ?? p.Outcome?.Title)));
        Assert.Equal(2, outcome.ExerciseIds.Count);
        // Der Titel kommt aus der Unit – dieselbe Bezeichnung, die auch in der Quelle steht.
        Assert.Contains(unit.Label, outcome.Title);

        var classTest = await supervisor.GetClassTestAsync(outcome.ClassTestId!.Value);
        Assert.Equal(new DateOnly(2026, 9, 15), classTest.Klassenarbeit.ScheduledDate);
        Assert.Equal(KlassenarbeitStatus.Planned, classTest.Klassenarbeit.Status);
        Assert.Equal([.. outcome.ExerciseIds.Order()],
            [.. classTest.AssignedExercises.Select(e => e.Id).Order()]);
        // Der kind-skopierte Tag hält das Bündel auch außerhalb der Klassenarbeit zusammen.
        Assert.Contains(outcome.TagName, classTest.Klassenarbeit.Tags.Select(t => t.Name));
    }

    /// <summary>
    /// A failed part costs the class test one part, not all of them – and it is reported instead of
    /// passing off an incomplete test as finished.
    /// </summary>
    [Fact]
    public async Task Eine_Klausur_mit_einem_kaputten_Teil_bleibt_unvollstaendig()
    {
        // Die eine vorbereitete Antwort passt nur zum Vokabel-Teil; der Lückentext-Teil scheitert an den Regeln.
        var (pipeline, creator, _) = BuildAgent(ProfileVocabularyJson);
        var supervisor = new SupervisorApi(Authenticated());
        var (subjectId, chapterId) = await FreshChapterAsync(creator, "Klausur-kaputt");
        var (_, unit, profile) = await FreshProfileAsync(creator, subjectId, "Klausur-kaputt");

        var planner = new ExamPlanner(pipeline, creator, supervisor);
        var outcome = await planner.RunAsync(new ExamRequest(
            Request(subjectId, chapterId, "Vocabulary", profileId: profile.Id, unitId: unit.Id),
            Types: ["Vocabulary", "Cloze"], PerType: 3, ScheduledDate: null, Title: "Übungsklausur Test"));

        Assert.False(outcome.Complete);
        Assert.Single(outcome.ExerciseIds);
        Assert.Contains(outcome.Parts, p => p.TypeKey == "Cloze" && p.Outcome is { DraftAccepted: false });
        // Die gelungene Übung ist trotzdem angelegt und der Klassenarbeit zugewiesen.
        var classTest = await supervisor.GetClassTestAsync(outcome.ClassTestId!.Value);
        Assert.Single(classTest.AssignedExercises);
    }

    [Fact]
    public async Task Ein_unbekannter_Uebungstyp_scheitert_mit_klarer_Ansage()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, chapterId) = await FreshChapterAsync(creator, "Unbekannt");

        var error = await Assert.ThrowsAsync<AgentUsageException>(() =>
            pipeline.CreateAsync(Request(subjectId, chapterId, "Birkenbihl")));

        Assert.Contains("Vocabulary", error.Message);
    }
}
