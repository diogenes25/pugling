using System.Globalization;
using Pugling.Agent.Creator.Briefing;
using Pugling.Agent.Creator.Drafting;
using Pugling.Agent.Creator.Images;
using Pugling.Client;

namespace Pugling.Agent.Creator;

/// <summary>
/// The verbs of the console app. Deliberately thin: resolve arguments, call the pipeline, print the
/// result in an understandable way - the domain logic lives in <see cref="CreatorPipeline"/>,
/// <see cref="ExamPlanner"/>, <see cref="PlanPlanner"/>, <see cref="UnitNotesFiller"/>, <see cref="ImageRun"/> and the
/// strategies.
/// </summary>
public sealed class AgentCommands(CreatorApi creator, SupervisorApi supervisor, CreatorPipeline pipeline, ExamPlanner exams,
    PlanPlanner plans, UnitNotesFiller unitNotes, ImageRun images, PlanAssigner assigner)
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
    /// Adds existing exercises to a child's running plan (B-20) - no language model involved. Exit 0 when positions
    /// were created (or a dry run found something new), 1 when the server refused one, an exercise is not assignable or
    /// the tag exceeds the window, 3 when nothing was new.
    /// </summary>
    private async Task<int> AssignAsync(CommandLine command, CancellationToken ct)
    {
        var outcome = await assigner.RunAsync(AssignRequest.Parse(command), ct);
        foreach (var line in outcome.Report()) Console.WriteLine(line);
        return outcome.ExitCode;
    }

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

    /// <summary>Vocabulary entries a single <c>images</c> run takes when <c>--max</c> is not given – a reviewable batch.</summary>
    private const int DefaultImageBatch = 20;

    /// <summary>
    /// Generates the missing vocabulary images (B-485): <c>--child</c> or <c>--unit</c> (with optional <c>--tag</c>),
    /// never both. Prints every image, the words left without one, the interests that still lack an image prompt and
    /// the word/interest pairs without a usable scene; a failed image makes the run exit 1 without stopping the others.
    /// A missing scene does not – like an undrawable word it is a gap to rerun, not an error.
    /// </summary>
    private async Task<int> ImagesAsync(CommandLine command, CancellationToken ct)
    {
        var childId = command.Has("child") ? command.RequiredInt("child") : (int?)null;
        var unitId = command.Has("unit") ? command.RequiredInt("unit") : (int?)null;
        if ((childId is null) == (unitId is null))
            throw new AgentUsageException("images braucht genau eins von --child und --unit.");
        var tag = command.Value("tag");
        if (tag is not null && childId is not null)
            throw new AgentUsageException("--tag gilt nur mit --unit – mit --child kommen die Interessen aus dem Kind.");
        var take = command.Int("max", DefaultImageBatch);
        if (take is < 1 or > 500)
            throw new AgentUsageException("--max muss zwischen 1 und 500 liegen.");
        var dryRun = command.Flag("dry-run");

        var outcome = await images.RunAsync(new ImageRunRequest(childId, unitId, tag, take, dryRun), ct);

        foreach (var job in outcome.Jobs)
        {
            var label = $"{job.Word} [{job.TagSlug ?? ImagePromptBuilder.Neutral}]";
            Console.WriteLine(job switch
            {
                { Error: { } error } => $"  ✗ {label}: {error}",
                { AssetId: { } id } => $"  ✓ {label} → Bild {id}",
                _ => $"  · {label}: {job.Prompt}",
            });
        }
        if (outcome.NotImageable.Count > 0)
            Console.WriteLine($"Ohne Bild (nicht eindeutig zeichenbar): {string.Join(", ", outcome.NotImageable)}");
        if (outcome.MissingPrompt.Count > 0)
            Console.WriteLine("Interessen ohne Bild-Prompt (im Vater-Web unter Interessen pflegen, dann erneut laufen lassen): "
                              + string.Join(", ", outcome.MissingPrompt));
        if (outcome.NoScene.Count > 0)
            Console.WriteLine("Ohne Interessen-Bild (keine brauchbare Szene, erneut laufen lassen oder Bild-Prompt schärfen): "
                              + string.Join(", ", outcome.NoScene));
        if (outcome.Jobs.Count == 0)
        {
            // Only claim "nothing missing" when it is true: words left out or tags without prompt still lack images.
            Console.WriteLine(outcome.NotImageable.Count > 0 || outcome.MissingPrompt.Count > 0 || outcome.NoScene.Count > 0
                ? "Nichts gezeichnet – die übrigen Lücken stehen oben."
                : "Nichts zu zeichnen – für diesen Umfang fehlt kein Bild.");
            return 0;
        }
        if (dryRun)
        {
            var cost = outcome.Jobs.Count * images.CostPerImage;
            Console.WriteLine($"Trockenlauf – nichts gezeichnet, nichts gespeichert. {outcome.Jobs.Count} Bild(er), "
                              + $"geschätzt {cost.ToString("0.00", CultureInfo.GetCultureInfo("de-DE"))} €.");
            return 0;
        }
        var failed = outcome.Jobs.Count(j => j.Error is not null);
        Console.WriteLine($"Fertig: {outcome.Jobs.Count - failed} Bild(er) angelegt, {failed} fehlgeschlagen.");
        return failed == 0 ? 0 : 1;
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
            case "assign":
                return await AssignAsync(command, ct);
            case "images":
                return await ImagesAsync(command, ct);
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
        var request = await BuildRequestAsync(command, typeRequired: false, writes: false, ct);
        var briefing = await pipeline.BriefAsync(request, ct);
        Console.WriteLine(briefing.ToPromptText());
    }

    /// <summary>Generates an exercise (or plans it as a dry run) and reports the result.</summary>
    private async Task<int> CreateAsync(CommandLine command, CancellationToken ct)
    {
        var request = await BuildRequestAsync(command, typeRequired: true, writes: true, ct);
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
        var request = await BuildRequestAsync(command, typeRequired: false, writes: true, ct);
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
        var request = await BuildRequestAsync(command, typeRequired: false, writes: true, ct);
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
    /// <c>--child</c> alone the profile on the child's book (B-473), with a profile without a series the child's book in
    /// the profile's subject (B-484). A unit not named with <c>--series-unit</c> is the one named with <c>--unit</c>,
    /// else the child's current unit in that series, else the series' first (B-481). The profile itself, when none is
    /// named, is chosen by the briefing on the series decided here - not here, so there is one rule for it.
    /// With <paramref name="writes"/> (create, plan, exam - a dry run too, since it predicts the real run) a series without
    /// a subject is a usage error before any model call: the server files no exercise there (<c>series_without_subject</c>,
    /// B-508). The briefing alone may still show the profile's subject.
    /// </summary>
    private async Task<GenerationRequest> BuildRequestAsync(CommandLine command, bool typeRequired, bool writes, CancellationToken ct)
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
        // A named profile bound to a series without a subject can write nowhere: on its own series the server refuses, on
        // any other the briefing refuses the profile. Said once, before --series sends the user through the second lock.
        if (writes && profileId is not null && profile?.SeriesId is int boundTo
            && await GetSeriesOrUsageErrorAsync(boundTo, ct) is { SubjectId: null } boundSeries)
            // The series came from the profile's binding, not from the command line - the message says so (B-519).
            throw new AgentUsageException(
                $"Creator-Profil {profile.Id} ({profile.Name}) ist an Reihe {boundSeries.Id} gebunden: " + ProfileFit.NoSubject(boundSeries));
        // A child and no place named: the exercise belongs in the child's book, not in the first series of the catalog
        // (B-473). The best profile for the child (no subject filter - the place is what is unknown) gives the series.
        if (profile is null && childId is int forChild && command.Value("series") is null && command.Value("series-unit") is null)
        {
            profile = await BestProfileForChildAsync(forChild, writes, ct);
            profileId = profile.Id;
        }
        var placeNamed = command.Value("series") is not null || command.Value("series-unit") is not null;
        // A profile for every book (no series of its own) says nothing about the shelf; the first series of the catalog
        // would be the guessed shelf of B-473 through another door (B-484).
        var profileSeries = profile is { SeriesId: null } && !placeNamed
            ? childId is int ofProfileChild
                ? await SeriesOfChildsBookAsync(ofProfileChild, profile, writes, ct)
                : throw new AgentUsageException(
                    $"Creator-Profil {profile.Id} ({profile.Name}) hängt an keinem Lehrwerk – gib --series <id> an.")
            : profile?.SeriesId;
        // A named place is found on its own: searched only in the profile's series, a unit of another work would read as
        // a typo instead of the conflict it is.
        var (seriesId, seriesUnitId) = await ResolveLocationAsync(command, placeNamed ? null : profileSeries, ct);
        // Whether the profile fits this work is the briefing's call (ProfileFit), which create, plan and exam all pass
        // before anything is written.
        var series = await GetSeriesOrUsageErrorAsync(seriesId, ct);
        // Two units named for one exercise: the briefing reads its material from one and it would land in the other.
        if (command.Value("series-unit") is not null && command.Value("unit") is not null
            && command.RequiredInt("unit") != seriesUnitId)
            throw new AgentUsageException(
                "--unit und --series-unit nennen verschiedene Units – die Übung entsteht dort, wo ihr Stoff herkommt; "
                + "gib nur eine an.");
        // Wherever the series came from, the exercise goes where its material comes from: the unit named for the
        // briefing (--unit, which the briefing checks against the series), else the child's current one (B-481).
        if (command.Value("series-unit") is null)
        {
            if (command.Value("unit") is not null)
                seriesUnitId = command.RequiredInt("unit");
            else if (childId is int ofChild && await CurrentUnitOfChildAsync(ofChild, seriesId, ct) is int current)
                seriesUnitId = current;
        }
        // The exercise's subject is the subject of the work it is filed in; the profile's counts only where the work has
        // none (B-488).
        if (writes && series.SubjectId is null) throw new AgentUsageException(ProfileFit.NoSubject(series));
        var subjectId = ProfileFit.SubjectIdOf(series, profile) ?? throw new AgentUsageException(ProfileFit.NoSubject(series));

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
            Strict: command.Flag("strict"),
            SchoolTest: command.Flag("schultest"));
    }

    /// <summary>
    /// The child's current unit in the series, if a book of the child is in it. A child this account cannot see has
    /// none here; the briefing then reports it with its usage message instead of a bare 403/404.
    /// </summary>
    private async Task<int?> CurrentUnitOfChildAsync(int childId, int seriesId, CancellationToken ct)
    {
        try
        {
            // Two books on the same series at different units are a choice, not a list order (B-482).
            var units = (await supervisor.ListTextbooksAsync(childId, ct))
                .Where(b => b.SeriesId == seriesId && b.CurrentUnitId is not null)
                .Select(b => b.CurrentUnitId!.Value).Distinct().ToList();
            return units.Count switch
            {
                0 => null,
                1 => units[0],
                _ => throw new AgentUsageException(
                    $"Kind {childId} hat mehrere Lehrbücher auf Reihe {seriesId} bei verschiedenen Units "
                    + $"({string.Join(", ", units)}) – gib --series-unit <id> an."),
            };
        }
        catch (PuglingApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Forbidden
                                                 or System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// For a profile without a series of its own: the series of the child's book that the profile fits by the series'
    /// subject (<see cref="ProfileFit.FitsSubjectOf"/>), not by the book's. None or several are a usage error - choosing
    /// would be guessing. A child this account cannot see is reported as such. With <paramref name="writes"/> a series
    /// without a subject is no book of the profile: it only borrows the profile's subject and files no exercise (B-510).
    /// </summary>
    private async Task<int> SeriesOfChildsBookAsync(int childId, CreatorProfileResponse profile, bool writes,
        CancellationToken ct)
    {
        IReadOnlyList<TextbookResponse> books;
        try
        {
            books = await supervisor.ListTextbooksAsync(childId, ct);
        }
        catch (PuglingApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Forbidden
                                                 or System.Net.HttpStatusCode.NotFound)
        {
            throw new AgentUsageException(
                $"Kind {childId} ist für dieses Konto nicht sichtbar – es existiert nicht, oder das Konto betreut es nicht.");
        }
        // Judged by the book's series, not the book's own subject: the briefing files the exercise in the series' subject
        // and accepts the profile by that (B-506).
        var series = new List<int>();
        TextbookSeriesResponse? withoutSubject = null;
        foreach (var seriesId in books.Where(b => b.SeriesId is not null).Select(b => b.SeriesId!.Value).Distinct())
        {
            var candidate = await GetSeriesOrUsageErrorAsync(seriesId, ct);
            if (!ProfileFit.FitsSubjectOf(profile, candidate)) continue;
            if (writes && candidate.SubjectId is null) withoutSubject ??= candidate;
            else series.Add(seriesId);
        }
        // Only a series without a subject would have fitted: its own lock says more than "no book" (B-510).
        if (series.Count == 0 && withoutSubject is not null)
            throw new AgentUsageException(
                $"Das Lehrbuch von Kind {childId} steht auf Reihe {withoutSubject.Id}: " + ProfileFit.NoSubject(withoutSubject));
        return series.Count switch
        {
            1 => series[0],
            0 => throw new AgentUsageException(
                $"Creator-Profil {profile.Id} hängt an keinem Lehrwerk, und Kind {childId} hat kein Lehrbuch auf einer Reihe "
                + (profile.SubjectName is { } subject ? $"in {subject} " : "des Katalogs ")
                + "– gib --series <id> an."),
            _ => throw new AgentUsageException(
                $"Kind {childId} hat mehrere Lehrbücher, die zu Creator-Profil {profile.Id} passen – wähle mit "
                + $"--series <id>: {string.Join(", ", series)}."),
        };
    }

    /// <summary>
    /// The profile that knows the child's material best. None, or one without a series, is a usage error: the place
    /// would otherwise be guessed, and a guessed shelf is the silent grab B-473 is about.
    /// </summary>
    private async Task<CreatorProfileResponse> BestProfileForChildAsync(int childId, bool writes, CancellationToken ct)
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
        var bySeries = onChildsBooks.GroupBy(m => m.Profile.SeriesId!.Value).ToList();
        if (bySeries.Count > 1)
        {
            // The id offered per book is the one the briefing accepts there; the best score could be a misfit (B-506). A
            // book where none fits stays in the list - dropping it would let the score pick the subject (B-481) - but says
            // so, or the user types an id that is bound to fail.
            // A book on a series without a subject stays too, for the same reason, but a writing verb ends there (B-510).
            var offered = new List<string>();
            foreach (var book in bySeries)
            {
                var (best, fits) = await FittingOnSeriesAsync(book.Key, [.. book], ct);
                var bookSeries = await GetSeriesOrUsageErrorAsync(book.Key, ct);
                offered.Add($"[{best.Id}] {best.Name}"
                            + (writes && bookSeries.SubjectId is null ? " (Reihe ohne Fach – nimmt keine Übung auf)"
                                : fits ? "" : " (passt nicht zum Fach seiner Reihe)"));
            }
            throw new AgentUsageException(
                $"Für Kind {childId} gibt es mehrere Lehrbücher mit Profil – wähle mit --profile <id>: "
                + string.Join(", ", offered));
        }
        return (await FittingOnSeriesAsync(bySeries[0].Key, [.. bySeries[0]], ct)).Profile;
    }

    /// <summary>
    /// Among the matched profiles on one series (best score first), the first the briefing accepts for that series - the
    /// user named none, so the briefing must not reject one in their name (B-500). None fits: the best one, with
    /// <c>Fits</c> false, and the briefing says why.
    /// </summary>
    private async Task<(CreatorProfileResponse Profile, bool Fits)> FittingOnSeriesAsync(int seriesId,
        IReadOnlyList<CreatorProfileMatch> candidates, CancellationToken ct)
    {
        var series = await GetSeriesOrUsageErrorAsync(seriesId, ct);
        return candidates.FirstOrDefault(m => ProfileFit.FitsSubjectOf(m.Profile, series)) is { } fitting
            ? (fitting.Profile, true)
            : (candidates[0].Profile, false);
    }

    /// <summary>
    /// Loads the named profile: its series is the default location, and its subject picks the child's book for a profile
    /// without one and stands in for a series without a subject. Whether it fits the place is the briefing's call.
    /// </summary>
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
          pugling-creator images   (--child <id> | --unit <id> [--tag <slug>]) [--max <n>] [--dry-run]
                                   (Bilder zu Vokabeln: je Wort ein neutrales Bild und je eins für die zwei
                                   stärksten Interessen des Kindes; mit --unit für die ganze Unit, optional
                                   für ein Interesse. Gezeichnet vom Bild-Dienst (Images:Model, Schlüssel in
                                   Images:ApiKey); --dry-run zeigt die Prompts und die Kosten ohne Schlüssel.
                                   --max: höchstens so viele Wörter je Lauf (Standard 20))
          pugling-creator assign   --child <id> (--exercise-id <id> [--exercise-id <id> …] | --tag <name>)
                                   [--plan-id <id>] [--points <n>] [--goal-threshold <n>]
                                   [--cadence Daily|Weekly] [--dry-run]
                                   (vorhandene Übungen als Positionen in den spielbaren Plan des Kindes
                                   übernehmen – ohne Sprachmodell; --plan-id wählt einen anderen eigenen
                                   Plan des Kindes; schon vorhandene Übungen werden übersprungen;
                                   ohne Overrides gelten die Server-Vorgaben; Exit 3 = nichts Neues.
                                   Beispiel: assign --child 1 --exercise-id 12 --exercise-id 15 --dry-run)

        Wer und in wessen Namen:
          --child <id>          Kind, auf das zugeschnitten wird (individuelle Übung)
          --profile <id>        Creator-Profil („Fachlehrer"); ohne Angabe das Profil auf dem Buch
                                des Kindes (hat es mehrere Bücher mit Profil: Pflicht), mit --series
                                das Profil dieser Reihe, sonst eines ohne Reihe im Fach der Reihe
          --general             mit --child: Stoff des Kindes nutzen, aber NICHT individualisieren
          --unit <id>           Unit der Buchreihe, aus der der Stoff kommt; die Übung entsteht dort
                                (Standard: aktuelle Unit des Kindes; ein anderes --series-unit: Fehler)

        Weitere Optionen:
          --type <Typ>          Vocabulary | Cloze | Translation | Grammar      (Pflicht bei create)
          --series <id>         Lehrwerk-Reihe der Übung  (Standard: Reihe des mit --profile genannten
                                Profils – hat es keine, mit --child die Reihe des Kinder-Buchs im
                                Fach des Profils; nur mit --child die Reihe des Profils auf seinem Buch,
                                ohne ein solches oder bei mehreren ein Fehler; das Fach kommt
                                aus der Reihe – create/plan/exam brauchen eine Reihe mit Fach,
                                briefing zeigt sonst das des Profils)
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
