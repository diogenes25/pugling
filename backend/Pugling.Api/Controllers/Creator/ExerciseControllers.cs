using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

// Ein Controller je Übungstyp. Jeder erbt die gemeinsame CRUD-Logik aus
// ExerciseControllerBase<TConfig> und legt nur Route, Tag und Type fest.
// Dadurch bekommt jeder Typ einen eigenen Pfad und ein eigenes Config-Schema in Swagger.

/// <summary>Shared route prefix of all exercise types.</summary>
internal static class ExerciseRoutes
{
    public const string Base = ApiRoutes.Creator + "/subjects/{subjectId:int}/chapters/{chapterId:int}";
}

/// <summary>
/// Vocabulary exercises. The exercise itself describes type/goal/value; its vocabulary pairs live one level deeper as
/// stably identified <see cref="ExerciseItem"/>s (CRUD under <c>{exerciseId}/items/{itemId}</c>). On creation,
/// the POST still accepts inline <see cref="VocabItem"/>/<see cref="VocabRef"/> in the payload and materializes
/// them into the item table; every vocabulary entry used is thereby created/linked in the store.
/// </summary>
[Route(ExerciseRoutes.Base + "/vocabulary")]
[Tags("Creator – Vocabulary")]
public class VocabularyController(PuglingDbContext db, ExerciseTypeRegistry registry, ExerciseItemService items, VocabularyStoreService store)
    : ExerciseControllerBase<VocabularyConfig>(db, registry)
{
    /// <inheritdoc/>
    protected override string TypeKey => ExerciseTypeKeys.Vocabulary;

    /// <summary>
    /// Ensures on create/change that all store entries referenced by ID exist and – if
    /// inline vocabulary entries without an ID are present – that the language codes are set (needed to create them in the store).
    /// </summary>
    protected override async Task<string?> ValidateConfigAsync(int subjectId, VocabularyConfig config, CancellationToken ct = default)
    {
        if (config.Refs is { Count: > 0 } refs)
        {
            if (refs.Any(r => r.VocabularyId <= 0))
                return "Every reference needs a valid vocabularyId (> 0).";
            var ids = refs.Select(r => r.VocabularyId).Distinct().ToList();
            var existing = await Db.Vocabularies.Where(v => ids.Contains(v.Id)).Select(v => v.Id).ToListAsync(ct);
            var missing = ids.Except(existing).ToList();
            if (missing.Count > 0) return $"Unknown vocabulary item IDs: {string.Join(", ", missing)}";
        }

        // Inline-Items tragen – wie der Item-Endpunkt – entweder eine (existierende) VocabularyId ODER Front + Back
        // (die dann im Store angelegt werden). Front/Back sind optional, ohne VocabularyId aber beide Pflicht.
        if (config.Items.Any(i => i.VocabularyId is null
            && (string.IsNullOrWhiteSpace(i.Front) || string.IsNullOrWhiteSpace(i.Back))))
            return "Every inline item needs either a vocabularyId or both front and back.";
        if (config.Items.Any(i => i.VocabularyId is null)
            && (string.IsNullOrWhiteSpace(config.SourceLang) || string.IsNullOrWhiteSpace(config.TargetLang)))
            return "sourceLang and targetLang are required to create inline vocabulary items in the store.";

        var itemIds = config.Items.Where(i => i.VocabularyId is > 0).Select(i => i.VocabularyId!.Value).Distinct().ToList();
        if (itemIds.Count > 0)
        {
            var existing = await Db.Vocabularies.Where(v => itemIds.Contains(v.Id)).Select(v => v.Id).ToListAsync(ct);
            var missing = itemIds.Except(existing).ToList();
            if (missing.Count > 0) return $"Unknown vocabulary item IDs: {string.Join(", ", missing)}";
        }
        return null;
    }

    /// <summary>
    /// Materializes the items of the exercise after saving into the <see cref="ExerciseItem"/> table (stable item IDs):
    /// on POST from the payload, on PUT only if the payload carries any items/refs at all (a pure settings PUT
    /// leaves the item set maintained via <c>/items</c> untouched). The reconciliation preserves the id of surviving words.
    /// The config is then reduced to pure settings – items/refs are now the table (a single source).
    /// </summary>
    protected override async Task AfterSaveAsync(Exercise exercise, VocabularyConfig config, bool isCreate, CancellationToken ct = default)
    {
        var hasPayloadItems = config.Items.Count > 0 || config.Refs is { Count: > 0 };
        if (isCreate || hasPayloadItems)
            await items.SyncFromConfigAsync(exercise.Id, config, ct);
        if (hasPayloadItems)
        {
            config.Items = [];
            config.Refs = null;
            SetConfig(exercise, config);
            await Db.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Sets the items of the exercise as a snapshot to the current vocabulary of the named tags (optionally only
    /// base forms, optionally all tags via AND). The adult thereby materializes "all words from unit 3" – the
    /// reconciliation preserves the id (and the progress) of surviving words; only dropped ones disappear.
    /// </summary>
    [HttpPost("{exerciseId:int}/refs-from-tags")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseResponse<VocabularyConfig>>> RefsFromTags(
        int subjectId, int chapterId, int exerciseId, RefsFromTagsDto dto, CancellationToken ct = default)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId, ct);
        if (exercise is null) return NotFound();
        if (EnsureCanWrite(exercise) is { } forbidden) return forbidden;

        var tags = (dto.Tags ?? []).Select(t => t.Trim()).Where(t => t.Length > 0).Distinct().ToList();
        if (tags.Count == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "At least one tag is required.");

        var query = Db.Vocabularies.AsNoTracking().AsQueryable();
        if (dto.BaseFormsOnly) query = query.Where(v => v.BaseFormId == null);
        if (dto.MatchAll)
            foreach (var name in tags) query = query.Where(v => v.TagLinks.Any(l => l.VocabTag!.Name == name));
        else
            query = query.Where(v => v.TagLinks.Any(l => tags.Contains(l.VocabTag!.Name)));
        var hitIds = await query.OrderBy(v => v.Key).Select(v => v.Id).ToListAsync(ct);
        // Ein Snapshot ohne Treffer würde die Übung **leeren** – und zwar lautlos: ein vertippter Tag
        // (oder `baseFormsOnly` auf einer Liste rein flektierter Formen) sähe wie ein Erfolg aus und ließe
        // eine Übung ohne Wörter zurück. Nichts ändern ist hier die einzige vertretbare Antwort.
        if (hitIds.Count == 0)
            return this.ProblemWithCode(ApiErrors.NoTagMatches,
                "No vocabulary matched these tags; the exercise was left unchanged.");

        await items.ReconcileAsync(exercise.Id, hitIds.Select(id => new DesiredItem(id, null)).ToList(), ct);
        return Map(exercise, User.AdultId());
    }

    // ---- Einzel-Items (Vokabelpaare) als eigene Sub-Ressource -----------------------------------------

    // Konkreter Pfad (wie VocabLink.Path); das Routen-Template ApiRoutes.Creator trägt den Versions-Platzhalter.
    private static string ItemSelf(int subjectId, int chapterId, int exerciseId, int itemId) =>
        $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}/items/{itemId}";

    private static VocabItemResponse MapItem(int subjectId, int chapterId, int exerciseId, ExerciseItem item) =>
        new(item.Id, item.OrderIndex, item.VocabularyId, item.Vocabulary?.Word ?? "", item.Vocabulary?.Translation ?? "",
            item.Hint, ItemSelf(subjectId, chapterId, exerciseId, item.Id), VocabLink.Path + item.VocabularyId);

    /// <summary>All items of the exercise in order (front/back from the store).</summary>
    [HttpGet("{exerciseId:int}/items")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<VocabItemResponse>>> ListItems(int subjectId, int chapterId, int exerciseId, CancellationToken ct = default)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId, ct);
        if (exercise is null) return NotFound();
        var rows = await Db.ExerciseItems.AsNoTracking().Include(i => i.Vocabulary)
            .Where(i => i.ExerciseId == exerciseId)
            .OrderBy(i => i.OrderIndex).ThenBy(i => i.Id)
            .ToListAsync(ct);
        return rows.Select(i => MapItem(subjectId, chapterId, exerciseId, i)).ToList();
    }

    /// <summary>A single item of the exercise.</summary>
    [HttpGet("{exerciseId:int}/items/{itemId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VocabItemResponse>> GetItem(int subjectId, int chapterId, int exerciseId, int itemId, CancellationToken ct = default)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId, ct);
        if (exercise is null) return NotFound();
        var item = await FindItemAsync(exerciseId, itemId, ct);
        return item is null
            ? this.ProblemWithCode(ApiErrors.ItemNotFound, "The exercise item does not exist in this exercise.")
            : MapItem(subjectId, chapterId, exerciseId, item);
    }

    /// <summary>Adds a vocabulary pair to the exercise (by store id or inline). New items land at the end.</summary>
    [HttpPost("{exerciseId:int}/items")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VocabItemResponse>> AddItem(int subjectId, int chapterId, int exerciseId, VocabItemInput body, CancellationToken ct)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId, ct);
        if (exercise is null) return NotFound();
        if (EnsureCanWrite(exercise) is { } forbidden) return forbidden;

        var config = ConfigOf(exercise);
        var resolved = await ResolveVocabularyIdAsync(body, config, ct);
        if (resolved is not { } vocabId) return this.ProblemWithCode(ApiErrors.ValidationError,
            "Provide an existing vocabularyId, or front and back (plus the exercise's sourceLang/targetLang) to create one.");

        // Eine Vokabel darf je Übung nur ein Item haben (Unique in der DB): zwei Items auf dasselbe Wort
        // erzeugten zwei konkurrierende ItemProgress-Zeilen, und der Lernstand desselben Worts liefe
        // innerhalb einer Übung auseinander. Ohne diese Vorprüfung käme der Index als 500 durch.
        if (await Db.ExerciseItems.AnyAsync(i => i.ExerciseId == exerciseId && i.VocabularyId == vocabId, ct))
            return this.ProblemWithCode(ApiErrors.DuplicateVocabularyInExercise,
                "This vocabulary entry is already an item of the exercise.");

        // Anfügen ans Ende verschiebt keine bestehenden Positionen (sicher); eine feste Einfügeposition schon.
        if (body.OrderIndex is not null && await ExerciseInPlanAsync(exerciseId, ct)) return ShiftBlockedProblem();
        var nextOrder = body.OrderIndex ??
            (await Db.ExerciseItems.Where(i => i.ExerciseId == exerciseId).Select(i => (int?)i.OrderIndex).MaxAsync(ct) is { } max ? max + 1 : 0);
        var item = new ExerciseItem
        {
            ExerciseId = exerciseId,
            VocabularyId = vocabId,
            Hint = NormalizeHint(body.Hint),
            OrderIndex = nextOrder,
        };
        Db.ExerciseItems.Add(item);
        await Db.SaveChangesAsync(ct);

        item.Vocabulary = await Db.Vocabularies.FindAsync([vocabId], ct);
        return CreatedAtAction(nameof(GetItem), new { subjectId, chapterId, exerciseId, itemId = item.Id },
            MapItem(subjectId, chapterId, exerciseId, item));
    }

    /// <summary>Changes an item: swap the vocabulary entry (by id or inline), adjust the hint or order.</summary>
    [HttpPatch("{exerciseId:int}/items/{itemId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VocabItemResponse>> PatchItem(int subjectId, int chapterId, int exerciseId, int itemId, VocabItemInput body, CancellationToken ct)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId, ct);
        if (exercise is null) return NotFound();
        if (EnsureCanWrite(exercise) is { } forbidden) return forbidden;
        var item = await FindItemAsync(exerciseId, itemId, ct);
        if (item is null) return this.ProblemWithCode(ApiErrors.ItemNotFound, "The exercise item does not exist in this exercise.");

        if (body.VocabularyId is not null || body.Front is not null || body.Back is not null)
        {
            var config = ConfigOf(exercise);
            var resolved = await ResolveVocabularyIdAsync(body, config, ct);
            if (resolved is not { } vocabId) return this.ProblemWithCode(ApiErrors.ValidationError,
                "Provide an existing vocabularyId, or front and back (plus the exercise's sourceLang/targetLang) to change the word.");
            item.VocabularyId = vocabId;
        }
        if (body.Hint is not null) item.Hint = NormalizeHint(body.Hint);
        if (body.OrderIndex is { } order && order != item.OrderIndex)
        {
            // Umsortieren verschiebt Positionen → bei in-Plan gespielter Übung blocken (siehe ExerciseInPlanAsync).
            if (await ExerciseInPlanAsync(exerciseId, ct)) return ShiftBlockedProblem();
            item.OrderIndex = order;
        }
        await Db.SaveChangesAsync(ct);

        item.Vocabulary = await Db.Vocabularies.FindAsync([item.VocabularyId], ct);
        return MapItem(subjectId, chapterId, exerciseId, item);
    }

    /// <summary>Removes an item from the exercise.</summary>
    [HttpDelete("{exerciseId:int}/items/{itemId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(int subjectId, int chapterId, int exerciseId, int itemId, CancellationToken ct)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId, ct);
        if (exercise is null) return NotFound();
        if (EnsureCanWrite(exercise) is { } forbidden) return forbidden;
        var item = await FindItemAsync(exerciseId, itemId, ct);
        if (item is null) return this.ProblemWithCode(ApiErrors.ItemNotFound, "The exercise item does not exist in this exercise.");
        // Löschen verschiebt Folgepositionen → bei in-Plan gespielter Übung blocken (Fortschritt bliebe fehl-verankert).
        if (await ExerciseInPlanAsync(exerciseId, ct)) return ShiftBlockedProblem();
        Db.ExerciseItems.Remove(item);
        await Db.SaveChangesAsync(ct);
        return NoContent();
    }

    // Kein Vorgabewert für `ct` (hier wie in den übrigen Helfern): ein weggelassenes optionales Argument
    // sieht weder CA2016 noch der Signatur-Wächter – ohne Default erzwingt der Compiler das Durchreichen.
    private Task<ExerciseItem?> FindItemAsync(int exerciseId, int itemId, CancellationToken ct) =>
        Db.ExerciseItems.Include(i => i.Vocabulary).FirstOrDefaultAsync(i => i.Id == itemId && i.ExerciseId == exerciseId, ct);

    // Wird die Übung in einem Lehrplan gespielt? Dann verankert PositionItemProgress den Leitner-Fortschritt
    // je Position auf der (positionalen) Item-Reihenfolge – index-verschiebende Item-Mutationen (Löschen,
    // Umsortieren, Einfügen an fester Position) würden gespeicherten Fortschritt aufs falsche Wort umbiegen.
    private Task<bool> ExerciseInPlanAsync(int exerciseId, CancellationToken ct) =>
        Db.PlanPositions.AnyAsync(p => p.ExerciseId == exerciseId, ct);

    private ObjectResult ShiftBlockedProblem() =>
        this.ProblemWithCode(ApiErrors.ExerciseInUse,
            "The exercise is used in a study plan; items cannot be removed or reordered (it would shift saved progress). Adding to the end is allowed; remove it from plans first for other changes.");

    private static string? NormalizeHint(string? hint) =>
        string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();

    /// <summary>
    /// Determines the target vocabulary entry of an item input: prefers the given store id (must exist), otherwise
    /// creates front/back inline in the store (needs the exercise's language codes). Return value <c>null</c> = insufficient input.
    /// </summary>
    private async Task<int?> ResolveVocabularyIdAsync(VocabItemInput body, VocabularyConfig config, CancellationToken ct)
    {
        if (body.VocabularyId is { } id)
            return await Db.Vocabularies.AnyAsync(v => v.Id == id, ct) ? id : null;
        if (string.IsNullOrWhiteSpace(body.Front) || string.IsNullOrWhiteSpace(body.Back)
            || string.IsNullOrWhiteSpace(config.SourceLang) || string.IsNullOrWhiteSpace(config.TargetLang))
            return null;
        var vocab = await store.GetOrCreateAsync(config.SourceLang, body.Front.Trim(), config.TargetLang, body.Back.Trim(), ct: ct);
        await Db.SaveChangesAsync(ct);
        return vocab.Id;
    }
}

/// <summary>Reading comprehension exercises.</summary>
[Route(ExerciseRoutes.Base + "/reading")]
[Tags("Creator – Reading")]
public class ReadingController(PuglingDbContext db, ExerciseTypeRegistry registry) : ExerciseControllerBase<ReadingConfig>(db, registry)
{
    /// <inheritdoc/>
    protected override string TypeKey => ExerciseTypeKeys.Reading;
}

/// <summary>Cloze exercises. Gaps may reference the vocabulary store via <see cref="Gap.VocabKey"/>.</summary>
[Route(ExerciseRoutes.Base + "/cloze")]
[Tags("Creator – Cloze")]
public class ClozeController(PuglingDbContext db, ExerciseTypeRegistry registry) : ExerciseControllerBase<ClozeConfig>(db, registry)
{
    /// <inheritdoc/>
    protected override string TypeKey => ExerciseTypeKeys.Cloze;

    /// <summary>Ensures on create/change that all store keys referenced in gaps exist.</summary>
    protected override async Task<string?> ValidateConfigAsync(int subjectId, ClozeConfig config, CancellationToken ct = default)
    {
        var keys = config.Gaps.Where(g => !string.IsNullOrWhiteSpace(g.VocabKey))
            .Select(g => g.VocabKey!).Distinct().ToList();
        if (keys.Count == 0) return null;
        var existing = await Db.Vocabularies.Where(v => keys.Contains(v.Key)).Select(v => v.Key).ToListAsync(ct);
        var missing = keys.Except(existing).ToList();
        return missing.Count == 0 ? null : $"Unknown vocabulary keys in gaps: {string.Join(", ", missing)}";
    }
}

/// <summary>Essay exercises.</summary>
[Route(ExerciseRoutes.Base + "/essays")]
[Tags("Creator – Essays")]
public class EssaysController(PuglingDbContext db, ExerciseTypeRegistry registry) : ExerciseControllerBase<EssayConfig>(db, registry)
{
    /// <inheritdoc/>
    protected override string TypeKey => ExerciseTypeKeys.Essay;
}

/// <summary>Listening comprehension exercises.</summary>
[Route(ExerciseRoutes.Base + "/listening")]
[Tags("Creator – Listening")]
public class ListeningController(PuglingDbContext db, ExerciseTypeRegistry registry) : ExerciseControllerBase<ListeningConfig>(db, registry)
{
    /// <inheritdoc/>
    protected override string TypeKey => ExerciseTypeKeys.Listening;
}

/// <summary>Grammar exercises.</summary>
[Route(ExerciseRoutes.Base + "/grammar")]
[Tags("Creator – Grammar")]
public class GrammarController(PuglingDbContext db, ExerciseTypeRegistry registry) : ExerciseControllerBase<GrammarConfig>(db, registry)
{
    /// <inheritdoc/>
    protected override string TypeKey => ExerciseTypeKeys.Grammar;
}

/// <summary>Matching exercises (pairs). Besides the CRUD, <see cref="Check"/> evaluates the given matches.</summary>
[Route(ExerciseRoutes.Base + "/matching")]
[Tags("Creator – Matching")]
public class MatchingController(PuglingDbContext db, ExerciseTypeRegistry registry)
    : ExerciseControllerBase<MatchingConfig>(db, registry)
{
    /// <inheritdoc/>
    protected override string TypeKey => ExerciseTypeKeys.Matching;

    /// <summary>Evaluates the matches: per pair, the right side given for the left side counts.</summary>
    [HttpPost("{exerciseId:int}/check")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<CheckResult>> Check(int subjectId, int chapterId, int exerciseId, CheckDto body, CancellationToken ct = default) =>
        RunCheckAsync(subjectId, chapterId, exerciseId, body, ct);
}

/// <summary>
/// Translation exercises. Every translation pair without <see cref="TranslationItem.VocabularyId"/> is
/// automatically created in the store and linked on save; the response adds the link <c>_self</c> per pair.
/// </summary>
[Route(ExerciseRoutes.Base + "/translation")]
[Tags("Creator – Translation")]
public class TranslationController(PuglingDbContext db, ExerciseTypeRegistry registry, VocabularyStoreService store) : ExerciseControllerBase<TranslationConfig>(db, registry)
{
    /// <inheritdoc/>
    protected override string TypeKey => ExerciseTypeKeys.Translation;

    /// <summary>Requires the language codes as soon as pairs without <see cref="TranslationItem.VocabularyId"/> need to be created.</summary>
    protected override Task<string?> ValidateConfigAsync(int subjectId, TranslationConfig config, CancellationToken ct = default) =>
        Task.FromResult(config.Items.Any(i => i.VocabularyId is null)
            && (string.IsNullOrWhiteSpace(config.SourceLang) || string.IsNullOrWhiteSpace(config.TargetLang))
            ? "sourceLang and targetLang are required to create translation pairs in the store."
            : null);

    /// <summary>Creates each not-yet-linked pair in the store (or finds it) and links it by ID.</summary>
    protected override async Task NormalizeConfigAsync(int subjectId, TranslationConfig config, CancellationToken ct = default)
    {
        var pending = new List<(int Index, Vocabulary Vocab)>();
        for (var i = 0; i < config.Items.Count; i++)
        {
            var item = config.Items[i];
            if (item.VocabularyId is not null) continue;
            pending.Add((i, await store.GetOrCreateAsync(config.SourceLang, item.Source, config.TargetLang, item.Target, ct: ct)));
        }
        if (pending.Count == 0) return;
        await Db.SaveChangesAsync(ct);
        foreach (var (index, vocab) in pending)
            config.Items[index] = config.Items[index] with { VocabularyId = vocab.Id };
    }

    /// <summary>Adds the derived self-link <c>_self</c> per translation pair (not persisted).</summary>
    protected override TranslationConfig ConfigForResponse(Exercise exercise)
    {
        var config = ConfigOf(exercise);
        for (var i = 0; i < config.Items.Count; i++)
            config.Items[i] = config.Items[i] with { Self = VocabLink.Self(config.Items[i].VocabularyId) };
        return config;
    }
}

// VocabCandidate/DecodedWord/DecodedSentence/BirkenbihlSentenceInput/WordOverride/DecodePreviewInput
// leben im Vertrags-Projekt (Pugling.Contracts.Creator).


/// <summary>
/// Birkenbihl method: texts in the learning language with grammar-independent word-for-word decoding
/// plus natural translation. Pure content exercise for reading/listening – deliberately without <c>/check</c>, since the
/// method does not actively query. Besides the inherited CRUD (exercise + languages + sentences en bloc), the
/// controller offers the vocabulary-backed automation: sentences are looked up word for word in the shared vocabulary store
/// and are individually correctable (homonyms).
/// </summary>
[Route(ExerciseRoutes.Base + "/birkenbihl")]
[Tags("Creator – Birkenbihl")]
public class BirkenbihlController(PuglingDbContext db, ExerciseTypeRegistry registry, BirkenbihlDecodingService decoder, VocabularyStoreService store)
    : ExerciseControllerBase<BirkenbihlConfig>(db, registry)
{
    /// <inheritdoc/>
    protected override string TypeKey => ExerciseTypeKeys.Birkenbihl;

    /// <summary>Adds the derived self-link <c>_self</c> per decoded word from its vocabulary ID (not persisted).</summary>
    protected override BirkenbihlConfig ConfigForResponse(Exercise exercise)
    {
        var config = ConfigOf(exercise);
        foreach (var s in config.Sentences)
            for (var i = 0; i < s.Decoding.Count; i++)
                s.Decoding[i] = s.Decoding[i] with { Self = VocabLink.Self(s.Decoding[i].VocabularyId) };
        return config;
    }

    /// <summary>
    /// Assigns missing (≤ 0) sentence/word IDs on save via the generic CRUD: the create form
    /// delivers the sentences without IDs, but the vocabulary-backed additional endpoints (<c>.../words/{wordId}</c>)
    /// need exercise-wide unique IDs. Already assigned IDs remain – so nothing collides.
    /// </summary>
    protected override void NormalizeConfig(BirkenbihlConfig config)
    {
        var sentenceId = NextSentenceSeed(config);
        var wordId = NextWordSeed(config);
        for (var i = 0; i < config.Sentences.Count; i++)
        {
            var s = config.Sentences[i];
            var decoding = s.Decoding.Select(w => w.WordId > 0 ? w : w with { WordId = wordId++ }).ToList();
            config.Sentences[i] = s.SentenceId > 0 ? s with { Decoding = decoding } : s with { SentenceId = sentenceId++, Decoding = decoding };
        }
        config.NextSentenceId = sentenceId;
        config.NextWordId = wordId;
    }

    // Nächste freie ID: berücksichtigt sowohl den Zähler als auch bereits vergebene IDs, damit auch per CRUD
    // (ohne gepflegte Zähler) angelegte Configs kollisionsfrei bleiben. Mindestens 1 (0 = „noch keine").
    private static int NextSentenceSeed(BirkenbihlConfig c) =>
        Math.Max(Math.Max(c.NextSentenceId, 1), c.Sentences.Select(s => s.SentenceId).DefaultIfEmpty(0).Max() + 1);

    private static int NextWordSeed(BirkenbihlConfig c) =>
        Math.Max(Math.Max(c.NextWordId, 1), c.Sentences.SelectMany(s => s.Decoding).Select(w => w.WordId).DefaultIfEmpty(0).Max() + 1);

    private static VocabCandidate ToCandidate(VocabHit h) =>
        new(h.Id, h.Word, h.Translation, h.PartOfSpeech.ToString(), VocabLink.Path + h.Id);

    // Kandidaten nur bei echter Mehrdeutigkeit (mehr als ein Treffer) ausgeben – sonst rauscht die Antwort zu.
    private static IReadOnlyList<VocabCandidate>? CandidatesOf(TokenLookup t) =>
        t.Candidates.Count > 1 ? t.Candidates.Select(ToCandidate).ToList() : null;

    private static DecodedWord ToDecodedWord(WordPair w, IReadOnlyList<VocabCandidate>? candidates) =>
        new(w.WordId, w.LearningWord, w.Gloss, w.VocabularyId, VocabLink.Self(w.VocabularyId), candidates);

    /// <summary>
    /// Decodes a sentence automatically via the vocabulary store and <b>saves</b> it in the exercise.
    /// Every word gets an exercise-wide unique <c>wordId</c> (→ individually exchangeable later), the sentence
    /// gets a <c>sentenceId</c>. Unknown words come back with an empty gloss; ambiguous ones with candidates.
    /// </summary>
    [HttpPost("{exerciseId:int}/sentences")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecodedSentence>> AddSentence(
        int subjectId, int chapterId, int exerciseId, BirkenbihlSentenceInput body, CancellationToken ct)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId, ct);
        if (exercise is null) return NotFound();
        if (EnsureCanWrite(exercise) is { } forbidden) return forbidden;
        if (string.IsNullOrWhiteSpace(body.LearningSentence)) return this.ProblemWithCode(ApiErrors.ValidationError, "The sentence in the learning language is required.");

        var config = ConfigOf(exercise);
        var lookups = await decoder.LookupAsync(config.LearningLang, config.NativeLang, body.LearningSentence, ct);

        var sentenceId = NextSentenceSeed(config);
        config.NextSentenceId = sentenceId + 1;
        var wordId = NextWordSeed(config);
        var pairs = new List<WordPair>();
        var words = new List<DecodedWord>();
        foreach (var t in lookups)
        {
            var pair = new WordPair(wordId++, t.Surface, t.Best?.Translation, t.Best?.Id);
            pairs.Add(pair);
            words.Add(ToDecodedWord(pair, CandidatesOf(t)));
        }
        config.NextWordId = wordId;

        var sentence = new BirkenbihlSentence(sentenceId, body.LearningSentence.Trim(),
            (body.NaturalTranslation ?? "").Trim(), pairs);
        config.Sentences.Add(sentence);
        SetConfig(exercise, config);
        await Db.SaveChangesAsync(ct);

        var result = new DecodedSentence(sentenceId, sentence.LearningSentence, sentence.NaturalTranslation, words);
        return CreatedAtAction(nameof(Get), new { subjectId, chapterId, exerciseId }, result);
    }

    /// <summary>
    /// Swaps the meaning of a single word (homonym correction). With <c>vocabularyId</c>, the gloss follows
    /// the chosen card; with only <c>gloss</c>, a free gloss without a card is set.
    /// </summary>
    [HttpPut("{exerciseId:int}/words/{wordId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecodedWord>> SetWord(
        int subjectId, int chapterId, int exerciseId, int wordId, WordOverride body, CancellationToken ct)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId, ct);
        if (exercise is null) return NotFound();
        if (EnsureCanWrite(exercise) is { } forbidden) return forbidden;

        var config = ConfigOf(exercise);
        var (sentence, index) = FindWord(config, wordId);
        if (index < 0) return NotFound();
        var word = sentence.Decoding[index];

        WordPair updated;
        if (body.VocabularyId is { } vocabId)
        {
            // Karte muss existieren und zum Sprachpaar der Übung passen – sonst würde eine fremde Glosse gesetzt.
            var card = await Db.Vocabularies.AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == vocabId
                    && v.SourceLanguage == config.LearningLang && v.TargetLanguage == config.NativeLang, ct);
            if (card is null) return this.ProblemWithCode(ApiErrors.InvalidReference, "Vocabulary item not found or its language pair does not match the exercise.");
            updated = word with { Gloss = card.Translation, VocabularyId = card.Id };
        }
        else if (string.IsNullOrWhiteSpace(body.Gloss))
        {
            // Glosse entfernen; Wort bleibt undekodiert und ohne Karte.
            updated = word with { Gloss = null, VocabularyId = null };
        }
        else
        {
            // Freie Glosse: trotzdem im Store verankern, damit jede genutzte Vokabel dort liegt und verlinkt ist.
            var gloss = body.Gloss.Trim();
            var vocab = await store.GetOrCreateAsync(config.LearningLang, word.LearningWord, config.NativeLang, gloss, ct: ct);
            await Db.SaveChangesAsync(ct);
            updated = word with { Gloss = gloss, VocabularyId = vocab.Id };
        }

        sentence.Decoding[index] = updated;
        SetConfig(exercise, config);
        await Db.SaveChangesAsync(ct);

        // Kandidaten der aktuellen Schreibweise erneut ermitteln (nützlich, um direkt eine andere Bedeutung zu wählen).
        var lookups = await decoder.LookupAsync(config.LearningLang, config.NativeLang, updated.LearningWord, ct);
        return ToDecodedWord(updated, lookups.Count > 0 ? CandidatesOf(lookups[0]) : null);
    }

    /// <summary>All matching vocabulary cards for the current spelling of a word (for choosing the meaning).</summary>
    [HttpGet("{exerciseId:int}/words/{wordId:int}/candidates")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<VocabCandidate>>> WordCandidates(
        int subjectId, int chapterId, int exerciseId, int wordId, CancellationToken ct)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId, ct);
        if (exercise is null) return NotFound();

        var config = ConfigOf(exercise);
        var (sentence, index) = FindWord(config, wordId);
        if (index < 0) return NotFound();

        var lookups = await decoder.LookupAsync(config.LearningLang, config.NativeLang, sentence.Decoding[index].LearningWord, ct);
        return lookups.Count == 0 ? new List<VocabCandidate>() : lookups[0].Candidates.Select(ToCandidate).ToList();
    }

    /// <summary>Removes a sentence from the exercise.</summary>
    [HttpDelete("{exerciseId:int}/sentences/{sentenceId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSentence(
        int subjectId, int chapterId, int exerciseId, int sentenceId, CancellationToken ct)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId, ct);
        if (exercise is null) return NotFound();
        if (EnsureCanWrite(exercise) is { } forbidden) return forbidden;

        var config = ConfigOf(exercise);
        var removed = config.Sentences.RemoveAll(s => s.SentenceId == sentenceId);
        if (removed == 0) return NotFound();
        SetConfig(exercise, config);
        await Db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Stateless preview: decodes a sentence via the vocabulary store and returns the word tuples,
    /// <b>without</b> saving anything (IDs are <c>0</c> here). Handy to check before creating which
    /// words already exist in the store.
    /// </summary>
    [HttpPost("~/" + ApiRoutes.Creator + "/birkenbihl/decode")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DecodedSentence>> Decode(DecodePreviewInput body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.LearningSentence)) return this.ProblemWithCode(ApiErrors.ValidationError, "The sentence in the learning language is required.");

        var lookups = await decoder.LookupAsync(body.LearningLang ?? "", body.NativeLang ?? "", body.LearningSentence, ct);
        var words = lookups.Select(t =>
            new DecodedWord(0, t.Surface, t.Best?.Translation, t.Best?.Id, VocabLink.Self(t.Best?.Id), CandidatesOf(t))).ToList();
        return new DecodedSentence(0, body.LearningSentence.Trim(), (body.NaturalTranslation ?? "").Trim(), words);
    }

    // Findet ein Wort übungsweit über seine wordId; liefert den Satz und den Index im Decoding (-1 = nicht gefunden).
    private static (BirkenbihlSentence Sentence, int Index) FindWord(BirkenbihlConfig config, int wordId)
    {
        foreach (var s in config.Sentences)
        {
            var idx = s.Decoding.FindIndex(w => w.WordId == wordId);
            if (idx >= 0) return (s, idx);
        }
        return (null!, -1);
    }
}

