import type { ActionType, ShopRefillKind, UnitType } from "./types";

// Geteilte Anzeige-Helfer für den Familien-Shop (Sohn-App + Vater-Web).
// Die deutschen Labels/Emojis liegen bewusst hier zentral, damit beide Rollen dieselbe Sprache
// sprechen; die Enum-Werte selbst kommen 1:1 vom Server (JsonStringEnumConverter).

/** Kurz-Einheit für Mengenangaben („30 Min", „50 g"). */
export const UNIT_LABEL: Record<UnitType, string> = {
  Stueck: "Stück",
  Minute: "Min",
  Stunde: "Std",
  Gramm: "g",
  Mal: "Mal",
};

/** Emoji je Belohnungsart – gibt den Shop-Karten ein Gesicht. */
export const ACTION_EMOJI: Record<ActionType, string> = {
  Sonstiges: "🎁",
  TV: "📺",
  Zocken: "🎮",
  Suessigkeit: "🍬",
  Ausflug: "🎡",
};

/** Klartext-Name der Belohnungsart (Vater-Verwaltung). */
export const ACTION_LABEL: Record<ActionType, string> = {
  Sonstiges: "Sonstiges",
  TV: "Fernsehen",
  Zocken: "Zocken",
  Suessigkeit: "Süßigkeit",
  Ausflug: "Ausflug",
};

/** Klartext-Name der Auffüll-Regel. */
export const REFILL_LABEL: Record<ShopRefillKind, string> = {
  None: "Kein Auffüllen",
  Once: "Einmalig",
  Daily: "Täglich",
  TwiceDaily: "2× täglich",
  Weekly: "Wöchentlich",
};

/** Menge mit Einheit, z. B. „30 Min", „50 g", „1 Mal". */
export const unitAmount = (quantity: number, unit: UnitType): string => `${quantity} ${UNIT_LABEL[unit]}`;

/**
 * Preis-Text eines Angebots: zeigt nur die positiven Preise. Ein Angebot kann Coin- UND Gem-Preis
 * tragen (Backend erzwingt: mindestens einer > 0), beide werden dann kombiniert dargestellt.
 */
export function priceLabel(coinPrice: number, gemPrice: number): string {
  const parts: string[] = [];
  if (coinPrice > 0) parts.push(`🪙 ${coinPrice}`);
  if (gemPrice > 0) parts.push(`💎 ${gemPrice}`);
  return parts.join(" + ") || "gratis";
}

/** Auswahllisten für Formulare (Vater legt Artikel/Angebote an). */
export const UNIT_OPTIONS: { value: UnitType; label: string }[] = [
  { value: "Minute", label: "Minuten" },
  { value: "Stunde", label: "Stunden" },
  { value: "Gramm", label: "Gramm" },
  { value: "Stueck", label: "Stück" },
  { value: "Mal", label: "Mal" },
];

export const ACTION_OPTIONS: { value: ActionType; label: string }[] = [
  { value: "TV", label: "📺 Fernsehen" },
  { value: "Zocken", label: "🎮 Zocken" },
  { value: "Suessigkeit", label: "🍬 Süßigkeit" },
  { value: "Ausflug", label: "🎡 Ausflug" },
  { value: "Sonstiges", label: "🎁 Sonstiges" },
];
