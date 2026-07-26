using System.Globalization;

namespace Pugling.Agent.Creator;

/// <summary>
/// Fehlerhafte Aufrufsyntax. Wird in <c>Program</c> abgefangen und als Kurzhilfe ausgegeben –
/// ein Tippfehler soll keinen Stacktrace produzieren.
/// </summary>
public sealed class AgentUsageException(string message) : Exception(message);

/// <summary>
/// Minimaler Parser für <c>verb --option wert --flag</c>. Bewusst handgeschrieben statt einer
/// Kommandozeilen-Bibliothek: der Agent hat drei Verben, und das Projekt bleibt so abhängigkeitsarm.
/// </summary>
public sealed class CommandLine
{
    private readonly Dictionary<string, string?> _options = new(StringComparer.OrdinalIgnoreCase);

    private CommandLine(string verb) => Verb = verb;

    /// <summary>Das gewählte Verb (<c>create</c>, <c>briefing</c>, <c>types</c>, <c>help</c>).</summary>
    public string Verb { get; }

    /// <summary>Zerlegt die Argumente; ohne Verb gilt <c>help</c>.</summary>
    public static CommandLine Parse(string[] args)
    {
        var verb = args.Length > 0 && !args[0].StartsWith('-') ? args[0] : "help";
        var line = new CommandLine(verb);

        for (int i = verb == "help" ? 0 : 1; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
                throw new AgentUsageException($"Unerwartetes Argument '{arg}' – Optionen beginnen mit '--'.");

            var name = arg[2..];
            // Wert entweder als '--name=wert' oder als nächstes Argument; sonst ist es ein Schalter.
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

    /// <summary>Wert einer Option oder <c>null</c>.</summary>
    public string? Value(string name) => _options.TryGetValue(name, out var v) ? v : null;

    /// <summary>Ist der Schalter gesetzt (mit oder ohne Wert)?</summary>
    public bool Flag(string name) => _options.ContainsKey(name)
        && Value(name) is not ("false" or "0" or "no");

    /// <summary>Zahl-Option mit Vorgabewert.</summary>
    public int Int(string name, int fallback) =>
        Value(name) is { } raw
            ? int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : throw new AgentUsageException($"--{name} erwartet eine Zahl, bekam '{raw}'.")
            : fallback;

    /// <summary>Pflicht-Zahl-Option.</summary>
    public int RequiredInt(string name) =>
        Value(name) is null ? throw new AgentUsageException($"--{name} fehlt.") : Int(name, 0);

    /// <summary>Pflicht-Text-Option.</summary>
    public string RequiredValue(string name) =>
        Value(name) is { Length: > 0 } value ? value : throw new AgentUsageException($"--{name} fehlt.");

    /// <summary>Komma-getrennte Liste (z. B. <c>--words apple,pear,plum</c>); leer, wenn nicht gesetzt.</summary>
    public IReadOnlyList<string> List(string name) =>
        Value(name) is { Length: > 0 } raw
            ? [.. raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)]
            : [];
}
