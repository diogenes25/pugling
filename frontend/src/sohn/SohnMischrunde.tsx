import { useEffect, useRef, useState, type Ref } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { api, ApiError } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useErrorMessage } from "../lib/chromeText";
import { useAction } from "../lib/useAction";
import { StatusBanner } from "../components/StatusBanner";
import { Mascot } from "../components/Mascot";
import { TaskAnnouncer } from "../components/ClozePrompt";
import { RevealAlternatives } from "../components/RevealAlternatives";
import { useVoiceFor } from "../components/SpeakButton";
import { ErgebnisZeile } from "./ErgebnisZeile";
import { useSohn } from "./SohnApp";
import { aufgabenform, istDiktat } from "./aufgabenform";
import { Antwortflaeche, Aufgabe, LektionsRahmen } from "./Lektion";
import { SelfAssessAnswer } from "./SohnTest";
import type { MixedReviewAnswer, MixedReviewOverview, MixedReviewResult, MixedReviewRunResponse } from "../lib/types";

/*
 * Die Mischrunde (B-766): fällige Karten aus allen Plänen in einer freiwilligen Runde. Was dazugehört, in welcher Stufe
 * jede Karte fragt, welcher Kasten sich bewegt und was sie zahlt, entscheidet der Server (B-764, B-765) – hier wird nur
 * angezeigt und am Ende einer Runde abgegeben.
 */

/** Wohin der Einstieg die gestartete Runde mitgibt, damit die Seite sie nicht noch einmal anfragt. */
type MischState = { run?: MixedReviewRunResponse } | null;

/**
 * Der Einstieg im Bonus-Training: nur, solange Karten fällig sind oder eine Runde offen ist – und nur hinter der Sperre des
 * Bonus-Trainings, darum hängt er im offenen Zweig von `SohnBonus`. Der Start ist eine Mutation: beim Senden gesperrt, ein
 * abgewiesener Start (nichts mehr fällig, Sperre) meldet sich am Platz.
 */
export function MischrundeEinstieg() {
  const { t } = useTranslation();
  const { childId } = useSohn();
  const nav = useNavigate();
  const errorMessage = useErrorMessage();
  const action = useAction(errorMessage);
  const overview = useAsync<MixedReviewOverview>(() => api.mixedReview(childId), [childId]);
  const o = overview.data;
  // Beiwerk des Bonus-Trainings: ohne Antwort (Ladefehler) bleibt der Einstieg einfach weg.
  const zeigen = o != null && o.unlocked && (o.dueCards > 0 || o.openRunId != null);

  async function starten() {
    const run = await action.runFor(() => api.startMixedReview(childId));
    if (run) nav("/sohn/bonus/misch", { state: { run } satisfies MischState });
  }

  return (
    <>
      {zeigen && (
        <div className="card bonus-zeile mischrunde-einstieg" style={{ borderColor: "var(--gold)" }}>
          <div className="row"><span aria-hidden="true">🔀</span><b>{t("mix.title")}</b></div>
          <div className="sub" style={{ margin: "4px 0 8px" }}>
            {/* Eine offene Runde, deren Karten inzwischen im Plan geübt wurden, zählt keine fälligen mehr. */}
            {o.dueCards > 0 ? t("mix.teaser", { count: Math.min(o.dueCards, o.roundSize) }) : t("mix.teaserOpen")}
          </div>
          <button type="button" className="btn lime small" onClick={() => void starten()} disabled={action.busy}>
            {o.openRunId != null ? t("mix.resume") : t("mix.start")}
          </button>
        </div>
      )}
      {/* Die Live-Region steht immer da (B-134); ein abgewiesener Start meldet sich hier, unter dem Knopf. */}
      <StatusBanner message={action.message} />
    </>
  );
}

