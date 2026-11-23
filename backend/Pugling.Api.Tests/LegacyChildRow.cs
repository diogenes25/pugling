using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// Writes a child row into a database migrated only up to an older step. Today's <see cref="Child"/> entity carries
/// every column added since, so adding it through EF fails on such a database; the raw insert names only the columns the
/// <c>Children</c> table had at the base of the chain.
/// </summary>
internal static class LegacyChildRow
{
    /// <summary>Inserts a child with default values and returns its id.</summary>
    public static async Task<int> InsertAsync(PuglingDbContext db, string name)
    {
        var skins = $"[\"{SkinCatalog.Default}\"]";
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO Children (Name, SchoolType, Gender, Interests, AllowedContentRating, CreatedAt, SelectedSkin, OwnedSkins, ConcurrencyStamp)
            VALUES ({name}, 0, 'None', '[]', 0, '2026-11-01 08:00:00', {SkinCatalog.Default}, {skins}, {Guid.NewGuid().ToString()})
            """);
        return await db.Database.SqlQuery<int>($"SELECT MAX(Id) AS Value FROM Children").SingleAsync();
    }
}
