using System.ComponentModel.DataAnnotations;

namespace Pugling.Agent.Creator;

/// <summary>
/// Settings for the local language model (section <c>Agent</c>). Deliberately separate from
/// <c>PuglingClientOptions</c>: one describes <b>what</b> generates, the other <b>where</b>
/// the result is written to.
/// </summary>
public sealed class AgentOptions
{
    /// <summary>Configuration section this is bound from.</summary>
    public const string SectionName = "Agent";

    /// <summary>Base URL of the Ollama server.</summary>
    [Required]
    public string Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Model name as in <c>ollama list</c>. It must be an <b>instruct</b> model that reliably delivers
    /// schema-conformant JSON (e.g. <c>qwen2.5:14b-instruct</c> or <c>llama3.1:8b</c>); roleplay finetunes
    /// or very small models fail at structured output.
    /// </summary>
    [Required]
    public string Model { get; set; } = "qwen2.5:14b-instruct";

    /// <summary>
    /// Model that reads photographed textbook pages (<c>fill --images</c>, B-375). It must take images; the text
    /// model above stays in charge of structuring the extract. Missing locally is a usage error naming
    /// <c>ollama pull</c>.
    /// </summary>
    [Required]
    public string VisionModel { get; set; } = "llama3.2-vision";

    /// <summary>Creativity. Keep it low - we want correct tasks, not surprises.</summary>
    [Range(0d, 2d)]
    public double Temperature { get; set; } = 0.4;

    /// <summary>
    /// Context window in tokens sent with every call (Ollama <c>num_ctx</c>). Left to the server, a 14B model ran
    /// with 32768 tokens and its key-value cache pushed three quarters of the model onto the CPU; a briefing plus a
    /// draft needs well under 8192. 0 = leave it to the Ollama server.
    /// </summary>
    [Range(0, 131072)]
    public int ContextLength { get; set; } = 8192;

    /// <summary>Timeout of a single model call; local models take noticeably long on CPU.</summary>
    [Range(10, 3600)]
    public int TimeoutSeconds { get; set; } = 180;

    /// <summary>
    /// How many times a draft that violates the deterministic rules goes back to the model with the
    /// concrete violations. 0 = no repair (abort on the first violation).
    /// </summary>
    [Range(0, 3)]
    public int RepairAttempts { get; set; } = 1;
}
