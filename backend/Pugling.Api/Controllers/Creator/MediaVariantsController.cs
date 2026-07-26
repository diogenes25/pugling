using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Die technische Achse des Medien-Stores: <b>dieselbe Darstellung in mehreren Auflösungen/Formaten</b>
/// (Thumbnail in der Liste, Karte in der Übung, groß in der Vorschau). Adressiert wird über den
/// semantischen <see cref="MediaPurpose"/>, nicht über Pixelmaße – so kann die Auslieferung später auf
/// andere Größen umstellen, ohne den Vertrag zu brechen. Je Asset ist (Zweck, Format) eindeutig;
/// mehrere Formate pro Zweck sind erwünscht (webp + avif für <c>&lt;picture&gt;</c>/srcset).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/media/{assetId:int}/variants")]
[Tags("Creator – Media Store")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class MediaVariantsController(PuglingDbContext db) : ControllerBase
{
    internal static MediaVariantResponse Map(MediaVariant v) =>
        new(v.Id, v.Purpose, v.Width, v.Height, v.Format, v.Url, v.Bytes);

    /// <summary>Alle Auflösungen eines Assets (nach Zweck, dann Format).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<MediaVariantResponse>>> List(int assetId)
    {
        if (!await db.MediaAssets.AnyAsync(a => a.Id == assetId)) return NotFound();

        var variants = await db.MediaVariants.AsNoTracking()
            .Where(v => v.MediaAssetId == assetId)
            .ToListAsync();

        // Sortiert wird bewusst im Speicher: `Purpose` liegt als String in der DB, ein `OrderBy` in SQL
        // ordnete daher alphabetisch (Card, Full, Hero, Thumb) statt in der semantischen Reihenfolge des
        // Enums (Thumb → Card → Full → Hero) – und widerspräche damit derselben Liste, die
        // <see cref="MediaAssetsController.Map"/> am Asset ausliefert.
        return variants
            .OrderBy(v => v.Purpose).ThenBy(v => v.Format, StringComparer.Ordinal)
            .Select(Map)
            .ToList();
    }

    /// <summary>Reicht eine Auflösung nach. (Zweck, Format) muss am Asset noch frei sein.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MediaVariantResponse>> Create(int assetId, CreateMediaVariantDto dto)
    {
        if (!await db.MediaAssets.AnyAsync(a => a.Id == assetId)) return NotFound();
        if (MediaAssetsController.Validate(dto.Url, dto.Width, dto.Height, dto.Format) is { } error)
            return this.ProblemWithCode(ApiErrors.ValidationError, error);

        var format = dto.Format.Trim().ToLowerInvariant();
        if (await db.MediaVariants.AnyAsync(v => v.MediaAssetId == assetId && v.Purpose == dto.Purpose && v.Format == format))
            return this.ProblemWithCode(ApiErrors.MediaVariantExists,
                $"The asset already has a variant for purpose '{dto.Purpose}' and format '{format}'.");

        var variant = MediaAssetsController.NewVariant(dto);
        variant.MediaAssetId = assetId;
        db.MediaVariants.Add(variant);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(List), new { assetId }, Map(variant));
    }

    /// <summary>Ändert eine Auflösung (partiell).</summary>
    [HttpPatch("{variantId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MediaVariantResponse>> Update(int assetId, int variantId, UpdateMediaVariantDto dto)
    {
        var variant = await db.MediaVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.MediaAssetId == assetId);
        if (variant is null) return this.ProblemWithCode(ApiErrors.MediaVariantNotFound, "The variant does not belong to this asset.");

        if (MediaAssetsController.Validate(dto.Url ?? variant.Url, dto.Width ?? variant.Width,
                dto.Height ?? variant.Height, dto.Format) is { } error)
            return this.ProblemWithCode(ApiErrors.ValidationError, error);

        var purpose = dto.Purpose ?? variant.Purpose;
        var format = dto.Format?.Trim().ToLowerInvariant() ?? variant.Format;
        // Verschiebt der PATCH die Variante auf einen belegten Slot, wäre der Unique-Index ein 500 –
        // deshalb vorher als klarer Konflikt melden.
        if ((purpose != variant.Purpose || format != variant.Format)
            && await db.MediaVariants.AnyAsync(v => v.MediaAssetId == assetId && v.Id != variantId
                && v.Purpose == purpose && v.Format == format))
            return this.ProblemWithCode(ApiErrors.MediaVariantExists,
                $"The asset already has a variant for purpose '{purpose}' and format '{format}'.");

        variant.Purpose = purpose;
        variant.Format = format;
        if (dto.Url is not null) variant.Url = dto.Url.Trim();
        if (dto.Width.HasValue) variant.Width = dto.Width.Value;
        if (dto.Height.HasValue) variant.Height = dto.Height.Value;
        if (dto.Bytes.HasValue) variant.Bytes = dto.Bytes;

        await db.SaveChangesAsync();
        return Map(variant);
    }

    /// <summary>Löscht eine Auflösung. Das Asset bleibt bestehen (ggf. ohne Datei).</summary>
    [HttpDelete("{variantId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int assetId, int variantId)
    {
        var variant = await db.MediaVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.MediaAssetId == assetId);
        if (variant is null) return this.ProblemWithCode(ApiErrors.MediaVariantNotFound, "The variant does not belong to this asset.");

        db.MediaVariants.Remove(variant);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
