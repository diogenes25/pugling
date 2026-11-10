namespace Pugling.Agent.Creator.Drafting;

// The draft shapes the language model fills in. Deliberately NOT the contract configs themselves:
// those carry technical fields (ids, store references, HATEOAS links, counters) that no model may
// invent. The drafts are flat and minimal - the smaller the JSON schema, the more reliably local
// models return valid answers. Translating draft → config is each strategy's job, not the model's.

/// <summary>Draft of a vocabulary exercise: title plus the word pairs.</summary>
/// <param name="Title">Descriptive German title of the exercise.</param>
/// <param name="Items">The word pairs in learning order.</param>
public sealed record VocabularyDraft(string Title, List<VocabularyDraftItem> Items);

/// <summary>A word pair.</summary>
/// <param name="Front">Word in the language being learned.</param>
/// <param name="Back">Translation in the native language.</param>
/// <param name="Hint">Optional short memory aid (mnemonic, context).</param>
public sealed record VocabularyDraftItem(string Front, string Back, string? Hint);

/// <summary>
/// Draft of a cloze text. The answers stand in square brackets inside the text; <see cref="ClozeMarkup"/> derives
/// placeholders, gaps and word bank from it - the model no longer numbers anything.
/// </summary>
/// <param name="Title">Descriptive German title of the exercise.</param>
/// <param name="Text">Running text with every answer in brackets: <c>We [went] (go) to the Tower.</c></param>
/// <param name="Distractors">Wrong but plausible words for the word bank; never one of the answers.</param>
public sealed record ClozeDraft(string Title, string Text, List<string>? Distractors);

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
