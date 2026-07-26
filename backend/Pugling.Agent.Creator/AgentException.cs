namespace Pugling.Agent.Creator;

/// <summary>
/// Der Lauf ist an etwas gescheitert, das der Nutzer beheben kann (Modell liefert keinen brauchbaren
/// Entwurf, Ollama nicht erreichbar, …). Wird in <c>Program</c> als Klartext ausgegeben – ohne Stacktrace.
/// </summary>
public sealed class AgentException(string message, Exception? innerException = null)
    : Exception(message, innerException);
