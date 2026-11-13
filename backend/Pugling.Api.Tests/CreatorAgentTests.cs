using System.Net.Http.Json;
using System.Text.Json;
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
    private AgentCommands Commands(CreatorApi creator, CreatorPipeline pipeline, FakeChatClient? chat = null) =>
        new(creator, new SupervisorApi(Authenticated()), pipeline, new ExamPlanner(pipeline, creator, new SupervisorApi(Authenticated())),
            new PlanPlanner(pipeline, new SupervisorApi(Authenticated())), Filler(creator, chat ?? new FakeChatClient("{}")),
            ImageAgentTests.Run(creator, chat ?? new FakeChatClient("{}"), new FakeImageGenerator()));

    private static UnitNotesFiller Filler(CreatorApi creator, FakeChatClient chat, IModelInventory? models = null)
    {
        var options = Options.Create(new AgentOptions());
        var reader = new PageReader(chat, models ?? new FakeModelInventory(options.Value.VisionModel), options,
            NullLogger<PageReader>.Instance);
        return new(chat, creator, reader, options, NullLogger<UnitNotesFiller>.Instance);
    }

    /// <summary>A draft the fake model returns for the `fill` verb (B-22).</summary>
    private const string UnitNotesJson = """
        {"topics":["Growing up","Family life"],"grammar":"Present perfect vs. simple past",
         "vocabularyNotes":"to grow up, responsibility, to argue"}
        """;

    /// <summary>
    /// B-22, AC 2/3/5: the draft lands in the empty fields of the unit, a field that already carries a value is
    /// kept without <c>--force</c>, and the source is the only material in the request. Red if the filler
    /// overwrites a filled field (drop the "has value and not force" branch), or if the source is not in the prompt.
    /// </summary>
    [Fact]
    public async Task Fill_Befuellt_Nur_Leere_Felder_Aus_Der_Quelle()
    {
        var (_, creator, _) = BuildAgent();
        var (_, seriesId, unitId) = await FreshUnitAsync(creator, "Fill");
        // The hand-written grammar note must survive: it is the one field that already carries a value.
        await creator.UpdateUnitAsync(seriesId, unitId, new UpdateSeriesUnitDto(null, null, null, null, null, "Handschrift", null));
        var chat = new FakeChatClient(UnitNotesJson);

        var outcome = await Filler(creator, chat).RunAsync(new UnitFillRequest(seriesId, unitId,
            "Unit 3 Growing up: present perfect vs. simple past; Wortschatz: to grow up, responsibility, to argue",
            Force: false, DryRun: false));

        Assert.Equal(["topics", "vocabularyNotes"], outcome.Written);
        Assert.Equal(["grammar"], outcome.Kept);
        var unit = await creator.GetUnitAsync(seriesId, unitId);
        Assert.Equal(["Growing up", "Family life"], unit.Topics);
        Assert.Equal("Handschrift", unit.Grammar);
        Assert.Equal("to grow up, responsibility, to argue", unit.VocabularyNotes);
        // The anti-invention rule stands in the system prompt, the source in the user message - nothing else.
        Assert.Contains("AUSSCHLIESSLICH die mitgelieferte Quelle", chat.LastMessages[0].Text);
        Assert.Contains("to grow up, responsibility, to argue", chat.LastMessages[1].Text);
    }

    /// <summary>B-22, AC 3: with <c>--force</c> the filled field is overwritten too. Red if the flag is ignored.</summary>
    [Fact]
    public async Task Fill_Ueberschreibt_Mit_Force()
    {
        var (_, creator, _) = BuildAgent();
        var (_, seriesId, unitId) = await FreshUnitAsync(creator, "Fill-Force");
        await creator.UpdateUnitAsync(seriesId, unitId, new UpdateSeriesUnitDto(null, null, null, null, null, "Handschrift", null));

        var outcome = await Filler(creator, new FakeChatClient(UnitNotesJson)).RunAsync(
            new UnitFillRequest(seriesId, unitId, "Quelle", Force: true, DryRun: false));

        Assert.Equal(["topics", "grammar", "vocabularyNotes"], outcome.Written);
        Assert.Empty(outcome.Kept);
        Assert.Equal("Present perfect vs. simple past", (await creator.GetUnitAsync(seriesId, unitId)).Grammar);
    }

    /// <summary>B-22, AC 4: the dry run shows the draft and changes nothing. Red if the PATCH is sent anyway.</summary>
    [Fact]
    public async Task Fill_Trockenlauf_Schreibt_Nichts()
    {
        var (_, creator, _) = BuildAgent();
        var (_, seriesId, unitId) = await FreshUnitAsync(creator, "Fill-Dry");

        var outcome = await Filler(creator, new FakeChatClient(UnitNotesJson)).RunAsync(
            new UnitFillRequest(seriesId, unitId, "Quelle", Force: false, DryRun: true));

        Assert.Equal(["topics", "grammar", "vocabularyNotes"], outcome.Written);
        Assert.Null(outcome.After);
        var unit = await creator.GetUnitAsync(seriesId, unitId);
        Assert.Empty(unit.Topics);
        Assert.Null(unit.Grammar);
        Assert.Null(unit.VocabularyNotes);
    }

    /// <summary>
    /// B-292: the agent keeps the API's limits the API's way. A topic over 200 characters is left out and named,
    /// never cut (the controller rejects a cut topic on purpose); free text over 2000 is cut at a word boundary and
    /// the field is named. The two over-long values differ in kind on purpose - one is dropped, the other shortened.
    /// Red if a topic is truncated instead of dropped, or if the free text is cut mid-word or silently. B-296: the
    /// cut has a lower bound too - the exact lengths are asserted, so a cut at the first space or at 1000 is red as
    /// well; the second text puts whitespace exactly at the limit, which must keep all 2000 characters.
    /// </summary>
    [Fact]
    public async Task Fill_Laesst_Zu_Lange_Themen_Weg_Und_Kuerzt_Freitext_Am_Wortende()
    {
        var (_, creator, _) = BuildAgent();
        var (_, seriesId, unitId) = await FreshUnitAsync(creator, "Fill-Grenzen");
        var longTopic = string.Concat(Enumerable.Repeat("Thema ", 40)).Trim(); // 239 characters
        var exact = new string('x', 200);                                         // the limit itself stays
        var longText = string.Join(" ", Enumerable.Repeat("Wortes", 400));    // 2799 characters; 2000 falls mid-word
        var atBoundary = string.Join(" ", Enumerable.Repeat("ab", 1000));     // 2999 characters; index 2000 is a space
        // 52 topics in all: the limit is 50, so the two beyond it are dropped too - and named, not vanished.
        var many = Enumerable.Range(1, 50).Select(i => $"T{i}").ToList();
        var json = System.Text.Json.JsonSerializer.Serialize(new { topics = new[] { "Kurz", longTopic, exact }.Concat(many).ToArray(), grammar = longText, vocabularyNotes = atBoundary });

        var outcome = await Filler(creator, new FakeChatClient(json)).RunAsync(new UnitFillRequest(seriesId, unitId, "Quelle", Force: false, DryRun: false));

        Assert.Equal(50, outcome.Draft.Topics!.Count);
        Assert.Equal(["Kurz", exact], outcome.Draft.Topics.Take(2));
        Assert.Equal([longTopic, "T49", "T50"], outcome.Dropped);
        Assert.Equal(["grammar", "vocabularyNotes"], outcome.Shortened);
        // 285 whole words of six characters plus 284 spaces: the last boundary before 2000, and nothing shorter.
        Assert.Equal(1994, outcome.Draft.Grammar!.Length);
        Assert.EndsWith("Wortes", outcome.Draft.Grammar);
        // Whitespace at the limit itself: the head already ends on a whole word, so no character is given away.
        Assert.Equal(2000, outcome.Draft.VocabularyNotes!.Length);
        var unit = await creator.GetUnitAsync(seriesId, unitId);
        Assert.Equal(50, unit.Topics.Count);
        Assert.Equal(outcome.Draft.Grammar, unit.Grammar);
        Assert.Equal(outcome.Draft.VocabularyNotes, unit.VocabularyNotes);
    }

    /// <summary>
    /// B-22: a draft with nothing in it writes nothing - no PATCH, <c>After</c> stays null. Red if the filler sends
    /// an all-null PATCH anyway (drop the <c>written.Count == 0</c> short-circuit).
    /// </summary>
    [Fact]
    public async Task Fill_Mit_Leerem_Entwurf_Schreibt_Nichts()
    {
        var (_, creator, _) = BuildAgent();
        var (_, seriesId, unitId) = await FreshUnitAsync(creator, "Fill-Leer");
        var chat = new FakeChatClient("{}");

        var outcome = await Filler(creator, chat).RunAsync(new UnitFillRequest(seriesId, unitId, "Quelle", Force: true, DryRun: false));

        Assert.Equal(1, chat.Calls);
        Assert.Empty(outcome.Written);
        Assert.Null(outcome.After);
    }

    /// <summary>
    /// B-22: a series the caller does not own is refused BEFORE the model is asked - the API would answer 403 after
    /// a minute of model time (measured on the first live walk against the seeded series). Red if the check moves
    /// behind the draft (<c>chat.Calls</c> would be 1) or is dropped (the PATCH's 403 surfaces instead).
    /// </summary>
    [Fact]
    public async Task Fill_Lehnt_Eine_Fremde_Reihe_Vor_Dem_Modellaufruf_Ab()
    {
        var (_, creator, _) = BuildAgent();
        // Series 1 is seeded and owned by nobody - the one case every fresh database carries.
        var chat = new FakeChatClient(UnitNotesJson);
        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Filler(creator, chat)
            .RunAsync(new UnitFillRequest(1, 1, "Quelle", Force: false, DryRun: true)));
        Assert.Contains("gehört dir nicht", error.Message);
        Assert.Equal(0, chat.Calls);
    }

    /// <summary>
    /// B-296: whitespace at the limit AND just before it - the kept head must not end on a space, or the draft
    /// shown to the human would end on a blank (the stored unit would not: the controller trims). Red if the
    /// <c>TrimEnd()</c> of the whitespace branch in <c>UnitNotesFiller.Clamp</c> is dropped (2000 characters, not 1999).
    /// </summary>
    [Fact]
    public async Task Fill_Kuerzt_Freitext_Mit_Leerraum_Vor_Der_Grenze_Ohne_Nachlaufendes_Leerzeichen()
    {
        var (_, creator, _) = BuildAgent();
        var (_, seriesId, unitId) = await FreshUnitAsync(creator, "Fill-Leerraum");
        var doubleSpace = new string('x', 1999) + "  " + new string('y', 10); // indices 1999 and 2000 are spaces
        var json = System.Text.Json.JsonSerializer.Serialize(new { grammar = doubleSpace });

        var outcome = await Filler(creator, new FakeChatClient(json)).RunAsync(new UnitFillRequest(seriesId, unitId, "Quelle", Force: false, DryRun: true));

        Assert.Equal(["grammar"], outcome.Shortened);
        Assert.Equal(1999, outcome.Draft.Grammar!.Length);
    }

    /// <summary>
    /// B-300: the two cuts of <c>UnitNotesFiller.Clamp</c> the boundary tests above leave open. A text without any
    /// whitespace is cut hard at the limit - red if that branch returns an empty head (the field would be named as
    /// shortened and then silently not written, the loss B-292 is about). Two blanks before the last boundary end
    /// the draft without a trailing blank - red if the final <c>TrimEnd()</c> of the word-boundary branch is dropped
    /// (1991, not 1990).
    /// </summary>
    [Theory]
    [InlineData(2500, 0, 0, 2000)]
    [InlineData(1990, 2, 20, 1990)]
    public async Task Fill_Kuerzt_Freitext_Ohne_Oder_Mit_Doppeltem_Leerraum_Exakt(int xs, int blanks, int ys, int expected)
    {
        var (_, creator, _) = BuildAgent();
        var (_, seriesId, unitId) = await FreshUnitAsync(creator, $"Fill-Schnitt-{xs}");
        var text = new string('x', xs) + new string(' ', blanks) + new string('y', ys);
        var json = System.Text.Json.JsonSerializer.Serialize(new { grammar = text });

        var outcome = await Filler(creator, new FakeChatClient(json)).RunAsync(new UnitFillRequest(seriesId, unitId, "Quelle", Force: false, DryRun: true));

        Assert.Equal(["grammar"], outcome.Shortened);
        Assert.Equal(expected, outcome.Draft.Grammar!.Length);
    }

    /// <summary>
    /// B-302: words separated by whitespace other than a blank, newline or tab still end at a whole word - a non-breaking
    /// space (PDF copies) or a lone carriage return (old line endings). Same arithmetic as the blank case above: 285
    /// words of six characters plus 284 separators. Red if the word-boundary search knows fewer separators than the
    /// limit check (the hard cut would leave 2000 characters ending mid-word).
    /// </summary>
    [Theory]
    [InlineData('\u00A0')]
    [InlineData('\r')]
    [InlineData('\u2003')]
    public async Task Fill_Kuerzt_Freitext_An_Jeder_Art_Von_Leerraum_Am_Wortende(char separator)
    {
        var (_, creator, _) = BuildAgent();
        var (_, seriesId, unitId) = await FreshUnitAsync(creator, $"Fill-Trenner-{(int)separator}");
        var text = string.Join(separator, Enumerable.Repeat("Wortes", 400));
        var json = System.Text.Json.JsonSerializer.Serialize(new { grammar = text });

        var outcome = await Filler(creator, new FakeChatClient(json)).RunAsync(new UnitFillRequest(seriesId, unitId, "Quelle", Force: false, DryRun: true));

        Assert.Equal(["grammar"], outcome.Shortened);
        Assert.Equal(1994, outcome.Draft.Grammar!.Length);
    }

    /// <summary>
    /// B-309: the other direction of B-302 - such whitespace exactly AT the limit means the head already ends on a
    /// whole word, so nothing is given away. Separators at 1990 and 2000: the right answer keeps 2000 characters; a
    /// limit check narrowed to a blank (<c>== ' '</c> or <c>char.IsSeparator</c> for <c>\r</c>) falls through to the
    /// boundary search and cuts at 1990 - a whole word lost without need.
    /// </summary>
    [Theory]
    [InlineData('\u00A0')]
    [InlineData('\r')]
    [InlineData('\u2003')]
    public async Task Fill_Behaelt_Das_Letzte_Wort_Wenn_Jede_Art_Von_Leerraum_Auf_Der_Grenze_Liegt(char separator)
    {
        var (_, creator, _) = BuildAgent();
        var (_, seriesId, unitId) = await FreshUnitAsync(creator, $"Fill-Grenze-{(int)separator}");
        var text = new string('x', 1990) + separator + new string('y', 9) + separator + new string('z', 10);
        var json = System.Text.Json.JsonSerializer.Serialize(new { grammar = text });

        var outcome = await Filler(creator, new FakeChatClient(json)).RunAsync(new UnitFillRequest(seriesId, unitId, "Quelle", Force: false, DryRun: true));

        Assert.Equal(["grammar"], outcome.Shortened);
        Assert.Equal(2000, outcome.Draft.Grammar!.Length);
    }

    /// <summary>
    /// B-303: a <c>null</c> among the topics (a model in JSON mode is not held to the item type) is left out like an
    /// empty one - not a <see cref="NullReferenceException"/> that ends the run raw. Red if the trim dereferences
    /// a null topic (the old <c>t.Trim()</c>); the empty <c>Dropped</c> holds the intent that a missing topic is no loss to name.
    /// </summary>
    [Fact]
    public async Task Fill_Laesst_Ein_Null_Thema_Weg_Statt_Abzustuerzen()
    {
        var (_, creator, _) = BuildAgent();
        var (_, seriesId, unitId) = await FreshUnitAsync(creator, "Fill-Null-Thema");
        var chat = new FakeChatClient("""{"topics":["Growing up",null,"  "]}""");

        var outcome = await Filler(creator, chat).RunAsync(new UnitFillRequest(seriesId, unitId, "Quelle", Force: false, DryRun: true));

        Assert.Equal(["Growing up"], outcome.Draft.Topics);
        Assert.Empty(outcome.Dropped);
    }

    /// <summary>
    /// B-301: a series that does not exist is a usage error naming it, like a unit of another series (B-297) - and
    /// the model is never asked. Red if the NotFound catch around the series lookup is removed (a raw
    /// <c>PuglingApiException</c>, exit 1 instead of 2).
    /// </summary>
    [Fact]
    public async Task Fill_Mit_Unbekannter_Reihe_Ist_Eine_Fehlbedienung()
    {
        var (_, creator, _) = BuildAgent();
        var chat = new FakeChatClient(UnitNotesJson);

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Filler(creator, chat)
            .RunAsync(new UnitFillRequest(987654, 1, "Quelle", Force: false, DryRun: true)));
        Assert.Contains("Reihe 987654", error.Message);
        // Statement of intent, not a probe: the run stopped before any model call with or without the B-301 fix (a raw 404 came first too).
        Assert.Equal(0, chat.Calls);
    }

    /// <summary>
    /// B-297: a unit that belongs to another series is a usage error (exit 2, usage text) naming unit and series,
    /// not a raw <c>PuglingApiException</c> (exit 1, "the API answered 404") - and the model is never asked. The two
    /// series are both the caller's own, so only the series-bound 404 of the controller can refuse. Red if the
    /// NotFound catch is removed.
    /// </summary>
    [Fact]
    public async Task Fill_Mit_Unit_Einer_Anderen_Reihe_Ist_Eine_Fehlbedienung()
    {
        var (_, creator, _) = BuildAgent();
        var (_, seriesA, _) = await FreshUnitAsync(creator, "Fill-Reihe-A");
        var (_, _, unitOfB) = await FreshUnitAsync(creator, "Fill-Reihe-B");
        var chat = new FakeChatClient(UnitNotesJson);

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Filler(creator, chat)
            .RunAsync(new UnitFillRequest(seriesA, unitOfB, "Quelle", Force: false, DryRun: true)));
        Assert.Contains($"Unit {unitOfB}", error.Message);
        Assert.Contains($"Reihe {seriesA}", error.Message);
        Assert.Equal(0, chat.Calls);
    }

    /// <summary>B-22: an unreadable source path is a usage error (exit 2), not an unhandled exception. Red if the catch is removed.</summary>
    [Fact]
    public async Task Fill_Mit_Fehlender_Quelldatei_Ist_Eine_Fehlbedienung()
    {
        var (pipeline, creator, _) = BuildAgent();
        var (_, seriesId, unitId) = await FreshUnitAsync(creator, "Fill-Datei");
        var missing = Path.Combine(Path.GetTempPath(), $"pugling-fehlt-{Guid.NewGuid():N}.txt");

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline)
            .RunAsync(CommandLine.Parse(["fill", "--series", seriesId.ToString(), "--unit", unitId.ToString(), "--source-file", missing]), CancellationToken.None));
        Assert.Contains("nicht lesbar", error.Message);
    }

    /// <summary>
    /// B-22, AC 1: without a source the verb refuses as a usage error (exit 2 via <c>AgentUsageException</c>) - and
    /// never asks the model. Red if the command drafts from the unit's title alone. This case covers the command's
    /// guard; the filler's own guard is defense in depth for direct callers and not reached from here.
    /// </summary>
    [Fact]
    public async Task Fill_Ohne_Quelle_Ist_Eine_Fehlbedienung()
    {
        var (pipeline, creator, _) = BuildAgent();
        var (_, seriesId, unitId) = await FreshUnitAsync(creator, "Fill-Ohne");
        var chat = new FakeChatClient(UnitNotesJson);

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline, chat)
            .RunAsync(CommandLine.Parse(["fill", "--series", seriesId.ToString(), "--unit", unitId.ToString()]), CancellationToken.None));
        Assert.Contains("--source-file", error.Message);
        Assert.Equal(0, chat.Calls);
    }

    /// <summary>Two prepared page extracts the fake vision model answers with, one per photo (B-375).</summary>
    private const string PageOneExtract = "Themen: Growing up\nGrammatik: present perfect für Erfahrungen\nWörter: to grow up – aufwachsen";
    private const string PageTwoExtract = "Themen: Family life\nGrammatik: –\nWörter: to argue – streiten";

    /// <summary>A throwaway folder with the given files; the caller deletes it in <c>finally</c>.</summary>
    private static string PhotoFolder(params (string Name, byte[] Bytes)[] files)
    {
        var folder = Path.Combine(Path.GetTempPath(), $"pugling-fotos-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        foreach (var (name, bytes) in files) File.WriteAllBytes(Path.Combine(folder, name), bytes);
        return folder;
    }

    /// <summary>
    /// B-375, AC 1-3: each photo goes to the vision model on its own, carrying exactly its image, on
    /// <c>Agent:VisionModel</c>; the structuring call that drafts the notes carries no image and runs on the default
    /// model, and its source is the extracts in page order. Both prompts forbid copying the book's tasks and texts.
    /// Red if the photos skip the vision step, if the image reaches the structuring call (the copyright cut, decision 1),
    /// if the vision call runs without <c>ModelId</c> (the text model would get the image), or if either prompt loses
    /// its prohibition.
    /// </summary>
    [Fact]
    public async Task Fill_Aus_Fotos_Liest_Jede_Seite_Mit_Dem_Vision_Modell_Und_Strukturiert_Ohne_Bild()
    {
        var (_, creator, _) = BuildAgent();
        var (_, seriesId, unitId) = await FreshUnitAsync(creator, "Fill-Fotos");
        var chat = new FakeChatClient(PageOneExtract, PageTwoExtract, UnitNotesJson);
        IReadOnlyList<PageImage> photos =
            [new("seite-1.jpg", [1, 2, 3], "image/jpeg"), new("seite-2.png", [4, 5], "image/png")];

        var outcome = await Filler(creator, chat).RunAsync(new UnitFillRequest(seriesId, unitId, null,
            Force: false, DryRun: false, Pages: photos));

        Assert.Equal(3, chat.Calls);
        for (var i = 0; i < 2; i++)
        {
            var (messages, options) = chat.History[i];
            Assert.Equal("llama3.2-vision", options?.ModelId);
            var image = Assert.Single(messages.SelectMany(m => m.Contents).OfType<DataContent>());
            Assert.Equal(photos[i].MediaType, image.MediaType);
            Assert.Equal(photos[i].Bytes, image.Data.ToArray());
            Assert.Contains("Übernimm NICHTS davon wörtlich", messages[0].Text);
            Assert.Contains("Aufgabenstellungen", messages[0].Text);
        }
        var (structuring, structuringOptions) = chat.History[2];
        Assert.Null(structuringOptions?.ModelId);
        Assert.Empty(structuring.SelectMany(m => m.Contents).OfType<DataContent>());
        Assert.Contains("Übernimm keine Aufgabenstellungen", structuring[0].Text);
        var source = structuring[1].Text;
        Assert.True(source.IndexOf("### Seite seite-1.jpg", StringComparison.Ordinal)
                    < source.IndexOf("### Seite seite-2.png", StringComparison.Ordinal));
        Assert.Contains("to grow up – aufwachsen", source);
        Assert.Contains("to argue – streiten", source);

        Assert.Equal(["seite-1.jpg", "seite-2.png"], outcome.Extracts.Select(e => e.FileName));
        Assert.Equal(["topics", "grammar", "vocabularyNotes"], outcome.Written);
        Assert.Equal(["Growing up", "Family life"], (await creator.GetUnitAsync(seriesId, unitId)).Topics);
    }

    /// <summary>
    /// B-375, AC 4: the dry run returns each page's extract for the human to hold against the photo, and writes nothing.
    /// Red if the extracts are not handed out (the console would show only the draft) or if the PATCH is sent anyway.
    /// </summary>
    [Fact]
    public async Task Fill_Aus_Fotos_Trockenlauf_Zeigt_Die_Auszuege_Und_Schreibt_Nichts()
    {
        var (_, creator, _) = BuildAgent();
        var (_, seriesId, unitId) = await FreshUnitAsync(creator, "Fill-Fotos-Dry");

        var outcome = await Filler(creator, new FakeChatClient(PageOneExtract, UnitNotesJson)).RunAsync(
            new UnitFillRequest(seriesId, unitId, null, Force: false, DryRun: true,
                Pages: [new("seite-1.jpg", [1], "image/jpeg")]));

        var extract = Assert.Single(outcome.Extracts);
        Assert.Equal(PageOneExtract, extract.Text);
        Assert.Null(outcome.After);
        Assert.Empty((await creator.GetUnitAsync(seriesId, unitId)).Topics);
    }

    /// <summary>
    /// B-375, AC 4: the printed draft keeps umlauts and dashes readable - the human checks it against the photo. Found
    /// on the live run ("für"). Red if the relaxed encoder is dropped. The second assertion keeps the word pair
    /// rule in the structuring prompt (live run: translations were lost); red if the rule is removed.
    /// </summary>
    [Fact]
    public void Fill_Entwurf_Druckt_Umlaute_Lesbar_Und_Behaelt_Uebersetzungen()
    {
        var printed = UnitNotesFiller.Print(new UnitNotesDraft(["Erwachsenwerden"], "für Erfahrungen", "to grow up – aufwachsen"));

        Assert.Contains("für Erfahrungen", printed);
        Assert.Contains("to grow up – aufwachsen", printed);
        Assert.Contains("Übersetzung, bleibt sie dabei", UnitNotesFiller.System);
    }

    /// <summary>
    /// B-375, AC 1: a human source given next to the photos is appended after the page extracts, not dropped. Red if
    /// the photos replace the source text.
    /// </summary>
    [Fact]
    public async Task Fill_Aus_Fotos_Haengt_Die_Ergaenzende_Quelle_An()
    {
        var (_, creator, _) = BuildAgent();
        var (_, seriesId, unitId) = await FreshUnitAsync(creator, "Fill-Fotos-Quelle");
        var chat = new FakeChatClient(PageOneExtract, UnitNotesJson);

        await Filler(creator, chat).RunAsync(new UnitFillRequest(seriesId, unitId, "Inhaltsverzeichnis: Unit 3 Growing up",
            Force: false, DryRun: true, Pages: [new("seite-1.jpg", [1], "image/jpeg")]));

        var source = chat.History[1].Messages[1].Text;
        Assert.Contains("to grow up – aufwachsen", source);
        Assert.Contains("### Ergänzende Quelle", source);
        Assert.Contains("Inhaltsverzeichnis: Unit 3 Growing up", source);
    }

    /// <summary>
    /// B-375, AC 5: a vision model that is not installed is a usage error naming the pull command - before any model
    /// call, and nothing is written. Red if the inventory check is removed (the fake would answer, and the unit would
    /// be filled) or moved after the first page.
    /// </summary>
    [Fact]
    public async Task Fill_Aus_Fotos_Ohne_Vision_Modell_Ist_Eine_Fehlbedienung_Vor_Dem_Ersten_Aufruf()
    {
        var (_, creator, _) = BuildAgent();
        var (_, seriesId, unitId) = await FreshUnitAsync(creator, "Fill-Fotos-Modell");
        var chat = new FakeChatClient(PageOneExtract, UnitNotesJson);

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Filler(creator, chat, new FakeModelInventory("qwen2.5:14b-instruct"))
            .RunAsync(new UnitFillRequest(seriesId, unitId, null, Force: false, DryRun: false,
                Pages: [new("seite-1.jpg", [1], "image/jpeg")])));

        Assert.Contains("ollama pull llama3.2-vision", error.Message);
        Assert.Equal(0, chat.Calls);
        Assert.Empty((await creator.GetUnitAsync(seriesId, unitId)).Topics);
    }

    /// <summary>
    /// B-375, AC 1: a folder contributes its images sorted by name (the page order of camera names) and skips other
    /// files; each page carries the media type of its extension. Red if the sort is dropped (the files are written in
    /// reverse, and the file system's order is not guaranteed) or if a non-image in the folder is sent along.
    /// </summary>
    [Fact]
    public async Task Fotos_Laden_Nimmt_Die_Bilder_Eines_Ordners_Nach_Namen()
    {
        var folder = PhotoFolder(("IMG_0003.webp", [3]), ("notiz.txt", [9]), ("IMG_0002.PNG", [2]), ("IMG_0001.jpg", [1]));
        try
        {
            var pages = await PageImage.LoadAsync([folder], CancellationToken.None);

            Assert.Equal(["IMG_0001.jpg", "IMG_0002.PNG", "IMG_0003.webp"], pages.Select(p => p.FileName));
            Assert.Equal(["image/jpeg", "image/png", "image/webp"], pages.Select(p => p.MediaType));
            Assert.Equal([2], pages[1].Bytes);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    /// B-375, AC 1: the page order does not depend on the file system (NTFS happens to list by name, so the folder
    /// case above stays green without any sort - measured). Numbers count as numbers: a human names photos
    /// "seite-2" and "seite-10". Red if the sort is dropped (the input is reversed) or becomes a plain string sort
    /// ("seite-10" would come before "seite-2").
    /// </summary>
    [Fact]
    public void Fotos_Reihenfolge_Sortiert_Nach_Namen_Mit_Zahlen_Als_Zahlen()
    {
        Assert.Equal(["seite-1.jpg", "Seite-2.jpg", "seite-10.jpg"],
            PageImage.InPageOrder(["seite-10.jpg", "Seite-2.jpg", "seite-1.jpg"]));
    }

    /// <summary>
    /// B-375, AC 6: an unsupported format, a folder without images and a missing file are usage errors naming the path.
    /// Red if the extension check is removed (a HEIC would reach the model), if an empty folder yields zero pages
    /// silently, or if the read error escapes as a raw exception.
    /// </summary>
    [Fact]
    public async Task Fotos_Laden_Lehnt_Falsche_Eingaben_Als_Fehlbedienung_Ab()
    {
        var folder = PhotoFolder(("seite.heic", [1]), ("notiz.txt", [2]));
        try
        {
            var heic = Path.Combine(folder, "seite.heic");
            var missing = Path.Combine(folder, "fehlt.jpg");

            Assert.Contains("seite.heic", (await Assert.ThrowsAsync<AgentUsageException>(
                () => PageImage.LoadAsync([heic], CancellationToken.None))).Message);
            Assert.Contains("kein Bild", (await Assert.ThrowsAsync<AgentUsageException>(
                () => PageImage.LoadAsync([folder], CancellationToken.None))).Message);
            Assert.Contains("fehlt.jpg", (await Assert.ThrowsAsync<AgentUsageException>(
                () => PageImage.LoadAsync([missing], CancellationToken.None))).Message);
            // A mistyped folder names the missing path, not the format (review finding).
            Assert.Contains("gibt es nicht", (await Assert.ThrowsAsync<AgentUsageException>(
                () => PageImage.LoadAsync([Path.Combine(folder, "unti-3")], CancellationToken.None))).Message);
            // A file named next to its own folder is read once (review finding).
            var jpg = Path.Combine(folder, "seite.jpg");
            await File.WriteAllBytesAsync(jpg, [7]);
            Assert.Single(await PageImage.LoadAsync([folder, jpg], CancellationToken.None));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    /// B-375, AC 1/6: on the command line <c>--images</c> alone is a complete source - <c>--source-file</c> is not
    /// required next to it. Red if the command still insists on the source file.
    /// </summary>
    [Fact]
    public async Task Fill_Mit_Images_Braucht_Keine_Quelldatei()
    {
        var (pipeline, creator, _) = BuildAgent();
        var (_, seriesId, unitId) = await FreshUnitAsync(creator, "Fill-Fotos-Kommando");
        var folder = PhotoFolder(("seite-1.jpg", [1]));
        try
        {
            var chat = new FakeChatClient(PageOneExtract, UnitNotesJson);

            var exit = await Commands(creator, pipeline, chat).RunAsync(CommandLine.Parse(
                ["fill", "--series", seriesId.ToString(), "--unit", unitId.ToString(), "--images", folder, "--dry-run"]),
                CancellationToken.None);

            Assert.Equal(0, exit);
            Assert.Equal(2, chat.Calls);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

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

    /// <summary>
    /// A vocabulary exercise as a person (or a loader script) creates it - straight through the API, without the agent.
    /// Only such an exercise is the unit's word list: what the agent generated is left out of it.
    /// </summary>
    private static Task<int> BookVocabularyAsync(CreatorApi creator, int seriesId, int seriesUnitId,
        params (string Front, string Back, string? Hint)[] pairs) =>
        BookVocabularyAsync(creator, seriesId, seriesUnitId, "front-to-back", pairs);

    private static async Task<int> BookVocabularyAsync(CreatorApi creator, int seriesId, int seriesUnitId,
        string direction, params (string Front, string Back, string? Hint)[] pairs)
    {
        var created = await creator.CreateExerciseAsync(seriesId, seriesUnitId, "vocabulary",
            new ExercisePayload<VocabularyConfig>("Wörter aus dem Buch", 1, 10, new VocabularyConfig
            {
                Direction = direction,
                SourceLang = "en",
                TargetLang = "de",
                Items = [.. pairs.Select(p => new VocabItem(p.Front, p.Back, p.Hint))],
            }));
        return created.Id;
    }

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
        var (pipeline, creator, chat) = BuildAgent(VocabularyJson, VocabularyJson);
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
        // B-264: the dressing reaches the prompt on this path too - the request names a child, the briefing does not.
        Assert.Contains(chat.LastMessages, m => m.Text.Contains($"Kleide die Aufgaben in die Interessen ein: {einhorn.Label}"));
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
        // The exact sentence: one spelling twice is named once, not "the horse = the horse" (B-336, AC 2).
        Assert.Contains(chat.LastMessages, m => m.Text.Contains("Vokabeln kommen doppelt vor: the horse."));
    }

    /// <summary>
    /// B-336: two fronts that differ only in what the server folds (accent, apostrophe, doubled space) are the same word
    /// to the server, which refuses the second with a 400 after the draft rule let it through - the run then ended instead
    /// of repairing. Each row differs in exactly one folded property; red if the rule groups the fronts by anything weaker
    /// than <c>VocabKey.Part</c> (the old case-insensitive comparison keeps all three green on the rule and red on the 400).
    /// </summary>
    [Theory]
    [InlineData("the café", "the cafe")]
    [InlineData("rock'n'roll", "rock n roll")]
    [InlineData("the  stable", "the stable")]
    // Case only: the old rule caught it too, but the message must still name both spellings (Ordinal, not IgnoreCase).
    [InlineData("The horse", "the horse")]
    public async Task Eine_Dublette_nach_dem_Store_Schluessel_geht_in_die_Reparatur(string first, string second)
    {
        var broken = $$"""
            {"title":"Tiere","items":[
              {"front":"{{first}}","back":"das Wort","hint":null},
              {"front":"{{second}}","back":"das Wort","hint":null},
              {"front":"the goat","back":"die Ziege","hint":null}]}
            """;
        var (pipeline, creator, chat) = BuildAgent(broken, VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, $"Dublette-{second}");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary"));

        Assert.Equal(2, chat.Calls);
        Assert.True(outcome.Published);
        var repair = Assert.Single(chat.LastMessages, m => m.Text.Contains("doppelt vor")).Text;
        Assert.Contains(first, repair);
        Assert.Contains(second, repair);
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

    /// <summary>
    /// A number as answer must not count as revealed by another gap's placeholder: "3" stands in "{{3}}". Red if the
    /// reveal check reads the placeholders (<c>ReadableText</c>).
    /// </summary>
    [Fact]
    public async Task Eine_Zahl_als_Loesung_wird_nicht_vom_Platzhalter_verraten()
    {
        const string cloze = """
            {"title":"Zahlen","text":"Tom has [two] horses and [3] sheep. He feeds [them] every day.",
             "distractors":["four","us"]}
            """;
        var (pipeline, creator, _) = BuildAgent(cloze);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Cloze-Zahl");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Cloze"));

        Assert.True(outcome.Published, string.Join(" | ", outcome.Violations));
    }

    /// <summary>
    /// The model marks answers in brackets; C# numbers the placeholders, builds the gaps and the word bank. Red if the
    /// conversion numbers wrong (self-test below 100 %, text assertion), if the word bank keeps text order (it would be
    /// the answer key), or if a distractor ends up as an accepted answer - the defect of the old shape, where a model
    /// filled 'alternatives' with its wrong words and the server graded "came" correct for "went".
    /// </summary>
    [Fact]
    public async Task Lueckentext_entsteht_und_besteht_den_Selbsttest()
    {
        const string cloze = """
            {"title":"Auf dem Bauernhof","text":"Every morning Tom feeds the [sheep] and the [horse]. Then he cleans the [stable].",
             "distractors":["tractor","fence"]}
            """;
        var (pipeline, creator, _) = BuildAgent(cloze);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Cloze");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Cloze"));

        Assert.Empty(outcome.Violations);
        Assert.True(outcome.Published, $"Selbsttest: {outcome.SelfTestPercent} %");
        var created = await creator.GetExerciseAsync<ClozeConfig>(seriesId, seriesUnitId, "cloze", outcome.ExerciseId!.Value);
        Assert.Equal("Every morning Tom feeds the {{1}} and the {{2}}. Then he cleans the {{3}}.", created.Config.Text);
        Assert.Equal(["fence", "horse", "sheep", "stable", "tractor"], created.Config.WordBank);
        Assert.All(created.Config.Gaps, g => Assert.Null(g.Alternatives));
    }

    /// <summary>
    /// B-306: a model in JSON mode is not held to the item type - a <c>null</c> among the distractors is left out like
    /// an empty entry, and the exercise is still created. Red if <c>Clean</c> trims before it filters (a
    /// <see cref="NullReferenceException"/> after the draft had passed every rule).
    /// </summary>
    [Fact]
    public async Task Ein_Lueckentext_mit_Null_Eintraegen_entsteht_ohne_sie()
    {
        const string cloze = """
            {"title":"Auf dem Bauernhof","text":"Every morning Tom feeds the [horse|pony] and the [sheep]. Then he cleans the [stable].",
             "distractors":["tractor",null,"fence"]}
            """;
        var (pipeline, creator, _) = BuildAgent(cloze);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Cloze-null");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Cloze"));

        Assert.Empty(outcome.Violations);
        Assert.True(outcome.Published, $"Selbsttest: {outcome.SelfTestPercent} %");
    }

    /// <summary>
    /// Markup the converter cannot read is a rule violation (the repair round can fix it), not a gap without an answer:
    /// an empty bracket, a bracket without its partner, and a <c>{{n}}</c> the model wrote itself out of habit. Red if
    /// any of the three checks in <c>ClozeMarkup.Parse</c> goes - the placeholder one would otherwise reach the server
    /// as a field nobody can answer.
    /// </summary>
    [Fact]
    public async Task Unlesbare_Klammern_sind_eine_Regelverletzung()
    {
        const string cloze = """
            {"title":"Auf dem Bauernhof","text":"Tom feeds the [horse], the [] and the {{3}}. Then he [cleans the stable.",
             "distractors":["tractor","fence"]}
            """;
        var (pipeline, creator, _) = BuildAgent(cloze);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Cloze-Markup");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Cloze"));

        Assert.False(outcome.DraftAccepted);
        Assert.Contains(outcome.Violations, v => v.Contains("ist leer"));
        Assert.Contains(outcome.Violations, v => v.Contains("ohne Gegenstück"));
        Assert.Contains(outcome.Violations, v => v.Contains("{{n}}"));
        Assert.Empty(await creator.SearchExercisesAsync(subjectId: subjectId));
    }

    /// <summary>
    /// B-318: a <c>null</c> item in a vocabulary, translation or grammar draft is a rule violation naming its position,
    /// not a crash. B-306 made the three strategies read their items null-safely (<c>item?.Front</c> …) but tested only
    /// the cloze; red if any <c>?.</c> that reads the items becomes <c>.</c> (NullReferenceException) - in the rule loop,
    /// in <c>NoDuplicates</c>, and in <c>CoversRequiredWords</c>. The last one is only read with required words, which
    /// is why every row names one the real items cover (B-321: without them that mutant stayed green).
    /// </summary>
    [Theory]
    [InlineData("Vocabulary", """
        {"title":"Tiere","items":[{"front":"the horse","back":"das Pferd","hint":null},
          {"front":"the sheep","back":"das Schaf","hint":null},null]}
        """, "Vokabel 3", "the horse")]
    [InlineData("Translation", """
        {"title":"Sätze","items":[{"source":"The horse is fast.","target":"Das Pferd ist schnell.","alternatives":null},
          {"source":"I feed the sheep.","target":"Ich füttere das Schaf.","alternatives":null},null]}
        """, "Satz 3", "horse")]
    [InlineData("Grammar", """
        {"title":"Simple Present","instruction":"Setze das Verb ein.","tasks":[
          {"prompt":"He ___ (to feed) the horse.","answer":"feeds","ruleHint":null},
          {"prompt":"They ___ (to clean) the stable.","answer":"clean","ruleHint":null},null]}
        """, "Aufgabe 3", "feed")]
    public async Task Ein_Null_Element_ist_eine_Regelverletzung_statt_eines_Absturzes(string type, string draft, string position,
        string requiredWord)
    {
        var (pipeline, creator, _) = BuildAgent(draft);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, $"null-Element-{type}");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, type, words: [requiredWord]));

        Assert.False(outcome.DraftAccepted);
        Assert.Contains(outcome.Violations, v => v.StartsWith(position, StringComparison.Ordinal));
        Assert.Empty(await creator.SearchExercisesAsync(subjectId: subjectId));
    }

    /// <summary>
    /// B-306: the same <c>null</c> among a translation item's alternatives - left out, the exercise is created.
    /// Red if the alternatives are trimmed before they are filtered.
    /// </summary>
    [Fact]
    public async Task Eine_Uebersetzung_mit_Null_Alternative_entsteht_ohne_sie()
    {
        const string translation = """
            {"title":"Sätze über Tiere","items":[
              {"source":"The horse is fast.","target":"Das Pferd ist schnell.","alternatives":[null,"Das Pferd ist flink."]},
              {"source":"I feed the sheep.","target":"Ich füttere das Schaf.","alternatives":null},
              {"source":"We clean the stable.","target":"Wir putzen den Stall.","alternatives":null}]}
            """;
        var (pipeline, creator, _) = BuildAgent(translation);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Translation-null");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Translation"));

        Assert.Empty(outcome.Violations);
        Assert.True(outcome.Published, $"Selbsttest: {outcome.SelfTestPercent} %");
    }

    /// <summary>
    /// What the brackets do not make redundant: an answer standing as a word outside the brackets gives the gap away,
    /// a choice list behind a gap ("(sheep, goat)") puts the answer right beside it, and too few real distractors let
    /// the word bank give the answers away by elimination. A distractor that is also an answer does not count as one:
    /// it is dropped, so "horse" leaves only "fence". Red if any of the three rules goes, or if the answer-distractor
    /// is counted (no distractor violation). The text also holds two cases the reveal check must NOT report - "horses"
    /// contains "horse" only as a substring, and the base-form hint "(clean)" after its own gap is part of the task -
    /// so a substring check or one that reads the hints is red too, through the exact list of violations.
    /// </summary>
    [Fact]
    public async Task Eine_verratene_Loesung_eine_Auswahlliste_und_zu_wenige_Ablenker_werden_abgelehnt()
    {
        const string cloze = """
            {"title":"Kaputt","text":"Tom feeds the [horse] and the horses. The sheep is in the field. They [clean] (clean) the [sheep] (sheep, goat).",
             "distractors":["horse","fence"]}
            """;
        var (pipeline, creator, _) = BuildAgent(cloze);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Cloze-kaputt");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Cloze"));

        Assert.False(outcome.DraftAccepted);
        Assert.Collection(outcome.Violations,
            v => Assert.StartsWith("Lücke 3: in die runde Klammer", v),
            v => Assert.StartsWith("Lücke 3: die Lösung 'sheep'", v),
            v => Assert.StartsWith("Es fehlen Ablenker: 1", v));
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
    /// A model that marks more answers than asked for keeps its text: the brackets beyond the count are dissolved into
    /// plain words - with their base-form hint, or "cleans (clean)" would stand in the text - and no repair round is
    /// spent on it. Red if <c>Normalize</c> stops limiting (four gaps against three requested is a count violation),
    /// or if <c>Limit</c> leaves the hint behind. The distractor "goat" doubles a kept answer and is dropped without a
    /// violation; red if that turns back into a rule.
    /// </summary>
    [Fact]
    public async Task Ueberzaehlige_Luecken_werden_zu_Text_statt_zu_einer_Reparatur_Runde()
    {
        const string cloze = """
            {"title":"Zu viele","text":"Tom feeds the [horse], then the [sheep] and the [goat]. Then he [cleans] (clean) the stable.",
             "distractors":["tractor","fence","goat"]}
            """;
        var (pipeline, creator, _) = BuildAgent(cloze);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Cloze-zuviel");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Cloze"));

        Assert.True(outcome.Published, string.Join(" | ", outcome.Violations));
        var created = await creator.GetExerciseAsync<ClozeConfig>(seriesId, seriesUnitId, "cloze", outcome.ExerciseId!.Value);
        Assert.Equal("Tom feeds the {{1}}, then the {{2}} and the {{3}}. Then he cleans the stable.", created.Config.Text);
    }

    /// <summary>
    /// With a word list in the unit, the program decides the words and their translations; the model contributes the
    /// title and the hints. The draft below reorders the words, "corrects" two translations, swaps one word for another
    /// and attaches a hint that gives the answer away. Red if <c>Normalize</c> takes the model's pairs again (backs,
    /// order and the cow), if a revealing hint survives (<c>DropRevealingHint</c>, inflected too:
    /// "Pferde" gives "das Pferd" away) or the book's hint does not stand
    /// in for it ("Reiten"), if the pairs do not reach the prompt with their translations, or if a cloze part gets
    /// them too (it practices a grammar focus). The store ids are asserted as a consequence, not as a claim of their
    /// own: the lookup by word and translation finds the same entries.
    /// </summary>
    [Fact]
    public async Task Mit_Wortliste_der_Unit_stehen_Woerter_und_Uebersetzungen_fest()
    {
        const string modelDraft = """
            {"title":"Unit-Wörter","items":[
              {"front":"the goat","back":"die Geiß","hint":"meckert auf der Weide"},
              {"front":"the horse","back":"der Gaul","hint":"Viele Pferde stehen im Stall"},
              {"front":"the cow","back":"die Kuh","hint":"Milch"}]}
            """;
        const string cloze = """
            {"title":"Lückentext zur Unit","text":"Tom [feeds] the horse, Ann [cleans] the stable and they [play] (play) cards.",
             "distractors":["sleeps","runs"]}
            """;
        var (pipeline, creator, chat) = BuildAgent(modelDraft, cloze);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Wortliste");
        var book = await BookVocabularyAsync(creator, seriesId, seriesUnitId,
            ("the horse", "das Pferd", "Reiten"), ("the sheep", "das Schaf", null), ("the goat", "die Ziege", null));
        var bookItems = await creator.ListItemsAsync(seriesId, seriesUnitId, book);

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary"));

        Assert.True(outcome.Published, string.Join(" | ", outcome.Violations));
        var items = await creator.ListItemsAsync(seriesId, seriesUnitId, outcome.ExerciseId!.Value);
        Assert.Equal(["the horse", "the sheep", "the goat"], items.Select(i => i.Front));
        Assert.Equal(["das Pferd", "das Schaf", "die Ziege"], items.Select(i => i.Back));
        Assert.Equal(bookItems.Select(i => i.VocabularyId), items.Select(i => i.VocabularyId));
        Assert.Equal(["Reiten", null, "meckert auf der Weide"], items.Select(i => i.Hint));
        Assert.Contains("- the horse = das Pferd", chat.LastMessages[1].Text);

        var (_, clozeOutcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Cloze"));
        Assert.True(clozeOutcome.Published, string.Join(" | ", clozeOutcome.Violations));
        Assert.DoesNotContain("Wortschatz dieser Übung", chat.LastMessages[1].Text);
    }

    /// <summary>
    /// B-524, part D: with <c>--schultest</c> a cloze becomes the school's vocabulary test. Each gap links the store
    /// entry of its word from the unit's list, carries the sentence form where it differs, the round brackets leave
    /// the text, and the German distractors go into the cue box instead of an English word bank. On the new stage
    /// the box then shows the book's meanings plus the distractors. Red if the gaps lose their store link (no
    /// meanings in the box), if the form is dropped ("goats" would no longer be the solution), if the hints stay in
    /// the text, or if the distractors land in the word bank again.
    /// </summary>
    [Fact]
    public async Task Schultest_verknuepft_Luecken_mit_der_Wortliste_und_fuellt_den_Wortkasten()
    {
        const string draft = """
            {"title":"Vokabeltest Bauernhof","text":"1. Ann rides her [horse] (the horse) every day.\n2. Two [sheep] (the sheep) stand in the field.\n3. The [goats] (the goat) eat everything.",
             "distractors":["Pferdestall","Schäfer"]}
            """;
        var (pipeline, creator, chat) = BuildAgent(draft);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Schultest");
        await BookVocabularyAsync(creator, seriesId, seriesUnitId,
            ("the horse", "das Pferd", null), ("the sheep", "das Schaf", null), ("the goat", "die Ziege", null));

        var (_, outcome) = await pipeline.CreateAsync(
            Request(subjectId, seriesId, seriesUnitId, "Cloze") with { SchoolTest = true });

        Assert.True(outcome.Published, string.Join(" | ", outcome.Violations));
        Assert.Contains("- the horse = das Pferd", chat.LastMessages[1].Text);
        var created = await creator.GetExerciseAsync<ClozeConfig>(seriesId, seriesUnitId, "cloze", outcome.ExerciseId!.Value);
        Assert.Equal("1. Ann rides her {{1}} every day.\n2. Two {{2}} stand in the field.\n3. The {{3}} eat everything.",
            created.Config.Text);
        Assert.All(created.Config.Gaps, g => Assert.False(string.IsNullOrWhiteSpace(g.VocabKey)));
        Assert.Equal(["horse", "sheep", "goats"], created.Config.Gaps.Select(g => g.Form));
        Assert.Null(created.Config.WordBank);
        Assert.Equal(["Pferdestall", "Schäfer"], created.Config.CueDistractors);

        var preview = await Authenticated().GetFromJsonAsync<JsonElement>(
            $"/api/v1/creator/exercises/{outcome.ExerciseId}/preview?stage={(int)Pugling.Api.Models.ClozeStage.CueBoxFreeText}");
        var box = preview.GetProperty("items")[0].GetProperty("cueBox").EnumerateArray().Select(m => m.GetString());
        Assert.Equal(["das Pferd", "das Schaf", "die Ziege", "Pferdestall", "Schäfer"],
            box.OrderBy(m => m, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// B-524, part D: a gap word outside the unit's list, or a distractor that is a meaning of the list, is refused -
    /// the first leaves the cue box without a meaning, the second puts a second right entry into it. Red if the school
    /// rules are not run (the draft would be published with an unlinked gap).
    /// </summary>
    [Fact]
    public async Task Schultest_weist_fremde_Woerter_und_Bedeutungen_als_Ablenker_zurueck()
    {
        const string draft = """
            {"title":"Vokabeltest Bauernhof","text":"1. Ann rides her [horse] (the horse) every day.\n2. The [cow] (the cow) gives milk.",
             "distractors":["das Pferd","Schäfer"]}
            """;
        var (pipeline, creator, _) = BuildAgent(draft, draft);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Schultest-fremd");
        await BookVocabularyAsync(creator, seriesId, seriesUnitId,
            ("the horse", "das Pferd", null), ("the sheep", "das Schaf", null), ("the goat", "die Ziege", null));

        var (_, outcome) = await pipeline.CreateAsync(
            Request(subjectId, seriesId, seriesUnitId, "Cloze", count: 2) with { SchoolTest = true });

        Assert.False(outcome.Published);
        Assert.Contains(outcome.Violations, v => v.Contains("'the cow' steht nicht in der Wortliste"));
        Assert.Contains(outcome.Violations, v => v.Contains("Ablenker 'das Pferd'"));
    }

    /// <summary>
    /// B-524, part D (review): a required word is met by its base form in the round bracket, not by the sentence form,
    /// and a required word outside the unit's list (a weak word of an earlier unit) is not demanded - it has no store
    /// entry in the slice, so asking for it would contradict the school rules in every repair round. Red if the school
    /// test checks the required words against the sentence forms ("goats" never equals "the goat") or against the
    /// whole required list ("the cow" would be missing forever).
    /// </summary>
    [Fact]
    public async Task Schultest_misst_Pflichtwoerter_an_der_Grundform_und_nur_aus_der_Liste()
    {
        const string draft = """
            {"title":"Vokabeltest Bauernhof","text":"1. Ann rides her [horse] (the horse) every day.\n2. Two [sheep] (the sheep) stand in the field.\n3. The [goats] (the goat) eat everything.",
             "distractors":["Pferdestall","Schäfer"]}
            """;
        var (pipeline, creator, _) = BuildAgent(draft);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Schultest-Pflicht");
        await BookVocabularyAsync(creator, seriesId, seriesUnitId,
            ("the horse", "das Pferd", null), ("the sheep", "das Schaf", null), ("the goat", "die Ziege", null));

        var (_, outcome) = await pipeline.CreateAsync(
            Request(subjectId, seriesId, seriesUnitId, "Cloze", words: ["the goat", "the cow"]) with { SchoolTest = true });

        Assert.True(outcome.Published, string.Join(" | ", outcome.Violations));
    }

    /// <summary>
    /// A title the unit already has gets a number instead of costing a repair round. Red if <c>UniqueTitle</c> goes:
    /// the second draft then breaks the title rule in every round and is refused.
    /// </summary>
    [Fact]
    public async Task Ein_vergebener_Titel_bekommt_eine_Nummer()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Titel-doppelt");
        await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary"));

        var (_, second) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary"));

        Assert.True(second.Published, string.Join(" | ", second.Violations));
        Assert.Equal("Tiere auf dem Bauernhof (2)", second.Title);
    }

    /// <summary>
    /// The printed draft is for a human: umlauts stay umlauts. Red if the print options go back to the default
    /// encoder ("für" instead of "für").
    /// </summary>
    [Fact]
    public async Task Der_gedruckte_Entwurf_behaelt_Umlaute()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson.Replace("Tiere auf dem Bauernhof", "Tiere für Kinder"));
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Umlaute");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary", dryRun: true));

        Assert.Contains("\"Tiere für Kinder\"", outcome.DraftJson);
    }

    /// <summary>
    /// Which words an exercise gets: weak words of the unit first, then the next slice per part, wrapping at the end.
    /// Red if <c>Slice</c> ignores the part (part 1 equals part 0), stops wrapping (part 2 comes back short), or
    /// drops the weak words.
    /// </summary>
    [Fact]
    public void Jeder_Teil_nimmt_die_naechste_Scheibe_der_Wortliste()
    {
        VocabPair[] pool = [new("a", "A", 1), new("b", "B", 2), new("c", "C", 3), new("d", "D", 4), new("e", "E", 5)];

        Assert.Equal(["a", "b"], UnitVocabulary.Slice(pool, [], 2, 0).Select(p => p.Word));
        Assert.Equal(["c", "d"], UnitVocabulary.Slice(pool, [], 2, 1).Select(p => p.Word));
        Assert.Equal(["e", "a"], UnitVocabulary.Slice(pool, [], 2, 2).Select(p => p.Word));
        Assert.Equal(["d", "a"], UnitVocabulary.Slice(pool, ["D", "unbekannt"], 2, 0).Select(p => p.Word));
    }

    /// <summary>
    /// B-463: with a book word list in the unit, the study plan assigns the book's vocabulary exercises - the whole
    /// list - instead of generating ten-word slices, and gives each the settings a long list needs: Leitner box, a
    /// daily dose that finishes the list by the end of the run, a typed test on the free-text stage.
    /// <para>
    /// Red probes, one per claim: generate again for Vocabulary and the call count falls (and nothing is assigned);
    /// plan every book exercise instead of those adding a word and list C shows up as a third position; drop a
    /// setting from <c>BookListPosition</c> and its assertion falls; round the dose down (5 / 3 = 1) or count only a list's new
    /// words (3 / 3 = 1) and the dose assertion falls. Naming Vocabulary twice must not plan the lists twice.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Ein_Lehrplan_plant_die_ganze_Buch_Wortliste_mit_Karteikasten_und_Tagesdosis_ein()
    {
        var (pipeline, creator, chat) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Lehrplan-Buchliste");
        var listA = await BookVocabularyAsync(creator, seriesId, seriesUnitId,
            ("the horse", "das Pferd", null), ("the sheep", "das Schaf", null), ("the goat", "die Ziege", null),
            ("the cow", "die Kuh", null), ("the pig", "das Schwein", null));
        var listB = await BookVocabularyAsync(creator, seriesId, seriesUnitId,
            ("the duck", "die Ente", null), ("the hen", "das Huhn", null), ("the cat", "die Katze", null),
            ("the horse", "das Pferd", null));
        // A list that adds no word of its own is a duplicate of the others, not a part of the word list.
        await BookVocabularyAsync(creator, seriesId, seriesUnitId,
            ("the horse", "das Pferd", null), ("the sheep", "das Schaf", null), ("the goat", "die Ziege", null));
        var supervisor = new SupervisorApi(Authenticated());

        var outcome = await new PlanPlanner(pipeline, supervisor).RunAsync(new PlanRequest(
            Request(subjectId, seriesId, seriesUnitId, "Vocabulary"),
            Types: ["Vocabulary", "Vocabulary"], PerType: 2, Intensity: PlanIntensity.Normal,
            StartDate: null, DurationDays: 3, Title: null));

        Assert.Equal(0, chat.Calls);
        Assert.Empty(outcome.ExerciseIds);
        Assert.True(outcome.Complete);
        var positions = await supervisor.ListPositionsAsync(outcome.PlanId!.Value);
        Assert.Equal([listA, listB], positions.Select(p => p.ExerciseId));
        Assert.All(positions, p =>
        {
            Assert.True(p.UseLeitner);
            Assert.True(p.RequireTypedTest);
            Assert.Equal(4, p.Stage);
        });
        // ⌈5 / 3⌉ and ⌈4 / 3⌉: the dose counts every entry the position plays, the overlap word included - counting
        // only list B's three new words would give 1.
        Assert.Equal([2, 2], positions.Select(p => p.NewItemsPerDay));
    }

    /// <summary>
    /// B-472: a book list asked both ways plays two cards per word, and the dose counts cards - so the plan doubles it to
    /// finish the list by the last day. Red if <c>UnitWordList.ItemCount</c> counts rows again (⌈4 / 3⌉ = 2).
    /// </summary>
    [Fact]
    public async Task Eine_Buchliste_in_beiden_Richtungen_bekommt_die_Dosis_ihrer_Karten()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Lehrplan-beide");
        await BookVocabularyAsync(creator, seriesId, seriesUnitId, "both",
            ("the horse", "das Pferd", null), ("the sheep", "das Schaf", null), ("the goat", "die Ziege", null),
            ("the cow", "die Kuh", null));
        var supervisor = new SupervisorApi(Authenticated());

        var outcome = await new PlanPlanner(pipeline, supervisor).RunAsync(new PlanRequest(
            Request(subjectId, seriesId, seriesUnitId, "Vocabulary"),
            Types: ["Vocabulary"], PerType: 1, Intensity: PlanIntensity.Normal,
            StartDate: null, DurationDays: 3, Title: null));

        Assert.Equal(8, Assert.Single(outcome.WordLists).ItemCount);
        // ⌈8 / 3⌉: eight cards over three days.
        Assert.Equal(3, Assert.Single(await supervisor.ListPositionsAsync(outcome.PlanId!.Value)).NewItemsPerDay);
    }

    /// <summary>
    /// What the agent generated itself is no word list: on a unit without book vocabulary, the second vocabulary part
    /// of a plan must not take the first part's model-chosen words as "the unit's". And a book list too short for an
    /// exercise (two words) leaves the choice to the model instead of failing every round. Red if the briefing reads
    /// generated exercises into the pool (part B repeats part A's words), or if <c>FixedPairs</c> takes a list below
    /// <c>DraftRules.MinItems</c> (part A fails with two cards).
    /// </summary>
    [Fact]
    public async Task Eigene_KI_Uebungen_und_eine_zu_kurze_Liste_sind_keine_Wortliste()
    {
        const string partB = """
            {"title":"Teil B","items":[
              {"front":"the cow","back":"die Kuh","hint":null},{"front":"the pig","back":"das Schwein","hint":null},
              {"front":"the duck","back":"die Ente","hint":null}]}
            """;
        var (pipeline, creator, _) = BuildAgent(VocabularyJson, partB);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Keine-Wortliste");
        await BookVocabularyAsync(creator, seriesId, seriesUnitId, ("the cat", "die Katze", null), ("the dog", "der Hund", null));

        var outcome = await new PlanPlanner(pipeline, new SupervisorApi(Authenticated())).RunAsync(new PlanRequest(
            Request(subjectId, seriesId, seriesUnitId, "Vocabulary"),
            Types: ["Vocabulary"], PerType: 2, Intensity: PlanIntensity.Normal,
            StartDate: null, DurationDays: 14, Title: null));

        Assert.True(outcome.Complete, string.Join(" | ", outcome.Parts.Select(p => p.Error ?? string.Join(" | ", p.Outcome!.Violations))));
        var words = new List<string[]>();
        foreach (var id in outcome.ExerciseIds)
            words.Add([.. (await creator.ListItemsAsync(seriesId, seriesUnitId, id)).Select(i => i.Front)]);
        Assert.Equal(["the horse", "the sheep", "the goat"], words[0]);
        Assert.Equal(["the cow", "the pig", "the duck"], words[1]);
    }

    /// <summary>
    /// The grammar parts of a plan take the unit's grammar topics in turn; the other types keep their part number.
    /// Red if the rotation always takes the first topic, or if a second round on a topic reads like the first (the
    /// same topic text twice is the same exercise twice). Which types count as grammar is <c>RunAsync</c>'s business
    /// and not claimed here.
    /// </summary>
    [Fact]
    public void Grammatik_Teile_eines_Lehrplans_bekommen_je_ein_Thema_der_Unit()
    {
        string[] topics = ["Question tags", "Past progressive"];

        Assert.EndsWith("Schwerpunkt: Question tags", PlanPlanner.PartTopic("Unit 1", "Cloze", 1, 2, topics, 0));
        Assert.EndsWith("Schwerpunkt: Past progressive", PlanPlanner.PartTopic("Unit 1", "Cloze", 2, 2, topics, 1));
        Assert.EndsWith("Schwerpunkt: Question tags (Teil 2)", PlanPlanner.PartTopic("Unit 1", "Grammar", 1, 1, topics, 2));
        Assert.Equal("Unit 1 (Lehrplan, Vocabulary 1/2)", PlanPlanner.PartTopic("Unit 1", "Vocabulary", 1, 2, topics, -1));
    }

    /// <summary>
    /// B-458: a grammar task whose prompt already contains its answer is rejected - the child could copy it. The base
    /// form in round brackets does not count: "They ___ (play) football." with the answer "play" is a real task.
    /// Red if <c>AnswerNotInPrompt</c> goes (no violation), or if it reads the brackets (two violations).
    /// </summary>
    [Fact]
    public async Task Eine_Grammatikaufgabe_mit_der_Loesung_im_Text_wird_abgelehnt()
    {
        const string grammar = """
            {"title":"Past progressive","instruction":"Setze die richtige Form ein.","tasks":[
              {"prompt":"They were playing football. While they ___ (play), it started to rain.","answer":"were playing","ruleHint":null},
              {"prompt":"They ___ (play) football every Sunday.","answer":"play","ruleHint":null},
              {"prompt":"She ___ (read) when the phone rang.","answer":"was reading","ruleHint":null}]}
            """;
        var (pipeline, creator, _) = BuildAgent(grammar);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Grammar-verraten");

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Grammar"));

        Assert.False(outcome.DraftAccepted);
        Assert.StartsWith("Aufgabe 1: die Lösung 'were playing' steht schon", Assert.Single(outcome.Violations));
        Assert.Empty(await creator.SearchExercisesAsync(subjectId: subjectId));
    }

    /// <summary>
    /// B-473: with <c>--child</c> and no place named, the exercise belongs in the child's book - the series of the
    /// best-matching profile, at the child's current unit there - not in the first series of the catalog. Red if the
    /// location falls back to "first series with a unit" (the exercise lands elsewhere) or to the series' first unit
    /// (it lands in unit 1, not the current unit 2).
    /// </summary>
    [Fact]
    public async Task Ohne_Ortsangabe_landet_die_Uebung_im_Buch_des_Kindes_bei_seiner_Unit()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var supervisor = new SupervisorApi(Authenticated());
        var subject = await creator.CreateSubjectAsync(new CreateSubjectDto($"Agent-Test Buch des Kindes {Guid.NewGuid():N}"));
        var series = await creator.CreateSeriesAsync(new CreateTextbookSeriesDto(
            $"Buch des Kindes {Guid.NewGuid():N}", null, subject.Name, subject.Id, SchoolTypes.Gymnasium, "en", "de", null));
        var first = await creator.CreateUnitAsync(series.Id, new CreateSeriesUnitDto("Unit 1", 8, 1, null, null, null, null));
        var current = await creator.CreateUnitAsync(series.Id, new CreateSeriesUnitDto("Unit 2", 8, 2, null, null, null, null));
        var child = await supervisor.CreateChildAsync(new CreateChildDto($"Buchkind {Guid.NewGuid():N}"[..20], null, 8,
            SchoolTypes.Gymnasium, null));
        await supervisor.CreateTextbookAsync(child.Id, new CreateTextbookDto("Access", subject.Name, subject.Id, 8, null, null, null,
            SeriesId: series.Id, CurrentUnitId: current.Id));
        await creator.CreateProfileAsync(new CreateCreatorProfileDto(
            $"Lehrer für das Buch {Guid.NewGuid():N}", subject.Name, subject.Id, SchoolTypes.Gymnasium, GradeMin: 7, GradeMax: 9,
            SeriesId: series.Id, SourceLang: "en", TargetLang: "de", Persona: null, Didactics: null,
            DefaultTypes: ["Vocabulary"], Active: true));
        var commands = Commands(creator, pipeline);

        var command = CommandLine.Parse(["create", "--child", child.Id.ToString(), "--type", "Vocabulary", "--count", "3"]);
        Assert.Equal(0, await commands.RunAsync(command, default));

        Assert.Single(await creator.SearchExercisesAsync(seriesUnitId: current.Id));
        Assert.Empty(await creator.SearchExercisesAsync(seriesUnitId: first.Id));
    }

    /// <summary>
    /// B-473, decision 2: a child whose books no profile knows gets a usage error - other profiles fit grade and school
    /// type and would otherwise be picked, which is the guessed shelf again. Red if any matching profile with a series
    /// is taken (the exercise is created somewhere instead).
    /// </summary>
    [Fact]
    public async Task Ohne_Profil_zum_Buch_des_Kindes_meldet_der_Agent_eine_Fehlbedienung()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var supervisor = new SupervisorApi(Authenticated());
        // A profile with a series that fits the child by grade and school type alone - not by its books.
        var (subjectId, _, _) = await FreshUnitAsync(creator, "Fremdes Regal");
        await FreshProfileAsync(creator, subjectId, "Fremdes Regal");
        var child = await supervisor.CreateChildAsync(new CreateChildDto($"Ohne Buch {Guid.NewGuid():N}"[..20], null, 8,
            SchoolTypes.Gymnasium, null));

        var command = CommandLine.Parse(["create", "--child", child.Id.ToString(), "--type", "Vocabulary", "--count", "3"]);
        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline).RunAsync(command, default));

        // Its own message, not just "--profile": the list of several books names that option too (B-481).
        Assert.Contains("kennt kein Creator-Profil", error.Message);
    }

    /// <summary>
    /// B-481, AK 1: the child's current unit applies wherever the series comes from - named with <c>--series</c> or
    /// with <c>--profile</c>. Red if only the "no place named" branch looks at the child's book (both land in unit 1).
    /// </summary>
    [Theory]
    [InlineData("--series")]
    [InlineData("--profile")]
    public async Task Mit_genannter_Reihe_landet_die_Uebung_bei_der_Unit_des_Kindes(string option)
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var supervisor = new SupervisorApi(Authenticated());
        var subject = await creator.CreateSubjectAsync(new CreateSubjectDto($"Agent-Test Reihe genannt {Guid.NewGuid():N}"));
        var series = await creator.CreateSeriesAsync(new CreateTextbookSeriesDto(
            $"Reihe genannt {Guid.NewGuid():N}", null, subject.Name, subject.Id, SchoolTypes.Gymnasium, "en", "de", null));
        await creator.CreateUnitAsync(series.Id, new CreateSeriesUnitDto("Unit 1", 8, 1, null, null, null, null));
        var current = await creator.CreateUnitAsync(series.Id, new CreateSeriesUnitDto("Unit 2", 8, 2, null, null, null, null));
        var child = await supervisor.CreateChildAsync(new CreateChildDto($"Reihenkind {Guid.NewGuid():N}"[..20], null, 8,
            SchoolTypes.Gymnasium, null));
        // A book in another series comes first: the unit must be taken from the book of the named series, not any book.
        var (otherSubject, otherSeries, otherUnit) = await FreshUnitAsync(creator, "Anderes Buch");
        await supervisor.CreateTextbookAsync(child.Id, new CreateTextbookDto("Latein", null, otherSubject, 8, null, null, null,
            SeriesId: otherSeries, CurrentUnitId: otherUnit));
        await supervisor.CreateTextbookAsync(child.Id, new CreateTextbookDto("Access", subject.Name, subject.Id, 8, null, null, null,
            SeriesId: series.Id, CurrentUnitId: current.Id));
        var profile = await creator.CreateProfileAsync(new CreateCreatorProfileDto(
            $"Lehrer Reihe genannt {Guid.NewGuid():N}", subject.Name, subject.Id, SchoolTypes.Gymnasium, GradeMin: 7, GradeMax: 9,
            SeriesId: series.Id, SourceLang: "en", TargetLang: "de", Persona: null, Didactics: null,
            DefaultTypes: ["Vocabulary"], Active: true));

        var value = option == "--series" ? series.Id : profile.Id;
        var command = CommandLine.Parse(["create", "--child", child.Id.ToString(), option, value.ToString(),
            "--type", "Vocabulary", "--count", "3"]);
        Assert.Equal(0, await Commands(creator, pipeline).RunAsync(command, default));

        Assert.Single(await creator.SearchExercisesAsync(seriesUnitId: current.Id));
    }

    /// <summary>
    /// B-481, review: the unit named for the material (<c>--unit</c>) is also where the exercise goes - the child's
    /// current unit must not overrule it. Red if the child's unit wins (it lands in unit 2, the briefing used unit 3).
    /// </summary>
    [Fact]
    public async Task Genannte_Unit_geht_vor_der_Unit_des_Kindes()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var supervisor = new SupervisorApi(Authenticated());
        var subject = await creator.CreateSubjectAsync(new CreateSubjectDto($"Agent-Test Unit genannt {Guid.NewGuid():N}"));
        var series = await creator.CreateSeriesAsync(new CreateTextbookSeriesDto(
            $"Unit genannt {Guid.NewGuid():N}", null, subject.Name, subject.Id, SchoolTypes.Gymnasium, "en", "de", null));
        var current = await creator.CreateUnitAsync(series.Id, new CreateSeriesUnitDto("Unit 2", 8, 2, null, null, null, null));
        var named = await creator.CreateUnitAsync(series.Id, new CreateSeriesUnitDto("Unit 3", 8, 3, null, null, null, null));
        var child = await supervisor.CreateChildAsync(new CreateChildDto($"Unitkind {Guid.NewGuid():N}"[..20], null, 8,
            SchoolTypes.Gymnasium, null));
        await supervisor.CreateTextbookAsync(child.Id, new CreateTextbookDto("Access", subject.Name, subject.Id, 8, null, null, null,
            SeriesId: series.Id, CurrentUnitId: current.Id));

        var command = CommandLine.Parse(["create", "--child", child.Id.ToString(), "--series", series.Id.ToString(),
            "--unit", named.Id.ToString(), "--type", "Vocabulary", "--count", "3"]);
        Assert.Equal(0, await Commands(creator, pipeline).RunAsync(command, default));

        Assert.Single(await creator.SearchExercisesAsync(seriesUnitId: named.Id));
    }

    /// <summary>
    /// B-481, review: a child this account cannot see, with a series named, still gets the usage message of the briefing
    /// ("nicht sichtbar", exit 2) - not the bare 404 of the textbook lookup that now runs first. Red if that lookup's
    /// API error escapes.
    /// </summary>
    [Fact]
    public async Task Unsichtbares_Kind_mit_Reihe_meldet_eine_Fehlbedienung()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (_, seriesId, _) = await FreshUnitAsync(creator, "Fremdes Kind");

        var command = CommandLine.Parse(["create", "--child", "999999", "--series", seriesId.ToString(),
            "--type", "Vocabulary", "--count", "3"]);
        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline).RunAsync(command, default));

        Assert.Contains("nicht sichtbar", error.Message);
    }

    /// <summary>
    /// B-481, AK 2: two books with a profile each are a choice of subject even when the scores differ - here the English
    /// profile earns a point for its grade bounds, the Latin one has none. Red if the higher score picks the subject.
    /// </summary>
    [Fact]
    public async Task Zwei_Buecher_mit_verschiedenen_Punkten_verlangen_trotzdem_die_Wahl()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var supervisor = new SupervisorApi(Authenticated());
        var child = await supervisor.CreateChildAsync(new CreateChildDto($"Ungleich {Guid.NewGuid():N}"[..20], null, 8,
            SchoolTypes.Gymnasium, null));
        foreach (var (name, gradeMin, gradeMax) in new[] { ("Englisch-Buch", (int?)7, (int?)9), ("Latein-Buch", null, null) })
        {
            var (subjectId, seriesId, unitId) = await FreshUnitAsync(creator, name);
            await supervisor.CreateTextbookAsync(child.Id, new CreateTextbookDto(name, null, subjectId, 8, null, null, null,
                SeriesId: seriesId, CurrentUnitId: unitId));
            await creator.CreateProfileAsync(new CreateCreatorProfileDto($"{name} {Guid.NewGuid():N}", null, subjectId,
                SchoolTypes.Gymnasium, GradeMin: gradeMin, GradeMax: gradeMax, SeriesId: seriesId, SourceLang: "en",
                TargetLang: "de", Persona: null, Didactics: null, DefaultTypes: ["Vocabulary"], Active: true));
        }
        var matches = await creator.MatchProfilesAsync(child.Id, null, default);
        var onBooks = matches.Where(m => m.Reasons.Contains("series_match")).Select(m => m.Score).Distinct().ToList();
        Assert.True(onBooks.Count == 2, "Setup: the two book profiles must score differently.");

        var command = CommandLine.Parse(["create", "--child", child.Id.ToString(), "--type", "Vocabulary", "--count", "3"]);
        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline).RunAsync(command, default));

        Assert.Contains("mehrere Lehrbücher", error.Message);
    }

    /// <summary>
    /// B-473: two books, each with its profile, score alike - the agent asks instead of letting the lower id decide the
    /// subject. Red if the first of the tied matches is taken.
    /// </summary>
    [Fact]
    public async Task Zwei_gleich_gute_Buecher_verlangen_die_Wahl_des_Profils()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var supervisor = new SupervisorApi(Authenticated());
        var child = await supervisor.CreateChildAsync(new CreateChildDto($"Zwei Buecher {Guid.NewGuid():N}"[..20], null, 8,
            SchoolTypes.Gymnasium, null));
        foreach (var name in new[] { "Englisch-Buch", "Latein-Buch" })
        {
            var (subjectId, seriesId, unitId) = await FreshUnitAsync(creator, name);
            await supervisor.CreateTextbookAsync(child.Id, new CreateTextbookDto(name, null, subjectId, 8, null, null, null,
                SeriesId: seriesId, CurrentUnitId: unitId));
            await creator.CreateProfileAsync(new CreateCreatorProfileDto($"{name} {Guid.NewGuid():N}", null, subjectId,
                SchoolTypes.Gymnasium, GradeMin: 7, GradeMax: 9, SeriesId: seriesId, SourceLang: "en", TargetLang: "de",
                Persona: null, Didactics: null, DefaultTypes: ["Vocabulary"], Active: true));
        }

        var command = CommandLine.Parse(["create", "--child", child.Id.ToString(), "--type", "Vocabulary", "--count", "3"]);
        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline).RunAsync(command, default));

        Assert.Contains("mehrere Lehrbücher", error.Message);
    }

    /// <summary>
    /// A child with a book in a fresh subject (units 1 and 2, current 2), and two profiles of that subject: one on the
    /// book's series without grade bounds, one on another series that scores higher (grades 7-9; with a profile on the
    /// book the child also has a book of the other work, so both earn the series bonus). The setup asserts the
    /// scores differ, so "the profile on the target series" and "the best score" name different profiles.
    /// </summary>
    private async Task<(CreatorApi Creator, CreatorPipeline Pipeline, FakeChatClient Chat, ChildResponse Child,
        TextbookSeriesResponse Series, SeriesUnitResponse First, SeriesUnitResponse Current, CreatorProfileResponse OnSeries,
        CreatorProfileResponse Elsewhere)> TwoTeachersAsync(string name, bool profileOnSeries = true,
        bool bookTeacherActive = true, params string[] responses)
    {
        var (pipeline, creator, chat) = BuildAgent(responses.Length > 0 ? responses : [VocabularyJson]);
        var supervisor = new SupervisorApi(Authenticated());
        var subject = await creator.CreateSubjectAsync(new CreateSubjectDto($"Agent-Test {name} {Guid.NewGuid():N}"));
        var series = await creator.CreateSeriesAsync(new CreateTextbookSeriesDto(
            $"{name} Buch {Guid.NewGuid():N}", null, subject.Name, subject.Id, SchoolTypes.Gymnasium, "en", "de", null));
        var first = await creator.CreateUnitAsync(series.Id, new CreateSeriesUnitDto("Unit 1", 8, 1, null, null, null, null));
        var current = await creator.CreateUnitAsync(series.Id, new CreateSeriesUnitDto("Unit 2 – Buchstoff", 8, 2, null,
            ["Buchthema Tower Bridge"], null, null));
        var other = await creator.CreateSeriesAsync(new CreateTextbookSeriesDto(
            $"{name} Fremdwerk {Guid.NewGuid():N}", null, subject.Name, subject.Id, SchoolTypes.Gymnasium, "en", "de", null));
        await creator.CreateUnitAsync(other.Id, new CreateSeriesUnitDto("Unit 9 – Fremdstoff", 8, 9, null, null, null, null));
        var child = await supervisor.CreateChildAsync(new CreateChildDto($"{name} {Guid.NewGuid():N}"[..20], null, 8,
            SchoolTypes.Gymnasium, null));
        await supervisor.CreateTextbookAsync(child.Id, new CreateTextbookDto("Access", subject.Name, subject.Id, 8, null, null, null,
            SeriesId: series.Id, CurrentUnitId: current.Id));
        // With a profile on the book, the child also has a book of the other work in the same subject: only then does the
        // other teacher earn the series bonus too, and the grade bounds put that teacher ahead.
        if (profileOnSeries)
            await supervisor.CreateTextbookAsync(child.Id, new CreateTextbookDto("Fremdwerk", subject.Name, subject.Id, 8, null,
                null, null, SeriesId: other.Id, CurrentUnitId: null));
        var onSeries = await creator.CreateProfileAsync(new CreateCreatorProfileDto($"{name} Buchlehrer {Guid.NewGuid():N}",
            subject.Name, subject.Id, SchoolTypes.Gymnasium, GradeMin: null, GradeMax: null,
            SeriesId: profileOnSeries ? series.Id : null, SourceLang: "en", TargetLang: "de",
            Persona: "Du bist der Lehrer DIESES Buchs.", Didactics: null, DefaultTypes: ["Vocabulary"],
            Active: bookTeacherActive));
        var elsewhere = await creator.CreateProfileAsync(new CreateCreatorProfileDto($"{name} Fremdlehrer {Guid.NewGuid():N}",
            subject.Name, subject.Id, SchoolTypes.Gymnasium, GradeMin: 7, GradeMax: 9, SeriesId: other.Id,
            SourceLang: "en", TargetLang: "de", Persona: "Du bist der Lehrer des FREMDEN Werks.", Didactics: null,
            DefaultTypes: ["Vocabulary"], Active: true));
        var scores = (await creator.MatchProfilesAsync(child.Id, subject.Id, default))
            .Where(m => m.Profile.Id == onSeries.Id || m.Profile.Id == elsewhere.Id)
            .ToDictionary(m => m.Profile.Id, m => m.Score);
        Assert.True(bookTeacherActive ? scores.Count == 2 && scores[elsewhere.Id] > scores[onSeries.Id]
                : scores.Keys.SequenceEqual([elsewhere.Id]),
            "Setup: the profile on the other series must score higher (or be the only match).");
        return (creator, pipeline, chat, child, series, first, current, onSeries, elsewhere);
    }

    /// <summary>
    /// B-484, AK 1: with <c>--child</c> and a place named, the profile is the one on the target series - not the best
    /// score, which here belongs to a teacher of another work. Red if the briefing takes the first match again (the
    /// foreign persona is in the system prompt), or if the material is read from the profile's series instead of the
    /// place (the foreign unit is in the prompt).
    /// </summary>
    [Theory]
    [InlineData("--series")]
    [InlineData("--series-unit")]
    public async Task Mit_genanntem_Ort_spricht_der_Lehrer_dieses_Buchs(string option)
    {
        var (creator, pipeline, chat, child, series, _, current, _, _) = await TwoTeachersAsync("Ort genannt");

        string[] place = option == "--series"
            ? ["--series", series.Id.ToString()]
            : ["--series", series.Id.ToString(), "--series-unit", current.Id.ToString()];
        var command = CommandLine.Parse(["create", "--child", child.Id.ToString(), .. place, "--type", "Vocabulary", "--count", "3"]);
        Assert.Equal(0, await Commands(creator, pipeline).RunAsync(command, default));

        var system = Assert.Single(chat.LastMessages, m => m.Role == ChatRole.System).Text;
        Assert.StartsWith("Du bist der Lehrer DIESES Buchs.", system);
        var prompt = string.Join("\n", chat.LastMessages.Select(m => m.Text));
        Assert.Contains("Buchthema Tower Bridge", prompt);
        Assert.DoesNotContain("Fremdstoff", prompt);
        Assert.DoesNotContain("FREMDEN", prompt);
        Assert.Single(await creator.SearchExercisesAsync(seriesUnitId: current.Id));
    }

    /// <summary>
    /// B-484, AK 1, no profile on the target series: a teacher without a series speaks, never the teacher of another
    /// work, and the unit's material is in the prompt either way. Red if the higher-scoring foreign profile is taken, or
    /// if profiles without a series are filtered out too (no persona).
    /// </summary>
    [Fact]
    public async Task Ohne_Lehrer_dieses_Buchs_spricht_ein_werkunabhaengiger_statt_eines_fremden()
    {
        var (creator, pipeline, chat, child, series, _, current, _, _) =
            await TwoTeachersAsync("Ohne Buchlehrer", profileOnSeries: false);

        var command = CommandLine.Parse(["create", "--child", child.Id.ToString(), "--series", series.Id.ToString(),
            "--type", "Vocabulary", "--count", "3"]);
        Assert.Equal(0, await Commands(creator, pipeline).RunAsync(command, default));

        var system = Assert.Single(chat.LastMessages, m => m.Role == ChatRole.System).Text;
        Assert.StartsWith("Du bist der Lehrer DIESES Buchs.", system);
        var prompt = string.Join("\n", chat.LastMessages.Select(m => m.Text));
        Assert.DoesNotContain("FREMDEN", prompt);
        Assert.Contains("Buchthema Tower Bridge", prompt);
        Assert.Single(await creator.SearchExercisesAsync(seriesUnitId: current.Id));
    }

    /// <summary>
    /// B-484, review: with no profile on the book, the material still comes from the place - series and unit in the
    /// prompt, and the exercise's source names them, not the child's free-text book title. The command line may still
    /// find a subject-neutral profile of the shared database, so "no profile at all" is pinned through the pipeline
    /// without child and profile. Red if the material hangs off the profile again (the unit's topic is missing) or the
    /// source falls back to the child's book ("Access").
    /// </summary>
    [Fact]
    public async Task Ohne_jeden_Lehrer_bleibt_der_Stoff_der_Unit_und_die_Quelle_des_Werks()
    {
        var (creator, pipeline, chat, child, series, _, current, _, _) =
            await TwoTeachersAsync("Ohne Lehrer", profileOnSeries: false, bookTeacherActive: false);

        var command = CommandLine.Parse(["create", "--child", child.Id.ToString(), "--series", series.Id.ToString(),
            "--type", "Vocabulary", "--count", "3"]);
        Assert.Equal(0, await Commands(creator, pipeline).RunAsync(command, default));

        var prompt = string.Join("\n", chat.LastMessages.Select(m => m.Text));
        Assert.DoesNotContain("Du bist der Lehrer", prompt);
        Assert.Contains($"Lehrwerk: {series.Name}", prompt);
        Assert.Contains("Buchthema Tower Bridge", prompt);
        var exercise = Assert.Single(await creator.SearchExercisesAsync(seriesUnitId: current.Id));
        var detail = await creator.GetExerciseDetailAsync(exercise.Id);
        Assert.Equal($"{series.Name}, Klasse 8, Unit 2 – Buchstoff", detail.Source);

        var bare = await pipeline.BriefAsync(Request(series.SubjectId!.Value, series.Id, current.Id, "Vocabulary", childId: null));
        Assert.Null(bare.Profile);
        Assert.Contains("Buchthema Tower Bridge", bare.ToPromptText());
    }

    /// <summary>
    /// B-484, review: a named profile of one work next to a named place in another is a usage error - its persona would
    /// speak about a book the material is not from. Red if the combination passes (exit 0).
    /// </summary>
    [Fact]
    public async Task Profil_einer_anderen_Reihe_neben_genanntem_Ort_ist_eine_Fehlbedienung()
    {
        var (creator, pipeline, _, child, series, _, _, _, elsewhere) = await TwoTeachersAsync("Profil fremd");

        var command = CommandLine.Parse(["create", "--child", child.Id.ToString(), "--profile", elsewhere.Id.ToString(),
            "--series", series.Id.ToString(), "--type", "Vocabulary", "--count", "3"]);
        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline).RunAsync(command, default));

        Assert.Contains("gehört zur Reihe", error.Message);
    }

    /// <summary>B-488: a series in subject B and a teacher for every book of subject A.</summary>
    private async Task<(CreatorPipeline Pipeline, CreatorApi Creator, FakeChatClient Chat, int SubjectA, int SeriesB,
        int UnitB, CreatorProfileResponse ProfileA)> ForeignSubjectAsync(string name)
    {
        var (pipeline, creator, chat) = BuildAgent(VocabularyJson);
        var (subjectA, _, _) = await FreshUnitAsync(creator, $"{name} Fach A");
        var (_, seriesB, unitB) = await FreshUnitAsync(creator, $"{name} Fach B");
        var profileA = await creator.CreateProfileAsync(new CreateCreatorProfileDto(
            $"Profil Fach A {Guid.NewGuid():N}", null, subjectA, null, null, null, null,
            SourceLang: "en", TargetLang: "de", Persona: "Du bist der Lehrer des FREMDEN Fachs.", Didactics: null,
            DefaultTypes: null, Active: true));
        return (pipeline, creator, chat, subjectA, seriesB, unitB, profileA);
    }

    /// <summary>
    /// B-488: a teacher for every book of one subject next to a named series of another is a usage error on every verb -
    /// the word list, the exercise search and the grammar topics would all be read in the profile's subject and find
    /// nothing. Red if the profile's subject is not checked against the series' (exit 0 for create, a run without the
    /// unit's material for plan and exam).
    /// </summary>
    [Theory]
    [InlineData("create", "--type", "Vocabulary", "--count", "3")]
    [InlineData("exam", "--types", "Vocabulary", "--per-type", "3")]
    [InlineData("plan", "--child", "1", "--types", "Vocabulary", "--per-type", "3")]
    public async Task Werkunabhaengiges_Profil_eines_anderen_Fachs_neben_genannter_Reihe_ist_eine_Fehlbedienung(
        string verb, params string[] extra)
    {
        var (pipeline, creator, chat, _, seriesB, _, profileA) = await ForeignSubjectAsync($"Fach {verb}");

        var command = CommandLine.Parse([verb, "--profile", profileA.Id.ToString(), "--series", seriesB.ToString(),
            "--title", TestApi.UniqueName("Fach fremd"), .. extra]);
        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline).RunAsync(command, default));

        Assert.Contains("unterrichtet", error.Message);
        Assert.Equal(0, chat.Calls);
    }

    /// <summary>
    /// B-488, at the sink: a caller of the pipeline that names the profile itself gets the same rule. Red if only the
    /// console checked the fit.
    /// </summary>
    [Fact]
    public async Task Pipeline_mit_Profil_eines_anderen_Fachs_ist_eine_Fehlbedienung()
    {
        var (pipeline, creator, _, _, seriesB, unitB, profileA) = await ForeignSubjectAsync("Pipeline Fach");

        var subjectB = (await creator.GetSeriesAsync(seriesB)).SubjectId!.Value;

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => pipeline.BriefAsync(
            Request(subjectB, seriesB, unitB, "Vocabulary", childId: null, profileId: profileA.Id)));

        Assert.Contains("unterrichtet", error.Message);
    }

    /// <summary>
    /// B-488, at the sink: the exercise's subject is the subject of the work it is filed in. A request naming another one
    /// would search the unit's word list in the wrong subject. Red if the briefing takes the request's subject as given.
    /// </summary>
    [Fact]
    public async Task Pipeline_mit_einem_anderen_Fach_als_dem_der_Reihe_ist_eine_Fehlbedienung()
    {
        var (pipeline, _, _, subjectA, seriesB, unitB, _) = await ForeignSubjectAsync("Pipeline Reihe");

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => pipeline.BriefAsync(
            Request(subjectA, seriesB, unitB, "Vocabulary", childId: null)));

        Assert.Contains("nicht das Fach der Reihe", error.Message);
    }

    /// <summary>
    /// B-495: a profile bound to series B but teaching subject A cannot be fixed by choosing another series - the series
    /// check then rejects it ("gehört zur Reihe"). The message names the profile as the cause. Red if it offers "eine
    /// Reihe in …" to a profile that is bound to a series (the circle of B-495).
    /// </summary>
    [Fact]
    public async Task Profil_auf_einer_Reihe_mit_fremdem_Fach_nennt_das_Profil_als_Ursache()
    {
        var (pipeline, creator, _, subjectA, seriesB, unitB, _) = await ForeignSubjectAsync("Gebunden fremd");
        var bound = await creator.CreateProfileAsync(new CreateCreatorProfileDto(
            $"Profil auf Reihe B {Guid.NewGuid():N}", null, subjectA, null, null, null, seriesB,
            SourceLang: "en", TargetLang: "de", Persona: null, Didactics: null, DefaultTypes: null, Active: true));
        var subjectB = (await creator.GetSeriesAsync(seriesB)).SubjectId!.Value;

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => pipeline.BriefAsync(
            Request(subjectB, seriesB, unitB, "Vocabulary", childId: null, profileId: bound.Id)));

        Assert.Contains("gebunden", error.Message);
        Assert.DoesNotContain("eine Reihe in", error.Message);
        // B-506, AK 4: the profile is this account's own, so correcting it stays a way out (B-500).
        Assert.Contains("korrigiere", error.Message);
    }

    /// <summary>
    /// B-495 (rest): a profile with a free-text subject (no <c>SubjectId</c>, B-142) of another subject is a misfit too -
    /// otherwise the prompt says "Fach: Latein" next to the English unit. Red if a profile without a subject id always
    /// passes.
    /// </summary>
    [Fact]
    public async Task Profil_mit_freiem_Fachnamen_eines_anderen_Fachs_ist_eine_Fehlbedienung()
    {
        var (pipeline, creator, _, _, seriesB, unitB, _) = await ForeignSubjectAsync("Freitext fremd");
        var freeText = await creator.CreateProfileAsync(new CreateCreatorProfileDto(
            $"Profil Freitext {Guid.NewGuid():N}", $"Latein {Guid.NewGuid():N}", null, null, null, null, null,
            SourceLang: "la", TargetLang: "de", Persona: null, Didactics: null, DefaultTypes: null, Active: true));
        var subjectB = (await creator.GetSeriesAsync(seriesB)).SubjectId!.Value;

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => pipeline.BriefAsync(
            Request(subjectB, seriesB, unitB, "Vocabulary", childId: null, profileId: freeText.Id)));

        Assert.Contains("unterrichtet", error.Message);
    }

    /// <summary>
    /// B-495 (rest): in a series without a subject the exercise takes the request's subject - a profile of another
    /// subject is a misfit there as well. Red if the subject is only compared with the series' (which has none).
    /// </summary>
    [Fact]
    public async Task Profil_eines_anderen_Fachs_in_einer_Reihe_ohne_Fach_ist_eine_Fehlbedienung()
    {
        var (pipeline, creator, _, subjectA, seriesB, _, profileA) = await ForeignSubjectAsync("Ohne Fach");
        var subjectB = (await creator.GetSeriesAsync(seriesB)).SubjectId!.Value;
        Assert.NotEqual(subjectA, subjectB); // precondition: the profile teaches another subject than the request
        var bare = await creator.CreateSeriesAsync(new CreateTextbookSeriesDto(
            $"Reihe ohne Fach {Guid.NewGuid():N}", null, null, null, null, null, null, null));
        var unit = await creator.CreateUnitAsync(bare.Id, new CreateSeriesUnitDto("Unit 1", null, 1, null, null, null, null));

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => pipeline.BriefAsync(
            Request(subjectB, bare.Id, unit.Id, "Vocabulary", childId: null, profileId: profileA.Id)));

        Assert.Contains("unterrichtet", error.Message);
    }

    // ---- B-500 / B-498: who may fix a profile, and which profile speaks for a child's book ----

    private CreatorApi Teacher() => new(factory.CreateDefaultClient(AuthHandler.Standalone(new PuglingClientOptions
    {
        BaseUrl = "http://localhost",
        AccountId = 2,
        Pin = "9999",
    })));

    /// <summary>
    /// B-500: a profile bound to the series with a foreign subject, but owned by another creator, cannot be corrected by
    /// this account (<c>not_owner</c>). The message offers only what this account can do. Red if it advises correcting
    /// the profile ("korrigiere") although the reader does not own it.
    /// </summary>
    [Fact]
    public async Task Fremdes_gebundenes_Profil_mit_fremdem_Fach_raet_nicht_zum_Korrigieren()
    {
        var (pipeline, creator, _, subjectA, seriesB, unitB, _) = await ForeignSubjectAsync("Fremd gebunden");
        var foreign = await Teacher().CreateProfileAsync(new CreateCreatorProfileDto(
            $"Profil des Kollegen {Guid.NewGuid():N}", null, subjectA, null, null, null, seriesB,
            SourceLang: "en", TargetLang: "de", Persona: null, Didactics: null, DefaultTypes: null, Active: true));
        var subjectB = (await creator.GetSeriesAsync(seriesB)).SubjectId!.Value;

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => pipeline.BriefAsync(
            Request(subjectB, seriesB, unitB, "Vocabulary", childId: null, profileId: foreign.Id)));

        Assert.Contains("nicht diesem Konto", error.Message);
        Assert.DoesNotContain("korrigiere", error.Message);
    }

    /// <summary>
    /// B-500 (rest): a free-text subject that names the exercise's subject fits - in any case (the server already trims
    /// the stored name, so spaces are not tested here). Green before
    /// B-500 as well; it carries the comparison: red if it turns ordinal ("ENGLISCH" ≠ "Englisch") or if a profile
    /// without a subject id never fits.
    /// </summary>
    [Fact]
    public async Task Profil_mit_freiem_Fachnamen_desselben_Fachs_passt()
    {
        var (pipeline, creator, _, _, seriesB, unitB, _) = await ForeignSubjectAsync("Freitext gleich");
        var series = await creator.GetSeriesAsync(seriesB);
        var freeText = await creator.CreateProfileAsync(new CreateCreatorProfileDto(
            $"Profil Freitext gleich {Guid.NewGuid():N}", series.SubjectName!.ToUpperInvariant(), null, null, null,
            null, null, SourceLang: "en", TargetLang: "de", Persona: "Freitext-Lehrer.", Didactics: null, DefaultTypes: null,
            Active: true));

        var briefing = await pipeline.BriefAsync(
            Request(series.SubjectId!.Value, seriesB, unitB, "Vocabulary", childId: null, profileId: freeText.Id));

        Assert.Equal(freeText.Id, briefing.Profile?.ProfileId);
    }

    /// <summary>
    /// B-500 (rest): with only <c>--child</c>, two profiles on the child's book. The higher-scoring one teaches another
    /// subject (grade bounds put it ahead); the other names no subject and so fits every one. The one that fits speaks -
    /// the user never named the misfit. Red if the console takes the best score unchecked (the briefing then rejects a
    /// profile the user did not choose, exit 2).
    /// </summary>
    [Fact]
    public async Task Nur_mit_Kind_spricht_das_passende_Profil_des_Buchs()
    {
        var (creator, pipeline, chat, child, series, _, _, _, _) = await TwoTeachersAsync("Passend zuerst", profileOnSeries: false);
        var otherSubject = await creator.CreateSubjectAsync(new CreateSubjectDto($"Agent-Test Fremdfach {Guid.NewGuid():N}"));
        var misfit = await creator.CreateProfileAsync(new CreateCreatorProfileDto($"Fremdfach-Lehrer {Guid.NewGuid():N}",
            null, otherSubject.Id, SchoolTypes.Gymnasium, GradeMin: 7, GradeMax: 9, SeriesId: series.Id,
            SourceLang: "en", TargetLang: "de", Persona: "Du bist der Lehrer des FREMDEN Fachs.", Didactics: null,
            DefaultTypes: ["Vocabulary"], Active: true));
        var fitting = await creator.CreateProfileAsync(new CreateCreatorProfileDto($"Buchlehrer ohne Fach {Guid.NewGuid():N}",
            null, null, SchoolTypes.Gymnasium, GradeMin: null, GradeMax: null, SeriesId: series.Id,
            SourceLang: "en", TargetLang: "de", Persona: "Du bist der Lehrer DIESES Buchs.", Didactics: null,
            DefaultTypes: ["Vocabulary"], Active: true));
        var scores = (await creator.MatchProfilesAsync(child.Id, null, default)).ToDictionary(m => m.Profile.Id, m => m.Score);
        Assert.True(scores[misfit.Id] > scores[fitting.Id], "Setup: the misfit must score higher.");

        Assert.Equal(0, await Commands(creator, pipeline).RunAsync(CommandLine.Parse(
            ["create", "--child", child.Id.ToString(), "--type", "Vocabulary", "--count", "3"]), default));

        // Which one spoke: the fitting profile's persona leads the system prompt (a pick of any other profile is red).
        var system = Assert.Single(chat.LastMessages, m => m.Role == ChatRole.System).Text;
        Assert.StartsWith("Du bist der Lehrer DIESES Buchs.", system);
    }

    /// <summary>
    /// B-498/B-500 (review): in a series without a subject the briefing takes the subject from the profile, so a subject
    /// teacher fits there. Only <c>--child</c>, a teacher with a subject (ahead on grade bounds) and one without: the
    /// teacher speaks, the run works. Red if the console prefers by the series' (missing) subject - it then picks the
    /// subjectless profile and the run ends at "hat kein Fach".
    /// </summary>
    [Fact]
    public async Task Nur_mit_Kind_in_einer_Reihe_ohne_Fach_spricht_der_Fachlehrer()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var supervisor = new SupervisorApi(Authenticated());
        var subject = await creator.CreateSubjectAsync(new CreateSubjectDto($"Agent-Test Ohne-Fach-Reihe {Guid.NewGuid():N}"));
        var bare = await creator.CreateSeriesAsync(new CreateTextbookSeriesDto(
            $"Reihe ohne Fach {Guid.NewGuid():N}", null, null, null, SchoolTypes.Gymnasium, "en", "de", null));
        var unit = await creator.CreateUnitAsync(bare.Id, new CreateSeriesUnitDto("Unit 1", 8, 1, null, null, null, null));
        var child = await supervisor.CreateChildAsync(new CreateChildDto($"OhneFach {Guid.NewGuid():N}"[..20], null, 8,
            SchoolTypes.Gymnasium, null));
        await supervisor.CreateTextbookAsync(child.Id, new CreateTextbookDto("Buch ohne Fach", subject.Name, subject.Id, 8,
            null, null, null, SeriesId: bare.Id, CurrentUnitId: unit.Id));
        var teacher = await creator.CreateProfileAsync(new CreateCreatorProfileDto($"Fachlehrer {Guid.NewGuid():N}",
            null, subject.Id, SchoolTypes.Gymnasium, GradeMin: 7, GradeMax: 9, SeriesId: bare.Id, SourceLang: "en",
            TargetLang: "de", Persona: "Du bist der Fachlehrer.", Didactics: null, DefaultTypes: ["Vocabulary"], Active: true));
        await creator.CreateProfileAsync(new CreateCreatorProfileDto($"Ohne Fach {Guid.NewGuid():N}",
            null, null, SchoolTypes.Gymnasium, GradeMin: null, GradeMax: null, SeriesId: bare.Id, SourceLang: "en",
            TargetLang: "de", Persona: "Du bist fachlos.", Didactics: null, DefaultTypes: ["Vocabulary"], Active: true));
        var scores = (await creator.MatchProfilesAsync(child.Id, null, default)).ToDictionary(m => m.Profile.Id, m => m.Score);
        Assert.True(scores[teacher.Id] == scores.Values.Max(), "Setup: the subject teacher must score highest.");

        Assert.Equal(0, await Commands(creator, pipeline)
            .RunAsync(CommandLine.Parse(["briefing", "--child", child.Id.ToString()]), default));
    }

    // ---- B-506: the console picks a book and a profile on the inputs the briefing builds ----

    /// <summary>
    /// A child whose only book names subject A but sits on a series in subject B (or, with <paramref name="seriesSubject"/>
    /// false, on a series without a subject). Nothing on the server binds the book's subject to the series'.
    /// </summary>
    private async Task<(CreatorPipeline Pipeline, CreatorApi Creator, ChildResponse Child, int SubjectA, int SeriesId,
        int UnitId)> BookOnForeignSeriesAsync(string name, bool seriesSubject)
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var supervisor = new SupervisorApi(Authenticated());
        var (subjectA, _, _) = await FreshUnitAsync(creator, $"{name} Buchfach");
        int seriesId, unitId;
        if (seriesSubject)
        {
            (_, seriesId, unitId) = await FreshUnitAsync(creator, $"{name} Reihenfach");
        }
        else
        {
            var bare = await creator.CreateSeriesAsync(new CreateTextbookSeriesDto(
                $"{name} ohne Fach {Guid.NewGuid():N}", null, null, null, null, null, null, null));
            seriesId = bare.Id;
            unitId = (await creator.CreateUnitAsync(seriesId, new CreateSeriesUnitDto("Unit 1", null, 1, null, null, null, null))).Id;
        }
        var child = await supervisor.CreateChildAsync(new CreateChildDto($"{name} {Guid.NewGuid():N}"[..20], null, 8,
            SchoolTypes.Gymnasium, null));
        var book = await supervisor.CreateTextbookAsync(child.Id, new CreateTextbookDto("Buch", null, subjectA, 8, null, null,
            null, SeriesId: seriesId, CurrentUnitId: unitId));
        // Precondition: the book's subject differs from its series'.
        Assert.Equal(subjectA, book.SubjectId);
        Assert.NotEqual(subjectA, (await creator.GetSeriesAsync(seriesId)).SubjectId);
        return (pipeline, creator, child, subjectA, seriesId, unitId);
    }

    private static CommandLine CreateFor(CreatorProfileResponse profile, ChildResponse child) =>
        CommandLine.Parse(["create", "--profile", profile.Id.ToString(), "--child", child.Id.ToString(),
            "--type", "Vocabulary", "--count", "3"]);

    private static Task<CreatorProfileResponse> SubjectProfileAsync(CreatorApi creator, int? subjectId, string? subjectName) =>
        creator.CreateProfileAsync(new CreateCreatorProfileDto($"Ohne Reihe {Guid.NewGuid():N}", subjectName, subjectId,
            null, null, null, null, SourceLang: "en", TargetLang: "de", Persona: null, Didactics: null, DefaultTypes: null,
            Active: true));

    /// <summary>
    /// B-506, AK 1a: a teacher of the book's subject A on a series in B is no book for that teacher - the briefing would
    /// file the exercise in B and reject the profile. Red if the console compares with the book's subject (the series is
    /// picked and the briefing says "unterrichtet").
    /// </summary>
    [Fact]
    public async Task Buchfach_ohne_Reihenfach_ist_kein_Buch_des_Profils()
    {
        var (pipeline, creator, child, subjectA, _, _) = await BookOnForeignSeriesAsync("Buchfach", seriesSubject: true);
        var teacher = await SubjectProfileAsync(creator, subjectA, null);

        var error = await Assert.ThrowsAsync<AgentUsageException>(() =>
            Commands(creator, pipeline).RunAsync(CreateFor(teacher, child), default));

        Assert.Contains("kein Lehrbuch", error.Message);
    }

    /// <summary>
    /// B-506, AK 1b: a teacher of the series' subject B takes that book, whatever the book itself names. Red if the console
    /// compares with the book's subject ("kein Lehrbuch").
    /// </summary>
    [Fact]
    public async Task Reihenfach_entscheidet_die_Buchwahl()
    {
        var (pipeline, creator, child, _, seriesId, unitId) = await BookOnForeignSeriesAsync("Reihenfach", seriesSubject: true);
        var teacher = await SubjectProfileAsync(creator, (await creator.GetSeriesAsync(seriesId)).SubjectId, null);

        Assert.Equal(0, await Commands(creator, pipeline).RunAsync(CreateFor(teacher, child), default));

        Assert.Single(await creator.SearchExercisesAsync(seriesUnitId: unitId));
    }

    /// <summary>
    /// B-506, AK 1c: on a series without a subject the briefing takes the profile's, so a teacher with a subject id fits
    /// there, whatever the book names. Checked on <c>briefing</c>: the server refuses exercises in a series without a
    /// subject (<c>series_without_subject</c>, B-508). Red if a series without a subject never fits ("kein Lehrbuch").
    /// </summary>
    [Fact]
    public async Task Reihe_ohne_Fach_nimmt_den_Fachlehrer()
    {
        var (pipeline, creator, child, _, _, _) = await BookOnForeignSeriesAsync("Ohne Fach Id", seriesSubject: false);
        var (otherSubject, _, _) = await FreshUnitAsync(creator, "Fach des Lehrers");
        var teacher = await SubjectProfileAsync(creator, otherSubject, null);

        Assert.Equal(0, await Commands(creator, pipeline).RunAsync(CommandLine.Parse(
            ["briefing", "--profile", teacher.Id.ToString(), "--child", child.Id.ToString()]), default));
    }

    /// <summary>
    /// B-506, AK 1d: a profile with only a free-text subject cannot give a series without a subject one ("hat kein Fach"),
    /// so it has no book there - even when the free text names the book's own subject. Red if a series without a subject
    /// fits every profile (the run ends at "hat kein Fach") or if the book's subject decides (the series is picked).
    /// </summary>
    [Fact]
    public async Task Reihe_ohne_Fach_nimmt_kein_Freitext_Profil()
    {
        var (pipeline, creator, child, _, _, _) = await BookOnForeignSeriesAsync("Ohne Fach Frei", seriesSubject: false);
        // The book's own subject name (B-510): under the old rule (compare with the book) this profile would pick the series.
        var bookSubject = (await new SupervisorApi(Authenticated()).ListTextbooksAsync(child.Id, default)).Single().SubjectName!;
        var freeText = await SubjectProfileAsync(creator, null, bookSubject);

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline).RunAsync(
            CommandLine.Parse(["briefing", "--profile", freeText.Id.ToString(), "--child", child.Id.ToString()]), default));

        Assert.Contains("kein Lehrbuch", error.Message);
    }

    /// <summary>
    /// B-506, AK 2: two books with a profile each, and on the child's book a teacher of another subject outscores the
    /// book's teacher. The choice offered for that book is the one the briefing accepts. Red if each book offers its best
    /// score (the user types the misfit's id and runs into the briefing's rejection).
    /// </summary>
    [Fact]
    public async Task Mehrere_Buecher_nennen_je_Buch_das_passende_Profil()
    {
        var (creator, pipeline, _, child, series, _, _, onSeries, elsewhere) = await TwoTeachersAsync("Mehrere passend");
        var otherSubject = await creator.CreateSubjectAsync(new CreateSubjectDto($"Agent-Test Fremdfach {Guid.NewGuid():N}"));
        var misfit = await creator.CreateProfileAsync(new CreateCreatorProfileDto($"Fremdfach-Lehrer {Guid.NewGuid():N}",
            null, otherSubject.Id, SchoolTypes.Gymnasium, GradeMin: 7, GradeMax: 9, SeriesId: series.Id,
            SourceLang: "en", TargetLang: "de", Persona: null, Didactics: null, DefaultTypes: ["Vocabulary"], Active: true));
        var scores = (await creator.MatchProfilesAsync(child.Id, null, default)).ToDictionary(m => m.Profile.Id, m => m.Score);
        Assert.True(scores[misfit.Id] > scores[onSeries.Id], "Setup: the misfit must outscore the book's teacher.");

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline).RunAsync(
            CommandLine.Parse(["create", "--child", child.Id.ToString(), "--type", "Vocabulary", "--count", "3"]), default));

        Assert.Contains("mehrere Lehrbücher", error.Message);
        Assert.Contains($"[{onSeries.Id}]", error.Message);
        Assert.Contains($"[{elsewhere.Id}]", error.Message);
        Assert.DoesNotContain($"[{misfit.Id}]", error.Message);
        Assert.DoesNotContain("passt nicht", error.Message);
    }

    /// <summary>
    /// B-506 (review): a third book whose only profile teaches another subject than the book's series. It stays on the
    /// list - dropping it would let the score pick the subject (B-481) - but is marked, so the user does not type an id
    /// bound to fail. Red if the entry is offered unmarked.
    /// </summary>
    [Fact]
    public async Task Mehrere_Buecher_markieren_ein_Buch_ohne_passendes_Profil()
    {
        var (creator, pipeline, _, child, _, _, _, onSeries, _) = await TwoTeachersAsync("Mehrere unpassend");
        var (_, thirdSeries, thirdUnit) = await FreshUnitAsync(creator, "Drittbuch");
        await new SupervisorApi(Authenticated()).CreateTextbookAsync(child.Id, new CreateTextbookDto("Drittbuch", null, null,
            8, null, null, null, SeriesId: thirdSeries, CurrentUnitId: thirdUnit));
        var otherSubject = await creator.CreateSubjectAsync(new CreateSubjectDto($"Agent-Test Fremdfach {Guid.NewGuid():N}"));
        var misfit = await creator.CreateProfileAsync(new CreateCreatorProfileDto($"Einziger Fremdfach-Lehrer {Guid.NewGuid():N}",
            null, otherSubject.Id, SchoolTypes.Gymnasium, GradeMin: null, GradeMax: null, SeriesId: thirdSeries,
            SourceLang: "en", TargetLang: "de", Persona: null, Didactics: null, DefaultTypes: ["Vocabulary"], Active: true));

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline).RunAsync(
            CommandLine.Parse(["create", "--child", child.Id.ToString(), "--type", "Vocabulary", "--count", "3"]), default));

        Assert.Contains($"[{misfit.Id}] {misfit.Name} (passt nicht zum Fach seiner Reihe)", error.Message);
        Assert.Contains($"[{onSeries.Id}] {onSeries.Name},", error.Message);
    }

    /// <summary>
    /// B-506, AK 3: the reverse order of <see cref="Nur_mit_Kind_in_einer_Reihe_ohne_Fach_spricht_der_Fachlehrer"/> - in a
    /// series without a subject, a profile without one outscores the subject teacher. The teacher speaks. Red if the score
    /// decides there unchecked (the run ends at "hat kein Fach").
    /// </summary>
    [Fact]
    public async Task Nur_mit_Kind_in_einer_Reihe_ohne_Fach_spricht_der_Fachlehrer_auch_schwaecher_bewertet()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var supervisor = new SupervisorApi(Authenticated());
        var subject = await creator.CreateSubjectAsync(new CreateSubjectDto($"Agent-Test Ohne-Fach schwach {Guid.NewGuid():N}"));
        var bare = await creator.CreateSeriesAsync(new CreateTextbookSeriesDto(
            $"Reihe ohne Fach {Guid.NewGuid():N}", null, null, null, SchoolTypes.Gymnasium, "en", "de", null));
        var unit = await creator.CreateUnitAsync(bare.Id, new CreateSeriesUnitDto("Unit 1", 8, 1, null, null, null, null));
        var child = await supervisor.CreateChildAsync(new CreateChildDto($"OhneFach {Guid.NewGuid():N}"[..20], null, 8,
            SchoolTypes.Gymnasium, null));
        await supervisor.CreateTextbookAsync(child.Id, new CreateTextbookDto("Buch ohne Fach", subject.Name, subject.Id, 8,
            null, null, null, SeriesId: bare.Id, CurrentUnitId: unit.Id));
        var teacher = await creator.CreateProfileAsync(new CreateCreatorProfileDto($"Fachlehrer {Guid.NewGuid():N}",
            null, subject.Id, SchoolTypes.Gymnasium, GradeMin: null, GradeMax: null, SeriesId: bare.Id, SourceLang: "en",
            TargetLang: "de", Persona: "Du bist der Fachlehrer.", Didactics: null, DefaultTypes: ["Vocabulary"], Active: true));
        var subjectless = await creator.CreateProfileAsync(new CreateCreatorProfileDto($"Ohne Fach {Guid.NewGuid():N}",
            null, null, SchoolTypes.Gymnasium, GradeMin: 7, GradeMax: 9, SeriesId: bare.Id, SourceLang: "en",
            TargetLang: "de", Persona: "Du bist fachlos.", Didactics: null, DefaultTypes: ["Vocabulary"], Active: true));
        var scores = (await creator.MatchProfilesAsync(child.Id, null, default)).ToDictionary(m => m.Profile.Id, m => m.Score);
        Assert.True(scores[subjectless.Id] > scores[teacher.Id], "Setup: the subjectless profile must score higher.");

        Assert.Equal(0, await Commands(creator, pipeline)
            .RunAsync(CommandLine.Parse(["briefing", "--child", child.Id.ToString()]), default));
    }

    /// <summary>
    /// B-498: a profile without a series and with a free-text subject takes the child's book in that subject - here the
    /// child has none, so it is the usage error that says so. Red if the free-text profile counts every book of the child
    /// (then an English series is picked and the briefing rejects the profile instead).
    /// </summary>
    [Fact]
    public async Task Freitext_Profil_ohne_Reihe_nimmt_kein_Buch_eines_anderen_Fachs()
    {
        var (creator, pipeline, _, child, _, _, _, _, _) = await TwoTeachersAsync("Freitext Buch", profileOnSeries: false);
        var latin = await creator.CreateProfileAsync(new CreateCreatorProfileDto($"Latein frei {Guid.NewGuid():N}",
            $"Latein {Guid.NewGuid():N}", null, null, null, null, null, SourceLang: "la", TargetLang: "de", Persona: null,
            Didactics: null, DefaultTypes: null, Active: true));

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline).RunAsync(
            CommandLine.Parse(["create", "--profile", latin.Id.ToString(), "--child", child.Id.ToString(),
                "--type", "Vocabulary", "--count", "3"]), default));

        Assert.Contains("kein Lehrbuch", error.Message);
    }

    /// <summary>
    /// B-488 (rest of the look-back): a profile of one work next to a bare <c>--series-unit</c> of another names the
    /// conflict. The unit used to be searched only in the profile's series and was reported as non-existent. Red if the
    /// named place is looked up in the profile's series again ("gibt es nicht").
    /// </summary>
    [Fact]
    public async Task Profil_einer_anderen_Reihe_neben_einer_Unit_nennt_den_Konflikt()
    {
        var (creator, pipeline, _, child, _, _, current, _, elsewhere) = await TwoTeachersAsync("Unit fremd");

        var command = CommandLine.Parse(["create", "--child", child.Id.ToString(), "--profile", elsewhere.Id.ToString(),
            "--series-unit", current.Id.ToString(), "--type", "Vocabulary", "--count", "3"]);
        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline).RunAsync(command, default));

        Assert.Contains("gehört zur Reihe", error.Message);
    }

    /// <summary>
    /// B-484, AK 2: a named profile without a series takes the series of the child's book in its subject, at the child's
    /// unit - not the first series of the catalog. Red if the location falls back to the catalog (the exercise lands
    /// elsewhere), ignores the child's unit (it lands in unit 1), or counts a book of another subject (two to choose).
    /// </summary>
    [Fact]
    public async Task Werkunabhaengiges_Profil_legt_im_Buch_des_Kindes_ab()
    {
        var (creator, pipeline, _, child, _, first, current, generalist, _) =
            await TwoTeachersAsync("Werkunabhaengig", profileOnSeries: false);
        // A catalogued book in another subject: only the profile's subject counts, or there would be two to choose from.
        var (otherSubject, otherSeries, otherUnit) = await FreshUnitAsync(creator, "Anderes Fach");
        await new SupervisorApi(Authenticated()).CreateTextbookAsync(child.Id, new CreateTextbookDto("Latein", null,
            otherSubject, 8, null, null, null, SeriesId: otherSeries, CurrentUnitId: otherUnit));

        var command = CommandLine.Parse(["create", "--child", child.Id.ToString(), "--profile", generalist.Id.ToString(),
            "--type", "Vocabulary", "--count", "3"]);
        Assert.Equal(0, await Commands(creator, pipeline).RunAsync(command, default));

        Assert.Single(await creator.SearchExercisesAsync(seriesUnitId: current.Id));
        Assert.Empty(await creator.SearchExercisesAsync(seriesUnitId: first.Id));
    }

    /// <summary>
    /// B-484, AK 2, where the child's book cannot decide: no child, no book in the subject, or two books in it. Each is a
    /// usage error naming the way out. Red if any of them falls back to a series of the catalog.
    /// </summary>
    [Theory]
    [InlineData("ohne Kind", "hängt an keinem Lehrwerk")]
    [InlineData("ohne Buch", "kein Lehrbuch auf einer Reihe")]
    [InlineData("zwei Buecher", "mehrere Lehrbücher")]
    public async Task Werkunabhaengiges_Profil_ohne_eindeutiges_Buch_ist_eine_Fehlbedienung(string fall, string expected)
    {
        var (creator, pipeline, _, child, _, _, _, generalist, _) =
            await TwoTeachersAsync($"Unklar {fall}", profileOnSeries: false);
        var supervisor = new SupervisorApi(Authenticated());
        string[] who = ["--child", child.Id.ToString()];
        if (fall == "ohne Kind")
        {
            who = [];
        }
        else if (fall == "ohne Buch")
        {
            var bookless = await supervisor.CreateChildAsync(new CreateChildDto($"Buchlos {Guid.NewGuid():N}"[..20], null, 8,
                SchoolTypes.Gymnasium, null));
            who = ["--child", bookless.Id.ToString()];
        }
        else
        {
            // A second series in the profile's subject: the series decides, not the book's own subject (B-506).
            var second = await creator.CreateSeriesAsync(new CreateTextbookSeriesDto(
                $"Zweites Buch {Guid.NewGuid():N}", null, null, generalist.SubjectId, null, null, null, null));
            var secondSeries = second.Id;
            var secondUnit = (await creator.CreateUnitAsync(secondSeries,
                new CreateSeriesUnitDto("Unit 1", null, 1, null, null, null, null))).Id;
            await supervisor.CreateTextbookAsync(child.Id, new CreateTextbookDto("Zweitbuch", null, generalist.SubjectId, 8,
                null, null, null, SeriesId: secondSeries, CurrentUnitId: secondUnit));
        }

        var command = CommandLine.Parse(["create", .. who, "--profile", generalist.Id.ToString(), "--type", "Vocabulary", "--count", "3"]);
        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline).RunAsync(command, default));

        Assert.Contains(expected, error.Message);
    }

    /// <summary>
    /// B-484: <c>--unit</c> and <c>--series-unit</c> naming two units would read the material from one and file the
    /// exercise in the other. Red if the named <c>--series-unit</c> silently wins (exit 0).
    /// </summary>
    [Fact]
    public async Task Zwei_verschiedene_Units_sind_eine_Fehlbedienung()
    {
        var (creator, pipeline, _, child, series, first, current, _, _) = await TwoTeachersAsync("Zwei Units");

        var command = CommandLine.Parse(["create", "--child", child.Id.ToString(), "--series", series.Id.ToString(),
            "--series-unit", current.Id.ToString(), "--unit", first.Id.ToString(), "--type", "Vocabulary", "--count", "3"]);
        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline).RunAsync(command, default));

        Assert.Contains("--unit und --series-unit", error.Message);
    }

    /// <summary>
    /// B-484, Nachschau of B-481: <c>plan</c> builds its request through the same rule as <c>create</c> - a plan with
    /// <c>--child</c> and <c>--series</c> fills the child's current unit, not the series' first. Red if the plan path
    /// bypasses the child's unit.
    /// </summary>
    [Fact]
    public async Task Ein_Plan_mit_Reihe_fuellt_die_Unit_des_Kindes()
    {
        var (creator, pipeline, _, child, series, first, current, _, _) = await TwoTeachersAsync("Plan Reihe");

        var command = CommandLine.Parse(["plan", "--child", child.Id.ToString(), "--series", series.Id.ToString(),
            "--types", "Vocabulary", "--per-type", "1", "--count", "3", "--title", TestApi.UniqueName("Plan Reihe")]);
        Assert.Equal(0, await Commands(creator, pipeline).RunAsync(command, default));

        Assert.NotEmpty(await creator.SearchExercisesAsync(seriesUnitId: current.Id));
        Assert.Empty(await creator.SearchExercisesAsync(seriesUnitId: first.Id));
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

    /// <summary>
    /// Interests reach the prompt, and the material is the work the exercise is filed in - not the child's free-text
    /// book, which here even belongs to another subject (B-484: one place decides the material). Red if the free-text
    /// book becomes material or source again.
    /// </summary>
    [Fact]
    public async Task Das_Briefing_traegt_Interessen_und_das_Werk_der_Ablage_in_den_Prompt()
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
        Assert.Contains("Unit 1: Animals", prompt);
        Assert.Contains($"Lehrwerk: {(await creator.GetSeriesAsync(seriesId)).Name}", prompt);
        Assert.DoesNotContain("Green Line 1", prompt);
        Assert.DoesNotContain("Green Line 1", briefing.Source);

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
        var (subjectId, _, _) = await FreshUnitAsync(creator, "Allgemein");
        var (series, unit, profile) = await FreshProfileAsync(creator, subjectId, "Allgemein");

        var request = Request(subjectId, series.Id, unit.Id, "Vocabulary", childId: null, profileId: profile.Id, unitId: unit.Id);
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
        var (subjectId, _, _) = await FreshUnitAsync(creator, "Unit-Stoff");
        var (series, unit, profile) = await FreshProfileAsync(creator, subjectId, "Unit-Stoff");

        var briefing = await pipeline.BriefAsync(
            Request(subjectId, series.Id, unit.Id, "Vocabulary", childId: null, profileId: profile.Id, unitId: unit.Id));
        var prompt = briefing.ToPromptText();

        Assert.Contains($"Lehrwerk: {series.Name} (Cornelsen)", prompt);
        Assert.Contains("Unit 3 – Growing up", prompt);
        Assert.Contains("Present perfect vs. simple past", prompt);
        Assert.Contains("to grow up, responsibility, to argue", prompt);
        // The profile's languages apply when the command line names none.
        Assert.Equal("en", briefing.SourceLang);
    }

    /// <summary>
    /// B-471: the unit's new grammar comes first. The topics arrive sorted by label, so with two cloze parts a plan
    /// practised "past progressive" and "present progressive" and never the new "question tags". The unit's notes mark
    /// new material with "NEU" (lehrwerk-aufbereiten, FORMAT.md); those topics lead, in the order the notes name them.
    /// Red if the order stays alphabetical, if the marker is not matched case-insensitively against the label, or if a
    /// topic only mentioned (without the marker) is moved forward too.
    /// </summary>
    [Fact]
    public void Neuer_Grammatik_Stoff_der_Unit_kommt_zuerst()
    {
        string[] labels = ["Past progressive", "Present progressive", "Question tags", "Simple past"];
        const string notes = "Wiederholung simple past und present progressive. NEU question tags: positiver Satz → "
                             + "verneinter Tag. NEU past progressive: was/were + -ing.";

        Assert.Equal(["Question tags", "Past progressive", "Present progressive", "Simple past"],
            GrammarOrder.NewMaterialFirst(labels, notes));
        Assert.Equal(labels, GrammarOrder.NewMaterialFirst(labels, null));
        Assert.Equal(labels, GrammarOrder.NewMaterialFirst(labels, "Wiederholung question tags."));

        // A label that is a prefix of another (Access unit 3): the new topic is the longer one, not the revised one.
        string[] perfect = ["Adverbs", "Present perfect", "Present perfect progressive"];
        Assert.Equal(["Present perfect progressive", "Adverbs", "Present perfect"],
            GrammarOrder.NewMaterialFirst(perfect, "Wiederholung present perfect. NEU present perfect progressive: have been + -ing."));
        // … and the shorter label only counts on a word boundary: "NEU present perfectly" names no topic.
        Assert.Equal(perfect, GrammarOrder.NewMaterialFirst(perfect, "NEU present perfectly normal"));

        // B-482: a dash or an opening quote after the marker - red if only ":" and spaces count.
        Assert.Equal(["Question tags", "Past progressive", "Present progressive", "Simple past"],
            GrammarOrder.NewMaterialFirst(labels, "NEU – question tags. NEU „past progressive“: was/were + -ing."));
        // B-482: two topics with the same label both stay - red if the rest is filtered by value.
        string[] twins = ["Adverbs", "Question tags", "Question tags"];
        Assert.Equal(["Question tags", "Adverbs", "Question tags"], GrammarOrder.NewMaterialFirst(twins, "NEU question tags"));
        // B-482 (review): a label that starts with a separator itself - red if the marker swallows its first character.
        // B-519: single typographic quotes open a label too.
        Assert.Equal(["Question tags", "Past progressive", "Present progressive", "Simple past"],
            GrammarOrder.NewMaterialFirst(labels, "NEU ‚question tags‘ und NEU ‘past progressive’."));
        string[] forms = ["Adverbs", "-ing form"];
        Assert.Equal(["-ing form", "Adverbs"], GrammarOrder.NewMaterialFirst(forms, "NEU -ing form: swimming, reading"));
    }

    /// <summary>B-471 through the briefing: the labels a plan rotates over come in that order.</summary>
    [Fact]
    public async Task Das_Briefing_nennt_den_neuen_Grammatik_Stoff_zuerst()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Neuer Stoff");
        var old = await creator.CreateGrammarTopicAsync(new CreateGrammarTopicDto("Afrikanisch alt", subjectId));
        var fresh = await creator.CreateGrammarTopicAsync(new CreateGrammarTopicDto("Zebra neu", subjectId));
        await creator.UpdateUnitAsync(seriesId, seriesUnitId, new UpdateSeriesUnitDto(null, null, null, null, null,
            "Wiederholung Afrikanisch alt. NEU Zebra neu.", null, [old.Id, fresh.Id]));

        var briefing = await pipeline.BriefAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary"));

        Assert.Equal(["Zebra neu", "Afrikanisch alt"], briefing.GrammarTopicLabels);
    }

    /// <summary>
    /// AC 6 of B-155: an exercise the agent creates in a unit that carries grammar topics carries those topics - the
    /// same pre-fill rule as the form, only without a human who deselects. Red if the briefing drops the unit's
    /// topics or the payload does not pass them on; the free-text grammar of the unit stays in the prompt beside
    /// them (AC 8, held by <see cref="Der_Stoff_der_Unit_steht_im_Prompt"/>).
    /// </summary>
    [Fact]
    public async Task Der_Agent_Uebernimmt_Die_Grammatik_Themen_Der_Unit()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Grammatik-Thema");
        var topic = await creator.CreateGrammarTopicAsync(new CreateGrammarTopicDto("Present perfect", subjectId));
        var other = await creator.CreateGrammarTopicAsync(new CreateGrammarTopicDto("Passive", subjectId));
        await creator.UpdateUnitAsync(seriesId, seriesUnitId,
            new UpdateSeriesUnitDto(null, null, null, null, null, null, null, [topic.Id, other.Id]));
        // A sibling unit with another topic: the agent must take over THIS unit's topics, not the series'.
        var sibling = await creator.CreateUnitAsync(seriesId, new CreateSeriesUnitDto("Unit 2", null, 2, null, null, null, null, [other.Id]));
        Assert.NotEqual(seriesUnitId, sibling.Id);

        var (briefing, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary", childId: null));
        Assert.Equal(new[] { topic.Id, other.Id }.Order(), briefing.GrammarTopicIds.Order());
        Assert.True(outcome.Published, $"Selbsttest: {outcome.SelfTestPercent} %");

        var detail = await creator.GetExerciseDetailAsync(outcome.ExerciseId!.Value);
        Assert.Equal(new[] { topic.Id, other.Id }.Order(), detail.GrammarTopicIds);
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
            {"title":"Klausur-Lückentext","text":"Ben has [responsibility] for his sister, never wants to [argue] and has [grown] up fast.",
             "distractors":["promise","answer"]}
            """;
        var (pipeline, creator, _) = BuildAgent(ProfileVocabularyJson, cloze);
        var supervisor = new SupervisorApi(Authenticated());
        var (subjectId, _, _) = await FreshUnitAsync(creator, "Klausur");
        var (series, unit, profile) = await FreshProfileAsync(creator, subjectId, "Klausur");

        var planner = new ExamPlanner(pipeline, creator, supervisor);
        var outcome = await planner.RunAsync(new ExamRequest(
            Request(subjectId, series.Id, unit.Id, "Vocabulary", profileId: profile.Id, unitId: unit.Id),
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
        var (subjectId, _, _) = await FreshUnitAsync(creator, "Klausur-kaputt");
        var (series, unit, profile) = await FreshProfileAsync(creator, subjectId, "Klausur-kaputt");

        var planner = new ExamPlanner(pipeline, creator, supervisor);
        var outcome = await planner.RunAsync(new ExamRequest(
            Request(subjectId, series.Id, unit.Id, "Vocabulary", profileId: profile.Id, unitId: unit.Id),
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
        {"title":"Lehrplan-Lückentext","text":"The horse [runs] in the field, the sheep [sleeps] and the goat [eats].",
         "distractors":["jumps","swims"]}
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
            "Lehrplan: Test", [new GeneratedPart("Vocabulary", outcome, null)], [], null, [], [], []);

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
            [42], PlanId: 7, PositionIds: [], FailedPositions: [new FailedPosition(42, "exercise_empty")], WordLists: []).Complete);
    }

    /// <summary>
    /// B-307: <c>create</c> with an unknown <c>--series</c> (and a <c>--series-unit</c>, so the location is taken
    /// unchecked) is a usage error naming the series - not a raw <c>PuglingApiException</c> (exit 1), the failure
    /// B-301 fixed for <c>fill</c>. Red if the NotFound catch in <c>AgentCommands.ResolveSubjectIdAsync</c> is removed.
    /// </summary>
    [Fact]
    public async Task Create_mit_unbekannter_Reihe_ist_eine_Fehlbedienung()
    {
        var (pipeline, creator, chat) = BuildAgent(VocabularyJson);
        var command = CommandLine.Parse([
            "create", "--child", "1", "--type", "Vocabulary", "--count", "3",
            "--series", "987654", "--series-unit", "1",
        ]);

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline).RunAsync(command, default));

        Assert.Contains("Reihe 987654", error.Message);
        // Statement of intent, not a probe: the run stopped before any model call with or without the B-307 fix.
        Assert.Equal(0, chat.Calls);
    }

    /// <summary>
    /// B-307: the same through the pipeline, whose briefing reads the series itself (the path a profile or a plan
    /// takes). Red if the NotFound catch in <c>BriefingBuilder.BuildAsync</c> is removed.
    /// </summary>
    [Fact]
    public async Task Das_Briefing_mit_unbekannter_Reihe_ist_eine_Fehlbedienung()
    {
        var (pipeline, creator, chat) = BuildAgent(VocabularyJson);
        var (subjectId, _, _) = await FreshUnitAsync(creator, "Briefing-Reihe-weg");

        var error = await Assert.ThrowsAsync<AgentUsageException>(
            () => pipeline.CreateAsync(Request(subjectId, 987654, 1, "Vocabulary")));

        Assert.Contains("Reihe 987654", error.Message);
        // Statement of intent, not a probe: the run stopped before any model call with or without the B-307 fix.
        Assert.Equal(0, chat.Calls);
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
    /// B-21 AC 3, the whole cascade "request > profile > default": without languages on the request the PROFILE's
    /// pair decides which weak words come in; with languages on the request, the request wins. The profile is
    /// Latin→French - a pair no other test uses (the cap of 15 weak words cannot push the expected word out), and
    /// with a target other than the default "de", so ignoring the profile's TARGET shows too. Three weak words,
    /// one per pair, make every wrong pair visible. Red if the briefing ignores the profile (en/de: the English
    /// word), ignores only its target (la/de: "lupus"), reads the null pair as "no filter" (all three), or lets
    /// the profile beat an explicit request (second briefing: the French word instead of the English one).
    /// </summary>
    [Fact]
    public async Task Foerdermodus_nimmt_das_Sprachpaar_des_Profils_wenn_die_Anfrage_keins_nennt()
    {
        var father = await TestApi.AdultAsync(factory);
        var child = await TestApi.ChildAsync(factory);
        async Task WeakAsync(string src, string tgt, string front, string back)
        {
            var exerciseId = await TestApi.CreateVocabExerciseAsync(father, src, tgt, (front, back));
            var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)Pugling.Api.Models.TestStage.FreeText);
            var sessionId = await TestApi.StartPositionSessionAsync(child, planId, positionId);
            await TestApi.PositionReviewAsync(child, planId, positionId, sessionId, 0, givenAnswer: "falsch");
        }
        await WeakAsync("en", "de", "the wolf (B-21)", "der Wolf");
        await WeakAsync("la", "de", "lupus (B-21)", "der Wolf (la)");
        await WeakAsync("la", "fr", "lupus (B-21, fr)", "le loup");

        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Foerdermodus-Profil");
        var profile = await creator.CreateProfileAsync(new CreateCreatorProfileDto(
            $"Latein – Fördermodus {Guid.NewGuid():N}", null, subjectId, null, null, null, null,
            SourceLang: "la", TargetLang: "fr", Persona: null, Didactics: null, DefaultTypes: null, Active: true));
        var fromProfileRequest = Request(subjectId, seriesId, seriesUnitId, "Vocabulary", profileId: profile.Id);
        var fromProfile = await pipeline.BriefAsync(fromProfileRequest with { UseWeakWords = true, SourceLang = null, TargetLang = null });

        Assert.Equal(("la", "fr"), (fromProfile.SourceLang, fromProfile.TargetLang));
        Assert.Contains("lupus (B-21, fr)", fromProfile.RequiredWords);
        Assert.DoesNotContain("lupus (B-21)", fromProfile.RequiredWords);
        Assert.DoesNotContain("the wolf (B-21)", fromProfile.RequiredWords);

        // The request's own pair beats the profile.
        var fromRequest = await pipeline.BriefAsync(fromProfileRequest with { UseWeakWords = true, SourceLang = "en", TargetLang = "de" });

        Assert.Contains("the wolf (B-21)", fromRequest.RequiredWords);
        Assert.DoesNotContain("lupus (B-21, fr)", fromRequest.RequiredWords);
        // Same target as the request, other source: only the sourceLang filter keeps it out.
        Assert.DoesNotContain("lupus (B-21)", fromRequest.RequiredWords);
    }

    /// <summary>
    /// B-308: <c>plan</c>/<c>exam</c> with an unknown series. Nothing before the parts read the series, so the briefing's
    /// usage error came once per part and the run ended with "plan incomplete" instead of a usage error. The briefing is
    /// now checked once before the first part. Driven through the planners directly: since B-488 the console reads the
    /// series itself and stops earlier, which would leave this check untested. Red if the check is removed (the error is
    /// swallowed per part, no exception reaches the caller) - measured for the exam half only (B-488, mutant M8); the plan
    /// half alone does not tell the removal apart (B-498).
    /// </summary>
    [Fact]
    public async Task Plan_und_Klausur_mit_unbekannter_Reihe_brechen_einmal_ab()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, _, _) = await FreshUnitAsync(creator, "Reihe-weg");
        var supervisor = new SupervisorApi(Authenticated());
        var request = Request(subjectId, 987654, 1, "Vocabulary");

        var exam = await Assert.ThrowsAsync<AgentUsageException>(() => new ExamPlanner(pipeline, creator, supervisor)
            .RunAsync(new ExamRequest(request, Types: ["Vocabulary", "Cloze"], PerType: 3, ScheduledDate: null,
                Title: TestApi.UniqueName("Reihe weg"))));
        var plan = await Assert.ThrowsAsync<AgentUsageException>(() => new PlanPlanner(pipeline, supervisor)
            .RunAsync(new PlanRequest(request, Types: ["Vocabulary", "Cloze"], PerType: 3, Intensity: PlanIntensity.Normal,
                StartDate: null, DurationDays: 3, Title: TestApi.UniqueName("Reihe weg"))));

        Assert.Contains("Reihe 987654", exam.Message);
        Assert.Contains("Reihe 987654", plan.Message);
    }

    /// <summary>
    /// B-313: a type the agent cannot generate in <c>--types</c> is a usage error of the whole request (exit 2), checked before the
    /// first model call - a typo must not leave a partial plan or exam behind. The valid type comes FIRST, so a per-part check would
    /// already have called the model for it: red (Calls &gt; 0, or no exception) if the check moves back into the part loop.
    /// </summary>
    [Theory]
    [InlineData("plan", "--child", "1")]
    [InlineData("exam", "--child", "1")]
    public async Task Plan_und_Klausur_mit_unbekanntem_Typ_brechen_vor_dem_Modell_ab(string verb, params string[] extra)
    {
        var (pipeline, creator, chat) = BuildAgent(VocabularyJson);
        var (_, seriesId, seriesUnitId) = await FreshUnitAsync(creator, $"Typ-Tippfehler-{verb}");

        var command = CommandLine.Parse([
            verb, "--types", "Vocabulary,Clozee", "--per-type", "1", "--title", TestApi.UniqueName("Tippfehler"),
            "--series", seriesId.ToString(), "--series-unit", seriesUnitId.ToString(), .. extra,
        ]);
        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline).RunAsync(command, default));

        Assert.Contains("Clozee", error.Message);
        Assert.Equal(0, chat.Calls);
    }

    /// <summary>
    /// B-308: <c>profiles --child</c> with a child the server does not know is a usage error naming the child, not a
    /// raw 404 (exit 1). Red if the NotFound catch in <c>AgentCommands.ShowProfilesAsync</c> is removed.
    /// </summary>
    [Fact]
    public async Task Profile_fuer_ein_unbekanntes_Kind_sind_eine_Fehlbedienung()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline)
            .RunAsync(CommandLine.Parse(["profiles", "--child", "987654"]), default));

        Assert.Contains("Kind 987654", error.Message);
    }

    /// <summary>
    /// B-308, the other half: an existing child the account does not supervise (the realistic case - a teacher asking
    /// about a real child). Since B-310 the server answers 404 here too, so this case and the one above share their
    /// red condition (the catch removed); it stays because it walks the real teacher account instead of an unknown id.
    /// Seed account 2, Herr Schmidt, holds only the Creator role and supervises no child.
    /// </summary>
    [Fact]
    public async Task Profile_fuer_ein_fremdes_Kind_sind_eine_Fehlbedienung()
    {
        var (pipeline, _, _) = BuildAgent(VocabularyJson);
        var teacher = new CreatorApi(factory.CreateDefaultClient(AuthHandler.Standalone(new PuglingClientOptions
        {
            BaseUrl = "http://localhost",
            AccountId = 2,
            Pin = "9999",
        })));

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(teacher, pipeline)
            .RunAsync(CommandLine.Parse(["profiles", "--child", "1"]), default));

        Assert.Contains("Kind 1 ", error.Message);
    }

    /// <summary>
    /// B-21 AC 4: <c>--use-weak</c> without a child used to do nothing, silently. Every verb that builds a
    /// request (here <c>create</c>, <c>plan</c> and <c>exam</c>, B-19) refuses it instead. Red if the guard is removed: then
    /// the verbs get past it and fail later for an unrelated reason, or not at all.
    /// </summary>
    [Theory]
    [InlineData("create", "--type", "Vocabulary")]
    [InlineData("plan")]
    [InlineData("plan", "--dry-run")]
    [InlineData("exam")]
    public async Task Foerdermodus_ohne_Kind_bricht_mit_Meldung_ab(string verb, params string[] extra)
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var commands = Commands(creator, pipeline);

        var command = CommandLine.Parse([verb, "--profile", "999999", "--use-weak", .. extra]);
        var error = await Assert.ThrowsAsync<AgentUsageException>(() => commands.RunAsync(command, default));

        Assert.Contains("--use-weak braucht --child", error.Message);
    }

    /// <summary>
    /// B-311: the location lookup used to read only the first page of series (the client's default of 50), so a
    /// valid series or unit past it was rejected as a usage error. Fifty series that sort before the target are
    /// created here, so the target is past the first page no matter what else the shared database holds. Red if
    /// <c>--series</c> alone goes through the series list again (first call), or if the unit search without
    /// <c>--series</c> reads only one page (second call).
    /// </summary>
    [Fact]
    public async Task Reihe_und_Unit_jenseits_der_ersten_Seite_werden_gefunden()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var subject = await creator.CreateSubjectAsync(new CreateSubjectDto($"Agent-Test Seite 2 {Guid.NewGuid():N}"));
        for (var i = 0; i < 50; i++)
            await creator.CreateSeriesAsync(new CreateTextbookSeriesDto(
                $"aaa B-311 {i:D2} {Guid.NewGuid():N}", null, subject.Name, subject.Id, null, null, null, null));
        var target = await creator.CreateSeriesAsync(new CreateTextbookSeriesDto(
            $"zzz B-311 {Guid.NewGuid():N}", null, subject.Name, subject.Id, null, null, null, null));
        var unit = await creator.CreateUnitAsync(target.Id, new CreateSeriesUnitDto("Unit 1", null, 1, null, null, null, null));
        // The premise of the case, asserted rather than assumed: the client's default page does not reach the target.
        Assert.DoesNotContain(await creator.ListSeriesAsync(), s => s.Id == target.Id);
        var commands = Commands(creator, pipeline);

        await commands.RunAsync(CommandLine.Parse(["briefing", "--child", "1", "--series", target.Id.ToString()]), default);
        await commands.RunAsync(CommandLine.Parse(["briefing", "--child", "1", "--series-unit", unit.Id.ToString()]), default);
    }

    /// <summary>
    /// B-311, the side finding: an unknown <c>--series</c> without <c>--series-unit</c> used to get the diagnosis for an
    /// empty catalog ("Es gibt keine Reihe mit Unit") instead of its own id. Red if the series is no longer looked up by id.
    /// </summary>
    [Fact]
    public async Task Eine_unbekannte_Reihe_ohne_Unit_wird_mit_ihrer_Id_gemeldet()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline)
            .RunAsync(CommandLine.Parse(["briefing", "--child", "1", "--series", "987654"]), default));

        Assert.Contains("Reihe 987654", error.Message);
    }

    /// <summary>
    /// B-312: <c>profiles --subject</c> with a subject id the server does not know used to print "there is no creator
    /// profile yet" and exit 0 - false as soon as profiles exist. It is a usage error naming the id, like every other
    /// wrong id of the agent (B-301/B-307/B-308). Red if the subject is no longer checked before the listing.
    /// </summary>
    [Theory]
    [InlineData]
    [InlineData("--child", "1")]
    public async Task Profile_fuer_ein_unbekanntes_Fach_sind_eine_Fehlbedienung(params string[] extra)
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        // The premise: profiles exist, so "there is none yet" would be a false statement, not a harmless one.
        await creator.CreateProfileAsync(new CreateCreatorProfileDto(
            $"Profil B-312 {Guid.NewGuid():N}", null, null, null, null, null, null,
            SourceLang: "en", TargetLang: "de", Persona: null, Didactics: null, DefaultTypes: null, Active: true));

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline)
            .RunAsync(CommandLine.Parse(["profiles", "--subject", "987654", .. extra]), default));

        Assert.Contains("Fach 987654", error.Message);
    }

    /// <summary>
    /// B-279: <c>--general</c> drops the child as the audience, but <c>--use-weak</c> still pulled that child's weak words
    /// into the required vocabulary - a "general" exercise carrying one child's remedial words. The combination is a usage
    /// error, the line B-21 AC 4 drew for <c>--use-weak</c> without a child. Red if the guard is removed: the briefing
    /// is then built without complaint.
    /// </summary>
    [Fact]
    public async Task Foerdermodus_mit_general_bricht_mit_Meldung_ab()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Foerdermodus-General");

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => pipeline.BriefAsync(
            Request(subjectId, seriesId, seriesUnitId, "Vocabulary", general: true) with { UseWeakWords = true }));

        Assert.Contains("--general", error.Message);
    }

    /// <summary>
    /// B-264: without a child, <c>--interests</c> tagged the exercise but never reached the prompt - the model was told
    /// "neutral everyday situations" while the catalog claimed a dressing the exercise did not have. Red if the labels
    /// are dropped from the briefing (the label is missing from the prompt) or if the childless prompt keeps its
    /// "neutral" instruction next to them (the contradiction comes back). The control case without interests keeps that
    /// instruction - otherwise the second assertion would also pass for a prompt that lost it everywhere.
    /// </summary>
    [Fact]
    public async Task KindloserPfad_KleidetDieAufgabenInDieAngegebenenInteressenEin()
    {
        var (pipeline, creator, chat) = BuildAgent(VocabularyJson, VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Einkleidung-Prompt");
        var einhorn = await creator.CreateInterestTagAsync(new CreateInterestTagDto(TestApi.UniqueName("Einhorn")));

        var (_, outcome) = await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary",
            childId: null, interests: [einhorn.Slug]));
        var prompt = string.Join("\n", chat.LastMessages.Select(m => m.Text));

        Assert.Contains($"Kleide die Aufgaben in die Interessen ein: {einhorn.Label}", prompt);
        Assert.DoesNotContain("neutrale", prompt);
        var detail = await creator.GetExerciseDetailAsync(outcome.ExerciseId!.Value);
        Assert.Contains(einhorn.Label, detail.Description);

        await pipeline.CreateAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary", childId: null));
        Assert.Contains("neutrale", string.Join("\n", chat.LastMessages.Select(m => m.Text)));
    }

    /// <summary>
    /// B-322: the <c>briefing</c> verb prints only the briefing, not the request part that names the dressing - and the
    /// childless briefing said "from the given interests" without naming them. Red if the labels leave the briefing text
    /// (the verb would point at interests it does not show).
    /// </summary>
    [Fact]
    public async Task Das_kindlose_Briefing_nennt_die_vorgegebenen_Interessen_selbst()
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var (subjectId, seriesId, seriesUnitId) = await FreshUnitAsync(creator, "Einkleidung-Briefing");
        var einhorn = await creator.CreateInterestTagAsync(new CreateInterestTagDto(TestApi.UniqueName("Einhorn")));

        var briefing = await pipeline.BriefAsync(Request(subjectId, seriesId, seriesUnitId, "Vocabulary",
            childId: null, interests: [einhorn.Slug]));

        Assert.Contains(einhorn.Label, briefing.ToPromptText());
    }

    // ---- B-508: a series without a subject ends a writing verb before the model ----

    /// <summary>A subject teacher without a series and a series of <paramref name="owner"/> without a subject.</summary>
    private async Task<(CreatorPipeline Pipeline, CreatorApi Creator, FakeChatClient Chat, CreatorProfileResponse Teacher,
        int SeriesId, int UnitId)> SubjectlessSeriesAsync(string name, CreatorApi? owner = null)
    {
        var (pipeline, creator, chat) = BuildAgent(VocabularyJson);
        var (subjectId, _, _) = await FreshUnitAsync(creator, $"{name} Lehrerfach");
        var teacher = await SubjectProfileAsync(creator, subjectId, null);
        var shelf = owner ?? creator;
        var bare = await shelf.CreateSeriesAsync(new CreateTextbookSeriesDto(
            $"{name} ohne Fach {Guid.NewGuid():N}", null, null, null, null, null, null, null));
        var unit = await shelf.CreateUnitAsync(bare.Id, new CreateSeriesUnitDto("Unit 1", null, 1, null, null, null, null));
        return (pipeline, creator, chat, teacher, bare.Id, unit.Id);
    }

    private static CommandLine OnSeries(string verb, CreatorProfileResponse teacher, int seriesId, int unitId, params string[] more) =>
        CommandLine.Parse([verb, "--profile", teacher.Id.ToString(), "--series", seriesId.ToString(),
            "--series-unit", unitId.ToString(), "--type", "Vocabulary", "--count", "3", .. more]);

    /// <summary>
    /// B-508, AK 1 and 2: every writing verb - a dry run too, which predicts the real run - ends at a series without a
    /// subject with a usage error before the model is called. Red if the profile's subject is taken there (the model runs
    /// and the server answers 400 <c>series_without_subject</c>, or the dry run passes).
    /// </summary>
    [Theory]
    [InlineData("create")]
    [InlineData("create", "--dry-run")]
    [InlineData("plan")]
    [InlineData("plan", "--dry-run")]
    [InlineData("exam")]
    [InlineData("exam", "--dry-run")]
    public async Task Schreibendes_Verb_endet_an_einer_Reihe_ohne_Fach_vor_dem_Modell(string verb, params string[] more)
    {
        var (pipeline, creator, chat, teacher, seriesId, unitId) = await SubjectlessSeriesAsync($"Schreibt {verb}");

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline)
            .RunAsync(OnSeries(verb, teacher, seriesId, unitId, more), default));

        Assert.Contains("hat kein Fach", error.Message);
        Assert.Contains("Vater-Web (Lehrwerke", error.Message);
        Assert.Equal(0, chat.Calls);
    }

    /// <summary>
    /// B-508, AK 3: the briefing writes nothing and keeps working on the same series, with the profile's subject (B-488).
    /// Red if the early stop also hits the briefing.
    /// </summary>
    [Fact]
    public async Task Briefing_laeuft_auf_einer_Reihe_ohne_Fach_weiter()
    {
        var (pipeline, creator, _, teacher, seriesId, unitId) = await SubjectlessSeriesAsync("Briefing zeigt");

        Assert.Equal(0, await Commands(creator, pipeline).RunAsync(OnSeries("briefing", teacher, seriesId, unitId), default));
    }

    /// <summary>
    /// B-508, AK 4: a series of another account cannot get a subject from this one (<c>not_owner</c>), so the message
    /// names the owner (B-516: and nothing else). Red if it advises the Vater-Web regardless of who owns the series.
    /// </summary>
    [Fact]
    public async Task Fremde_Reihe_ohne_Fach_raet_nicht_zum_Vater_Web()
    {
        var (pipeline, creator, chat, teacher, seriesId, unitId) = await SubjectlessSeriesAsync("Fremd ohne Fach", Teacher());

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline)
            .RunAsync(OnSeries("create", teacher, seriesId, unitId), default));

        Assert.Contains("nicht diesem Konto; ein Fach setzt nur ihr Eigentümer.", error.Message);
        Assert.DoesNotContain("Vater-Web", error.Message);
        Assert.DoesNotContain("--series", error.Message);
        Assert.DoesNotContain("--profile", error.Message);
        Assert.Equal(0, chat.Calls);
    }

    /// <summary>
    /// B-508 (review): a series of the base stock has no owner, so nobody can give it a subject - the message sends the
    /// reader to no owner. Red if the base stock gets the "ask the owner" branch (or the Vater-Web one).
    /// </summary>
    [Fact]
    public void Grundbestands_Reihe_ohne_Fach_nennt_nur_eine_andere_Reihe()
    {
        var stock = new TextbookSeriesResponse(5, "Grundbestand", "grundbestand", null, null, null, null, SchoolTypes.None,
            null, null, null, OwnerAdultId: null, IsOwn: false, UnitCount: 1, GradeMin: null, GradeMax: null, DateTime.UtcNow);

        var message = ProfileFit.NoSubject(stock);

        Assert.Contains("Grundbestand, niemand kann ihr ein Fach geben", message);
        Assert.DoesNotContain("--series", message);
        Assert.DoesNotContain("--profile", message);
        Assert.DoesNotContain("Eigentümer", message);
        Assert.DoesNotContain("Vater-Web", message);
    }

    // ---- B-510: the book choice takes the sink's lock on a series without a subject ----

    /// <summary>A child with a book on a series in subject E (unless <paramref name="withSubjectBook"/> is false) and a book
    /// on a series without a subject, and a teacher of E without a series of its own.</summary>
    private async Task<(CreatorPipeline Pipeline, CreatorApi Creator, ChildResponse Child, CreatorProfileResponse Teacher,
        int SubjectSeries, int BareSeries)> BookWithAndWithoutSubjectAsync(string name, bool withSubjectBook = true)
    {
        var (pipeline, creator, _) = BuildAgent(VocabularyJson);
        var supervisor = new SupervisorApi(Authenticated());
        var (subjectE, seriesE, unitE) = await FreshUnitAsync(creator, $"{name} E");
        var bare = await creator.CreateSeriesAsync(new CreateTextbookSeriesDto(
            $"{name} ohne Fach {Guid.NewGuid():N}", null, null, null, null, null, null, null));
        var bareUnit = await creator.CreateUnitAsync(bare.Id, new CreateSeriesUnitDto("Unit 1", null, 1, null, null, null, null));
        var child = await supervisor.CreateChildAsync(new CreateChildDto($"{name} {Guid.NewGuid():N}"[..20], null, 8,
            SchoolTypes.Gymnasium, null));
        if (withSubjectBook)
            await supervisor.CreateTextbookAsync(child.Id, new CreateTextbookDto("Buch E", null, subjectE, 8, null, null, null,
                SeriesId: seriesE, CurrentUnitId: unitE));
        await supervisor.CreateTextbookAsync(child.Id, new CreateTextbookDto("Buch ohne Fach", null, null, 8, null, null, null,
            SeriesId: bare.Id, CurrentUnitId: bareUnit.Id));
        var teacher = await SubjectProfileAsync(creator, subjectE, null);
        return (pipeline, creator, child, teacher, seriesE, bare.Id);
    }

    /// <summary>
    /// B-510, AK 1: a writing verb does not count the book on a series without a subject - the teacher's book is the one in
    /// its subject, as before B-506. Red if the series without a subject counts (two books, "mehrere Lehrbücher").
    /// </summary>
    [Fact]
    public async Task Schreiben_zaehlt_ein_Buch_auf_einer_Reihe_ohne_Fach_nicht()
    {
        var (pipeline, creator, child, teacher, _, _) = await BookWithAndWithoutSubjectAsync("Schreiben zaehlt");

        Assert.Equal(0, await Commands(creator, pipeline).RunAsync(CreateFor(teacher, child), default));
    }

    /// <summary>
    /// B-510, AK 2: only the book on a series without a subject would have fitted - its own lock is the message, with its
    /// way out, not "kein Lehrbuch". Already true before B-510 (the series was picked and stopped later); it guards the new
    /// filter: red if the dropped series leaves the bare "no book" message.
    /// </summary>
    [Fact]
    public async Task Schreiben_nur_mit_Buch_ohne_Fach_nennt_die_Sperre_der_Reihe()
    {
        var (pipeline, creator, child, teacher, _, _) = await BookWithAndWithoutSubjectAsync("Nur ohne Fach", withSubjectBook: false);

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline)
            .RunAsync(CreateFor(teacher, child), default));

        Assert.Contains("hat kein Fach", error.Message);
        Assert.DoesNotContain("kein Lehrbuch", error.Message);
        // B-519: the series comes from the child's book.
        Assert.Contains($"Das Lehrbuch von Kind {child.Id} steht auf Reihe", error.Message);
    }

    /// <summary>
    /// B-510, AK 3: the briefing writes nothing and still takes the book on a series without a subject (B-506, AK 1c). Red
    /// if the lock of the writing verbs reaches the briefing's book choice.
    /// </summary>
    [Fact]
    public async Task Briefing_nimmt_das_Buch_ohne_Fach_weiter()
    {
        var (pipeline, creator, child, teacher, _, _) = await BookWithAndWithoutSubjectAsync("Briefing ohne Fach", withSubjectBook: false);

        Assert.Equal(0, await Commands(creator, pipeline).RunAsync(CommandLine.Parse(
            ["briefing", "--profile", teacher.Id.ToString(), "--child", child.Id.ToString()]), default));
    }

    /// <summary>
    /// B-510, AK 4: two books with a profile each, one on a series without a subject. Both stay on the list (B-481), the
    /// one a writing verb cannot use is marked. Red if it is offered unmarked (the user types an id bound to fail).
    /// </summary>
    [Fact]
    public async Task Mehrere_Buecher_markieren_eine_Reihe_ohne_Fach()
    {
        var (pipeline, creator, child, teacher, seriesE, bare) = await BookWithAndWithoutSubjectAsync("Mehrere ohne Fach");
        var onE = await creator.CreateProfileAsync(new CreateCreatorProfileDto($"Lehrer E {Guid.NewGuid():N}", null,
            teacher.SubjectId, null, null, null, seriesE, SourceLang: "en", TargetLang: "de", Persona: null, Didactics: null,
            DefaultTypes: null, Active: true));
        var onBare = await creator.CreateProfileAsync(new CreateCreatorProfileDto($"Lehrer ohne Fach {Guid.NewGuid():N}", null,
            teacher.SubjectId, null, null, null, bare, SourceLang: "en", TargetLang: "de", Persona: null, Didactics: null,
            DefaultTypes: null, Active: true));

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline).RunAsync(
            CommandLine.Parse(["create", "--child", child.Id.ToString(), "--type", "Vocabulary", "--count", "3"]), default));

        Assert.Contains($"[{onBare.Id}] {onBare.Name} (Reihe ohne Fach – nimmt keine Übung auf)", error.Message);
        Assert.Contains($"[{onE.Id}] {onE.Name}", error.Message);
        Assert.DoesNotContain($"[{onE.Id}] {onE.Name} (", error.Message);
    }

    /// <summary>
    /// B-511: a named profile bound to an own series without a subject, no --series. "--series" would be a circle - the
    /// briefing rejects the profile on any other series - so the message names another profile. Red if it offers
    /// --series there.
    /// </summary>
    [Fact]
    public async Task Gebundenes_Profil_an_Reihe_ohne_Fach_nennt_ein_anderes_Profil()
    {
        var (pipeline, creator, chat, teacher, seriesId, unitId) = await SubjectlessSeriesAsync("Gebunden ohne Fach");
        var bound = await creator.CreateProfileAsync(new CreateCreatorProfileDto($"Gebunden {Guid.NewGuid():N}", null,
            teacher.SubjectId, null, null, null, seriesId, SourceLang: "en", TargetLang: "de", Persona: null, Didactics: null,
            DefaultTypes: null, Active: true));

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline).RunAsync(CommandLine.Parse(
            ["create", "--profile", bound.Id.ToString(), "--type", "Vocabulary"]), default));

        Assert.Contains("hat kein Fach", error.Message);
        // B-519: where the series comes from - the profile's binding, not the command line.
        Assert.Contains($"Creator-Profil {bound.Id} ({bound.Name}) ist an Reihe {seriesId} gebunden", error.Message);
        // B-516: the lock and the step that lifts it, no other advice.
        Assert.DoesNotContain("--series", error.Message);
        Assert.DoesNotContain("--profile", error.Message);
        Assert.Equal(0, chat.Calls);

        // The way out, taken (pm-loop Step 5): a profile bound to a series with a subject, no --series - it writes.
        var (otherSubject, otherSeries, _) = await FreshUnitAsync(creator, "Reihe des anderen Profils");
        var other = await creator.CreateProfileAsync(new CreateCreatorProfileDto($"Anderes {Guid.NewGuid():N}", null,
            otherSubject, null, null, null, otherSeries, SourceLang: "en", TargetLang: "de", Persona: null, Didactics: null,
            DefaultTypes: null, Active: true));
        Assert.Equal(0, await Commands(creator, pipeline).RunAsync(CommandLine.Parse(
            ["create", "--profile", other.Id.ToString(), "--type", "Vocabulary", "--count", "3"]), default));
    }

    /// <summary>
    /// B-511 (review): the same bound profile with --series on a series that has a subject. The briefing would refuse the
    /// profile there ("lass --series weg") and lead back to the lock above - so the lock comes first. Red if the run reaches
    /// the briefing's "gehört zur Reihe".
    /// </summary>
    [Fact]
    public async Task Gebundenes_Profil_an_Reihe_ohne_Fach_endet_auch_mit_series_an_der_Sperre()
    {
        var (pipeline, creator, chat, teacher, seriesId, _) = await SubjectlessSeriesAsync("Gebunden mit series");
        var (_, otherSeries, otherUnit) = await FreshUnitAsync(creator, "Reihe mit Fach");
        var bound = await creator.CreateProfileAsync(new CreateCreatorProfileDto($"Gebunden {Guid.NewGuid():N}", null,
            teacher.SubjectId, null, null, null, seriesId, SourceLang: "en", TargetLang: "de", Persona: null, Didactics: null,
            DefaultTypes: null, Active: true));

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline).RunAsync(CommandLine.Parse(
            ["create", "--profile", bound.Id.ToString(), "--series", otherSeries.ToString(), "--series-unit", otherUnit.ToString(),
             "--type", "Vocabulary"]), default));

        Assert.Contains($"Reihe {seriesId}", error.Message);
        Assert.Contains("hat kein Fach", error.Message);
        Assert.DoesNotContain("gehört zur Reihe", error.Message);
        Assert.Equal(0, chat.Calls);
    }

    /// <summary>
    /// B-514: a named profile bound to another series Y (with a subject), and --series on a series X without one. "--series
    /// with a subject" would lead into the briefing's refusal of the profile anywhere but Y - the way out is to drop
    /// --series (Y) or another profile. Red if the message advises another series.
    /// </summary>
    [Fact]
    public async Task Fremd_gebundenes_Profil_auf_Reihe_ohne_Fach_raet_nicht_zu_einer_anderen_Reihe()
    {
        var (pipeline, creator, chat, teacher, bareSeries, bareUnit) = await SubjectlessSeriesAsync("Fremd gebunden ohne Fach");
        var (ownSubject, ownSeries, _) = await FreshUnitAsync(creator, "Reihe des Profils");
        var bound = await creator.CreateProfileAsync(new CreateCreatorProfileDto($"Gebunden an Y {Guid.NewGuid():N}", null,
            ownSubject, null, null, null, ownSeries, SourceLang: "en", TargetLang: "de", Persona: null, Didactics: null,
            DefaultTypes: null, Active: true));

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline)
            .RunAsync(OnSeries("create", bound, bareSeries, bareUnit), default));

        Assert.Contains("hat kein Fach", error.Message);
        // B-516: no advice that a profile bound elsewhere could run into.
        Assert.DoesNotContain("--series", error.Message);
        Assert.DoesNotContain("--profile", error.Message);
        Assert.Equal(0, chat.Calls);

        // The way out, taken (pm-loop Step 5): without --series/--series-unit the profile's own series files the exercise.
        Assert.Equal(0, await Commands(creator, pipeline).RunAsync(CommandLine.Parse(
            ["create", "--profile", bound.Id.ToString(), "--type", "Vocabulary", "--count", "3"]), default));
    }

    /// <summary>
    /// B-514: the early stop for a named profile bound to a series without a subject is only for writing verbs - the briefing
    /// writes nothing and shows that profile's subject. Red if the stop also hits the briefing (the <c>writes</c> condition dropped).
    /// </summary>
    [Fact]
    public async Task Briefing_mit_gebundenem_Profil_auf_Reihe_ohne_Fach_laeuft()
    {
        var (pipeline, creator, _, teacher, seriesId, _) = await SubjectlessSeriesAsync("Briefing gebunden");
        var bound = await creator.CreateProfileAsync(new CreateCreatorProfileDto($"Gebunden {Guid.NewGuid():N}", null,
            teacher.SubjectId, null, null, null, seriesId, SourceLang: "en", TargetLang: "de", Persona: null, Didactics: null,
            DefaultTypes: null, Active: true));

        Assert.Equal(0, await Commands(creator, pipeline).RunAsync(CommandLine.Parse(
            ["briefing", "--profile", bound.Id.ToString()]), default));
    }

    /// <summary>
    /// B-482: two books of the child on the same series at different units - which unit is a choice, not the order of the
    /// book list. Red if the first book silently decides (the exercise lands in its unit).
    /// </summary>
    [Fact]
    public async Task Zwei_Buecher_derselben_Reihe_bei_verschiedenen_Units_sind_eine_Fehlbedienung()
    {
        var (pipeline, creator, chat) = BuildAgent(VocabularyJson);
        var supervisor = new SupervisorApi(Authenticated());
        var (subjectId, seriesId, unit1) = await FreshUnitAsync(creator, "Zwei Buecher");
        var unit2 = (await creator.CreateUnitAsync(seriesId, new CreateSeriesUnitDto("Unit 2", null, 2, null, null, null, null))).Id;
        var child = await supervisor.CreateChildAsync(new CreateChildDto($"Zwei {Guid.NewGuid():N}"[..20], null, 8,
            SchoolTypes.Gymnasium, null));
        await supervisor.CreateTextbookAsync(child.Id, new CreateTextbookDto("Band A", null, subjectId, 8, null, null, null,
            SeriesId: seriesId, CurrentUnitId: unit1));
        await supervisor.CreateTextbookAsync(child.Id, new CreateTextbookDto("Band B", null, subjectId, 8, null, null, null,
            SeriesId: seriesId, CurrentUnitId: unit2));
        var teacher = await SubjectProfileAsync(creator, subjectId, null);

        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline).RunAsync(CommandLine.Parse(
            ["create", "--profile", teacher.Id.ToString(), "--series", seriesId.ToString(), "--child", child.Id.ToString(),
             "--type", "Vocabulary", "--count", "3"]), default));

        Assert.Contains("verschiedenen Units", error.Message);
        Assert.Contains("--series-unit", error.Message);
        Assert.Equal(0, chat.Calls);

        // The way out, taken: with --series-unit the exercise lands in that unit.
        Assert.Equal(0, await Commands(creator, pipeline).RunAsync(CommandLine.Parse(
            ["create", "--profile", teacher.Id.ToString(), "--series-unit", unit2.ToString(), "--child", child.Id.ToString(),
             "--type", "Vocabulary", "--count", "3"]), default));
        Assert.Single(await creator.SearchExercisesAsync(seriesUnitId: unit2));
    }

    /// <summary>
    /// B-516: the one way out the message names - set the subject in the Vater-Web - taken: with the teacher's subject set
    /// on the own series, the same call writes. Red if the lift of the lock is not enough (another check would stop it).
    /// </summary>
    [Fact]
    public async Task Eigene_Reihe_ohne_Fach_Fach_setzen_genuegt()
    {
        var (pipeline, creator, chat, teacher, seriesId, unitId) = await SubjectlessSeriesAsync("Fach setzen");
        var error = await Assert.ThrowsAsync<AgentUsageException>(() => Commands(creator, pipeline)
            .RunAsync(OnSeries("create", teacher, seriesId, unitId), default));
        Assert.Contains("hat kein Fach", error.Message);

        await creator.UpdateSeriesAsync(seriesId, new UpdateTextbookSeriesDto(null, null, null, teacher.SubjectId, null, null,
            null, null));

        Assert.Equal(0, await Commands(creator, pipeline).RunAsync(OnSeries("create", teacher, seriesId, unitId), default));
    }

    /// <summary>
    /// B-501: an answer of two words is revealed by the text even when a no-break space stands between them. Red if only the
    /// plain space of the answer matches.
    /// </summary>
    [Fact]
    public void Loesung_mit_geschuetztem_Leerzeichen_im_Text_gilt_als_verraten() =>
        Assert.True(ClozeMarkup.ContainsWord("Yesterday I was\u00A0playing football.", "was playing"));
}
