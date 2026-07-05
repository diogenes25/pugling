using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Services;

/// <summary>
/// DB-gestützte Auflösung der Inhalte einer Übung zu <see cref="ContentItem"/>s. Für die meisten Typen
/// delegiert er an den zustandslosen <see cref="ExerciseContentProvider"/>; für Vokabel-Übungen, die den
/// Store per Key referenzieren (<see cref="VocabularyConfig.Refs"/>), lädt er die Store-Vokabeln
/// (Komplextyp) und baut daraus die Items. So bleibt dieselbe Vokabel über mehrere Übungen hinweg
/// verknüpft und zentral pflegbar. Legacy-Vokabeln (nur inline <see cref="VocabularyConfig.Items"/>)
/// laufen weiterhin über den Provider.
/// </summary>
public class ExerciseContentResolver(PuglingDbContext db, ExerciseContentProvider provider)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Die Inhalte einer Übung als verfahrensneutrale Item-Liste (mit Store-Auflösung für Vokabeln/Lückentext).</summary>
    public async Task<IReadOnlyList<ContentItem>> ItemsOfAsync(Exercise exercise)
    {
        if (exercise.Type == ExerciseType.Vocabulary)
        {
            var config = string.IsNullOrWhiteSpace(exercise.ConfigJson)
                ? new VocabularyConfig()
                : JsonSerializer.Deserialize<VocabularyConfig>(exercise.ConfigJson, JsonOptions) ?? new VocabularyConfig();
            if (config.Refs is { Count: > 0 } refs)
                return await ResolveVocabRefsAsync(refs, config.Direction);
        }
        else if (exercise.Type == ExerciseType.Cloze)
        {
            var config = string.IsNullOrWhiteSpace(exercise.ConfigJson)
                ? new ClozeConfig()
                : JsonSerializer.Deserialize<ClozeConfig>(exercise.ConfigJson, JsonOptions) ?? new ClozeConfig();
            // Nur wenn mindestens eine Lücke den Store referenziert – sonst reicht die Inline-Projektion.
            if (config.Gaps.Any(g => !string.IsNullOrWhiteSpace(g.VocabKey)))
                return await ResolveClozeRefsAsync(config);
        }
        // Inline-Typen (inkl. Legacy-Vokabeln/Lückentexte ohne Store-Bezug): zustandslose Projektion aus der Config.
        return provider.ItemsOf(exercise);
    }

    private async Task<IReadOnlyList<ContentItem>> ResolveVocabRefsAsync(List<string> refs, string? direction)
    {
        var byKey = await db.Vocabulary.AsNoTracking()
            .Where(v => refs.Contains(v.Key))
            .ToDictionaryAsync(v => v.Key);

        // Reihenfolge = Reihenfolge der Refs; Index = stabile Position (→ PositionItemProgress.ItemIndex).
        // Fehlende Keys bleiben als Platzhalter erhalten, damit sich die Indizes nicht verschieben.
        // Die Abfragerichtung dreht das aufgelöste Item (Wort ↔ Übersetzung), siehe ExerciseContentProvider.
        // Die Aussprache-Audioquelle trägt das Item unabhängig von der Richtung mit (sie gehört zum Wort);
        // die Hör-Stufe (TestStage.Audio) liest sie, andere Stufen ignorieren sie.
        return refs.Select((key, i) => byKey.TryGetValue(key, out var v)
            ? ExerciseContentProvider.WithDirection(
                new ContentItem(i, v.Word, v.Translation, [v.Translation], v.Noun?.Article, AudioUrl: v.PronunciationAudioUrl), direction)
            : new ContentItem(i, $"(Vokabel '{key}' fehlt)", "", [""])).ToList();
    }

    /// <summary>
    /// Baut die Lückentext-Items wie der Provider, zieht aber die Lösung je Lücke aus dem Vokabel-Store,
    /// wenn <see cref="Gap.VocabKey"/> gesetzt ist (Store-Wort = fehlendes Wort im Text; Übersetzung als Hinweis).
    /// Lücken ohne Key nutzen die inline <see cref="Gap.Answer"/>. Der Item-Index bleibt die Lücken-Reihenfolge –
    /// ein fehlender Key wird zum Platzhalter, verschiebt aber keine Indizes (Leitner-/Test-Fortschritt bleibt stabil).
    /// </summary>
    private async Task<IReadOnlyList<ContentItem>> ResolveClozeRefsAsync(ClozeConfig config)
    {
        var keys = config.Gaps.Where(g => !string.IsNullOrWhiteSpace(g.VocabKey))
            .Select(g => g.VocabKey!).Distinct().ToList();
        var byKey = await db.Vocabulary.AsNoTracking()
            .Where(v => keys.Contains(v.Key))
            .ToDictionaryAsync(v => v.Key);

        return config.Gaps.Select((g, i) =>
        {
            if (string.IsNullOrWhiteSpace(g.VocabKey))
                return new ContentItem(i, config.Text, g.Answer, Accepted(g.Answer, g.Alternatives), Hint: null, GapIndex: g.Index);
            if (byKey.TryGetValue(g.VocabKey, out var v))
                return new ContentItem(i, config.Text, v.Word, Accepted(v.Word, g.Alternatives), v.Translation, g.Index);
            // Fehlender Store-Key: Platzhalter auf gleichem Index (keine Lösung), damit sich nichts verschiebt.
            return new ContentItem(i, config.Text, "", [""], $"(Vokabel '{g.VocabKey}' fehlt)", g.Index);
        }).ToList();
    }

    // Lösung + Alternativen, roh (Normalisierung macht erst der AnswerGrader) – wie im Provider.
    private static IReadOnlyList<string> Accepted(string answer, IEnumerable<string>? alternatives) =>
        alternatives is null ? [answer] : [answer, .. alternatives];
}
