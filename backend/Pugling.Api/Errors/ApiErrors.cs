using System.Reflection;

namespace Pugling.Api.Errors;

/// <summary>
/// Zentrale Registry aller Fehler-Codes der API. Wire-Strings sind snake_case und <b>stabiler
/// Vertragsbestandteil</b> – nie umbenennen, nur additiv erweitern. Jeder Code trägt seinen
/// kanonischen HTTP-Status; der Aufrufer muss den Status nicht mehr getrennt angeben.
/// </summary>
public static class ApiErrors
{
    // ── Generisch / status-getriebene Defaults (auch Middleware-, Auth- und Framework-Pfade) ──

    /// <summary>Modell-/Eingabevalidierung fehlgeschlagen (400).</summary>
    public static readonly ApiError ValidationError = new("validation_error", 400, "Invalid request.");
    /// <summary>Generischer Bad-Request-Default für 400 ohne spezifischeren Code.</summary>
    public static readonly ApiError BadRequest = new("bad_request", 400, "Invalid request.");
    /// <summary>Im Request-Body referenzierte Entität existiert nicht / gehört nicht zum Kontext (400).</summary>
    public static readonly ApiError InvalidReference = new("invalid_reference", 400, "Invalid request.");
    /// <summary>Kein/ungültiges Token – Authentifizierung erforderlich (401).</summary>
    public static readonly ApiError Unauthorized = new("unauthorized", 401, "Authentication required.");
    /// <summary>Login mit falscher Id/PIN (401).</summary>
    public static readonly ApiError InvalidCredentials = new("invalid_credentials", 401, "Invalid credentials.");
    /// <summary>Zugriff verweigert (falsche Rolle / fremde Ressource) (403).</summary>
    public static readonly ApiError Forbidden = new("forbidden", 403, "Access denied.");
    /// <summary>Übung eines anderen Autors – nur der Autor darf ändern/löschen (403).</summary>
    public static readonly ApiError NotAuthor = new("not_author", 403, "Access denied.");
    /// <summary>Ressource nicht gefunden / nicht eigenes Kind (404).</summary>
    public static readonly ApiError NotFound = new("not_found", 404, "Resource not found.");
    /// <summary>Generischer Konflikt-Default für 409 ohne spezifischeren Code.</summary>
    public static readonly ApiError Conflict = new("conflict", 409, "Conflict.");
    /// <summary>Nebenläufige Kollision (Doppelklick/Retry) – bitte erneut versuchen (409).</summary>
    public static readonly ApiError ConcurrencyConflict = new("concurrency_conflict", 409, "Conflict.");
    /// <summary>Zu viele Anfragen – Rate-Limit greift (429).</summary>
    public static readonly ApiError RateLimited = new("rate_limited", 429, "Too many requests.");
    /// <summary>Unerwarteter Serverfehler (500).</summary>
    public static readonly ApiError Internal = new("internal_error", 500, "An unexpected error occurred.");
    /// <summary>Auffang-Code für sonst nicht abgebildete HTTP-Status (Status variabel).</summary>
    public static readonly ApiError HttpError = new("http_error", 0, "Error.");

    // ── Fachlich (je eine konkrete Geschäftsbedingung) ──

