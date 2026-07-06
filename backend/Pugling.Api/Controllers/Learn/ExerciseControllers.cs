using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Controllers.Learn;

// Ein Controller je Übungstyp. Jeder erbt die gemeinsame CRUD-Logik aus
// ExerciseControllerBase<TConfig> und legt nur Route, Tag und Type fest.
// Dadurch bekommt jeder Typ einen eigenen Pfad und ein eigenes Config-Schema in Swagger.

/// <summary>Gemeinsames Routen-Präfix aller Übungstypen.</summary>
internal static class ExerciseRoutes
{
    public const string Base = ApiRoutes.V1 + "/learn/subjects/{subjectId:int}/chapters/{chapterId:int}";
}

/// <summary>Antworten des Kindes, positionsbezogen (Index in der Aufgaben-/Paarliste).</summary>
public record CheckAnswersDto(List<GivenAnswer> Answers);
/// <summary>Antworten zu einem generierten Drill; <paramref name="Seed"/> muss der beim Generieren genutzte sein.</summary>
public record CheckDrillDto(int? Seed, List<GivenAnswer> Answers);
/// <summary>Antworten für eine Liste: die genannten Einträge in der eingegebenen Reihenfolge.</summary>
public record CheckListDto(List<string?> Answers);

/// <summary>
/// Vokabelübungen. Referenzieren den Vokabel-Store per <see cref="VocabularyConfig.Refs"/> (ID); Inline-<see cref="VocabItem"/>
/// ohne ID werden beim Speichern automatisch im Store angelegt und verknüpft. Die Antwort ergänzt je Vokabel den Link <c>_self</c>.
/// </summary>
[Route(ExerciseRoutes.Base + "/vocabulary")]
[Tags("Learn – Vocabulary")]
public class VocabularyController(PuglingDbContext db, VocabularyStoreService store) : ExerciseControllerBase<VocabularyConfig>(db)
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
        if (config.Items.Any(i => i.VocabularyId is null)
            && (string.IsNullOrWhiteSpace(config.SourceLang) || string.IsNullOrWhiteSpace(config.TargetLang)))
            return "sourceLang and targetLang are required to create inline vocabulary items in the store.";
        return null;
    }

    /// <summary>
    /// Legt jede Inline-Vokabel ohne <see cref="VocabItem.VocabularyId"/> im Store an (bzw. findet sie) und verknüpft sie,
    /// damit garantiert jede genutzte Vokabel im Store liegt. Ergänzt außerdem fehlende <see cref="VocabRef.Key"/> als Lesehilfe.
    /// </summary>
    protected override async Task NormalizeConfigAsync(int subjectId, VocabularyConfig config)
    {
        // Inline-Items ohne ID anlegen/finden; IDs materialisieren sich erst nach SaveChanges → danach zurückschreiben.
        var pending = new List<(int Index, Vocabulary Vocab)>();
        for (var i = 0; i < config.Items.Count; i++)
        {
            var item = config.Items[i];
            if (item.VocabularyId is not null) continue;
            pending.Add((i, await store.GetOrCreateAsync(config.SourceLang, item.Front, config.TargetLang, item.Back)));
        }
        if (pending.Count > 0)
        {
            await Db.SaveChangesAsync();
            foreach (var (index, vocab) in pending)
                config.Items[index] = config.Items[index] with { VocabularyId = vocab.Id };
        }

        // Refs: fehlenden Key aus der ID nachziehen (dient der Lesbarkeit/Usage-Suche; _self kommt erst in der Antwort).
        if (config.Refs is { Count: > 0 } refs)
        {
            var need = refs.Where(r => r.Key is null && r.VocabularyId > 0).Select(r => r.VocabularyId).Distinct().ToList();
            if (need.Count > 0)
            {
                var keyById = await Db.Vocabulary.Where(v => need.Contains(v.Id)).ToDictionaryAsync(v => v.Id, v => v.Key);
                for (var i = 0; i < refs.Count; i++)
                    if (refs[i].Key is null && keyById.TryGetValue(refs[i].VocabularyId, out var k))
                        refs[i] = refs[i] with { Key = k };
            }
        }
    }

    /// <summary>Ergänzt je Referenz und Inline-Vokabel den abgeleiteten Selbstlink <c>_self</c> (nicht persistiert).</summary>
    protected override VocabularyConfig ConfigForResponse(Exercise exercise)
    {
        var config = ConfigOf(exercise);
        if (config.Refs is { Count: > 0 } refs)
            for (var i = 0; i < refs.Count; i++)
                refs[i] = refs[i] with { Self = VocabLink.Self(refs[i].VocabularyId) };
        for (var i = 0; i < config.Items.Count; i++)
            config.Items[i] = config.Items[i] with { Self = VocabLink.Self(config.Items[i].VocabularyId) };
        return config;
    }

    /// <summary>Auswahl der Vokabeln per Tag statt manueller Referenzliste.</summary>
    public record RefsFromTagsDto(List<string> Tags, bool MatchAll = false, bool BaseFormsOnly = false);

    /// <summary>
    /// Füllt <see cref="VocabularyConfig.Refs"/> der Übung mit den aktuellen Vokabeln der genannten Tags
    /// (optional nur Grundformen, optional alle Tags per UND). Der Vater materialisiert damit „alle Wörter
    /// aus Unit 3" als Snapshot – so bleibt der Leitner-Fortschritt stabil (die Auffrischung erfolgt bewusst).
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
        var hits = await query.OrderBy(v => v.Key).Select(v => new { v.Id, v.Key }).ToListAsync();

        var config = ConfigOf(exercise);
        config.Refs = hits.Select(h => new VocabRef(h.Id, h.Key)).ToList();
        SetConfig(exercise, config);
        await Db.SaveChangesAsync();
        return Map(exercise, User.FatherId());
    }
}

/// <summary>Leseverständnis-Übungen.</summary>
[Route(ExerciseRoutes.Base + "/reading")]
[Tags("Learn – Reading")]
public class ReadingController(PuglingDbContext db) : ExerciseControllerBase<ReadingConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.Reading;
}

/// <summary>Lückentext-Übungen. Lücken dürfen per <see cref="Gap.VocabKey"/> den Vokabel-Store referenzieren.</summary>
[Route(ExerciseRoutes.Base + "/cloze")]
[Tags("Learn – Cloze")]
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
[Tags("Learn – Essays")]
public class EssaysController(PuglingDbContext db) : ExerciseControllerBase<EssayConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.Essay;
}

/// <summary>Hörverständnis-Übungen.</summary>
[Route(ExerciseRoutes.Base + "/listening")]
[Tags("Learn – Listening")]
public class ListeningController(PuglingDbContext db) : ExerciseControllerBase<ListeningConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.Listening;
}

/// <summary>Grammatik-Übungen.</summary>
[Route(ExerciseRoutes.Base + "/grammar")]
[Tags("Learn – Grammar")]
public class GrammarController(PuglingDbContext db) : ExerciseControllerBase<GrammarConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.Grammar;
}

/// <summary>Zuordnungs-Übungen (Paare). Neben dem CRUD bewertet <see cref="Check"/> die genannten Zuordnungen.</summary>
[Route(ExerciseRoutes.Base + "/matching")]
[Tags("Learn – Matching")]
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
[Tags("Learn – Translation")]
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
[Tags("Learn – Birkenbihl")]
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
    [HttpPost("~/" + ApiRoutes.V1 + "/learn/birkenbihl/decode")]
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
[Tags("Learn – Arithmetic")]
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
[Tags("Learn – Arithmetic Drill")]
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
[Tags("Learn – List")]
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
