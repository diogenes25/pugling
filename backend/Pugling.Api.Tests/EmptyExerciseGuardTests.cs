using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Eine <b>ungefüllte</b> Übung (Typ trägt seine Inhalte als Item-Tabelle, hat aber kein Item) darf nicht
/// unbemerkt in einen Lehrplan wandern – das Kind bekäme eine Pflicht, die es nicht spielen kann, und erfuhr
/// es bisher erst im Test als <c>no_checkable_content</c>.
///
/// Der Riegel sitzt bewusst beim <b>Zuweisen</b>, nicht beim Anlegen: „erst anlegen, dann füllen" ist ein
/// gewollter Weg (POST mit leeren <c>refs</c>, danach <c>/items</c> bzw. <c>/refs-from-tags</c>) – siehe
/// <see cref="ErstAnlegenDannFuellen_BleibtMoeglich"/>. Und er gilt nur für item-basierte Typen: ein Aufsatz
/// hat *nie* Items, ein Rechen-Drill erzeugt seine Aufgaben aus Regeln.
/// </summary>
public class EmptyExerciseGuardTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private static async Task<(int subjectId, int chapterId)> ChapterAsync(HttpClient father, string name)
    {
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects", new { name }));
        var chapterId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit", orderIndex = 1 }));
        return (subjectId, chapterId);
    }

    /// <summary>Vokabelübung ohne jedes Wort – der Datenstand, den Anmerkung 13 gemeldet hat.</summary>
    private static async Task<int> EmptyVocabExerciseAsync(HttpClient father, int subjectId, int chapterId) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary",
            new { title = "Einfach Vokabeln", orderIndex = 1, rewardPoints = 10, config = new { direction = "front-to-back" } }));

    private static async Task<int> EmptyPlanAsync(HttpClient father, int childId = 1) =>
        await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/study-plans",
            new { childId, title = "Leer-Guard-Plan", durationDays = 10 }));

    [Fact]
    public async Task LeereVokabeluebung_LaesstSichNichtZuweisen()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (s, c) = await ChapterAsync(father, "Leer-Zuweisen");
        var exerciseId = await EmptyVocabExerciseAsync(father, s, c);
        var planId = await EmptyPlanAsync(father);

        var res = await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, cadence = "Daily" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("exercise_empty", body.GetProperty("code").GetString());
        // Und der Plan bleibt leer – die Position darf nicht halb entstanden sein.
        var positions = await father.GetFromJsonAsync<List<JsonElement>>(
            $"/api/v1/supervisor/study-plans/{planId}/positions");
        Assert.Empty(positions!);
    }

    [Fact]
    public async Task GefuellteVokabeluebung_LaesstSichWeiterZuweisen()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, k1) = await TestApi.CreateStoreVocabAsync(father, "spring", "Frühling");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, k1);
        var planId = await EmptyPlanAsync(father);

        var res = await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, cadence = "Daily" });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        await AssertPositionOnPlanAsync(father, planId, exerciseId);
    }

    /// <summary>
    /// Der Ablauf, den eine Schranke beim Anlegen zerstört hätte: leer anlegen, per Item-Endpunkt füllen,
    /// dann zuweisen. Genau so arbeitet auch <c>refs-from-tags</c>.
    /// </summary>
    [Fact]
    public async Task ErstAnlegenDannFuellen_BleibtMoeglich()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (s, c) = await ChapterAsync(father, "Erst-Leer-Dann-Voll");
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{s}/chapters/{c}/vocabulary",
            new { title = "Wird noch gefüllt", orderIndex = 1, rewardPoints = 10, config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de" } }));

        // Anlegen ohne Wörter ist erlaubt (kein 400) …
        var addRes = await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{s}/chapters/{c}/vocabulary/{exerciseId}/items",
            new { front = "sun", back = "Sonne" });
        Assert.Equal(HttpStatusCode.Created, addRes.StatusCode);

        // Das Wort ist wirklich drin – ein 201 auf den Item-POST sagt darüber nichts.
        var items = await father.GetFromJsonAsync<List<JsonElement>>(
            $"/api/v1/creator/subjects/{s}/chapters/{c}/vocabulary/{exerciseId}/items");
        Assert.Contains(items!, i => i.GetProperty("front").GetString() == "sun");

        // … und nach dem Füllen greift der Riegel nicht mehr.
        var planId = await EmptyPlanAsync(father);
        var res = await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, cadence = "Daily" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        await AssertPositionOnPlanAsync(father, planId, exerciseId);
    }

    /// <summary>
    /// Regressionsschutz: Der Riegel darf nur item-basierte Typen treffen. Ein Aufsatz hat typbedingt keine
    /// Items und bleibt zuweisbar – sonst hätte der Fix eine ganze Lernform unbrauchbar gemacht.
    /// </summary>
    [Fact]
    public async Task Aufsatz_OhneItems_BleibtZuweisbar()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (s, c) = await ChapterAsync(father, "Aufsatz-Zuweisen");
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{s}/chapters/{c}/essays",
            new { title = "Brief über Hobbys", orderIndex = 1, rewardPoints = 10, config = new { prompt = "Schreibe einen Brief.", minWords = 80 } }));
        var planId = await EmptyPlanAsync(father);

        var res = await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, cadence = "Daily" });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        await AssertPositionOnPlanAsync(father, planId, exerciseId);
    }

    /// <summary>
    /// „Zuweisbar" heißt: die Position steht danach auch <b>am Plan</b>. Ein 201 belegt nur, dass der Riegel
    /// nicht zugeschlagen hat – nicht, dass die Zuweisung auf der richtigen Übung landete. Das ist die
    /// Fehlerklasse „Erfolgsstatus zugesichert, Effekt nie nachgelesen" (docs/testplan.md, Etappe 1a).
    /// </summary>
    private static async Task AssertPositionOnPlanAsync(HttpClient father, int planId, int exerciseId)
    {
        var positions = await father.GetFromJsonAsync<List<JsonElement>>(
            $"/api/v1/supervisor/study-plans/{planId}/positions");
        Assert.Contains(positions!, p => p.GetProperty("exerciseId").GetInt32() == exerciseId);
    }

    /// <summary>
    /// Die Vorschau nennt jetzt den Grund: „noch nicht gefüllt" statt des allgemeinen
    /// <c>no_checkable_content</c>, das beim Aufsatz eine Typ-Eigenschaft beschreibt.
    /// </summary>
    [Fact]
    public async Task Vorschau_LeereVokabeluebung_MeldetExerciseEmpty()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (s, c) = await ChapterAsync(father, "Leer-Vorschau");
        var exerciseId = await EmptyVocabExerciseAsync(father, s, c);

        var res = await father.GetAsync($"/api/v1/creator/exercises/{exerciseId}/preview");

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("exercise_empty", body.GetProperty("code").GetString());
    }

    /// <summary>
    /// Ein Snapshot ohne Treffer darf die Übung nicht lautlos leeren – ein vertippter Tag sah vorher wie ein
    /// Erfolg aus und ließ eine Übung ohne Wörter zurück.
    /// </summary>
    [Fact]
    public async Task RefsFromTags_OhneTreffer_LaesstItemsUnberuehrt()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "bridge", "Brücke");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var detail = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}");
        var subjectId = detail.GetProperty("subjectId").GetInt32();
        var chapterId = detail.GetProperty("chapterId").GetInt32();
        var itemsUrl = $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}/items";

        var res = await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}/refs-from-tags",
            new { tags = new[] { "gibt-es-nicht" } });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        // Eigener Code: ein Aufrufer muss „deine Tags treffen nichts" von „du hast keinen Tag geschickt"
        // unterscheiden können – dort hilft ein anderer Tag, hier ein Bugfix.
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("no_tag_matches", body.GetProperty("code").GetString());
        var items = await father.GetFromJsonAsync<List<JsonElement>>(itemsUrl);
        Assert.Single(items!);   // das eine Wort steht weiterhin drin
    }

    [Fact]
    public async Task RefsFromTags_OhneTags_BleibtValidierungsfehler()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "river", "Fluss");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var detail = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}");
        var s = detail.GetProperty("subjectId").GetInt32();
        var c = detail.GetProperty("chapterId").GetInt32();

        var res = await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{s}/chapters/{c}/vocabulary/{exerciseId}/refs-from-tags",
            new { tags = Array.Empty<string>() });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_error", body.GetProperty("code").GetString());
    }
}
