namespace Pugling.Contracts;

/// <summary>
/// The app's two currencies. <see cref="Coins"/> ("coins") buys <b>real-world</b> supervisor offers,
/// <see cref="Gems"/> buys <b>cosmetic/playful</b> things (skins, future game features).
/// </summary>
public enum Currency
{
    /// <summary>Coins 🪙 – for real-world offers from the supervisor (playtime, allowance …).</summary>
    Coins = 0,
    /// <summary>Gems 💎 – for avatar/skins and game features.</summary>
    Gems = 1,
}
