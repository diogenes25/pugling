using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// The shared mechanics of image linking. Three carriers (vocabulary, exercise item, exercise) behave
/// identically – resolve, check for duplicate, create, list – but differ in route and permission
/// check. Without this place, the same flow would sit three times across two controllers.
/// </summary>
public class MediaLinkService(PuglingDbContext db)
{
    /// <summary>Which carrier is meant; determines the FK column to set.</summary>
    public enum Carrier
    {
        /// <summary>A vocabulary in the store.</summary>
        Vocabulary,
        /// <summary>A single vocabulary item of an exercise.</summary>
        ExerciseItem,
        /// <summary>An entire exercise.</summary>
        Exercise,
    }

    /// <summary>
    /// Creates a link. Returns either the finished link or an error code that the
    /// controller emits unchanged as <c>ProblemDetails</c> (this layer doesn't know HTTP).
    /// </summary>
    public async Task<(MediaLink? Link, ApiError? Error, string? Detail)> LinkAsync(
        Carrier carrier, int carrierId, AddMediaLinkDto dto, CancellationToken ct = default)
    {
        var asset = await ResolveAssetAsync(dto, ct);
        if (asset is null)
            return (null, ApiErrors.InvalidReference, "Provide an existing mediaAssetId or key.");

        if (await ExistsAsync(carrier, carrierId, asset.Id, ct))
            return (null, ApiErrors.MediaAlreadyLinked, "The media asset is already linked to this object.");

        var link = new MediaLink { MediaAssetId = asset.Id, Weight = dto.Weight };
        switch (carrier)
        {
            case Carrier.Vocabulary: link.VocabularyId = carrierId; break;
            case Carrier.ExerciseItem: link.ExerciseItemId = carrierId; break;
            default: link.ExerciseId = carrierId; break;
        }

        db.MediaLinks.Add(link);
        await db.SaveChangesAsync(ct);

        link.MediaAsset = await LoadAssetGraphAsync(asset.Id, ct);
        return (link, null, null);
    }

    /// <summary>The links of a carrier, best editorial rank first.</summary>
    public async Task<IReadOnlyList<MediaLink>> ListAsync(Carrier carrier, int carrierId, CancellationToken ct = default) =>
        await WithAssetGraph(db.MediaLinks.AsNoTracking())
            .Where(Match(carrier, carrierId))
            .OrderByDescending(l => l.Weight).ThenBy(l => l.Id)
            .ToListAsync(ct);

    /// <summary>Finds a link <b>within</b> its carrier – so a foreign link can never be matched.</summary>
    public Task<MediaLink?> FindAsync(Carrier carrier, int carrierId, int linkId, CancellationToken ct = default) =>
        db.MediaLinks.Where(Match(carrier, carrierId)).FirstOrDefaultAsync(l => l.Id == linkId, ct);

    /// <summary>Removes the link. The asset itself remains in the store (it may still be attached to other objects).</summary>
    public async Task RemoveAsync(MediaLink link, CancellationToken ct = default)
    {
        db.MediaLinks.Remove(link);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Projects a link along with its asset (variants + tags) into the contract.</summary>
    public static MediaLinkResponse Map(MediaLink link) =>
        new(link.Id, link.Weight, Controllers.Creator.MediaAssetsController.Map(link.MediaAsset!));

    /// <summary>Loads the asset along with variants/tags for a freshly created link.</summary>
    private Task<MediaAsset?> LoadAssetGraphAsync(int assetId, CancellationToken ct) =>
        db.MediaAssets.AsNoTracking()
            .Include(a => a.Variants)
            .Include(a => a.TagLinks).ThenInclude(t => t.InterestTag)
            .FirstOrDefaultAsync(a => a.Id == assetId, ct);

    private static IQueryable<MediaLink> WithAssetGraph(IQueryable<MediaLink> q) =>
        q.Include(l => l.MediaAsset!).ThenInclude(a => a.Variants)
            .Include(l => l.MediaAsset!).ThenInclude(a => a.TagLinks).ThenInclude(t => t.InterestTag);

    /// <summary>The carrier predicate in one place – otherwise the wrong column sneaks in somewhere.</summary>
    private static System.Linq.Expressions.Expression<Func<MediaLink, bool>> Match(Carrier carrier, int carrierId) =>
        carrier switch
        {
            Carrier.Vocabulary => l => l.VocabularyId == carrierId,
            Carrier.ExerciseItem => l => l.ExerciseItemId == carrierId,
            _ => l => l.ExerciseId == carrierId,
        };

    private Task<bool> ExistsAsync(Carrier carrier, int carrierId, int assetId, CancellationToken ct) =>
        db.MediaLinks.Where(Match(carrier, carrierId)).AnyAsync(l => l.MediaAssetId == assetId, ct);

    /// <summary>Asset by id, or – for agents working with descriptive keys – by key.</summary>
    private async Task<MediaAsset?> ResolveAssetAsync(AddMediaLinkDto dto, CancellationToken ct)
    {
        if (dto.MediaAssetId is { } id)
            return await db.MediaAssets.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (!string.IsNullOrWhiteSpace(dto.Key))
            return await db.MediaAssets.FirstOrDefaultAsync(a => a.Key == dto.Key, ct);
        return null;
    }
}
