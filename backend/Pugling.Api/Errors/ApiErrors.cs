using System.Reflection;

namespace Pugling.Api.Errors;

/// <summary>
/// Central registry of all API error codes. Wire strings are snake_case and a <b>stable
/// contract element</b> – never rename them, only extend additively. Each code carries its
/// canonical HTTP status; the caller no longer has to specify the status separately.
/// </summary>
public static class ApiErrors
{
    // ── Generic / status-driven defaults (middleware, auth and framework paths too) ───────────

    /// <summary>Model/input validation failed (400).</summary>
    public static readonly ApiError ValidationError = new("validation_error", 400, "Invalid request.");
    /// <summary>Generic bad-request default for a 400 without a more specific code.</summary>
    public static readonly ApiError BadRequest = new("bad_request", 400, "Invalid request.");
    /// <summary>
    /// The body contains a field the contract does not know (400). A separate code next to
    /// <see cref="ValidationError"/>, because the cause is different: not "value wrong", but
    /// "field doesn't exist" – a typo or an outdated field on the caller's side. Before this rule
    /// the server silently accepted such fields and reported 201 (see docs/codequalitaet-gates-plan.md, L3).
    /// </summary>
    public static readonly ApiError UnknownField = new("unknown_field", 400, "Invalid request.");
    /// <summary>Entity referenced in the request body does not exist / does not belong to the context (400).</summary>
    public static readonly ApiError InvalidReference = new("invalid_reference", 400, "Invalid request.");
    /// <summary>No/invalid token – authentication required (401).</summary>
    public static readonly ApiError Unauthorized = new("unauthorized", 401, "Authentication required.");
    /// <summary>Login with wrong id/PIN (401).</summary>
    public static readonly ApiError InvalidCredentials = new("invalid_credentials", 401, "Invalid credentials.");
    /// <summary>Access denied (wrong role / resource not owned) (403).</summary>
    public static readonly ApiError Forbidden = new("forbidden", 403, "Access denied.");
    /// <summary>No write permission on the exercise – neither owner nor write grant (403).</summary>
    public static readonly ApiError NotAuthor = new("not_author", 403, "Access denied.");
    /// <summary>No owner permission – delete, permission management, and visibility toggling are owner-only (403).</summary>
    public static readonly ApiError NotOwner = new("not_owner", 403, "Access denied.");
    /// <summary>Resource not found / not the caller's own child (404).</summary>
    public static readonly ApiError NotFound = new("not_found", 404, "Resource not found.");
    /// <summary>Generic conflict default for a 409 without a more specific code.</summary>
    public static readonly ApiError Conflict = new("conflict", 409, "Conflict.");
    /// <summary>Concurrent collision (double-click/retry) – please retry (409).</summary>
    public static readonly ApiError ConcurrencyConflict = new("concurrency_conflict", 409, "Conflict.");
    /// <summary>Too many requests – rate limit applies (429).</summary>
    public static readonly ApiError RateLimited = new("rate_limited", 429, "Too many requests.");
    /// <summary>Unexpected server error (500).</summary>
    public static readonly ApiError Internal = new("internal_error", 500, "An unexpected error occurred.");
    /// <summary>Catch-all code for HTTP statuses not otherwise mapped (status variable).</summary>
    public static readonly ApiError HttpError = new("http_error", 0, "Error.");

    // ── Domain errors (one per concrete business condition) ──

