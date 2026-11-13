import { Link } from "react-router-dom";
import { arcadeDatum } from "../lib/datum";
import { useTranslation } from "react-i18next";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useChosenPlan } from "./useChosenPlan";
import { BadgesGallery } from "./GamificationPanels";
import { MyObjectives } from "./MyObjectives";
import type { OverviewResponse, ProgressResponse } from "../lib/types";

export function SohnProgress() {
  const { t } = useTranslation();
  // Dieselbe Auswahl wie die Startseite, nicht die gemerkte Id: die kann zu einem gelöschten Plan gehören (B-254).
  const { plans, chosen } = useChosenPlan();

  if (plans.loading) return <div className="sohn-body"><div className="loading">{t("progress.loading")}</div></div>;
  if (plans.error) return <div className="sohn-body"><div className="error-box">{plans.error}</div></div>;
  if (!chosen) return <div className="sohn-body"><div className="loading">{t("progress.noPlan")}</div>
    <Link to="/sohn" className="btn ghost" style={{ textDecoration: "none" }}>{t("progress.toHome")}</Link></div>;
  // Eine eigene Komponente, erst mit der Wahl gemountet: ihr Laden beginnt mit `loading`, statt einen Render lang die
  // Antwort für „kein Plan“ als fehlende Daten zu lesen und den Fehlerkasten aufblitzen zu lassen.
  return <ProgressForPlan planId={chosen.id} />;
}

function ProgressForPlan({ planId }: { planId: number }) {
  const { t, i18n } = useTranslation();
  const overview = useAsync<OverviewResponse>(() => api.overview(planId), [planId]);
  const prog = useAsync<ProgressResponse>(() => api.overviewProgress(planId), [planId]);

  if (overview.loading || prog.loading) return <div className="sohn-body"><div className="loading">{t("progress.loading")}</div></div>;
  if (!overview.data || !prog.data) return <div className="sohn-body"><div className="error-box">{overview.error ?? prog.error ?? t("home.error")}</div></div>;

  const o = overview.data;
  const pr = prog.data;
  const last7 = pr.days.slice(-7);

  return (
    <div className="sohn-body">
      <div className="row">
        <span className="screen-title" style={{ margin: 0 }}>{t("progress.title")}</span>
        <span className="chip" style={{ marginLeft: "auto" }}>🏆<b className="tabnum">{pr.totalPoints}</b></span>
      </div>
      <p className="sub">{t("progress.summary", { title: o.title, done: pr.daysComplete, total: pr.totalDays, streak: pr.currentStreak })}</p>

      <div className="card">
        <div className="row" style={{ marginBottom: 8 }}>
          <b style={{ fontSize: 13 }}>{t("progress.myExercises")}</b>
          <span className="sub" style={{ marginLeft: "auto" }}>{t("progress.today", { met: o.today.goalsMet, total: o.today.goalsTotal })}</span>
        </div>
        {o.today.positions.map((pos) => (
          <div className="row" key={pos.positionId} style={{ padding: "4px 0" }}>
            {/* Eine freie Position hat keine Pflicht und darum kein Häkchen – der Server meldet sie als erfüllt (B-467). */}
            <span>{pos.cadence === "None" ? "•" : pos.goalMet ? "✅" : "⬜"}</span>
            <b>{pos.exerciseTitle}</b>
            <span className="sub" style={{ marginLeft: "auto" }}>
              {pos.cadence === "Daily" ? t("home.cadenceDaily") : pos.cadence === "Weekly" ? t("home.cadenceWeekly") : t("home.cadenceNone")}
            </span>
          </div>
        ))}
        {o.today.positions.length === 0 && <p className="sub">{t("progress.noExercises")}</p>}
      </div>

      <div className="card">
        <div className="row" style={{ marginBottom: 8 }}>
          <b style={{ fontSize: 13 }}>{t("progress.lastDays")}</b>
          <span className="sub" style={{ marginLeft: "auto" }}>{t("progress.days", { count: last7.length })}</span>
        </div>
        <div className="calendar">
          {last7.map((d) => (
            <i key={d.day} className={d.dutyDone ? "t" : d.goalsMet > 0 ? "d" : ""} title={arcadeDatum(d.day, i18n.language)} />
          ))}
        </div>
        <div className="sub" style={{ marginTop: 8, fontSize: 11 }}>
          <span style={{ color: "var(--lime)" }}>■</span> {t("progress.partly")} &nbsp; <span style={{ color: "var(--gold)" }}>■</span> {t("progress.complete")}
        </div>
      </div>

      {/* Die großen Ziele stehen über den Abzeichen: sie laufen über Wochen und sagen, wofür das alles gut ist. */}
      <MyObjectives />

      <BadgesGallery />

      <Link to="/sohn" className="btn gold" style={{ textDecoration: "none" }}>{t("progress.toMission")}</Link>
    </div>
  );
}
