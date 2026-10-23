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
    // Account 1 = the father: creator (creating catalog content) AND supervisor of child 1 (reading profile/progress).
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

    /// <summary>The console verbs with the same underpinning as the real agent (incl. both planners).</summary>
    private AgentCommands Commands(CreatorApi creator, CreatorPipeline pipeline) =>
        new(creator, pipeline, new ExamPlanner(pipeline, creator, new SupervisorApi(Authenticated())),
            new PlanPlanner(pipeline, new SupervisorApi(Authenticated())));

    /// <summary>Creates an empty subject with a catalogued series/unit so that every test stands on its own.</summary>
    private static async Task<(int SubjectId, int SeriesId, int SeriesUnitId)> FreshUnitAsync(CreatorApi creator, string name)
    {
        var subject = await creator.CreateSubjectAsync(new CreateSubjectDto($"Agent-Test {name} {Guid.NewGuid():N}"));
        var series = await creator.CreateSeriesAsync(new CreateTextbookSeriesDto(
            $"Agent-Reihe {name} {Guid.NewGuid():N}", null, subject.Name, subject.Id, null, null, null, null));
        var unit = await creator.CreateUnitAsync(series.Id, new CreateSeriesUnitDto("Unit 1", null, 1, null, null, null, null));
        return (subject.Id, series.Id, unit.Id);
    }

    private static GenerationRequest Request(int subjectId, int seriesId, int seriesUnitId, string type,
        int count = 3, IReadOnlyList<string>? words = null, bool dryRun = false, bool strict = true,
        int? childId = 1, int? profileId = null, int? unitId = null, bool general = false,
        IReadOnlyList<string>? interests = null) =>
        new(ChildId: childId, ProfileId: profileId, UnitId: unitId, General: general,
            SubjectId: subjectId, SeriesId: seriesId, SeriesUnitId: seriesUnitId, TypeKey: type,
            Topic: "Unit 1: Animals", ItemCount: count, Words: words ?? [], UseWeakWords: false,
            SourceLang: "en", TargetLang: "de", RewardPoints: 10, Interests: interests ?? [],
            DryRun: dryRun, Strict: strict);

    private const string VocabularyJson = """
        {"title":"Tiere auf dem Bauernhof","items":[
          {"front":"the horse","back":"das Pferd","hint":"Reiten"},
          {"front":"the sheep","back":"das Schaf","hint":null},
          {"front":"the goat","back":"die Ziege","hint":null}]}
        """;

    /// <summary>
    /// <b>AK 3 of B-46.</b> With a child, the exercise records the child's <b>positively</b> weighted
    /// interests by itself - no extra decision by the caller. And it records only those: a dislike is
    /// precisely what the prompt tells the model to avoid, so tagging it would label the exercise with its
    /// own opposite.
    /// <para>
    /// Red probe: swap <c>child.WeightedInterests</c> for the unfiltered list in
    /// <c>ResolveInterestTagIdsAsync</c> and the last assertion falls; return <c>[]</c> there and the one
    /// before it falls.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Kinderpfad_TaggtDiePositivenInteressen_UndNichtDieAbneigung()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Einkleidung-Kind");
        var supervisor = new SupervisorApi(Authenticated());

        var abneigung = await creator.CreateInterestTagAsync(new CreateInterestTagDto(TestApi.UniqueName("Spinnen")));
        await supervisor.SetInterestWeightAsync(1, abneigung.Id, new SetChildInterestWeightDto(-3));

        int[] erwartet = [.. (await supervisor.ListInterestsAsync(1)).Where(i => i.Weight > 0)
            .Select(i => i.TagId).Order()];
        // Without this the case would also pass for a version that tags nothing at all - the seeded child
        // is the only reason there is anything to find, and that is worth asserting rather than assuming.
        Assert.NotEmpty(erwartet);

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary"));
        Assert.True(outcome.Published, $"Selbsttest: {outcome.SelfTestPercent} %");

        var detail = await creator.GetExerciseDetailAsync(outcome.ExerciseId!.Value);
        int[] getaggt = [.. detail.InterestTagIds.Order()];
        Assert.Equal(erwartet, getaggt);
        Assert.DoesNotContain(abneigung.Id, detail.InterestTagIds);
    }

    /// <summary>
    /// <b>AK 4 of B-46.</b> Without a child, <c>--interests</c> dresses the exercise - that is the whole
    /// point of the story's second half: a catalog exercise for an interest profile, built for nobody in
    /// particular. The slug is passed in upper case on purpose: slugs are lower case by definition, so a
    /// case-sensitive lookup would reject exactly what a user reads off the taxonomy listing.
    /// </summary>
    [Fact]
    public async Task KindloserPfad_TaggtDieAngegebenenSlugs()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Einkleidung-Slug");
        var einhorn = await creator.CreateInterestTagAsync(new CreateInterestTagDto(TestApi.UniqueName("Einhorn")));

        var (briefing, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary",
            childId: null, interests: [einhorn.Slug.ToUpperInvariant()]));

        // No child anywhere in the briefing - otherwise this would be the child path in disguise.
        Assert.False(briefing.Individual);
        Assert.True(outcome.Published, $"Selbsttest: {outcome.SelfTestPercent} %");

        var detail = await creator.GetExerciseDetailAsync(outcome.ExerciseId!.Value);
        int[] nurEinhorn = [einhorn.Id];
        Assert.Equal(nurEinhorn, detail.InterestTagIds);
    }

    /// <summary>
    /// Aus dem Review zu B-46. Die Auflösung fragt seither <b>je Slug</b> beim Server nach, weil eine
    /// Vollauflistung bei 100 Einträgen abschneidet und jedes weitere Stichwort als Tippfehler gemeldet
    /// hätte. <c>search</c> ist aber ein Teilstring über Slug <i>oder</i> Label — der exakte Vergleich
    /// danach ist deshalb tragend, und dieser Fall ist sein Beleg: zwei Stichworte, von denen eines das
    /// andere enthält.
    /// <para>
    /// Nicht abgedeckt und bewusst so: die 100er-Grenze selbst. Ein Fall dafür bräuchte 101 Stichworte in
    /// der geteilten Klassen-DB und würde die Nachbarfälle vergiften — der Fix ist stattdessen so gebaut,
    /// dass die Grenze nicht mehr erreicht wird.
    /// </para>
    /// </summary>
    [Fact]
    public async Task SlugWirdExaktGetroffen_NichtAlsTeilstring()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Einkleidung-Exakt");

        // Das gesuchte Stichwort sortiert absichtlich SPAET (`zzz-`), der Lockvogel FRUEH (`aaa-`) und
        // enthält den gesuchten Slug vollständig. Die Serverliste ist nach Slug sortiert, „nimm den ersten
        // Treffer" greift hier also nachweisbar den Falschen. Eine erste Fassung dieses Falls hatte die
        // Reihenfolge umgekehrt - und blieb bei genau diesem Defekt gruen (gemessen).
        var gesucht = await creator.CreateInterestTagAsync(new CreateInterestTagDto(TestApi.UniqueName("zzz-einhorn")));
        var lockvogel = await creator.CreateInterestTagAsync(new CreateInterestTagDto("Aaa " + gesucht.Slug));
        Assert.Contains(gesucht.Slug, lockvogel.Slug, StringComparison.Ordinal);
        Assert.True(string.CompareOrdinal(lockvogel.Slug, gesucht.Slug) < 0,
            $"Der Lockvogel muss vor dem Ziel sortieren: {lockvogel.Slug} vs. {gesucht.Slug}");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary",
            childId: null, profileId: null, interests: [gesucht.Slug]));

        var detail = await creator.GetExerciseDetailAsync(outcome.ExerciseId!.Value);
        int[] nurGesucht = [gesucht.Id];
        Assert.Equal(nurGesucht, detail.InterestTagIds);
    }

    /// <summary>
    /// <b>AK 4, second half.</b> A mistyped slug aborts with usage help instead of a raw <c>400</c> - and
    /// above all instead of an exercise that reports success and carries no dressing at all. The message
    /// names the offending slug, because "unknown interest" alone would leave the caller guessing which of
    /// five it was.
    /// </summary>
    [Fact]
    public async Task UnbekannterSlug_BrichtSprechendAb()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Einkleidung-Tippfehler");

        var ex = await Assert.ThrowsAsync<AgentUsageException>(() => pipeline.CreateAsync(
            Request(subjectId, seriesId, seriesUnitId, "Vocabulary", childId: null, interests: ["gibt-es-nicht"])));

        Assert.Contains("gibt-es-nicht", ex.Message);
    }

    /// <summary>
    /// The case B-46's estimate did not cover, decided here rather than guessed: <c>--child</c> together
    /// with <c>--interests</c> leaves "whose interests win?" unanswered, and the story defined two separate
    /// paths instead of a precedence. So it is a usage error - and the message names the way out.
    /// <para>
    /// The second half is what makes this a real assertion: with <c>--general</c> the same combination
    /// <b>works</b>, because that switch drops the child and hands the childless path over. The refusal is
    /// therefore keyed on what the briefing carries, not on which switches were typed.
    /// </para>
    /// </summary>
    [Fact]
    public async Task KindUndVorgegebeneInteressen_IstEinBedienfehler_MitGeneralAberErlaubt()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson, VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Einkleidung-Beides");
        var einhorn = await creator.CreateInterestTagAsync(new CreateInterestTagDto(TestApi.UniqueName("Einhorn")));

        var ex = await Assert.ThrowsAsync<AgentUsageException>(() => pipeline.CreateAsync(
            Request(subjectId, seriesId, seriesUnitId, "Vocabulary", childId: 1, interests: [einhorn.Slug])));
        Assert.Contains("--general", ex.Message);

        var (briefing, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary",
            childId: 1, general: true, interests: [einhorn.Slug]));
        Assert.False(briefing.Individual);
        var detail = await creator.GetExerciseDetailAsync(outcome.ExerciseId!.Value);
        int[] nurEinhorn = [einhorn.Id];
        Assert.Equal(nurEinhorn, detail.InterestTagIds);
    }

    [Fact]
    public async Task Vokabeluebung_entsteht_aus_dem_Entwurf_und_besteht_den_Selbsttest()
    {
        var (pipeline, creator, chat) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Vokabeln");

        var (briefing, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary"));

        Assert.Empty(outcome.Violations);
        Assert.True(outcome.Published, $"Selbsttest: {outcome.SelfTestPercent} %");
        Assert.Equal(1, chat.Calls);
        Assert.Equal("Tiere auf dem Bauernhof", outcome.Title);

        // The word pairs must sit in the catalog as their own item tier and be linked in the vocabulary store.
        var items = await creator.ListItemsAsync(seriesId, seriesUnitId, outcome.ExerciseId!.Value);
        Assert.Equal(3, items.Count);
        Assert.All(items, i => Assert.True(i.VocabularyId > 0));
        Assert.Contains(items, i => i.Front == "the horse" && i.Back == "das Pferd");

        // The metadata comes from the briefing - that is how the supervisor finds the exercise again later.
        var detail = await creator.GetExerciseDetailAsync(outcome.ExerciseId.Value);
        Assert.Equal(briefing.Grade, detail.GradeMin);
        Assert.Contains(briefing.Audience, detail.Description);
    }

    [Fact]
    public async Task Trockenlauf_plant_die_Uebung_ohne_sie_anzulegen()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Trockenlauf");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary", dryRun: true));

        Assert.True(outcome.DraftAccepted);
        Assert.Null(outcome.ExerciseId);
        Assert.Contains("das Pferd", outcome.DraftJson);
        Assert.Empty(await creator.SearchExercisesAsync(subjectId: subjectId));
    }

    [Fact]
    public async Task Ein_fehlerhafter_Entwurf_geht_mit_den_Verstoessen_zurueck_ans_Modell()
    {
        // First attempt: one vocabulary entry too few and one duplicate - both deterministically checkable.
        const string broken = """
            {"title":"Tiere","items":[
              {"front":"the horse","back":"das Pferd","hint":null},
              {"front":"the horse","back":"das Pferd","hint":null}]}
            """;
        var (pipeline, creator, chat) = BuildAgent(broken, VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Reparatur");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary"));

        Assert.Equal(2, chat.Calls);
        Assert.True(outcome.Published);
        // The repair round must have named the concrete violations, not just "again".
        Assert.Contains(chat.LastMessages, m => m.Text.Contains("doppelt vor"));
    }

    [Fact]
    public async Task Ein_dauerhaft_fehlerhafter_Entwurf_landet_nicht_im_Katalog()
    {
        const string broken = """
            {"title":"Tiere","items":[{"front":"the horse","back":"the horse","hint":null}]}
            """;
        var (pipeline, creator, _) = BuildAgent(broken);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Ablehnung");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary"));

        Assert.False(outcome.DraftAccepted);
        Assert.Null(outcome.ExerciseId);
        Assert.Contains(outcome.Violations, v => v.Contains("Zu wenige Aufgaben"));
        Assert.Contains(outcome.Violations, v => v.Contains("identisch"));
        Assert.Empty(await creator.SearchExercisesAsync(subjectId: subjectId));
    }

    [Fact]
    public async Task Der_Pflicht_Wortschatz_darf_nicht_ausgetauscht_werden()
    {
        // The "model" replaces two prescribed words with its own - exactly the case the core rule forbids
        // (interests dress the material up, they do not change it).
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Wortschatz");

        var (_, outcome) = await pipeline.CreateAsync(
            Request(subjectId, seriesId, seriesUnitId, "Vocabulary", words: ["the horse", "the tractor", "the barn"]));

        Assert.False(outcome.DraftAccepted);
        var violation = Assert.Single(outcome.Violations, v => v.Contains("Pflicht-Wortschatz"));
        Assert.Contains("the tractor", violation);
        Assert.Contains("the barn", violation);
    }

    [Fact]
    public async Task Bekannte_Vokabeln_werden_verlinkt_statt_dupliziert()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Dedupe");
        await creator.CreateVocabularyAsync(
            new CreateVocabularyDto(null, "en", "de", "the horse", "das Pferd", PartOfSpeech.Noun));

        // Unlike subject and unit, the vocabulary store is **shared across the class**: earlier tests of this
        // class run the same `VocabularyJson` through and may long since have materialized "the horse". So the
        // statement must not be "points at *my* row" (then the test hangs on the execution order) but exactly
        // what its name promises: **no new row, a reference to an existing one.**
        var before = await KnownHorsesAsync(creator);
        Assert.NotEmpty(before);

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary"));

        Assert.Equal(before, await KnownHorsesAsync(creator));
        var items = await creator.ListItemsAsync(seriesId, seriesUnitId, outcome.ExerciseId!.Value);
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
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Cloze");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Cloze"));

        Assert.Empty(outcome.Violations);
        Assert.True(outcome.Published, $"Selbsttest: {outcome.SelfTestPercent} %");
    }

    [Fact]
    public async Task Ein_Lueckentext_ohne_passende_Platzhalter_wird_abgelehnt()
    {
        // Gap 3 has no placeholder, placeholder {{4}} has no gap, and the solution stands in the text.
        const string cloze = """
            {"title":"Kaputt","text":"Tom feeds the {{1}} and the horse. Then he cleans the {{4}}.",
             "gaps":[{"index":1,"answer":"horse","alternatives":null},
                     {"index":2,"answer":"sheep","alternatives":null},
                     {"index":3,"answer":"stable","alternatives":null}],
             "wordBank":["horse","sheep","stable"]}
            """;
        var (pipeline, creator, _) = BuildAgent(cloze);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Cloze-kaputt");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Cloze"));

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
        // First the whole list is missing, then the front sides are missing in every entry.
        var (pipeline, creator, _) = BuildAgent(
            """{"title":"Tiere"}""",
            """{"title":"Tiere","items":[{"back":"das Pferd"},{"back":"das Schaf"},{"back":"die Ziege"}]}""");
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Nullfelder");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary"));

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
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Cloze-doppelt");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Cloze"));

        Assert.False(outcome.DraftAccepted);
        Assert.Contains(outcome.Violations, v => v.Contains("{{2}} steht 2×"));
        Assert.Empty(await creator.SearchExercisesAsync(subjectId: subjectId));
    }

    /// <summary>
    /// <c>--series-unit</c> without <c>--series</c> used to be silently dropped: the exercise ended up in
    /// the first unit of the first series. A silent reach into the wrong shelf is worse than an
    /// error message.
    /// </summary>
    [Fact]
    public async Task Eine_unbekannte_Unit_wird_gemeldet_statt_still_in_die_erste_zu_schreiben()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        // There are series with units - without the check exactly one of them would be picked silently.
        await FreshUnitAsync(creator, "Unit-Wahl");
        var commands = Commands(creator, pipeline);

        var command = CommandLine.Parse(["briefing", "--child", "1", "--series-unit", "999999"]);
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

        // Without a verb, help still applies - and the options are read from the first argument on.
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
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Weitere Typen");

        var (_, translated) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Translation"));
        var (_, grammarOutcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Grammar"));

        Assert.True(translated.Published, $"Übersetzung – Selbsttest: {translated.SelfTestPercent} %");
        Assert.True(grammarOutcome.Published, $"Grammatik – Selbsttest: {grammarOutcome.SelfTestPercent} %");
    }

    [Fact]
    public async Task Das_Briefing_traegt_Profil_Interessen_und_Lehrbuch_in_den_Prompt()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Briefing");
        var supervisor = new SupervisorApi(Authenticated());
        await supervisor.UpdateChildAsync(1, new UpdateChildDto(null, null, null, null, null,
            Interests: ["Fußball", "Minecraft"]));
        await supervisor.CreateTextbookAsync(1,
            new CreateTextbookDto("Green Line 1", "Englisch", null, 5, "Klett", null, "Unit 1"));
        // The weighted taxonomy next to the free text - including a dislike.
        await supervisor.SetInterestsAsync(1, new SetChildInterestsDto(
            [new ChildInterestInput(3, Label: "Weltraum"), new ChildInterestInput(-3, Label: "Spinnen")]));

        var briefing = await pipeline.BriefAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary"));
        var prompt = briefing.ToPromptText();

        Assert.Contains("Fußball", prompt);
        Assert.Contains("Minecraft", prompt);
        Assert.Contains("Green Line 1", prompt);
        Assert.Contains("Unit 1: Animals", prompt);
        Assert.Contains("Green Line 1", briefing.Source);

        // The strongest preference comes before the free text …
        Assert.Matches(@"Interessen \(wichtigste zuerst\): Weltraum", prompt);
        // … and the dislike is listed as a ban, not as another topic.
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
        var publisher = await creator.CreatePublisherAsync(new CreatePublisherDto("Cornelsen"));
        var series = await creator.CreateSeriesAsync(new CreateTextbookSeriesDto(
            $"Access {name} {Guid.NewGuid():N}", publisher.Id, "Englisch", subjectId,
            SchoolTypes.Gymnasium, "en", "de", "Kompetenzorientiertes Lehrwerk mit Units je Halbjahr."));
        var unit = await creator.CreateUnitAsync(series.Id, new CreateSeriesUnitDto(
            "Unit 3 – Growing up", Grade: 8, OrderIndex: 3, BookType: null,
            Topics: ["Familie", "Freundschaft", "Erwachsenwerden"],
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
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Allgemein");
        var (series, unit, profile) = await FreshProfileAsync(creator, subjectId, "Allgemein");

        var request = Request(subjectId, seriesId, seriesUnitId, "Vocabulary", childId: null, profileId: profile.Id, unitId: unit.Id);
        var (briefing, outcome) = await pipeline.CreateAsync(request);

        Assert.True(outcome.Published, $"Selbsttest: {outcome.SelfTestPercent} %");
        Assert.False(briefing.Individual);

        // The prompt describes the teacher and the subject matter, but no child.
        var prompt = briefing.ToPromptText();
        Assert.DoesNotContain("## Das Kind", prompt);
        Assert.Contains("Kein bestimmtes Kind", prompt);
        Assert.Contains(profile.Name, prompt);

        // Metadata from the profile: the whole grade range, the school type and the source with its unit.
        var detail = await creator.GetExerciseDetailAsync(outcome.ExerciseId!.Value);
        Assert.Equal(7, detail.GradeMin);
        Assert.Equal(8, detail.GradeMax);
        Assert.Equal(SchoolTypes.Gymnasium, detail.SchoolTypes);
        Assert.Contains(series.Name, detail.Source);
        Assert.Contains(unit.Label, detail.Source);
        Assert.Contains("gemeinsamen Katalog", detail.Description);

        // The profile's persona sits in the system prompt - before the fixed rules, which it does not soften.
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
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Unit-Stoff");
        var (series, unit, profile) = await FreshProfileAsync(creator, subjectId, "Unit-Stoff");

        var briefing = await pipeline.BriefAsync(
            Request(subjectId, seriesId, seriesUnitId, "Vocabulary", childId: null, profileId: profile.Id, unitId: unit.Id));
        var prompt = briefing.ToPromptText();

        Assert.Contains($"Lehrwerk: {series.Name} (Cornelsen)", prompt);
        Assert.Contains("Unit 3 – Growing up", prompt);
        Assert.Contains("Present perfect vs. simple past", prompt);
        Assert.Contains("to grow up, responsibility, to argue", prompt);
        // The profile's languages apply when the command line names none.
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
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Fremde-Unit");
        var (_, _, profile) = await FreshProfileAsync(creator, subjectId, "Fremde-Unit");
        var (_, foreignUnit, _) = await FreshProfileAsync(creator, subjectId, "Fremdwerk");

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => pipeline.BriefAsync(
            Request(subjectId, seriesId, seriesUnitId, "Vocabulary", childId: null, profileId: profile.Id, unitId: foreignUnit.Id)));

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
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Klausur");
        var (_, unit, profile) = await FreshProfileAsync(creator, subjectId, "Klausur");

        var planner = new ExamPlanner(pipeline, creator, supervisor);
        var outcome = await planner.RunAsync(new ExamRequest(
            Request(subjectId, seriesId, seriesUnitId, "Vocabulary", profileId: profile.Id, unitId: unit.Id),
            Types: ["Vocabulary", "Cloze"], PerType: 3,
            ScheduledDate: new DateOnly(2026, 9, 15), Title: null));

        Assert.True(outcome.Complete, string.Join(" | ", outcome.Parts.Select(p => p.Error ?? p.Outcome?.Title)));
        Assert.Equal(2, outcome.ExerciseIds.Count);
        // The title comes from the unit - the same label that also appears in the source.
        Assert.Contains(unit.Label, outcome.Title);

        var classTest = await supervisor.GetClassTestAsync(outcome.ClassTestId!.Value);
        Assert.Equal(new DateOnly(2026, 9, 15), classTest.Klassenarbeit.ScheduledDate);
        Assert.Equal(KlassenarbeitStatus.Planned, classTest.Klassenarbeit.Status);
        Assert.Equal([.. outcome.ExerciseIds.Order()],
            [.. classTest.AssignedExercises.Select(e => e.Id).Order()]);
        // The child-scoped tag holds the bundle together outside the class test too.
        Assert.Contains(outcome.TagName, classTest.Klassenarbeit.Tags.Select(t => t.Name));
    }

    /// <summary>
    /// A failed part costs the class test one part, not all of them – and it is reported instead of
    /// passing off an incomplete test as finished.
    /// </summary>
    [Fact]
    public async Task Eine_Klausur_mit_einem_kaputten_Teil_bleibt_unvollstaendig()
    {
        // The one prepared answer only fits the vocabulary part; the cloze part fails the rules.
        var (pipeline, creator, _) = BuildAgent(ProfileVocabularyJson);
        var supervisor = new SupervisorApi(Authenticated());
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Klausur-kaputt");
        var (_, unit, profile) = await FreshProfileAsync(creator, subjectId, "Klausur-kaputt");

        var planner = new ExamPlanner(pipeline, creator, supervisor);
        var outcome = await planner.RunAsync(new ExamRequest(
            Request(subjectId, seriesId, seriesUnitId, "Vocabulary", profileId: profile.Id, unitId: unit.Id),
            Types: ["Vocabulary", "Cloze"], PerType: 3, ScheduledDate: null, Title: "Übungsklausur Test"));

        Assert.False(outcome.Complete);
        Assert.Single(outcome.ExerciseIds);
        Assert.Contains(outcome.Parts, p => p.TypeKey == "Cloze" && p.Outcome is { DraftAccepted: false });
        // The successful exercise is created nonetheless and assigned to the class test.
        var classTest = await supervisor.GetClassTestAsync(outcome.ClassTestId!.Value);
        Assert.Single(classTest.AssignedExercises);
    }

    /// <summary>
    /// The command line's <c>--points</c> default must equal <see cref="GoalPointsPolicy.Fallback"/>.
    /// <para>
    /// Since B-45 the figure is the target reward a new position <b>inherits</b>, and the product states 20
    /// everywhere (wizard pre-fill, manual creation form, the server's own fallback). The agent kept the 10
    /// from the time the field meant "what the exercise is worth" - so every agent-generated exercise
    /// silently halved the coin target of any position created from it. The test drives the real command
    /// line, because that is where the default lives; the request helper above passes the figure
    /// explicitly and would never have noticed.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Ohne_points_erbt_die_Position_die_Vorgabe_des_Produkts_und_nicht_die_Haelfte()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Punkte-Vorgabe");
        var commands = Commands(creator, pipeline);

        // Deliberately no `--points`: the default under test is the one the command line falls back to.
        // `--count 3` must be given, though - the fake model answers with three items, and the request's
        // own default of 10 would have the draft rejected by the rules before the figure ever mattered.
        var command = CommandLine.Parse([
            "create", "--child", "1", "--type", "Vocabulary", "--count", "3",
            "--series", seriesId.ToString(), "--series-unit", seriesUnitId.ToString(),
        ]);
        Assert.Equal(0, await commands.RunAsync(command, default));

        var exercise = Assert.Single(await creator.SearchExercisesAsync(subjectId: subjectId));
        Assert.Equal(GoalPointsPolicy.Fallback, exercise.RewardPoints);

        // The figure a position would inherit is the agent's own, passed through rather than repaired.
        // Deliberately compared against `RewardPoints` and NOT against `Fallback` a second time: with the
        // line above holding, `Assert.Equal(Fallback, Effective(Fallback))` is true for every conceivable
        // value of `Fallback` - both branches of `Effective` return it - and could therefore never fail.
        // What this line still carries is that `SuggestedGoalPoints` is fed from the policy at all.
        // The bound itself is pinned where a value actually gets repaired:
        // `PlanPositionCrudTests.cs:336` and `:372-388`.
        Assert.Equal(exercise.RewardPoints, exercise.SuggestedGoalPoints);
    }

    /// <summary>The cloze answer used by the study-plan cases; deliberately independent of any unit material.</summary>
    private const string PlanClozeJson = """
        {"title":"Lehrplan-Lückentext","text":"The horse {{1}} in the field, the sheep {{2}} and the goat {{3}}.",
         "gaps":[{"index":1,"answer":"runs","alternatives":null},
                 {"index":2,"answer":"sleeps","alternatives":null},
                 {"index":3,"answer":"eats","alternatives":null}],
         "wordBank":["runs","sleeps","eats","jumps"]}
        """;

    /// <summary>
    /// <b>AK 1–3 of B-19.</b> The generated plan is a container the child could play – one position per
    /// generated exercise, each a daily obligation carrying the chosen intensity – and it is <b>off</b>.
    /// <para>
    /// The inactive state is the story's whole safety net (decision 5): machine-written content must not
    /// become an obligation before a human has looked at it, and an active plan would additionally switch
    /// off the plan the child is playing today.
    /// </para>
    /// <para>
    /// Red probes, one per claim: pass <c>Active: true</c> in <c>PlanPlanner</c> and the <c>Active</c>
    /// assertion falls; swap the <c>Intensiv</c> row of <c>PlanPlanner.Intensities</c> and the three value
    /// assertions fall; drop <c>Cadence: GoalCadence.Daily</c> and the cadence assertion falls (the server
    /// then stores <c>None</c>, which is free practice and can never cost a coin).
    /// </para>
    /// </summary>
    [Fact]
    public async Task Ein_erzeugter_Lehrplan_ist_inaktiv_und_traegt_die_Werte_der_Intensitaet()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson, PlanClozeJson);
        var supervisor = new SupervisorApi(Authenticated());
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Lehrplan");

        var planner = new PlanPlanner(pipeline, supervisor);
        var outcome = await planner.RunAsync(new PlanRequest(
            Request(subjectId, seriesId, seriesUnitId, "Vocabulary"),
            Types: ["Vocabulary", "Cloze"], PerType: 1, Intensity: PlanIntensity.Intensiv,
            StartDate: null, DurationDays: 21, Title: null));

        Assert.True(outcome.Complete, string.Join(" | ", outcome.Parts.Select(p => p.Error ?? p.Outcome?.Title)));
        Assert.Equal(2, outcome.ExerciseIds.Count);
        Assert.Equal(2, outcome.PositionIds.Count);

        var plan = await supervisor.GetPlanAsync(outcome.PlanId!.Value);
        Assert.False(plan.Active, "Ein ungeprüfter KI-Plan darf nicht sofort Pflicht werden.");
        Assert.Equal(2, plan.PositionCount);

        var positions = await supervisor.ListPositionsAsync(plan.Id);
        Assert.Equal([.. outcome.ExerciseIds.Order()], [.. positions.Select(p => p.ExerciseId).Order()]);
        Assert.All(positions, position =>
        {
            Assert.Equal(GoalCadence.Daily, position.Cadence);
            Assert.Equal(90, position.GoalThreshold);
            Assert.Equal(30, position.PointsGoalMet);
            Assert.Equal(10, position.PenaltyCoins);
        });
    }

    /// <summary>
    /// <b>AK 4 of B-19.</b> A failed type costs the plan one position, not all of them – and the run says
    /// so instead of passing off a thinner plan as the one that was asked for. Driven through the real
    /// command line, because the exit code is part of the claim and lives there.
    /// </summary>
    [Fact]
    public async Task Ein_gescheiterter_Typ_kostet_den_Lehrplan_eine_Position_nicht_alle()
    {
        // The single prepared answer only fits the vocabulary part; the cloze part fails the rules.
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var supervisor = new SupervisorApi(Authenticated());
        var (_, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Lehrplan-kaputt");
        var titel = TestApi.UniqueName("Lehrplan halb");

        var command = CommandLine.Parse([
            "plan", "--child", "1", "--types", "Vocabulary,Cloze", "--per-type", "1", "--count", "3",
            "--title", titel, "--series", seriesId.ToString(), "--series-unit", seriesUnitId.ToString(),
        ]);
        Assert.Equal(1, await Commands(creator, pipeline).RunAsync(command, default));

        var plan = Assert.Single(await supervisor.ListPlansAsync(1), p => p.Title == titel);
        Assert.False(plan.Active);
        // The vocabulary exercise made it: the broken cloze part did not take it down with it.
        Assert.Single(await supervisor.ListPositionsAsync(plan.Id));
    }

    /// <summary>
    /// <b>AK 6 of B-19.</b> A plan without a child is meaningless – and the refusal comes <b>before</b> the
    /// first model call, so a mistyped invocation does not first cost minutes of generation and leave the
    /// generated exercises behind with no plan to hold them.
    /// <para>
    /// Red probe: move the <c>ChildId</c> check below the generation loop and the call count rises above 0.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Ein_Lehrplan_ohne_Kind_wird_abgelehnt_bevor_das_Modell_laeuft()
    {
        var (pipeline, creator, chat) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Lehrplan-ohne-Kind");
        var (_, unit, profile) = await FreshProfileAsync(creator, subjectId, "Lehrplan-ohne-Kind");

        var planner = new PlanPlanner(pipeline, new SupervisorApi(Authenticated()));
        var error = await Assert.ThrowsAsync<AgentUsageException>(() => planner.RunAsync(new PlanRequest(
            Request(subjectId, seriesId, seriesUnitId, "Vocabulary", childId: null, profileId: profile.Id, unitId: unit.Id),
            Types: ["Vocabulary"], PerType: 1, Intensity: PlanIntensity.Normal,
            StartDate: null, DurationDays: 14, Title: null)));

        Assert.Contains("--child", error.Message);
        Assert.Equal(0, chat.Calls);
    }

    /// <summary>
    /// „Vollständig" schließt den Selbsttest ein – und nimmt den Trockenlauf ausdrücklich aus.
    /// <para>
    /// Ohne <c>--strict</c> bleibt eine Übung, deren eigene Lösungen nur zu 60 % passen, im Katalog
    /// (<c>RolledBack: false</c>) und würde hier ein <b>Tagesziel mit Münz-Malus</b>. <c>create</c> meldet
    /// für dieselbe Übung längst Exit 1; der Lehrplan hätte sie als fertigen Plan durchgewinkt.
    /// </para>
    /// <para>
    /// Ausnahmsweise ein Unit-Test statt eines Integrationstests: <c>Complete</c> ist ein reines Prädikat,
    /// und ein Selbsttest unter 100 % ohne Regelverstoß ist über den <c>FakeChatClient</c> nur mit einem
    /// Entwurf zu erzwingen, dessen Zustandekommen selbst von Store-Verhalten abhinge – der Fall wäre dann
    /// an das gebunden, was er nicht prüfen will.
    /// </para>
    /// <para>
    /// Rote Proben: die Selbsttest-Bedingung aus <c>PlanOutcome.Complete</c> entfernen → der erste Fall
    /// fällt; die Ausnahme <c>ExerciseId is null</c> entfernen → der dritte fällt (der Trockenlauf hat
    /// weder Id noch Prozentzahl).
    /// </para>
    /// </summary>
    [Fact]
    public void Ein_Lehrplan_mit_unsauberem_Selbsttest_gilt_nicht_als_vollstaendig()
    {
        static PlanOutcome Mit(GenerationOutcome outcome) => new(
            "Lehrplan: Test", [new GeneratedPart("Vocabulary", outcome, null)], [], null, [], []);

        static GenerationOutcome Lauf(int? exerciseId, int? selfTestPercent) =>
            new("Vocabulary", "Titel", "{}", [], exerciseId, selfTestPercent, RolledBack: false);

        // Angelegt, regelkonform, aber die Übung widerspricht ihren eigenen Lösungen.
        Assert.False(Mit(Lauf(exerciseId: 42, selfTestPercent: 60)).Complete);
        // Sauber durchgelaufen.
        Assert.True(Mit(Lauf(exerciseId: 42, selfTestPercent: 100)).Complete);
        // Trockenlauf: keine Übung, kein Selbsttest – und trotzdem ein vollständiger Lauf.
        Assert.True(Mit(Lauf(exerciseId: null, selfTestPercent: null)).Complete);
        // Eine nicht angelegte Position macht den Plan unvollständig, auch wenn jede Übung sauber ist.
        Assert.False(new PlanOutcome("Lehrplan: Test", [new GeneratedPart("Vocabulary", Lauf(42, 100), null)],
            [42], PlanId: 7, PositionIds: [], FailedPositions: [new FailedPosition(42, "exercise_empty")]).Complete);
    }

    /// <summary>
    /// A mistyped intensity is refused, it does not fall back to <c>Normal</c>.
    /// <para>
    /// The difference is a real one for the child: whoever types <c>--intensity lokcer</c> means "no coin
    /// penalty", and <c>Normal</c> would hand out −5 per missed day instead. The exit code is 2 (invoked
    /// incorrectly), not 1 – <c>Program</c> maps <c>AgentUsageException</c> there, and this goes through
    /// the real command line so that mapping is part of the case.
    /// </para>
    /// <para>
    /// Red probe: replace the <c>throw</c> in <c>AgentCommands.ParseIntensity</c> with
    /// <c>PlanIntensity.Normal</c> and the case falls – the run then succeeds instead of being refused.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Eine_vertippte_Intensitaet_wird_abgelehnt_statt_auf_Normal_zurueckzufallen()
    {
        var (pipeline, creator, chat) = BuildAgent(VocabularyJson);
        var (_, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Intensitaet-Tippfehler");

        var command = CommandLine.Parse([
            "plan", "--child", "1", "--types", "Vocabulary", "--per-type", "1", "--count", "3",
            "--intensity", "lokcer",
            "--series", seriesId.ToString(), "--series-unit", seriesUnitId.ToString(),
        ]);

        var error = await Assert.ThrowsAsync<AgentUsageException>(
            () => Commands(creator, pipeline).RunAsync(command, default));

        Assert.Contains("lokcer", error.Message);
        // The three valid spellings belong in the message – otherwise the caller has to go read the help.
        Assert.Contains("Locker", error.Message);
        Assert.Equal(0, chat.Calls);
    }

    /// <summary>
    /// <c>Locker</c> really means <b>no</b> penalty – the one intensity whose whole point is the gentle
    /// start, and the branch the report line treats separately ("kein Malus").
    /// <para>
    /// Its own case because the <c>Intensiv</c> one above cannot carry it: a table that hands out −10
    /// everywhere would pass that case and still take coins off a child whose father asked for none.
    /// Red probe: raise the penalty in the <c>Locker</c> row of <c>PlanPlanner.Intensities</c> above 0.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Die_Stufe_Locker_vergibt_keinen_Muenz_Malus()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var supervisor = new SupervisorApi(Authenticated());
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Lehrplan-locker");

        var planner = new PlanPlanner(pipeline, supervisor);
        var outcome = await planner.RunAsync(new PlanRequest(
            Request(subjectId, seriesId, seriesUnitId, "Vocabulary"),
            Types: ["Vocabulary"], PerType: 1, Intensity: PlanIntensity.Locker,
            StartDate: null, DurationDays: 14, Title: null));

        Assert.True(outcome.Complete, string.Join(" | ", outcome.Parts.Select(p => p.Error ?? p.Outcome?.Title)));
        var position = Assert.Single(await supervisor.ListPositionsAsync(outcome.PlanId!.Value));
        Assert.Equal(0, position.PenaltyCoins);
        Assert.Equal(70, position.GoalThreshold);
        Assert.Equal(10, position.PointsGoalMet);
    }

    /// <summary>
    /// A run of zero or fewer days is refused instead of quietly becoming something else.
    /// <para>
    /// The server turns a non-positive <c>durationDays</c> into 10 without a word, while help and README
    /// promise 14 – so <c>--days 0</c> would produce a plan whose length nobody stated. Red probe: drop the
    /// <c>DurationDays</c> guard from <c>PlanPlanner.RunAsync</c> and the case falls.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Ein_Lehrplan_ohne_Laufzeit_wird_abgelehnt()
    {
        var (pipeline, creator, chat) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Lehrplan-ohne-Tage");

        var planner = new PlanPlanner(pipeline, new SupervisorApi(Authenticated()));
        var error = await Assert.ThrowsAsync<AgentUsageException>(() => planner.RunAsync(new PlanRequest(
            Request(subjectId, seriesId, seriesUnitId, "Vocabulary"),
            Types: ["Vocabulary"], PerType: 1, Intensity: PlanIntensity.Normal,
            StartDate: null, DurationDays: 0, Title: null)));

        Assert.Contains("--days", error.Message);
        Assert.Equal(0, chat.Calls);
    }

    /// <summary>
    /// <b>AK 5 of B-19.</b> A dry run plans and prints; it writes nothing – neither exercises nor the plan.
    /// The subject is fresh, so "no exercise" is a statement about this run and not about the seed.
    /// </summary>
    [Fact]
    public async Task Ein_Lehrplan_Trockenlauf_legt_weder_Uebung_noch_Plan_an()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson, PlanClozeJson);
        var supervisor = new SupervisorApi(Authenticated());
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Lehrplan-trocken");
        var planVorher = (await supervisor.ListPlansAsync(1)).Count;

        var planner = new PlanPlanner(pipeline, supervisor);
        var outcome = await planner.RunAsync(new PlanRequest(
            Request(subjectId, seriesId, seriesUnitId, "Vocabulary", dryRun: true),
            Types: ["Vocabulary", "Cloze"], PerType: 1, Intensity: PlanIntensity.Normal,
            StartDate: null, DurationDays: 14, Title: null));

        Assert.Null(outcome.PlanId);
        Assert.Empty(outcome.ExerciseIds);
        Assert.Empty(outcome.PositionIds);
        Assert.Empty(await creator.SearchExercisesAsync(subjectId: subjectId));
        Assert.Equal(planVorher, (await supervisor.ListPlansAsync(1)).Count);
    }

    [Fact]
    public async Task Ein_unbekannter_Uebungstyp_scheitert_mit_klarer_Ansage()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Unbekannt");

        var error = await Assert.ThrowsAsync<AgentUsageException>(() =>
            pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Birkenbihl")));

        Assert.Contains("Vocabulary", error.Message);
    }

    /// <summary>
    /// B-21 AC 3/5: with <c>--use-weak</c> the briefing takes the child's weak words of the exercise's language
    /// pair only. They become required vocabulary - a weak French word in an English exercise would be forced
    /// into a draft the rules then reject, or worse, accept. Red if the briefing loads the rollup without the
    /// pair (the French word appears) or if the switch stopped loading anything (the English word is missing).
    /// </summary>
    [Fact]
    public async Task Foerdermodus_nimmt_nur_schwache_Woerter_des_Sprachpaars_der_Uebung()
    {
        var father = await TestApi.AdultAsync(factory);
        var child = await TestApi.ChildAsync(factory);
        async Task WeakAsync(string src, string front, string back)
        {
            var exerciseId = await TestApi.CreateVocabExerciseAsync(father, src, "de", (front, back));
            var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)Pugling.Api.Models.TestStage.FreeText);
            var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
            await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "falsch");
        }
        await WeakAsync("en", "the hedgehog", "der Igel");
        await WeakAsync("fr", "le hérisson", "der Igel (fr)");

        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Foerdermodus");
        var briefing = await pipeline.BriefAsync(
            Request(subjectId, seriesId, seriesUnitId, "Vocabulary") with { UseWeakWords = true });

        Assert.Contains("the hedgehog", briefing.RequiredWords);
        Assert.DoesNotContain("le hérisson", briefing.RequiredWords);
        Assert.DoesNotContain(briefing.Child!.WeakWords, w => w.Word == "le hérisson");
    }

    /// <summary>
    /// B-21 AC 4: <c>--use-weak</c> without a child used to do nothing, silently. Every verb that builds a
    /// request (here <c>create</c> and <c>plan</c>, B-19) refuses it instead. Red if the guard is removed: then
    /// the verbs get past it and fail later for an unrelated reason, or not at all.
    /// </summary>
    [Theory]
    [InlineData("create", "--type", "Vocabulary")]
    [InlineData("plan")]
    public async Task Foerdermodus_ohne_Kind_bricht_mit_Meldung_ab(string verb, params string[] extra)
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var commands = Commands(creator, pipeline);

        var command = CommandLine.Parse([verb, "--profile", "999999", "--use-weak", .. extra]);
        var error = await Assert.ThrowsAsync<AgentUsageException>(() => commands.RunAsync(command, default));

        Assert.Contains("--use-weak braucht --child", error.Message);
    }
}
