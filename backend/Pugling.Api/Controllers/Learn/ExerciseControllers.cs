using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
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

/// <summary>Vokabelübungen. Referenzieren den Vokabel-Store per <see cref="VocabularyConfig.Refs"/> (Keys).</summary>
[Route(ExerciseRoutes.Base + "/vocabulary")]
[Tags("Learn – Vocabulary")]
public class VocabularyController(PuglingDbContext db) : ExerciseControllerBase<VocabularyConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.Vocabulary;

    /// <summary>Sichert beim Anlegen/Ändern zu, dass alle referenzierten Store-Keys existieren.</summary>
    protected override async Task<string?> ValidateConfigAsync(int subjectId, VocabularyConfig config)
    {
        if (config.Refs is not { Count: > 0 } refs) return null;
        var keys = refs.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToList();
        var existing = await Db.Vocabulary.Where(v => keys.Contains(v.Key)).Select(v => v.Key).ToListAsync();
        var missing = keys.Except(existing).ToList();
        return missing.Count == 0 ? null : $"Unbekannte Vokabel-Keys: {string.Join(", ", missing)}";
    }

    /// <summary>Auswahl der Vokabeln per Tag statt manueller Key-Liste.</summary>
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
        if (tags.Count == 0) return Problem(statusCode: 400, detail: "Mindestens ein Tag ist erforderlich.");

        var query = Db.Vocabulary.AsNoTracking().AsQueryable();
        if (dto.BaseFormsOnly) query = query.Where(v => v.BaseFormId == null);
        if (dto.MatchAll)
            foreach (var name in tags) query = query.Where(v => v.TagLinks.Any(l => l.VocabTag!.Name == name));
        else
            query = query.Where(v => v.TagLinks.Any(l => tags.Contains(l.VocabTag!.Name)));
        var keys = await query.OrderBy(v => v.Key).Select(v => v.Key).ToListAsync();

        var config = ConfigOf(exercise);
        config.Refs = keys;
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
        return missing.Count == 0 ? null : $"Unbekannte Vokabel-Keys in Lücken: {string.Join(", ", missing)}";
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
public class MatchingController(PuglingDbContext db, ExerciseAnswerChecker checker, AuthAccess access)
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

    /// <summary>Kind, Titel und Laufzeit des zu erzeugenden Leitner-Plans.</summary>
    public record ToStudyPlanDto(int ChildId, string? Title, int? DurationDays);
    /// <summary>Der erzeugte Study-Plan als Startpunkt für den Client.</summary>
    public record StudyPlanCreated(int PlanId, string Title, int ItemCount);

    /// <summary>
    /// Erzeugt aus den Paaren einen <b>Leitner-Study-Plan</b> im neuen System: je Paar eine Vokabel
    /// (Wort = linke Spalte, Übersetzung = rechte Spalte) im Vokabel-Store, gebündelt in einem
    /// <see cref="StudyPlan"/> mit <see cref="StudyPlan.Method"/> = Matching und <see cref="StudyPlan.UseLeitner"/> = true.
    /// Damit laufen Boxen/Fälligkeit, Punkte pro Kind und Reporting über den einen vereinheitlichten Mechanismus
    /// (<c>/api/study-plans/{id}</c>) – der alte Topic/Card-Kasten wird nicht mehr benötigt.
    /// </summary>
    [HttpPost("{exerciseId:int}/to-study-plan")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudyPlanCreated>> ToStudyPlan(int subjectId, int chapterId, int exerciseId, ToStudyPlanDto body)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        if (exercise is null) return NotFound();

        var pairs = ConfigOf(exercise).Pairs;
        if (pairs.Count == 0) return Problem(statusCode: 400, detail: "Die Zuordnung enthält keine Paare.");
        // Einheitlich 404 für "kein eigenes Kind" (kein Enumerieren fremder Kind-Ids), wie im StudyPlansController.
        if (!await access.FatherOwnsChildAsync(User, body.ChildId)) return Problem(statusCode: 404, detail: "Kind nicht gefunden.");

        // Je Paar eine Vokabel mit stabilem Key (idempotent: erneutes Konvertieren aktualisiert statt zu duplizieren).
        var keys = pairs.Select((_, i) => $"match_{exercise.Id}_{i}").ToList();
        var existing = await Db.Vocabulary.Where(v => keys.Contains(v.Key)).ToDictionaryAsync(v => v.Key, v => v);

        var items = new List<StudyPlanItem>();
        for (int i = 0; i < pairs.Count; i++)
        {
            if (!existing.TryGetValue(keys[i], out var vocab))
            {
                vocab = new Vocabulary { Key = keys[i] };
                Db.Vocabulary.Add(vocab);
            }
            vocab.Word = pairs[i].Left;
            vocab.Translation = pairs[i].Right;
            items.Add(new StudyPlanItem { Vocabulary = vocab, Order = i });
        }

        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var duration = body.DurationDays is > 0 ? body.DurationDays.Value : 14;
        var plan = new StudyPlan
        {
            ChildId = body.ChildId,
            Method = LearningMethod.Matching,
            Title = string.IsNullOrWhiteSpace(body.Title) ? exercise.Title : body.Title!.Trim(),
            SubjectId = subjectId,
            UseLeitner = true,
            DefaultStage = (int)MatchStage.Direct,
            StartDate = start,
            EndDate = start.AddDays(duration - 1),
            Items = items,
        };
        // Bonus-Vorschlag der Übung EINMAL in den Plan kopieren (kind-individuell, danach frei anpassbar;
        // spätere Änderungen an der Übung wirken nicht rückwirkend). Ohne Vorschlag greifen die Plan-Defaults.
        if (exercise.SuggestedBonus is { } sb)
        {
            plan.ComboThreshold = sb.ComboThreshold;
            plan.ComboBonusPoints = sb.ComboBonusPoints;
            plan.SpeedThresholdSeconds = sb.SpeedThresholdSeconds;
            plan.SpeedBonusPoints = sb.SpeedBonusPoints;
            plan.NewContentPoints = sb.NewContentPoints;
        }
        Db.StudyPlans.Add(plan);
        await Db.SaveChangesAsync();

        return Created($"/api/study-plans/{plan.Id}", new StudyPlanCreated(plan.Id, plan.Title, items.Count));
    }
}

