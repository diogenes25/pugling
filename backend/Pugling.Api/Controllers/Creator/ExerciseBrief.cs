using System.Text.Json;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Maps catalog exercises onto the contract record <see cref="ExerciseBrief"/>. The record itself
/// lives in the contracts project (Pugling.Contracts.Creator) – only the entity knowledge stays here.
/// </summary>
public static class ExerciseBriefMapping
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Maps an exercise. Expects <see cref="Exercise.Chapter"/> and its
    /// <see cref="Chapter.Subject"/> to be loaded (Include), otherwise the names stay empty.
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
