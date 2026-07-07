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
            return await ResolveVocabularyItemsAsync(exercise, config.Direction);
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

    /// <summary>
    /// Löst die Items einer Vokabelübung aus der <see cref="ExerciseItem"/>-Tabelle auf: jede Zeile trägt die
    /// stabile <c>ItemId</c> und verweist per <c>VocabularyId</c> auf den Store (Wort/Übersetzung/Audio kommen
    /// live von dort, zentral pflegbar). Der Item-Index ergibt sich aus der Listenposition (sortiert nach
    /// <see cref="ExerciseItem.OrderIndex"/>, Id) – so bleibt er lückenlos/stabil, unabhängig vom Sortierschlüssel.
    /// Ein optionaler Zeilen-Hinweis übersteuert den abgeleiteten Store-Hinweis (z. B. Artikel). Fehlt der
    /// Store-Eintrag, bleibt ein Platzhalter auf gleichem Index (Leitner-/Test-Fortschritt kippt nicht).
    /// Ohne Item-Zeilen (nicht migrierte/leere Übung) greift die zustandslose Config-Projektion als Fallback.
    /// </summary>
    private async Task<IReadOnlyList<ContentItem>> ResolveVocabularyItemsAsync(Exercise exercise, string? direction)
    {
        var rows = await db.ExerciseItems.AsNoTracking()
            .Where(i => i.ExerciseId == exercise.Id)
            .OrderBy(i => i.OrderIndex).ThenBy(i => i.Id)
            .Select(i => new { i.Id, i.VocabularyId, i.Hint })
            .ToListAsync();
        if (rows.Count == 0) return provider.ItemsOf(exercise);

        var ids = rows.Select(r => r.VocabularyId).Distinct().ToList();
        var byId = await db.Vocabulary.AsNoTracking().Where(v => ids.Contains(v.Id)).ToDictionaryAsync(v => v.Id);

        // Die Aussprache-Audioquelle gehört zum Wort und wird richtungsunabhängig mitgetragen (die Hör-Stufe
        // liest sie); WithDirection dreht Wort ↔ Übersetzung und bewahrt dabei ItemId/VocabularyId.
        return rows.Select((r, i) =>
        {
            if (!byId.TryGetValue(r.VocabularyId, out var v))
                return new ContentItem(i, $"(Vokabel #{r.VocabularyId} fehlt)", "", [""], ItemId: r.Id, VocabularyId: r.VocabularyId);
            var item = new ContentItem(i, v.Word, v.Translation, [v.Translation],
                r.Hint ?? v.Noun?.Article, AudioUrl: v.PronunciationAudioUrl, ItemId: r.Id, VocabularyId: r.VocabularyId);
            return ExerciseContentProvider.WithDirection(item, direction);
        }).ToList();
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
