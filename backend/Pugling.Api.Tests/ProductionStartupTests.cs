using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Pugling.Api.Tests;

/// <summary>
/// The only tests that run the API as a <b>non-development</b> host.
/// <para>
/// Everything else in this project runs <c>Development</c>, so the branches that apply exclusively to a
/// real instance were executed 0% of the time – and that is precisely the class of defect that has
/// already struck here: the <c>vite-plugin-pwa</c> peer conflict let the Azure deployment fail unnoticed
/// for 24 days because no gate ever entered the deployment configuration (see
/// docs/codequalitaet-gates-plan.md, D1).
/// </para>
/// <para>
/// Pinned here is the production <b>configuration</b>, not a different flow: the three
/// <c>IsDevelopment()</c> branches in Program.cs (remark scope, JWT fail-fast, seed) plus the dev
/// fallback key in <c>TokenService</c>. <c>wwwroot</c>, Kestrel and <c>MapFallbackToFile</c> need a
/// published artifact and stay out on purpose – they are B-47.
/// </para>
/// </summary>
public class ProductionStartupTests(ProductionWebAppFactory factory) : IClassFixture<ProductionWebAppFactory>
{
    /// <summary>
    /// The commissioning path of a fresh production instance, in one flow: the database is empty because
    /// no seed ran, the anonymous registration is nevertheless reachable, and the token it yields
    /// validates – which it only can if the <b>configured</b> signing key took effect instead of the dev
    /// fallback.
    /// <para>
    /// Deliberately one test and not four: each step is the precondition of the next, and split up, three
    /// of them would need to rebuild the same state.
    /// </para>
    /// </summary>
    [Fact]
    public async Task FrischeProduktionsinstanz_IstOhneSeedInBetriebZuNehmen()
    {
        // Its **own** host, not the class fixture: step 3 asserts that the registered adult gets id 1, and
        // step 6 that the catalog is empty - both only hold on a database nobody else has written to.
        // Sharing the fixture made the test depend on the order within the class: green in Release, red in
        // Debug, and red under any `--filter`.
        using var fresh = new ProductionWebAppFactory();
        var client = fresh.CreateClient();

        // 1. No seed. In development the demo family sits at adult #1 with PIN 0000; here that login must
        //    not exist at all - otherwise every production instance would ship with a known back door.
        var seedLogin = await client.PostAsJsonAsync("/api/v1/auth/adult", new { adultId = 1, pin = "0000" });
        Assert.Equal(HttpStatusCode.Unauthorized, seedLogin.StatusCode);

        // 2. Registration is reachable without a login - the bootstrap of an empty instance. Without it a
        //    fresh deployment would have no way in at all.
        var created = await client.PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name = "Produktions-Erstnutzer", email = (string?)null, pin = "1234" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var adultId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        // 3. And this really is the first row: the seed would have taken id 1.
        Assert.Equal(1, adultId);

        // 4. Log in with it.
        var login = await client.PostAsJsonAsync("/api/v1/auth/adult", new { adultId, pin = "1234" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        // 5. The token is accepted.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var me = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var body = await me.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(adultId, body.GetProperty("adultId").GetInt32());
        Assert.Equal("Supervisor", body.GetProperty("role").GetString());
        // A registration creates creator + supervisor - the father holds both tiers.
        var roles = body.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToList();
        Assert.Contains("Creator", roles);
        Assert.Contains("Supervisor", roles);

        // 6. Nor is the seed's **content** there. A second leg for the same assurance, because step 1 alone
        //    is ambiguous: a 401 would also come from a wrong PIN on an existing adult. The empty catalog
        //    does not have that ambiguity - the seed creates subjects, the registration does not. Reachable
        //    only now: the catalog is a creator endpoint and needs the token.
        var subjects = await client.GetAsync("/api/v1/creator/subjects");
        Assert.Equal(HttpStatusCode.OK, subjects.StatusCode);
        Assert.Empty((await subjects.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
    }

    /// <summary>
    /// The dev fallback signing key opens nothing on a configured instance.
    /// <para>
    /// <b>Why this and not "the real token is accepted".</b> A 200 above proves only that issuing and
    /// validating agree – and they always do: <c>Program.cs</c> validates with the very
    /// <see cref="Pugling.Api.Auth.TokenService"/> instance that signs. Were <c>Jwt:Key</c> ignored, both
    /// sides would fall back together and the call would still return 200. Only a token forged with the
    /// fallback key separates the two cases: it must fail, and it is the one that matters – the fallback
    /// stands in plain text in the source tree, so anyone can mint it.
    /// </para>
    /// </summary>
    [Fact]
    public async Task MitDemDevFallbackSchluessel_GefaelschtesToken_WirdAbgelehnt()
    {
        // Deliberately spelled out instead of referenced: this literal IS the danger, and a test that reads
        // it from the same place the production code does would move along with a typo there.
        const string devFallback = "pugling-dev-signing-key-change-me-please-0123456789";
        var forged = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim("aid", "1"), new Claim(ClaimTypes.Role, "Supervisor")]),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(devFallback)), SecurityAlgorithms.HmacSha256),
        });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forged);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    /// <summary>
    /// The cross-account remark view is closed outside development: <c>Remarks:GlobalRead</c> defaults to
    /// <c>IsDevelopment()</c> (Program.cs). Remark answers carry file and line references, i.e. code
    /// internals – on a real instance nobody reads another account's notes.
    /// </summary>
    [Fact]
    public async Task AnmerkungsBlick_UeberAlleKonten_IstInProduktionZu()
    {
        var client = factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, "Anmerkungs-Leser", "2345");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var all = await client.GetAsync("/api/v1/remarks?scope=all");
        Assert.Equal(HttpStatusCode.Forbidden, all.StatusCode);
        var problem = await all.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("remark_scope_forbidden", problem.GetProperty("code").GetString());

        // Counter-check in the same breath: without the scope the list works. Otherwise this test would
        // also pass if remarks were broken outright in production.
        var own = await client.GetAsync("/api/v1/remarks");
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
    }

    /// <summary>
    /// Without <c>Jwt:Key</c> the host must not come up outside development – otherwise a real instance
    /// would silently sign with the fallback key that is public in the source tree.
    /// <para>
    /// Asserted on the <b>message</b>, not the exception type: <c>WebApplicationFactory</c> runs the entry
    /// point on its own thread and hands the failure on wrapped – sometimes the original exception,
    /// sometimes an "entry point exited without ever building an IHost". Pinning the type would make the
    /// test hostage to that plumbing, so the assertion reads <c>ToString()</c>, which renders every
    /// nesting level and every arm of an <c>AggregateException</c>.
    /// </para>
    /// </summary>
    [Fact]
    public void OhneJwtSchluessel_BrichtDerStartAb()
    {
        using var brittle = new ProductionWithoutJwtKeyFactory();

        var thrown = Record.Exception(() => brittle.CreateClient());

        Assert.NotNull(thrown);
        Assert.Contains("Jwt:Key", thrown.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// A database from the <b>old</b> migration chain has a complete schema but none of the known
    /// migrations. <c>Migrate()</c> would try to apply the <c>InitialCreate</c> and fail with
    /// <c>table "Adults" already exists</c> – a message that points at nothing. Program.cs catches that
    /// case and replaces it with one an action follows from.
    /// <para>
    /// <b>Expires with the first release.</b> "The chain is exactly one migration" is a rule of the
    /// unpublished phase (CLAUDE.md → EF migrations, <c>SchemaGuardTests</c> keeps the length at 1). Once
    /// the rule is dropped, this test goes with it – it is not a permanent assurance.
    /// </para>
    /// </summary>
    [Fact]
    public void DatenbankAusDerAltenKette_MeldetKlartextStattEfFehler()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"pugling_altkette_{Guid.NewGuid():N}.db");
        PrepareForeignMigrationHistory(dbPath, "20250101000000_EineFremdeMigration");
        using var legacy = new LegacyMigrationChainFactory(dbPath);

        var thrown = Record.Exception(() => legacy.CreateClient());

        Assert.NotNull(thrown);
        var reported = thrown.ToString();
        Assert.Contains("alten Migrationskette", reported, StringComparison.Ordinal);
        Assert.Contains("EineFremdeMigration", reported, StringComparison.Ordinal);
        // The counter-check that makes the assertion worth anything: the raw EF message must NOT be what
        // reaches the operator.
        Assert.DoesNotContain("already exists", reported, StringComparison.OrdinalIgnoreCase);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers an adult through the anonymous endpoint and logs it in. Each caller brings its own name
    /// and PIN: the tests that use the class fixture share <b>one</b> host and thus one database, and the
    /// login rate limit stays on here.
    /// </summary>
    private static async Task<string> RegisterAndLoginAsync(HttpClient client, string name, string pin)
    {
        var created = await client.PostAsJsonAsync("/api/v1/supervisor/adults",
            new { name, email = (string?)null, pin });
        created.EnsureSuccessStatusCode();
        var adultId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var login = await client.PostAsJsonAsync("/api/v1/auth/adult", new { adultId, pin });
        login.EnsureSuccessStatusCode();
        return (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    /// <summary>
    /// Creates a SQLite file that looks like a database of the old chain: the EF history table with an
    /// entry the current assembly does not know. The schema itself stays empty – Program.cs decides on
    /// the history, and an empty file is enough to reach that decision.
    /// </summary>
    private static void PrepareForeignMigrationHistory(string dbPath, string migrationId)
    {
        // `Pooling=False` for the same reason the factory sets it: a pooled connection keeps the file handle
        // open past Dispose, and the throwaway file would then survive the run. This covers only the
        // preparation connection here - the host's own is handled in PuglingWebAppFactoryBase.
        using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=False");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL);
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ($id, '9.0.0');
            """;
        cmd.Parameters.AddWithValue("$id", migrationId);
        cmd.ExecuteNonQuery();
    }
}
