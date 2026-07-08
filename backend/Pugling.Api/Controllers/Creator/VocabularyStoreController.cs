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
/// Atomarer Vokabel-Store ("Single Source of Truth"). Sätze und Übungen referenzieren diese Einträge
/// später über ihren <c>Key</c>. Der Store ist kindneutral (gemeinsamer Katalog, nur Vater) und so
/// gebaut, dass die Datenarbeit an einen Agenten auslagerbar ist: „einfach" (nur Word) anlegen,
/// unfertige Vokabeln gezielt filtern, per Batch/Lookup massenhaft anlegen/vervollständigen und die
/// Formen-Familie (go→went→gone) über die Grundform-Kante navigieren.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/vocabulary")]
[Tags("Learn – Vocabulary Store")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
public class VocabularyStoreController(PuglingDbContext db) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public record VocabularyResponse(int Id, string Key, string Version, string SourceLanguage,
        string TargetLanguage, string Word, string Translation, PartOfSpeech PartOfSpeech,
        NounInfo? Noun, VerbInfo? Verb, int? BaseFormId, string? BaseFormKey, string? BaseFormRelation,
        string? PronunciationAudioUrl, IReadOnlyList<string> Tags, DateTime CreatedAt);

    static VocabularyResponse Map(Vocabulary v) =>
        new(v.Id, v.Key, v.Version, v.SourceLanguage, v.TargetLanguage, v.Word, v.Translation,
            v.PartOfSpeech, v.Noun, v.Verb, v.BaseFormId, v.BaseForm?.Key, v.BaseFormRelation,
            v.PronunciationAudioUrl,
            v.TagLinks.Select(l => l.VocabTag!.Name).OrderBy(n => n, StringComparer.Ordinal).ToList(),
            v.CreatedAt);

    /// <summary>Basis-Query mit den für <see cref="Map"/> nötigen Navigationen (Grundform + Tags).</summary>
    private IQueryable<Vocabulary> WithGraph(IQueryable<Vocabulary> q) =>
        q.Include(v => v.BaseForm).Include(v => v.TagLinks).ThenInclude(l => l.VocabTag);

    /// <summary>
    /// Liste der Vokabeln, optional gefiltert. Die Vollständigkeits-Filter bilden die drei Agenten-Kriterien
    /// ab: <paramref name="untranslated"/> (nicht übersetzt), <paramref name="incomplete"/> (unvollständig)
    /// und <paramref name="linked"/> (mit/ohne Grundform-Verknüpfung). Über <paramref name="tag"/> lässt sich
    /// nach Schlagworten filtern (z. B. „Kapitel 5"); mehrere Tags sind per Default ODER-verknüpft,
    /// <paramref name="matchAll"/> schaltet auf UND. Die Gesamtzahl (vor Paging) steht im Header
    /// <c>X-Total-Count</c>.
    /// </summary>
    /// <param name="search">Volltext in Word/Translation/Key (Teilstring).</param>
    /// <param name="word">Teilstring-Filter allein auf das Wort (Ausgangssprache).</param>
    /// <param name="translation">Teilstring-Filter allein auf die Übersetzung (Zielsprache).</param>
    /// <param name="partOfSpeech">Exakte Wortart.</param>
    /// <param name="untranslated">true = nur Einträge ohne Übersetzung.</param>
    /// <param name="incomplete">true = nur unvollständige Einträge (keine Übersetzung / Wortart „Other" / fehlende Noun-/Verb-Details).</param>
    /// <param name="linked">true = nur verknüpfte (Grundform gesetzt), false = nur unverknüpfte.</param>
    /// <param name="baseFormsOnly">true = nur Grundformen (keine flektierten Formen); klarer Alias für Authoring, fachlich wie <c>linked=false</c>.</param>
    /// <param name="sourceLanguage">Filter auf die Ausgangssprache.</param>
    /// <param name="targetLanguage">Filter auf die Zielsprache.</param>
    /// <param name="tag">Ein oder mehrere Tag-Namen (wiederholbar).</param>
    /// <param name="matchAll">Bei mehreren Tags: true = alle (UND), false = beliebiger (ODER, Default).</param>
    /// <param name="sort">Sortierspalte: <c>key</c> (Default), <c>word</c>, <c>translation</c>, <c>pos</c>, <c>created</c>.
    /// Kurzform <c>-word</c> = absteigend.</param>
    /// <param name="dir"><c>asc</c> (Default) oder <c>desc</c>; hat Vorrang vor einem <c>-</c>-Präfix in <paramref name="sort"/>.</param>
    /// <param name="skip">Anzahl zu überspringender Einträge (Paging).</param>
    /// <param name="take">Maximale Trefferzahl (1..500).</param>
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
        [FromQuery] int take = PagingExtensions.DefaultTake)
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

        var items = await WithGraph(ApplySort(query, SortingExtensions.ParseSort(sort, dir))).ToPagedListAsync(Response, skip, take);
        return items.Select(Map);
    }

    /// <summary>
    /// Wendet die per Whitelist erlaubte Sortierung an; jede Variante endet mit <c>Id</c> als Tiebreaker,
    /// damit das Paging-Fenster deterministisch bleibt. Unbekannte/leere Keys → Standard nach <c>Key</c>.
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

    /// <summary>Eine Vokabel per numerischer Id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VocabularyResponse>> Get(int id)
    {
        var v = await WithGraph(db.Vocabulary.AsNoTracking()).FirstOrDefaultAsync(x => x.Id == id);
        return v is null ? NotFound() : Map(v);
    }

    /// <summary>Eine Vokabel per stabilem Key (Referenz-Slug).</summary>
    [HttpGet("by-key/{key}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VocabularyResponse>> GetByKey(string key)
    {
        var v = await WithGraph(db.Vocabulary.AsNoTracking()).FirstOrDefaultAsync(x => x.Key == key);
        return v is null ? NotFound() : Map(v);
    }

    /// <summary>
    /// Alle Formen einer Grundform-Familie (z. B. go → went → gone). Ausgehend von einer beliebigen Form
    /// wird die Grundform bestimmt (<c>BaseFormId ?? Id</c>) und samt aller darauf verweisenden Formen
    /// geliefert – jede mit ihrem <c>BaseFormRelation</c>-Label. Grundform steht zuerst.
    /// </summary>
    [HttpGet("{id:int}/forms")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<VocabularyResponse>>> Forms(int id)
    {
        var self = await db.Vocabulary.AsNoTracking().Select(v => new { v.Id, v.BaseFormId })
            .FirstOrDefaultAsync(v => v.Id == id);
        if (self is null) return NotFound();

        var baseId = self.BaseFormId ?? self.Id;
        var family = await WithGraph(db.Vocabulary.AsNoTracking())
            .Where(v => v.Id == baseId || v.BaseFormId == baseId)
            .ToListAsync();

        // Grundform zuerst, danach die flektierten Formen stabil nach Key.
        return family
            .OrderByDescending(v => v.Id == baseId).ThenBy(v => v.Key, StringComparer.Ordinal)
            .Select(Map).ToList();
    }

    /// <summary>
    /// Anlegen einer Vokabel. „Einfach" genügt <see cref="Word"/> (+ Sprachen): <see cref="Key"/> darf leer
    /// bleiben (der Server generiert einen eindeutigen Slug), <see cref="Translation"/> darf entfallen
    /// (bleibt leer und ist per <c>?untranslated=true</c> auffindbar) und <see cref="PartOfSpeech"/> darf
    /// entfallen (Default <see cref="Models.PartOfSpeech.Other"/>). „Komplex" füllt zusätzlich
    /// Noun/Verb/BaseForm/Audio; fehlende Details lassen sich später per PATCH nachliefern.
    /// <see cref="Tags"/> (Namen) werden create-if-missing verknüpft.
    /// </summary>
    public record CreateVocabularyDto(string? Key, string SourceLanguage, string TargetLanguage,
        string Word, string? Translation = null, PartOfSpeech? PartOfSpeech = null, string? Version = null,
        NounInfo? Noun = null, VerbInfo? Verb = null, string? BaseFormKey = null,
        string? BaseFormRelation = null, string? PronunciationAudioUrl = null, List<string>? Tags = null);

    /// <summary>Erstellt eine Vokabel. Fehlt der Key, wird ein eindeutiger generiert; BaseFormKey (falls gesetzt) muss existieren.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VocabularyResponse>> Create(CreateVocabularyDto dto)
    {
        var outcome = await CreateCoreAsync(dto);
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
    /// Gemeinsame Anlege-Logik für Einzel-POST und Batch. Legt an und lädt Grundform + Tags für die Antwort.
    /// Ein bereits vergebener, explizit gesetzter Key liefert <see cref="CreateKind.Conflict"/> (der Aufrufer
    /// entscheidet: 409 einzeln bzw. idempotentes „existing" im Batch).
    /// </summary>
    private async Task<CreateOutcome> CreateCoreAsync(CreateVocabularyDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Word))
            return new(CreateKind.Error, null, null, "Word is required.");

        string key;
        if (string.IsNullOrWhiteSpace(dto.Key))
        {
            key = await UniqueKeyAsync(VocabKey.Generate(dto.SourceLanguage, dto.Word, dto.TargetLanguage, dto.Translation ?? ""));
        }
        else
        {
            key = dto.Key.Trim();
            if (await db.Vocabulary.AnyAsync(v => v.Key == key))
                return new(CreateKind.Conflict, null, key, $"Key '{key}' already exists.");
        }

        int? baseFormId = null;
        if (!string.IsNullOrWhiteSpace(dto.BaseFormKey))
        {
            baseFormId = await db.Vocabulary.Where(v => v.Key == dto.BaseFormKey)
                .Select(v => (int?)v.Id).FirstOrDefaultAsync();
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
            PartOfSpeech = dto.PartOfSpeech ?? Models.PartOfSpeech.Other,
            Noun = dto.Noun,
            Verb = dto.Verb,
            BaseFormId = baseFormId,
            BaseFormRelation = string.IsNullOrWhiteSpace(dto.BaseFormRelation) ? null : dto.BaseFormRelation.Trim(),
            PronunciationAudioUrl = dto.PronunciationAudioUrl,
        };
        db.Vocabulary.Add(vocab);
        await ApplyTagsAsync(vocab, dto.Tags);
        await db.SaveChangesAsync();

        await LoadGraphAsync(vocab);
        return new(CreateKind.Created, vocab, key, null);
    }

    /// <summary>Macht einen generierten Basiskey eindeutig, indem bei Kollision _2, _3 … angehängt wird.</summary>
    private async Task<string> UniqueKeyAsync(string baseKey)
    {
        var key = string.IsNullOrWhiteSpace(baseKey) ? "vokabel" : baseKey;
        if (!await db.Vocabulary.AnyAsync(v => v.Key == key)) return key;
        for (var n = 2; ; n++)
        {
            var candidate = $"{key}_{n}";
            if (!await db.Vocabulary.AnyAsync(v => v.Key == candidate)) return candidate;
        }
    }

    /// <summary>Nur gesetzte Felder werden geändert. BaseFormKey = "" hebt die Verknüpfung (und ihr Label) auf; Tags werden ergänzt (nicht ersetzt).</summary>
    public record UpdateVocabularyDto(string? Version, string? SourceLanguage, string? TargetLanguage,
        string? Word, string? Translation, PartOfSpeech? PartOfSpeech, NounInfo? Noun, VerbInfo? Verb,
        string? BaseFormKey, string? BaseFormRelation, string? PronunciationAudioUrl, List<string>? Tags);

    /// <summary>Ändert eine Vokabel (partiell).</summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VocabularyResponse>> Update(int id, UpdateVocabularyDto dto)
    {
        var (status, vocab, error) = await UpdateCoreAsync(id, dto);
        return status switch
        {
            UpdateStatus.Ok => Map(vocab!),
            UpdateStatus.NotFound => NotFound(),
            _ => this.ProblemWithCode(ApiErrors.ValidationError, error),
        };
    }

    private enum UpdateStatus { Ok, NotFound, Error }

    /// <summary>Gemeinsame Update-Logik für Einzel-PATCH und Batch (lädt Grundform + Tags für die Antwort).</summary>
    private async Task<(UpdateStatus Status, Vocabulary? Vocab, string? Error)> UpdateCoreAsync(int id, UpdateVocabularyDto dto)
    {
        var vocab = await db.Vocabulary.Include(v => v.TagLinks).ThenInclude(l => l.VocabTag)
            .FirstOrDefaultAsync(v => v.Id == id);
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
                    .Select(v => (int?)v.Id).FirstOrDefaultAsync();
                if (baseFormId is null) return (UpdateStatus.Error, null, $"BaseFormKey '{dto.BaseFormKey}' not found.");
                vocab.BaseFormId = baseFormId;
            }
        }
        if (dto.BaseFormRelation is not null)
            vocab.BaseFormRelation = dto.BaseFormRelation.Trim() is { Length: > 0 } r ? r : null;

        await ApplyTagsAsync(vocab, dto.Tags);
        await db.SaveChangesAsync();
        await LoadGraphAsync(vocab);
        return (UpdateStatus.Ok, vocab, null);
    }

    /// <summary>Löscht eine Vokabel. Nicht möglich, solange sie Grundform anderer Vokabeln ist.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var vocab = await db.Vocabulary.FindAsync(id);
        if (vocab is null) return NotFound();

        if (await db.Vocabulary.AnyAsync(v => v.BaseFormId == id))
            return this.ProblemWithCode(ApiErrors.VocabularyInUse, "The vocabulary item is the base form of other entries and cannot be deleted.");

        // Verhindert stille „(Vokabel fehlt)"-Platzhalter in Übungen, die die Vokabel referenzieren.
        if ((await ReferencingExercisesAsync(vocab.Id, vocab.Key)).Count > 0)
            return this.ProblemWithCode(ApiErrors.VocabularyInUse, "The vocabulary item is used in one or more exercises and cannot be deleted.");

        db.Vocabulary.Remove(vocab);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Eine Übung, die diese Vokabel referenziert (Vokabel-Refs bzw. Lückentext-Lücke).</summary>
    public record VocabUsage(int ExerciseId, string Title, string Type, int ChapterId, int SubjectId);

    /// <summary>
    /// Welche Übungen die Vokabel referenzieren – Vokabel-Übungen über ihre <see cref="ExerciseItem"/>-Zeilen (per Id),
    /// Lückentexte über <see cref="Gap.VocabKey"/> in der Config. Grundlage für den Lösch-Schutz und die Autoren-Sicht.
    /// </summary>
    [HttpGet("{id:int}/usage")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<VocabUsage>>> Usage(int id)
    {
        var key = await db.Vocabulary.Where(v => v.Id == id).Select(v => v.Key).FirstOrDefaultAsync();
        if (key is null) return NotFound();
        return await ReferencingExercisesAsync(id, key);
    }

    /// <summary>
    /// Findet referenzierende Übungen: Vokabelübungen über die <see cref="ExerciseItem"/>-Tabelle (per Vokabel-Id),
    /// Lückentexte weiterhin per Key in der ConfigJson (SQL-Vorfilter + präzise JSON-Prüfung).
    /// </summary>
    private async Task<List<VocabUsage>> ReferencingExercisesAsync(int id, string key)
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
            .ToListAsync();
        var used = viaItems
            .Select(e => new VocabUsage(e.Id, e.Title, e.Type.ToString(), e.ChapterId, e.SubjectId))
            .ToList();

        // Lückentexte: Key-Referenz in der ConfigJson.
        var clozeCandidates = await db.Exercises.AsNoTracking().Include(e => e.Chapter)
            .Where(e => e.Type == ExerciseType.Cloze && e.ConfigJson.Contains(key))
            .ToListAsync();
        foreach (var e in clozeCandidates)
        {
            var referenced = JsonSerializer.Deserialize<ClozeConfig>(e.ConfigJson, JsonOptions)?.Gaps.Any(g => g.VocabKey == key) ?? false;
            if (referenced)
                used.Add(new VocabUsage(e.Id, e.Title, e.Type.ToString(), e.ChapterId, e.Chapter?.SubjectId ?? 0));
        }
        return used;
    }

    // ---- Agenten-Primitive: Lookup (Dedup) + Batch-Anlegen/-Nachtragen ------------------------------

    /// <summary>Anfrage der Existenzprüfung: Wörter (für die Text-Extraktion) und/oder Keys (für Ref-Validierung).</summary>
    public record LookupRequest(string? SourceLanguage, string? TargetLanguage, List<string>? Words, List<string>? Keys);
    /// <summary>Treffer je angefragtem Wort inkl. bereits vorhandener Store-Einträge.</summary>
    public record LookupResult(string Word, bool Exists, IReadOnlyList<VocabularyResponse> Matches);
    /// <summary>Antwort der Existenzprüfung: pro Wort ein Ergebnis plus die Menge existierender Keys.</summary>
    public record LookupResponse(IReadOnlyList<LookupResult> Words, IReadOnlyList<string> ExistingKeys);

    /// <summary>
    /// Existenz-Prüfung für die Text→Vokabel-Extraktion (Dedup, bevor der Agent anlegt). Der Vergleich läuft
    /// case-insensitiv über <c>Word</c>, optional gefiltert nach Sprachpaar. Zusätzlich lässt sich prüfen,
    /// welche <paramref name="request"/>.Keys bereits existieren (z. B. zum Validieren von Übungs-Refs).
    /// </summary>
    [HttpPost("lookup")]
    public async Task<ActionResult<LookupResponse>> Lookup(LookupRequest request)
    {
        var words = (request.Words ?? [])
            .Select(w => w.Trim()).Where(w => w.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var results = new List<LookupResult>();
        if (words.Count > 0)
        {
            var lowered = words.Select(w => w.ToLower()).ToList();
            var q = db.Vocabulary.AsNoTracking().Where(v => lowered.Contains(v.Word.ToLower()));
            if (!string.IsNullOrWhiteSpace(request.SourceLanguage))
                q = q.Where(v => v.SourceLanguage == request.SourceLanguage);
            if (!string.IsNullOrWhiteSpace(request.TargetLanguage))
                q = q.Where(v => v.TargetLanguage == request.TargetLanguage);

            var matches = await WithGraph(q).ToListAsync();
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
            : await db.Vocabulary.AsNoTracking().Where(v => keys.Contains(v.Key)).Select(v => v.Key).ToListAsync();

        return new LookupResponse(results, existingKeys);
    }

    /// <summary>Ergebnis eines einzelnen Batch-Elements (Teilerfolg möglich).</summary>
    public record BatchItemResult(int Index, string Status, int? Id, string? Key, string? Error);

    /// <summary>
    /// Legt viele Vokabeln in einem Aufruf an – idempotent: ein bereits existierender, explizit gesetzter
    /// Key liefert Status <c>existing</c> (kein Fehler), sodass derselbe Batch gefahrlos wiederholt werden
    /// kann. Ohne Key generiert der Server einen eindeutigen (Status <c>created</c>). Sprachlogik
    /// (Tokenisieren/Übersetzen) liegt beim Aufrufer – die API verwaltet nur die Daten.
    /// </summary>
    [HttpPost("batch")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<BatchItemResult>>> CreateBatch(List<CreateVocabularyDto> items)
    {
        if (items is not { Count: > 0 }) return this.ProblemWithCode(ApiErrors.ValidationError, "At least one entry is required.");

        var results = new List<BatchItemResult>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            var outcome = await CreateCoreAsync(items[i]);
            switch (outcome.Kind)
            {
                case CreateKind.Created:
                    results.Add(new(i, "created", outcome.Vocab!.Id, outcome.Vocab.Key, null));
                    break;
                case CreateKind.Conflict:
                    // Idempotent: der Eintrag mit diesem Key existiert bereits – zurückmelden statt Fehler.
                    var existingId = await db.Vocabulary.Where(v => v.Key == outcome.Key).Select(v => (int?)v.Id).FirstOrDefaultAsync();
                    results.Add(new(i, "existing", existingId, outcome.Key, null));
                    break;
                default:
                    results.Add(new(i, "error", null, outcome.Key, outcome.Error));
                    break;
            }
        }
        return results;
    }

    /// <summary>Ein Batch-Änderungselement: die Ziel-Id plus dieselben partiellen Felder wie beim Einzel-PATCH.</summary>
    public record BatchUpdateItem(int Id, string? Version, string? SourceLanguage, string? TargetLanguage,
        string? Word, string? Translation, PartOfSpeech? PartOfSpeech, NounInfo? Noun, VerbInfo? Verb,
        string? BaseFormKey, string? BaseFormRelation, string? PronunciationAudioUrl, List<string>? Tags);

    /// <summary>Trägt Felder vieler Vokabeln in einem Aufruf nach (gleiche Merge-Semantik wie Einzel-PATCH).</summary>
    [HttpPatch("batch")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<BatchItemResult>>> UpdateBatch(List<BatchUpdateItem> items)
    {
        if (items is not { Count: > 0 }) return this.ProblemWithCode(ApiErrors.ValidationError, "At least one entry is required.");

        var results = new List<BatchItemResult>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            var it = items[i];
            var (status, vocab, error) = await UpdateCoreAsync(it.Id, new UpdateVocabularyDto(
                it.Version, it.SourceLanguage, it.TargetLanguage, it.Word, it.Translation, it.PartOfSpeech,
                it.Noun, it.Verb, it.BaseFormKey, it.BaseFormRelation, it.PronunciationAudioUrl, it.Tags));
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

    /// <summary>Lädt Grundform + Tags einer getrackten Vokabel für die Antwort-Projektion nach.</summary>
    private async Task LoadGraphAsync(Vocabulary vocab)
    {
        await db.Entry(vocab).Reference(v => v.BaseForm).LoadAsync();
        await db.Entry(vocab).Collection(v => v.TagLinks).Query().Include(l => l.VocabTag).LoadAsync();
    }

    /// <summary>
    /// Verknüpft die Vokabel mit den genannten Tags (create-if-missing, exakter Name, additiv – bereits
    /// verknüpfte werden übersprungen). Erwartet, dass vorhandene <c>TagLinks</c> der Vokabel geladen sind.
    /// </summary>
    private async Task ApplyTagsAsync(Vocabulary vocab, List<string>? tagNames)
    {
        if (tagNames is null) return;
        var names = tagNames.Select(n => n.Trim()).Where(n => n.Length > 0).Distinct(StringComparer.Ordinal).ToList();
        if (names.Count == 0) return;

        var existing = await db.VocabTags.Where(t => names.Contains(t.Name)).ToListAsync();
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
