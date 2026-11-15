using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Controllers;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// B-122: the six top-level collections take an optional <c>skip</c>/<c>take</c> without a default.
/// Without <c>take</c> they still return everything; <c>X-Total-Count</c> is set in both cases, and every
/// ordering ends on <c>Id</c> so a window over equal names is deterministic.
/// </summary>
public class TopLevelPagingTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static int _pinSeq = 7100;

    /// <summary>A fresh adult (creator + supervisor) of its own, so the lists hold only what the case seeds.</summary>
    private async Task<HttpClient> FreshAdultAsync()
    {
        var pin = Interlocked.Increment(ref _pinSeq).ToString();
        var id = await TestApi.IdAsync(await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Paging-Papa"), pin }));
        return await TestApi.AdultAsync(factory, id, pin);
    }

    private static async Task<int> CreateChildAsync(HttpClient adult, string name, string? pin = null) =>
        await TestApi.IdAsync(await adult.PostAsJsonAsync("/api/v1/supervisor/children", new { name, pin }));

    /// <summary>
    /// Seeds at least two rows behind <paramref name="route"/> and returns the client allowed to read it plus
    /// the list URL (with a trailing <c>?</c> or <c>&amp;</c> ready for the paging parameters).
    /// </summary>
    private async Task<(HttpClient client, string url)> SeedAsync(string route)
    {
        var adult = await FreshAdultAsync();
        switch (route)
        {
            case "subjects":
                for (var i = 0; i < 2; i++)
                    (await adult.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("Paging-Fach") })).EnsureSuccessStatusCode();
                return (adult, "/api/v1/creator/subjects?");
            case "tags":
                {
                    var childId = await CreateChildAsync(adult, "Tag-Kind");
                    foreach (var name in new[] { "Beta", "Alpha" })
                        (await adult.PostAsJsonAsync("/api/v1/creator/tags", new { childId, name })).EnsureSuccessStatusCode();
                    return (adult, $"/api/v1/creator/tags?childId={childId}&");
                }
            case "vocabulary-tags":
                for (var i = 0; i < 2; i++)
                    (await adult.PostAsJsonAsync("/api/v1/creator/vocabulary/tags", new { name = TestApi.UniqueName("Paging-Vokabeltag") })).EnsureSuccessStatusCode();
                return (adult, "/api/v1/creator/vocabulary/tags?");
            case "children":
                await CreateChildAsync(adult, "Zora");
                await CreateChildAsync(adult, "Anton");
                return (adult, "/api/v1/supervisor/children?");
            case "study-plans":
                {
                    var childId = await CreateChildAsync(adult, "Plan-Kind");
                    await TestApi.CreateEmptyPlanAsync(adult, childId);
                    await TestApi.CreateEmptyPlanAsync(adult, childId);
                    return (adult, $"/api/v1/supervisor/study-plans?childId={childId}&");
                }
            case "student-plans":
                {
                    var pin = Interlocked.Increment(ref _pinSeq).ToString();
                    var childId = await CreateChildAsync(adult, "Spiel-Kind", pin);
                    var a = await TestApi.CreateEmptyPlanAsync(adult, childId);
                    var b = await TestApi.CreateEmptyPlanAsync(adult, childId);
                    // The product keeps one playable plan per child; two are forced in here only so a window over
                    // the student list has something to cut - the endpoint itself does not enforce the invariant.
                    using (var scope = factory.Services.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
                        var today = DateOnly.FromDateTime(DateTime.UtcNow);
                        foreach (var plan in db.StudyPlans.Where(p => p.Id == a || p.Id == b).ToList())
                        {
                            plan.Active = true;
                            plan.StartDate = today.AddDays(-1);
                            plan.EndDate = today.AddDays(5);
                        }
                        db.SaveChanges();
                    }
                    return (await TestApi.ChildAsync(factory, childId, pin), "/api/v1/student/study-plans?");
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(route), route, null);
        }
    }

    private static int TotalCount(HttpResponseMessage res) =>
        int.Parse(res.Headers.GetValues("X-Total-Count").Single());

    private static async Task<int[]> IdsAsync(HttpResponseMessage res)
    {
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var arr = await res.Content.ReadFromJsonAsync<JsonElement>();
        return [.. arr.EnumerateArray().Select(e => e.GetProperty("id").GetInt32())];
    }

    public static TheoryData<string> Routes =>
        ["subjects", "tags", "vocabulary-tags", "children", "study-plans", "student-plans"];

    /// <summary>
    /// AK 1 + AK 3. Red when an endpoint accepts <c>take</c> but does not apply it (the <c>take</c> branch of
    /// <c>ToOptionallyPagedListAsync</c> commented out ⇒ <c>take=1</c> returns two rows), when it ignores
    /// <c>skip</c> (second page equals the first), or when the header is only set inside the <c>take</c>
    /// branch (no <c>X-Total-Count</c> on the unbounded call ⇒ <c>GetValues</c> throws).
    /// </summary>
    [Theory]
    [MemberData(nameof(Routes))]
    public async Task Fenster_Folgt_Der_Unbegrenzten_Antwort_Und_Der_Kopf_Steht_Immer(string route)
    {
        var (client, url) = await SeedAsync(route);

        var full = await client.GetAsync(url);
        var all = await IdsAsync(full);
        Assert.True(all.Length >= 2, $"{route}: at least two rows expected, got {all.Length}.");
        Assert.Equal(all.Length, TotalCount(full));

        var first = await client.GetAsync(url + "take=1");
        Assert.Equal([all[0]], await IdsAsync(first));
        Assert.Equal(all.Length, TotalCount(first));

        var second = await client.GetAsync(url + "skip=1&take=1");
        Assert.Equal([all[1]], await IdsAsync(second));
        Assert.Equal(all.Length, TotalCount(second));
    }

    /// <summary>
    /// AK 2. Red when the helper falls back to <see cref="PagingExtensions.DefaultTake"/> for
    /// <c>take == null</c>: 101 own subjects plus the seed exceed the default window.
    /// </summary>
    [Fact]
    public async Task Ohne_Take_Kommen_Alle_Zeilen_Auch_Ueber_DefaultTake()
    {
        var adult = await FreshAdultAsync();
        for (var i = 0; i < PagingExtensions.DefaultTake + 1; i++)
            (await adult.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("Massen-Fach") })).EnsureSuccessStatusCode();

        var res = await adult.GetAsync("/api/v1/creator/subjects");
        var ids = await IdsAsync(res);
        Assert.True(ids.Length > PagingExtensions.DefaultTake, $"{ids.Length} subjects returned, more than {PagingExtensions.DefaultTake} expected.");
        Assert.Equal(ids.Length, TotalCount(res));
    }

    /// <summary>
    /// AK 4. Red when the tie between equal names is broken by anything but ascending <c>Id</c>
    /// (probe: <c>ThenByDescending(c =&gt; c.Id)</c> in <c>ChildrenController.List</c>).
    /// </summary>
    [Fact]
    public async Task Gleichnamige_Kinder_Kommen_Nach_Id_Aufsteigend()
    {
        var adult = await FreshAdultAsync();
        var first = await CreateChildAsync(adult, "Max");
        var second = await CreateChildAsync(adult, "Max");
        Assert.True(first < second);

        Assert.Equal([first], await IdsAsync(await adult.GetAsync("/api/v1/supervisor/children?take=1")));
        Assert.Equal([second], await IdsAsync(await adult.GetAsync("/api/v1/supervisor/children?skip=1&take=1")));
    }

    /// <summary>
    /// AK 5. Red when the ownership guard moves behind the paging (or is dropped): a foreign child's tags
    /// must stay <c>403</c> even with a window.
    /// </summary>
    [Fact]
    public async Task Fremdes_Kind_Bleibt_Gesperrt_Auch_Mit_Take()
    {
        var stranger = await FreshAdultAsync();
        var res = await stranger.GetAsync("/api/v1/creator/tags?childId=1&take=1");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
