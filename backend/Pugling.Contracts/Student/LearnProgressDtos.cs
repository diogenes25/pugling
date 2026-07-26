namespace Pugling.Contracts.Student;

// Vertrag der katalog-hierarchischen Lernstand-Sicht (Fach → Kapitel → Übung → Item), abgeleitet aus
// den Lehrplänen des Kindes. Ergänzt die flache Vokabel-Sicht in ProgressDtos.cs; das Item-DTO teilen
// sich beide Sichten bewusst (identische Form). Dazu die Sohn-Sicht auf Missionen/Auszeichnungen
// und der positionsgebundene Report.

/// <summary>Aggregierter Lernstand über eine Menge Vokabel-Items (auf jeder Ebene identisch aufgebaut).</summary>
public record MasteryRollup(
    int TotalItems, int IntroducedItems, int MasteredItems, int WeakItems,
    int AvgMasteryPercent, int SeenCount, int CorrectCount, int CorrectPercent, DateTime? LastActivityAt);

/// <summary>Fortschritt eines Fachs. <paramref name="Active"/> = enthält ≥1 aktuell (über aktiven Plan) zugewiesene Übung.</summary>
public record SubjectProgressResponse(int SubjectId, string Name, int ChapterCount, int ExerciseCount, bool Active, MasteryRollup Progress);

/// <summary>Fortschritt eines Kapitels. <paramref name="Active"/> = enthält ≥1 aktuell zugewiesene Übung.</summary>
public record ChapterProgressResponse(int ChapterId, string Name, int OrderIndex, int ExerciseCount, bool Active, MasteryRollup Progress);

/// <summary>Fortschritt einer einzelnen Vokabelübung. <paramref name="Active"/> = aktuell über einen aktiven Plan zugewiesen.</summary>
public record ExerciseProgressResponse(int ExerciseId, string Title, int OrderIndex, bool Active, MasteryRollup Progress);

/// <summary>Stand einer Mission aus Sohn-Sicht: Ziel, aktueller Wert, erfüllt?</summary>
public record MissionStatus(int Id, string Title, ProgressMetric Metric, MissionPeriod Period,
    int Target, int Current, bool Completed, int RewardPoints);

/// <summary>Stand einer Auszeichnung aus Sohn-Sicht: Schwelle, aktueller Wert, erreicht?</summary>
public record AchievementStatus(int Id, string Title, string? Icon, ProgressMetric Metric,
    int Threshold, int Current, bool Earned, DateTime? EarnedAt, int RewardPoints);

/// <summary>Report-Zeile eines einzelnen Inhalts.</summary>
public record ItemReport(int ItemIndex, string Prompt, string Answer, bool Introduced,
    int Box, int MasteryPercent, int ReviewCount, DateOnly? DueOn, DateTime? LastReviewedAt,
    int TestsSeen, int TestsCorrect);

/// <summary>Report einer Position samt Kopf-Kennzahlen (eingeführt/beherrscht).</summary>
public record Report(int PositionId, int ExerciseId, string ExerciseTitle, string ExerciseType,
    int MaxBox, int TotalItems, int IntroducedItems, int MasteredItems, IReadOnlyList<ItemReport> Items);
