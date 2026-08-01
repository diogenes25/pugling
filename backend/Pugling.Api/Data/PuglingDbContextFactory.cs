using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Pugling.Api.Data;

/// <summary>
/// For the EF tools only (<c>dotnet ef migrations/database</c>). It returns the DbContext directly so the
/// tools do not have to spin up the whole web host including the seed. The connection only serves the
/// model/migration generation, not the runtime.
/// </summary>
public sealed class PuglingDbContextFactory : IDesignTimeDbContextFactory<PuglingDbContext>
{
    /// <summary>Creates a <see cref="PuglingDbContext"/> against the local SQLite file, exclusively for the design-time tools.</summary>
    public PuglingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PuglingDbContext>()
            .UseSqlite("Data Source=pugling.db")
            .Options;
        return new PuglingDbContext(options);
    }
}
