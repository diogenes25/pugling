import type { components } from "./contract";

/**
 * Die Typen, die **nicht** aus dem Vertragsdokument kommen können – jeder mit dem Grund, warum er hier steht.
 * Alles andere lebt als Alias auf `contract.ts` in [types.ts](types.ts); wer hier etwas ergänzt, muss den
 * Grund mitliefern, sonst gehört es dorthin.
 *
 * Es sind **elf**, und die Zahl ist eine Zusicherung: was strukturgleich in `contract.ts` steht, gehört
 * dorthin. `SchoolType` lag hier einmal mit der Begründung „`[Flags]`-Enum" – falsch, das Schema listet die
 * Einzelnamen und ist zeichengleich mit der Handliste gewesen. Nicht ausdrückbar ist nur die **Kombination**
 * („Realschule, Gymnasium"), und die reist als freier String (B-60).
 *
 * Sie zerfallen in drei Sorten:
 * 1. **Der Vertrag sagt nur `string`** – die Oberfläche engt ein (`Role`, `GoalStatus`).
 * 2. **Das Schema kann die Form nicht ausdrücken** – Generik (`Paged`), absichtlich kollabierte Generik
 *    (`CreateExercisePayload`).
 * 3. **Es ist gar kein Schema** – Query-Parameter reisen einzeln, nicht als Rumpf (`…SearchParams`,
 *    die Sortierschlüssel).
 */

/**
 * Die primäre Ebene fürs Routing (aus `LoginResponse.role`). `Creator` ist das **Lehrer-Konto**: ein
 * Erwachsener, der Inhalte erstellt und kein Kind betreut – sein Token trägt keinen Supervisor-Claim.
 *
 * **Von Hand, weil** `LoginResponse.Role` im Vertrag ein `string` ist (`Roles.Supervisor` sind Konstanten,
 * kein Enum). Generiert wäre der Typ `string` und jede Fallunterscheidung im Routing unbewacht.
 */
export type Role = "Supervisor" | "Creator" | "Student";

/**
 * Wie eine Etappe gerade steht; live aus dem Lernstand berechnet, nie gespeichert.
 *
 * **Von Hand, weil** der Server das Feld als `string` führt (kein Enum) – dieselbe Lage wie bei `Role`.
 */
export type GoalStatus = "open" | "achieved" | "overdue";

/**
 * Server-paginierte Liste: eine Seite plus Gesamtzahl (kommt aus dem `X-Total-Count`-Header).
 *
 * **Von Hand, weil** es kein Schema dazu gibt: die Gesamtzahl reist im **Header**, der Rumpf ist ein
 * nacktes Array. `httpPaged` in [api.ts](api.ts) setzt beides zusammen.
 */
export interface Paged<T> {
  items: T[];
  total: number;
}

/**
 * Sortierrichtung und die erlaubten Sortierschlüssel (Server-Whitelists in `ExerciseCatalogController`
 * bzw. `VocabularyStoreController`).
 *
 * **Von Hand, weil** sie als Query-Zeichenkette reisen; im Dokument stehen sie als `string`-Parameter ohne
 * Werteliste. Die Whitelist hier ist die einzige Stelle, die einen Tippfehler auffallen lässt.
 */
export type SortDir = "asc" | "desc";
/** Erlaubte Sortierschlüssel des Übungskatalogs – siehe [SortDir](#). */
export type ExerciseSortKey = "title" | "type" | "grade" | "source" | "created";
/** Erlaubte Sortierschlüssel des Vokabel-Stores – siehe [SortDir](#). */
export type VocabSortKey = "key" | "word" | "translation" | "pos" | "created";

/**
 * Suchparameter des Übungskatalogs.
 *
 * **Von Hand, weil** Query-Parameter im OpenAPI-Dokument je Operation einzeln stehen und **kein** Schema
 * bilden. Das Bündel ist eine Bequemlichkeit des Clients, kein Vertragstyp.
 */
export interface ExerciseSearchParams {
  subjectId?: number;
  seriesUnitId?: number;
  grade?: number;
  schoolType?: components["schemas"]["SchoolTypes"];
  categoryId?: number;
  type?: string;
  search?: string;
  /** Nur eigene Übungen des Vaters (Verwaltung statt Entdeckung der geteilten Bibliothek). */
  mineOnly?: boolean;
  /** Sortierschlüssel + Richtung (Server-Whitelist); Paginierung per skip/take. */
  sort?: ExerciseSortKey;
  dir?: SortDir;
  skip?: number;
  take?: number;
}

