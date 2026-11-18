import { useEffect, useId, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { api, ApiError } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useAuth } from "../lib/auth";
import { Mascot } from "../components/Mascot";
import { useSohn } from "./SohnApp";
import { nachFach } from "./tagessicht";
import { useSkinText } from "../lib/skinText";
import { jetztGerade } from "../lib/datum";
import { useDialogFocus } from "../lib/useDialogFocus";
import { useAction } from "../lib/useAction";
import { useGemerkterFehler } from "../lib/useGemerkterFehler";
import { useErrorMessage } from "../lib/chromeText";
import { prefersReducedMotion } from "../lib/ui";
import { InterestIntroCard } from "./SohnInterests";
import { BonusEinstieg } from "./SohnBonus";
import {
  abschnitte, frist, hauptweg, istTagespflicht, kennenlernAnteil, naechstePositionId, nebenweg, pflichtErfuellt, spielPfad, zielAnteil,
  type Frist, type Spielweg,
} from "./pfad";
import type { DailyBoxOpened, DailyBoxStatus, PositionStatus, TodayResponse } from "../lib/types";

const TYPE_ICON: Record<string, string> = {
  Vocabulary: "🗣️", Cloze: "📝", Matching: "🔗", Arithmetic: "➗", ArithmeticDrill: "➗",
  List: "📋", Birkenbihl: "🎧", Reading: "📖", Grammar: "🔤", Translation: "🌍", Essay: "✍️", Listening: "🎧",
};
const typeIcon = (type: string) => TYPE_ICON[type] ?? "🎯";
export const cadenceKey = (c: PositionStatus["cadence"]) =>
  c === "Daily" ? "home.cadenceDaily" : c === "Weekly" ? "home.cadenceWeekly"
    : c === "UntilDone" ? "home.cadenceUntilDone" : "home.cadenceNone";

/** Seitlicher Versatz der Knoten: eine Schlangenlinie, die sich alle acht Knoten wiederholt. */
const SCHLANGE = [0, -56, -84, -56, 0, 56, 84, 56];

/**
 * Die Startseite der Arcade (B-606): die Kind-Tagessicht über **alle** aktiven Pläne, nach Fach gegliedert – nicht mehr
 * die Übersicht des einen gewählten Plans. Pflicht, Truhe und Serie meldet der Server für das ganze Kind; eine Position
 * führt in den Weg ihres eigenen Plans (`planId`). Der gemerkte Plan (`useChosenPlan`) gilt nur noch für Üben und Test.
 */
