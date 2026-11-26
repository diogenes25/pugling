namespace Pugling.Contracts.Shared;

// The praise channel (B-377): a supervisor sends a child a short plain-text praise under
// supervisor/children/{childId}/praise; the child reads its unread praise under student/me/praise and stamps it seen.

/// <summary>A praise to send: plain text, 1..140 characters after trimming. No coins - gifts keep their own path.</summary>
public record CreatePraiseDto(string Text);

/// <summary>A praise as its sender sees it: <c>SeenAt</c> is when the child closed it with "Thanks!", <c>null</c> = unread.</summary>
public record PraiseResponse(int Id, int ChildId, string Text, DateTime CreatedAt, DateTime? SeenAt);

/// <summary>An unread praise as the child sees it, with the sending supervisor's name.</summary>
public record MyPraiseResponse(int Id, string SupervisorName, string Text, DateTime CreatedAt);

/// <summary>The praise ids the child's overlay showed; only the child's own, still unread ones are stamped.</summary>
public record MarkPraiseSeenDto(IReadOnlyList<int> Ids);
