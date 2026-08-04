using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Auth;
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

        // Adult 1 - the father. The default in tutorial-api.sh and TestApi.FatherAsync.
        var papa = await LoginAsync(c, "adult", new { adultId = 1, pin = "0000" });
        Assert.Equal("Papa", papa.GetProperty("name").GetString());
        Assert.Equal("Supervisor", papa.GetProperty("role").GetString());

        // Adult 2 - the teacher. Created in SeedTeacherLibrary, i.e. *after* SeedAdmin; several existing tests
        // (MediaLinkTests, RemarkTests) require exactly this 2.
        // The role can now be asserted: **creator, not supervisor**. Before, startup called
        // `EnsureForAdultAsync` for every adult, and the teacher got the supervisor role even though
        // `EnsureForTeacherAsync` existed precisely for them and was never reached. The seed now distinguishes
        // by the supervision assignment - this test is the promise that it stays that way.
        var lehrer = await LoginAsync(c, "adult", new { adultId = 2, pin = "9999" });
        Assert.Equal("Herr Schmidt (Englischlehrer)", lehrer.GetProperty("name").GetString());
        Assert.Equal("Creator", lehrer.GetProperty("role").GetString());

        // Child 1 - the son. The default in TestApi.ChildAsync and in the Playwright suite.
        var sohn = await LoginAsync(c, "child", new { childId = 1, pin = "1111" });
        Assert.Equal("Sohn", sohn.GetProperty("name").GetString());
        Assert.Equal("Student", sohn.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Geseedeter_Katalog_Ist_Vollstaendig_Genug_Zum_Testen()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();

        // Subject 1 = English: the catalog is created in SeedCatalog, and this is its first subject.
        var first = await db.Subjects.AsNoTracking().OrderBy(s => s.Id).FirstAsync();
        Assert.Equal("Englisch", first.Name);

        // Adult 2 is the teacher - tutorial-api.sh and the skills address them through this id.
        var lehrer = await db.Adults.AsNoTracking().SingleAsync(a => a.Id == 2);
        Assert.Equal("englischlehrer@example.com", lehrer.Email);

        // Self-protection: if the seed did not run (or only halfway), the assurance above would hold by
        // accident, and every test building on seeded content would fail somewhere else.
        Assert.True(await db.Subjects.CountAsync() >= 3, "Fewer than three seeded subjects - did SeedCatalog run?");
        Assert.True(await db.Vocabularies.CountAsync() >= 10, "Zu wenige geseedete Vokabeln – lief SeedVocabulary?");
        Assert.True(await db.StudyPlans.AnyAsync(), "Kein geseedeter Lehrplan – lief SeedDemoPlan?");
    }

    /// <summary>
    /// <b>The seed must be able to run a second time without duplicating anything</b> - startup calls it on
    /// <i>every</i> boot. Nothing checked exactly that so far: the idempotency of every sub-routine was
    /// promised but never measured. A forgotten "does it exist already?" guard in a new routine would only
    /// have shown up as demo data standing there twice after a restart.
    /// <para>
    /// The row counts of <b>all</b> tables are compared, not a selection: a selection would be blind exactly
    /// where the next routine is added.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Seed_Zweimal_Ausgefuehrt_Dupliziert_Nichts()
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<PuglingDbContext>();

        // Seed once, then measure, then seed again: the host has already seeded at startup, but the test
        // factory clears the time slots afterwards (neutralizing the wall clock). Without this first run the
        // difference would be its cleanup and not a seed bug.
        await SeedAsync(sp, db);
        var vorher = await ZeilenzahlenAsync(db);

        // Self-protection: if the DB were empty, the test would compare two zeros.
        Assert.True(vorher.Values.Sum() > 100,
            $"Too few seeded rows in total ({vorher.Values.Sum()}) - did the seed run at all?");

        await SeedAsync(sp, db);

        Assert.Equal(vorher, await ZeilenzahlenAsync(db));
    }

    private static Task SeedAsync(IServiceProvider sp, PuglingDbContext db) =>
        Seed.RunAsync(db,
            sp.GetRequiredService<ExerciseItemService>(),
            sp.GetRequiredService<AccountService>(),
            sp.GetRequiredService<InterestTagService>(),
            CancellationToken.None);

    /// <summary>
    /// Row count per table. Through the raw connection instead of the DbSets, so that tables without a DbSet
    /// are counted too - otherwise the comparison would be blind exactly where a join table grows.
    /// </summary>
    private static async Task<Dictionary<string, long>> ZeilenzahlenAsync(PuglingDbContext db)
    {
        var counts = new Dictionary<string, long>(StringComparer.Ordinal);
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync();
        foreach (var table in db.Model.GetRelationalModel().Tables)
        {
            using var cmd = connection.CreateCommand();
            // Table names come from the EF model, not from outside - there is nothing to inject here.
            cmd.CommandText = $"SELECT COUNT(*) FROM \"{table.Name}\"";
            counts[table.Name] = Convert.ToInt64(await cmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
        }
        return counts;
    }

    /// <summary>
    /// The seeded teacher must be able to edit their <b>own</b> exercises. Before, they could not: the rights
    /// run exclusively through <c>ExerciseGrant</c>, but they were only granted by one raw SQL line in a
    /// migration - and that is a no-op on an empty DB. So the exercises had an author but no owner.
    /// </summary>
    [Fact]
    public async Task Lehrer_Darf_Seine_Geseedete_Uebung_Bearbeiten()
    {
        int seriesId, seriesUnitId, exerciseId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var uebung = await db.Exercises.AsNoTracking().Include(e => e.SeriesUnit)
                .FirstAsync(e => e.AuthorAdultId == 2 && e.Type == Pugling.Api.Exercises.ExerciseTypeKeys.Vocabulary);
            (seriesId, seriesUnitId, exerciseId) = (uebung.SeriesUnit!.SeriesId, uebung.SeriesUnitId, uebung.Id);
        }

        // The check runs through an *additive* write (creating an item), not through the full PUT: the same
        // rights path (`EnsureCanWrite` → grant), but without replacing the seeded exercise. That very call
        // returns 403 for another creator (ExerciseItemsAndProgressTests) - here it has to succeed.
        var lehrer = await TestApi.FatherAsync(factory, id: 2, pin: "9999");
        var res = await lehrer.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/vocabulary/{exerciseId}/items",
            new { front = "climate", back = "Klima" });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }
}
