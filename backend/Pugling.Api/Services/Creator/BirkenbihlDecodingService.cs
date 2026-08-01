using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;

namespace Pugling.Api.Services.Creator;

/// <summary>A hit from the vocabulary store for a word surface form.</summary>
/// <param name="Id">Vocabulary id (→ link <c>/api/v1/learn/vocabulary/{id}</c>).</param>
/// <param name="Word">Word in the learning language (source language of the card).</param>
/// <param name="Translation">Native-language translation/gloss (target language of the card).</param>
/// <param name="PartOfSpeech">Part of speech – helps resolve homonyms.</param>
public record VocabHit(int Id, string Word, string Translation, PartOfSpeech PartOfSpeech);

/// <summary>
/// Lookup result for a token of the sentence: the original surface form, the provisionally best hit
/// (<paramref name="Best"/>, <c>null</c> if not in the store) and – in case of ambiguity – all matching
/// cards as candidates (<paramref name="Candidates"/>, so the supervisor can pick the right meaning).
/// </summary>
public record TokenLookup(string Surface, VocabHit? Best, IReadOnlyList<VocabHit> Candidates);

/// <summary>
/// Core piece of the Birkenbihl automation: splits a sentence in the learning language into words and
/// looks each one up in the shared vocabulary store (direction learning language → native language).
/// Deliberately without its own persistence – the controller assigns ids and saves. The comparison of
/// language codes is exact: the supervisor must use the same codes as in the vocabulary store (e.g. "en"/"de").
/// </summary>
public partial class BirkenbihlDecodingService(PuglingDbContext db)
{
    // A word token is a run of letters/digits including a word-internal apostrophe (e.g. "don't"); punctuation drops out.
    [GeneratedRegex(@"\p{L}[\p{L}\p{N}']*", RegexOptions.CultureInvariant)]
    private static partial Regex WordToken();

    /// <summary>Splits a sentence into its word surface forms, preserving position (without punctuation).</summary>
    public static IReadOnlyList<string> Tokenize(string sentence) =>
        WordToken().Matches(sentence ?? "").Select(m => m.Value).ToList();

    /// <summary>
    /// Decodes a sentence: returns the best vocabulary hit plus candidates per word token. Looks up all
    /// surface forms in <b>one</b> query (no N+1) and groups in memory. Case is ignored during
    /// comparison; the original surface form is preserved for display.
    /// </summary>
    public async Task<IReadOnlyList<TokenLookup>> LookupAsync(
        string learningLang, string nativeLang, string sentence, CancellationToken ct = default)
    {
        var surfaces = Tokenize(sentence);
        if (surfaces.Count == 0) return [];

        var lowered = surfaces.Select(s => s.ToLowerInvariant()).Distinct().ToList();
        var matches = await db.Vocabularies.AsNoTracking()
            .Where(v => v.SourceLanguage == learningLang && v.TargetLanguage == nativeLang
                && lowered.Contains(v.Word.ToLower()))
            .Select(v => new VocabHit(v.Id, v.Word, v.Translation, v.PartOfSpeech))
            .ToListAsync(ct);

        // All hits per surface form (case-insensitive). The first hit is the provisional choice; where there
        // are several we return the candidates too, so a wrongly guessed homonym can be swapped out.
        return surfaces.Select(surface =>
        {
            var hits = matches
                .Where(m => string.Equals(m.Word, surface, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return new TokenLookup(surface, hits.FirstOrDefault(), hits);
        }).ToList();
    }
}
