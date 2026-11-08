using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Position CRUD (stage 5): the father assembles a study plan from global catalog exercises –
/// creating, retrieving, playing, deleting a position (with history protection).
/// </summary>
public class PlanPositionCrudTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private static async Task<int> EmptyPlanAsync(HttpClient father, int childId = 1)
    {
        var res = await father.PostAsJsonAsync("/api/v1/supervisor/study-plans",
            new { childId, title = "Positions-Plan", durationDays = 10 });
        return await TestApi.IdAsync(res);
    }

    /// <summary>
    /// A plan created <b>inactive</b> leaves the child's running plan alone.
    /// <para>
    /// Creating a plan otherwise makes it the child's only playable one and switches every sibling off –
    /// deliberately, because "exactly one active plan" is an anti-cheat rule. That default is exactly wrong
    /// for a plan nobody has reviewed yet: the AI creator would replace the plan the child is playing
    /// tonight, and unchecked content would start booking penalty coins. Hence <c>active: false</c> on
    /// creation, and hence this case.
    /// </para>
    /// <para>
    /// Red probe: drop <c>Active = dto.Active</c> from <c>StudyPlansController.Create</c> and both the
    /// second and the third assertion fall – the new plan comes back active and takes the old one down
    /// with it.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Plan_InaktivAngelegt_LaesstDenLaufendenPlanInRuhe()
    {
        var father = await TestApi.AdultAsync(_factory);
        var laufend = await EmptyPlanAsync(father);

        var neu = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/study-plans",
            new { childId = 1, title = "Ungeprüfter KI-Plan", durationDays = 10, active = false }));

        // Without this line the case would also pass for a server that ignores `active` and simply hands
        // back what it was sent - the state has to be read back.
        var angelegt = await father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/study-plans/{neu}");
        JsonAssert.False(angelegt, "active");

        var vorheriger = await father.GetFromJsonAsync<JsonElement>($"/api/v1/supervisor/study-plans/{laufend}");
        JsonAssert.True(vorheriger, "active");
    }

    [Fact]
    public async Task Position_Anlegen_Abrufen_Spielen()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, k1) = await TestApi.CreateStoreVocabAsync(father, "spring", "Frühling");
        var (_, k2) = await TestApi.CreateStoreVocabAsync(father, "autumn", "Herbst");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, k1, k2);
        var planId = await EmptyPlanAsync(father);

        // The supervisor creates the position on the global exercise (Leitner, typed stage).
        var posId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, useLeitner = true, stage = (int)TestStage.FreeText, cadence = "Daily" }));

        var list = await father.GetFromJsonAsync<List<JsonElement>>($"/api/v1/supervisor/study-plans/{planId}/positions");
        Assert.Single(list!);
        Assert.Equal(exerciseId, list![0].GetProperty("exerciseId").GetInt32());

        // The child plays the position → the content comes from the referenced exercise/store. Known words, so the
        // card is asked rather than introduced (B-419).
        TestApi.MarkIntroduced(_factory, posId);
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{posId}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        var outcome = await (await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review",
            new { itemIndex = 0, givenAnswer = "Frühling" })).Content.ReadFromJsonAsync<JsonElement>();
        JsonAssert.True(outcome, "wasCorrect");
    }

    [Fact]
    public async Task Position_Loeschen_OhneVerlauf204_MitVerlauf409()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "winter", "Winter");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var planId = await EmptyPlanAsync(father);

        // Unplayed → deletable.
        var posId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId, useLeitner = true, stage = (int)TestStage.FreeText }));
        var del = await father.DeleteAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{posId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        // A new position, this time played → protected (409, no loss of the learning history).
        var posId2 = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId, useLeitner = true, stage = (int)TestStage.FreeText }));
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{posId2}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review", new { itemIndex = 0, givenAnswer = "Winter" });

        var del2 = await father.DeleteAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{posId2}");
        Assert.Equal(HttpStatusCode.Conflict, del2.StatusCode);
    }

    [Fact]
    public async Task Position_UnbekannteUebung_Liefert400()
    {
        var father = await TestApi.AdultAsync(_factory);
        var planId = await EmptyPlanAsync(father);
        var res = await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId = 999999 });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Position_UebernimmtExerciseDefaults_UndOrderStrategyIstApiSichtbar()
    {
        var father = await TestApi.AdultAsync(_factory);
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = "Defaults-Position" }));
        var seriesId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Defaults-Reihe"), subjectId }));
        var seriesUnitId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units", new { label = "Unit" }));
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary", new
            {
                title = "Nur zwei Karten",
                orderIndex = 1,
                rewardPoints = 10,
                defaultStage = (int)TestStage.FreeText,
                defaultItemCount = 2,
                defaultUseLeitner = true,
                defaultRequireTypedTest = true,
                config = new
                {
                    direction = "front-to-back",
                    sourceLang = "en",
                    targetLang = "de",
                    items = new[]
                    {
                        new { front = "a", back = "1" },
                        new { front = "b", back = "2" },
                        new { front = "c", back = "3" },
                    },
                },
            }));
        var planId = await EmptyPlanAsync(father);

        var created = await (await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions", new
        {
            exerciseId,
            orderStrategy = "Serial",
        })).Content.ReadFromJsonAsync<JsonElement>();
        var positionId = created.GetProperty("id").GetInt32();

        Assert.Equal(JsonValueKind.Null, created.GetProperty("stage").ValueKind);
        Assert.Equal(JsonValueKind.Null, created.GetProperty("itemCount").ValueKind);
        Assert.Equal("Serial", created.GetProperty("orderStrategy").GetString());
        JsonAssert.True(created, "useLeitner");
        JsonAssert.True(created, "requireTypedTest");

        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = TestApi.PracticeBase(planId, positionId);
        var session = await (await child.PostAsJsonAsync(baseUrl, new { mode = "Info" })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, session.GetProperty("total").GetInt32());

        var cards = await child.GetFromJsonAsync<JsonElement>($"{baseUrl}/{session.GetProperty("id").GetInt32()}/cards");
        Assert.Equal(new[] { 0, 1 }, cards.EnumerateArray().Select(card => card.GetProperty("itemIndex").GetInt32()).ToArray());

        var patched = await (await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}", new
        {
            orderStrategy = "Random",
        })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Random", patched.GetProperty("orderStrategy").GetString());
    }

    [Fact]
    public async Task Plan_Loeschen_EntferntPlanMitGespielterPosition()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "summer", "Sommer");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var planId = await EmptyPlanAsync(father);
        var posId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId, useLeitner = true, stage = (int)TestStage.FreeText }));

        // Play the position → a session/progress exists (blocks the position DELETE, but not the plan DELETE).
        var child = await TestApi.ChildAsync(_factory);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{posId}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        await child.PostAsJsonAsync($"{baseUrl}/{sessionId}/review", new { itemIndex = 0, givenAnswer = "Sommer" });

        // The whole plan can be deleted (cascading positions/progress/sessions).
        var del = await father.DeleteAsync($"/api/v1/supervisor/study-plans/{planId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        // Afterwards the plan (and thus the position list) is gone → 404 through the ownership filter.
        var after = await father.GetAsync($"/api/v1/supervisor/study-plans/{planId}/positions");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);

        // The referenced catalog exercise is preserved.
        Assert.Equal(HttpStatusCode.OK, (await father.GetAsync($"/api/v1/creator/exercises/{exerciseId}")).StatusCode);
    }

    /*
     * `goalThreshold` ist eine Prozent-Bestehensgrenze. Wer sie mit einer Trefferzahl verwechselt, soll ein
     * 400 sehen statt einer Pflicht, die lautlos jeden Versuch durchwinkt – 0 und 120 sind beides keine
     * Prozentangaben, und „Standard" sagt man mit dem Auslassen des Feldes.
     */
    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-5)]
    public async Task Position_SchwelleAusserhalbProzent_WirdAbgewiesen(int goalThreshold)
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var planId = await EmptyPlanAsync(father);
        var url = $"/api/v1/supervisor/study-plans/{planId}/positions";

        var create = await father.PostAsJsonAsync(url, new { exerciseId, cadence = "Daily", goalThreshold });
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        Assert.Equal("validation_error",
            (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // Not afterwards either: otherwise PATCH would let in exactly what POST rejects.
        var posId = await TestApi.IdAsync(await father.PostAsJsonAsync(url, new { exerciseId, cadence = "Daily" }));
        var patch = await father.PatchAsJsonAsync($"{url}/{posId}", new { goalThreshold });
        Assert.Equal(HttpStatusCode.BadRequest, patch.StatusCode);

        // The valid bound goes through and comes back.
        var ok = await father.PatchAsJsonAsync($"{url}/{posId}", new { goalThreshold = 90 });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(90, (await ok.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("goalThreshold").GetInt32());
    }

    /// <summary>Creates an empty Birkenbihl exercise (a type with no typed stage at all) and returns its id.</summary>
    private static async Task<int> CreateBirkenbihlExerciseAsync(HttpClient father)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Birkenbihl-Position-Test") }));
        var seriesId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Birkenbihl-Reihe"), subjectId }));
        var unitId = await TestApi.IdAsync(await father.PostAsJsonAsync($"/api/v1/creator/textbook-series/{seriesId}/units",
            new { label = "Lektion 1", orderIndex = 1 }));
        return await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/birkenbihl", new
            {
                title = "Birkenbihl",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { learningLang = "en", nativeLang = "de" },
            }));
    }

    /*
     * RequireTypedTest gates PositionPracticeController's `scored` on `typed || !RequireTypedTest`. Birkenbihl's
     * IsTypedStage is constant false (it learns by reading, never by typing) - so a position with
     * requireTypedTest: true on this type would never score, silently, and the father would only notice after
     * weeks of an unmet goal (B-93).
     */
    [Fact]
    public async Task RequireTypedTest_AufEinemTypOhneGetippteStufe_WirdAbgewiesen()
    {
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await CreateBirkenbihlExerciseAsync(father);
        var planId = await EmptyPlanAsync(father);
        var url = $"/api/v1/supervisor/study-plans/{planId}/positions";

        var create = await father.PostAsJsonAsync(url, new { exerciseId, requireTypedTest = true });
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        Assert.Equal("validation_error",
            (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // Not afterwards either: otherwise PATCH would let in exactly what POST rejects.
        var posId = await TestApi.IdAsync(await father.PostAsJsonAsync(url, new { exerciseId }));
        var patch = await father.PatchAsJsonAsync($"{url}/{posId}", new { requireTypedTest = true });
        Assert.Equal(HttpStatusCode.BadRequest, patch.StatusCode);

        // A type that DOES have a typed stage (vocabulary) is unaffected - the same setting still works there.
        var vocabId = await TestApi.CreateVocabExerciseAsync(father);
        var vocabPos = await father.PostAsJsonAsync(url, new { exerciseId = vocabId, requireTypedTest = true });
        Assert.Equal(HttpStatusCode.Created, vocabPos.StatusCode);
    }

    [Fact]
    public async Task Einzelne_Position_Wird_Gelesen_Eine_Fremde_Nicht()
    {
        // The single view of the position (a C3 coverage gap): only the list was covered so far.
        var father = await TestApi.AdultAsync(_factory);
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var planId = await EmptyPlanAsync(father);
        var url = $"/api/v1/supervisor/study-plans/{planId}/positions";
        var posId = await TestApi.IdAsync(await father.PostAsJsonAsync(url, new { exerciseId, stage = 2 }));

        var position = await (await father.GetAsync($"{url}/{posId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(posId, position.GetProperty("id").GetInt32());
        Assert.Equal(exerciseId, position.GetProperty("exerciseId").GetInt32());
        Assert.Equal(2, position.GetProperty("stage").GetInt32());

        // A position belonging to a *different* plan is not findable under this plan - otherwise the position
        // of someone else's plan could be read through a plan of your own.
        var andererPlan = await EmptyPlanAsync(father);
        Assert.Equal(HttpStatusCode.NotFound,
            (await father.GetAsync($"/api/v1/supervisor/study-plans/{andererPlan}/positions/{posId}")).StatusCode);
    }

    /// <summary>
    /// B-45: the creator's points recommendation reaches the position. Before this, <c>RewardPoints</c> was a
    /// mandatory field no scoring path ever read - a creator entering 15 changed nothing.
    /// </summary>
    [Fact]
    public async Task Position_Erbt_Punkteempfehlung_Der_Uebung()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "meadow", "Wiese");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, 15, key);
        var planId = await EmptyPlanAsync(father);

        // No pointsGoalMet in the request: the recommendation has to fill the gap.
        var geerbt = await (await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId, stage = 2 }))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(15, geerbt.GetProperty("pointsGoalMet").GetInt32());
    }

    /// <summary>
    /// B-45, AC 2: an explicit value keeps winning - the "position override first" order must not shift.
    /// <para>
    /// Its own <c>Fact</c>, not an assertion after the inheritance case: a test stops at its first failure, so
    /// a regression in the ORDER would have been invisible for as long as inheritance itself was broken.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Eigener_Punktewert_Schlaegt_Die_Empfehlung()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "pasture", "Weide");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, 15, key);
        var planId = await EmptyPlanAsync(father);

        var eigen = await (await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, stage = 2, pointsGoalMet = 7 })).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(7, eigen.GetProperty("pointsGoalMet").GetInt32());
    }

    /// <summary>
    /// B-45: an implausibly large recommendation is not inherited. Inheriting opened this hole - the catalog is
    /// shared, <c>RewardPoints</c> has no bound, and the target reward is paid out as coins; before, the
    /// ceiling was the hard 20. An explicit value stays unbounded: that is the supervisor's own decision.
    /// </summary>
    [Fact]
    public async Task Unplausibel_Hohe_Empfehlung_Wird_Nicht_Geerbt()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "windmill", "Windmühle");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, 100_000, key);
        var planId = await EmptyPlanAsync(father);
        var url = $"/api/v1/supervisor/study-plans/{planId}/positions";

        var geerbt = await (await father.PostAsJsonAsync(url, new { exerciseId, stage = 2 }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(20, geerbt.GetProperty("pointsGoalMet").GetInt32());

        // Deliberately stated, deliberately unbounded - the bound guards silent inheritance, not the father.
        var gewollt = await (await father.PostAsJsonAsync(url,
            new { exerciseId, stage = 2, pointsGoalMet = 100_000 })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(100_000, gewollt.GetProperty("pointsGoalMet").GetInt32());
    }

    /// <summary>
    /// B-192: the catalog search publishes the figure a position would <b>actually</b> inherit, so the
    /// supervisor form can pre-fill with what will apply instead of with the raw recommendation.
    /// <para>
    /// Why this matters and why the earlier test did not catch it: the bound only ever applied when
    /// <c>PointsGoalMet</c> was omitted, and the father web never omits it - it sends a number every time
    /// (<c>PositionSettings.pointsGoalMet</c> is typed <c>number</c>). So the guard that
    /// <c>Unplausibel_Hohe_Empfehlung_Wird_Nicht_Geerbt</c> pins held for the API path and for no one
    /// else. Publishing the effective figure closes that by making the number the father SEES the number
    /// that applies.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Katalogtreffer_Nennt_Den_Wirksamen_Punktevorschlag()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "harbour", "Hafen");

        // Zwei Uebungen, die sich nur in der Empfehlung unterscheiden - die eine plausibel, die andere
        // jenseits jeder Grenze. Ohne BEIDE Faelle koennte der Test nicht zwischen "durchgereicht" und
        // "wirksam gerechnet" unterscheiden.
        var brauchbar = await TestApi.CreateVocabRefExerciseAsync(father, 30, key);
        var unsinnig = await TestApi.CreateVocabRefExerciseAsync(father, 100_000, key);

        var zeilen = await KatalogZeilenAsync(father);

        var a = zeilen.Single(z => z.GetProperty("id").GetInt32() == brauchbar);
        Assert.Equal(30, a.GetProperty("rewardPoints").GetInt32());
        Assert.Equal(30, a.GetProperty("suggestedGoalPoints").GetInt32());

        var b = zeilen.Single(z => z.GetProperty("id").GetInt32() == unsinnig);
        // Der rohe Wert bleibt lesbar - wer die Uebung bearbeitet, soll sehen, was dort steht.
        Assert.Equal(100_000, b.GetProperty("rewardPoints").GetInt32());
        // Der wirksame nicht: genau das haette das Formular vorbelegt, und genau das war der Defekt.
        Assert.Equal(20, b.GetProperty("suggestedGoalPoints").GetInt32());

        // Der negative Fall zog mit B-192 hierher um. Er war bis dahin NUR im Frontend gepinnt
        // (`PlanPositions.test.ts`), und dort entscheidet das Formular jetzt nichts mehr - ohne diesen
        // Fall waere die Abdeckung beim Umzug der Regel verloren gegangen. `0` ist in JS ohnehin falsy,
        // `-5` nicht: das ist der einzige Fall, fuer den die untere Grenze ueberhaupt traegt.
        var negativ = await TestApi.CreateVocabRefExerciseAsync(father, -5, key);
        var c = (await KatalogZeilenAsync(father))
            .Single(z => z.GetProperty("id").GetInt32() == negativ);
        Assert.Equal(-5, c.GetProperty("rewardPoints").GetInt32());
        Assert.Equal(20, c.GetProperty("suggestedGoalPoints").GetInt32());
    }

    /// <summary>
    /// B-192: was der Katalog als wirksamen Vorschlag nennt, ist auch das, was die angelegte Position
    /// traegt. Ohne diese Zusicherung koennten beide Seiten je fuer sich richtig sein und trotzdem
    /// auseinanderlaufen - der Katalog rechnet, der Controller rechnet, und niemand vergleicht.
    /// </summary>
    [Fact]
    public async Task Wirksamer_Vorschlag_Und_Angelegte_Position_Stimmen_Ueberein()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "lighthouse", "Leuchtturm");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, 100_000, key);
        var planId = await EmptyPlanAsync(father);


        var vorschlag = (await KatalogZeilenAsync(father))
            .Single(z => z.GetProperty("id").GetInt32() == exerciseId)
            .GetProperty("suggestedGoalPoints").GetInt32();

        var position = await (await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId, stage = 2 }))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(vorschlag, position.GetProperty("pointsGoalMet").GetInt32());

        // Und derselbe Abgleich mit einer PLAUSIBLEN Empfehlung. Ohne dieses Paar faehrt der Test nur den
        // Rueckfall-Zweig - dort sind beide Seiten 20, und 20 ist zugleich der Entity-Vorgabewert. Eine
        // Fassung, die die Vererbung im Create-Pfad GANZ entfernt (`?? 20`), bliebe damit gruen; genau das
        // hat der `pugling-reviewer` per Defekt-Injektion gemessen. Erst hier loest der Test seine eigene
        // Doku ein: "beide Seiten je fuer sich richtig und trotzdem auseinander".
        var (_, key2) = await TestApi.CreateStoreVocabAsync(father, "anchor", "Anker");
        var plausibel = await TestApi.CreateVocabRefExerciseAsync(father, 35, key2);
        var vorschlag2 = (await KatalogZeilenAsync(father))
            .Single(z => z.GetProperty("id").GetInt32() == plausibel)
            .GetProperty("suggestedGoalPoints").GetInt32();
        Assert.Equal(35, vorschlag2);

        var position2 = await (await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId = plausibel, stage = 2 })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(vorschlag2, position2.GetProperty("pointsGoalMet").GetInt32());
    }

    /// <summary>
    /// The catalog rows of one page - and proof that the page really holds everything.
    /// </summary>
    /// <remarks>
    /// The bare <c>.Single(...)</c> the callers use silently bets that no exercise fell off the page; the
    /// class shares one database and grows with every test. Without the count assertion a grown class
    /// fails as "sequence contains no matching element", which says nothing about the cause.
    /// </remarks>
    private static async Task<List<JsonElement>> KatalogZeilenAsync(HttpClient father)
    {
        const int take = 200;
        var antwort = await father.GetAsync($"/api/v1/creator/exercises?take={take}");
        var gesamt = int.Parse(antwort.Headers.GetValues("X-Total-Count").Single());
        Assert.True(gesamt <= take,
            $"The catalog holds {gesamt} exercises, the page takes {take} - raise it or filter.");
        return (await antwort.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
    }

    /// <summary>
    /// B-45, decision 4: a recommendation of 0 must not silently drag every new position down to zero target
    /// points. <c>RewardPoints</c> carries no server-side lower bound, so an old exercise left at 0 would
    /// otherwise be a regression against the previous hard default of 20.
    /// </summary>
    [Fact]
    public async Task Punkteempfehlung_Null_Laesst_Den_Vorgabewert_Stehen()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "harvest", "Ernte");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, 0, key);
        var planId = await EmptyPlanAsync(father);

        var position = await (await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId, stage = 2 }))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(20, position.GetProperty("pointsGoalMet").GetInt32());
    }

    /// <summary>
    /// B-45, decision 5: the recommendation is copied ONCE, like <c>SuggestedBonus</c>. A creator raising the
    /// recommendation afterwards must not move study plans that are already running.
    /// </summary>
    [Fact]
    public async Task Spaetere_Aenderung_Der_Empfehlung_Verschiebt_Bestehende_Positionen_Nicht()
    {
        var father = await TestApi.AdultAsync(_factory);
        var (vocabId, key) = await TestApi.CreateStoreVocabAsync(father, "orchard", "Obstgarten");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, 15, key);
        var planId = await EmptyPlanAsync(father);
        var url = $"/api/v1/supervisor/study-plans/{planId}/positions";
        var posId = await TestApi.IdAsync(await father.PostAsJsonAsync(url, new { exerciseId, stage = 2 }));

        // The creator raises the recommendation. The full replacement (PUT) has to carry the config along,
        // otherwise it clears it - the exercise would then be unplayable and unrelated to this assertion.
        var detail = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}");
        var seriesId = detail.GetProperty("seriesId").GetInt32();
        var unitId = detail.GetProperty("seriesUnitId").GetInt32();
        var geaendert = await father.PutAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary/{exerciseId}", new
            {
                title = "Vokabeln (Store)",
                orderIndex = 1,
                rewardPoints = 99,
                config = new { direction = "front-to-back", refs = new[] { new { vocabularyId = vocabId } } },
            });
        Assert.True(geaendert.IsSuccessStatusCode, await geaendert.Content.ReadAsStringAsync());
        // The precondition, asserted rather than staged: without this the test would stay green even if the
        // creator's change never landed - it would then guard against a change that did not happen.
        var neuerStand = await geaendert.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(99, neuerStand.GetProperty("rewardPoints").GetInt32());

        var position = await father.GetFromJsonAsync<JsonElement>($"{url}/{posId}");
        Assert.Equal(15, position.GetProperty("pointsGoalMet").GetInt32());
    }
}
