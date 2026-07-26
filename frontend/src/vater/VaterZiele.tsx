import { useId, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api, errorMessage } from "../lib/api";
import { confirmAction } from "../lib/ui";
import { useAsync } from "../lib/useAsync";
import type {
  ChapterResponse, ChildResponse, CreateKeyResultRequest, ExerciseSummary, GoalStatus, KeyResult,
  KeyResultMetric, LearnGoal, LearnGoalMetric, Objective, ObjectiveKind, Paged, SubjectResponse,
} from "../lib/types";

/**
 * Die zwei Ziel-Ebenen über dem Lernstand.
 *
 * * **Lernziele** sind einzelne Messlatten auf einem Stück Katalog („in Unit 1 sitzen 80 %").
 * * **Objectives** sind die Klammer darüber: ein benanntes Vorhaben mit Etappen (Key Results), einem
 *   Termin und einer Belohnung.
 *
 * Beide sind vom Pflichtziel einer Lehrplan-Position klar getrennt: hier gibt es **keinen Malus** und keinen
 * Tagesrhythmus. Ein Ziel ist eine Aussage über den Stand, keine Aufgabe für heute. Es wird bei jeder
 * Abfrage neu ausgewertet – „open/achieved/overdue" ist nie ein gespeicherter, veralteter Wert.
 */

const GOAL_METRICS: { value: LearnGoalMetric; label: string; hint: string; max?: boolean }[] = [
  { value: "AvgMastery", label: "Ø Beherrschung", hint: "Durchschnitt über die begonnenen Wörter, in Prozent" },
  { value: "Coverage", label: "Abdeckung", hint: "Anteil der überhaupt begonnenen Wörter, in Prozent" },
  { value: "MasteredPercent", label: "Anteil „sitzt sicher“", hint: "Anteil der Wörter in der höchsten Box, in Prozent" },
  { value: "MaxWeakItems", label: "Höchstens N schwache Wörter", hint: "Anzahl – hier ist der Zielwert eine Obergrenze", max: true },
];

const KR_METRICS: { value: KeyResultMetric; label: string; hint: string; max?: boolean }[] = [
  { value: "AvgMastery", label: "Ø Beherrschung", hint: "Durchschnitt über die begonnenen Wörter, in Prozent" },
  { value: "MasteredPercent", label: "Anteil „sitzt sicher“", hint: "Anteil der Wörter in der höchsten Box, in Prozent" },
  { value: "MaxWeakItems", label: "Höchstens N schwache Wörter", hint: "Anzahl – Obergrenze", max: true },
  { value: "ClassTestGrade", label: "Klassenarbeits-Note", hint: "Note als Zahl ×10 (20 = mindestens 2,0) – Obergrenze", max: true },
];

const goalMetric = (m: LearnGoalMetric) => GOAL_METRICS.find((x) => x.value === m);
const krMetric = (m: KeyResultMetric) => KR_METRICS.find((x) => x.value === m);

/**
 * Zielwert menschenlesbar. `ClassTestGrade` ist im Vertrag Note ×10 – ungerechnet gelesen ergäbe „Ziel 20"
 * eine unsinnige Aussage.
 */
function valueLabel(metric: LearnGoalMetric | KeyResultMetric, value: number): string {
  if (metric === "ClassTestGrade") return (value / 10).toFixed(1);
  if (metric === "MaxWeakItems") return String(value);
  return `${value} %`;
}

/** „mindestens" bzw. „höchstens" – ohne diese Richtung liest man jeden Balken falsch. */
function directionLabel(metric: LearnGoalMetric | KeyResultMetric): string {
  return metric === "MaxWeakItems" || metric === "ClassTestGrade" ? "höchstens" : "mindestens";
}

function StatusPill({ status }: { status: GoalStatus }) {
  if (status === "achieved") return <span className="pill lime">erreicht</span>;
  if (status === "overdue") return <span className="pill mag">Termin verpasst</span>;
  return <span className="pill">offen</span>;
}

