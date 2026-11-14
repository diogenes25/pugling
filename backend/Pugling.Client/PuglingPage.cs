namespace Pugling.Client;

/// <summary>One window of a paginated list, together with the total the server reported.</summary>
/// <param name="Items">The rows of this window.</param>
/// <param name="TotalCount">
/// The unwindowed count from the <c>X-Total-Count</c> header; <c>null</c> if the server sent none.
/// </param>
public sealed record PuglingPage<T>(IReadOnlyList<T> Items, int? TotalCount)
{
    /// <summary>Did the server hold back rows beyond this window?</summary>
    public bool Truncated => TotalCount is { } total && total > Items.Count;
}
