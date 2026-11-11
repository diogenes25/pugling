using Pugling.Client;

namespace Pugling.Agent.Creator.Briefing;

/// <summary>
/// Assembles the <see cref="CreatorBriefing"/>: the <b>profile</b> and the textbook units from the
/// <b>creator</b>, catalog and vocabulary also from the creator, child profile and textbooks from the
/// <b>supervisor</b>, learning progress from the <b>student</b> read views.
/// <para>
/// The child part is <b>optional</b>, and a permission question hangs on that: only someone who names a
/// child needs an account that supervises this child. General catalog exercises need only the creator
/// role - so supervisor and student views are queried here only when a child is set.
/// </para>
/// </summary>
public sealed class BriefingBuilder(CreatorApi creator, SupervisorApi supervisor, StudentApi student)
{
    /// <summary>At most this many weak words go into the prompt - more would overload local models.</summary>
    private const int MaxWeakWords = 15;

    /// <summary>The exercise types built from words; only they get a slice of the unit's word list.</summary>
    private static readonly HashSet<string> WordTypes = new(["Vocabulary", "Translation"], StringComparer.OrdinalIgnoreCase);

    /// <summary>Builds the briefing for a request.</summary>
    public async Task<CreatorBriefing> BuildAsync(GenerationRequest request, CancellationToken ct = default)
    {
        // Weak words are what tailors an exercise to one child; `--general` is the promise that it is tailored to
        // nobody. Silently honoring one of the two would break the other (B-279, the line of B-21 AC 4).
        if (request.General && request.UseWeakWords)
            throw new AgentUsageException(
                "--use-weak passt nicht zu --general: die schwach beherrschten Wörter gehören zu einem Kind, "
                + "eine allgemeine Übung ist für keines zugeschnitten.");

        var subject = await creator.GetSubjectAsync(request.SubjectId, ct);
        // The series reaches here unchecked when --series and --series-unit are both given: a typo is a usage
        // error, not a raw 404 (B-307, the sibling of B-301 in `fill`).
        TextbookSeriesResponse targetSeries;
        try
        {
            targetSeries = await creator.GetSeriesAsync(request.SeriesId, ct);
        }
        catch (PuglingApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            throw new AgentUsageException($"Reihe {request.SeriesId} gibt es nicht – prüfe die Id im Vater-Web.");
        }
        var targetUnit = (await creator.ListUnitsAsync(request.SeriesId, ct: ct))
            .FirstOrDefault(u => u.Id == request.SeriesUnitId)
            ?? throw new AgentUsageException(
                $"Unit {request.SeriesUnitId} gehört nicht zu Reihe {request.SeriesId} ({targetSeries.Name}).");

        var child = request.ChildId is int childId ? await LoadChildAsync(childId, ct) : null;
        var profile = await ResolveProfileAsync(request, ct);
        // Explicitly set languages beat the profile; without either, the usual defaults apply.
        var sourceLang = request.SourceLang ?? profile?.SourceLang ?? "en";
        var targetLang = request.TargetLang ?? profile?.TargetLang ?? "de";
        // Weak words only of THIS exercise's language pair (B-21): they become required vocabulary, and a child
        // learning two languages would otherwise get French words forced into an English exercise. Loaded after
        // the profile because the pair can come from it - and after the child, whose visibility check gives the
        // readable error.
        if (child is not null && request.UseWeakWords)
            child = child with
            {
                WeakWords = await student.ListWordMasteryAsync(child.ChildId, onlyWeak: true, take: MaxWeakWords,
                    sourceLang: sourceLang, targetLang: targetLang, ct: ct),
            };
        var (series, unit) = await ResolveMaterialAsync(request, profile, child, subject.Id, subject.Name, ct);

        // Existing titles go into the prompt as "do not repeat" - that prevents near-duplicates.
        var existing = await creator.SearchExercisesAsync(subjectId: request.SubjectId,
            seriesUnitId: request.SeriesUnitId, take: 50, ct: ct);

        // Vocabulary priority: explicitly given > the child's weak words > (empty, then the model picks).
        IReadOnlyList<string> requiredWords = request.Words.Count > 0
            ? request.Words
            : [.. (child?.WeakWords ?? []).Select(w => w.Word)];

        // The unit's word list and grammar topics are read, not generated: both are fixed by the book.
        var (pool, wordLists) = await LoadUnitVocabularyAsync(request, existing, ct);
        // Only the word-practicing types get a slice: a cloze or grammar part has a grammar focus, and ten word
        // pairs beside it would only pull the text towards vocabulary.
        var wordPairs = WordTypes.Contains(request.TypeKey)
            ? UnitVocabulary.Slice(pool, [.. (child?.WeakWords ?? []).Select(w => w.Word)], request.ItemCount, request.Part)
            : [];
        var grammarLabels = targetUnit.GrammarTopicIds.Count == 0
            ? []
            : (await creator.ListGrammarTopicsAsync(subjectId: subject.Id, ct: ct))
                .Where(t => targetUnit.GrammarTopicIds.Contains(t.Id))
                .Select(t => t.Label)
                .ToList();

        // The same `--general` reading as the Child line below: what the briefing carries decides, not what
        // was typed - so a general exercise built from a child's material is dressed by --interests.
        var dressedChild = request.General ? null : child;
        var dressing = await ResolveInterestTagsAsync(request, dressedChild, ct);

        return new CreatorBriefing(
            Profile: profile is null ? null : Facts(profile, series, unit),
            // `--general` keeps the child's series and unit but drops the person: the subject matter is
            // right, the exercise stays usable for the shared catalog.
            Child: dressedChild,
            SubjectId: subject.Id,
            SubjectName: subject.Name,
            SeriesUnitId: targetUnit.Id,
            SeriesUnitLabel: targetUnit.Label,
            Topic: request.Topic,
            SourceLang: sourceLang,
            TargetLang: targetLang,
            ExistingExerciseTitles: [.. existing.Select(e => e.Title)],
            RequiredWords: requiredWords,
            InterestTagIds: [.. dressing.Select(t => t.Id)],
            // With a child its interests reach the prompt through ChildFacts; without one these labels are the only
            // way they do - tagging alone left the catalog claiming a dressing the exercise did not have (B-264).
            CatalogInterests: dressedChild is null ? [.. dressing.Select(t => t.Label)] : [],
            GrammarTopicIds: targetUnit.GrammarTopicIds,
            GrammarTopicLabels: grammarLabels,
            WordPairs: wordPairs,
            // The same threshold the vocabulary strategy applies to a slice: a list too short for one exercise is no
            // word list, and the plan generates instead.
            UnitWordLists: pool.Count >= Drafting.DraftRules.MinItems ? wordLists : []);
    }

