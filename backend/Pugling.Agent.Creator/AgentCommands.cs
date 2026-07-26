using Pugling.Client;

namespace Pugling.Agent.Creator;

/// <summary>
/// Die Verben der Konsolen-App. Bewusst dünn: Argumente auflösen, die Pipeline rufen, das Ergebnis
/// verständlich ausgeben – die Fachlogik liegt in <see cref="CreatorPipeline"/> und den Strategien.
/// </summary>
public sealed class AgentCommands(CreatorApi creator, CreatorPipeline pipeline)
{
    /// <summary>Standardwerte des Auftrags, wenn die Kommandozeile schweigt.</summary>
    private const int DefaultItemCount = 10;
    private const int DefaultRewardPoints = 10;

    /// <summary>Führt das gewählte Verb aus und liefert den Exit-Code.</summary>
    public async Task<int> RunAsync(CommandLine command, CancellationToken ct)
    {
        switch (command.Verb.ToLowerInvariant())
        {
            case "types":
                await ShowTypesAsync(ct);
                return 0;
            case "briefing":
                await ShowBriefingAsync(command, ct);
                return 0;
            case "create":
                return await CreateAsync(command, ct);
            case "help":
                PrintUsage();
                return 0;
            default:
                throw new AgentUsageException($"Unbekanntes Kommando '{command.Verb}'.");
        }
    }

