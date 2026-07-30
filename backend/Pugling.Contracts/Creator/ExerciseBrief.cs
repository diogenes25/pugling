using System.Text.Json;

namespace Pugling.Contracts.Creator;

/// <summary>
/// Lean, type-spanning view of a catalog exercise – for lists in which exercises of
/// different types appear together (tagged exercises, exercises of a class test).
/// The type-specific configuration is passed through as raw JSON.
/// </summary>
public record ExerciseBrief(
    int Id, int ChapterId, string ChapterName, int? SubjectId, string SubjectName,
    string Type, string Title, int RewardPoints, JsonElement Config);
