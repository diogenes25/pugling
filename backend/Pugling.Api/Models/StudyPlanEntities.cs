using System.ComponentModel.DataAnnotations.Schema;

namespace Pugling.Api.Models;

// Verfahrensneutraler Lehrplan-Rahmen: Zeit, Punkte, Fortschritt und Abschlusstest gelten
// für JEDES Lernverfahren gleich. Verfahrens-spezifisch sind nur der Inhalt (Vokabel vs.
// Lückentext) und die Test-Mechanik/Stufen (siehe TestsController / ClozeTestsController).

/// <summary>Lernverfahren eines Lehrplans.</summary>
public enum LearningMethod { Vocabulary = 0, Cloze = 1, Matching = 2 }

/// <summary>Stufe des Zuordnungs-Verfahrens (steigende Schwierigkeit). Nutzt den Vokabel-Store.</summary>
public enum MatchStage
{
    /// <summary>Wort → Übersetzung, keine Ablenker.</summary>
    Direct = 1,
    /// <summary>Wort → Übersetzung, mit Zusatz-Ablenkern im Auswahl-Pool.</summary>
    Distractors = 2,
    /// <summary>Übersetzung → Wort, keine Ablenker.</summary>
    Reverse = 3,
    /// <summary>Übersetzung → Wort, mit Ablenkern.</summary>
    ReverseDistractors = 4,
}

/// <summary>Teststufe des Vokabel-Lernkartentests (steigende Schwierigkeit).</summary>
public enum TestStage
{
    /// <summary>Vokabel + Übersetzung werden angezeigt (Kennenlernen).</summary>
    ShowBoth = 1,
    /// <summary>Vokabel -> aufdecken -> Selbsteinschätzung "gewusst? Ja/Nein".</summary>
    SelfAssess = 2,
    /// <summary>Übersetzung tippen; Länge bekannt (Buchstabenfelder), Buchstaben-Tipps möglich.</summary>
    LetterBoxes = 3,
    /// <summary>Übersetzung frei eintippen.</summary>
    FreeText = 4,
    /// <summary>Vokabel wird vorgelesen -> Übersetzung frei eintippen.</summary>
    Audio = 5,
}

/// <summary>Ein Schritt im Stufen-Fahrplan: ab Tag <c>DayNumber</c> (1-basiert) gilt Stufe <c>Stage</c>.</summary>
public record StageStep(int DayNumber, int Stage);

/// <summary>Vom Vater erstellter Lehrplan für ein Kind (z. B. "Vokabeltest in 10 Tagen").</summary>
public class StudyPlan
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public LearningMethod Method { get; set; } = LearningMethod.Vocabulary;
    public string Title { get; set; } = "";
    /// <summary>Optionale Verknüpfung zum Katalog-Fach – Basis für die Stundenplan-Steuerung.</summary>
    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    /// <summary>Wie viele neue Inhalte an einem Unterrichtstag eingeführt werden.</summary>
    public int NewItemsPerLesson { get; set; } = 5;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    // --- Tages-Anforderungen ---
    /// <summary>Mindest-Übungszeit pro Tag in Minuten.</summary>
    public int DailyMinutesRequired { get; set; } = 20;
    /// <summary>Muss täglich ein Abschlusstest bestanden werden?</summary>
    public bool DailyTestRequired { get; set; } = true;
    /// <summary>Bestehensgrenze des Tests in Prozent.</summary>
    public int DailyTestPassPercent { get; set; } = 80;
    /// <summary>Standard-Teststufe (verfahrensabhängig interpretiert), wenn Fahrplan/Angabe fehlen.</summary>
    public int DefaultStage { get; set; } = 2;
    /// <summary>
    /// Wenn true, zählt ein Test nur als bestanden, wenn er auf einer "gewerteten" Stufe läuft
    /// (getippt/Freitext) – verhindert bloßes Klicken/Auswählen. Bewertung setzt der Controller.
    /// </summary>
    public bool RequireTypedTest { get; set; }
    /// <summary>Optionaler Stufen-Fahrplan (Tag -> Stufe); steigert die Schwierigkeit über die Laufzeit.</summary>
    public List<StageStep>? StageSchedule { get; set; }

    // --- Leitner-Wiederholung (Karteikasten) ---
    /// <summary>
    /// Aktiviert die Karteikasten-Terminierung: jede Karte wandert bei richtiger Antwort eine Box höher
    /// (längeres Intervall), bei falscher zurück in Box 1. Die Wiederholung eines Tages umfasst dann nur
    /// die <em>fälligen</em> Karten. Aus = bisheriges Verhalten (alle eingeführten Inhalte).
    /// </summary>
    public bool UseLeitner { get; set; }
    /// <summary>Höchste Box (Standard 5).</summary>
    public int MaxBox { get; set; } = 5;
    /// <summary>
    /// Intervall in Tagen je Box (Index = Box; Index 0 ungenutzt). Null = Standard <c>[0,1,2,4,7,14]</c>.
    /// </summary>
    public List<int>? BoxIntervalDays { get; set; }

    // --- Punkte ---
    public int PointsMinutesMet { get; set; } = 10;
    public int PointsTestPassed { get; set; } = 20;
    public int PointsDayCompleteBonus { get; set; } = 10;

    /// <summary>Basispunkte für einen erstmals wiederholten (neuen) Inhalt – „neuer Stoff zählt am meisten".</summary>
    public int NewContentPoints { get; set; } = 10;

    // --- Combo (Motivations-Bonus für Treffer in Folge beim Üben) ---
    /// <summary>Alle N richtigen Antworten in Folge gibt es einen Combo-Bonus. 0 = Combo-Bonus aus.</summary>
    public int ComboThreshold { get; set; } = 5;
    /// <summary>Basis-Bonuspunkte je Combo-Meilenstein; eskaliert (N-ter Meilenstein → Basis × N). 0 = aus.</summary>
    public int ComboBonusPoints { get; set; } = 5;

    // --- Schnelle Antwort (Motivations-Bonus fürs zügige Beantworten) ---
    /// <summary>
    /// Wird eine Karte in höchstens so vielen Sekunden beantwortet, gibt es <see cref="SpeedBonusPoints"/>.
    /// Serverseitig gemessen (Zeit seit der letzten Antwort derselben Sitzung). 0 = Feature aus.
    /// </summary>
    public int SpeedThresholdSeconds { get; set; }
    /// <summary>Bonuspunkte für eine schnelle Antwort. 0 = aus.</summary>
    public int SpeedBonusPoints { get; set; }

    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<StudyPlanItem> Items { get; set; } = new();
}

