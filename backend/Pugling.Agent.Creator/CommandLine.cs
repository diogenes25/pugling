using System.Globalization;

namespace Pugling.Agent.Creator;

/// <summary>
/// Incorrect invocation syntax. Caught in <c>Program</c> and printed as short help - a typo should not
/// produce a stack trace.
/// </summary>
public sealed class AgentUsageException(string message) : Exception(message);

/// <summary>
/// Minimal parser for <c>verb --option value --flag</c>. Deliberately hand-written instead of a
/// command-line library: the agent has three verbs, and this keeps the project low on dependencies.
/// </summary>
public sealed class CommandLine
{
    private readonly Dictionary<string, string?> _options = new(StringComparer.OrdinalIgnoreCase);

    private CommandLine(string verb) => Verb = verb;

    /// <summary>The chosen verb (<c>create</c>, <c>briefing</c>, <c>types</c>, <c>help</c>).</summary>
    public string Verb { get; }

    /// <summary>Parses the arguments; without a verb, <c>help</c> applies.</summary>
    public static CommandLine Parse(string[] args)
    {
        var hasVerb = args.Length > 0 && !args[0].StartsWith('-');
        var verb = hasVerb ? args[0] : "help";
        var line = new CommandLine(verb);

        // Options start after the verb - and only from 0 if there was no verb at all. The earlier condition
        // (`verb == "help" ? 0 : 1`) confused "no verb given" with "the verb is help": a typed `help` was
        // then read as an option again and rejected as an unexpected argument.
        for (int i = hasVerb ? 1 : 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
                throw new AgentUsageException($"Unerwartetes Argument '{arg}' – Optionen beginnen mit '--'.");

            var name = arg[2..];
            // The value comes either as '--name=value' or as the next argument; otherwise it is a flag.
            if (name.Contains('='))
            {
                var split = name.Split('=', 2);
                line._options[split[0]] = split[1];
                continue;
            }

            var next = i + 1 < args.Length ? args[i + 1] : null;
            if (next is not null && !next.StartsWith("--", StringComparison.Ordinal))
            {
                line._options[name] = next;
                i++;
            }
            else
            {
                line._options[name] = null;
            }
        }

        return line;
    }

    /// <summary>Value of an option, or <c>null</c>.</summary>
    public string? Value(string name) => _options.TryGetValue(name, out var v) ? v : null;

    /// <summary>Is the switch set (with or without a value)?</summary>
    public bool Flag(string name) => _options.ContainsKey(name)
        && Value(name) is not ("false" or "0" or "no");

    /// <summary>Numeric option with a default value.</summary>
    public int Int(string name, int fallback) =>
        Value(name) is { } raw
            ? int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : throw new AgentUsageException($"--{name} erwartet eine Zahl, bekam '{raw}'.")
            : fallback;

    /// <summary>Required numeric option.</summary>
    public int RequiredInt(string name) =>
        Value(name) is null ? throw new AgentUsageException($"--{name} fehlt.") : Int(name, 0);

    /// <summary>Required text option.</summary>
    public string RequiredValue(string name) =>
        Value(name) is { Length: > 0 } value ? value : throw new AgentUsageException($"--{name} fehlt.");

    /// <summary>Comma-separated list (e.g. <c>--words apple,pear,plum</c>); empty if not set.</summary>
    public IReadOnlyList<string> List(string name) =>
        Value(name) is { Length: > 0 } raw
            ? [.. raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)]
            : [];
}
