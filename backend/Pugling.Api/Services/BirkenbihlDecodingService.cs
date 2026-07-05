using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services;

/// <summary>Ein Treffer aus dem Vokabelspeicher zu einer Wort-Oberfläche.</summary>
/// <param name="Id">Vokabel-Id (→ Link <c>/api/v1/learn/vocabulary/{id}</c>).</param>
/// <param name="Word">Wort in der Lernsprache (Ausgangssprache der Karte).</param>
/// <param name="Translation">Muttersprachliche Übersetzung/Glosse (Zielsprache der Karte).</param>
/// <param name="PartOfSpeech">Wortart – hilft beim Auflösen von Homonymen.</param>
public record VocabHit(int Id, string Word, string Translation, PartOfSpeech PartOfSpeech);

/// <summary>
/// Nachschlag-Ergebnis für ein Token des Satzes: die Original-Oberfläche, der provisorisch beste Treffer
/// (<paramref name="Best"/>, <c>null</c> wenn nicht im Speicher) und – bei Mehrdeutigkeit – alle passenden
/// Karten als Kandidaten (<paramref name="Candidates"/>, damit der Vater die richtige Bedeutung wählen kann).
/// </summary>
public record TokenLookup(string Surface, VocabHit? Best, IReadOnlyList<VocabHit> Candidates);

/// <summary>
/// Kernstück der Birkenbihl-Automatik: zerlegt einen Satz der Lernsprache in Wörter und schlägt jedes im
/// gemeinsamen Vokabelspeicher nach (Richtung Lernsprache → Muttersprache). Bewusst ohne eigene Persistenz –
/// der Controller vergibt IDs und speichert. Der Vergleich der Sprachcodes ist exakt: der Vater muss dieselben
/// Codes wie im Vokabelspeicher verwenden (z. B. „en"/„de").
/// </summary>
public partial class BirkenbihlDecodingService(PuglingDbContext db)
{
    // Wort-Token = Folgen aus Buchstaben/Ziffern samt Wort-innerem Apostroph (z. B. „don't"); Satzzeichen fallen weg.
    [GeneratedRegex(@"\p{L}[\p{L}\p{N}']*", RegexOptions.CultureInvariant)]
    private static partial Regex WordToken();

    /// <summary>Zerlegt einen Satz positionsgetreu in seine Wort-Oberflächen (ohne Satzzeichen).</summary>
    public static IReadOnlyList<string> Tokenize(string sentence) =>
        WordToken().Matches(sentence ?? "").Select(m => m.Value).ToList();

    /// <summary>
    /// Dekodiert einen Satz: liefert pro Wort-Token den besten Vokabeltreffer plus Kandidaten. Schlägt alle
    /// Oberflächen in <b>einer</b> Query nach (kein N+1) und gruppiert im Speicher. Groß-/Kleinschreibung wird
    /// beim Vergleich ignoriert; die Original-Oberfläche bleibt für die Anzeige erhalten.
    /// </summary>
    public async Task<IReadOnlyList<TokenLookup>> LookupAsync(
        string learningLang, string nativeLang, string sentence, CancellationToken ct = default)
    {
        var surfaces = Tokenize(sentence);
        if (surfaces.Count == 0) return [];

        var lowered = surfaces.Select(s => s.ToLowerInvariant()).Distinct().ToList();
        var matches = await db.Vocabulary.AsNoTracking()
            .Where(v => v.SourceLanguage == learningLang && v.TargetLanguage == nativeLang
                && lowered.Contains(v.Word.ToLower()))
            .Select(v => new VocabHit(v.Id, v.Word, v.Translation, v.PartOfSpeech))
            .ToListAsync(ct);

        // Je Oberfläche alle Treffer (case-insensitiv). Erster Treffer = provisorische Wahl; bei mehreren
        // liefern wir die Kandidaten mit, damit ein falsch geratenes Homonym gezielt getauscht werden kann.
        return surfaces.Select(surface =>
        {
            var hits = matches
                .Where(m => string.Equals(m.Word, surface, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return new TokenLookup(surface, hits.FirstOrDefault(), hits);
        }).ToList();
    }
}
