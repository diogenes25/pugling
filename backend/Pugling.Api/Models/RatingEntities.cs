namespace Pugling.Api.Models;

// Der Sohn bewertet einen Lerninhalt danach, wie gut er zu seinem aktuellen Schulstoff passt.
// Das gibt dem Vater Signale, um den Lehrplan an den tatsächlichen Unterricht anzupassen.

/// <summary>5-stufige Bewertung eines Lerninhalts durch das Kind.</summary>
public enum ExerciseFeedback
{
    /// <summary>Entspricht genau unserem aktuellen Lernstoff.</summary>
    SehrGut = 0,
    /// <summary>Aus unserem Lernstoff, aber nicht das aktuelle Thema (Wiederholung).</summary>
    Gut = 1,
    /// <summary>Passt zu meinem Wissensstand.</summary>
    Neutral = 2,
    /// <summary>Dieses Thema haben wir (noch) nicht.</summary>
    Schlecht = 3,
    /// <summary>Übung ist fehlerhaft.</summary>
    Fehler = 4,
}

/// <summary>Bewertung eines Lerninhalts (Vokabel/Lückentext über die ContentId) durch das Kind.</summary>
public class ContentRating
{
    public int Id { get; set; }
    public int StudyPlanId { get; set; }
    public StudyPlan? StudyPlan { get; set; }
    public int ChildId { get; set; }
    /// <summary>Inhalts-Bezug (Vokabel-Id oder Lückentext-Id), wie in <see cref="StudyPlanItem.ContentId"/>.</summary>
    public int ContentId { get; set; }
    public ExerciseFeedback Feedback { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
