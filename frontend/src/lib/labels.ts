import type { Gender, PointKind, SchoolType } from "./types";

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

/** Wählbare Schularten (ohne `None` – das ist „nicht gesetzt", kein Auswahlwert). */
export const SCHOOL_TYPES: SchoolType[] = [
  "Grundschule", "Hauptschule", "Realschule", "Gymnasium", "Gesamtschule", "Berufsschule",
];

/**
 * Klartext für die Begründungs-Codes der Fachlehrer-Suche. Der Server liefert bewusst **Codes** statt
 * Sätze (stabiler Vertrag, i18n bleibt Sache der Oberfläche) – hier werden sie zu Deutsch.
 */
const MATCH_REASON_LABELS: Record<string, string> = {
  series_match: "gleiche Buchreihe",
  subject_match: "gleiches Fach",
  grade_in_range: "Klassenstufe passt",
  school_type_match: "Schulart passt",
};
export const matchReasonLabel = (code: string): string => MATCH_REASON_LABELS[code] ?? code;

/** Geschlecht mit Klartext; `None` bleibt wählbar, weil „keine Angabe" eine legitime Antwort ist. */
export const GENDERS: { value: Gender; label: string }[] = [
  { value: "None", label: "keine Angabe" },
  { value: "Male", label: "männlich" },
  { value: "Female", label: "weiblich" },
  { value: "Diverse", label: "divers" },
];
