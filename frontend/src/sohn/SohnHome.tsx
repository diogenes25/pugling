import { useEffect } from "react";
import { Link } from "react-router-dom";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useAuth } from "../lib/auth";
import { Mascot } from "../components/Mascot";
import { useSohn } from "./SohnApp";
import { MissionsPanel } from "./GamificationPanels";
import type { OverviewResponse, PlanResponse, PositionStatus } from "../lib/types";

const TYPE_ICON: Record<string, string> = {
  Vocabulary: "🗣️", Cloze: "📝", Matching: "🔗", Arithmetic: "➗", ArithmeticDrill: "➗",
  List: "📋", Birkenbihl: "🎧", Reading: "📖", Grammar: "🔤", Translation: "🌍", Essay: "✍️", Listening: "🎧",
};
const typeIcon = (t: string) => TYPE_ICON[t] ?? "🎯";
const cadenceLabel = (c: PositionStatus["cadence"]) =>
  c === "Daily" ? "Tagesziel" : c === "Weekly" ? "Wochenziel" : "frei";

export function SohnHome() {
  const { signOut } = useAuth();
  const { planId, setPlanId, skin, setStreak } = useSohn();
  const plans = useAsync<PlanResponse[]>(() => api.plans(), []);

  // Den spielbaren Plan vorwählen, sobald die Liste da ist (server-autoritative Affordance statt eigener Regel).
  useEffect(() => {
    if (!plans.data) return;
    const current = plans.data.find((p) => p.id === planId);
    const chosen = current ?? plans.data.find((p) => p.isPlayable) ?? plans.data[0];
    if (chosen && chosen.id !== planId) setPlanId(chosen.id);
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

  const activePlan = plans.data.find((p) => p.id === planId) ?? plans.data.find((p) => p.isPlayable) ?? plans.data[0];
  const activePlanId = activePlan.id;
  return <HomeForPlan planId={activePlanId} plans={plans.data} onPickPlan={setPlanId} onStreak={setStreak} />;
}

function HomeForPlan({
  planId, plans, onPickPlan, onStreak,
}: {
  planId: number; plans: PlanResponse[]; onPickPlan: (id: number) => void; onStreak: (n: number) => void;
}) {
  const { skin } = useSohn();
  const overview = useAsync<OverviewResponse>(() => api.overview(planId), [planId]);

  useEffect(() => {
    if (overview.data) onStreak(overview.data.currentStreak);
  }, [overview.data, onStreak]);

  if (overview.loading) return <div className="sohn-body"><div className="loading">Lade Mission…</div></div>;
  if (overview.error || !overview.data) return <div className="sohn-body"><div className="error-box">{overview.error ?? "Fehler"}</div></div>;

  const o = overview.data;
  const t = o.today;
  const mood = t.dutyDone ? "hyped" : t.goalsMet > 0 ? "happy" : "sleepy";
  const goalPct = t.goalsTotal > 0 ? Math.round((t.goalsMet / t.goalsTotal) * 100) : 0;

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
          <div className="sub">{o.title}</div>
        </div>
      </div>

      {t.dutyDone ? (
        <div className="card" style={{ borderColor: "var(--lime)" }}>
          <span className="pill lime">✓ Heute geschafft!</span>
          <p className="sub" style={{ marginTop: 6 }}>Stark. Jede weitere Runde macht deinen {skin.name} nur stärker.</p>
        </div>
      ) : (
        <div className="card">
          <div className="row" style={{ marginBottom: 8 }}>
            <b style={{ fontSize: 13 }}>🎯 Ziele heute</b>
            <span className="sub" style={{ marginLeft: "auto" }}>{t.goalsMet} / {t.goalsTotal}</span>
          </div>
          <div className="bar lime"><i style={{ width: `${goalPct}%` }} /></div>
        </div>
      )}

      {t.positions.length === 0 && (
        <div className="card" style={{ textAlign: "center" }}>
          <p className="sub">Dieser Plan hat noch keine Übungen. Dein Vater fügt sie gleich hinzu.</p>
        </div>
      )}

      {t.positions.map((pos) => <PositionCard key={pos.positionId} pos={pos} />)}

      <MissionsPanel />
    </div>
  );
}

function PositionCard({ pos }: { pos: PositionStatus }) {
  const canPractice = pos.useLeitner || (!pos.testable && pos.checkMode === "None");
  const practiceLabel = pos.useLeitner
    ? `▶ ÜBEN${pos.dueCount > 0 ? ` (${pos.dueCount} fällig)` : ""}`
    : "▶ DURCHSPIELEN";

  return (
    <div className="card" style={pos.goalMet ? { borderColor: "var(--lime)" } : undefined}>
      <div className="row" style={{ marginBottom: 8 }}>
        <span style={{ fontSize: 20 }}>{typeIcon(pos.exerciseType)}</span>
        <b style={{ fontSize: 15 }}>{pos.exerciseTitle}</b>
        <span className="pill" style={{ marginLeft: "auto" }}>{cadenceLabel(pos.cadence)}</span>
        {pos.goalMet
          ? <span className="pill lime">✓</span>
          : pos.cadence !== "None" && <span className="pill mag">offen</span>}
      </div>
      <div className="row" style={{ gap: 8 }}>
        {canPractice && (
          <Link to={`/sohn/practice/${pos.positionId}`} className="btn gold" style={{ flex: 1, textDecoration: "none" }}>
            {practiceLabel}
          </Link>
        )}
        {pos.testable && (
          <Link to={`/sohn/test/${pos.positionId}`} className="btn" style={{ flex: 1, textDecoration: "none" }}>
            <span aria-hidden="true">🎯</span> TEST
          </Link>
        )}
      </div>
    </div>
  );
}
