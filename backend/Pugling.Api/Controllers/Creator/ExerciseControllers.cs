using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Creator;

// Ein Controller je Übungstyp. Jeder erbt die gemeinsame CRUD-Logik aus
// ExerciseControllerBase<TConfig> und legt nur Route, Tag und Type fest.
// Dadurch bekommt jeder Typ einen eigenen Pfad und ein eigenes Config-Schema in Swagger.

/// <summary>Gemeinsames Routen-Präfix aller Übungstypen.</summary>
internal static class ExerciseRoutes
{
    public const string Base = ApiRoutes.Creator + "/subjects/{subjectId:int}/chapters/{chapterId:int}";
}

/// <summary>Antworten des Kindes, positionsbezogen (Index in der Aufgaben-/Paarliste).</summary>
public record CheckAnswersDto(List<GivenAnswer> Answers);
/// <summary>Antworten zu einem generierten Drill; <paramref name="Seed"/> muss der beim Generieren genutzte sein.</summary>
public record CheckDrillDto(int? Seed, List<GivenAnswer> Answers);
/// <summary>Antworten für eine Liste: die genannten Einträge in der eingegebenen Reihenfolge.</summary>
public record CheckListDto(List<string?> Answers);

/// <summary>
/// Vokabelübungen. Die Übung selbst beschreibt Art/Ziel/Wert; ihre Vokabelpaare leben eine Ebene tiefer als
/// stabil identifizierte <see cref="ExerciseItem"/>s (CRUD unter <c>{exerciseId}/items/{itemId}</c>). Beim Anlegen
/// akzeptiert der POST weiterhin inline <see cref="VocabItem"/>/<see cref="VocabRef"/> im Payload und materialisiert
/// sie in die Item-Tabelle; jede genutzte Vokabel wird dabei im Store angelegt/verknüpft.
/// </summary>
[Route(ExerciseRoutes.Base + "/vocabulary")]
[Tags("Creator – Vocabulary")]
public class VocabularyController(PuglingDbContext db, ExerciseItemService items, VocabularyStoreService store)
    : ExerciseControllerBase<VocabularyConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.Vocabulary;

    /// <summary>
    /// Sichert beim Anlegen/Ändern zu, dass alle per ID referenzierten Store-Einträge existieren und – falls
    /// Inline-Vokabeln ohne ID vorliegen – die Sprachcodes gesetzt sind (nötig, um sie im Store anzulegen).
    /// </summary>
    protected override async Task<string?> ValidateConfigAsync(int subjectId, VocabularyConfig config)
    {
        if (config.Refs is { Count: > 0 } refs)
        {
            if (refs.Any(r => r.VocabularyId <= 0))
                return "Every reference needs a valid vocabularyId (> 0).";
            var ids = refs.Select(r => r.VocabularyId).Distinct().ToList();
            var existing = await Db.Vocabulary.Where(v => ids.Contains(v.Id)).Select(v => v.Id).ToListAsync();
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
            var existing = await Db.Vocabulary.Where(v => itemIds.Contains(v.Id)).Select(v => v.Id).ToListAsync();
            var missing = itemIds.Except(existing).ToList();
            if (missing.Count > 0) return $"Unknown vocabulary item IDs: {string.Join(", ", missing)}";
        }
        return null;
    }

    /// <summary>
    /// Materialisiert die Items der Übung nach dem Speichern in die <see cref="ExerciseItem"/>-Tabelle (stabile ItemIds):
    /// beim POST aus dem Payload, beim PUT nur, wenn der Payload überhaupt Items/Refs trägt (ein reiner Einstellungs-PUT
    /// lässt die per <c>/items</c> gepflegte Item-Menge unangetastet). Der Abgleich bewahrt die Id überlebender Wörter.
    /// Anschließend wird die Config auf reine Einstellungen reduziert – Items/Refs sind ab jetzt die Tabelle (eine Quelle).
    /// </summary>
    protected override async Task AfterSaveAsync(Exercise exercise, VocabularyConfig config, bool isCreate)
    {
        var hasPayloadItems = config.Items.Count > 0 || config.Refs is { Count: > 0 };
        if (isCreate || hasPayloadItems)
            await items.SyncFromConfigAsync(exercise.Id, config);
        if (hasPayloadItems)
        {
            config.Items = [];
            config.Refs = null;
            SetConfig(exercise, config);
            await Db.SaveChangesAsync();
        }
    }

    /// <summary>Auswahl der Vokabeln per Tag statt manueller Referenzliste.</summary>
    public record RefsFromTagsDto(List<string> Tags, bool MatchAll = false, bool BaseFormsOnly = false);

    /// <summary>
    /// Setzt die Items der Übung als Snapshot auf die aktuellen Vokabeln der genannten Tags (optional nur
    /// Grundformen, optional alle Tags per UND). Der Vater materialisiert damit „alle Wörter aus Unit 3" – der
    /// Abgleich bewahrt die Id (und den Fortschritt) überlebender Wörter; nur weggefallene verschwinden.
    /// </summary>
    [HttpPost("{exerciseId:int}/refs-from-tags")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseResponse<VocabularyConfig>>> RefsFromTags(
        int subjectId, int chapterId, int exerciseId, RefsFromTagsDto dto)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        if (exercise is null) return NotFound();
        if (EnsureCanModify(exercise) is { } forbidden) return forbidden;

        var tags = (dto.Tags ?? []).Select(t => t.Trim()).Where(t => t.Length > 0).Distinct().ToList();
        if (tags.Count == 0) return this.ProblemWithCode(ApiErrors.ValidationError, "At least one tag is required.");

        var query = Db.Vocabulary.AsNoTracking().AsQueryable();
        if (dto.BaseFormsOnly) query = query.Where(v => v.BaseFormId == null);
        if (dto.MatchAll)
            foreach (var name in tags) query = query.Where(v => v.TagLinks.Any(l => l.VocabTag!.Name == name));
        else
            query = query.Where(v => v.TagLinks.Any(l => tags.Contains(l.VocabTag!.Name)));
        var hitIds = await query.OrderBy(v => v.Key).Select(v => v.Id).ToListAsync();

        await items.ReconcileAsync(exercise.Id, hitIds.Select(id => new DesiredItem(id, null)).ToList());
        return Map(exercise, User.FatherId());
    }

    // ---- Einzel-Items (Vokabelpaare) als eigene Sub-Ressource -----------------------------------------

    /// <summary>Ein einzelnes Vokabelpaar der Übung. Front/Rückseite kommen aus dem verknüpften Store-Eintrag.</summary>
    /// <param name="Id">Stabile Item-Id (ItemId).</param>
    /// <param name="OrderIndex">Sortierschlüssel innerhalb der Übung.</param>
    /// <param name="VocabularyId">Verknüpfter Vokabel-Store-Eintrag.</param>
    /// <param name="Front">Wort der Lernsprache (aus dem Store).</param>
    /// <param name="Back">Übersetzung (aus dem Store).</param>
    /// <param name="Hint">Übungslokaler Hinweis; überschreibt den abgeleiteten Store-Hinweis.</param>
    /// <param name="Self">HATEOAS-Link auf das Item selbst.</param>
    /// <param name="Vocabulary">HATEOAS-Link auf den Store-Eintrag.</param>
    public record VocabItemResponse(int Id, int OrderIndex, int VocabularyId, string Front, string Back, string? Hint,
        [property: JsonPropertyName("_self")] string Self,
        [property: JsonPropertyName("vocabulary")] string Vocabulary);

    /// <summary>
    /// Anlegen/Ändern eines Items: entweder per <paramref name="VocabularyId"/> (bestehende Store-Vokabel) oder inline
    /// per <paramref name="Front"/>/<paramref name="Back"/> (wird im Store angelegt/gefunden). <paramref name="Hint"/>
    /// leer = löschen, gesetzt = überschreiben; beim PATCH bleibt jedes weggelassene Feld unverändert.
    /// </summary>
    public record VocabItemInput(int? VocabularyId = null, string? Front = null, string? Back = null,
        string? Hint = null, int? OrderIndex = null);

    // Konkreter Pfad (wie VocabLink.Path); das Routen-Template ApiRoutes.Creator trägt den Versions-Platzhalter.
    private static string ItemSelf(int subjectId, int chapterId, int exerciseId, int itemId) =>
        $"/api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}/items/{itemId}";

    private static VocabItemResponse MapItem(int subjectId, int chapterId, int exerciseId, ExerciseItem item) =>
        new(item.Id, item.OrderIndex, item.VocabularyId, item.Vocabulary?.Word ?? "", item.Vocabulary?.Translation ?? "",
            item.Hint, ItemSelf(subjectId, chapterId, exerciseId, item.Id), VocabLink.Path + item.VocabularyId);

    /// <summary>Alle Items der Übung in Reihenfolge (Front/Rückseite aus dem Store).</summary>
    [HttpGet("{exerciseId:int}/items")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<VocabItemResponse>>> ListItems(int subjectId, int chapterId, int exerciseId)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        if (exercise is null) return NotFound();
        var rows = await Db.ExerciseItems.AsNoTracking().Include(i => i.Vocabulary)
            .Where(i => i.ExerciseId == exerciseId)
            .OrderBy(i => i.OrderIndex).ThenBy(i => i.Id)
            .ToListAsync();
        return rows.Select(i => MapItem(subjectId, chapterId, exerciseId, i)).ToList();
    }

    /// <summary>Ein einzelnes Item der Übung.</summary>
    [HttpGet("{exerciseId:int}/items/{itemId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VocabItemResponse>> GetItem(int subjectId, int chapterId, int exerciseId, int itemId)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        if (exercise is null) return NotFound();
        var item = await FindItemAsync(exerciseId, itemId);
        return item is null
            ? this.ProblemWithCode(ApiErrors.ItemNotFound, "The exercise item does not exist in this exercise.")
            : MapItem(subjectId, chapterId, exerciseId, item);
    }

    /// <summary>Fügt der Übung ein Vokabelpaar hinzu (per Store-Id oder inline). Neue Items landen ans Ende.</summary>
    [HttpPost("{exerciseId:int}/items")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VocabItemResponse>> AddItem(int subjectId, int chapterId, int exerciseId, VocabItemInput body, CancellationToken ct)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        if (exercise is null) return NotFound();
        if (EnsureCanModify(exercise) is { } forbidden) return forbidden;

        var config = ConfigOf(exercise);
        var resolved = await ResolveVocabularyIdAsync(body, config, ct);
        if (resolved is not { } vocabId) return this.ProblemWithCode(ApiErrors.ValidationError,
            "Provide an existing vocabularyId, or front and back (plus the exercise's sourceLang/targetLang) to create one.");

        // Anfügen ans Ende verschiebt keine bestehenden Positionen (sicher); eine feste Einfügeposition schon.
        if (body.OrderIndex is not null && await ExerciseInPlanAsync(exerciseId)) return ShiftBlockedProblem();
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

        item.Vocabulary = await Db.Vocabulary.FindAsync([vocabId], ct);
        return CreatedAtAction(nameof(GetItem), new { subjectId, chapterId, exerciseId, itemId = item.Id },
            MapItem(subjectId, chapterId, exerciseId, item));
    }

    /// <summary>Ändert ein Item: Vokabel austauschen (per Id oder inline), Hinweis oder Reihenfolge anpassen.</summary>
    [HttpPatch("{exerciseId:int}/items/{itemId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VocabItemResponse>> PatchItem(int subjectId, int chapterId, int exerciseId, int itemId, VocabItemInput body, CancellationToken ct)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        if (exercise is null) return NotFound();
        if (EnsureCanModify(exercise) is { } forbidden) return forbidden;
        var item = await FindItemAsync(exerciseId, itemId);
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
            if (await ExerciseInPlanAsync(exerciseId)) return ShiftBlockedProblem();
            item.OrderIndex = order;
        }
        await Db.SaveChangesAsync(ct);

        item.Vocabulary = await Db.Vocabulary.FindAsync([item.VocabularyId], ct);
        return MapItem(subjectId, chapterId, exerciseId, item);
    }

    /// <summary>Entfernt ein Item aus der Übung.</summary>
    [HttpDelete("{exerciseId:int}/items/{itemId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(int subjectId, int chapterId, int exerciseId, int itemId, CancellationToken ct)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        if (exercise is null) return NotFound();
        if (EnsureCanModify(exercise) is { } forbidden) return forbidden;
        var item = await FindItemAsync(exerciseId, itemId);
        if (item is null) return this.ProblemWithCode(ApiErrors.ItemNotFound, "The exercise item does not exist in this exercise.");
        // Löschen verschiebt Folgepositionen → bei in-Plan gespielter Übung blocken (Fortschritt bliebe fehl-verankert).
        if (await ExerciseInPlanAsync(exerciseId)) return ShiftBlockedProblem();
        Db.ExerciseItems.Remove(item);
        await Db.SaveChangesAsync(ct);
        return NoContent();
    }

    private Task<ExerciseItem?> FindItemAsync(int exerciseId, int itemId) =>
        Db.ExerciseItems.Include(i => i.Vocabulary).FirstOrDefaultAsync(i => i.Id == itemId && i.ExerciseId == exerciseId);

    // Wird die Übung in einem Lehrplan gespielt? Dann verankert PositionItemProgress den Leitner-Fortschritt
    // je Position auf der (positionalen) Item-Reihenfolge – index-verschiebende Item-Mutationen (Löschen,
    // Umsortieren, Einfügen an fester Position) würden gespeicherten Fortschritt aufs falsche Wort umbiegen.
    private Task<bool> ExerciseInPlanAsync(int exerciseId) =>
        Db.PlanPositions.AnyAsync(p => p.ExerciseId == exerciseId);

    private ObjectResult ShiftBlockedProblem() =>
        this.ProblemWithCode(ApiErrors.ExerciseInUse,
            "The exercise is used in a study plan; items cannot be removed or reordered (it would shift saved progress). Adding to the end is allowed; remove it from plans first for other changes.");

    private static string? NormalizeHint(string? hint) =>
        string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();

    /// <summary>
    /// Ermittelt die Ziel-Vokabel eines Item-Eingabe: bevorzugt die gegebene Store-Id (muss existieren), sonst
    /// legt sie Front/Rückseite inline im Store an (braucht die Sprachcodes der Übung). Rückgabe <c>null</c> = unzureichende Eingabe.
    /// </summary>
    private async Task<int?> ResolveVocabularyIdAsync(VocabItemInput body, VocabularyConfig config, CancellationToken ct)
    {
        if (body.VocabularyId is { } id)
            return await Db.Vocabulary.AnyAsync(v => v.Id == id, ct) ? id : null;
        if (string.IsNullOrWhiteSpace(body.Front) || string.IsNullOrWhiteSpace(body.Back)
            || string.IsNullOrWhiteSpace(config.SourceLang) || string.IsNullOrWhiteSpace(config.TargetLang))
            return null;
        var vocab = await store.GetOrCreateAsync(config.SourceLang, body.Front.Trim(), config.TargetLang, body.Back.Trim(), ct: ct);
        await Db.SaveChangesAsync(ct);
        return vocab.Id;
    }
}

/// <summary>Leseverständnis-Übungen.</summary>
[Route(ExerciseRoutes.Base + "/reading")]
[Tags("Creator – Reading")]
public class ReadingController(PuglingDbContext db) : ExerciseControllerBase<ReadingConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.Reading;
}

