using System.Globalization;
using System.Net;
using System.Net.Http.Json;
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
/// Integration-test-driven "capture harness": drives the real API with seeded credentials,
/// checks HTTP status AND machine-readable error <c>code</c> for every response, and writes verified
/// request/response example pairs as Markdown under <c>docs/api-examples/</c>. Is both a CI gate
/// (any failed expectation turns the test red) and a doc generator: the examples are correct by
/// construction, because they are only recorded after the assertion has passed.
/// </summary>
public class DocsCaptureTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    // `NewLine = "\n"`: without it the line break of the indentation follows `Environment.NewLine` (CRLF on
    // Windows, LF on Linux) - for identical content Windows then counts more bytes, and the fixed
    // 1500-character truncation in `Truncate` cuts the JSON at a different place on each platform (measured:
    // the D4 CI gate found exactly that as its first real diff).
    private static readonly JsonSerializerOptions Indented = new(JsonSerializerDefaults.Web) { WriteIndented = true, NewLine = "\n" };

    /// <summary>
    /// A recorded request/response pair (no real token – bearer is redacted).
    /// <paramref name="ResponseMediaType"/> comes from the response header: not every response is JSON
    /// (<c>remarks/export</c> returns Markdown), and a mislabeled code block would be worse in the docs
    /// than none at all.
    /// </summary>
    private sealed record Entry(string ResourceGroup, string Title, string Method, string Path, string Role,
        string? RequestBodyJson, int ExpectedStatus, int ActualStatus, string? ResponseBodyJson, bool IsError,
        string? ResponseMediaType)
    {
        /// <summary>Whether the response body is JSON – only those qualify as an OpenAPI example (parsed there).</summary>
        public bool IsJsonResponse => ResponseMediaType is null
            || ResponseMediaType.Contains("json", StringComparison.OrdinalIgnoreCase);
    }

    private readonly List<Entry> _entries = [];
    // code → (group, title) of the first capture that verified this code (the coverage report).
    private readonly Dictionary<string, (string Group, string Title)> _codeHits = [];

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  The one capture helper: sends, checks the status (+ optionally the code), records, returns the body.
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

        // CI gate: the status has to match …
        Assert.True(expectedStatus == res.StatusCode,
            $"[{group}] {title}: erwartet HTTP {(int)expectedStatus}, war {(int)res.StatusCode}. Body: {raw}");

        // … and - where required - the machine-readable code.
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
            requestJson, (int)expectedStatus, (int)res.StatusCode, responseJson, isError,
            res.Content.Headers.ContentType?.MediaType));
        return bodyEl;
    }

    /// <summary>Derives the role from the bearer token (for documentation only – the token itself is never written).</summary>
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
        // Mask timestamps - with **or without** a zone marker: the API serializes `DateTime` (UTC values
        // without a `Z`), and a pattern insisting on `Z` never matched and made every run rewrite all
        // timestamps in the checked-in documentation.
        redacted = Regex.Replace(redacted,
            "\"\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}(?:\\.\\d+)?(?:Z|[+-]\\d{2}:\\d{2})?\"",
            "\"<timestamp>\"");
        // Non-JSON responses (the markdown export) carry their timestamps without quotes in running text
        // ("Stand: 2026-07-27 09:12 UTC") - without this rule the export would move every minute.
        redacted = Regex.Replace(redacted, @"\d{4}-\d{2}-\d{2} \d{2}:\d{2} UTC", "<timestamp>");
        // Plain date values only where they are **run-relative** (plan start/end, history days move daily).
        // Fixed literals such as `2099-03-01` from the requests stay readable - otherwise the example would
        // lose its point.
        return Regex.Replace(redacted, "\"(\\d{4})-(\\d{2})-(\\d{2})\"",
            m => IsRunRelativeDate(m.Value.Trim('"')) ? "\"<date>\"" : m.Value);
    }

    /// <summary>Date within reach of the test run (±1 year)? Only such values shift from run to run.</summary>
    private static bool IsRunRelativeDate(string value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
        && Math.Abs(date.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber) <= 366;

    /// <summary>Language tag of the code block – a Markdown block labeled as JSON would be a false statement.</summary>
    private static string LanguageOf(Entry entry) => entry.ResponseMediaType switch
    {
        null => "json",
        var m when m.Contains("json", StringComparison.OrdinalIgnoreCase) => "json",
        var m when m.Contains("markdown", StringComparison.OrdinalIgnoreCase) => "markdown",
        _ => "text",
    };

    /// <summary>Longest run of consecutive backticks in the content (determines the required fence length).</summary>
    private static int LongestBacktickRun(string content)
    {
        var longest = 0;
        var run = 0;
        foreach (var ch in content)
        {
            if (ch == '`') { run++; longest = Math.Max(longest, run); }
            else run = 0;
        }
        return longest;
    }

    private static string? Truncate(string? s) =>
        s is { Length: > 1500 } ? s[..1500] + "\n… (gekürzt)" : s;

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  Direct DB manipulation (no API path): credit gems (Achievement→gems) or coins (Base→coins).
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    private async Task GrantAsync(int childId, int amount, PointKind kind, string reason)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.ChildPointsEntries.Add(new ChildPointsEntry { ChildId = childId, Amount = amount, Kind = kind, Reason = reason });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CaptureAll()
    {
        var completed = false;
        try
        {
            var anon = factory.CreateClient();
            var father = await TestApi.FatherAsync(factory);       // father (id 1 / PIN 0000)
            var child = await TestApi.ChildAsync(factory);         // child (id 1 / PIN 1111)

            // A second adult (anonymous registration) for the cross-ownership 404/403.
            var father2Id = await TestApi.IdAsync(await anon.PostAsJsonAsync("/api/v1/supervisor/adults", new { name = "Zweiter Papa", pin = "2222" }));
            var father2 = await TestApi.FatherAsync(factory, father2Id, "2222");
            var foreignChildId = await TestApi.IdAsync(await father2.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Fremdes Kind", pin = "3333" }));

            await CaptureAuthAsync(anon);
            await CaptureChildrenAsync(father, father2, foreignChildId);
            var (docSubjectId, docSeriesId, docSeriesUnitId, docExerciseId) = await CaptureCatalogAsync(father);
            await CaptureExerciseTypesAsync(father, docSeriesId, docSeriesUnitId);
            await CaptureGrantsAsync(father, father2, father2Id, foreignChildId, docSeriesId, docSeriesUnitId, docExerciseId);
            await CaptureMeAsync(father, child);
            await CaptureStudyPlansAsync(father, father2, child, docSeriesId, docSeriesUnitId, docExerciseId);
            await CaptureClassTestsAsync(father);
            await CaptureVocabularyAsync(father);
            await CaptureTagsAsync(father, child, foreignChildId);
            await CaptureTimetableAsync(father, docSubjectId);
            await CaptureShopAsync(father, child);
            await CaptureRemarksAsync(father, child, docExerciseId);
            completed = true;
        }
        finally
        {
            // Write even on partial runs (that eases debugging); red assertions still fail the test.
            if (_entries.Count > 0) WriteMarkdown();
            if (completed) WriteOpenApiExamples();
        }

        // B-84: "Über HTTP im In-Process-Test nicht erreichbar." claimed unreachability for codes this
        // generator simply never called - 14 of the (then) 19 affected codes were already triggered via
        // HTTP elsewhere in the suite. Write-then-read here is race-free by construction: WriteMarkdown()
        // above already completed synchronously on this same thread.
        var indexMd = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "api-examples", "index.md"));
        Assert.DoesNotContain("Über HTTP im In-Process-Test nicht erreichbar.", indexMd);
        Assert.Contains("Von DocsCaptureTests nicht mitgeschnitten", indexMd);
    }

    // ── auth ────────────────────────────────────────────────────────────────────────────────────
    private async Task CaptureAuthAsync(HttpClient anon)
    {
        const string g = "auth";
        await Capture(anon, g, "Vater registrieren (anonym)", HttpMethod.Post, "/api/v1/supervisor/adults",
            new { name = "Neuer Papa", pin = "1234" }, HttpStatusCode.Created);
        await Capture(anon, g, "Vater-Login", HttpMethod.Post, "/api/v1/auth/adult",
            new { adultId = 1, pin = "0000" }, HttpStatusCode.OK);
        await Capture(anon, g, "Sohn-Login", HttpMethod.Post, "/api/v1/auth/child",
            new { childId = 1, pin = "1111" }, HttpStatusCode.OK);
        await Capture(anon, g, "Login mit falscher PIN", HttpMethod.Post, "/api/v1/auth/adult",
            new { adultId = 1, pin = "9998" }, HttpStatusCode.Unauthorized, ApiErrors.InvalidCredentials.Code);
        await Capture(anon, g, "Login mit nicht-numerischer adultId", HttpMethod.Post, "/api/v1/auth/adult",
            new { adultId = "1a", pin = "0000" }, HttpStatusCode.BadRequest, ApiErrors.ValidationError.Code);
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
    private async Task<(int subjectId, int seriesId, int seriesUnitId, int exerciseId)> CaptureCatalogAsync(HttpClient father)
    {
        const string g = "catalog";
        var subject = await Capture(father, g, "Fach anlegen", HttpMethod.Post, "/api/v1/creator/subjects",
            new { name = "Doku-Fach" }, HttpStatusCode.Created);
        var subjectId = subject.GetProperty("id").GetInt32();

        await Capture(father, g, "Fach ohne Namen anlegen", HttpMethod.Post, "/api/v1/creator/subjects",
            new { name = "" }, HttpStatusCode.BadRequest, ApiErrors.ValidationError.Code);

        // A series needs its subject set - only then can it host exercises (series_without_subject otherwise).
        var series = await Capture(father, g, "Lehrwerk-Reihe anlegen", HttpMethod.Post, "/api/v1/creator/textbook-series",
            new
            {
                name = "Doku-Reihe",
                publisher = (string?)null,
                subjectName = (string?)null,
                subjectId,
                schoolTypes = (string?)null,
                sourceLanguage = (string?)null,
                targetLanguage = (string?)null,
                notes = (string?)null,
            }, HttpStatusCode.Created);
        var seriesId = series.GetProperty("id").GetInt32();

        var unit = await Capture(father, g, "Unit anlegen", HttpMethod.Post, $"/api/v1/creator/textbook-series/{seriesId}/units",
            new { label = "Unit 1", grade = (int?)null, orderIndex = 1, topics = (string?)null, grammar = (string?)null, vocabularyNotes = (string?)null },
            HttpStatusCode.Created);
        var seriesUnitId = unit.GetProperty("id").GetInt32();

        // The exercise is created as a shell (settings only); the vocabulary pairs come underneath through the
        // item endpoint as their own sub-resource (items are a tier of their own, see VocabularyController).
        var exercise = await Capture(father, g, "Vokabel-Übung anlegen", HttpMethod.Post,
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary",
            new
            {
                title = "Begrüßungen",
                orderIndex = 1,
                rewardPoints = 10,
                config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de" },
            }, HttpStatusCode.Created);
        var exerciseId = exercise.GetProperty("id").GetInt32();

        // A vocabulary pair through the item endpoint: inline via front/back - without a vocabularyId the entry
        // is created in the store. (With a vocabularyId the id suffices; front/back would come from the store.)
        await Capture(father, g, "Vokabelpaar hinzufügen", HttpMethod.Post,
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{exerciseId}/items",
            new { front = "hello", back = "hallo" }, HttpStatusCode.Created);
        // Create a second pair directly (not as an example), so that the exercise carries two items for the play flow.
        (await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{exerciseId}/items",
            new { front = "goodbye", back = "tschüss" })).EnsureSuccessStatusCode();

        await Capture(father, g, "Unbekannte Übung lesen", HttpMethod.Get, "/api/v1/creator/exercises/999999",
            null, HttpStatusCode.NotFound, ApiErrors.NotFound.Code);

        await Capture(father, g, "Art (Kategorie) anlegen", HttpMethod.Post, $"/api/v1/creator/subjects/{subjectId}/categories",
            new { name = "Vokabeln" }, HttpStatusCode.Created);
        await Capture(father, g, "Doppelte Art anlegen", HttpMethod.Post, $"/api/v1/creator/subjects/{subjectId}/categories",
            new { name = "Vokabeln" }, HttpStatusCode.Conflict, ApiErrors.Conflict.Code);

        // An exercise sitting in a study plan cannot be deleted (position reference → 409).
        TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText);
        await Capture(father, g, "Verwendete Übung löschen", HttpMethod.Delete,
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{exerciseId}",
            null, HttpStatusCode.Conflict, ApiErrors.ExerciseInUse.Code);

        // Editing an exercise of another author (teacher library, AuthorAdultId = teacher) → 403 not_author.
        var foreign = await FindForeignAuthoredExerciseAsync(father);
        if (foreign is { } ex && await FindSeriesIdForUnitAsync(father, ex.SubjectId, ex.SeriesUnitId) is int foreignSeriesId)
            await Capture(father, g, "Fremd-Autor-Übung bearbeiten", HttpMethod.Put,
                $"/api/v1/creator/textbook-series/{foreignSeriesId}/units/{ex.SeriesUnitId}/vocabulary/{ex.Id}",
                new { title = "Übernahmeversuch", orderIndex = 1, rewardPoints = 1, config = new { } },
                HttpStatusCode.Forbidden, ApiErrors.NotAuthor.Code);

        return (subjectId, seriesId, seriesUnitId, exerciseId);
    }

    // ── exercise grants (RWX: owner/write/execute + the execute gate) ─────────────────────────────────
    private async Task CaptureGrantsAsync(HttpClient father, HttpClient father2, int father2Id,
        int foreignChildId, int seriesId, int seriesUnitId, int exerciseId)
    {
        const string g = "exercise-grants";

        await Capture(father, g, "Rechte einer Übung auflisten (nur Owner)", HttpMethod.Get,
            $"/api/v1/creator/exercises/{exerciseId}/grants", null, HttpStatusCode.OK);

        await Capture(father2, g, "Rechte einer fremden Übung auflisten", HttpMethod.Get,
            $"/api/v1/creator/exercises/{exerciseId}/grants", null, HttpStatusCode.Forbidden, ApiErrors.NotOwner.Code);

        await Capture(father, g, "Write-Recht an anderen Creator vergeben", HttpMethod.Post,
            $"/api/v1/creator/exercises/{exerciseId}/grants",
            new { creatorId = father2Id, permission = "Write" }, HttpStatusCode.Created);

        // The creator is the only owner (auto grant, adult id 1) - they cannot be removed as the last one.
        await Capture(father, g, "Letzten Owner entfernen", HttpMethod.Delete,
            $"/api/v1/creator/exercises/{exerciseId}/grants/1/Owner",
            null, HttpStatusCode.Conflict, ApiErrors.LastOwner.Code);

        // Execute gate: another creator must not assign an exercise that is not publicly executable.
        var privateEx = await Capture(father, g, "Nicht öffentlich ausführbare Übung anlegen", HttpMethod.Post,
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary",
            new
            {
                title = "Nur intern",
                orderIndex = 2,
                rewardPoints = 10,
                executePublic = false,
                config = new { direction = "front-to-back", sourceLang = "en", targetLang = "de" },
            }, HttpStatusCode.Created);
        var privateExId = privateEx.GetProperty("id").GetInt32();

        var plan = await Capture(father2, g, "Lehrplan für eigenes Kind anlegen", HttpMethod.Post,
            "/api/v1/supervisor/study-plans",
            new { childId = foreignChildId, title = "Plan (fremd)", durationDays = 5 }, HttpStatusCode.Created);
        var planId = plan.GetProperty("id").GetInt32();

        await Capture(father2, g, "Nicht ausführbare Übung zuweisen", HttpMethod.Post,
            $"/api/v1/supervisor/study-plans/{planId}/positions",
            new { exerciseId = privateExId }, HttpStatusCode.Forbidden, ApiErrors.ExerciseNotExecutable.Code);
    }

    // ── exercise types (one verified create POST per type) ─────────────────────────────────────
    // Vocabulary is already covered in CaptureCatalogAsync; here the remaining types, so that EVERY type POST
    // carries a verified request/response example into the OpenAPI spec (and thus the Bruno collection).
    private async Task CaptureExerciseTypesAsync(HttpClient father, int seriesId, int seriesUnitId)
    {
        const string g = "catalog";
        string Base(string type) => $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/{type}";

        await Capture(father, g, "Leseübung anlegen", HttpMethod.Post, Base("reading"),
            new
            {
                title = "Der Wetterbericht",
                orderIndex = 3,
                rewardPoints = 10,
                config = new
                {
                    text = "Today it is sunny with a light breeze.",
                    questions = new[] { new { prompt = "How is the weather?", choices = new[] { "sunny", "rainy", "snowy" }, answer = "sunny" } },
                },
            }, HttpStatusCode.Created);

        await Capture(father, g, "Lückentext anlegen", HttpMethod.Post, Base("cloze"),
            new
            {
                title = "Present Simple",
                orderIndex = 4,
                rewardPoints = 10,
                config = new
                {
                    text = "She {{1}} to school every day and {{2}} her friends.",
                    gaps = new[] { new { index = 1, answer = "goes" }, new { index = 2, answer = "meets" } },
                    wordBank = new[] { "goes", "meets", "go", "meet" },
                },
            }, HttpStatusCode.Created);

        await Capture(father, g, "Aufsatz anlegen", HttpMethod.Post, Base("essays"),
            new
            {
                title = "My last holiday",
                orderIndex = 5,
                rewardPoints = 20,
                config = new
                {
                    prompt = "Write about your last holiday.",
                    minWords = 80,
                    maxWords = 200,
                    rubric = new[] { new { criterion = "Content", maxScore = 5 }, new { criterion = "Grammar", maxScore = 5 } },
                },
            }, HttpStatusCode.Created);

        await Capture(father, g, "Hörübung anlegen", HttpMethod.Post, Base("listening"),
            new
            {
                title = "At the station",
                orderIndex = 6,
                rewardPoints = 10,
                config = new
                {
                    audioUrl = "https://example.com/audio/at-the-station.mp3",
                    transcript = "The train to London leaves at nine o'clock.",
                    questions = new[] { new { prompt = "When does the train leave?", choices = new[] { "at nine", "at ten", "at noon" }, answer = "at nine" } },
                },
            }, HttpStatusCode.Created);

        await Capture(father, g, "Grammatikübung anlegen", HttpMethod.Post, Base("grammar"),
            new
            {
                title = "Simple Past",
                orderIndex = 7,
                rewardPoints = 10,
                config = new
                {
                    instruction = "Put the verb in brackets into the simple past.",
                    tasks = new[]
                    {
                        new { prompt = "I (go) to school.", answer = "went", ruleHint = "irregular verb" },
                        new { prompt = "She (play) tennis.", answer = "played", ruleHint = "regular: + ed" },
                    },
                },
            }, HttpStatusCode.Created);

        await Capture(father, g, "Zuordnungsübung anlegen", HttpMethod.Post, Base("matching"),
            new
            {
                title = "Countries & capitals",
                orderIndex = 8,
                rewardPoints = 10,
                config = new
                {
                    instruction = "Match each country to its capital.",
                    pairs = new[] { new { left = "France", right = "Paris" }, new { left = "Spain", right = "Madrid" } },
                },
            }, HttpStatusCode.Created);

        await Capture(father, g, "Übersetzungsübung anlegen", HttpMethod.Post, Base("translation"),
            new
            {
                title = "Everyday phrases",
                orderIndex = 9,
                rewardPoints = 10,
                config = new
                {
                    sourceLang = "en",
                    targetLang = "de",
                    items = new[]
                    {
                        new { source = "Good morning", target = "Guten Morgen", alternatives = new[] { "Guten Tag" } },
                        new { source = "Thank you", target = "Danke", alternatives = new[] { "Vielen Dank" } },
                    },
                },
            }, HttpStatusCode.Created);

        await Capture(father, g, "Feste Rechenaufgaben anlegen", HttpMethod.Post, Base("arithmetic"),
            new
            {
                title = "Kopfrechnen gemischt",
                orderIndex = 10,
                rewardPoints = 10,
                config = new
                {
                    problems = new[]
                    {
                        new { prompt = "7 + 8", answer = 15m, tolerance = 0m },
                        new { prompt = "12 / 5", answer = 2.4m, tolerance = 0.1m },
                    },
                },
            }, HttpStatusCode.Created);

        await Capture(father, g, "Rechen-Drill (Regeln) anlegen", HttpMethod.Post, Base("arithmetic-drill"),
            new
            {
                title = "Einmaleins-Drill",
                orderIndex = 11,
                rewardPoints = 10,
                config = new
                {
                    operations = new[] { "Multiplication" },
                    minOperand = 2,
                    maxOperand = 10,
                    problemCount = 10,
                    allowNegativeResults = false,
                    divisionMustBeWhole = true,
                },
            }, HttpStatusCode.Created);

        await Capture(father, g, "Merkliste anlegen", HttpMethod.Post, Base("list"),
            new
            {
                title = "Die vier Himmelsrichtungen",
                orderIndex = 12,
                rewardPoints = 15,
                config = new
                {
                    instruction = "Nenne die vier Himmelsrichtungen.",
                    ordered = false,
                    items = new[]
                    {
                        new { value = "Norden", alternatives = new[] { "Nord" } },
                        new { value = "Osten", alternatives = new[] { "Ost" } },
                    },
                },
            }, HttpStatusCode.Created);

        await Capture(father, g, "Birkenbihl-Übung anlegen", HttpMethod.Post, Base("birkenbihl"),
            new
            {
                title = "Birkenbihl: Small talk",
                orderIndex = 13,
                rewardPoints = 10,
                config = new { learningLang = "en", nativeLang = "de", sentences = Array.Empty<object>() },
            }, HttpStatusCode.Created);
    }

    private sealed record ForeignExercise(int Id, int? SubjectId, int SeriesUnitId);

    /// <summary>Searches the catalog for a vocabulary exercise with a foreign author (≠ father, ≠ system) for the not_author case.</summary>
    private static async Task<ForeignExercise?> FindForeignAuthoredExerciseAsync(HttpClient father)
    {
        var list = await father.GetFromJsonAsync<List<JsonElement>>("/api/v1/creator/exercises?type=Vocabulary&take=500");
        foreach (var e in list ?? [])
        {
            var author = e.TryGetProperty("authorAdultId", out var a) && a.ValueKind == JsonValueKind.Number ? a.GetInt32() : (int?)null;
            var isOwn = e.TryGetProperty("isOwn", out var o) && o.GetBoolean();
            if (author is { } id && id != 1 && !isOwn)
                return new ForeignExercise(e.GetProperty("id").GetInt32(),
                    e.TryGetProperty("subjectId", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetInt32() : (int?)null,
                    e.GetProperty("seriesUnitId").GetInt32());
        }
        return null;
    }

    /// <summary>
    /// Resolves the textbook series a unit belongs to: a search hit only carries the unit id, but the
    /// nested exercise routes need the series id too. Scoped by the exercise's subject (if any) to keep the
    /// series/unit scan small.
    /// </summary>
    private static async Task<int?> FindSeriesIdForUnitAsync(HttpClient father, int? subjectId, int seriesUnitId)
    {
        var seriesQuery = subjectId is int sid ? $"?subjectId={sid}&take=500" : "?take=500";
        var seriesList = await father.GetFromJsonAsync<List<JsonElement>>($"/api/v1/creator/textbook-series{seriesQuery}");
        foreach (var series in seriesList ?? [])
        {
            var seriesId = series.GetProperty("id").GetInt32();
            var units = await father.GetFromJsonAsync<List<JsonElement>>($"/api/v1/creator/textbook-series/{seriesId}/units");
            if (units!.Any(u => u.GetProperty("id").GetInt32() == seriesUnitId)) return seriesId;
        }
        return null;
    }

    // ── me (the child) + the listing/skin economy ──────────────────────────────────────────────────────
    private async Task CaptureMeAsync(HttpClient father, HttpClient child)
    {
        const string g = "me";

        // Read views of the seeded child (id 1) - realistic data (missions/skins/listings are seeded).
        await Capture(child, g, "Eigener Kontostand (Wallet)", HttpMethod.Get, "/api/v1/student/me/points", null, HttpStatusCode.OK);

        // Ledger entries sit one level deeper: list + single view. Create one deterministic entry.
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

        // A fresh child A: deterministic balances for the purchase/equip cases.
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
        int docSeriesId, int docSeriesUnitId, int docExerciseId)
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

        // The child practices (learn mode, server-driven): start a session, grade one card (the check happens
        // server-side - the answer already carries the next card + the completion signal), optionally fetch the next card through /next.
        var session = await Capture(child, g, "Übungssitzung starten (Lern-Modus)", HttpMethod.Post,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions", new { mode = "Lern" }, HttpStatusCode.Created);
        var sessionId = session.GetProperty("id").GetInt32();
        await Capture(child, g, "Nächste Karte (server-geführter Cursor)", HttpMethod.Get,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions/{sessionId}/next", null, HttpStatusCode.OK);
        await Capture(child, g, "Karte bewerten (Review, mit nächster Karte)", HttpMethod.Post,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions/{sessionId}/review",
            new { itemIndex = 0, givenAnswer = "hallo" }, HttpStatusCode.OK);

        // Info mode (free practice): all cards at once, but /review writes no feedback (204).
        var infoSession = await Capture(child, g, "Übungssitzung starten (Info-Modus, freies Üben)", HttpMethod.Post,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions", new { mode = "Info" }, HttpStatusCode.Created);
        var infoSessionId = infoSession.GetProperty("id").GetInt32();
        await Capture(child, g, "Karten am Stück (Info-Modus/Offline-Batch)", HttpMethod.Get,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions/{infoSessionId}/cards", null, HttpStatusCode.OK);
        await Capture(child, g, "Review im Info-Modus (kein Feedback → 204)", HttpMethod.Post,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions/{infoSessionId}/review",
            new { itemIndex = 0, givenAnswer = "hallo" }, HttpStatusCode.NoContent);

        // The final test = a class test (strictly server-driven): start (metadata only), fetch a question,
        // answer it (without correctness), submit (grade), submit again (→ test_already_submitted).
        var attempt = await Capture(child, g, "Test starten (Klausur, ohne Aufgaben-Bulk)", HttpMethod.Post,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests", new { }, HttpStatusCode.Created);
        var attemptId = attempt.GetProperty("attemptId").GetInt32();
        await Capture(child, g, "Nächste Prüfungsfrage (One-at-a-time)", HttpMethod.Get,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests/{attemptId}/next", null, HttpStatusCode.OK);
        await Capture(child, g, "Prüfungsantwort abgeben (ohne Korrektheit)", HttpMethod.Post,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests/{attemptId}/answer",
            new { givenAnswer = "hallo" }, HttpStatusCode.OK);
        // Answer the remaining questions (not captured) so that the attempt is complete.
        await child.PostAsJsonAsync($"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests/{attemptId}/answer",
            new { givenAnswer = "tschüss" });
        await Capture(child, g, "Test abgeben (auswerten)", HttpMethod.Post,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests/{attemptId}/submit", new { }, HttpStatusCode.OK);
        await Capture(child, g, "Test erneut abgeben", HttpMethod.Post,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests/{attemptId}/submit", new { },
            HttpStatusCode.BadRequest, ApiErrors.TestAlreadySubmitted.Code);

        // Daily mission + history. The history supports paging (skip/take, X-Total-Count), sorting
        // (day/-day/points/-points) and filters (from/to, dutyDone).
        await Capture(child, g, "Tagesmission (Overview)", HttpMethod.Get,
            $"/api/v1/student/study-plans/{planId}/overview", null, HttpStatusCode.OK);
        await Capture(child, g, "Verlauf – Paging & Sortierung (neueste zuerst)", HttpMethod.Get,
            $"/api/v1/student/study-plans/{planId}/overview/progress?take=3&sort=-day", null, HttpStatusCode.OK);
        await Capture(child, g, "Verlauf – nur erledigte Tage", HttpMethod.Get,
            $"/api/v1/student/study-plans/{planId}/overview/progress?dutyDone=true", null, HttpStatusCode.OK);

        // The period's attempt cap. Deliberately AFTER the overview/history captures: setting it up consumes
        // this position's allowance, and an additional (failed) attempt would otherwise show up in their
        // numbers. One attempt has been submitted above, the second one here - the third is grade farming and
        // is rejected.
        var secondAttempt = await child.PostAsJsonAsync(
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests", new { });
        secondAttempt.EnsureSuccessStatusCode();
        var secondAttemptId = (await secondAttempt.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("attemptId").GetInt32();
        await child.PostAsJsonAsync(
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests/{secondAttemptId}/submit", new { });
        await Capture(child, g, "Dritter Testversuch des Tages (Deckel)", HttpMethod.Post,
            $"/api/v1/student/study-plans/{planId}/positions/{positionId}/tests", new { },
            HttpStatusCode.Conflict, ApiErrors.TestAttemptsExhausted.Code);

        // A test on a reading exercise without checkable content → no_checkable_content.
        var reading = await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{docSeriesId}/units/{docSeriesUnitId}/reading",
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

        // Assigning a vocabulary exercise that is not filled yet → exercise_empty. The difference to the case
        // above is the point: there "nothing to check" is a property of the type, here it is an unfinished state.
        var emptyVocab = await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{docSeriesId}/units/{docSeriesUnitId}/vocabulary",
            new { title = "Vokabeln (noch leer)", orderIndex = 3, rewardPoints = 5, config = new { direction = "front-to-back" } });
        emptyVocab.EnsureSuccessStatusCode();
        var emptyVocabId = (await emptyVocab.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        await Capture(father, g, "Ungefüllte Übung zuweisen", HttpMethod.Post,
            $"/api/v1/supervisor/study-plans/{planId}/positions", new { exerciseId = emptyVocabId, cadence = "Daily" },
            HttpStatusCode.BadRequest, ApiErrors.ExerciseEmpty.Code);

        // A tag snapshot without hits → no_tag_matches (the exercise stays unchanged). Its own code, so that a
        // caller can tell it apart from "no tag sent at all" (validation_error).
        await Capture(father, g, "Tag-Schnappschuss ohne Treffer", HttpMethod.Post,
            $"/api/v1/creator/textbook-series/{docSeriesId}/units/{docSeriesUnitId}/vocabulary/{emptyVocabId}/refs-from-tags",
            new { tags = new[] { "gibt-es-nicht" } }, HttpStatusCode.BadRequest, ApiErrors.NoTagMatches.Code);

        // Deleting a position that has been played → position_has_data.
        await Capture(father, g, "Bespielte Position löschen", HttpMethod.Delete,
            $"/api/v1/supervisor/study-plans/{planId}/positions/{positionId}", null,
            HttpStatusCode.Conflict, ApiErrors.PositionHasData.Code);

        // Deactivate the plan → the child can no longer play it (plan_inactive).
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

    // ── vocabulary (store) ──────────────────────────────────────────────────────────────────────────
    private async Task CaptureVocabularyAsync(HttpClient father)
    {
        const string g = "vocabulary";
        var dto = new { key = "en_doku_de_beispiel", sourceLanguage = "en", targetLanguage = "de", word = "example", translation = "Beispiel", partOfSpeech = "Noun" };
        await Capture(father, g, "Vokabel anlegen", HttpMethod.Post, "/api/v1/creator/vocabulary", dto, HttpStatusCode.Created);
        await Capture(father, g, "Vokabel mit doppeltem Key", HttpMethod.Post, "/api/v1/creator/vocabulary", dto,
            HttpStatusCode.Conflict, ApiErrors.DuplicateKey.Code);

        // A seeded base form (the basis of inflected forms) cannot be deleted → vocabulary_in_use.
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
        // An unknown field: the server rejects it instead of dropping it silently. It belongs in the example
        // collection because it concerns every endpoint and generated clients depend on it.
        await Capture(father, g, "Unbekanntes Feld im Body", HttpMethod.Post, "/api/v1/supervisor/children/1/timetable",
            new { subjectId = docSubjectId, dayOfWeek = "Wednesday", timeOfDay = "Vormittag", tageszeit = "Vormittag" },
            HttpStatusCode.BadRequest, ApiErrors.UnknownField.Code);
    }

    // ── shop (supervisor admin + the child's side) ──────────────────────────────────────────────────────────
    private async Task CaptureShopAsync(HttpClient father, HttpClient child)
    {
        const string g = "shop";

        // ── Article CRUD ──────────────────────────────────────────────────────
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

        // ── Listing CRUD ─────────────────────────────────────────────────────
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
            }, HttpStatusCode.Created);
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

        // ── The child buys + an activation request ──────────────────────────────────
        var shopChildId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Shop-Doku-Kind", pin = "7001" }));
        var shopChild = await TestApi.ChildAsync(factory, shopChildId, "7001");

        // Credit coins (through the supervisor's points endpoint)
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{shopChildId}/points",
            new { amount = 300, reason = "Doku-Münzen" })).EnsureSuccessStatusCode();

        await Capture(shopChild, g, "Shop-Sicht (Sohn)", HttpMethod.Get, "/api/v1/student/me/shop",
            null, HttpStatusCode.OK);

        var purchaseView = await Capture(shopChild, g, "Shop-Angebot kaufen", HttpMethod.Post,
            $"/api/v1/student/me/shop/listings/{listingId}/purchase", new { }, HttpStatusCode.OK);
        var purchaseId = purchaseView.GetProperty("purchases").EnumerateArray().First().GetProperty("id").GetInt32();

        // Empty stock scenario: create a new listing with stock=0, then buy → shop_insufficient_stock
        var emptyListingEl = await father.PostAsJsonAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings",
            new { coinPrice = 50, gemPrice = 0, unitsPerPurchase = 10, currentStock = 0, maxStock = 1 });
        emptyListingEl.EnsureSuccessStatusCode();
        var emptyListingId = (await emptyListingEl.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        await Capture(shopChild, g, "Shop-Angebot kaufen (ausverkauft)", HttpMethod.Post,
            $"/api/v1/student/me/shop/listings/{emptyListingId}/purchase", new { },
            HttpStatusCode.Conflict, ApiErrors.ShopInsufficientStock.Code);

        // A deactivated listing → shop_listing_inactive
        (await father.PatchAsJsonAsync($"/api/v1/supervisor/shop/articles/{articleId}/listings/{emptyListingId}",
            new { active = false })).EnsureSuccessStatusCode();
        await Capture(shopChild, g, "Shop-Angebot kaufen (deaktiviert)", HttpMethod.Post,
            $"/api/v1/student/me/shop/listings/{emptyListingId}/purchase", new { },
            HttpStatusCode.BadRequest, ApiErrors.ShopListingInactive.Code);

        // Without funds: a fresh child (0 coins) buys an active, in-stock listing → insufficient_coins.
        var brokeChildId = await TestApi.IdAsync(
            await father.PostAsJsonAsync("/api/v1/supervisor/children", new { name = "Shop-Doku-Kind (pleite)", pin = "7009" }));
        var brokeChild = await TestApi.ChildAsync(factory, brokeChildId, "7009");
        await Capture(brokeChild, g, "Shop-Angebot kaufen (kein Guthaben)", HttpMethod.Post,
            $"/api/v1/student/me/shop/listings/{listingId}/purchase", new { },
            HttpStatusCode.BadRequest, ApiErrors.InsufficientCoins.Code);

        // Activation requests: the child requests units from its inventory (30 available).
        var activation1El = await Capture(shopChild, g, "Aktivierungsanfrage stellen", HttpMethod.Post,
            $"/api/v1/student/me/shop/inventory/{articleId}/activate",
            new { quantity = 30 }, HttpStatusCode.OK);
        var activationId = activation1El.GetProperty("id").GetInt32();

        // A second request (10 units) - checked against the aggregated inventory at request time (30 >= 10);
        // the binding funds check only happens when the supervisor approves.
        var act2Res = await shopChild.PostAsJsonAsync($"/api/v1/student/me/shop/inventory/{articleId}/activate",
            new { quantity = 10 });
        act2Res.EnsureSuccessStatusCode();
        var activation2Id = (await act2Res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        // A request that is too large → insufficient_inventory (999 > 30)
        await Capture(shopChild, g, "Aktivierungsanfrage (Inventar erschöpft)", HttpMethod.Post,
            $"/api/v1/student/me/shop/inventory/{articleId}/activate",
            new { quantity = 999 }, HttpStatusCode.BadRequest, ApiErrors.InsufficientInventory.Code);

        // The child's own stock (the counterpart to the activate POST)
        await Capture(shopChild, g, "Eigenes Inventar (Sohn)", HttpMethod.Get,
            "/api/v1/student/me/shop/inventory", null, HttpStatusCode.OK);

        // The child's own activations
        await Capture(shopChild, g, "Eigene Aktivierungen (Sohn)", HttpMethod.Get,
            "/api/v1/student/me/shop/activations", null, HttpStatusCode.OK);

        // ── Supervisor: inventory / purchases / activations ───────────────────────────
        await Capture(father, g, "Kind-Inventar", HttpMethod.Get,
            $"/api/v1/supervisor/children/{shopChildId}/shop/inventory", null, HttpStatusCode.OK);

        await Capture(father, g, "Kind-Käufe", HttpMethod.Get,
            $"/api/v1/supervisor/children/{shopChildId}/shop/purchases", null, HttpStatusCode.OK);

        await Capture(father, g, "Kind-Aktivierungen", HttpMethod.Get,
            $"/api/v1/supervisor/children/{shopChildId}/shop/activations", null, HttpStatusCode.OK);

        // Approval really reduces the inventory (30 → 0); the funds are checked at approval time.
        await Capture(father, g, "Aktivierung genehmigen", HttpMethod.Post,
            $"/api/v1/supervisor/children/{shopChildId}/shop/activations/{activationId}/approve", null, HttpStatusCode.OK);

        // activation_not_pending: approving the same request again → 409
        await Capture(father, g, "Aktivierung erneut genehmigen", HttpMethod.Post,
            $"/api/v1/supervisor/children/{shopChildId}/shop/activations/{activationId}/approve", null,
            HttpStatusCode.Conflict, ApiErrors.ActivationNotPending.Code);

        // The inventory is now exhausted (0): approving the second open request fails → insufficient_inventory.
        // The request stays open and can still be rejected.
        await Capture(father, g, "Aktivierung genehmigen (Inventar erschöpft)", HttpMethod.Post,
            $"/api/v1/supervisor/children/{shopChildId}/shop/activations/{activation2Id}/approve", null,
            HttpStatusCode.BadRequest, ApiErrors.InsufficientInventory.Code);

        // Reject the second request (still possible despite the failed approval)
        await Capture(father, g, "Aktivierung ablehnen", HttpMethod.Post,
            $"/api/v1/supervisor/children/{shopChildId}/shop/activations/{activation2Id}/reject", null, HttpStatusCode.OK);

        // Cancelling a purchase refunds coins/gems and reduces the inventory (max(0, 0 − 30) = 0).
        await Capture(father, g, "Kauf stornieren (Vater)", HttpMethod.Post,
            $"/api/v1/supervisor/children/{shopChildId}/shop/purchases/{purchaseId}/cancel", null, HttpStatusCode.OK);

        // ── Delete article/listing ────────────────────────────────────────────
        await Capture(father, g, "Angebot löschen", HttpMethod.Delete,
            $"/api/v1/supervisor/shop/articles/{articleId}/listings/{listingId}", null, HttpStatusCode.NoContent);

        await Capture(father, g, "Artikel löschen", HttpMethod.Delete,
            $"/api/v1/supervisor/shop/articles/{articleId}", null, HttpStatusCode.NoContent);
    }

    // ── remarks (captured while testing - a tier-neutral resource) ───────────────────────────────
    // Captures the circle that makes up the feature: capture including context → log id → write the answer
    // back → a follow-up remark with a reference. Plus the visibility wall (a student sees only their own)
    // and the error case `remark_not_found`.
    private async Task CaptureRemarksAsync(HttpClient father, HttpClient child, int docExerciseId)
    {
        const string g = "remarks";

        var created = await Capture(father, g, "Anmerkung erfassen (mit Kontext)", HttpMethod.Post, "/api/v1/remarks",
            new
            {
                text = "Ich will meine E-Mail-Adresse ändern und finde keine Stelle dafür.",
                category = "Question",
                context = new
                {
                    route = "/vater/kind/1",
                    appArea = "vater",
                    childId = 1,
                    exerciseId = docExerciseId,
                    contextJson = """{"tab":"stammdaten"}""",
                    // Ring buffer: metadata only - no bodies, headers or tokens.
                    // The login request carries the PIN in its body; a raw capture would put it into the DB.
                    recentErrorsJson = """[{"method":"GET","path":"/api/v1/supervisor/adults/1","status":404,"code":"not_found","at":"2026-07-27T09:12:44Z"}]""",
                },
            }, HttpStatusCode.Created);
        var remarkId = created.GetProperty("id").GetInt32();

        await Capture(father, g, "Anmerkung ohne Text erfassen", HttpMethod.Post, "/api/v1/remarks",
            new { text = "   " }, HttpStatusCode.BadRequest, ApiErrors.ValidationError.Code);

        await Capture(father, g, "Anmerkung zur Log-Id lesen", HttpMethod.Get, $"/api/v1/remarks/{remarkId}",
            null, HttpStatusCode.OK);

        await Capture(father, g, "Eigene Anmerkungen (Liste im Widget)", HttpMethod.Get,
            "/api/v1/remarks?mine=true&take=5", null, HttpStatusCode.OK);

        // The back channel: Claude Code writes the answer back and closes the case. At `Planned` the answer
        // would be kept just the same - that turns the open note into an analyzed backlog entry.
        await Capture(father, g, "Antwort zurückschreiben und abschließen", HttpMethod.Patch, $"/api/v1/remarks/{remarkId}",
            new
            {
                answer = "Die API kann das über PATCH api/v1/supervisor/adults/{id} (AdultsController.Update); im Vater-Web gibt es dafür kein Formular.",
                answeredBy = "claude-code",
                status = "Done",
            }, HttpStatusCode.OK);

        // The history: what happens after the resolution no longer overwrites it. The implementation note comes
        // from Claude (`Assistant`) and deliberately leaves the status untouched - otherwise every note of its
        // own would reopen the case.
        await Capture(father, g, "Umsetzungsnotiz in den Verlauf schreiben", HttpMethod.Post,
            $"/api/v1/remarks/{remarkId}/comments",
            new
            {
                body = "Gebaut: Formular unter /vater/profil ergänzt (VaterProfil.tsx), PATCH über api.updateAdult.",
                author = "Assistant",
                authorLabel = "claude-code",
            }, HttpStatusCode.Created);

        await Capture(father, g, "Verlauf einer Anmerkung lesen", HttpMethod.Get,
            $"/api/v1/remarks/{remarkId}/comments", null, HttpStatusCode.OK);

        await Capture(father, g, "Leeren Beitrag schreiben", HttpMethod.Post,
            $"/api/v1/remarks/{remarkId}/comments", new { body = "   " },
            HttpStatusCode.BadRequest, ApiErrors.ValidationError.Code);

        await Capture(father, g, "Folgeanmerkung mit Verweis anlegen", HttpMethod.Post, "/api/v1/remarks",
            new
            {
                text = "Formular für die E-Mail-Adresse im Vater-Web nachziehen.",
                category = "Idea",
                parentRemarkId = remarkId,
            }, HttpStatusCode.Created);

        await Capture(father, g, "Verweis auf unbekannte Vorgänger-Anmerkung", HttpMethod.Post, "/api/v1/remarks",
            new { text = "Bezug ins Leere", parentRemarkId = 999999 },
            HttpStatusCode.BadRequest, ApiErrors.InvalidReference.Code);

        // The visibility wall: a student sees their own remarks only. That is why the answer here is 404 and
        // not 403 - a 403 would disclose that the remark exists, and its answers carry file and line
        // references.
        await Capture(child, g, "Fremde Anmerkung lesen (Sohn)", HttpMethod.Get, $"/api/v1/remarks/{remarkId}",
            null, HttpStatusCode.NotFound, ApiErrors.RemarkNotFound.Code);

        await Capture(father, g, "Unbekannte Anmerkung lesen", HttpMethod.Get, "/api/v1/remarks/999999",
            null, HttpStatusCode.NotFound, ApiErrors.RemarkNotFound.Code);

        // The markdown export - the only bridge to the test skills, which run against a throwaway DB and see
        // the real remarks only as a file under docs/anmerkungen/. The response is `text/markdown`, not JSON;
        // filtered on `status=Done` so that the example shows an answered case.
        await Capture(father, g, "Anmerkungen als Markdown exportieren", HttpMethod.Get,
            "/api/v1/remarks/export?status=Done", null, HttpStatusCode.OK);

        // Only the supervisor may export: answers carry file and line references, i.e. code internals.
        await Capture(child, g, "Export als Sohn abrufen", HttpMethod.Get, "/api/v1/remarks/export",
            null, HttpStatusCode.Forbidden, ApiErrors.Forbidden.Code);

        // Reopening - deliberately *after* the export, so that its example (`status=Done`) shows the closed
        // case. An entry by the **human** pulls the remark back to `Open`; that is the mechanism the follow-up
        // skill uses to present it again on the next run.
        await Capture(father, g, "Nachhaken (holt die Anmerkung zurück auf offen)", HttpMethod.Post,
            $"/api/v1/remarks/{remarkId}/comments",
            new { body = "Und wie ändere ich die Adresse des Kindes?" }, HttpStatusCode.Created);

        await Capture(father, g, "Anmerkung nach dem Nachhaken lesen", HttpMethod.Get,
            $"/api/v1/remarks/{remarkId}", null, HttpStatusCode.OK);

        // The cross-account view: the follow-up skill's perspective. It hangs on the switch `Remarks:GlobalRead`
        // (on in development) and **not** on a role - testing constantly creates throwaway accounts, because
        // some bugs only appear in a certain constellation, and authorizing each one would be administration
        // without any return. With the switch off (the default outside development), the same call answers
        // `403 remark_scope_forbidden`.
        await Capture(father, g, "Anmerkungen aller Konten lesen (scope=all)", HttpMethod.Get,
            "/api/v1/remarks?scope=all&take=5", null, HttpStatusCode.OK);

        // A student stays excluded in any case - even with the switch on: answers and history carry file and
        // line references.
        await Capture(child, g, "Alle Konten lesen als Sohn", HttpMethod.Get,
            "/api/v1/remarks?scope=all", null, HttpStatusCode.Forbidden, ApiErrors.RemarkScopeForbidden.Code);

        // Delete last - the follow-up remark hangs on it through `SetNull` and survives.
        await Capture(father, g, "Anmerkung löschen", HttpMethod.Delete, $"/api/v1/remarks/{remarkId}",
            null, HttpStatusCode.NoContent);
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    //  Markdown output: one file per group + index.md (overview, coverage, "not capturable").
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    private void WriteMarkdown()
    {
        var outDir = Path.Combine(RepoRoot(), "docs", "api-examples");
        Directory.CreateDirectory(outDir);

        var groups = _entries.GroupBy(e => e.ResourceGroup).OrderBy(gr => gr.Key, StringComparer.Ordinal).ToList();
        foreach (var group in groups)
            File.WriteAllText(Path.Combine(outDir, $"{group.Key}.md"), NormalizeTrailingNewline(RenderGroup(group.Key, [.. group])));

        File.WriteAllText(Path.Combine(outDir, "index.md"), NormalizeTrailingNewline(RenderIndex(groups)));
    }

    // Written atomically (B-57): OpenApiExampleCatalog.Load can run concurrently in the same test process
    // (no [Collection] serializes DocsCaptureTests against OpenApiExampleTests/ClientRouteGuardTests/
    // ErrorCodeTests) and reads this exact file with a plain File.OpenRead. A direct File.WriteAllText
    // truncates first and fills in afterward - a reader landing in between sees either a locked file
    // (IOException) or a torn, partially-written one (JsonException). Writing to a temp file in the SAME
    // directory and renaming it in is a metadata-only operation on NTFS: any reader sees either the complete
    // old file or the complete new one, never a partial write.
    private void WriteOpenApiExamples()
    {
        var outDir = Path.Combine(RepoRoot(), "backend", "Pugling.Api", "OpenApi");
        Directory.CreateDirectory(outDir);

        var usedKeys = new HashSet<string>(StringComparer.Ordinal);
        var examples = _entries.Select(e => ToOpenApiExample(e, usedKeys)).ToList();
        var json = JsonSerializer.Serialize(examples, Indented);
        var finalPath = Path.Combine(outDir, "openapi-examples.generated.json");
        // Same directory as the final file - required for the rename to be a same-volume metadata operation
        // instead of a cross-volume copy, which would reintroduce exactly the torn-read window this avoids.
        var tempPath = Path.Combine(outDir, $"{Path.GetRandomFileName()}.tmp");
        try
        {
            File.WriteAllText(tempPath, json);
            MoveWithRetry(tempPath, finalPath);
        }
        catch
        {
            // A failed move must not leave a stray temp file behind in the source tree.
            if (File.Exists(tempPath)) File.Delete(tempPath);
            throw;
        }
    }

    // On Windows, replacing finalPath while OpenApiExampleCatalog.Load has it open for read (FileShare.Read,
    // no FileShare.Delete) briefly denies the rename itself - a transient window, not a real failure. A short
    // retry rides it out instead of surfacing a spurious crash from a reader that will have closed the handle
    // within microseconds.
    private static void MoveWithRetry(string tempPath, string finalPath)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                File.Move(tempPath, finalPath, overwrite: true);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException && attempt < 20)
            {
                Thread.Sleep(5);
            }
        }
    }

    // Non-JSON responses go into the catalog without a body: the transformer parses the value
    // (`JsonNode.Parse`) and attaches it to the operation's JSON media types - a markdown body would have no
    // business there. The call itself stays documented (path, role, status).
    private static OpenApiExampleEntry ToOpenApiExample(Entry entry, HashSet<string> usedKeys) =>
        new(UniqueKey(entry, usedKeys), entry.ResourceGroup, entry.Title, entry.Method, entry.Path, entry.Role,
            entry.RequestBodyJson, entry.ExpectedStatus, entry.IsJsonResponse ? entry.ResponseBodyJson : null,
            entry.IsError, entry.IsError ? TryReadCode(entry.ResponseBodyJson) : null);

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
            sb.AppendLine(e.IsError ? $"### {e.Title} — Fehlerfall" : $"## {e.Title}").AppendLine();
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
                sb.AppendLine("Request:").AppendLine().AppendLine("```json").AppendLine(rq).AppendLine("```").AppendLine();
            }

            var mediaNote = e.IsJsonResponse ? "" : $" (`{e.ResponseMediaType}`)";
            sb.AppendLine($"Response — `HTTP {e.ActualStatus}`{mediaNote}:").AppendLine();
            var body = Truncate(e.ResponseBodyJson) ?? "(kein Inhalt)";
            // Fence and language from the response type: the markdown export itself contains ```json blocks,
            // and a triple fence would close early and tear the page apart (CommonMark).
            var fence = new string('`', Math.Max(3, LongestBacktickRun(body) + 1));
            sb.AppendLine(fence + LanguageOf(e)).AppendLine(body).AppendLine(fence).AppendLine();
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

        // Error code coverage against the central registry.
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

        // Codes that cannot be captured automatically, with a reason.
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
                sb.AppendLine($"- `{code}` — {(reasons.TryGetValue(code, out var r) ? r : "Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.")}");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    // An `AppendLine` at the end of every section adds up to several blank lines (MD012); markdownlint also
    // wants exactly one trailing line break (MD047), not an extra one.
    private static string NormalizeTrailingNewline(string markdown) => markdown.TrimEnd() + "\n";

    /// <summary>Finds the repo root: upward from <see cref="AppContext.BaseDirectory"/> until <c>backend</c>+<c>docs</c> (or <c>.git</c>) are present.</summary>
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
