namespace Pugling.Contracts.Creator;

// Vertrag der RWX-Rechtevergabe an Übungen (mehrere Owner + Write/Execute je Creator).

/// <summary>Ein vergebenes Recht an einen Creator.</summary>
public record GrantResponse(int CreatorId, string CreatorName, GrantPermission Permission,
    int? GrantedByFatherId, DateTime CreatedAt);

/// <summary>Eingabe zum Vergeben eines Rechts.</summary>
public record AddGrantDto(int CreatorId, GrantPermission Permission);

/// <summary>
/// Eingabe zum Freigeben oder <b>Zurückziehen</b> einer Übung – die Gegenbewegung zum Veröffentlichen.
/// </summary>
/// <param name="ExecutePublic">
/// <c>true</c> = jeder Creator darf sie einem Kind zuweisen. <c>false</c> = <b>zurückgezogen</b>: neue
/// Zuweisungen kann nur noch vornehmen, wer ein ausdrückliches Recht an der Übung hält (Owner/Write/Execute).
/// <para>
/// Was Zurückziehen <b>nicht</b> tut: laufende Lehrpläne anfassen. Die Prüfung greift beim Zuweisen, nicht
/// beim Spielen – ein Kind, das die Übung heute lernt, lernt sie weiter. Genau darum ist dies der Weg,
/// Material aus dem Verkehr zu nehmen, und nicht das Löschen (das eine benutzte Übung ohnehin verweigert).
/// </para>
/// </param>
public record SetExerciseSharingDto(bool ExecutePublic);

/// <summary>Der Freigabe-Stand einer Übung nach dem Umschalten.</summary>
/// <param name="Id">Die Übung.</param>
/// <param name="ExecutePublic">Ist sie für alle zuweisbar?</param>
/// <param name="GrantCount">
/// Wie viele ausdrückliche Rechte an ihr hängen. Nach dem Zurückziehen ist das der Kreis, der sie noch
/// zuweisen darf – bei <c>1</c> (nur der Owner selbst) ist sie damit praktisch aus dem Verkehr.
/// </param>
public record ExerciseSharingResponse(int Id, bool ExecutePublic, int GrantCount);
