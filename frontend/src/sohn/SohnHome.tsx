import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useAuth } from "../lib/auth";
import { Mascot } from "../components/Mascot";
import { useSohn } from "./SohnApp";
import { useChosenPlan } from "./useChosenPlan";
import { useSkinText } from "../lib/skinText";
import { jetztGerade } from "../lib/datum";
import { useDialogFocus } from "../lib/useDialogFocus";
import { InterestIntroCard } from "./SohnInterests";
import { BonusEinstieg } from "./SohnBonus";
import {
  abschnitte, frist, hauptweg, kennenlernAnteil, naechstePositionId, nebenweg, pflichtErfuellt, spielPfad,
  type Frist, type Spielweg,
} from "./pfad";
import type { OverviewResponse, PlanResponse, PositionStatus } from "../lib/types";

const TYPE_ICON: Record<string, string> = {
  Vocabulary: "🗣️", Cloze: "📝", Matching: "🔗", Arithmetic: "➗", ArithmeticDrill: "➗",
  List: "📋", Birkenbihl: "🎧", Reading: "📖", Grammar: "🔤", Translation: "🌍", Essay: "✍️", Listening: "🎧",
};
const typeIcon = (type: string) => TYPE_ICON[type] ?? "🎯";
const cadenceKey = (c: PositionStatus["cadence"]) =>
  c === "Daily" ? "home.cadenceDaily" : c === "Weekly" ? "home.cadenceWeekly" : "home.cadenceNone";

/** Seitlicher Versatz der Knoten: eine Schlangenlinie, die sich alle acht Knoten wiederholt. */
const SCHLANGE = [0, -56, -84, -56, 0, 56, 84, 56];

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

      <Pfad positions={today.positions} />

      {/* Nach dem Pfad, nicht davor: erst die Pflicht, dann die Kür. Offen wie beim Server – erledigte Pflicht oder
          ein Plan ohne Pflicht; die Sperre selbst hält der Server. */}
      <BonusEinstieg offen={today.dutyDone || today.goalsTotal === 0} />
    </div>
  );
}

/**
 * Der Lernpfad (B-525): die Positionen des Plans als Knoten in Schlangenlinie, in der Reihenfolge des Servers und
 * gegliedert nach der Unit. Kein Knoten ist gesperrt – der Pfad zeigt, was dran ist, er erzwingt keine Reihenfolge.
 */
