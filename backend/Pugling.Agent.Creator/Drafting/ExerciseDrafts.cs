namespace Pugling.Agent.Creator.Drafting;

// Die Entwurfs-Formen, die das Sprachmodell füllt. Bewusst NICHT die Vertrags-Configs selbst:
// die tragen technische Felder (Ids, Store-Verweise, HATEOAS-Links, Zähler), die kein Modell
// erfinden darf. Die Entwürfe sind flach und minimal – je kleiner das JSON-Schema, desto
// zuverlässiger liefern lokale Modelle gültige Antworten. Die Übersetzung Entwurf → Config
// macht die jeweilige Strategie, nicht das Modell.

/// <summary>Draft of a vocabulary exercise: title plus the word pairs.</summary>
/// <param name="Title">Descriptive German title of the exercise.</param>
/// <param name="Items">The word pairs in learning order.</param>
public sealed record VocabularyDraft(string Title, List<VocabularyDraftItem> Items);

/// <summary>A word pair.</summary>
/// <param name="Front">Word in the language being learned.</param>
/// <param name="Back">Translation in the native language.</param>
/// <param name="Hint">Optional short memory aid (mnemonic, context).</param>
public sealed record VocabularyDraftItem(string Front, string Back, string? Hint);

/// <summary>Draft of a cloze text.</summary>
/// <param name="Title">Descriptive German title of the exercise.</param>
/// <param name="Text">Running text with placeholders <c>{{1}}</c>, <c>{{2}}</c> … at the gap positions.</param>
/// <param name="Gaps">Exactly one gap per placeholder, with the same number.</param>
/// <param name="WordBank">Selection words (all answers plus a few plausible distractors).</param>
public sealed record ClozeDraft(string Title, string Text, List<ClozeGapDraft> Gaps, List<string>? WordBank);

/// <summary>A gap of the cloze text.</summary>
/// <param name="Index">Number of the placeholder in the text (<c>{{1}}</c> → 1).</param>
/// <param name="Answer">The one correct answer.</param>
/// <param name="Alternatives">Additionally accepted spellings.</param>
public sealed record ClozeGapDraft(int Index, string Answer, List<string>? Alternatives);

/// <summary>Draft of a translation exercise.</summary>
/// <param name="Title">Descriptive German title of the exercise.</param>
/// <param name="Items">The sentence pairs.</param>
public sealed record TranslationDraft(string Title, List<TranslationDraftItem> Items);

/// <summary>A sentence pair.</summary>
/// <param name="Source">Sentence in the source language.</param>
/// <param name="Target">Expected translation.</param>
/// <param name="Alternatives">Further accepted translations.</param>
public sealed record TranslationDraftItem(string Source, string Target, List<string>? Alternatives);

/// <summary>Draft of a grammar exercise.</summary>
/// <param name="Title">Descriptive German title of the exercise.</param>
/// <param name="Instruction">German work instruction for the child.</param>
/// <param name="Tasks">The individual tasks.</param>
public sealed record GrammarDraft(string Title, string Instruction, List<GrammarDraftTask> Tasks);

/// <summary>A grammar task.</summary>
/// <param name="Prompt">Task prompt/source sentence.</param>
/// <param name="Answer">The expected answer.</param>
/// <param name="RuleHint">Short hint at the underlying rule.</param>
public sealed record GrammarDraftTask(string Prompt, string Answer, string? RuleHint);
