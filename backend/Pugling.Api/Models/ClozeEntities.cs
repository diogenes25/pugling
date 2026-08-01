namespace Pugling.Api.Models;

// The cloze method: a first-class cloze store (learning material, maintained by the supervisor), analogous
// to the vocabulary store. Sentences with the placeholders {{1}}, {{2}} … + solutions per gap.

/// <summary>Stage of the cloze method (increasing difficulty / less help).</summary>
public enum ClozeStage
{
    // `WordBank = 1` (word pool without translation) is gone: the value appeared in no `StageOptions`, was
    // nowhere a `DefaultStage`/`PreviewStage` and was set by no seed - a stage nobody could select. Since the
    // enum convention (string in the DB) the numeric values carry no meaning, so the gap at 1 is none.

    /// <summary>Translation + a choice of possible words.</summary>
    TranslationWordBank = 2,
    /// <summary>Translation + free-text entry.</summary>
    TranslationFreeText = 3,
    /// <summary>Free-text entry only.</summary>
    FreeText = 4,
}

/// <summary>A cloze text as learning material (reference base for study plans/exercises).</summary>
public class ClozeText
{
    public int Id { get; set; }
    /// <summary>Stable, unique reference key (e.g. "cz_greetings_1").</summary>
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public string SourceLanguage { get; set; } = "";
    public string TargetLanguage { get; set; } = "";
    /// <summary>Text with the placeholders {{1}}, {{2}} … at the gaps.</summary>
    public string Text { get; set; } = "";
    /// <summary>Optional translation of the whole sentence (help on stage 2/3).</summary>
    public string? Translation { get; set; }
    /// <summary>Solutions per gap (JSON column).</summary>
    public List<Gap> Gaps { get; set; } = new();
    /// <summary>Optional word pool to choose from (JSON column).</summary>
    public List<string>? WordBank { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
