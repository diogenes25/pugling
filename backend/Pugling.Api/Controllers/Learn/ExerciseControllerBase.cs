using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Learn;

/// <summary>
/// Übung zum Anlegen/Ändern: gemeinsame Felder + typ-spezifische Config + optionaler Bonus-Vorschlag.
/// Die Metadaten (Klassenstufe, Schulart, Quelle, Art) dienen der Lehrplan-Vorfilterung und sind optional.
/// </summary>
public record ExercisePayload<TConfig>(string Title, int OrderIndex, int RewardPoints, TConfig Config,
    SuggestedBonus? SuggestedBonus = null,
    int? GradeMin = null, int? GradeMax = null, SchoolTypes SchoolTypes = SchoolTypes.None,
    string? Source = null, int? CategoryId = null, string? Description = null,
    bool DefaultUseLeitner = false, bool DefaultRequireTypedTest = false, int? DefaultStage = null);

/// <summary>Übung in der Antwort. <paramref name="IsOwn"/> zeigt, ob der anfragende Vater Autor ist (Editier-/Löschrecht).</summary>
public record ExerciseResponse<TConfig>(int Id, int ChapterId, string Type, string Title,
    int OrderIndex, int RewardPoints, DateTime CreatedAt, TConfig Config, SuggestedBonus? SuggestedBonus,
    int? GradeMin, int? GradeMax, SchoolTypes SchoolTypes, string? Source, int? CategoryId, string? CategoryName,
    int? AuthorFatherId, bool IsOwn, string? Description, bool DefaultUseLeitner, bool DefaultRequireTypedTest);

