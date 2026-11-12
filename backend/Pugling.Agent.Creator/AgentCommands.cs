using System.Globalization;
using Pugling.Agent.Creator.Drafting;
using Pugling.Client;

namespace Pugling.Agent.Creator;

/// <summary>
/// The verbs of the console app. Deliberately thin: resolve arguments, call the pipeline, print the
/// result in an understandable way - the domain logic lives in <see cref="CreatorPipeline"/>,
/// <see cref="ExamPlanner"/>, <see cref="PlanPlanner"/>, <see cref="UnitNotesFiller"/> and the strategies.
/// </summary>
public sealed class AgentCommands(CreatorApi creator, SupervisorApi supervisor, CreatorPipeline pipeline, ExamPlanner exams,
    PlanPlanner plans, UnitNotesFiller unitNotes)
{
    // Default values for the request when the command line is silent.
    private const int DefaultItemCount = 10;
    private const int DefaultExamPerType = 6;

    /// <summary>Exercises per type in a generated plan - not tasks, unlike the class test's option of the same name.</summary>
    private const int DefaultPlanPerType = 2;

    /// <summary>Run of a generated plan. Two weeks is the wizard's shortest offer and short enough to be reviewed.</summary>
    private const int DefaultPlanDurationDays = 14;

    /// <summary>
    /// Mirrors <c>GoalPointsPolicy.Fallback</c>: since B-45 the figure is not "what the exercise is worth"
    /// but the target reward a new position <b>inherits</b>, and a position that inherits an unusable
    /// recommendation lands on 20. A lower default here would silently halve the coin target of every
    /// agent-generated exercise against the one the product otherwise states everywhere.
    /// <para>
    /// It carries its own doc because the group comment above cannot: this one is the only value in the
    /// block whose correctness depends on a rule living in another project.
    /// </para>
    /// </summary>
    private const int DefaultRewardPoints = 20;

    /// <summary>The types a class test consists of when none are named.</summary>
    private static readonly string[] DefaultExamTypes = ["Vocabulary", "Cloze", "Grammar"];

    /// <summary>The types a generated study plan consists of when none are named.</summary>
    private static readonly string[] DefaultPlanTypes = ["Vocabulary", "Cloze"];

    /// <summary>
    /// Drafts a unit's notes from a human-supplied source (B-22): <c>--series</c> and <c>--unit</c> are required, and
    /// at least one of <c>--images</c> (photographed pages, B-375) and <c>--source-file</c> (or <c>-</c> for stdin) -
    /// without a source there is nothing to structure, and the model must not invent the material. Prints the
    /// per-page extracts, what was written and what was kept.
    /// </summary>
    private async Task<int> FillAsync(CommandLine command, CancellationToken ct)
    {
        var seriesId = command.RequiredInt("series");
        var unitId = command.RequiredInt("unit");
        var imagePaths = command.List("images");
        var images = imagePaths.Count > 0 ? await PageImage.LoadAsync(imagePaths, ct) : null;
        // Presence, not value: a bare --source-file next to --images is a mistake to report, not to ignore.
        var source = images is null || command.Has("source-file") ? await ReadSourceAsync(command, ct) : null;
        var outcome = await unitNotes.RunAsync(new UnitFillRequest(seriesId, unitId, source,
            Force: command.Flag("force"), DryRun: command.Flag("dry-run"), Pages: images), ct);

        Console.WriteLine($"Unit:    {outcome.Before.Label} (Reihe {seriesId})");
        // The extract per page comes first: it is what the human holds against the photo before trusting the draft.
        foreach (var extract in outcome.Extracts)
        {
            Console.WriteLine($"Erkannter Stoff – {extract.FileName}:");
            Console.WriteLine(extract.Text);
            Console.WriteLine();
        }
        Console.WriteLine("Entwurf:");
        Console.WriteLine(UnitNotesFiller.Print(outcome.Draft));
        // What the limits took out, before the write is reported - a human must see the loss, not discover it.
        foreach (var topic in outcome.Dropped)
            Console.WriteLine(topic.Length > 200
                ? $"Weggelassen (Thema länger als 200 Zeichen, die API lehnt das ab): {topic[..60]}…"
                : $"Weggelassen (mehr als 50 Themen, die Unit nimmt nicht mehr): {topic}");
        if (outcome.Shortened.Count > 0)
            Console.WriteLine($"Gekürzt auf höchstens 2000 Zeichen (am Wortende): {string.Join(", ", outcome.Shortened)}");
        if (outcome.Kept.Count > 0)
            Console.WriteLine($"Behalten (schon befüllt, ohne --force unangetastet): {string.Join(", ", outcome.Kept)}");
        if (outcome.Written.Count == 0)
        {
            Console.WriteLine("Nichts zu schreiben – der Entwurf enthält nichts für ein leeres Feld.");
            return 0;
        }
        if (command.Flag("dry-run"))
        {
            Console.WriteLine($"Trockenlauf – es wurde nichts gespeichert. Würde schreiben: {string.Join(", ", outcome.Written)}");
            return 0;
        }
        Console.WriteLine($"Geschrieben: {string.Join(", ", outcome.Written)}");
        return 0;
    }

    /// <summary>The source text: a file, or stdin when <c>--source-file -</c>. Missing or empty is a usage error.</summary>
    private static async Task<string> ReadSourceAsync(CommandLine command, CancellationToken ct)
    {
        var path = command.Value("source-file");
        if (string.IsNullOrWhiteSpace(path))
            throw new AgentUsageException("--images oder --source-file fehlt: eine Quelle (Fotos der Buchseiten, Inhaltsverzeichnis, "
                                          + "Kapitelübersicht) ist Pflicht – das Modell darf den Stoff einer Unit nicht erfinden. "
                                          + "'-' liest von Stdin (Umleitung oder Pipe).");
        // Reading stdin to its end blocks on a bare terminal, and Ctrl+C is swallowed by the cancel handler - say so
        // instead of sitting silently (B-293).
        if (path == "-" && !Console.IsInputRedirected)
            Console.Error.WriteLine("Lese die Quelle von Stdin – Eingabe mit Ctrl+Z und Enter (Windows) bzw. Ctrl+D beenden.");
        string text;
        try
        {
            text = path == "-" ? await Console.In.ReadToEndAsync(ct) : await File.ReadAllTextAsync(path, ct);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A wrong path is the same class of mistake as no path: a usage error (exit 2), not a stack trace.
            throw new AgentUsageException($"Die Quelle '{path}' ist nicht lesbar: {ex.Message}");
        }
        if (string.IsNullOrWhiteSpace(text))
            throw new AgentUsageException($"Die Quelle '{path}' ist leer.");
        return text;
    }

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
            case "plan":
                return await PlanAsync(command, ct);
            case "fill":
                return await FillAsync(command, ct);
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
        // The listing does not check the filter: an unknown subject id came back as "no profile yet", exit 0 (B-312).
        var subject = subjectId is int sid ? await GetSubjectOrUsageErrorAsync(sid, ct) : null;

        if (command.Value("child") is not null)
        {
            var childId = command.RequiredInt("child");
            IReadOnlyList<CreatorProfileMatch> matches;
            try
            {
                matches = await creator.MatchProfilesAsync(childId, subjectId, ct);
            }
            // Unknown and not supervised are one answer, 404, on the server as well (B-308, B-310).
            catch (PuglingApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
            {
                throw new AgentUsageException(
                    $"Kind {childId} ist für dieses Konto nicht sichtbar – es existiert nicht, oder das Konto betreut es nicht. "
                    + "Prüfe Pugling:AccountId/Pugling:Pin.");
            }
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
            Console.WriteLine(subject is null
                ? "Es gibt noch kein Creator-Profil. Ohne Profil arbeitet der Agent als Generalist."
                : $"Kein Creator-Profil für Fach {subject.Id} ({subject.Name}). Ohne Profil arbeitet der Agent als Generalist.");
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
            : "Für:     den gemeinsamen Katalog (allgemeine Übung)"
              + (briefing.Interests.Count > 0 ? $" (eingekleidet in: {string.Join(", ", briefing.Interests)})" : ""));
        Console.WriteLine($"Ort:     {briefing.SubjectName} › {briefing.SeriesUnitLabel}");
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
        foreach (var part in outcome.Parts) Console.WriteLine(Describe(part));
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

    /// <summary>
    /// Generates a whole study plan: several exercises on the same material and one position per
    /// exercise, in a container the father still has to switch on.
    /// </summary>
    private async Task<int> PlanAsync(CommandLine command, CancellationToken ct)
    {
        var request = await BuildRequestAsync(command, typeRequired: false, ct);
        var types = command.List("types") is { Count: > 0 } wanted ? wanted : DefaultPlanTypes;
        var plan = new PlanRequest(request, types, command.Int("per-type", DefaultPlanPerType),
            ParseIntensity(command), ParseDate(command, "start"),
            command.Int("days", DefaultPlanDurationDays), command.Value("title"));

        var outcome = await plans.RunAsync(plan, ct);

        // Not "Lehrplan: {Title}": the derived title already begins with that word, and the first real
        // run printed "Lehrplan: Lehrplan: Unite 2 - En ville".
        Console.WriteLine($"Titel:   {outcome.Title}");
        Console.WriteLine();
        foreach (var part in outcome.Parts) Console.WriteLine(Describe(part));
        // A book word list is assigned, not generated: no self-test to report, but the dose the child will get.
        foreach (var list in outcome.WordLists)
        {
            var dose = PlanPlanner.BookListPosition(list, plan.DurationDays, PlanPlanner.Intensities[plan.Intensity]).NewItemsPerDay;
            Console.WriteLine($"  ✓ {"Vocabulary",-12} Wortliste {list.ExerciseId} „{list.Title}“ ({list.ItemCount} Einträge, "
                              + $"{dose} neu je Tag, Karteikasten, getippter Test)");
        }
        Console.WriteLine();

        if (outcome.PlanId is int planId)
        {
            // The figures come from the planner, not from a second table here - one rule, one place.
            var settings = PlanPlanner.Intensities[plan.Intensity];
            Console.WriteLine($"Lehrplan {planId} angelegt, {outcome.PositionIds.Count} Position(en), "
                              + $"Intensität {plan.Intensity}: bestehen ab {settings.Pass} %, "
                              + $"{settings.Points} Punkte/Ziel, "
                              + (settings.Penalty > 0 ? $"−{settings.Penalty} 🪙 bei Versäumnis." : "kein Malus."));
            // The one thing that does not happen by itself - and the story's whole safety net.
            Console.WriteLine($"Der Plan ist noch NICHT aktiv. Prüfe ihn im Vater-Web unter /vater/plan/{planId} "
                              + "und schalte ihn dort scharf.");
            // Named with the plan, not thrown: the exercise exists and can still be assigned by hand.
            foreach (var failure in outcome.FailedPositions)
                Console.Error.WriteLine($"  ! Übung {failure.ExerciseId} kam nicht in den Plan: {failure.Error}");
        }
        else if (request.DryRun)
        {
            Console.WriteLine("Trockenlauf – es wurde nichts gespeichert.");
        }
        else
        {
            Console.WriteLine("Keine einzige Übung ist entstanden – ohne Position bleibt auch der Lehrplan aus.");
        }

        if (outcome.Complete) return 0;

        Console.Error.WriteLine("Der Lehrplan ist unvollständig – prüfe die gescheiterten Teile oben.");
        return 1;
    }

    /// <summary>
    /// One report line per generated exercise - the same five outcomes for the class test and the study
    /// plan, so they are formatted in one place rather than twice.
    /// </summary>
    private static string Describe(GeneratedPart part) => part switch
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
    };

    /// <summary>
    /// The intensity option. A typo must not silently fall back to <c>Normal</c> - that would hand the
    /// child a penalty the caller thought they had switched off with <c>--intensity locker</c>.
    /// </summary>
    private static PlanIntensity ParseIntensity(CommandLine command) =>
        command.Value("intensity") is { Length: > 0 } raw
            ? Enum.TryParse<PlanIntensity>(raw, ignoreCase: true, out var intensity)
                ? intensity
                : throw new AgentUsageException(
                    $"--intensity erwartet {string.Join(" | ", Enum.GetNames<PlanIntensity>())}, bekam '{raw}'.")
            : PlanIntensity.Normal;

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

        // Withdraw first: otherwise the success message names an exercise id that no longer exists.
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
    /// would be missing. Series and series unit may be omitted: then the profile's series applies - with
    /// <c>--child</c> alone the profile on the child's book (B-473). A unit not named with <c>--series-unit</c> is the
    /// one named with <c>--unit</c>, else the child's current unit in that series, else the series' first (B-481).
    /// </summary>
    private async Task<GenerationRequest> BuildRequestAsync(CommandLine command, bool typeRequired, CancellationToken ct)
    {
        var childId = command.Value("child") is not null ? command.RequiredInt("child") : (int?)null;
        var profileId = command.Value("profile") is not null ? command.RequiredInt("profile") : (int?)null;
        if (childId is null && profileId is null)
            throw new AgentUsageException(
                "Gib --child <id> für eine individuelle Übung oder --profile <id> für eine allgemeine an.");
        // Weak words belong to a child; without one the switch would silently do nothing (B-21).
        if (childId is null && command.Flag("use-weak"))
            throw new AgentUsageException(
                "--use-weak braucht --child <id>: die schwach beherrschten Wörter gehören zu einem Kind.");

        var profile = profileId is int id ? await LoadProfileAsync(id, ct) : null;
        // A child and no place named: the exercise belongs in the child's book, not in the first series of the catalog
        // (B-473). The best profile for the child (no subject filter - the place is what is unknown) gives the series.
        if (profile is null && childId is int forChild && command.Value("series") is null && command.Value("series-unit") is null)
        {
            profile = await BestProfileForChildAsync(forChild, ct);
            profileId = profile.Id;
        }
        var (seriesId, seriesUnitId) = await ResolveLocationAsync(command, profile?.SeriesId, ct);
        // Wherever the series came from, the exercise goes where its material comes from: the unit named for the
        // briefing (--unit, which the briefing checks against the series), else the child's current one (B-481).
        if (command.Value("series-unit") is null)
        {
            if (command.Value("unit") is not null)
                seriesUnitId = command.RequiredInt("unit");
            else if (childId is int ofChild && await CurrentUnitOfChildAsync(ofChild, seriesId, ct) is int current)
                seriesUnitId = current;
        }
        var subjectId = profile?.SubjectId ?? await ResolveSubjectIdAsync(seriesId, ct);

        return new GenerationRequest(
            ChildId: childId,
            ProfileId: profileId,
            UnitId: command.Value("unit") is not null ? command.RequiredInt("unit") : null,
            General: command.Flag("general"),
            SubjectId: subjectId,
            SeriesId: seriesId,
            SeriesUnitId: seriesUnitId,
            TypeKey: typeRequired ? command.RequiredValue("type") : command.Value("type") ?? "Vocabulary",
            Topic: command.Value("topic"),
            ItemCount: command.Int("count", DefaultItemCount),
            Words: command.List("words"),
            UseWeakWords: command.Flag("use-weak"),
        // With nothing given, the profile decides and then the defaults (en/de) - not the command line.
            SourceLang: command.Value("source-lang"),
            TargetLang: command.Value("target-lang"),
            RewardPoints: command.Int("points", DefaultRewardPoints),
            Interests: command.List("interests"),
            DryRun: command.Flag("dry-run"),
            Strict: command.Flag("strict"));
    }

    /// <summary>
    /// The child's current unit in the series, if a book of the child is in it. A child this account cannot see has
    /// none here; the briefing then reports it with its usage message instead of a bare 403/404.
    /// </summary>
    private async Task<int?> CurrentUnitOfChildAsync(int childId, int seriesId, CancellationToken ct)
    {
        try
        {
            return (await supervisor.ListTextbooksAsync(childId, ct))
                .FirstOrDefault(b => b.SeriesId == seriesId && b.CurrentUnitId is not null)?.CurrentUnitId;
        }
        catch (PuglingApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Forbidden
                                                 or System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// The profile that knows the child's material best. None, or one without a series, is a usage error: the place
    /// would otherwise be guessed, and a guessed shelf is the silent grab B-473 is about.
    /// </summary>
    private async Task<CreatorProfileResponse> BestProfileForChildAsync(int childId, CancellationToken ct)
    {
        IReadOnlyList<CreatorProfileMatch> matches;
        try
        {
            matches = await creator.MatchProfilesAsync(childId, null, ct);
        }
        catch (PuglingApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            throw new AgentUsageException(
                $"Kind {childId} ist für dieses Konto nicht sichtbar – es existiert nicht, oder das Konto betreut es nicht.");
        }
        // Only a profile on the series of one of the child's books says where the exercise belongs; the match also lists
        // profiles that merely fit grade and school type, and taking one of those is the guessed shelf again.
        var onChildsBooks = matches.Where(m => m.Profile.SeriesId is not null && m.Reasons.Contains("series_match")).ToList();
        if (onChildsBooks.Count == 0)
            throw new AgentUsageException(
                $"Für Kind {childId} kennt kein Creator-Profil die Reihe eines seiner Lehrbücher – gib --profile <id> oder "
                + "--series <id> --series-unit <id> an ('pugling-creator profiles --child' zeigt die Treffer).");

        // Two books with a profile each (English and Latin) are a choice of subject, which the score cannot make: it
        // weighs grade and school type, so one point ahead would pick the subject unannounced (B-481).
        var books = onChildsBooks.DistinctBy(m => m.Profile.SeriesId).ToList();
        if (books.Count > 1)
            throw new AgentUsageException(
                $"Für Kind {childId} gibt es mehrere Lehrbücher mit Profil – wähle mit --profile <id>: "
                + string.Join(", ", books.Select(m => $"[{m.Profile.Id}] {m.Profile.Name}")));
        return books[0].Profile;
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

    /// <summary>
    /// Resolves the target series/unit (since B-106, exercises hang off a series unit, not a chapter).
    /// <c>--series</c>/<c>--series-unit</c> may be omitted: then the profile's series applies, otherwise
    /// the first series with a unit.
    /// </summary>
    private async Task<(int SeriesId, int SeriesUnitId)> ResolveLocationAsync(CommandLine command,
        int? profileSeriesId, CancellationToken ct)
    {
        var wantedSeries = command.Value("series") is not null ? command.RequiredInt("series") : profileSeriesId;
        var wantedUnit = command.Value("series-unit") is not null ? command.RequiredInt("series-unit") : (int?)null;

        if (wantedSeries is { } seriesId && wantedUnit is { } seriesUnitId) return (seriesId, seriesUnitId);

        // A named series is looked up by id, not searched for in a listing: the listing is paged, and a valid series
        // past its first page was rejected as a typo (B-311).
        IEnumerable<TextbookSeriesResponse> candidates = wantedSeries is { } named
            ? [await GetSeriesOrUsageErrorAsync(named, ct)]
            : await ListAllSeriesAsync(ct);
        foreach (var s in candidates)
        {
            var units = await creator.ListUnitsAsync(s.Id, ct: ct);
            if (units.Count == 0) continue;

            // `--series-unit` without `--series`: the unit determines the series. It used to fall through
            // silently and the exercise landed in the FIRST unit of the FIRST series - a silent grab into
            // the wrong shelf is worse than an error message.
            if (wantedUnit is { } wanted)
            {
                if (units.Any(u => u.Id == wanted)) return (s.Id, wanted);
                continue;
            }

            return (s.Id, units[0].Id);
        }

        throw new AgentUsageException(wantedUnit is { } missing
            ? $"Unit {missing} gibt es nicht"
              + (wantedSeries is { } id ? $" in Reihe {id}" : " in keiner Reihe")
              + " – prüfe die Id mit 'pugling-creator types' bzw. im Vater-Web."
            : "Es gibt keine Reihe mit Unit – lege erst Lehrwerk-Struktur an oder gib --series und --series-unit an.");
    }

    /// <summary>
    /// Every series, page by page. Only the search for a bare <c>--series-unit</c> (or for any unit at all) needs the
    /// whole catalog; one page would report a unit of the 51st series as non-existent (B-311).
    /// </summary>
    private async Task<IReadOnlyList<TextbookSeriesResponse>> ListAllSeriesAsync(CancellationToken ct)
    {
        // Small on purpose: the test proves the loop with 51 series, which a larger page would swallow in one call.
        const int pageSize = 50;
        var all = new List<TextbookSeriesResponse>();
        while (true)
        {
            var page = await creator.ListSeriesAsync(skip: all.Count, take: pageSize, ct: ct);
            all.AddRange(page);
            if (page.Count < pageSize) return all;
        }
    }

    /// <summary>A series by id; an unknown id is a usage error naming it, not a raw 404 (B-307, B-311).</summary>
    private async Task<TextbookSeriesResponse> GetSeriesOrUsageErrorAsync(int seriesId, CancellationToken ct)
    {
        try
        {
            return await creator.GetSeriesAsync(seriesId, ct);
        }
        catch (PuglingApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            throw new AgentUsageException($"Reihe {seriesId} gibt es nicht – prüfe die Id im Vater-Web.");
        }
    }

    /// <summary>A subject by id; an unknown id is a usage error naming it (B-312).</summary>
    private async Task<SubjectResponse> GetSubjectOrUsageErrorAsync(int subjectId, CancellationToken ct)
    {
        try
        {
            return await creator.GetSubjectAsync(subjectId, ct);
        }
        catch (PuglingApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            throw new AgentUsageException($"Fach {subjectId} gibt es nicht – prüfe die Id im Vater-Web.");
        }
    }

    /// <summary>The subject a series carries (for profile matching); throws if the series has none set.</summary>
    private async Task<int> ResolveSubjectIdAsync(int seriesId, CancellationToken ct)
    {
        var series = await GetSeriesOrUsageErrorAsync(seriesId, ct);
        return series.SubjectId
            ?? throw new AgentUsageException(
                $"Reihe {seriesId} ({series.Name}) hat kein Fach zugeordnet – setze eines im Vater-Web, bevor du Übungen anlegst.");
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
        Ein erzeugter Lehrplan entsteht bewusst INAKTIV – der Vater sichtet ihn und schaltet ihn scharf.

        Verwendung:
          pugling-creator types
          pugling-creator profiles [--child <id>] [--subject <id>]
          pugling-creator briefing (--child <id> | --profile <id>) [Optionen]
          pugling-creator create   --type <Typ> (--child <id> | --profile <id>) [Optionen]
          pugling-creator exam     (--child <id> | --profile <id>) [--types a,b,c] [--per-type <n>]
                                   [--date JJJJ-MM-TT] [--title "..."] [Optionen]
          pugling-creator plan     --child <id> [--types a,b,c] [--per-type <n>] [--intensity <Stufe>]
                                   [--start JJJJ-MM-TT] [--days <n>] [--title "..."] [Optionen]
          pugling-creator fill     --series <id> --unit <id> (--images <Datei|Ordner>[,…] | --source-file <Datei|->)
                                   [--force] [--dry-run]
                                   (Stoffnotizen einer Unit aus einer Quelle entwerfen; nur leere Felder,
                                   --force überschreibt; '-' liest die Quelle von Stdin – nur mit Umleitung
                                   oder Pipe sinnvoll, interaktiv mit Ctrl+Z+Enter bzw. Ctrl+D beenden.
                                   --images: Fotos der Buchseiten (.jpg/.png/.webp, ein Ordner = alle Bilder
                                   darin nach Namen), gelesen vom lokalen Vision-Modell (Agent:VisionModel) –
                                   übernommen wird nur der Stoff, keine Aufgaben oder Texte des Buches)

        Wer und in wessen Namen:
          --child <id>          Kind, auf das zugeschnitten wird (individuelle Übung)
          --profile <id>        Creator-Profil („Fachlehrer"); ohne Angabe das Profil auf dem Buch
                                des Kindes (hat es mehrere Bücher mit Profil: Pflicht)
          --general             mit --child: Stoff des Kindes nutzen, aber NICHT individualisieren
          --unit <id>           Unit der Buchreihe, aus der der Stoff kommt; ohne --series-unit
                                entsteht die Übung auch dort (Standard: aktuelle Unit des Kindes)

        Weitere Optionen:
          --type <Typ>          Vocabulary | Cloze | Translation | Grammar      (Pflicht bei create)
          --series <id>         Lehrwerk-Reihe der Übung  (Standard: Reihe des mit --profile genannten
                                Profils; nur mit --child die Reihe des Profils auf seinem Buch,
                                ohne ein solches oder bei mehreren ein Fehler; das Fach kommt
                                aus dem Profil, sonst aus der Reihe)
          --series-unit <id>    Unit, in der die Übung entsteht (Standard: --unit, sonst mit --child
                                die aktuelle Unit des Kindes in dieser Reihe, sonst die erste;
                                allein angegeben bestimmt sie auch die Reihe – mit --profile muss
                                sie in dessen Reihe liegen)
          --subject <id>        nur profiles: Fach, auf das die Treffer gefiltert werden
          --topic "..."         Thema/Lehrbuch-Unit
          --count <n>           Anzahl Aufgaben                                 (Standard 10)
          --types a,b,c         Übungstypen des Bündels    (exam: Vocabulary,Cloze,Grammar;
                                                            plan: Vocabulary,Cloze)
          --per-type <n>        exam: AUFGABEN je Typ (Standard 6) –
                                plan: ÜBUNGEN je Typ, je eine Position (Standard 2);
                                die Aufgaben je Übung kommen dort aus --count;
                                hat die Unit eine Wortliste aus dem Buch, plant plan
                                für Vocabulary deren Übungen ganz ein (Karteikasten,
                                Tagesdosis bis Planende, getippter Test)
          --date JJJJ-MM-TT     Termin der Klassenarbeit                        (Standard: in 7 Tagen)
          --title "..."         Titel der Klausur bzw. des Lehrplans (Standard: aus Unit bzw. Thema)
          --intensity <Stufe>   Locker | Normal | Intensiv – Bestehensgrenze, Punkte und Münz-Malus
                                je Position                                     (Standard Normal)
          --start JJJJ-MM-TT    erster Tag des Lehrplan-Zeitraums               (Standard: heute)
          --days <n>            Dauer des Lehrplan-Zeitraums in Tagen           (Standard 14)
          --words a,b,c         Pflicht-Wortschatz (unveränderlich)
          --interests a,b       Interessen-Slugs, in die die Übung eingekleidet wird (ohne --child;
                                mit --child kommen sie aus dem Kind selbst)
          --use-weak            schwach beherrschte Wörter des Kindes verwenden (nur mit --child)
          --source-lang <code>  Lernsprache                     (Standard: Profil, sonst en)
          --target-lang <code>  Muttersprache                   (Standard: Profil, sonst de)
          --points <n>          empfohlene Zielpunkte der Position              (Standard 20, max 200)
          --dry-run             nur planen und drucken, nichts speichern
          --strict              Übung löschen, wenn der Selbsttest nicht 100 % erreicht

        Konfiguration: appsettings.json (Pugling:BaseUrl, Pugling:AccountId, Agent:Model, Agent:VisionModel),
        die PIN über User-Secrets oder Umgebung: setx Pugling__Pin 0000
        """);
}
