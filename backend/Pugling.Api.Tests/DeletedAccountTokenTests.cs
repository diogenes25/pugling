using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// B-245: a token issued before <c>DELETE supervisor/adults/{id}</c> stays cryptographically valid for its whole
/// lifetime, but the account and the adult behind its <c>aid</c>/<c>fid</c> are gone. Every writer that puts
/// <c>fid</c> into a foreign key (subjects, series, categories, profiles, exercises, children) then ran into the
/// constraint and answered 500. The token must be refused like any other invalid credential: 401.
/// </summary>
public class DeletedAccountTokenTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<(HttpClient client, int adultId)> FreshAdultAsync()
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Geloescht"), pin = "4545" });
        res.EnsureSuccessStatusCode();
        var id = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return (await TestApi.AdultAsync(factory, id, "4545"), id);
    }

    /// <summary>
    /// Builds the mixed account no creation path builds: moves the child's Student profile onto the adult's account
    /// and drops the child's own account, then checks the shape (two adult rows, one Student row) so a wrong seed
    /// fails here and not as a misleading message in the test that uses it.
    /// </summary>
    private async Task MixIntoAdultAccountAsync(int childId, int accountId, int adultId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        var studentProfile = await db.AccountProfiles.SingleAsync(p => p.ChildId == childId);
        var childAccountId = studentProfile.AccountId;
        studentProfile.AccountId = accountId;
        db.Accounts.Remove(await db.Accounts.SingleAsync(a => a.Id == childAccountId));
        await db.SaveChangesAsync();
        Assert.Equal(1, await db.AccountProfiles.CountAsync(p => p.AccountId == accountId && p.ChildId == childId));
        Assert.Equal(2, await db.AccountProfiles.CountAsync(p => p.AccountId == accountId && p.AdultId == adultId));
    }

    /// <summary>
    /// Red without the check: the write reaches the FK on <c>Adults</c> and the request fails with 500. The same
    /// token is shown to work before the delete, so the 401 afterwards is about the deletion and nothing else.
    /// </summary>
    [Fact]
    public async Task Token_Eines_Geloeschten_Kontos_Bekommt_401_Statt_500()
    {
        var (adult, adultId) = await FreshAdultAsync();
        var before = await adult.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("Vorher") });
        Assert.Equal(HttpStatusCode.Created, before.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await adult.DeleteAsync($"/api/v1/supervisor/adults/{adultId}")).StatusCode);

        var after = await adult.PostAsJsonAsync("/api/v1/creator/subjects", new { name = TestApi.UniqueName("Nachher") });
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }

    /// <summary>
    /// The other deletion path: a child's token after <c>DELETE supervisor/children/{id}</c>. Red if the check looked
    /// at <c>fid</c> only (a child token has none) or if deleting a child stopped removing its account.
    /// </summary>
    [Fact]
    public async Task Token_Eines_Geloeschten_Kindes_Bekommt_401()
    {
        var papa = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await papa.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Geloeschtes-Kind"), pin = "4646" }));
        var child = await TestApi.ChildAsync(factory, childId, "4646");
        Assert.Equal(HttpStatusCode.OK, (await child.GetAsync("/api/v1/auth/me")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await papa.DeleteAsync($"/api/v1/supervisor/children/{childId}")).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await child.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    /// <summary>
    /// B-285: the third deletion path. An adult who deletes themselves takes their orphaned children along, but
    /// the children's accounts have no foreign key to the child and survived as empty shells - so the child's
    /// token stayed valid (200 on <c>auth/me</c>). The distinction from the case above is the actor: the adult
    /// deletes <i>themselves</i>, not the child. Red if <c>AdultsController.Delete</c> only removes the adult's
    /// own account.
    /// </summary>
    [Fact]
    public async Task Token_Eines_Mit_Dem_Vater_Geloeschten_Kindes_Bekommt_401()
    {
        var (adult, adultId) = await FreshAdultAsync();
        // The fresh adult is the child's only supervisor, so the child orphans with them.
        var childId = await TestApi.IdAsync(await adult.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Verwaistes-Kind"), pin = "4747" }));
        var child = await TestApi.ChildAsync(factory, childId, "4747");
        Assert.Equal(HttpStatusCode.OK, (await child.GetAsync("/api/v1/auth/me")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await adult.DeleteAsync($"/api/v1/supervisor/adults/{adultId}")).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await child.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    /// <summary>
    /// A mixed account - one login that carries the adult's profile AND the orphaned child's Student profile. The
    /// model allows it (one row per role), no creation path builds it today, so it is seeded through the context.
    /// Two separate sweeps ("all profiles are this adult" / "all profiles are orphaned children") each see the
    /// other profile and leave the account as a shell. Red if the sweep is split into two predicates again.
    /// </summary>
    [Fact]
    public async Task Gemischtes_Konto_Von_Vater_Und_Verwaistem_Kind_Verschwindet_Mit()
    {
        var (adult, adultId) = await FreshAdultAsync();
        var childId = await TestApi.IdAsync(await adult.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Gemischt-Kind"), pin = "4848" }));
        var me = await adult.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        var accountId = me.GetProperty("accountId").GetInt32();

        await MixIntoAdultAccountAsync(childId, accountId, adultId);

        Assert.Equal(HttpStatusCode.NoContent, (await adult.DeleteAsync($"/api/v1/supervisor/adults/{adultId}")).StatusCode);

        using var check = factory.Services.CreateScope();
        var after = check.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.False(await after.Accounts.AnyAsync(a => a.Id == accountId), "the mixed account survived as an empty shell");
    }

    /// <summary>
    /// The exclusion side of the same mixed account (B-289): the Student profile on the adult's login belongs to a
    /// child that a second supervisor keeps. The account must stay - it is that child's only login - even though
    /// one of its profiles points at the deleted adult. Red if the sweep drops its <c>All</c> clause and removes
    /// every account that merely <i>has</i> a matching profile.
    /// </summary>
    [Fact]
    public async Task Gemischtes_Konto_Mit_Ko_Betreutem_Kind_Bleibt_Stehen()
    {
        var (adult, adultId) = await FreshAdultAsync();
        var (_, motherId) = await FreshAdultAsync();
        var childId = await TestApi.IdAsync(await adult.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Gemischt-Ko-Kind"), pin = "4949" }));
        (await adult.PostAsJsonAsync($"/api/v1/supervisor/children/{childId}/supervisors",
            new { supervisorId = motherId, relation = "Mother" })).EnsureSuccessStatusCode();
        var accountId = (await adult.GetFromJsonAsync<JsonElement>("/api/v1/auth/me")).GetProperty("accountId").GetInt32();

        await MixIntoAdultAccountAsync(childId, accountId, adultId);

        Assert.Equal(HttpStatusCode.NoContent, (await adult.DeleteAsync($"/api/v1/supervisor/adults/{adultId}")).StatusCode);

        using var check = factory.Services.CreateScope();
        var after = check.ServiceProvider.GetRequiredService<PuglingDbContext>();
        Assert.True(await after.Accounts.AnyAsync(a => a.Id == accountId), "the co-supervised child's login was swept away");
        // And it still works: the child logs in through that account (its PIN is the account's, i.e. the adult's).
        var child = await TestApi.ChildAsync(factory, childId, "4545");
        Assert.Equal(HttpStatusCode.OK, (await child.GetAsync("/api/v1/auth/me")).StatusCode);
    }
}
