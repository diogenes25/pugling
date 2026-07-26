using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Die geteilte Mechanik der Bild-Zuordnung. Drei Träger (Vokabel, Übungs-Item, Übung) verhalten sich
/// identisch – auflösen, auf Dublette prüfen, anlegen, listen –, unterscheiden sich aber in Route und
/// Rechte-Prüfung. Ohne diese Stelle stünde derselbe Ablauf dreimal in zwei Controllern.
/// </summary>
public class MediaLinkService(PuglingDbContext db)
{
    /// <summary>Welcher Träger gemeint ist; bestimmt die zu setzende FK-Spalte.</summary>
    public enum Carrier { Vocabulary, ExerciseItem, Exercise }

    /// <summary>
    /// Legt eine Zuordnung an. Liefert entweder den fertigen Link oder einen Fehler-Code, den der
    /// Controller unverändert als <c>ProblemDetails</c> ausgibt (die Ebene kennt kein HTTP).
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

    /// <summary>Die Zuordnungen eines Trägers, bester redaktioneller Rang zuerst.</summary>
    public async Task<IReadOnlyList<MediaLink>> ListAsync(Carrier carrier, int carrierId, CancellationToken ct = default) =>
        await WithAssetGraph(db.MediaLinks.AsNoTracking())
            .Where(Match(carrier, carrierId))
            .OrderByDescending(l => l.Weight).ThenBy(l => l.Id)
            .ToListAsync(ct);

    /// <summary>Findet eine Zuordnung <b>innerhalb</b> ihres Trägers – so kann eine fremde Zuordnung nie getroffen werden.</summary>
    public Task<MediaLink?> FindAsync(Carrier carrier, int carrierId, int linkId, CancellationToken ct = default) =>
        db.MediaLinks.Where(Match(carrier, carrierId)).FirstOrDefaultAsync(l => l.Id == linkId, ct);

    /// <summary>Löst die Zuordnung. Das Asset selbst bleibt im Store (es hängt womöglich an anderen Objekten).</summary>
    public async Task RemoveAsync(MediaLink link, CancellationToken ct = default)
    {
        db.MediaLinks.Remove(link);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Projiziert eine Zuordnung samt Asset (Varianten + Tags) in den Vertrag.</summary>
    public static MediaLinkResponse Map(MediaLink link) =>
        new(link.Id, link.Weight, Controllers.Creator.MediaAssetsController.Map(link.MediaAsset!));

    /// <summary>Lädt Asset samt Varianten/Tags für eine frisch angelegte Zuordnung nach.</summary>
    private Task<MediaAsset?> LoadAssetGraphAsync(int assetId, CancellationToken ct) =>
        db.MediaAssets.AsNoTracking()
            .Include(a => a.Variants)
            .Include(a => a.TagLinks).ThenInclude(t => t.InterestTag)
            .FirstOrDefaultAsync(a => a.Id == assetId, ct);

    private static IQueryable<MediaLink> WithAssetGraph(IQueryable<MediaLink> q) =>
        q.Include(l => l.MediaAsset!).ThenInclude(a => a.Variants)
            .Include(l => l.MediaAsset!).ThenInclude(a => a.TagLinks).ThenInclude(t => t.InterestTag);

    /// <summary>Das Trägerprädikat an einer Stelle – sonst schleicht sich irgendwo die falsche Spalte ein.</summary>
    private static System.Linq.Expressions.Expression<Func<MediaLink, bool>> Match(Carrier carrier, int carrierId) =>
        carrier switch
        {
            Carrier.Vocabulary => l => l.VocabularyId == carrierId,
            Carrier.ExerciseItem => l => l.ExerciseItemId == carrierId,
            _ => l => l.ExerciseId == carrierId,
        };

    private Task<bool> ExistsAsync(Carrier carrier, int carrierId, int assetId, CancellationToken ct) =>
        db.MediaLinks.Where(Match(carrier, carrierId)).AnyAsync(l => l.MediaAssetId == assetId, ct);

    /// <summary>Asset per Id oder – für Agenten, die über sprechende Keys arbeiten – per Key.</summary>
    private async Task<MediaAsset?> ResolveAssetAsync(AddMediaLinkDto dto, CancellationToken ct)
    {
        if (dto.MediaAssetId is { } id)
            return await db.MediaAssets.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (!string.IsNullOrWhiteSpace(dto.Key))
            return await db.MediaAssets.FirstOrDefaultAsync(a => a.Key == dto.Key, ct);
        return null;
    }
}
