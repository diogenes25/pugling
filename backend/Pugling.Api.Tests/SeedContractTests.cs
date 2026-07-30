using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// Pins down what the seed guarantees to the outside world: the <b>ids and PINs of the seeded accounts</b>.
/// <para>
/// These ids are ordering artifacts of <see cref="Seed"/> - and they are hard-wired in several places
/// <b>outside the repo's test run</b> that nobody flags red:
/// <list type="bullet">
///   <item><c>frontend/playwright.config.ts</c> and <c>frontend/e2e/*.spec.ts</c> (E2E runs nightly, doesn't block a deploy)</item>
///   <item><c>.claude/scripts/tutorial-api.sh</c> ("login_adult 2 9999" = teacher Herr Schmidt)</item>
///   <item><c>.claude/skills/{creator,supervisor,student,anmerkungen}/SKILL.md</c> (no test covers them)</item>
///   <item>the checked-in output of <see cref="DocsCaptureTests"/> under <c>docs/api-examples/</c></item>
/// </list>
/// If a new seed routine shifts the order, all four break silently. If this test turns red, the cause is
/// almost always an insertion <i>before</i> <see cref="Seed"/>'s identity-bearing steps -
/// new routines belong at the end.
/// </para>
/// </summary>
public class SeedContractTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private static async Task<JsonElement> LoginAsync(HttpClient c, string role, object dto)
    {
        var res = await c.PostAsJsonAsync($"/api/v1/auth/{role}", dto);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Geseedete_Konten_Behalten_Ihre_Ids_Und_Pins()
    {
        var c = factory.CreateClient();

        // Adult 1 – der Vater. Default in tutorial-api.sh und TestApi.FatherAsync.
        var papa = await LoginAsync(c, "adult", new { adultId = 1, pin = "0000" });
        Assert.Equal("Papa", papa.GetProperty("name").GetString());
        Assert.Equal("Supervisor", papa.GetProperty("role").GetString());

        // Adult 2 – der Lehrer. Entsteht in SeedTeacherLibrary, also *nach* SeedAdmin; mehrere
        // Bestandstests (MediaLinkTests, RemarkTests) setzen genau diese 2 voraus.
        // Bewusst *keine* Zusicherung auf die Rolle: der geseedete Lehrer bekommt heute Creator **und**
        // Supervisor, weil AccountBackfill für jeden Adult EnsureForFatherAsync ruft – EnsureForTeacherAsync
        // (Creator-only, der eigentliche Zweck des Kontos) wird für ihn nie erreicht. Das ist ein Befund,
        // keine Zusage; er wird beim Einmeinden der Backfills ins Seed behoben. Hier würde eine Zusicherung
        // den Defekt einbetonieren, egal in welche Richtung.
        var lehrer = await LoginAsync(c, "adult", new { adultId = 2, pin = "9999" });
        Assert.Equal("Herr Schmidt (Englischlehrer)", lehrer.GetProperty("name").GetString());

        // Child 1 – der Sohn. Default in TestApi.ChildAsync und in der Playwright-Suite.
        var sohn = await LoginAsync(c, "child", new { childId = 1, pin = "1111" });
        Assert.Equal("Sohn", sohn.GetProperty("name").GetString());
        Assert.Equal("Student", sohn.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Geseedeter_Katalog_Ist_Vollstaendig_Genug_Zum_Testen()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();

        // Fach 1 = Englisch: der Katalog entsteht in SeedCatalog, dessen erstes Fach.
        var first = await db.Subjects.AsNoTracking().OrderBy(s => s.Id).FirstAsync();
        Assert.Equal("Englisch", first.Name);

        // Adult 2 ist der Lehrer – tutorial-api.sh und die Skills sprechen ihn über diese Id an.
        var lehrer = await db.Adults.AsNoTracking().SingleAsync(a => a.Id == 2);
        Assert.Equal("englischlehrer@example.com", lehrer.Email);

        // Selbstschutz: liefe der Seed gar nicht (oder nur halb), bestünde die Zusicherung oben zufällig,
        // und jeder Test, der auf geseedete Inhalte baut, scheiterte an einer anderen Stelle.
        Assert.True(await db.Subjects.CountAsync() >= 3, "Weniger als drei geseedete Fächer – lief SeedCatalog?");
        Assert.True(await db.Vocabulary.CountAsync() >= 10, "Zu wenige geseedete Vokabeln – lief SeedVocabulary?");
        Assert.True(await db.StudyPlans.AnyAsync(), "Kein geseedeter Lehrplan – lief SeedDemoPlan?");
    }
}
