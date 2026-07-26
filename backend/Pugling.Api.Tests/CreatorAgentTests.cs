using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Pugling.Agent.Creator;
using Pugling.Agent.Creator.Briefing;
using Pugling.Agent.Creator.Drafting;
using Pugling.Client;

namespace Pugling.Api.Tests;

/// <summary>
/// Der KI-Creator gegen die echte API (In-Process) mit einem <see cref="FakeChatClient"/> statt Ollama.
/// Die Tests belegen den Teil, der deterministisch sein muss: dass ein sauberer Entwurf zu einer
/// spielbaren, selbstgetesteten Übung wird – und dass ein unsauberer es nicht in den Katalog schafft.
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

    /// <summary>Baut die Pipeline mit allen vier Typen und einem Modell, das die übergebenen Antworten liefert.</summary>
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

    /// <summary>Legt ein leeres Fach mit Kapitel an, damit jeder Test für sich steht.</summary>
    private static async Task<(int SubjectId, int ChapterId)> FreshChapterAsync(CreatorApi creator, string name)
    {
        var subject = await creator.CreateSubjectAsync(new CreateSubjectDto($"Agent-Test {name} {Guid.NewGuid():N}"));
        var chapter = await creator.CreateChapterAsync(subject.Id, new CreateChapterDto("Unit 1", 1));
        return (subject.Id, chapter.Id);
    }

    private static GenerationRequest Request(int subjectId, int chapterId, string type,
        int count = 3, IReadOnlyList<string>? words = null, bool dryRun = false, bool strict = true) =>
        new(ChildId: 1, SubjectId: subjectId, ChapterId: chapterId, TypeKey: type,
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
        Assert.Contains(briefing.Name, detail.Description);
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
        var existing = await creator.CreateVocabularyAsync(
            new CreateVocabularyDto(null, "en", "de", "the horse", "das Pferd", PartOfSpeech.Noun));

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, chapterId, "Vocabulary"));

        var items = await creator.ListItemsAsync(subjectId, chapterId, outcome.ExerciseId!.Value);
        Assert.Equal(existing.Id, Assert.Single(items, i => i.Front == "the horse").VocabularyId);
    }

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
