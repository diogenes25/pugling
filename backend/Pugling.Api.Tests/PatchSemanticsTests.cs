using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Pugling.Api.Tests;

/// <summary>
/// The PATCH semantics guard from docs/codequalitaet-gates-plan.md (C2) - for <b>every</b>
/// <c>Update…Dto</c> of the contract, not just a handful of examples.
/// <para>
/// Two rules, mechanically pinned down:
/// </para>
/// <list type="number">
/// <item><b><c>null</c> means "not specified"</b> - set a field, then follow up with <c>null</c>:
/// the value stays. If this rule breaks, any form that leaves a field blank silently overwrites the
/// existing data.</item>
/// <item><b>The <c>Clear…</c> switch clears - and wins.</b> If a UI sends both a value <em>and</em>
/// the switch (the "- no value -" of a select field next to the old value), "clear" must win. Otherwise
/// it reports "Saved." and the old value stays put.</item>
/// </list>
/// <para>
/// Completeness is itself checked: <see cref="Jedes_UpdateDto_Ist_Belegt"/> and
/// <see cref="Jeder_ClearSchalter_Ist_Belegt"/> compare the case table reflectively against the contract. A
/// new <c>Update…Dto</c> or a new switch turns this test red until a case is added - the
/// rule stays covered even if nobody thinks of this file.
/// </para>
/// <para>
/// Domain-specific individual cases (e.g. "the unit disappears along with the series") still live in
/// <see cref="PatchClearFieldTests"/>; this file is about the surface as a whole.
/// </para>
/// </summary>
public class PatchSemanticsTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // ─────────────────────────────────────────────────────────────────── The surface

    /// <summary>A <c>Clear…</c> switch and the field it clears (the mapping cannot be inferred:
    /// <c>clearSubject</c> clears <c>subjectId</c>, <c>clearUnit</c> clears <c>currentUnitId</c>).</summary>
    private sealed record Schalter(string Name, string Feld);

    /// <summary>Where the patch goes - and where to read the state back if the PATCH response doesn't show the field.</summary>
    private sealed record Ziel(HttpClient Client, string PatchUrl, string? LeseUrl = null);

    /// <summary>A patchable resource: how to create it, the field for the round trip, and the switches.</summary>
    private sealed record Fall(
        Type UpdateDto,
        string Feld,
        JsonNode Wert,
        Func<PuglingWebAppFactory, Task<Ziel>> AnlegenAsync,
        Schalter[] Schalter);

    /// <summary>
    /// Update DTOs without a round trip - <b>not</b> a catch-all, every entry carries its reason.
    /// </summary>
    private static readonly Dictionary<string, string> Ausnahmen = new(StringComparer.Ordinal)
    {
        // It has no optional field: `UpdateMediaLinkDto(int Weight)` requires the weight. "Not specified" cannot
        // be expressed in it at all, so there is no null semantics to check.
        ["UpdateMediaLinkDto"] = "the only field (Weight) is required - null cannot be expressed",
    };

    private static Fall[] Faelle =>
    [
        new(typeof(UpdateSubjectDto), "name", "Fach neu", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync("/api/v1/creator/subjects", new { name = Eindeutig("Fach") }));
            return new Ziel(c, $"/api/v1/creator/subjects/{id}");
        }, []),

        new(typeof(UpdateChapterDto), "name", "Kapitel neu", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var subject = await TestApi.IdAsync(await c.PostAsJsonAsync("/api/v1/creator/subjects", new { name = Eindeutig("Fach") }));
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync($"/api/v1/creator/subjects/{subject}/chapters",
                new { name = "Kapitel 1", orderIndex = 1 }));
            return new Ziel(c, $"/api/v1/creator/subjects/{subject}/chapters/{id}");
        }, []),

        new(typeof(UpdateCategoryDto), "name", "Kategorie neu", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var subject = await TestApi.IdAsync(await c.PostAsJsonAsync("/api/v1/creator/subjects", new { name = Eindeutig("Fach") }));
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync($"/api/v1/creator/subjects/{subject}/categories",
                new { name = "Grammatik" }));
            return new Ziel(c, $"/api/v1/creator/subjects/{subject}/categories/{id}");
        }, []),

        new(typeof(UpdateClozeDto), "title", "Lückentext neu", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync("/api/v1/creator/cloze-texts", new
            {
                key = Eindeutig("cz"),
                title = "Begrüßungen",
                sourceLanguage = "en",
                targetLanguage = "de",
                text = "Good {{1}}!",
                gaps = new[] { new { index = 1, answer = "morning" } },
                translation = "Guten Morgen!",
                wordBank = new[] { "morning", "evening" },
            }));
            return new Ziel(c, $"/api/v1/creator/cloze-texts/{id}");
        }, [new("clearTranslation", "translation"), new("clearWordBank", "wordBank")]),

        new(typeof(UpdateInterestTagDto), "label", "Interesse neu", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync("/api/v1/creator/interest-tags",
                new { label = Eindeutig("Fußball") }));
            return new Ziel(c, $"/api/v1/creator/interest-tags/{id}");
        }, []),

        new(typeof(UpdateTagDto), "name", "Tag neu", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var childId = await NeuesKindAsync(c);
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync("/api/v1/creator/tags",
                new { childId, name = Eindeutig("Unit"), color = "#abc" }));
            return new Ziel(c, $"/api/v1/creator/tags/{id}");
        }, []),

        new(typeof(UpdateVocabTagDto), "name", "Vokabel-Tag neu", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync("/api/v1/creator/vocabulary/tags",
                new { name = Eindeutig("Thema"), color = "#def" }));
            return new Ziel(c, $"/api/v1/creator/vocabulary/tags/{id}");
        }, []),

        new(typeof(UpdateVocabularyDto), "translation", "die Kuh", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            // With alternatives from the start - `clearTranslationAlternatives` proves nothing on an entry
            // that has none.
            var (id, _) = await TestApi.CreateStoreVocabAsync(c, Eindeutig("cow"), "das Rind",
                translationAlternatives: ["die Kuh"]);
            return new Ziel(c, $"/api/v1/creator/vocabulary/{id}");
        }, [new("clearTranslationAlternatives", "translationAlternatives")]),

        new(typeof(UpdateTextbookSeriesDto), "name", "Reihe neu", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var id = await NeueReiheAsync(c);
            return new Ziel(c, $"/api/v1/creator/textbook-series/{id}");
        }, []),

        new(typeof(UpdateSeriesUnitDto), "label", "Unit neu", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var series = await NeueReiheAsync(c);
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync(
                $"/api/v1/creator/textbook-series/{series}/units", new { label = "Unit 1", grade = 5 }));
            return new Ziel(c, $"/api/v1/creator/textbook-series/{series}/units/{id}");
        }, []),

        new(typeof(UpdateCreatorProfileDto), "name", "Frau Schmidt", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var series = await NeueReiheAsync(c);
            var subject = await TestApi.IdAsync(await c.PostAsJsonAsync("/api/v1/creator/subjects", new { name = Eindeutig("Fach") }));
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync("/api/v1/creator/profiles", new
            {
                // The name is unique per creator - two cases of this class would otherwise create the same one.
                name = Eindeutig("Frau"),
                subjectId = subject,
                subjectName = "Englisch",
                gradeMin = 5,
                gradeMax = 8,
                seriesId = series,
                schoolTypes = "Gymnasium",
            }));
            return new Ziel(c, $"/api/v1/creator/profiles/{id}");
        }, [new("clearSubject", "subjectId"), new("clearSeries", "seriesId"),
            new("clearGradeMin", "gradeMin"), new("clearGradeMax", "gradeMax")]),

        new(typeof(UpdateMediaAssetDto), "description", "Ein Pferd auf der Weide", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var id = await NeuesMotivAsync(c);
            return new Ziel(c, $"/api/v1/creator/media/{id}");
        }, []),

        new(typeof(UpdateMediaVariantDto), "url", "https://example.test/neu.webp", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var asset = await NeuesMotivAsync(c);
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync($"/api/v1/creator/media/{asset}/variants",
                new { purpose = "Card", url = "https://example.test/alt.webp", width = 400, height = 300 }));
            return new Ziel(c, $"/api/v1/creator/media/{asset}/variants/{id}");
        }, []),

        new(typeof(UpdateAdultDto), "name", "Papa umbenannt", async f =>
        {
            // An adult of **our own**: `Update` allows the own record only, and renaming the seeded father
            // would pull the ground from under the other cases of this class.
            var (client, id) = await NeuerErwachsenerAsync(f);
            return new Ziel(client, $"/api/v1/supervisor/adults/{id}");
        }, []),

        new(typeof(UpdateMyAccountDto), "name", "Selbst umbenannt", async f =>
        {
            // A unique address: `Account.Email` carries a unique index, and both theories create one.
            var (client, id) = await NeuerErwachsenerAsync(f, email: $"{Eindeutig("papa")}@example.test");
            // `MeResponse` does not carry the e-mail - so for `clearEmail` the state has to come from the adult
            // record.
            return new Ziel(client, "/api/v1/auth/me", $"/api/v1/supervisor/adults/{id}");
        }, [new("clearEmail", "email")]),

        new(typeof(UpdateChildDto), "name", "Kind neu", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync("/api/v1/supervisor/children",
                new { name = Eindeutig("Kind"), birthYear = 2013, grade = 6, pin = "1111" }));
            return new Ziel(c, $"/api/v1/supervisor/children/{id}");
        }, [new("clearBirthYear", "birthYear"), new("clearGrade", "grade")]),

        new(typeof(UpdateTextbookDto), "title", "Access 7", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var childId = await NeuesKindAsync(c);
            var series = await NeueReiheAsync(c);
            var unit = await TestApi.IdAsync(await c.PostAsJsonAsync(
                $"/api/v1/creator/textbook-series/{series}/units", new { label = "Unit 1", grade = 5 }));
            var subject = await TestApi.IdAsync(await c.PostAsJsonAsync("/api/v1/creator/subjects", new { name = Eindeutig("Fach") }));
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/textbooks",
                new { title = "Access 6", subjectId = subject, subjectName = "Englisch", grade = 6, seriesId = series, currentUnitId = unit }));
            return new Ziel(c, $"/api/v1/supervisor/children/{childId}/textbooks/{id}");
        }, [new("clearSeries", "seriesId"), new("clearUnit", "currentUnitId"),
            new("clearSubject", "subjectId"), new("clearGrade", "grade")]),

        new(typeof(UpdateMissionDto), "title", "Mission neu", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var childId = await NeuesKindAsync(c);
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/missions",
                new { title = "Zehn Wörter", metric = "NewWords", target = 10, period = "Daily", rewardPoints = 5 }));
            return new Ziel(c, $"/api/v1/supervisor/children/{childId}/missions/{id}");
        }, []),

        new(typeof(UpdateAchievementDto), "title", "Auszeichnung neu", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var childId = await NeuesKindAsync(c);
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/achievements",
                new { title = "Hundert Wörter", metric = "NewWords", threshold = 100, rewardPoints = 50 }));
            return new Ziel(c, $"/api/v1/supervisor/children/{childId}/achievements/{id}");
        }, []),

        new(typeof(UpdateClassTestDto), "title", "Klassenarbeit neu", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var childId = await NeuesKindAsync(c);
            // `Create` returns `KlassenarbeitDetail` - the id sits one level deeper than usual.
            var angelegt = await c.PostAsJsonAsync("/api/v1/supervisor/class-tests",
                new { childId, title = "Vokabeltest", scheduledDate = "2026-09-01", grade = 2.0m });
            angelegt.EnsureSuccessStatusCode();
            var id = (await angelegt.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("klassenarbeit").GetProperty("id").GetInt32();
            return new Ziel(c, $"/api/v1/supervisor/class-tests/{id}");
        }, [new("clearGrade", "grade")]),

        new(typeof(UpdatePlanDto), "title", "Plan neu", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var childId = await NeuesKindAsync(c);
            var id = await TestApi.CreateEmptyPlanAsync(c, childId);
            return new Ziel(c, $"/api/v1/supervisor/study-plans/{id}");
        }, []),

        new(typeof(UpdatePositionDto), "stage", 3, async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var childId = await NeuesKindAsync(c);
            var planId = await TestApi.CreateEmptyPlanAsync(c, childId);
            var exerciseId = await TestApi.CreateVocabExerciseAsync(c);
            // With a time slot from the start - `clearTimeSlots` proves nothing on a position that has none.
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync(
                $"/api/v1/supervisor/study-plans/{planId}/positions", new
                {
                    exerciseId,
                    stage = 1,
                    timeSlots = new[] { new { name = "Hausaufgaben", start = "13:00", end = "15:00", multiplier = 2.0 } },
                }));
            return new Ziel(c, $"/api/v1/supervisor/study-plans/{planId}/positions/{id}");
        }, [new("clearTimeSlots", "timeSlots")]),

        new(typeof(UpdateShopArticleDto), "title", "Artikel neu", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync("/api/v1/supervisor/shop/articles",
                new { articleNumber = Eindeutig("A"), title = "Eis", unitType = "Stueck", actionType = "Suessigkeit" }));
            return new Ziel(c, $"/api/v1/supervisor/shop/articles/{id}");
        }, []),

        new(typeof(UpdateShopListingDto), "title", "Angebot neu", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var article = await TestApi.IdAsync(await c.PostAsJsonAsync("/api/v1/supervisor/shop/articles",
                new { articleNumber = Eindeutig("A"), title = "Eis", unitType = "Stueck", actionType = "Suessigkeit" }));
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync($"/api/v1/supervisor/shop/articles/{article}/listings",
                new { title = "Ein Eis", coinPrice = 10, gemPrice = 0, unitsPerPurchase = 1, currentStock = 5, maxStock = 5 }));
            return new Ziel(c, $"/api/v1/supervisor/shop/articles/{article}/listings/{id}");
        }, []),

        new(typeof(UpdateObjectiveRequest), "title", "Objective neu", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var (childId, _, objectiveId) = await NeuesObjectiveAsync(c);
            return new Ziel(c, $"/api/v1/supervisor/children/{childId}/objectives/{objectiveId}");
        }, []),

        new(typeof(UpdateKeyResultRequest), "title", "Etappe neu", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var (childId, keyResultId, objectiveId) = await NeuesObjectiveAsync(c);
            return new Ziel(c, $"/api/v1/supervisor/children/{childId}/objectives/{objectiveId}/key-results/{keyResultId}");
        }, []),

        new(typeof(UpdateRemarkDto), "text", "Text geändert", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var childId = await NeuesKindAsync(c);
            var exerciseId = await TestApi.CreateVocabExerciseAsync(c);
            var planId = await TestApi.CreateEmptyPlanAsync(c, childId);
            var positionId = await TestApi.IdAsync(await c.PostAsJsonAsync(
                $"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId }));
            var parent = await TestApi.IdAsync(await c.PostAsJsonAsync("/api/v1/remarks", new { text = "Vorgänger" }));
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync("/api/v1/remarks", new
            {
                text = "Bild fehlt auf der Karte",
                parentRemarkId = parent,
                context = new { route = "/sohn/lernen", appArea = "sohn", childId, exerciseId, studyPlanId = planId, planPositionId = positionId },
            }));
            // Only the skill sets the answer - for `clearAnswer` it has to be there beforehand.
            (await c.PatchAsJsonAsync($"/api/v1/remarks/{id}", new { answer = "Kommt aus dem MediaSelector.", answeredBy = "Claude" }))
                .EnsureSuccessStatusCode();
            return new Ziel(c, $"/api/v1/remarks/{id}");
        }, [new("clearAnswer", "answer"), new("clearChild", "context.childId"),
            new("clearExercise", "context.exerciseId"), new("clearStudyPlan", "context.studyPlanId"),
            new("clearPlanPosition", "context.planPositionId"), new("clearParent", "parentRemarkId")]),
    ];

    // ─────────────────────────────────────────────────────────────────── Rule 1: null changes nothing

    public static TheoryData<string> FallNamen => [.. Faelle.Select(f => f.UpdateDto.Name)];

    [Theory]
    [MemberData(nameof(FallNamen))]
    public async Task Null_Laesst_Den_Wert_Stehen(string dto)
    {
        var fall = Faelle.Single(f => f.UpdateDto.Name == dto);
        var ziel = await fall.AnlegenAsync(factory);

        // Setting has to work - otherwise the round trip afterwards would only check that nothing happens.
        var gesetzt = await PatchAsync(ziel, new JsonObject { [fall.Feld] = fall.Wert.DeepClone() });
        Assert.Equal(fall.Wert.ToJsonString(), Lesen(gesetzt, fall.Feld)?.ToJsonString());

        // And now the rule: `null` means "not specified", not "clear".
        var danach = await PatchAsync(ziel, new JsonObject { [fall.Feld] = null });
        Assert.Equal(fall.Wert.ToJsonString(), Lesen(danach, fall.Feld)?.ToJsonString());
    }

    // ─────────────────────────────────────────────────────────────────── Rule 2: clear empties and wins

    public static TheoryData<string, string> SchalterNamen =>
        [.. Faelle.SelectMany(f => f.Schalter.Select(s => (f.UpdateDto.Name, s.Name)))];

    [Theory]
    [MemberData(nameof(SchalterNamen))]
    public async Task Clear_Schalter_Leert_Und_Gewinnt(string dto, string schalter)
    {
        var fall = Faelle.Single(f => f.UpdateDto.Name == dto);
        var feld = fall.Schalter.Single(s => s.Name == schalter).Feld;
        // A **fresh** resource per switch: switches interlock (clearing the series takes the unit with it), and
        // on a shared resource the order would decide the outcome.
        var ziel = await fall.AnlegenAsync(factory);

        var stand = await PatchAsync(ziel, new JsonObject());
        var wert = Lesen(stand, feld);
        Assert.False(wert is null || wert.GetValueKind() == JsonValueKind.Null,
            $"Creating {dto} does not set '{feld}' - without a value the switch '{schalter}' proves nothing.");

        // Does the update DTO carry an input field of the same name? Not every switch has one: the context
        // references of a remark (`context.childId`, `parentRemarkId`) can only be **cleared** through PATCH,
        // not set. Where there is no input field there is no `null` to send either - then all that remains of
        // the switch is the single statement "it clears", and that is what gets checked.
        var eingabefeld = Eingabefeld(fall.UpdateDto, feld);
        if (eingabefeld is not null)
        {
            // The counter-check: a cleared form field would arrive as `null` and must clear nothing.
            var mitNull = await PatchAsync(ziel, new JsonObject { [eingabefeld] = null });
            Assert.Equal(wert!.ToJsonString(), Lesen(mitNull, feld)?.ToJsonString());
        }

        // The switch clears - and wins against the old value sent at the same time.
        var body = new JsonObject { [schalter] = true };
        if (eingabefeld is not null) body[eingabefeld] = wert!.DeepClone();
        var geleert = await PatchAsync(ziel, body);
        Assert.Equal(JsonValueKind.Null, Lesen(geleert, feld)?.GetValueKind() ?? JsonValueKind.Null);
    }

    // ─────────────────────────────────────────────────────────────────── Completeness (reflective)

    [Fact]
    public void Jedes_UpdateDto_Ist_Belegt()
    {
        var vertrag = UpdateDtos().Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        var belegt = Faelle.Select(f => f.UpdateDto.Name).ToHashSet(StringComparer.Ordinal);

        // Self-protection: if the reflection does not bite, the check would be vacuous.
        Assert.True(vertrag.Count >= 20, $"Zu wenige Update-DTOs gefunden ({vertrag.Count}) – falsche Assembly?");
        Assert.True(belegt.All(vertrag.Contains),
            "The case table names DTOs that do not (or no longer) exist in the contract:\n"
            + string.Join("\n", belegt.Except(vertrag)));

        var offen = vertrag.Except(belegt).Except(Ausnahmen.Keys).ToList();
        Assert.True(offen.Count == 0,
            "Update DTO without a PATCH round trip - `null` could silently overwrite there:\n"
            + string.Join("\n", offen));
    }

    [Fact]
    public void Jeder_ClearSchalter_Ist_Belegt()
    {
        // All `bool Clear…` parameters of the update DTOs - the complete set of clearable fields.
        var vertrag = UpdateDtos()
            .SelectMany(t => (t.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First()).GetParameters()
                .Where(p => p.ParameterType == typeof(bool) && p.Name!.StartsWith("Clear", StringComparison.Ordinal))
                .Select(p => $"{t.Name}/{Camel(p.Name!)}"))
            .ToHashSet(StringComparer.Ordinal);
        var belegt = Faelle.SelectMany(f => f.Schalter.Select(s => $"{f.UpdateDto.Name}/{s.Name}"))
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(vertrag.Count >= 15, $"Too few clear switches found ({vertrag.Count}) - the reflection does not bite.");
        Assert.True(belegt.All(vertrag.Contains),
            "The case table names switches the contract does not have:\n" + string.Join("\n", belegt.Except(vertrag)));

        var offen = vertrag.Except(belegt).ToList();
        Assert.True(offen.Count == 0,
            "Clear switch without a test - a UI offering \"- not specified -\" could silently do nothing:\n"
            + string.Join("\n", offen));
    }

    /// <summary>
    /// All partial update contracts of the project.
    /// <para>
    /// <b>Both</b> naming forms, and that's not a convenience: the target-tier contract names
    /// <c>UpdateObjectiveRequest</c>/<c>UpdateKeyResultRequest</c>/<c>UpdateLearnGoalRequest</c>, everything
    /// else <c>Update…Dto</c>. Checking only for <c>…Dto</c> would have silently skipped these four -
    /// a guard with a blind spot is worse than none, because it claims coverage it doesn't have.
    /// </para>
    /// </summary>
    private static IEnumerable<Type> UpdateDtos() =>
        typeof(PointKind).Assembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsNested
                && t.Name.StartsWith("Update", StringComparison.Ordinal)
                && (t.Name.EndsWith("Dto", StringComparison.Ordinal)
                    || t.Name.EndsWith("Request", StringComparison.Ordinal)));

    // ─────────────────────────────────────────────────────────────────── Helpers

    /// <summary>
    /// Patches and returns the state afterwards. A <c>PATCH</c> with an empty body doubles as the
    /// read tool - per null semantics it changes nothing and needs no dedicated GET route.
    /// </summary>
    private static async Task<JsonElement> PatchAsync(Ziel ziel, JsonObject body)
    {
        using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        var res = await ziel.Client.PatchAsync(ziel.PatchUrl, content);
        Assert.True(res.IsSuccessStatusCode,
            $"PATCH {ziel.PatchUrl} with {body.ToJsonString()} → {(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");

        if (ziel.LeseUrl is null)
            return await res.Content.ReadFromJsonAsync<JsonElement>();

        var gelesen = await ziel.Client.GetAsync(ziel.LeseUrl);
        gelesen.EnsureSuccessStatusCode();
        return await gelesen.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>
    /// The name of the input field in the update DTO that sets the same value - or <c>null</c> if there is
    /// none (then the field can only be cleared via PATCH). What's compared is the first path segment: a field
    /// nested under <c>context</c> has no counterpart in the DTO anyway.
    /// </summary>
    private static string? Eingabefeld(Type updateDto, string feldPfad)
    {
        var name = feldPfad.Split('.')[0];
        var parameter = updateDto.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length).First().GetParameters()
            .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        return parameter is null || feldPfad.Contains('.') ? null : Camel(parameter.Name!);
    }

    /// <summary>Reads a field, including nested ones (<c>context.childId</c>).</summary>
    private static JsonNode? Lesen(JsonElement wurzel, string pfad)
    {
        var knoten = JsonNode.Parse(wurzel.GetRawText());
        foreach (var teil in pfad.Split('.'))
        {
            if (knoten is not JsonObject obj || !obj.TryGetPropertyValue(teil, out var naechster))
                return null;
            knoten = naechster;
        }
        return knoten;
    }

    private static string Eindeutig(string präfix) => $"{präfix}-{Guid.NewGuid():N}"[..24];

    private static string Camel(string name) => char.ToLowerInvariant(name[0]) + name[1..];

    private static async Task<int> NeuesKindAsync(HttpClient supervisor) =>
        await TestApi.IdAsync(await supervisor.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = Eindeutig("Kind"), pin = "1111" }));

    private static async Task<int> NeueReiheAsync(HttpClient creator) =>
        await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = Eindeutig("Access"), sourceLanguage = "en", targetLanguage = "de" }));

    private static async Task<int> NeuesMotivAsync(HttpClient creator) =>
        await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/media",
            new { description = "Ein Pferd", key = Eindeutig("m") }));

    /// <summary>Child + objective with one key result - the target tier needs both ids.</summary>
    private static async Task<(int ChildId, int KeyResultId, int ObjectiveId)> NeuesObjectiveAsync(HttpClient supervisor)
    {
        var childId = await NeuesKindAsync(supervisor);
        var subjectId = await TestApi.IdAsync(await supervisor.PostAsJsonAsync("/api/v1/creator/subjects", new { name = Eindeutig("Fach") }));
        var res = await supervisor.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/objectives", new
        {
            title = "Englisch aufholen",
            kind = "Committed",
            rewardOnComplete = 50,
            rewardPerKeyResult = 10,
            keyResults = new[] { new { subjectId, metric = "AvgMastery", targetValue = 70, title = "Vokabeln" } },
        });
        res.EnsureSuccessStatusCode();
        var objective = await res.Content.ReadFromJsonAsync<JsonElement>();
        return (childId, objective.GetProperty("keyResults")[0].GetProperty("id").GetInt32(),
            objective.GetProperty("id").GetInt32());
    }

    /// <summary>Registers its own adult and logs them in - for the cases that modify themselves.</summary>
    private static async Task<(HttpClient Client, int Id)> NeuerErwachsenerAsync(PuglingWebAppFactory f, string? email = null)
    {
        var id = await TestApi.IdAsync(await f.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = Eindeutig("Papa"), email, pin = "4444" }));
        return (await TestApi.FatherAsync(f, id, "4444"), id);
    }
}