/** Suchparameter des Vokabel-Stores – von Hand aus demselben Grund wie `ExerciseSearchParams`. */
export interface VocabularySearchParams {
  search?: string;
  sourceLanguage?: string;
  targetLanguage?: string;
  partOfSpeech?: components["schemas"]["PartOfSpeech"];
  tags?: string[];
  matchAll?: boolean;
  sort?: VocabSortKey;
  dir?: SortDir;
  skip?: number;
  take?: number;
}

/**
 * Übungstypen, für die diese UI einen Editor hat. Deckungsgleich mit den Server-Typen
 * (`ExerciseTypeKeys`) – das **Routen-Segment und der Anzeigename kommen aber aus dem Typ-Manifest**
 * (`GET creator/exercise-types`), nicht aus einer Tabelle hier: sie driften sonst auseinander, und der
 * Server weicht durchaus ab (Aufsatz liegt unter `essays`, nicht `essay`).
 *
 * **Von Hand, weil** der Typ-Schlüssel im Vertrag ein `string` ist – die Typen sind ein Plugin-Register,
 * kein Enum. Diese Liste sagt „dafür gibt es ein Formular", nicht „das kennt der Server".
 */
export type ExerciseTypeKey =
  | "Vocabulary" | "Arithmetic" | "ArithmeticDrill" | "Cloze" | "Matching" | "List" | "Birkenbihl"
  | "Reading" | "Listening" | "Essay" | "Grammar" | "Translation";

/**
 * Gemeinsame Nutzlast zum Anlegen einer Übung (spiegelt `ExercisePayload<TConfig>` im Backend).
 * `config` ist typ-spezifisch – der Server interpretiert es je Routen-Segment.
 *
 * **Von Hand, weil** das Backend zwölf generische Ausprägungen erzeugt
 * (`ExercisePayloadOfVocabularyConfig` …) und die Oberfläche sie **absichtlich** zu einer kollabiert: der
 * Typ steht erst zur Laufzeit fest (Routen-Segment aus dem Manifest), die Formulare je Typ liegen in
 * `exerciseConfig.tsx`. Zwölf Aliase würden hier nichts bewachen, was `config: unknown` nicht schon sagt.
 */
export interface CreateExercisePayload {
  title: string;
  description?: string | null;
  orderIndex: number;
  rewardPoints: number;
  config: unknown;
  gradeMin?: number | null;
  gradeMax?: number | null;
  schoolTypes?: string;
  source?: string | null;
  categoryId?: number | null;
  defaultUseLeitner?: boolean;
  defaultRequireTypedTest?: boolean;
  /** Standard-Abfrageform (TestStage-Wert) – v. a. für Vokabeln (z. B. Multiple-Choice = 6). */
  defaultStage?: number | null;
  /** Empfohlene Anzahl genutzter Inhalte je Position; null = alle. */
  defaultItemCount?: number | null;
  /**
   * Bonus-Vorschlag. Beim **Ändern** unbedingt mitschicken: der Server ersetzt die Übung vollständig,
   * ein Weglassen würde den Vorschlag löschen.
   */
  suggestedBonus?: components["schemas"]["SuggestedBonus"] | null;
  /**
   * Für alle Väter zuweisbar? Beim **Ändern** den vorhandenen Wert mitschicken – der Server-Default ist
   * `true`, und ein Umschalten verlangt Owner-Rechte (sonst 403 für einen Write-Grantee).
   */
  executePublic?: boolean;
}

/**
 * Die Antwort auf Anlegen/Ändern einer Übung – das Gegenstück zu `CreateExercisePayload` und aus **demselben**
 * Grund von Hand: der Server erzeugt zwölf generische Ausprägungen (`ExerciseResponseOfVocabularyConfig` …),
 * die Route steht erst zur Laufzeit fest.
 *
 * Vorher war hier `ExerciseSummary` getippt – ein anderes Schema, dem `subjectId` fehlt und das `config` nicht
 * kennt. Gelesen wird heute nur `.id`, aber eine Lüge im Typ ist gerade in dieser Datei die falsche Stelle.
 */
export interface ExerciseWriteResult {
  id: number;
  seriesUnitId: number;
  type: string;
  title: string;
  config: unknown;
}
