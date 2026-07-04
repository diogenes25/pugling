import { useNavigate } from "react-router-dom";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useSohn } from "./SohnApp";
import type { PlanResponse, ProgressResponse } from "../lib/types";

const RANKS = ["Frischling", "Kenner", "Profi", "Ass", "Meister", "Großmeister"];

export function SohnProgress() {
  const { planId } = useSohn();
  const nav = useNavigate();

  const plan = useAsync<PlanResponse | null>(() => (planId ? api.plan(planId) : Promise.resolve(null)), [planId]);
  const prog = useAsync<ProgressResponse | null>(() => (planId ? api.progress(planId) : Promise.resolve(null)), [planId]);

  if (!planId) return <div className="sohn-body"><div className="loading">Wähle zuerst eine Mission auf der Basis.</div>
    <button type="button" className="btn ghost" onClick={() => nav("/sohn")}>Zur Basis</button></div>;
  if (plan.loading || prog.loading) return <div className="sohn-body"><div className="loading">Lade Fortschritt…</div></div>;
  if (!plan.data || !prog.data) return <div className="sohn-body"><div className="error-box">{plan.error ?? prog.error ?? "Fehler"}</div></div>;

  const p = plan.data;
  const pr = prog.data;
  // Aktuelle "Liga": höchste erreichte Box über alle Items.
  const currentBox = p.items.reduce((max, it) => Math.max(max, it.box), 1);
  const boxes = Array.from({ length: p.maxBox }, (_, i) => i + 1);
  const last7 = pr.days.filter((d) => d.day <= pr.days[pr.days.length - 1]?.day).slice(-7);

  return (
    <div className="sohn-body">
      <div className="row">
        <span className="screen-title" style={{ margin: 0 }}>Trophäenweg</span>
        <span className="chip" style={{ marginLeft: "auto" }}>🏆<b className="tabnum">{pr.totalPoints}</b></span>
      </div>
      <p className="sub">{p.title} · {pr.daysComplete}/{pr.totalDays} Tage geschafft · Streak {pr.currentStreak} 🔥</p>

      {boxes.map((b) => {
        const state = b < currentBox ? "done" : b === currentBox ? "now" : "";
        const inBox = p.items.filter((it) => it.box === b).length;
        return (
          <div className={`league ${state}`} key={b}>
            <div className="badge">{b < currentBox ? "✓" : b}</div>
            <div className="meta">
              <div className="t">Box {b} · {RANKS[b - 1] ?? "Rang"}</div>
              <div className="sub">{state === "now" ? `${inBox} Karten hier` : b < currentBox ? "gemeistert" : "gesperrt"}</div>
            </div>
          </div>
        );
      })}

      <div className="card">
        <div className="row" style={{ marginBottom: 8 }}>
          <b style={{ fontSize: 13 }}>🔥 Letzte Tage</b>
          <span className="sub" style={{ marginLeft: "auto" }}>{last7.length} Tage</span>
        </div>
        <div className="calendar">
          {last7.map((d) => (
            <i key={d.day} className={d.dayComplete ? "t" : d.minutesMet ? "d" : ""} title={d.day} />
          ))}
        </div>
        <div className="sub" style={{ marginTop: 8, fontSize: 11 }}>
          <span style={{ color: "var(--lime)" }}>■</span> Zeit &nbsp; <span style={{ color: "var(--gold)" }}>■</span> Zeit + Test
        </div>
      </div>

      <button type="button" className="btn gold" onClick={() => nav("/sohn/practice")}>▶ Weiter üben</button>
    </div>
  );
}
