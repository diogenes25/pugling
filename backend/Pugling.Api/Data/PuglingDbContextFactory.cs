using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Pugling.Api.Data;

/// <summary>
/// Nur für die EF-Tools (<c>dotnet ef migrations/database</c>). Liefert den DbContext direkt,
/// damit die Tools nicht den kompletten Web-Host samt Seed hochfahren müssen. Die Verbindung
/// dient nur der Modell-/Migrations-Erzeugung, nicht der Laufzeit.
/// </summary>
public sealed class PuglingDbContextFactory : IDesignTimeDbContextFactory<PuglingDbContext>
{
    public PuglingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PuglingDbContext>()
            .UseSqlite("Data Source=pugling.db")
            .Options;
        return new PuglingDbContext(options);
    }
}
