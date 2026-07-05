import { useId, useState } from "react";
import { api, errorMessage } from "../lib/api";
import { confirmAction } from "../lib/ui";
import { useAsync } from "../lib/useAsync";
import type {
  CreatePositionDto, ExerciseSummary, GoalCadence, ItemReport, PositionReport, PositionResponse, SubjectResponse,
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

const TYPE_LABEL: Record<string, string> = {
  Vocabulary: "Vokabeln", Arithmetic: "Rechnen", Cloze: "Lückentext",
  Matching: "Zuordnung", List: "Liste", Birkenbihl: "Birkenbihl",
};
const typeLabel = (t: string) => TYPE_LABEL[t] ?? t;

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

/** Katalog-Übung über eine Filterleiste finden und als Position hinzufügen (übrige Werte erbt die Position). */
function AddPosition({ planId, onAdded, onError }: { planId: number; onAdded: () => void; onError: (t: string) => void }) {
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);
  const [filter, setFilter] = useState<ExerciseFilter>({});
  const exercises = useAsync<ExerciseSummary[]>(() => api.searchExercises(filter).then((r) => r.items),
    [filter.subjectId, filter.chapterId, filter.grade, filter.schoolType, filter.categoryId, filter.type, filter.search]);
  const [exerciseId, setExerciseId] = useState<number | "">("");
  const [cadence, setCadence] = useState<GoalCadence>("Daily");
  const [pointsGoalMet, setPointsGoalMet] = useState(20);
  const [useLeitner, setUseLeitner] = useState(false);
  const [requireTyped, setRequireTyped] = useState(false);
  const [busy, setBusy] = useState(false);

  async function add(e: React.FormEvent) {
    e.preventDefault();
    if (exerciseId === "") { onError("Bitte eine Übung aus der Liste wählen."); return; }
    setBusy(true);
    const dto: CreatePositionDto = {
      exerciseId: Number(exerciseId), cadence, pointsGoalMet, useLeitner, requireTypedTest: requireTyped,
    };
    try {
      await api.addPosition(planId, dto);
      setExerciseId("");
      onAdded();
    } catch (err) { onError(errorMessage(err)); }
    finally { setBusy(false); }
  }

  const results = exercises.data ?? [];

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
                  onChange={() => { setExerciseId(ex.id); setUseLeitner(ex.defaultUseLeitner); setRequireTyped(ex.defaultRequireTypedTest); }}
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
      </div>

      <div className="row" style={{ gap: 12, alignItems: "flex-end", flexWrap: "wrap" }}>
        <div className="field" style={{ maxWidth: 180 }}>
          <label>Ziel-Rhythmus</label>
          <select aria-label="Ziel-Rhythmus" value={cadence} onChange={(e) => setCadence(e.target.value as GoalCadence)}>
            {CADENCES.map((c) => <option key={c} value={c}>{CADENCE_LABEL[c]}</option>)}
          </select>
        </div>
        <div className="field" style={{ maxWidth: 140 }}>
          <label>Punkte (Ziel erreicht)</label>
          <input aria-label="Punkte bei erreichtem Ziel" type="number" min={0} value={pointsGoalMet} onChange={(e) => setPointsGoalMet(Number(e.target.value))} />
        </div>
        <label className="checkline"><input type="checkbox" checked={useLeitner} onChange={(e) => setUseLeitner(e.target.checked)} /> Leitner-Kasten</label>
        <label className="checkline"><input type="checkbox" checked={requireTyped} onChange={(e) => setRequireTyped(e.target.checked)} /> nur getippte Tests</label>
        <button type="submit" className="btn inline-btn" style={{ width: "auto", marginLeft: "auto" }} disabled={busy || exerciseId === ""}>
          {busy ? "…" : "+ Position hinzufügen"}
        </button>
      </div>
      <p className="muted" style={{ margin: 0, fontSize: 12 }}>
        Nicht gesetzte Werte (Combo, Tempo-Bonus, neue Inhalte …) erbt die Position aus dem Bonus-Vorschlag der Übung.
      </p>
    </form>
  );
}

