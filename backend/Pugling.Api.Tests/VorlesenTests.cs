using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Tests;

/// <summary>
/// B-252: the practice card says whether its prompt may be read aloud by the device, and in which language
/// (<c>speechLang</c>). Only a vocabulary card in reading direction, only for a word nobody recorded, never on the listening
/// stage - the prompt is then the foreign word itself, so reading it aloud gives nothing away.
/// </summary>
public class VorlesenTests(PuglingWebAppFactory factory) : IClassFixture<PuglingWebAppFactory>
{
    private async Task<JsonElement> FirstCardAsync(string direction, int stage, bool withRecording)
    {
        var father = await TestApi.AdultAsync(factory);
        var pin = "5" + Random.Shared.Next(100, 999);
        var childId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/supervisor/children",
            new { name = TestApi.UniqueName("Vorlese-Kind"), pin }));
        var subjectId = await TestApi.IdAsync(await father.PostAsJsonAsync("/api/v1/creator/subjects",
            new { name = TestApi.UniqueName("Französisch-Vorlesen") }));
        var (seriesId, unitId) = await TestApi.CreateSeriesAndUnitAsync(father, subjectId);
        var exerciseId = await TestApi.IdAsync(await father.PostAsJsonAsync(
            $"/api/v1/creator/textbook-series/{seriesId}/units/{unitId}/vocabulary", new
            {
                title = "Am Bahnhof",
                orderIndex = 1,
                rewardPoints = 10,
                config = new
                {
                    direction,
                    sourceLang = "fr",
                    targetLang = "de",
                    items = new[] { new { front = TestApi.UniqueName("la gare"), back = "der Bahnhof" } },
                },
            }));
        if (withRecording)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PuglingDbContext>();
            var vocabularyId = db.ExerciseItems.Where(i => i.ExerciseId == exerciseId).Select(i => i.VocabularyId).Single();
            var vocabulary = db.Vocabularies.Single(v => v.Id == vocabularyId);
            vocabulary.PronunciationAudioUrl = "/media/la-gare.mp3";
            db.SaveChanges();
        }
        var (planId, positionId) = TestApi.SeedLeitnerPosition(factory, exerciseId, stage, childId: childId);
        var child = await TestApi.ChildAsync(factory, childId, pin);
        var baseUrl = $"/api/v1/student/study-plans/{planId}/positions/{positionId}/practice-sessions";
        var sessionId = await TestApi.IdAsync(await child.PostAsJsonAsync(baseUrl, new { }));
        var cards = await child.GetFromJsonAsync<List<JsonElement>>($"{baseUrl}/{sessionId}/cards");
        return cards![0];
    }

    private static string? SpeechLang(JsonElement card) =>
        card.TryGetProperty("speechLang", out var l) && l.ValueKind == JsonValueKind.String ? l.GetString() : null;

    /// <summary>
    /// AK 1: fr → de, no recording, typed stage → "fr". Red when the field is never set. On the choice: a writing stage
    /// shows the German word since B-597, and only the learnt word is ever read aloud.
    /// </summary>
    [Fact]
    public async Task Ohne_Aufnahme_In_Leserichtung_Darf_Vorgelesen_Werden()
    {
        var card = await FirstCardAsync("front-to-back", (int)TestStage.MultipleChoice, withRecording: false);
        Assert.Equal("fr", SpeechLang(card));
    }

    /// <summary>
    /// AK 2: the same word WITH a recording → null, also on a stage that shows no audio. Red when the rule reads the card's
    /// audio (always empty outside the listening stage) instead of the word's recording.
    /// </summary>
    [Fact]
    public async Task Mit_Aufnahme_Wird_Nicht_Vorgelesen()
    {
        var card = await FirstCardAsync("front-to-back", (int)TestStage.MultipleChoice, withRecording: true);
        Assert.Null(SpeechLang(card));
    }

    /// <summary>AK 3: de → fr (reversed) → null: the prompt is German, the French word the solution. Red when the reversal keeps it.</summary>
    [Fact]
    public async Task Umgekehrte_Richtung_Wird_Nicht_Vorgelesen()
    {
        var card = await FirstCardAsync("back-to-front", (int)TestStage.MultipleChoice, withRecording: false);
        Assert.Null(SpeechLang(card));
    }

    /// <summary>
    /// AK 4: the listening stage without a recording → null, although the text stands on the card as a stand-in. Red when
    /// the listening stage is not left out (reading the word aloud there would be the task done by the device).
    /// </summary>
    [Fact]
    public async Task Hoerstufe_Wird_Nicht_Vorgelesen()
    {
        var card = await FirstCardAsync("front-to-back", (int)TestStage.Audio, withRecording: false);
        Assert.Null(SpeechLang(card));
    }
}
