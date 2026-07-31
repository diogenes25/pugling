namespace Pugling.Agent.Creator;

/// <summary>
/// The run failed on something the user can fix (model does not deliver a usable draft,
/// Ollama unreachable, …). Printed as plain text in <c>Program</c> - without a stack trace.
/// </summary>
public sealed class AgentException(string message, Exception? innerException = null)
    : Exception(message, innerException);