/// <summary>Lückentext-Übungen. Lücken dürfen per <see cref="Gap.VocabKey"/> den Vokabel-Store referenzieren.</summary>
[Route(ExerciseRoutes.Base + "/cloze")]
[Tags("Creator – Cloze")]
public class ClozeController(PuglingDbContext db) : ExerciseControllerBase<ClozeConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.Cloze;

    /// <summary>Sichert beim Anlegen/Ändern zu, dass alle in Lücken referenzierten Store-Keys existieren.</summary>
    protected override async Task<string?> ValidateConfigAsync(int subjectId, ClozeConfig config)
    {
        var keys = config.Gaps.Where(g => !string.IsNullOrWhiteSpace(g.VocabKey))
            .Select(g => g.VocabKey!).Distinct().ToList();
        if (keys.Count == 0) return null;
        var existing = await Db.Vocabulary.Where(v => keys.Contains(v.Key)).Select(v => v.Key).ToListAsync();
        var missing = keys.Except(existing).ToList();
        return missing.Count == 0 ? null : $"Unknown vocabulary keys in gaps: {string.Join(", ", missing)}";
    }
}

/// <summary>Aufsatz-Übungen.</summary>
[Route(ExerciseRoutes.Base + "/essays")]
[Tags("Creator – Essays")]
public class EssaysController(PuglingDbContext db) : ExerciseControllerBase<EssayConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.Essay;
}

