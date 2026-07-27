namespace Pugling.Contracts.Shared;

// Ebenen-übergreifender Vertrag der Test-Anmerkungen: Supervisor wie Student erfassen über dieselbe
// Ressource, die Trennung passiert über die Sichtbarkeit im Controller, nicht über zwei Verträge.

/// <summary>
/// Der Kontext-Schnappschuss zum Zeitpunkt der Erfassung – der eigentliche Wert einer Anmerkung.
/// Das Widget füllt ihn automatisch; der Mensch tippt nur den Text.
/// </summary>
/// <param name="Route">Pfad im SPA, z. B. <c>/vater/kind/3/lernstand</c>.</param>
/// <param name="AppArea">Anwendungsbereich (<c>vater</c>/<c>sohn</c>).</param>
/// <param name="ChildId">Kind, das beim Erfassen ausgewählt war.</param>
/// <param name="ExerciseId">Übung, die beim Erfassen offen war.</param>
/// <param name="StudyPlanId">Lehrplan, der beim Erfassen offen war.</param>
/// <param name="PlanPositionId">Position, die beim Erfassen offen war.</param>
/// <param name="ContextJson">Zustands-Schnappschuss (Filter, Auswahl, offenes Modal) als rohes JSON – nur IDs und Filterwerte, nie geladene Entitäten.</param>
/// <param name="RecentErrorsJson">
/// Ringpuffer der letzten Fehler als rohes JSON. <b>Nur Metadaten</b> (Methode, Pfad, Status, Fehler-<c>code</c>,
/// Zeitstempel) – keine Bodies, Header oder Tokens: Der Login-Request trägt die PIN im Body.
/// </param>
public record RemarkContextDto(
    string? Route,
    string? AppArea,
    int? ChildId,
    int? ExerciseId,
    int? StudyPlanId,
    int? PlanPositionId,
    string? ContextJson,
    string? RecentErrorsJson);

/// <summary>Eine neue Anmerkung erfassen. Pflicht ist allein der Text – alles andere liefert das Widget oder bleibt leer.</summary>
/// <param name="Text">Der Beobachtungs-/Fragetext.</param>
/// <param name="Category">Optionale Einordnung; ohne Angabe <see cref="RemarkCategory.Unspecified"/> (der Skill zieht sie später nach).</param>
/// <param name="Context">Automatisch erfasster Kontext.</param>
/// <param name="ParentRemarkId">Optionaler Verweis auf die Anmerkung, aus der diese hervorging (setzt der Skill, nicht das Widget).</param>
public record CreateRemarkDto(
    string Text,
    RemarkCategory? Category,
    RemarkContextDto? Context,
    int? ParentRemarkId);

/// <summary>
/// Anmerkung ändern. PATCH-Semantik: <c>null</c> heißt „nicht angegeben" (der Wert bleibt), <b>nicht</b>
/// „leeren" – dafür gibt es die ausdrücklichen <c>Clear…</c>-Schalter.
/// </summary>
/// <param name="Text">Neuer Text.</param>
/// <param name="Category">Neue Einordnung.</param>
/// <param name="Status">Neuer Bearbeitungsstand.</param>
/// <param name="Answer">Die Antwort (schreibt der Skill zurück); bleibt auch bei <see cref="RemarkStatus.Planned"/> erhalten.</param>
/// <param name="AnsweredBy">Wer geantwortet hat, z. B. <c>claude-code</c>. Nur zusammen mit <paramref name="Answer"/> wirksam.</param>
/// <param name="ClearAnswer">Antwort samt Zeitstempel/Urheber leeren.</param>
/// <param name="ClearChild">Kind-Bezug leeren.</param>
/// <param name="ClearExercise">Übungs-Bezug leeren.</param>
/// <param name="ClearStudyPlan">Lehrplan-Bezug leeren.</param>
/// <param name="ClearPlanPosition">Positions-Bezug leeren.</param>
/// <param name="ClearParent">Verweis auf die Vorgänger-Anmerkung leeren.</param>
public record UpdateRemarkDto(
    string? Text,
    RemarkCategory? Category,
    RemarkStatus? Status,
    string? Answer,
    string? AnsweredBy,
    bool ClearAnswer = false,
    bool ClearChild = false,
    bool ClearExercise = false,
    bool ClearStudyPlan = false,
    bool ClearPlanPosition = false,
    bool ClearParent = false);

/// <summary>Eine Anmerkung, wie die API sie ausliefert.</summary>
/// <param name="Id">Die dem Menschen gezeigte „Log-Id" – der Schlüssel für „Beantworte die Frage 123".</param>
/// <param name="Text">Der Beobachtungs-/Fragetext.</param>
/// <param name="Category">Einordnung.</param>
/// <param name="Status">Bearbeitungsstand.</param>
/// <param name="Answer">Antwort, falls vorhanden.</param>
/// <param name="AnsweredAt">Zeitpunkt der Beantwortung.</param>
/// <param name="AnsweredBy">Urheber der Antwort.</param>
/// <param name="ParentRemarkId">Vorgänger-Anmerkung, falls diese aus einer Antwort hervorging.</param>
/// <param name="AccountId">Konto des Erfassers.</param>
/// <param name="AuthorRole">Rolle zum Zeitpunkt der Erfassung.</param>
/// <param name="IsOwn">Ob die Anmerkung vom abfragenden Konto stammt – das Widget zeigt nur eigene.</param>
/// <param name="Context">Der mitgeschnittene Kontext.</param>
/// <param name="UserAgent">Browserkennung.</param>
/// <param name="CreatedAt">Erfassungszeitpunkt (UTC).</param>
public record RemarkDto(
    int Id,
    string Text,
    RemarkCategory Category,
    RemarkStatus Status,
    string? Answer,
    DateTime? AnsweredAt,
    string? AnsweredBy,
    int? ParentRemarkId,
    int AccountId,
    ProfileRole AuthorRole,
    bool IsOwn,
    RemarkContextDto Context,
    string? UserAgent,
    DateTime CreatedAt);
