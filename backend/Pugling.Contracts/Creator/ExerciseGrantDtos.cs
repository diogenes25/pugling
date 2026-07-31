namespace Pugling.Contracts.Creator;

// Vertrag der RWX-Rechtevergabe an Übungen (mehrere Owner + Write/Execute je Creator).

/// <summary>A permission granted to a creator.</summary>
public record GrantResponse(int CreatorId, string CreatorName, GrantPermission Permission,
    int? GrantedByAdultId, DateTime CreatedAt);

/// <summary>Input for granting a permission.</summary>
public record AddGrantDto(int CreatorId, GrantPermission Permission);

/// <summary>
/// Input for publishing or <b>withdrawing</b> an exercise – the counter-move to publishing.
/// </summary>
/// <param name="ExecutePublic">
/// <c>true</c> = any creator may assign it to a child. <c>false</c> = <b>withdrawn</b>: only whoever holds
/// an explicit permission on the exercise (owner/write/execute) can still make new assignments.
/// <para>
/// What withdrawing does <b>not</b> do: touch running study plans. The check applies when assigning, not
/// when playing – a child learning the exercise today keeps learning it. That is exactly why this is the way
/// to take material out of circulation, and not deletion (which a used exercise refuses anyway).
/// </para>
/// </param>
public record SetExerciseSharingDto(bool ExecutePublic);

/// <summary>
/// Input for <b>teacher</b> registration: an account that exclusively creates content.
/// </summary>
/// <param name="Name">Display name – appears as the author on their exercises.</param>
/// <param name="Email">Optional; if set, unique account-wide.</param>
/// <param name="Pin">Login PIN. Empty = the account cannot (yet) log in.</param>
public record CreateTeacherDto(string Name, string? Email, string? Pin);

/// <summary>
/// The created teacher account. <paramref name="CreatorId"/> is the <b>login name</b> and at the same time the id
/// under which their authorship and their permissions on exercises hang.
/// </summary>
/// <param name="CreatorId">Domain id of the creator (login name).</param>
/// <param name="AccountId">Account id for the account-centric login <c>auth/login</c>.</param>
/// <param name="Name">Display name.</param>
/// <param name="Email">Email, if specified.</param>
/// <param name="Roles">
/// The account's roles – for a teacher <b>only</b> <c>Creator</c>. Deliberately in the response: the
/// difference to a father account is exactly this list, and it should be visible without a second call.
/// </param>
public record TeacherAccountResponse(int CreatorId, int AccountId, string Name, string? Email,
    IReadOnlyList<string> Roles);

/// <summary>The publication status of an exercise after toggling.</summary>
/// <param name="Id">The exercise.</param>
/// <param name="ExecutePublic">Is it assignable to everyone?</param>
/// <param name="GrantCount">
/// How many explicit permissions hang on it. After withdrawing, this is the circle that can still
/// assign it – at <c>1</c> (only the owner themselves) it is practically out of circulation.
/// </param>
public record ExerciseSharingResponse(int Id, bool ExecutePublic, int GrantCount);
