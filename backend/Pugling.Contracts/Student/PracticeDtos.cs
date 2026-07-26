namespace Pugling.Contracts.Student;

// Vertrag der Übungsschleife (Leitner) einer Lehrplan-Position. Server-autoritativ: der Server
// wählt die Karte, bewertet die Antwort und führt den Cursor – das Frontend rendert nur.

/// <summary>Eine laufende oder beendete Übungssitzung an einer Position.</summary>
public record SessionResponse(int Id, int PlanId, int PositionId, DateOnly Day,
    DateTime StartedAt, DateTime? EndedAt, int ActiveSeconds, int ReviewCount,
    PlayMode Mode, int Cursor, int Total);

/// <summary>
/// Start-Payload einer Übungssitzung. <paramref name="Mode"/> wählt den Ausspiel-Modus (Standard
/// <see cref="PlayMode.Lern"/> = server-geführt mit Cursor; <see cref="PlayMode.Info"/> = freies Üben ohne
/// Feedback). <paramref name="Day"/> nur zum Nachtragen (Vater).
/// </summary>
public record StartPracticeDto(DateOnly? Day, PlayMode Mode = PlayMode.Lern);

/// <summary>Meldet (aktive) Übungssekunden; der Server deckelt sie pro Heartbeat (Anti-Zeit-Cheat).</summary>
public record HeartbeatDto(int Seconds, bool Active);

/// <summary>
/// Eine Übungskarte – bewusst OHNE Lösung, außer bei Anzeige-/Selbsteinschätzungs-Stufen, die die
/// Lösung per Design aufdecken (der Server bewertet, nie das Frontend).
/// </summary>
public record PracticeCard(int ItemIndex, int Stage, string Type, string Prompt,
    string? Hint, int? AnswerLength, string? Reveal, IReadOnlyList<string>? Choices, string? AudioUrl);

/// <summary>Die nächste Karte im Lern-Modus (oder <c>Done</c>), server-geführt über den Sitzungs-Cursor.</summary>
public record NextResponse(PracticeCard? Card, bool Done, int Cursor, int Total);

/// <summary>
/// Die Antwort des Kindes auf eine Übungskarte. <paramref name="ItemIndex"/> adressiert das Inhalts-Atom
/// in der Übung. Getippte Stufen liefern <paramref name="GivenAnswer"/>, Anzeige-/Selbsteinschätzungs-
/// Stufen <paramref name="WasKnown"/>. Die Stufe erzwingt der Server; er bewertet – nie das Frontend.
/// </summary>
public record ReviewDto(int ItemIndex, string? GivenAnswer, bool? WasKnown);

/// <summary>
/// Ergebnis einer Leitner-Wiederholung (serverseitig bewertet) inkl. Boni fürs Feedback. <see cref="Next"/>
/// trägt im Lern-Modus direkt die nächste Karte (kein separater Roundtrip nötig); <see cref="Done"/> zeigt
/// das Ende des Laufs an. Bei nicht gewerteten Karten (nicht fällig / schon heute gewertet / nicht-Leitner)
/// sind die Punktefelder 0, Bewertung und Cursor laufen dennoch weiter.
/// </summary>
public record ReviewOutcome(bool WasCorrect, string Expected, int Awarded, int Box,
    DateOnly? DueOn, int Combo, int ComboBonus, int SpeedBonus, PracticeCard? Next, bool Done);
