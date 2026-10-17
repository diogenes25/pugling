using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// Lifecycle of a child: deleting it must take its login account along.
/// <para>
/// The counterpart of <see cref="AdultLifecycleTests"/>. An <c>Account</c> has no foreign key to the child -
/// the cascade only removes its Student profile. Without the explicit cleanup the row survived with a live
/// PIN hash: invisible to every supervisor, but still answering <c>auth/login</c> with a token.
/// </para>
/// </summary>
public class ChildLifecycleTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    [Fact]
    public async Task Loeschen_Nimmt_Das_Login_Konto_Des_Kindes_Mit()
    {
        var papa = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await papa.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = "Kind mit Konto", pin = "5201" }));

        int accountId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            accountId = await db.Accounts.Where(a => a.Profiles.Any(p => p.ChildId == childId)).Select(a => a.Id).SingleAsync();
        }
        var anonym = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await anonym.PostAsJsonAsync("/api/v1/auth/login", new { accountId, pin = "5201" })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await papa.DeleteAsync($"/api/v1/supervisor/children/{childId}")).StatusCode);

        // Red without the cleanup in ChildrenController.Delete: the account row stays and the login still answers 200.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            Assert.False(await db.Children.AnyAsync(c => c.Id == childId));
            Assert.False(await db.Accounts.AnyAsync(a => a.Id == accountId));
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonym.PostAsJsonAsync("/api/v1/auth/login", new { accountId, pin = "5201" })).StatusCode);
    }
}
