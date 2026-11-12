import { useEffect } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useAuth } from "../lib/auth";
import { Mascot } from "../components/Mascot";
import { useSohn } from "./SohnApp";
import { useChosenPlan } from "./useChosenPlan";
import { useSkinText } from "../lib/skinText";
import { MissionsPanel } from "./GamificationPanels";
import { InterestIntroCard } from "./SohnInterests";
import type { OverviewResponse, PlanResponse, PositionStatus } from "../lib/types";

const TYPE_ICON: Record<string, string> = {
  Vocabulary: "🗣️", Cloze: "📝", Matching: "🔗", Arithmetic: "➗", ArithmeticDrill: "➗",
  List: "📋", Birkenbihl: "🎧", Reading: "📖", Grammar: "🔤", Translation: "🌍", Essay: "✍️", Listening: "🎧",
};
const typeIcon = (type: string) => TYPE_ICON[type] ?? "🎯";
const cadenceKey = (c: PositionStatus["cadence"]) =>
  c === "Daily" ? "home.cadenceDaily" : c === "Weekly" ? "home.cadenceWeekly" : "home.cadenceNone";

export function SohnHome() {
  const { t } = useTranslation();
  const { signOut } = useAuth();
  const { setPlanId, skin, setStreak } = useSohn();
  const { plans, chosen } = useChosenPlan();

  if (plans.loading) return <div className="sohn-body"><div className="loading">{t("home.loading")}</div></div>;
  if (plans.error) return <div className="sohn-body"><div className="error-box">{plans.error}</div></div>;

  if (!plans.data || !chosen) {
    return (
      <div className="sohn-body">
        <InterestIntroCard />
        <Mascot skin={skin} mood="sleepy" size={120} />
        <div className="card" style={{ textAlign: "center" }}>
          <div className="screen-title">{t("home.noMissionTitle")}</div>
          <p className="sub">{t("home.noMissionText")}</p>
        </div>
        <button type="button" className="btn ghost" onClick={signOut}>{t("home.signOut")}</button>
      </div>
    );
  }

  return <HomeForPlan planId={chosen.id} plans={plans.data} onPickPlan={setPlanId} onStreak={setStreak} />;
}

function HomeForPlan({
  planId, plans, onPickPlan, onStreak,
}: {
  planId: number; plans: PlanResponse[]; onPickPlan: (id: number) => void; onStreak: (n: number) => void;
}) {
  const { t } = useTranslation();
  const { skin } = useSohn();
  const skinText = useSkinText();
  const overview = useAsync<OverviewResponse>(() => api.overview(planId), [planId]);

  useEffect(() => {
    if (overview.data) onStreak(overview.data.currentStreak);
  }, [overview.data, onStreak]);

  if (overview.loading) return <div className="sohn-body"><div className="loading">{t("home.loadingMission")}</div></div>;
  if (overview.error || !overview.data) return <div className="sohn-body"><div className="error-box">{overview.error ?? t("home.error")}</div></div>;

  const o = overview.data;
  const today = o.today;
  const mood = today.dutyDone ? "hyped" : today.goalsMet > 0 ? "happy" : "sleepy";
  const goalPct = today.goalsTotal > 0 ? Math.round((today.goalsMet / today.goalsTotal) * 100) : 0;

  return (
    <div className="sohn-body">
      <InterestIntroCard />
      {plans.length > 1 && (
        <div className="field">
          <select value={planId} onChange={(e) => onPickPlan(Number(e.target.value))} aria-label={t("home.choosePlan")}>
            {plans.map((pl) => <option key={pl.id} value={pl.id}>{pl.title}</option>)}
          </select>
        </div>
      )}

      <div className="row">
        <Mascot skin={skin} mood={mood} size={84} />
        <div>
          <div className="screen-title" style={{ margin: 0 }}>{t("home.dailyMission")}</div>
          <div className="sub">{o.title}</div>
        </div>
      </div>

      {today.dutyDone ? (
        <div className="card" style={{ borderColor: "var(--lime)" }}>
          <span className="pill lime">{t("home.doneToday")}</span>
          {/* Der Skin-Name reist als Platzhalter; übersetzt wird er über die Skin-Id (B-304), wie im Skins-Shop. */}
          <p className="sub" style={{ marginTop: 6 }}>{t("home.doneText", { skin: skinText.name(skin) })}</p>
        </div>
      ) : (
        <div className="card">
          <div className="row" style={{ marginBottom: 8 }}>
            <b style={{ fontSize: 13 }}>{t("home.goalsToday")}</b>
            <span className="sub" style={{ marginLeft: "auto" }}>{today.goalsMet} / {today.goalsTotal}</span>
          </div>
          <div className="bar lime"><i style={{ width: `${goalPct}%` }} /></div>
        </div>
      )}

      {today.positions.length === 0 && (
        <div className="card" style={{ textAlign: "center" }}>
          <p className="sub">{t("home.noExercises")}</p>
        </div>
      )}

      {today.positions.map((pos) => <PositionCard key={pos.positionId} pos={pos} />)}

      <MissionsPanel />
    </div>
  );
}

