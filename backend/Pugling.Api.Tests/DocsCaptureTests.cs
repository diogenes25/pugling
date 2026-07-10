using System.Net;
using System.Net.Http.Json;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;
using Pugling.Api.OpenApi;

namespace Pugling.Api.Tests;

/// <summary>
/// Integrationstest-gesteuerte „Capture-Harness": fährt die echte API mit geseedeten Zugangsdaten,
/// prüft je Antwort HTTP-Status UND maschinenlesbaren Fehler-<c>code</c> und schreibt verifizierte
/// Request/Response-Beispielpaare als Markdown unter <c>docs/api-examples/</c>. Ist zugleich CI-Gate
/// (jede fehlgeschlagene Erwartung lässt den Test rot werden) und Doku-Generator: die Beispiele sind
/// per Definition korrekt, weil sie erst nach bestandener Assertion aufgezeichnet werden.
/// </summary>
public class DocsCaptureTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static readonly JsonSerializerOptions Indented = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Ein aufgezeichnetes Request/Response-Paar (kein echtes Token – Bearer wird maskiert).</summary>
    private sealed record Entry(string ResourceGroup, string Title, string Method, string Path, string Role,
        string? RequestBodyJson, int ExpectedStatus, int ActualStatus, string? ResponseBodyJson, bool IsError);

    private readonly List<Entry> _entries = [];
    // code → (Gruppe, Titel) der ersten Aufzeichnung, die diesen Code verifiziert hat (Abdeckungs-Report).
    private readonly Dictionary<string, (string Group, string Title)> _codeHits = [];

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  Der eine Capture-Helfer: sendet, prüft Status (+ optional code), zeichnet auf, liefert den Body.
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    private async Task<JsonElement> Capture(HttpClient client, string group, string title, HttpMethod method,
        string path, object? body, HttpStatusCode expectedStatus, string? expectedCode = null)
    {
        using var req = new HttpRequestMessage(method, path);
        string? requestJson = null;
        if (body is not null)
        {
            requestJson = JsonSerializer.Serialize(body, Indented);
            req.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        }

        var res = await client.SendAsync(req);
        var raw = await res.Content.ReadAsStringAsync();

        JsonElement bodyEl = default;
        string? responseJson = null;
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                bodyEl = JsonSerializer.Deserialize<JsonElement>(raw);
                responseJson = Redact(JsonSerializer.Serialize(bodyEl, Indented));
            }
            catch
            {
                responseJson = Redact(raw);
            }
        }

        // CI-Gate: Status muss stimmen …
        Assert.True(expectedStatus == res.StatusCode,
            $"[{group}] {title}: erwartet HTTP {(int)expectedStatus}, war {(int)res.StatusCode}. Body: {raw}");

        // … und – falls gefordert – der maschinenlesbare code.
        if (expectedCode is not null)
        {
            var code = bodyEl.ValueKind == JsonValueKind.Object && bodyEl.TryGetProperty("code", out var c)
                ? c.GetString()
                : null;
            Assert.True(code == expectedCode,
                $"[{group}] {title}: erwartet code '{expectedCode}', war '{code}' (HTTP {(int)res.StatusCode}). Body: {raw}");
            _codeHits.TryAdd(expectedCode, (group, title));
        }

        var isError = (int)expectedStatus >= 400;
        _entries.Add(new Entry(group, title, method.Method, path, RoleOf(client),
            requestJson, (int)expectedStatus, (int)res.StatusCode, responseJson, isError));
        return bodyEl;
    }

    /// <summary>Leitet die Rolle aus dem Bearer-Token ab (nur zur Doku – das Token selbst wird nie geschrieben).</summary>
    private static string RoleOf(HttpClient client)
    {
        var auth = client.DefaultRequestHeaders.Authorization;
        if (auth?.Parameter is not { } token) return "anonymous";
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return "authenticated";
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            if (json.Contains("\"Supervisor\"")) return "father";
            if (json.Contains("\"Student\"")) return "child";
            return "authenticated";
        }
        catch
        {
            return "authenticated";
        }
    }

    private static string Redact(string s)
    {
        var redacted = Regex.Replace(s, "(\"token\"\\s*:\\s*)\"[^\"]*\"", "$1\"<redacted-jwt>\"");
        redacted = Regex.Replace(redacted, "(\"traceId\"\\s*:\\s*)\"[^\"]*\"", "$1\"<trace-id>\"");
        return Regex.Replace(redacted,
            "\"\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}(?:\\.\\d+)?Z\"",
            "\"<timestamp>\"");
    }

    private static string? Truncate(string? s) =>
        s is { Length: > 1500 } ? s[..1500] + "\n… (gekürzt)" : s;

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  Direkte DB-Manipulation (kein API-Weg): Gems (Achievement→Gems) bzw. Münzen (Base→Coins) gutschreiben.
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    private async Task GrantAsync(int childId, int amount, PointKind kind, string reason)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.ChildPoints.Add(new ChildPointsEntry { ChildId = childId, Amount = amount, Kind = kind, Reason = reason });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CaptureAll()
    {
        var completed = false;
        try
        {
            var anon = factory.CreateClient();
            var father = await TestApi.FatherAsync(factory);       // Papa (id 1 / PIN 0000)
            var child = await TestApi.ChildAsync(factory);         // Sohn (id 1 / PIN 1111)

            // Zweiter Vater (anonyme Registrierung) für Cross-Ownership-404/403.
            var father2Id = await TestApi.IdAsync(await anon.PostAsJsonAsync("/api/v1/supervisor/fathers", new { name = "Zweiter Papa", pin = "2222" }));
            var father2 = await TestApi.FatherAsync(factory, father2Id, "2222");
            var foreignChildId = await TestApi.IdAsync(await father2.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Fremdes Kind", pin = "3333" }));

            await CaptureAuthAsync(anon);
            await CaptureChildrenAsync(father, father2, foreignChildId);
            var (docSubjectId, docChapterId, docExerciseId) = await CaptureCatalogAsync(father);
            await CaptureMeAsync(father, child);
            await CaptureStudyPlansAsync(father, father2, child, docSubjectId, docChapterId, docExerciseId);
            await CaptureClassTestsAsync(father);
            await CaptureVocabularyAsync(father);
            await CaptureTagsAsync(father, child, foreignChildId);
            await CaptureTimetableAsync(father, docSubjectId);
            await CaptureShopAsync(father, child);
            completed = true;
        }
        finally
        {
            // Auch bei Teil-Läufen schreiben (erleichtert das Debuggen); rote Assertions lassen den Test dennoch fehlschlagen.
            if (_entries.Count > 0) WriteMarkdown();
            if (completed) WriteOpenApiExamples();
        }
    }

    // ── auth ────────────────────────────────────────────────────────────────────────────────────
    private async Task CaptureAuthAsync(HttpClient anon)
    {
        const string g = "auth";
        await Capture(anon, g, "Vater registrieren (anonym)", HttpMethod.Post, "/api/v1/supervisor/fathers",
            new { name = "Neuer Papa", pin = "1234" }, HttpStatusCode.Created);
        await Capture(anon, g, "Vater-Login", HttpMethod.Post, "/api/v1/auth/father",
            new { fatherId = 1, pin = "0000" }, HttpStatusCode.OK);
        await Capture(anon, g, "Sohn-Login", HttpMethod.Post, "/api/v1/auth/child",
            new { childId = 1, pin = "1111" }, HttpStatusCode.OK);
        await Capture(anon, g, "Login mit falscher PIN", HttpMethod.Post, "/api/v1/auth/father",
            new { fatherId = 1, pin = "9998" }, HttpStatusCode.Unauthorized, ApiErrors.InvalidCredentials.Code);
        await Capture(anon, g, "Login mit nicht-numerischer fatherId", HttpMethod.Post, "/api/v1/auth/father",
            new { fatherId = "1a", pin = "0000" }, HttpStatusCode.BadRequest, ApiErrors.ValidationError.Code);
        await Capture(anon, g, "Selbstauskunft ohne Token", HttpMethod.Get, "/api/v1/auth/me",
            null, HttpStatusCode.Unauthorized, ApiErrors.Unauthorized.Code);
    }

    // ── children ────────────────────────────────────────────────────────────────────────────────
    private async Task CaptureChildrenAsync(HttpClient father, HttpClient father2, int foreignChildId)
    {
        const string g = "children";
        await Capture(father, g, "Eigene Kinder auflisten", HttpMethod.Get, "/api/v1/supervisor/children", null, HttpStatusCode.OK);
        var created = await Capture(father, g, "Kind anlegen", HttpMethod.Post, "/api/v1/supervisor/children",
            new { name = "Doku-Kind", pin = "4242" }, HttpStatusCode.Created);
        var childId = created.GetProperty("id").GetInt32();

        await Capture(father, g, "Kind ohne Namen anlegen", HttpMethod.Post, "/api/v1/supervisor/children",
            new { name = "", pin = "0000" }, HttpStatusCode.BadRequest, ApiErrors.ValidationError.Code);
        await Capture(father, g, "Einzelnes Kind lesen", HttpMethod.Get, $"/api/v1/supervisor/children/{childId}", null, HttpStatusCode.OK);
        await Capture(father, g, "Kind ändern (Klassenstufe)", HttpMethod.Patch, $"/api/v1/supervisor/children/{childId}",
            new { grade = 4 }, HttpStatusCode.OK);
        await Capture(father, g, "Fremdes Kind lesen", HttpMethod.Get, $"/api/v1/supervisor/children/{foreignChildId}",
            null, HttpStatusCode.NotFound, ApiErrors.NotFound.Code);
        await Capture(father, g, "Kind löschen", HttpMethod.Delete, $"/api/v1/supervisor/children/{childId}", null, HttpStatusCode.NoContent);
    }

    // ── catalog ─────────────────────────────────────────────────────────────────────────────────
    private async Task<(int subjectId, int chapterId, int exerciseId)> CaptureCatalogAsync(HttpClient father)
    {
        const string g = "catalog";
        var subject = await Capture(father, g, "Fach anlegen", HttpMethod.Post, "/api/v1/creator/subjects",
            new { name = "Doku-Fach" }, HttpStatusCode.Created);
        var subjectId = subject.GetProperty("id").GetInt32();

        await Capture(father, g, "Fach ohne Namen anlegen", HttpMethod.Post, "/api/v1/creator/subjects",
            new { name = "" }, HttpStatusCode.BadRequest, ApiErrors.ValidationError.Code);

        var chapter = await Capture(father, g, "Kapitel anlegen", HttpMethod.Post, $"/api/v1/creator/subjects/{subjectId}/chapters",
            new { name = "Kapitel 1", orderIndex = 1 }, HttpStatusCode.Created);
        var chapterId = chapter.GetProperty("id").GetInt32();

        // Die Übung wird als Hülle angelegt (nur Einstellungen); die Vokabelpaare kommen darunter über den
        // Item-Endpunkt als eigene Sub-Ressource (Items sind eine eigene Ebene, siehe VocabularyController).
        var exercise = await Capture(father, g, "Vokabel-Übung anlegen", HttpMethod.Post,
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary",
            new
            {
                title = "Begrüßungen",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de" },
            }, HttpStatusCode.Created);
        var exerciseId = exercise.GetProperty("id").GetInt32();

        // Vokabelpaar per Item-EP: inline über front/back – ohne vocabularyId wird die Vokabel im Store angelegt.
        // (Mit vocabularyId genügt die ID; Front/Back kämen dann aus dem Store.) Front/Back sind optional.
        await Capture(father, g, "Vokabelpaar hinzufügen", HttpMethod.Post,
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}/items",
            new { front = "hello", back = "hallo" }, HttpStatusCode.Created);
        // Zweites Paar direkt anlegen (nicht als Beispiel), damit die Übung zwei Items für den Spielfluss trägt.
        (await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}/items",
            new { front = "goodbye", back = "tschüss" })).EnsureSuccessStatusCode();

        await Capture(father, g, "Unbekannte Übung lesen", HttpMethod.Get, "/api/v1/creator/exercises/999999",
            null, HttpStatusCode.NotFound, ApiErrors.NotFound.Code);

        await Capture(father, g, "Art (Kategorie) anlegen", HttpMethod.Post, $"/api/v1/creator/subjects/{subjectId}/categories",
            new { name = "Vokabeln" }, HttpStatusCode.Created);
        await Capture(father, g, "Doppelte Art anlegen", HttpMethod.Post, $"/api/v1/creator/subjects/{subjectId}/categories",
            new { name = "Vokabeln" }, HttpStatusCode.Conflict, ApiErrors.Conflict.Code);

        // Übung, die in einem Lehrplan steckt, lässt sich nicht löschen (Positions-Referenz → 409).
        TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText);
        await Capture(father, g, "Verwendete Übung löschen", HttpMethod.Delete,
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}",
            null, HttpStatusCode.Conflict, ApiErrors.ExerciseInUse.Code);

        // Fremd-Autor-Übung (Lehrer-Bibliothek, AuthorFatherId = Lehrer) bearbeiten → 403 not_author.
        var foreign = await FindForeignAuthoredExerciseAsync(father);
        if (foreign is { } ex)
            await Capture(father, g, "Fremd-Autor-Übung bearbeiten", HttpMethod.Put,
                $"/api/v1/creator/subjects/{ex.SubjectId}/chapters/{ex.ChapterId}/vocabulary/{ex.Id}",
                new { title = "Übernahmeversuch", orderIndex = 1, rewardPoints = 1, config = new { } },
                HttpStatusCode.Forbidden, ApiErrors.NotAuthor.Code);

        return (subjectId, chapterId, exerciseId);
    }

    private sealed record ForeignExercise(int Id, int SubjectId, int ChapterId);

    /// <summary>Sucht im Katalog eine Vokabel-Übung mit fremdem Autor (≠ Papa, ≠ System) für den not_author-Fall.</summary>
    private static async Task<ForeignExercise?> FindForeignAuthoredExerciseAsync(HttpClient father)
    {
        var list = await father.GetFromJsonAsync<List<JsonElement>>("/api/v1/creator/exercises?type=Vocabulary&take=500");
        foreach (var e in list ?? [])
        {
            var author = e.TryGetProperty("authorFatherId", out var a) && a.ValueKind == JsonValueKind.Number ? a.GetInt32() : (int?)null;
            var isOwn = e.TryGetProperty("isOwn", out var o) && o.GetBoolean();
            if (author is { } id && id != 1 && !isOwn)
                return new ForeignExercise(e.GetProperty("id").GetInt32(),
                    e.GetProperty("subjectId").GetInt32(), e.GetProperty("chapterId").GetInt32());
        }
        return null;
    }

    // ── me (Sohn) + Angebots-/Skin-Ökonomie ──────────────────────────────────────────────────────
    private async Task CaptureMeAsync(HttpClient father, HttpClient child)
    {
        const string g = "me";

        // Lese-Sichten des geseedeten Sohns (id 1) – realistische Daten (Missionen/Skins/Angebote geseedet).
        await Capture(child, g, "Eigener Kontostand (Wallet)", HttpMethod.Get, "/api/v1/student/me/points", null, HttpStatusCode.OK);

        // Buchungen liegen eine Ebene tiefer: Liste + Einzelansicht. Eine deterministische Buchung anlegen.
        await GrantAsync(1, 15, PointKind.Base, "Doku-Buchung");
        var pointEntries = await Capture(child, g, "Eigene Buchungen (Liste)", HttpMethod.Get,
            "/api/v1/student/me/points/entries", null, HttpStatusCode.OK);
        var entryId = pointEntries.EnumerateArray().First().GetProperty("id").GetInt32();
        await Capture(child, g, "Einzelne Buchung", HttpMethod.Get,
            $"/api/v1/student/me/points/entries/{entryId}", null, HttpStatusCode.OK);
        var missions = await Capture(child, g, "Eigene Missionen (Liste)", HttpMethod.Get, "/api/v1/student/me/missions", null, HttpStatusCode.OK);
        var missionId = missions.EnumerateArray().First().GetProperty("id").GetInt32();
        await Capture(child, g, "Einzelne Mission", HttpMethod.Get, $"/api/v1/student/me/missions/{missionId}", null, HttpStatusCode.OK);

        var achievements = await Capture(child, g, "Eigene Auszeichnungen (Liste)", HttpMethod.Get, "/api/v1/student/me/achievements", null, HttpStatusCode.OK);
        var achievementId = achievements.EnumerateArray().First().GetProperty("id").GetInt32();
        await Capture(child, g, "Einzelne Auszeichnung", HttpMethod.Get, $"/api/v1/student/me/achievements/{achievementId}", null, HttpStatusCode.OK);

        await Capture(child, g, "Eigener Skin-Zustand", HttpMethod.Get, "/api/v1/student/me/skins", null, HttpStatusCode.OK);

        await Capture(father, g, "Vater greift auf Sohn-Route zu", HttpMethod.Get, "/api/v1/student/me/points",
            null, HttpStatusCode.Forbidden, ApiErrors.Forbidden.Code);

        await Capture(child, g, "Bereits besessenen Skin kaufen", HttpMethod.Post, "/api/v1/student/me/skins/pug/purchase",
            new { }, HttpStatusCode.Conflict, ApiErrors.SkinAlreadyUnlocked.Code);

        // Frisches Kind A: deterministische Salden für die Kauf-/Ausrüst-Fälle.
        var childAId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Ökonomie-Kind A", pin = "5001" }));
        var childA = await TestApi.ChildAsync(factory, childAId, "5001");

        await Capture(childA, g, "Skin kaufen ohne Gems", HttpMethod.Post, "/api/v1/student/me/skins/fox/purchase",
            new { }, HttpStatusCode.BadRequest, ApiErrors.InsufficientGems.Code);
        await Capture(childA, g, "Unbekannten Skin kaufen", HttpMethod.Post, "/api/v1/student/me/skins/banane/purchase",
            new { }, HttpStatusCode.NotFound, ApiErrors.NotFound.Code);

        await GrantAsync(childAId, 2500, PointKind.Achievement, "Doku-Gems");
        await Capture(childA, g, "Skin kaufen (mit Gems)", HttpMethod.Post, "/api/v1/student/me/skins/ninja/purchase",
            new { }, HttpStatusCode.OK);
        await Capture(childA, g, "Besessenen Skin ausrüsten", HttpMethod.Post, "/api/v1/student/me/skins/pug/equip",
            new { }, HttpStatusCode.OK);
        await Capture(childA, g, "Nicht besessenen Skin ausrüsten", HttpMethod.Post, "/api/v1/student/me/skins/fox/equip",
            new { }, HttpStatusCode.BadRequest, ApiErrors.SkinNotUnlocked.Code);
    }

    // ── study-plans / positions / practice / tests ────────────────────────────────────────────────
    private async Task CaptureStudyPlansAsync(HttpClient father, HttpClient father2, HttpClient child,
        int docSubjectId, int docChapterId, int docExerciseId)
    {
        const string g = "study-plans";

        var plan = await Capture(father, g, "Lehrplan anlegen", HttpMethod.Post, "/api/v1/supervisor/study-plans",
            new { childId = 1, title = "Doku-Lehrplan", durationDays = 10 }, HttpStatusCode.Created);
        var planId = plan.GetProperty("id").GetInt32();

        var pos = await Capture(father, g, "Position anlegen", HttpMethod.Post, $"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId = docExerciseId, useLeitner = true, stage = (int)TestStage.FreeText, cadence = "Daily" },
            HttpStatusCode.Created);
        var positionId = pos.GetProperty("id").GetInt32();

        await Capture(father, g, "Position mit unbekannter Übung", HttpMethod.Post, $"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId = 999999 }, HttpStatusCode.BadRequest, ApiErrors.InvalidReference.Code);

        await Capture(father, g, "Unbekannten Lehrplan lesen", HttpMethod.Get, "/api/v1/supervisor/study-plans/999999",
            null, HttpStatusCode.NotFound, ApiErrors.NotFound.Code);

        // Sohn übt (Lern-Modus, server-geführt): Sitzung starten, eine Karte bewerten (serverseitige Prüfung –
        // die Antwort trägt bereits die nächste Karte + Abschluss-Signal), optional die nächste Karte per /next holen.
        var session = await Capture(child, g, "Übungssitzung starten (Lern-Modus)", HttpMethod.Post,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions", new { mode = "Lern" }, HttpStatusCode.Created);
        var sessionId = session.GetProperty("id").GetInt32();
        await Capture(child, g, "Nächste Karte (server-geführter Cursor)", HttpMethod.Get,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions/{sessionId}/next", null, HttpStatusCode.OK);
        await Capture(child, g, "Karte bewerten (Review, mit nächster Karte)", HttpMethod.Post,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions/{sessionId}/review",
            new { itemIndex = 0, givenAnswer = "hallo" }, HttpStatusCode.OK);

        // Info-Modus (freies Üben): alle Karten am Stück, aber /review schreibt kein Feedback (204).
        var infoSession = await Capture(child, g, "Übungssitzung starten (Info-Modus, freies Üben)", HttpMethod.Post,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions", new { mode = "Info" }, HttpStatusCode.Created);
        var infoSessionId = infoSession.GetProperty("id").GetInt32();
        await Capture(child, g, "Karten am Stück (Info-Modus/Offline-Batch)", HttpMethod.Get,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions/{infoSessionId}/cards", null, HttpStatusCode.OK);
        await Capture(child, g, "Review im Info-Modus (kein Feedback → 204)", HttpMethod.Post,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions/{infoSessionId}/review",
            new { itemIndex = 0, givenAnswer = "hallo" }, HttpStatusCode.NoContent);

        // Abschlusstest = Klausur (strikt server-getrieben): starten (nur Metadaten), Frage einzeln holen,
        // beantworten (ohne Korrektheit), abschließen (auswerten), erneut abgeben (→ test_already_submitted).
        var attempt = await Capture(child, g, "Test starten (Klausur, ohne Aufgaben-Bulk)", HttpMethod.Post,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests", new { }, HttpStatusCode.Created);
        var attemptId = attempt.GetProperty("attemptId").GetInt32();
        await Capture(child, g, "Nächste Prüfungsfrage (One-at-a-time)", HttpMethod.Get,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests/{attemptId}/next", null, HttpStatusCode.OK);
        await Capture(child, g, "Prüfungsantwort abgeben (ohne Korrektheit)", HttpMethod.Post,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests/{attemptId}/answer",
            new { givenAnswer = "hallo" }, HttpStatusCode.OK);
        // Restliche Fragen beantworten (nicht abgebildet), damit der Versuch vollständig ist.
        await child.PostAsJsonAsync($"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests/{attemptId}/answer",
            new { givenAnswer = "tschüss" });
        await Capture(child, g, "Test abgeben (auswerten)", HttpMethod.Post,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests/{attemptId}/submit", new { }, HttpStatusCode.OK);
        await Capture(child, g, "Test erneut abgeben", HttpMethod.Post,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests/{attemptId}/submit", new { },
            HttpStatusCode.BadRequest, ApiErrors.TestAlreadySubmitted.Code);

        // Tagesmission + Verlauf. Der Verlauf unterstützt Paging (skip/take, X-Total-Count),
        // Sortierung (day/-day/points/-points) und Filter (from/to, dutyDone).
        await Capture(child, g, "Tagesmission (Overview)", HttpMethod.Get,
            $"/api/v1/student/study-plans/{planId}/overview", null, HttpStatusCode.OK);
        await Capture(child, g, "Verlauf – Paging & Sortierung (neueste zuerst)", HttpMethod.Get,
            $"/api/v1/student/study-plans/{planId}/overview/progress?take=3&sort=-day", null, HttpStatusCode.OK);
        await Capture(child, g, "Verlauf – nur erledigte Tage", HttpMethod.Get,
            $"/api/v1/student/study-plans/{planId}/overview/progress?dutyDone=true", null, HttpStatusCode.OK);

        // Test auf einer Leseübung ohne prüfbaren Inhalt → no_checkable_content.
        var reading = await father.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{docSubjectId}/chapters/{docChapterId}/reading",
            new { title = "Leseverstehen (leer)", orderIndex = 2, rewardPoints = 5, config = new { text = "A short text without questions.", questions = Array.Empty<object>() } });
        reading.EnsureSuccessStatusCode();
        var readingExerciseId = (await reading.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var readingPos = await father.PostAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId = readingExerciseId, useLeitner = false, stage = (int)TestStage.FreeText });
        readingPos.EnsureSuccessStatusCode();
        var readingPosId = (await readingPos.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        await Capture(child, g, "Test auf Übung ohne prüfbaren Inhalt", HttpMethod.Post,
            $"/api/v1/student/study-plans/{planId}/positions/{readingPosId}/tests", new { },
            HttpStatusCode.BadRequest, ApiErrors.NoCheckableContent.Code);

        // Bespielte Position löschen → position_has_data.
        await Capture(father, g, "Bespielte Position löschen", HttpMethod.Delete,
            $"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}", null,
            HttpStatusCode.Conflict, ApiErrors.PositionHasData.Code);

        // Plan deaktivieren → der Sohn kann ihn nicht mehr spielen (plan_inactive).
        (await father.PatchAsJsonAsync($"/api/v1/supervisor/study-plans/{planId}", new { active = false })).EnsureSuccessStatusCode();
        await Capture(child, g, "Deaktivierten Plan spielen", HttpMethod.Post,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions", new { },
            HttpStatusCode.Forbidden, ApiErrors.PlanInactive.Code);
    }

    // ── class-tests ────────────────────────────────────────────────────────────────────────────────
    private async Task CaptureClassTestsAsync(HttpClient father)
    {
        const string g = "class-tests";
        await Capture(father, g, "Klassenarbeit planen", HttpMethod.Post, "/api/v1/supervisor/class-tests",
            new { childId = 1, title = "Vokabeltest Unit 5", scheduledDate = "2099-03-01" }, HttpStatusCode.Created);
        await Capture(father, g, "Note außerhalb des Bereichs", HttpMethod.Post, "/api/v1/supervisor/class-tests",
            new { childId = 1, title = "Ungültige Note", scheduledDate = "2099-03-01", grade = 9.0 },
            HttpStatusCode.BadRequest, ApiErrors.ValidationError.Code);
        await Capture(father, g, "Unbekannte Übung zuweisen", HttpMethod.Post, "/api/v1/supervisor/class-tests",
            new { childId = 1, title = "Unbekannte Übung", scheduledDate = "2099-03-01", exerciseIds = new[] { 999999 } },
            HttpStatusCode.BadRequest, ApiErrors.InvalidReference.Code);
    }

    // ── vocabulary (Store) ──────────────────────────────────────────────────────────────────────────
    private async Task CaptureVocabularyAsync(HttpClient father)
    {
        const string g = "vocabulary";
        var dto = new { key = "en_doku_de_beispiel", sourceLanguage = "en", targetLanguage = "de", word = "example", translation = "Beispiel", partOfSpeech = "Noun" };
        await Capture(father, g, "Vokabel anlegen", HttpMethod.Post, "/api/v1/creator/vocabulary", dto, HttpStatusCode.Created);
        await Capture(father, g, "Vokabel mit doppeltem Key", HttpMethod.Post, "/api/v1/creator/vocabulary", dto,
            HttpStatusCode.Conflict, ApiErrors.DuplicateKey.Code);

        // Geseedete Grundform (Basis flektierter Formen) lässt sich nicht löschen → vocabulary_in_use.
        var baseForm = await Capture(father, g, "Grundform-Vokabel lesen", HttpMethod.Get,
            "/api/v1/creator/vocabulary/by-key/en_go_de_gehen", null, HttpStatusCode.OK);
        var baseId = baseForm.GetProperty("id").GetInt32();
        await Capture(father, g, "Verwendete Grundform löschen", HttpMethod.Delete, $"/api/v1/creator/vocabulary/{baseId}",
            null, HttpStatusCode.Conflict, ApiErrors.VocabularyInUse.Code);
    }

    // ── tags ──────────────────────────────────────────────────────────────────────────────────────
    private async Task CaptureTagsAsync(HttpClient father, HttpClient child, int foreignChildId)
    {
        const string g = "tags";
        var tag = await Capture(father, g, "Tag anlegen (Vater)", HttpMethod.Post, "/api/v1/creator/tags",
            new { childId = 1, name = "Doku-Tag", color = "#3b82f6" }, HttpStatusCode.Created);
        var tagId = tag.GetProperty("id").GetInt32();

        await Capture(child, g, "Tag anlegen (Sohn)", HttpMethod.Post, "/api/v1/creator/tags",
            new { childId = 1, name = "Sohn-Tag", color = "#22c55e" }, HttpStatusCode.Created);

        await Capture(father, g, "Tag mit doppeltem Namen", HttpMethod.Post, "/api/v1/creator/tags",
            new { childId = 1, name = "Doku-Tag" }, HttpStatusCode.BadRequest, ApiErrors.DuplicateTagName.Code);

        await Capture(father, g, "Tag für fremdes Kind anlegen", HttpMethod.Post, "/api/v1/creator/tags",
            new { childId = foreignChildId, name = "Fremd" }, HttpStatusCode.Forbidden, ApiErrors.Forbidden.Code);

        await Capture(father, g, "Unbekannte Übungen taggen", HttpMethod.Post, $"/api/v1/creator/tags/{tagId}/exercises",
            new { exerciseIds = new[] { 999999 } }, HttpStatusCode.BadRequest, ApiErrors.InvalidReference.Code);
    }

    // ── timetable ────────────────────────────────────────────────────────────────────────────────
    private async Task CaptureTimetableAsync(HttpClient father, int docSubjectId)
    {
        const string g = "timetable";
        await Capture(father, g, "Stundenplan-Eintrag anlegen", HttpMethod.Post, "/api/v1/supervisor/children/1/timetable",
            new { subjectId = docSubjectId, dayOfWeek = "Tuesday", timeOfDay = "Nachmittag" }, HttpStatusCode.Created);
        await Capture(father, g, "Gleiches Fach am selben Wochentag", HttpMethod.Post, "/api/v1/supervisor/children/1/timetable",
            new { subjectId = docSubjectId, dayOfWeek = "Tuesday", timeOfDay = "Vormittag" },
            HttpStatusCode.Conflict, ApiErrors.TimetableSlotTaken.Code);
    }

    // ── shop (Vater-Admin + Sohn-Seite) ──────────────────────────────────────────────────────────
    private async Task CaptureShopAsync(HttpClient father, HttpClient child)
    {
        const string g = "shop";

        // ── Artikel-CRUD ──────────────────────────────────────────────────────
        var articleEl = await Capture(father, g, "Artikel anlegen", HttpMethod.Post, "/api/v1/supervisor/shop/articles",
            new
            {
                articleNumber = "TV-900",
                title = "Fernsehzeit",
                description = "Bildschirmzeit in Minuten",
                unitType = "Minute",
                actionType = "TV"
            }, HttpStatusCode.Created);
        var articleId = articleEl.GetProperty("id").GetInt32();

        await Capture(father, g, "Artikel mit doppelter Nummer anlegen", HttpMethod.Post, "/api/v1/supervisor/shop/articles",
            new { articleNumber = "TV-900", title = "Duplikat", unitType = "Minute", actionType = "TV" },
            HttpStatusCode.Conflict, ApiErrors.DuplicateKey.Code);

        await Capture(father, g, "Artikel auflisten", HttpMethod.Get, "/api/v1/supervisor/shop/articles",
            null, HttpStatusCode.OK);

        await Capture(father, g, "Artikel auflisten (Suche)", HttpMethod.Get, "/api/v1/supervisor/shop/articles?search=Fernseh",
            null, HttpStatusCode.OK);

        await Capture(father, g, "Artikel ändern", HttpMethod.Patch, $"/api/v1/supervisor/shop/articles/{articleId}",
            new { title = "Fernsehzeit (30 Min)", description = "30 Minuten freie Bildschirmzeit" },
            HttpStatusCode.OK);

        // ── Angebots-CRUD ─────────────────────────────────────────────────────
        var listingEl = await Capture(father, g, "Angebot anlegen", HttpMethod.Post,
            $"/api/v1/supervisor/shop/articles/{articleId}/listings",
            new
            {
                title = "30 Min Fernsehen",
                description = "Einmalige Halbstunde",
                coinPrice = 120,
                gemPrice = 0,
                unitsPerPurchase = 30,
                currentStock = 5,
                maxStock = 5
            },
            HttpStatusCode.Created);
        var listingId = listingEl.GetProperty("id").GetInt32();

        await Capture(father, g, "Angebot anlegen (ungültiger Preis)", HttpMethod.Post,
            $"/api/v1/supervisor/shop/articles/{articleId}/listings",
            new { coinPrice = 0, gemPrice = 0, unitsPerPurchase = 30, currentStock = 5, maxStock = 5 },
            HttpStatusCode.BadRequest, ApiErrors.ValidationError.Code);

        await Capture(father, g, "Angebote auflisten", HttpMethod.Get,
            $"/api/v1/supervisor/shop/articles/{articleId}/listings", null, HttpStatusCode.OK);

        await Capture(father, g, "Angebot ändern (Bestand auffüllen)", HttpMethod.Patch,
            $"/api/v1/supervisor/shop/articles/{articleId}/listings/{listingId}",
            new { currentStock = 5, maxStock = 10 }, HttpStatusCode.OK);

        // ── Sohn kauft + Aktivierungsanfrage ──────────────────────────────────
        var shopChildId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Shop-Doku-Kind", pin = "7001" }));
        var shopChild = await TestApi.ChildAsync(factory, shopChildId, "7001");

        // Münzen gutschreiben (über Points-Endpunkt des Vaters)
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{shopChildId}/points",
            new { amount = 300, reason = "Doku-Münzen" })).EnsureSuccessStatusCode();

        await Capture(shopChild, g, "Shop-Sicht (Sohn)", HttpMethod.Get, "/api/v1/student/me/shop",
            null, HttpStatusCode.OK);

        var purchaseView = await Capture(shopChild, g, "Shop-Angebot kaufen", HttpMethod.Post,
            $"/api/v1/student/me/shop/listings/{listingId}/purchase", new { }, HttpStatusCode.OK);
        var purchaseId = purchaseView.GetProperty("purchases").EnumerateArray().First().GetProperty("id").GetInt32();

        // Leeres-Lager-Szenario: neues Listing mit stock=0 anlegen, dann kaufen → shop_insufficient_stock
        var emptyListingEl = await father.PostAsJsonAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings",
            new { coinPrice = 50, gemPrice = 0, unitsPerPurchase = 10, currentStock = 0, maxStock = 1 });
        emptyListingEl.EnsureSuccessStatusCode();
        var emptyListingId = (await emptyListingEl.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        await Capture(shopChild, g, "Shop-Angebot kaufen (ausverkauft)", HttpMethod.Post,
            $"/api/v1/student/me/shop/listings/{emptyListingId}/purchase", new { },
            HttpStatusCode.Conflict, ApiErrors.ShopInsufficientStock.Code);

        // Deaktiviertes Listing → shop_listing_inactive
        (await father.PatchAsJsonAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings/{emptyListingId}",
            new { active = false })).EnsureSuccessStatusCode();
        await Capture(shopChild, g, "Shop-Angebot kaufen (deaktiviert)", HttpMethod.Post,
            $"/api/v1/student/me/shop/listings/{emptyListingId}/purchase", new { },
            HttpStatusCode.BadRequest, ApiErrors.ShopListingInactive.Code);

        // Ohne Deckung: frisches Kind (0 Münzen) kauft ein aktives, vorrätiges Angebot → insufficient_coins.
        var brokeChildId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Shop-Doku-Kind (pleite)", pin = "7009" }));
        var brokeChild = await TestApi.ChildAsync(factory, brokeChildId, "7009");
        await Capture(brokeChild, g, "Shop-Angebot kaufen (kein Guthaben)", HttpMethod.Post,
            $"/api/v1/student/me/shop/listings/{listingId}/purchase", new { },
            HttpStatusCode.BadRequest, ApiErrors.InsufficientCoins.Code);

        // Aktivierungsanfragen: der Sohn beantragt Einheiten aus seinem Inventar (30 verfügbar).
        var activation1El = await Capture(shopChild, g, "Aktivierungsanfrage stellen", HttpMethod.Post,
            $"/api/v1/student/me/shop/inventory/{articleId}/activate",
            new { quantity = 30 }, HttpStatusCode.OK);
        var activationId = activation1El.GetProperty("id").GetInt32();

        // Zweite Anfrage (10 Einheiten) – zum Anfragezeitpunkt gegen das aggregierte Inventar geprüft (30 >= 10);
        // die verbindliche Deckungsprüfung erfolgt erst bei der Genehmigung durch den Vater.
        var act2Res = await shopChild.PostAsJsonAsync($"/api/v1/student/me/shop/inventory/{articleId}/activate",
            new { quantity = 10 });
        act2Res.EnsureSuccessStatusCode();
        var activation2Id = (await act2Res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        // Zu große Anfrage → insufficient_inventory (999 > 30)
        await Capture(shopChild, g, "Aktivierungsanfrage (Inventar erschöpft)", HttpMethod.Post,
            $"/api/v1/student/me/shop/inventory/{articleId}/activate",
            new { quantity = 999 }, HttpStatusCode.BadRequest, ApiErrors.InsufficientInventory.Code);

        // Eigener Bestand des Sohns (Gegenstück zum activate-POST)
        await Capture(shopChild, g, "Eigenes Inventar (Sohn)", HttpMethod.Get,
            "/api/v1/student/me/shop/inventory", null, HttpStatusCode.OK);

        // Eigene Aktivierungen des Sohns
        await Capture(shopChild, g, "Eigene Aktivierungen (Sohn)", HttpMethod.Get,
            "/api/v1/student/me/shop/activations", null, HttpStatusCode.OK);

        // ── Vater: Inventar / Käufe / Aktivierungen ───────────────────────────
        await Capture(father, g, "Kind-Inventar", HttpMethod.Get,
            $"/api/v1/supervisor/children/{shopChildId}/shop/inventory", null, HttpStatusCode.OK);

        await Capture(father, g, "Kind-Käufe", HttpMethod.Get,
            $"/api/v1/supervisor/children/{shopChildId}/shop/purchases", null, HttpStatusCode.OK);

        await Capture(father, g, "Kind-Aktivierungen", HttpMethod.Get,
            $"/api/v1/supervisor/children/{shopChildId}/shop/activations", null, HttpStatusCode.OK);

        // Genehmigung reduziert das Inventar real (30 → 0); die Deckung wird zum Genehmigungszeitpunkt geprüft.
        await Capture(father, g, "Aktivierung genehmigen", HttpMethod.Post,
            $"/api/v1/supervisor/children/{shopChildId}/shop/activations/{activationId}/approve", null, HttpStatusCode.OK);

        // activation_not_pending: dieselbe Anfrage erneut genehmigen → 409
        await Capture(father, g, "Aktivierung erneut genehmigen", HttpMethod.Post,
            $"/api/v1/supervisor/children/{shopChildId}/shop/activations/{activationId}/approve", null,
            HttpStatusCode.Conflict, ApiErrors.ActivationNotPending.Code);

        // Inventar nun erschöpft (0): Genehmigung der zweiten offenen Anfrage scheitert → insufficient_inventory.
        // Die Anfrage bleibt offen und kann weiterhin abgelehnt werden.
        await Capture(father, g, "Aktivierung genehmigen (Inventar erschöpft)", HttpMethod.Post,
            $"/api/v1/supervisor/children/{shopChildId}/shop/activations/{activation2Id}/approve", null,
            HttpStatusCode.BadRequest, ApiErrors.InsufficientInventory.Code);

        // Zweite Anfrage ablehnen (trotz gescheiterter Genehmigung weiterhin möglich)
        await Capture(father, g, "Aktivierung ablehnen", HttpMethod.Post,
            $"/api/v1/supervisor/children/{shopChildId}/shop/activations/{activation2Id}/reject", null, HttpStatusCode.OK);

        // Kauf stornieren erstattet Coins/Gems und reduziert das Inventar (max(0, 0 − 30) = 0).
        await Capture(father, g, "Kauf stornieren (Vater)", HttpMethod.Post,
            $"/api/v1/supervisor/children/{shopChildId}/shop/purchases/{purchaseId}/cancel", null, HttpStatusCode.OK);

        // ── Artikel/Listing löschen ────────────────────────────────────────────
        await Capture(father, g, "Angebot löschen", HttpMethod.Delete,
            $"/api/v1/supervisor/shop/articles/{articleId}/listings/{listingId}", null, HttpStatusCode.NoContent);

        await Capture(father, g, "Artikel löschen", HttpMethod.Delete,
            $"/api/v1/supervisor/shop/articles/{articleId}", null, HttpStatusCode.NoContent);
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  Markdown-Ausgabe: je Gruppe eine Datei + index.md (Übersicht, Abdeckung, „nicht erfassbar").
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    private void WriteMarkdown()
    {
        var outDir = Path.Combine(RepoRoot(), "docs", "api-examples");
        Directory.CreateDirectory(outDir);

        var groups = _entries.GroupBy(e => e.ResourceGroup).OrderBy(gr => gr.Key, StringComparer.Ordinal).ToList();
        foreach (var group in groups)
            File.WriteAllText(Path.Combine(outDir, $"{group.Key}.md"), RenderGroup(group.Key, [.. group]));

        File.WriteAllText(Path.Combine(outDir, "index.md"), RenderIndex(groups));
    }

    private void WriteOpenApiExamples()
    {
        var outDir = Path.Combine(RepoRoot(), "backend", "Pugling.Api", "OpenApi");
        Directory.CreateDirectory(outDir);

        var usedKeys = new HashSet<string>(StringComparer.Ordinal);
        var examples = _entries.Select(e => ToOpenApiExample(e, usedKeys)).ToList();
        var json = JsonSerializer.Serialize(examples, Indented);
        File.WriteAllText(Path.Combine(outDir, "openapi-examples.generated.json"), json);
    }

    private static OpenApiExampleEntry ToOpenApiExample(Entry entry, HashSet<string> usedKeys) =>
        new(UniqueKey(entry, usedKeys), entry.ResourceGroup, entry.Title, entry.Method, entry.Path, entry.Role,
            entry.RequestBodyJson, entry.ExpectedStatus, entry.ResponseBodyJson, entry.IsError,
            entry.IsError ? TryReadCode(entry.ResponseBodyJson) : null);

    private static string UniqueKey(Entry entry, HashSet<string> usedKeys)
    {
        var key = Slug($"{entry.ResourceGroup}-{entry.Title}");
        var uniqueKey = key;
        var suffix = 2;
        while (!usedKeys.Add(uniqueKey))
            uniqueKey = $"{key}-{suffix++}";
        return uniqueKey;
    }

    private static string Slug(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var withoutMarks = new string(normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());
        var slug = Regex.Replace(withoutMarks.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "example" : slug;
    }

    private static string? TryReadCode(string? responseBodyJson)
    {
        if (string.IsNullOrWhiteSpace(responseBodyJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(responseBodyJson);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("code", out var code)
                ? code.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string RenderGroup(string group, IReadOnlyList<Entry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# API-Beispiele – {group}").AppendLine();
        sb.AppendLine("_Automatisch erzeugt von `DocsCaptureTests` (Integrationstest). Jedes Beispiel ist verifiziert: "
            + "Status und – bei Fehlern – der maschinenlesbare `code` wurden im Testlauf geprüft. Nicht von Hand bearbeiten._").AppendLine();

        foreach (var e in entries)
        {
            sb.AppendLine(e.IsError ? $"### {e.Title} — Fehlerfall" : $"## {e.Title}");
            sb.AppendLine($"`{e.Method} {e.Path}`").AppendLine();
            var bearer = e.Role switch
            {
                "father" => "`Authorization: Bearer <father-token>`",
                "child" => "`Authorization: Bearer <child-token>`",
                "anonymous" => "_(kein Token)_",
                _ => "`Authorization: Bearer <token>`",
            };
            sb.AppendLine($"Rolle: **{e.Role}** — {bearer}").AppendLine();

            if (e.RequestBodyJson is { } rq)
            {
                sb.AppendLine("Request:").AppendLine("```json").AppendLine(rq).AppendLine("```").AppendLine();
            }

            sb.AppendLine($"Response — `HTTP {e.ActualStatus}`:");
            sb.AppendLine("```json").AppendLine(Truncate(e.ResponseBodyJson) ?? "(kein Inhalt)").AppendLine("```").AppendLine();
        }
        return sb.ToString();
    }

    private string RenderIndex(IReadOnlyList<IGrouping<string, Entry>> groups)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# API-Beispiele – Übersicht").AppendLine();
        sb.AppendLine($"Automatisch erzeugt von `backend/Pugling.Api.Tests/DocsCaptureTests.cs`. "
            + $"Insgesamt **{_entries.Count}** Beispiele in **{groups.Count}** Gruppen.").AppendLine();

        sb.AppendLine("| Gruppe | Beispiele | Fehlerfälle | Datei |");
        sb.AppendLine("| --- | ---: | ---: | --- |");
        foreach (var g in groups)
            sb.AppendLine($"| {g.Key} | {g.Count()} | {g.Count(e => e.IsError)} | [`{g.Key}.md`](./{g.Key}.md) |");
        sb.AppendLine();

        // Fehler-Code-Abdeckung gegen die zentrale Registry.
        sb.AppendLine("## Fehler-Code-Abdeckung").AppendLine();
        sb.AppendLine($"Verifiziert: **{_codeHits.Count} / {ApiErrors.AllCodes.Count}** Codes aus `ApiErrors`.").AppendLine();
        sb.AppendLine("| Code | Beispiel |");
        sb.AppendLine("| --- | --- |");
        foreach (var code in ApiErrors.AllCodes.Where(c => _codeHits.ContainsKey(c)))
        {
            var (grp, title) = _codeHits[code];
            sb.AppendLine($"| `{code}` | {grp} – {title} |");
        }
        sb.AppendLine();

        // Nicht automatisch erfassbare Codes mit Begründung.
        var reasons = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["bad_request"] = "Generischer 400-Default (`ForStatus`): nur Sicherheitsnetz für Framework-Antworten ohne spezifischen Code – alle regulären 400-Pfade tragen bereits einen fachlichen Code.",
            ["concurrency_conflict"] = "Erfordert eine echte Schreib-Kollision (Doppelklick/Retry) über das Concurrency-Token; in-process nicht deterministisch per HTTP auslösbar (siehe SkinPurchaseTests, direkt über DbContext).",
            ["rate_limited"] = "Login-Rate-Limit ist in der Test-Factory bewusst abgeschaltet (`RateLimiting:LoginEnabled=false`), sonst würden die vielen Test-Logins scheitern.",
            ["internal_error"] = "500-Fallback für unbehandelte Ausnahmen – kein sicherer, gezielter Auslöser über die öffentliche API.",
        };
        var missing = ApiErrors.AllCodes.Where(c => !_codeHits.ContainsKey(c)).ToList();
        sb.AppendLine("## Nicht automatisch erfassbar").AppendLine();
        if (missing.Count == 0)
        {
            sb.AppendLine("_(keine – alle Codes der Registry sind mit einem Beispiel belegt)_").AppendLine();
        }
        else
        {
            foreach (var code in missing)
                sb.AppendLine($"- `{code}` — {(reasons.TryGetValue(code, out var r) ? r : "Über HTTP im In-Process-Test nicht erreichbar.")}");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>Findet die Repo-Wurzel: von <see cref="AppContext.BaseDirectory"/> aufwärts, bis <c>backend</c>+<c>docs</c> (oder <c>.git</c>) vorliegen.</summary>
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var hasBackendDocs = Directory.Exists(Path.Combine(dir.FullName, "backend")) && Directory.Exists(Path.Combine(dir.FullName, "docs"));
            if (hasBackendDocs || Directory.Exists(Path.Combine(dir.FullName, ".git")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Repo-Wurzel (backend + docs bzw. .git) nicht gefunden.");
    }
}
