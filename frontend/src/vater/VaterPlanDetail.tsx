import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import type { PlanResponse, ProgressResponse, UpdatePlanDto } from "../lib/types";

export function VaterPlanDetail() {
  const { planId } = useParams();
  const id = Number(planId);
  const plan = useAsync<PlanResponse>(() => api.plan(id), [id]);
  const prog = useAsync<ProgressResponse>(() => api.progress(id), [id]);

  const [editing, setEditing] = useState(false);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [busy, setBusy] = useState(false);
  const [newKey, setNewKey] = useState("");

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
        <span className="pill" style={{ marginLeft: "auto" }}>{p.method}{p.useLeitner ? " · Leitner" : ""}</span>
        {p.active ? <span className="pill lime">aktiv</span> : <span className="pill">inaktiv</span>}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy}
          onClick={() => mutate(() => api.updatePlan(id, { active: !p.active }), p.active ? "Plan deaktiviert." : "Plan aktiviert.")}>
          {p.active ? "Deaktivieren" : "Aktivieren"}
        </button>
        <button type="button" className="btn inline-btn" style={{ width: "auto" }} onClick={() => setEditing((v) => !v)}>
          {editing ? "Schließen" : "Bearbeiten"}
        </button>
      </div>
      <p className="muted">Kind #{p.childId} · {p.startDate} – {p.endDate} · {p.newItemsPerLesson} neue/Tag · {p.dailyMinutesRequired} min · bestehen ab {p.dailyTestPassPercent}%
        {p.comboThreshold > 0 && p.comboBonusPoints > 0
          ? ` · Combo alle ${p.comboThreshold} → +${p.comboBonusPoints}`
          : " · Combo aus"}</p>

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

      <section>
        <h3 className="h-section">Inhalte & Leitner-Boxen</h3>
        {editing && (
          <form className="row" style={{ marginBottom: 8 }} onSubmit={(e) => {
            e.preventDefault();
            const key = newKey.trim();
            if (!key) return;
            mutate(() => api.addPlanItem(id, key), `Inhalt „${key}" hinzugefügt.`).then(() => setNewKey(""));
          }}>
            <input value={newKey} onChange={(e) => setNewKey(e.target.value)} placeholder="Inhalts-Key (z. B. en_house_de_haus)" style={{ maxWidth: 340 }} />
            <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={busy || !newKey.trim()}>Inhalt hinzufügen</button>
          </form>
        )}
        <div style={{ overflowX: "auto" }}>
          <table className="table">
            <thead><tr><th>#</th><th>Wort</th><th>Übersetzung</th><th className="num">Box</th><th>Fällig</th>{editing && <th></th>}</tr></thead>
            <tbody>
              {p.items.map((it) => (
                <tr key={it.id}>
                  <td className="num">{it.order + 1}</td><td>{it.label}</td><td>{it.detail}</td>
                  <td className="num">{it.box}</td><td className="muted">{it.dueOn ?? "–"}</td>
                  {editing && <td>
                    <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy}
                      onClick={() => mutate(() => api.removePlanItem(id, it.id), `„${it.label}" entfernt.`)}>Entfernen</button>
                  </td>}
                </tr>
              ))}
              {p.items.length === 0 && <tr><td colSpan={editing ? 6 : 5} className="muted">Keine Inhalte im Plan.</td></tr>}
            </tbody>
          </table>
        </div>
      </section>

      {prog.data && (
        <section>
          <h3 className="h-section">Tagesverlauf</h3>
          <div style={{ overflowX: "auto" }}>
            <table className="table">
              <thead><tr><th>Tag</th><th className="num">Minuten</th><th>Test</th><th className="num">Punkte</th><th>Status</th></tr></thead>
              <tbody>
                {prog.data.days.map((d) => (
                  <tr key={d.day}>
                    <td>{d.day}</td>
                    <td className="num">{d.minutesPracticed}{d.minutesMet ? " ✓" : ""}</td>
                    <td>{d.testPassed ? `bestanden ${d.bestScorePercent}%` : d.testAttempts > 0 ? `${d.bestScorePercent}%` : "–"}</td>
                    <td className="num">{d.pointsAwarded}</td>
                    <td>{d.dayComplete ? <span className="pill lime">komplett</span> : <span className="pill">offen</span>}</td>
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

/** Formular zum partiellen Ändern/Verlängern eines Lehrplans. */
function PlanEditForm({ plan, busy, onSave }: { plan: PlanResponse; busy: boolean; onSave: (dto: UpdatePlanDto) => void }) {
  // Felder als Strings halten: Ein geleertes Zahlenfeld darf NICHT als 0 gespeichert werden
  // (Number("")===0 hätte z.B. die Bestehensgrenze still auf 0 % gesetzt). Beim Speichern gehen
  // nur nicht-leere, gültige Werte in das PATCH-DTO – leer = unverändert.
  const [form, setForm] = useState({
    title: plan.title,
    endDate: plan.endDate,
    newItemsPerLesson: String(plan.newItemsPerLesson),
    dailyMinutesRequired: String(plan.dailyMinutesRequired),
    dailyTestPassPercent: String(plan.dailyTestPassPercent),
  });

  function up<K extends keyof typeof form>(k: K, v: string) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  /** Ganzzahl aus dem Feld, sofern nicht leer und ≥ min – sonst undefined (Feld wird ausgelassen). */
  function intOrOmit(raw: string, min: number): number | undefined {
    const n = Number(raw);
    return raw.trim() !== "" && Number.isFinite(n) && n >= min ? Math.trunc(n) : undefined;
  }

  function submit(e: React.FormEvent) {
    e.preventDefault();
    const dto: UpdatePlanDto = {};
    if (form.title.trim() !== "" && form.title !== plan.title) dto.title = form.title.trim();
    if (form.endDate) dto.endDate = form.endDate;
    const nip = intOrOmit(form.newItemsPerLesson, 1);
    const dmr = intOrOmit(form.dailyMinutesRequired, 0);
    const dtp = intOrOmit(form.dailyTestPassPercent, 0);
    if (nip !== undefined) dto.newItemsPerLesson = nip;
    if (dmr !== undefined) dto.dailyMinutesRequired = dmr;
    if (dtp !== undefined) dto.dailyTestPassPercent = Math.min(100, dtp);
    onSave(dto);
  }

  return (
    <form className="form-grid" style={{ alignItems: "end" }} onSubmit={submit}>
      <div className="field" style={{ minWidth: 220 }}><label>Titel</label>
        <input title="Titel" value={form.title} onChange={(e) => up("title", e.target.value)} /></div>
      <div className="field"><label>Ende (verlängern)</label>
        <input title="Enddatum" type="date" value={form.endDate} onChange={(e) => up("endDate", e.target.value)} /></div>
      <div className="field" style={{ maxWidth: 130 }}><label>Neue/Tag</label>
        <input title="Neue Inhalte pro Tag" type="number" min={1} value={form.newItemsPerLesson} onChange={(e) => up("newItemsPerLesson", e.target.value)} /></div>
      <div className="field" style={{ maxWidth: 130 }}><label>Minuten/Tag</label>
        <input title="Übungsminuten pro Tag" type="number" min={0} value={form.dailyMinutesRequired} onChange={(e) => up("dailyMinutesRequired", e.target.value)} /></div>
      <div className="field" style={{ maxWidth: 130 }}><label>Bestehen ab %</label>
        <input title="Bestehensgrenze in Prozent" type="number" min={0} max={100} value={form.dailyTestPassPercent} onChange={(e) => up("dailyTestPassPercent", e.target.value)} /></div>
      <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={busy}>Speichern</button>
    </form>
  );
}
