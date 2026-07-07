using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Models;
using Pugling.Api.Services;

namespace Pugling.Api.Data;

/// <summary>
/// Idempotenter Backfill beim Start: materialisiert die Items bestehender <b>und geseedeter</b> Vokabelübungen
/// aus ihrer <see cref="Exercise.ConfigJson"/> (inline <c>Items</c> bzw. ID-<c>Refs</c>) in die
/// <see cref="ExerciseItem"/>-Tabelle und reduziert die Config danach auf reine Einstellungen (Richtung/Sprachen).
/// Übungen, deren Config bereits keine Items/Refs mehr trägt, werden übersprungen – so bleibt der Lauf günstig
/// und wiederholbar. Der Abgleich (<see cref="ExerciseItemService.ReconcileAsync"/>) bewahrt vorhandene ItemIds.
/// </summary>
public static class ExerciseItemBackfill
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task RunAsync(PuglingDbContext db, ExerciseItemService items, CancellationToken ct = default)
    {
        var vocabExercises = await db.Exercises
            .Where(e => e.Type == ExerciseType.Vocabulary)
            .ToListAsync(ct);

        foreach (var exercise in vocabExercises)
        {
            var config = string.IsNullOrWhiteSpace(exercise.ConfigJson)
                ? new VocabularyConfig()
                : JsonSerializer.Deserialize<VocabularyConfig>(exercise.ConfigJson, JsonOptions) ?? new VocabularyConfig();
            if (config.Items.Count == 0 && (config.Refs is null || config.Refs.Count == 0))
                continue; // Config bereits auf Einstellungen reduziert – nichts zu tun.

            await items.SyncFromConfigAsync(exercise.Id, config, ct);
            config.Items = [];
            config.Refs = null;
            exercise.ConfigJson = JsonSerializer.Serialize(config, JsonOptions);
            await db.SaveChangesAsync(ct);
        }
    }
}
