namespace Pugling.Agent.Creator;

/// <summary>
/// Der Auftrag an den Agenten: <b>für wen</b> (Kind), <b>wo</b> im Katalog (Fach/Kapitel), <b>was</b>
/// (Übungstyp, Thema, Umfang) und unter welchen Sicherheitsregeln (Trockenlauf, strenger Selbsttest).
/// </summary>
/// <param name="ChildId">Das Kind, auf das zugeschnitten wird (Profil, Interessen, Lernstand).</param>
/// <param name="SubjectId">Zielfach im Katalog.</param>
/// <param name="ChapterId">Zielkapitel im Katalog.</param>
/// <param name="TypeKey">Übungstyp-Schlüssel aus dem Manifest (z. B. <c>Vocabulary</c>, <c>Cloze</c>).</param>
/// <param name="Topic">Freitext-Thema bzw. Lehrbuch-Unit („Unit 3: Animals").</param>
/// <param name="ItemCount">Gewünschte Aufgabenzahl.</param>
/// <param name="Words">
/// Vorgegebener Wortschatz. Ist er gesetzt, ist er <b>unveränderlich</b> – das Modell darf ihn nur
/// einkleiden, nicht austauschen (siehe <see cref="Drafting.DraftPrompts"/>).
/// </param>
/// <param name="UseWeakWords">Schwach beherrschte Wörter des Kindes als Wortschatz heranziehen.</param>
/// <param name="SourceLang">Sprachcode der Lernsprache (Vorderseite/Quelle).</param>
/// <param name="TargetLang">Sprachcode der Muttersprache (Rückseite/Ziel).</param>
/// <param name="RewardPoints">Punkte, die die Übung wert ist.</param>
/// <param name="DryRun">Nur planen und drucken – nichts schreiben.</param>
/// <param name="Strict">Übung wieder löschen, wenn der Selbsttest nicht 100 % erreicht.</param>
public sealed record GenerationRequest(
    int ChildId,
    int SubjectId,
    int ChapterId,
    string TypeKey,
    string? Topic,
    int ItemCount,
    IReadOnlyList<string> Words,
    bool UseWeakWords,
    string SourceLang,
    string TargetLang,
    int RewardPoints,
    bool DryRun,
    bool Strict);
