using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;

namespace Pugling.Api.Data;

/// <summary>
/// Idempotenter Backfill beim Start: legt zu jedem bestehenden/geseedeten <c>Father</c> ein Konto mit den
/// Rollen Creator+Supervisor und zu jedem <c>Child</c> ein Konto mit der Rolle Student an (PIN-Hash wird
/// vom Father/Child übernommen). Bereits verknüpfte Profile werden übersprungen – der Lauf bleibt günstig
/// und wiederholbar (analog <see cref="ExerciseItemBackfill"/>).
/// </summary>
public static class AccountBackfill
{
    public static async Task RunAsync(PuglingDbContext db, AccountService accounts, CancellationToken ct = default)
    {
        foreach (var father in await db.Fathers.AsNoTracking().ToListAsync(ct))
            await accounts.EnsureForFatherAsync(father, ct);

        foreach (var child in await db.Children.AsNoTracking().ToListAsync(ct))
            await accounts.EnsureForChildAsync(child, ct);
    }
}
