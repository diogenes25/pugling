import type { PointKind } from "./types";

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
  // Tombstone: historische Buchungen des entfernten Angebots-Systems.
  Reward: "Prämie eingelöst",
  ManualGems: "Papa-Geschenk (Gems)",
  GoalPenalty: "Pflicht gerissen (Malus)",
};
export const pointKindLabel = (k: PointKind): string => POINT_KIND_LABELS[k] ?? k;

/** Symbol + Name der beiden Währungen. */
export const COIN_LABEL = "🪙 Münzen";
export const GEM_LABEL = "💎 Gems";
