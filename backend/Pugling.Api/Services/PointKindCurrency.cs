using Pugling.Api.Models;

namespace Pugling.Api.Services;

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

/// <summary>
/// Ordnet jedem <see cref="PointKind"/> genau eine <see cref="Currency"/> zu. Fleiß fürs Lernen
/// (Basis/Zeit/Test/Tag) sowie manuelle Vater-Buchungen und Angebots-Käufe laufen über Münzen;
/// alle Motivations-Boni (Combo/Speed/Dauer/Missionen/Auszeichnungen) und Skin-Käufe über Gems.
/// <para>
/// Bewusst wird die Währung <b>aus dem Kind abgeleitet</b> statt als eigene Spalte gespeichert:
/// so bleibt der bestehende Ledger unverändert (keine Migration/kein Backfill), und der Kontostand
/// je Währung ist die Summe der Buchungen mit passendem Kind. Die Zuordnung ist erschöpfend – ein
/// nicht gemappter Kind wirft (siehe Test), damit ein neuer Kind niemals still aus dem Saldo fällt.
/// </para>
/// </summary>
public static class PointKindCurrency
{
    /// <summary>Währung, zu der eine Buchung des <paramref name="kind"/> zählt.</summary>
    public static Currency Of(PointKind kind) => kind switch
    {
        PointKind.Base or PointKind.Minutes or PointKind.Test or PointKind.DayComplete
            or PointKind.Manual or PointKind.Reward => Currency.Coins,
        PointKind.Combo or PointKind.Speed or PointKind.Duration or PointKind.Mission
            or PointKind.Achievement or PointKind.SkinPurchase => Currency.Gems,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "PointKind ohne Währungs-Zuordnung"),
    };

    /// <summary>Alle Buchungs-Kinds, die auf das Münz-Konto zählen (für Saldo-Queries).</summary>
    public static readonly PointKind[] CoinKinds =
        [.. Enum.GetValues<PointKind>().Where(k => Of(k) == Currency.Coins)];

    /// <summary>Alle Buchungs-Kinds, die auf das Gem-Konto zählen (für Saldo-Queries).</summary>
    public static readonly PointKind[] GemKinds =
        [.. Enum.GetValues<PointKind>().Where(k => Of(k) == Currency.Gems)];
}
