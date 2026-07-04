import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useAuth } from "../lib/auth";
import { Mascot } from "../components/Mascot";
import { useSohn } from "./SohnApp";
import type { PlanResponse } from "../lib/types";

export function SohnHome() {
  const { signOut } = useAuth();
  const { planId, setPlanId, skin, setStreak } = useSohn();
  const plans = useAsync<PlanResponse[]>(() => api.plans(), []);

  // Ersten aktiven Plan vorwählen, sobald die Liste da ist.
  useEffect(() => {
    if (!plans.data || planId) return;
    const chosen = plans.data.find((p) => p.active) ?? plans.data[0];
    if (chosen) setPlanId(chosen.id);
  }, [plans.data, planId, setPlanId]);

  if (plans.loading) return <div className="sohn-body"><div className="loading">Lade…</div></div>;
  if (plans.error) return <div className="sohn-body"><div className="error-box">{plans.error}</div></div>;

  if (!plans.data || plans.data.length === 0) {
    return (
      <div className="sohn-body">
        <Mascot skin={skin} mood="sleepy" size={120} />
        <div className="card" style={{ textAlign: "center" }}>
          <div className="screen-title">Noch keine Mission</div>
          <p className="sub">Dein Vater hat noch keinen Lehrplan für dich erstellt. Sobald es losgeht, erscheint hier deine Tagesmission.</p>
        </div>
        <button type="button" className="btn ghost" onClick={signOut}>Abmelden</button>
      </div>
    );
  }

  const activePlanId = planId ?? plans.data[0].id;
  return <HomeForPlan planId={activePlanId} plans={plans.data} onPickPlan={setPlanId} onStreak={setStreak} />;
}

function HomeForPlan({
  planId, plans, onPickPlan, onStreak,
}: {
  planId: number; plans: PlanResponse[]; onPickPlan: (id: number) => void; onStreak: (n: number) => void;
}) {
  const { skin } = useSohn();
  const nav = useNavigate();
  const today = useAsync(() => api.today(planId), [planId]);

  useEffect(() => {
    if (today.data) onStreak(today.data.currentStreak);
  }, [today.data, onStreak]);

  if (today.loading) return <div className="sohn-body"><div className="loading">Lade Mission…</div></div>;
  if (today.error || !today.data) return <div className="sohn-body"><div className="error-box">{today.error ?? "Fehler"}</div></div>;

  const t = today.data;
  const plan = plans.find((p) => p.id === planId)!;
  const p = t.progress;
  const minutePct = Math.min(100, Math.round((p.minutesPracticed / Math.max(1, plan.dailyMinutesRequired)) * 100));
  const mood = t.dutyDone ? "hyped" : p.minutesPracticed > 0 ? "happy" : "sleepy";
  const dueCount = t.dueItems.length;

  return (
    <div className="sohn-body">
      {plans.length > 1 && (
        <div className="field">
          <select value={planId} onChange={(e) => onPickPlan(Number(e.target.value))} aria-label="Plan wählen">
            {plans.map((pl) => <option key={pl.id} value={pl.id}>{pl.title}</option>)}
          </select>
        </div>
      )}

      <div className="row">
        <Mascot skin={skin} mood={mood} size={84} />
        <div>
          <div className="screen-title" style={{ margin: 0 }}>Tagesmission</div>
          <div className="sub">{plan.title}{t.mode ? ` · ${t.mode === "New" ? "neue Wörter" : "Wiederholung"}` : ""}</div>
        </div>
      </div>

      {t.dutyDone && (
        <div className="card" style={{ borderColor: "var(--lime)" }}>
          <span className="pill lime">✓ Heute geschafft!</span>
          <p className="sub" style={{ marginTop: 6 }}>Stark. Jede weitere Runde macht deinen {skin.name} nur stärker.</p>
        </div>
      )}

      <div className="card">
        <div className="row" style={{ marginBottom: 8 }}>
          <b style={{ fontSize: 13 }}>⏱️ Übungszeit</b>
          <span className="sub" style={{ marginLeft: "auto" }}>{p.minutesPracticed} / {plan.dailyMinutesRequired} min</span>
        </div>
        <div className="bar cyan"><i style={{ width: `${minutePct}%` }} /></div>

        <div className="row" style={{ margin: "12px 0 8px" }}>
          <b style={{ fontSize: 13 }}>🎯 Tagestest</b>
          <span className="pill lime" style={{ marginLeft: "auto" }}>
            {p.testPassed ? `bestanden ${p.bestScorePercent}%` : p.testAttempts > 0 ? `${p.bestScorePercent}% – nochmal!` : "offen"}
          </span>
        </div>
        <div className="bar"><i style={{ width: `${p.testPassed ? 100 : (p.bestScorePercent ?? 0)}%` }} /></div>
      </div>

      {plan.useLeitner && (
        <button type="button" className="btn gold" onClick={() => nav("/sohn/practice")}>
          ▶ ÜBEN {dueCount > 0 ? `(${dueCount} fällig)` : ""}
        </button>
      )}
      <button type="button" className="btn" onClick={() => nav("/sohn/test")}>🎯 TAGESTEST</button>

      {p.outstanding.length > 0 && !t.dutyDone && (
        <p className="sub" style={{ textAlign: "center" }}>Noch offen: {p.outstanding.join(" · ")}</p>
      )}
    </div>
  );
}
