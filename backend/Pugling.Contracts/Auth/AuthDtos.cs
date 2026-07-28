namespace Pugling.Contracts.Auth;

// Vertrag der Login-Ebene (api/v1/auth/…). Die Records sind reine Transportformen: kein Verhalten,
// keine Abhängigkeit auf Entities – damit ein Client sie ohne die API-Assembly verwenden kann.

/// <summary>Antwort aller Login-Endpunkte: JWT samt primärer Ebene fürs UI-Routing.</summary>
/// <param name="Token">Das ausgestellte JWT (Bearer).</param>
/// <param name="Role">
/// Primäre Ebene fürs UI-Routing: <c>Supervisor</c>, <c>Creator</c> oder <c>Student</c>. Rangfolge in dieser
/// Reihenfolge – ein Vater trägt Creator <i>und</i> Supervisor und gehört in die Betreuungs-Sicht, ein
/// <b>Lehrer</b> hat nur Creator und gehört in die Werkstatt. Das Token selbst trägt <i>alle</i> Rollen des
/// Kontos; dieses Feld sagt nur, wo die Oberfläche starten soll.
/// </param>
/// <param name="Id">Fachliche Id des eingeloggten Profils (Father- bzw. Child-Id, beim Konto-Login die Konto-Id).</param>
/// <param name="Name">Anzeigename.</param>
/// <param name="ExpiresAt">Ablaufzeitpunkt des Tokens (UTC).</param>
public record LoginResponse(string Token, string Role, int Id, string Name, DateTime ExpiresAt);

/// <summary>
/// Die eigene Identität aus dem Token (<c>GET auth/me</c>) – Konto, alle Rollen und die fachlichen Ids.
/// </summary>
/// <param name="AccountId">Konto-Id (Subjekt des Tokens); <c>null</c> bei einem Alt-Token ohne <c>aid</c>.</param>
/// <param name="Role">Primäre Ebene fürs Routing – siehe <see cref="LoginResponse"/>.</param>
/// <param name="Roles">Alle Rollen des Tokens. Bei einem Lehrer-Konto genau <c>["Creator"]</c>.</param>
/// <param name="FatherId">Fachliche Id des Erwachsenen (Creator/Supervisor), sonst <c>null</c>.</param>
/// <param name="ChildId">Fachliche Id des Kindes (Student), sonst <c>null</c>.</param>
/// <param name="Name">Anzeigename.</param>
public record MeResponse(int? AccountId, string Role, IReadOnlyList<string> Roles,
    int? FatherId, int? ChildId, string? Name);

/// <summary>
/// Selbstverwaltung des eigenen Kontos (<c>PATCH auth/me</c>) – für <b>jede</b> Erwachsenen-Rolle, also auch
/// für ein Lehrer-Konto, dem die Vater-Endpunkte verschlossen sind.
///
/// <para>
/// <b>PATCH-Semantik:</b> <c>null</c> heißt „nicht angegeben" (der Wert bleibt). Die E-Mail ist das einzige
/// löschbare Feld und braucht dafür <see cref="ClearEmail"/> – ohne den Schalter meldete ein Formular mit
/// leerem Feld „gespeichert", und die alte Adresse stünde weiter da.
/// </para>
/// </summary>
/// <param name="Name">Neuer Anzeigename; erscheint auch als Autor an den eigenen Übungen.</param>
/// <param name="Email">Neue E-Mail. Muss kontoweit eindeutig sein.</param>
/// <param name="ClearEmail">E-Mail entfernen. Gewinnt gegen <paramref name="Email"/>, wenn beides kommt.</param>
/// <param name="Pin">Neue Anmelde-PIN (wird gehasht). Leerer String = PIN entfernen, damit ein Konto
/// bewusst stillgelegt werden kann; <c>null</c> = unverändert.</param>
public record UpdateMyAccountDto(string? Name, string? Email, bool ClearEmail = false, string? Pin = null);

/// <summary>Vater-Login per fachlicher Father-Id + PIN.</summary>
public record FatherLoginDto(int FatherId, string Pin);

/// <summary>Sohn-Login per fachlicher Child-Id + PIN.</summary>
public record ChildLoginDto(int ChildId, string Pin);

/// <summary>Konto-zentrischer Login: ein Token über alle Rollen des Kontos.</summary>
public record AccountLoginDto(int AccountId, string Pin);
