namespace Pugling.Api.Errors;

/// <summary>
/// Ein maschinenlesbarer Fehler-Code samt kanonischem HTTP-Status und RFC-7807-<c>type</c>-URI.
/// Der <see cref="Code"/> ist stabiler Vertragsbestandteil (der Client verzweigt/lokalisiert darauf) –
/// niemals umbenennen, nur additiv erweitern. Der Klartext-<c>detail</c> bleibt frei formulierbar.
/// </summary>
/// <param name="Code">Stabiler, maschinenlesbarer Code in snake_case (z. B. <c>insufficient_gems</c>).</param>
/// <param name="Status">Kanonischer HTTP-Statuscode dieses Fehlers.</param>
/// <param name="Title">Kurzer, statusartiger Titel (RFC-7807 <c>title</c>), englisch.</param>
public readonly record struct ApiError(string Code, int Status, string Title)
{
    /// <summary>Kanonischer <c>type</c>-URI der Form <c>https://pugling.app/errors/{code}</c>.</summary>
    public string TypeUri => $"https://pugling.app/errors/{Code}";
}