/// <summary>Hörverständnis-Übungen.</summary>
[Route(ExerciseRoutes.Base + "/listening")]
[Tags("Creator – Listening")]
public class ListeningController(PuglingDbContext db) : ExerciseControllerBase<ListeningConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.Listening;
}

/// <summary>Grammatik-Übungen.</summary>
[Route(ExerciseRoutes.Base + "/grammar")]
[Tags("Creator – Grammar")]
public class GrammarController(PuglingDbContext db) : ExerciseControllerBase<GrammarConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.Grammar;
}

/// <summary>Zuordnungs-Übungen (Paare). Neben dem CRUD bewertet <see cref="Check"/> die genannten Zuordnungen.</summary>
[Route(ExerciseRoutes.Base + "/matching")]
[Tags("Creator – Matching")]
public class MatchingController(PuglingDbContext db, ExerciseAnswerChecker checker)
    : ExerciseControllerBase<MatchingConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.Matching;

    /// <summary>Wertet die Zuordnungen aus: je Paar zählt die zur linken Seite genannte rechte Seite.</summary>
    [HttpPost("{exerciseId:int}/check")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CheckResult>> Check(int subjectId, int chapterId, int exerciseId, CheckAnswersDto body)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        return exercise is null ? NotFound() : checker.CheckMatching(ConfigOf(exercise), body.Answers);
    }

}

