using Microsoft.Extensions.AI;

namespace Pugling.Api.Tests;

/// <summary>
/// An <see cref="IChatClient"/> that emits prepared responses instead of asking a model.
/// This lets the complete creator pipeline run in the test against the real in-process server –
/// without a running Ollama and without the fuzziness of a language model. What gets checked is
/// exactly what is under our control: rules, the repair round, mapping onto the API, and the self-test.
/// </summary>
public sealed class FakeChatClient(params string[] responses) : IChatClient
{
    /// <summary>How often the "model" was asked – makes the repair round visible.</summary>
    public int Calls { get; private set; }

    /// <summary>The most recently asked messages (to check prompt content).</summary>
    public IReadOnlyList<ChatMessage> LastMessages { get; private set; } = [];

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        LastMessages = [.. messages];
        // Beyond the prepared answers the last one is repeated - so a test only has to supply as many answers
        // as its flow distinguishes.
        var text = responses[Math.Min(Calls++, responses.Length - 1)];
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("The agent does not use streaming responses.");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
