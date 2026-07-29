using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Pugling.Api.Tests;

/// <summary>
/// Der PATCH-Semantik-Wächter aus docs/codequalitaet-gates-plan.md (C2) – für <b>jedes</b>
/// <c>Update…Dto</c> des Vertrags, nicht für eine Handvoll Beispiele.
/// <para>
/// Zwei Regeln, mechanisch festgenagelt:
/// </para>
/// <list type="number">
/// <item><b><c>null</c> heißt „nicht angegeben"</b> – ein Feld setzen, dann mit <c>null</c> nachfassen:
/// der Wert bleibt. Reißt diese Regel, überschreibt jedes Formular, das ein Feld nicht ausfüllt, still den
/// Bestand.</item>
/// <item><b>Der <c>Clear…</c>-Schalter leert – und gewinnt.</b> Schickt eine Oberfläche Wert <em>und</em>
/// Schalter (das „– keine Angabe –" eines Auswahlfelds neben dem alten Wert), muss „leeren" gewinnen. Sonst
/// meldet sie „Gespeichert." und der alte Wert steht weiter da.</item>
/// </list>
/// <para>
/// Die Vollständigkeit ist selbst geprüft: <see cref="Jedes_UpdateDto_Ist_Belegt"/> und
/// <see cref="Jeder_ClearSchalter_Ist_Belegt"/> vergleichen die Falltabelle reflexiv mit dem Vertrag. Ein
/// neues <c>Update…Dto</c> oder ein neuer Schalter macht diesen Test rot, bis ein Fall dazukommt – die
/// Regel bleibt damit auch dann gedeckt, wenn niemand an diese Datei denkt.
/// </para>
/// <para>
/// Fachlich zugespitzte Einzelfälle (etwa „die Unit fällt mit der Reihe weg") stehen weiterhin in
/// <see cref="PatchClearFieldTests"/>; hier geht es um die Fläche.
/// </para>
/// </summary>
public class PatchSemanticsTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // ─────────────────────────────────────────────────────────────────── Die Fläche

    /// <summary>Ein <c>Clear…</c>-Schalter und das Feld, das er leert (die Zuordnung ist nicht ableitbar:
    /// <c>clearSubject</c> leert <c>subjectId</c>, <c>clearUnit</c> leert <c>currentUnitId</c>).</summary>
    private sealed record Schalter(string Name, string Feld);

    /// <summary>Wohin gepatcht wird – und wo der Stand zu lesen ist, falls die PATCH-Antwort das Feld nicht zeigt.</summary>
    private sealed record Ziel(HttpClient Client, string PatchUrl, string? LeseUrl = null);

    /// <summary>Eine patchbare Ressource: Anlage, das Feld für den Rundlauf und die Schalter.</summary>
    private sealed record Fall(
        Type UpdateDto,
        string Feld,
        JsonNode Wert,
        Func<PuglingWebAppFactory, Task<Ziel>> AnlegenAsync,
        Schalter[] Schalter);

    /// <summary>
    /// Update-DTOs ohne Rundlauf – <b>kein</b> Sammelbecken, jeder Eintrag trägt seinen Grund.
    /// </summary>
    private static readonly Dictionary<string, string> Ausnahmen = new(StringComparer.Ordinal)
    {
        // Hat kein optionales Feld: `UpdateMediaLinkDto(int Weight)` verlangt das Gewicht. „Nicht
        // angegeben" ist darin gar nicht ausdrückbar, es gibt also keine null-Semantik zu prüfen.
        ["UpdateMediaLinkDto"] = "einziges Feld (Weight) ist Pflicht – null ist nicht ausdrückbar",
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
            var (id, _) = await TestApi.CreateStoreVocabAsync(c, Eindeutig("cow"), "das Rind");
            return new Ziel(c, $"/api/v1/creator/vocabulary/{id}");
        }, []),

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
                // Der Name ist je Creator eindeutig – zwei Fälle dieser Klasse legen sonst denselben an.
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
            // Ein **eigener** Erwachsener: `Update` erlaubt nur den eigenen Datensatz, und ein Umbenennen
            // des geseedeten Papas würde anderen Fällen dieser Klasse den Boden wegziehen.
            var (client, id) = await NeuerErwachsenerAsync(f);
            return new Ziel(client, $"/api/v1/supervisor/adults/{id}");
        }, []),

        new(typeof(UpdateMyAccountDto), "name", "Selbst umbenannt", async f =>
        {
            // Eindeutige Adresse: `Account.Email` trägt einen Unique-Index, und beide Theorien legen einen an.
            var (client, id) = await NeuerErwachsenerAsync(f, email: $"{Eindeutig("papa")}@example.test");
            // `MeResponse` führt die E-Mail nicht – für `clearEmail` muss der Stand darum aus dem
            // Erwachsenen-Datensatz kommen.
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
            // `Create` liefert `KlassenarbeitDetail` – die Id steckt eine Ebene tiefer als üblich.
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
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync(
                $"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId, stage = 1 }));
            return new Ziel(c, $"/api/v1/supervisor/study-plans/{planId}/positions/{id}");
        }, []),

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

        new(typeof(UpdateLearnGoalRequest), "title", "Lernziel neu", async f =>
        {
            var c = await TestApi.FatherAsync(f);
            var childId = await NeuesKindAsync(c);
            var subjectId = await TestApi.IdAsync(await c.PostAsJsonAsync("/api/v1/creator/subjects", new { name = Eindeutig("Fach") }));
            var id = await TestApi.IdAsync(await c.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/learn-goals",
                new { subjectId, metric = "AvgMastery", targetValue = 70, title = "Vokabeln sitzen" }));
            return new Ziel(c, $"/api/v1/supervisor/children/{childId}/learn-goals/{id}");
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
            // Die Antwort setzt erst der Skill – für `clearAnswer` muss sie vorher stehen.
            (await c.PatchAsJsonAsync($"/api/v1/remarks/{id}", new { answer = "Kommt aus dem MediaSelector.", answeredBy = "Claude" }))
                .EnsureSuccessStatusCode();
            return new Ziel(c, $"/api/v1/remarks/{id}");
        }, [new("clearAnswer", "answer"), new("clearChild", "context.childId"),
            new("clearExercise", "context.exerciseId"), new("clearStudyPlan", "context.studyPlanId"),
            new("clearPlanPosition", "context.planPositionId"), new("clearParent", "parentRemarkId")]),
    ];

    // ─────────────────────────────────────────────────────────────────── Regel 1: null ändert nichts

    public static TheoryData<string> FallNamen => [.. Faelle.Select(f => f.UpdateDto.Name)];

    [Theory]
    [MemberData(nameof(FallNamen))]
    public async Task Null_Laesst_Den_Wert_Stehen(string dto)
    {
        var fall = Faelle.Single(f => f.UpdateDto.Name == dto);
        var ziel = await fall.AnlegenAsync(factory);

        // Setzen muss wirken – sonst prüfte der Rundlauf danach nur, dass sich nichts tut.
        var gesetzt = await PatchAsync(ziel, new JsonObject { [fall.Feld] = fall.Wert.DeepClone() });
        Assert.Equal(fall.Wert.ToJsonString(), Lesen(gesetzt, fall.Feld)?.ToJsonString());

        // Und jetzt die Regel: `null` heißt „nicht angegeben", nicht „leeren".
        var danach = await PatchAsync(ziel, new JsonObject { [fall.Feld] = null });
        Assert.Equal(fall.Wert.ToJsonString(), Lesen(danach, fall.Feld)?.ToJsonString());
    }

    // ─────────────────────────────────────────────────────────────────── Regel 2: Clear leert und gewinnt

    public static TheoryData<string, string> SchalterNamen =>
        [.. Faelle.SelectMany(f => f.Schalter.Select(s => (f.UpdateDto.Name, s.Name)))];

    [Theory]
    [MemberData(nameof(SchalterNamen))]
    public async Task Clear_Schalter_Leert_Und_Gewinnt(string dto, string schalter)
    {
        var fall = Faelle.Single(f => f.UpdateDto.Name == dto);
        var feld = fall.Schalter.Single(s => s.Name == schalter).Feld;
        // Je Schalter eine **frische** Ressource: Schalter greifen ineinander (das Leeren der Reihe nimmt
        // die Unit mit), und auf einer geteilten Ressource entschiede die Reihenfolge über das Ergebnis.
        var ziel = await fall.AnlegenAsync(factory);

        var stand = await PatchAsync(ziel, new JsonObject());
        var wert = Lesen(stand, feld);
        Assert.False(wert is null || wert.GetValueKind() == JsonValueKind.Null,
            $"Die Anlage von {dto} setzt '{feld}' nicht – ohne Wert belegt der Schalter '{schalter}' nichts.");

        // Trägt das Update-DTO ein gleichnamiges Eingabefeld? Nicht jeder Schalter hat eines: die
        // Kontext-Bezüge einer Anmerkung (`context.childId`, `parentRemarkId`) lassen sich per PATCH nur
        // **leeren**, nicht setzen. Wo es kein Eingabefeld gibt, gibt es auch kein `null` zu schicken –
        // dann bleibt vom Schalter nur die eine Aussage „er leert", und genau die wird geprüft.
        var eingabefeld = Eingabefeld(fall.UpdateDto, feld);
        if (eingabefeld is not null)
        {
            // Gegenprobe: das geräumte Formularfeld käme als `null` an und darf nichts leeren.
            var mitNull = await PatchAsync(ziel, new JsonObject { [eingabefeld] = null });
            Assert.Equal(wert!.ToJsonString(), Lesen(mitNull, feld)?.ToJsonString());
        }

        // Der Schalter leert – und gewinnt gegen den gleichzeitig geschickten alten Wert.
        var body = new JsonObject { [schalter] = true };
        if (eingabefeld is not null) body[eingabefeld] = wert!.DeepClone();
        var geleert = await PatchAsync(ziel, body);
        Assert.Equal(JsonValueKind.Null, Lesen(geleert, feld)?.GetValueKind() ?? JsonValueKind.Null);
    }

    // ─────────────────────────────────────────────────────────────────── Vollständigkeit (reflexiv)

    [Fact]
    public void Jedes_UpdateDto_Ist_Belegt()
    {
        var vertrag = UpdateDtos().Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        var belegt = Faelle.Select(f => f.UpdateDto.Name).ToHashSet(StringComparer.Ordinal);

        // Selbstschutz: greift die Reflexion nicht, wäre die Prüfung inhaltsleer.
        Assert.True(vertrag.Count >= 20, $"Zu wenige Update-DTOs gefunden ({vertrag.Count}) – falsche Assembly?");
        Assert.True(belegt.All(vertrag.Contains),
            "Falltabelle nennt DTOs, die es im Vertrag nicht (mehr) gibt:\n"
            + string.Join("\n", belegt.Except(vertrag)));

        var offen = vertrag.Except(belegt).Except(Ausnahmen.Keys).ToList();
        Assert.True(offen.Count == 0,
            "Update-DTO ohne PATCH-Rundlauf – `null` könnte dort still überschreiben:\n"
            + string.Join("\n", offen));
    }

    [Fact]
    public void Jeder_ClearSchalter_Ist_Belegt()
    {
        // Alle `bool Clear…`-Parameter der Update-DTOs – die vollständige Menge der löschbaren Felder.
        var vertrag = UpdateDtos()
            .SelectMany(t => (t.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First()).GetParameters()
                .Where(p => p.ParameterType == typeof(bool) && p.Name!.StartsWith("Clear", StringComparison.Ordinal))
                .Select(p => $"{t.Name}/{Camel(p.Name!)}"))
            .ToHashSet(StringComparer.Ordinal);
        var belegt = Faelle.SelectMany(f => f.Schalter.Select(s => $"{f.UpdateDto.Name}/{s.Name}"))
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(vertrag.Count >= 15, $"Zu wenige Clear-Schalter gefunden ({vertrag.Count}) – Reflexion greift nicht.");
        Assert.True(belegt.All(vertrag.Contains),
            "Falltabelle nennt Schalter, die der Vertrag nicht hat:\n" + string.Join("\n", belegt.Except(vertrag)));

        var offen = vertrag.Except(belegt).ToList();
        Assert.True(offen.Count == 0,
            "Clear-Schalter ohne Test – eine Oberfläche mit „– keine Angabe –“ könnte still nichts tun:\n"
            + string.Join("\n", offen));
    }

    /// <summary>
    /// Alle Teil-Update-Verträge des Projekts.
    /// <para>
    /// <b>Beide</b> Namensformen, und das ist keine Bequemlichkeit: der Vertrag nennt die Ziel-Ebene
    /// <c>UpdateObjectiveRequest</c>/<c>UpdateKeyResultRequest</c>/<c>UpdateLearnGoalRequest</c>, alles
    /// andere <c>Update…Dto</c>. Eine Prüfung nur auf <c>…Dto</c> hätte diese vier stillschweigend
    /// ausgelassen – ein Wächter mit blindem Fleck ist schlimmer als keiner, weil er Deckung behauptet.
    /// </para>
    /// </summary>
    private static IEnumerable<Type> UpdateDtos() =>
        typeof(PointKind).Assembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsNested
                && t.Name.StartsWith("Update", StringComparison.Ordinal)
                && (t.Name.EndsWith("Dto", StringComparison.Ordinal)
                    || t.Name.EndsWith("Request", StringComparison.Ordinal)));

    // ─────────────────────────────────────────────────────────────────── Hilfsmittel

    /// <summary>
    /// Patcht und liefert den Stand danach. Ein <c>PATCH</c> mit leerem Rumpf ist dabei zugleich das
    /// Lesewerkzeug – er ändert nach der null-Semantik nichts und braucht keine eigene GET-Route.
    /// </summary>
    private static async Task<JsonElement> PatchAsync(Ziel ziel, JsonObject body)
    {
        using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        var res = await ziel.Client.PatchAsync(ziel.PatchUrl, content);
        Assert.True(res.IsSuccessStatusCode,
            $"PATCH {ziel.PatchUrl} mit {body.ToJsonString()} → {(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");

        if (ziel.LeseUrl is null)
            return await res.Content.ReadFromJsonAsync<JsonElement>();

        var gelesen = await ziel.Client.GetAsync(ziel.LeseUrl);
        gelesen.EnsureSuccessStatusCode();
        return await gelesen.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>
    /// Der Name des Eingabefelds im Update-DTO, das denselben Wert setzt – oder <c>null</c>, wenn es keines
    /// gibt (dann ist das Feld per PATCH nur leerbar). Verglichen wird der erste Pfadabschnitt: ein Feld
    /// unterhalb von <c>context</c> hat im DTO ohnehin kein Gegenstück.
    /// </summary>
    private static string? Eingabefeld(Type updateDto, string feldPfad)
    {
        var name = feldPfad.Split('.')[0];
        var parameter = updateDto.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length).First().GetParameters()
            .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        return parameter is null || feldPfad.Contains('.') ? null : Camel(parameter.Name!);
    }

    /// <summary>Liest ein Feld, auch verschachtelt (<c>context.childId</c>).</summary>
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

    /// <summary>Kind + Objective mit einer Etappe – die Ziel-Ebene braucht beide Ids.</summary>
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

    /// <summary>Registriert einen eigenen Erwachsenen und meldet ihn an – für die Fälle, die sich selbst ändern.</summary>
    private static async Task<(HttpClient Client, int Id)> NeuerErwachsenerAsync(PuglingWebAppFactory f, string? email = null)
    {
        var id = await TestApi.IdAsync(await f.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = Eindeutig("Papa"), email, pin = "4444" }));
        return (await TestApi.FatherAsync(f, id, "4444"), id);
    }
}