/// <summary>
/// Übersetzungs-Übungen. Jedes Übersetzungspaar ohne <see cref="TranslationItem.VocabularyId"/> wird beim Speichern
/// automatisch im Store angelegt und verknüpft; die Antwort ergänzt je Paar den Link <c>_self</c>.
/// </summary>
[Route(ExerciseRoutes.Base + "/translation")]
[Tags("Creator – Translation")]
public class TranslationController(PuglingDbContext db, VocabularyStoreService store) : ExerciseControllerBase<TranslationConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.Translation;

    /// <summary>Verpflichtet die Sprachcodes, sobald Paare ohne <see cref="TranslationItem.VocabularyId"/> anzulegen sind.</summary>
    protected override Task<string?> ValidateConfigAsync(int subjectId, TranslationConfig config) =>
        Task.FromResult(config.Items.Any(i => i.VocabularyId is null)
            && (string.IsNullOrWhiteSpace(config.SourceLang) || string.IsNullOrWhiteSpace(config.TargetLang))
            ? "sourceLang and targetLang are required to create translation pairs in the store."
            : null);

    /// <summary>Legt jedes noch nicht verknüpfte Paar im Store an (bzw. findet es) und verknüpft es per ID.</summary>
    protected override async Task NormalizeConfigAsync(int subjectId, TranslationConfig config)
    {
        var pending = new List<(int Index, Vocabulary Vocab)>();
        for (var i = 0; i < config.Items.Count; i++)
        {
            var item = config.Items[i];
            if (item.VocabularyId is not null) continue;
            pending.Add((i, await store.GetOrCreateAsync(config.SourceLang, item.Source, config.TargetLang, item.Target)));
        }
        if (pending.Count == 0) return;
        await Db.SaveChangesAsync();
        foreach (var (index, vocab) in pending)
            config.Items[index] = config.Items[index] with { VocabularyId = vocab.Id };
    }

    /// <summary>Ergänzt je Übersetzungspaar den abgeleiteten Selbstlink <c>_self</c> (nicht persistiert).</summary>
    protected override TranslationConfig ConfigForResponse(Exercise exercise)
    {
        var config = ConfigOf(exercise);
        for (var i = 0; i < config.Items.Count; i++)
            config.Items[i] = config.Items[i] with { Self = VocabLink.Self(config.Items[i].VocabularyId) };
        return config;
    }
}

