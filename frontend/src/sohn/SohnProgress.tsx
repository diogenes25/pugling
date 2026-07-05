import { useNavigate } from "react-router-dom";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useSohn } from "./SohnApp";
import { BadgesGallery } from "./GamificationPanels";
import type { OverviewResponse, ProgressResponse } from "../lib/types";

export function SohnProgress() {
  const { planId } = useSohn();
  const nav = useNavigate();

  const overview = useAsync<OverviewResponse | null>(() => (planId ? api.overview(planId) : Promise.resolve(null)), [planId]);
  const prog = useAsync<ProgressResponse | null>(() => (planId ? api.overviewProgress(planId) : Promise.resolve(null)), [planId]);

  if (!planId) return <div className="sohn-body"><div className="loading">Wähle zuerst eine Mission auf der Basis.</div>
    <button type="button" className="btn ghost" onClick={() => nav("/sohn")}>Zur Basis</button></div>;
  if (overview.loading || prog.loading) return <div className="sohn-body"><div className="loading">Lade Fortschritt…</div></div>;
  if (!overview.data || !prog.data) return <div className="sohn-body"><div className="error-box">{overview.error ?? prog.error ?? "Fehler"}</div></div>;

  const o = overview.data;
  const pr = prog.data;
  const last7 = pr.days.slice(-7);

  return (
    <div className="sohn-body">
      <div className="row">
        <span className="screen-title" style={{ margin: 0 }}>Trophäenweg</span>
        <span className="chip" style={{ marginLeft: "auto" }}>🏆<b className="tabnum">{pr.totalPoints}</b></span>
      </div>
      <p className="sub">{o.title} · {pr.daysComplete}/{pr.totalDays} Tage geschafft · Streak {pr.currentStreak} 🔥</p>

      <div className="card">
        <div className="row" style={{ marginBottom: 8 }}>
          <b style={{ fontSize: 13 }}>🎯 Meine Übungen</b>
          <span className="sub" style={{ marginLeft: "auto" }}>{o.today.goalsMet}/{o.today.goalsTotal} heute</span>
        </div>
        {o.today.positions.map((pos) => (
          <div className="row" key={pos.positionId} style={{ padding: "4px 0" }}>
            <span>{pos.goalMet ? "✅" : pos.cadence === "None" ? "•" : "⬜"}</span>
            <b>{pos.exerciseTitle}</b>
            <span className="sub" style={{ marginLeft: "auto" }}>
              {pos.cadence === "Daily" ? "Tagesziel" : pos.cadence === "Weekly" ? "Wochenziel" : "frei"}
            </span>
          </div>
        ))}
        {o.today.positions.length === 0 && <p className="sub">Noch keine Übungen im Plan.</p>}
      </div>

      <div className="card">
        <div className="row" style={{ marginBottom: 8 }}>
          <b style={{ fontSize: 13 }}>🔥 Letzte Tage</b>
          <span className="sub" style={{ marginLeft: "auto" }}>{last7.length} Tage</span>
        </div>
        <div className="calendar">
          {last7.map((d) => (
            <i key={d.day} className={d.dutyDone ? "t" : d.goalsMet > 0 ? "d" : ""} title={d.day} />
          ))}
        </div>
        <div className="sub" style={{ marginTop: 8, fontSize: 11 }}>
          <span style={{ color: "var(--lime)" }}>■</span> teils geschafft &nbsp; <span style={{ color: "var(--gold)" }}>■</span> Tag komplett
        </div>
      </div>

      <BadgesGallery />

      <button type="button" className="btn gold" onClick={() => nav("/sohn")}>▶ Zur Tagesmission</button>
    </div>
  );
}
