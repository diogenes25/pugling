import { Link, useParams } from "react-router-dom";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import type { PlanResponse, ProgressResponse } from "../lib/types";

export function VaterPlanDetail() {
  const { planId } = useParams();
  const id = Number(planId);
  const plan = useAsync<PlanResponse>(() => api.plan(id), [id]);
  const prog = useAsync<ProgressResponse>(() => api.progress(id), [id]);

  if (plan.loading) return <div className="loading">Lade Plan…</div>;
  if (plan.error || !plan.data) return <div className="banner err">{plan.error ?? "Plan nicht gefunden."}</div>;
  const p = plan.data;

  return (
    <>
      <div className="row">
        <h2 className="h-section">{p.title}</h2>
        <span className="pill" style={{ marginLeft: "auto" }}>{p.method}{p.useLeitner ? " · Leitner" : ""}</span>
        {p.active ? <span className="pill lime">aktiv</span> : <span className="pill">inaktiv</span>}
      </div>
      <p className="muted">Kind #{p.childId} · {p.startDate} – {p.endDate} · {p.newItemsPerLesson} neue/Tag · {p.dailyMinutesRequired} min · bestehen ab {p.dailyTestPassPercent}%
        {p.comboThreshold > 0 && p.comboBonusPoints > 0
          ? ` · Combo alle ${p.comboThreshold} → +${p.comboBonusPoints}`
          : " · Combo aus"}</p>

      {prog.data && (
        <section className="vater-grid">
          <div className="card"><div className="muted">Punkte gesamt</div><div className="h-section">{prog.data.totalPoints}</div></div>
          <div className="card"><div className="muted">Tage geschafft</div><div className="h-section">{prog.data.daysComplete} / {prog.data.totalDays}</div></div>
          <div className="card"><div className="muted">Aktuelle Streak</div><div className="h-section">{prog.data.currentStreak} 🔥</div></div>
        </section>
      )}

      <section>
        <h3 className="h-section">Inhalte & Leitner-Boxen</h3>
        <div style={{ overflowX: "auto" }}>
          <table className="table">
            <thead><tr><th>#</th><th>Wort</th><th>Übersetzung</th><th className="num">Box</th><th>Fällig</th></tr></thead>
            <tbody>
              {p.items.map((it) => (
                <tr key={it.id}>
                  <td className="num">{it.order + 1}</td><td>{it.label}</td><td>{it.detail}</td>
                  <td className="num">{it.box}</td><td className="muted">{it.dueOn ?? "–"}</td>
                </tr>
              ))}
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
