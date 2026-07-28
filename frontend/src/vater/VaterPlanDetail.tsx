import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { confirmAction } from "../lib/ui";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import { PlanPositions } from "./PlanPositions";
import type { ChildResponse, PlanResponse, ProgressResponse, UpdatePlanDto } from "../lib/types";

export function VaterPlanDetail() {
  const { planId } = useParams();
  const nav = useNavigate();
  const id = Number(planId);
  const plan = useAsync<PlanResponse>(() => api.plan(id), [id]);
  const prog = useAsync<ProgressResponse>(() => api.overviewProgress(id), [id]);
  // Der Plan trägt nur die childId; für den Namen (und den Weg zum Kind) braucht es den Datensatz.
  const child = useAsync<ChildResponse | null>(
    () => (plan.data ? api.child(plan.data.childId) : Promise.resolve(null)), [plan.data?.childId]);

  const [editing, setEditing] = useState(false);
  const action = useAction();

  async function remove() {
    if (!confirmAction("Diesen Lehrplan wirklich löschen? Positionen, Fortschritt und Testversuche gehen verloren. Die Übungen im Katalog bleiben erhalten.")) return;
    if (await action.run(() => api.deletePlan(id))) nav("/vater");
  }

  if (plan.loading) return <div className="loading">Lade Plan…</div>;
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
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={action.busy}
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
        <Link to={`/vater/kind/${p.childId}`}>{child.data?.name ?? `Kind #${p.childId}`}</Link> · {p.startDate} – {p.endDate}
      </p>
      <p className="sub" style={{ marginTop: -4 }}>
        Nur der <strong>aktive</strong> Plan ist für dein Kind spielbar. Aktivierst du diesen, werden andere Pläne desselben Kindes automatisch deaktiviert – so kann es sich keine leichte Übung zum Punktesammeln aussuchen.
      </p>
      {p.description && <p style={{ marginTop: -6 }}>{p.description}</p>}

      <StatusBanner message={action.message} style={{ marginTop: 0 }} />

      {editing && <PlanEditForm plan={p} busy={action.busy}
        onSave={(dto) => mutate(() => api.updatePlan(id, dto), "Änderungen gespeichert.")} />}

      {prog.data && (
        <section className="vater-grid">
          <div className="card"><div className="muted">Punkte gesamt</div><div className="h-section">{prog.data.totalPoints}</div></div>
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
              <thead><tr><th>Tag</th><th className="num">Ziele</th><th className="num">Punkte</th><th>Status</th></tr></thead>
              <tbody>
                {prog.data.days.map((d) => (
                  <tr key={d.day}>
                    <td>{d.day}</td>
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

/** Formular zum Bearbeiten des Plan-Containers: Titel, Beschreibung, Laufzeit und Kind-Zuweisung. */
function PlanEditForm({ plan, busy, onSave }: { plan: PlanResponse; busy: boolean; onSave: (dto: UpdatePlanDto) => void }) {
  const children = useAsync<ChildResponse[]>(() => api.children(), []);
  const [form, setForm] = useState({
    title: plan.title, description: plan.description ?? "",
    startDate: plan.startDate, endDate: plan.endDate, childId: plan.childId,
  });

  function submit(e: React.FormEvent) {
    e.preventDefault();
    const dto: UpdatePlanDto = {};
    if (form.title.trim() !== "" && form.title !== plan.title) dto.title = form.title.trim();
    // Beschreibung immer mitschicken (auch Leeren erlaubt → null), wenn geändert.
    if ((form.description ?? "") !== (plan.description ?? "")) dto.description = form.description.trim() || null;
    if (form.startDate && form.startDate !== plan.startDate) dto.startDate = form.startDate;
    if (form.endDate && form.endDate !== plan.endDate) dto.endDate = form.endDate;
    if (form.childId !== plan.childId) dto.childId = form.childId;
    onSave(dto);
  }

  return (
    <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 10 }}>
      <div className="form-grid" style={{ alignItems: "end" }}>
        <div className="field" style={{ minWidth: 220 }}><label htmlFor="plan-edit-title">Titel</label>
          <input id="plan-edit-title" value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} /></div>
        <div className="field"><label htmlFor="plan-edit-child">Kind</label>
          <select id="plan-edit-child" value={form.childId} onChange={(e) => setForm((f) => ({ ...f, childId: Number(e.target.value) }))}>
            {children.data?.map((c) => <option key={c.id} value={c.id}>{c.name} (#{c.id})</option>)}
          </select>
        </div>
        <div className="field"><label htmlFor="plan-edit-start">Start</label>
          <input id="plan-edit-start" type="date" value={form.startDate} onChange={(e) => setForm((f) => ({ ...f, startDate: e.target.value }))} /></div>
        <div className="field"><label htmlFor="plan-edit-end">Ende</label>
          <input id="plan-edit-end" type="date" value={form.endDate} onChange={(e) => setForm((f) => ({ ...f, endDate: e.target.value }))} /></div>
      </div>
      <div className="field"><label htmlFor="plan-edit-desc">Beschreibung <span className="muted">(optional)</span></label>
        <textarea id="plan-edit-desc" value={form.description} rows={2} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))} /></div>
      <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={busy}>Speichern</button>
    </form>
  );
}
