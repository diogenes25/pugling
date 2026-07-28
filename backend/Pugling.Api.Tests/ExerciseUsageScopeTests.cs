using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pugling.Api.Tests;

/// <summary>
/// Verwendungs-Anzeige und Löschprüfung müssen **dieselbe** Frage gleich beantworten.
///
/// Vorher taten sie es nicht: <c>usage</c> filterte auf die eigenen Kinder, die Löschprüfung schaute global.
/// Steckte eine Übung im Lehrplan eines fremd betreuten Kindes, meldete die Anzeige „nirgends" und das
/// Löschen scheiterte trotzdem mit <c>409</c> – der Autor hatte keinen Weg, den Grund zu finden
/// (Anmerkung 14, aufgefallen an einer Übung, die in einem aktiven Plan eines dritten Kontos lag).
///
/// Der Aufbau hier ist genau diese Konstellation: Vater A **besitzt** die Übung, Vater B **betreut** das
/// Kind, das sie in seinem Plan hat.
/// </summary>
public class ExerciseUsageScopeTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private readonly PuglingWebAppFactory _factory = factory;

    private async Task<HttpClient> NewFatherAsync(string name, string pin)
    {
        var id = await TestApi.IdAsync(await _factory.CreateClient()
            .PostAsJsonAsync("/api/v1/supervisor/fathers", new { name, pin }));
        return await TestApi.FatherAsync(_factory, id, pin);
    }

    [Fact]
    public async Task FremdeVerwendung_WirdAlsZahlGenannt_UndBlocktDasLoeschen()
    {
        // Vater A legt die Übung an (und ist damit ihr Owner).
        var ownerPin = "3141";
        var owner = await NewFatherAsync("Übungs-Owner", ownerPin);
        var subjectId = await TestApi.IdAsync(await owner.PostAsJsonAsync("/api/v1/creator/subjects", new { name = $"Scope-Fach {Guid.NewGuid():N}" }));
        var chapterId = await TestApi.IdAsync(await owner.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters", new { name = "Unit", orderIndex = 1 }));
        var exerciseId = await TestApi.IdAsync(await owner.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary",
            new
            {
                title = "Geteilte Wörter",
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    direction = "front-to-back",
                    sourceLang = "en",
                    targetLang = "de",
                    items = new[] { new { front = "bridge", back = "Brücke" } },
                },
            }));

        // Vater B betreut ein eigenes Kind und nimmt die (öffentlich ausführbare) Übung in seinen Plan.
        var other = await NewFatherAsync("Fremder Betreuer", "2718");
        var childId = await TestApi.IdAsync(await other.PostAsJsonAsync(
            "/api/v1/supervisor/children", new { name = "Fremdkind", pin = "4444" }));
        var planId = await TestApi.IdAsync(await other.PostAsJsonAsync(
            "/api/v1/supervisor/study-plans", new { childId, title = "Fremder Plan", durationDays = 10 }));
        (await other.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, cadence = "Daily" })).EnsureSuccessStatusCode();

        // Aus Sicht des Owners: keine EIGENE Verwendung – aber die fremde wird als Zahl genannt.
        var usage = await owner.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/usage");
        Assert.Empty(usage.GetProperty("plans").EnumerateArray());
        Assert.Empty(usage.GetProperty("classTests").EnumerateArray());
        Assert.Equal(1, usage.GetProperty("otherCarersCount").GetInt32());

        // Und das Löschen scheitert – mit einer Meldung, die dieselbe Zahl nennt statt zu schweigen.
        var del = await owner.DeleteAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}");
        Assert.Equal(HttpStatusCode.Conflict, del.StatusCode);
        var problem = await del.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("exercise_in_use", problem.GetProperty("code").GetString());
        var detail = problem.GetProperty("detail").GetString()!;
        Assert.Contains("1 usage outside your care", detail);
        // Und die eigene Seite darf nicht mit auftauchen – der Owner hat hier keine eigene Verwendung.
        Assert.DoesNotContain("of yours", detail);

        // Gegenprobe aus Sicht von Vater B: für ihn ist es eine ganz normale, sichtbare Verwendung.
        var otherUsage = await other.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/usage");
        Assert.Single(otherUsage.GetProperty("plans").EnumerateArray());
        Assert.Equal(0, otherUsage.GetProperty("otherCarersCount").GetInt32());
    }

    [Fact]
    public async Task EigeneVerwendung_WirdWeiterhinBenanntUndNichtAlsFremdGezaehlt()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "castle", "Burg");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var planId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            "/api/v1/supervisor/study-plans", new { childId = 1, title = "Eigener Plan", durationDays = 10 }));
        (await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId, cadence = "Daily" })).EnsureSuccessStatusCode();

        var usage = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/usage");
        Assert.Single(usage.GetProperty("plans").EnumerateArray());
        // Der Kern der Trennung: die eigene Verwendung darf NICHT als „fremd" mitgezählt werden.
        Assert.Equal(0, usage.GetProperty("otherCarersCount").GetInt32());
    }

    [Fact]
    public async Task UnbenutzteUebung_MeldetNullUndLaesstSichLoeschen()
    {
        var father = await TestApi.FatherAsync(_factory);
        var (_, key) = await TestApi.CreateStoreVocabAsync(father, "harbour", "Hafen");
        var exerciseId = await TestApi.CreateVocabRefExerciseAsync(father, key);
        var detail = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}");
        var s = detail.GetProperty("subjectId").GetInt32();
        var c = detail.GetProperty("chapterId").GetInt32();

        var usage = await father.GetFromJsonAsync<JsonElement>($"/api/v1/creator/exercises/{exerciseId}/usage");
        Assert.Equal(0, usage.GetProperty("otherCarersCount").GetInt32());

        var del = await father.DeleteAsync($"/api/v1/creator/subjects/{s}/chapters/{c}/vocabulary/{exerciseId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }
}