// Die Antworten einer offenen Runde, je Runde im sessionStorage – „Später weiter“ macht dort weiter, wo das Kind war.
// Eine Bequemlichkeit: fehlt der Speicher (privates Fenster), beginnt die Runde eben bei Karte 1.
const answersKey = (runId: number) => `pugling.mix.run.${runId}`;
function loadAnswers(runId: number): MixedReviewAnswer[] {
  try { return JSON.parse(sessionStorage.getItem(answersKey(runId)) ?? "[]") as MixedReviewAnswer[]; } catch { return []; }
}
function saveAnswers(runId: number, all: MixedReviewAnswer[]) {
  try { sessionStorage.setItem(answersKey(runId), JSON.stringify(all)); } catch { /* ohne Speicher von vorn */ }
}
function clearAnswers(runId: number) {
  try { sessionStorage.removeItem(answersKey(runId)); } catch { /* nichts zu räumen */ }
}

/** Die Runde selbst: alle Karten nacheinander, abgegeben wird am Stück, das Ergebnis nennt Münzen und Tagesdeckel. */
export function SohnMischrunde() {
  const { t } = useTranslation();
  const { childId, skin, refreshWallet, celebrate } = useSohn();
  const nav = useNavigate();
  const location = useLocation();
  const errorMessage = useErrorMessage();
  const action = useAction(errorMessage);

  const [run, setRun] = useState<MixedReviewRunResponse | null>(null);
  const [cursor, setCursor] = useState(0);
  const [answers, setAnswers] = useState<MixedReviewAnswer[]>([]);
  const [wert, setWert] = useState("");
  const [revealed, setRevealed] = useState(false);
  const [result, setResult] = useState<MixedReviewResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const errorBox = useRef<HTMLDivElement | null>(null);
  // Hat das Kind die Runde verlassen, darf eine später antwortende Abgabe es nicht zurückholen (Muster `SohnBonusRun`).
  const gone = useRef(false);
  useEffect(() => { gone.current = false; return () => { gone.current = true; }; }, []);
  useEffect(() => { if (error) errorBox.current?.focus(); }, [error]);

  // Die Runde kommt vom Einstieg mit; ohne sie (Neuladen, Lesezeichen) setzt derselbe Start die offene Runde fort. Ein Effekt kann
  // zweimal laufen (StrictMode) – dasselbe Ref-Gate wie im Bonus-Training, damit nur ein POST geht.
  const started = useRef(false);
  useEffect(() => {
    if (started.current) return;
    started.current = true;
    const mitgegeben = (location.state as MischState)?.run;
    // Einmal verbrauchen: der Verlauf hielte die Runde sonst fest, und ein „Zurück“ nach dem Ergebnis (oder am nächsten
    // Tag) spielte eine abgegebene bzw. verworfene Runde ein zweites Mal durch, statt beim Server nachzufragen.
    if (mitgegeben) nav(location.pathname, { replace: true, state: null });
    const laden = mitgegeben ? Promise.resolve(mitgegeben) : api.startMixedReview(childId);
    laden.then((r) => {
      const saved = loadAnswers(r.runId).filter((a) => r.cards.some((c) => c.cardId === a.cardId));
      setRun(r); setAnswers(saved); setCursor(Math.min(saved.length, r.cards.length - 1));
      if (saved.length >= r.cards.length) void submit(r, saved);
    }, (e) => setError(errorMessage(e)));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [childId]);

  async function submit(r: MixedReviewRunResponse, all: MixedReviewAnswer[]) {
    await action.run(async () => {
      let res: MixedReviewResult;
      try {
        res = await api.submitMixedReview(childId, r.runId, all);
      } catch (e) {
        // Eine Runde von gestern verwirft der Server – ihre gespeicherten Antworten gehören zu nichts mehr.
        if (e instanceof ApiError && (e.code === "mixed_review_stale" || e.code === "mixed_review_completed")) clearAnswers(r.runId);
        if (!gone.current) setError(errorMessage(e));
        throw e;
      }
      clearAnswers(r.runId);
      refreshWallet();
      if (gone.current) return;
      setResult(res);
      if (res.coinsAwarded > 0) celebrate("small", "🔀", undefined, `+${res.coinsAwarded} 🪙`);
    });
  }

  // Nach einer Antwort nimmt das erste Bedienelement der nächsten Karte den Fokus (Muster Test/Bonus), das Ergebnis seinen Titel.
  const focusIntent = useRef(false);
  const answerRef = useRef<HTMLDivElement>(null);
  const resultTitleRef = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (!focusIntent.current) return;
    const waitingForResult = run != null && cursor >= run.cards.length && !result && !error;
    if (waitingForResult) return;
    focusIntent.current = false;
    if (result) resultTitleRef.current?.focus();
    else answerRef.current?.querySelector<HTMLElement>("input, button:not([disabled])")?.focus();
  }, [cursor, revealed, result, error, run]);

  function answer(a: MixedReviewAnswer) {
    if (!run) return;
    focusIntent.current = true;
    const all = [...answers, a];
    setAnswers(all); setWert(""); setRevealed(false); saveAnswers(run.runId, all);
    setCursor(cursor + 1);
    if (all.length >= run.cards.length) void submit(run, all);
  }

  const view = run && !result && !error && cursor < run.cards.length ? run.cards[cursor] : null;
  const card = view?.card ?? null;
  const diktatStimme = useVoiceFor(card?.dictationLang ?? "");
  return (
    <>
      <TaskAnnouncer text={card && istDiktat(card, diktatStimme !== null) ? t("lektion.listen") : card?.prompt}
        gapIndex={card?.gapIndex} />
      {render()}
    </>
  );

  function render() {
    const zurueck = <button type="button" className="btn ghost" onClick={() => nav("/sohn/bonus")}>{t("bonus.back")}</button>;
    if (error) return (
      <div className="sohn-body"><div className="error-box" role="alert" tabIndex={-1} ref={errorBox}>{error}</div>{zurueck}</div>
    );
    if (result) return <Ergebnis result={result} getippt={run?.cards.filter((c) => c.typed).map((c) => c.cardId) ?? []}
      titleRef={resultTitleRef} onZurueck={() => nav("/sohn/bonus")} skin={skin} />;
    if (!run || !view || !card) return (
      <div className="sohn-body"><div className="loading">{run ? t("bonus.submitting") : t("mix.loading")}</div>{zurueck}</div>
    );

    const form = aufgabenform(card, diktatStimme !== null);
    const typed = form.antwort !== "selbst" && form.antwort !== "kennenlernen";
    return (
      <LektionsRahmen
        test
        schliessenName={t("bonus.later")} onSchliessen={() => nav("/sohn/bonus")}
        zaehler={t("bonus.cardOf", { n: cursor + 1, total: run.cards.length })} anteil={cursor / run.cards.length}
        unterKopf={<div className="sub">{view.subjectName ?? view.planTitle} · {view.exerciseTitle}</div>}
        anweisung={t(form.anweisung)}
        // Ein leerer Tagesdeckel zahlt nichts; eine selbst eingeschätzte Karte zahlt nie – nur geprüfte Treffer (B-765).
        hinweis={run.coinsLeftToday === 0 || card.coinsPerHit === 0 ? t("mix.hintNoCoins") : t("mix.hintPaid", { coins: card.coinsPerHit })}
      >
        <Aufgabe
          karte={card} form={form} skin={skin} gedreht={!typed && revealed}
          hinweis={card.hint && typed && !card.setAnswer && <div className="sub" style={{ marginTop: 6 }}>💡 {card.hint}</div>}
          aufloesung={!typed && revealed && (
            <>
              <div className="rev">→ {card.reveal ?? t("test.revealedFallback")}</div>
              <RevealAlternatives alternatives={card.revealAlternatives} />
            </>
          )}
        />
        <div className="lektion-antwort" ref={answerRef}>
          {typed ? (
            <Antwortflaeche
              kartenId={view.cardId} karte={card} wert={wert} onWert={setWert} gesperrt={action.busy}
              knopf={t("practice.next")} feldName={t("test.solution")} platzhalter={t("test.solutionPlaceholder")}
              onAbschicken={() => answer({ cardId: view.cardId, givenAnswer: wert, wasKnown: null })}
            />
          ) : (
            <div className="lektion-fuss">
              <SelfAssessAnswer
                reveal={card.reveal} alternatives={card.revealAlternatives} revealed={revealed} loesungInDerKarte
                busy={action.busy} onReveal={() => { focusIntent.current = true; setRevealed(true); }}
                onJudge={(wasKnown) => answer({ cardId: view.cardId, givenAnswer: null, wasKnown })}
              />
            </div>
          )}
        </div>
      </LektionsRahmen>
    );
  }
}

