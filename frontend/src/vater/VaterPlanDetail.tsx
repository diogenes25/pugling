import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ReferenceSelect } from "../components/ReferenceSelect";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { confirmAction } from "../lib/ui";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import { PlanPositions } from "./PlanPositions";
import type { ChildResponse, PlanResponse, ProgressResponse, SubjectResponse, UpdatePlanDto } from "../lib/types";
import { ListenAusfall } from "../components/ListenAusfall";
import { FieldLabel } from "../components/InfoHint";
import { Pflicht } from "../components/Pflicht";
import { PlanUnitSelect } from "./PlanUnitSelect";
import { formatDate, formatDateRange } from "../lib/datum";

export function VaterPlanDetail() {
  const { planId } = useParams();
  const nav = useNavigate();
  const id = Number(planId);
  const plan = useAsync<PlanResponse>(() => api.plan(id), [id]);
  const prog = useAsync<ProgressResponse>(() => api.overviewProgress(id), [id]);
  // Der Plan trägt nur die childId; für den Namen (und den Weg zum Kind) braucht es den Datensatz.
  const child = useAsync<ChildResponse | null>(
    () => (plan.data ? api.child(plan.data.childId) : Promise.resolve(null)), [plan.data?.childId]);
  // Für den Fachnamen im Kopf (`PlanResponse` trägt nur die Id).
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);

  const [editing, setEditing] = useState(false);
  const action = useAction();

  async function remove() {
    if (!confirmAction("Diesen Lehrplan wirklich löschen? Seine Positionen gehen verloren, die Übungen im Katalog bleiben erhalten. Hat das Kind schon damit gelernt, lässt er sich nicht löschen – dann „Deaktivieren“.")) return;
    if (await action.run(() => api.deletePlan(id))) nav("/vater");
  }

  if (plan.loading && plan.data === null) return <div className="loading">Lade Plan…</div>;
  if (plan.error || !plan.data) return <div className="banner err">{plan.error ?? "Plan nicht gefunden."}</div>;
  const p = plan.data;

  // Die Meldung bleibt stehen, bis die nächste Aktion startet (`run` räumt sie) – wie überall sonst im
  // Vater-Web. Vorher verschwand sie hier nach 2,5 s per `setTimeout`, der beim Verlassen der Seite weiterlief.
  async function mutate(fn: () => Promise<unknown>, okText: string) {
    if (!await action.run(fn, okText)) return;
    plan.reload();
    prog.reload();
  }

  return (
    <>
      <div className="row">
        <h2 className="h-section">{p.title}</h2>
        <span className="pill" style={{ marginLeft: "auto" }}>{p.positionCount} Übungen</span>
        {p.active ? <span className="pill lime">aktiv</span> : <span className="pill">inaktiv</span>}
        {/* Server-autoritativ: aktiv, aber heute außerhalb der Laufzeit → für das Kind nicht spielbar. */}
        {p.active && !p.isPlayable && <span className="pill" title="Aktiv, aber heute außerhalb der Laufzeit">nicht in Laufzeit</span>}
        {/* Ohne Fach weist der Server das Aktivieren ab (B-539); Abschalten bleibt frei. Der Hinweis darunter nennt den Weg. */}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
          disabled={action.busy || (!p.active && p.subjectId == null)}
          onClick={() => mutate(() => api.updatePlan(id, { active: !p.active }), p.active ? "Plan deaktiviert." : "Plan aktiviert.")}>
          {p.active ? "Deaktivieren" : "Aktivieren"}
        </button>
        <button type="button" className="btn inline-btn" style={{ width: "auto" }} onClick={() => setEditing((v) => !v)}>
          {editing ? "Schließen" : "Bearbeiten"}
        </button>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto", color: "var(--mag, #c0392b)" }} disabled={action.busy} onClick={remove}>
          Löschen
        </button>
      </div>
      <p className="muted">
        <Link to={`/vater/kind/${p.childId}`}>{child.data?.name ?? `Kind #${p.childId}`}</Link>
        {" · "}{p.subjectId == null ? "ohne Fach" : subjects.data?.find((s) => s.id === p.subjectId)?.name ?? `Fach #${p.subjectId}`}
        {p.seriesUnitLabel && <> · {p.seriesUnitLabel}</>}
        {" · "}{formatDateRange(p.startDate, p.endDate)}
      </p>
      {p.subjectId == null && (
        <div className="banner" role="note">
          Dieser Plan hat noch kein Fach. Wähle es unter „Bearbeiten“ – erst dann lässt er sich wieder aktivieren oder ändern.
        </div>
      )}
      <p className="sub" style={{ marginTop: -4 }}>
        Je Fach ist <strong>ein</strong> aktiver Plan für dein Kind spielbar. Aktivierst du diesen, wird der aktive Plan desselben Fachs automatisch deaktiviert – Pläne anderer Fächer laufen weiter, und innerhalb eines Fachs kann sich dein Kind keinen leichten Plan zum Punktesammeln aussuchen.
      </p>
      {p.description && <p style={{ marginTop: -6 }}>{p.description}</p>}

      <StatusBanner message={action.message} style={{ marginTop: 0 }} />

      {editing && <PlanEditForm plan={p} childName={child.data?.name} busy={action.busy}
        onSave={(dto) => mutate(() => api.updatePlan(id, dto), "Änderungen gespeichert.")} />}

      {prog.data && (
        <section className="vater-grid">
          <div className="card"><div className="muted">Zielbonus gesamt</div><div className="h-section">{prog.data.totalPoints}</div></div>
          <div className="card"><div className="muted">Tage geschafft</div><div className="h-section">{prog.data.daysComplete} / {prog.data.totalDays}</div></div>
          <div className="card"><div className="muted">Aktuelle Streak</div><div className="h-section">{prog.data.currentStreak} 🔥</div></div>
        </section>
      )}

      <PlanPositions planId={id} />

      {prog.data && (
        <section>
          <h3 className="h-section">Tagesverlauf</h3>
          <div style={{ overflowX: "auto" }}>
            <table className="table">
              <thead><tr><th>Tag</th><th className="num">Ziele</th><th className="num">Zielbonus</th><th>Status</th></tr></thead>
              <tbody>
                {prog.data.days.map((d) => (
                  <tr key={d.day}>
                    <td>{formatDate(d.day)}</td>
                    <td className="num">{d.goalsMet} / {d.goalsTotal}</td>
                    <td className="num">{d.pointsAwarded}</td>
                    <td>{d.dutyDone ? <span className="pill lime">komplett</span> : <span className="pill">offen</span>}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}

      {/* Zurück zur **Plan-Liste**, nicht zur Betreuen-Startseite: wer einen Plan verlässt, will zum
          nächsten Plan – und dort liegen sie seit der Perspektiven-Trennung. */}
      <Link to="/vater/plaene" className="btn ghost inline-btn" style={{ width: "auto", alignSelf: "flex-start", textDecoration: "none", textAlign: "center" }}>← Alle Lehrpläne</Link>
    </>
  );
}

/** Formular zum Bearbeiten des Plan-Containers: Titel, Fach, Unit, Beschreibung, Laufzeit und Kind-Zuweisung. */
export function PlanEditForm({ plan, childName, busy, onSave }: {
  plan: PlanResponse;
  /** Der Name des geladenen Kindes – aus dem Datensatz, den die Seite ohnehin lädt (`PlanResponse` trägt keinen). */
  childName: string | undefined;
  busy: boolean;
  onSave: (dto: UpdatePlanDto) => void;
}) {
  const children = useAsync<ChildResponse[]>(() => api.children(), []);
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);
  const [form, setForm] = useState({
    title: plan.title, description: plan.description ?? "",
    startDate: plan.startDate, endDate: plan.endDate, childId: plan.childId,
    subjectId: (plan.subjectId ?? "") as number | "", seriesUnitId: (plan.seriesUnitId ?? "") as number | "",
  });

  function submit(e: React.FormEvent) {
    e.preventDefault();
    // Pflichtfeld: ohne Fach ist Speichern gesperrt, die Zeile kehrt nur stumm zurück (B-233).
    if (form.subjectId === "") return;
    const dto: UpdatePlanDto = {};
    if (form.subjectId !== plan.subjectId) dto.subjectId = form.subjectId;
    // Die Unit: gesetzt schickt sie mit, geleert braucht den Schalter – `null` hieße „nicht angegeben" (PATCH).
    if (form.seriesUnitId !== (plan.seriesUnitId ?? "")) {
      if (form.seriesUnitId === "") dto.clearSeriesUnit = true;
      else dto.seriesUnitId = form.seriesUnitId;
    }
    if (form.title.trim() !== "" && form.title !== plan.title) dto.title = form.title.trim();
    // Geändert → mitschicken, auch leer: `""` leert am Server, `null` hieße beim PATCH „unverändert" (B-791).
    if ((form.description ?? "") !== (plan.description ?? "")) dto.description = form.description.trim();
    if (form.startDate && form.startDate !== plan.startDate) dto.startDate = form.startDate;
    if (form.endDate && form.endDate !== plan.endDate) dto.endDate = form.endDate;
    if (form.childId !== plan.childId) dto.childId = form.childId;
    onSave(dto);
  }

  return (
    <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 10 }}>
      {/* Der Ausfall der Kinderliste war unsichtbar – das Feld stand leer, für einen Plan, der ein Kind hat
          (B-194, derselbe Fall wie B-172s Kriterium 5). Seit B-183 mit „Erneut laden". */}
      <ListenAusfall was="Die Kinder" fehler={children.error} laedt={children.loading} onRetry={children.reload} />
      <div className="form-grid" style={{ alignItems: "end" }}>
        <div className="field" style={{ minWidth: 220 }}><label htmlFor="plan-edit-title">Titel</label>
          <input id="plan-edit-title" value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} /></div>
        <div className="field"><label htmlFor="plan-edit-child">Kind</label>
          {/* Der achte Fall der Regel aus B-172: Fehlt das geladene Kind in der Liste, zeigt das Feld es als
              „(nicht geladen)" statt still auf die erste Option zu fallen. Kein `emptyLabel`: ein Plan ohne
              Kind ist kein wählbarer Zustand. `laedt`, weil die Liste HIER lädt, nicht auf Seitenebene – bei
              jedem Öffnen stünde sonst für die Dauer des Abrufs die Beschriftung des Ausfalls (B-229);
              `showPlaceholder`, nicht `loading`: nach einem Fehlschlag soll es „(nicht geladen)" heißen. */}
          <ReferenceSelect id="plan-edit-child" value={String(form.childId)}
            onChange={(v) => setForm((f) => ({ ...f, childId: Number(v) }))}
            loadedId={plan.childId} loadedName={childName} laedt={children.showPlaceholder}
            options={(children.data ?? []).map((c) => ({ value: String(c.id), label: `${c.name} (#${c.id})` }))}
          />
        </div>
        <div className="field"><FieldLabel htmlFor="plan-edit-subject" topic="planSubject">Fach <Pflicht /></FieldLabel>
          <select id="plan-edit-subject" value={form.subjectId} onChange={(e) => {
            const subjectId = e.target.value === "" ? "" : Number(e.target.value);
            // Die Unit gehört zum alten Fach – stehen gelassen wiese der Server die Kombination ab.
            setForm((f) => ({ ...f, subjectId, seriesUnitId: "" }));
          }}>
            <option value="">– Fach wählen –</option>
            {subjects.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
          <ListenAusfall was="Die Fächer" fehler={subjects.error} laedt={subjects.loading} onRetry={subjects.reload} />
        </div>
        <div className="field"><FieldLabel htmlFor="plan-edit-unit" topic="planUnit">Unit <span className="muted">(optional)</span></FieldLabel>
          <PlanUnitSelect id="plan-edit-unit" subjectId={form.subjectId} value={form.seriesUnitId}
            currentLabel={form.seriesUnitId === plan.seriesUnitId ? plan.seriesUnitLabel : null}
            onChange={(seriesUnitId) => setForm((f) => ({ ...f, seriesUnitId }))} />
        </div>
        <div className="field"><label htmlFor="plan-edit-start">Start</label>
          <input id="plan-edit-start" type="date" value={form.startDate} onChange={(e) => setForm((f) => ({ ...f, startDate: e.target.value }))} /></div>
        <div className="field"><label htmlFor="plan-edit-end">Ende</label>
          <input id="plan-edit-end" type="date" value={form.endDate} onChange={(e) => setForm((f) => ({ ...f, endDate: e.target.value }))} /></div>
      </div>
      <div className="field"><label htmlFor="plan-edit-desc">Beschreibung <span className="muted">(optional)</span></label>
        <textarea id="plan-edit-desc" value={form.description} rows={2} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))} /></div>
      <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={busy || form.subjectId === ""}>Speichern</button>
    </form>
  );
}