/// <summary>Fixed arithmetic problems (manually maintained list). <see cref="Check"/> evaluates the answers.</summary>
[Route(ExerciseRoutes.Base + "/arithmetic")]
[Tags("Creator – Arithmetic")]
public class ArithmeticController(PuglingDbContext db, ExerciseTypeRegistry registry)
    : ExerciseControllerBase<ArithmeticConfig>(db, registry)
{
    /// <inheritdoc/>
    protected override string TypeKey => ExerciseTypeKeys.Arithmetic;

    /// <summary>Evaluates the child's solutions against the stored problems (numeric, with tolerance).</summary>
    [HttpPost("{exerciseId:int}/check")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<CheckResult>> Check(int subjectId, int chapterId, int exerciseId, CheckDto body, CancellationToken ct = default) =>
        RunCheckAsync(subjectId, chapterId, exerciseId, body, ct);
}

/// <summary>
/// Random arithmetic problems: the rules (config) are stored, the concrete problems are delivered by
/// <see cref="Generate"/> on demand. <see cref="Check"/> regenerates the set from the same seed
/// and evaluates it – this keeps the check server-side. The controller inherits the CRUD of the rules.
/// </summary>
[Route(ExerciseRoutes.Base + "/arithmetic-drill")]
[Tags("Creator – Arithmetic Drill")]
public class ArithmeticDrillController(PuglingDbContext db, ExerciseTypeRegistry registry)
    : ExerciseControllerBase<ArithmeticDrillConfig>(db, registry)
{
    /// <inheritdoc/>
    protected override string TypeKey => ExerciseTypeKeys.ArithmeticDrill;

    private IGeneratingExerciseType DrillType => (IGeneratingExerciseType)Registry.Require(TypeKey);

    /// <summary>
    /// Generates a random set according to the stored rules. The used
    /// <c>Seed</c> is also returned – send it along with the later <see cref="Check"/>, so exactly this set is evaluated.
    /// </summary>
    [HttpPost("{exerciseId:int}/generate")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GeneratedDrill>> Generate(int subjectId, int chapterId, int exerciseId, [FromQuery] int? seed, CancellationToken ct = default)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId, ct);
        if (exercise is null) return NotFound();
        if (ArithmeticDrillExerciseType.Validate(ConfigOf(exercise)) is { } error) return this.ProblemWithCode(ApiErrors.ValidationError, error);

        var (effectiveSeed, problems) = DrillType.Generate(exercise.ConfigJson, seed);
        return new GeneratedDrill(exercise.Id, exercise.Title, effectiveSeed, problems);
    }

    /// <summary>Evaluates a previously generated set: the same <c>Seed</c> is regenerated and checked.</summary>
    [HttpPost("{exerciseId:int}/check")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CheckResult>> Check(int subjectId, int chapterId, int exerciseId, CheckDto body, CancellationToken ct = default)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId, ct);
        if (exercise is null) return NotFound();
        if (ArithmeticDrillExerciseType.Validate(ConfigOf(exercise)) is { } error) return this.ProblemWithCode(ApiErrors.ValidationError, error);

        return DrillType.Check(exercise.ConfigJson, body.Answers, body.Seed) is { } result
            ? result
            : this.ProblemWithCode(ApiErrors.ValidationError, "The seed of the generated problem must be provided for evaluation.");
    }
}

/// <summary>Lists to be memorized (e.g. the federal states). <see cref="Check"/> counts the given entries.</summary>
[Route(ExerciseRoutes.Base + "/list")]
[Tags("Creator – List")]
public class ListController(PuglingDbContext db, ExerciseTypeRegistry registry)
    : ExerciseControllerBase<ListConfig>(db, registry)
{
    /// <inheritdoc/>
    protected override string TypeKey => ExerciseTypeKeys.List;

    /// <summary>Evaluates the given entries – as a set, or position-exact with <c>Ordered</c>.</summary>
    [HttpPost("{exerciseId:int}/check")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<CheckResult>> Check(int subjectId, int chapterId, int exerciseId, CheckDto body, CancellationToken ct = default) =>
        RunCheckAsync(subjectId, chapterId, exerciseId, body, ct);
}
