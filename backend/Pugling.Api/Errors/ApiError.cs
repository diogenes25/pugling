namespace Pugling.Api.Errors;

/// <summary>
/// A machine-readable error code together with a canonical HTTP status and an RFC-7807 <c>type</c> URI.
/// The <see cref="Code"/> is a stable contract element (the client branches/localizes on it) –
/// never rename it, only extend additively. The plain-text <c>detail</c> remains freely worded.
/// </summary>
/// <param name="Code">Stable, machine-readable code in snake_case (e.g. <c>insufficient_gems</c>).</param>
/// <param name="Status">Canonical HTTP status code of this error.</param>
/// <param name="Title">Short, status-like title (RFC-7807 <c>title</c>), in English.</param>
public readonly record struct ApiError(string Code, int Status, string Title)
{
    /// <summary>Canonical <c>type</c> URI of the form <c>https://pugling.app/errors/{code}</c>.</summary>
    public string TypeUri => $"https://pugling.app/errors/{Code}";
}
