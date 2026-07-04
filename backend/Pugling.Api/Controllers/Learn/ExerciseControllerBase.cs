using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Learn;

/// <summary>Übung zum Anlegen/Ändern: gemeinsame Felder + typ-spezifische Config + optionaler Bonus-Vorschlag.</summary>
public record ExercisePayload<TConfig>(string Title, int OrderIndex, int RewardPoints, TConfig Config,
    SuggestedBonus? SuggestedBonus = null);

/// <summary>Übung in der Antwort.</summary>
public record ExerciseResponse<TConfig>(int Id, int ChapterId, string Type, string Title,
    int OrderIndex, int RewardPoints, DateTime CreatedAt, TConfig Config, SuggestedBonus? SuggestedBonus);

/// <summary>
/// Gemeinsame CRUD-Logik für alle Übungstypen unter einem Kapitel.
/// Konkrete Controller setzen nur Route + <see cref="Type"/>; die typ-spezifische
/// Konfiguration (<typeparamref name="TConfig"/>) wird als JSON gespeichert und
/// im API voll typisiert übertragen.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
public abstract class ExerciseControllerBase<TConfig>(PuglingDbContext db) : ControllerBase
    where TConfig : class, new()
{
    /// <summary>Übungstyp, den dieser Controller verwaltet.</summary>
    protected abstract ExerciseType Type { get; }

    /// <summary>DbContext für abgeleitete Controller mit Zusatz-Endpunkten über das reine CRUD hinaus.</summary>
    protected PuglingDbContext Db => db;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private Task<bool> ChapterExists(int subjectId, int chapterId) =>
        db.Chapters.AnyAsync(c => c.Id == chapterId && c.SubjectId == subjectId);

    /// <summary>Lädt eine Übung dieses Typs; Basis für abgeleitete Zusatz-Endpunkte (Generieren, Auswerten).</summary>
    protected Task<Exercise?> FindAsync(int subjectId, int chapterId, int exerciseId) =>
        db.Exercises.FirstOrDefaultAsync(e => e.Id == exerciseId && e.ChapterId == chapterId
            && e.Type == Type && e.Chapter!.SubjectId == subjectId);

    /// <summary>Deserialisiert die typisierte Konfiguration einer Übung (nie null; fällt auf Default zurück).</summary>
    protected TConfig ConfigOf(Exercise exercise) =>
        JsonSerializer.Deserialize<TConfig>(exercise.ConfigJson, JsonOptions) ?? new TConfig();

    private ExerciseResponse<TConfig> Map(Exercise e) =>
        new(e.Id, e.ChapterId, e.Type.ToString(), e.Title, e.OrderIndex, e.RewardPoints, e.CreatedAt, ConfigOf(e), e.SuggestedBonus);

    /// <summary>Liste der Übungen dieses Typs im Kapitel.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ExerciseResponse<TConfig>>>> List(int subjectId, int chapterId)
    {
        if (!await ChapterExists(subjectId, chapterId)) return NotFound();
        var exercises = await db.Exercises
            .Where(e => e.ChapterId == chapterId && e.Type == Type)
            .OrderBy(e => e.OrderIndex).ThenBy(e => e.Id)
            .ToListAsync();
        return exercises.Select(Map).ToList();
    }

    /// <summary>Eine einzelne Übung.</summary>
    [HttpGet("{exerciseId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseResponse<TConfig>>> Get(int subjectId, int chapterId, int exerciseId)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        return exercise is null ? NotFound() : Map(exercise);
    }

    /// <summary>Erstellt eine Übung dieses Typs im Kapitel.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseResponse<TConfig>>> Create(int subjectId, int chapterId, ExercisePayload<TConfig> body)
    {
        if (!await ChapterExists(subjectId, chapterId)) return NotFound();
        if (string.IsNullOrWhiteSpace(body.Title)) return BadRequest("Titel ist erforderlich.");

        var exercise = new Exercise
        {
            ChapterId = chapterId,
            Type = Type,
            Title = body.Title.Trim(),
            OrderIndex = body.OrderIndex,
            RewardPoints = body.RewardPoints,
            ConfigJson = JsonSerializer.Serialize(body.Config ?? new TConfig(), JsonOptions),
            SuggestedBonus = body.SuggestedBonus,
        };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { subjectId, chapterId, exerciseId = exercise.Id }, Map(exercise));
    }

    /// <summary>Ersetzt eine Übung vollständig (inkl. Config).</summary>
    [HttpPut("{exerciseId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseResponse<TConfig>>> Update(int subjectId, int chapterId, int exerciseId, ExercisePayload<TConfig> body)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        if (exercise is null) return NotFound();
        if (string.IsNullOrWhiteSpace(body.Title)) return BadRequest("Titel ist erforderlich.");

        exercise.Title = body.Title.Trim();
        exercise.OrderIndex = body.OrderIndex;
        exercise.RewardPoints = body.RewardPoints;
        exercise.ConfigJson = JsonSerializer.Serialize(body.Config ?? new TConfig(), JsonOptions);
        exercise.SuggestedBonus = body.SuggestedBonus;
        await db.SaveChangesAsync();

        return Map(exercise);
    }

    /// <summary>Löscht eine Übung.</summary>
    [HttpDelete("{exerciseId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int subjectId, int chapterId, int exerciseId)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId);
        if (exercise is null) return NotFound();
        db.Exercises.Remove(exercise);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
