namespace Pugling.Contracts.Auth;

// Vertrag der Login-Ebene (api/v1/auth/…). Die Records sind reine Transportformen: kein Verhalten,
// keine Abhängigkeit auf Entities – damit ein Client sie ohne die API-Assembly verwenden kann.

/// <summary>Antwort aller Login-Endpunkte: JWT samt primärer Ebene fürs UI-Routing.</summary>
/// <param name="Token">Das ausgestellte JWT (Bearer).</param>
/// <param name="Role">Primäre Ebene fürs UI-Routing (<c>Supervisor</c> bzw. <c>Student</c>).</param>
/// <param name="Id">Fachliche Id des eingeloggten Profils (Father- bzw. Child-Id, beim Konto-Login die Konto-Id).</param>
/// <param name="Name">Anzeigename.</param>
/// <param name="ExpiresAt">Ablaufzeitpunkt des Tokens (UTC).</param>
public record LoginResponse(string Token, string Role, int Id, string Name, DateTime ExpiresAt);

/// <summary>Vater-Login per fachlicher Father-Id + PIN.</summary>
public record FatherLoginDto(int FatherId, string Pin);

/// <summary>Sohn-Login per fachlicher Child-Id + PIN.</summary>
public record ChildLoginDto(int ChildId, string Pin);

/// <summary>Konto-zentrischer Login: ein Token über alle Rollen des Kontos.</summary>
public record AccountLoginDto(int AccountId, string Pin);
