using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Errors;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

// ExercisePayload<TConfig>/ExerciseResponse<TConfig> live in the contract project (Pugling.Contracts.Creator).

/// <summary>
/// Shared CRUD logic for all exercise types under a chapter.
/// Concrete controllers set only the route + <see cref="Type"/>; the type-specific
/// configuration (<typeparamref name="TConfig"/>) is stored as JSON and
/// transferred fully typed in the API.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(Roles = Roles.Creator)]
public abstract class ExerciseControllerBase<TConfig>(PuglingDbContext db, ExerciseTypeRegistry registry) : ControllerBase
    where TConfig : class, new()
{
    /// <summary>Exercise type key managed by this controller (= <see cref="IExerciseType.Key"/>, value of <see cref="Exercise.Type"/>).</summary>
    protected abstract string TypeKey { get; }

    /// <summary>Registry of the exercise types – for derived additional endpoints (/check, /generate).</summary>
    protected ExerciseTypeRegistry Registry => registry;

    /// <summary>
    /// Evaluates answers at the catalog endpoint via the type's <see cref="IExerciseType.Check"/> (a single
    /// source of truth). Derived controllers whose type offers a direct check expose their
    /// thin <c>/check</c> action on top of it.
    /// </summary>
    protected async Task<ActionResult<CheckResult>> RunCheckAsync(int subjectId, int chapterId, int exerciseId, CheckDto body, CancellationToken ct = default)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId, ct);
        if (exercise is null) return NotFound();
        return registry.Require(TypeKey).Check(exercise.ConfigJson, body.Answers, body.Seed) is { } result
            ? result
            : this.ProblemWithCode(ApiErrors.ValidationError, "This exercise type does not support answer checking.");
    }

    /// <summary>
    /// Type-specific validation of the config on create/change. Default: none. Derived controllers
    /// override this to e.g. check store references (vocabulary keys); return value = error text (→ 400)
    /// or <c>null</c> if everything is fine.
    /// </summary>
    protected virtual Task<string?> ValidateConfigAsync(int subjectId, TConfig config, CancellationToken ct = default) =>
        Task.FromResult<string?>(null);

    /// <summary>
    /// Normalizes the config before saving (default: unchanged). Derived controllers override this
    /// to establish server-side invariants – e.g. assigning exercise-wide unique IDs when the
    /// caller (like the create form) does not maintain them itself.
    /// </summary>
    protected virtual void NormalizeConfig(TConfig config) { }

    /// <summary>
    /// Asynchronous normalization before saving (default: nothing). Derived controllers override this
    /// when the invariant needs DB access – e.g. vocabulary exercises that create inline used words in the store
    /// and link them with their store ID. Runs after <see cref="NormalizeConfig"/> and may use <c>SaveChanges</c>.
    /// </summary>
    protected virtual Task NormalizeConfigAsync(int subjectId, TConfig config, CancellationToken ct = default) => Task.CompletedTask;

    /// <summary>
    /// Shapes the config for the response (default: as stored). Derived controllers override this
    /// to fill derived, non-persisted fields – e.g. the HATEOAS link <c>_self</c> per vocabulary entry from
    /// its ID. Purely computational (no DB access), since it is called per row of the list.
    /// </summary>
    protected virtual TConfig ConfigForResponse(Exercise exercise) => ConfigOf(exercise);

    /// <summary>
    /// Runs after saving on create/change (default: nothing). Derived controllers override this
    /// when they – beyond the pure ConfigJson – need to maintain dependent rows that need the just-assigned
    /// <see cref="Exercise.Id"/> (e.g. vocabulary exercises that materialize their items into their own table).
    /// <paramref name="isCreate"/> distinguishes POST (initial creation) from PUT (replacement).
    /// </summary>
    protected virtual Task AfterSaveAsync(Exercise exercise, TConfig config, bool isCreate, CancellationToken ct = default) => Task.CompletedTask;

    /// <summary>DbContext for derived controllers with additional endpoints beyond pure CRUD.</summary>
    protected PuglingDbContext Db => db;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // No default for `ct` in the helpers: it would make the call site look correct while the client's
    // cancellation fizzles out - and neither CA2016 nor the signature guard sees an omitted optional
    // argument. Without a default the compiler forces you to pass it on.
    private Task<bool> ChapterExists(int subjectId, int chapterId, CancellationToken ct) =>
        db.Chapters.AnyAsync(c => c.Id == chapterId && c.SubjectId == subjectId, ct);

    /// <summary>Checks that a set category belongs to the exercise's subject (prevents foreign subjects).</summary>
    private Task<bool> CategoryValid(int subjectId, int? categoryId, CancellationToken ct) =>
        categoryId is null
            ? Task.FromResult(true)
            : db.ExerciseCategories.AnyAsync(c => c.Id == categoryId && c.SubjectId == subjectId, ct);

    /// <summary>
    /// Checks the <b>write permission</b> (change) on an exercise: the catalog is global (every creator may find
    /// any exercise and adopt it into their study plans), but only whoever holds an owner or write grant may
    /// change it. Requires the exercise's grants to be loaded (<see cref="FindAsync"/> loads them along).
    /// Returns a <c>403</c> <see cref="ProblemDetails"/> if the permission is missing, otherwise <c>null</c>.
    /// </summary>
    protected ObjectResult? EnsureCanWrite(Exercise exercise) =>
        ExercisePermissionService.CanWrite(exercise.Grants, User.AdultId(), User.IsAdmin())
            ? null
            : this.ProblemWithCode(ApiErrors.NotAuthor, "You need owner or write permission to modify this exercise.");

    /// <summary>
    /// Checks the <b>administration permission</b> (owner only): deleting, granting/revoking permissions, toggling visibility.
    /// Requires loaded grants (see <see cref="FindAsync"/>).
    /// </summary>
    protected ObjectResult? EnsureCanAdminister(Exercise exercise) =>
        ExercisePermissionService.CanAdminister(exercise.Grants, User.AdultId(), User.IsAdmin())
            ? null
            : this.ProblemWithCode(ApiErrors.NotOwner, "Only an owner can delete this exercise or manage its permissions.");

    /// <summary>Loads an exercise of this type incl. its grants (for permission checking/display); basis for derived additional endpoints.</summary>
    protected Task<Exercise?> FindAsync(int subjectId, int chapterId, int exerciseId, CancellationToken ct) =>
        db.Exercises.Include(e => e.Category).Include(e => e.Grants)
            .FirstOrDefaultAsync(e => e.Id == exerciseId && e.ChapterId == chapterId
                && e.Type == TypeKey && e.Chapter!.SubjectId == subjectId, ct);

    /// <summary>Deserializes the typed configuration of an exercise (never null; falls back to default).</summary>
    protected TConfig ConfigOf(Exercise exercise) =>
        JsonSerializer.Deserialize<TConfig>(exercise.ConfigJson, JsonOptions) ?? new TConfig();

    /// <summary>Writes the typed configuration back into the exercise (JSON) – for derived additional endpoints.</summary>
    protected void SetConfig(Exercise exercise, TConfig config) =>
        exercise.ConfigJson = JsonSerializer.Serialize(config, JsonOptions);

    /// <summary>Projects an exercise; <paramref name="fid"/> is determined once per request (not per row). Expects loaded <see cref="Exercise.Grants"/>.</summary>
    protected ExerciseResponse<TConfig> Map(Exercise e, int? fid)
    {
        var isAdmin = User.IsAdmin();
        return new(e.Id, e.ChapterId, e.Type.ToString(), e.Title, e.OrderIndex, e.RewardPoints, e.CreatedAt, ConfigForResponse(e), e.SuggestedBonus,
            e.GradeMin, e.GradeMax, e.SchoolTypes, e.Source, e.CategoryId, e.Category?.Name,
            e.AuthorAdultId, ExercisePermissionService.CanWrite(e.Grants, fid, isAdmin), ExercisePermissionService.CanAdminister(e.Grants, fid, isAdmin),
            e.ExecutePublic, e.Grants.Count, e.Description,
            e.DefaultUseLeitner, e.DefaultRequireTypedTest, e.DefaultStage, e.DefaultItemCount);
    }

    /// <summary>List of the exercises of this type in the chapter.</summary>
    /// <param name="subjectId">Subject the chapter belongs to.</param>
    /// <param name="chapterId">Chapter whose exercises are read.</param>
    /// <param name="isOwn">Optional permission filter on write permission (owner/write grant; admin counts as <c>true</c>).</param>
    /// <param name="isOwner">Optional permission filter on administration permission (owner grant; admin counts as <c>true</c>).</param>
    /// <param name="skip">Number of entries to skip (paging).</param>
    /// <param name="take">Maximum number of hits (1..500). Total count in the <c>X-Total-Count</c> header.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ExerciseResponse<TConfig>>>> List(
        int subjectId, int chapterId,
        [FromQuery] bool? isOwn = null,
        [FromQuery] bool? isOwner = null,
        [FromQuery] int skip = 0, [FromQuery] int take = PagingExtensions.DefaultTake,
        CancellationToken ct = default)
    {
        if (!await ChapterExists(subjectId, chapterId, ct)) return NotFound();
        var fid = User.AdultId();
        var isAdmin = User.IsAdmin();

        var query = db.Exercises
            .AsNoTracking()
            .Include(e => e.Category)
            .Include(e => e.Grants)
            .Where(e => e.ChapterId == chapterId && e.Type == TypeKey);

        // isOwn/isOwner mirror the response fields and allow lists of "what may I change/manage".
        // An admin has both implicitly; so *true* filters return the normal list for an admin, *false* an empty one.
        if (isOwn is not null)
        {
            if (isOwn.Value)
            {
                if (!isAdmin)
                    query = query.Where(e => fid != null && e.Grants.Any(g => g.CreatorId == fid
                        && (g.Permission == GrantPermission.Owner || g.Permission == GrantPermission.Write)));
            }
            else
            {
                query = isAdmin
                    ? query.Where(_ => false)
                    : query.Where(e => fid == null || !e.Grants.Any(g => g.CreatorId == fid
                        && (g.Permission == GrantPermission.Owner || g.Permission == GrantPermission.Write)));
            }
        }

        if (isOwner is not null)
        {
            if (isOwner.Value)
            {
                if (!isAdmin)
                    query = query.Where(e => fid != null && e.Grants.Any(g => g.CreatorId == fid
                        && g.Permission == GrantPermission.Owner));
            }
            else
            {
                query = isAdmin
                    ? query.Where(_ => false)
                    : query.Where(e => fid == null || !e.Grants.Any(g => g.CreatorId == fid
                        && g.Permission == GrantPermission.Owner));
            }
        }

        var exercises = await query
            .OrderBy(e => e.OrderIndex).ThenBy(e => e.Id)
            .ToPagedListAsync(Response, skip, take, ct);
        return exercises.Select(e => Map(e, fid)).ToList();
    }

    /// <summary>A single exercise.</summary>
    [HttpGet("{exerciseId:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseResponse<TConfig>>> Get(int subjectId, int chapterId, int exerciseId, CancellationToken ct = default)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId, ct);
        return exercise is null ? NotFound() : Map(exercise, User.AdultId());
    }

    /// <summary>Creates an exercise of this type in the chapter.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseResponse<TConfig>>> Create(int subjectId, int chapterId, ExercisePayload<TConfig> body, CancellationToken ct = default)
    {
        if (!await ChapterExists(subjectId, chapterId, ct)) return NotFound();
        if (string.IsNullOrWhiteSpace(body.Title)) return this.ProblemWithCode(ApiErrors.ValidationError, "Title is required.");
        if (!await CategoryValid(subjectId, body.CategoryId, ct)) return this.ProblemWithCode(ApiErrors.InvalidReference, "Unknown category for this subject.");
        var config = body.Config ?? new TConfig();
        if (await ValidateConfigAsync(subjectId, config, ct) is { } createErr) return this.ProblemWithCode(ApiErrors.ValidationError, createErr);
        NormalizeConfig(config);
        await NormalizeConfigAsync(subjectId, config, ct);

        var exercise = new Exercise
        {
            ChapterId = chapterId,
            Type = TypeKey,
            Title = body.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(body.Description) ? null : body.Description.Trim(),
            OrderIndex = body.OrderIndex,
            RewardPoints = body.RewardPoints,
            ConfigJson = JsonSerializer.Serialize(config, JsonOptions),
            SuggestedBonus = body.SuggestedBonus,
            GradeMin = body.GradeMin,
            GradeMax = body.GradeMax,
            SchoolTypes = body.SchoolTypes,
            Source = string.IsNullOrWhiteSpace(body.Source) ? null : body.Source.Trim(),
            CategoryId = body.CategoryId,
            DefaultUseLeitner = body.DefaultUseLeitner,
            DefaultRequireTypedTest = body.DefaultRequireTypedTest,
            DefaultStage = body.DefaultStage,
            DefaultItemCount = body.DefaultItemCount,
            ExecutePublic = body.ExecutePublic,
            // Author = the creating creator (attribution). The edit/delete right runs through the owner grant
            // created below (RWX model), so ownership can be transferred/shared later.
            AuthorAdultId = User.AdultId(),
        };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync(ct);

        // The creator becomes the first owner (edit/delete/manage right). Without a fid (no creator profile)
        // the exercise stays ownerless like a system exercise - but then reachable only through [Authorize] anyway.
        if (User.AdultId() is int authorId)
        {
            var ownerGrant = new ExerciseGrant
            {
                ExerciseId = exercise.Id,
                CreatorId = authorId,
                Permission = GrantPermission.Owner,
                GrantedByAdultId = authorId,
            };
            db.ExerciseGrants.Add(ownerGrant);
            await db.SaveChangesAsync(ct);
            // No `exercise.Grants.Add(ownerGrant)`: EF's relationship fixup has already attached the grant to
            // the loaded navigation on save. Attaching it twice counted it twice - the POST response reported
            // `grantCount: 2` while GET and /grants correctly returned 1.
        }

        await AfterSaveAsync(exercise, config, isCreate: true, ct);

        // Reload the category for CategoryName in the response (cheap; only on create).
        if (exercise.CategoryId is not null)
            exercise.Category = await db.ExerciseCategories.FindAsync([exercise.CategoryId], ct);

        return CreatedAtAction(nameof(Get), new { subjectId, chapterId, exerciseId = exercise.Id }, Map(exercise, User.AdultId()));
    }

    /// <summary>Replaces an exercise completely (incl. config).</summary>
    [HttpPut("{exerciseId:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseResponse<TConfig>>> Update(int subjectId, int chapterId, int exerciseId, ExercisePayload<TConfig> body, CancellationToken ct = default)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId, ct);
        if (exercise is null) return NotFound();
        if (EnsureCanWrite(exercise) is { } forbidden) return forbidden;
        if (string.IsNullOrWhiteSpace(body.Title)) return this.ProblemWithCode(ApiErrors.ValidationError, "Title is required.");
        if (!await CategoryValid(subjectId, body.CategoryId, ct)) return this.ProblemWithCode(ApiErrors.InvalidReference, "Unknown category for this subject.");
        // Execute visibility is an owner right (controlled sharing) - a write grantee must not toggle it.
        if (body.ExecutePublic != exercise.ExecutePublic && EnsureCanAdminister(exercise) is { } adminForbidden) return adminForbidden;
        var config = body.Config ?? new TConfig();
        if (await ValidateConfigAsync(subjectId, config, ct) is { } updateErr) return this.ProblemWithCode(ApiErrors.ValidationError, updateErr);
        NormalizeConfig(config);

        exercise.ExecutePublic = body.ExecutePublic;
        exercise.Title = body.Title.Trim();
        exercise.Description = string.IsNullOrWhiteSpace(body.Description) ? null : body.Description.Trim();
        exercise.OrderIndex = body.OrderIndex;
        exercise.RewardPoints = body.RewardPoints;
        exercise.ConfigJson = JsonSerializer.Serialize(config, JsonOptions);
        exercise.SuggestedBonus = body.SuggestedBonus;
        exercise.GradeMin = body.GradeMin;
        exercise.GradeMax = body.GradeMax;
        exercise.SchoolTypes = body.SchoolTypes;
        exercise.Source = string.IsNullOrWhiteSpace(body.Source) ? null : body.Source.Trim();
        exercise.CategoryId = body.CategoryId;
        exercise.DefaultUseLeitner = body.DefaultUseLeitner;
        exercise.DefaultRequireTypedTest = body.DefaultRequireTypedTest;
        exercise.DefaultStage = body.DefaultStage;
        exercise.DefaultItemCount = body.DefaultItemCount;
        await db.SaveChangesAsync(ct);
        await AfterSaveAsync(exercise, config, isCreate: false, ct);

        // Refresh the navigation after a possibly changed CategoryId so that CategoryName is right.
        exercise.Category = exercise.CategoryId is null
            ? null
            : await db.ExerciseCategories.FindAsync([exercise.CategoryId], ct);

        return Map(exercise, User.AdultId());
    }

    /// <summary>Deletes an exercise. Not possible while it is embedded in a study plan or a class test.</summary>
    [HttpDelete("{exerciseId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int subjectId, int chapterId, int exerciseId, CancellationToken ct)
    {
        var exercise = await FindAsync(subjectId, chapterId, exerciseId, ct);
        if (exercise is null) return NotFound();
        if (EnsureCanAdminister(exercise) is { } forbidden) return forbidden;
        /*
         * Verwendete Übungen schützen: der FK PlanPosition→Exercise ist Restrict (sonst 500 statt klarer
         * Fehler). Die Zählung kommt aus derselben Quelle wie die Verwendungs-Anzeige, und die Meldung nennt
         * die Verwendungen, die der Aufrufer **nicht sehen kann** – vorher log ein „nirgends" in der Anzeige
         * gegen ein 409 hier, und der Autor hatte keinen Weg, den Widerspruch aufzulösen (Anmerkung 14).
         */
        var usage = await ExerciseUsageQueries.CountBlockingAsync(db, exerciseId, User.AdultId(), ct);
        if (usage.Any)
            return this.ProblemWithCode(ApiErrors.ExerciseInUse, ExerciseUsageQueries.Explain(usage));
        db.Exercises.Remove(exercise);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
