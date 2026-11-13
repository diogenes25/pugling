using System.Net;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pugling.Client;

namespace Pugling.Agent.Creator.Images;

/// <summary>What the <c>images</c> verb was asked to do: exactly one scope, a batch size, and whether to draw at all.</summary>
/// <param name="ChildId">Scope: the vocabulary of the child's playable plans, drawn for its strongest interests.</param>
/// <param name="SeriesUnitId">Scope: the vocabulary of a unit, independent of any child.</param>
/// <param name="Tag">Only with <paramref name="SeriesUnitId"/>: the slug of the interest to draw for.</param>
/// <param name="Take">How many vocabulary entries at most.</param>
/// <param name="DryRun">Plan and print the prompts, call neither the image service nor a writing endpoint.</param>
public sealed record ImageRunRequest(int? ChildId, int? SeriesUnitId, string? Tag, int Take, bool DryRun);

/// <summary>One image of the run – planned (dry run), created, or failed.</summary>
/// <param name="VocabularyId">The vocabulary entry the image belongs to.</param>
/// <param name="Word">The word, for the console.</param>
/// <param name="TagSlug">The interest tag; <c>null</c> for the neutral image.</param>
/// <param name="Prompt">The full prompt sent (or, on a dry run, that would be sent).</param>
/// <param name="AssetId">The store asset; <c>null</c> on a dry run or a failure.</param>
/// <param name="Error">Why the image failed; <c>null</c> otherwise.</param>
public sealed record ImageJobResult(int VocabularyId, string Word, string? TagSlug, string Prompt, int? AssetId,
    string? Error);

/// <summary>The run's report: what was drawn, what was left out and why.</summary>
/// <param name="Jobs">Every image planned or attempted.</param>
/// <param name="NotImageable">Words the model or the checks judged not drawable – they stay without image.</param>
/// <param name="MissingPrompt">Interest tags that were wanted but have no image prompt yet (decision 6).</param>
/// <param name="NoScene">
/// Word and interest pairs (<c>word [tag]</c>) whose woven scene did not pass the check – no interest image for them
/// (decision 10); the neutral image of the word is unaffected.
/// </param>
public sealed record ImageRunOutcome(IReadOnlyList<ImageJobResult> Jobs, IReadOnlyList<string> NotImageable,
    IReadOnlyList<string> MissingPrompt, IReadOnlyList<string> NoScene);

