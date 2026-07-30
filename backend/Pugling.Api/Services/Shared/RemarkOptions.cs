namespace Pugling.Api.Services.Shared;

/// <summary>
/// Settings for test remarks (<c>Remarks</c> section).
/// </summary>
public class RemarkOptions
{
    /// <summary>
    /// Whether <c>?scope=all</c> is open to every adult – i.e. the cross-account view that the
    /// follow-up skill needs.
    /// <para>
    /// <b>Why this is its own switch and not the <c>Admin</c> role:</b> A bug often only shows up
    /// in a particular constellation – a freshly registered adult without exercises surfaces things
    /// that never show up for the seeded dad, because he has content from the start. Testing therefore
    /// constantly produces throwaway accounts, and each one would otherwise first need to be flagged.
    /// <c>Admin</c> is the wrong tool for this: the role also bypasses the RWX rights on exercises
    /// (<see cref="Auth.ExercisePermissionService"/>) – with it, every adult could change, delete and
    /// re-permission other people's exercises, and <c>ExerciseGrant</c> would be decoration. Two things
    /// that have nothing to do with each other would then hang on a single switch.
    /// </para>
    /// <para>
    /// The default is <c>true</c> in development and <c>false</c> otherwise (set in <c>Program.cs</c>):
    /// on a development instance all accounts belong to the same person, whereas in production other
    /// families would otherwise read each other's test notes – and answers carry file and line references.
    /// A student remains excluded in <b>every</b> case.
    /// </para>
    /// </summary>
    public bool GlobalRead { get; set; }
}