/// <summary>Ein Lerninhalt im Plan – je nach Verfahren eine Vokabel ODER ein Lückentext.</summary>
public class StudyPlanItem
{
    public int Id { get; set; }
    public int StudyPlanId { get; set; }
    public StudyPlan? StudyPlan { get; set; }
    public int Order { get; set; }

    public int? VocabularyId { get; set; }
    public Vocabulary? Vocabulary { get; set; }
    public int? ClozeTextId { get; set; }
    public ClozeText? ClozeText { get; set; }
    /// <summary>Wann der Inhalt erstmals als "neu" eingeführt wurde. Null = noch nicht eingeführt.</summary>
    public DateOnly? IntroducedAt { get; set; }

    // --- Leitner-Box-Zustand (pro Kind, da ein Plan genau einem Kind gehört) ---
    /// <summary>Aktuelle Leitner-Box dieser Karte (1 = neu/schwer … MaxBox = sicher).</summary>
    public int Box { get; set; } = 1;
    /// <summary>Tag, an dem die Karte das nächste Mal fällig ist. Null = sofort fällig (noch nie bewertet).</summary>
    public DateOnly? DueOn { get; set; }
    /// <summary>Wie oft diese Karte schon per Leitner wiederholt wurde.</summary>
    public int ReviewCount { get; set; }
    /// <summary>Zeitpunkt der letzten Leitner-Wiederholung.</summary>
    public DateTime? LastReviewedAt { get; set; }

    /// <summary>Verfahrensneutraler Inhalts-Bezug (Vokabel-Id oder Lückentext-Id).</summary>
    [NotMapped]
    public int ContentId => VocabularyId ?? ClozeTextId ?? 0;
}

/// <summary>Übungssitzung: erfasst echte Übungszeit und was geübt wurde.</summary>
public class PracticeSession
{
    public int Id { get; set; }
    public int StudyPlanId { get; set; }
    public StudyPlan? StudyPlan { get; set; }
    public DateOnly Day { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    /// <summary>Aktiv geübte Sekunden (nur Zeit mit Interaktion).</summary>
    public int ActiveSeconds { get; set; }

    public List<ReviewEvent> Reviews { get; set; } = new();
}

/// <summary>Einzelne Wiederholung innerhalb einer Übungssitzung (verfahrensneutral).</summary>
public class ReviewEvent
{
    public int Id { get; set; }
    public int PracticeSessionId { get; set; }
    public PracticeSession? PracticeSession { get; set; }
    /// <summary>Inhalts-Bezug: Vokabel-Id oder Lückentext-Id.</summary>
    public int ContentId { get; set; }
    public int StageValue { get; set; }
    public bool WasCorrect { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
}

/// <summary>Ein Abschlusstest-Versuch an einem Tag (verfahrensneutral).</summary>
public class TestAttempt
{
    public int Id { get; set; }
    public int StudyPlanId { get; set; }
    public StudyPlan? StudyPlan { get; set; }
    public DateOnly Day { get; set; }
    /// <summary>Stufe (je nach Verfahren TestStage bzw. ClozeStage).</summary>
    public int StageValue { get; set; }
    /// <summary>Gilt dieser Versuch als "gewertet" (getippt/Freitext)? Setzt der Controller.</summary>
    public bool Graded { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int TotalItems { get; set; }
    public int CorrectItems { get; set; }
    public int ScorePercent { get; set; }
    public bool Passed { get; set; }

    public List<TestItemResult> Results { get; set; } = new();
}

/// <summary>Ergebnis einer einzelnen Test-Position (Vokabel bzw. Lückentext-Lücke).</summary>
public class TestItemResult
{
    public int Id { get; set; }
    public int TestAttemptId { get; set; }
    public TestAttempt? TestAttempt { get; set; }
    /// <summary>Inhalts-Bezug: Vokabel-Id oder Lückentext-Id.</summary>
    public int ContentId { get; set; }
    /// <summary>Bei Lückentext: Index der Lücke; bei Vokabeln null.</summary>
    public int? GapIndex { get; set; }
    public int StageValue { get; set; }
    public string? GivenAnswer { get; set; }
    public bool WasCorrect { get; set; }
    public int HintsUsed { get; set; }
}

/// <summary>Art einer Tages-Belohnung (für idempotente Punktevergabe).</summary>
public enum RewardKind { MinutesMet = 0, TestPassed = 1, DayCompleteBonus = 2 }

/// <summary>Protokolliert vergebene Tages-Belohnungen, damit Punkte nicht doppelt fließen.</summary>
public class StudyDayReward
{
    public int Id { get; set; }
    public int StudyPlanId { get; set; }
    public DateOnly Day { get; set; }
    public RewardKind Kind { get; set; }
    public int Points { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}
