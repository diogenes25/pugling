using System.Globalization;
using Pugling.Agent.Creator.Drafting;
using Pugling.Client;

namespace Pugling.Agent.Creator;

/// <summary>
/// The verbs of the console app. Deliberately thin: resolve arguments, call the pipeline, print the
/// result in an understandable way - the domain logic lives in <see cref="CreatorPipeline"/>, <see cref="ExamPlanner"/>
/// and the strategies.
/// </summary>
public sealed class AgentCommands(CreatorApi creator, CreatorPipeline pipeline, ExamPlanner exams)
{
    /// <summary>Default values for the request when the command line is silent.</summary>
    private const int DefaultItemCount = 10;
    private const int DefaultRewardPoints = 10;
    private const int DefaultExamPerType = 6;

    /// <summary>The types a class test consists of when none are named.</summary>
    private static readonly string[] DefaultExamTypes = ["Vocabulary", "Cloze", "Grammar"];

    /// <summary>Executes the chosen verb and returns the exit code.</summary>
    public async Task<int> RunAsync(CommandLine command, CancellationToken ct)
    {
        switch (command.Verb.ToLowerInvariant())
        {
            case "types":
                await ShowTypesAsync(ct);
                return 0;
            case "profiles":
                await ShowProfilesAsync(command, ct);
                return 0;
            case "briefing":
                await ShowBriefingAsync(command, ct);
                return 0;
            case "create":
                return await CreateAsync(command, ct);
            case "exam":
                return await ExamAsync(command, ct);
            case "help":
                PrintUsage();
                return 0;
            default:
                throw new AgentUsageException($"Unbekanntes Kommando '{command.Verb}'.");
        }
    }

    /// <summary>The server's type manifest - and which of those types the agent can generate itself.</summary>
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

    /// <summary>
    /// The creator profiles. With <c>--child</c>, in order of their fit plus a rationale - that is
    /// the answer to "which teacher knows this child's material?".
    /// </summary>
    private async Task ShowProfilesAsync(CommandLine command, CancellationToken ct)
    {
        var subjectId = command.Value("subject") is not null ? command.RequiredInt("subject") : (int?)null;

        if (command.Value("child") is not null)
        {
            var childId = command.RequiredInt("child");
            var matches = await creator.MatchProfilesAsync(childId, subjectId, ct);
            if (matches.Count == 0)
            {
                Console.WriteLine($"Kein Profil passt zu Kind {childId}. Lege eines an "
                                  + "(POST api/v1/creator/profiles) – Fach, Schulart, Klassenstufen und Buchreihe.");
                return;
            }

            Console.WriteLine($"Passende Profile für Kind {childId} (bestes zuerst):");
            foreach (var match in matches)
                Console.WriteLine($"  [{match.Profile.Id,3}] {match.Profile.Name,-40} Punkte: {match.Score,2}  "
                                  + $"{Describe(match.Reasons)}");
            return;
        }

        var profiles = await creator.ListProfilesAsync(subjectId: subjectId, ct: ct);
        if (profiles.Count == 0)
        {
            Console.WriteLine("Es gibt noch kein Creator-Profil. Ohne Profil arbeitet der Agent als Generalist.");
            return;
        }

        Console.WriteLine("Creator-Profile:");
        foreach (var profile in profiles)
            Console.WriteLine($"  [{profile.Id,3}] {profile.Name,-40} {profile.SubjectName ?? "(fachneutral)",-12} "
                              + $"{Grades(profile),-10} {profile.SeriesName ?? "(werkunabhängig)"}");
    }

    /// <summary>Shows what the agent would tailor the exercise to - without a language model.</summary>
    private async Task ShowBriefingAsync(CommandLine command, CancellationToken ct)
    {
        var request = await BuildRequestAsync(command, typeRequired: false, ct);
        var briefing = await pipeline.BriefAsync(request, ct);
        Console.WriteLine(briefing.ToPromptText());
    }

