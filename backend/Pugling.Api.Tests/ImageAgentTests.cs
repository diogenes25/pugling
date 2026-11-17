using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Pugling.Agent.Creator.Images;
using Pugling.Client;

namespace Pugling.Api.Tests;

/// <summary>
/// The <c>images</c> verb of the AI creator (B-485) against the real API with a <see cref="FakeChatClient"/> for the
/// motif and a <see cref="FakeImageGenerator"/> for the drawing. The end-to-end case runs through the card delivery:
/// only there does it show that children with different interests see different images of the same word.
/// </summary>
public class ImageAgentTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // One answer serves both questions: the motif call reads imageable/motif, the scene call reads scene.
    private const string Motif =
        """{"imageable": true, "motif": "a happy dog jumping", "scene": "a happy dog jumping over a soccer ball"}""";
    private const string WovenScene = "a happy dog jumping over a soccer ball";
    private const int ShowBoth = 1;

    /// <summary>The run as the host wires it, with the fakes in place of Ollama and the image service.</summary>
    internal static ImageRun Run(CreatorApi creator, IChatClient chat, IImageGenerator generator,
        ImageOptions? options = null) =>
        new(creator, new MotifPlanner(chat, NullLogger<MotifPlanner>.Instance),
            new Lazy<IImageGenerator>(() => generator), Options.Create(options ?? new ImageOptions()),
            NullLogger<ImageRun>.Instance);

    private HttpClient Authenticated() =>
        factory.CreateDefaultClient(AuthHandler.Standalone(new PuglingClientOptions
        {
            BaseUrl = "http://localhost",
            AccountId = 1,
            Pin = "0000",
        }));

    /// <summary>
    /// Red if the dry run calls the image service or writes to the store (AK 4), or if the prompt loses the motif, the
    /// tag's scene, the style or the no-text rule (AK 8).
    /// </summary>
    [Fact]
    public async Task Der_Trockenlauf_zeichnet_nichts_und_zeigt_die_Prompts()
    {
        var father = await TestApi.AdultAsync(factory);
        var s = await ScenarioAsync(father, "img1");
        await SetInterestsAsync(father, s.ChildIds[0], (s.Soccer, 3));
        var generator = new FakeImageGenerator();

        var outcome = await Run(new CreatorApi(Authenticated()), new FakeChatClient(Motif), generator)
            .RunAsync(new ImageRunRequest(s.ChildIds[0], null, null, 50, DryRun: true), default);

        Assert.Empty(generator.Prompts);
        Assert.Equal(4, outcome.Jobs.Count);
        Assert.All(outcome.Jobs, j => Assert.Null(j.AssetId));
        var soccer = Assert.Single(outcome.Jobs, j => j.VocabularyId == s.VocabularyIds[0] && j.TagSlug == s.Soccer);
        Assert.StartsWith(WovenScene + ", ", soccer.Prompt);
        Assert.Contains(new ImageOptions().Style, soccer.Prompt);
        Assert.EndsWith(ImagePromptBuilder.NoText, soccer.Prompt);
        var neutral = Assert.Single(outcome.Jobs, j => j.VocabularyId == s.VocabularyIds[0] && j.TagSlug is null);
        Assert.StartsWith("a happy dog jumping, ", neutral.Prompt);
        Assert.Empty((await GetAsync(father, $"/api/v1/creator/vocabulary/{s.VocabularyIds[0]}/media")).EnumerateArray());
    }

    /// <summary>
    /// The whole path: images are created as generated assets with trace, tag and a card variant (AK 4), a second run
    /// draws nothing (AK 6), and the card delivery gives each child its own picture (AK 5): the soccer child the soccer
    /// image, the space child the space image, the child without interests and the child who dislikes soccer the
    /// neutral one. Red if the interest links lose their lower rank (asserted on the links themselves – through the cards
    /// alone the selector's hash decides and the check would only fail by chance), if tags are not attached, or if the
    /// demand does not shrink after linking.
    /// </summary>
    [Fact]
    public async Task Kinder_mit_verschiedenen_Interessen_sehen_verschiedene_Bilder_desselben_Worts()
    {
        var father = await TestApi.AdultAsync(factory);
        var s = await ScenarioAsync(father, "img2", children: 4);
        await SetInterestsAsync(father, s.ChildIds[0], (s.Soccer, 3));
        await SetInterestsAsync(father, s.ChildIds[1], (s.Space, 3));
        await SetInterestsAsync(father, s.ChildIds[3], (s.Soccer, -3));
        var creator = new CreatorApi(Authenticated());
        var generator = new FakeImageGenerator();

        var first = await Run(creator, new FakeChatClient(Motif), generator)
            .RunAsync(new ImageRunRequest(s.ChildIds[0], null, null, 50, DryRun: false), default);
        var second = await Run(creator, new FakeChatClient(Motif), generator)
            .RunAsync(new ImageRunRequest(s.ChildIds[1], null, null, 50, DryRun: false), default);

        // Child 0: neutral + soccer for both words; child 1: only space – the neutral images exist already.
        Assert.Equal(4, first.Jobs.Count);
        Assert.Equal(2, second.Jobs.Count);
        Assert.All(first.Jobs.Concat(second.Jobs), j => Assert.Null(j.Error));

        int[] AssetsOf(ImageRunOutcome run, string? tag) => [.. run.Jobs.Where(j => j.TagSlug == tag).Select(j => j.AssetId!.Value)];
        var soccerAssets = AssetsOf(first, s.Soccer);
        var neutralAssets = AssetsOf(first, null);
        var spaceAssets = AssetsOf(second, s.Space);
        var soccerAsset = soccerAssets[0];

        var asset = await GetAsync(father, $"/api/v1/creator/media/{soccerAsset}");
        Assert.Equal("Generated", asset.GetProperty("origin").GetString());
        // The run must store its own key: a rerun finds an unlinked image only by it. Found in the role walk – the key
        // was derived from the alt text, so the reuse test below passed on a state the agent never produced.
        var soccerJob = first.Jobs.First(j => j.AssetId == soccerAsset);
        Assert.Equal(ImagePromptBuilder.Key(soccerJob.VocabularyId, s.Soccer), asset.GetProperty("key").GetString());
        // The trace carries the scene that was drawn – for an interest image the woven one (decision 10).
        Assert.Equal($"gpt-image-1-mini: {WovenScene}; {s.Soccer}", asset.GetProperty("source").GetString());
        Assert.Equal([s.Soccer], asset.GetProperty("tags").EnumerateArray().Select(t => t.GetString()!));
        Assert.Contains(asset.GetProperty("variants").EnumerateArray(), v => v.GetProperty("purpose").GetString() == "Card");
        Assert.DoesNotContain("dog-img2", asset.GetProperty("description").GetString());

        // Idempotent: the same child again finds nothing to draw.
        var again = await Run(creator, new FakeChatClient(Motif), generator)
            .RunAsync(new ImageRunRequest(s.ChildIds[0], null, null, 50, DryRun: false), default);
        Assert.Empty(again.Jobs);

        var links = await GetAsync(father, $"/api/v1/creator/vocabulary/{s.VocabularyIds[0]}/media");
        Assert.All(links.EnumerateArray(), l => Assert.Equal(
            l.GetProperty("asset").GetProperty("tags").GetArrayLength() == 0 ? 0 : ImageRun.InterestLinkWeight,
            l.GetProperty("weight").GetInt32()));

        // The first card of a session may be either word, so each child is checked against its kind of image.
        AssertImageFrom(soccerAssets, await CardImageAsync(father, s, 0));
        AssertImageFrom(spaceAssets, await CardImageAsync(father, s, 1));
        AssertImageFrom(neutralAssets, await CardImageAsync(father, s, 2));
        AssertImageFrom(neutralAssets, await CardImageAsync(father, s, 3));
    }

    /// <summary>
    /// A hand-made untagged image counts as the neutral one: the run draws none of its own, and the child without
    /// interests gets the hand-made image, not the generated soccer one. Red if the interest images were linked at the
    /// default rank 0 – then hand image and soccer image tie and the selector's hash decides; that is why the tie is
    /// made deterministic here by asserting the ranks, and the card only confirms the outcome.
    /// </summary>
    [Fact]
    public async Task Ein_handgemachtes_neutrales_Bild_schlaegt_fremde_Interessen_Bilder()
    {
        var father = await TestApi.AdultAsync(factory);
        var s = await ScenarioAsync(father, "img4", children: 2);
        await SetInterestsAsync(father, s.ChildIds[0], (s.Soccer, 3));
        foreach (var vocabularyId in s.VocabularyIds)
        {
            var assetId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/media", new
            {
                key = $"img4-hand-{vocabularyId}",
                description = "Von Hand",
                variants = new[] { new { purpose = "Card", url = $"https://cdn.test/img4-{vocabularyId}.webp", width = 512, height = 512 } },
            }));
            (await father.PostAsJsonAsync($"/api/v1/creator/vocabulary/{vocabularyId}/media",
                new { mediaAssetId = assetId })).EnsureSuccessStatusCode();
        }
        var generator = new FakeImageGenerator();

        var outcome = await Run(new CreatorApi(Authenticated()), new FakeChatClient(Motif), generator)
            .RunAsync(new ImageRunRequest(s.ChildIds[0], null, null, 50, DryRun: false), default);

        Assert.All(outcome.Jobs, j => Assert.Equal(s.Soccer, j.TagSlug));
        Assert.Equal(2, outcome.Jobs.Count);
        var links = await GetAsync(father, $"/api/v1/creator/vocabulary/{s.VocabularyIds[0]}/media");
        Assert.Contains(links.EnumerateArray(), l => l.GetProperty("weight").GetInt32() < 0);
        Assert.All(links.EnumerateArray().Where(l => l.GetProperty("weight").GetInt32() < 0),
            l => Assert.Equal([s.Soccer], l.GetProperty("asset").GetProperty("tags").EnumerateArray().Select(t => t.GetString()!)));
        // Hand-made images carry an external URL, not the store path with the asset id.
        Assert.StartsWith("https://cdn.test/img4-", await CardImageAsync(father, s, 1));
    }

    /// <summary>
    /// Idempotency beyond the demand: an image a previous run uploaded but did not link is linked without drawing it
    /// again (paid once), and an image already linked is not a failure. Red if the key lookup is dropped (the generator
    /// is called) or if "already linked" is reported as an error (every later run would exit 1 on it).
    /// </summary>
    [Fact]
    public async Task Ein_frueherer_Lauf_wird_aufgenommen_statt_doppelt_bezahlt()
    {
        var father = await TestApi.AdultAsync(factory);
        var s = await ScenarioAsync(father, "img5");
        var creator = new CreatorApi(Authenticated());
        // "dog": stored under the run's key, never linked (the link failed last time).
        var dog = await GeneratedAssetAsync(father, ImagePromptBuilder.Key(s.VocabularyIds[0], null), []);
        // "jump": linked under the run's key, but a creator tagged it since – so the demand wants a neutral image again.
        var jump = await GeneratedAssetAsync(father, ImagePromptBuilder.Key(s.VocabularyIds[1], null), [s.Space]);
        (await father.PostAsJsonAsync($"/api/v1/creator/vocabulary/{s.VocabularyIds[1]}/media",
            new { mediaAssetId = jump })).EnsureSuccessStatusCode();
        var generator = new FakeImageGenerator();
        var chat = new FakeChatClient(Motif);

        var outcome = await Run(creator, chat, generator)
            .RunAsync(new ImageRunRequest(s.ChildIds[0], null, null, 50, DryRun: false), default);

        Assert.Empty(generator.Prompts);
        // Taken up before the model is asked: a model failure on this rerun could otherwise strand the stored images.
        Assert.Equal(0, chat.Calls);
        Assert.Equal(2, outcome.Jobs.Count);
        Assert.All(outcome.Jobs, j => Assert.Null(j.Error));
        var dogLinks = await GetAsync(father, $"/api/v1/creator/vocabulary/{s.VocabularyIds[0]}/media");
        Assert.Contains(dogLinks.EnumerateArray(), l => l.GetProperty("asset").GetProperty("id").GetInt32() == dog);
    }

    /// <summary>
    /// Red if a word the model calls undrawable is drawn anyway, if a tag without prompt reaches the generator by its
    /// label (decision 6), or if one failed image stops the rest of the run.
    /// </summary>
    [Fact]
    public async Task Unzeichenbare_Woerter_Tags_ohne_Prompt_und_Fehler_bleiben_einzeln()
    {
        var father = await TestApi.AdultAsync(factory);
        var s = await ScenarioAsync(father, "img3");
        await SetInterestsAsync(father, s.ChildIds[0], (s.Soccer, 3), (s.NoPrompt, 2));
        var generator = new FakeImageGenerator { FailWhenPromptContains = WovenScene };
        // First word: not drawable; second word: drawable.
        var chat = new FakeChatClient("""{"imageable": false, "motif": ""}""", Motif);

        var outcome = await Run(new CreatorApi(Authenticated()), chat, generator)
            .RunAsync(new ImageRunRequest(s.ChildIds[0], null, null, 50, DryRun: false), default);

        Assert.Equal(["dog-img3"], outcome.NotImageable);
        Assert.Equal([s.NoPrompt], outcome.MissingPrompt);
        // Neutral and soccer for "jump" – the prompt-less tag never reaches the generator, "dog" not at all.
        Assert.Equal(2, generator.Prompts.Count);
        var neutral = Assert.Single(outcome.Jobs, j => j.TagSlug is null);
        Assert.NotNull(neutral.AssetId);
        var failed = Assert.Single(outcome.Jobs, j => j.TagSlug == s.Soccer);
        Assert.NotNull(failed.Error);
    }

    /// <summary>
    /// The response format reaches the service only when configured. Red if the setting is not passed on (a local
    /// server answers with a URL and every image fails with "no image"), or if it is sent by default (OpenAI's GPT image
    /// models reject the parameter).
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData(ImageGenerationResponseFormat.Data)]
    public async Task Das_Antwortformat_geht_nur_eingestellt_an_den_Dienst(ImageGenerationResponseFormat? format)
    {
        var father = await TestApi.AdultAsync(factory);
        var s = await ScenarioAsync(father, format is null ? "img6" : "img7");
        var generator = new FakeImageGenerator();

        await Run(new CreatorApi(Authenticated()), new FakeChatClient(Motif), generator,
                new ImageOptions { ResponseFormat = format })
            .RunAsync(new ImageRunRequest(s.ChildIds[0], null, null, 50, DryRun: false), default);

        Assert.NotEmpty(generator.ResponseFormats);
        Assert.All(generator.ResponseFormats, f => Assert.Equal(format, f));
    }

    /// <summary>
    /// Decision 10: the interest is woven into the scene by the model, which sees the tag's image prompt but never its
    /// label or anything about a child. Red if the interest image still uses the appended setting, if the scene question
    /// carries the label (a brand name in the real data) or the child's name, or if the neutral image asks for a scene.
    /// </summary>
    [Fact]
    public async Task Das_Interesse_steht_in_der_Szene_und_das_Modell_sieht_nur_den_Bild_Prompt()
    {
        var father = await TestApi.AdultAsync(factory);
        var s = await ScenarioAsync(father, "img8");
        await SetInterestsAsync(father, s.ChildIds[0], (s.Soccer, 3));
        var chat = new FakeChatClient(Motif);

        var outcome = await Run(new CreatorApi(Authenticated()), chat, new FakeImageGenerator())
            .RunAsync(new ImageRunRequest(s.ChildIds[0], null, null, 1, DryRun: true), default);

        var soccer = Assert.Single(outcome.Jobs, j => j.TagSlug == s.Soccer);
        Assert.DoesNotContain("on a soccer pitch", soccer.Prompt);
        // One motif question and one scene question for the one word – none for the neutral image.
        Assert.Equal(2, chat.History.Count);
        // The user turn only: it is the part built from data; the system turn is fixed text.
        var sceneQuestion = chat.History[1].Messages.Single(m => m.Role == ChatRole.User).Text;
        Assert.Contains("Interesse: on a soccer pitch", sceneQuestion);
        Assert.Contains("Szene der Bedeutung: a happy dog jumping", sceneQuestion);
        Assert.DoesNotContain("img8 Fussball", sceneQuestion);
        Assert.DoesNotContain("Kind", sceneQuestion);
        // Both questions carry the child rule – people as children, no fighters, no weapons. Red if either drops it.
        Assert.All(chat.History, call => Assert.Contains("Personen sind Kinder",
            call.Messages.Single(m => m.Role == ChatRole.System).Text));
    }

    /// <summary>
    /// Decision 10: no usable scene means no interest image – not the old appended setting, which would file a picture
    /// without the interest under its tag. Red if the run falls back, or if the neutral image is lost along with it.
    /// </summary>
    [Fact]
    public async Task Ohne_brauchbare_Szene_entfaellt_nur_das_Interessen_Bild()
    {
        var father = await TestApi.AdultAsync(factory);
        var s = await ScenarioAsync(father, "img9");
        await SetInterestsAsync(father, s.ChildIds[0], (s.Soccer, 3));
        var generator = new FakeImageGenerator();
        // The motif passes, the scene answer has no scene.
        var chat = new FakeChatClient("""{"imageable": true, "motif": "a happy dog jumping"}""");

        var outcome = await Run(new CreatorApi(Authenticated()), chat, generator)
            .RunAsync(new ImageRunRequest(s.ChildIds[0], null, null, 1, DryRun: false), default);

        Assert.Equal([$"dog-img9 [{s.Soccer}]"], outcome.NoScene);
        var neutral = Assert.Single(outcome.Jobs);
        Assert.Null(neutral.TagSlug);
        Assert.NotNull(neutral.AssetId);
        Assert.Single(generator.Prompts);
    }

    /// <summary>
    /// A service with a negative prompt gets the no-text rule there: diffusion models draw what the prompt names, "no
    /// letters" included. Red if the rule stays in the positive part or the separator is ignored.
    /// </summary>
    [Fact]
    public void Mit_Negativ_Trenner_wandert_das_Schriftverbot_in_den_Negativ_Prompt()
    {
        Assert.Equal("a cat, plain style|" + ImagePromptBuilder.NegativeTerms,
            ImagePromptBuilder.Prompt("a cat", "plain style", "|"));
        Assert.DoesNotContain("no text", ImagePromptBuilder.Prompt("a cat", "plain style", "|"));
    }

    /// <summary>
    /// The negative prompt carries the child-safety terms, not only the no-text rule. Red if they are dropped: the first
    /// run on real data drew a near-naked figure and a low-cut warrior for a child's cards with only "text" forbidden.
    /// </summary>
    [Theory]
    [InlineData("nude")]
    [InlineData("underwear")]
    [InlineData("cleavage")]
    [InlineData("weapon")]
    [InlineData("logo")]
    public void Der_Negativ_Prompt_schuetzt_das_Kind(string term)
    {
        var negative = ImagePromptBuilder.Prompt("a cat", "plain style", "|").Split('|')[1];
        Assert.Contains(term, negative.Split(", "));
    }

    /// <summary>
    /// Red if the separator in the model's scene splits the prompt (part of the motif would land in the negative prompt
    /// and be avoided), or if a blank separator counts as set (the no-text terms would end up in the positive prompt).
    /// </summary>
    [Fact]
    public void Ein_Trenner_in_der_Szene_und_ein_leerer_Trenner_spalten_den_Prompt_nicht()
    {
        Assert.Equal("a dog   soccer ball, plain style|" + ImagePromptBuilder.NegativeTerms,
            ImagePromptBuilder.Prompt("a dog | soccer ball", "plain style", "|"));
        Assert.Equal("a cat, plain style, " + ImagePromptBuilder.NoText, ImagePromptBuilder.Prompt("a cat", "plain style", " "));
    }

    /// <summary>
    /// The configured separator reaches the prompt of the run, not only the builder. Red if the run drops it – LocalAI
    /// would then get "no text, no letters" in the positive prompt, which a diffusion model tends to draw.
    /// </summary>
    [Fact]
    public async Task Der_Lauf_reicht_den_Negativ_Trenner_an_jeden_Prompt_weiter()
    {
        var father = await TestApi.AdultAsync(factory);
        var s = await ScenarioAsync(father, "img10");
        await SetInterestsAsync(father, s.ChildIds[0], (s.Soccer, 3));

        var outcome = await Run(new CreatorApi(Authenticated()), new FakeChatClient(Motif), new FakeImageGenerator(),
                new ImageOptions { NegativeSeparator = "|" })
            .RunAsync(new ImageRunRequest(s.ChildIds[0], null, null, 1, DryRun: true), default);

        Assert.Equal(2, outcome.Jobs.Count);
        Assert.All(outcome.Jobs, j =>
        {
            Assert.EndsWith("|" + ImagePromptBuilder.NegativeTerms, j.Prompt);
            Assert.DoesNotContain(ImagePromptBuilder.NoText, j.Prompt);
        });
    }

    /// <summary>Red if the scene check lets through what the motif check stops, or rejects a scene of allowed length.</summary>
    [Theory]
    [InlineData("a dog over a soccer ball", "a dog over a soccer ball")]
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData("line one\nline two", null)]
    public void Die_Szenenpruefung_laesst_nur_kurze_Szenen_durch(string? scene, string? expected) =>
        Assert.Equal(expected, MotifPlanner.CheckScene(new SceneDraft(scene)));

    [Fact]
    public void Eine_Szene_bis_zur_Grenze_besteht_und_darueber_nicht()
    {
        Assert.NotNull(MotifPlanner.CheckScene(new SceneDraft(new string('a', MotifPlanner.MaxSceneLength))));
        Assert.Null(MotifPlanner.CheckScene(new SceneDraft(new string('a', MotifPlanner.MaxSceneLength + 1))));
    }

    /// <summary>Red if the motif check lets through what the prompt must not carry: nothing, a paragraph, a line break.</summary>
    [Theory]
    [InlineData(true, "a dog", "a dog")]
    [InlineData(false, "a dog", null)]
    [InlineData(true, "", null)]
    [InlineData(true, "line one\nline two", null)]
    public void Die_Motivpruefung_laesst_nur_kurze_Szenen_durch(bool imageable, string motif, string? expected) =>
        Assert.Equal(expected, MotifPlanner.Check(new MotifDraft(imageable, motif)));

    [Fact]
    public void Ein_ueberlanges_Motiv_wird_verworfen() =>
        Assert.Null(MotifPlanner.Check(new MotifDraft(true, new string('a', MotifPlanner.MaxMotifLength + 1))));

    /// <summary>
    /// Red if the trace can exceed the column (the server rejects it, the image is lost after it was paid for), if the
    /// key is not stable per word and tag, or if the alt text carries anything that could name the answer.
    /// </summary>
    [Fact]
    public void Spur_Schluessel_und_Alttext_bleiben_in_ihren_Grenzen()
    {
        var source = ImagePromptBuilder.Source("gpt-image-1-mini", new string('m', 400), "fussball");
        Assert.True(source.Length <= ImagePromptBuilder.MaxSourceLength);
        Assert.EndsWith("; fussball", source);
        Assert.Equal("gen-v7-neutral", ImagePromptBuilder.Key(7, null));
        Assert.Equal("gen-v7-fussball", ImagePromptBuilder.Key(7, "fussball"));
        Assert.Equal("Bild zur Vokabel (Fußball)", ImagePromptBuilder.AltText("Fußball"));
        Assert.Equal("a cat, plain style, " + ImagePromptBuilder.NoText, ImagePromptBuilder.Prompt(" a cat ", "plain style", null));
    }

    // ---- Setup ----------------------------------------------------------------------------------------

    private sealed record Scenario(int[] ChildIds, int[] PlanIds, int[] PositionIds, int[] VocabularyIds,
        string Soccer, string Space, string NoPrompt);

    /// <summary>
    /// Children with a running plan each on the same two-word vocabulary exercise, plus three tags: soccer and space with
    /// an image prompt, one without.
    /// </summary>
    private static async Task<Scenario> ScenarioAsync(HttpClient father, string marker, int children = 1)
    {
        var soccer = await TagAsync(father, $"{marker} Fussball", "on a soccer pitch");
        var space = await TagAsync(father, $"{marker} Weltall", "in outer space");
        var noPrompt = await TagAsync(father, $"{marker} Ohneprompt", null);

        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = marker }));
        var seriesId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = $"{marker}-Reihe", subjectId }));
        var seriesUnitId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Unit" }));
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary", new
            {
                title = $"{marker}-Uebung",
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    direction = "front-to-back",
                    sourceLang = "en",
                    targetLang = "de",
                    items = new[]
                    {
                        // Alphabetical order is the demand's order: "dog" comes first.
                        new { front = $"dog-{marker}", back = $"Hund-{marker}" },
                        new { front = $"jump-{marker}", back = $"springen-{marker}" },
                    },
                },
            }));
        var items = await GetAsync(father,
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{exerciseId}/items");
        int[] vocabularyIds = [.. items.EnumerateArray().Select(i => i.GetProperty("vocabularyId").GetInt32())];

        var childIds = new List<int>();
        var planIds = new List<int>();
        var positionIds = new List<int>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var i = 0; i < children; i++)
        {
            var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
                new { name = $"Kind {marker}-{i}", pin = "9872" }));
            var planId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/study-plans", new
            {
                childId,
                subjectId = await TestApi.DefaultSubjectIdAsync(father),
                title = $"{marker}-Plan-{i}",
                startDate = today.AddDays(-1).ToString("yyyy-MM-dd"),
                durationDays = 31,
            }));
            var positionId = await TestApi.IdAsync(await father.PostAsJsonAsync(
                $"/api/v1/supervisor/study-plans/{planId}/positions",
                new { exerciseId, order = 1, cadence = "Daily", goalThreshold = 1, pointsGoalMet = 5 }));
            childIds.Add(childId);
            planIds.Add(planId);
            positionIds.Add(positionId);
        }
        return new Scenario([.. childIds], [.. planIds], [.. positionIds], vocabularyIds, soccer, space, noPrompt);
    }

    /// <summary>The image URL on the first card of child <paramref name="index"/>'s session at a stage that shows images.</summary>
    private async Task<string> CardImageAsync(HttpClient father, Scenario s, int index)
    {
        var (planId, positionId) = (s.PlanIds[index], s.PositionIds[index]);
        // Known words: the card comes at the stage under test instead of as an introduction (B-419).
        TestApi.MarkIntroduced(factory, positionId);
        (await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}",
            new { stage = ShowBoth })).EnsureSuccessStatusCode();
        var start = await father.PostAsJsonAsync(
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions", new { mode = "Lern" });
        start.EnsureSuccessStatusCode();
        var sessionId = await TestApi.IdAsync(start);
        var card = (await GetAsync(father,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions/{sessionId}/next")).GetProperty("card");
        return card.GetProperty("imageUrl").GetString() ?? "";
    }

    private static async Task<int> GeneratedAssetAsync(HttpClient father, string key, string[] tags) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/media", new
        {
            key,
            description = "Bild zur Vokabel",
            origin = "Generated",
            tags,
            variants = new[] { new { purpose = "Card", url = $"https://cdn.test/{key}.webp", width = 512, height = 512 } },
        }));

    private static void AssertImageFrom(int[] assetIds, string imageUrl) =>
        Assert.True(assetIds.Any(id => imageUrl.Contains($"/{id}/", StringComparison.Ordinal)),
            $"Image '{imageUrl}' is none of the assets {string.Join(", ", assetIds)}.");

    private static async Task SetInterestsAsync(HttpClient father, int childId, params (string Slug, int Weight)[] interests) =>
        (await father.PutAsJsonAsync($"/api/v1/supervisor/children/{childId}/interests", new
        {
            interests = interests.Select(i => new { label = i.Slug, weight = i.Weight }),
        })).EnsureSuccessStatusCode();

    private static async Task<string> TagAsync(HttpClient father, string label, string? imagePrompt)
    {
        var res = await father.PostAsJsonAsync("/api/v1/creator/interest-tags", new { label, imagePrompt });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("slug").GetString()!;
    }

    private static async Task<JsonElement> GetAsync(HttpClient client, string url)
    {
        var res = await client.GetAsync(url);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }
}