/// <summary>Übersetzungs-Übungen.</summary>
[Route(ExerciseRoutes.Base + "/translation")]
[Tags("Learn – Translation")]
public class TranslationController(PuglingDbContext db) : ExerciseControllerBase<TranslationConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.Translation;
}

/// <summary>
/// Birkenbihl-Methode: Texte in der Lernsprache mit grammatik-unabhängiger Wort-für-Wort-Dekodierung
/// plus natürlicher Übersetzung. Reine Inhaltsübung zum Lesen/Hören – bewusst ohne <c>/check</c>,
/// da das Verfahren nicht aktiv abfragt. CRUD kommt aus der Basis.
/// </summary>
[Route(ExerciseRoutes.Base + "/birkenbihl")]
[Tags("Learn – Birkenbihl")]
public class BirkenbihlController(PuglingDbContext db) : ExerciseControllerBase<BirkenbihlConfig>(db)
{
    protected override ExerciseType Type => ExerciseType.Birkenbihl;
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
        c.Operations.Count == 0 ? "Mindestens eine Rechenart ist erforderlich."
        : c.MaxOperand < c.MinOperand ? "MaxOperand muss ≥ MinOperand sein."
        : c.ProblemCount is < 1 or > 100 ? "ProblemCount muss zwischen 1 und 100 liegen."
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
        if (Validate(config) is { } error) return Problem(statusCode: 400, detail: error);

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
        if (Validate(config) is { } error) return Problem(statusCode: 400, detail: error);
        if ((body.Seed ?? config.Seed) is not { } seed)
            return Problem(statusCode: 400, detail: "Zum Auswerten muss der Seed des generierten Satzes angegeben werden.");

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
