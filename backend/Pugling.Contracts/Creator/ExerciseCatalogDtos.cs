using System.Text.Json;

namespace Pugling.Contracts.Creator;

// Vertrag der typ-übergreifenden Katalogsicht: Suchen, Bearbeiten-Laden, Verwendungsnachweis.
// Die typ-spezifische Konfiguration reist als rohes JSON (JsonElement) mit.

/// <summary>
/// Schlanke Trefferzeile der Übungssuche (kindneutraler Katalog). <c>AuthorFatherId</c>/<c>AuthorName</c> tragen die
/// Attribution der geteilten Bibliothek (<c>null</c> = geseedete System-Übung); <c>IsOwn</c> = der anfragende Vater
/// darf die Übung ändern/löschen.
/// </summary>
public record ExerciseSummary(int Id, int ChapterId, int SubjectId, string Type, string Title,
    int? GradeMin, int? GradeMax, SchoolTypes SchoolTypes, string? Source, int? CategoryId, string? CategoryName,
    int? AuthorFatherId, string? AuthorName, bool IsOwn, bool IsOwner, bool ExecutePublic, string? Description,
    bool DefaultUseLeitner, bool DefaultRequireTypedTest);

/// <summary>
/// Vollständige, typ-übergreifende Sicht auf eine Übung inklusive roher Config und aller Metadaten –
/// die Grundlage zum Bearbeiten (Config in den typ-spezifischen Editor laden; gespeichert wird über
/// den per-Typ-PUT <c>.../chapters/{}/&lt;typ&gt;/{id}</c>).
/// </summary>
public record ExerciseDetail(int Id, int ChapterId, string ChapterName, int SubjectId, string SubjectName,
    string Type, string Title, int OrderIndex, int RewardPoints, int? GradeMin, int? GradeMax,
    SchoolTypes SchoolTypes, string? Source, int? CategoryId, string? CategoryName,
    SuggestedBonus? SuggestedBonus, int? DefaultStage, int? DefaultItemCount,
    int? AuthorFatherId, string? AuthorName, bool IsOwn, bool IsOwner, bool ExecutePublic, int GrantCount,
    JsonElement Config, string? Description,
    bool DefaultUseLeitner, bool DefaultRequireTypedTest);

/// <summary>Ein Lehrplan, in dem eine Übung als Position steckt.</summary>
public record PlanUsage(int PlanId, string PlanTitle, int ChildId, string ChildName);

/// <summary>Eine Klassenarbeit, der eine Übung zugewiesen ist (direkt oder über einen Tag).</summary>
public record ClassTestUsage(int Id, string Title, int ChildId, string ChildName);

/// <summary>Wo eine Übung verwendet wird (nur Ressourcen der eigenen Kinder).</summary>
public record UsageResponse(IReadOnlyList<PlanUsage> Plans, IReadOnlyList<ClassTestUsage> ClassTests);