    /// <summary>Generates an exercise (or plans it as a dry run) and reports the result.</summary>
    private async Task<int> CreateAsync(CommandLine command, CancellationToken ct)
    {
        var request = await BuildRequestAsync(command, typeRequired: true, ct);
        var (briefing, outcome) = await pipeline.CreateAsync(request, ct);

        Console.WriteLine($"Profil:  {briefing.Profile?.Name ?? "(keines – Generalist)"}");
        Console.WriteLine(briefing.Individual
            ? $"Für:     {briefing.Audience}"
              + (briefing.Interests.Count > 0 ? $" (Interessen: {string.Join(", ", briefing.Interests)})" : "")
            : "Für:     den gemeinsamen Katalog (allgemeine Übung)");
        Console.WriteLine($"Ort:     {briefing.SubjectName} › {briefing.ChapterName}");
        if (briefing.Source is { } source) Console.WriteLine($"Quelle:  {source}");
        Console.WriteLine($"Typ:     {outcome.TypeKey}");
        Console.WriteLine($"Titel:   {outcome.Title}");
        Console.WriteLine();

        return Report(outcome, request.DryRun);
    }

    /// <summary>
    /// Generates an exercise-based class test: several exercises on the same material and - with a
    /// child given - the planned class test to go with them.
    /// </summary>
    private async Task<int> ExamAsync(CommandLine command, CancellationToken ct)
    {
        var request = await BuildRequestAsync(command, typeRequired: false, ct);
        var types = command.List("types") is { Count: > 0 } wanted ? wanted : DefaultExamTypes;
        var exam = new ExamRequest(request, types, command.Int("per-type", DefaultExamPerType),
            ParseDate(command, "date"), command.Value("title"));

        var outcome = await exams.RunAsync(exam, ct);

        Console.WriteLine($"Klausur: {outcome.Title}");
        Console.WriteLine();
        foreach (var part in outcome.Parts)
        {
            Console.WriteLine(part switch
            {
                { Error: { } error } => $"  ✗ {part.TypeKey,-12} gescheitert: {error}",
                { Outcome: { DraftAccepted: false } bad } =>
                    $"  ✗ {part.TypeKey,-12} Regelverstöße: {string.Join(" | ", bad.Violations)}",
                { Outcome: { RolledBack: true } back } =>
                    $"  ✗ {part.TypeKey,-12} zurückgenommen (Selbsttest {back.SelfTestPercent} %)",
                { Outcome: { ExerciseId: null } dry } => $"  · {part.TypeKey,-12} Trockenlauf: {dry.Title}",
                { Outcome: { } ok } =>
                    $"  ✓ {part.TypeKey,-12} Übung {ok.ExerciseId} „{ok.Title}“ (Selbsttest {ok.SelfTestPercent} %)",
                _ => $"  ? {part.TypeKey}",
            });
        }
        Console.WriteLine();

        if (outcome.ClassTestId is int classTestId)
            Console.WriteLine($"Klassenarbeit {classTestId} geplant, Tag „{outcome.TagName}“, "
                              + $"{outcome.ExerciseIds.Count} Übung(en) zugewiesen.");
        else if (request.DryRun)
            Console.WriteLine("Trockenlauf – es wurde nichts gespeichert.");
        else if (request.ChildId is null)
            Console.WriteLine($"{outcome.ExerciseIds.Count} Übung(en) im Katalog. Ohne --child entsteht keine "
                              + "Klassenarbeit; das Bündel hält die Quellenangabe zusammen.");

        if (outcome.Complete) return 0;

        Console.Error.WriteLine("Die Klausur ist unvollständig – prüfe die gescheiterten Teile oben.");
        return 1;
    }

