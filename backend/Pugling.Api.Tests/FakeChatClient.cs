using Microsoft.Extensions.AI;

namespace Pugling.Api.Tests;

/// <summary>
/// Ein <see cref="IChatClient"/>, der vorbereitete Antworten ausgibt, statt ein Modell zu fragen.
/// Damit läuft die komplette Creator-Pipeline im Test gegen den echten In-Process-Server – ohne
/// laufendes Ollama und ohne die Unschärfe eines Sprachmodells. Geprüft wird also genau das, was
/// unter unserer Kontrolle steht: Regeln, Reparatur-Runde, Abbildung auf die API und der Selbsttest.
/// </summary>
public sealed class FakeChatClient(params string[] responses) : IChatClient
{
    /// <summary>Wie oft das „Modell" gefragt wurde – macht die Reparatur-Runde sichtbar.</summary>
    public int Calls { get; private set; }

    /// <summary>Die zuletzt gestellten Nachrichten (um Prompt-Inhalte zu prüfen).</summary>
    public IReadOnlyList<ChatMessage> LastMessages { get; private set; } = [];

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        LastMessages = [.. messages];
        // Über die vorbereiteten Antworten hinaus wird die letzte wiederholt – so muss ein Test nur
        // so viele Antworten liefern, wie sein Ablauf unterscheidet.
        var text = responses[Math.Min(Calls++, responses.Length - 1)];
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Der Agent nutzt keine Streaming-Antworten.");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
