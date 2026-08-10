using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;

namespace Pugling.Api.Services.Shared;

/// <summary>
/// Keeps the pair <c>SubjectId</c> + <c>SubjectName</c> from contradicting itself.
/// <para>
/// <b>Why the pair exists at all.</b> <c>SubjectName</c> is the fallback for works that are <em>not</em>
/// catalogued - a series may name its subject in free text without pointing at a <c>Subject</c> row. The
/// moment an id is present, though, the catalog is the truth and the free text is a second, competing
/// statement about the same thing. Three resources carry the pair (series, creator profile, a child's
/// textbook) and each used to assign the two halves independently, so a <c>PATCH</c> moving only the id
/// left the old name standing (B-142).
/// </para>
/// <para>
/// <b>Why the server derives it instead of asking the caller to.</b> The contract used to name the
/// caller's duty to send both fields - a rule nothing enforced. The frontend honoured it, the client
/// library, the creator agent and the <c>.http</c> flows did not. A duty spread across every consumer is
/// the kind of rule this repo otherwise holds mechanically.
/// </para>
/// </summary>
public static class SubjectNaming
{
    /// <summary>
    /// The display name belonging to <paramref name="subjectId"/>, or <c>null</c> when no id is given.
    /// <para>
    /// Costs one indexed lookup per write that sets a subject. Deliberately not folded into the existing
    /// reference checks: those live in three differently shaped validators, and threading a return value
    /// through all of them would buy one round trip at the price of the clarity this helper exists for.
    /// </para>
    /// </summary>
    public static async Task<string?> ResolveNameAsync(PuglingDbContext db, int? subjectId, CancellationToken ct) =>
        subjectId is int id
            ? await db.Subjects.AsNoTracking().Where(s => s.Id == id).Select(s => s.Name).FirstOrDefaultAsync(ct)
            : null;
}