/**
 * Das Ergebnis: richtig von gewertet, die Münzen und der Tagesdeckel über Bonus-Training und Mischrunde zusammen (B-766
 * Entscheidung 2). Ohne Münzen steht der Grund – Deckel 0, Deckel voll, oder Wörter, die heute schon zu sehen waren.
 */
export function Ergebnis({ result, getippt, titleRef, onZurueck, skin }: {
  result: MixedReviewResult;
  /** Die Karten der Runde, die geprüft (getippt oder gewählt) gefragt wurden – nur deren Treffer können zahlen. */
  getippt: number[];
  titleRef?: Ref<HTMLDivElement>;
  onZurueck: () => void;
  skin: Parameters<typeof Mascot>[0]["skin"];
}) {
  const { t } = useTranslation();
  const ohneMuenzen = result.coinsAwarded === 0;
  const grund = !ohneMuenzen ? null
    : result.dailyCoinCap === 0 ? t("mix.reasonCapZero")
    : result.capReached ? t("mix.reasonCapFull")
    // Ein geprüfter Treffer ohne Münzen: das Wort war heute schon zu sehen. Nur selbst eingeschätzte Treffer zahlen nie.
    : result.items.some((i) => i.wasCorrect && getippt.includes(i.cardId)) ? t("mix.reasonSeenToday")
    : result.correct > 0 ? t("mix.reasonSelfAssess")
    : t("bonus.noCoins");
  return (
    <div className="sohn-body">
      <div className="victory">
        <div className="vtitle win" ref={titleRef} tabIndex={-1}>{t("bonus.resultTitle", { correct: result.correct, total: result.total })}</div>
        <Mascot skin={skin} mood={result.coinsAwarded > 0 ? "hyped" : "happy"} size={84} />
        <p className="sub">{ohneMuenzen ? grund : t("bonus.earned", { coins: result.coinsAwarded })}</p>
        {!ohneMuenzen && result.capReached && <p className="sub">{t("bonus.capCut")}</p>}
        <p className="sub mischrunde-deckel">
          {result.dailyCoinCap != null
            ? t("bonus.coinsToday", { coins: result.coinsToday, cap: result.dailyCoinCap })
            : t("bonus.coinsTodayNoCap", { coins: result.coinsToday })}
        </p>
        <div className="card" style={{ width: "100%", textAlign: "left" }}>
          {result.items.map((o) => (
            // Eine übersprungene Karte wurde nicht gewertet: kein Richtig/Falsch, nur der Grund.
            o.skipped
              ? <div key={o.cardId} className="ergebnis-zeile sub">{t("mix.skipped", { title: o.exerciseTitle })}</div>
              : <ErgebnisZeile key={o.cardId} prompt={o.prompt} gapIndex={o.gapIndex} expected={o.expected}
                  givenAnswer={o.givenAnswer} wasCorrect={o.wasCorrect} akzent={o.accentSlip ?? false}
                  zeigeAntwort={o.givenAnswer != null} />
          ))}
        </div>
        <p className="sub">{t("mix.voluntary")}</p>
        <button type="button" className="btn gold" onClick={onZurueck}>{t("bonus.back")}</button>
      </div>
    </div>
  );
}
