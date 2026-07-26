using Pugling.Contracts.Shared;

namespace Pugling.Contracts.Student;

// Vertrag der Sohn-Sichten auf den eigenen Lernstand: Tagesmission und Laufzeit-Verlauf eines Lehrplans
// sowie der plan-übergreifende Vokabel-Lernstand (pro Item, pro Wort, mit Antwort-Historie).

/// <summary>Tagesmission: der Plan im Überblick plus der heutige Rollup über seine Positionen.</summary>
public record OverviewResponse(int PlanId, string Title, DateOnly StartDate, DateOnly EndDate, bool Active,
    int CurrentStreak, DayOverview Today);

/// <summary>Tag-für-Tag-Verlauf über die gesamte Laufzeit (erledigte Tage, erreichte Ziele, Punkte).</summary>
public record ProgressResponse(int PlanId, DateOnly StartDate, DateOnly EndDate, int DaysComplete,
    int TotalDays, int TotalPoints, int CurrentStreak, IReadOnlyList<ProgressDay> Days);

/// <summary>Lernstand eines Kindes zu einem Item (Front/Rückseite aus dem Store, kanonisch Wort → Übersetzung).</summary>
public record ItemProgressResponse(int ItemId, int ExerciseId, int VocabularyId, string Front, string Back,
    int Box, int MaxBox, int MasteryPercent, int SeenCount, int CorrectCount,
    DateOnly? IntroducedAt, DateTime? LastAnswerAt, bool? LastCorrect,
    [property: System.Text.Json.Serialization.JsonPropertyName("vocabulary")] string Vocabulary);

/// <summary>Aggregierter Wort-Beherrschungsstand über alle Übungen, die dieses Store-Wort nutzen.</summary>
public record WordMasteryResponse(int VocabularyId, string Word, string Translation, int ItemCount,
    int AvgMasteryPercent, int MinBox, int SeenCount, int CorrectCount, int CorrectPercent,
    [property: System.Text.Json.Serialization.JsonPropertyName("vocabulary")] string Vocabulary);

/// <summary>Ein protokolliertes Antwort-Ereignis der Item-Historie.</summary>
public record HistoryResponse(DateTime At, string Source, int StageValue, string? GivenAnswer,
    bool WasCorrect, int? PlanPositionId);
