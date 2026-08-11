using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// B-127: a publisher stays ownerless on purpose - naming one is not authorship. But an ownerless row
/// must not reach into owned ones: deleting it clears the assignment on every series pointing at it
/// (SetNull), including series of other accounts. The lock therefore runs along the SERIES' owner, not
/// along the publisher.
/// </summary>
public class VerlagLoeschenSperreTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static async Task<int> PublisherAsync(HttpClient creator) =>
        await TestApi.IdAsync(await creator.PostAsJsonAsync("/api/v1/creator/publishers",
            new { name = TestApi.UniqueName("Verlag") }));

    /// <summary>
    /// A throwaway creator account, deliberately NOT a seeded one: the factory is shared across the class,
    /// and repurposing Papa or the teacher would make other tests depend on execution order (the lesson
    /// <c>RemarkTests.RegisterAdminFatherAsync</c> already paid for).
    /// </summary>
    private async Task<HttpClient> FremderCreatorAsync(string pin)
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Fremder Creator"), pin });
        res.EnsureSuccessStatusCode();
        var id = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return await TestApi.AdultAsync(factory, id, pin);
    }

    [Fact]
    public async Task Verlag_MitFremderReihe_LaesstSichNichtLoeschen()
    {
        var mine = await TestApi.AdultAsync(factory);
        var stranger = await FremderCreatorAsync("3141");

        var publisherId = await PublisherAsync(mine);
        await stranger.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Fremde Reihe"), publisherId });

        var res = await mine.DeleteAsync($"/api/v1/creator/publishers/{publisherId}");

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("publisher_in_use", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Verlag_MitNurEigenenReihen_BleibtLoeschbar()
    {
        var mine = await TestApi.AdultAsync(factory);
        var publisherId = await PublisherAsync(mine);
        var seriesId = await TestApi.IdAsync(await mine.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Eigene Reihe"), publisherId }));

        // The case the whole page exists for: cleaning up a typo one has made oneself.
        var res = await mine.DeleteAsync($"/api/v1/creator/publishers/{publisherId}");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        var series = await mine.GetFromJsonAsync<JsonElement>($"/api/v1/creator/textbook-series/{seriesId}");
        Assert.False(series.GetProperty("publisherId").ValueKind is JsonValueKind.Number);
    }

    /// <summary>
    /// Pins the product decision "ownerless counts as foreign", which nothing else would catch.
    /// <para>
    /// <c>OwnerAdultId</c> is nullable and documented as "seeded, owned by nobody" - so this is the case
    /// of the shared catalog itself, and reading it fail-closed is what <c>IsOwnedBy</c> does too. Flipping
    /// that call would leave every other test green.
    /// </para>
    /// <para>
    /// What this case does <b>not</b> prove: that the predicate had to spell the null branch out. It was
    /// red before the fix for the same reason as the foreign-series case - there was no lock at all.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Verlag_MitEigentuemerloserReihe_LaesstSichNichtLoeschen()
    {
        var mine = await TestApi.AdultAsync(factory);
        var publisherId = await PublisherAsync(mine);
        var seriesId = await TestApi.IdAsync(await mine.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Geseedete Reihe"), publisherId }));

        // Ownerless is what the SEED produces; there is no endpoint that gives away a series, so this one
        // state has to be established directly. It is the state under test, not a shortcut past the API.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var series = await db.TextbookSeries.FirstAsync(s => s.Id == seriesId);
            series.OwnerAdultId = null;
            await db.SaveChangesAsync();
        }

        var res = await mine.DeleteAsync($"/api/v1/creator/publishers/{publisherId}");

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("publisher_in_use", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Admin_LoeschtAuchMitFremderReihe()
    {
        var stranger = await FremderCreatorAsync("3142");
        var admin = await AdminAsync("3143");

        var publisherId = await PublisherAsync(stranger);
        await stranger.PostAsJsonAsync("/api/v1/creator/textbook-series",
            new { name = TestApi.UniqueName("Fremde Reihe"), publisherId });

        // Without this valve the lock is a trap: as soon as two creators each hang a series on the same
        // publisher, neither can delete it - each sees the other's series as foreign.
        //
        // Note on what this test proves: it is green BEFORE the lock exists too, because nothing blocked
        // back then. Only together with the two locking cases above does it say anything - it pins that the
        // valve was not forgotten, not that it works in isolation.
        var res = await admin.DeleteAsync($"/api/v1/creator/publishers/{publisherId}");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    private async Task<HttpClient> AdminAsync(string pin)
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = TestApi.UniqueName("Admin"), pin });
        res.EnsureSuccessStatusCode();
        var id = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var adult = await db.Adults.FirstAsync(a => a.Id == id);
            adult.IsAdmin = true;
            await db.SaveChangesAsync();
        }

        // The token has to be issued AFTER the flag, otherwise it carries no admin role claim.
        return await TestApi.AdultAsync(factory, id, pin);
    }
}
