namespace Pugling.Api.Controllers;

/// <summary>
/// Parses the sort specification of the list endpoints. Supports two notations with the same effect:
/// <c>?sort=title&amp;dir=desc</c> and the short form <c>?sort=-title</c> (leading <c>-</c> = descending, <c>+</c>/none = ascending).
/// An explicit <c>dir</c> takes precedence over the prefix. Which <c>Key</c>s are allowed
/// is decided by the respective endpoint (whitelist) – no dynamic property access happens here, by design.
/// </summary>
public static class SortingExtensions
{
    /// <summary>Splits the specification into (column key, descending?). Without a value, <c>Key</c> is null → endpoint default.</summary>
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
