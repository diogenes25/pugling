namespace Pugling.Contracts.Supervisor;

// Vertrag der Motivations-Ebene, die der Vater pflegt: Missionen (zeitgebundene, wiederholbare Ziele)
// und Auszeichnungen (permanente Meilensteine). Beide messen dieselben Fortschritts-Metriken.

/// <summary>Eine vom Vater definierte Mission des Kindes.</summary>
public record MissionDto(int Id, string Title, ProgressMetric Metric, int Target, MissionPeriod Period,
    int RewardPoints, bool Active);

/// <summary>Eingabe zum Anlegen einer Mission.</summary>
public record CreateMissionDto(string Title, ProgressMetric Metric, int Target, MissionPeriod Period, int RewardPoints);

/// <summary>Partielle Änderung einer Mission; Metrik und Periode sind unveränderlich.</summary>
public record UpdateMissionDto(string? Title, int? Target, int? RewardPoints, bool? Active);

/// <summary>Eine vom Vater definierte Auszeichnung des Kindes.</summary>
public record AchievementDto(int Id, string Title, string? Icon, ProgressMetric Metric, int Threshold,
    int RewardPoints, bool Active);

/// <summary>Eingabe zum Anlegen einer Auszeichnung.</summary>
public record CreateAchievementDto(string Title, string? Icon, ProgressMetric Metric, int Threshold, int RewardPoints);

/// <summary>Partielle Änderung einer Auszeichnung; die Metrik ist unveränderlich.</summary>
public record UpdateAchievementDto(string? Title, string? Icon, int? Threshold, int? RewardPoints, bool? Active);
