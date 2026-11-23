namespace Pugling.Api.Services.Student;

/// <summary>
/// Tower-game settings from the configuration (section <c>Gamification:TowerGame</c>, B-851). Coins stay the father's
/// lever, so the game pays little and has its own daily cap; the minute limit keeps it a supplement to practice.
/// </summary>
public class TowerGameOptions
{
    /// <summary>Configuration section.</summary>
    public const string SectionName = "Gamification:TowerGame";

    /// <summary>Coins for a won level.</summary>
    public int CoinsPerWin { get; set; } = 10;
    /// <summary>Most tower-game coins a child earns per day.</summary>
    public int DailyCoinCap { get; set; } = 30;
    /// <summary>Default play minutes per day; the supervisor may override it per child.</summary>
    public int MinutesPerDay { get; set; } = 20;
    /// <summary>Minutes a level already running may go on after the daily limit is reached; it is graded normally.</summary>
    public int GraceMinutes { get; set; } = 5;
    /// <summary>Most seconds one heartbeat adds. The app beats every ~15 s; a longer gap is a paused or backgrounded app.</summary>
    public int MaxSecondsPerBeat { get; set; } = 20;
}
