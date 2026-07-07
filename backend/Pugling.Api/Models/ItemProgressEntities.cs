namespace Pugling.Api.Models;

/// <summary>Woher eine protokollierte Antwort stammt: freies Üben (Leitner) oder ein Abschlusstest.</summary>
public enum ItemReviewSource
{
    Practice = 0,
    Test = 1,
}

/// <summary>
/// Plan-übergreifender Lernstand eines Kindes zu einem einzelnen Übungs-Item (Vokabelpaar). Anders als
/// <see cref="PositionItemProgress"/> (Leitner-Terminierung je Plan-Position) hängt dieser Stand an der stabilen
/// <see cref="ExerciseItem.Id"/> (der „ItemId") und trägt die <see cref="VocabularyId"/> denormalisiert mit – so
/// lässt sich der Fortschritt sowohl je Übungs-Item als auch je Wort (übungsübergreifend) auswerten. Genau eine
/// Zeile je (Kind, Item); aus den protokollierten Antworten fortgeschrieben (siehe <see cref="ItemReviewEvent"/>).
/// </summary>
public class ItemProgress
{
    public int Id { get; set; }

    public int ChildId { get; set; }
    public Child? Child { get; set; }

    /// <summary>Das Übungs-Item; verschwindet mit ihm (Cascade).</summary>
    public int ItemId { get; set; }
    public ExerciseItem? Item { get; set; }

    /// <summary>Denormalisiert: Übung des Items (Filter „Fortschritt in dieser Übung").</summary>
    public int ExerciseId { get; set; }
    /// <summary>Denormalisiert: referenzierte Store-Vokabel (Rollup „wie sitzt dieses Wort über alle Übungen?").</summary>
    public int VocabularyId { get; set; }

    /// <summary>Aktuelle Leitner-Box (1 = neu/schwer … <see cref="MaxBox"/> = sicher).</summary>
    public int Box { get; set; } = 1;
    /// <summary>Höchste Box dieses aggregierten Stands (fest, plan-unabhängig).</summary>
    public const int MaxBox = 5;
    /// <summary>Beherrschung in Prozent, aus <see cref="Box"/> abgeleitet (für einfache Auswertung/Sortierung).</summary>
    public int MasteryPercent { get; set; }

    /// <summary>Wie oft das Item schon beantwortet wurde (Üben + Test).</summary>
    public int SeenCount { get; set; }
    /// <summary>Davon richtig beantwortet.</summary>
    public int CorrectCount { get; set; }

    /// <summary>Tag der ersten Beantwortung (erstmalige Einführung).</summary>
    public DateOnly? IntroducedAt { get; set; }
    /// <summary>Zeitpunkt der letzten Antwort.</summary>
    public DateTime? LastAnswerAt { get; set; }
    /// <summary>Ob die letzte Antwort richtig war.</summary>
    public bool? LastCorrect { get; set; }
}

/// <summary>
/// Eine einzelne protokollierte Antwort eines Kindes zu einem Item – die Historie hinter <see cref="ItemProgress"/>.
/// Trägt <see cref="ExerciseId"/>/<see cref="VocabularyId"/> denormalisiert, damit die Wort-Historie auch dann
/// erhalten bleibt, wenn das Item später gelöscht wird (<see cref="ItemId"/> wird dann auf <c>null</c> gesetzt).
/// </summary>
public class ItemReviewEvent
{
    public int Id { get; set; }

    public int ChildId { get; set; }
    public Child? Child { get; set; }

    /// <summary>Das Übungs-Item; <c>null</c>, wenn es nach der Antwort gelöscht wurde (Historie bleibt fürs Wort-Rollup).</summary>
    public int? ItemId { get; set; }
    public ExerciseItem? Item { get; set; }

    /// <summary>Denormalisiert: Übung des Items.</summary>
    public int ExerciseId { get; set; }
    /// <summary>Denormalisiert: referenzierte Store-Vokabel (Wort-Rollup, überlebt Item-Löschung).</summary>
    public int VocabularyId { get; set; }
    /// <summary>Optionaler Kontext: die Lehrplan-Position, über die geübt/getestet wurde.</summary>
    public int? PlanPositionId { get; set; }

    /// <summary>Herkunft der Antwort (Üben oder Test).</summary>
    public ItemReviewSource Source { get; set; }
    /// <summary>Die serverseitig erzwungene Stufe, in der die Antwort gegeben wurde.</summary>
    public int StageValue { get; set; }
    /// <summary>Die gegebene Antwort (bei getippten Stufen); <c>null</c> bei reiner Selbsteinschätzung.</summary>
    public string? GivenAnswer { get; set; }
    /// <summary>Ob die Antwort richtig war.</summary>
    public bool WasCorrect { get; set; }

    public DateTime At { get; set; } = DateTime.UtcNow;
}
