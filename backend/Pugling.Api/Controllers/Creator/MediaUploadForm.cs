namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Form of the image upload (multipart/form-data).
/// <para>
/// Deliberately <b>not</b> in the contracts project: <see cref="IFormFile"/> is an ASP.NET Core type, and
/// <c>Pugling.Contracts</c> is a leaf without framework dependencies – otherwise a client could no longer
/// use it in isolation. The response is instead the regular <c>MediaAssetResponse</c> from the contract.
/// </para>
/// </summary>
/// <param name="File">The image file; the server generates all resolutions from it itself.</param>
/// <param name="Description">What is shown – doubles as the alt text. Required.</param>
/// <param name="Key">Optional stable reference key; empty = derived from the description.</param>
/// <param name="Tags">Tags, comma-separated (multipart has no list semantics like JSON).</param>
/// <param name="Rating">Suitability; without a value, the strictest level.</param>
/// <param name="Origin">Origin; without a value <see cref="MediaOrigin.Upload"/>.</param>
/// <param name="License">Short license designation, if the source requires it.</param>
/// <param name="Attribution">Attribution, if the license requires it.</param>
public record MediaUploadForm(
    IFormFile? File,
    string? Description,
    string? Key = null,
    string? Tags = null,
    ContentRating? Rating = null,
    MediaOrigin? Origin = null,
    string? License = null,
    string? Attribution = null);
