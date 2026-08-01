using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Media store: <b>one motif, many images</b>. An asset is not "the image for <i>running</i>", but
/// a concrete <b>representation</b> – the running unicorn in comic style, cartoon, the jogging person as
/// a photo. Which of these a child later sees is decided by their profile; here only the stock is maintained.
/// <para>
/// Two axes stay strictly separate: the <b>content</b> axis (this asset, with style tags and suitability)
/// and the <b>technical</b> axis (resolutions of the same asset, see <see cref="MediaVariantsController"/>).
/// Bytes never live in the DB – only URLs, as with the pronunciation audio source of the vocabulary store.
/// </para>
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/media")]
[Tags("Creator – Media Store")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class MediaAssetsController(PuglingDbContext db, InterestTagService tags, IMediaStorage storage,
    MediaImageProcessor images, MediaOptions mediaOptions) : ControllerBase
{
    internal static MediaAssetResponse Map(MediaAsset a) =>
        new(a.Id, a.Key, a.Description, a.Kind, a.Rating, a.License, a.Attribution, a.Origin, a.Source,
            a.Placeholder,
            [.. a.Variants.OrderBy(v => v.Purpose).ThenBy(v => v.Format, StringComparer.Ordinal).Select(MediaVariantsController.Map)],
            [.. a.TagLinks.Where(l => l.InterestTag is not null).Select(l => l.InterestTag!.Slug).OrderBy(s => s, StringComparer.Ordinal)],
            a.CreatedAt);

    /// <summary>Base query with the navigations needed for <see cref="Map"/> (variants + tags).</summary>
    private static IQueryable<MediaAsset> WithGraph(IQueryable<MediaAsset> q) =>
        q.Include(a => a.Variants).Include(a => a.TagLinks).ThenInclude(l => l.InterestTag);

    /// <summary>
    /// List of assets, optionally filtered. <paramref name="maxRating"/> is the filter that makes the
    /// target-audience separation visible: it returns only what is approved for the given level –
    /// the same cut that the later automatic selection per child applies strictly. The total count
    /// (before paging) is in the <c>X-Total-Count</c> header.
    /// </summary>
    /// <param name="search">Substring in description or key.</param>
    /// <param name="tag">One or more tag slugs (repeatable).</param>
    /// <param name="matchAll">With multiple tags: true = all (AND), false = any (OR, default).</param>
    /// <param name="kind">Only assets of this media kind.</param>
    /// <param name="maxRating">Highest allowed suitability level (e.g. <c>Everyone</c> for the child view).</param>
    /// <param name="origin">Only assets of this origin (e.g. only AI-generated ones).</param>
    /// <param name="withoutVariants">true = only assets without any file (unfinished, like the vocabulary filter <c>incomplete</c>).</param>
    /// <param name="untagged">true = only assets without tags (practically invisible for selection).</param>
    /// <param name="sort">Sort column: <c>key</c> (default), <c>description</c>, <c>rating</c>, <c>created</c>. Short form <c>-created</c> = descending.</param>
    /// <param name="dir"><c>asc</c> (default) or <c>desc</c>; takes precedence over a <c>-</c> prefix in <paramref name="sort"/>.</param>
    /// <param name="skip">Number of entries to skip (paging).</param>
    /// <param name="take">Maximum number of hits (1..500).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<IEnumerable<MediaAssetResponse>> List(
        [FromQuery] string? search = null,
        [FromQuery] string[]? tag = null,
        [FromQuery] bool matchAll = false,
        [FromQuery] MediaKind? kind = null,
        [FromQuery] ContentRating? maxRating = null,
        [FromQuery] MediaOrigin? origin = null,
        [FromQuery] bool? withoutVariants = null,
        [FromQuery] bool? untagged = null,
        [FromQuery] string? sort = null,
        [FromQuery] string? dir = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var query = db.MediaAssets.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Description.Contains(search) || a.Key.Contains(search));
        if (kind is not null)
            query = query.Where(a => a.Kind == kind);
        // An ordering comparison - only possible because the rating is persisted as an int (see DbContext).
        if (maxRating is not null)
            query = query.Where(a => a.Rating <= maxRating);
        if (origin is not null)
            query = query.Where(a => a.Origin == origin);
        if (withoutVariants is true)
            query = query.Where(a => a.Variants.Count == 0);
        if (untagged is true)
            query = query.Where(a => a.TagLinks.Count == 0);

        var slugs = tag?.Select(t => InterestSlug.From(t)).Where(t => t.Length > 0).Distinct(StringComparer.Ordinal).ToList();
        if (slugs is { Count: > 0 })
        {
            if (matchAll)
                foreach (var slug in slugs)
                    query = query.Where(a => a.TagLinks.Any(l => l.InterestTag!.Slug == slug));
            else
                query = query.Where(a => a.TagLinks.Any(l => slugs.Contains(l.InterestTag!.Slug)));
        }

        var items = await WithGraph(ApplySort(query, SortingExtensions.ParseSort(sort, dir)))
            .ToPagedListAsync(Response, skip, take, ct);
        return items.Select(Map);
    }

    /// <summary>
    /// Applies the sorting allowed via whitelist; every variant ends with <c>Id</c> as a tiebreaker,
    /// so the paging window stays deterministic. Unknown/empty keys → default by <c>Key</c>.
    /// </summary>
    private static IOrderedQueryable<MediaAsset> ApplySort(IQueryable<MediaAsset> q, (string? Key, bool Desc) sort) =>
        (sort.Key?.ToLowerInvariant(), sort.Desc) switch
        {
            ("description", false) => q.OrderBy(a => a.Description).ThenBy(a => a.Id),
            ("description", true) => q.OrderByDescending(a => a.Description).ThenBy(a => a.Id),
            ("rating", false) => q.OrderBy(a => a.Rating).ThenBy(a => a.Id),
            ("rating", true) => q.OrderByDescending(a => a.Rating).ThenBy(a => a.Id),
            ("created", false) => q.OrderBy(a => a.CreatedAt).ThenBy(a => a.Id),
            ("created", true) => q.OrderByDescending(a => a.CreatedAt).ThenBy(a => a.Id),
            (_, true) => q.OrderByDescending(a => a.Key).ThenBy(a => a.Id),
            _ => q.OrderBy(a => a.Key).ThenBy(a => a.Id),
        };

    /// <summary>An asset by numeric id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaAssetResponse>> Get(int id, CancellationToken ct = default)
    {
        var asset = await WithGraph(db.MediaAssets.AsNoTracking()).FirstOrDefaultAsync(a => a.Id == id, ct);
        return asset is null ? NotFound() : Map(asset);
    }

    /// <summary>An asset by stable key (reference slug).</summary>
    [HttpGet("by-key/{key}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaAssetResponse>> GetByKey(string key, CancellationToken ct = default)
    {
        var asset = await WithGraph(db.MediaAssets.AsNoTracking()).FirstOrDefaultAsync(a => a.Key == key, ct);
        return asset is null ? NotFound() : Map(asset);
    }

    /// <summary>
    /// Creates an asset. If the key is missing, a unique one is generated from the description. Tags (slugs)
    /// and variants may be included right away – an agent thus creates a finished representation in one go.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MediaAssetResponse>> Create(CreateMediaAssetDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Description))
            return this.ProblemWithCode(ApiErrors.ValidationError, "Description is required (it doubles as the alt text).");

        string key;
        if (string.IsNullOrWhiteSpace(dto.Key))
        {
            key = await UniqueKeyAsync(InterestSlug.From(dto.Description), ct);
        }
        else
        {
            key = dto.Key.Trim();
            if (await db.MediaAssets.AnyAsync(a => a.Key == key, ct))
                return this.ProblemWithCode(ApiErrors.DuplicateKey, $"Key '{key}' already exists.");
        }

        foreach (var variant in dto.Variants ?? [])
            if (Validate(variant.Url, variant.Width, variant.Height, variant.Format) is { } error)
                return this.ProblemWithCode(ApiErrors.ValidationError, error);

        // A duplicate (purpose, format) would only fail at the unique index - report it as a clear 409 first.
        var duplicate = (dto.Variants ?? [])
            .GroupBy(v => (v.Purpose, Format: v.Format.Trim().ToLowerInvariant()))
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            return this.ProblemWithCode(ApiErrors.MediaVariantExists,
                $"Duplicate variant for purpose '{duplicate.Key.Purpose}' and format '{duplicate.Key.Format}'.");

        var asset = new MediaAsset
        {
            Key = key,
            Description = dto.Description.Trim(),
            Kind = dto.Kind,
            Rating = dto.Rating,
            License = Trimmed(dto.License),
            Attribution = Trimmed(dto.Attribution),
            Origin = dto.Origin,
            Source = Trimmed(dto.Source),
            Placeholder = Trimmed(dto.Placeholder),
            Variants = [.. (dto.Variants ?? []).Select(NewVariant)],
        };
        db.MediaAssets.Add(asset);

        foreach (var tag in await tags.EnsureManyAsync(dto.Tags ?? [], ct: ct))
            asset.TagLinks.Add(new MediaTagLink { MediaAsset = asset, InterestTag = tag });

        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = asset.Id }, Map(asset));
    }

    /// <summary>
    /// Creates an asset from an <b>uploaded file</b>: the server decodes it and generates the
    /// resolutions itself (thumb/card/full, aspect-ratio-preserving, WebP) plus a placeholder color.
    /// This is the convenient path compared to the URL endpoint – the creator needs neither an image source on
    /// the web nor a graphics program to scale it.
    /// <para>
    /// No <see cref="MediaPurpose.Hero"/>: the wide header format requires cropping, and where a
    /// motif may be cropped, only a human can decide.
    /// </para>
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MediaAssetResponse>> Upload([FromForm] MediaUploadForm form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Description))
            return this.ProblemWithCode(ApiErrors.ValidationError, "Description is required (it doubles as the alt text).");
        if (form.File is not { Length: > 0 })
            return this.ProblemWithCode(ApiErrors.ValidationError, "A file is required.");
        if (form.File.Length > mediaOptions.MaxUploadBytes)
            return this.ProblemWithCode(ApiErrors.MediaUploadTooLarge,
                $"The file exceeds the limit of {mediaOptions.MaxUploadBytes / (1024 * 1024)} MB.");

        string key;
        if (string.IsNullOrWhiteSpace(form.Key))
        {
            key = await UniqueKeyAsync(InterestSlug.From(form.Description), ct);
        }
        else
        {
            key = form.Key.Trim();
            if (await db.MediaAssets.AnyAsync(a => a.Key == key, ct))
                return this.ProblemWithCode(ApiErrors.DuplicateKey, $"Key '{key}' already exists.");
        }

        using var buffer = new MemoryStream();
        await form.File.CopyToAsync(buffer, ct);

        ProcessedImage processed;
        try
        {
            processed = images.Process(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
        }
        catch (ArgumentException e)
        {
            // Not decodable (wrong type, broken file) - a user error, not a server error.
            return this.ProblemWithCode(ApiErrors.MediaNotAnImage, e.Message);
        }

        var asset = new MediaAsset
        {
            Key = key,
            Description = form.Description.Trim(),
            Rating = form.Rating ?? ContentRating.Everyone,
            License = Trimmed(form.License),
            Attribution = Trimmed(form.Attribution),
            Origin = form.Origin ?? MediaOrigin.Upload,
            Placeholder = processed.Placeholder,
        };
        db.MediaAssets.Add(asset);

        foreach (var tag in await tags.EnsureManyAsync(SplitTags(form.Tags), ct: ct))
            asset.TagLinks.Add(new MediaTagLink { MediaAsset = asset, InterestTag = tag });

        // Save first: the storage folder is named after the id. Unlike the key, that is guaranteed to be
        // filesystem-safe and never changes, even if the key were to become renamable one day.
        await db.SaveChangesAsync(ct);

        foreach (var rendered in processed.Variants)
        {
            var url = await storage.SaveAsync($"{asset.Id}/{rendered.Purpose}.{rendered.Format}".ToLowerInvariant(),
                rendered.Content, ct);
            asset.Variants.Add(new MediaVariant
            {
                Purpose = rendered.Purpose,
                Width = rendered.Width,
                Height = rendered.Height,
                Format = rendered.Format,
                Url = url,
                Bytes = rendered.Content.Length,
            });
        }
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = asset.Id }, Map(asset));
    }

    /// <summary>Changes an asset (partial). Tags are <b>added</b>, not replaced (removal via DELETE).</summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaAssetResponse>> Update(int id, UpdateMediaAssetDto dto, CancellationToken ct = default)
    {
        var asset = await WithGraph(db.MediaAssets).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (asset is null) return NotFound();

        if (dto.Description is not null)
        {
            var description = dto.Description.Trim();
            if (description.Length == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "Description must not be empty.");
            asset.Description = description;
        }
        if (dto.Kind.HasValue) asset.Kind = dto.Kind.Value;
        if (dto.Rating.HasValue) asset.Rating = dto.Rating.Value;
        if (dto.License is not null) asset.License = Trimmed(dto.License);
        if (dto.Attribution is not null) asset.Attribution = Trimmed(dto.Attribution);
        if (dto.Origin.HasValue) asset.Origin = dto.Origin.Value;
        if (dto.Source is not null) asset.Source = Trimmed(dto.Source);
        if (dto.Placeholder is not null) asset.Placeholder = Trimmed(dto.Placeholder);

        await AttachAsync(asset, dto.Tags, ct);
        await db.SaveChangesAsync(ct);
        return Map(asset);
    }

    /// <summary>
    /// Where this image is assigned (vocabulary entries, exercise items, exercises) – the reverse direction of the assignment.
    /// Deliberately its own endpoint rather than a delete lock: unlike a missing vocabulary entry, a
    /// missing image leaves no placeholder, it only shrinks the selection. The creator should be able to
    /// <i>see</i> what they lose before deleting – they are not held back.
    /// </summary>
    [HttpGet("{id:int}/usage")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<MediaUsage>>> Usage(int id, CancellationToken ct = default)
    {
        if (!await db.MediaAssets.AnyAsync(a => a.Id == id, ct)) return NotFound();

        var rows = await db.MediaLinks.AsNoTracking()
            .Where(l => l.MediaAssetId == id)
            .Select(l => new
            {
                l.Weight,
                l.VocabularyId,
                VocabularyWord = l.Vocabulary!.Word,
                l.ExerciseItemId,
                ItemWord = l.ExerciseItem!.Vocabulary!.Word,
                ItemExerciseTitle = l.ExerciseItem.Exercise!.Title,
                l.ExerciseId,
                ExerciseTitle = l.Exercise!.Title,
            })
            .ToListAsync(ct);

        return rows.Select(r => r switch
        {
            { VocabularyId: { } vid } => new MediaUsage("vocabulary", vid, r.VocabularyWord, r.Weight),
            { ExerciseItemId: { } iid } => new MediaUsage("item", iid, $"{r.ItemWord} ({r.ItemExerciseTitle})", r.Weight),
            _ => new MediaUsage("exercise", r.ExerciseId!.Value, r.ExerciseTitle, r.Weight),
        }).ToList();
    }

    /// <summary>
    /// Deletes an asset along with its variants, tag links, and assignments (cascade). Not
    /// locked while it is in use – see <see cref="Usage"/> for the rationale.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var asset = await db.MediaAssets.FindAsync([id], ct);
        if (asset is null) return NotFound();
        db.MediaAssets.Remove(asset);
        await db.SaveChangesAsync(ct);
        // Clean up the files only after the DB delete succeeded: if the DB aborts, the files are still there
        // (an orphaned file is harmless, an asset without a file would be a broken card). Assets that were only
        // entered by URL have no folder of their own - the delete then runs empty. Deliberately WITHOUT the
        // request token: the delete is committed, cleaning up is the compensating step. A client abort must not
        // decide whether it runs - otherwise the variant files would lie around forever without an owning row
        // (there is no cleanup job).
        await storage.DeleteFolderAsync(asset.Id.ToString(), CancellationToken.None);
        return NoContent();
    }

    /// <summary>
    /// Links an asset with tags from the shared taxonomy (create-if-missing, already
    /// linked ones are skipped). Returns the asset with its current tags.
    /// </summary>
    [HttpPost("{id:int}/tags")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaAssetResponse>> AttachTags(int id, TagMediaDto dto, CancellationToken ct = default)
    {
        var asset = await WithGraph(db.MediaAssets).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (asset is null) return NotFound();
        if (dto.Tags is not { Count: > 0 }) return this.ProblemWithCode(ApiErrors.ValidationError, "At least one tag is required.");

        await AttachAsync(asset, dto.Tags, ct);
        await db.SaveChangesAsync(ct);
        return Map(asset);
    }

    /// <summary>Removes the link of an asset with a tag (the tag itself remains).</summary>
    [HttpDelete("{id:int}/tags/{tagId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DetachTag(int id, int tagId, CancellationToken ct = default)
    {
        var link = await db.MediaTagLinks.FirstOrDefaultAsync(l => l.MediaAssetId == id && l.InterestTagId == tagId, ct);
        if (link is null) return NotFound();
        db.MediaTagLinks.Remove(link);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---- Helpers -------------------------------------------------------------------------------------

    /// <summary>Attaches the named tags (expects loaded <c>TagLinks</c>); does not save.</summary>
    private async Task AttachAsync(MediaAsset asset, List<string>? names, CancellationToken ct)
    {
        if (names is null) return;
        var already = asset.TagLinks.Where(l => l.InterestTag is not null).Select(l => l.InterestTag!.Slug).ToHashSet(StringComparer.Ordinal);
        foreach (var tag in await tags.EnsureManyAsync(names, ct: ct))
            if (already.Add(tag.Slug))
                asset.TagLinks.Add(new MediaTagLink { MediaAsset = asset, InterestTag = tag });
    }

    /// <summary>Makes a generated base key unique by appending _2, _3 … on collision.</summary>
    private async Task<string> UniqueKeyAsync(string baseKey, CancellationToken ct)
    {
        var key = string.IsNullOrWhiteSpace(baseKey) ? "medium" : baseKey;
        if (!await db.MediaAssets.AnyAsync(a => a.Key == key, ct)) return key;
        for (var n = 2; ; n++)
        {
            var candidate = $"{key}_{n}";
            if (!await db.MediaAssets.AnyAsync(a => a.Key == candidate, ct)) return candidate;
        }
    }

    /// <summary>Shared variant validation for creation (here) and adding later (variants controller).</summary>
    internal static string? Validate(string? url, int? width, int? height, string? format)
    {
        if (string.IsNullOrWhiteSpace(url)) return "Variant url is required.";
        if (width is <= 0 || height is <= 0) return "Variant width and height must be greater than zero.";
        if (format is not null && format.Trim().Length == 0) return "Variant format must not be empty.";
        return null;
    }

    internal static MediaVariant NewVariant(CreateMediaVariantDto dto) => new()
    {
        Purpose = dto.Purpose,
        Url = dto.Url.Trim(),
        Width = dto.Width,
        Height = dto.Height,
        Format = dto.Format.Trim().ToLowerInvariant(),
        Bytes = dto.Bytes,
    };

    private static string? Trimmed(string? value) => value?.Trim() is { Length: > 0 } v ? v : null;

    /// <summary>Multipart has no lists like JSON – tags arrive there as a single comma-separated line.</summary>
    private static List<string> SplitTags(string? tags) =>
        [.. (tags ?? "").Split(',').Select(t => t.Trim()).Where(t => t.Length > 0)];
}
