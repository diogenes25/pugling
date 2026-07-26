using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Client;
using Pugling.Contracts;
using Pugling.Contracts.Creator;
using Pugling.Contracts.Supervisor;

namespace Pugling.Api.Tests;

/// <summary>
/// Fährt <c>Pugling.Client</c> gegen die echte API (In-Process-TestServer). Das ist der Beweis, dass die
/// geteilte HTTP-Schicht trägt, bevor die KI-Agenten darauf aufsetzen: Login/Token, Enum-als-String-Parität,
/// ProblemDetails→Ausnahme mit stabilem <c>code</c> und die typisierten Wrapper beider Ebenen.
/// </summary>
public class PuglingClientTests : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory;

    public PuglingClientTests(PuglingWebAppFactory factory) => _factory = factory;

    // Seed: Konto 2 = Herr Schmidt (Creator+Supervisor, PIN 9999), Konto 1 = Papa (PIN 0000, betreut Kind 1).
    private HttpClient Authenticated(int accountId, string pin) =>
        _factory.CreateDefaultClient(AuthHandler.Standalone(new PuglingClientOptions
        {
            BaseUrl = "http://localhost",
            AccountId = accountId,
            Pin = pin,
        }));

    private CreatorApi Creator() => new(Authenticated(2, "9999"));

    private SupervisorApi Supervisor() => new(Authenticated(1, "0000"));

    // Die Student-Lesesichten liest der Agent mit dem Supervisor-Konto (die Controller sind nur [Authorize]).
    private StudentApi ProgressOfChild() => new(Authenticated(1, "0000"));

    [Fact]
    public async Task Client_meldet_sich_selbst_an_und_liest_das_Typ_Manifest()
    {
        var creator = Creator();

        // Kein manuelles Token: der AuthHandler holt es beim ersten Aufruf.
        var types = await creator.GetExerciseTypesAsync();

        Assert.NotEmpty(types);
        var vocabulary = Assert.Single(types, t => t.Type == "Vocabulary");
        Assert.Equal("vocabulary", vocabulary.AuthoringRoute);
        // Enum-Parität: der Server sendet "StudyPlanTest" als String – ohne den Converter bräche das still.
        Assert.Equal(ExerciseCheckMode.StudyPlanTest, vocabulary.CheckMode);
    }

    [Fact]
    public async Task Creator_legt_Fach_Kapitel_und_typisierte_Uebung_an()
    {
        var creator = Creator();

        var subject = await creator.CreateSubjectAsync(new CreateSubjectDto("Client-Test Englisch"));
        var chapter = await creator.CreateChapterAsync(subject.Id, new CreateChapterDto("Unit 1", 1));

        var payload = new ExercisePayload<VocabularyConfig>("Classroom words", 1, 10, new VocabularyConfig
        {
            Direction = "front-to-back",
            SourceLang = "en",
            TargetLang = "de",
            Items = [new VocabItem("the blackboard", "die Tafel"), new VocabItem("the break", "die Pause")],
        });
        var exercise = await creator.CreateExerciseAsync(subject.Id, chapter.Id, "vocabulary", payload);

        Assert.Equal("Vocabulary", exercise.Type);
        Assert.True(exercise.IsOwn);
        Assert.True(exercise.IsOwner);

        // Die Inline-Vokabeln müssen als eigene Item-Ebene materialisiert und im Store verlinkt sein.
        var items = await creator.ListItemsAsync(subject.Id, chapter.Id, exercise.Id);
        Assert.Equal(2, items.Count);
        Assert.All(items, i => Assert.True(i.VocabularyId > 0));
        Assert.Contains(items, i => i.Front == "the blackboard" && i.Back == "die Tafel");

        // Und die Übung muss über die kindneutrale Katalogsuche auffindbar sein.
        var found = await creator.SearchExercisesAsync(subjectId: subject.Id);
        Assert.Contains(found, e => e.Id == exercise.Id);
    }

    [Fact]
    public async Task Creator_dedupliziert_Vokabeln_ueber_Lookup()
    {
        var creator = Creator();

        var created = await creator.CreateVocabularyAsync(
            new CreateVocabularyDto(null, "en", "de", "lighthouse", "Leuchtturm", PartOfSpeech.Noun));

        var lookup = await creator.LookupVocabularyAsync(new LookupRequest("en", "de", ["lighthouse", "definitelynotaword"], null));

        var hit = Assert.Single(lookup.Words, w => w.Word == "lighthouse");
        Assert.True(hit.Exists);
        Assert.Contains(hit.Matches, m => m.Id == created.Id);
        Assert.False(Assert.Single(lookup.Words, w => w.Word == "definitelynotaword").Exists);
    }

    [Fact]
    public async Task Supervisor_baut_Plan_mit_Position_und_Lernziel()
    {
        var creator = Creator();
        var supervisor = Supervisor();

        // Inhalt beim Creator anlegen …
        var subject = await creator.CreateSubjectAsync(new CreateSubjectDto("Client-Test Steuerung"));
        var chapter = await creator.CreateChapterAsync(subject.Id, new CreateChapterDto("Kapitel 1", 1));
        var exercise = await creator.CreateExerciseAsync(subject.Id, chapter.Id, "matching",
            new ExercisePayload<MatchingConfig>("Verben zuordnen", 1, 8, new MatchingConfig
            {
                Instruction = "Match the infinitive with its past simple form.",
                Pairs = [new MatchPair("go", "went"), new MatchPair("buy", "bought")],
            }));

        // … und vom Supervisor zuweisen.
        var child = Assert.Single(await supervisor.ListChildrenAsync(), c => c.Id == 1);
        var plan = await supervisor.CreatePlanAsync(
            new CreatePlanDto(child.Id, "Client-Test Plan", subject.Id, null, 14));
        var position = await supervisor.AddPositionAsync(plan.Id, new CreatePositionDto(
            ExerciseId: exercise.Id, Order: null, Stage: null, ItemCount: null, Scope: null,
            Cadence: GoalCadence.Daily, OrderStrategy: null, GoalThreshold: 2, RequireTypedTest: null,
            UseLeitner: null, MaxBox: null, BoxIntervalDays: null, StageSchedule: null,
            PointsGoalMet: 20, PenaltyCoins: 5, NewContentPoints: null, ComboThreshold: null,
            ComboBonusPoints: null, SpeedThresholdSeconds: null, SpeedBonusPoints: null));

        Assert.Equal(exercise.Id, position.ExerciseId);
        Assert.Equal(GoalCadence.Daily, position.Cadence);
        Assert.Equal(20, position.PointsGoalMet);
        Assert.Equal(5, position.PenaltyCoins);

        var goal = await supervisor.CreateLearnGoalAsync(child.Id, new CreateLearnGoalRequest(
            subject.Id, null, null, LearnGoalMetric.MasteredPercent, 80, null, "80 % beherrschen"));
        Assert.Equal(80, goal.TargetValue);
        Assert.Contains(await supervisor.ListLearnGoalsAsync(child.Id), g => g.Id == goal.Id);
    }

    [Fact]
    public async Task Supervisor_verschenkt_Muenzen_und_liest_das_Tagesdashboard()
    {
        var supervisor = Supervisor();

        var before = await supervisor.GetChildAsync(1);
        var entry = await supervisor.GrantPointsAsync(1, new PointsEntryDto(25, "Client-Test", Currency.Coins));
        var after = await supervisor.GetChildAsync(1);

        Assert.Equal(25, entry.Amount);
        Assert.Equal(PointKind.Manual, entry.Kind);
        Assert.Equal(before.Coins + 25, after.Coins);

        var dashboard = await supervisor.GetDailyOverviewAsync();
        Assert.Contains(dashboard.Children, c => c.ChildId == 1);
    }

    [Fact]
    public void Alle_Fassaden_lassen_sich_gemeinsam_registrieren()
    {
        // Regression: eine geteilte AuthHandler-Instanz lehnt die HttpClientFactory beim zweiten
        // benannten Client ab („InnerHandler must be null"). Geteilt wird deshalb nur das Token.
        var services = new ServiceCollection();
        services.AddPuglingClient(options =>
        {
            options.BaseUrl = "http://localhost";
            options.AccountId = 1;
            options.Pin = "0000";
        });

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<CreatorApi>());
        Assert.NotNull(provider.GetRequiredService<SupervisorApi>());
        Assert.NotNull(provider.GetRequiredService<StudentApi>());
        Assert.Same(provider.GetRequiredService<PuglingTokenStore>(), provider.GetRequiredService<PuglingTokenStore>());
    }

    [Fact]
    public async Task Supervisor_pflegt_und_liest_die_Lehrbuecher_des_Kindes()
    {
        var supervisor = Supervisor();

        var created = await supervisor.CreateTextbookAsync(1,
            new CreateTextbookDto("Green Line 1", "Englisch", null, 5, "Klett", null, "Unit 3"));

        var books = await supervisor.ListTextbooksAsync(1);

        Assert.Contains(books, b => b.Id == created.Id && b.CurrentChapter == "Unit 3");
    }

    [Fact]
    public async Task Supervisor_liest_den_Lernstand_seines_Kindes_ueber_die_Student_Sichten()
    {
        var progress = ProgressOfChild();

        // Alle drei Sichten müssen dem Supervisor offenstehen – auf ihnen beruht die Schwächen-Analyse.
        var items = await progress.ListVocabularyProgressAsync(1, take: 5);
        var weakWords = await progress.ListWordMasteryAsync(1, onlyWeak: true, take: 5);
        var subjects = await progress.ListSubjectProgressAsync(1);

        Assert.NotNull(items);
        Assert.NotNull(weakWords);
        Assert.All(subjects, s => Assert.True(s.Progress.TotalItems >= 0));
    }

    [Fact]
    public async Task Fremdes_Kind_bleibt_auch_in_den_Student_Sichten_verschlossen()
    {
        // Herr Schmidt betreut kein Kind – der Lernstand von Kind 1 geht ihn nichts an.
        var foreign = new StudentApi(Authenticated(2, "9999"));

        var error = await Assert.ThrowsAsync<PuglingApiException>(() => foreign.ListWordMasteryAsync(1));

        Assert.Contains(error.StatusCode, new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound });
    }

    [Fact]
    public async Task Fehler_werden_als_PuglingApiException_mit_stabilem_Code_geworfen()
    {
        var creator = Creator();

        // 404: es gibt kein Fach mit dieser Id.
        var notFound = await Assert.ThrowsAsync<PuglingApiException>(() => creator.GetSubjectAsync(999_999));
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(notFound.Code));

        // 400 mit Feldfehlern: Birkenbihl akzeptiert keine Sätze ohne Dekodierung.
        var subject = await creator.CreateSubjectAsync(new CreateSubjectDto("Client-Test Fehler"));
        var chapter = await creator.CreateChapterAsync(subject.Id, new CreateChapterDto("Kapitel", 1));
        var invalid = await Assert.ThrowsAsync<PuglingApiException>(() =>
            creator.CreateExerciseAsync(subject.Id, chapter.Id, "birkenbihl",
                new ExercisePayload<BirkenbihlConfig>("Ohne Dekodierung", 1, 5, new BirkenbihlConfig
                {
                    LearningLang = "en",
                    NativeLang = "de",
                    Sentences = [new BirkenbihlSentence(0, "I go to school.", "Ich gehe zur Schule.", null!)],
                })));

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("validation_error", invalid.Code);
        Assert.NotEmpty(invalid.Errors);
    }

    [Fact]
    public async Task Falsche_Rolle_liefert_403_statt_stiller_Fehlfunktion()
    {
        // Herr Schmidt ist reiner Lehrer – er betreut kein Kind, darf also dessen Punkte nicht lesen.
        var teacherAsSupervisor = new SupervisorApi(Authenticated(2, "9999"));

        var error = await Assert.ThrowsAsync<PuglingApiException>(() => teacherAsSupervisor.GetPointsAsync(1));

        Assert.Contains(error.StatusCode, new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound });
        Assert.False(string.IsNullOrWhiteSpace(error.Code));
    }

    [Fact]
    public async Task Birkenbihl_wird_zweistufig_angelegt_und_serverseitig_dekodiert()
    {
        var creator = Creator();

        var subject = await creator.CreateSubjectAsync(new CreateSubjectDto("Client-Test Birkenbihl"));
        var chapter = await creator.CreateChapterAsync(subject.Id, new CreateChapterDto("Kapitel", 1));
        var exercise = await creator.CreateExerciseAsync(subject.Id, chapter.Id, "birkenbihl",
            new ExercisePayload<BirkenbihlConfig>("At the school gate", 1, 15, new BirkenbihlConfig
            {
                LearningLang = "en",
                NativeLang = "de",
                Sentences = [],
            }));

        var sentence = await creator.AddBirkenbihlSentenceAsync(subject.Id, chapter.Id, exercise.Id,
            new BirkenbihlSentenceInput("I go to school every day.", "Ich gehe jeden Tag zur Schule."));

        Assert.Equal(1, sentence.SentenceId);
        Assert.NotEmpty(sentence.Result);
        // Wort-Ids sind übungsweit eindeutig und werden serverseitig vergeben.
        Assert.Equal(sentence.Result.Select(w => w.WordId).Distinct().Count(), sentence.Result.Count);
        Assert.Contains(sentence.Result, w => w.LearningWord == "go");
    }

    /// <summary>
    /// Der Beweis, dass die <b>geteilte</b> Taxonomie über die Ebenen hinweg trägt: der Creator taggt ein
    /// Bild, der Supervisor pflegt dasselbe Interesse am Kind – und beide landen auf demselben Tag. Genau
    /// darauf setzt die spätere Bildauswahl auf; zwei getrennte Vokabulare könnten nur raten.
    /// </summary>
    [Fact]
    public async Task Medien_und_Kind_Interessen_treffen_denselben_Tag()
    {
        var creator = Creator();
        var supervisor = Supervisor();

        var asset = await creator.CreateMediaAsync(new CreateMediaAssetDto(
            "Ein Astronaut schwebt vor der Erde",
            Rating: ContentRating.Everyone,
            Origin: MediaOrigin.Stock,
            Tags: ["Weltraum", "Foto"],
            Variants: [new CreateMediaVariantDto(MediaPurpose.Card, "https://cdn.test/astro-512.webp", 512, 512)]));

        // Enum-Parität in beide Richtungen (String über die Leitung, Enum im Vertrag).
        Assert.Equal(ContentRating.Everyone, asset.Rating);
        Assert.Equal(MediaOrigin.Stock, asset.Origin);
        Assert.Equal(MediaPurpose.Card, Assert.Single(asset.Variants).Purpose);
        Assert.Contains("weltraum", asset.Tags);

        var interests = await supervisor.SetInterestsAsync(1, new SetChildInterestsDto(
            [new ChildInterestInput(3, Slug: "weltraum"), new ChildInterestInput(-2, Label: "Spinnen")]));

        var space = Assert.Single(interests, i => i.Slug == "weltraum");
        Assert.Equal(3, space.Weight);
        // Negatives Gewicht = Abneigung; sie schließt passende Bilder später hart aus.
        Assert.Equal(-2, Assert.Single(interests, i => i.Slug == "spinnen").Weight);

        // Dieselbe Tag-Zeile trägt jetzt beide Seiten – das ist der Angelpunkt.
        var tag = Assert.Single(await creator.ListInterestTagsAsync("weltraum"));
        Assert.Equal(space.TagId, tag.Id);
        Assert.Equal(1, tag.MediaCount);
        Assert.Equal(1, tag.ChildCount);
    }

    /// <summary>
    /// Die Zuordnung ist n:m in beide Richtungen – der Grund für eine eigene Ressource statt einer Spalte
    /// am Träger. Hier über den Client: ein Motiv bekommt zwei Darstellungen, eine davon dient zusätzlich
    /// einem zweiten Wort.
    /// </summary>
    [Fact]
    public async Task Ein_Motiv_traegt_mehrere_Bilder_und_ein_Bild_dient_mehreren_Woertern()
    {
        var creator = Creator();

        var unicorn = await creator.CreateMediaAsync(new CreateMediaAssetDto(
            "Ein Einhorn laeuft im Comic-Stil", Key: "client_run_unicorn", Tags: ["Einhorn", "Comic"]));
        var flash = await creator.CreateMediaAsync(new CreateMediaAssetDto(
            "Flash rennt", Key: "client_run_flash", Tags: ["Superhelden"]));

        var en = await creator.CreateVocabularyAsync(new CreateVocabularyDto(null, "en", "de", "run", "laufen"));
        var de = await creator.CreateVocabularyAsync(new CreateVocabularyDto(null, "de", "en", "laufen", "run"));

        // Ein Wort, zwei Darstellungen – der Rang entscheidet nur bei Gleichstand der Interessen.
        await creator.LinkVocabularyMediaAsync(en.Id, new AddMediaLinkDto(unicorn.Id, Weight: 5));
        await creator.LinkVocabularyMediaAsync(en.Id, new AddMediaLinkDto(Key: "client_run_flash"));
        // Dasselbe Bild an einem zweiten Wort (getrennte Store-Zeile je Sprachrichtung).
        await creator.LinkVocabularyMediaAsync(de.Id, new AddMediaLinkDto(unicorn.Id));

        var forEnglish = await creator.ListVocabularyMediaAsync(en.Id);
        Assert.Equal(2, forEnglish.Count);
        Assert.Equal("client_run_unicorn", forEnglish[0].Asset.Key);

        var usage = await creator.GetMediaUsageAsync(unicorn.Id);
        Assert.Equal(2, usage.Count);
        Assert.All(usage, u => Assert.Equal("vocabulary", u.Carrier));

        // Löschen ist nicht gesperrt: ohne Bild schrumpft nur die Auswahl, es bleibt kein Platzhalter.
        await creator.DeleteMediaAsync(flash.Id);
        Assert.Single(await creator.ListVocabularyMediaAsync(en.Id));
    }

    /// <summary>
    /// Der Upload über den Client: ein Agent liefert Bytes, der Server macht daraus die Auflösungen.
    /// Genau der Weg, den ein KI-Creator später nimmt – er kann Bilder erzeugen, aber nicht hosten.
    /// </summary>
    [Fact]
    public async Task Client_laedt_ein_Bild_hoch_und_der_Server_erzeugt_die_Aufloesungen()
    {
        var creator = Creator();

        var asset = await creator.UploadMediaAsync(TinyPng(600, 300), "generiert.png",
            // Eigenes Schlagwort: die Tests dieser Klasse teilen sich eine DB, und ein Nachbartest zählt
            // die Assets an „weltraum".
            "Ein generiertes Motiv", tags: ["Raumfahrt"], origin: MediaOrigin.Generated);

        Assert.Equal(MediaOrigin.Generated, asset.Origin);
        Assert.Contains("raumfahrt", asset.Tags);
        // Thumb/Card aus einer Datei; Full bleibt bei der Quellbreite (kein Hochskalieren).
        Assert.Equal(3, asset.Variants.Count);
        Assert.Equal(128, Assert.Single(asset.Variants, v => v.Purpose == MediaPurpose.Thumb).Width);
        Assert.Equal(600, Assert.Single(asset.Variants, v => v.Purpose == MediaPurpose.Full).Width);
        Assert.Matches("^#[0-9a-f]{6}$", asset.Placeholder!);
    }

    /// <summary>Ein echtes PNG – der Server soll an dekodierbaren Bytes arbeiten, nicht an einer Attrappe.</summary>
    private static byte[] TinyPng(int width, int height)
    {
        using var bitmap = new SkiaSharp.SKBitmap(width, height);
        using (var canvas = new SkiaSharp.SKCanvas(bitmap)) canvas.Clear(SkiaSharp.SKColors.SlateBlue);
        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>Der Eignungs-Filter blendet aus, was ein Kindprofil nie sehen dürfte.</summary>
    [Fact]
    public async Task MaxRating_blendet_nicht_freigegebene_Darstellungen_aus()
    {
        var creator = Creator();
        const string marker = "client-rating-motiv";

        await creator.CreateMediaAsync(new CreateMediaAssetDto($"{marker} kindgerecht"));
        await creator.CreateMediaAsync(new CreateMediaAssetDto($"{marker} erwachsen", Rating: ContentRating.Mature));

        Assert.Equal(2, (await creator.ListMediaAsync(marker)).Count);

        var kidSafe = Assert.Single(await creator.ListMediaAsync(marker, maxRating: ContentRating.Everyone));
        Assert.Equal(ContentRating.Everyone, kidSafe.Rating);
    }
}
