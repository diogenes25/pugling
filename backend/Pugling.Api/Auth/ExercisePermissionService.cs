using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Auth;

/// <summary>
/// Die eine Stelle der RWX-Rechte-Regel für Übungen (löst das frühere 1-Autor-<c>Owns</c> als Durchsetzung ab).
/// Ein Recht hängt an einem <see cref="ExerciseGrant"/> für den anfragenden Creator (<c>fid</c>):
/// <list type="bullet">
/// <item><b>Write</b> (ändern) = Owner- oder Write-Grant.</item>
/// <item><b>Administer</b> (löschen, Rechte vergeben, Sichtbarkeit umschalten) = Owner-Grant.</item>
/// <item><b>Execute</b> (zuweisen) = <see cref="Exercise.ExecutePublic"/> ODER irgendein Grant.</item>
/// </list>
/// Read wird bewusst nicht geprüft – der Katalog bleibt für alle lesbar. Fehlt der <c>fid</c>, ist alles
/// fail-closed <c>false</c> (geseedete System-Übungen ohne Owner bleiben unverwaltbar).
/// </summary>
public class ExercisePermissionService(PuglingDbContext db)
{
    /// <summary>Darf der anfragende Creator die Übung inhaltlich ändern (Owner- oder Write-Grant, oder Admin)?</summary>
    public async Task<bool> CanWriteAsync(ClaimsPrincipal user, int exerciseId, CancellationToken ct = default)
    {
        if (user.IsAdmin()) return true;
        var fid = user.AdultId();
        return fid is not null && await db.ExerciseGrants.AnyAsync(g =>
            g.ExerciseId == exerciseId && g.CreatorId == fid
            && (g.Permission == GrantPermission.Owner || g.Permission == GrantPermission.Write), ct);
    }

    /// <summary>Darf der anfragende Creator die Übung verwalten – löschen, Rechte vergeben/entziehen, Sichtbarkeit umschalten (Owner-Grant, oder Admin)?</summary>
    public async Task<bool> CanAdministerAsync(ClaimsPrincipal user, int exerciseId, CancellationToken ct = default)
    {
        if (user.IsAdmin()) return true;
        var fid = user.AdultId();
        return fid is not null && await db.ExerciseGrants.AnyAsync(g =>
            g.ExerciseId == exerciseId && g.CreatorId == fid && g.Permission == GrantPermission.Owner, ct);
    }

    /// <summary>
    /// Darf der anfragende Creator die Übung zuweisen (in Lehrplan/Klassenarbeit aufnehmen)? Öffentlich
    /// ausführbare Übungen (Default) darf jeder; sonst nur mit einem Owner-/Write-/Execute-Grant (oder Admin).
    /// </summary>
    public async Task<bool> CanExecuteAsync(ClaimsPrincipal user, Exercise exercise, CancellationToken ct = default)
    {
        if (exercise.ExecutePublic || user.IsAdmin()) return true;
        var fid = user.AdultId();
        return fid is not null && await db.ExerciseGrants.AnyAsync(g =>
            g.ExerciseId == exercise.Id && g.CreatorId == fid, ct);
    }

    // ── In-Memory-Varianten für Projektionen mit bereits geladenen Grants (kein DB-Roundtrip pro Zeile) ──
    // Der Admin-Bypass wird per isAdmin-Flag hereingereicht (die statische Methode kennt den Principal nicht).

    /// <summary>Write-Regel auf einer geladenen Grant-Menge (für Listen/Detail-Projektionen).</summary>
    public static bool CanWrite(IEnumerable<ExerciseGrant> grants, int? fid, bool isAdmin = false) =>
        isAdmin || (fid is int f && grants.Any(g => g.CreatorId == f
            && (g.Permission is GrantPermission.Owner or GrantPermission.Write)));

    /// <summary>Owner-Regel auf einer geladenen Grant-Menge (für Listen/Detail-Projektionen).</summary>
    public static bool CanAdminister(IEnumerable<ExerciseGrant> grants, int? fid, bool isAdmin = false) =>
        isAdmin || (fid is int f && grants.Any(g => g.CreatorId == f && g.Permission == GrantPermission.Owner));
}
