using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-628: the child reports a card, the supervisors see and handle it, the creator sees the counted reasons, and the child
/// gets a notice once. Every case runs on its own child, so the shared fixture's other cases cannot decide it. Case by case
/// the change that turns it red is named.
/// </summary>
public class ExerciseFeedbackTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private sealed record Kid(HttpClient Father, HttpClient Child, int ChildId, int ExerciseId, int PlanId, int PositionId)
    {
        public string FeedbackUrl => $"/api/v1/student/study-plans/{PlanId}/positions/{PositionId}/feedback";
    }

    // Own child of the seeded father with one vocabulary position (hello/goodbye, typed).
    private async Task<Kid> KidAsync()
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Melder"), pin = "4711" }));
        var exerciseId = await TestApi.CreateVocabExerciseAsync(father);
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, (int)TestStage.FreeText, childId: childId);
        return new Kid(father, await TestApi.ChildAsync(factory, childId, "4711"), childId, exerciseId, planId, positionId);
    }

    private static async Task<JsonElement> OkJsonAsync(HttpResponseMessage res)
    {
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task AssertProblemAsync(HttpResponseMessage res, HttpStatusCode status, string code)
    {
        Assert.Equal(status, res.StatusCode);
        Assert.Equal(code, (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    private T WithDb<T>(Func<PuglingDbContext, T> body)
    {
        using var scope = factory.Services.CreateScope();
        return body(scope.ServiceProvider.GetRequiredService<PuglingDbContext>());
    }

    private List<ExerciseFeedback> Rows(int childId) =>
        WithDb(db => db.ExerciseFeedbacks.AsNoTracking().Where(f => f.ChildId == childId).OrderBy(f => f.Id).ToList());

    // A second, foreign supervisor (own account, no link to the kid).
    private async Task<(HttpClient Client, int Id)> OtherAdultAsync(string pin)
    {
        var id = await TestApi.IdAsync(await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Fremd"), pin }));
        return (await TestApi.AdultAsync(factory, id, pin), id);
    }

    /// <summary>
    /// AK 1: the child reports a card with two reasons and a text - one entry per reason, both bound to child, position,
    /// exercise and card (with its store word). Red if the card or the position is not stored, or if several reasons
    /// collapse into one entry.
    /// </summary>
    [Fact]
    public async Task Kind_meldet_eine_Karte_mit_zwei_Gruenden()
    {
        var k = await KidAsync();

        var receipt = await OkJsonAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl,
            new { reasons = new[] { "WrongTranslation", "TooHard" }, text = "heißt doch hallo", itemIndex = 1 }));

        Assert.Equal(2, receipt.GetProperty("ids").GetArrayLength());
        var rows = Rows(k.ChildId);
        Assert.Equal([FeedbackReason.WrongTranslation, FeedbackReason.TooHard], rows.Select(r => r.Reason!.Value));
        Assert.All(rows, r =>
        {
            Assert.Equal((k.PositionId, k.ExerciseId, 1, 1, "heißt doch hallo"),
                (r.PlanPositionId!.Value, r.ExerciseId, r.ItemIndex!.Value, r.Count, r.Text));
            Assert.NotNull(r.ExerciseItemId);
            Assert.NotNull(r.VocabularyId);
        });
    }

    /// <summary>
    /// AK 1, the refusals: without reason and text 400; a card beyond the exercise 404; somebody else's position 403 (the
    /// plan filter); the supervisor reporting on the child's behalf 403. Red if any of them writes an entry.
    /// </summary>
    [Fact]
    public async Task Meldung_ohne_Inhalt_fremde_Karte_fremde_Position_und_Vater_abgewiesen()
    {
        var k = await KidAsync();
        var other = await KidAsync();

        await AssertProblemAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl, new { reasons = Array.Empty<string>(), text = "  " }),
            HttpStatusCode.BadRequest, "validation_error");
        await AssertProblemAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl, new { reasons = new[] { "TooHard" }, itemIndex = 7 }),
            HttpStatusCode.NotFound, "item_not_found");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await k.Child.PostAsJsonAsync(other.FeedbackUrl, new { reasons = new[] { "TooHard" } })).StatusCode);
        await AssertProblemAsync(await k.Father.PostAsJsonAsync(k.FeedbackUrl, new { reasons = new[] { "TooHard" } }),
            HttpStatusCode.Forbidden, "forbidden");

        Assert.Empty(Rows(k.ChildId));
        Assert.Empty(Rows(other.ChildId));
    }

    /// <summary>
    /// AK 2: a report mid-round changes nothing of the game - the box of the card, the child's ledger and the session cursor
    /// are the same before and after. Red if reporting is wired into the review (a box drop, a booking, a skipped card).
    /// </summary>
    [Fact]
    public async Task Meldung_aendert_weder_Kasten_noch_Wallet_noch_Cursor()
    {
        var k = await KidAsync();
        var sessionId = await TestApi.StartPositionSessionAsync(k.Child, k.PlanId, k.PositionId);
        (await TestApi.PositionReviewAsync(k.Child, k.PlanId, k.PositionId, sessionId, 0, givenAnswer: "hallo"))
            .EnsureSuccessStatusCode();
        (int Box, int Ledger, int Cursor) Snapshot() => WithDb(db => (
            db.PositionItemProgress.Where(p => p.PlanPositionId == k.PositionId).Sum(p => p.Box),
            db.ChildPointsEntries.Count(e => e.ChildId == k.ChildId),
            db.PracticeSessions.Single(s => s.Id == sessionId).Cursor));
        var before = Snapshot();

        await OkJsonAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl, new { reasons = new[] { "TooHard", "TooStrict" }, itemIndex = 0 }));
        await OkJsonAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl, new { reasons = new[] { "TooHard" }, itemIndex = 1 }));

        Assert.Equal(before, Snapshot());
    }

    /// <summary>
    /// AK 3: while the child's test on the position runs, a report is refused with feedback_during_test; after submitting it
    /// is accepted. Red if the running test is not checked, or if a submitted test still locks.
    /// </summary>
    [Fact]
    public async Task Waehrend_des_Tests_gesperrt_nach_dem_Abgeben_erlaubt()
    {
        var k = await KidAsync();
        var testsUrl = $"/api/v1/student/study-plans/{k.PlanId}/positions/{k.PositionId}/tests";
        var attemptId = await TestApi.IdWithKeyAsync(await k.Child.PostAsJsonAsync(testsUrl, new { }), "attemptId");

        await AssertProblemAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl, new { reasons = new[] { "TooStrict" }, itemIndex = 0 }),
            HttpStatusCode.Conflict, "feedback_during_test");
        Assert.Empty(Rows(k.ChildId));

        var answers = new[] { new { itemIndex = 0, givenAnswer = "halo" }, new { itemIndex = 1, givenAnswer = "tschüss" } };
        (await k.Child.PostAsJsonAsync($"{testsUrl}/{attemptId}/submit", new { answers })).EnsureSuccessStatusCode();
        await OkJsonAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl, new { reasons = new[] { "TooStrict" }, itemIndex = 0 }));
        Assert.Single(Rows(k.ChildId));
    }

    /// <summary>
    /// AK 4: the same open reason on the same card is merged - one entry, count 2, both texts; a different card or a done
    /// entry starts a new one. Red if every report adds a row, or if the merge ignores the card or the status.
    /// </summary>
    [Fact]
    public async Task Gleicher_offener_Grund_an_derselben_Karte_wird_zusammengelegt()
    {
        var k = await KidAsync();
        await OkJsonAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl, new { reasons = new[] { "TooHard" }, text = "eins", itemIndex = 0 }));
        await OkJsonAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl, new { reasons = new[] { "TooHard" }, text = "zwei", itemIndex = 0 }));
        await OkJsonAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl, new { reasons = new[] { "TooHard" }, itemIndex = 1 }));

        var rows = Rows(k.ChildId);
        Assert.Equal(2, rows.Count);
        Assert.Equal((2, "eins\nzwei"), (rows[0].Count, rows[0].Text));

        (await k.Father.PatchAsJsonAsync($"/api/v1/supervisor/feedback/{rows[0].Id}", new { status = "Done" }))
            .EnsureSuccessStatusCode();
        await OkJsonAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl, new { reasons = new[] { "TooHard" }, itemIndex = 0 }));
        Assert.Equal(3, Rows(k.ChildId).Count);
    }

    /// <summary>
    /// AK 5: every supervisor of the child sees the report with card, solution, the child's answer and the text, filters by
    /// child and status, and marks it done for all; a foreign supervisor sees nothing and cannot change it. Red if the list
    /// is not limited to the caller's children, if done is per supervisor, or if the child's answer is missing.
    /// </summary>
    [Fact]
    public async Task Alle_Betreuer_sehen_und_erledigen_fremde_sehen_nichts()
    {
        var k = await KidAsync();
        var (mother, motherId) = await OtherAdultAsync("2323");
        (await k.Father.PostAsJsonAsync($"/api/v1/supervisor/children/{k.ChildId}/supervisors",
            new { supervisorId = motherId, relation = "Mother" })).EnsureSuccessStatusCode();
        var (stranger, _) = await OtherAdultAsync("3434");
        var sessionId = await TestApi.StartPositionSessionAsync(k.Child, k.PlanId, k.PositionId);
        (await TestApi.PositionReviewAsync(k.Child, k.PlanId, k.PositionId, sessionId, 0, givenAnswer: "halo"))
            .EnsureSuccessStatusCode();
        await OkJsonAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl, new { reasons = new[] { "TooStrict" }, text = "nur ein l", itemIndex = 0 }));

        var list = await OkJsonAsync(await k.Father.GetAsync($"/api/v1/supervisor/feedback?childId={k.ChildId}&status=Open"));
        var entry = Assert.Single(list.EnumerateArray());
        Assert.Equal(("hello", "hallo", "halo", "nur ein l", "Everyone"),
            (entry.GetProperty("cue").GetString(), entry.GetProperty("solution").GetString(),
             entry.GetProperty("childAnswer").GetString(), entry.GetProperty("text").GetString(),
             entry.GetProperty("reach").GetString()));
        var id = entry.GetProperty("id").GetInt32();

        Assert.Empty((await OkJsonAsync(await stranger.GetAsync("/api/v1/supervisor/feedback"))).EnumerateArray());
        await AssertProblemAsync(await stranger.GetAsync($"/api/v1/supervisor/feedback?childId={k.ChildId}"),
            HttpStatusCode.NotFound, "not_found");
        await AssertProblemAsync(await stranger.PatchAsJsonAsync($"/api/v1/supervisor/feedback/{id}", new { status = "Done" }),
            HttpStatusCode.NotFound, "not_found");

        await OkJsonAsync(await mother.PatchAsJsonAsync($"/api/v1/supervisor/feedback/{id}", new { status = "Done" }));
        var forFather = await OkJsonAsync(await k.Father.GetAsync($"/api/v1/supervisor/feedback?childId={k.ChildId}&status=Done"));
        Assert.Equal(id, Assert.Single(forFather.EnumerateArray()).GetProperty("id").GetInt32());
        Assert.Empty((await OkJsonAsync(await k.Father.GetAsync($"/api/v1/supervisor/feedback?childId={k.ChildId}&status=Open")))
            .EnumerateArray());
    }

    /// <summary>
    /// AK 6: the creator sees the counted reasons of the exercise - per reason in total and per card - without the child and
    /// without the text; a creator without write permission is refused. Red if the counts ignore the merge count, if text or
    /// child leak into the summary, or if any creator may read it.
    /// </summary>
    [Fact]
    public async Task Ersteller_sieht_gezaehlte_Gruende_ohne_Kind_und_Text()
    {
        var k = await KidAsync();
        await OkJsonAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl, new { reasons = new[] { "WrongTranslation" }, text = "geheim", itemIndex = 1 }));
        await OkJsonAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl, new { reasons = new[] { "WrongTranslation", "Liked" }, itemIndex = 1 }));
        await OkJsonAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl, new { text = "nur Text" }));

        var res = await k.Father.GetAsync($"/api/v1/creator/exercises/{k.ExerciseId}/feedback");
        var raw = await res.Content.ReadAsStringAsync();
        var summary = await OkJsonAsync(res);

        Assert.Equal(3, summary.GetProperty("total").GetInt32());
        var top = summary.GetProperty("byReason")[0];
        Assert.Equal(("WrongTranslation", 2, "Everyone"),
            (top.GetProperty("reason").GetString(), top.GetProperty("count").GetInt32(), top.GetProperty("reach").GetString()));
        var card = Assert.Single(summary.GetProperty("byCard").EnumerateArray());
        Assert.Equal(1, card.GetProperty("itemIndex").GetInt32());
        Assert.DoesNotContain("geheim", raw);
        Assert.DoesNotContain("childId", raw, StringComparison.OrdinalIgnoreCase);

        var (stranger, _) = await OtherAdultAsync("4545");
        await AssertProblemAsync(await stranger.GetAsync($"/api/v1/creator/exercises/{k.ExerciseId}/feedback"),
            HttpStatusCode.Forbidden, "not_owner");
    }

    /// <summary>
    /// AK 7: a done entry appears to the child once as a notice with the card's cue; the child acknowledges it and it is
    /// gone; the supervisor cannot acknowledge for the child, and an open entry is no notice. Red if notices list open
    /// entries, if seen is not stored, or if the supervisor's read swallows the notice.
    /// </summary>
    [Fact]
    public async Task Kind_sieht_den_Hinweis_nach_erledigt_genau_einmal()
    {
        var k = await KidAsync();
        var noticesUrl = $"/api/v1/student/children/{k.ChildId}/feedback-notices";
        var ids = (await OkJsonAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl,
            new { reasons = new[] { "WrongTranslation" }, itemIndex = 0 }))).GetProperty("ids");
        var id = ids[0].GetInt32();
        Assert.Empty((await OkJsonAsync(await k.Child.GetAsync(noticesUrl))).EnumerateArray());

        await OkJsonAsync(await k.Father.PatchAsJsonAsync($"/api/v1/supervisor/feedback/{id}", new { status = "Done" }));
        // The father may read along (preview); his read must not count as the child's.
        Assert.Single((await OkJsonAsync(await k.Father.GetAsync(noticesUrl))).EnumerateArray());
        await AssertProblemAsync(await k.Father.PostAsync($"{noticesUrl}/{id}/seen", null), HttpStatusCode.Forbidden, "forbidden");
        var notice = Assert.Single((await OkJsonAsync(await k.Child.GetAsync(noticesUrl))).EnumerateArray());
        Assert.Equal(("hello", "WrongTranslation"),
            (notice.GetProperty("cue").GetString(), notice.GetProperty("reason").GetString()));

        Assert.Equal(HttpStatusCode.NoContent, (await k.Child.PostAsync($"{noticesUrl}/{id}/seen", null)).StatusCode);
        Assert.Empty((await OkJsonAsync(await k.Child.GetAsync(noticesUrl))).EnumerateArray());
    }

    /// <summary>
    /// AK 8: the reasons come with their reach - reasons about the exercise reach everyone, reasons about the child only it.
    /// Red if a reason's reach is flipped (B-630 would shape content for everyone from one child's "too hard").
    /// </summary>
    [Fact]
    public async Task Gruende_tragen_ihre_Reichweite()
    {
        var k = await KidAsync();
        var reasons = (await OkJsonAsync(await k.Child.GetAsync("/api/v1/student/feedback-reasons"))).EnumerateArray()
            .ToDictionary(r => r.GetProperty("reason").GetString()!, r => r.GetProperty("reach").GetString());

        Assert.Equal(8, reasons.Count);
        Assert.Equal(["WrongTranslation", "TooMuchAsked", "TooStrict", "SeveralMeanings"],
            reasons.Where(r => r.Value == "Everyone").Select(r => r.Key));
    }

    /// <summary>
    /// Review finding: the list puts open reports first even though the status is stored as text ("Done" sorts before
    /// "Open"), so a bounded list never cuts off an open one. Red if the list orders by the column.
    /// </summary>
    [Fact]
    public async Task Offene_Meldungen_stehen_vor_erledigten()
    {
        var k = await KidAsync();
        var done = (await OkJsonAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl, new { reasons = new[] { "TooHard" }, itemIndex = 0 })))
            .GetProperty("ids")[0].GetInt32();
        var open = (await OkJsonAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl, new { reasons = new[] { "TooEasy" }, itemIndex = 1 })))
            .GetProperty("ids")[0].GetInt32();
        // Done last, so "newest report first" alone would put it on top as well.
        await OkJsonAsync(await k.Father.PatchAsJsonAsync($"/api/v1/supervisor/feedback/{done}", new { status = "Done" }));
        WithDb(db => db.ExerciseFeedbacks.Where(f => f.Id == done)
            .ExecuteUpdate(u => u.SetProperty(f => f.LastReportedAt, DateTime.UtcNow.AddHours(1))));

        var first = await OkJsonAsync(await k.Father.GetAsync($"/api/v1/supervisor/feedback?childId={k.ChildId}&take=1"));

        Assert.Equal(open, Assert.Single(first.EnumerateArray()).GetProperty("id").GetInt32());
    }

    /// <summary>
    /// AK 3, the other side of "today": a test left open on an earlier day is no running test and does not lock the
    /// position for good. Red if the lock ignores the attempt's day.
    /// </summary>
    [Fact]
    public async Task Offener_Test_von_gestern_sperrt_nicht()
    {
        var k = await KidAsync();
        await TestApi.IdWithKeyAsync(await k.Child.PostAsJsonAsync(
            $"/api/v1/student/study-plans/{k.PlanId}/positions/{k.PositionId}/tests", new { }), "attemptId");
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        WithDb(db => db.TestAttempts.Where(t => t.PlanPositionId == k.PositionId)
            .ExecuteUpdate(u => u.SetProperty(t => t.Day, yesterday)));

        await OkJsonAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl, new { reasons = new[] { "TooHard" }, itemIndex = 0 }));
    }

    // A token with the supervisor AND the student role, as a login issues it for an account carrying both profiles.
    private HttpClient DualRoleClient(int adultId, int childId)
    {
        var tokens = factory.Services.GetRequiredService<Pugling.Api.Auth.TokenService>();
        var (token, _) = tokens.IssueForAccount(new Account { Id = 1, DisplayName = "Doppelrolle" },
        [
            new AccountProfile { Role = ProfileRole.Supervisor, AdultId = adultId },
            new AccountProfile { Role = ProfileRole.Student, ChildId = childId },
        ]);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Review finding: an account that is supervisor of the kid and student of another child (child 1) passes the plan and
    /// child filters as supervisor - it must neither report the kid's card under child 1 nor acknowledge the kid's notice.
    /// Red if the student side does not compare its own child with the plan's (resp. the route's) child.
    /// </summary>
    [Fact]
    public async Task Doppelrolle_meldet_und_quittiert_nicht_fuer_ein_betreutes_Kind()
    {
        var k = await KidAsync();
        var dual = DualRoleClient(adultId: 1, childId: 1);

        await AssertProblemAsync(await dual.PostAsJsonAsync(k.FeedbackUrl, new { reasons = new[] { "TooHard" }, itemIndex = 0 }),
            HttpStatusCode.Forbidden, "forbidden");
        Assert.Empty(WithDb(db => db.ExerciseFeedbacks.Where(f => f.PlanPositionId == k.PositionId).ToList()));

        var id = (await OkJsonAsync(await k.Child.PostAsJsonAsync(k.FeedbackUrl, new { reasons = new[] { "TooHard" }, itemIndex = 0 })))
            .GetProperty("ids")[0].GetInt32();
        await OkJsonAsync(await k.Father.PatchAsJsonAsync($"/api/v1/supervisor/feedback/{id}", new { status = "Done" }));
        await AssertProblemAsync(await dual.PostAsync($"/api/v1/student/children/{k.ChildId}/feedback-notices/{id}/seen", null),
            HttpStatusCode.Forbidden, "forbidden");
        Assert.Null(Rows(k.ChildId).Single().SeenByChildAt);
    }
}