/** Eine Positionszeile mit Inline-Bearbeiten (Ziel/Punkte/Leitner) und Entfernen (409-bewusst). */
function PositionRow({ planId, pos, onChanged, flash }: {
  planId: number; pos: PositionResponse; onChanged: () => void; flash: Flash;
}) {
  const uid = useId();
  const [editing, setEditing] = useState(false);
  const [showReport, setShowReport] = useState(false);
  const [cadence, setCadence] = useState<GoalCadence>(pos.cadence);
  const [goalThreshold, setGoalThreshold] = useState(pos.goalThreshold?.toString() ?? "");
  const [pointsGoalMet, setPointsGoalMet] = useState(pos.pointsGoalMet);
  const [newContentPoints, setNewContentPoints] = useState(pos.newContentPoints);
  const [useLeitner, setUseLeitner] = useState(pos.useLeitner);
  const [requireTyped, setRequireTyped] = useState(pos.requireTypedTest);
  const [busy, setBusy] = useState(false);

  function cancel() {
    setCadence(pos.cadence);
    setGoalThreshold(pos.goalThreshold?.toString() ?? "");
    setPointsGoalMet(pos.pointsGoalMet);
    setNewContentPoints(pos.newContentPoints);
    setUseLeitner(pos.useLeitner);
    setRequireTyped(pos.requireTypedTest);
    setEditing(false);
  }

  async function save() {
    setBusy(true);
    const threshold = goalThreshold.trim() === "" ? null : Number(goalThreshold);
    try {
      await api.updatePosition(planId, pos.id, {
        cadence, goalThreshold: threshold, pointsGoalMet, newContentPoints,
        useLeitner, requireTypedTest: requireTyped,
      });
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
            {pos.goalThreshold != null && <span className="muted"> · Schwelle {pos.goalThreshold}</span>}
            {pos.requireTypedTest && <span className="muted"> · getippt</span>}
          </td>
          <td className="num">Ziel {pos.pointsGoalMet} · neu {pos.newContentPoints}
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
        <div className="row" style={{ gap: 10, alignItems: "flex-end", flexWrap: "wrap" }}>
          <div className="field" style={{ maxWidth: 160 }}><label>Ziel-Rhythmus</label>
            <select aria-label="Ziel-Rhythmus" value={cadence} onChange={(e) => setCadence(e.target.value as GoalCadence)}>
              {CADENCES.map((c) => <option key={c} value={c}>{CADENCE_LABEL[c]}</option>)}
            </select>
          </div>
          <div className="field" style={{ maxWidth: 110 }}><label htmlFor={`${uid}-schwelle`}>Schwelle</label>
            <input id={`${uid}-schwelle`} type="number" min={0} value={goalThreshold} placeholder="Std." onChange={(e) => setGoalThreshold(e.target.value)} /></div>
          <div className="field" style={{ maxWidth: 110 }}><label>Punkte Ziel</label>
            <input aria-label="Punkte bei erreichtem Ziel" type="number" min={0} value={pointsGoalMet} onChange={(e) => setPointsGoalMet(Number(e.target.value))} /></div>
          <div className="field" style={{ maxWidth: 110 }}><label>Punkte neu</label>
            <input aria-label="Punkte für neuen Inhalt" type="number" min={0} value={newContentPoints} onChange={(e) => setNewContentPoints(Number(e.target.value))} /></div>
          <label className="checkline"><input type="checkbox" checked={useLeitner} onChange={(e) => setUseLeitner(e.target.checked)} /> Leitner</label>
          <label className="checkline"><input type="checkbox" checked={requireTyped} onChange={(e) => setRequireTyped(e.target.checked)} /> getippt</label>
        </div>
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

/** Farbcodierte „sitzt"-Ampel: neu (grau) · unsicher (magenta) · in Arbeit (neutral) · sitzt sicher (lime). */
function MasteryPill({ it, maxBox }: { it: ItemReport; maxBox: number }) {
  if (!it.introduced) return <span className="pill">neu</span>;
  if (it.box >= maxBox) return <span className="pill lime">sitzt · {it.masteryPercent}%</span>;
  if (it.masteryPercent < 50) return <span className="pill mag">{it.masteryPercent}% · Box {it.box}</span>;
  return <span className="pill">{it.masteryPercent}% · Box {it.box}</span>;
}
