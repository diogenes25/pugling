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
        // Die Rolle ist jetzt zusicherbar: **Creator, nicht Supervisor**. Vorher rief der Start für jeden
        // Adult `EnsureForAdultAsync`, und der Lehrer bekam die Supervisor-Rolle, obwohl
        // `EnsureForTeacherAsync` genau für ihn existierte und nie erreicht wurde. Der Seed unterscheidet
        // jetzt am Betreuungsauftrag – dieser Test ist die Zusage, dass es dabei bleibt.
        var lehrer = await LoginAsync(c, "adult", new { adultId = 2, pin = "9999" });
        Assert.Equal("Herr Schmidt (Englischlehrer)", lehrer.GetProperty("name").GetString());
        Assert.Equal("Creator", lehrer.GetProperty("role").GetString());

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
        Assert.True(await db.Vocabularies.CountAsync() >= 10, "Zu wenige geseedete Vokabeln – lief SeedVocabulary?");
        Assert.True(await db.StudyPlans.AnyAsync(), "Kein geseedeter Lehrplan – lief SeedDemoPlan?");
    }

    /// <summary>
    /// <b>Der Seed muss ein zweites Mal laufen können, ohne etwas zu verdoppeln</b> – der Start ruft ihn
    /// bei <i>jedem</i> Hochfahren. Genau das prüfte bisher nichts: die Idempotenz jeder Teilroutine war
    /// zugesagt, aber nie gemessen. Ein vergessener „existiert schon?"-Wächter in einer neuen Routine
    /// hätte sich nur dadurch gezeigt, dass die Demo-Daten nach einem Neustart doppelt dastehen.
    /// <para>
    /// Verglichen werden die Zeilenzahlen <b>aller</b> Tabellen, nicht eine Auswahl: eine Auswahl wäre
    /// genau dort blind, wo die nächste Routine hinzukommt.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Seed_Zweimal_Ausgefuehrt_Dupliziert_Nichts()
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<PuglingDbContext>();

        // Erst einmal säen, dann messen, dann erneut säen: der Host hat beim Start zwar schon geseedet,
        // aber die Test-Factory räumt danach die Zeitfenster ab (Wanduhr-Neutralisierung). Ohne diesen
        // ersten Lauf wäre der Unterschied ihr Aufräumen und nicht ein Seed-Fehler.
        await SeedAsync(sp, db);
        var vorher = await ZeilenzahlenAsync(db);

        // Selbstschutz: läge die DB leer vor, verglich der Test zwei Nullen.
        Assert.True(vorher.Values.Sum() > 100,
            $"Zu wenige geseedete Zeilen insgesamt ({vorher.Values.Sum()}) – lief der Seed überhaupt?");

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
    /// Zeilenzahl je Tabelle. Über die rohe Verbindung statt über die DbSets, damit auch Tabellen ohne
    /// DbSet mitgezählt werden – sonst wäre der Vergleich genau dort blind, wo eine Join-Tabelle wächst.
    /// </summary>
    private static async Task<Dictionary<string, long>> ZeilenzahlenAsync(PuglingDbContext db)
    {
        var counts = new Dictionary<string, long>(StringComparer.Ordinal);
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync();
        foreach (var table in db.Model.GetRelationalModel().Tables)
        {
            using var cmd = connection.CreateCommand();
            // Tabellennamen kommen aus dem EF-Modell, nicht von außen – hier gibt es nichts zu injizieren.
            cmd.CommandText = $"SELECT COUNT(*) FROM \"{table.Name}\"";
            counts[table.Name] = Convert.ToInt64(await cmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
        }
        return counts;
    }

    /// <summary>
    /// Der geseedete Lehrer muss seine <b>eigenen</b> Übungen bearbeiten können. Vorher konnte er das
    /// nicht: die Rechte laufen ausschließlich über <c>ExerciseGrant</c>, vergeben wurden sie aber nur von
    /// einer Raw-SQL-Zeile in einer Migration – und die ist auf einer leeren DB ein No-op. Die Übungen
    /// hatten also einen Autor, aber keinen Owner.
    /// </summary>
    [Fact]
    public async Task Lehrer_Darf_Seine_Geseedete_Uebung_Bearbeiten()
    {
        int subjectId, chapterId, exerciseId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var uebung = await db.Exercises.AsNoTracking().Include(e => e.Chapter)
                .FirstAsync(e => e.AuthorAdultId == 2 && e.Type == Pugling.Api.Exercises.ExerciseTypeKeys.Vocabulary);
            (subjectId, chapterId, exerciseId) = (uebung.Chapter!.SubjectId, uebung.ChapterId, uebung.Id);
        }

        // Geprüft wird über einen *additiven* Schreibzugriff (ein Item anlegen), nicht über das
        // vollständige PUT: derselbe Rechte-Pfad (`EnsureCanWrite` → Grant), aber ohne die geseedete
        // Übung zu ersetzen. Genau dieser Aufruf liefert einem fremden Creator 403
        // (ExerciseItemsAndProgressTests) – hier muss er gelingen.
        var lehrer = await TestApi.FatherAsync(factory, id: 2, pin: "9999");
        var res = await lehrer.PostAsJsonAsync(
            $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}/items",
            new { front = "climate", back = "Klima" });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }
}