/// <summary>
/// Generates the missing vocabulary images (B-485): reads the work list from <c>creator/media/demand</c>, first takes up
/// images a previous run already stored, then asks the <see cref="MotifPlanner"/> once per word for the motif and once
/// per interest for the woven scene (decision 10), draws the neutral image and the interest images of that word in the
/// same run (decision 4), uploads each with its trace and links it straight to the vocabulary entry (decision 2).
/// <para>
/// The generator is resolved lazily: a dry run and every other verb must work without an API key.
/// </para>
/// </summary>
public sealed class ImageRun(CreatorApi creator, MotifPlanner motifs, Lazy<IImageGenerator> generator,
    IOptions<ImageOptions> options, ILogger<ImageRun> logger)
{
    /// <summary>
    /// Editorial rank of a generated interest image's link – below the default 0 every other link carries. For a child
    /// without a matching interest every image scores 0, and the selector breaks that tie by rank before its hash: so
    /// any untagged image – generated or hand-made – beats another child's football. A matching interest still wins,
    /// it scores above 0. Lowering the interest images rather than raising the neutral one is the point: the neutral
    /// image may be an older hand-linked one at rank 0 that this run never touches.
    /// </summary>
    public const int InterestLinkWeight = -1;

    /// <summary>Approximate price of one image in euros, for the dry run's estimate.</summary>
    public double CostPerImage => options.Value.CostPerImage;

    /// <summary>Runs the request and reports every image; a failed image does not stop the others.</summary>
    public async Task<ImageRunOutcome> RunAsync(ImageRunRequest request, CancellationToken ct)
    {
        var demand = request.ChildId is { } childId
            ? await creator.GetMediaDemandForChildAsync(childId, request.Take, ct)
            : await creator.GetMediaDemandForUnitAsync(request.SeriesUnitId!.Value, request.Tag, request.Take, ct);

        var jobs = new List<ImageJobResult>();
        var notImageable = new List<string>();
        var noScene = new List<string>();
        var missingPrompt = new SortedSet<string>(StringComparer.Ordinal);
        var settings = options.Value;

        foreach (var entry in demand)
        {
            foreach (var tag in entry.WantedTags.Where(t => string.IsNullOrWhiteSpace(t.ImagePrompt)))
                missingPrompt.Add(tag.Slug);

            // The neutral image first, then the interests – all of one word in one go, before the child sees the card.
            var wanted = new List<MediaDemandTag?>();
            if (entry.NeedsNeutral) wanted.Add(null);
            wanted.AddRange(entry.WantedTags.Where(t => !string.IsNullOrWhiteSpace(t.ImagePrompt)));

            // An image a previous run uploaded is taken up before the model is asked anything: a model failure on the
            // rerun must not strand an image that is already paid for, and the question would be wasted anyway.
            var toDraw = new List<MediaDemandTag?>();
            foreach (var tag in wanted)
            {
                var reused = request.DryRun ? null : await ReuseAsync(entry, tag, ct);
                if (reused is null) toDraw.Add(tag);
                else jobs.Add(reused);
            }
            if (toDraw.Count == 0) continue;

            var motif = await motifs.PlanAsync(entry, ct);
            if (motif is null)
            {
                notImageable.Add(entry.Word);
                continue;
            }

            foreach (var tag in toDraw)
            {
                // The interest belongs inside the scene, not after it (decision 10); the neutral image draws the motif.
                var scene = tag is null ? motif : await motifs.PlanSceneAsync(entry, motif, tag.ImagePrompt!, ct);
                if (scene is null)
                {
                    noScene.Add($"{entry.Word} [{tag!.Slug}]");
                    continue;
                }
                var prompt = ImagePromptBuilder.Prompt(scene, settings.Style, settings.NegativeSeparator);
                if (request.DryRun)
                {
                    jobs.Add(new ImageJobResult(entry.VocabularyId, entry.Word, tag?.Slug, prompt, null, null));
                    continue;
                }
                jobs.Add(await DrawAsync(entry, tag, scene, prompt, ct));
            }
        }
        return new ImageRunOutcome(jobs, notImageable, [.. missingPrompt], noScene);
    }

    /// <summary>
    /// Links the image a previous run stored under this pair's key, or returns <c>null</c> when there is none and the
    /// image still has to be drawn. A failure is reported for this image only.
    /// </summary>
    private async Task<ImageJobResult?> ReuseAsync(MediaDemandEntry entry, MediaDemandTag? tag, CancellationToken ct)
    {
        var key = ImagePromptBuilder.Key(entry.VocabularyId, tag?.Slug);
        try
        {
            var asset = await ExistingAsync(key, ct);
            if (asset is null) return null;
            await LinkAsync(entry.VocabularyId, asset.Id, tag is null ? 0 : InterestLinkWeight, ct);
            return new ImageJobResult(entry.VocabularyId, entry.Word, tag?.Slug, asset.Source ?? key, asset.Id, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not AgentUsageException)
        {
            logger.LogWarning(ex, "Vorhandenes Bild für '{Word}' ({Tag}) nicht aufnehmbar.", entry.Word,
                tag?.Slug ?? ImagePromptBuilder.Neutral);
            return new ImageJobResult(entry.VocabularyId, entry.Word, tag?.Slug, key, null, ex.Message);
        }
    }

    /// <summary>Draws, uploads and links one image. Any failure is reported for this image only.</summary>
    private async Task<ImageJobResult> DrawAsync(MediaDemandEntry entry, MediaDemandTag? tag, string scene, string prompt,
        CancellationToken ct)
    {
        var settings = options.Value;
        try
        {
            var key = ImagePromptBuilder.Key(entry.VocabularyId, tag?.Slug);
            var bytes = await GenerateAsync(prompt, ct);
            var asset = await creator.UploadMediaAsync(bytes, $"{key}.png", ImagePromptBuilder.AltText(tag?.Label),
                tags: tag is null ? null : [tag.Slug], rating: ContentRating.Everyone, origin: MediaOrigin.Generated,
                source: ImagePromptBuilder.Source(settings.Model, scene, tag?.Slug), key: key, ct: ct);
            await LinkAsync(entry.VocabularyId, asset.Id, tag is null ? 0 : InterestLinkWeight, ct);
            return new ImageJobResult(entry.VocabularyId, entry.Word, tag?.Slug, prompt, asset.Id, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not AgentUsageException)
        {
            logger.LogWarning(ex, "Bild für '{Word}' ({Tag}) fehlgeschlagen.", entry.Word, tag?.Slug ?? ImagePromptBuilder.Neutral);
            return new ImageJobResult(entry.VocabularyId, entry.Word, tag?.Slug, prompt, null, ex.Message);
        }
    }

    /// <summary>
    /// Links the image to the vocabulary entry. Already linked counts as done: the demand can ask again for a pair whose
    /// image hangs there (a creator re-tagged it, two runs raced), and reporting that as a failure would make every
    /// later run exit 1 on the same image.
    /// </summary>
    private async Task LinkAsync(int vocabularyId, int assetId, int weight, CancellationToken ct)
    {
        try
        {
            await creator.LinkVocabularyMediaAsync(vocabularyId, new AddMediaLinkDto(MediaAssetId: assetId, Weight: weight), ct);
        }
        catch (PuglingApiException ex) when (ex.Code == "media_already_linked")
        {
            logger.LogInformation("Bild {AssetId} hing schon an Vokabel {VocabularyId}.", assetId, vocabularyId);
        }
    }

    /// <summary>
    /// An image a previous run already uploaded under this key but could not link – reused instead of drawn (and paid
    /// for) a second time.
    /// </summary>
    private async Task<MediaAssetResponse?> ExistingAsync(string key, CancellationToken ct)
    {
        try
        {
            return await creator.GetMediaByKeyAsync(key, ct);
        }
        catch (PuglingApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>One square PNG from the image service.</summary>
    private async Task<ReadOnlyMemory<byte>> GenerateAsync(string prompt, CancellationToken ct)
    {
        var settings = options.Value;
        var response = await generator.Value.GenerateImagesAsync(prompt, new ImageGenerationOptions
        {
            Count = 1,
            ImageSize = new System.Drawing.Size(settings.Size, settings.Size),
            // No MediaType: the adapter may turn it into a format parameter the GPT image models reject; they return PNG
            // by default, and the server decodes whatever arrives.
            ModelId = settings.Model,
            ResponseFormat = settings.ResponseFormat,
        }, ct);
        return response.Contents.OfType<DataContent>().FirstOrDefault()?.Data
               ?? throw new AgentException("Der Bild-Dienst lieferte kein Bild zurück.");
    }
}
