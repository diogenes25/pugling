using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// B-377: the supervisor sends a child a short plain-text praise, the child sees its unread praise once with the
/// sender's name and stamps it seen, and the sender reads back when. Praise books nothing. Each case works on its own
/// fresh child so the unread lists are the case's alone.
/// </summary>
public class PraiseTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string Mine = "/api/v1/student/me/praise";

    private static string Url(int childId) => $"/api/v1/supervisor/children/{childId}/praise";

    private static async Task<JsonElement> Json(HttpResponseMessage res)
    {
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<(int Id, HttpClient Client)> KidAsync(HttpClient father, string pin)
    {
        var id = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Lobkind"), pin }));
        return (id, await TestApi.ChildAsync(factory, id, pin));
    }

    /// <summary>Registers a further adult and links it as co-supervisor of <paramref name="childId"/>; returns its client.</summary>
    private async Task<HttpClient> CoSupervisorAsync(HttpClient father, int childId, string name, string pin)
    {
        var id = await TestApi.IdAsync(await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name, email = (string?)null, pin }));
        (await father.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/supervisors",
            new { supervisorId = id, relation = "Mother" })).EnsureSuccessStatusCode();
        return await TestApi.AdultAsync(factory, id, pin);
    }

    private async Task<HttpClient> StrangerAsync(string pin)
    {
        var id = await TestApi.IdAsync(await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Fremd"), email = (string?)null, pin }));
        return await TestApi.AdultAsync(factory, id, pin);
    }

    private static Task<HttpResponseMessage> PraiseAsync(HttpClient supervisor, int childId, string text) =>
        supervisor.PostAsJsonAsync(Url(childId), new { text });

    private static async Task<string> CodeAsync(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString()!;

    /// <summary>
    /// AK 1 and AK 6: a praise is stored with sender, time and no stamp (201) and books nothing - balance, ledger and
    /// the child's stamp are unchanged. A supervisor who does not look after the child gets 404, a child login 403.
    /// Red when the ownership filter or the role gate is missing, or when a ledger entry hangs on the praise path.
    /// </summary>
    [Fact]
    public async Task Lob_Wird_Angelegt_Und_Bucht_Nichts()
    {
        factory.Clock.FreezeAt(new DateTime(2026, 11, 25, 17, 0, 0, DateTimeKind.Utc));
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, "3771");
        var before = Ledger(kid.Id);

        var res = await PraiseAsync(father, kid.Id, "Super gemacht!");
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var created = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Super gemacht!", created.GetProperty("text").GetString());
        Assert.Equal(JsonValueKind.Null, created.GetProperty("seenAt").ValueKind);
        Assert.Equal(new DateTime(2026, 11, 25, 17, 0, 0, DateTimeKind.Utc), created.GetProperty("createdAt").GetDateTime());
        Assert.Equal(before, Ledger(kid.Id));

        var stranger = await StrangerAsync("3772");
        Assert.Equal(HttpStatusCode.NotFound, (await PraiseAsync(stranger, kid.Id, "Hallo")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PraiseAsync(kid.Client, kid.Id, "Selbstlob")).StatusCode);
    }

    /// <summary>
    /// AK 2: empty, blank and 141 characters are 400 <c>validation_error</c>; exactly 140 characters with surrounding
    /// blanks is 201 and stored trimmed. Red when the bound is missing or counted before trimming.
    /// </summary>
    [Fact]
    public async Task Text_Wird_Getrimmt_Und_Auf_140_Zeichen_Begrenzt()
    {
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, "3773");

        foreach (var bad in new[] { "", "   ", new string('x', 141) })
        {
            var res = await PraiseAsync(father, kid.Id, bad);
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            Assert.Equal("validation_error", await CodeAsync(res));
        }

        var exact = new string('y', 140);
        var ok = await Json(await PraiseAsync(father, kid.Id, "  " + exact + "  "));
        Assert.Equal(exact, ok.GetProperty("text").GetString());
    }

    /// <summary>
    /// AK 3 and AK 4: the sender lists only his own praise, newest first, with the total in <c>X-Total-Count</c>; the
    /// child lists its unread praise of both supervisors with their names, newest first, at most five, and never a
    /// sibling's. A supervisor login on the child route is 403. Red when the co-supervisor's praise shows in the
    /// father's list, when the sixth praise or a sibling's appears, or when the order flips.
    /// </summary>
    [Fact]
    public async Task Jeder_Sieht_Nur_Seine_Lobe()
    {
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, "3774");
        var sibling = await KidAsync(father, "3775");
        var mother = await CoSupervisorAsync(father, kid.Id, "Mama Lob", "3776");

        var start = new DateTime(2026, 11, 25, 8, 0, 0, DateTimeKind.Utc);
        for (var i = 1; i <= 5; i++)
        {
            factory.Clock.FreezeAt(start.AddMinutes(i));
            await Json(await PraiseAsync(father, kid.Id, $"Papa {i}"));
        }
        factory.Clock.FreezeAt(start.AddMinutes(10));
        await Json(await PraiseAsync(mother, kid.Id, "Mama 1"));
        await Json(await PraiseAsync(father, sibling.Id, "Fürs Geschwister"));

        var sent = await father.GetAsync(Url(kid.Id));
        var fathers = (await Json(sent)).EnumerateArray().Select(p => p.GetProperty("text").GetString()).ToList();
        Assert.Equal(["Papa 5", "Papa 4", "Papa 3", "Papa 2", "Papa 1"], fathers);
        Assert.Equal("5", sent.Headers.GetValues("X-Total-Count").Single());

        var unread = (await Json(await kid.Client.GetAsync(Mine))).EnumerateArray().ToList();
        Assert.Equal(["Mama 1", "Papa 5", "Papa 4", "Papa 3", "Papa 2"], unread.Select(p => p.GetProperty("text").GetString()));
        Assert.Equal("Mama Lob", unread[0].GetProperty("supervisorName").GetString());

        Assert.Equal(HttpStatusCode.Forbidden, (await father.GetAsync(Mine)).StatusCode);
    }

    /// <summary>
    /// AK 5: "Thanks!" stamps only the child's own unread praise - a sibling's id in the same call stays unread - and a
    /// second call keeps the first stamp. After it the child's list is empty and the sender reads <c>seenAt</c>. Red when
    /// a foreign praise is stamped or the second call overwrites the stamp.
    /// </summary>
    [Fact]
    public async Task Gesehen_Stempelt_Nur_Eigene_Und_Nur_Einmal()
    {
        var father = await TestApi.AdultAsync(factory);
        var kid = await KidAsync(father, "3777");
        var sibling = await KidAsync(father, "3778");
        factory.Clock.FreezeAt(new DateTime(2026, 11, 25, 9, 0, 0, DateTimeKind.Utc));
        var own = (await Json(await PraiseAsync(father, kid.Id, "Klasse"))).GetProperty("id").GetInt32();
        var foreign = (await Json(await PraiseAsync(father, sibling.Id, "Auch klasse"))).GetProperty("id").GetInt32();

        var firstStamp = new DateTime(2026, 11, 25, 10, 0, 0, DateTimeKind.Utc);
        factory.Clock.FreezeAt(firstStamp);
        Assert.Equal(HttpStatusCode.NoContent,
            (await kid.Client.PostAsJsonAsync($"{Mine}/seen", new { ids = new[] { own, foreign } })).StatusCode);
        factory.Clock.FreezeAt(firstStamp.AddHours(1));
        Assert.Equal(HttpStatusCode.NoContent,
            (await kid.Client.PostAsJsonAsync($"{Mine}/seen", new { ids = new[] { own } })).StatusCode);

        Assert.Empty((await Json(await kid.Client.GetAsync(Mine))).EnumerateArray());
        Assert.Single((await Json(await sibling.Client.GetAsync(Mine))).EnumerateArray());
        var sent = (await Json(await father.GetAsync(Url(kid.Id)))).EnumerateArray().Single();
        Assert.Equal(firstStamp, sent.GetProperty("seenAt").GetDateTime());
    }

    private (int Count, int Sum, Guid Stamp) Ledger(int childId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var entries = db.ChildPointsEntries.AsNoTracking().Where(p => p.ChildId == childId).Select(p => p.Amount).ToList();
        var stamp = db.Children.AsNoTracking().Single(c => c.Id == childId).ConcurrencyStamp;
        return (entries.Count, entries.Sum(), stamp);
    }
}
