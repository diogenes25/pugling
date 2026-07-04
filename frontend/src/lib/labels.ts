import type { PointKind, RewardRedemptionStatus } from "./types";

/** Deutsche Klartext-Labels für die Buchungs-Kategorien im Konto-/Wallet-Verlauf. */
const POINT_KIND_LABELS: Record<PointKind, string> = {
  Base: "Wiederholung",
  Manual: "Papa-Buchung",
  Minutes: "Übungszeit",
  Test: "Test bestanden",
  DayComplete: "Tag komplett",
  Combo: "Combo-Bonus",
  Speed: "Schnell-Bonus",
  Duration: "Ausdauer-Bonus",
  Mission: "Mission",
  Achievement: "Auszeichnung",
  SkinPurchase: "Skin gekauft",
  Reward: "Prämie eingelöst",
};
export const pointKindLabel = (k: PointKind): string => POINT_KIND_LABELS[k] ?? k;

/** Deutsche Labels für den Status einer Einlöse-Anfrage. */
const REDEMPTION_STATUS_LABELS: Record<RewardRedemptionStatus, string> = {
  Requested: "wartet auf Papa",
  Approved: "genehmigt",
  Rejected: "abgelehnt",
};
export const redemptionStatusLabel = (s: RewardRedemptionStatus): string => REDEMPTION_STATUS_LABELS[s] ?? s;
