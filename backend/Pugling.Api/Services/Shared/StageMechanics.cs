using Pugling.Api.Models;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Procedure-neutral stage/answer mechanics shared by the plan-position learning engine and the answer
/// comparison: which stage is "typed" (objectively checkable) and how an answer is normalized for
/// comparison. Deliberately stateless statics – the single source of truth for these small rules.
/// </summary>
public static class StageMechanics
{
    /// <summary>
    /// Vocabulary stages that are objectively checkable (server-side against the solution) – as
    /// opposed to pure self-assessment. Multiple choice counts as one of these: the selection is
    /// checked against the correct option.
    /// </summary>
    public static bool IsTyped(TestStage stage) =>
        stage is TestStage.LetterBoxes or TestStage.FreeText or TestStage.Audio or TestStage.MultipleChoice;

    /// <summary>
    /// Objectively checkable cloze stages – every one of them, including the word bank. Picking from a pool
    /// is an answer like any other (same reasoning as multiple choice above): the server compares it against
    /// the gap's solution. Counting the word-bank stage as self-assessment would hand the child the solution
    /// as <c>Reveal</c> and turn the pool into decoration next to it.
    /// </summary>
    public static bool IsTyped(ClozeStage stage) =>
        stage is ClozeStage.TranslationWordBank or ClozeStage.TranslationFreeText or ClozeStage.FreeText;

    /// <summary>Normalizes an answer for comparison (trim, lowercase, collapse repeated spaces).</summary>
    public static string Normalize(string? s) =>
        string.Join(' ', (s ?? "").Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
