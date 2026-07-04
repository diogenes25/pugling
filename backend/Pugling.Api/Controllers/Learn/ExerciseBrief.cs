using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Learn;

/// <summary>
/// Schlanke, typ-übergreifende Sicht auf eine Katalog-Übung – für Listen, in denen Übungen
/// verschiedener Typen gemeinsam erscheinen (getaggte Übungen, Übungen einer Klassenarbeit).
/// Die typ-spezifische Konfiguration wird als rohes JSON durchgereicht.
/// </summary>
public record ExerciseBrief(
    int Id, int ChapterId, string ChapterName, int? SubjectId, string SubjectName,
    string Type, string Title, int RewardPoints, JsonElement Config)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Bildet eine Übung ab. Erwartet, dass <see cref="Exercise.Chapter"/> und dessen
    /// <see cref="Chapter.Subject"/> geladen sind (Include), sonst bleiben die Namen leer.
    /// </summary>
    public static ExerciseBrief From(Exercise e) => new(
        e.Id,
        e.ChapterId,
        e.Chapter?.Name ?? "",
        e.Chapter?.SubjectId,
        e.Chapter?.Subject?.Name ?? "",
        e.Type.ToString(),
        e.Title,
        e.RewardPoints,
        JsonSerializer.Deserialize<JsonElement>(
            string.IsNullOrWhiteSpace(e.ConfigJson) ? "{}" : e.ConfigJson, JsonOptions));
}
