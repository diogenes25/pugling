using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Maps catalog exercises onto the contract record <see cref="ExerciseBrief"/>. The record itself
/// lives in the contracts project (Pugling.Contracts.Creator) – only the entity knowledge stays here.
/// </summary>
public static class ExerciseBriefMapping
{
    /// <summary>
    /// Maps an exercise. Expects <see cref="Exercise.SeriesUnit"/>, its <see cref="SeriesUnit.Series"/>
    /// and that series' <see cref="TextbookSeries.Subject"/> to be loaded (Include), otherwise the names
    /// stay empty.
    /// <para>
    /// <see cref="Exercise.ConfigJson"/> is deliberately NOT mapped – see <see cref="ExerciseBrief"/>
    /// for why. Do not add it back here; the brief is read by student tokens.
    /// </para>
    /// </summary>
    public static ExerciseBrief From(Exercise e) => new(
        e.Id,
        e.SeriesUnitId,
        e.SeriesUnit?.Label ?? "",
        e.SeriesUnit?.Series?.SubjectId,
        e.SeriesUnit?.Series?.Subject?.Name ?? "",
        e.Type.ToString(),
        e.Title,
        e.RewardPoints);
}
