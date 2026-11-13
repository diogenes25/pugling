import { useEffect, useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useErrorMessage } from "../lib/chromeText";
import { useAction } from "../lib/useAction";
import { Mascot } from "../components/Mascot";
import { TaskAnnouncer } from "../components/ClozePrompt";
import { RevealAlternatives } from "../components/RevealAlternatives";
import { useSohn } from "./SohnApp";
import { aufgabenform } from "./aufgabenform";
import { Antwortflaeche, Aufgabe, LektionsRahmen } from "./Lektion";
import { SelfAssessAnswer } from "./SohnTest";
import type { ExtraAnswer, ExtraExercise, ExtraForm, ExtraRunResponse, ExtraRunResult, ExtraTrainingOverview } from "../lib/types";

/*
 * Das Bonus-Training: Extra-Übungen zum aktuellen Thema, offen erst nach der Tagespflicht. Alle Regeln liegen beim
 * Server – welche Übungen dazugehören, in welcher Form eine Runde fragt (Vokabeln: Karteikarte → Auswahl → Tippen),
 * was sie zahlt und wo der Tagesdeckel kappt. Hier wird nur angezeigt und am Ende einer Runde abgegeben.
 */

const FORM_KEY = {
  SelfAssess: "bonus.formSelfAssess", Choice: "bonus.formChoice", Typed: "bonus.formTyped",
} as const satisfies Record<ExtraForm, string>;

/** Die Form einer Runde in Arcade-Sprache, mit dem Preis je Treffer. */
function useFormText() {
  const { t } = useTranslation();
  return (form: ExtraForm, coins: number) => t(FORM_KEY[form], { coins });
}

// Die Antworten einer offenen Runde, je Runde im sessionStorage: „Später weiter“ soll dort weitermachen, wo das Kind
// war. Eine Bequemlichkeit – fehlt der Speicher (privates Fenster), beginnt die Runde eben bei Karte 1.
const answersKey = (runId: number) => `pugling.bonus.run.${runId}`;
function loadAnswers(runId: number): ExtraAnswer[] {
  try { return JSON.parse(sessionStorage.getItem(answersKey(runId)) ?? "[]") as ExtraAnswer[]; } catch { return []; }
}
function saveAnswers(runId: number, all: ExtraAnswer[]) {
  try { sessionStorage.setItem(answersKey(runId), JSON.stringify(all)); } catch { /* ohne Speicher von vorn */ }
}
function clearAnswers(runId: number) {
  try { sessionStorage.removeItem(answersKey(runId)); } catch { /* nichts zu räumen */ }
}

/** Der Einstieg auf dem Pfad: gesperrt, bis die Tagespflicht erledigt ist – die Sperre selbst hält der Server. */
export function BonusEinstieg({ offen }: { offen: boolean }) {
  const { t } = useTranslation();
  return (
    <div className="card bonus-einstieg" style={{ borderColor: offen ? "var(--gold)" : undefined }}>
      <div className="row">
        <span style={{ fontSize: 26 }} aria-hidden="true">{offen ? "⭐" : "🔒"}</span>
        <b>{t("bonus.title")}</b>
      </div>
      <p className="sub" style={{ margin: "6px 0" }}>{offen ? t("bonus.teaserOpen") : t("bonus.teaserLocked")}</p>
      {offen && (
        <Link to="/sohn/bonus" className="btn gold small" style={{ textDecoration: "none", textAlign: "center" }}>{t("bonus.enter")}</Link>
      )}
    </div>
  );
}

