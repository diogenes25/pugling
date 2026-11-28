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
    /// <summary>
    /// Play seconds a wave takes at least. The reported wave of a run counts only as far as its counted play time
    /// covers it, so a forged wave number buys nothing.
    /// </summary>
    public int MinSecondsPerWave { get; set; } = 20;
    /// <summary>Correct assignments per day that keep the game streak and count the day.</summary>
    public int StreakWords { get; set; } = 10;
    /// <summary>Correct assignments per day that fill the game chest.</summary>
    public int ChestWords { get; set; } = 15;
    /// <summary>Fewest gems the game chest pays.</summary>
    public int ChestMinGems { get; set; } = 3;
    /// <summary>Most gems the game chest pays.</summary>
    public int ChestMaxGems { get; set; } = 8;
    /// <summary>
    /// The trophy path: lifetime trophies and the gems reaching them pays once. Empty by default and set in the
    /// configuration - a list default would be appended to, not replaced, by the configuration binder.
    /// </summary>
    public List<TowerTrophyPathStep> TrophyPath { get; set; } = [];
    /// <summary>
    /// The hero skins and their prices in gems; a price of 0 is owned by every child. Empty by default and set in
    /// the configuration, like <see cref="TrophyPath"/>. The ids are what the app knows its looks by.
    /// </summary>
    public List<TowerSkinPrice> Skins { get; set; } = [];
}

/// <summary>One configured hero skin.</summary>
public class TowerSkinPrice
{
    /// <summary>The skin's id, shared with the app.</summary>
    public string Id { get; set; } = "";
    /// <summary>Its price in gems; 0 = free.</summary>
    public int Gems { get; set; }
}

/// <summary>One configured step of the trophy path.</summary>
public class TowerTrophyPathStep
{
    /// <summary>Lifetime trophies that reach the step.</summary>
    public int Trophies { get; set; }
    /// <summary>Gems the step pays once.</summary>
    public int Gems { get; set; }
}