    /// <summary>The shared result output of a single exercise run.</summary>
    private static int Report(GenerationOutcome outcome, bool dryRun)
    {
        if (!outcome.DraftAccepted)
        {
            Console.Error.WriteLine("Der Entwurf hat die Regeln auch nach der Reparatur-Runde nicht bestanden:");
            foreach (var violation in outcome.Violations) Console.Error.WriteLine($"  - {violation}");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Verworfener Entwurf:");
            Console.Error.WriteLine(outcome.DraftJson);
            return 1;
        }

        if (dryRun)
        {
            Console.WriteLine("Trockenlauf – es wurde nichts gespeichert. Entwurf:");
            Console.WriteLine(outcome.DraftJson);
            return 0;
        }

        // Die Rücknahme zuerst: sonst nennt die Erfolgsmeldung eine Übungs-Id, die es schon nicht mehr gibt.
        if (outcome.RolledBack)
        {
            Console.Error.WriteLine($"Der Selbsttest erreichte nur {outcome.SelfTestPercent} % – " +
                                    "die Übung wurde wieder gelöscht (--strict). Es wurde nichts gespeichert.");
            return 1;
        }

        Console.WriteLine($"Angelegt als Übung {outcome.ExerciseId}.");
        Console.WriteLine($"Selbsttest: {outcome.SelfTestPercent} %");

        if (outcome.SelfTestPercent != 100)
        {
            Console.Error.WriteLine("Achtung: Aufgaben und Lösungen passen nicht vollständig zusammen. " +
                                    "Prüfe die Übung im Vater-Web oder wiederhole den Lauf mit --strict.");
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// Builds the request from the command line. Either <c>--child</c> (individual) or <c>--profile</c>
    /// (general) must be present - without either, both the target audience and the subject knowledge
    /// would be missing. Subject and chapter may be omitted: then the profile's subject applies, otherwise
    /// the first subject with a chapter.
    /// </summary>
    private async Task<GenerationRequest> BuildRequestAsync(CommandLine command, bool typeRequired, CancellationToken ct)
    {
        var childId = command.Value("child") is not null ? command.RequiredInt("child") : (int?)null;
        var profileId = command.Value("profile") is not null ? command.RequiredInt("profile") : (int?)null;
        if (childId is null && profileId is null)
            throw new AgentUsageException(
                "Gib --child <id> für eine individuelle Übung oder --profile <id> für eine allgemeine an.");

        var profile = profileId is int id ? await LoadProfileAsync(id, ct) : null;
        var (subjectId, chapterId) = await ResolveLocationAsync(command, profile?.SubjectId, ct);

        return new GenerationRequest(
            ChildId: childId,
            ProfileId: profileId,
            UnitId: command.Value("unit") is not null ? command.RequiredInt("unit") : null,
            General: command.Flag("general"),
            SubjectId: subjectId,
            ChapterId: chapterId,
            TypeKey: typeRequired ? command.RequiredValue("type") : command.Value("type") ?? "Vocabulary",
            Topic: command.Value("topic"),
            ItemCount: command.Int("count", DefaultItemCount),
            Words: command.List("words"),
            UseWeakWords: command.Flag("use-weak"),
            // Ohne Angabe entscheiden Profil und dann die Vorgaben (en/de) – nicht die Kommandozeile.
            SourceLang: command.Value("source-lang"),
            TargetLang: command.Value("target-lang"),
            RewardPoints: command.Int("points", DefaultRewardPoints),
            DryRun: command.Flag("dry-run"),
            Strict: command.Flag("strict"));
    }

    /// <summary>Loads the named profile - only to know its subject as a default for the catalog location.</summary>
    private async Task<CreatorProfileResponse?> LoadProfileAsync(int profileId, CancellationToken ct)
    {
        try
        {
            return await creator.GetProfileAsync(profileId, ct);
        }
        catch (PuglingApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            throw new AgentUsageException(
                $"Creator-Profil {profileId} gibt es nicht – 'pugling-creator profiles' zeigt die vorhandenen.");
        }
    }

    private async Task<(int SubjectId, int ChapterId)> ResolveLocationAsync(CommandLine command,
        int? profileSubjectId, CancellationToken ct)
    {
        var wantedSubject = command.Value("subject") is not null ? command.RequiredInt("subject") : profileSubjectId;
        var wantedChapter = command.Value("chapter") is not null ? command.RequiredInt("chapter") : (int?)null;

        if (wantedSubject is { } subjectId && wantedChapter is { } chapterId) return (subjectId, chapterId);

        var subjects = await creator.ListSubjectsAsync(ct);
        foreach (var subject in subjects.Where(s => wantedSubject is null || s.Id == wantedSubject))
        {
            var chapters = await creator.ListChaptersAsync(subject.Id, ct);
            if (chapters.Count == 0) continue;

            // `--chapter` ohne `--subject`: das Kapitel bestimmt das Fach. Vorher fiel die Angabe still
            // durch und die Übung landete im ERSTEN Kapitel des ERSTEN Fachs – ein stiller Griff ins
            // falsche Regal ist schlimmer als eine Fehlermeldung.
            if (wantedChapter is { } wanted)
            {
                if (chapters.Any(c => c.Id == wanted)) return (subject.Id, wanted);
                continue;
            }

            return (subject.Id, chapters[0].Id);
        }

        throw new AgentUsageException(wantedChapter is { } missing
            ? $"Kapitel {missing} gibt es nicht"
              + (wantedSubject is { } id ? $" im Fach {id}" : " in keinem Fach")
              + " – prüfe die Id mit 'pugling-creator types' bzw. im Vater-Web."
            : "Es gibt kein Fach mit Kapitel – lege erst Katalog-Struktur an oder gib --subject und --chapter an.");
    }

    /// <summary>Date option in ISO format; a typo should produce an error message, not today's date.</summary>
    private static DateOnly? ParseDate(CommandLine command, string name) =>
        command.Value(name) is { Length: > 0 } raw
            ? DateOnly.TryParse(raw, CultureInfo.InvariantCulture, out var date)
                ? date
                : throw new AgentUsageException($"--{name} erwartet ein Datum als JJJJ-MM-TT, bekam '{raw}'.")
            : null;

    /// <summary>Translates the server's stable rationale codes into a readable sentence.</summary>
    private static string Describe(IReadOnlyList<string> reasons) => reasons.Count == 0
        ? "(nur allgemein passend)"
        : string.Join(", ", reasons.Select(r => r switch
        {
            "series_match" => "gleiche Buchreihe",
            "subject_match" => "gleiches Fach",
            "grade_in_range" => "Klassenstufe passt",
            "school_type_match" => "Schulart passt",
            _ => r,
        }));

    /// <summary>A profile's grade range in short form.</summary>
    private static string Grades(CreatorProfileResponse profile) => (profile.GradeMin, profile.GradeMax) switch
    {
        ({ } min, { } max) when min == max => $"Klasse {min}",
        ({ } min, { } max) => $"Klasse {min}–{max}",
        ({ } min, null) => $"ab Klasse {min}",
        (null, { } max) => $"bis Klasse {max}",
        _ => "alle Klassen",
    };

    /// <summary>Short help text.</summary>
    public static void PrintUsage() => Console.WriteLine("""
        pugling-creator – erzeugt Pugling-Übungen mit einem lokalen Sprachmodell (Ollama).

        Verwendung:
          pugling-creator types
          pugling-creator profiles [--child <id>] [--subject <id>]
          pugling-creator briefing (--child <id> | --profile <id>) [Optionen]
          pugling-creator create   --type <Typ> (--child <id> | --profile <id>) [Optionen]
          pugling-creator exam     (--child <id> | --profile <id>) [--types a,b,c] [--per-type <n>]
                                   [--date JJJJ-MM-TT] [--title "..."] [Optionen]

        Wer und in wessen Namen:
          --child <id>          Kind, auf das zugeschnitten wird (individuelle Übung)
          --profile <id>        Creator-Profil („Fachlehrer"); ohne Angabe das bestpassende zum Kind
          --general             mit --child: Stoff des Kindes nutzen, aber NICHT individualisieren
          --unit <id>           Unit der Buchreihe (Standard: aktuelle Unit des Kindes)

        Weitere Optionen:
          --type <Typ>          Vocabulary | Cloze | Translation | Grammar      (Pflicht bei create)
          --subject <id>        Zielfach     (Standard: Fach des Profils, sonst erstes Fach mit Kapitel)
          --chapter <id>        Zielkapitel  (Standard: erstes Kapitel des Fachs;
                                allein angegeben bestimmt es auch das Fach)
          --topic "..."         Thema/Lehrbuch-Unit
          --count <n>           Anzahl Aufgaben                                 (Standard 10)
          --types a,b,c         Übungstypen der Klausur         (Standard Vocabulary,Cloze,Grammar)
          --per-type <n>        Aufgaben je Typ in der Klausur                  (Standard 6)
          --date JJJJ-MM-TT     Termin der Klassenarbeit                        (Standard: in 7 Tagen)
          --title "..."         Titel der Klausur               (Standard: aus Unit bzw. Thema)
          --words a,b,c         Pflicht-Wortschatz (unveränderlich)
          --use-weak            schwach beherrschte Wörter des Kindes verwenden
          --source-lang <code>  Lernsprache                     (Standard: Profil, sonst en)
          --target-lang <code>  Muttersprache                   (Standard: Profil, sonst de)
          --points <n>          Punkte der Übung                                (Standard 10)
          --dry-run             nur planen und drucken, nichts speichern
          --strict              Übung löschen, wenn der Selbsttest nicht 100 % erreicht

        Konfiguration: appsettings.json (Pugling:BaseUrl, Pugling:AccountId, Agent:Model),
        die PIN über User-Secrets oder Umgebung: setx Pugling__Pin 0000
        """);
}
