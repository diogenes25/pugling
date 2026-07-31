namespace Pugling.Contracts.Supervisor;

// Vertrag der Motivations-Ebene, die der Vater pflegt: Missionen (zeitgebundene, wiederholbare Ziele)
// und Auszeichnungen (permanente Meilensteine). Beide messen dieselben Fortschritts-Metriken.

/// <summary>A mission of the child, defined by the supervisor.</summary>
public record MissionDto(int Id, string Title, ProgressMetric Metric, int Target, MissionPeriod Period,
    int RewardPoints, bool Active);

/// <summary>Input for creating a mission.</summary>
public record CreateMissionDto(string Title, ProgressMetric Metric, int Target, MissionPeriod Period, int RewardPoints);

/// <summary>Partial change to a mission; metric and period are immutable.</summary>
public record UpdateMissionDto(string? Title, int? Target, int? RewardPoints, bool? Active);

/// <summary>An award of the child, defined by the supervisor.</summary>
public record AchievementDto(int Id, string Title, string? Icon, ProgressMetric Metric, int Threshold,
    int RewardPoints, bool Active);

/// <summary>Input for creating an award.</summary>
public record CreateAchievementDto(string Title, string? Icon, ProgressMetric Metric, int Threshold, int RewardPoints);

/// <summary>Partial change to an award; the metric is immutable.</summary>
public record UpdateAchievementDto(string? Title, string? Icon, int? Threshold, int? RewardPoints, bool? Active);
