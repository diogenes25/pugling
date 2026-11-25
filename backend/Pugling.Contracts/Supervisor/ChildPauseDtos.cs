namespace Pugling.Contracts.Supervisor;

/// <summary>
/// A pause of the child (B-731): from <paramref name="From"/> to <paramref name="Until"/>, both days included. No duty is
/// owed on these days - no penalty, and the streak neither counts nor breaks.
/// </summary>
public record ChildPauseResponse(int Id, DateOnly From, DateOnly Until, string? Note, DateTime CreatedAt);

/// <summary>
/// Input for a pause. <paramref name="Until"/> before <paramref name="From"/> is <c>validation_error</c>; a pause may lie
/// in the past (entered after the fact, also for today). <paramref name="Note"/> is free text of at most 200 characters.
/// </summary>
public record CreateChildPauseDto(DateOnly From, DateOnly Until, string? Note = null);