/** Die Übersicht: Sperre oder Pool, dazu die Münzen des Tages gegen den Deckel. */
export function SohnBonus() {
  const { t } = useTranslation();
  const { childId, skin } = useSohn();
  const overview = useAsync<ExtraTrainingOverview>(() => api.extraTraining(childId), [childId]);

  if (overview.loading && overview.data === null) return <div className="sohn-body"><div className="loading">{t("bonus.loading")}</div></div>;
  if (overview.error || !overview.data) return <div className="sohn-body"><div className="error-box" role="alert">{overview.error ?? t("home.error")}</div></div>;
  const o = overview.data;
  const capReached = o.dailyCoinCap != null && o.coinsToday >= o.dailyCoinCap;
  // Ein Deckel von 0 heißt „zahlt nichts“ – dort gibt es kein „morgen wieder“ zu versprechen.
  const capZero = o.dailyCoinCap === 0;

  return (
    <div className="sohn-body">
      <div className="row">
        <Mascot skin={skin} mood={o.unlocked ? "hyped" : "sleepy"} size={72} />
        <div>
          <h1 className="screen-title" style={{ margin: 0 }}>{t("bonus.title")}</h1>
          <div className="sub">
            {o.dailyCoinCap != null
              ? t("bonus.coinsToday", { coins: o.coinsToday, cap: o.dailyCoinCap })
              : t("bonus.coinsTodayNoCap", { coins: o.coinsToday })}
          </div>
        </div>
      </div>

      {!o.unlocked ? (
        <div className="card">
          <p className="sub" style={{ marginTop: 0 }}>{o.lockReason === "no_plan" ? t("bonus.lockedNoPlan") : t("bonus.lockedDuty")}</p>
          {o.outstanding.length > 0 && (
            <>
              <b style={{ fontSize: 13 }}>{t("bonus.stillOpen")}</b>
              <ul className="sub" style={{ margin: "4px 0 0 18px", padding: 0 }}>
                {o.outstanding.map((s) => <li key={s}>{s}</li>)}
              </ul>
            </>
          )}
          <Link to="/sohn" className="btn ghost small" style={{ textDecoration: "none", textAlign: "center", marginTop: 10 }}>{t("progress.toHome")}</Link>
        </div>
      ) : (
        <>
          {capReached && <div className="card" style={{ borderColor: "var(--gold)" }}><p className="sub" style={{ margin: 0 }}>{t(capZero ? "bonus.capZero" : "bonus.capReached")}</p></div>}
          {o.exercises.length === 0 && <div className="card"><p className="sub" style={{ margin: 0 }}>{t("bonus.empty")}</p></div>}
          {o.exercises.map((e) => <BonusZeile key={e.exerciseId} e={e} capReached={capReached} capZero={capZero} />)}
        </>
      )}
    </div>
  );
}

/**
 * Eine Zeile des Pools. Bei vollem Deckel bietet eine Einmal-Übung keinen Start an: der Server wiese ihn ab
 * (`extra_cap_reached`), weil sie sonst für 0 Münzen verbraucht wäre – die Zeile sagt stattdessen, wann sie wieder geht.
 */
function BonusZeile({ e, capReached, capZero }: { e: ExtraExercise; capReached: boolean; capZero: boolean }) {
  const { t } = useTranslation();
  const formText = useFormText();
  const wartetAufMorgen = capReached && !e.repeatable && !e.done;
  return (
    <div className={`card bonus-zeile${e.done ? " erledigt" : ""}`}>
      <div className="row">
        <b>{e.title}</b>
        {e.unitLabel && <span className="pill" style={{ marginLeft: "auto" }}>{e.unitLabel}</span>}
      </div>
      <div className="sub" style={{ margin: "4px 0 8px" }}>
        {e.done ? t("bonus.done") : formText(e.nextForm, e.coinsPerHit)}
        {e.repeatable && e.completedRuns > 0 && <> · {t("bonus.played", { count: e.completedRuns })}</>}
      </div>
      {wartetAufMorgen && <div className="sub"><b>{t(capZero ? "bonus.oneShotNoCoins" : "bonus.oneShotTomorrow")}</b></div>}
      {!e.done && !wartetAufMorgen && (
        <Link to={`/sohn/bonus/${e.exerciseId}`} className="btn lime small" style={{ textDecoration: "none", textAlign: "center" }}
          aria-label={`${e.openRunId != null ? t("bonus.resume") : t("bonus.play")}: ${e.title}`}>
          {e.openRunId != null ? t("bonus.resume") : t("bonus.play")}
        </Link>
      )}
    </div>
  );
}

/**
 * Eine Bonus-Runde: alle Karten nacheinander, abgegeben wird am Stück. Kein Blatt nach jeder Karte – gewertet wird
 * erst am Ende, wie im Test, damit eine Runde nicht Karte für Karte verrät, was richtig war.
 */
