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

    /// <summary>Creativity. Keep it low - we want correct tasks, not surprises.</summary>
    [Range(0d, 2d)]
    public double Temperature { get; set; } = 0.4;

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
