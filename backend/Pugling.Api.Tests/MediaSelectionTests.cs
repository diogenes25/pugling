using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Die Auswahl je Kind (Etappe 4) – hier kommt der ganze Entwurf zusammen: aus mehreren Darstellungen
/// eines Motivs wird für <b>dieses</b> Kind eine, sie wird eingefroren (Bildkonstanz = Merkeffekt), und
/// sie erscheint nur auf Stufen, auf denen ein Motiv die Lösung nicht verraten kann.
/// </summary>
public class MediaSelectionTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // Werte aus TestStage. Nicht getippt (Lösung ohnehin sichtbar) → Bild erlaubt: ShowBoth, SelfAssess.
    private const int ShowBoth = 1;
    private const int SelfAssess = 2;
    // Getippt → Bild verriete die Lösung.
    private const int LetterBoxes = 3;
    private const int FreeText = 4;
    private const int MultipleChoice = 6;

    [Fact]
    public async Task DasInteresseDesKindes_EntscheidetWelcheDarstellungKommt()
    {
        var father = await TestApi.FatherAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-interesse");

        // Das Kind mag Einhörner, nicht Superhelden – von drei Darstellungen muss das Einhorn kommen.
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", 3), ("Superhelden", 0)]);

        var card = await FirstCardAsync(father, setup, SelfAssess);
        Assert.Equal("Ein Einhorn laeuft", card.GetProperty("imageAlt").GetString());
        Assert.Contains("unicorn", card.GetProperty("imageUrl").GetString());
    }

    [Fact]
    public async Task Abneigung_SchliesstAus_StattNurSchlechterZuRanken()
    {
        var father = await TestApi.FatherAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-abneigung");

        // „Comic" ist stark positiv, aber das Einhorn trägt zusätzlich einen abgelehnten Tag. Eine reine
        // Punktesumme würde es trotzdem wählen – der harte Ausschluss darf sich davon nicht überstimmen lassen.
        await SetInterestsAsync(father, setup.ChildId, [("Comic", 3), ("Einhorn", -3)]);

        var card = await FirstCardAsync(father, setup, SelfAssess);
        Assert.DoesNotContain("unicorn", card.GetProperty("imageUrl").GetString());
    }

    [Fact]
    public async Task Eignung_UeberDerFreigabe_KommtNie()
    {
        var father = await TestApi.FatherAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-rating", includeMature: true);

        // Das Kind „mag" den Tag des nicht freigegebenen Bildes am stärksten – das Rating sticht trotzdem.
        await SetInterestsAsync(father, setup.ChildId, [("Freizuegig", 3), ("Einhorn", 1)]);

        var card = await FirstCardAsync(father, setup, SelfAssess);
        Assert.DoesNotContain("mature", card.GetProperty("imageUrl").GetString());
    }

    [Fact]
    public async Task DieWahlBleibtStabil_AuchWennSpaeterEinBesseresBildDazukommt()
    {
        var father = await TestApi.FatherAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-konstanz");
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", 1)]);

        var first = (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString();

        // Ein Bild, das nach Punkten deutlich besser passt, kommt nachträglich dazu …
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", 1), ("Pixel-Art", 3)]);
        var late = await CreateAssetAsync(father, $"{setup.Marker}_pixel", "Laeufer in Pixel-Art", ["Pixel-Art"]);
        await father.PostAsJsonAsync($"/api/v1/creator/vocabulary/{setup.VocabularyId}/media",
            new { mediaAssetId = late });

        // … darf die laufende Wahl aber nicht kippen: Wiedererkennung IST der Merkeffekt.
        var second = (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString();
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task AnderesBild_TauschtAus_UndZiehtDasAbgelehnteNieWieder()
    {
        var father = await TestApi.FatherAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-reshuffle");
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", 3)]);

        var before = (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString();

        var res = await father.PostAsJsonAsync($"/api/v1/student/children/{setup.ChildId}/media-picks/reshuffle",
            new { vocabularyId = setup.VocabularyId });
        res.EnsureSuccessStatusCode();
        var after = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("imageUrl").GetString();
        Assert.NotEqual(before, after);

        // Die Karte folgt der neuen Wahl …
        Assert.Equal(after, (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString());

        // … und das abgelehnte Bild taucht auch nach weiterem Umwählen nicht wieder auf.
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
        Assert.Equal(3, seen.Count); // genau die drei Darstellungen des Szenarios
    }

    [Fact]
    public async Task OhneAlternative_BleibtDasBildStehen_StattZuVerschwinden()
    {
        var father = await TestApi.FatherAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-letztes", assetCount: 1);

        var before = (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString();

        var res = await father.PostAsJsonAsync($"/api/v1/student/children/{setup.ChildId}/media-picks/reshuffle",
            new { vocabularyId = setup.VocabularyId });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("media_no_alternative", await CodeOf(res));

        // Entscheidend: der einzige Kandidat wurde NICHT verbrannt – die Karte trägt weiter ihr Bild.
        Assert.Equal(before, (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString());
    }

    [Fact]
    public async Task GetippteStufen_ZeigenKeinBild_DennEinMotivVerraetDieBedeutung()
    {
        var father = await TestApi.FatherAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-anticheat");
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", 3)]);

        // Anzeige- und Selbsteinschätzungsstufe decken die Lösung ohnehin auf – hier hilft das Bild beim
        // Einprägen, und genau dafür ist es da.
        foreach (var openStage in new[] { ShowBoth, SelfAssess })
            Assert.False(IsNull(await FirstCardAsync(father, setup, openStage), "imageUrl"),
                $"Stufe {openStage} sollte ein Bild tragen.");

        foreach (var typedStage in new[] { LetterBoxes, FreeText, MultipleChoice })
        {
            var card = await FirstCardAsync(father, setup, typedStage);
            Assert.True(IsNull(card, "imageUrl"), $"Stufe {typedStage} darf kein Bild tragen.");
            // Auch der Alt-Text muss weg – „Ein Einhorn laeuft" verriete dasselbe wie das Bild.
            Assert.True(IsNull(card, "imageAlt"), $"Stufe {typedStage} darf keinen Alt-Text tragen.");
        }
    }

    [Fact]
    public async Task ItemZuordnung_SchlaegtDieStoreZuordnung()
    {
        var father = await TestApi.FatherAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-kaskade");
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", 3)]);

        var special = await CreateAssetAsync(father, $"{setup.Marker}_override", "Nur in dieser Uebung", []);
        var link = await father.PostAsJsonAsync(
            $"/api/v1/creator/exercises/{setup.ExerciseId}/items/{setup.ItemId}/media",
            new { mediaAssetId = special });
        link.EnsureSuccessStatusCode();

        // Trotz starkem Interesse am Einhorn gewinnt die übungslokale Übersteuerung – sie ist die
        // genauere Aussage über genau diese Übung.
        var card = await FirstCardAsync(father, setup, SelfAssess);
        Assert.Equal("Nur in dieser Uebung", card.GetProperty("imageAlt").GetString());
    }

    [Fact]
    public async Task OhneZuordnung_BleibtDieKarteBildlos_StattEinenNotnagelZuZeigen()
    {
        var father = await TestApi.FatherAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-leer", assetCount: 0);

        var card = await FirstCardAsync(father, setup, SelfAssess);
        Assert.True(IsNull(card, "imageUrl"));
    }

    [Fact]
    public async Task ZweiKinder_BekommenIhrJeweilsPassendesBild()
    {
        var father = await TestApi.FatherAsync(factory);
        var first = await ScenarioAsync(father, "auswahl-kind-a");
        // Zweites Kind auf dieselbe Übung: gleicher Stoff, anderes Profil.
        var second = await ScenarioAsync(father, "auswahl-kind-b", reuse: first);

        await SetInterestsAsync(father, first.ChildId, [("Einhorn", 3)]);
        await SetInterestsAsync(father, second.ChildId, [("Superhelden", 3)]);

        var cardA = await FirstCardAsync(father, first, SelfAssess);
        var cardB = await FirstCardAsync(father, second, SelfAssess);

        Assert.Contains("unicorn", cardA.GetProperty("imageUrl").GetString());
        Assert.Contains("flash", cardB.GetProperty("imageUrl").GetString());
    }

    // ---- Szenario-Aufbau ----------------------------------------------------------------------------

    private sealed record Scenario(string Marker, int ChildId, int PlanId, int PositionId,
        int ExerciseId, int ItemId, int VocabularyId);

    /// <summary>
    /// Baut ein vollständiges, isoliertes Szenario: eigenes Kind, eine Vokabelübung mit einem Wort,
    /// dazu bis zu drei Darstellungen im Store, und einen aktiven Lehrplan mit einer Position darauf.
    /// <paramref name="reuse"/> hängt ein zweites Kind an dieselbe Übung (für den Profil-Vergleich).
    /// </summary>
    private async Task<Scenario> ScenarioAsync(HttpClient father, string marker, int assetCount = 3,
        bool includeMature = false, Scenario? reuse = null)
    {
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = marker, pin = "9876" }));

        int exerciseId, itemId, vocabularyId;
        if (reuse is not null)
        {
            (exerciseId, itemId, vocabularyId) = (reuse.ExerciseId, reuse.ItemId, reuse.VocabularyId);
        }
        else
        {
            var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name = marker }));
            var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
                $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit", orderIndex = 1 }));
            exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
                $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary", new
                {
                    title = $"{marker}-Uebung",
                    orderIndex = 1,
                    rewardPoints = 10,
                    config = new
                    {
                        direction = "front-to-back",
                        sourceLang = "en",
                        targetLang = "de",
                        // Eigenes Wort je Szenario: der Vokabel-Store ist find-or-create, ein geteiltes
                        // „run" würde die Bild-Zuordnungen aller Szenarien auf derselben Zeile sammeln.
                        items = new[] { new { front = $"run-{marker}", back = $"laufen-{marker}" } },
                    },
                }));

            var items = await GetAsync(father,
                $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}/items");
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
            endDate = today.AddDays(30).ToString("yyyy-MM-dd"),
            active = true,
        }));
        var positionId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, order = 1, goalCadence = "Daily", goalThreshold = 1, goalPoints = 5 }));

        return new Scenario(marker, childId, planId, positionId, exerciseId, itemId, vocabularyId);
    }

    /// <summary>Startet eine Übungssitzung auf der gewünschten Stufe und liefert die erste Karte.</summary>
    private static async Task<JsonElement> FirstCardAsync(HttpClient father, Scenario s, int stage)
    {
        // Die Stufe kommt aus dem Fahrplan der Position (der Server erzwingt sie – nie der Client).
        var patch = await father.PatchAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{s.PlanId}/positions/{s.PositionId}", new { stage });
        patch.EnsureSuccessStatusCode();

        var start = await father.PostAsJsonAsync(
            $"/api/v1/student/study-plans/{s.PlanId}/positions/{s.PositionId}/practice-sessions",
            new { mode = "Lern" });
        start.EnsureSuccessStatusCode();
        var sessionId = await TestApi.IdAsync(start);

        var next = await GetAsync(father,
            $"/api/v1/student/study-plans/{s.PlanId}/positions/{s.PositionId}/practice-sessions/{sessionId}/next");
        return next.GetProperty("card");
    }

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
