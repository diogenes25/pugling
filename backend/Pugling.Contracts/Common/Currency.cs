namespace Pugling.Contracts;

/// <summary>
/// Die beiden Währungen der App. <see cref="Coins"/> („Münzen") kauft <b>reale</b> Vater-Angebote,
/// <see cref="Gems"/> kauft <b>kosmetische/spielerische</b> Dinge (Skins, künftige Spielfeatures).
/// </summary>
public enum Currency
{
    /// <summary>Münzen 🪙 – für reale Angebote des Vaters (Spielzeit, Taschengeld …).</summary>
    Coins = 0,
    /// <summary>Gems 💎 – für Avatar/Skins und Spielfeatures.</summary>
    Gems = 1,
}
