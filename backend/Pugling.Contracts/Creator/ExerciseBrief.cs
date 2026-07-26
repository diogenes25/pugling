using System.Text.Json;

namespace Pugling.Contracts.Creator;

/// <summary>
/// Schlanke, typ-übergreifende Sicht auf eine Katalog-Übung – für Listen, in denen Übungen
/// verschiedener Typen gemeinsam erscheinen (getaggte Übungen, Übungen einer Klassenarbeit).
/// Die typ-spezifische Konfiguration wird als rohes JSON durchgereicht.
/// </summary>
public record ExerciseBrief(
    int Id, int ChapterId, string ChapterName, int? SubjectId, string SubjectName,
    string Type, string Title, int RewardPoints, JsonElement Config);
