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

    /// <summary>
    /// Writes a renamed subject's new name into every row that points at it (B-145) - the other half of the
    /// invariant: <see cref="ResolveNameAsync"/> keeps the name right when a row's id moves, this keeps it
    /// right when the subject itself changes. Without it each copy stayed wrong until that row's next
    /// unrelated PATCH happened to heal it.
    /// <para>
    /// Matches on the id, never on the old name: a free-text row that merely spells the same name is an
    /// uncatalogued work, not a copy, and must not follow. Rows of other owners are rewritten too - the
    /// name is a cached display value of the catalog, not data that belongs to the row's owner.
    /// </para>
    /// </summary>
    public static async Task PropagateRenameAsync(PuglingDbContext db, int subjectId, string name, CancellationToken ct)
    {
        await db.TextbookSeries.Where(s => s.SubjectId == subjectId)
            .ExecuteUpdateAsync(u => u.SetProperty(s => s.SubjectName, name), ct);
        await db.CreatorProfiles.Where(p => p.SubjectId == subjectId)
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.SubjectName, name), ct);
        await db.Textbooks.Where(t => t.SubjectId == subjectId)
            .ExecuteUpdateAsync(u => u.SetProperty(t => t.SubjectName, name), ct);
    }
}
