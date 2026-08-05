using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// The per-child selection (stage 4) – this is where the whole design comes together: from several
/// representations of a motif, one is chosen for <b>this</b> child, it is frozen (image constancy =
/// memory effect), and it only appears on stages where a motif cannot give away the solution.
/// </summary>
public class MediaSelectionTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // Values from TestStage. Not typed (the solution is visible anyway) → image allowed: ShowBoth, SelfAssess.
    private const int ShowBoth = 1;
    private const int SelfAssess = 2;
    // Typed → the image would give the solution away.
    private const int LetterBoxes = 3;
    private const int FreeText = 4;
    private const int MultipleChoice = 6;

    /// <summary>PIN of the scenario children – for the endpoints that the student calls themselves.</summary>
    private const string ChildPin = "9876";

    [Fact]
    public async Task DasInteresseDesKindes_EntscheidetWelcheDarstellungKommt()
    {
        var father = await TestApi.AdultAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-interesse");

        // The child likes unicorns, not superheroes - of three renditions the unicorn has to come.
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", 3), ("Superhelden", 0)]);

        var card = await FirstCardAsync(father, setup, SelfAssess);
        Assert.Equal("Ein Einhorn laeuft", card.GetProperty("imageAlt").GetString());
        Assert.Contains("unicorn", card.GetProperty("imageUrl").GetString());
    }

    [Fact]
    public async Task Abneigung_SchliesstAus_StattNurSchlechterZuRanken()
    {
        var father = await TestApi.AdultAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-abneigung");

        // "Comic" is strongly positive, but the unicorn additionally carries a rejected tag. A plain score sum
        // would pick it anyway - the hard exclusion must not be outvoted by that.
        await SetInterestsAsync(father, setup.ChildId, [("Comic", 3), ("Einhorn", -3)]);

        var card = await FirstCardAsync(father, setup, SelfAssess);
        Assert.DoesNotContain("unicorn", card.GetProperty("imageUrl").GetString());
    }

    [Fact]
    public async Task Eignung_UeberDerFreigabe_KommtNie()
    {
        var father = await TestApi.AdultAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-rating", includeMature: true);

        // The child "likes" the tag of the unreleased image most - the rating still wins.
        await SetInterestsAsync(father, setup.ChildId, [("Freizuegig", 3), ("Einhorn", 1)]);

        var card = await FirstCardAsync(father, setup, SelfAssess);
        Assert.DoesNotContain("mature", card.GetProperty("imageUrl").GetString());
    }

    [Fact]
    public async Task DieWahlBleibtStabil_AuchWennSpaeterEinBesseresBildDazukommt()
    {
        var father = await TestApi.AdultAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-konstanz");
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", 1)]);

        var first = (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString();

        // An image that fits considerably better by score is added afterwards …
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", 1), ("Pixel-Art", 3)]);
        var late = await CreateAssetAsync(father, $"{setup.Marker}_pixel", "Laeufer in Pixel-Art", ["Pixel-Art"]);
        await father.PostAsJsonAsync($"/api/v1/creator/vocabulary/{setup.VocabularyId}/media",
            new { mediaAssetId = late });

        // … but must not tip the running choice: recognition IS the retention effect.
        var second = (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString();
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task AnderesBild_TauschtAus_UndZiehtDasAbgelehnteNieWieder()
    {
        var father = await TestApi.AdultAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-reshuffle");
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", 3)]);

        var before = (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString();

        var res = await father.PostAsJsonAsync($"/api/v1/student/children/{setup.ChildId}/media-picks/reshuffle",
            new { vocabularyId = setup.VocabularyId });
        res.EnsureSuccessStatusCode();
        var after = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("imageUrl").GetString();
        Assert.NotEqual(before, after);

        // The card follows the new choice …
        Assert.Equal(after, (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString());

        // … and the rejected image does not reappear even after further re-choosing.
        var seen = new List<string?> { before, after };
        while (true)
        {
            var next = await father.PostAsJsonAsync($"/api/v1/student/children/{setup.ChildId}/media-picks/reshuffle",
                new { vocabularyId = setup.VocabularyId });
            if (next.StatusCode == HttpStatusCode.Conflict) break;
            next.EnsureSuccessStatusCode();
            var url = (await next.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("imageUrl").GetString();
            Assert.DoesNotContain(url, seen);
            seen.Add(url);
        }
        Assert.Equal(3, seen.Count); // exactly the three renditions of the scenario
    }

    [Fact]
    public async Task OhneAlternative_BleibtDasBildStehen_StattZuVerschwinden()
    {
        var father = await TestApi.AdultAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-letztes", assetCount: 1);

        var before = (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString();

        var res = await father.PostAsJsonAsync($"/api/v1/student/children/{setup.ChildId}/media-picks/reshuffle",
            new { vocabularyId = setup.VocabularyId });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("media_no_alternative", await CodeOf(res));

        // The decisive part: the only candidate was NOT burned - the card still carries its image.
        Assert.Equal(before, (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString());
    }

    /// <summary>
    /// <b>A tie score</b> is the only case where the tiebreak actually decides anything – and that is
    /// exactly why the documented determinism guarantee ("no <c>Random</c>, no
    /// <c>string.GetHashCode</c>") was unguarded: none of the other tests produce a tie
    /// (docs/testplan.md, injection D07). Image constancy <i>is</i> the memory effect in vocabulary
    /// learning; a random tiebreak would destroy it for every carrier that has not yet been frozen.
    /// <para>
    /// Between rounds, the test clears the freezing – otherwise it would only verify that step 3 of the
    /// cascade (the frozen choice wins) works, and the tiebreak would never be exercised again.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Punktgleichstand_WirdDeterministischGebrochen_NichtZufaellig()
    {
        var father = await TestApi.AdultAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-gleichstand", assetCount: 0);

        // Two renditions without tags and with the same editorial rank: identical score (0 - the child has no
        // interests), identical weight. So the choice hangs on the tiebreak alone.
        foreach (var variant in new[] { "a", "b" })
        {
            var assetId = await CreateAssetAsync(father, $"{setup.Marker}_{variant}", $"Variante {variant}", []);
            (await father.PostAsJsonAsync($"/api/v1/creator/vocabulary/{setup.VocabularyId}/media",
                new { mediaAssetId = assetId, weight = 0 })).EnsureSuccessStatusCode();
        }

        var seen = new List<string?>();
        for (var round = 0; round < 8; round++)
        {
            ClearPicks(setup.ChildId);
            seen.Add((await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString());
        }
        Assert.Single(seen.Distinct());

        // The counter-check against a vacuously green test: only if BOTH images really were up for choice did
        // the tiebreak decide anything. So "another image" has to hand out the second one.
        var other = await father.PostAsJsonAsync($"/api/v1/student/children/{setup.ChildId}/media-picks/reshuffle",
            new { vocabularyId = setup.VocabularyId });
        other.EnsureSuccessStatusCode();
        Assert.DoesNotContain((await other.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("imageUrl").GetString(), seen);
    }

    /// <summary>
    /// The guarantee is <b>process-independent</b> determinism, not just "the same within a single run".
    /// This is exactly where FNV-1a differs from <c>string.GetHashCode</c>, whose seed is randomized per
    /// process: a restart would shift the choice of every carrier that has not yet been frozen. Comparing
    /// two calls within the same process cannot show this – fixed golden values can. If this test fails,
    /// the hash has changed, and with it the image choice for all children whose carriers are not yet
    /// frozen.
    /// </summary>
    [Fact]
    public void Tiebreak_LiefertFestgeschriebeneGoldwerte()
    {
        Assert.Equal(467997332u, Tiebreak(1, 1, 1));
        Assert.Equal(2034659765u, Tiebreak(1, 2, 3));
        Assert.Equal(3601875931u, Tiebreak(7, 42, 99));
    }

    /// <summary>
    /// B-01: A final test must not decide which image the child later sees while practising. It renders
    /// no image at all – so freezing one is a pure side effect, and a lasting one: the frozen pick is
    /// the choice from then on, and a superseded one is deleted along the way.
    /// </summary>
    [Fact]
    public async Task Abschlusstest_SchreibtKeineBildwahlFest()
    {
        var father = await TestApi.AdultAsync(factory);
        var setup = await ScenarioAsync(father, "klausur-keine-wahl");
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", 3)]);
        // A blank slate: whatever picks exist after the run were written by the run.
        ClearPicks(setup.ChildId);

        var testUrl = $"/api/v1/student/study-plans/{setup.PlanId}/positions/{setup.PositionId}/tests";
        var start = await father.PostAsJsonAsync(testUrl, new { stage = SelfAssess });
        start.EnsureSuccessStatusCode();
        var attemptId = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("attemptId").GetInt32();

        // Both paths of the exam: the start (order freeze) and fetching a question.
        var question = (await GetAsync(father, $"{testUrl}/{attemptId}/next")).GetProperty("item");
        Assert.False(IsNull(question, "prompt"));

        Assert.Empty(PicksOf(setup.ChildId));
    }

    /// <summary>Clears a child's frozen image picks so the selection recalculates.</summary>
    private void ClearPicks(int childId)
    {
        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<PuglingDbContext>()
            .ChildMediaPicks.Where(p => p.ChildId == childId).ExecuteDelete();
    }

    /// <summary>The child's frozen image picks – the row that must stay untouched by a test run.</summary>
    private List<ChildMediaPick> PicksOf(int childId)
    {
        using var scope = factory.Services.CreateScope();
        return [.. scope.ServiceProvider.GetRequiredService<PuglingDbContext>()
            .ChildMediaPicks.Where(p => p.ChildId == childId)];
    }

    /// <summary>
    /// The tiebreak is private (it belongs to no one but the selection) – it is called reflectively for
    /// the golden values. If it disappears, this test should fail loudly rather than vanish silently.
    /// </summary>
    private static uint Tiebreak(int childId, int carrierId, int assetId)
    {
        var method = typeof(MediaSelector).GetMethod("StableTiebreak", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (uint)method.Invoke(null, [childId, carrierId, assetId])!;
    }

    /// <summary>
    /// If the frozen choice later becomes inadmissible (here: the father adds an aversion against its
    /// motif afterwards), the old freeze must be <b>withdrawn</b>, not just overridden. Otherwise it
    /// would remain the active choice, the new selection would be recomputed on every call, and the
    /// second freeze would violate the unique index: the card would become permanently unavailable for
    /// this child – with no way back via the API.
    /// </summary>
    [Fact]
    public async Task UnzulaessigGewordeneWahl_WirdZurueckgezogen_StattDieKarteZuVerbrennen()
    {
        var father = await TestApi.AdultAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-veraltet");
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", 3)]);

        Assert.Contains("unicorn", (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString());

        // The frozen motif drops out of the selection - a dislike excludes hard.
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", -3)]);
        var second = (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString();
        Assert.DoesNotContain("unicorn", second);

        // The actual regression test is the third request: it used to run into the unique index.
        Assert.Equal(second, (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString());
        Assert.Equal(second, (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString());
    }

    /// <summary>
    /// "Another image" <b>hands out an image</b> – on a typed stage, the endpoint would thus be the hole
    /// in the anti-cheat rule: the card withholds both image <i>and</i> alt text, because the motif shows
    /// the meaning of exactly the word that is supposed to be typed. It must carry the same restriction.
    /// </summary>
    [Fact]
    public async Task AnderesBild_AufGetippterStufe_GibtNichtsHeraus()
    {
        var father = await TestApi.AdultAsync(factory);
        var setup = await ScenarioAsync(father, "reshuffle-stufe");
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", 3)]);
        var sohn = await TestApi.ChildAsync(factory, setup.ChildId, ChildPin);

        var (typedSession, typedCard) = await SessionAsync(father, sohn, setup, LetterBoxes);
        Assert.True(IsNull(typedCard, "imageUrl"), "The card itself shows no image on this stage.");

        var res = await sohn.PostAsync(ReshuffleUrl(setup, typedSession, CardIndex(typedCard)), null);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("media_not_on_card", await CodeOf(res));

        // On self-assessment - where the image serves its purpose - it stays possible.
        var (openSession, openCard) = await SessionAsync(father, sohn, setup, SelfAssess);
        (await sohn.PostAsync(ReshuffleUrl(setup, openSession, CardIndex(openCard)), null)).EnsureSuccessStatusCode();
    }

    /// <summary>
    /// The index addresses a card <b>of this session</b>. Without this boundary, an unrestricted index
    /// could be used to enumerate the motifs and descriptions of the entire exercise – including those of
    /// cards the session never delivers.
    /// </summary>
    [Fact]
    public async Task AnderesBild_NurFuerKartenDerSitzung()
    {
        var father = await TestApi.AdultAsync(factory);
        var setup = await ScenarioAsync(father, "reshuffle-index");
        var sohn = await TestApi.ChildAsync(factory, setup.ChildId, ChildPin);

        var (sessionId, _) = await SessionAsync(father, sohn, setup, SelfAssess);
        var res = await sohn.PostAsync(ReshuffleUrl(setup, sessionId, 99), null);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    /// <summary>
    /// Like every playing endpoint: a deactivated plan is closed for the student, open for the
    /// supervisor (preview/follow-up).
    /// </summary>
    [Fact]
    public async Task AnderesBild_ImStillgelegtenPlan_BleibtDemSohnVerschlossen()
    {
        var father = await TestApi.AdultAsync(factory);
        var setup = await ScenarioAsync(father, "reshuffle-plan");
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", 3)]);
        var sohn = await TestApi.ChildAsync(factory, setup.ChildId, ChildPin);

        var (sessionId, card) = await SessionAsync(father, sohn, setup, SelfAssess);
        (await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{setup.PlanId}", new { active = false }))
            .EnsureSuccessStatusCode();

        var res = await sohn.PostAsync(ReshuffleUrl(setup, sessionId, CardIndex(card)), null);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("plan_inactive", await CodeOf(res));

        (await father.PostAsync(ReshuffleUrl(setup, sessionId, CardIndex(card)), null)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetippteStufen_ZeigenKeinBild_DennEinMotivVerraetDieBedeutung()
    {
        var father = await TestApi.AdultAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-anticheat");
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", 3)]);

        // The display and self-assessment stages reveal the solution anyway - here the image helps with
        // memorizing, and that is exactly what it is for.
        foreach (var openStage in new[] { ShowBoth, SelfAssess })
            Assert.False(IsNull(await FirstCardAsync(father, setup, openStage), "imageUrl"),
                $"Stufe {openStage} sollte ein Bild tragen.");

        foreach (var typedStage in new[] { LetterBoxes, FreeText, MultipleChoice })
        {
            var card = await FirstCardAsync(father, setup, typedStage);
            Assert.True(IsNull(card, "imageUrl"), $"Stage {typedStage} must not carry an image.");
            // The alt text has to go too - "a unicorn is running" would give away the same as the image.
            Assert.True(IsNull(card, "imageAlt"), $"Stufe {typedStage} darf keinen Alt-Text tragen.");
        }
    }

    [Fact]
    public async Task ItemZuordnung_SchlaegtDieStoreZuordnung()
    {
        var father = await TestApi.AdultAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-kaskade");
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", 3)]);

        var special = await CreateAssetAsync(father, $"{setup.Marker}_override", "Nur in dieser Uebung", []);
        var link = await father.PostAsJsonAsync(
            $"/api/v1/creator/exercises/{setup.ExerciseId}/items/{setup.ItemId}/media",
            new { mediaAssetId = special });
        link.EnsureSuccessStatusCode();

        // Despite a strong interest in the unicorn, the exercise-local override wins - it is the more precise
        // statement about this very exercise.
        var card = await FirstCardAsync(father, setup, SelfAssess);
        Assert.Equal("Nur in dieser Uebung", card.GetProperty("imageAlt").GetString());
    }

    [Fact]
    public async Task OhneZuordnung_BleibtDieKarteBildlos_StattEinenNotnagelZuZeigen()
    {
        var father = await TestApi.AdultAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-leer", assetCount: 0);

        var card = await FirstCardAsync(father, setup, SelfAssess);
        Assert.True(IsNull(card, "imageUrl"));
    }

    [Fact]
    public async Task ZweiKinder_BekommenIhrJeweilsPassendesBild()
    {
        var father = await TestApi.AdultAsync(factory);
        var first = await ScenarioAsync(father, "auswahl-kind-a");
        // A second child on the same exercise: the same material, a different profile.
        var second = await ScenarioAsync(father, "auswahl-kind-b", reuse: first);

        await SetInterestsAsync(father, first.ChildId, [("Einhorn", 3)]);
        await SetInterestsAsync(father, second.ChildId, [("Superhelden", 3)]);

        var cardA = await FirstCardAsync(father, first, SelfAssess);
        var cardB = await FirstCardAsync(father, second, SelfAssess);

        Assert.Contains("unicorn", cardA.GetProperty("imageUrl").GetString());
        Assert.Contains("flash", cardB.GetProperty("imageUrl").GetString());
    }

    // ---- Scenario setup ----------------------------------------------------------------------------

    private sealed record Scenario(string Marker, int ChildId, int PlanId, int PositionId,
        int ExerciseId, int ItemId, int VocabularyId);

    /// <summary>
    /// Builds a complete, isolated scenario: its own child, one vocabulary exercise with one word, up to
    /// three representations in the store, and an active study plan with one position on it.
    /// <paramref name="reuse"/> attaches a second child to the same exercise (for the profile comparison).
    /// </summary>
    private async Task<Scenario> ScenarioAsync(HttpClient father, string marker, int assetCount = 3,
        bool includeMature = false, Scenario? reuse = null)
    {
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = marker, pin = ChildPin }));

        int exerciseId, itemId, vocabularyId;
        if (reuse is not null)
        {
            (exerciseId, itemId, vocabularyId) = (reuse.ExerciseId, reuse.ItemId, reuse.VocabularyId);
        }
        else
        {
            var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = marker }));
            var seriesId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/textbook-series",
                new { name = $"{marker}-Reihe", subjectId }));
            var seriesUnitId = await TestApi.IdAsync(await father.PostAsJsonAsync(
                $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Unit" }));
            exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
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
                        // A word of its own per scenario: the vocabulary store is find-or-create, and a shared
                        // "run" would collect the image assignments of all scenarios on the same row.
                        items = new[] { new { front = $"run-{marker}", back = $"laufen-{marker}" } },
                    },
                }));

            var items = await GetAsync(father,
                $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{exerciseId}/items");
            itemId = items[0].GetProperty("id").GetInt32();
            vocabularyId = items[0].GetProperty("vocabularyId").GetInt32();

            var assets = new List<(string Key, string Description, string[] Tags, string Rating)>
            {
                ($"{marker}_unicorn", "Ein Einhorn laeuft", ["Einhorn", "Comic"], "Everyone"),
                ($"{marker}_flash", "Flash rennt", ["Superhelden", "Comic"], "Everyone"),
                ($"{marker}_photo", "Eine joggende Person", ["Foto"], "Everyone"),
            };
            if (includeMature)
                assets.Add(($"{marker}_mature", "Nicht fuer Kinder", ["Freizuegig"], "Mature"));

            foreach (var (key, description, tags, rating) in assets.Take(includeMature ? assets.Count : assetCount))
            {
                var assetId = await CreateAssetAsync(father, key, description, tags, rating);
                (await father.PostAsJsonAsync($"/api/v1/creator/vocabulary/{vocabularyId}/media",
                    new { mediaAssetId = assetId })).EnsureSuccessStatusCode();
            }
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var planId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/study-plans", new
        {
            childId,
            title = $"{marker}-Plan",
            startDate = today.AddDays(-1).ToString("yyyy-MM-dd"),
            // The plan is meant to be running. This used to say `endDate`/`active` - fields that exist only in
            // the update DTO and that the server silently discarded on create; the runtime in truth came from
            // the default. `durationDays` is the field the contract provides for it.
            durationDays = 31,
        }));
        var positionId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions",
            // `cadence`/`pointsGoalMet` - this used to say `goalCadence`/`goalPoints`. Neither name exists in
            // the contract; the server discarded them silently, so the position never had the mandatory cadence
            // and the points this setup prescribes.
            new { exerciseId, order = 1, cadence = "Daily", goalThreshold = 1, pointsGoalMet = 5 }));

        return new Scenario(marker, childId, planId, positionId, exerciseId, itemId, vocabularyId);
    }

    /// <summary>Starts an exercise session at the desired stage and returns the first card.</summary>
    private static async Task<JsonElement> FirstCardAsync(HttpClient father, Scenario s, int stage) =>
        (await SessionAsync(father, father, s, stage)).Card;

    /// <summary>
    /// Like <see cref="FirstCardAsync"/>, but also returns the session id and lets
    /// <paramref name="player"/> play – for the endpoints that the child calls themselves (the stage is
    /// still only set by the supervisor).
    /// </summary>
    private static async Task<(int SessionId, JsonElement Card)> SessionAsync(HttpClient father, HttpClient player,
        Scenario s, int stage)
    {
        // The stage comes from the position's schedule (the server enforces it - never the client).
        var patch = await father.PatchAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{s.PlanId}/positions/{s.PositionId}", new { stage });
        patch.EnsureSuccessStatusCode();

        var start = await player.PostAsJsonAsync(
            $"/api/v1/student/study-plans/{s.PlanId}/positions/{s.PositionId}/practice-sessions",
            new { mode = "Lern" });
        start.EnsureSuccessStatusCode();
        var sessionId = await TestApi.IdAsync(start);

        var next = await GetAsync(player,
            $"/api/v1/student/study-plans/{s.PlanId}/positions/{s.PositionId}/practice-sessions/{sessionId}/next");
        return (sessionId, next.GetProperty("card"));
    }

    private static string ReshuffleUrl(Scenario s, int sessionId, int itemIndex) =>
        $"/api/v1/student/study-plans/{s.PlanId}/positions/{s.PositionId}/practice-sessions/{sessionId}" +
        $"/cards/{itemIndex}/image/reshuffle";

    private static int CardIndex(JsonElement card) => card.GetProperty("itemIndex").GetInt32();

    private static async Task SetInterestsAsync(HttpClient father, int childId, (string Label, int Weight)[] interests)
    {
        var res = await father.PutAsJsonAsync($"/api/v1/supervisor/children/{childId}/interests", new
        {
            interests = interests.Select(i => new { label = i.Label, weight = i.Weight }),
        });
        res.EnsureSuccessStatusCode();
    }

    private static async Task<int> CreateAssetAsync(HttpClient father, string key, string description,
        string[] tags, string rating = "Everyone") =>
        await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/media", new
        {
            key,
            description,
            rating,
            tags,
            variants = new[] { new { purpose = "Card", url = $"https://cdn.test/{key}.webp", width = 512, height = 512 } },
        }));

    private static bool IsNull(JsonElement obj, string property) =>
        !obj.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null;

    private static async Task<JsonElement> GetAsync(HttpClient client, string url)
    {
        var res = await client.GetAsync(url);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<string?> CodeOf(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();
}