    /// <summary>Skin is already unlocked (409).</summary>
    public static readonly ApiError SkinAlreadyUnlocked = new("skin_already_unlocked", 409, "Skin already unlocked.");
    /// <summary>Skin is not (yet) unlocked – cannot be equipped (400).</summary>
    public static readonly ApiError SkinNotUnlocked = new("skin_not_unlocked", 400, "Skin not unlocked.");
    /// <summary>Not enough gems for the skin purchase (400).</summary>
    public static readonly ApiError InsufficientGems = new("insufficient_gems", 400, "Not enough gems.");
    /// <summary>Not enough coins for the shop purchase (400).</summary>
    public static readonly ApiError InsufficientCoins = new("insufficient_coins", 400, "Not enough coins.");
    /// <summary>Shop listing is deactivated / no longer available (400).</summary>
    public static readonly ApiError ShopListingInactive = new("shop_listing_inactive", 400, "Shop listing no longer available.");
    /// <summary>Shop listing does not have sufficient stock (409).</summary>
    public static readonly ApiError ShopInsufficientStock = new("shop_insufficient_stock", 409, "Shop listing is out of stock.");
    /// <summary>Purchase is not (or no longer) open – already cancelled (409).</summary>
    public static readonly ApiError PurchaseNotOpen = new("purchase_not_open", 409, "Purchase not open.");
    /// <summary>Not enough units in inventory for the requested activation quantity (400).</summary>
    public static readonly ApiError InsufficientInventory = new("insufficient_inventory", 400, "Not enough units in inventory.");
    /// <summary>Activation request is not (or no longer) pending – already approved/rejected (409).</summary>
    public static readonly ApiError ActivationNotPending = new("activation_not_pending", 409, "Activation request is not pending.");
    /// <summary>Key already exists (e.g. vocabulary/cloze/media key) (409).</summary>
    public static readonly ApiError DuplicateKey = new("duplicate_key", 409, "Key already exists.");
    /// <summary>A textbook series without a subject may not host any exercise (B-106 T-01) (400).</summary>
    public static readonly ApiError SeriesWithoutSubject = new("series_without_subject", 400, "This textbook series has no subject assigned yet.");
    /// <summary>The child already has an award for this metric and threshold (409).</summary>
    public static readonly ApiError DuplicateAchievement = new("duplicate_achievement", 409, "The child already has an award for this metric and threshold.");
    /// <summary>This vocabulary entry is already an item of the exercise – a word may have only one item per exercise (409).</summary>
    public static readonly ApiError DuplicateVocabularyInExercise = new("duplicate_vocabulary_in_exercise", 409, "This vocabulary entry is already an item of the exercise.");
    /// <summary>
    /// The goal already has a key result with this scope and metric (409) – <c>RewardPerKeyResult</c> would
    /// otherwise pay twice for the same milestone.
    /// </summary>
    public static readonly ApiError DuplicateKeyResult = new("duplicate_key_result", 409, "This goal already has a key result with this scope and metric.");
    /// <summary>A test cannot be started on a free display stage (B-96) – a test without a question is not a test (400).</summary>
    public static readonly ApiError StageNotTestable = new("stage_not_testable", 400, "This stage is a free display stage and cannot be tested.");
    /// <summary>Image variant does not exist / does not belong to this asset (404).</summary>
    public static readonly ApiError MediaVariantNotFound = new("media_variant_not_found", 404, "Media variant not found.");
    /// <summary>The asset already has a variant for this purpose and format (409).</summary>
    public static readonly ApiError MediaVariantExists = new("media_variant_exists", 409, "A variant for this purpose and format already exists.");
    /// <summary>The image is already linked to this carrier (vocabulary/item/exercise) (409).</summary>
    public static readonly ApiError MediaAlreadyLinked = new("media_already_linked", 409, "The media asset is already linked to this object.");
    /// <summary>Media link does not exist / does not belong to this carrier (404).</summary>
    public static readonly ApiError MediaLinkNotFound = new("media_link_not_found", 404, "Media link not found.");
    /// <summary>"Different image" not possible – there is no permitted alternative for this carrier (409).</summary>
    public static readonly ApiError MediaNoAlternative = new("media_no_alternative", 409, "No alternative image available.");
    /// <summary>
    /// "Different image" on a card that does not show an image at all (409). Covers two cases with <b>one</b>
    /// response – the typed stage (there a motif would give away the answer) and the missing match –
    /// so the error does not reveal whether an image would even <i>exist</i>.
    /// </summary>
    public static readonly ApiError MediaNotOnCard = new("media_not_on_card", 409, "This card does not show an image.");
    /// <summary>The uploaded file could not be decoded as an image (400).</summary>
    public static readonly ApiError MediaNotAnImage = new("media_not_an_image", 400, "The uploaded file is not a readable image.");
    /// <summary>The uploaded file exceeds the allowed maximum (400).</summary>
    public static readonly ApiError MediaUploadTooLarge = new("media_upload_too_large", 400, "The uploaded file is too large.");
    /// <summary>A tag with this name already exists (400).</summary>
    public static readonly ApiError DuplicateTagName = new("duplicate_tag_name", 400, "Tag name already exists.");
    /// <summary>
    /// The email address is already assigned to an account (409). <c>Account.Email</c> carries a
    /// filtered unique index; without this error, registering a second account with the same
    /// address would have returned <b>500</b>, and a form could not show the reason.
    /// </summary>
    public static readonly ApiError DuplicateEmail = new("duplicate_email", 409, "Email already in use.");
    /// <summary>
    /// The subject teacher already has this name (409). The name is unique per creator
    /// (<c>CreatorProfile(OwnerAdultId, Name)</c>) – it is the display name in the profile picker.
    /// </summary>
    public static readonly ApiError DuplicateProfileName = new("duplicate_profile_name", 409, "A profile with this name already exists.");
    /// <summary>Exercise is used in a study plan/class test and cannot be deleted (409).</summary>
    public static readonly ApiError ExerciseInUse = new("exercise_in_use", 409, "Exercise is in use.");
    /// <summary>Exercise is not publicly executable and may not be assigned without an execute/write/owner permission (403).</summary>
    public static readonly ApiError ExerciseNotExecutable = new("exercise_not_executable", 403, "Exercise cannot be assigned.");
    /// <summary>
    /// A student tried to mark an exercise that is not assigned to them (403). Distinct from
    /// <see cref="Forbidden"/> on purpose: the caller does own the tag, only the exercise is out of reach.
    /// </summary>
    public static readonly ApiError ExerciseNotAssigned = new("exercise_not_assigned", 403, "Exercise is not assigned to this child.");
    /// <summary>
    /// A student tried to mark a store vocabulary item that does not occur in any assigned exercise (403).
    /// Its own code rather than reusing <see cref="ExerciseNotAssigned"/>: the code is a stable part of the
    /// contract and must not name an exercise where a word is meant, so a UI can tell the two cases apart.
    /// </summary>
    public static readonly ApiError VocabularyNotAssigned = new("vocabulary_not_assigned", 403, "Vocabulary is not assigned to this child.");
    /// <summary>The last owner of an exercise cannot be removed (409).</summary>
    public static readonly ApiError LastOwner = new("last_owner", 409, "Cannot remove the last owner.");
    /// <summary>Exercise item (vocabulary pair) does not exist / does not belong to this exercise (404).</summary>
    public static readonly ApiError ItemNotFound = new("item_not_found", 404, "Exercise item not found.");
    /// <summary>Vocabulary is a base form/referenced in exercises and cannot be deleted (409).</summary>
    public static readonly ApiError VocabularyInUse = new("vocabulary_in_use", 409, "Vocabulary item is in use.");
    /// <summary>Position already has practice/test data and cannot be deleted (409).</summary>
    public static readonly ApiError PositionHasData = new("position_has_data", 409, "Position has practice/test data.");
    /// <summary>Study plan is currently not active/playable (403).</summary>
    public static readonly ApiError PlanInactive = new("plan_inactive", 403, "Study plan is not active.");
    /// <summary>Test has already been submitted (400).</summary>
    public static readonly ApiError TestAlreadySubmitted = new("test_already_submitted", 400, "Test already submitted.");
    /// <summary>All test attempts of the period are used up (409).</summary>
    public static readonly ApiError TestAttemptsExhausted = new("test_attempts_exhausted", 409, "No test attempts left.");
    /// <summary>Exercise contains no checkable content (400).</summary>
    public static readonly ApiError NoCheckableContent = new("no_checkable_content", 400, "No checkable content.");
    /// <summary>
    /// The exercise is <b>not yet filled</b>: its type carries its content as an item table
    /// (<see cref="Exercises.StoreResolution.ItemTable"/>), but it does not have a single item (400).
    /// Deliberately separate from <see cref="NoCheckableContent"/>: there, "no checkable tasks" is a
    /// <i>property of the type</i> (essay), here it is an incomplete data state the author can fix.
    /// </summary>
    public static readonly ApiError ExerciseEmpty = new("exercise_empty", 400, "Exercise has no content yet.");
    /// <summary>
    /// The tag snapshot would have matched **no** vocabulary – the exercise remains unchanged (400).
    /// A separate code instead of <see cref="ValidationError"/>, because a caller (AI creator, REST tutorial)
    /// must distinguish this from "no tag sent at all": here a *different* tag helps, there it's a bugfix.
    /// </summary>
    public static readonly ApiError NoTagMatches = new("no_tag_matches", 400, "No vocabulary matched these tags.");
    /// <summary>Timetable slot (weekday + subject) is already taken (409).</summary>
    public static readonly ApiError TimetableSlotTaken = new("timetable_slot_taken", 409, "Timetable slot already taken.");
    /// <summary>The exercise carries a type key the <see cref="Exercises.ExerciseTypeRegistry"/> does not know – a data integrity error, not a user error (500).</summary>
    public static readonly ApiError UnknownExerciseType = new("unknown_exercise_type", 500, "The exercise has an unknown type.");
    /// <summary>Remark does not exist or is not visible to the caller (404).</summary>
    public static readonly ApiError RemarkNotFound = new("remark_not_found", 404, "Remark not found.");
    /// <summary>Comment in the history does not exist, belongs to a different remark, or a different account (404).</summary>
    public static readonly ApiError RemarkCommentNotFound = new("remark_comment_not_found", 404, "Remark comment not found.");
    /// <summary>Cross-account access (<c>scope=all</c>) is not open on this instance (403).</summary>
    public static readonly ApiError RemarkScopeForbidden = new("remark_scope_forbidden", 403, "Reading across accounts is disabled on this instance.");

    /// <summary>
    /// All known codes (materialized once via reflection over the fields). Feeds the
    /// OpenAPI <c>enum</c> and the drift regression test, so the list never diverges from the registry.
    /// </summary>
    public static readonly IReadOnlyList<string> AllCodes =
    [
        .. typeof(ApiErrors)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(ApiError))
            .Select(f => ((ApiError)f.GetValue(null)!).Code)
            .Distinct()
            .OrderBy(c => c, StringComparer.Ordinal),
    ];

    /// <summary>
    /// Status → generic default code. Safety net for framework/middleware responses without
    /// a specific code (empty 401/403/404/429, unhandled 500, unmapped statuses). The
    /// catch-all <see cref="HttpError"/> is declared as a field and therefore included in <see cref="AllCodes"/>.
    /// </summary>
    public static ApiError ForStatus(int status) => status switch
    {
        400 => BadRequest,
        401 => Unauthorized,
        403 => Forbidden,
        404 => NotFound,
        409 => Conflict,
        429 => RateLimited,
        >= 500 => Internal,
        _ => HttpError with { Status = status },
    };
}
