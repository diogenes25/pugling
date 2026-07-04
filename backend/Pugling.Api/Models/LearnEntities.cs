namespace Pugling.Api.Models;

// Gemeinsamer Lern-Katalog (learn):
//   Subject -> Chapter -> Exercise (typisiert)
// Der Katalog wird EINMAL gepflegt (nicht pro Kind) und später Kindern zugeordnet.

/// <summary>Schulfach im Lehrplan-Katalog (z. B. Englisch, Mathe).</summary>
public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Chapter> Chapters { get; set; } = new();
}

/// <summary>Kapitel innerhalb eines Fachs.</summary>
public class Chapter
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public string Name { get; set; } = "";
    public int OrderIndex { get; set; }

    public List<Exercise> Exercises { get; set; } = new();
}

/// <summary>Art der Übung. Bestimmt, wie <see cref="Exercise.ConfigJson"/> interpretiert wird.</summary>
public enum ExerciseType
{
    Vocabulary = 0,
    Reading = 1,
    Cloze = 2,
    Essay = 3,
    Listening = 4,
    Grammar = 5,
    Matching = 6,
    Translation = 7,
    Arithmetic = 8,
    ArithmeticDrill = 9,
    List = 10,
    Birkenbihl = 11,
}

/// <summary>
/// Eine Übung in einem Kapitel. Die gemeinsamen Felder sind typisiert;
/// der typ-spezifische Teil steckt als JSON in <see cref="ConfigJson"/>
/// und wird im API pro Typ als eigenes Schema ein-/ausgegeben.
/// </summary>
public class Exercise
{
    public int Id { get; set; }
    public int ChapterId { get; set; }
    public Chapter? Chapter { get; set; }
    public ExerciseType Type { get; set; }
    public string Title { get; set; } = "";
    public int OrderIndex { get; set; }
    /// <summary>Punkte, die das Kind für das Absolvieren erhält.</summary>
    public int RewardPoints { get; set; }
    /// <summary>Typ-spezifische Konfiguration als JSON (siehe die *Config-Klassen).</summary>
    public string ConfigJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
