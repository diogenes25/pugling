namespace Pugling.Api.Controllers;

/// <summary>
/// Parst die Sortier-Angabe der Listen-Endpunkte. Unterstützt zwei Schreibweisen, die dieselbe Wirkung haben:
/// <c>?sort=title&amp;dir=desc</c> und die Kurzform <c>?sort=-title</c> (führendes <c>-</c> = absteigend, <c>+</c>/nichts = aufsteigend).
/// Ein explizites <c>dir</c> hat Vorrang vor dem Präfix. Welche <c>Key</c>s zulässig sind,
/// entscheidet der jeweilige Endpunkt (Whitelist) – hier findet bewusst kein dynamischer Property-Zugriff statt.
/// </summary>
public static class SortingExtensions
{
    /// <summary>Zerlegt die Angabe in (Spalten-Key, absteigend?). Ohne Angabe ist <c>Key</c> null → Endpunkt-Standard.</summary>
    public static (string? Key, bool Desc) ParseSort(string? sort, string? dir = null)
    {
        if (string.IsNullOrWhiteSpace(sort)) return (null, false);

        var key = sort.Trim();
        var desc = false;
        if (key.StartsWith('-')) { desc = true; key = key[1..]; }
        else if (key.StartsWith('+')) { key = key[1..]; }

        if (!string.IsNullOrWhiteSpace(dir))
            desc = dir.Equals("desc", StringComparison.OrdinalIgnoreCase);

        key = key.Trim();
        return (key.Length == 0 ? null : key, desc);
    }
}
