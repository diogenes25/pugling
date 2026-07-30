using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;

namespace Pugling.Api.Tests;

/// <summary>
/// Nagelt fest, was der Seed nach außen zusagt: die <b>Ids und PINs der geseedeten Konten</b>.
/// <para>
/// Diese Ids sind Reihenfolge-Artefakte von <see cref="Seed"/> – und sie sind an mehreren Stellen
/// <b>außerhalb des Repos-Testlaufs</b> hart verdrahtet, die niemand rot werden lässt:
/// <list type="bullet">
///   <item><c>frontend/playwright.config.ts</c> und <c>frontend/e2e/*.spec.ts</c> (E2E läuft nachts, blockt kein Deploy)</item>
///   <item><c>.claude/scripts/tutorial-api.sh</c> („login_adult 2 9999" = Lehrer Herr Schmidt)</item>
///   <item><c>.claude/skills/{creator,supervisor,student,anmerkungen}/SKILL.md</c> (kein Test deckt sie ab)</item>
///   <item>die eingecheckten Ausgaben von <see cref="DocsCaptureTests"/> unter <c>docs/api-examples/</c></item>
/// </list>
/// Verschiebt eine neue Seed-Routine die Reihenfolge, brechen alle vier lautlos. Wird dieser Test rot,
/// ist die Ursache fast immer eine Einfügung <i>vor</i> <see cref="Seed"/>s identitätstragenden Schritten –
/// neue Routinen gehören ans Ende.
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
