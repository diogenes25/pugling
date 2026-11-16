import { useEffect, useId, useRef, useState } from "react";
import { AddPanel } from "../components/AddPanel";
import { ListenAusfall } from "../components/ListenAusfall";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { useAction, type ActionState } from "../lib/useAction";
import { confirmAction } from "../lib/ui";
import { useAsync } from "../lib/useAsync";
import { useExerciseTypes } from "../lib/exerciseTypes";
import { useGemerkterFehler } from "../lib/useGemerkterFehler";
import { TruncationHint } from "../components/ListControls";
import { MasteryPill } from "../components/MasteryPill";
import { FieldLabel, InfoHint } from "../components/InfoHint";
import { auswahlNachFilterwechsel, auswahlNachUmsortieren, NICHT_ZUWEISBAR_GRUND } from "./wizardSearch";
import type {
  CreatePositionDto, ExercisePreviewStage, LearningCheck, ExerciseSummary, GoalCadence, ItemScope, PositionReport, PositionResponse, Paged,
  PracticeOrder, ScoringTimeSlot, SubjectResponse, UpdatePositionDto,
} from "../lib/types";
import { ExerciseFilterBar, type ExerciseFilter } from "./ExerciseFilterBar";
import { ratingSummary } from "./ExerciseRatingPanel";
import { ExercisePreviewModal } from "./ExercisePreviewModal";
import { formatDate } from "../lib/datum";
import { leiterText } from "./wizardFinish";

/*
 * Positions-UI des neuen Lehrplan-Modells: Ein Plan ist ein Container aus Katalog-Übungen. Jede
 * Position verweist auf eine globale Übung (der Inhalt bleibt dort) und trägt ihre EIGENEN Ziele
 * (Rhythmus + Schwelle), Punkte und Leitner-Einstellungen. Hier stellt der Vater den Plan „zusammen".
 */

const CADENCE_LABEL: Record<GoalCadence, string> = {
  None: "frei (kein Ziel, keine Punkte)", Daily: "Tagesziel", Weekly: "Wochenziel",
};
const CADENCES: GoalCadence[] = ["Daily", "Weekly", "None"];
const ORDER_LABEL: Record<PracticeOrder, string> = {
  WeakestFirst: "Schwächste zuerst", Serial: "Reihenfolge", Random: "Zufällig", NewestWeighted: "Neue bevorzugt",
};
/**
 * Die Frage vor „Entfernen". Der Server nimmt nur eine Position ohne Übungs- und Testdaten heraus (sonst
 * `position_has_data`, dessen Text das sagt) – Übungs- oder Testdaten gehen also nie verloren.
 */
export const REMOVE_POSITION_QUESTION = "Diese Position wirklich aus dem Plan nehmen? Die Übung bleibt im Katalog.";

const ORDERS: PracticeOrder[] = ["WeakestFirst", "Serial", "Random", "NewestWeighted"];

// B-16: welche Inhalte der Übung die Position spielt. „Alle" ist die Vorgabe des Servers und selbst wählbar.
const SCOPE_LABEL: Record<ItemScope, string> = {
  All: "alle Inhalte", New: "nur neue", Old: "nur schon eingeführte",
};
const SCOPES: ItemScope[] = ["All", "New", "Old"];
/** Die erste Option des Stufen-Felds: `null` an der Position, der Standard der Übung gilt (B-16). */
export const STAGE_DEFAULT_LABEL = "Standard der Übung";

// Anzeigename der Typen aus dem Server-Manifest – eine dritte Kopie der Tabelle lief zwangsläufig
// aus dem Takt (das UI kannte sechs Typen, der Server führt zwölf).

