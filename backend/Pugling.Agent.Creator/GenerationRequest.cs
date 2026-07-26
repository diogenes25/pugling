namespace Pugling.Agent.Creator;

/// <summary>
/// Der Auftrag an den Agenten: <b>in wessen Namen</b> (Creator-Profil), <b>für wen</b> (Kind – oder
/// niemanden, dann entsteht eine allgemeine Katalog-Übung), <b>wo</b> im Katalog (Fach/Kapitel),
/// <b>was</b> (Übungstyp, Thema, Umfang) und unter welchen Sicherheitsregeln (Trockenlauf, Selbsttest).
/// </summary>
/// <param name="ChildId">
/// Das Kind, auf das zugeschnitten wird (Profil, Interessen, Lernstand) – oder <c>null</c> für eine
/// <b>allgemeine</b> Übung. Ohne Kind braucht der Agent kein Betreuungsrecht, nur die Creator-Rolle.
/// </param>
/// <param name="ProfileId">
/// Das Creator-Profil („Fachlehrer"). Fehlt es, sucht der Agent bei gesetztem <paramref name="ChildId"/>
/// das am besten passende (Reihen-Treffer zuerst); ohne beides ist der Auftrag unvollständig.
/// </param>
/// <param name="UnitId">
/// Die Unit der Lehrwerk-Reihe, deren Stoff gilt. Fehlt sie, gilt die aktuelle Unit des Kindes; fehlt
/// auch die, trägt allein <paramref name="Topic"/> den Stoff.
/// </param>
/// <param name="General">
/// Auch mit <paramref name="ChildId"/> <b>ohne</b> Individualisierung entwerfen: Reihe und Unit des
/// Kindes bestimmen den Stoff, seine Interessen bleiben außen vor. Für Übungen, die in den gemeinsamen
/// Katalog sollen, aber am Stand eines konkreten Kindes ausgerichtet sind.
/// </param>
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
/// <param name="SourceLang">Sprachcode der Lernsprache; <c>null</c> = den des Profils übernehmen.</param>
/// <param name="TargetLang">Sprachcode der Muttersprache; <c>null</c> = den des Profils übernehmen.</param>
/// <param name="RewardPoints">Punkte, die die Übung wert ist.</param>
/// <param name="DryRun">Nur planen und drucken – nichts schreiben.</param>
/// <param name="Strict">Übung wieder löschen, wenn der Selbsttest nicht 100 % erreicht.</param>
public sealed record GenerationRequest(
    int? ChildId,
    int? ProfileId,
    int? UnitId,
    bool General,
    int SubjectId,
    int ChapterId,
    string TypeKey,
    string? Topic,
    int ItemCount,
    IReadOnlyList<string> Words,
    bool UseWeakWords,
    string? SourceLang,
    string? TargetLang,
    int RewardPoints,
    bool DryRun,
    bool Strict);
