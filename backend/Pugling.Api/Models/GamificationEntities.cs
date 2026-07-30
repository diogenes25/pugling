namespace Pugling.Api.Models;

// Motivations-Ebene über den Einzel-Boni: Missionen (zeitgebundene, wiederholbare Ziele) und
// Auszeichnungen (permanente Meilensteine). Beide messen dieselben Fortschritts-Metriken über die
// Aktivität eines Kindes (siehe Services.MetricsService) und schütten über ChildPointsEntry aus.

// ProgressMetric/MissionPeriod/UnitType/ActionType/ShopRefillKind/ShopPurchaseStatus/
// ActivationRequestStatus leben im Vertrags-Projekt (Pugling.Contracts).

/// <summary>
/// Ein vom Vater definiertes Ziel für ein Kind (Tages-/Wochen-/Zusatzziel). Erfüllt das Kind im
/// jeweiligen Zeitraum die <see cref="Target"/>-Marke der <see cref="Metric"/>, gibt es einmalig
/// <see cref="RewardPoints"/>. Sinnvolle Vorlagen werden geseedet, sind aber frei editier-/löschbar.
/// </summary>
public class Mission
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public string Title { get; set; } = "";
    public ProgressMetric Metric { get; set; }
    /// <summary>Zu erreichender Wert der Metrik im Zeitraum.</summary>
    public int Target { get; set; }
    public MissionPeriod Period { get; set; }
    /// <summary>Belohnung bei Erfüllung (einmal je Zeitraum).</summary>
    public int RewardPoints { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Protokolliert die einmalige Belohnung einer Mission je Zeitraum (idempotent, Anti-Doppelvergabe).</summary>
public class MissionAward
{
    public int Id { get; set; }
    public int MissionId { get; set; }
    public Mission? Mission { get; set; }
    /// <summary>Zeitraum-Schlüssel: "2026-07-04" (täglich), "2026-W27" (wöchentlich) oder "once".</summary>
    public string PeriodKey { get; set; } = "";
    public int Points { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Eine vom Vater definierte Auszeichnung (Badge) für ein Kind: ab <see cref="Threshold"/> der
/// <see cref="Metric"/> (lebenslang gezählt bzw. aktuelle Serie) einmalig verliehen, mit Emoji-Icon
/// und optionaler Punkte-Belohnung. Duolingo-artige Meilensteine, frei konfigurierbar.
/// </summary>
public class Achievement
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    public string Title { get; set; } = "";
    /// <summary>Emoji o. Ä. für die Badge-Darstellung (z. B. "🔥").</summary>
    public string? Icon { get; set; }
    public ProgressMetric Metric { get; set; }
    /// <summary>Schwelle, ab der die Auszeichnung erreicht ist.</summary>
    public int Threshold { get; set; }
    /// <summary>Optionale Punkte-Belohnung beim Erreichen (0 = nur Badge).</summary>
    public int RewardPoints { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Protokolliert, wann ein Kind eine Auszeichnung erreicht hat (genau einmal, idempotent).</summary>
public class AchievementAward
{
    public int Id { get; set; }
    public int AchievementId { get; set; }
    public Achievement? Achievement { get; set; }
    public int Points { get; set; }
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
}

// Hinweis: Das frühere „Angebots"-System (Reward/RewardRedemption/OfferPeriod) wurde entfernt – der
// Familien-Shop (ShopArticle/ShopListing/ShopPurchase/ActivationRequest) ist der einzige Münz-Ausgabeweg.
// Der Ledger-Kind <c>PointKind.Reward</c> bleibt nur als Tombstone für historische Buchungen bestehen.

/// <summary>
/// Basis-Katalogartikel des Vaters. Definiert die <em>Art</em> des Artikels (z. B. „Fernsehen" mit
/// <see cref="UnitType"/> Minute und <see cref="ActionType"/> TV). Preis und Bestand liegen in
/// <see cref="ShopListing"/>s – ein Artikel kann mehrere Angebote zu unterschiedlichen Konditionen haben.
/// Artikelnummern sind familienintern eindeutig.
/// </summary>
public class ShopArticle
{
    public int Id { get; set; }
    public int AdultId { get; set; }
    public Adult? Adult { get; set; }
    /// <summary>Familieninterne Artikelnummer/SKU, eindeutig je Vater.</summary>
    public string ArticleNumber { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Maßeinheit der Menge (z. B. <see cref="UnitType.Minute"/> für Fernsehzeit).</summary>
    public UnitType UnitType { get; set; }
    /// <summary>Aktionstyp (z. B. <see cref="ActionType.TV"/>); kategorisiert den Artikel für die Vater-Sicht.</summary>
    public ActionType ActionType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ShopListing> Listings { get; set; } = [];
}

/// <summary>
/// Kaufbares Angebot zu einem <see cref="ShopArticle"/>. Ein Artikel kann mehrere Angebote mit
/// unterschiedlichen Preisen und Mengen haben (z. B. „10 Min TV für 50 Münzen" und
/// „60 Min TV für 250 Münzen"). <see cref="UnitsPerPurchase"/> gibt an, wie viele Einheiten
/// (in der <see cref="UnitType"/> des Artikels) ein Kauf ins <see cref="ChildInventory"/> des Sohns legt.
/// </summary>
public class ShopListing
{
    public int Id { get; set; }
    public int ShopArticleId { get; set; }
    public ShopArticle? ShopArticle { get; set; }
    /// <summary>Optionaler Anzeige-Titel; wenn leer, wird der Titel des zugehörigen Artikels verwendet.</summary>
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Preisanteil in Münzen; darf 0 sein, wenn Gems gesetzt sind.</summary>
    public int CoinPrice { get; set; }
    /// <summary>Preisanteil in Gems; darf 0 sein, wenn Münzen gesetzt sind.</summary>
    public int GemPrice { get; set; }
    /// <summary>Menge (in der <see cref="UnitType"/> des Artikels) pro Kauf, z. B. 30 für „30 Minuten".</summary>
    public int UnitsPerPurchase { get; set; } = 1;
    public bool Active { get; set; } = true;
    /// <summary>Aktuell kaufbarer Lagerbestand.</summary>
    public int CurrentStock { get; set; }
    /// <summary>Zielbestand, auf den automatische Auffüllungen setzen.</summary>
    public int MaxStock { get; set; }
    public ShopRefillKind RefillKind { get; set; } = ShopRefillKind.None;
    /// <summary>Optionaler einmaliger Auffüllzeitpunkt (UTC) für <see cref="ShopRefillKind.Once"/>.</summary>
    public DateTime? RefillAtUtc { get; set; }
    /// <summary>Optionaler Wochentag für <see cref="ShopRefillKind.Weekly"/>.</summary>
    public DayOfWeek? RefillDayOfWeek { get; set; }
    /// <summary>Letzte angewendete automatische Auffüllung; macht Refill idempotent.</summary>
    public DateTime? LastRefilledAtUtc { get; set; }
    /// <summary>Nebenläufigkeits-Marke für Bestand/Auffüllung: parallele Käufe dürfen den Stock nicht überziehen.</summary>
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Aggregiertes Inventar eines Kindes für einen <see cref="ShopArticle"/>. Mehrere Käufe desselben
/// Artikels (über verschiedene <see cref="ShopListing"/>s oder zu unterschiedlichen Zeiten) summieren
/// sich hier auf. Das Kind kann aus diesem Bestand Aktivierungsanfragen stellen.
/// <para>
/// <b>Bezahlte Einheiten sind Geld und überleben darum Katalogpflege.</b> Der Artikelbezug ist deshalb
/// optional (FK <c>SetNull</c>) und die anzeigetragenden Felder liegen als <b>Momentaufnahme</b> daneben –
/// dasselbe Muster wie bei <see cref="ShopPurchase"/> und <see cref="ActivationRequest"/>. Vorher
/// kaskadierte das Löschen eines Artikels bis hierher und vernichtete gekaufte, noch nicht verbrauchte
/// Einheiten, während der Kaufbeleg per <c>SetNull</c> daneben stehenblieb: ein Beleg ohne Gegenwert.
/// </para>
/// </summary>
public class ChildInventory
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    /// <summary>Referenz auf den Artikel; wird auf null gesetzt, falls der Artikel später gelöscht wird.</summary>
    public int? ShopArticleId { get; set; }
    public ShopArticle? ShopArticle { get; set; }
    /// <summary>
    /// Ausstellender Supervisor (Momentaufnahme aus <c>ShopArticle.AdultId</c>). Trägt die Vater-Sicht,
    /// nachdem der Artikel gelöscht ist – die filterte vorher über <c>ShopArticle.AdultId</c> und hätte
    /// den Posten unsichtbar gemacht, was so gut wie gelöscht ist.
    /// </summary>
    public int SupervisorId { get; set; }
    // Momentaufnahmen (Bestand bleibt lesbar und sortierbar, auch nachdem der Artikel gelöscht ist)
    /// <summary>Artikelnummer zum Kaufzeitpunkt; sie ist auch der Sortierschlüssel beider Inventar-Sichten.</summary>
    public string ArticleNumber { get; set; } = "";
    public string ArticleTitle { get; set; } = "";
    /// <summary>Maßeinheit der Menge (z. B. <see cref="UnitType.Minute"/>).</summary>
    public UnitType UnitType { get; set; }
    /// <summary>Aktionstyp (z. B. <see cref="ActionType.TV"/>).</summary>
    public ActionType ActionType { get; set; }
    /// <summary>Verfügbare Gesamtmenge in der Einheit des Artikels (z. B. 120 Minuten TV).</summary>
    public int Quantity { get; set; }
    /// <summary>Nebenläufigkeits-Marke: verhindert, dass gleichzeitige Aktivierungen den Bestand überziehen.</summary>
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();
}

/// <summary>
/// Historische Kaufbuchung für ein <see cref="ShopListing"/>. Artikelnummer, Titel, Preise und
/// <see cref="UnitsPerPurchase"/> werden als Momentaufnahme gespeichert, damit die Kaufhistorie
/// stabil bleibt, wenn der Vater das Angebot später ändert oder löscht.
/// </summary>
public class ShopPurchase
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    /// <summary>Referenz auf das Angebot; wird auf null gesetzt, falls das Angebot später gelöscht wird.</summary>
    public int? ShopListingId { get; set; }
    public ShopListing? ShopListing { get; set; }
    /// <summary>Ausstellender Supervisor (Momentaufnahme aus <c>ShopArticle.AdultId</c>): nur er storniert.</summary>
    public int SupervisorId { get; set; }
    // Momentaufnahmen (stabile Kaufhistorie auch nach Änderung/Löschung des Angebots)
    public string ArticleNumber { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int CoinPrice { get; set; }
    public int GemPrice { get; set; }
    /// <summary>Menge, um die das Inventar beim Kauf erhöht wurde (Momentaufnahme von <see cref="ShopListing.UnitsPerPurchase"/>).</summary>
    public int UnitsPerPurchase { get; set; } = 1;
    public ShopPurchaseStatus Status { get; set; } = ShopPurchaseStatus.Owned;
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    /// <summary>Nebenläufigkeits-Marke für Stornieren, damit ein Kauf nur einmal geschlossen wird.</summary>
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();
}

/// <summary>
/// Aktivierungsanfrage des Sohns: er möchte <see cref="RequestedQuantity"/> Einheiten aus seinem
/// aggregierten Inventar (<see cref="ChildInventory"/>) verbrauchen. Der Vater genehmigt oder lehnt
/// ab; nur bei Genehmigung wird das Inventar reduziert. Titel und Einheit werden als Momentaufnahme
/// festgehalten, damit die Anfrage-Historie auch nach Artikel-Löschung lesbar bleibt.
/// </summary>
public class ActivationRequest
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child? Child { get; set; }
    /// <summary>Referenz auf den Artikel; wird auf null gesetzt, falls der Artikel später gelöscht wird.</summary>
    public int? ShopArticleId { get; set; }
    public ShopArticle? ShopArticle { get; set; }
    /// <summary>Ausstellender Supervisor (Momentaufnahme aus <c>ShopArticle.AdultId</c>): nur er genehmigt/lehnt ab.</summary>
    public int SupervisorId { get; set; }
    /// <summary>Beantragte Menge in der Einheit des Artikels (z. B. 10 Minuten).</summary>
    public int RequestedQuantity { get; set; }
    public ActivationRequestStatus Status { get; set; } = ActivationRequestStatus.Pending;
    // Momentaufnahmen
    public string ArticleTitle { get; set; } = "";
    public UnitType UnitType { get; set; }
    public ActionType ActionType { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Zeitpunkt der Vater-Entscheidung (null solange offen).</summary>
    public DateTime? ClosedAt { get; set; }
}