/** Fortschrittsbalken; der Server liefert `progressPercent` bereits richtungsgerecht (0–100). */
function ProgressBar({ percent }: { percent: number }) {
  const clamped = Math.max(0, Math.min(100, percent));
  return (
    <span className="row" style={{ gap: 6, alignItems: "center", minWidth: 140 }}>
      <span aria-hidden="true" style={{
        flex: 1, height: 8, borderRadius: 4, background: "var(--stroke)", overflow: "hidden", display: "block",
      }}>
        <span style={{
          display: "block", height: "100%", width: `${clamped}%`,
          background: clamped >= 100 ? "var(--lime, #8cdc78)" : "var(--cyan, #26d9ff)",
        }} />
      </span>
      <span className="muted tabnum" style={{ fontSize: 12 }}>{clamped} %</span>
    </span>
  );
}

export function VaterZiele() {
  const childId = Number(useParams().childId);
  const child = useAsync<ChildResponse>(() => api.child(childId), [childId]);
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);

  return (
    <>
      <div className="row" style={{ marginBottom: 8 }}>
        <h2 className="h-section">Ziele{child.data ? ` · ${child.data.name}` : ""}</h2>
        <Link to={`/vater/kind/${childId}`} className="btn ghost small"
          style={{ marginLeft: "auto", textDecoration: "none" }}>← Kind</Link>
      </div>
      <p className="muted" style={{ marginTop: -4 }}>
        Ziele messen den <strong>Stand</strong>, nicht die Pflicht von heute: kein Malus, kein Tagesrhythmus.
        Der Fortschritt wird bei jedem Aufruf neu aus dem Lernstand berechnet – siehe{" "}
        <Link to={`/vater/kind/${childId}/lernstand`}>Lernstand</Link>.
      </p>

      <LearnGoals childId={childId} subjects={subjects.data ?? []} />
      <Objectives childId={childId} subjects={subjects.data ?? []} />
    </>
  );
}

// ─── Scope-Wähler (Fach → optional Kapitel → optional Übung) ──────────────────

interface Scope { subjectId: number | ""; chapterId: number | ""; exerciseId: number | ""; }

const emptyScope: Scope = { subjectId: "", chapterId: "", exerciseId: "" };

/**
 * Der Geltungsbereich eines Ziels. Das Fach ist Pflicht, Kapitel und Übung engen weiter ein – je enger, je
 * konkreter die Aussage („in dieser einen Übung" statt „im ganzen Fach").
 */
