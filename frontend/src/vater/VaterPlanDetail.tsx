import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { PlanPositions } from "./PlanPositions";
import type { PlanResponse, ProgressResponse, UpdatePlanDto } from "../lib/types";

export function VaterPlanDetail() {
  const { planId } = useParams();
  const id = Number(planId);
  const plan = useAsync<PlanResponse>(() => api.plan(id), [id]);
  const prog = useAsync<ProgressResponse>(() => api.overviewProgress(id), [id]);

  const [editing, setEditing] = useState(false);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  if (plan.loading) return <div className="loading">Lade Plan…</div>;
  if (plan.error || !plan.data) return <div className="banner err">{plan.error ?? "Plan nicht gefunden."}</div>;
  const p = plan.data;

  function flash(ok: boolean, text: string) {
    setMsg({ ok, text });
    setTimeout(() => setMsg(null), 2500);
  }

  async function mutate(fn: () => Promise<unknown>, okText: string) {
    setBusy(true);
    try {
      await fn();
      plan.reload();
      prog.reload();
      flash(true, okText);
    } catch (err) {
      flash(false, errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <div className="row">
        <h2 className="h-section">{p.title}</h2>
        <span className="pill" style={{ marginLeft: "auto" }}>{p.positionCount} Übungen</span>
        {p.active ? <span className="pill lime">aktiv</span> : <span className="pill">inaktiv</span>}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy}
          onClick={() => mutate(() => api.updatePlan(id, { active: !p.active }), p.active ? "Plan deaktiviert." : "Plan aktiviert.")}>
          {p.active ? "Deaktivieren" : "Aktivieren"}
        </button>
        <button type="button" className="btn inline-btn" style={{ width: "auto" }} onClick={() => setEditing((v) => !v)}>
          {editing ? "Schließen" : "Bearbeiten"}
        </button>
      </div>
      <p className="muted">Kind #{p.childId} · {p.startDate} – {p.endDate}</p>

      {msg && <div className={`banner ${msg.ok ? "ok" : "err"}`}>{msg.text}</div>}

      {editing && <PlanEditForm plan={p} busy={busy}
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

      <Link to="/vater" className="btn ghost inline-btn" style={{ width: "auto", alignSelf: "flex-start", textDecoration: "none", textAlign: "center" }}>← Zur Übersicht</Link>
    </>
  );
}

/** Formular zum Umbenennen/Verlängern eines Lehrplans (Inhalte laufen über Positionen). */
function PlanEditForm({ plan, busy, onSave }: { plan: PlanResponse; busy: boolean; onSave: (dto: UpdatePlanDto) => void }) {
  const [form, setForm] = useState({ title: plan.title, endDate: plan.endDate });

  function submit(e: React.FormEvent) {
    e.preventDefault();
    const dto: UpdatePlanDto = {};
    if (form.title.trim() !== "" && form.title !== plan.title) dto.title = form.title.trim();
    if (form.endDate) dto.endDate = form.endDate;
    onSave(dto);
  }

  return (
    <form className="form-grid" style={{ alignItems: "end" }} onSubmit={submit}>
      <div className="field" style={{ minWidth: 220 }}><label>Titel</label>
        <input title="Titel" value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} /></div>
      <div className="field"><label>Ende (verlängern)</label>
        <input title="Enddatum" type="date" value={form.endDate} onChange={(e) => setForm((f) => ({ ...f, endDate: e.target.value }))} /></div>
      <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={busy}>Speichern</button>
    </form>
  );
}