    /// <summary>Skin ist bereits freigeschaltet (409).</summary>
    public static readonly ApiError SkinAlreadyUnlocked = new("skin_already_unlocked", 409, "Skin already unlocked.");
    /// <summary>Skin ist (noch) nicht freigeschaltet – kann nicht ausgerüstet werden (400).</summary>
    public static readonly ApiError SkinNotUnlocked = new("skin_not_unlocked", 400, "Skin not unlocked.");
    /// <summary>Zu wenig Gems für den Skin-Kauf (400).</summary>
    public static readonly ApiError InsufficientGems = new("insufficient_gems", 400, "Not enough gems.");
    /// <summary>Zu wenig Münzen für den Shop-Kauf (400).</summary>
    public static readonly ApiError InsufficientCoins = new("insufficient_coins", 400, "Not enough coins.");
    /// <summary>Shop-Angebot ist deaktiviert / nicht mehr verfügbar (400).</summary>
    public static readonly ApiError ShopListingInactive = new("shop_listing_inactive", 400, "Shop listing no longer available.");
    /// <summary>Shop-Angebot ist nicht ausreichend auf Lager (409).</summary>
    public static readonly ApiError ShopInsufficientStock = new("shop_insufficient_stock", 409, "Shop listing is out of stock.");
    /// <summary>Kauf steht nicht (mehr) offen – bereits storniert (409).</summary>
    public static readonly ApiError PurchaseNotOpen = new("purchase_not_open", 409, "Purchase not open.");
    /// <summary>Nicht genug Einheiten im Inventar für die beantragte Aktivierungsmenge (400).</summary>
    public static readonly ApiError InsufficientInventory = new("insufficient_inventory", 400, "Not enough units in inventory.");
    /// <summary>Aktivierungsanfrage ist nicht (mehr) offen – bereits genehmigt/abgelehnt (409).</summary>
    public static readonly ApiError ActivationNotPending = new("activation_not_pending", 409, "Activation request is not pending.");
    /// <summary>Schlüssel existiert bereits (z. B. Vokabel-/Cloze-Key) (409).</summary>
    public static readonly ApiError DuplicateKey = new("duplicate_key", 409, "Key already exists.");
    /// <summary>Tag mit diesem Namen existiert bereits (400).</summary>
    public static readonly ApiError DuplicateTagName = new("duplicate_tag_name", 400, "Tag name already exists.");
    /// <summary>Übung wird in Lehrplan/Klassenarbeit verwendet und kann nicht gelöscht werden (409).</summary>
    public static readonly ApiError ExerciseInUse = new("exercise_in_use", 409, "Exercise is in use.");
    /// <summary>Übungs-Item (Vokabelpaar) existiert nicht / gehört nicht zu dieser Übung (404).</summary>
    public static readonly ApiError ItemNotFound = new("item_not_found", 404, "Exercise item not found.");
    /// <summary>Vokabel ist Grundform/in Übungen referenziert und kann nicht gelöscht werden (409).</summary>
    public static readonly ApiError VocabularyInUse = new("vocabulary_in_use", 409, "Vocabulary item is in use.");
    /// <summary>Position hat bereits Übungs-/Testdaten und kann nicht gelöscht werden (409).</summary>
    public static readonly ApiError PositionHasData = new("position_has_data", 409, "Position has practice/test data.");
    /// <summary>Lehrplan ist gerade nicht aktiv/spielbar (403).</summary>
    public static readonly ApiError PlanInactive = new("plan_inactive", 403, "Study plan is not active.");
    /// <summary>Test wurde bereits eingereicht (400).</summary>
    public static readonly ApiError TestAlreadySubmitted = new("test_already_submitted", 400, "Test already submitted.");
    /// <summary>Übung enthält keine prüfbaren Inhalte (400).</summary>
    public static readonly ApiError NoCheckableContent = new("no_checkable_content", 400, "No checkable content.");
    /// <summary>Stundenplan-Slot (Wochentag + Fach) ist bereits belegt (409).</summary>
    public static readonly ApiError TimetableSlotTaken = new("timetable_slot_taken", 409, "Timetable slot already taken.");

    /// <summary>
    /// Alle bekannten Codes (per Reflection über die Felder, einmalig materialisiert). Speist das
    /// OpenAPI-<c>enum</c> und den Drift-Regressionstest, damit die Liste nie von der Registry abweicht.
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
    /// Status → generischer Default-Code. Sicherheitsnetz für Framework-/Middleware-Antworten ohne
    /// spezifischen Code (leere 401/403/404/429, unbehandelte 500, nicht abgebildete Status). Der
    /// Auffang <see cref="HttpError"/> ist als Feld deklariert und damit in <see cref="AllCodes"/>.
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