export function SohnBonusRun() {
  const { t } = useTranslation();
  const { childId, skin, refreshWallet, celebrate } = useSohn();
  const { exerciseId: raw } = useParams();
  const exerciseId = Number(raw);
  const nav = useNavigate();
  const errorMessage = useErrorMessage();
  const formText = useFormText();

  const [run, setRun] = useState<ExtraRunResponse | null>(null);
  const [cursor, setCursor] = useState(0);
  const [answers, setAnswers] = useState<ExtraAnswer[]>([]);
  const [wert, setWert] = useState("");
  const [revealed, setRevealed] = useState(false);
  const [result, setResult] = useState<ExtraRunResult | null>(null);
  const [next, setNext] = useState<ExtraExercise | null>(null);
  const [error, setError] = useState<string | null>(null);
  const action = useAction(errorMessage);
  const errorBox = useRef<HTMLDivElement | null>(null);
  // Hat das Kind die Runde verlassen, darf eine später antwortende Abgabe es nicht zurückholen und nicht anderswo
  // feiern (Muster `SohnTest`). Beim Einhängen zurückgesetzt, weil StrictMode im Dev einmal aus- und wieder einhängt.
  const gone = useRef(false);
  useEffect(() => { gone.current = false; return () => { gone.current = true; }; }, []);
  // Der Fehler ersetzt die Lektion samt fokussiertem Knopf: ohne Fokus-Landung fiele er auf `body`.
  useEffect(() => { if (error) errorBox.current?.focus(); }, [error]);

  // Ein Effekt kann zweimal laufen (StrictMode); der Server setzt zwar die offene Runde fort, aber ein zweiter POST
  // ist unnötig – dasselbe Ref-Gate wie im Test.
  const startedFor = useRef<number | null>(null);
  useEffect(() => {
    if (startedFor.current === exerciseId) return;
    startedFor.current = exerciseId;
    api.startExtraRun(childId, exerciseId).then((r) => {
      // Die Runde bleibt beim Server offen und kommt mit denselben Karten zurück; die schon gegebenen Antworten liegen
      // nur hier – ohne sie hieße „Weiterspielen“ in Wahrheit „von vorn“.
      const saved = loadAnswers(r.runId).filter((a) => r.cards.some((c) => c.itemIndex === a.itemIndex));
      setRun(r); setAnswers(saved); setCursor(Math.min(saved.length, r.cards.length - 1));
      if (saved.length >= r.cards.length) void submit(r, saved);
    }, (e) => setError(errorMessage(e)));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [childId, exerciseId]);

  async function submit(r: ExtraRunResponse, all: ExtraAnswer[]) {
    await action.run(async () => {
      let res: ExtraRunResult;
      try {
        res = await api.submitExtraRun(childId, r.runId, all);
      } catch (e) {
        if (!gone.current) setError(errorMessage(e));
        throw e;
      }
      clearAnswers(r.runId);
      refreshWallet();
      if (gone.current) return;
      setResult(res);
      if (res.coinsAwarded > 0) celebrate("small", "⭐", undefined, `+${res.coinsAwarded} 🪙`);
      // Was die nächste Runde fragt, weiß nur der Server – eine Zugabe: scheitert sie, bleibt das Ergebnis stehen.
      api.extraTraining(childId).then(
        (o) => { if (!gone.current) setNext(o.exercises.find((x) => x.exerciseId === exerciseId) ?? null); },
        () => {});
    });
  }

  function answer(a: ExtraAnswer) {
    if (!run) return;
    const all = [...answers, a];
    setAnswers(all); setWert(""); setRevealed(false); saveAnswers(run.runId, all);
    // Auch nach der letzten Karte weiter: dann zeigt das Bild „Wird ausgewertet …“ statt der beantworteten Karte.
    setCursor(cursor + 1);
    if (all.length >= run.cards.length) void submit(run, all);
  }

  const card = run && !result && !error && cursor < run.cards.length ? run.cards[cursor] : null;
  return (
    <>
      <TaskAnnouncer text={card?.prompt} gapIndex={card?.gapIndex} />
      {view()}
    </>
  );

  function view() {
    if (error) return (
      <div className="sohn-body"><div className="error-box" role="alert" tabIndex={-1} ref={errorBox}>{error}</div>
        <button type="button" className="btn ghost" onClick={() => nav("/sohn/bonus")}>{t("bonus.back")}</button></div>
    );
    if (result) return (
      <div className="sohn-body">
        <div className="victory">
          <div className="vtitle win">{t("bonus.resultTitle", { correct: result.correct, total: result.total })}</div>
          <Mascot skin={skin} mood={result.coinsAwarded > 0 ? "hyped" : "happy"} size={84} />
          <p className="sub">{result.coinsAwarded > 0 ? t("bonus.earned", { coins: result.coinsAwarded }) : t("bonus.noCoins")}</p>
          {result.capReached && <p className="sub">{t("bonus.capCut")}</p>}
          {next && next.repeatable && next.nextForm !== result.form && (
            <p className="sub">{t("bonus.nextHarder", { form: formText(next.nextForm, next.coinsPerHit) })}</p>
          )}
          <div className="card" style={{ width: "100%", textAlign: "left" }}>
            {result.items.map((o) => (
              <div className="row" key={o.itemIndex} style={{ padding: "4px 0" }}>
                <span aria-hidden="true">{o.wasCorrect ? "✅" : "❌"}</span>
                <span className="sr-only">{o.wasCorrect ? t("test.correct") : t("test.incorrect")}</span>
                <span className="test-prompt">{o.prompt}</span>
                <span className="sub" style={{ marginLeft: "auto", textAlign: "right" }}>{o.expected}</span>
              </div>
            ))}
          </div>
          <button type="button" className="btn gold" onClick={() => nav("/sohn/bonus")}>{t("bonus.back")}</button>
        </div>
      </div>
    );
    if (!run || !card) return (
      <div className="sohn-body"><div className="loading">{run ? t("bonus.submitting") : t("bonus.loading")}</div>
        <button type="button" className="btn ghost" onClick={() => nav("/sohn/bonus")}>{t("bonus.back")}</button></div>
    );

    const form = aufgabenform(card);
    const typed = form.antwort !== "selbst" && form.antwort !== "kennenlernen";
    return (
      <LektionsRahmen
        test
        // Die Runde bleibt beim Server offen und wird mit denselben Karten fortgesetzt – darum ohne Rückfrage.
        schliessenName={t("bonus.later")} onSchliessen={() => nav("/sohn/bonus")}
        zaehler={t("bonus.cardOf", { n: cursor + 1, total: run.cards.length })} anteil={cursor / run.cards.length}
        anweisung={t(form.anweisung)}
        hinweis={run.coinsPerHit > 0 ? t("bonus.hintPaid", { coins: run.coinsPerHit }) : t("bonus.hintSelf")}
      >
        <Aufgabe
          karte={card} form={form} skin={skin} gedreht={!typed && revealed}
          hinweis={card.hint && typed && <div className="sub" style={{ marginTop: 6 }}>💡 {card.hint}</div>}
          aufloesung={!typed && revealed && (
            <>
              <div className="rev">→ {card.reveal ?? t("test.revealedFallback")}</div>
              <RevealAlternatives alternatives={card.revealAlternatives} />
            </>
          )}
        />
        <div className="lektion-antwort">
          {typed ? (
            <Antwortflaeche
              kartenId={card.itemIndex} karte={card} wert={wert} onWert={setWert} gesperrt={action.busy}
              knopf={t("practice.next")} feldName={t("test.solution")} platzhalter={t("test.solutionPlaceholder")}
              onAbschicken={() => answer({ itemIndex: card.itemIndex, givenAnswer: wert })}
            />
          ) : (
            <div className="lektion-fuss">
              <SelfAssessAnswer
                reveal={card.reveal} alternatives={card.revealAlternatives} revealed={revealed} loesungInDerKarte
                busy={action.busy} onReveal={() => setRevealed(true)}
                onJudge={(wasKnown) => answer({ itemIndex: card.itemIndex, wasKnown })}
              />
            </div>
          )}
        </div>
      </LektionsRahmen>
    );
  }
}
