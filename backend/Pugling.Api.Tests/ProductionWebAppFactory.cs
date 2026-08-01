using Microsoft.AspNetCore.Hosting;

namespace Pugling.Api.Tests;

/// <summary>
/// The API in-process as a <b>non-development</b> host. Every other factory in this project runs
/// <c>Development</c>, which leaves the three <c>IsDevelopment()</c> branches in Program.cs and the dev
/// fallback key in <c>TokenService</c> completely unexecuted – the only configuration a real instance
/// ever uses was the one no test ever entered.
/// <para>
/// What changes with the environment name alone: the cross-account remark view closes
/// (<c>Remarks:GlobalRead</c>), the fail-fast on <c>Jwt:Key</c> arms, and the seed stops running
/// (<c>Seed:Enabled</c> defaults to false). The seed is deliberately <b>not</b> switched back on: an
/// empty database plus the anonymous registration is the real commissioning path of a fresh instance,
/// and that is what should be pinned.
/// </para>
/// <para>
/// The login rate limit stays <b>on</b> here (unlike <see cref="PuglingWebAppFactory"/>): it is part of
/// the production configuration. The limiter is per host, so the few logins of one test class stay far
/// below the 10-per-minute window – if this class grows, count the logins rather than flipping the switch.
/// </para>
/// </summary>
public class ProductionWebAppFactory : PuglingWebAppFactoryBase
{
    /// <summary>
    /// The configured signing key. Its value does not matter, only that it is <b>not</b> the dev fallback
    /// from <c>TokenService</c> – a token that validates against it proves the configured key took effect.
    /// </summary>
    internal const string JwtKey = "produktions-testschluessel-nicht-der-dev-fallback-0123456789";

    /// <summary>Creates a production host against a fresh throwaway database file.</summary>
    public ProductionWebAppFactory() { }

    /// <summary>Creates a production host against a prepared database file.</summary>
    /// <param name="dbPath">Path of the SQLite file to run against.</param>
    protected ProductionWebAppFactory(string dbPath) : base(dbPath) { }

    /// <inheritdoc />
    protected override string EnvironmentName => "Production";

    /// <inheritdoc />
    protected override void ConfigureFactory(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Key", JwtKey);
    }
}

/// <summary>
/// A production host <b>without</b> <c>Jwt:Key</c> – the fail-fast from <c>Program.cs</c> must abort the
/// start. Deliberately its own type instead of a flag on <see cref="ProductionWebAppFactory"/>: a factory
/// whose whole purpose is to throw has no business being reusable.
/// </summary>
public sealed class ProductionWithoutJwtKeyFactory : PuglingWebAppFactoryBase
{
    /// <inheritdoc />
    protected override string EnvironmentName => "Production";
}

/// <summary>
/// A production host pointed at a database that carries entries of the <b>old</b> migration chain – the
/// startup must abort with the plain-text message from Program.cs instead of EF's
/// <c>table "Adults" already exists</c>.
/// <para>
/// <b>This type has an expiry date.</b> Folding the chain into a single <c>InitialCreate</c> is a rule of
/// the unpublished phase (CLAUDE.md → EF migrations); it ends with the first release, and then this
/// factory and its test go with it.
/// </para>
/// </summary>
/// <param name="dbPath">The prepared SQLite file with the foreign migration history.</param>
public sealed class LegacyMigrationChainFactory(string dbPath) : ProductionWebAppFactory(dbPath);
