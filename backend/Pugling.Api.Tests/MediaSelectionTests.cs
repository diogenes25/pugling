using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

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

    /// <summary>PIN der Szenario-Kinder – für die Endpunkte, die der Sohn selbst aufruft.</summary>
    private const string ChildPin = "9876";

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

    /// <summary>
    /// <b>Punktgleichstand</b> ist der einzige Fall, in dem der Tiebreak überhaupt entscheidet – und genau
    /// deshalb war die dokumentierte Determinismus-Zusage („kein <c>Random</c>, kein
    /// <c>string.GetHashCode</c>") unbewacht: keiner der übrigen Tests erzeugt einen Gleichstand
    /// (docs/testplan.md, Injektion D07). Bildkonstanz <i>ist</i> beim Vokabellernen der Merkeffekt; ein
    /// zufälliger Tiebreak zerstört ihn für jeden Träger, der noch nicht eingefroren ist.
    /// <para>
    /// Der Test räumt zwischen den Runden die Einfrierung weg – sonst prüfte er nur, dass Schritt 3 der
    /// Kaskade (die eingefrorene Wahl gewinnt) funktioniert, und der Tiebreak käme nie wieder dran.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Punktgleichstand_WirdDeterministischGebrochen_NichtZufaellig()
    {
        var father = await TestApi.FatherAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-gleichstand", assetCount: 0);

        // Zwei Darstellungen ohne Tags und mit gleichem redaktionellem Rang: identische Punktzahl (0 –
        // das Kind hat keine Interessen), identisches Gewicht. Damit hängt die Wahl allein am Tiebreak.
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

        // Gegenprobe gegen einen vakuum-grünen Test: nur wenn wirklich BEIDE Bilder zur Wahl standen, hat
        // der Tiebreak etwas entschieden. „Anderes Bild" muss also das zweite herausgeben.
        var other = await father.PostAsJsonAsync($"/api/v1/student/children/{setup.ChildId}/media-picks/reshuffle",
            new { vocabularyId = setup.VocabularyId });
        other.EnsureSuccessStatusCode();
        Assert.DoesNotContain((await other.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("imageUrl").GetString(), seen);
    }

    /// <summary>
    /// Die Zusage lautet <b>prozessunabhängig</b> deterministisch, nicht bloß „innerhalb eines Laufs
    /// gleich". Genau darin unterscheidet sich FNV-1a von <c>string.GetHashCode</c>, dessen Startwert pro
    /// Prozess randomisiert ist: ein Neustart verschöbe die Wahl jedes noch nicht eingefrorenen Trägers.
    /// Ein Vergleich zweier Aufrufe im selben Prozess kann das nicht zeigen – festgeschriebene Goldwerte
    /// können es. Fällt dieser Test, hat sich der Hash geändert und mit ihm die Bildwahl aller Kinder,
    /// deren Träger noch nicht eingefroren sind.
    /// </summary>
    [Fact]
    public void Tiebreak_LiefertFestgeschriebeneGoldwerte()
    {
        Assert.Equal(467997332u, Tiebreak(1, 1, 1));
        Assert.Equal(2034659765u, Tiebreak(1, 2, 3));
        Assert.Equal(3601875931u, Tiebreak(7, 42, 99));
    }

    /// <summary>Räumt die eingefrorenen Bildwahlen eines Kindes weg, damit die Auswahl erneut rechnet.</summary>
    private void ClearPicks(int childId)
    {
        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<PuglingDbContext>()
            .ChildMediaPicks.Where(p => p.ChildId == childId).ExecuteDelete();
    }

    /// <summary>
    /// Der Tiebreak ist privat (er gehört niemandem außer der Auswahl) – für die Goldwerte wird er
    /// reflexiv gerufen. Verschwindet er, soll dieser Test laut scheitern und nicht still verschwinden.
    /// </summary>
    private static uint Tiebreak(int childId, int carrierId, int assetId)
    {
        var method = typeof(MediaSelector).GetMethod("StableTiebreak", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (uint)method.Invoke(null, [childId, carrierId, assetId])!;
    }

    /// <summary>
    /// Wird die eingefrorene Wahl nachträglich unzulässig (hier: der Vater trägt eine Abneigung gegen ihr
    /// Motiv nach), muss die alte Einfrierung <b>zurückgezogen</b> werden und nicht bloß übergangen. Sonst
    /// bliebe sie die aktive Wahl, die Neuwahl fiele bei jedem Abruf erneut, und das zweite Einfrieren
    /// risse den Unique-Index: die Karte wäre für dieses Kind dauerhaft nicht mehr abrufbar – ohne einen
    /// Weg zurück über die API.
    /// </summary>
    [Fact]
    public async Task UnzulaessigGewordeneWahl_WirdZurueckgezogen_StattDieKarteZuVerbrennen()
    {
        var father = await TestApi.FatherAsync(factory);
        var setup = await ScenarioAsync(father, "auswahl-veraltet");
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", 3)]);

        Assert.Contains("unicorn", (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString());

        // Das eingefrorene Motiv fällt aus der Auswahl – eine Abneigung schließt hart aus.
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", -3)]);
        var second = (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString();
        Assert.DoesNotContain("unicorn", second);

        // Der eigentliche Regressionstest ist der dritte Abruf: er lief vorher in den Unique-Index.
        Assert.Equal(second, (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString());
        Assert.Equal(second, (await FirstCardAsync(father, setup, SelfAssess)).GetProperty("imageUrl").GetString());
    }

    /// <summary>
    /// „Anderes Bild" <b>gibt ein Bild heraus</b> – auf einer getippten Stufe wäre der Endpunkt damit das
    /// Loch in der Anti-Cheat-Regel: die Karte hält Bild <i>und</i> Alt-Text zurück, weil das Motiv die
    /// Bedeutung genau des Wortes zeigt, das getippt werden soll. Er muss dieselbe Schranke tragen.
    /// </summary>
    [Fact]
    public async Task AnderesBild_AufGetippterStufe_GibtNichtsHeraus()
    {
        var father = await TestApi.FatherAsync(factory);
        var setup = await ScenarioAsync(father, "reshuffle-stufe");
        await SetInterestsAsync(father, setup.ChildId, [("Einhorn", 3)]);
        var sohn = await TestApi.ChildAsync(factory, setup.ChildId, ChildPin);

        var (typedSession, typedCard) = await SessionAsync(father, sohn, setup, LetterBoxes);
        Assert.True(IsNull(typedCard, "imageUrl"), "Die Karte selbst zeigt auf dieser Stufe kein Bild.");

        var res = await sohn.PostAsync(ReshuffleUrl(setup, typedSession, CardIndex(typedCard)), null);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("media_not_on_card", await CodeOf(res));

        // Auf der Selbsteinschätzung – wo das Bild seinen Zweck erfüllt – bleibt es möglich.
        var (openSession, openCard) = await SessionAsync(father, sohn, setup, SelfAssess);
        (await sohn.PostAsync(ReshuffleUrl(setup, openSession, CardIndex(openCard)), null)).EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Der Index adressiert eine Karte <b>dieser Sitzung</b>. Ohne die Grenze ließen sich über einen freien
    /// Index die Motive und Beschreibungen der ganzen Übung durchzählen – auch die der Karten, die die
    /// Sitzung nie ausliefert.
    /// </summary>
    [Fact]
    public async Task AnderesBild_NurFuerKartenDerSitzung()
    {
        var father = await TestApi.FatherAsync(factory);
        var setup = await ScenarioAsync(father, "reshuffle-index");
        var sohn = await TestApi.ChildAsync(factory, setup.ChildId, ChildPin);

        var (sessionId, _) = await SessionAsync(father, sohn, setup, SelfAssess);
        var res = await sohn.PostAsync(ReshuffleUrl(setup, sessionId, 99), null);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    /// <summary>
    /// Wie jeder spielende Endpunkt: ein stillgelegter Plan ist für den Sohn zu, für den Vater
    /// (Vorschau/Nachtrag) offen.
    /// </summary>
    [Fact]
    public async Task AnderesBild_ImStillgelegtenPlan_BleibtDemSohnVerschlossen()
    {
        var father = await TestApi.FatherAsync(factory);
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
            new { name = marker, pin = ChildPin }));

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
            // Der Plan soll laufen. Früher stand hier `endDate`/`active` – Felder, die es nur im
            // Update-DTO gibt und die der Server beim Anlegen still verwarf; die Laufzeit ergab sich
            // in Wahrheit aus dem Default. `durationDays` ist das Feld, das der Vertrag dafür vorsieht.
            durationDays = 31,
        }));
        var positionId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/supervisor/study-plans/{planId}/positions",
            // `cadence`/`pointsGoalMet` – früher stand hier `goalCadence`/`goalPoints`. Beide Namen gibt
            // es im Vertrag nicht; der Server verwarf sie still, die Position hatte also nie den
            // Pflichtrhythmus und die Punkte, die dieser Aufbau vorgibt.
            new { exerciseId, order = 1, cadence = "Daily", goalThreshold = 1, pointsGoalMet = 5 }));

        return new Scenario(marker, childId, planId, positionId, exerciseId, itemId, vocabularyId);
    }

    /// <summary>Startet eine Übungssitzung auf der gewünschten Stufe und liefert die erste Karte.</summary>
    private static async Task<JsonElement> FirstCardAsync(HttpClient father, Scenario s, int stage) =>
        (await SessionAsync(father, father, s, stage)).Card;

    /// <summary>
    /// Wie <see cref="FirstCardAsync"/>, gibt aber auch die Sitzungs-Id zurück und lässt
    /// <paramref name="player"/> spielen – für die Endpunkte, die das Kind selbst aufruft (die Stufe setzt
    /// weiterhin nur der Vater).
    /// </summary>
    private static async Task<(int SessionId, JsonElement Card)> SessionAsync(HttpClient father, HttpClient player,
        Scenario s, int stage)
    {
        // Die Stufe kommt aus dem Fahrplan der Position (der Server erzwingt sie – nie der Client).
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
