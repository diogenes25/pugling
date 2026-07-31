using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// The technical axis of the media store: <b>the same representation in multiple resolutions/formats</b>
/// (thumbnail in the list, card in the exercise, large in the preview). Addressed via the
/// semantic <see cref="MediaPurpose"/>, not via pixel dimensions – so delivery can later switch to
/// other sizes without breaking the contract. Per asset, (purpose, format) is unique;
/// multiple formats per purpose are desired (webp + avif for <c>&lt;picture&gt;</c>/srcset).
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

    /// <summary>All resolutions of an asset (by purpose, then format).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<MediaVariantResponse>>> List(int assetId, CancellationToken ct = default)
    {
        if (!await db.MediaAssets.AnyAsync(a => a.Id == assetId, ct)) return NotFound();

        var variants = await db.MediaVariants.AsNoTracking()
            .Where(v => v.MediaAssetId == assetId)
            .ToListAsync(ct);

        // Sortiert wird bewusst im Speicher: `Purpose` liegt als String in der DB, ein `OrderBy` in SQL
        // ordnete daher alphabetisch (Card, Full, Hero, Thumb) statt in der semantischen Reihenfolge des
        // Enums (Thumb → Card → Full → Hero) – und widerspräche damit derselben Liste, die
        // <see cref="MediaAssetsController.Map"/> am Asset ausliefert.
        return variants
            .OrderBy(v => v.Purpose).ThenBy(v => v.Format, StringComparer.Ordinal)
            .Select(Map)
            .ToList();
    }

    /// <summary>Adds a resolution afterward. (Purpose, format) must still be free on the asset.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MediaVariantResponse>> Create(int assetId, CreateMediaVariantDto dto, CancellationToken ct = default)
    {
        if (!await db.MediaAssets.AnyAsync(a => a.Id == assetId, ct)) return NotFound();
        if (MediaAssetsController.Validate(dto.Url, dto.Width, dto.Height, dto.Format) is { } error)
            return this.ProblemWithCode(ApiErrors.ValidationError, error);

        var format = dto.Format.Trim().ToLowerInvariant();
        if (await db.MediaVariants.AnyAsync(v => v.MediaAssetId == assetId && v.Purpose == dto.Purpose && v.Format == format, ct))
            return this.ProblemWithCode(ApiErrors.MediaVariantExists,
                $"The asset already has a variant for purpose '{dto.Purpose}' and format '{format}'.");

        var variant = MediaAssetsController.NewVariant(dto);
        variant.MediaAssetId = assetId;
        db.MediaVariants.Add(variant);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(List), new { assetId }, Map(variant));
    }

    /// <summary>Changes a resolution (partial).</summary>
    [HttpPatch("{variantId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MediaVariantResponse>> Update(int assetId, int variantId, UpdateMediaVariantDto dto, CancellationToken ct = default)
    {
        var variant = await db.MediaVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.MediaAssetId == assetId, ct);
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
                && v.Purpose == purpose && v.Format == format, ct))
            return this.ProblemWithCode(ApiErrors.MediaVariantExists,
                $"The asset already has a variant for purpose '{purpose}' and format '{format}'.");

        variant.Purpose = purpose;
        variant.Format = format;
        if (dto.Url is not null) variant.Url = dto.Url.Trim();
        if (dto.Width.HasValue) variant.Width = dto.Width.Value;
        if (dto.Height.HasValue) variant.Height = dto.Height.Value;
        if (dto.Bytes.HasValue) variant.Bytes = dto.Bytes;

        await db.SaveChangesAsync(ct);
        return Map(variant);
    }

    /// <summary>Deletes a resolution. The asset remains (possibly without a file).</summary>
    [HttpDelete("{variantId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int assetId, int variantId, CancellationToken ct = default)
    {
        var variant = await db.MediaVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.MediaAssetId == assetId, ct);
        if (variant is null) return this.ProblemWithCode(ApiErrors.MediaVariantNotFound, "The variant does not belong to this asset.");

        db.MediaVariants.Remove(variant);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
