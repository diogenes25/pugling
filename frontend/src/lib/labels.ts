import type {
  Gender, GrantPermission, InterestFacet, PointKind, SchoolType, SchoolTypeValue, SupervisorRelation,
  Weekday,
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
  DailyBoxCoins: "Tägliche Belohnungsbox",
  DailyBoxGems: "Tägliche Belohnungsbox (Gems)",
};
export const pointKindLabel = (k: PointKind): string => POINT_KIND_LABELS[k] ?? k;

/** Symbol + Name der beiden Währungen. */
export const COIN_LABEL = "🪙 Münzen";
export const GEM_LABEL = "💎 Gems";

/*
 * Die **wählbaren** Schularten – abgeleitet statt abgeschrieben (B-149).
 *
 * Die Liste war eine handgepflegte Kopie eines Server-Enums, und seit B-143/B-148 ist sie nicht mehr nur
 * beschriftend: Vier Stellen fragen sie, ob ein Wert überhaupt auswählbar ist. Ergänzte der Server eine
 * Schulart, zeigte das UI sie als „Kombination" (unwählbar), verwarf sie still oder machte „für alle"
 * unerreichbar – je nach Stelle etwas anderes.
 *
 * `SELECTABLE` ist der Tausch, der das beendet: Der `Record` **muss** jeden Wert des Server-Enums tragen.
 * Fehlt einer, ist `tsc -b` rot und nennt diese Datei; steht einer zu viel darin, ebenso. Keine
 * Typzusicherung nötig – `Object.keys` liefert `string[]`, und `SchoolType` *ist* ein String.
 *
 * Das Literal muss dabei **direkt** hier stehen. Die Richtung „ein Wert wurde entfernt" hängt am
 * Excess-Property-Check, und den gibt es nur beim frischen Objektliteral: Wer die Zuweisung über eine
 * Variable oder ein Spread führt, verliert diese Hälfte des Tors lautlos.
 *
 * **Reichweite, ehrlich:** Das Tor ist der Compiler, also `npm run build` und CI. `npm test` fährt es
 * nicht. Und es meldet nur – nachtragen muss die Zeile unten jemand von Hand.
 *
 * `None` ist ausgenommen, weil es **kein Einzelwert der Auswahl** ist. Nicht, weil es „nicht gesetzt"
 * hieße: Das stimmt nur am Kind (`VaterKind`, „– keine Angabe –"). An Reihe und Fachlehrer-Profil ist
 * `None` ein echter, gewollter Wert und heißt „für alle" – dort steht es als eigene Option im Feld; an
 * der Übung entsteht derselbe Wert implizit aus einer leeren Checkbox-Gruppe.
 *
 * Die **Reihenfolge** ist eine Anzeigeentscheidung dieser Datei (aufsteigend nach Schulform), keine des
 * Servers: `Object.keys` gibt die Schreibreihenfolge des Literals zurück.
 */
const SELECTABLE: Record<Exclude<SchoolTypeValue, "None">, true>
  // Die Wache über der Wache: Verlöre `SchoolTypeValue` je seine Werteliste und wäre wieder ein nackter
  // `string` – etwa weil jemand das Geschwister-Schema im Transformer anfasst –, dann kollabierte
  // `Exclude<…>` zu `string`, und `Record<string, true>` nähme jedes Literal an. Das Tor wäre still tot,
  // und genau diese Sorte Stille ist der Grund für diese Story. Verschwindet das Schema ganz, meldet es
  // schon `types.ts`; nur der Zwischenzustand „da, aber wertlos" braucht diese Zeile.
  & (string extends SchoolTypeValue ? { readonly enumVerfallen: never } : unknown) = {
  Grundschule: true, Hauptschule: true, Realschule: true,
  Gymnasium: true, Gesamtschule: true, Berufsschule: true,
};

/** Wählbare Schularten (ohne `None`) – abgeleitet aus dem Server-Enum, Begründung und Tor siehe oben. */
export const SCHOOL_TYPES: SchoolType[] = Object.keys(SELECTABLE);

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