export function SohnHome() {
  const { t } = useTranslation();
  const { signOut } = useAuth();
  const { childId, skin, setStreak } = useSohn();
  const skinText = useSkinText();
  const titelPraefix = useId();
  const sicht = useAsync<TodayResponse>(() => api.today(childId), [childId]);

  useEffect(() => {
    if (sicht.data) setStreak(sicht.data.currentStreak);
  }, [sicht.data, setStreak]);

  // `showPlaceholder`, nicht `loading`: ein `reload` (Truhe nach `daily_box_none`) hängte sonst die ganze Startseite aus.
  if (sicht.showPlaceholder) return <div className="sohn-body"><div className="loading">{t("home.loadingMission")}</div></div>;
  if (!sicht.data) return <div className="sohn-body"><div className="error-box">{sicht.error ?? t("home.error")}</div></div>;

  const today = sicht.data;
  if (today.positions.length === 0) {
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

  const mood = today.dutyDone ? "hyped" : today.goalsMet > 0 ? "happy" : "sleepy";
  const { tag, woche } = zielZaehler(today.positions);
  // Der Balken zählt dieselben Ziele wie Truhe und Bonus (Review B-535): die Tagesziele, ohne sie alle.
  const belohnung = belohnungsZiele(today.positions);
  const goalPct = belohnung.total > 0 ? Math.round((belohnung.met / belohnung.total) * 100) : 0;

  const faecher = nachFach(today.positions);
  // Ein einziger START über alle Fächer: der Pfad je Fach hebt sonst in jedem Fach einen „nächsten“ Knoten hervor.
  const naechste = naechstePositionId(today.positions);
  // Die Pläne von heute, einmal je Plan in der Reihenfolge des Servers – die Fächer nennen schon die Abschnitte.
  const planTitel = [...new Set(today.positions.map((p) => p.planTitle).filter(Boolean))];

  return (
    <div className="sohn-body">
      <InterestIntroCard />

      <div className="row">
        <Mascot skin={skin} mood={mood} size={84} />
        <div>
          <h1 className="screen-title" style={{ margin: 0, fontWeight: "inherit" }}>{t("home.dailyMission")}</h1>
          <div className="sub">{planTitel.join(" · ")}</div>
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
          {tag.total > 0 && (
            <div className="row" style={{ marginBottom: 8 }}>
              <b style={{ fontSize: 13 }}>{t("home.goalsToday")}</b>
              <span className="sub" style={{ marginLeft: "auto" }}>{tag.met} / {tag.total}</span>
            </div>
          )}
          {woche.total > 0 && (
            <div className="row" style={{ marginBottom: 8 }}>
              <b style={{ fontSize: 13 }}>{t("home.goalsWeek")}</b>
              <span className="sub" style={{ marginLeft: "auto" }}>{woche.met} / {woche.total}</span>
            </div>
          )}
          <div className="bar lime"><i style={{ width: `${goalPct}%` }} /></div>
        </div>
      )}

      {/* Scheitert das Neuladen (etwa nach `daily_box_none`), bleibt die alte Übersicht stehen – der Fehler muss
          trotzdem sichtbar sein, sonst wartet das Kind auf einen Stand, der nie kommt. */}
      <NeuLadenFehler fehler={sicht.error} laedt={sicht.loading} onRetry={sicht.reload} />

      {/* B-606: je Fach ein Abschnitt mit seinem eigenen Pfad; der Fachname steht sichtbar darüber. */}
      {faecher.map((f, i) => (
        <section key={f.fach} className="fach-gruppe" aria-labelledby={`${titelPraefix}-fach-${i}`}>
          <h2 id={`${titelPraefix}-fach-${i}`} className="screen-title fach-titel">{f.fach || t("home.noSubject")}</h2>
          <Pfad positions={f.positions} naechsteId={naechste} />
        </section>
      ))}

      {/* Der letzte Knoten des Pfads (B-527): erst die Pflicht, dann die Truhe, dann die Kür. */}
      <Truhe box={today.dailyBox} heute={today.day} ziele={belohnung.art} freigeschaltet={today.rewardsUnlocked}
        onVeraltet={sicht.reload} />

      {/* Nach dem Pfad, nicht davor: erst die Pflicht, dann die Kür. Offen liest der Server (`rewardsUnlocked`:
          die Tagesziele, B-535) statt es nachzurechnen; ein Plan ohne Pflicht hat nichts davor. Die Sperre hält der Server. */}
      <BonusEinstieg offen={today.rewardsUnlocked || today.goalsTotal === 0} ziele={belohnung.art} />
    </div>
  );
}

/**
 * Der Lernpfad (B-525): die Positionen des Plans als Knoten in Schlangenlinie, in der Reihenfolge des Servers und
 * gegliedert nach der Unit. Kein Knoten ist gesperrt – der Pfad zeigt, was dran ist, er erzwingt keine Reihenfolge.
 */
