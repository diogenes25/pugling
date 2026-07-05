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

    /// <summary>Die Inhalte einer Übung als verfahrensneutrale Item-Liste (mit Store-Auflösung für Vokabeln).</summary>
    public async Task<IReadOnlyList<ContentItem>> ItemsOfAsync(Exercise exercise)
    {
        if (exercise.Type == ExerciseType.Vocabulary)
        {
            var config = string.IsNullOrWhiteSpace(exercise.ConfigJson)
                ? new VocabularyConfig()
                : JsonSerializer.Deserialize<VocabularyConfig>(exercise.ConfigJson, JsonOptions) ?? new VocabularyConfig();
            if (config.Refs is { Count: > 0 } refs)
                return await ResolveVocabRefsAsync(refs);
        }
        // Inline-Typen (inkl. Legacy-Vokabeln ohne Refs): zustandslose Projektion aus der Config.
        return provider.ItemsOf(exercise);
    }

    private async Task<IReadOnlyList<ContentItem>> ResolveVocabRefsAsync(List<string> refs)
    {
        var byKey = await db.Vocabulary.AsNoTracking()
            .Where(v => refs.Contains(v.Key))
            .ToDictionaryAsync(v => v.Key);

        // Reihenfolge = Reihenfolge der Refs; Index = stabile Position (→ PositionItemProgress.ItemIndex).
        // Fehlende Keys bleiben als Platzhalter erhalten, damit sich die Indizes nicht verschieben.
        return refs.Select((key, i) => byKey.TryGetValue(key, out var v)
            ? new ContentItem(i, v.Word, v.Translation, [v.Translation], v.Noun?.Article)
            : new ContentItem(i, $"(Vokabel '{key}' fehlt)", "", [""])).ToList();
    }
}
