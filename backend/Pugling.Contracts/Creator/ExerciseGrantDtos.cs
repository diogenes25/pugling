namespace Pugling.Contracts.Creator;

// Vertrag der RWX-Rechtevergabe an Übungen (mehrere Owner + Write/Execute je Creator).

/// <summary>Ein vergebenes Recht an einen Creator.</summary>
public record GrantResponse(int CreatorId, string CreatorName, GrantPermission Permission,
    int? GrantedByFatherId, DateTime CreatedAt);

/// <summary>Eingabe zum Vergeben eines Rechts.</summary>
public record AddGrantDto(int CreatorId, GrantPermission Permission);
