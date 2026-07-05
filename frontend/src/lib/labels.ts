import type { OfferPeriod, PointKind, RewardRedemptionStatus } from "./types";

/** Deutsche Klartext-Labels für die Buchungs-Kategorien im Konto-/Wallet-Verlauf. */
const POINT_KIND_LABELS: Record<PointKind, string> = {
  Base: "Wiederholung",
  Manual: "Papa-Buchung",
  Minutes: "Übungszeit",
  Test: "Test bestanden",
  DayComplete: "Tag komplett",
  Goal: "Ziel erreicht",
  Combo: "Combo-Bonus",
  Speed: "Schnell-Bonus",
  Duration: "Ausdauer-Bonus",
  Mission: "Mission",
  Achievement: "Auszeichnung",
  SkinPurchase: "Skin gekauft",
  Reward: "Prämie eingelöst",
};
export const pointKindLabel = (k: PointKind): string => POINT_KIND_LABELS[k] ?? k;

/** Deutsche Labels für den Kauf-Status im Konto. */
const REDEMPTION_STATUS_LABELS: Record<RewardRedemptionStatus, string> = {
  Purchased: "gekauft – wartet auf Papa",
  Fulfilled: "erfüllt",
  Cancelled: "storniert (rückerstattet)",
};
export const redemptionStatusLabel = (s: RewardRedemptionStatus): string => REDEMPTION_STATUS_LABELS[s] ?? s;

/** Deutsche Labels für die Wiederkehr eines Angebots. */
const OFFER_PERIOD_LABELS: Record<OfferPeriod, string> = {
  OneOff: "einmalig",
  Daily: "täglich",
  Weekly: "wöchentlich",
  Monthly: "monatlich",
};
export const offerPeriodLabel = (p: OfferPeriod): string => OFFER_PERIOD_LABELS[p] ?? p;

/** Symbol + Name der beiden Währungen. */
export const COIN_LABEL = "🪙 Münzen";
export const GEM_LABEL = "💎 Gems";
