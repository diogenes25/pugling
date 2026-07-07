using Microsoft.EntityFrameworkCore;

namespace Pugling.Api.Controllers;

/// <summary>Geteiltes Offset-Paging für Listen-Endpunkte (skip/take + Header <c>X-Total-Count</c>).</summary>
public static class PagingExtensions
{
    /// <summary>Standard-Seitengröße, wenn der Aufrufer kein take angibt.</summary>
    public const int DefaultTake = 100;

    /// <summary>Obergrenze pro Seite (Schutz vor Voll-Scans).</summary>
    public const int MaxTake = 500;

    /// <summary>
    /// Führt eine gefilterte, sortierte Query seitenweise aus: setzt zuerst die Gesamttrefferzahl
    /// in den Header <c>X-Total-Count</c> (vor dem Body!), wendet dann Skip/Take an.
    /// <paramref name="take"/> wird auf 0..<see cref="MaxTake"/> geklemmt (<c>0</c> = nur zählen, keine Zeilen laden –
    /// nützlich für reine Kennzahlen), <paramref name="skip"/> auf &gt;= 0.
    /// Erwartet eine bereits mit <c>OrderBy</c> versehene Query, damit das Fenster deterministisch ist.
    /// </summary>
    public static async Task<List<T>> ToPagedListAsync<T>(
        this IQueryable<T> query, HttpResponse response, int skip, int take, CancellationToken ct = default)
    {
        response.Headers["X-Total-Count"] = (await query.CountAsync(ct)).ToString();
        return await query.Skip(Math.Max(skip, 0)).Take(Math.Clamp(take, 0, MaxTake)).ToListAsync(ct);
    }

    /// <summary>
    /// In-Memory-Variante von <see cref="ToPagedListAsync{T}"/> für bereits materialisierte, sortierte
    /// Listen (z. B. Kennzahlen aus Services ohne <c>IQueryable</c>): schreibt die Gesamtzahl in
    /// <c>X-Total-Count</c> (vor dem Body!), wendet dann Skip/Take an. <paramref name="take"/> wird auf
    /// 0..<see cref="MaxTake"/> geklemmt, <paramref name="skip"/> auf &gt;= 0. Erwartet eine bereits
    /// sortierte Quelle, damit das Fenster deterministisch ist.
    /// </summary>
    public static List<T> ToPagedList<T>(this IEnumerable<T> source, HttpResponse response, int skip, int take)
    {
        var all = source as IReadOnlyCollection<T> ?? source.ToList();
        response.Headers["X-Total-Count"] = all.Count.ToString();
        return all.Skip(Math.Max(skip, 0)).Take(Math.Clamp(take, 0, MaxTake)).ToList();
    }
}