export function PositionCard({ pos }: { pos: PositionStatus }) {
  const { t } = useTranslation();
  // Üben steht offen, sobald es etwas zu üben gibt: mit Leitner-Kasten, mit Inhalten auch ohne ihn (B-454 – sonst
  // wäre der Test der einzige Weg und fragte Wörter, die das Kind nie gesehen hat), und immer, wenn heute nichts
  // zu prüfen ist. `testable` ist eine TAGES-Aussage des Servers (auch falsch auf einer Kennenlern-Stufe, B-96) –
  // nicht `checkMode` abfragen, sonst hat eine Position an einem nicht prüfbaren Tag überhaupt keinen Knopf.
  // Eine freie Position hat keine Pflicht und darum kein Häkchen – der Server meldet sie als erfüllt, auch ungespielt (B-467).
  const dutyMet = pos.goalMet && pos.cadence !== "None";
  const canPractice = pos.useLeitner || !pos.testable || pos.poolSize > 0;
  const practiceLabel = pos.useLeitner
    ? (pos.dueCount > 0 ? t("home.practiceDue", { count: pos.dueCount }) : t("home.practice"))
    : pos.testable ? t("home.practice") : t("home.playThrough");

  return (
    <div className="card" style={dutyMet ? { borderColor: "var(--lime)" } : undefined}>
      <div className="row" style={{ marginBottom: 8 }}>
        <span style={{ fontSize: 20 }}>{typeIcon(pos.exerciseType)}</span>
        <b style={{ fontSize: 15 }}>{pos.exerciseTitle}</b>
        <span className="pill" style={{ marginLeft: "auto" }}>{t(cadenceKey(pos.cadence))}</span>
        {pos.cadence !== "None" && (dutyMet
          ? <span className="pill lime">✓</span>
          : <span className="pill mag">{t("home.open")}</span>)}
      </div>
      {/* B-04: how far through the material - only while there is still something ahead. */}
      {pos.introducedCount > 0 && pos.introducedCount < pos.poolSize && (
        <div className="muted" style={{ fontSize: 13, marginBottom: 8 }}>
          {t("home.introduced", { count: pos.introducedCount, total: pos.poolSize })}
        </div>
      )}
      <div className="row" style={{ gap: 8 }}>
        {canPractice && (
          <Link to={`/sohn/practice/${pos.positionId}`} className="btn gold" style={{ flex: 1, textDecoration: "none" }}>
            {practiceLabel}
          </Link>
        )}
        {pos.testable && (
          <Link to={`/sohn/test/${pos.positionId}`} className="btn" style={{ flex: 1, textDecoration: "none" }}>
            <span aria-hidden="true">🎯</span> {t("home.test")}
          </Link>
        )}
      </div>
    </div>
  );
}
