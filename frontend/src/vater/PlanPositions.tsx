import { useId, useState } from "react";
import { api, errorMessage } from "../lib/api";
import { confirmAction } from "../lib/ui";
import { useAsync } from "../lib/useAsync";
import { useExerciseTypes } from "../lib/exerciseTypes";
import { TruncationHint } from "../components/ListControls";
import { MasteryPill } from "../components/MasteryPill";
import type {
  CreatePositionDto, ExerciseSummary, GoalCadence, PositionReport, PositionResponse, Paged, PracticeOrder, SubjectResponse,
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

type Flash = (ok: boolean, text: string) => void;

export function PlanPositions({ planId }: { planId: number }) {
  const positions = useAsync<PositionResponse[]>(() => api.positions(planId), [planId]);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);

  const flash: Flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3500); };

  return (
    <section>
      <h3 className="h-section">Übungen im Plan {positions.data ? `(${positions.data.length})` : ""}</h3>
      <p className="muted" style={{ marginTop: 0 }}>
        Jede Position verweist auf eine Katalog-Übung und trägt eigene Ziele, Punkte und Leitner-Einstellungen.
      </p>
      <div role="status" aria-live="polite">
        {msg && <div className={`banner ${msg.ok ? "ok" : "err"}`}>{msg.text}</div>}
      </div>

      <AddPosition planId={planId}
        onAdded={() => { positions.reload(); flash(true, "Übung als Position hinzugefügt."); }}
        onError={(t) => flash(false, t)} />

      {positions.loading ? <div className="loading">Lade Positionen…</div> : positions.error ? (
        <div className="banner err">{positions.error}</div>
      ) : (
        <div style={{ overflowX: "auto", marginTop: 12 }}>
          <table className="table">
            <thead><tr><th>#</th><th>Übung</th><th>Ziel</th><th className="num">Punkte</th><th>Leitner</th><th>Aktionen</th></tr></thead>
            <tbody>
              {positions.data?.map((p) => (
                <PositionRow key={p.id} planId={planId} pos={p} onChanged={positions.reload} flash={flash} />
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
interface PositionSettings {
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
}

/** Startwerte einer neuen Position; die Übung bringt ihre Lern-Standards als Vorschlag mit. */
function defaultSettings(ex?: ExerciseSummary): PositionSettings {
  return {
    cadence: "Daily", goalThreshold: "", itemCount: ex?.defaultItemCount?.toString() ?? "",
    orderStrategy: "WeakestFirst", pointsGoalMet: 20, penaltyCoins: 0,
    // Leer = Vorschlag der Übung übernehmen (siehe PositionSettings).
    newContentPoints: "", comboThreshold: "", comboBonusPoints: "",
    useLeitner: ex?.defaultUseLeitner ?? false, requireTypedTest: ex?.defaultRequireTypedTest ?? false,
  };
}

/** Der gespeicherte Stand einer Position als Formular-Zustand. */
function settingsFrom(pos: PositionResponse): PositionSettings {
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
  };
}

/** Leeres Feld = `null` = „Standard bzw. Vorschlag der Übung übernehmen". */
const numOrNull = (v: string): number | null => (v.trim() === "" ? null : Number(v));

/** Formular-Zustand in die Vertragsform (leere Felder werden zu `null` = Standard). */
function settingsToDto(s: PositionSettings) {
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
  };
}

/** Die Felder selbst. Präsentational: den Zustand hält der Aufrufer (Anlegen bzw. Zeile im Edit-Modus). */
function PositionFields({ value, onChange }: { value: PositionSettings; onChange: (next: PositionSettings) => void }) {
  const uid = useId();
  const up = <K extends keyof PositionSettings>(k: K, v: PositionSettings[K]) => onChange({ ...value, [k]: v });

  return (
    <div className="row" style={{ gap: 12, alignItems: "flex-end", flexWrap: "wrap" }}>
      <div className="field" style={{ maxWidth: 180 }}>
        <label htmlFor={`${uid}-cadence`}>Ziel-Rhythmus</label>
        <select id={`${uid}-cadence`} aria-label="Ziel-Rhythmus" value={value.cadence}
          onChange={(e) => up("cadence", e.target.value as GoalCadence)}>
          {CADENCES.map((c) => <option key={c} value={c}>{CADENCE_LABEL[c]}</option>)}
        </select>
      </div>
      {/* Der Server wertet die Schwelle als Bestehens-Prozentsatz des Abschlusstests aus (Standard 80). */}
      <div className="field" style={{ maxWidth: 140 }}>
        <label htmlFor={`${uid}-pass`}>Bestehen ab %</label>
        <input id={`${uid}-pass`} aria-label="Bestehen ab Prozent" type="number" min={1} max={100}
          placeholder="80" value={value.goalThreshold} onChange={(e) => up("goalThreshold", e.target.value)} />
      </div>
      <div className="field" style={{ maxWidth: 130 }}>
        <label htmlFor={`${uid}-count`}>Inhalte</label>
        <input id={`${uid}-count`} aria-label="Anzahl Inhalte" type="number" min={1} placeholder="alle"
          value={value.itemCount} onChange={(e) => up("itemCount", e.target.value)} />
      </div>
      <div className="field" style={{ maxWidth: 180 }}>
        <label htmlFor={`${uid}-order`}>Reihenfolge</label>
        <select id={`${uid}-order`} aria-label="Reihenfolge" value={value.orderStrategy}
          onChange={(e) => up("orderStrategy", e.target.value as PracticeOrder)}>
          {ORDERS.map((o) => <option key={o} value={o}>{ORDER_LABEL[o]}</option>)}
        </select>
      </div>
      <div className="field" style={{ maxWidth: 140 }}>
        <label htmlFor={`${uid}-points`}>Punkte (Ziel erreicht)</label>
        <input id={`${uid}-points`} aria-label="Punkte bei erreichtem Ziel" type="number" min={0}
          value={value.pointsGoalMet} onChange={(e) => up("pointsGoalMet", Number(e.target.value))} />
      </div>
      {/* Der „Stick": verpasste Pflicht kostet Münzen. 0 = reine Belohnung; Schulden sind erlaubt. */}
      <div className="field" style={{ maxWidth: 150 }}>
        <label htmlFor={`${uid}-penalty`}>Münz-Malus (versäumt)</label>
        <input id={`${uid}-penalty`} aria-label="Münz-Malus bei gerissener Pflicht" type="number" min={0}
          value={value.penaltyCoins} onChange={(e) => up("penaltyCoins", Number(e.target.value))} />
      </div>
      {/* Leer lassen = Bonus-Vorschlag der Übung übernehmen (Platzhalter „erbt"). */}
      <div className="field" style={{ maxWidth: 130 }}>
        <label htmlFor={`${uid}-new`}>Punkte neuer Inhalt</label>
        <input id={`${uid}-new`} aria-label="Punkte für neuen Inhalt" type="number" min={0} placeholder="erbt"
          value={value.newContentPoints} onChange={(e) => up("newContentPoints", e.target.value)} />
      </div>
      <div className="field" style={{ maxWidth: 150 }}>
        <label htmlFor={`${uid}-combo-n`}>Combo alle … Treffer</label>
        <input id={`${uid}-combo-n`} aria-label="Combo-Schwelle" type="number" min={0} placeholder="erbt"
          value={value.comboThreshold} onChange={(e) => up("comboThreshold", e.target.value)} />
      </div>
      <div className="field" style={{ maxWidth: 140 }}>
        <label htmlFor={`${uid}-combo-p`}>Combo-Bonuspunkte</label>
        <input id={`${uid}-combo-p`} aria-label="Combo-Bonuspunkte" type="number" min={0} placeholder="erbt"
          value={value.comboBonusPoints} onChange={(e) => up("comboBonusPoints", e.target.value)} />
      </div>
      <label className="checkline">
        <input type="checkbox" checked={value.useLeitner} onChange={(e) => up("useLeitner", e.target.checked)} /> Leitner-Kasten
      </label>
      <label className="checkline">
        <input type="checkbox" checked={value.requireTypedTest} onChange={(e) => up("requireTypedTest", e.target.checked)} /> nur getippte Tests
      </label>
    </div>
  );
}

/** Katalog-Übung über eine Filterleiste finden und als Position hinzufügen (übrige Werte erbt die Position). */
function AddPosition({ planId, onAdded, onError }: { planId: number; onAdded: () => void; onError: (t: string) => void }) {
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);
  const types = useExerciseTypes();
  const typeLabel = (t: string) => types?.label(t) ?? t;
  const [filter, setFilter] = useState<ExerciseFilter>({});
  // Die Gesamtzahl wird mitgeführt: der Server liefert nur eine Seite, und eine still gekappte
  // Auswahlliste liest sich wie „mehr gibt es nicht".
  const exercises = useAsync<Paged<ExerciseSummary>>(() => api.searchExercises(filter),
    [filter.subjectId, filter.chapterId, filter.grade, filter.schoolType, filter.categoryId, filter.type, filter.search]);
  const [exerciseId, setExerciseId] = useState<number | "">("");
  const [settings, setSettings] = useState<PositionSettings>(defaultSettings());
  const [busy, setBusy] = useState(false);

  async function add(e: React.FormEvent) {
    e.preventDefault();
    if (exerciseId === "") { onError("Bitte eine Übung aus der Liste wählen."); return; }
    setBusy(true);
    const dto: CreatePositionDto = { exerciseId: Number(exerciseId), ...settingsToDto(settings) };
    try {
      await api.addPosition(planId, dto);
      setExerciseId("");
      onAdded();
    } catch (err) { onError(errorMessage(err)); }
    finally { setBusy(false); }
  }

  const results = exercises.data?.items ?? [];

  return (
    <form className="card" onSubmit={add} style={{ display: "flex", flexDirection: "column", gap: 10 }}>
      {/* Umfangreiche Filterleiste statt flachem Pulldown: Fach/Kapitel/Klasse/Schulart/Typ/Art/Freitext. */}
      <ExerciseFilterBar value={filter} onChange={setFilter} subjects={subjects.data ?? []} />

      <div className="field">
        <label>Übung aus dem Katalog <span className="muted">({results.length} Treffer)</span></label>
        {exercises.loading ? <div className="loading">Lade…</div> : (
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
                    {ex.categoryName && <span className="muted"> · {ex.categoryName}</span>}
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
        <button type="submit" className="btn inline-btn" style={{ width: "auto", marginLeft: "auto" }} disabled={busy || exerciseId === ""}>
          {busy ? "…" : "+ Position hinzufügen"}
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
function PositionRow({ planId, pos, onChanged, flash }: {
  planId: number; pos: PositionResponse; onChanged: () => void; flash: Flash;
}) {
  const types = useExerciseTypes();
  const typeLabel = (t: string) => types?.label(t) ?? t;
  const [editing, setEditing] = useState(false);
  const [showReport, setShowReport] = useState(false);
  const [settings, setSettings] = useState<PositionSettings>(() => settingsFrom(pos));
  const [busy, setBusy] = useState(false);

  function cancel() {
    setSettings(settingsFrom(pos));
    setEditing(false);
  }

  async function save() {
    setBusy(true);
    try {
      await api.updatePosition(planId, pos.id, settingsToDto(settings));
      setEditing(false);
      onChanged();
      flash(true, "Position gespeichert.");
    } catch (err) { flash(false, errorMessage(err)); }
    finally { setBusy(false); }
  }

  async function remove() {
    if (!confirmAction("Diese Position wirklich entfernen? Fortschritt und Auswertung dieser Position gehen verloren.")) return;
    setBusy(true);
    try { await api.deletePosition(planId, pos.id); onChanged(); flash(true, "Position entfernt."); }
    catch (err) { flash(false, errorMessage(err)); setBusy(false); }
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
          </td>
          <td>{pos.useLeitner ? <span className="pill lime">an · max {pos.maxBox}</span> : <span className="muted">aus</span>}</td>
          <td className="row" style={{ gap: 6, justifyContent: "flex-end" }}>
            <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
              aria-expanded={showReport} onClick={() => setShowReport((s) => !s)}>📊 Report</button>
            <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={() => setEditing(true)}>Bearbeiten</button>
            <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={remove}>Entfernen</button>
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
        <button type="button" className="btn inline-btn" style={{ width: "auto" }} disabled={busy} onClick={save}>OK</button>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={cancel}>Abbrechen</button>
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