    /// <summary>Das Typ-Manifest des Servers – und welche Typen der Agent davon selbst erzeugt.</summary>
    private async Task ShowTypesAsync(CancellationToken ct)
    {
        var manifest = await creator.GetExerciseTypesAsync(ct);
        Console.WriteLine("Übungstypen des Servers (● = vom Agenten erzeugbar):");
        foreach (var type in manifest.OrderBy(t => t.Type))
        {
            var supported = pipeline.SupportedTypes.Contains(type.Type, StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($"  {(supported ? '●' : '○')} {type.Type,-16} {type.Label,-20} " +
                              $"Route: {type.AuthoringRoute,-18} Prüfung: {type.CheckMode}");
        }
    }

    /// <summary>Zeigt, worauf der Agent die Übung zuschneiden würde – ohne Sprachmodell.</summary>
    private async Task ShowBriefingAsync(CommandLine command, CancellationToken ct)
    {
        var request = await BuildRequestAsync(command, typeRequired: false, ct);
        var briefing = await pipeline.BriefAsync(request, ct);
        Console.WriteLine(briefing.ToPromptText());
    }

    /// <summary>Erzeugt eine Übung (oder plant sie im Trockenlauf) und berichtet das Ergebnis.</summary>
    private async Task<int> CreateAsync(CommandLine command, CancellationToken ct)
    {
        var request = await BuildRequestAsync(command, typeRequired: true, ct);
        var (briefing, outcome) = await pipeline.CreateAsync(request, ct);

        Console.WriteLine($"Kind:    {briefing.Name}"
                          + (briefing.Interests.Count > 0 ? $" (Interessen: {string.Join(", ", briefing.Interests)})" : ""));
        Console.WriteLine($"Ort:     {briefing.SubjectName} › {briefing.ChapterName}");
        Console.WriteLine($"Typ:     {outcome.TypeKey}");
        Console.WriteLine($"Titel:   {outcome.Title}");
        Console.WriteLine();

        if (!outcome.DraftAccepted)
        {
            Console.Error.WriteLine("Der Entwurf hat die Regeln auch nach der Reparatur-Runde nicht bestanden:");
            foreach (var violation in outcome.Violations) Console.Error.WriteLine($"  - {violation}");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Verworfener Entwurf:");
            Console.Error.WriteLine(outcome.DraftJson);
            return 1;
        }

        if (request.DryRun)
        {
            Console.WriteLine("Trockenlauf – es wurde nichts gespeichert. Entwurf:");
            Console.WriteLine(outcome.DraftJson);
            return 0;
        }

        Console.WriteLine($"Angelegt als Übung {outcome.ExerciseId}.");
        Console.WriteLine($"Selbsttest: {outcome.SelfTestPercent} %");

        if (outcome.RolledBack)
        {
            Console.Error.WriteLine("Der Selbsttest scheiterte – die Übung wurde wieder gelöscht (--strict).");
            return 1;
        }

        if (outcome.SelfTestPercent != 100)
        {
            Console.Error.WriteLine("Achtung: Aufgaben und Lösungen passen nicht vollständig zusammen. " +
                                    "Prüfe die Übung im Vater-Web oder wiederhole den Lauf mit --strict.");
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// Baut den Auftrag aus der Kommandozeile. Fach und Kapitel dürfen fehlen – dann nimmt der Agent
    /// das erste Fach mit Kapitel, damit ein schneller Blick (<c>briefing --child 1</c>) ohne Ids klappt.
    /// </summary>
    private async Task<GenerationRequest> BuildRequestAsync(CommandLine command, bool typeRequired, CancellationToken ct)
    {
        var childId = command.RequiredInt("child");
        var (subjectId, chapterId) = await ResolveLocationAsync(command, ct);

        return new GenerationRequest(
            ChildId: childId,
            SubjectId: subjectId,
            ChapterId: chapterId,
            TypeKey: typeRequired ? command.RequiredValue("type") : command.Value("type") ?? "Vocabulary",
            Topic: command.Value("topic"),
            ItemCount: command.Int("count", DefaultItemCount),
            Words: command.List("words"),
            UseWeakWords: command.Flag("use-weak"),
            SourceLang: command.Value("source-lang") ?? "en",
            TargetLang: command.Value("target-lang") ?? "de",
            RewardPoints: command.Int("points", DefaultRewardPoints),
            DryRun: command.Flag("dry-run"),
            Strict: command.Flag("strict"));
    }

    private async Task<(int SubjectId, int ChapterId)> ResolveLocationAsync(CommandLine command, CancellationToken ct)
    {
        if (command.Value("subject") is not null && command.Value("chapter") is not null)
            return (command.RequiredInt("subject"), command.RequiredInt("chapter"));

        var subjects = await creator.ListSubjectsAsync(ct);
        foreach (var subject in subjects.Where(s => command.Value("subject") is null || s.Id == command.Int("subject", 0)))
        {
            var chapters = await creator.ListChaptersAsync(subject.Id, ct);
            if (chapters.Count > 0) return (subject.Id, chapters[0].Id);
        }

        throw new AgentUsageException(
            "Es gibt kein Fach mit Kapitel – lege erst Katalog-Struktur an oder gib --subject und --chapter an.");
    }

    /// <summary>Kurzhilfe.</summary>
    public static void PrintUsage() => Console.WriteLine("""
        pugling-creator – erzeugt Pugling-Übungen mit einem lokalen Sprachmodell (Ollama).

        Verwendung:
          pugling-creator types
          pugling-creator briefing --child <id> [--subject <id> --chapter <id>] [--topic "..."] [--use-weak]
          pugling-creator create   --child <id> --type <Typ> [Optionen]

        Optionen von 'create':
          --child <id>          Kind, auf das zugeschnitten wird                (Pflicht)
          --type <Typ>          Vocabulary | Cloze | Translation | Grammar      (Pflicht)
          --subject <id>        Zielfach     (Standard: erstes Fach mit Kapitel)
          --chapter <id>        Zielkapitel  (Standard: erstes Kapitel des Fachs)
          --topic "..."         Thema/Lehrbuch-Unit
          --count <n>           Anzahl Aufgaben                                 (Standard 10)
          --words a,b,c         Pflicht-Wortschatz (unveränderlich)
          --use-weak            schwach beherrschte Wörter des Kindes verwenden
          --source-lang <code>  Lernsprache                                     (Standard en)
          --target-lang <code>  Muttersprache                                   (Standard de)
          --points <n>          Punkte der Übung                                (Standard 10)
          --dry-run             nur planen und drucken, nichts speichern
          --strict              Übung löschen, wenn der Selbsttest nicht 100 % erreicht

        Konfiguration: appsettings.json (Pugling:BaseUrl, Pugling:AccountId, Agent:Model),
        die PIN über User-Secrets oder Umgebung: setx Pugling__Pin 0000
        """);
}
