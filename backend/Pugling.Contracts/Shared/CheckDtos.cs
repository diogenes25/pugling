namespace Pugling.Contracts.Shared;

// Tier-spanning contract of answer evaluation: the same shape for the stateless catalog check (creator)
// as for the server-authoritative final test (student).

/// <summary>An answer submitted by the child, positional (index in the respective item list).</summary>
public record GivenAnswer(int Index, string? Value);

/// <summary>Evaluation of a single item.</summary>
public record ItemCheck(int Index, string Prompt, string? Given, string Expected, bool Correct);

/// <summary>Overall result of an evaluation: hit count, percentage, and individual results.</summary>
public record CheckResult(int Total, int Correct, int ScorePercent, IReadOnlyList<ItemCheck> Items);

/// <summary>A generated arithmetic expression together with its solution.</summary>
public record GeneratedProblem(string Prompt, decimal Answer);
