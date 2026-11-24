using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-760: a child's homework - set by the supervisor, handed in by the child, accepted (coins, once) or given back (note,
/// due day stays). The penalty comes lazily at the next login or purchase, once, and only for a homework that was set before
/// its due day was over and got no hand-in by the end of it, at most 14 days back. The test zone is UTC (factory), so the
/// learning day is the UTC calendar day.
/// </summary>
public class HausaufgabenTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage res)
    {
        Assert.True(res.IsSuccessStatusCode, $"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static string Url(int childId) => $"/api/v1/supervisor/children/{childId}/homework";

    private async Task<(HttpClient Father, int ChildId)> FreshChildAsync(string pin, object? amounts = null)
    {
        var father = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("HA-Kind"), pin }));
        if (amounts is not null)
            (await father.PatchAsJsonAsync($"/api/v1/supervisor/children/{childId}", amounts)).EnsureSuccessStatusCode();
        return (father, childId);
    }

    /// <summary>Sets a homework through the API and backdates its creation to <paramref name="createdOn"/> (noon UTC).</summary>
    private async Task<int> HomeworkAsync(HttpClient father, int childId, DateOnly dueOn, DateOnly? createdOn = null,
        string text = "S. 12 Nr. 3")
    {
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync(Url(childId), new { text, dueOn }));
        if (createdOn is { } day) Backdate(id, day);
        return id;
    }

    private void Backdate(int homeworkId, DateOnly createdOn)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var at = createdOn.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);
        db.Homeworks.Where(h => h.Id == homeworkId).ExecuteUpdate(h => h.SetProperty(x => x.CreatedAt, at));
    }

    private void AddSubmission(int homeworkId, DateTime submittedAt)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        db.HomeworkSubmissions.Add(new HomeworkSubmission { HomeworkId = homeworkId, SubmittedAt = submittedAt });
        db.Homeworks.Where(h => h.Id == homeworkId).ExecuteUpdate(h => h.SetProperty(x => x.Status, HomeworkStatus.Submitted));
        db.SaveChanges();
    }

    private T WithDb<T>(Func<PuglingDbContext, T> read)
    {
        using var scope = factory.Services.CreateScope();
        return read(scope.ServiceProvider.GetRequiredService<PuglingDbContext>());
    }

    private int Penalties(int childId) =>
        WithDb(db => db.ChildPointsEntries.Count(e => e.ChildId == childId && e.Kind == PointKind.GoalPenalty));

    private async Task<HttpClient> SubmitAsync(int childId, string pin, int homeworkId)
    {
        var child = await TestApi.ChildAsync(factory, childId, pin);
        await JsonAsync(await child.PostAsync($"/api/v1/student/homework/{homeworkId}/submissions", null));
        return child;
    }

    /// <summary>
    /// AK 1 and 7: the child sees only its own homework and hands in only its own; somebody else's answers like an unknown
    /// one. Red when <c>SubmitAsync</c> or <c>ListAsync</c> drops the child filter.
    /// </summary>
    [Fact]
    public async Task Kind_Sieht_Und_Abgibt_Nur_Eigene_Hausaufgaben()
    {
        var (father, mine) = await FreshChildAsync("7601");
        var other = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("HA-Geschwister"), pin = "7602" }));
        var own = await HomeworkAsync(father, mine, Today.AddDays(2), text: "Eigene");
        var foreign = await HomeworkAsync(father, other, Today.AddDays(2), text: "Fremde");

        var child = await TestApi.ChildAsync(factory, mine, "7601");
        var list = await JsonAsync(await child.GetAsync("/api/v1/student/homework"));
        Assert.Equal([own], list.EnumerateArray().Select(h => h.GetProperty("id").GetInt32()));

        var steal = await child.PostAsync($"/api/v1/student/homework/{foreign}/submissions", null);
        Assert.Equal(HttpStatusCode.NotFound, steal.StatusCode);
        Assert.Equal(0, WithDb(db => db.HomeworkSubmissions.Count(s => s.HomeworkId == foreign)));

        // The supervisor reads under his own route; the child's seat has no list for him.
        Assert.Equal(HttpStatusCode.Forbidden, (await father.GetAsync("/api/v1/student/homework")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await father.PostAsync($"/api/v1/student/homework/{own}/submissions", null)).StatusCode);
    }

    /// <summary>
    /// AK 2: no hand-in by the due day costs the child's own penalty once at login, bumps the wallet stamp, and a second
    /// login books nothing more. Red when the settlement books twice, forgets the stamp or ignores the child's amount.
    /// </summary>
    [Fact]
    public async Task Ohne_Abgabe_Kostet_Den_Malus_Des_Kindes_Genau_Einmal()
    {
        var (father, childId) = await FreshChildAsync("7603", new { homeworkPenaltyCoins = 7 });
        var homeworkId = await HomeworkAsync(father, childId, Today.AddDays(-1), createdOn: Today.AddDays(-3));
        var stamp = WithDb(db => db.Children.Where(c => c.Id == childId).Select(c => c.ConcurrencyStamp).Single());

        var child = await TestApi.ChildAsync(factory, childId, "7603");
        var entries = await JsonAsync(await child.GetAsync("/api/v1/student/me/points/entries"));
        var penalty = Assert.Single(entries.EnumerateArray(), e => e.GetProperty("kind").GetString() == "GoalPenalty");
        Assert.Equal(-7, penalty.GetProperty("amount").GetInt32());
        Assert.Contains("Hausaufgabe", penalty.GetProperty("reason").GetString());
        Assert.NotEqual(stamp, WithDb(db => db.Children.Where(c => c.Id == childId).Select(c => c.ConcurrencyStamp).Single()));

        await TestApi.ChildAsync(factory, childId, "7603");
        Assert.Equal(1, Penalties(childId));
        var view = Assert.Single((await JsonAsync(await father.GetAsync(Url(childId)))).EnumerateArray());
        Assert.Equal(homeworkId, view.GetProperty("id").GetInt32());
        Assert.True(view.GetProperty("penaltyCharged").GetBoolean());
        Assert.Equal(7, view.GetProperty("penaltyCoins").GetInt32());
    }

    /// <summary>
    /// AK 2, second half: a hand-in by the end of the due day averts the penalty, also when the supervisor gave it back for
    /// rework. Red when a rework note brings the penalty back or the settlement ignores hand-ins.
    /// </summary>
    [Fact]
    public async Task Abgabe_Bis_Zur_Frist_Und_Nacharbeit_Kosten_Nichts()
    {
        var (father, childId) = await FreshChildAsync("7604");
        var homeworkId = await HomeworkAsync(father, childId, Today.AddDays(-1), createdOn: Today.AddDays(-3));
        // Handed in on the due day, 23:30 - the last half hour still counts. Written directly: a login through the API would
        // already settle before the hand-in could be dated back.
        AddSubmission(homeworkId, Today.AddDays(-1).ToDateTime(new TimeOnly(23, 30), DateTimeKind.Utc));
        await JsonAsync(await father.PostAsJsonAsync($"{Url(childId)}/{homeworkId}/rework", new { note = "12d fehlt" }));

        await TestApi.ChildAsync(factory, childId, "7604");
        Assert.Equal(0, Penalties(childId));
        var view = Assert.Single((await JsonAsync(await father.GetAsync(Url(childId)))).EnumerateArray());
        Assert.True(view.GetProperty("submissions")[0].GetProperty("inTime").GetBoolean());
    }

    /// <summary>
    /// B-687 decision 2: a hand-in after the due day averts nothing - a due day is a due day. Red when the settlement counts
    /// any hand-in instead of one by the end of the due day.
    /// </summary>
    [Fact]
    public async Task Spaete_Abgabe_Wendet_Den_Malus_Nicht_Ab()
    {
        var (father, childId) = await FreshChildAsync("7605");
        var homeworkId = await HomeworkAsync(father, childId, Today.AddDays(-2), createdOn: Today.AddDays(-4));
        // Handed in yesterday morning, a day late; nobody logged in in between, so nothing is charged yet.
        AddSubmission(homeworkId, Today.AddDays(-1).ToDateTime(new TimeOnly(0, 30), DateTimeKind.Utc));

        await TestApi.ChildAsync(factory, childId, "7605");
        Assert.Equal(1, Penalties(childId));
    }

    /// <summary>
    /// AK 3: a homework set after its due day was over costs nothing, nor one due more than 14 days back; one due exactly 14
    /// days back still does (the same reach as the duty's settlement). Red when the settlement does not read the creation or
    /// the window.
    /// </summary>
    [Fact]
    public async Task Nachtrag_Und_Alte_Frist_Kosten_Nichts()
    {
        var (father, childId) = await FreshChildAsync("7606");
        await HomeworkAsync(father, childId, Today.AddDays(-3), createdOn: Today.AddDays(-2), text: "Nachgetragen");
        await HomeworkAsync(father, childId, Today.AddDays(-15), createdOn: Today.AddDays(-20), text: "Zu alt");
        var edge = await HomeworkAsync(father, childId, Today.AddDays(-14), createdOn: Today.AddDays(-20), text: "Grenze");

        await TestApi.ChildAsync(factory, childId, "7606");
        Assert.Equal([edge], WithDb(db => db.HomeworkPenalties
            .Where(p => p.Homework!.ChildId == childId).Select(p => p.HomeworkId).ToList()));
    }

    /// <summary>
    /// AK 4: acceptance books the child's amount once as HomeworkCoins, a second acceptance is refused, and the coins count as
    /// earned this week. Accepting without a hand-in is refused too. Red when acceptance is not idempotent, uses the server
    /// default over the child's amount, or the kind falls out of the earned coins.
    /// </summary>
    [Fact]
    public async Task Annahme_Bucht_Einmal_Und_Zaehlt_Als_Verdient()
    {
        var (father, childId) = await FreshChildAsync("7607", new { homeworkCoins = 25 });
        var homeworkId = await HomeworkAsync(father, childId, Today.AddDays(1));

        var early = await father.PostAsync($"{Url(childId)}/{homeworkId}/accept", null);
        Assert.Equal(HttpStatusCode.Conflict, early.StatusCode);
        Assert.Contains("homework_not_submitted", await early.Content.ReadAsStringAsync());

        var child = await SubmitAsync(childId, "7607", homeworkId);
        var stamp = WithDb(db => db.Children.Where(c => c.Id == childId).Select(c => c.ConcurrencyStamp).Single());
        var accepted = await JsonAsync(await father.PostAsync($"{Url(childId)}/{homeworkId}/accept", null));
        Assert.Equal("Accepted", accepted.GetProperty("status").GetString());
        Assert.Equal(25, accepted.GetProperty("rewardCoins").GetInt32());
        Assert.NotEqual(stamp, WithDb(db => db.Children.Where(c => c.Id == childId).Select(c => c.ConcurrencyStamp).Single()));

        var again = await father.PostAsync($"{Url(childId)}/{homeworkId}/accept", null);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Contains("homework_accepted", await again.Content.ReadAsStringAsync());
        var resubmit = await child.PostAsync($"/api/v1/student/homework/{homeworkId}/submissions", null);
        Assert.Equal(HttpStatusCode.Conflict, resubmit.StatusCode);

        var coins = WithDb(db => db.ChildPointsEntries
            .Where(e => e.ChildId == childId && e.Kind == PointKind.HomeworkCoins).Select(e => e.Amount).ToList());
        Assert.Equal([25], coins);
        var week = await JsonAsync(await child.GetAsync("/api/v1/student/me/points/week"));
        Assert.Equal(25, week.GetProperty("thisWeek").GetInt32());
    }

    /// <summary>
    /// AK 5: a rework note is readable on the homework, the due day stays, and a new hand-in keeps the note. An empty note is
    /// refused. Red when the note is lost or the due day moves.
    /// </summary>
    [Fact]
    public async Task Nacharbeit_Ist_Lesbar_Und_Verschiebt_Die_Frist_Nicht()
    {
        var (father, childId) = await FreshChildAsync("7608");
        var due = Today.AddDays(2);
        var homeworkId = await HomeworkAsync(father, childId, due);
        var child = await SubmitAsync(childId, "7608", homeworkId);

        var empty = await father.PostAsJsonAsync($"{Url(childId)}/{homeworkId}/rework", new { note = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);

        var back = await JsonAsync(await father.PostAsJsonAsync($"{Url(childId)}/{homeworkId}/rework", new { note = "12d fehlt" }));
        Assert.Equal("Rework", back.GetProperty("status").GetString());
        // Given back, it waits for the next hand-in: accepting it now is refused (a hand-in at the same moment would otherwise
        // overwrite "accepted" and open a second booking - review finding of Sprint 141).
        var early = await father.PostAsync($"{Url(childId)}/{homeworkId}/accept", null);
        Assert.Equal(HttpStatusCode.Conflict, early.StatusCode);
        Assert.Contains("homework_not_submitted", await early.Content.ReadAsStringAsync());
        var seen = Assert.Single((await JsonAsync(await child.GetAsync("/api/v1/student/homework"))).EnumerateArray());
        Assert.Equal("12d fehlt", seen.GetProperty("reworkNote").GetString());
        Assert.Equal(due.ToString("yyyy-MM-dd"), seen.GetProperty("dueOn").GetString());

        var again = await JsonAsync(await child.PostAsync($"/api/v1/student/homework/{homeworkId}/submissions", null));
        Assert.Equal("Submitted", again.GetProperty("status").GetString());
        Assert.Equal("12d fehlt", again.GetProperty("reworkNote").GetString());
        Assert.Equal(2, again.GetProperty("submissions").GetArrayLength());
    }

    /// <summary>
    /// AK 6, <b>declaration of intent</b>: an open, overdue homework changes nothing of the child's day - no duty, no lock.
    /// The starting state (nothing reads homework there) already holds this; only code that pulls homework into the day's
    /// duty would turn it red.
    /// </summary>
    [Fact]
    public async Task Offene_Hausaufgabe_Aendert_Den_Tag_Nicht()
    {
        var (father, childId) = await FreshChildAsync("7609");
        var child = await TestApi.ChildAsync(factory, childId, "7609");
        var before = await (await child.GetAsync($"/api/v1/student/children/{childId}/today")).Content.ReadAsStringAsync();

        await HomeworkAsync(father, childId, Today, createdOn: Today.AddDays(-1));
        var after = await (await child.GetAsync($"/api/v1/student/children/{childId}/today")).Content.ReadAsStringAsync();
        Assert.Equal(before, after);
    }

    /// <summary>
    /// AK 10: the purchase settles the homework penalty before its funds check, like the duty's - a child that buys before
    /// its next login does not escape it. Red when the settlement runs only at login: the purchase then goes through.
    /// </summary>
    [Fact]
    public async Task Kauf_Rechnet_Den_Malus_Vor_Der_Kontopruefung_Ab()
    {
        var (father, childId) = await FreshChildAsync("7610", new { homeworkPenaltyCoins = 5 });
        var child = await TestApi.ChildAsync(factory, childId, "7610"); // logged in before the homework exists
        await HomeworkAsync(father, childId, Today.AddDays(-1), createdOn: Today.AddDays(-2));
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/points", new { amount = 10, reason = "Start" }))
            .EnsureSuccessStatusCode();
        var listing = await TestApi.CreateShopListingAsync(father, TestApi.UniqueName("HA"), coinPrice: 10, unitsPerPurchase: 1, stock: 5);

        var buy = await child.PostAsync($"/api/v1/student/me/shop/listings/{listing}/purchase", null);
        Assert.Equal(HttpStatusCode.BadRequest, buy.StatusCode);
        Assert.Contains("insufficient_coins", await buy.Content.ReadAsStringAsync());
        Assert.Equal(1, Penalties(childId));
    }

    /// <summary>
    /// The supervisor bounds his list with <c>take</c> (latest due day first) and removes a homework. Red when the delete ignores
    /// the child in the route or <c>take</c> cuts from the wrong end.
    /// </summary>
    [Fact]
    public async Task Vater_Begrenzt_Die_Liste_Und_Loescht()
    {
        var (father, childId) = await FreshChildAsync("7611");
        var homeworkId = await HomeworkAsync(father, childId, Today.AddDays(3));
        var (_, otherChild) = await FreshChildAsync("7612");

        Assert.Equal(HttpStatusCode.NotFound, (await father.DeleteAsync($"{Url(otherChild)}/{homeworkId}")).StatusCode);
        var later = await HomeworkAsync(father, childId, Today.AddDays(5));
        var first = Assert.Single((await JsonAsync(await father.GetAsync($"{Url(childId)}?take=1"))).EnumerateArray());
        Assert.Equal(later, first.GetProperty("id").GetInt32()); // latest due day first
        Assert.Equal(HttpStatusCode.BadRequest, (await father.GetAsync($"{Url(childId)}?take=0")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync($"{Url(childId)}/{later}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await father.DeleteAsync($"{Url(childId)}/{homeworkId}")).StatusCode);
        Assert.Equal(0, (await JsonAsync(await father.GetAsync(Url(childId)))).GetArrayLength());
    }

    /// <summary>A homework needs a text and a due day, and a subject must exist.</summary>
    [Fact]
    public async Task Anlegen_Prueft_Text_Und_Fach()
    {
        var (father, childId) = await FreshChildAsync("7613");
        Assert.Equal(HttpStatusCode.BadRequest,
            (await father.PostAsJsonAsync(Url(childId), new { text = " ", dueOn = Today })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await father.PostAsJsonAsync(Url(childId), new { text = "ohne Frist" })).StatusCode);
        var unknown = await father.PostAsJsonAsync(Url(childId), new { text = "x", dueOn = Today, subjectId = 999999 });
        Assert.Contains("invalid_reference", await unknown.Content.ReadAsStringAsync());

        var subjectId = await TestApi.DefaultSubjectIdAsync(father);
        var created = await JsonAsync(await father.PostAsJsonAsync(Url(childId), new { text = "Vokabeln", dueOn = Today, subjectId }));
        Assert.Equal(subjectId, created.GetProperty("subjectId").GetInt32());
        Assert.False(string.IsNullOrEmpty(created.GetProperty("subjectName").GetString()));
        Assert.Equal(20, created.GetProperty("rewardCoins").GetInt32()); // the server default without an own amount
    }
}