export function Pfad({ positions, jetzt = jetztGerade(), naechsteId }: {
  positions: PositionStatus[]; jetzt?: Date;
  /** Der nächste Knoten über mehrere Pfade hinweg (B-606); ohne Angabe der erste offene dieses Pfads. */
  naechsteId?: number | null;
}) {
  const { t } = useTranslation();
  const { skin } = useSohn();
  const [offenId, setOffenId] = useState<number | null>(null);
  const naechste = naechsteId === undefined ? naechstePositionId(positions) : naechsteId;
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

/** Kantenlänge des Knotens in px – muss zu `.pfad-knoten` in index.css passen. */
const KNOTEN = 104;
const RING_AUSSEN = 48;
const RING_INNEN = 41;
const RING_BREITE = 5;

/** Ein Fortschrittsring: leere Spur plus der gefüllte Bogen ab zwölf Uhr. */
function Ring({ r, anteil, farbe, klasse }: { r: number; anteil: number; farbe: string; klasse: string }) {
  const mitte = KNOTEN / 2;
  const umfang = 2 * Math.PI * r;
  return (
    <>
      <circle className={`${klasse}-bahn`} cx={mitte} cy={mitte} r={r} fill="none" strokeWidth={RING_BREITE} stroke="rgba(255,255,255,.14)" />
      {/* Erst ab einem Stand über 0: das runde Linienende malte bei 0 sonst einen Punkt. */}
      {anteil > 0 && (
        <circle
          className={klasse} cx={mitte} cy={mitte} r={r} fill="none" strokeWidth={RING_BREITE} stroke={farbe}
          strokeLinecap="round" strokeDasharray={`${umfang * anteil} ${umfang}`} transform={`rotate(-90 ${mitte} ${mitte})`}
        />
      )}
    </>
  );
}

function Knoten({ pos, naechste, jetzt, onOeffnen }: {
  pos: PositionStatus; naechste: boolean; jetzt: Date; onOeffnen: () => void;
}) {
  const { t } = useTranslation();
  const fristText = useFristText();
  const anteil = kennenlernAnteil(pos);
  const ziel = zielAnteil(pos);
  const erfuellt = pflichtErfuellt(pos);
  const f = frist(pos, jetzt);

  // Der Vorlesename trägt, was das Auge am Knoten sieht: Titel, „als Nächstes dran“ (das sichtbare START ist
  // Schmuck und versteckt), Takt, Stand, Frist und Preis.
  const name = [
    pos.exerciseTitle,
    naechste ? t("pfad.next") : null,
    t(cadenceKey(pos.cadence)),
    erfuellt ? t("pfad.goalMet") : null,
    ziel !== null && ziel > 0 && !erfuellt ? t("pfad.goalProgress", { percent: Math.round(ziel * 100) }) : null,
    anteil !== null ? t("home.introduced", { count: pos.imprintedCount ?? 0, total: pos.poolSize }) : null,
    f ? fristText(f) : null,
    f && pos.penaltyCoins > 0 ? t("pfad.penalty", { count: pos.penaltyCoins }) : null,
  ].filter(Boolean).join(", ");

  return (
    <>
      <button
        type="button"
        className={`pfad-knoten${naechste ? " naechste" : ""}${erfuellt ? " erfuellt" : ""}${pos.cadence === "None" ? " frei" : ""}`}
        aria-label={name} aria-haspopup="dialog" aria-current={naechste ? "step" : undefined}
        onClick={onOeffnen}
      >
        {(anteil !== null || ziel !== null) && (
          <svg className="pfad-ring" width={KNOTEN} height={KNOTEN} aria-hidden="true">
            {/* Außen der Weg durch den Stoff (gold), innen die laufende Pflicht (grün, voll genau mit dem Haken). */}
            {anteil !== null && <Ring r={RING_AUSSEN} anteil={anteil} farbe="var(--gold)" klasse="pfad-ring-anteil" />}
            {ziel !== null && <Ring r={RING_INNEN} anteil={ziel} farbe="var(--lime)" klasse="pfad-ring-ziel" />}
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
  const { setPlanId } = useSohn();
  const blatt = useRef<HTMLDivElement | null>(null);
  const los = useRef<HTMLAnchorElement | null>(null);
  // B-606: die Startseite zeigt mehrere Pläne; Üben und Test spielen den gemerkten Plan – also erst den der Position merken.
  const merkePlan = () => { if (pos.planId) setPlanId(pos.planId); };
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
          {/* B-588: nur mit Seite – „Unit 2“ allein schickte das Kind irgendwo in die Unit suchen. */}
          {pos.bookPages && (
            <span className="pill pfad-buchbezug">
              {t("pfad.bookRef", { series: pos.seriesName ?? "", unit: pos.seriesUnitLabel, pages: pos.bookPages })}
            </span>
          )}
          <FristZeile pos={pos} jetzt={jetzt} />
        </div>
        {anteil !== null && (
          <div className="sub">
            {t("home.introduced", { count: pos.imprintedCount ?? 0, total: pos.poolSize })}
            {pos.useLeitner && pos.dueCount > 0 && <> · {t("pfad.due", { count: pos.dueCount })}</>}
          </div>
        )}
        <Link ref={los} to={spielPfad(hauptweg(pos), pos.positionId)} onClick={merkePlan} className="btn gold" style={{ textDecoration: "none", textAlign: "center" }}>
          {t("pfad.go")}
        </Link>
        {zweiter && (
          <Link to={spielPfad(zweiter, pos.positionId)} onClick={merkePlan} className="btn ghost small" style={{ textDecoration: "none", textAlign: "center" }}>
            {t(WEG_TEXT[zweiter])}
          </Link>
        )}
      </div>
    </div>
  );
}

/**
 * „Ziele heute“ und „Ziele diese Woche“ (B-535): getrennt gezählt – gemischt stünde „1 / 2“ neben einem offenen
 * Bonus-Training, obwohl alles von heute geschafft ist. Die Nacharbeit „bis erledigt“ ist jeden Tag fällig und zählt
 * darum zu heute (B-636).
 */
export function zielZaehler(positions: readonly PositionStatus[]) {
  const zaehle = (gilt: (p: PositionStatus) => boolean) => {
    const ziele = positions.filter(gilt);
    return { met: ziele.filter((p) => p.goalMet).length, total: ziele.length };
  };
  return { tag: zaehle((p) => istTagespflicht(p.cadence)), woche: zaehle((p) => p.cadence === "Weekly") };
}

/** Welche Ziele die Truhe freischalten: die Tagesziele, ohne sie alle (Wochen-)Ziele, ohne Pflicht gar keine (B-535). */
export type TruhenZiele = "tag" | "woche" | "keine";

/**
 * Die Ziele, an denen Truhe und Bonus-Training hängen, mit ihrem Stand – dieselbe Auswahl wie
 * `PositionProgressService.RewardObligations` am Server: die Tagesziele, ohne sie alle Ziele mit Takt. Ob sie
 * **erfüllt** sind, liest die Seite trotzdem am Server (`rewardsUnlocked`); hier geht es um Wortlaut und Balken.
 */
export function belohnungsZiele(positions: readonly PositionStatus[]): { art: TruhenZiele; met: number; total: number } {
  const mitTakt = positions.filter((p) => p.cadence !== "None");
  const tag = mitTakt.filter((p) => istTagespflicht(p.cadence));
  const ziele = tag.length > 0 ? tag : mitTakt;
  return {
    art: tag.length > 0 ? "tag" : mitTakt.length > 0 ? "woche" : "keine",
    met: ziele.filter((p) => p.goalMet).length, total: ziele.length,
  };
}

/**
 * Ein gescheitertes Neuladen der Übersicht (Review B-527). Der Fehler überlebt den Start der Wiederholung
 * (`useGemerkterFehler`, B-216) – sonst hinge der Kasten samt Knopf im Moment des Tippens aus.
 */
export function NeuLadenFehler({ fehler, laedt, onRetry }: { fehler: string | null; laedt: boolean; onRetry: () => void }) {
  const { t } = useTranslation();
  const gescheitert = useGemerkterFehler(fehler, laedt);
  return (
    <div role="alert">
      {gescheitert && (
        <div className="error-box">
          {t("home.reloadFailed", { error: gescheitert })}{" "}
          <button type="button" className="btn ghost small" disabled={laedt} onClick={onRetry}>{t("home.retry")}</button>
        </div>
      )}
    </div>
  );
}

const ZU_TEXT = { tag: "truhe.closed", woche: "truhe.closedWeek", keine: "truhe.closedNone" } as const satisfies Record<TruhenZiele, string>;

/**
 * Die Tagestruhe am Ende des Pfads (B-527) in drei Zuständen: *bereit*, sobald eine wartet (leuchtet, Knopf, bei
 * mehreren die Zahl); *offen*, wenn die Truhe von **heute** geöffnet ist (Inhalt, „Heute geöffnet“); sonst *zu* (nur ein
 * Hinweis, kein Knopf). Ob die Ziele erfüllt sind, sagt der Server (`freigeschaltet` = `rewardsUnlocked`): verdient wird
 * die Truhe aber erst auf dem Schreibweg – beim Abschluss einer Runde oder eines Tests (`DailyBoxService`). Erfüllt und
 * noch nicht verdient sagt die Truhe darum genau das, statt „Schaffe deine Tagesziele“ neben erfüllten Zielen.
 * Eine ältere Truhe zeigt nach dem Öffnen ihren Inhalt, gilt aber nicht als „heute“. Den Inhalt
 * nennt der Server erst nach dem Öffnen – die Truhe kann ihn vorher gar nicht verraten.
 */
export function Truhe({ box, heute, ziele, freigeschaltet, onVeraltet }: {
  box: DailyBoxStatus; heute: string; ziele: TruhenZiele; freigeschaltet: boolean; onVeraltet: () => void;
}) {
  const { t } = useTranslation();
  const { childId, refreshWallet } = useSohn();
  const errorMessage = useErrorMessage();
  const action = useAction(errorMessage);
  const flaeche = useRef<HTMLElement | null>(null);
  const [geoeffnet, setGeoeffnet] = useState<DailyBoxOpened | null>(null);
  // Nach `daily_box_none` (Doppeltipp, zweiter Tab) wartet nichts mehr, bis die neu geladene Übersicht da ist.
  const [leer, setLeer] = useState(false);
  useEffect(() => { setLeer(false); }, [box]);

  const wartend = leer ? 0 : geoeffnet ? geoeffnet.waitingAfter : box.waiting;
  const inhalt = geoeffnet
    ? { coins: geoeffnet.coins, gems: geoeffnet.gems }
    : box.openedToday && box.coinsAwarded != null ? { coins: box.coinsAwarded, gems: box.gemsAwarded ?? 0 } : null;
  const heuteOffen = box.openedToday || geoeffnet?.day === heute;
  const zustand = wartend > 0 ? "bereit" : heuteOffen ? "offen" : "zu";

  async function oeffnen() {
    let leerGemeldet = false;
    const res = await action.runFor(async () => {
      try {
        return await api.openDailyBox(childId);
      } catch (e) {
        leerGemeldet = e instanceof ApiError && e.code === "daily_box_none";
        throw e;
      }
    });
    if (!res) {
      // Die Truhe ist schon offen: nicht weiter leuchten lassen, und den wahren Stand vom Server holen.
      if (leerGemeldet) { setLeer(true); onVeraltet(); }
      return;
    }
    setGeoeffnet(res);
    refreshWallet();
    // Der Knopf verschwindet mit dem Öffnen; ohne Fokus-Landung fiele der Fokus auf `body` (Muster B-231).
    flaeche.current?.focus();
  }

  return (
    <section ref={flaeche} tabIndex={-1} className={`truhe ${zustand}`} aria-label={t("truhe.title")}>
      {/* Der Schlüssel startet das Wackeln bei jedem Öffnen neu; ohne Bewegung bleibt nur der Deckel offen. */}
      <div key={geoeffnet ? `${geoeffnet.day}-${geoeffnet.waitingAfter}` : "kiste"}
        className={`truhe-kiste${geoeffnet && !prefersReducedMotion() ? " oeffnet" : ""}`} aria-hidden="true">
        {zustand === "zu" ? "🔒" : zustand === "bereit" ? "🎁" : "📭"}
      </div>
      {/* Live-Regionen stehen immer im DOM und tragen ihren Text nur bedingt (B-134) – sonst sagt sie niemand an. */}
      <div className="truhe-inhalt" role="status">
        {inhalt ? (inhalt.gems > 0 ? t("truhe.content", inhalt) : t("truhe.contentCoins", inhalt)) : null}
      </div>
      {zustand === "zu" && <p className="sub">{freigeschaltet ? t("truhe.comesNext") : t(ZU_TEXT[ziele])}</p>}
      {zustand === "offen" && <span className="pill lime">{t("truhe.opened")}</span>}
      {zustand === "bereit" && (
        <>
          <b>{t("truhe.ready")}</b>
          {wartend > 1 && <span className="truhe-zahl">{t("truhe.waiting", { count: wartend })}</span>}
          <button type="button" className="btn gold" disabled={action.busy} onClick={oeffnen}>{t("truhe.open")}</button>
        </>
      )}
      <div role="alert">{action.message && !action.message.ok ? <div className="error-box">{action.message.text}</div> : null}</div>
    </section>
  );
}
