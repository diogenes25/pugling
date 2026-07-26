namespace Pugling.Contracts;

/// <summary>
/// Lernverfahren – nur noch die Selbstbeschreibung im Übungstyp-Manifest (<see cref="ExerciseTypeManifest"/>)
/// braucht diese Zuordnung. Kein plan-weites Verfahren mehr.
/// </summary>
public enum LearningMethod { Vocabulary = 0, Cloze = 1, Matching = 2 }

/// <summary>Ein Schritt im Stufen-Fahrplan: ab Tag <c>DayNumber</c> (1-basiert) gilt Stufe <c>Stage</c>.</summary>
public record StageStep(int DayNumber, int Stage);

/// <summary>
/// Ausspiel-Modus einer Übungssitzung. <see cref="Info"/> = freies Üben: Inhalte am Stück, das Frontend
/// führt die Iteration, es fließt <b>kein</b> Lernfeedback (keine Bewertung/Punkte/Leitner, zählt nicht aufs
/// Ziel). <see cref="Lern"/> = server-geführt: der Server hält Cursor + eingefrorene Reihenfolge und bewertet.
/// </summary>
public enum PlayMode { Info = 0, Lern = 1 }
