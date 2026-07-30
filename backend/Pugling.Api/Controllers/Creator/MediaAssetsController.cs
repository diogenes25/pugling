using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Medien-Store: <b>ein Motiv, viele Bilder</b>. Ein Asset ist nicht „das Bild zu <i>laufen</i>", sondern
/// eine konkrete <b>Darstellung</b> – das laufende Einhorn im Comic-Stil, Flash, die joggende Person als
/// Foto. Welche davon ein Kind später sieht, entscheidet sein Profil; hier wird nur der Vorrat gepflegt.
/// <para>
/// Zwei Achsen bleiben strikt getrennt: die <b>inhaltliche</b> (dieses Asset, mit Stil-Tags und Eignung)
/// und die <b>technische</b> (Auflösungen desselben Assets, siehe <see cref="MediaVariantsController"/>).
/// Bytes liegen nie in der DB – nur URLs, wie bei der Aussprache-Audioquelle des Vokabel-Stores.
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

    /// <summary>Basis-Query mit den für <see cref="Map"/> nötigen Navigationen (Varianten + Tags).</summary>
    private static IQueryable<MediaAsset> WithGraph(IQueryable<MediaAsset> q) =>
        q.Include(a => a.Variants).Include(a => a.TagLinks).ThenInclude(l => l.InterestTag);

    /// <summary>
    /// Liste der Assets, optional gefiltert. <paramref name="maxRating"/> ist der Filter, der die
    /// Zielgruppen-Trennung sichtbar macht: er liefert nur, was für die genannte Stufe freigegeben ist –
    /// derselbe Schnitt, den die spätere automatische Auswahl je Kind hart anwendet. Die Gesamtzahl
    /// (vor Paging) steht im Header <c>X-Total-Count</c>.
    /// </summary>
    /// <param name="search">Teilstring in Beschreibung oder Key.</param>
    /// <param name="tag">Ein oder mehrere Tag-Slugs (wiederholbar).</param>
    /// <param name="matchAll">Bei mehreren Tags: true = alle (UND), false = beliebiger (ODER, Default).</param>
    /// <param name="kind">Nur Assets dieser Medienart.</param>
    /// <param name="maxRating">Höchste zulässige Eignungsstufe (z. B. <c>Everyone</c> für die Kindersicht).</param>
    /// <param name="origin">Nur Assets dieser Herkunft (z. B. nur KI-generierte).</param>
    /// <param name="withoutVariants">true = nur Assets ohne jede Datei (unfertig, wie der Vokabel-Filter <c>incomplete</c>).</param>
    /// <param name="untagged">true = nur Assets ohne Schlagworte (für die Auswahl praktisch unsichtbar).</param>
    /// <param name="sort">Sortierspalte: <c>key</c> (Default), <c>description</c>, <c>rating</c>, <c>created</c>. Kurzform <c>-created</c> = absteigend.</param>
    /// <param name="dir"><c>asc</c> (Default) oder <c>desc</c>; hat Vorrang vor einem <c>-</c>-Präfix in <paramref name="sort"/>.</param>
    /// <param name="skip">Anzahl zu überspringender Einträge (Paging).</param>
    /// <param name="take">Maximale Trefferzahl (1..500).</param>
    /// <param name="ct">Abbruch-Token.</param>
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
        // Ordnender Vergleich – nur möglich, weil das Rating als int persistiert ist (siehe DbContext).
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
    /// Wendet die per Whitelist erlaubte Sortierung an; jede Variante endet mit <c>Id</c> als Tiebreaker,
    /// damit das Paging-Fenster deterministisch bleibt. Unbekannte/leere Keys → Standard nach <c>Key</c>.
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

    /// <summary>Ein Asset per numerischer Id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaAssetResponse>> Get(int id, CancellationToken ct = default)
    {
        var asset = await WithGraph(db.MediaAssets.AsNoTracking()).FirstOrDefaultAsync(a => a.Id == id, ct);
        return asset is null ? NotFound() : Map(asset);
    }

    /// <summary>Ein Asset per stabilem Key (Referenz-Slug).</summary>
    [HttpGet("by-key/{key}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaAssetResponse>> GetByKey(string key, CancellationToken ct = default)
    {
        var asset = await WithGraph(db.MediaAssets.AsNoTracking()).FirstOrDefaultAsync(a => a.Key == key, ct);
        return asset is null ? NotFound() : Map(asset);
    }

    /// <summary>
    /// Legt ein Asset an. Fehlt der Key, wird ein eindeutiger aus der Beschreibung erzeugt. Tags (Slugs)
    /// und Varianten dürfen gleich mitkommen – ein Agent legt so eine fertige Darstellung in einem Zug an.
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

        // Doppelte (Zweck, Format) würden erst am Unique-Index scheitern – vorher als klarer 409 melden.
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
    /// Legt ein Asset aus einer <b>hochgeladenen Datei</b> an: der Server dekodiert sie und erzeugt die
    /// Auflösungen selbst (Thumb/Card/Full, seitenverhältnis-erhaltend, WebP) plus eine Platzhalterfarbe.
    /// Das ist der bequeme Weg gegenüber dem URL-Endpunkt – der Creator braucht weder eine Bildquelle im
    /// Netz noch ein Grafikprogramm zum Skalieren.
    /// <para>
    /// Kein <see cref="MediaPurpose.Hero"/>: das breite Aufmacherformat verlangt Beschnitt, und wo ein
    /// Motiv beschnitten werden darf, kann nur ein Mensch entscheiden.
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
            // Nicht dekodierbar (falscher Typ, kaputte Datei) – Nutzerfehler, kein Serverfehler.
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

        // Erst speichern: der Ablage-Ordner heißt nach der Id. Die ist – anders als der Key – garantiert
        // dateisystem-sicher und ändert sich nie, auch wenn der Key später einmal umbenennbar würde.
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

    /// <summary>Ändert ein Asset (partiell). Tags werden <b>ergänzt</b>, nicht ersetzt (Lösen per DELETE).</summary>
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
    /// Wo dieses Bild zugeordnet ist (Vokabeln, Übungs-Items, Übungen) – die Rückrichtung zur Zuordnung.
    /// Bewusst als eigener Endpunkt statt als Löschsperre: anders als eine fehlende Vokabel hinterlässt
    /// ein fehlendes Bild keinen Platzhalter, es schrumpft nur die Auswahl. Der Creator soll vor dem
    /// Löschen <i>sehen</i> können, was er verliert – aufgehalten wird er nicht.
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
    /// Löscht ein Asset samt seiner Varianten, Tag-Verknüpfungen und Zuordnungen (Cascade). Nicht
    /// gesperrt, wenn es in Gebrauch ist – siehe <see cref="Usage"/> für die Begründung.
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
        // Erst nach dem erfolgreichen Löschen in der DB die Dateien wegräumen: bricht die DB ab, sind die
        // Dateien noch da (verwaiste Datei ist harmlos, ein Asset ohne Datei wäre eine kaputte Karte).
        // Assets, die nur per URL eingetragen wurden, haben keinen eigenen Ordner – das Löschen läuft leer.
        await storage.DeleteFolderAsync(asset.Id.ToString(), ct);
        return NoContent();
    }

    /// <summary>
    /// Verknüpft ein Asset mit Schlagworten der geteilten Taxonomie (create-if-missing, bereits
    /// verknüpfte werden übersprungen). Liefert das Asset mit seinen aktuellen Tags.
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

    /// <summary>Löst die Verknüpfung eines Assets mit einem Schlagwort (der Tag selbst bleibt bestehen).</summary>
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

    // ---- Helfer -------------------------------------------------------------------------------------

    /// <summary>Hängt die genannten Schlagworte an (erwartet geladene <c>TagLinks</c>); speichert nicht.</summary>
    private async Task AttachAsync(MediaAsset asset, List<string>? names, CancellationToken ct)
    {
        if (names is null) return;
        var already = asset.TagLinks.Where(l => l.InterestTag is not null).Select(l => l.InterestTag!.Slug).ToHashSet(StringComparer.Ordinal);
        foreach (var tag in await tags.EnsureManyAsync(names, ct: ct))
            if (already.Add(tag.Slug))
                asset.TagLinks.Add(new MediaTagLink { MediaAsset = asset, InterestTag = tag });
    }

    /// <summary>Macht einen generierten Basiskey eindeutig, indem bei Kollision _2, _3 … angehängt wird.</summary>
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

    /// <summary>Gemeinsame Variantenprüfung für Anlegen (hier) und Nachreichen (Varianten-Controller).</summary>
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

    /// <summary>Multipart kennt keine Listen wie JSON – Schlagworte kommen dort als eine kommagetrennte Zeile.</summary>
    private static List<string> SplitTags(string? tags) =>
        [.. (tags ?? "").Split(',').Select(t => t.Trim()).Where(t => t.Length > 0)];
}