export function PlanPositions({ planId }: { planId: number }) {
  const positions = useAsync<PositionResponse[]>(() => api.positions(planId), [planId]);
  // **Eine** Aktion für die Zeilen: es lässt sich ohnehin nur eine Position auf einmal bearbeiten, und so steht
  // ihre Rückmeldung an einer Stelle statt in jeder Zeile. Das Hinzufügen hat seine eigene (siehe `AddPosition`).
  const action = useAction();

  return (
    <section>
      <h3 className="h-section">Übungen im Plan {positions.data ? `(${positions.data.length})` : ""}</h3>
      <p className="muted" style={{ marginTop: 0 }}>
        Jede Position verweist auf eine Katalog-Übung und trägt eigene Ziele, Punkte und Leitner-Einstellungen.
      </p>
      <StatusBanner message={action.message} />

      {/* B-183: ein gescheitertes Lesen bietet „Erneut laden". Der Platzhalter nur vor dem ersten Ergebnis
          (`showPlaceholder`), sonst tauschte die Wiederholung den Ausfall gegen „Lade…" (B-216). */}
      <ListenAusfall was="Die Positionen des Plans" fehler={positions.error} laedt={positions.loading}
        onRetry={positions.reload} schluessel={planId} />
      {positions.showPlaceholder ? <div className="loading">Lade Positionen…</div> : positions.data === null ? null : (
        <div style={{ overflowX: "auto", marginTop: 12 }}>
          <table className="table">
            <thead><tr><th>#</th><th>Übung</th><th>Ziel</th><th className="num">Punkte</th><th>Leitner</th><th>Aktionen</th></tr></thead>
            <tbody>
              {positions.data?.map((p) => (
                <PositionRow key={p.id} planId={planId} pos={p} onChanged={positions.reload} action={action} />
              ))}
              {positions.data?.length === 0 && (
                <tr><td colSpan={6} className="muted">Noch keine Übungen im Plan – füge unten eine aus dem Katalog hinzu.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* Unter dem Bestand und zugeklappt (B-534): das Anlege-Formular ist so groß wie die Tabelle und stand
          vorher davor. Ein leerer Plan öffnet es gleich – dort ist es der einzige nächste Schritt. Erst mit
          geladenem Bestand rendern, `defaultOpen` gilt nur beim Einhängen. */}
      {/* Bewusst kein Panel ohne Bestand: scheitert das Laden, bietet der ListenAusfall oben „Erneut laden" – ein Hinzufügen in einen Plan, dessen Inhalt unbekannt ist, wäre blind. */}
      {positions.data && (
        <AddPanel label="Übung aus dem Katalog" defaultOpen={positions.data.length === 0}>
          <AddPosition planId={planId} onAdded={positions.reload} />
        </AddPanel>
      )}
    </section>
  );
}

/*
 * Die Einstellungen einer Position an EINER Stelle – Anlegen und Bearbeiten müssen dieselben Felder mit
 * denselben Beschriftungen zeigen, sonst lernt der Vater die Bedeutung zweimal (und einmal falsch).
 *
 * `goalThreshold`/`itemCount` sind Strings, weil "" hier eine eigene Bedeutung hat: "Standard des
 * Verfahrens" bzw. „alle Inhalte" – eine 0 wäre eine Aussage, ein leeres Feld ist keine.
 */
export interface PositionSettings {
  cadence: GoalCadence;
  goalThreshold: string;
  itemCount: string;
  /** Tagesdosis neuer Inhalte (B-04); "" = keine Dosis – der Server kennt dafür die 0. */
  newItemsPerDay: string;
  orderStrategy: PracticeOrder;
  pointsGoalMet: number;
  penaltyCoins: number;
  /*
   * Bonus-Werte als Strings, weil "" hier „erben" heißt: die Position übernimmt dann den Bonus-Vorschlag
   * ihrer Übung (der Server setzt `?? sb?.… ?? Default` nur ein, wenn das Feld `null` ist). Eine Zahl zu
   * senden ist eine Aussage – und würde einen bewusst getunten Übungs-Vorschlag überschreiben.
   */
  newContentPoints: string;
  comboThreshold: string;
  comboBonusPoints: string;
  useLeitner: boolean;
  /** B-219: ein Ausrutscher senkt nur ein Fach, erst der zweite in Folge setzt auf Fach 1. Nur mit Leitner wirksam. */
  forgivingDemotion: boolean;
  requireTypedTest: boolean;
  /*
   * Die Abfrageform des Tests (B-16) als String: "" = „Standard der Übung" (`null`), sonst ein Wert aus den
   * Manifest-Stufen des Typs. Eine Zahl, die nicht in der Liste steht, ist im Formular gar nicht wählbar.
   */
  stage: string;
  /*
   * Die gespeicherte Stufe – nur, um beim Bearbeiten „zurück auf Standard" als `clearStage` zu senden: `null` heißt im
   * PATCH „nicht angegeben", die alte Stufe bliebe sonst stehen. `null` = keine gespeichert (oder Anlegen).
   */
  stageStored: number | null;
  /*
   * Dasselbe für „Inhalte" und „Bestehen ab %" (B-547): ein geleertes Feld heißt „alle" bzw. „Standard", im PATCH aber
   * „nicht angegeben". Ohne den gespeicherten Wert wüsste das Formular nicht, ob es `clearItemCount`/`clearGoalThreshold`
   * senden muss.
   */
  itemCountStored: number | null;
  goalThresholdStored: number | null;
  scope: ItemScope;
  /*
   * Tempo-Bonus wie die Combo (B-16, Entscheidung 8): "" heißt beim Anlegen „erbt den Vorschlag der Übung", beim
   * Bearbeiten „bleibt" (PATCH-`null`); 0 schaltet aus.
   */
  speedThresholdSeconds: string;
  speedBonusPoints: string;
  /*
   * Das Zeitfenster als drei Strings; alle drei leer heißt „kein eigenes Fenster" – dann gelten nur die
   * globalen Fenster des Servers. Das Formular stellt genau EIN Fenster ein, die Ablage ist eine Liste.
   */
  timeSlotStart: string;
  timeSlotEnd: string;
  timeSlotMultiplier: string;
  /*
   * Der Name des Fensters wird nicht abgefragt, aber mitgeführt: ein per API (KI-Creator) gesetztes
   * „Hausaufgaben" wäre nach einem beliebigen Positions-Edit sonst durch unseren Vorgabenamen ersetzt.
   */
  timeSlotName: string;
  /*
   * Die gespeicherte Liste im Wortlaut – aus zwei Gründen, die beide lautlos zuschlugen, weil das Formular
   * `timeSlots` bei JEDEM Speichern mitschickt (anders als `boxIntervalDays` & Co., die es weglässt):
   * 1. Der Server nimmt bis zu 24 Fenster, und API/KI-Creator setzen mehrere. Ohne die Liste ersetzte auch
   *    eine reine Punkte-Änderung sie durch das eine bearbeitete Fenster – Fenster 2..n waren weg.
   * 2. `<input type="time">` kennt keine Sekunden: ein gespeichertes „23:59:59.9999999" käme als „23:59"
   *    zurück und schrumpfte das Fenster beim Speichern um bis zu eine Minute.
   * `null` = die Position hat noch keine (Anlegen).
   */
  timeSlotStored: ScoringTimeSlot[] | null;
}

/** Vorgabename, wenn das Fenster hier im Formular entsteht – der Server nutzt ihn nur zur Lesbarkeit. */
const TIME_SLOT_NAME = "Zeitfenster der Pflicht";

/** Obergrenze des Faktors – dieselbe Zahl bewacht der Server (`MaxMultiplier`). */
const MAX_MULTIPLIER = 10;

/** „13:00:00" → „13:00" – das Eingabefeld für Uhrzeiten arbeitet ohne Sekunden. */
const hhmm = (t: string) => t.slice(0, 5);

/** Trägt die Position ein eigenes Zeitfenster? (Alle drei Felder gefüllt.) */
const hasTimeSlot = (s: PositionSettings) =>
  s.timeSlotStart !== "" && s.timeSlotEnd !== "" && s.timeSlotMultiplier.trim() !== "";

/** Die gespeicherten Fenster jenseits des ersten: hier nicht bearbeitbar, aber unangetastet mitgeschrieben. */
const restSlots = (s: PositionSettings): ScoringTimeSlot[] => s.timeSlotStored?.slice(1) ?? [];

/*
 * „13:00" bleibt „13:00:00", solange die Anzeige den gespeicherten Wert nur gekürzt hat – nur ein wirklich
 * geänderter Wert überschreibt ihn. Sonst schnitte jedes Speichern die Sekunden ab (siehe `timeSlotStored`).
 */
const keptTime = (stored: string | undefined, shown: string) =>
  stored !== undefined && hhmm(stored) === shown ? stored : shown;

/*
 * Das Fenster in Vertragsform: das bearbeitete voran, dahinter die unberührten. `null` heißt „keins" und
 * leert die Liste beim Speichern – aber nur, wenn auch kein unberührtes mehr übrig ist.
 */
function timeSlotOf(s: PositionSettings): ScoringTimeSlot[] | null {
  const rest = restSlots(s);
  if (!hasTimeSlot(s)) return rest.length > 0 ? rest : null;
  const stored = s.timeSlotStored?.[0];
  return [{
    name: s.timeSlotName || TIME_SLOT_NAME,
    start: keptTime(stored?.start, s.timeSlotStart),
    end: keptTime(stored?.end, s.timeSlotEnd),
    multiplier: Number(s.timeSlotMultiplier),
  }, ...rest];
}

/*
 * Warum das hier und nicht erst am Server scheitert: Halb gefüllt ist kein Fenster, sondern ein Versehen –
 * und „13 bis 13 Uhr" wäre ein Fenster, das nie zutrifft. Beides sähe nach „gespeichert" aus und würde
 * nichts tun. Der Server lehnt dieselben Fälle ab (validation_error); das hier ist die freundliche Fassung.
 */
export function timeSlotProblem(s: PositionSettings): string | null {
  const filled = [s.timeSlotStart, s.timeSlotEnd, s.timeSlotMultiplier.trim()].filter((v) => v !== "").length;
  if (filled === 0) return null;
  if (filled < 3) return "Zeitfenster: bitte von, bis und Faktor ausfüllen – oder alle drei leer lassen.";
  if (s.timeSlotStart >= s.timeSlotEnd) return "Zeitfenster: „von“ muss vor „bis“ liegen.";
  if (!(Number(s.timeSlotMultiplier) > 0)) return "Zeitfenster: der Faktor muss größer als 0 sein.";
  // Dieselbe Grenze wie am Server – ohne sie käme hier die englische Server-Meldung an.
  if (Number(s.timeSlotMultiplier) > MAX_MULTIPLIER) return `Zeitfenster: der Faktor darf höchstens ${MAX_MULTIPLIER} sein.`;
  return null;
}

/**
 * Womit das Punkte-Feld vorbelegt wird, wenn der Vater eine Übung anklickt (B-45).
 *
 * Gelesen wird `suggestedGoalPoints` – was eine Position **tatsächlich** erben würde. Der rohe
 * `rewardPoints` steht daneben und wird hier bewusst nicht angefasst: Die Grenze `(0, 200]` und der
 * Rückfall auf 20 sind eine Server-Regel (`GoalPointsPolicy`), und sie hier nachzurechnen hieße, dieselbe
 * Regel zweimal zu führen (B-192). Vorher las das Feld den rohen Wert – der Vater sah 100000 im Feld und
 * bekam beim Anlegen 20.
 *
 * `getippt` ist der Zustand, der vorher fehlte (B-191): Ohne ihn stand `current` für zweierlei zugleich –
 * „der Vater hat das getippt" und „die vorher angeklickte Übung hat das empfohlen" – und die zweite
 * Bedeutung trug den Wert von Übung A in Übung B, wo ihn niemand gesetzt hatte. Eine selbst getippte Zahl
 * bleibt jetzt **immer** stehen, nicht mehr nur bei einer schweigenden Übung.
 */
export function pointsFrom(
  ex: ExerciseSummary | undefined, current: number, getippt: boolean,
): number {
  if (getippt || !ex) return current;
  return ex.suggestedGoalPoints;
}

/**
 * Startwerte einer neuen Position.
 *
 * **Ohne Übung**, und das ist keine Nachlässigkeit: beim Aufbau des Formulars ist noch keine gewählt. Was
 * die Übung beiträgt, trägt der `onChange` der Trefferliste nach – vorher stand hier ein `ex`-Parameter,
 * den nie ein Aufrufer belegte, und ein Kommentar, der das Gegenteil behauptete.
 */
function defaultSettings(): PositionSettings {
  return {
    cadence: "Daily", goalThreshold: "", itemCount: "", newItemsPerDay: "",
    orderStrategy: "WeakestFirst", pointsGoalMet: 20, penaltyCoins: 0,
    // Leer = Vorschlag der Übung übernehmen (siehe PositionSettings).
    newContentPoints: "", comboThreshold: "", comboBonusPoints: "",
    useLeitner: false, forgivingDemotion: false, requireTypedTest: false,
    stage: "", stageStored: null, itemCountStored: null, goalThresholdStored: null, scope: "All", speedThresholdSeconds: "", speedBonusPoints: "",
    // Kein Fenster: eine Tageszeit ist eine Aussage über den Familienalltag, kein Vorschlag der Übung.
    timeSlotStart: "", timeSlotEnd: "", timeSlotMultiplier: "", timeSlotName: "", timeSlotStored: null,
  };
}

/** Der gespeicherte Stand einer Position als Formular-Zustand. */
export function settingsFrom(pos: PositionResponse): PositionSettings {
  return {
    cadence: pos.cadence,
    goalThreshold: pos.goalThreshold?.toString() ?? "",
    itemCount: pos.itemCount?.toString() ?? "",
    newItemsPerDay: pos.newItemsPerDay > 0 ? pos.newItemsPerDay.toString() : "",
    orderStrategy: pos.orderStrategy,
    pointsGoalMet: pos.pointsGoalMet,
    penaltyCoins: pos.penaltyCoins,
    // Am gespeicherten Stand ist die Erbschaft längst aufgelöst – hier stehen konkrete Zahlen.
    newContentPoints: pos.newContentPoints.toString(),
    comboThreshold: pos.comboThreshold.toString(),
    comboBonusPoints: pos.comboBonusPoints.toString(),
    useLeitner: pos.useLeitner,
    forgivingDemotion: pos.forgivingDemotion ?? false,
    requireTypedTest: pos.requireTypedTest,
    stage: pos.stage?.toString() ?? "",
    stageStored: pos.stage ?? null,
    itemCountStored: pos.itemCount ?? null,
    goalThresholdStored: pos.goalThreshold ?? null,
    scope: pos.scope,
    speedThresholdSeconds: pos.speedThresholdSeconds.toString(),
    speedBonusPoints: pos.speedBonusPoints.toString(),
    // Bearbeitet wird nur das erste Fenster; die ganze Liste reist mit, damit das Speichern die übrigen
    // nicht verwirft und die Sekunden nicht abschneidet (siehe `timeSlotStored`).
    timeSlotStart: hhmm(pos.timeSlots?.[0]?.start ?? ""),
    timeSlotEnd: hhmm(pos.timeSlots?.[0]?.end ?? ""),
    timeSlotMultiplier: pos.timeSlots?.[0]?.multiplier?.toString() ?? "",
    timeSlotName: pos.timeSlots?.[0]?.name ?? "",
    timeSlotStored: pos.timeSlots ?? null,
  };
}

/*
 * Dosis und Leitner-Kasten gehören zusammen (B-04): der Server lehnt eine Dosis ohne Kasten ab. Darum zieht eine
 * eingetragene Dosis das Häkchen mit, und ein abgewähltes Häkchen leert die Dosis – statt einen Stand zu bauen,
 * den erst das Speichern als Fehler meldet.
 */
export const withDose = (s: PositionSettings, dose: string): PositionSettings =>
  ({ ...s, newItemsPerDay: dose, useLeitner: s.useLeitner || Number(dose) > 0 });
export const withLeitner = (s: PositionSettings, useLeitner: boolean): PositionSettings =>
  ({ ...s, useLeitner, newItemsPerDay: useLeitner ? s.newItemsPerDay : "" });

/** Leeres Feld = `null` = „Standard bzw. Vorschlag der Übung übernehmen". */
const numOrNull = (v: string): number | null => (v.trim() === "" ? null : Number(v));

/*
 * Formular-Zustand in die Vertragsform (leere Felder werden zu `null` = Standard).
 *
 * Die Rückgabe ist ANNOTIERT, und das ist der Punkt: TypeScript prüft überzählige Eigenschaften nicht über
 * einen Spread. Ohne die Annotation fiele ein Tippfehler im Feldnamen erst zur Laufzeit als
 * `400 unknown_field` auf – hier fällt er im Typecheck.
 */
export function settingsToDto(s: PositionSettings): Omit<CreatePositionDto, "exerciseId"> {
  return {
    cadence: s.cadence,
    goalThreshold: s.goalThreshold.trim() === "" ? null : Number(s.goalThreshold),
    itemCount: s.itemCount.trim() === "" ? null : Number(s.itemCount),
    // Immer eine Zahl: im PATCH hieße `null` „nicht angegeben", und ein geleertes Feld ließe die alte Dosis stehen.
    newItemsPerDay: s.newItemsPerDay.trim() === "" ? 0 : Number(s.newItemsPerDay),
    orderStrategy: s.orderStrategy,
    pointsGoalMet: s.pointsGoalMet,
    penaltyCoins: s.penaltyCoins,
    newContentPoints: numOrNull(s.newContentPoints),
    comboThreshold: numOrNull(s.comboThreshold),
    comboBonusPoints: numOrNull(s.comboBonusPoints),
    useLeitner: s.useLeitner,
    forgivingDemotion: s.forgivingDemotion,
    requireTypedTest: s.requireTypedTest,
    stage: numOrNull(s.stage),
    scope: s.scope,
    speedThresholdSeconds: numOrNull(s.speedThresholdSeconds),
    speedBonusPoints: numOrNull(s.speedBonusPoints),
    timeSlots: timeSlotOf(s),
  };
}

/*
 * Beim Ändern braucht das geleerte Fenster einen ausdrücklichen Schalter: `null` heißt im Vertrag „nicht
 * angegeben" (der alte Wert bliebe stehen), und dann meldete das Formular „Gespeichert." während weiter
 * verdoppelt wird. Nur der PATCH kennt den Schalter – `CreatePositionDto` hat ihn nicht, und ein unbekanntes
 * Feld lehnt der Server mit `unknown_field` ab.
 */
export function settingsToUpdateDto(s: PositionSettings): UpdatePositionDto {
  return {
    ...settingsToDto(s),
    clearTimeSlots: timeSlotOf(s) === null,
    // B-16: dieselbe Lage bei der Stufe – „Standard der Übung" ist `null`, und das ließe die gespeicherte stehen.
    clearStage: s.stage === "" && s.stageStored !== null,
    // B-547: geleerte Felder kommen nur über den Schalter auf „alle Inhalte" bzw. „Standard" zurück.
    clearItemCount: s.itemCount.trim() === "" && s.itemCountStored !== null,
    clearGoalThreshold: s.goalThreshold.trim() === "" && s.goalThresholdStored !== null,
  };
}

/**
 * Die Felder selbst. Präsentational: den Zustand hält der Aufrufer (Anlegen bzw. Zeile im Edit-Modus).
 *
 * `stages` sind die Manifest-Stufen des Übungstyps (B-16) – leer heißt: der Typ kennt keine Stufenwahl (oder das Manifest
 * lädt noch), und dann gibt es kein Stufen-Feld. Keine Liste im Frontend (`VOCAB_FORMS` gehört dem Creator-Formular).
 */
function PositionFields({ value, onChange, stages }: {
  value: PositionSettings; onChange: (next: PositionSettings) => void; stages: readonly ExercisePreviewStage[];
}) {
  const uid = useId();
  const up = <K extends keyof PositionSettings>(k: K, v: PositionSettings[K]) => onChange({ ...value, [k]: v });
  /*
   * Der Zustand des Aufklapp-Blocks liegt hier und nicht am Wert: `open={hasTimeSlot(value)}` würde den Block
   * beim ersten Tippen wieder zuklappen, weil das erste gefüllte Feld noch kein vollständiges Fenster ist.
   * Offen startet er nur, wenn schon eines gespeichert ist.
   */
  const [slotOpen, setSlotOpen] = useState(() => hasTimeSlot(value));

  return (
    <div className="row" style={{ gap: 12, alignItems: "flex-end", flexWrap: "wrap" }}>
      <div className="field" style={{ maxWidth: 180 }}>
        <FieldLabel htmlFor={`${uid}-cadence`} topic="cadence">Ziel-Rhythmus</FieldLabel>
        <select id={`${uid}-cadence`} aria-label="Ziel-Rhythmus" value={value.cadence}
          onChange={(e) => up("cadence", e.target.value as GoalCadence)}>
          {CADENCES.map((c) => <option key={c} value={c}>{CADENCE_LABEL[c]}</option>)}
        </select>
      </div>
      {/* Der Server wertet die Schwelle als Bestehens-Prozentsatz des Abschlusstests aus (Standard 80). */}
      <div className="field" style={{ maxWidth: 140 }}>
        <FieldLabel htmlFor={`${uid}-pass`} topic="goalThreshold">Bestehen ab %</FieldLabel>
        <input id={`${uid}-pass`} aria-label="Bestehen ab Prozent" type="number" min={1} max={100}
          placeholder="80" value={value.goalThreshold} onChange={(e) => up("goalThreshold", e.target.value)} />
      </div>
      <div className="field" style={{ maxWidth: 130 }}>
        <FieldLabel htmlFor={`${uid}-count`} topic="itemCount">Inhalte</FieldLabel>
        <input id={`${uid}-count`} aria-label="Anzahl Inhalte" type="number" min={1} placeholder="alle"
          value={value.itemCount} onChange={(e) => up("itemCount", e.target.value)} />
      </div>
      <div className="field" style={{ maxWidth: 150 }}>
        <FieldLabel htmlFor={`${uid}-dose`} topic="newItemsPerDay">Neue Inhalte je Tag</FieldLabel>
        <input id={`${uid}-dose`} type="number" min={0} max={200} placeholder="alle sofort"
          value={value.newItemsPerDay}
          onChange={(e) => onChange(withDose(value, e.target.value))} />
      </div>
      <div className="field" style={{ maxWidth: 180 }}>
        <FieldLabel htmlFor={`${uid}-order`} topic="orderStrategy">Reihenfolge</FieldLabel>
        <select id={`${uid}-order`} aria-label="Reihenfolge" value={value.orderStrategy}
          onChange={(e) => up("orderStrategy", e.target.value as PracticeOrder)}>
          {ORDERS.map((o) => <option key={o} value={o}>{ORDER_LABEL[o]}</option>)}
        </select>
      </div>
      <div className="field" style={{ maxWidth: 180 }}>
        <FieldLabel htmlFor={`${uid}-scope`} topic="scope">Inhalte-Auswahl</FieldLabel>
        <select id={`${uid}-scope`} aria-label="Inhalte-Auswahl" value={value.scope}
          onChange={(e) => up("scope", e.target.value as ItemScope)}>
          {SCOPES.map((o) => <option key={o} value={o}>{SCOPE_LABEL[o]}</option>)}
        </select>
      </div>
      {stages.length > 0 && (
        <div className="field" style={{ maxWidth: 220 }}>
          <FieldLabel htmlFor={`${uid}-stage`} topic="stage">Stufe</FieldLabel>
          <select id={`${uid}-stage`} aria-label="Stufe" value={value.stage}
            onChange={(e) => up("stage", e.target.value)}>
            <option value="">{STAGE_DEFAULT_LABEL}</option>
            {stages.map((o) => <option key={o.value} value={String(o.value)}>{o.label}</option>)}
          </select>
        </div>
      )}
      {/* B-538: eine Position ohne Pflicht zahlt keine Münzen – Ziel-Punkte, Malus und Boni wären hier wirkungslos. */}
      {value.cadence === "None" ? (
        <p className="sub" style={{ margin: 0, alignSelf: "center" }}>Ohne Pflicht bringt diese Position keine Punkte.</p>
      ) : (
        <>
        <div className="field" style={{ maxWidth: 140 }}>
          <FieldLabel htmlFor={`${uid}-points`} topic="pointsGoalMet">Punkte (Ziel erreicht)</FieldLabel>
          <input id={`${uid}-points`} aria-label="Punkte bei erreichtem Ziel" type="number" min={0}
            value={value.pointsGoalMet} onChange={(e) => up("pointsGoalMet", Number(e.target.value))} />
        </div>
        {/* Der „Stick": verpasste Pflicht kostet Münzen. 0 = reine Belohnung; Schulden sind erlaubt. */}
        <div className="field" style={{ maxWidth: 150 }}>
          <FieldLabel htmlFor={`${uid}-penalty`} topic="penaltyCoins">Münz-Malus (versäumt)</FieldLabel>
          <input id={`${uid}-penalty`} aria-label="Münz-Malus bei gerissener Pflicht" type="number" min={0}
            value={value.penaltyCoins} onChange={(e) => up("penaltyCoins", Number(e.target.value))} />
        </div>
        {/* Leer lassen = Bonus-Vorschlag der Übung übernehmen (Platzhalter „erbt"). */}
        <div className="field" style={{ maxWidth: 130 }}>
          <FieldLabel htmlFor={`${uid}-new`} topic="newContentPoints">Punkte neuer Inhalt</FieldLabel>
          <input id={`${uid}-new`} aria-label="Punkte für neuen Inhalt" type="number" min={0} placeholder="erbt"
            value={value.newContentPoints} onChange={(e) => up("newContentPoints", e.target.value)} />
        </div>
        <div className="field" style={{ maxWidth: 150 }}>
          <FieldLabel htmlFor={`${uid}-combo-n`} topic="comboThreshold">Combo alle … Treffer</FieldLabel>
          <input id={`${uid}-combo-n`} aria-label="Combo-Schwelle" type="number" min={0} placeholder="erbt"
            value={value.comboThreshold} onChange={(e) => up("comboThreshold", e.target.value)} />
        </div>
        <div className="field" style={{ maxWidth: 140 }}>
          <FieldLabel htmlFor={`${uid}-combo-p`} topic="comboBonusPoints">Combo-Bonuspunkte</FieldLabel>
          <input id={`${uid}-combo-p`} aria-label="Combo-Bonuspunkte" type="number" min={0} placeholder="erbt"
            value={value.comboBonusPoints} onChange={(e) => up("comboBonusPoints", e.target.value)} />
        </div>
        <div className="field" style={{ maxWidth: 150 }}>
          <FieldLabel htmlFor={`${uid}-speed-s`} topic="speedThresholdSeconds">Tempo bis … Sek.</FieldLabel>
          <input id={`${uid}-speed-s`} aria-label="Tempo-Schwelle in Sekunden" type="number" min={0} placeholder="erbt"
            value={value.speedThresholdSeconds} onChange={(e) => up("speedThresholdSeconds", e.target.value)} />
        </div>
        <div className="field" style={{ maxWidth: 140 }}>
          <FieldLabel htmlFor={`${uid}-speed-p`} topic="speedBonusPoints">Tempo-Bonuspunkte</FieldLabel>
          <input id={`${uid}-speed-p`} aria-label="Tempo-Bonuspunkte" type="number" min={0} placeholder="erbt"
            value={value.speedBonusPoints} onChange={(e) => up("speedBonusPoints", e.target.value)} />
        </div>
        </>
      )}
      <span className="label-row">
        <label className="checkline">
          <input type="checkbox" checked={value.useLeitner}
            onChange={(e) => onChange(withLeitner(value, e.target.checked))} /> Leitner-Kasten
        </label>
        <InfoHint topic="useLeitner" />
      </span>
      {/* Nur mit Leitner sinnvoll (B-219): ausgegraut statt versteckt, damit der Zusammenhang sichtbar bleibt. */}
      <span className="label-row">
        <label className="checkline">
          <input type="checkbox" checked={value.forgivingDemotion} disabled={!value.useLeitner}
            onChange={(e) => up("forgivingDemotion", e.target.checked)} /> Ausrutscher verzeihen
        </label>
        <InfoHint topic="forgivingDemotion" />
      </span>
      <span className="label-row">
        <label className="checkline">
          <input type="checkbox" checked={value.requireTypedTest} onChange={(e) => up("requireTypedTest", e.target.checked)} /> nur getippte Tests
        </label>
        <InfoHint topic="requireTypedTest" />
      </span>
      {/* Eingeklappt, damit die Felder nicht ein zwölftes Mal in die Zeile drängen – die meisten Pflichten
          brauchen kein eigenes Fenster. */}
      <details open={slotOpen} onToggle={(e) => setSlotOpen((e.currentTarget as HTMLDetailsElement).open)}
        style={{ width: "100%" }}>
        {/* Die Zusammenfassung nennt auch den halb gefüllten Zustand: „keins" wäre dort eine Lüge, und die
            Fehlermeldung verlangte dann Felder, die zugeklappt niemand sieht. */}
        <summary style={{ cursor: "pointer" }}>
          Zeitfenster (Punkte-Faktor)
          <span className="muted">
            {" · "}
            {hasTimeSlot(value) ? `${value.timeSlotStart}–${value.timeSlotEnd} ×${value.timeSlotMultiplier}`
              : timeSlotProblem(value) ? "unvollständig" : "keins"}
            {/* Die weiteren Fenster gehören in die Zusammenfassung: „13:00–15:00 ×2" allein läse sich als
                „das ist alles", und der Vater stellte einen Faktor ein, den ein späteres Fenster überstimmt. */}
            {restSlots(value).length > 0 ? ` (+${restSlots(value).length} weitere)` : ""}
          </span>
        </summary>
        <div className="row" style={{ gap: 12, alignItems: "flex-end", flexWrap: "wrap", marginTop: 8 }}>
          <div className="field" style={{ maxWidth: 150 }}>
            <FieldLabel htmlFor={`${uid}-slot-from`} topic="positionTimeSlot">von</FieldLabel>
            <input id={`${uid}-slot-from`} aria-label="Zeitfenster von" type="time"
              value={value.timeSlotStart} onChange={(e) => up("timeSlotStart", e.target.value)} />
          </div>
          <div className="field" style={{ maxWidth: 150 }}>
            <label htmlFor={`${uid}-slot-to`}>bis</label>
            <input id={`${uid}-slot-to`} aria-label="Zeitfenster bis" type="time"
              value={value.timeSlotEnd} onChange={(e) => up("timeSlotEnd", e.target.value)} />
          </div>
          <div className="field" style={{ maxWidth: 130 }}>
            <label htmlFor={`${uid}-slot-factor`}>Faktor</label>
            {/* `step="any"` statt einer Schrittweite: mit `step={0.1}` wies der Browser im Anlegen-Formular
                (ein `<form>`) „1,25" ab, während dieselbe Zahl beim Bearbeiten durchging – ein Wert, der je
                Maske gültig oder ungültig ist. Die Regel steht in `timeSlotProblem` und am Server. */}
            <input id={`${uid}-slot-factor`} aria-label="Zeitfenster-Faktor" type="number" min={0} max={MAX_MULTIPLIER} step="any"
              placeholder="z. B. 2" value={value.timeSlotMultiplier}
              onChange={(e) => up("timeSlotMultiplier", e.target.value)} />
          </div>
          {restSlots(value).length > 0 && (
            <p className="muted" style={{ width: "100%", margin: 0 }}>
              Diese Position hat {restSlots(value).length} weitere Zeitfenster (per API gesetzt). Sie bleiben beim
              Speichern erhalten – hier bearbeitest du nur das erste.
            </p>
          )}
        </div>
      </details>
    </div>
  );
}

/** Katalog-Übung über eine Filterleiste finden und als Position hinzufügen (übrige Werte erbt die Position). */
function AddPosition({ planId, onAdded }: { planId: number; onAdded: () => void }) {
  /*
   * Eine eigene Aktion samt Banner am Absende-Knopf: die Rückmeldung landet dort, wo gehandelt wurde. Mit der
   * geteilten Aktion der Zeilen stand „hinzugefügt" bzw. ein 409 über der Tabelle – in einem langen Plan außer Sicht,
   * und vom Knopf aus sah das Hinzufügen aus wie „nichts passiert".
   */
  const action = useAction();
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);
  const types = useExerciseTypes();
  const typeLabel = types.label;
  const umfang = types.umfang;
  // B-424: die Übung, die der Vater gerade ausprobiert (Testmodus, nebenwirkungsfrei) – die Wahl bleibt davon unberührt.
  const [vorschau, setVorschau] = useState<{ id: number; title: string } | null>(null);
  const [filter, setFilter] = useState<ExerciseFilter>({});
  // B-270: gute Übungen dort finden, wo man sie auswählt. Nur zwei Reihenfolgen – die Katalog-Ordnung (Server-Vorgabe)
  // und „beste zuerst"; unbewertete stehen dabei hinten (Server-Regel aus B-12). Eine Richtung zum Umdrehen braucht es
  // hier nicht: wer die schlechtesten sucht, stellt keinen Plan zusammen.
  const [besteZuerst, setBesteZuerst] = useState(false);
  // Die Gesamtzahl wird mitgeführt: der Server liefert nur eine Seite, und eine still gekappte
  // Auswahlliste liest sich wie „mehr gibt es nicht".
  /*
   * Die Suchkriterien **einmal**, für zwei Verwendungen: als Abhängigkeiten der Abfrage und – gebündelt –
   * als Schlüssel, an dem hängt, ob die gezeigten Zeilen noch zum eingestellten Filter gehören. Zwei
   * handgepflegte Listen derselben acht Felder müssten übereinstimmen, und ein achtes Feld würde an
   * genau einer von beiden vergessen — dann ginge die Sperre nicht mehr auf oder gar nicht mehr an, ohne
   * dass es jemand sähe. Der Zwilling hat aus demselben Grund eine Quelle (`wizardSearchParams`).
   *
   * Normalisiert (`trim`, leer → `undefined`): Der Server trimmt die Freitextsuche selbst, ein Leerschlag
   * am Ende ist also dieselbe Suche mit derselben Antwort — er darf weder eine Abfrage auslösen noch die
   * Auswahl verwerfen.
   */
  const kriterien = planbauKriterien(filter, besteZuerst);
  const exercises = useAsync<Paged<ExerciseSummary>>(
    () => api.searchExercises(besteZuerst ? { ...filter, sort: "rating", dir: "desc" } : filter), kriterien);
  const [exerciseId, setExerciseId] = useState<number | "">("");
  const [auswahlHinweis, setAuswahlHinweis] = useState<string | null>(null);
  // Eigene Id-Wurzel: Der Grund für die gesperrte Liste braucht eine stabile Id, damit die Radiogruppe
  // ihn per `aria-describedby` dauerhaft benennen kann.
  const uid = useId();
  /*
   * Ab hier dieselbe Regel wie im Assistenten (B-169, `VaterWizard.tsx`) — diese Fläche ist sein Zwilling:
   * dieselbe Filterleiste, dieselben `ExerciseSummary`-Zeilen, dieselbe geblätterte Suche. Dort steht sie
   * seit B-169, hier fehlte sie (B-167).
   *
   * Ob die gezeigten Zeilen zu den eingestellten Kriterien gehören, sagt `exercises.stale` (B-164) – nicht `loading`:
   * das zöge „Kriterien geändert" und „dieselben Kriterien neu geladen" zusammen und deckte den **Fehlerzweig** nicht
   * (scheitert die neue Abfrage, fällt `loading` zurück, während die alten Zeilen stehen bleiben). Die Kriterien gehen
   * normalisiert in den Hook, darum ist „abc " dieselbe Suche wie „abc".
   */
  // B-540: der Auswahl-Schlüssel ohne Sortierung – ein Umsortieren ist dieselbe Suche und darf die Auswahl nicht leeren.
  const filterKey = planbauAuswahlSchluessel(filter);
  /** Der Schlüssel, für den die aktuelle Auswahl gilt – nach dem Effekt immer der aktuelle. */
  const geltenderFilterKey = useRef(filterKey);
  const zeilenVeraltet = exercises.stale;
  useEffect(() => {
    /*
     * Die Auswahl überlebte den Filterwechsel: Ihre Markierung verschwand mit der Zeile, `exerciseId`
     * blieb — und der Absende-Knopf blieb damit bedienbar. Der Vater legte so eine Position auf eine
     * Übung an, die er gar nicht mehr sah (B-167; das einzige Symptom mit Schreibwirkung).
     *
     * Dieselbe Funktion wie im Assistenten (B-161). Sie ist auf eine Liste geschnitten; eine
     * Einzelauswahl ist eine Liste mit höchstens einem Element. Stumm zu leeren wäre die andere Falle
     * (B-116) — darum der Hinweis.
     */
    const folge = auswahlNachFilterwechsel(
      exerciseId === "" ? [] : [exerciseId], geltenderFilterKey.current, filterKey);
    if (!folge) return;
    geltenderFilterKey.current = filterKey;
    // Die Rückgabe der geteilten Regel auswerten statt hart zu leeren: verfeinert sie ihre Antwort je
    // („behalte, was noch in der Liste steht"), zieht diese Aufrufstelle mit, statt still zurückzubleiben.
    setExerciseId(folge.selected[0] ?? "");
    // Nur überschreiben, wenn es etwas zu melden gibt: ein zweiter Tastendruck im Suchfeld hat nichts
    // mehr zu verlieren und darf die Meldung des ersten nicht wegwischen.
    if (folge.hinweis) setAuswahlHinweis(folge.hinweis);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filterKey]);
  /*
   * B-540: nach jeder frisch geladenen Seite **derselben** Suche (Umsortieren, „Erneut suchen") bleibt die Auswahl nur,
   * wenn die Übung noch in der Liste steht – sonst zeigte sie auf eine unsichtbare Übung (B-167). Erst wenn die Daten zum
   * eingestellten Stand gehören (`!stale`), sonst prüfte der Effekt gegen die alte Seite. Nach einem Filterwechsel ist die
   * Auswahl schon leer; dann tut er nichts.
   */
  useEffect(() => {
    if (exercises.stale || !exercises.data || exerciseId === "") return;
    const folge = auswahlNachUmsortieren([exerciseId], exercises.data.items.map((x) => x.id));
    if (!folge.hinweis) return;
    setExerciseId(folge.selected[0] ?? "");
    setAuswahlHinweis(folge.hinweis);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [exercises.data, exercises.stale]);
  const [settings, setSettings] = useState<PositionSettings>(defaultSettings());
  /*
   * Hat der Vater die Zielpunkte SELBST gesagt? (B-191)
   *
   * Ohne diesen Zustand steht die Zahl im Feld für zweierlei – seine Eingabe und die Empfehlung der
   * zuletzt angeklickten Übung – und beim nächsten Übungswechsel gewinnt die falsche Lesart: Übung A
   * (100) gefolgt von Übung B (ohne Empfehlung) ergab eine Position mit 100 Münzen Zielbelohnung, die
   * weder B vorschlägt noch jemand getippt hat.
   *
   * Er liegt hier und nicht in `PositionSettings`: Das ist Bedien-, kein Positionszustand – er darf
   * `settingsToDto` nicht erreichen, und die Zeile im Bearbeiten-Modus braucht ihn gar nicht (dort ist
   * jeder Wert bereits eine gespeicherte Aussage).
   */
  const [punkteGetippt, setPunkteGetippt] = useState(false);

  async function add(e: React.FormEvent) {
    e.preventDefault();
    if (exerciseId === "") { action.fail("Bitte eine Übung aus der Liste wählen."); return; }
    const slotProblem = timeSlotProblem(settings);
    if (slotProblem) { action.fail(slotProblem); return; }
    const dto: CreatePositionDto = { exerciseId: Number(exerciseId), ...settingsToDto(settings) };
    if (!await action.run(() => api.addPosition(planId, dto), "Übung als Position hinzugefügt.")) return;
    setExerciseId("");
    /*
     * Mit der Position endet auch die Aussage, die zu ihr gehoerte (Reviewer-Fund). Ohne dieses
     * Zuruecksetzen belegte KEINE weitere Uebung mehr vor: Position 2..n truegen die Zahl, die der Vater
     * fuer Position 1 getippt hat. Das widerspricht B-45s Akzeptanzkriterium 4 und der ausgelieferten
     * Feldhilfe ("Beim Hinzufuegen einer Uebung steht hier deren Empfehlung") - und es waere ein
     * Rueckschritt gegenueber dem Stand vor B-191, wo die Empfehlung der neuen Uebung gewann.
     */
    setPunkteGetippt(false);
    onAdded();
  }

  const results = exercises.data?.items ?? [];
  // Die gewählte Übung – ihr Typ bestimmt die Stufen im Formular (B-16).
  const gewaehlt = results.find((ex) => ex.id === exerciseId);
  // Der letzte Fehler überlebt den Start der Wiederholung (B-216) – wie im Zwilling (`VaterWizard.tsx`), und
  // nur die: unter einem anderen `filterKey` läuft eine andere Suche, nicht dieselbe noch einmal (B-227).
  const sucheGescheitert = useGemerkterFehler(exercises.error, exercises.loading, filterKey);
  // Ein gesperrter Knopf verliert in Chromium den Fokus – darum wandert er beim Klick auf den Kasten
  // (Begründung in `ListenAusfall.tsx`).
  const fehlerKasten = useRef<HTMLDivElement>(null);

  return (
    <>
    <form className="card" onSubmit={add} style={{ display: "flex", flexDirection: "column", gap: 10 }}>
      {/* Umfangreiche Filterleiste statt flachem Pulldown: Fach/Reihe/Unit/Klasse/Schulart/Typ/Art/Freitext. */}
      <ExerciseFilterBar value={filter} onChange={setFilter} subjects={subjects.data ?? []} />

      <div className="field">
        {/* Die Zahl gehört zu einer Abfrage, nicht zu einem Bildschirm: Solange die Zeilen zum vorigen
            Filter gehören, wäre „(N Treffer)" die Zahl von vorhin — eine Aussage über einen Filter, der
            gar nicht mehr eingestellt ist. Dieselbe Wahrheit trägt auch die Sperre der Radios. */}
        <label>Übung aus dem Katalog{" "}
          <span className="muted">
            {sucheGescheitert ? "(Suche gescheitert)" : zeilenVeraltet ? "(Suche läuft…)" : `(${results.length} Treffer)`}
          </span>
        </label>
        <div className="row" style={{ gap: 6, alignItems: "center", margin: "4px 0" }}>
          <label className="muted" htmlFor={`${uid}-reihenfolge`} style={{ fontSize: 12 }}>Trefferliste sortieren</label>
          <select id={`${uid}-reihenfolge`} value={besteZuerst ? "rating" : "catalog"}
            onChange={(e) => setBesteZuerst(e.target.value === "rating")}>
            <option value="catalog">wie im Katalog</option>
            <option value="rating">beste Bewertung zuerst</option>
          </select>
        </div>
        {/* Der Fehlerzweig braucht seine eigene Fläche, nicht nur die Sperre: Scheitert die Suche, bleibt
            `zeilenVeraltet` **dauerhaft** wahr (`exercises.stale`: die Deps der Daten ziehen nur bei Erfolg nach,
            B-164). Ohne diesen Kasten stünde für immer „Suche läuft…" neben einer Liste, die nie
            wieder wählbar wird — und der einzige Ausweg wäre, blind am Filter zu drehen. `reload()` ist
            der Weg zurück: gelingt es, gehören die Daten wieder zur Suche, und die Sperre fällt. Derselbe Aufbau wie im
            Zwilling (`VaterWizard.tsx`). */}
        {sucheGescheitert && (
          <div className="banner err" role="alert" tabIndex={-1} ref={fehlerKasten}>
            Die Übungssuche ist gescheitert: {sucheGescheitert}
            <button type="button" className="btn ghost inline-btn" style={{ width: "auto", marginLeft: 8 }}
              disabled={exercises.loading} onClick={() => { fehlerKasten.current?.focus(); exercises.reload(); }}>
              {exercises.loading ? "Lädt…" : "Erneut suchen"}
            </button>
          </div>
        )}
        {/* Der Grund gehört als Text daneben, nicht als `title` an das gesperrte Element: der erscheint
            nicht (B-160). Die Live-Region steht **immer** im DOM, auch leer – entstünde sie zusammen mit
            ihrem Text, sagte kein Screenreader sie an (B-134, dieselbe Bauform wie `StatusBanner`). */}
        <div role="status" aria-live="polite">
          {/* Im Fehlerfall schweigt dieser Satz: dort steht der Kasten darüber, und „gleich wählbar" wäre
              dann eine Zusage, die nichts mehr einlöst. */}
          {zeilenVeraltet && !exercises.error && (
            <p id={`${uid}-veraltet`} className="muted" style={{ margin: "4px 0", fontSize: 12 }}>
              Liste gehört noch zum vorigen Filter.
            </p>
          )}
        </div>
        {exercises.loading && exercises.data === null ? <div className="loading">Lade…</div> : (
          /* `aria-describedby` verbindet die Gruppe dauerhaft mit dem Grund – die Live-Region sagt ihn
             nur einmal an, und wer erst danach hierher navigiert, fände ihn sonst nicht mehr. */
          <div role="radiogroup" aria-label="Übung wählen"
            aria-describedby={zeilenVeraltet && !exercises.error ? `${uid}-veraltet` : undefined}
            style={{ maxHeight: 240, overflowY: "auto", border: "1px solid var(--stroke)", borderRadius: 8, display: "flex", flexDirection: "column" }}>
            {results.map((ex) => (
              // Der Knopf „Ausprobieren" steht neben dem Label, nicht darin (B-424): er darf die Wahl nicht ändern.
              <div key={ex.id} className="row"
                style={{ gap: 8, alignItems: "flex-start", padding: "6px 10px",
                  background: exerciseId === ex.id ? "rgba(140,220,120,.10)" : undefined,
                  borderBottom: "1px solid var(--stroke)" }}>
              <label className="row" style={{ gap: 8, alignItems: "flex-start", cursor: "pointer", flex: 1 }}>
                {/* Gesperrt, solange die Zeilen zum vorigen Filter gehören (B-167): ein Klick markiert
                    nicht nur, er schreibt die Lern-Standards dieser Übung ins Formular. Und gesperrt, wenn der Vater
                    die Übung nicht zuweisen darf (B-140) – der Server rechnet das (`canAssign`), der Grund steht
                    daneben; sonst sagte erst das Speichern `exercise_not_executable`. */}
                <input type="radio" name="add-position-exercise" disabled={zeilenVeraltet || !ex.canAssign}
                  aria-describedby={ex.canAssign ? undefined : `${uid}-gesperrt-${ex.id}`}
                  checked={exerciseId === ex.id}
                  // Nur die Werte übernehmen, die AUS der Übung kommen – das Pflichtziel und der Malus
                  // bleiben stehen (ein Übungswechsel ist keine Rücknahme).
                  //
                  // Die Zielpunkte gehören seit B-45 zur ersten Gruppe – aber nur, solange der Vater
                  // nichts gesagt hat: Eine selbst getippte Zahl bleibt seit B-191 IMMER stehen, nicht
                  // mehr nur dann, wenn die neue Übung keine brauchbare Empfehlung trägt. Der Malus bleibt
                  // ganz außen vor – für ihn gibt es keine Empfehlung, er ist eine Aussage des Vaters.
                  onChange={() => {
                    setExerciseId(ex.id);
                    setSettings((cur) => ({
                      ...cur,
                      useLeitner: ex.defaultUseLeitner || Number(cur.newItemsPerDay) > 0,
                      requireTypedTest: ex.defaultRequireTypedTest,
                      itemCount: ex.defaultItemCount?.toString() ?? "",
                      pointsGoalMet: pointsFrom(ex, cur.pointsGoalMet, punkteGetippt),
                      // B-16, Entscheidung 7: die Stufen gehören zum Typ – eine der vorigen Übung wäre hier womöglich
                      // ungültig (validation_error) oder, schlimmer, gültig und ungewollt.
                      stage: "",
                    }));
                  }}
                  style={{ marginTop: 3 }} />
                <span style={{ display: "flex", flexDirection: "column" }}>
                  <span>{ex.title} <span className="muted">· {typeLabel(ex.type)}</span>
                    {umfang(ex.type, ex.itemCount) && <span className="muted"> · {umfang(ex.type, ex.itemCount)}</span>}
                    {(ex.gradeMin != null || ex.gradeMax != null) &&
                      <span className="muted"> · Kl. {ex.gradeMin ?? "?"}–{ex.gradeMax ?? "?"}</span>}
                    {/* Nur die Art trägt ein Etikett, Typ/Klasse/Quelle nicht — und das ist Absicht (B-163):
                        der Typ kommt aus zwölf festen Werten, die man lernt, die Art erfindet jeder Creator
                        je Fach frei. Nur bei ihr kann der Leser die Achse grundsätzlich nicht kennen. Wer die
                        Fragmente hier glattzieht, nimmt genau diese Unterscheidung wieder weg. */}
                    {ex.categoryName && <span className="muted"> · Art: {ex.categoryName}</span>}
                    {ex.source && <span className="muted"> · {ex.source}</span>}
                    <span className="muted"> · {ratingSummary(ex.averageRating, ex.ratingCount)}</span>
                  </span>
                  {ex.description && <span className="muted" style={{ fontSize: 12 }}>{ex.description}</span>}
                  {/* Als Text, nicht als `title` (B-160). „Ausprobieren" bleibt daneben bedienbar – Lesen ist global. */}
                  {!ex.canAssign && (
                    <span id={`${uid}-gesperrt-${ex.id}`} className="pill mag" style={{ fontSize: 12, alignSelf: "flex-start" }}>
                      {NICHT_ZUWEISBAR_GRUND}
                    </span>
                  )}
                </span>
              </label>
              {/* Gesperrt wie das Radio (B-167): verschwände die Zeile bei offenem Dialog, fiele der Fokus beim Schließen ins Leere. */}
              <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={zeilenVeraltet}
                aria-label={`„${ex.title}" ausprobieren`} onClick={() => setVorschau({ id: ex.id, title: ex.title })}>🧪 Ausprobieren</button>
              </div>
            ))}
            {/* Nur wenn die Liste auch zum eingestellten Filter gehört: sonst stünde „Keine Treffer" neben
                „(Suche läuft…)" – zwei Aussagen über zwei verschiedene Abfragen in einem Blickfeld. */}
            {results.length === 0 && !zeilenVeraltet && !exercises.error && (
              <div className="muted" style={{ padding: "8px 10px" }}>Keine Treffer – Filter anpassen.</div>
            )}
          </div>
        )}
        {/* Aus demselben Grund wie die Trefferzahl: „Zeigt 20 von 143" gehörte zum vorigen Filter. Die Region
            bleibt dabei stehen und wird nur geleert – sonst entstünde sie mit dem neuen Hinweis zusammen, und
            gerade die Änderung, die der Vater mit dem Filter ausgelöst hat, bliebe stumm (B-134). */}
        <TruncationHint shown={results.length} total={zeilenVeraltet ? 0 : (exercises.data?.total ?? 0)} />
        <div role="status" aria-live="polite">
          {auswahlHinweis && <div className="banner info" style={{ marginTop: 6 }}>{auswahlHinweis}</div>}
        </div>
      </div>

      {/* Jede Änderung, die durch das Formular kommt, ist eine Aussage des Vaters – der Übungswechsel
          oben setzt `settings` dagegen direkt und läuft hier nicht durch. Damit braucht `PositionFields`
          keinen zusätzlichen Prop, und die Unterscheidung hängt an dem, was ohnehin verschieden ist:
          dem Weg, auf dem der Wert entsteht. */}
      <PositionFields value={settings} stages={gewaehlt ? types.stages(gewaehlt.type) : []} onChange={(next) => {
        if (next.pointsGoalMet !== settings.pointsGoalMet) setPunkteGetippt(true);
        setSettings(next);
      }} />

      <div className="row">
        {/* `zeilenVeraltet` gehört an das Element mit **Schreibwirkung**, nicht nur an die Radios: Solange
            die Liste zum vorigen Filter gehört, kann der Vater nicht sehen, worauf er die Position legt.
            Die Auswahl-Rücknahme darunter greift erst im Effekt – also einen Tick später als dieser
            Render, und das ist genau die Lücke, um die es in dieser Story geht. */}
        <button type="submit" className="btn inline-btn" style={{ width: "auto", marginLeft: "auto" }}
          disabled={action.busy || exerciseId === "" || zeilenVeraltet}>
          {action.busy ? "Füge hinzu…" : "+ Position hinzufügen"}
        </button>
      </div>
      <StatusBanner message={action.message} style={{ marginTop: 0 }} />
      <p className="muted" style={{ margin: 0, fontSize: 12 }}>
        Leere Felder bedeuten den Standard: Bestehen ab 80 %, alle Inhalte – und bei den Bonus-Werten den
        <b> Vorschlag der Übung</b>. Beim Auswählen einer Übung übernimmt das Formular deren Lern-Standards.
      </p>
    </form>
    {/* Außerhalb des Formulars: ein Knopf im Dialog ohne `type` würde sonst die Position abschicken. */}
    {vorschau && <ExercisePreviewModal exerciseId={vorschau.id} title={vorschau.title} onClose={() => setVorschau(null)} />}
    </>
  );
}

/** Eine Positionszeile mit Inline-Bearbeiten (Ziel/Punkte/Leitner) und Entfernen (409-bewusst). */
function PositionRow({ planId, pos, onChanged, action }: {
  planId: number; pos: PositionResponse; onChanged: () => void; action: ActionState;
}) {
  const types = useExerciseTypes();
  const typeLabel = types.label;
  const [editing, setEditing] = useState(false);
  const [showReport, setShowReport] = useState(false);
  const [settings, setSettings] = useState<PositionSettings>(() => settingsFrom(pos));
  // B-469/B-550: womit die Pflicht geprüft wird – der Server sagt es (`learningCheck`), die Zeile rechnet nichts nach.
  const lernkontrolle = lernkontrolleText(pos.learningCheck);
  // B-423, AK 7: die Stufenleiter nur lesbar – bearbeiten bleibt B-16. Namen aus dem Manifest, sonst „Stufe n".
  const leiter = leiterText(pos.stageSchedule, (st) => types.stages(pos.exerciseType).find((o) => o.value === st)?.label);

  function cancel() {
    setSettings(settingsFrom(pos));
    setEditing(false);
  }

  /*
   * Beim Öffnen frisch aus der gespeicherten Position: die Zeile bleibt über ein Neuladen hinweg montiert (`key={p.id}`),
   * und ohne das stünden nach dem ersten Speichern die alten Merkwerte (`stageStored`, `itemCountStored` …) im Formular –
   * ein zweites „zurück auf Standard" schickte dann keinen Schalter, und „Gespeichert." stimmte nicht (B-547).
   */
  function beginEdit() {
    setSettings(settingsFrom(pos));
    setEditing(true);
  }

  async function save() {
    const slotProblem = timeSlotProblem(settings);
    if (slotProblem) { action.fail(slotProblem); return; }
    if (!await action.run(() => api.updatePosition(planId, pos.id, settingsToUpdateDto(settings)), "Position gespeichert.")) return;
    setEditing(false);
    onChanged();
  }

  async function remove() {
    if (!confirmAction(REMOVE_POSITION_QUESTION)) return;
    if (await action.run(() => api.deletePosition(planId, pos.id), "Position entfernt.")) onChanged();
  }

  if (!editing) {
    return (
      <>
        <tr>
          <td className="num">{pos.order + 1}</td>
          <td>
            {pos.exerciseTitle} <span className="muted">· {typeLabel(pos.exerciseType)}</span>
          </td>
          <td>
            {CADENCE_LABEL[pos.cadence]}
            {pos.goalThreshold != null && <span className="muted"> · bestehen ab {pos.goalThreshold}%</span>}
            {pos.itemCount != null && <span className="muted"> · {pos.itemCount} Inhalte</span>}
            {dosisText(pos) !== null && <span className="muted"> · {dosisText(pos)}</span>}
            <span className="muted"> · {ORDER_LABEL[pos.orderStrategy]}</span>
            {leiter && <span className="muted"> · Stufen: {leiter}</span>}
            {lernkontrolle && (
              <>
                {" · "}
                <span className={TON_KLASSE[lernkontrolle.ton]}>{lernkontrolle.text}</span>
                <InfoHint topic="lernkontrolle" />
              </>
            )}
          </td>
          {/* B-538: eine freie Position bucht nichts – ihre gespeicherten Punkte sind wirkungslos und stehen darum nicht da. */}
          {pos.cadence === "None" ? <td className="num muted">keine Punkte</td> : (
            <td className="num">Ziel {pos.pointsGoalMet} · neu {pos.newContentPoints}
              {pos.penaltyCoins > 0 && <span className="pill amber"> · Malus −{pos.penaltyCoins}🪙</span>}
              {pos.comboThreshold > 0 && pos.comboBonusPoints > 0 && <span className="muted"> · Combo +{pos.comboBonusPoints}</span>}
              {pos.timeSlots?.map((s) => (
                <span key={`${s.start}-${s.end}`} className="muted">
                  {" · "}{hhmm(s.start)}–{hhmm(s.end)} ×{s.multiplier.toLocaleString("de-DE")}
                </span>
              ))}
            </td>
          )}
          <td>{pos.useLeitner ? <span className="pill lime">an · max {pos.maxBox}{pos.forgivingDemotion ? " · verzeihend" : ""}</span> : <span className="muted">aus</span>}</td>
          <td className="row" style={{ gap: 6, justifyContent: "flex-end" }}>
            <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
              aria-expanded={showReport} onClick={() => setShowReport((s) => !s)}>📊 Report</button>
            <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={action.busy} onClick={beginEdit}>Bearbeiten</button>
            <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={action.busy} onClick={remove}>Entfernen</button>
          </td>
        </tr>
        {showReport && (
          <tr>
            <td colSpan={6} style={{ background: "rgba(255,255,255,.02)" }}>
              <PositionReportPanel planId={planId} positionId={pos.id} />
            </td>
          </tr>
        )}
      </>
    );
  }

  return (
    <tr>
      <td className="num">{pos.order + 1}</td>
      <td>{pos.exerciseTitle} <span className="muted">· {typeLabel(pos.exerciseType)}</span></td>
      <td colSpan={3}>
        <PositionFields value={settings} onChange={setSettings} stages={types.stages(pos.exerciseType)} />
      </td>
      <td className="row" style={{ gap: 6, justifyContent: "flex-end" }}>
        <button type="button" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy} onClick={save}>OK</button>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={action.busy} onClick={cancel}>Abbrechen</button>
      </td>
    </tr>
  );
}

/** Lern-Report der Position: je Inhalt „sitzt/sitzt nicht" (Box/Beherrschung) + Test-Trefferquote. */
function PositionReportPanel({ planId, positionId }: { planId: number; positionId: number }) {
  const report = useAsync<PositionReport>(() => api.positionReport(planId, positionId), [planId, positionId]);

  if (report.loading) return <div className="loading">Lade Report…</div>;
  if (report.error || !report.data) return <div className="banner err">{report.error ?? "Report nicht verfügbar."}</div>;
  const r = report.data;

  if (r.totalItems === 0) return <div className="muted">Diese Übung hat keine einzeln auswertbaren Inhalte.</div>;

  return (
    <div style={{ padding: "6px 2px" }}>
      <p className="muted" style={{ marginTop: 0 }}>
        {r.introducedItems}/{r.totalItems} eingeführt · {r.masteredItems} sitzen sicher (Box {r.maxBox})
      </p>
      <div style={{ overflowX: "auto" }}>
        <table className="table">
          <thead><tr><th>Inhalt</th><th>Lösung</th><th>Beherrschung</th><th className="num">Test</th><th>Fällig</th></tr></thead>
          <tbody>
            {r.items.map((it) => (
              <tr key={it.itemIndex}>
                <td>{it.prompt}</td>
                <td className="muted">{it.answer}</td>
                <td><MasteryPill it={it} maxBox={r.maxBox} /></td>
                <td className="num">{it.testsSeen === 0 ? "—" : `${it.testsCorrect}/${it.testsSeen}`}</td>
                <td className="muted">{it.dueOn ? formatDate(it.dueOn) : "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

/**
 * Was die Positionszeile zur Tagesdosis sagt – die Dosis, die das Spiel anwendet (`dailyDoseInForce`), nicht die
 * gespeicherte. Wertet die Liste als Menge, ruht die Dosis; die Zeile sagt das, statt „2 neue je Tag" zu versprechen
 * (B-480).
 */
export function dosisText(pos: { newItemsPerDay: number; dailyDoseInForce: number }): string | null {
  if (pos.dailyDoseInForce > 0) return `${pos.dailyDoseInForce} neue je Tag`;
  // Der Grund kommt vom Server (die Übung wertet als Menge, `GradesAsSet`) – der Satz nennt ihn so, nicht über einen Typ.
  return pos.newItemsPerDay > 0 ? "Tagesdosis ruht (die Übung wertet als Menge)" : null;
}

/** Wie laut die Zeile die Lernkontrolle sagt: neutral, gelb (schwach) oder rot (Ziel nicht erreichbar). */
export type LernkontrollTon = "neutral" | "warnung" | "fehler";

const TON_KLASSE: Record<LernkontrollTon, string> = { neutral: "muted", warnung: "pill amber", fehler: "pill mag" };

/**
 * Was die Positionszeile über die Lernkontrolle sagt (B-469, Entscheidung 2; B-550). Die Art bestimmt der Server
 * (`PositionResponse.learningCheck`) – aus Typ, Stufe, Stufenplan und „nur getippt“. Die Zeile übersetzt sie nur: eine
 * Ableitung hier (vorher aus Manifest und Häkchen) übersah die Stufenleiter und warnte bei ihr grundlos (B-550).
 *
 * `null` heißt: nichts sagen – der Server liefert es nur bei unbekanntem Typ, und dort wäre jeder Satz geraten.
 */
export function lernkontrolleText(check: LearningCheck | null | undefined): { text: string; ton: LernkontrollTon } | null {
  if (!check) return null;
  switch (check.kind) {
    case "Typed": return { text: "Test getippt", ton: "neutral" };
    case "SelfAssessed": return { text: "Test, Selbsteinschätzung zählt", ton: "warnung" };
    case "Ladder": {
      const tag = check.typedFromDay;
      // Ohne Tag (der Vertrag erlaubt `null`) nur die Reihenfolge – kein erfundener Tag.
      return tag != null && tag > 1
        ? { text: `Selbsteinschätzung bis Tag ${tag - 1}, ab Tag ${tag} getippt`, ton: "neutral" }
        : { text: "Erst Selbsteinschätzung, dann getippt", ton: "neutral" };
    }
    case "Playthrough": return { text: "zählt Durchspielen", ton: "warnung" };
    case "NoTest": return { text: "kein Test – zählt Durchspielen", ton: "warnung" };
    case "Unreachable":
      return {
        text: "Ziel nicht erreichbar: nur getippte Tests zählen, aber an manchen Tagen wird per Selbsteinschätzung geprüft",
        ton: "fehler",
      };
  }
}

/**
 * Der Auswahl-Schlüssel des Planbaus (B-540): dieselben Felder wie {@link planbauKriterien}, aber **ohne** Sortierung –
 * eine andere Reihenfolge ist dieselbe Treffermenge, die Auswahl fällt dort nicht pauschal (das regelt
 * `auswahlNachUmsortieren` je geladener Seite). Aus derselben Quelle gebaut, damit kein Filterfeld an nur einer Stelle
 * fehlen kann. Dient auch dem gemerkten Suchfehler: er überlebt ein Umsortieren, die Suche ist dieselbe.
 */
export function planbauAuswahlSchluessel(filter: ExerciseFilter): string {
  return JSON.stringify(planbauKriterien(filter, false));
}

/**
 * Die Abhängigkeiten der Übungssuche im Planbau – jede Facette des Filters und die Reihenfolge. Eigene Funktion, damit ein
 * Test prüft, dass **jedes** Feld von `ExerciseFilter` darin steht: ein vergessenes ließe den Filter wechseln, ohne dass
 * die Abfrage neu liefe – so geschehen mit dem Grammatik-Thema (B-155) und beinahe mit der Reihe (B-518).
 */
export function planbauKriterien(filter: ExerciseFilter, besteZuerst: boolean): unknown[] {
  return [
    filter.subjectId, filter.seriesId, filter.seriesUnitId, filter.grade, filter.schoolType, filter.categoryId,
    filter.type || undefined, filter.search?.trim() || undefined,
    filter.grammarTopicId,
    // Die Reihenfolge gehört dazu: die Liste ist eine Seite, und eine andere Sortierung zeigt andere Zeilen (B-270).
    besteZuerst,
  ];
}

