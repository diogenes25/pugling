import { useId, useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { useAction, type ActionState } from "../lib/useAction";
import { confirmAction } from "../lib/ui";
import { useAsync } from "../lib/useAsync";
import { useExerciseTypes } from "../lib/exerciseTypes";
import { TruncationHint } from "../components/ListControls";
import { MasteryPill } from "../components/MasteryPill";
import { FieldLabel, InfoHint } from "../components/InfoHint";
import type {
  CreatePositionDto, ExerciseSummary, GoalCadence, PositionReport, PositionResponse, Paged, PracticeOrder,
  ScoringTimeSlot, SubjectResponse, UpdatePositionDto,
} from "../lib/types";
import { ExerciseFilterBar, type ExerciseFilter } from "./ExerciseFilterBar";

/*
 * Positions-UI des neuen Lehrplan-Modells: Ein Plan ist ein Container aus Katalog-Übungen. Jede
 * Position verweist auf eine globale Übung (der Inhalt bleibt dort) und trägt ihre EIGENEN Ziele
 * (Rhythmus + Schwelle), Punkte und Leitner-Einstellungen. Hier stellt der Vater den Plan „zusammen".
 */

const CADENCE_LABEL: Record<GoalCadence, string> = {
  None: "frei (kein Ziel)", Daily: "Tagesziel", Weekly: "Wochenziel",
};
const CADENCES: GoalCadence[] = ["Daily", "Weekly", "None"];
const ORDER_LABEL: Record<PracticeOrder, string> = {
  WeakestFirst: "Schwächste zuerst", Serial: "Reihenfolge", Random: "Zufällig", NewestWeighted: "Neue bevorzugt",
};
const ORDERS: PracticeOrder[] = ["WeakestFirst", "Serial", "Random", "NewestWeighted"];

// Anzeigename der Typen aus dem Server-Manifest – eine dritte Kopie der Tabelle lief zwangsläufig
// aus dem Takt (das UI kannte sechs Typen, der Server führt zwölf).

export function PlanPositions({ planId }: { planId: number }) {
  const positions = useAsync<PositionResponse[]>(() => api.positions(planId), [planId]);
  // **Eine** Aktion für den ganzen Abschnitt: es lässt sich ohnehin nur eine Position auf einmal
  // bearbeiten, und so steht die Rückmeldung an einer Stelle statt in jeder Zeile.
  const action = useAction();

  return (
    <section>
      <h3 className="h-section">Übungen im Plan {positions.data ? `(${positions.data.length})` : ""}</h3>
      <p className="muted" style={{ marginTop: 0 }}>
        Jede Position verweist auf eine Katalog-Übung und trägt eigene Ziele, Punkte und Leitner-Einstellungen.
      </p>
      <StatusBanner message={action.message} />

      <AddPosition planId={planId} action={action} onAdded={positions.reload} />

      {positions.loading && positions.data === null ? <div className="loading">Lade Positionen…</div> : positions.error ? (
        <div className="banner err">{positions.error}</div>
      ) : (
        <div style={{ overflowX: "auto", marginTop: 12 }}>
          <table className="table">
            <thead><tr><th>#</th><th>Übung</th><th>Ziel</th><th className="num">Punkte</th><th>Leitner</th><th>Aktionen</th></tr></thead>
            <tbody>
              {positions.data?.map((p) => (
                <PositionRow key={p.id} planId={planId} pos={p} onChanged={positions.reload} action={action} />
              ))}
              {positions.data?.length === 0 && (
                <tr><td colSpan={6} className="muted">Noch keine Übungen im Plan – füge oben eine aus dem Katalog hinzu.</td></tr>
              )}
            </tbody>
          </table>
        </div>
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
  requireTypedTest: boolean;
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

/** Startwerte einer neuen Position; die Übung bringt ihre Lern-Standards als Vorschlag mit. */
function defaultSettings(ex?: ExerciseSummary): PositionSettings {
  return {
    cadence: "Daily", goalThreshold: "", itemCount: ex?.defaultItemCount?.toString() ?? "",
    orderStrategy: "WeakestFirst", pointsGoalMet: 20, penaltyCoins: 0,
    // Leer = Vorschlag der Übung übernehmen (siehe PositionSettings).
    newContentPoints: "", comboThreshold: "", comboBonusPoints: "",
    useLeitner: ex?.defaultUseLeitner ?? false, requireTypedTest: ex?.defaultRequireTypedTest ?? false,
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
    orderStrategy: pos.orderStrategy,
    pointsGoalMet: pos.pointsGoalMet,
    penaltyCoins: pos.penaltyCoins,
    // Am gespeicherten Stand ist die Erbschaft längst aufgelöst – hier stehen konkrete Zahlen.
    newContentPoints: pos.newContentPoints.toString(),
    comboThreshold: pos.comboThreshold.toString(),
    comboBonusPoints: pos.comboBonusPoints.toString(),
    useLeitner: pos.useLeitner,
    requireTypedTest: pos.requireTypedTest,
    // Bearbeitet wird nur das erste Fenster; die ganze Liste reist mit, damit das Speichern die übrigen
    // nicht verwirft und die Sekunden nicht abschneidet (siehe `timeSlotStored`).
    timeSlotStart: hhmm(pos.timeSlots?.[0]?.start ?? ""),
    timeSlotEnd: hhmm(pos.timeSlots?.[0]?.end ?? ""),
    timeSlotMultiplier: pos.timeSlots?.[0]?.multiplier?.toString() ?? "",
    timeSlotName: pos.timeSlots?.[0]?.name ?? "",
    timeSlotStored: pos.timeSlots ?? null,
  };
}

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
    orderStrategy: s.orderStrategy,
    pointsGoalMet: s.pointsGoalMet,
    penaltyCoins: s.penaltyCoins,
    newContentPoints: numOrNull(s.newContentPoints),
    comboThreshold: numOrNull(s.comboThreshold),
    comboBonusPoints: numOrNull(s.comboBonusPoints),
    useLeitner: s.useLeitner,
    requireTypedTest: s.requireTypedTest,
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
  return { ...settingsToDto(s), clearTimeSlots: timeSlotOf(s) === null };
}

/** Die Felder selbst. Präsentational: den Zustand hält der Aufrufer (Anlegen bzw. Zeile im Edit-Modus). */
function PositionFields({ value, onChange }: { value: PositionSettings; onChange: (next: PositionSettings) => void }) {
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
      <div className="field" style={{ maxWidth: 180 }}>
        <FieldLabel htmlFor={`${uid}-order`} topic="orderStrategy">Reihenfolge</FieldLabel>
        <select id={`${uid}-order`} aria-label="Reihenfolge" value={value.orderStrategy}
          onChange={(e) => up("orderStrategy", e.target.value as PracticeOrder)}>
          {ORDERS.map((o) => <option key={o} value={o}>{ORDER_LABEL[o]}</option>)}
        </select>
      </div>
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
      <span className="label-row">
        <label className="checkline">
          <input type="checkbox" checked={value.useLeitner} onChange={(e) => up("useLeitner", e.target.checked)} /> Leitner-Kasten
        </label>
        <InfoHint topic="useLeitner" />
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
function AddPosition({ planId, action, onAdded }: { planId: number; action: ActionState; onAdded: () => void }) {
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);
  const types = useExerciseTypes();
  const typeLabel = (t: string) => types?.label(t) ?? t;
  const [filter, setFilter] = useState<ExerciseFilter>({});
  // Die Gesamtzahl wird mitgeführt: der Server liefert nur eine Seite, und eine still gekappte
  // Auswahlliste liest sich wie „mehr gibt es nicht".
  const exercises = useAsync<Paged<ExerciseSummary>>(() => api.searchExercises(filter),
    [filter.subjectId, filter.seriesUnitId, filter.grade, filter.schoolType, filter.categoryId, filter.type, filter.search]);
  const [exerciseId, setExerciseId] = useState<number | "">("");
  const [settings, setSettings] = useState<PositionSettings>(defaultSettings());

  async function add(e: React.FormEvent) {
    e.preventDefault();
    if (exerciseId === "") { action.fail("Bitte eine Übung aus der Liste wählen."); return; }
    const slotProblem = timeSlotProblem(settings);
    if (slotProblem) { action.fail(slotProblem); return; }
    const dto: CreatePositionDto = { exerciseId: Number(exerciseId), ...settingsToDto(settings) };
    if (!await action.run(() => api.addPosition(planId, dto), "Übung als Position hinzugefügt.")) return;
    setExerciseId("");
    onAdded();
  }

  const results = exercises.data?.items ?? [];

  return (
    <form className="card" onSubmit={add} style={{ display: "flex", flexDirection: "column", gap: 10 }}>
      {/* Umfangreiche Filterleiste statt flachem Pulldown: Fach/Reihe/Unit/Klasse/Schulart/Typ/Art/Freitext. */}
      <ExerciseFilterBar value={filter} onChange={setFilter} subjects={subjects.data ?? []} />

      <div className="field">
        <label>Übung aus dem Katalog <span className="muted">({results.length} Treffer)</span></label>
        {exercises.loading && exercises.data === null ? <div className="loading">Lade…</div> : (
          <div role="radiogroup" aria-label="Übung wählen"
            style={{ maxHeight: 240, overflowY: "auto", border: "1px solid var(--stroke)", borderRadius: 8, display: "flex", flexDirection: "column" }}>
            {results.map((ex) => (
              <label key={ex.id} className="row"
                style={{ gap: 8, alignItems: "flex-start", padding: "6px 10px", cursor: "pointer",
                  background: exerciseId === ex.id ? "rgba(140,220,120,.10)" : undefined,
                  borderBottom: "1px solid var(--stroke)" }}>
                <input type="radio" name="add-position-exercise" checked={exerciseId === ex.id}
                  // Nur die Werte übernehmen, die AUS der Übung kommen – schon eingestellte Ziele,
                  // Punkte und Malus bleiben stehen (ein Übungswechsel ist keine Rücknahme).
                  onChange={() => {
                    setExerciseId(ex.id);
                    setSettings((cur) => ({
                      ...cur,
                      useLeitner: ex.defaultUseLeitner,
                      requireTypedTest: ex.defaultRequireTypedTest,
                      itemCount: ex.defaultItemCount?.toString() ?? "",
                    }));
                  }}
                  style={{ marginTop: 3 }} />
                <span style={{ display: "flex", flexDirection: "column" }}>
                  <span>{ex.title} <span className="muted">· {typeLabel(ex.type)}</span>
                    {(ex.gradeMin != null || ex.gradeMax != null) &&
                      <span className="muted"> · Kl. {ex.gradeMin ?? "?"}–{ex.gradeMax ?? "?"}</span>}
                    {/* Nur die Art trägt ein Etikett, Typ/Klasse/Quelle nicht — und das ist Absicht (B-163):
                        der Typ kommt aus zwölf festen Werten, die man lernt, die Art erfindet jeder Creator
                        je Fach frei. Nur bei ihr kann der Leser die Achse grundsätzlich nicht kennen. Wer die
                        Fragmente hier glattzieht, nimmt genau diese Unterscheidung wieder weg. */}
                    {ex.categoryName && <span className="muted"> · Art: {ex.categoryName}</span>}
                    {ex.source && <span className="muted"> · {ex.source}</span>}
                  </span>
                  {ex.description && <span className="muted" style={{ fontSize: 12 }}>{ex.description}</span>}
                </span>
              </label>
            ))}
            {results.length === 0 && <div className="muted" style={{ padding: "8px 10px" }}>Keine Treffer – Filter anpassen.</div>}
          </div>
        )}
        <TruncationHint shown={results.length} total={exercises.data?.total ?? 0} />
      </div>

      <PositionFields value={settings} onChange={setSettings} />

      <div className="row">
        <button type="submit" className="btn inline-btn" style={{ width: "auto", marginLeft: "auto" }} disabled={action.busy || exerciseId === ""}>
          {action.busy ? "Füge hinzu…" : "+ Position hinzufügen"}
        </button>
      </div>
      <p className="muted" style={{ margin: 0, fontSize: 12 }}>
        Leere Felder bedeuten den Standard: Bestehen ab 80 %, alle Inhalte – und bei den Bonus-Werten den
        <b> Vorschlag der Übung</b>. Beim Auswählen einer Übung übernimmt das Formular deren Lern-Standards.
      </p>
    </form>
  );
}

/** Eine Positionszeile mit Inline-Bearbeiten (Ziel/Punkte/Leitner) und Entfernen (409-bewusst). */
function PositionRow({ planId, pos, onChanged, action }: {
  planId: number; pos: PositionResponse; onChanged: () => void; action: ActionState;
}) {
  const types = useExerciseTypes();
  const typeLabel = (t: string) => types?.label(t) ?? t;
  const [editing, setEditing] = useState(false);
  const [showReport, setShowReport] = useState(false);
  const [settings, setSettings] = useState<PositionSettings>(() => settingsFrom(pos));

  function cancel() {
    setSettings(settingsFrom(pos));
    setEditing(false);
  }

  async function save() {
    const slotProblem = timeSlotProblem(settings);
    if (slotProblem) { action.fail(slotProblem); return; }
    if (!await action.run(() => api.updatePosition(planId, pos.id, settingsToUpdateDto(settings)), "Position gespeichert.")) return;
    setEditing(false);
    onChanged();
  }

  async function remove() {
    if (!confirmAction("Diese Position wirklich entfernen? Fortschritt und Auswertung dieser Position gehen verloren.")) return;
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
            <span className="muted"> · {ORDER_LABEL[pos.orderStrategy]}</span>
            {pos.requireTypedTest && <span className="muted"> · getippt</span>}
          </td>
          <td className="num">Ziel {pos.pointsGoalMet} · neu {pos.newContentPoints}
            {pos.penaltyCoins > 0 && <span className="pill amber"> · Malus −{pos.penaltyCoins}🪙</span>}
            {pos.comboThreshold > 0 && pos.comboBonusPoints > 0 && <span className="muted"> · Combo +{pos.comboBonusPoints}</span>}
            {pos.timeSlots?.map((s) => (
              <span key={`${s.start}-${s.end}`} className="muted">
                {" · "}{hhmm(s.start)}–{hhmm(s.end)} ×{s.multiplier.toLocaleString("de-DE")}
              </span>
            ))}
          </td>
          <td>{pos.useLeitner ? <span className="pill lime">an · max {pos.maxBox}</span> : <span className="muted">aus</span>}</td>
          <td className="row" style={{ gap: 6, justifyContent: "flex-end" }}>
            <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
              aria-expanded={showReport} onClick={() => setShowReport((s) => !s)}>📊 Report</button>
            <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={action.busy} onClick={() => setEditing(true)}>Bearbeiten</button>
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
        <PositionFields value={settings} onChange={setSettings} />
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
                <td className="muted">{it.dueOn ?? "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