export function Pfad({ positions, jetzt = jetztGerade() }: { positions: PositionStatus[]; jetzt?: Date }) {
  const { t } = useTranslation();
  const { skin } = useSohn();
  const [offenId, setOffenId] = useState<number | null>(null);
  const naechste = naechstePositionId(positions);
  const offen = positions.find((p) => p.positionId === offenId) ?? null;

  let lauf = 0;
  return (
    <>
      {abschnitte(positions).map((a, i) => (
        <section key={`${a.unitId}-${i}`} className="pfad-abschnitt" aria-label={a.title}>
          <div className="pfad-banner">
            <span>{t("pfad.section", { number: i + 1 })}</span>
            <b>{a.title}</b>
          </div>
          <div className="pfad-schlange">
            {a.positions.map((pos) => {
              const dx = SCHLANGE[lauf++ % SCHLANGE.length];
              const istNaechste = pos.positionId === naechste;
              return (
                <div key={pos.positionId} className={`pfad-zeile${istNaechste ? " mit-start" : ""}`} style={{ transform: `translateX(${dx}px)` }}>
                  {istNaechste && <span className="pfad-start" aria-hidden="true">{t("pfad.start")}</span>}
                  <Knoten pos={pos} naechste={istNaechste} jetzt={jetzt} onOeffnen={() => setOffenId(pos.positionId)} />
                  {istNaechste && (
                    <div className="pfad-begleiter" style={dx > 0 ? { right: "calc(50% + 52px)" } : { left: "calc(50% + 52px)" }} aria-hidden="true">
                      <Mascot skin={skin} mood="happy" size={64} />
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </section>
      ))}
      {offen && <KnotenBlatt key={offen.positionId} pos={offen} jetzt={jetzt} onSchliessen={() => setOffenId(null)} />}
    </>
  );
}

function useFristText() {
  const { t } = useTranslation();
  return (f: Frist) => f.art === "sonntag" ? t("pfad.untilSunday") : t("pfad.hoursLeft", { count: f.stunden });
}

/**
 * Die Zeile zu einem offenen Pflichtziel: Frist und – nur wenn es einen gibt – der Malus. Am Knoten ist sie
 * `versteckt`: dort trägt der Vorlesename des Knopfs dasselbe schon, sie würde doppelt angesagt.
 */
function FristZeile({ pos, jetzt, versteckt = false }: { pos: PositionStatus; jetzt: Date; versteckt?: boolean }) {
  const { t } = useTranslation();
  const fristText = useFristText();
  const f = frist(pos, jetzt);
  if (!f) return null;
  return (
    <span className="pfad-frist" aria-hidden={versteckt || undefined}>
      <span aria-hidden="true">⏰ </span>{fristText(f)}
      {pos.penaltyCoins > 0 && <> · {t("pfad.penalty", { count: pos.penaltyCoins })}</>}
    </span>
  );
}

function Knoten({ pos, naechste, jetzt, onOeffnen }: {
  pos: PositionStatus; naechste: boolean; jetzt: Date; onOeffnen: () => void;
}) {
  const { t } = useTranslation();
  const fristText = useFristText();
  const anteil = kennenlernAnteil(pos);
  const erfuellt = pflichtErfuellt(pos);
  const f = frist(pos, jetzt);

  // Der Vorlesename trägt, was das Auge am Knoten sieht: Titel, „als Nächstes dran“ (das sichtbare START ist
  // Schmuck und versteckt), Takt, Stand, Frist und Preis.
  const name = [
    pos.exerciseTitle,
    naechste ? t("pfad.next") : null,
    t(cadenceKey(pos.cadence)),
    erfuellt ? t("pfad.goalMet") : null,
    anteil !== null ? t("home.introduced", { count: pos.introducedCount, total: pos.poolSize }) : null,
    f ? fristText(f) : null,
    f && pos.penaltyCoins > 0 ? t("pfad.penalty", { count: pos.penaltyCoins }) : null,
  ].filter(Boolean).join(", ");

  const R = 44;
  const umfang = 2 * Math.PI * R;
  return (
    <>
      <button
        type="button"
        className={`pfad-knoten${naechste ? " naechste" : ""}${erfuellt ? " erfuellt" : ""}${pos.cadence === "None" ? " frei" : ""}`}
        aria-label={name} aria-haspopup="dialog" aria-current={naechste ? "step" : undefined}
        onClick={onOeffnen}
      >
        {anteil !== null && (
          <svg className="pfad-ring" width="96" height="96" aria-hidden="true">
            <circle cx="48" cy="48" r={R} fill="none" strokeWidth="6" stroke="rgba(255,255,255,.14)" />
            {/* Erst ab dem ersten kennengelernten Inhalt: das runde Linienende malte bei 0 sonst einen Punkt. */}
            {anteil > 0 && (
              <circle
                className="pfad-ring-anteil" cx="48" cy="48" r={R} fill="none" strokeWidth="6" stroke="var(--gold)"
                strokeLinecap="round" strokeDasharray={`${umfang * anteil} ${umfang}`} transform="rotate(-90 48 48)"
              />
            )}
          </svg>
        )}
        <span className="pfad-kern" aria-hidden="true">{typeIcon(pos.exerciseType)}</span>
        {erfuellt && <span className="pfad-haken" aria-hidden="true">✓</span>}
      </button>
      <span className="pfad-titel" aria-hidden="true">{pos.exerciseTitle}</span>
      <FristZeile pos={pos} jetzt={jetzt} versteckt />
    </>
  );
}

const WEG_TEXT = { practice: "pfad.practice", test: "pfad.test" } as const satisfies Record<Spielweg, string>;

/** Das Blatt zu einem Knoten: Stand der Position und genau ein Hauptknopf; der andere Weg bleibt klein daneben. */
function KnotenBlatt({ pos, jetzt, onSchliessen }: { pos: PositionStatus; jetzt: Date; onSchliessen: () => void }) {
  const { t } = useTranslation();
  const blatt = useRef<HTMLDivElement | null>(null);
  const los = useRef<HTMLAnchorElement | null>(null);
  // Fokus auf „Los“, Tab im Blatt gefangen, Escape schließt, danach zurück zum Knoten – sonst wäre `aria-modal`
  // gelogen: Tab wanderte hinter den Schleier.
  useDialogFocus(blatt, onSchliessen, los);
  const anteil = kennenlernAnteil(pos);
  const zweiter = nebenweg(pos);
  const titelId = `pfad-blatt-${pos.positionId}`;

  return (
    <div className="pfad-schleier" onClick={onSchliessen}>
      <div ref={blatt} tabIndex={-1} className="pfad-blatt" role="dialog" aria-modal="true" aria-labelledby={titelId} onClick={(e) => e.stopPropagation()}>
        <div className="row">
          <span style={{ fontSize: 30 }} aria-hidden="true">{typeIcon(pos.exerciseType)}</span>
          <b id={titelId} className="pfad-blatt-titel">{pos.exerciseTitle}</b>
          <button type="button" className="pfad-zu" onClick={onSchliessen} aria-label={t("pfad.close")}>✕</button>
        </div>
        <div className="row" style={{ flexWrap: "wrap", rowGap: 6 }}>
          <span className="pill">{t(cadenceKey(pos.cadence))}</span>
          {pflichtErfuellt(pos) && <span className="pill lime">✓ {t("pfad.goalMet")}</span>}
          <FristZeile pos={pos} jetzt={jetzt} />
        </div>
        {anteil !== null && (
          <div className="sub">
            {t("home.introduced", { count: pos.introducedCount, total: pos.poolSize })}
            {pos.useLeitner && pos.dueCount > 0 && <> · {t("pfad.due", { count: pos.dueCount })}</>}
          </div>
        )}
        <Link ref={los} to={spielPfad(hauptweg(pos), pos.positionId)} className="btn gold" style={{ textDecoration: "none", textAlign: "center" }}>
          {t("pfad.go")}
        </Link>
        {zweiter && (
          <Link to={spielPfad(zweiter, pos.positionId)} className="btn ghost small" style={{ textDecoration: "none", textAlign: "center" }}>
            {t(WEG_TEXT[zweiter])}
          </Link>
        )}
      </div>
    </div>
  );
}
