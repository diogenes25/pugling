namespace Pugling.Api.Models;

/// <summary>
/// Zeitfenster mit Punkte-Multiplikator (vom Vater einstellbar). Gewichtet die Punkte für
/// Leitner-Wiederholungen: früher am Tag zählt mehr (siehe <see cref="Services.PointsService"/>).
/// </summary>
public class TimeSlotRule
{
    public int Id { get; set; }
    public string Name { get; set; } = "";     // "Vormittag"
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public double Multiplier { get; set; } = 1.0;
}
