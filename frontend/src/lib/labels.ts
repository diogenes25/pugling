import type {
  Gender, GrantPermission, InterestFacet, PointKind, SchoolType, SupervisorRelation, Weekday,
} from "./types";

/** Deutsche Klartext-Labels für die Buchungs-Kategorien im Konto-/Wallet-Verlauf. */
const POINT_KIND_LABELS: Record<PointKind, string> = {
  Base: "Wiederholung",
  Manual: "Papa-Buchung",
  Goal: "Ziel erreicht",
  Combo: "Combo-Bonus",
  Speed: "Schnell-Bonus",
  Mission: "Mission",
  Achievement: "Auszeichnung",
  SkinPurchase: "Skin gekauft",
  ShopCoins: "Im Shop gekauft",
  ShopGems: "Im Shop gekauft (Gems)",
  ManualGems: "Papa-Geschenk (Gems)",
  GoalPenalty: "Pflicht gerissen (Malus)",
  ObjectiveCoins: "Ziel-Etappe erreicht",
  ObjectiveGems: "Ziel-Etappe erreicht (Gems)",
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

/**
 * Verwandtschaft eines Betreuers zum Kind. Rein deskriptiv – die Steuerrechte sind für alle Betreuer
 * gleich; nur das Einlösen bleibt ausstellergebunden.
 */
export const SUPERVISOR_RELATIONS: { value: SupervisorRelation; label: string }[] = [
  { value: "Mother", label: "Mutter" },
  { value: "Father", label: "Vater" },
  { value: "Grandma", label: "Oma" },
  { value: "Grandpa", label: "Opa" },
  { value: "Guardian", label: "Vormund" },
  { value: "Other", label: "sonstige" },
];
export const supervisorRelationLabel = (r: SupervisorRelation): string =>
  SUPERVISOR_RELATIONS.find((x) => x.value === r)?.label ?? r;

/**
 * Facetten der geteilten Interessen-Taxonomie. `Style` (Comic/Foto/Pixel-Art) steht bewusst im selben
 * Vokabular wie die Themen – bei der Bildauswahl verhält es sich gleich, nur schwächer gewichtet.
 */
export const INTEREST_FACETS: { value: InterestFacet; label: string }[] = [
  { value: "Franchise", label: "Marke/Serie" }, { value: "Sport", label: "Sport" },
  { value: "Animal", label: "Tiere" }, { value: "Vehicle", label: "Fahrzeuge" },
  { value: "Music", label: "Musik" }, { value: "Hobby", label: "Hobby" },
  { value: "Nature", label: "Natur" }, { value: "Style", label: "Stil" },
  { value: "Other", label: "Sonstiges" },
];
export const interestFacetLabel = (f: InterestFacet): string =>
  INTEREST_FACETS.find((x) => x.value === f)?.label ?? f;

/** Wochentage in der Reihenfolge der Schulwoche (der Server serialisiert `System.DayOfWeek` als Name). */
export const WEEKDAYS: { value: Weekday; label: string }[] = [
  { value: "Monday", label: "Montag" },
  { value: "Tuesday", label: "Dienstag" },
  { value: "Wednesday", label: "Mittwoch" },
  { value: "Thursday", label: "Donnerstag" },
  { value: "Friday", label: "Freitag" },
  { value: "Saturday", label: "Samstag" },
  { value: "Sunday", label: "Sonntag" },
];

/**
 * Die RWX-Rechte an einer Übung in der Sprache des Vaters. `hint` erklärt die Folge der Vergabe – die
 * Hierarchie `Owner` ⊃ `Write` ⊃ `Execute` sieht man den Namen sonst nicht an. **Lesen** fehlt bewusst:
 * das darf jeder Creator und ist kein vergebbares Recht.
 */
export const GRANT_PERMISSIONS: { value: GrantPermission; label: string; hint: string }[] = [
  { value: "Execute", label: "Zuweisen", hint: "darf die Übung in eigene Lehrpläne hängen" },
  { value: "Write", label: "Bearbeiten", hint: "darf Inhalt und Beschreibung ändern" },
  { value: "Owner", label: "Verwalten", hint: "darf zusätzlich löschen und Rechte vergeben" },
];
export const grantPermissionLabel = (p: GrantPermission): string =>
  GRANT_PERMISSIONS.find((x) => x.value === p)?.label ?? p;

/** Geschlecht mit Klartext; `None` bleibt wählbar, weil „keine Angabe" eine legitime Antwort ist. */
export const GENDERS: { value: Gender; label: string }[] = [
  { value: "None", label: "keine Angabe" },
  { value: "Male", label: "männlich" },
  { value: "Female", label: "weiblich" },
  { value: "Diverse", label: "divers" },
];
