namespace Pugling.Api.Services.Student;

/// <summary>
/// Bonus training settings from the configuration (section <c>Gamification:ExtraTraining</c>). The bonus pays
/// for checked hits only and is capped per day, so extra practice is worth more coins without turning easy
/// forms into a coin farm.
/// </summary>
public class ExtraTrainingOptions
{
    /// <summary>Configuration section.</summary>
    public const string SectionName = "Gamification:ExtraTraining";

    /// <summary>Most bonus coins a child earns per day; <c>null</c> = no cap.</summary>
    public int? DailyCoinCap { get; set; } = 90;
    /// <summary>Coins per correct card on the choice form.</summary>
    public int CoinsPerChoiceHit { get; set; } = 1;
    /// <summary>Coins per correct card on the typed form.</summary>
    public int CoinsPerTypedHit { get; set; } = 3;
    /// <summary>Completed runs a vocabulary exercise is asked as self-assessment before it moves on to choice.</summary>
    public int SelfAssessRuns { get; set; } = 2;
    /// <summary>Completed runs on the choice form before a vocabulary exercise is asked typed for good.</summary>
    public int ChoiceRuns { get; set; } = 2;
}