/// <summary>Ein austauschbarer Vokabel-Kandidat für ein Wort (bei Homonymen mehrere).</summary>
/// <param name="VocabularyId">Vokabel-Id.</param>
/// <param name="Word">Wort in der Lernsprache.</param>
/// <param name="Translation">Muttersprachliche Glosse dieser Bedeutung.</param>
/// <param name="PartOfSpeech">Wortart (hilft beim Unterscheiden gleicher Schreibweisen).</param>
/// <param name="Self">Link auf die Vokabelkarte (<c>_self</c>).</param>
public record VocabCandidate(int VocabularyId, string Word, string Translation, string PartOfSpeech,
    [property: JsonPropertyName("_self")] string Self);

/// <summary>
/// Ein dekodiertes Wort der Ausgabe: <paramref name="LearningWord"/> der Lernsprache → wörtliche Glosse
/// <paramref name="Gloss"/>. <paramref name="Gloss"/>/<paramref name="VocabularyId"/>/<paramref name="Self"/>
/// sind <c>null</c>, wenn das Wort (noch) nicht im Vokabelspeicher liegt. <paramref name="Candidates"/> ist nur
/// bei mehrdeutigen Wörtern gefüllt (mehrere passende Karten – der Vater kann per Wort-Endpunkt die richtige wählen).
/// </summary>
public record DecodedWord(int WordId, string LearningWord, string? Gloss, int? VocabularyId,
    [property: JsonPropertyName("_self")] string? Self, IReadOnlyList<VocabCandidate>? Candidates);

