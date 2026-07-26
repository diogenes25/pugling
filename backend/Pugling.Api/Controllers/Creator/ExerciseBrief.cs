using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Bildet Katalog-Übungen auf den Vertrags-Record <see cref="ExerciseBrief"/> ab. Der Record selbst
/// lebt im Vertrags-Projekt (Pugling.Contracts.Creator) – hier bleibt allein die Entity-Kenntnis.
/// </summary>
public static class ExerciseBriefMapping
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
