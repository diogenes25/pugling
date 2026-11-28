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
    /// <summary>
    /// A feedback judged justified is final (409, B-767): its correction is booked (B-768) and no path books it back, so
    /// it can be neither opened again nor judged otherwise.
    /// </summary>
    public static readonly ApiError FeedbackJustified = new("feedback_justified", 409, "Feedback already judged justified.");
    /// <summary>
    /// "Justified" on a feedback without a test answer (409, B-767 decision 1): there is no graded answer to correct -
    /// a report from practice only, about the whole exercise, or from before the answer was recorded.
    /// </summary>
    public static readonly ApiError FeedbackWithoutTestAnswer = new("feedback_without_test_answer", 409, "Feedback refers to no test answer.");
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
    /// <summary>An exercise category with this name already exists in the subject (409).</summary>
    public static readonly ApiError DuplicateCategoryName = new("duplicate_category_name", 409, "A category with this name already exists in this subject.");
    /// <summary>
    /// Another publisher already carries the slug this name derives to (409). Creating is slug-idempotent,
    /// so renaming has to answer to the same rule - otherwise two rows share a display name (B-124).
    /// The message names the <b>slug</b>, not the name: after a rename the colliding row may well carry a
    /// different display name, and telling the caller "already uses this name" would send them looking for
    /// something they cannot find in the list.
    /// </summary>
    public static readonly ApiError DuplicatePublisher = new("duplicate_publisher", 409, "Another publisher already uses the slug this name derives to.");
    /// <summary>Another textbook series already carries the slug this name derives to (409). See <see cref="DuplicatePublisher"/>.</summary>
    public static readonly ApiError DuplicateTextbookSeries = new("duplicate_textbook_series", 409, "Another textbook series already uses this display name, or the slug it derives to.");
    /// <summary>Another interest tag already carries the slug this label derives to (409). See <see cref="DuplicatePublisher"/>.</summary>
    public static readonly ApiError DuplicateInterestTag = new("duplicate_interest_tag", 409, "Another interest tag already uses the slug this label derives to.");
    /// <summary>The child's free-text interest list is full – a new wish needs an existing one removed first (409).</summary>
    public static readonly ApiError InterestWishLimitReached = new("interest_wish_limit_reached", 409, "The free-text interest list is full.");
    /// <summary>Exercise is used in a study plan/class test and cannot be deleted (409).</summary>
    public static readonly ApiError ExerciseInUse = new("exercise_in_use", 409, "Exercise is in use.");
    /// <summary>
    /// A series unit or a whole series cannot be deleted: an exercise in it is used, or an objective milestone points at a
    /// unit (409, B-413). A code of its own next to <see cref="ExerciseInUse"/>: the milestone on a unit is a usage of no
    /// exercise, so a translation pointing at an exercise's usage display would send the user to a place that never shows it.
    /// </summary>
    public static readonly ApiError UnitContentInUse = new("unit_content_in_use", 409, "Unit content is in use.");
    /// <summary>
    /// The exercise is the build target of prerequisite exercises and cannot be deleted (409, B-700). A code of its own next
    /// to <see cref="ExerciseInUse"/>: the usage display never shows prerequisites, so a translation pointing there would send
    /// the user to a place that does not explain the lock (the B-413 trap).
    /// </summary>
    public static readonly ApiError ExerciseIsBuildTarget = new("exercise_is_build_target", 409, "Exercise is a build target.");
    /// <summary>
    /// The child has a round or a test of today open on the position, so its build level cannot change now (409, B-848):
    /// the round's frozen cards and the test's questions belong to the exercise of the old level.
    /// </summary>
    public static readonly ApiError BuildLevelInPlay = new("build_level_in_play", 409, "The position is being played.");
    /// <summary>
    /// The exercise is played in a study plan, so its items cannot be removed or reordered (409, B-205). A code of its
    /// own next to <see cref="ExerciseInUse"/>, because the rule differs: nothing is deleted, appending stays allowed,
    /// and the progress anchored on the item order is what is protected.
    /// </summary>
    public static readonly ApiError ExerciseItemsLocked = new("exercise_items_locked", 409, "Exercise items are locked by a study plan.");
    /// <summary>
    /// The interest tag dresses exercises and cannot be deleted (409). A code of its own next to
    /// <see cref="ExerciseInUse"/>, because the blocked resource is the other one: there the exercise is
    /// held by a plan, here the tag is held by exercises.
    /// </summary>
    public static readonly ApiError InterestTagInUse = new("interest_tag_in_use", 409, "Interest tag is in use.");
    /// <summary>
    /// A student tried to shrink a tag that a planned class test is linked to - remove an exercise from it or delete it
    /// (409, B-940). The tag is part of the test's material, and the preparation duty reads it live: the child would
    /// otherwise decide what it has to prepare. Adults are not locked, and a test whose day has passed no longer locks.
    /// </summary>
    public static readonly ApiError TagLockedByClassTest = new("tag_locked_by_class_test", 409, "Tag is linked to a planned class test.");
    /// <summary>
    /// Grammar topic is carried by exercises or units and cannot be deleted (409, B-155). Same line as
    /// <see cref="InterestTagInUse"/>: the assignment is authored information nothing else records, so a
    /// cascade would erase it silently.
    /// </summary>
    public static readonly ApiError GrammarTopicInUse = new("grammar_topic_in_use", 409, "Grammar topic is in use.");
    /// <summary>
    /// Subject is referenced by a child's key result or timetable entry and cannot be deleted (409).
    /// The line runs along whether the reference is REQUIRED, not along ownership: child-owned rows whose
    /// subject is optional (textbook, study plan, class test) keep losing only their assignment, and only
    /// the two mandatory ones block the delete (B-144).
    /// </summary>
    public static readonly ApiError SubjectInUse = new("subject_in_use", 409, "Subject is in use.");
    /// <summary>
    /// A series may not change or clear its subject while one of its exercises or units carries a grammar topic or category
    /// of the old subject (409, B-291). The subject boundary is checked when a carrier is written; a silent subject change
    /// would leave assignments that the carrier's next save rejects - and dropping them instead would erase authored data.
    /// </summary>
    public static readonly ApiError SeriesSubjectInUse = new("series_subject_in_use", 409, "The series' subject is in use.");
    /// <summary>
    /// Publisher is referenced by a series that is not the caller's own and cannot be deleted (409) - a
    /// foreign account's series, or an ownerless one of the shared catalog. A publisher has no owner on
    /// purpose, so this is the only thing standing between a shared catalog row and an unrelated creator's
    /// assignments (B-127).
    /// </summary>
    public static readonly ApiError PublisherInUse = new("publisher_in_use", 409, "Publisher is in use.");
    /// <summary>Exercise is not publicly executable and may not be assigned without an execute/write/owner permission (403).</summary>
    public static readonly ApiError ExerciseNotExecutable = new("exercise_not_executable", 403, "Exercise cannot be assigned.");
    /// <summary>
    /// An owner (or admin) tried to rate the exercise they administer (403, B-12). The rating is meant as the
    /// signal of the creators who USE someone else's material; the author's own stars would only inflate it.
    /// </summary>
    public static readonly ApiError CannotRateOwnExercise = new("cannot_rate_own_exercise", 403, "You cannot rate an exercise you own.");
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
    /// <summary>Study plan was already played by its child and cannot be handed to another child (409, B-673) or deleted (409, B-659).</summary>
    public static readonly ApiError PlanHasData = new("plan_has_data", 409, "Study plan has practice/test data.");
    /// <summary>Study plan is currently not active/playable (403).</summary>
    public static readonly ApiError PlanInactive = new("plan_inactive", 403, "Study plan is not active.");
    /// <summary>
    /// The card cannot be made easier (B-635): it is not the round's current card, it is already at the easiest form, or
    /// the position is no remediation and the card no learn card (409).
    /// </summary>
    public static readonly ApiError CannotEase = new("cannot_ease", 409, "This card cannot be made easier.");
    /// <summary>Test has already been submitted (400).</summary>
    public static readonly ApiError TestAlreadySubmitted = new("test_already_submitted", 400, "Test already submitted.");
    /// <summary>The bonus training is locked until today's daily goals are done (B-535; all goals when no plan has daily ones, B-611) (409).</summary>
    public static readonly ApiError ExtraTrainingLocked = new("extra_training_locked", 409, "Bonus training is locked.");
    /// <summary>
    /// The fixed-form bonus exercise is worth nothing more today: three runs of it were completed today (B-538). Its worth
    /// resets the next day; vocabulary is never refused for this (409).
    /// </summary>
    public static readonly ApiError ExtraExerciseDone = new("extra_exercise_done", 409, "Bonus exercise worth nothing more today.");
    /// <summary>
    /// The child's tower-game minutes of the day are used up (B-851): no new level starts until tomorrow; a level already
    /// running may finish within the grace time (409).
    /// </summary>
    public static readonly ApiError TowerGameTimeLimit = new("tower_game_time_limit", 409, "Tower game time used up for today.");
    /// <summary>The tower-game run has ended, or its grace time after the daily limit is over: it takes no more assignments (409).</summary>
    public static readonly ApiError TowerGameRunClosed = new("tower_game_run_closed", 409, "Tower game run is closed.");
    /// <summary>The tower game's chest cannot be opened: it is not full yet, or today's chest is already opened (409).</summary>
    public static readonly ApiError TowerGameChestNone = new("tower_game_chest_none", 409, "No game chest to open.");
    /// <summary>The homework is accepted and its coins are booked (B-760): it takes no further hand-in or verdict (409).</summary>
    public static readonly ApiError HomeworkAccepted = new("homework_accepted", 409, "Homework already accepted.");
    /// <summary>
    /// The homework waits for no hand-in (B-760): nothing handed in yet, or given back for rework and not handed in again. It can
    /// be accepted only with a new hand-in (409).
    /// </summary>
    public static readonly ApiError HomeworkNotSubmitted = new("homework_not_submitted", 409, "No new hand-in.");
    /// <summary>The bonus run has already been submitted (409).</summary>
    public static readonly ApiError ExtraRunCompleted = new("extra_run_completed", 409, "Bonus run already submitted.");
    /// <summary>
    /// The exercise changed since the bonus run started (B-545): the run is discarded unpaid, never graded against a
    /// key its questions did not come from, and the exercise is open again (409).
    /// </summary>
    public static readonly ApiError ExtraRunStale = new("extra_run_stale", 409, "Bonus run is out of date.");
    /// <summary>
    /// A position of the plan already practises exactly this set of words (B-584) - a review or the exercise they came
    /// from: a second position would be the same duty twice. An overlapping set is allowed (409).
    /// </summary>
    public static readonly ApiError ReviewAlreadyInPlan = new("review_already_in_plan", 409, "Review already in plan.");
    /// <summary>
    /// "Typed tests only" on a stage that is never graded (B-552): the goal could not be met. Its own code so the father's
    /// form can name it in German (400).
    /// </summary>
    public static readonly ApiError TypedTestUnreachable = new("typed_test_unreachable", 400, "Typed test unreachable.");
    /// <summary>
    /// The words come from another subject than the plan (B-584): the review belongs into a plan of their subject (400).
    /// Its own code because the father's form names this case in German - a generic validation error could not.
    /// </summary>
    public static readonly ApiError ReviewSubjectMismatch = new("review_subject_mismatch", 400, "Review words of another subject.");
    /// <summary>
    /// Today's bonus coin cap is reached and the exercise is a fixed-form one: its run would show the solutions for no
    /// coins, so it is neither started nor graded and can be played once coins are left again (409).
    /// </summary>
    public static readonly ApiError ExtraCapReached = new("extra_cap_reached", 409, "Bonus coin cap reached.");
    /// <summary>No card is due in a position the mixed review may draw from (B-764): nothing to repeat right now (409).</summary>
    public static readonly ApiError MixedReviewEmpty = new("mixed_review_empty", 409, "Nothing due for a mixed review.");
    /// <summary>The mixed review round has already been submitted (409).</summary>
    public static readonly ApiError MixedReviewCompleted = new("mixed_review_completed", 409, "Mixed review already submitted.");
    /// <summary>
    /// The mixed review round was drawn on an earlier day (B-765): its cards were due then, not now. It is discarded
    /// unpaid; starting again draws today's cards (409).
    /// </summary>
    public static readonly ApiError MixedReviewStale = new("mixed_review_stale", 409, "Mixed review is out of date.");
    /// <summary>No daily reward chest is waiting to be opened (B-527) - none earned yet, or all already opened (409).</summary>
    public static readonly ApiError DailyBoxNone = new("daily_box_none", 409, "No reward chest waiting.");
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