/// <summary>
/// Gemeinsame CRUD-Logik für alle Übungstypen unter einem Kapitel.
/// Konkrete Controller setzen nur Route + <see cref="Type"/>; die typ-spezifische
/// Konfiguration (<typeparamref name="TConfig"/>) wird als JSON gespeichert und
/// im API voll typisiert übertragen.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
public abstract class ExerciseControllerBase<TConfig>(PuglingDbContext db) : ControllerBase
    where TConfig : class, new()
{
    /// <summary>Übungstyp, den dieser Controller verwaltet.</summary>
    protected abstract ExerciseType Type { get; }

    /// <summary>
    /// Typ-spezifische Validierung der Config beim Anlegen/Ändern. Standard: keine. Abgeleitete Controller
    /// überschreiben dies, um z. B. Store-Referenzen (Vokabel-Keys) zu prüfen; Rückgabe = Fehlertext (→ 400)
    /// oder <c>null</c>, wenn alles in Ordnung ist.
    /// </summary>
    protected virtual Task<string?> ValidateConfigAsync(int subjectId, TConfig config) =>
        Task.FromResult<string?>(null);

    /// <summary>
    /// Normalisiert die Config vor dem Speichern (Standard: unverändert). Abgeleitete Controller überschreiben
    /// dies, um serverseitige Invarianten herzustellen – z. B. übungsweit eindeutige IDs zu vergeben, wenn der
    /// Aufrufer sie (wie das Anlege-Formular) nicht selbst pflegt.
    /// </summary>
    protected virtual void NormalizeConfig(TConfig config) { }

    /// <summary>
    /// Asynchrone Normalisierung vor dem Speichern (Standard: nichts). Abgeleitete Controller überschreiben dies,
    /// wenn die Invariante DB-Zugriff braucht – z. B. Vokabel-Übungen, die inline genutzte Wörter im Store anlegen
    /// und mit ihrer Store-ID verknüpfen. Läuft nach <see cref="NormalizeConfig"/> und darf <c>SaveChanges</c> nutzen.
    /// </summary>
    protected virtual Task NormalizeConfigAsync(int subjectId, TConfig config) => Task.CompletedTask;

    /// <summary>
    /// Formt die Config für die Antwort (Standard: wie gespeichert). Abgeleitete Controller überschreiben dies,
    /// um abgeleitete, nicht persistierte Felder zu füllen – z. B. den HATEOAS-Link <c>_self</c> je Vokabel aus
    /// ihrer ID. Rein rechnerisch (kein DB-Zugriff), da pro Zeile der Liste aufgerufen.
    /// </summary>
    protected virtual TConfig ConfigForResponse(Exercise exercise) => ConfigOf(exercise);

    /// <summary>DbContext für abgeleitete Controller mit Zusatz-Endpunkten über das reine CRUD hinaus.</summary>
    protected PuglingDbContext Db => db;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private Task<bool> ChapterExists(int subjectId, int chapterId) =>
        db.Chapters.AnyAsync(c => c.Id == chapterId && c.SubjectId == subjectId);

    /// <summary>Prüft, dass eine gesetzte Art zum Fach der Übung gehört (fremde Fächer verhindern).</summary>
    private Task<bool> CategoryValid(int subjectId, int? categoryId) =>
        categoryId is null
            ? Task.FromResult(true)
            : db.ExerciseCategories.AnyAsync(c => c.Id == categoryId && c.SubjectId == subjectId);

    /// <summary>
    /// Prüft das Schreibrecht auf eine Übung: Der Katalog ist global (jeder Vater darf jede Übung
    /// finden und in seine Lehrpläne übernehmen), aber ändern/löschen darf nur der Autor. Geseedete
    /// System-Übungen (<see cref="Exercise.AuthorFatherId"/> = null) sind für niemanden editierbar.
    /// Gibt bei fehlendem Recht ein <c>403</c>-<see cref="ProblemDetails"/> zurück, sonst <c>null</c>.
    /// </summary>
    protected ObjectResult? EnsureCanModify(Exercise exercise) =>
        User.Owns(exercise)
            ? null
            : this.ProblemWithCode(ApiErrors.NotAuthor, "This exercise belongs to another father and can only be modified or deleted by its author.");

    /// <summary>Lädt eine Übung dieses Typs; Basis für abgeleitete Zusatz-Endpunkte (Generieren, Auswerten).</summary>
    protected Task<Exercise?> FindAsync(int subjectId, int chapterId, int exerciseId) =>
        db.Exercises.Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == exerciseId && e.ChapterId == chapterId
                && e.Type == Type && e.Chapter!.SubjectId == subjectId);

    /// <summary>Deserialisiert die typisierte Konfiguration einer Übung (nie null; fällt auf Default zurück).</summary>
    protected TConfig ConfigOf(Exercise exercise) =>
        JsonSerializer.Deserialize<TConfig>(exercise.ConfigJson, JsonOptions) ?? new TConfig();

    /// <summary>Schreibt die typisierte Konfiguration zurück in die Übung (JSON) – für abgeleitete Zusatz-Endpunkte.</summary>
    protected void SetConfig(Exercise exercise, TConfig config) =>
        exercise.ConfigJson = JsonSerializer.Serialize(config, JsonOptions);

    /// <summary>Projiziert eine Übung; <paramref name="fid"/> wird einmal pro Request ermittelt (nicht pro Zeile).</summary>
    protected ExerciseResponse<TConfig> Map(Exercise e, int? fid) =>
        new(e.Id, e.ChapterId, e.Type.ToString(), e.Title, e.OrderIndex, e.RewardPoints, e.CreatedAt, ConfigForResponse(e), e.SuggestedBonus,
            e.GradeMin, e.GradeMax, e.SchoolTypes, e.Source, e.CategoryId, e.Category?.Name,
            e.AuthorFatherId, ClaimsPrincipalExtensions.IsOwnedBy(e.AuthorFatherId, fid), e.Description,
            e.DefaultUseLeitner, e.DefaultRequireTypedTest);

    /// <summary>Liste der Übungen dieses Typs im Kapitel.</summary>
    /// <param name="subjectId">Fach, zu dem das Kapitel gehört.</param>
    /// <param name="chapterId">Kapitel, dessen Übungen gelesen werden.</param>
    /// <param name="skip">Anzahl zu überspringender Einträge (Paging).</param>
    /// <param name="take">Maximale Trefferzahl (1..500). Gesamtzahl im Header <c>X-Total-Count</c>.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ExerciseResponse<TConfig>>>> List(
        int subjectId, int chapterId,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake)
    {
        if (!await ChapterExists(subjectId, chapterId)) return NotFound();
        var fid = User.FatherId();
        var exercises = await db.Exercises
            .AsNoTracking()
            .Include(e => e.Category)
            .Where(e => e.ChapterId == chapterId && e.Type == Type)
            .OrderBy(e => e.OrderIndex).ThenBy(e => e.Id)
            .ToPagedListAsync(Response, skip, take);
        return exercises.Select(e => Map(e, fid)).ToList();
    }

    /// <summary>Eine einzelne Übung.</summary>
    [HttpGet("{exerciseId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseResponse<TConfig>>> Get(int subjectId, int chapterId, int exerciseId)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        return exercise is null ? NotFound() : Map(exercise, User.FatherId());
    }

    /// <summary>Erstellt eine Übung dieses Typs im Kapitel.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseResponse<TConfig>>> Create(int subjectId, int chapterId, ExercisePayload<TConfig> body)
    {
        if (!await ChapterExists(subjectId, chapterId)) return NotFound();
        if (string.IsNullOrWhiteSpace(body.Title)) return this.ProblemWithCode(ApiErrors.ValidationError, "Title is required.");
        if (!await CategoryValid(subjectId, body.CategoryId)) return this.ProblemWithCode(ApiErrors.InvalidReference, "Unknown category for this subject.");
        var config = body.Config ?? new TConfig();
        if (await ValidateConfigAsync(subjectId, config) is { } createErr) return this.ProblemWithCode(ApiErrors.ValidationError, createErr);
        NormalizeConfig(config);
        await NormalizeConfigAsync(subjectId, config);

        var exercise = new Exercise
        {
            ChapterId = chapterId,
            Type = Type,
            Title = body.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(body.Description) ? null : body.Description.Trim(),
            OrderIndex = body.OrderIndex,
            RewardPoints = body.RewardPoints,
            ConfigJson = JsonSerializer.Serialize(config, JsonOptions),
            SuggestedBonus = body.SuggestedBonus,
            GradeMin = body.GradeMin,
            GradeMax = body.GradeMax,
            SchoolTypes = body.SchoolTypes,
            Source = string.IsNullOrWhiteSpace(body.Source) ? null : body.Source.Trim(),
            CategoryId = body.CategoryId,
            DefaultUseLeitner = body.DefaultUseLeitner,
            DefaultRequireTypedTest = body.DefaultRequireTypedTest,
            DefaultStage = body.DefaultStage,
            // Autor = der anlegende Vater. Sichert ihm später das alleinige Editier-/Löschrecht (Katalog bleibt global lesbar).
            AuthorFatherId = User.FatherId(),
        };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        // Für CategoryName in der Antwort die Art nachladen (billig; nur beim Erzeugen).
        if (exercise.CategoryId is not null)
            exercise.Category = await db.ExerciseCategories.FindAsync(exercise.CategoryId);

        return CreatedAtAction(nameof(Get), new { subjectId, chapterId, exerciseId = exercise.Id }, Map(exercise, User.FatherId()));
    }

    /// <summary>Ersetzt eine Übung vollständig (inkl. Config).</summary>
    [HttpPut("{exerciseId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseResponse<TConfig>>> Update(int subjectId, int chapterId, int exerciseId, ExercisePayload<TConfig> body)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        if (exercise is null) return NotFound();
        if (EnsureCanModify(exercise) is { } forbidden) return forbidden;
        if (string.IsNullOrWhiteSpace(body.Title)) return this.ProblemWithCode(ApiErrors.ValidationError, "Title is required.");
        if (!await CategoryValid(subjectId, body.CategoryId)) return this.ProblemWithCode(ApiErrors.InvalidReference, "Unknown category for this subject.");
        var config = body.Config ?? new TConfig();
        if (await ValidateConfigAsync(subjectId, config) is { } updateErr) return this.ProblemWithCode(ApiErrors.ValidationError, updateErr);
        NormalizeConfig(config);

        exercise.Title = body.Title.Trim();
        exercise.Description = string.IsNullOrWhiteSpace(body.Description) ? null : body.Description.Trim();
        exercise.OrderIndex = body.OrderIndex;
        exercise.RewardPoints = body.RewardPoints;
        exercise.ConfigJson = JsonSerializer.Serialize(config, JsonOptions);
        exercise.SuggestedBonus = body.SuggestedBonus;
        exercise.GradeMin = body.GradeMin;
        exercise.GradeMax = body.GradeMax;
        exercise.SchoolTypes = body.SchoolTypes;
        exercise.Source = string.IsNullOrWhiteSpace(body.Source) ? null : body.Source.Trim();
        exercise.CategoryId = body.CategoryId;
        exercise.DefaultUseLeitner = body.DefaultUseLeitner;
        exercise.DefaultRequireTypedTest = body.DefaultRequireTypedTest;
        exercise.DefaultStage = body.DefaultStage;
        await db.SaveChangesAsync();

        // Navigation nach evtl. geänderter CategoryId aktualisieren, damit CategoryName stimmt.
        exercise.Category = exercise.CategoryId is null
            ? null
            : await db.ExerciseCategories.FindAsync(exercise.CategoryId);

        return Map(exercise, User.FatherId());
    }

    /// <summary>Löscht eine Übung. Nicht möglich, solange sie in einem Lehrplan oder einer Klassenarbeit steckt.</summary>
    [HttpDelete("{exerciseId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int subjectId, int chapterId, int exerciseId)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        if (exercise is null) return NotFound();
        if (EnsureCanModify(exercise) is { } forbidden) return forbidden;
        // Verwendete Übungen schützen: der FK PlanPosition→Exercise ist Restrict (sonst 500 statt klarer Fehler).
        if (await db.PlanPositions.AnyAsync(p => p.ExerciseId == exerciseId)
            || await db.KlassenarbeitExercises.AnyAsync(x => x.ExerciseId == exerciseId))
            return this.ProblemWithCode(ApiErrors.ExerciseInUse, "The exercise is used in a study plan or a class test and cannot be deleted.");
        db.Exercises.Remove(exercise);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