/// <summary>Ein dekodierter Satz: Original + natürliche Übersetzung + die Wort-für-Wort-Tuple.</summary>
public record DecodedSentence(int SentenceId, string LearningSentence, string NaturalTranslation, IReadOnlyList<DecodedWord> Result);

/// <summary>Eingabe zum Hinzufügen eines Satzes: der Satz der Lernsprache + seine natürliche, korrekte Übersetzung.</summary>
public record BirkenbihlSentenceInput(string LearningSentence, string NaturalTranslation);

/// <summary>
/// Korrektur eines einzelnen Worts. <paramref name="VocabularyId"/> gesetzt → die Glosse folgt dieser Karte
/// (richtige Bedeutung bei Homonymen). Nur <paramref name="Gloss"/> gesetzt → freie Glosse ohne Karte. Beides
/// leer → Glosse entfernen (Wort bleibt undekodiert).
/// </summary>
public record WordOverride(int? VocabularyId, string? Gloss);

/// <summary>Eingabe der zustandslosen Vorschau: Sprachen + der zu dekodierende Satz samt Übersetzung.</summary>
public record DecodePreviewInput(string LearningLang, string NativeLang, string LearningSentence, string NaturalTranslation);

/// <summary>
/// Birkenbihl-Methode: Texte in der Lernsprache mit grammatik-unabhängiger Wort-für-Wort-Dekodierung
/// plus natürlicher Übersetzung. Reine Inhaltsübung zum Lesen/Hören – bewusst ohne <c>/check</c>, da das
/// Verfahren nicht aktiv abfragt. Neben dem geerbten CRUD (Übung + Sprachen + Sätze en bloc) bietet der
/// Controller die vokabel-gestützte Automatik: Sätze werden Wort für Wort im gemeinsamen Vokabelspeicher
/// nachgeschlagen und einzeln korrigierbar (Homonyme).
/// </summary>
[Route(ExerciseRoutes.Base + "/birkenbihl")]
[Tags("Creator – Birkenbihl")]
public class BirkenbihlController(PuglingDbContext db, BirkenbihlDecodingService decoder, VocabularyStoreService store)
    : ExerciseControllerBase<BirkenbihlConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.Birkenbihl;

    /// <summary>Ergänzt je dekodiertem Wort den abgeleiteten Selbstlink <c>_self</c> aus seiner Vokabel-ID (nicht persistiert).</summary>
    protected override BirkenbihlConfig ConfigForResponse(Exercise exercise)
    {
        var config = ConfigOf(exercise);
        foreach (var s in config.Sentences)
            for (var i = 0; i < s.Decoding.Count; i++)
                s.Decoding[i] = s.Decoding[i] with { Self = VocabLink.Self(s.Decoding[i].VocabularyId) };
        return config;
    }

    /// <summary>
    /// Vergibt fehlende (≤ 0) Satz-/Wort-IDs beim Speichern über das generische CRUD: Das Anlege-Formular
    /// liefert die Sätze ohne IDs, die vokabel-gestützten Zusatz-Endpunkte (<c>.../words/{wordId}</c>) brauchen
    /// aber übungsweit eindeutige IDs. Bereits vergebene IDs bleiben erhalten – so kollidiert nichts.
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
    /// Dekodiert einen Satz automatisch über den Vokabelspeicher und <b>speichert</b> ihn in der Übung.
    /// Jedes Wort erhält eine übungsweit eindeutige <c>wordId</c> (→ später einzeln austauschbar), der Satz
    /// eine <c>sentenceId</c>. Unbekannte Wörter kommen mit leerer Glosse zurück; mehrdeutige mit Kandidaten.
    /// </summary>
    [HttpPost("{exerciseId:int}/sentences")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecodedSentence>> AddSentence(
        int subjectId, int chapterId, int exerciseId, BirkenbihlSentenceInput body, CancellationToken ct)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        if (exercise is null) return NotFound();
        if (EnsureCanModify(exercise) is { } forbidden) return forbidden;
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
    /// Tauscht die Bedeutung eines einzelnen Worts aus (Homonym-Korrektur). Mit <c>vocabularyId</c> folgt die
    /// Glosse der gewählten Karte; nur mit <c>gloss</c> wird eine freie Glosse ohne Karte gesetzt.
    /// </summary>
    [HttpPut("{exerciseId:int}/words/{wordId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecodedWord>> SetWord(
        int subjectId, int chapterId, int exerciseId, int wordId, WordOverride body, CancellationToken ct)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        if (exercise is null) return NotFound();
        if (EnsureCanModify(exercise) is { } forbidden) return forbidden;

        var config = ConfigOf(exercise);
        var (sentence, index) = FindWord(config, wordId);
        if (index < 0) return NotFound();
        var word = sentence.Decoding[index];

        WordPair updated;
        if (body.VocabularyId is { } vocabId)
        {
            // Karte muss existieren und zum Sprachpaar der Übung passen – sonst würde eine fremde Glosse gesetzt.
            var card = await Db.Vocabulary.AsNoTracking()
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

    /// <summary>Alle passenden Vokabelkarten zur aktuellen Schreibweise eines Worts (für die Auswahl der Bedeutung).</summary>
    [HttpGet("{exerciseId:int}/words/{wordId:int}/candidates")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<VocabCandidate>>> WordCandidates(
        int subjectId, int chapterId, int exerciseId, int wordId, CancellationToken ct)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        if (exercise is null) return NotFound();

        var config = ConfigOf(exercise);
        var (sentence, index) = FindWord(config, wordId);
        if (index < 0) return NotFound();

        var lookups = await decoder.LookupAsync(config.LearningLang, config.NativeLang, sentence.Decoding[index].LearningWord, ct);
        return lookups.Count == 0 ? new List<VocabCandidate>() : lookups[0].Candidates.Select(ToCandidate).ToList();
    }

    /// <summary>Entfernt einen Satz aus der Übung.</summary>
    [HttpDelete("{exerciseId:int}/sentences/{sentenceId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSentence(
        int subjectId, int chapterId, int exerciseId, int sentenceId, CancellationToken ct)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        if (exercise is null) return NotFound();
        if (EnsureCanModify(exercise) is { } forbidden) return forbidden;

        var config = ConfigOf(exercise);
        var removed = config.Sentences.RemoveAll(s => s.SentenceId == sentenceId);
        if (removed == 0) return NotFound();
        SetConfig(exercise, config);
        await Db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Zustandslose Vorschau: dekodiert einen Satz über den Vokabelspeicher und gibt die Wort-Tuple zurück,
    /// <b>ohne</b> etwas zu speichern (IDs sind hier <c>0</c>). Praktisch, um vor dem Anlegen zu prüfen, welche
    /// Wörter schon im Speicher liegen.
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

/// <summary>Feste Rechenaufgaben (manuell gepflegte Liste). <see cref="Check"/> wertet die Antworten aus.</summary>
[Route(ExerciseRoutes.Base + "/arithmetic")]
[Tags("Creator – Arithmetic")]
public class ArithmeticController(PuglingDbContext db, ExerciseAnswerChecker checker)
    : ExerciseControllerBase<ArithmeticConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.Arithmetic;

    /// <summary>Bewertet die Lösungen des Kindes gegen die hinterlegten Aufgaben (numerisch, mit Toleranz).</summary>
    [HttpPost("{exerciseId:int}/check")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CheckResult>> Check(int subjectId, int chapterId, int exerciseId, CheckAnswersDto body)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        return exercise is null ? NotFound() : checker.CheckArithmetic(ConfigOf(exercise), body.Answers);
    }
}

/// <summary>
/// Zufalls-Rechenaufgaben: Gespeichert werden die Regeln (Config), die konkreten Aufgaben liefert
/// <see cref="Generate"/> auf Abruf. <see cref="Check"/> erzeugt den Satz aus demselben Seed erneut
/// und bewertet ihn – dadurch bleibt die Prüfung serverseitig. Das CRUD der Regeln erbt der Controller.
/// </summary>
[Route(ExerciseRoutes.Base + "/arithmetic-drill")]
[Tags("Creator – Arithmetic Drill")]
public class ArithmeticDrillController(PuglingDbContext db, ArithmeticProblemGenerator generator, ExerciseAnswerChecker checker)
    : ExerciseControllerBase<ArithmeticDrillConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.ArithmeticDrill;

    /// <summary>Ein frisch erzeugter Aufgabensatz zu einer Drill-Übung.</summary>
    public record GeneratedDrill(int ExerciseId, string Title, int Seed, IReadOnlyList<GeneratedProblem> Problems);

    /// <summary>Prüft die Config-Grenzen; gibt die Fehlermeldung zurück oder null, wenn alles passt.</summary>
    private static string? Validate(ArithmeticDrillConfig c) =>
        c.Operations.Count == 0 ? "At least one operation type is required."
        : c.MaxOperand < c.MinOperand ? "MaxOperand must be ≥ MinOperand."
        : c.ProblemCount is < 1 or > 100 ? "ProblemCount must be between 1 and 100."
        : null;

    /// <summary>
    /// Erzeugt einen Zufallssatz nach den gespeicherten Regeln. Zurückgegeben wird auch der verwendete
    /// <c>Seed</c> – ihn beim späteren <see cref="Check"/> mitschicken, damit exakt dieser Satz bewertet wird.
    /// </summary>
    [HttpPost("{exerciseId:int}/generate")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GeneratedDrill>> Generate(int subjectId, int chapterId, int exerciseId, [FromQuery] int? seed)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        if (exercise is null) return NotFound();
        var config = ConfigOf(exercise);
        if (Validate(config) is { } error) return this.ProblemWithCode(ApiErrors.ValidationError, error);

        // Wir fixieren den Seed (auch bei „echtem" Zufall), damit der Satz später auswertbar bleibt.
        int effectiveSeed = seed ?? config.Seed ?? Random.Shared.Next();
        var problems = generator.Generate(config, new Random(effectiveSeed));
        return new GeneratedDrill(exercise.Id, exercise.Title, effectiveSeed, problems);
    }

    /// <summary>Wertet einen zuvor generierten Satz aus: derselbe <c>Seed</c> wird erneut erzeugt und geprüft.</summary>
    [HttpPost("{exerciseId:int}/check")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CheckResult>> Check(int subjectId, int chapterId, int exerciseId, CheckDrillDto body)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        if (exercise is null) return NotFound();
        var config = ConfigOf(exercise);
        if (Validate(config) is { } error) return this.ProblemWithCode(ApiErrors.ValidationError, error);
        if ((body.Seed ?? config.Seed) is not { } seed)
            return this.ProblemWithCode(ApiErrors.ValidationError, "The seed of the generated problem must be provided for evaluation.");

        var problems = generator.Generate(config, new Random(seed));
        return checker.CheckGenerated(problems, body.Answers);
    }
}

/// <summary>Auswendig zu lernende Listen (z. B. die Bundesländer). <see cref="Check"/> zählt die genannten Einträge.</summary>
[Route(ExerciseRoutes.Base + "/list")]
[Tags("Creator – List")]
public class ListController(PuglingDbContext db, ExerciseAnswerChecker checker)
    : ExerciseControllerBase<ListConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.List;

    /// <summary>Bewertet die genannten Einträge – als Menge, oder positionsgenau bei <c>Ordered</c>.</summary>
    [HttpPost("{exerciseId:int}/check")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CheckResult>> Check(int subjectId, int chapterId, int exerciseId, CheckListDto body)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        return exercise is null ? NotFound() : checker.CheckList(ConfigOf(exercise), body.Answers);
    }
}
