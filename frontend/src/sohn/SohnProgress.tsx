import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useSohn } from "./SohnApp";
import { BadgesGallery } from "./GamificationPanels";
import { MyObjectives } from "./MyObjectives";
import type { OverviewResponse, ProgressResponse } from "../lib/types";

export function SohnProgress() {
  const { t } = useTranslation();
  const { planId } = useSohn();

  const overview = useAsync<OverviewResponse | null>(() => (planId ? api.overview(planId) : Promise.resolve(null)), [planId]);
  const prog = useAsync<ProgressResponse | null>(() => (planId ? api.overviewProgress(planId) : Promise.resolve(null)), [planId]);

  if (!planId) return <div className="sohn-body"><div className="loading">{t("progress.noPlan")}</div>
    <Link to="/sohn" className="btn ghost" style={{ textDecoration: "none" }}>{t("progress.toHome")}</Link></div>;
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
            <span>{pos.goalMet ? "✅" : pos.cadence === "None" ? "•" : "⬜"}</span>
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
            <i key={d.day} className={d.dutyDone ? "t" : d.goalsMet > 0 ? "d" : ""} title={d.day} />
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
