using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-747: <c>GET supervisor/shop/activations</c> lists the activation requests of all of a father's
/// children in one call - issuer-bound and limited to children still linked to him.
/// </summary>
public class ShopActivationsOverviewTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private const string Url = "/api/v1/supervisor/shop/activations";

    private async Task<(int id, HttpClient client)> NewAdultAsync(string name, string pin)
    {
        var id = await TestApi.IdAsync(await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name, email = (string?)null, pin }));
        return (id, await TestApi.AdultAsync(factory, id, pin));
    }

    private static async Task<int> NewChildAsync(HttpClient adult, string name) =>
        await TestApi.IdAsync(await adult.PostAsJsonAsync("/api/v1/supervisor/children", new { name, pin = "5151" }));

    private int AddRequest(int childId, int supervisorId, ActivationRequestStatus status, DateTime requestedAt)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var r = new ActivationRequest
        {
            ChildId = childId,
            SupervisorId = supervisorId,
            RequestedQuantity = 10,
            Status = status,
            ArticleTitle = "Fernsehen",
            UnitType = UnitType.Minute,
            ActionType = ActionType.TV,
            RequestedAt = requestedAt,
            ClosedAt = status == ActivationRequestStatus.Pending ? null : requestedAt,
        };
        db.ActivationRequests.Add(r);
        db.SaveChanges();
        return r.Id;
    }

    private static async Task<(int[] ids, int total)> ListAsync(HttpClient adult, string query = "")
    {
        var res = await adult.GetAsync(Url + query);
        res.EnsureSuccessStatusCode();
        var total = int.Parse(res.Headers.GetValues("X-Total-Count").Single());
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return (body.EnumerateArray().Select(e => e.GetProperty("id").GetInt32()).ToArray(), total);
    }

    /// <summary>
    /// Father A, co-father B. Child X is A's and B's, child Z only B's. Requests: A's for X (pending, approved,
    /// rejected, pending), B's for X, and one stamped A for Z (the link to A was never there or is gone).
    /// </summary>
    private async Task<Scenario> SetUpAsync()
    {
        var (aId, a) = await NewAdultAsync("Papa-747", "7471");
        var (bId, b) = await NewAdultAsync("Mama-747", "7472");
        var x = await NewChildAsync(a, "Kind-X");
        (await a.PostAsJsonAsync($"/api/v1/supervisor/children/{x}/supervisors",
            new { supervisorId = bId, relation = "Mother" })).EnsureSuccessStatusCode();
        var z = await NewChildAsync(b, "Kind-Z");

        var t = new DateTime(2026, 11, 20, 8, 0, 0, DateTimeKind.Utc);
        // aPending1 and aPending2 share their timestamp: only the id tiebreaker orders them.
        var aPending1 = AddRequest(x, aId, ActivationRequestStatus.Pending, t);
        var aPending2 = AddRequest(x, aId, ActivationRequestStatus.Pending, t);
        var aApproved = AddRequest(x, aId, ActivationRequestStatus.Approved, t.AddMinutes(-10));
        var aRejected = AddRequest(x, aId, ActivationRequestStatus.Rejected, t.AddMinutes(-20));
        var bPending = AddRequest(x, bId, ActivationRequestStatus.Pending, t.AddMinutes(5));
        var aForUnlinked = AddRequest(z, aId, ActivationRequestStatus.Pending, t.AddMinutes(6));
        return new(a, b, x, aPending1, aPending2, aApproved, aRejected, bPending, aForUnlinked);
    }

    private sealed record Scenario(HttpClient A, HttpClient B, int ChildX,
        int APending1, int APending2, int AApproved, int ARejected, int BPending, int AForUnlinked);

    [Fact]
    public async Task OhneFilter_NurEigeneAnfragenEigenerKinder_NeuesteZuerstMitIdAlsTiebreaker()
    {
        var s = await SetUpAsync();

        var (ids, total) = await ListAsync(s.A);

        // AK 1 + 6: neither B's request for the shared child nor A's stamp on a child that is not his.
        // AK 5: equal timestamps → higher id first.
        Assert.Equal([s.APending2, s.APending1, s.AApproved, s.ARejected], ids);
        Assert.Equal(4, total);

        // AK 6 seen from the other side: B sees only B's request.
        var (idsB, _) = await ListAsync(s.B);
        Assert.Equal([s.BPending], idsB);
    }

    [Fact]
    public async Task StatusPending_FiltertUndZaehlt_UndSinktNachApprove()
    {
        var s = await SetUpAsync();

        // AK 2 + 3: only the pending ones; the header counts all of them even on a one-row page.
        var (ids, _) = await ListAsync(s.A, "?status=Pending");
        Assert.Equal([s.APending2, s.APending1], ids);
        var (page, total) = await ListAsync(s.A, "?status=Pending&take=1");
        Assert.Equal([s.APending2], page);
        Assert.Equal(2, total);

        // AK 4: approving one lowers the pending count by one.
        (await s.A.PostAsJsonAsync($"/api/v1/supervisor/children/{s.ChildX}/shop/activations/{s.APending2}/approve", new { }))
            .EnsureSuccessStatusCode();
        var (after, afterTotal) = await ListAsync(s.A, "?status=Pending");
        Assert.Equal([s.APending1], after);
        Assert.Equal(1, afterTotal);
    }

    [Fact]
    public async Task Reihenfolge_BleibtUnterRejectStabil()
    {
        var s = await SetUpAsync();
        var (before, _) = await ListAsync(s.A);

        (await s.A.PostAsJsonAsync($"/api/v1/supervisor/children/{s.ChildX}/shop/activations/{s.APending1}/reject", new { }))
            .EnsureSuccessStatusCode();

        // AK 5: deciding a request does not move it (ordering is by request time, not by decision time).
        var (after, _) = await ListAsync(s.A);
        Assert.Equal(before, after);
    }
}
