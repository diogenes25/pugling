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

    private static async Task<string?> CodeAsync(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("code").GetString();

    private async Task<string?> SchoolTypeOfAsync(int childId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
        return (await db.Children.AsNoTracking().SingleAsync(c => c.Id == childId)).SchoolType.ToString();
    }

    /// <summary>
    /// B-152 AK 1-3: a child attends exactly one school type or none. One assertion per path (POST, PATCH), so a
    /// check missing on either path turns this red; the number 1024 is an undefined single bit and turns red when
    /// only <c>IsPow2</c> is checked without <c>Enum.IsDefined</c>.
    /// </summary>
    [Fact]
    public async Task Schulart_Des_Kindes_Ist_Ein_Wert_Oder_Keine_Angabe()
    {
        var papa = await TestApi.AdultAsync(factory);

        // AK 1: a combination on POST is refused and no child is created.
        var name = TestApi.UniqueName("Kombi-Kind");
        var combo = await papa.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name, schoolType = "Gymnasium, Realschule" });
        Assert.Equal(HttpStatusCode.BadRequest, combo.StatusCode);
        Assert.Equal("validation_error", await CodeAsync(combo));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            Assert.False(await db.Children.AnyAsync(c => c.Name == name));
        }

        // AK 3: a single value and None pass on POST.
        var single = await papa.PostAsJsonAsync("/api/v1/supervisor/children", new { name = TestApi.UniqueName("Kind"), schoolType = "Gymnasium" });
        Assert.Equal(HttpStatusCode.Created, single.StatusCode);
        var childId = await TestApi.IdAsync(single);
        Assert.Equal(HttpStatusCode.Created, (await papa.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Kind"), schoolType = "None" })).StatusCode);

        // AK 2: on PATCH a combination and an undefined number are refused, the child keeps its value.
        var patchCombo = await papa.PatchAsJsonAsync($"/api/v1/supervisor/children/{childId}", new { schoolType = "Gymnasium, Realschule" });
        Assert.Equal(HttpStatusCode.BadRequest, patchCombo.StatusCode);
        Assert.Equal("Gymnasium", await SchoolTypeOfAsync(childId));
        var patchUndefined = await papa.PatchAsJsonAsync($"/api/v1/supervisor/children/{childId}", new { schoolType = 1024 });
        Assert.Equal(HttpStatusCode.BadRequest, patchUndefined.StatusCode);
        Assert.Equal("Gymnasium", await SchoolTypeOfAsync(childId));

        // AK 3: a single value and None pass on PATCH.
        Assert.Equal(HttpStatusCode.OK, (await papa.PatchAsJsonAsync($"/api/v1/supervisor/children/{childId}", new { schoolType = "Realschule" })).StatusCode);
        Assert.Equal("Realschule", await SchoolTypeOfAsync(childId));
        Assert.Equal(HttpStatusCode.OK, (await papa.PatchAsJsonAsync($"/api/v1/supervisor/children/{childId}", new { schoolType = "None" })).StatusCode);
        Assert.Equal("None", await SchoolTypeOfAsync(childId));
    }

    /// <summary>
    /// B-152 AK 4: only a SENT value is checked. A child that already carries a combination (stored before the rule)
    /// stays patchable on other fields and keeps its combination. Red if the check reads the stored value instead.
    /// </summary>
    [Fact]
    public async Task Patch_Ohne_Schulart_Laesst_Eine_Alte_Kombination_Stehen()
    {
        var papa = await TestApi.AdultAsync(factory);
        var childId = await TestApi.IdAsync(await papa.PostAsJsonAsync("/api/v1/supervisor/children", new { name = TestApi.UniqueName("Altkind") }));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var child = await db.Children.SingleAsync(c => c.Id == childId);
            child.SchoolType = Pugling.Contracts.SchoolTypes.Gymnasium | Pugling.Contracts.SchoolTypes.Realschule;
            await db.SaveChangesAsync();
        }

        var res = await papa.PatchAsJsonAsync($"/api/v1/supervisor/children/{childId}", new { name = "Altkind umbenannt" });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("Realschule, Gymnasium", await SchoolTypeOfAsync(childId));
    }
}