    /// <summary>
    /// The word pairs of the target unit, taken from its vocabulary exercises in catalog order. Each word once: the
    /// first exercise that names it holds the book's translation, a later one at most a variant of it. Exercises the
    /// agent generated are left out - their pairs are a model's choice, and taking them as the word list would make
    /// the next run repeat them and pass a guessed translation off as the book's.
    /// <para>
    /// Also returns the exercises that make up the list: those adding at least one word. An exercise whose words all
    /// stand in an earlier one is a duplicate, and assigning it would make the child practice those words twice.
    /// </para>
    /// </summary>
    private async Task<(IReadOnlyList<VocabPair> Pool, IReadOnlyList<UnitWordList> Lists)> LoadUnitVocabularyAsync(
        GenerationRequest request, IReadOnlyList<ExerciseSummary> existing, CancellationToken ct)
    {
        var pairs = new List<VocabPair>();
        var lists = new List<UnitWordList>();
        // Folded like the server folds a word ("horse" = "the horse", B-336): two spellings of one word in a slice
        // would be a duplicate the model cannot repair, because the program fixed the words.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var exercise in existing.Where(e => string.Equals(e.Type, "Vocabulary", StringComparison.OrdinalIgnoreCase)
                                                     && e.Description?.StartsWith(Drafting.DraftPrompts.GeneratedMarker,
                                                         StringComparison.Ordinal) != true))
        {
            var items = await creator.ListItemsAsync(request.SeriesId, request.SeriesUnitId, exercise.Id, ct);
            var fresh = items.OrderBy(i => i.OrderIndex)
                .Where(i => seen.Add(VocabKey.Part(i.Front)))
                .Select(i => new VocabPair(i.Front, i.Back, i.VocabularyId, i.Hint))
                .ToList();
            if (fresh.Count == 0) continue;
            pairs.AddRange(fresh);
            lists.Add(new UnitWordList(exercise.Id, exercise.Title, items.Count));
        }

        return (pairs, lists);
    }

    /// <summary>
    /// The interest tags the new exercise is <b>dressed</b> in (B-46). Two paths, and the briefing's child
    /// decides which one applies - not which switches were typed.
    /// <para>
    /// With a child: its <b>positively</b> weighted interests, and only those.
    /// <see cref="ChildFacts.WeightedInterests"/> is already filtered that way, which is the whole point -
    /// tagging an exercise with a dislike would label it with the very thing the prompt tells the model to
    /// avoid. The free-text interests carry no tag id and therefore cannot be tagged; only what came from
    /// the taxonomy can, and that is the same restriction the image selection lives with.
    /// </para>
    /// <para>
    /// Without a child: the slugs from <c>--interests</c>, resolved against the same taxonomy. Nothing is
    /// created here, so a typo cannot leave a dead tag behind - it aborts with the list of known slugs,
    /// because the silent alternative is an exercise that reports success and carries no dressing.
    /// </para>
    /// </summary>
    private async Task<IReadOnlyList<(int Id, string Label)>> ResolveInterestTagsAsync(
        GenerationRequest request, ChildFacts? child, CancellationToken ct)
    {
        if (child is not null)
        {
            // Both at once is not a merge but an unanswered question ("whose interests win?"), and B-46
            // decided two separate paths rather than a precedence. Saying so beats guessing.
            if (request.Interests.Count > 0)
                throw new AgentUsageException(
                    "--interests gilt für den kindlosen Pfad. Mit --child kommen die Interessen aus dem Kind selbst; "
                    + "nimm --general dazu, wenn du sie stattdessen vorgeben willst - dann entfällt allerdings die "
                    + "gesamte Zuschneidung auf das Kind (schwache Wörter, Name, Klassenstufe, Anrede), nicht nur "
                    + "die Einkleidung.");
            return [.. child.WeightedInterests.Select(i => (i.TagId, i.Label))];
        }

        if (request.Interests.Count == 0) return [];

        var tags = new List<(int Id, string Label)>();
        var unknown = new List<string>();
        foreach (var wanted in request.Interests)
        {
            var slug = wanted.Trim();
            // Asked for one at a time instead of listing the whole taxonomy: the list endpoint pages at
            // 100 by default and is ordered by slug, so a full listing would have silently reported every
            // tag past the hundredth as a typo - and offered a truncated "known slugs" list as proof
            // (found in the B-46 review).
            var treffer = await creator.ListInterestTagsAsync(search: slug, ct: ct);
            // `search` is a substring over slug OR label, so the exact comparison stays necessary.
            // Case-insensitive, because slugs are lower case by derivation (InterestSlug) - folding case
            // can only ever match what the caller read off the listing.
            var tag = treffer.FirstOrDefault(t => string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase));
            if (tag is not null) tags.Add((tag.Id, tag.Label));
            else unknown.Add(wanted);
        }

        if (unknown.Count > 0)
            throw new AgentUsageException(
                $"Unbekannte Interessen-Slugs: {string.Join(", ", unknown)}. "
                + "Die vorhandenen Stichwörter listet `pugling-creator` nicht selbst - sie stehen unter "
                + "GET api/v1/creator/interest-tags.");

        return tags;
    }

    /// <summary>
    /// The request's profile: explicitly named, otherwise the <b>best fit</b> for the child. Without
    /// either, it stays empty - the agent then works as a generalist as before, instead of aborting with
    /// an error message (a catalog without profiles must remain usable).
    /// </summary>
    public async Task<CreatorProfileResponse?> ResolveProfileAsync(GenerationRequest request, CancellationToken ct = default)
    {
        if (request.ProfileId is int profileId)
        {
            try
            {
                return await creator.GetProfileAsync(profileId, ct);
            }
            catch (PuglingApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
            {
                throw new AgentUsageException($"Creator-Profil {profileId} gibt es nicht – 'pugling-creator profiles' zeigt die vorhandenen.");
            }
        }

        if (request.ChildId is not int childId) return null;

        var matches = await creator.MatchProfilesAsync(childId, request.SubjectId, ct);
        return matches.Count > 0 ? matches[0].Profile : null;
    }

    /// <summary>
    /// The request's material: the series (from the profile, otherwise from the child's textbook) and
    /// the unit - explicitly named, otherwise the child's current unit. The unit is <b>checked against
    /// the series</b>: a unit from an unrelated work would be worse than none, because the model would
    /// then treat its material as confirmed.
    /// </summary>
    private async Task<(TextbookSeriesResponse? Series, SeriesUnitResponse? Unit)> ResolveMaterialAsync(
        GenerationRequest request, CreatorProfileResponse? profile, ChildFacts? child,
        int subjectId, string subjectName, CancellationToken ct)
    {
        var childBook = child?.PrimaryTextbook(subjectId, subjectName);
        if ((profile?.SeriesId ?? childBook?.SeriesId) is not int seriesId)
        {
            if (request.UnitId is not null)
                throw new AgentUsageException(
                    "--unit setzt eine Lehrwerk-Reihe voraus: hinterlege sie am Creator-Profil oder am Lehrbuch des Kindes.");
            return (null, null);
        }

        var series = await creator.GetSeriesAsync(seriesId, ct);
        var units = await creator.ListUnitsAsync(seriesId, ct: ct);

        if (request.UnitId is int wanted)
            return (series, units.FirstOrDefault(u => u.Id == wanted)
                            ?? throw new AgentUsageException(
                                $"Unit {wanted} gehört nicht zur Reihe '{series.Name}' – " +
                                "'pugling-creator profiles' zeigt die Reihe des Profils."));

        // The child's unit only counts if it comes from the same series (profile and child may point at
        // different works).
        var current = childBook is { SeriesId: { } bookSeries, CurrentUnitId: int currentId } && bookSeries == seriesId
            ? units.FirstOrDefault(u => u.Id == currentId)
            : null;
        return (series, current);
    }

    /// <summary>Condenses profile, series and unit into the prompt facts.</summary>
    private static ProfileFacts Facts(CreatorProfileResponse profile, TextbookSeriesResponse? series,
        SeriesUnitResponse? unit) =>
        new(profile.Id, profile.Name, profile.SubjectName, profile.SchoolTypes, profile.GradeMin, profile.GradeMax,
            profile.SourceLang, profile.TargetLang, profile.Persona, profile.Didactics,
            // The series sits on the profile; if unset there it comes from the child's book and is reported
            // anyway - the subject matter counts, not where the value came from.
            series?.Id ?? profile.SeriesId, series?.Name ?? profile.SeriesName, series?.PublisherName, series?.Notes, unit);

    /// <summary>
    /// The child with everything that drives the tailoring. A pure creator account sees no children; the
    /// exception points this out explicitly instead of passing through a bare 403.
    /// </summary>
    private async Task<ChildFacts> LoadChildAsync(int childId, CancellationToken ct)
    {
        ChildResponse child;
        try
        {
            child = await supervisor.GetChildAsync(childId, ct);
        }
        catch (PuglingApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Forbidden
                                                 or System.Net.HttpStatusCode.NotFound)
        {
            throw new AgentUsageException(
                $"Kind {childId} ist für dieses Konto nicht sichtbar. Für eine individuelle Übung braucht der Agent " +
                $"ein Konto mit Creator-Rolle, das dieses Kind betreut (Supervisor). Prüfe Pugling:AccountId/Pugling:Pin – " +
                $"oder erzeuge mit --profile eine allgemeine Übung ohne Kind.");
        }

        var weighted = await LoadInterestsAsync(childId, ct);
        var textbooks = await LoadTextbooksAsync(childId, ct);

        return new ChildFacts(
            ChildId: child.Id,
            Name: child.Name,
            Age: child.BirthYear is { } year ? DateTime.UtcNow.Year - year : null,
            Grade: child.Grade,
            SchoolType: child.SchoolType,
            Gender: child.Gender,
            // Free text and weighted tags side by side: the free text carries nuances the taxonomy does not
            // know, the tags carry the ranking - and above all the dislikes.
            Interests: child.Interests,
            WeightedInterests: [.. weighted.Where(i => i.Weight > 0).OrderByDescending(i => i.Weight)],
            Dislikes: [.. weighted.Where(i => i.Weight < 0).OrderBy(i => i.Weight)],
            ProfileNotes: child.ProfileNotes,
            Textbooks: textbooks,
            // Filled in BuildAsync, once the exercise's language pair is known.
            WeakWords: []);
    }

    /// <summary>
    /// The weighted interests from the shared taxonomy. For the prompt, the <b>negative</b> ones matter
    /// most: a task about spiders is factually correct and still unusable if the child cannot stand
    /// spiders. If they are missing (freshly created child), the free text is all that remains.
    /// </summary>
    private async Task<IReadOnlyList<ChildInterestResponse>> LoadInterestsAsync(int childId, CancellationToken ct)
    {
        try
        {
            return await supervisor.ListInterestsAsync(childId, ct);
        }
        catch (PuglingApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            return [];
        }
    }

    private async Task<IReadOnlyList<TextbookResponse>> LoadTextbooksAsync(int childId, CancellationToken ct)
    {
        try
        {
            return await supervisor.ListTextbooksAsync(childId, ct);
        }
        catch (PuglingApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            // Generating works without a textbook too - then only the topic carries the subject matter.
            return [];
        }
    }
}
