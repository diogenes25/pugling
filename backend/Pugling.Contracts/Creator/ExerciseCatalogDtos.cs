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

/// <summary>
/// Wo eine Übung verwendet wird. <see cref="Plans"/> und <see cref="ClassTests"/> nennen nur Ressourcen der
/// <b>eigenen</b> Kinder – fremde Kinder eines anderen Betreuers dürfen hier nicht auftauchen.
/// </summary>
/// <param name="Plans">Lehrpläne eigener Kinder, in denen die Übung als Position steckt.</param>
/// <param name="ClassTests">Klassenarbeiten eigener Kinder (direkt zugewiesen oder über einen Tag).</param>
/// <param name="OtherLearnersCount">
/// Wie viele <b>verschiedene Kinder</b> die Übung einsetzen, die der Aufrufer <b>nicht betreut</b> – nur die
/// Zahl, ohne Namen. Zwei Gründe:
/// <list type="bullet">
/// <item>Ohne sie melden die Listen „nirgends", während das Löschen mit <c>409 exercise_in_use</c>
/// scheitert – ein Widerspruch, den der Autor nicht auflösen kann (Anmerkung 14).</item>
/// <item>Für einen <b>Creator ohne eigene Kinder</b> (ein Lehrer, oder eine KI-Creator-App) sind die beiden
/// Listen dauerhaft leer. Diese Zahl ist dann nicht die Fußnote, sondern die einzige Antwort auf seine
/// eigentliche Frage: wird mein Material benutzt?</item>
/// </list>
/// Gezählt werden <b>Kinder</b>, nicht Verwendungsstellen: drei Positionen in den Plänen desselben Kindes
/// sind ein Nutzer. Grundlage sind die FK-relevanten Verwendungen, die auch das Löschen blockieren – eine
/// nur über einen Tag eingesammelte Klassenarbeit hindert es nicht und zählt darum nicht mit.
/// </param>
public record UsageResponse(
    IReadOnlyList<PlanUsage> Plans, IReadOnlyList<ClassTestUsage> ClassTests, int OtherLearnersCount);
