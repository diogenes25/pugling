using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Atomic vocabulary store ("single source of truth"). Sentences and exercises later reference these entries
/// via their <c>Key</c>. The store is child-neutral (shared catalog, adult only) and built
/// so that the data work can be offloaded to an agent: create "simply" (word only),
/// specifically filter unfinished vocabulary entries, mass-create/complete via batch/lookup, and
/// navigate the form family (go→went→gone) via the base-form edge.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/vocabulary")]
[Tags("Creator – Vocabulary Store")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public class VocabularyStoreController(PuglingDbContext db) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    static VocabularyResponse Map(Vocabulary v) =>
        new(v.Id, v.Key, v.Version, v.SourceLanguage, v.TargetLanguage, v.Word, v.Translation,
            v.PartOfSpeech, v.Noun, v.Verb, v.BaseFormId, v.BaseForm?.Key, v.BaseFormRelation,
            v.PronunciationAudioUrl,
            v.TagLinks.Select(l => l.VocabTag!.Name).OrderBy(n => n, StringComparer.Ordinal).ToList(),
            v.CreatedAt);

    /// <summary>Base query with the navigations needed for <see cref="Map"/> (base form + tags).</summary>
    private IQueryable<Vocabulary> WithGraph(IQueryable<Vocabulary> q) =>
        q.Include(v => v.BaseForm).Include(v => v.TagLinks).ThenInclude(l => l.VocabTag);

    /// <summary>
    /// List of vocabulary entries, optionally filtered. The completeness filters map the three agent criteria:
    /// <paramref name="untranslated"/> (not translated), <paramref name="incomplete"/> (incomplete),
    /// and <paramref name="linked"/> (with/without base-form link). <paramref name="tag"/> lets you
    /// filter by tags (e.g. "chapter 5"); multiple tags are OR-combined by default,
    /// <paramref name="matchAll"/> switches to AND. The total count (before paging) is in the header
    /// <c>X-Total-Count</c>.
    /// </summary>
    /// <param name="search">Full text in word/translation/key (substring).</param>
    /// <param name="word">Substring filter on the word alone (source language).</param>
    /// <param name="translation">Substring filter on the translation alone (target language).</param>
    /// <param name="partOfSpeech">Exact part of speech.</param>
    /// <param name="untranslated">true = only entries without translation.</param>
    /// <param name="incomplete">true = only incomplete entries (no translation / part of speech "Other" / missing noun/verb details).</param>
    /// <param name="linked">true = only linked (base form set), false = only unlinked.</param>
    /// <param name="baseFormsOnly">true = only base forms (no inflected forms); clear alias for authoring, functionally like <c>linked=false</c>.</param>
    /// <param name="sourceLanguage">Filter on the source language.</param>
    /// <param name="targetLanguage">Filter on the target language.</param>
    /// <param name="tag">One or more tag names (repeatable).</param>
    /// <param name="matchAll">With multiple tags: true = all (AND), false = any (OR, default).</param>
    /// <param name="sort">Sort column: <c>key</c> (default), <c>word</c>, <c>translation</c>, <c>pos</c>, <c>created</c>.
    /// Short form <c>-word</c> = descending.</param>
    /// <param name="dir"><c>asc</c> (default) or <c>desc</c>; takes precedence over a <c>-</c> prefix in <paramref name="sort"/>.</param>
    /// <param name="skip">Number of entries to skip (paging).</param>
    /// <param name="take">Maximum number of hits (1..500).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<IEnumerable<VocabularyResponse>> List(
        [FromQuery] string? search = null,
        [FromQuery] string? word = null,
        [FromQuery] string? translation = null,
        [FromQuery] PartOfSpeech? partOfSpeech = null,
        [FromQuery] bool? untranslated = null,
        [FromQuery] bool? incomplete = null,
        [FromQuery] bool? linked = null,
        [FromQuery] bool? baseFormsOnly = null,
        [FromQuery] string? sourceLanguage = null,
        [FromQuery] string? targetLanguage = null,
        [FromQuery] string[]? tag = null,
        [FromQuery] bool matchAll = false,
        [FromQuery] string? sort = null,
        [FromQuery] string? dir = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        var query = db.Vocabulary.AsNoTracking().AsQueryable();

        if (partOfSpeech is not null)
            query = query.Where(v => v.PartOfSpeech == partOfSpeech);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(v => v.Word.Contains(search)
                || v.Translation.Contains(search) || v.Key.Contains(search));
        if (!string.IsNullOrWhiteSpace(word))
            query = query.Where(v => v.Word.Contains(word));
        if (!string.IsNullOrWhiteSpace(translation))
            query = query.Where(v => v.Translation.Contains(translation));
        if (untranslated is true)
            query = query.Where(v => v.Translation == "");
        if (incomplete is true)
            query = query.Where(v => v.Translation == "" || v.PartOfSpeech == PartOfSpeech.Other
                || (v.PartOfSpeech == PartOfSpeech.Noun && v.Noun == null)
                || (v.PartOfSpeech == PartOfSpeech.Verb && v.Verb == null));
        if (linked is bool wantLinked)
            query = wantLinked ? query.Where(v => v.BaseFormId != null) : query.Where(v => v.BaseFormId == null);
        if (baseFormsOnly is true)
            query = query.Where(v => v.BaseFormId == null);
        if (!string.IsNullOrWhiteSpace(sourceLanguage))
            query = query.Where(v => v.SourceLanguage == sourceLanguage);
        if (!string.IsNullOrWhiteSpace(targetLanguage))
            query = query.Where(v => v.TargetLanguage == targetLanguage);

        var tags = tag?.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.Ordinal).ToList();
        if (tags is { Count: > 0 })
        {
            if (matchAll)
                foreach (var name in tags)
                    query = query.Where(v => v.TagLinks.Any(l => l.VocabTag!.Name == name));
            else
                query = query.Where(v => v.TagLinks.Any(l => tags.Contains(l.VocabTag!.Name)));
        }

        var items = await WithGraph(ApplySort(query, SortingExtensions.ParseSort(sort, dir))).ToPagedListAsync(Response, skip, take, ct);
        return items.Select(Map);
    }

    /// <summary>
    /// Applies the sorting allowed via whitelist; every variant ends with <c>Id</c> as a tiebreaker,
    /// so the paging window stays deterministic. Unknown/empty keys → default by <c>Key</c>.
    /// </summary>
    private static IOrderedQueryable<Vocabulary> ApplySort(IQueryable<Vocabulary> q, (string? Key, bool Desc) sort) =>
        (sort.Key?.ToLowerInvariant(), sort.Desc) switch
        {
            ("word", false) => q.OrderBy(v => v.Word).ThenBy(v => v.Id),
            ("word", true) => q.OrderByDescending(v => v.Word).ThenBy(v => v.Id),
            ("translation", false) => q.OrderBy(v => v.Translation).ThenBy(v => v.Id),
            ("translation", true) => q.OrderByDescending(v => v.Translation).ThenBy(v => v.Id),
            ("pos", false) => q.OrderBy(v => v.PartOfSpeech).ThenBy(v => v.Id),
            ("pos", true) => q.OrderByDescending(v => v.PartOfSpeech).ThenBy(v => v.Id),
            ("created", false) => q.OrderBy(v => v.CreatedAt).ThenBy(v => v.Id),
            ("created", true) => q.OrderByDescending(v => v.CreatedAt).ThenBy(v => v.Id),
            (_, true) => q.OrderByDescending(v => v.Key).ThenBy(v => v.Id),
            _ => q.OrderBy(v => v.Key).ThenBy(v => v.Id),
        };

    /// <summary>A vocabulary entry by numeric id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VocabularyResponse>> Get(int id, CancellationToken ct = default)
    {
        var v = await WithGraph(db.Vocabulary.AsNoTracking()).FirstOrDefaultAsync(x => x.Id == id, ct);
        return v is null ? NotFound() : Map(v);
    }

    /// <summary>A vocabulary entry by stable key (reference slug).</summary>
    [HttpGet("by-key/{key}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VocabularyResponse>> GetByKey(string key, CancellationToken ct = default)
    {
        var v = await WithGraph(db.Vocabulary.AsNoTracking()).FirstOrDefaultAsync(x => x.Key == key, ct);
        return v is null ? NotFound() : Map(v);
    }

    /// <summary>
    /// All forms of a base-form family (e.g. go → went → gone). Starting from an arbitrary form,
    /// the base form is determined (<c>BaseFormId ?? Id</c>) and delivered along with all forms referencing it –
    /// each with its <c>BaseFormRelation</c> label. The base form comes first.
    /// </summary>
    [HttpGet("{id:int}/forms")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<VocabularyResponse>>> Forms(int id, CancellationToken ct = default)
    {
        var self = await db.Vocabulary.AsNoTracking().Select(v => new { v.Id, v.BaseFormId })
            .FirstOrDefaultAsync(v => v.Id == id, ct);
        if (self is null) return NotFound();

        var baseId = self.BaseFormId ?? self.Id;
        var family = await WithGraph(db.Vocabulary.AsNoTracking())
            .Where(v => v.Id == baseId || v.BaseFormId == baseId)
            .ToListAsync(ct);

        // Grundform zuerst, danach die flektierten Formen stabil nach Key.
        return family
            .OrderByDescending(v => v.Id == baseId).ThenBy(v => v.Key, StringComparer.Ordinal)
            .Select(Map).ToList();
    }

    /// <summary>Creates a vocabulary entry. If the key is missing, a unique one is generated; BaseFormKey (if set) must exist.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VocabularyResponse>> Create(CreateVocabularyDto dto, CancellationToken ct = default)
    {
        var outcome = await CreateCoreAsync(dto, ct);
        return outcome.Kind switch
        {
            CreateKind.Created => CreatedAtAction(nameof(Get), new { id = outcome.Vocab!.Id }, Map(outcome.Vocab)),
            CreateKind.Conflict => this.ProblemWithCode(ApiErrors.DuplicateKey, outcome.Error),
            _ => this.ProblemWithCode(ApiErrors.ValidationError, outcome.Error),
        };
    }

    private enum CreateKind { Created, Conflict, Error }
    private record CreateOutcome(CreateKind Kind, Vocabulary? Vocab, string? Key, string? Error);

    /// <summary>
    /// Shared creation logic for single POST and batch. Creates and loads base form + tags for the response.
    /// An already taken, explicitly set key returns <see cref="CreateKind.Conflict"/> (the caller
    /// decides: 409 individually or idempotent "existing" in the batch).
    /// </summary>
    private async Task<CreateOutcome> CreateCoreAsync(CreateVocabularyDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Word))
            return new(CreateKind.Error, null, null, "Word is required.");

        string key;
        if (string.IsNullOrWhiteSpace(dto.Key))
        {
            key = await UniqueKeyAsync(VocabKey.Generate(dto.SourceLanguage, dto.Word, dto.TargetLanguage, dto.Translation ?? ""), ct);
        }
        else
        {
            key = dto.Key.Trim();
            if (await db.Vocabulary.AnyAsync(v => v.Key == key, ct))
                return new(CreateKind.Conflict, null, key, $"Key '{key}' already exists.");
        }

        int? baseFormId = null;
        if (!string.IsNullOrWhiteSpace(dto.BaseFormKey))
        {
            baseFormId = await db.Vocabulary.Where(v => v.Key == dto.BaseFormKey)
                .Select(v => (int?)v.Id).FirstOrDefaultAsync(ct);
            if (baseFormId is null) return new(CreateKind.Error, null, key, $"BaseFormKey '{dto.BaseFormKey}' not found.");
        }

        var vocab = new Vocabulary
        {
            Key = key,
            Version = string.IsNullOrWhiteSpace(dto.Version) ? "1.0" : dto.Version,
            SourceLanguage = dto.SourceLanguage,
            TargetLanguage = dto.TargetLanguage,
            Word = dto.Word,
            Translation = dto.Translation ?? "",
            PartOfSpeech = dto.PartOfSpeech ?? Contracts.PartOfSpeech.Other,
            Noun = dto.Noun,
            Verb = dto.Verb,
            BaseFormId = baseFormId,
            BaseFormRelation = string.IsNullOrWhiteSpace(dto.BaseFormRelation) ? null : dto.BaseFormRelation.Trim(),
            PronunciationAudioUrl = dto.PronunciationAudioUrl,
        };
        db.Vocabulary.Add(vocab);
        await ApplyTagsAsync(vocab, dto.Tags, ct);
        await db.SaveChangesAsync(ct);

        await LoadGraphAsync(vocab, ct);
        return new(CreateKind.Created, vocab, key, null);
    }

    /// <summary>Makes a generated base key unique by appending _2, _3 … on collision.</summary>
    private async Task<string> UniqueKeyAsync(string baseKey, CancellationToken ct)
    {
        var key = string.IsNullOrWhiteSpace(baseKey) ? "vokabel" : baseKey;
        if (!await db.Vocabulary.AnyAsync(v => v.Key == key, ct)) return key;
        for (var n = 2; ; n++)
        {
            var candidate = $"{key}_{n}";
            if (!await db.Vocabulary.AnyAsync(v => v.Key == candidate, ct)) return candidate;
        }
    }

    /// <summary>Changes a vocabulary entry (partial).</summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VocabularyResponse>> Update(int id, UpdateVocabularyDto dto, CancellationToken ct = default)
    {
        var (status, vocab, error) = await UpdateCoreAsync(id, dto, ct);
        return status switch
        {
            UpdateStatus.Ok => Map(vocab!),
            UpdateStatus.NotFound => NotFound(),
            _ => this.ProblemWithCode(ApiErrors.ValidationError, error),
        };
    }

    private enum UpdateStatus { Ok, NotFound, Error }

    /// <summary>Shared update logic for single PATCH and batch (loads base form + tags for the response).</summary>
    private async Task<(UpdateStatus Status, Vocabulary? Vocab, string? Error)> UpdateCoreAsync(int id, UpdateVocabularyDto dto, CancellationToken ct)
    {
        var vocab = await db.Vocabulary.Include(v => v.TagLinks).ThenInclude(l => l.VocabTag)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
        if (vocab is null) return (UpdateStatus.NotFound, null, null);

        if (dto.Version is not null) vocab.Version = dto.Version;
        if (dto.SourceLanguage is not null) vocab.SourceLanguage = dto.SourceLanguage;
        if (dto.TargetLanguage is not null) vocab.TargetLanguage = dto.TargetLanguage;
        if (dto.Word is not null) vocab.Word = dto.Word;
        if (dto.Translation is not null) vocab.Translation = dto.Translation;
        if (dto.PartOfSpeech is not null) vocab.PartOfSpeech = dto.PartOfSpeech.Value;
        if (dto.Noun is not null) vocab.Noun = dto.Noun;
        if (dto.Verb is not null) vocab.Verb = dto.Verb;
        if (dto.PronunciationAudioUrl is not null) vocab.PronunciationAudioUrl = dto.PronunciationAudioUrl;

        if (dto.BaseFormKey is not null)
        {
            if (dto.BaseFormKey.Length == 0)
            {
                vocab.BaseFormId = null;
                vocab.BaseFormRelation = null;
            }
            else
            {
                if (dto.BaseFormKey == vocab.Key) return (UpdateStatus.Error, null, "A vocabulary item cannot be its own base form.");
                var baseFormId = await db.Vocabulary.Where(v => v.Key == dto.BaseFormKey)
                    .Select(v => (int?)v.Id).FirstOrDefaultAsync(ct);
                if (baseFormId is null) return (UpdateStatus.Error, null, $"BaseFormKey '{dto.BaseFormKey}' not found.");
                vocab.BaseFormId = baseFormId;
            }
        }
        if (dto.BaseFormRelation is not null)
            vocab.BaseFormRelation = dto.BaseFormRelation.Trim() is { Length: > 0 } r ? r : null;

        await ApplyTagsAsync(vocab, dto.Tags, ct);
        await db.SaveChangesAsync(ct);
        await LoadGraphAsync(vocab, ct);
        return (UpdateStatus.Ok, vocab, null);
    }

    /// <summary>Deletes a vocabulary entry. Not possible while it is the base form of other entries.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var vocab = await db.Vocabulary.FindAsync([id], ct);
        if (vocab is null) return NotFound();

        if (await db.Vocabulary.AnyAsync(v => v.BaseFormId == id, ct))
            return this.ProblemWithCode(ApiErrors.VocabularyInUse, "The vocabulary item is the base form of other entries and cannot be deleted.");

        // Verhindert stille „(Vokabel fehlt)"-Platzhalter in Übungen, die die Vokabel referenzieren.
        if ((await ReferencingExercisesAsync(vocab.Id, vocab.Key, ct)).Count > 0)
            return this.ProblemWithCode(ApiErrors.VocabularyInUse, "The vocabulary item is used in one or more exercises and cannot be deleted.");

        db.Vocabulary.Remove(vocab);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Which exercises reference the vocabulary entry – vocabulary exercises via their <see cref="ExerciseItem"/> rows (by id),
    /// cloze texts via <see cref="Gap.VocabKey"/> in the config. Basis for the delete protection and the author view.
    /// </summary>
    [HttpGet("{id:int}/usage")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<VocabUsage>>> Usage(int id, CancellationToken ct = default)
    {
        var key = await db.Vocabulary.Where(v => v.Id == id).Select(v => v.Key).FirstOrDefaultAsync(ct);
        if (key is null) return NotFound();
        return await ReferencingExercisesAsync(id, key, ct);
    }

    /// <summary>
    /// Finds referencing exercises: vocabulary exercises via the <see cref="ExerciseItem"/> table (by vocabulary id),
    /// cloze texts still by key in the ConfigJson (SQL pre-filter + precise JSON check).
    /// </summary>
    private async Task<List<VocabUsage>> ReferencingExercisesAsync(int id, string key, CancellationToken ct)
    {
        // Vokabelübungen: Referenz lebt in der Item-Tabelle (nicht mehr in der ConfigJson).
        var viaItems = await db.ExerciseItems.AsNoTracking()
            .Where(i => i.VocabularyId == id)
            .Select(i => new
            {
                i.Exercise!.Id,
                i.Exercise.Title,
                i.Exercise.Type,
                i.Exercise.ChapterId,
                SubjectId = i.Exercise.Chapter!.SubjectId,
            })
            .Distinct()
            .ToListAsync(ct);
        var used = viaItems
            .Select(e => new VocabUsage(e.Id, e.Title, e.Type.ToString(), e.ChapterId, e.SubjectId))
            .ToList();

        // Lückentexte: Key-Referenz in der ConfigJson.
        var clozeCandidates = await db.Exercises.AsNoTracking().Include(e => e.Chapter)
            .Where(e => e.Type == ExerciseTypeKeys.Cloze && e.ConfigJson.Contains(key))
            .ToListAsync(ct);
        foreach (var e in clozeCandidates)
        {
            var referenced = JsonSerializer.Deserialize<ClozeConfig>(e.ConfigJson, JsonOptions)?.Gaps.Any(g => g.VocabKey == key) ?? false;
            if (referenced)
                used.Add(new VocabUsage(e.Id, e.Title, e.Type.ToString(), e.ChapterId, e.Chapter?.SubjectId ?? 0));
        }
        return used;
    }

    // ---- Agenten-Primitive: Lookup (Dedup) + Batch-Anlegen/-Nachtragen ------------------------------

    /// <summary>
    /// Existence check for the text→vocabulary extraction (dedup before the agent creates). The comparison runs
    /// case-insensitively over <c>Word</c>, optionally filtered by language pair. Additionally, it can be checked
    /// which <paramref name="request"/>.Keys already exist (e.g. to validate exercise refs).
    /// </summary>
    [HttpPost("lookup")]
    public async Task<ActionResult<LookupResponse>> Lookup(LookupRequest request, CancellationToken ct = default)
    {
        var words = (request.Words ?? [])
            .Select(w => w.Trim()).Where(w => w.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var results = new List<LookupResult>();
        if (words.Count > 0)
        {
            // Kein ToLower(): die Spalte trägt die Collation NOCASE, der Vergleich ist also von sich aus
            // groß-/kleinschreibungsunabhängig – und *nur* ohne den Ausdruck um die Spalte greift der
            // Index auf Word. Vorher war das ein vollständiger Tabellendurchlauf über den größten Store,
            // im heißesten Creator-Pfad (Dubletten-Lookup beim Anlegen).
            var q = db.Vocabulary.AsNoTracking().Where(v => words.Contains(v.Word));
            if (!string.IsNullOrWhiteSpace(request.SourceLanguage))
                q = q.Where(v => v.SourceLanguage == request.SourceLanguage);
            if (!string.IsNullOrWhiteSpace(request.TargetLanguage))
                q = q.Where(v => v.TargetLanguage == request.TargetLanguage);

            var matches = await WithGraph(q).ToListAsync(ct);
            results = words.Select(w =>
            {
                var hits = matches.Where(m => string.Equals(m.Word, w, StringComparison.OrdinalIgnoreCase))
                    .Select(Map).ToList();
                return new LookupResult(w, hits.Count > 0, hits);
            }).ToList();
        }

        var keys = (request.Keys ?? []).Select(k => k.Trim()).Where(k => k.Length > 0).Distinct(StringComparer.Ordinal).ToList();
        var existingKeys = keys.Count == 0
            ? []
            : await db.Vocabulary.AsNoTracking().Where(v => keys.Contains(v.Key)).Select(v => v.Key).ToListAsync(ct);

        return new LookupResponse(results, existingKeys);
    }

    /// <summary>
    /// Creates many vocabulary entries in one call – idempotent: an already existing, explicitly set
    /// key returns status <c>existing</c> (no error), so the same batch can be safely repeated.
    /// Without a key, the server generates a unique one (status <c>created</c>). Language logic
    /// (tokenizing/translating) is up to the caller – the API only manages the data.
    /// </summary>
    [HttpPost("batch")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<BatchItemResult>>> CreateBatch(List<CreateVocabularyDto> items, CancellationToken ct = default)
    {
        if (items is not { Count: > 0 }) return this.ProblemWithCode(ApiErrors.ValidationError, "At least one entry is required.");

        var results = new List<BatchItemResult>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            var outcome = await CreateCoreAsync(items[i], ct);
            switch (outcome.Kind)
            {
                case CreateKind.Created:
                    results.Add(new(i, "created", outcome.Vocab!.Id, outcome.Vocab.Key, null));
                    break;
                case CreateKind.Conflict:
                    // Idempotent: der Eintrag mit diesem Key existiert bereits – zurückmelden statt Fehler.
                    var existingId = await db.Vocabulary.Where(v => v.Key == outcome.Key).Select(v => (int?)v.Id).FirstOrDefaultAsync(ct);
                    results.Add(new(i, "existing", existingId, outcome.Key, null));
                    break;
                default:
                    results.Add(new(i, "error", null, outcome.Key, outcome.Error));
                    break;
            }
        }
        return results;
    }

    /// <summary>Adds fields to many vocabulary entries in one call (same merge semantics as single PATCH).</summary>
    [HttpPatch("batch")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<BatchItemResult>>> UpdateBatch(List<BatchUpdateItem> items, CancellationToken ct = default)
    {
        if (items is not { Count: > 0 }) return this.ProblemWithCode(ApiErrors.ValidationError, "At least one entry is required.");

        var results = new List<BatchItemResult>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            var it = items[i];
            var (status, vocab, error) = await UpdateCoreAsync(it.Id, new UpdateVocabularyDto(
                it.Version, it.SourceLanguage, it.TargetLanguage, it.Word, it.Translation, it.PartOfSpeech,
                it.Noun, it.Verb, it.BaseFormKey, it.BaseFormRelation, it.PronunciationAudioUrl, it.Tags), ct);
            results.Add(status switch
            {
                UpdateStatus.Ok => new(i, "updated", vocab!.Id, vocab.Key, null),
                UpdateStatus.NotFound => new(i, "not-found", it.Id, null, $"Vocabulary item {it.Id} not found."),
                _ => new(i, "error", it.Id, null, error),
            });
        }
        return results;
    }

    // ---- Helfer -------------------------------------------------------------------------------------

    /// <summary>Loads base form + tags of a tracked vocabulary entry for the response projection.</summary>
    private async Task LoadGraphAsync(Vocabulary vocab, CancellationToken ct)
    {
        await db.Entry(vocab).Reference(v => v.BaseForm).LoadAsync(ct);
        await db.Entry(vocab).Collection(v => v.TagLinks).Query().Include(l => l.VocabTag).LoadAsync(ct);
    }

    /// <summary>
    /// Links the vocabulary entry with the named tags (create-if-missing, exact name, additive – already
    /// linked ones are skipped). Expects the vocabulary entry's existing <c>TagLinks</c> to be loaded.
    /// </summary>
    private async Task ApplyTagsAsync(Vocabulary vocab, List<string>? tagNames, CancellationToken ct)
    {
        if (tagNames is null) return;
        var names = tagNames.Select(n => n.Trim()).Where(n => n.Length > 0).Distinct(StringComparer.Ordinal).ToList();
        if (names.Count == 0) return;

        var existing = await db.VocabTags.Where(t => names.Contains(t.Name)).ToListAsync(ct);
        var byName = existing.ToDictionary(t => t.Name, StringComparer.Ordinal);
        var already = vocab.TagLinks.Where(l => l.VocabTag is not null).Select(l => l.VocabTag!.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var name in names)
        {
            if (already.Contains(name)) continue;
            if (!byName.TryGetValue(name, out var tag))
            {
                tag = new VocabTag { Name = name };
                db.VocabTags.Add(tag);
                byName[name] = tag;
            }
            vocab.TagLinks.Add(new VocabTagLink { VocabTag = tag, Vocabulary = vocab });
        }
    }
}
