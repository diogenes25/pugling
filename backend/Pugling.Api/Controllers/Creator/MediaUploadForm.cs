namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Formular des Bild-Uploads (multipart/form-data).
/// <para>
/// Bewusst <b>nicht</b> im Vertrags-Projekt: <see cref="IFormFile"/> ist ein ASP.NET-Core-Typ, und
/// <c>Pugling.Contracts</c> ist ein Blatt ohne Framework-Abhängigkeiten – sonst könnte ein Client es
/// nicht mehr pur verwenden. Die Antwort ist dafür der reguläre <c>MediaAssetResponse</c> aus dem Vertrag.
/// </para>
/// </summary>
/// <param name="File">Die Bilddatei; der Server erzeugt daraus alle Auflösungen selbst.</param>
/// <param name="Description">Was zu sehen ist – zugleich der Alt-Text. Pflicht.</param>
/// <param name="Key">Optionaler stabiler Referenz-Key; leer = aus der Beschreibung abgeleitet.</param>
/// <param name="Tags">Schlagworte, kommagetrennt (Multipart kennt keine Listen-Semantik wie JSON).</param>
/// <param name="Rating">Eignung; ohne Angabe die strengste Stufe.</param>
/// <param name="Origin">Herkunft; ohne Angabe <see cref="MediaOrigin.Upload"/>.</param>
/// <param name="License">Lizenz-Kurzbezeichnung, falls die Quelle sie verlangt.</param>
/// <param name="Attribution">Urhebernennung, falls die Lizenz sie verlangt.</param>
public record MediaUploadForm(
    IFormFile? File,
    string? Description,
    string? Key = null,
    string? Tags = null,
    ContentRating? Rating = null,
    MediaOrigin? Origin = null,
    string? License = null,
    string? Attribution = null);