function ScopePicker({ value, onChange, subjects }: {
  value: Scope; onChange: (s: Scope) => void; subjects: SubjectResponse[];
}) {
  // Eigene Id-Basis je Instanz: der Wähler steht ggf. mehrfach im DOM (Lernziel + Etappe zugleich).
  const uid = useId();
  const chapters = useAsync<ChapterResponse[]>(
    () => (value.subjectId === "" ? Promise.resolve([]) : api.chapters(Number(value.subjectId))), [value.subjectId]);
  const exercises = useAsync<Paged<ExerciseSummary>>(
    () => (value.subjectId === ""
      ? Promise.resolve({ items: [], total: 0 })
      : api.searchExercises({
        subjectId: Number(value.subjectId),
        chapterId: value.chapterId === "" ? undefined : Number(value.chapterId),
        type: "Vocabulary", take: 100,
      })),
    [value.subjectId, value.chapterId]);

  return (
    <>
      <div className="field">
        <label htmlFor={`${uid}-subject`}>Fach</label>
        <select id={`${uid}-subject`} value={value.subjectId}
          onChange={(e) => onChange({ ...emptyScope, subjectId: e.target.value === "" ? "" : Number(e.target.value) })}>
          <option value="">– wählen –</option>
          {subjects.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
        </select>
      </div>
      <div className="field">
        <label htmlFor={`${uid}-chapter`}>Kapitel <span className="muted">(optional)</span></label>
        <select id={`${uid}-chapter`} value={value.chapterId} disabled={value.subjectId === ""}
          onChange={(e) => onChange({ ...value, chapterId: e.target.value === "" ? "" : Number(e.target.value), exerciseId: "" })}>
          <option value="">ganzes Fach</option>
          {chapters.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
      </div>
      <div className="field">
        <label htmlFor={`${uid}-exercise`}>Übung <span className="muted">(optional)</span></label>
        <select id={`${uid}-exercise`} value={value.exerciseId} disabled={value.subjectId === ""}
          onChange={(e) => onChange({ ...value, exerciseId: e.target.value === "" ? "" : Number(e.target.value) })}>
          <option value="">alle Übungen</option>
          {exercises.data?.items.map((e) => <option key={e.id} value={e.id}>{e.title}</option>)}
        </select>
      </div>
    </>
  );
}

const scopeToDto = (s: Scope) => ({
  subjectId: Number(s.subjectId),
  chapterId: s.chapterId === "" ? null : Number(s.chapterId),
  exerciseId: s.exerciseId === "" ? null : Number(s.exerciseId),
});

// ─── Lernziele ────────────────────────────────────────────────────────────────

function LearnGoals({ childId, subjects }: { childId: number; subjects: SubjectResponse[] }) {
  const goals = useAsync<Paged<LearnGoal>>(() => api.learnGoals(childId), [childId]);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);

  async function act(fn: () => Promise<unknown>, okText: string) {
    setMsg(null);
    try { await fn(); goals.reload(); setMsg({ ok: true, text: okText }); }
    catch (err) { setMsg({ ok: false, text: errorMessage(err) }); }
  }

  return (
    <section>
      <h3 className="h-section">Lernziele {goals.data ? `(${goals.data.total})` : ""}</h3>

      <NewLearnGoal childId={childId} subjects={subjects}
        onCreated={() => { goals.reload(); setMsg({ ok: true, text: "Lernziel angelegt." }); }}
        onError={(t) => setMsg({ ok: false, text: t })} />

      {msg && <div className={`banner ${msg.ok ? "ok" : "err"}`} style={{ marginTop: 10 }} role="status" aria-live="polite">{msg.text}</div>}

      {goals.loading ? <div className="loading">Lade…</div> : goals.error ? <div className="banner err">{goals.error}</div> : (
        <div style={{ overflowX: "auto", marginTop: 10 }}>
          <table className="table">
            <thead><tr>
              <th>Ziel</th><th>Bereich</th><th>Messlatte</th><th>Stand</th><th>Termin</th><th>Status</th><th />
            </tr></thead>
            <tbody>
              {goals.data?.items.map((g) => (
                <LearnGoalRow key={g.id} goal={g}
                  onSave={(target) => act(() => api.updateLearnGoal(childId, g.id, { targetValue: target }), "Zielwert geändert.")}
                  onDelete={() => {
                    if (!confirmAction(`Lernziel „${g.title ?? g.scope}" löschen?`)) return;
                    act(() => api.deleteLearnGoal(childId, g.id), "Lernziel gelöscht.");
                  }} />
              ))}
              {goals.data?.items.length === 0 && (
                <tr><td colSpan={7} className="muted">Noch keine Lernziele – lege oben eines an.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

function LearnGoalRow({ goal, onSave, onDelete }: {
  goal: LearnGoal; onSave: (target: number) => void; onDelete: () => void;
}) {
  const [target, setTarget] = useState(String(goal.targetValue));
  const meta = goalMetric(goal.metric);
  const dirty = target.trim() !== "" && Number(target) !== goal.targetValue;

  return (
    <tr>
      <td>{goal.title ?? <span className="muted">(ohne Titel)</span>}</td>
      <td className="muted">{goal.scope}</td>
      <td>
        {meta?.label ?? goal.metric}
        <div className="muted" style={{ fontSize: 12 }}>{directionLabel(goal.metric)} {valueLabel(goal.metric, goal.targetValue)}</div>
      </td>
      <td>
        <ProgressBar percent={goal.progressPercent} />
        <div className="muted" style={{ fontSize: 12 }}>aktuell {valueLabel(goal.metric, goal.currentValue)}</div>
      </td>
      <td className="muted">{goal.dueDate ?? "—"}</td>
      <td><StatusPill status={goal.status} /></td>
      <td style={{ whiteSpace: "nowrap", textAlign: "right" }}>
        <input aria-label={`Zielwert für ${goal.title ?? goal.scope}`} type="number" min={0} value={target}
          onChange={(e) => setTarget(e.target.value)} style={{ width: 70 }} />{" "}
        {dirty && <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
          onClick={() => onSave(Number(target))}>OK</button>}{" "}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={onDelete}>Löschen</button>
      </td>
    </tr>
  );
}

function NewLearnGoal({ childId, subjects, onCreated, onError }: {
  childId: number; subjects: SubjectResponse[]; onCreated: () => void; onError: (t: string) => void;
}) {
  const [scope, setScope] = useState<Scope>(emptyScope);
  const [metric, setMetric] = useState<LearnGoalMetric>("AvgMastery");
  const [targetValue, setTargetValue] = useState(80);
  const [dueDate, setDueDate] = useState("");
  const [title, setTitle] = useState("");
  const [busy, setBusy] = useState(false);

  const meta = goalMetric(metric);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (scope.subjectId === "") { onError("Bitte ein Fach wählen."); return; }
    setBusy(true);
    try {
      await api.createLearnGoal(childId, {
        ...scopeToDto(scope), metric, targetValue,
        dueDate: dueDate || null, title: title.trim() || null,
      });
      setTitle("");
      onCreated();
    } catch (err) { onError(errorMessage(err)); }
    finally { setBusy(false); }
  }

  return (
    <form className="card" onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 10 }}>
      <div className="form-grid" style={{ alignItems: "end" }}>
        <ScopePicker value={scope} onChange={setScope} subjects={subjects} />
        <div className="field">
          <label htmlFor="lg-metric">Messlatte</label>
          <select id="lg-metric" value={metric}
            onChange={(e) => {
              const next = e.target.value as LearnGoalMetric;
              setMetric(next);
              // Der sinnvolle Startwert hängt an der Richtung: Prozent-Ziele hoch, Obergrenzen niedrig.
              setTargetValue(next === "MaxWeakItems" ? 3 : 80);
            }}>
            {GOAL_METRICS.map((m) => <option key={m.value} value={m.value}>{m.label}</option>)}
          </select>
        </div>
        <div className="field">
          <label htmlFor="lg-target">{meta?.max ? "Obergrenze" : "Zielwert"}</label>
          <input id="lg-target" type="number" min={0} value={targetValue} onChange={(e) => setTargetValue(Number(e.target.value))} />
        </div>
        <div className="field">
          <label htmlFor="lg-due">Stichtag <span className="muted">(optional)</span></label>
          <input id="lg-due" type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="lg-title">Titel <span className="muted">(optional)</span></label>
          <input id="lg-title" value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Unit 1 sitzt" />
        </div>
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={busy}>{busy ? "…" : "Lernziel anlegen"}</button>
      </div>
      <p className="sub" style={{ margin: 0 }}>
        {meta?.hint} {meta?.max && <strong>Der Zielwert ist hier eine Obergrenze.</strong>}
        {" "}Ohne Stichtag bleibt das Ziel offen, bis es erreicht ist – es kann nie „verpasst" werden.
      </p>
    </form>
  );
}

// ─── Objectives (Klammer mit Etappen) ─────────────────────────────────────────

function Objectives({ childId, subjects }: { childId: number; subjects: SubjectResponse[] }) {
  const objectives = useAsync<Paged<Objective>>(() => api.objectives(childId), [childId]);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);

  async function act(fn: () => Promise<unknown>, okText: string) {
    setMsg(null);
    try { await fn(); objectives.reload(); setMsg({ ok: true, text: okText }); }
    catch (err) { setMsg({ ok: false, text: errorMessage(err) }); }
  }

  return (
    <section>
      <h3 className="h-section">Große Ziele {objectives.data ? `(${objectives.data.total})` : ""}</h3>
      <p className="muted" style={{ marginTop: 0 }}>
        Ein benanntes Vorhaben mit Etappen. <strong>Verbindlich</strong> zahlt 🪙 Münzen (real einlösbar),
        <strong> Dehnungsziel</strong> zahlt 💎 Gems (Skins).
      </p>

      <NewObjective childId={childId} subjects={subjects}
        onCreated={() => { objectives.reload(); setMsg({ ok: true, text: "Großes Ziel angelegt." }); }}
        onError={(t) => setMsg({ ok: false, text: t })} />

      {msg && <div className={`banner ${msg.ok ? "ok" : "err"}`} style={{ marginTop: 10 }} role="status" aria-live="polite">{msg.text}</div>}

      {objectives.loading ? <div className="loading">Lade…</div> : objectives.error ? <div className="banner err">{objectives.error}</div> : (
        <div style={{ display: "flex", flexDirection: "column", gap: 10, marginTop: 10 }}>
          {objectives.data?.items.map((o) => (
            <ObjectiveCard key={o.id} objective={o} childId={childId} subjects={subjects}
              onChanged={objectives.reload}
              onToggleActive={() => act(() => api.updateObjective(childId, o.id, { active: !o.active }),
                o.active ? "Ziel stillgelegt." : "Ziel aktiviert.")}
              onDelete={() => {
                if (!confirmAction(`„${o.title}" samt Etappen löschen?`)) return;
                act(() => api.deleteObjective(childId, o.id), "Ziel gelöscht.");
              }} />
          ))}
          {objectives.data?.items.length === 0 && <p className="muted">Noch keine großen Ziele.</p>}
        </div>
      )}
    </section>
  );
}

function ObjectiveCard({ objective: o, childId, subjects, onChanged, onToggleActive, onDelete }: {
  objective: Objective; childId: number; subjects: SubjectResponse[];
  onChanged: () => void; onToggleActive: () => void; onDelete: () => void;
}) {
  const [addingKr, setAddingKr] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const currency = o.kind === "Committed" ? "🪙" : "💎";

  async function act(fn: () => Promise<unknown>) {
    setErr(null);
    try { await fn(); onChanged(); } catch (e) { setErr(errorMessage(e)); }
  }

  return (
    <div className="card" style={{ opacity: o.active ? 1 : 0.6 }}>
      <div className="row" style={{ alignItems: "center", gap: 8, flexWrap: "wrap" }}>
        <b>{o.title}</b>
        <span className="pill">{o.kind === "Committed" ? "verbindlich" : "Dehnungsziel"}</span>
        <StatusPill status={o.status} />
        {o.rewarded && <span className="pill lime">belohnt</span>}
        {!o.active && <span className="pill">stillgelegt</span>}
        <span style={{ marginLeft: "auto" }} />
        <span className="muted">{o.achievedCount}/{o.totalCount} Etappen</span>
        <ProgressBar percent={o.progressPercent} />
      </div>
      {o.motivation && <p className="muted" style={{ margin: "6px 0 0" }}>{o.motivation}</p>}
      <p className="muted" style={{ margin: "4px 0 0", fontSize: 13 }}>
        {o.start ?? "—"} bis {o.dueDate ?? "offen"} · Belohnung {currency} {o.rewardOnComplete} beim Abschluss
        {o.rewardPerKeyResult > 0 && ` · ${currency} ${o.rewardPerKeyResult} je Etappe`}
      </p>

      <table className="table" style={{ marginTop: 8 }}>
        <thead><tr><th>Etappe</th><th>Bereich</th><th>Messlatte</th><th>Stand</th><th>Status</th><th /></tr></thead>
        <tbody>
          {o.keyResults.map((kr) => (
            <KeyResultRow key={kr.id} kr={kr}
              onSave={(target) => act(() => api.updateKeyResult(childId, o.id, kr.id, { targetValue: target }))}
              onDelete={() => {
                if (!confirmAction(`Etappe „${kr.title ?? kr.scope}" entfernen?`)) return;
                act(() => api.deleteKeyResult(childId, o.id, kr.id));
              }} />
          ))}
          {o.keyResults.length === 0 && (
            <tr><td colSpan={6} className="muted">Noch keine Etappen – ohne sie kann das Ziel nicht erreicht werden.</td></tr>
          )}
        </tbody>
      </table>

      {err && <div className="banner err" style={{ marginTop: 8 }}>{err}</div>}

      <div className="row" style={{ gap: 8, marginTop: 8 }}>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
          aria-expanded={addingKr} onClick={() => setAddingKr((v) => !v)}>
          {addingKr ? "Schließen" : "+ Etappe"}
        </button>
        <span style={{ marginLeft: "auto" }} />
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={onToggleActive}>
          {o.active ? "Stilllegen" : "Aktivieren"}
        </button>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={onDelete}>Löschen</button>
      </div>

      {addingKr && (
        <KeyResultForm subjects={subjects}
          onSubmit={async (dto) => {
            await act(() => api.createKeyResult(childId, o.id, dto));
            setAddingKr(false);
          }} />
      )}
    </div>
  );
}

function KeyResultRow({ kr, onSave, onDelete }: { kr: KeyResult; onSave: (target: number) => void; onDelete: () => void }) {
  const [target, setTarget] = useState(String(kr.targetValue));
  const meta = krMetric(kr.metric);
  const dirty = target.trim() !== "" && Number(target) !== kr.targetValue;

  return (
    <tr>
      <td>{kr.title ?? <span className="muted">(ohne Titel)</span>}</td>
      <td className="muted">{kr.scope}</td>
      <td>
        {meta?.label ?? kr.metric}
        <div className="muted" style={{ fontSize: 12 }}>{directionLabel(kr.metric)} {valueLabel(kr.metric, kr.targetValue)}</div>
      </td>
      <td>
        <ProgressBar percent={kr.progressPercent} />
        <div className="muted" style={{ fontSize: 12 }}>aktuell {valueLabel(kr.metric, kr.currentValue)}</div>
      </td>
      <td><StatusPill status={kr.status} /></td>
      <td style={{ whiteSpace: "nowrap", textAlign: "right" }}>
        <input aria-label={`Zielwert für ${kr.title ?? kr.scope}`} type="number" min={0} value={target}
          onChange={(e) => setTarget(e.target.value)} style={{ width: 70 }} />{" "}
        {dirty && <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
          onClick={() => onSave(Number(target))}>OK</button>}{" "}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={onDelete}>Entfernen</button>
      </td>
    </tr>
  );
}

/** Formular einer Etappe – beim Anlegen des Ziels (erste Etappe) und später zum Nachtragen dasselbe. */
function KeyResultForm({ subjects, onSubmit }: {
  subjects: SubjectResponse[];
  onSubmit: (dto: CreateKeyResultRequest) => void | Promise<void>;
}) {
  const uid = useId();
  const [scope, setScope] = useState<Scope>(emptyScope);
  const [metric, setMetric] = useState<KeyResultMetric>("AvgMastery");
  const [targetValue, setTargetValue] = useState(80);
  const [title, setTitle] = useState("");
  const meta = krMetric(metric);

  return (
    <div style={{ marginTop: 10, paddingTop: 10, borderTop: "1px solid var(--stroke)" }}>
      <div className="form-grid" style={{ alignItems: "end" }}>
        <ScopePicker value={scope} onChange={setScope} subjects={subjects} />
        <div className="field">
          <label htmlFor={`${uid}-kr-metric`}>Messlatte</label>
          <select id={`${uid}-kr-metric`} value={metric}
            onChange={(e) => {
              const next = e.target.value as KeyResultMetric;
              setMetric(next);
              setTargetValue(next === "MaxWeakItems" ? 3 : next === "ClassTestGrade" ? 20 : 80);
            }}>
            {KR_METRICS.map((m) => <option key={m.value} value={m.value}>{m.label}</option>)}
          </select>
        </div>
        <div className="field">
          <label htmlFor={`${uid}-kr-target`}>{meta?.max ? "Obergrenze" : "Zielwert"}</label>
          <input id={`${uid}-kr-target`} type="number" min={0} value={targetValue} onChange={(e) => setTargetValue(Number(e.target.value))} />
        </div>
        <div className="field">
          <label htmlFor={`${uid}-kr-title`}>Titel <span className="muted">(optional)</span></label>
          <input id={`${uid}-kr-title`} value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Unit 1 sicher" />
        </div>
        <button type="button" className="btn inline-btn" style={{ width: "auto" }}
          disabled={scope.subjectId === ""}
          onClick={() => onSubmit({ ...scopeToDto(scope), metric, targetValue, title: title.trim() || null })}>
          Etappe übernehmen
        </button>
      </div>
      <p className="sub" style={{ margin: "6px 0 0" }}>
        {meta?.hint}
        {metric === "ClassTestGrade" && " Gemessen wird die beste Note im gewählten Fach."}
      </p>
    </div>
  );
}

function NewObjective({ childId, subjects, onCreated, onError }: {
  childId: number; subjects: SubjectResponse[]; onCreated: () => void; onError: (t: string) => void;
}) {
  const [open, setOpen] = useState(false);
  const [title, setTitle] = useState("");
  const [motivation, setMotivation] = useState("");
  const [kind, setKind] = useState<ObjectiveKind>("Committed");
  const [start, setStart] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [rewardOnComplete, setRewardOnComplete] = useState(100);
  const [rewardPerKeyResult, setRewardPerKeyResult] = useState(20);
  // Etappen werden inline gesammelt: ein Ziel ohne Etappe ist nicht erreichbar, deshalb gleich hier.
  const [keyResults, setKeyResults] = useState<CreateKeyResultRequest[]>([]);
  const [busy, setBusy] = useState(false);

  async function submit() {
    if (!title.trim()) { onError("Bitte einen Titel angeben."); return; }
    setBusy(true);
    try {
      await api.createObjective(childId, {
        title: title.trim(), motivation: motivation.trim() || null, kind,
        start: start || null, dueDate: dueDate || null,
        rewardOnComplete, rewardPerKeyResult,
        keyResults: keyResults.length > 0 ? keyResults : undefined,
      });
      setTitle(""); setMotivation(""); setKeyResults([]); setOpen(false);
      onCreated();
    } catch (err) { onError(errorMessage(err)); }
    finally { setBusy(false); }
  }

  if (!open) {
    return (
      <button type="button" className="btn inline-btn" style={{ width: "auto" }} onClick={() => setOpen(true)}>
        + Großes Ziel anlegen
      </button>
    );
  }

  const currency = kind === "Committed" ? "🪙 Münzen" : "💎 Gems";

  return (
    <div className="card" style={{ display: "flex", flexDirection: "column", gap: 10 }}>
      <div className="form-grid" style={{ alignItems: "end" }}>
        <div className="field"><label htmlFor="ob-title">Titel</label>
          <input id="ob-title" value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Englisch aufholen bis Ostern" /></div>
        <div className="field"><label htmlFor="ob-kind">Art</label>
          <select id="ob-kind" value={kind} onChange={(e) => setKind(e.target.value as ObjectiveKind)}>
            <option value="Committed">verbindlich (zahlt Münzen)</option>
            <option value="Stretch">Dehnungsziel (zahlt Gems)</option>
          </select></div>
        <div className="field"><label htmlFor="ob-start">Start <span className="muted">(optional)</span></label>
          <input id="ob-start" type="date" value={start} onChange={(e) => setStart(e.target.value)} /></div>
        <div className="field"><label htmlFor="ob-due">Termin <span className="muted">(optional)</span></label>
          <input id="ob-due" type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} /></div>
        <div className="field"><label htmlFor="ob-reward">Belohnung bei Abschluss</label>
          <input id="ob-reward" type="number" min={0} value={rewardOnComplete} onChange={(e) => setRewardOnComplete(Number(e.target.value))} /></div>
        <div className="field"><label htmlFor="ob-reward-kr">Belohnung je Etappe</label>
          <input id="ob-reward-kr" type="number" min={0} value={rewardPerKeyResult} onChange={(e) => setRewardPerKeyResult(Number(e.target.value))} /></div>
      </div>
      <div className="field">
        <label htmlFor="ob-motivation">Warum <span className="muted">(optional – das Kind liest es)</span></label>
        <textarea id="ob-motivation" rows={2} value={motivation} onChange={(e) => setMotivation(e.target.value)}
          placeholder="Damit die nächste Arbeit keine 5 wird." />
      </div>
      <p className="sub" style={{ margin: 0 }}>Ausgezahlt wird in {currency}.</p>

      <h5 style={{ margin: "6px 0 0" }}>Etappen ({keyResults.length})</h5>
      {keyResults.length > 0 && (
        <ul className="muted" style={{ margin: 0 }}>
          {keyResults.map((kr, i) => (
            <li key={i}>
              {kr.title ?? "(ohne Titel)"} · {krMetric(kr.metric)?.label} {directionLabel(kr.metric)}{" "}
              {valueLabel(kr.metric, kr.targetValue)}{" "}
              <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
                onClick={() => setKeyResults((cur) => cur.filter((_, idx) => idx !== i))}>entfernen</button>
            </li>
          ))}
        </ul>
      )}
      <KeyResultForm subjects={subjects} onSubmit={(dto) => setKeyResults((cur) => [...cur, dto])} />

      <div className="row" style={{ gap: 8 }}>
        <button type="button" className="btn inline-btn" style={{ width: "auto" }} disabled={busy} onClick={submit}>
          {busy ? "…" : "Ziel anlegen"}
        </button>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={() => setOpen(false)}>Abbrechen</button>
      </div>
    </div>
  );
}
