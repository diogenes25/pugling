import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { ApiError, api } from "../lib/api";
import { useErrorMessage } from "../lib/chromeText";
import { useAction } from "../lib/useAction";
import { useSohn } from "./SohnApp";
import { Mascot } from "../components/Mascot";
import { LetterBoxes } from "../components/LetterBoxes";
import { LetterTiles } from "../components/LetterTiles";
import { AudioButton } from "../components/AudioButton";
import { ClozePrompt } from "../components/ClozePrompt";
import { ListRule } from "../components/ListRule";
import { Passage } from "../components/Passage";
import { RevealAlternatives } from "../components/RevealAlternatives";
import { BirkenbihlDecoding } from "../components/BirkenbihlDecoding";
import type { AnswerDto, TestItem, TestSubmitResponse } from "../lib/types";

// Vokabel-Teststufen (numerisch, serverseitig erzwungen): 1 Zeigen … 7 Ordnen.
const STAGE_KEY = {
  1: "test.stage1", 2: "test.stage2", 3: "test.stage3", 4: "test.stage4", 5: "test.stage5", 6: "test.stage6", 7: "test.stage7",
} as const;
const isKnownStage = (s: number): s is keyof typeof STAGE_KEY => s in STAGE_KEY;

/** Name der Leitner-Stufe in der Arcade-Sprache; eine unbekannte Stufe zeigt ihre Nummer. */
function useStageLabel() {
  const { t } = useTranslation();
  return (s: number) => (isKnownStage(s) ? t(STAGE_KEY[s]) : t("test.stageN", { n: s }));
}

/**
 * Abschlusstest = Klausur: strikt server-getrieben. Der Client holt jede Frage einzeln (nextTest),
 * schickt die Antwort (answerTest, ohne Korrektheit zurück) und kann NICHT zurück. Erst der Abschluss
 * (submitTest) liefert die Auswertung – wie eine echte Klassenarbeit.
 */
export function SohnTest() {
  const { planId, refreshWallet, setStreak, skin, celebrate } = useSohn();
  const { positionId: positionIdRaw } = useParams();
  const positionId = Number(positionIdRaw);
  const nav = useNavigate();
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const stageLabel = useStageLabel();

  const [attemptId, setAttemptId] = useState<number | null>(null);
  const [stage, setStage] = useState(0);
  const [total, setTotal] = useState(0);
  const [cursor, setCursor] = useState(0);
  const [item, setItem] = useState<TestItem | null>(null);
  const [typedAnswer, setTypedAnswer] = useState("");
  const [revealed, setRevealed] = useState(false);
  const [result, setResult] = useState<TestSubmitResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  // Ein Plan, der nicht mehr dran ist (B-330), lässt sich nicht fortsetzen – „Weiter versuchen“ holte dieselbe 403.
  const [planStopped, setPlanStopped] = useState(false);
  // Ref-Gate gegen den Doppelklick (B-49). Der Fehler speist die `error`-Vollbild-Anzeige; `action.message`
  // wird hier nicht gerendert.
  const action = useAction(errorMessage);

  async function finish(id: number) {
    if (!planId) return;
    const res = await api.submitTest(planId, positionId, id);
    setResult(res);
    setItem(null);
    celebrate(res.passed ? "big" : "small", res.passed ? "🎉" : "💪",
      res.passed ? t("test.victory") : undefined, res.passed ? `${res.scorePercent}%` : undefined);
    setStreak((await api.overview(planId)).currentStreak);
    refreshWallet();
  }

  async function start() {
    if (!planId) return;
    setError(null); setResult(null); setItem(null); setTypedAnswer(""); setRevealed(false); setCursor(0);
    // Ein Neustart aus dem Ergebnis trägt sonst noch den abgegebenen Versuch – scheitert er, böte „Weiter versuchen“ diesen an.
    setAttemptId(null); setPlanStopped(false);
    try {
      const a = await api.startTest(planId, positionId);
      setAttemptId(a.attemptId); setStage(a.stage); setTotal(a.totalItems);
      const first = await api.nextTest(planId, positionId, a.attemptId);
      if (first.done) await finish(a.attemptId);
      // `?? null`: ein nullable Feld darf im Vertrag auch fehlen, der Zustand kennt nur „keine Aufgabe".
      else { setItem(first.item ?? null); setCursor(first.cursor); }
    } catch (e) {
      // 404 beim Start: Plan oder Position gibt es nicht mehr (Reload nach dem Löschen, B-326). Die Basis korrigiert die
      // Wahl; die Serverzeile („Study plan not found.“) sagte dem Kind nur, dass etwas kaputt ist.
      if (e instanceof ApiError && e.status === 404) { nav("/sohn", { replace: true }); return; }
      setError(errorMessage(e));
    }
  }

  /**
   * Merkt, für welche Position bereits ein Versuch gestartet wurde. Nötig, weil ein Effekt
   * <b>zweimal laufen kann</b> (React-StrictMode im Dev, generell bei einem Remount) – ein
   * `alive`-Flag verhindert dann zwar das Setzen von State, aber nicht den bereits abgeschickten
   * POST. Ergebnis wären zwei Klausur-Versuche: der zweite gewinnt die Anzeige, die erste Antwort
   * landet aber noch auf dem ersten – der Test bliebe eine Frage vor dem Ende hängen.
   */
  const startedFor = useRef<string | null>(null);

  useEffect(() => {
    if (!planId || !positionId) { nav("/sohn"); return; }
    const key = `${planId}:${positionId}`;
    if (startedFor.current === key) return;
    startedFor.current = key;
    void start();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [planId, positionId, nav]);

  // Laufenden Versuch nach einem transienten Fehler fortsetzen: aktuelle Cursor-Frage erneut holen.
  async function resume() {
    if (!planId || attemptId === null) { await start(); return; }
    setError(null);
    try {
      const nx = await api.nextTest(planId, positionId, attemptId);
      if (nx.done) { await finish(attemptId); return; }
      setItem(nx.item ?? null); setCursor(nx.cursor); setTypedAnswer(""); setRevealed(false);
    } catch (e) { setError(errorMessage(e)); setPlanStopped(isPlanInactive(e)); }
  }

  // Antwort abgeben (server-geführt: der Server adressiert stets die aktuelle Cursor-Frage) und weiterrücken.
  async function answerAndAdvance(dto: AnswerDto) {
    if (!planId || attemptId === null) return;
    // Der Fehler wird im `catch` gesetzt, nicht danach aus `action.message` gelesen: das ist der Zustand des Renders,
    // in dem der Klick entstand, und war beim ersten Fehlschlag `null` (B-320). Ein von der Sperre verworfener
    // Doppelklick ruft `fn` gar nicht auf und zeigt darum zu Recht nichts.
    await action.run(async () => {
      try {
        const ack = await api.answerTest(planId, positionId, attemptId, dto);
        if (ack.done) { await finish(attemptId); return; }
        const nx = await api.nextTest(planId, positionId, attemptId);
        if (nx.done) { await finish(attemptId); return; }
        setItem(nx.item ?? null); setCursor(nx.cursor); setTypedAnswer(""); setRevealed(false);
      } catch (e) {
        setError(errorMessage(e));
        setPlanStopped(isPlanInactive(e));
        throw e;
      }
    });
  }

  // Bei einem transienten Fehler den LAUFENDEN Versuch fortsetzen können (nicht verwerfen): resume() holt
  // die aktuelle Frage des bestehenden Attempts erneut, statt über start() einen neuen Versuch anzulegen.
  if (error) return <div className="sohn-body"><div className="error-box">{error}</div>
    {attemptId !== null && !planStopped && <button type="button" className="btn lime" onClick={resume}>{t("test.resume")}</button>}
    <button type="button" className="btn ghost" onClick={() => nav("/sohn")}>{t("progress.toHome")}</button></div>;

  if (result) return (
    <TestResult result={result} skin={skin} onHome={() => nav("/sohn")} onRetry={start} />
  );

  if (!item) return <div className="sohn-body"><div className="loading">{t("test.preparing")}</div></div>;

  // Getippte Stufe: Server liefert keine aufgedeckte Lösung (reveal === null) → Eingabefeld.
  const typed = item.reveal === null;
  const submitTyped = () => { if (typedAnswer.trim()) answerAndAdvance({ itemIndex: item.itemIndex, givenAnswer: typedAnswer }); };

  return (
    <div className="sohn-body">
      <div className="row">
        <span className="screen-title" style={{ margin: 0 }}>{t("test.title")}</span>
        <span className="row" style={{ marginLeft: "auto", gap: 8 }}>
          <span className="pill cyan">{t("test.questionOf", { n: Math.min(cursor + 1, total), total })}</span>
          <span className="pill mag">{stageLabel(stage)}</span>
          {/*
            „Später weiter" statt „Abbrechen": der Versuch bleibt am Cursor stehen und wird beim nächsten
            Start fortgesetzt (der Server gibt den laufenden Versuch zurück, statt einen neuen anzulegen).
            Deshalb kein Server-Aufruf und keine Rückfrage – es geht nichts verloren, und ein versehentliches
            Verlassen verbraucht keinen der begrenzten Versuche.
          */}
          <button type="button" className="pill toggle-pill exit-pill" onClick={() => nav("/sohn")}>
            {t("test.later")}
          </button>
        </span>
      </div>
      <p className="sub">{typed ? t("test.typedHint") : t("test.selfHint")}</p>

      {/* Aufdecken dreht die Karte um wie beim Üben (B-35); getippte Stufen decken nichts auf. */}
      <div className={!typed && revealed ? "card flipped" : "card"}>
        {/* Der Stoff, auf den sich die Frage bezieht – wie beim Üben, sonst wäre die Klausur die härtere
            Aufgabe bei weniger Material. */}
        <Passage text={item.passage} />
        {/* Aufnahme und Frage nebeneinander; ob das Wort verschwiegen wird, entscheidet der Server.
            Bedienelemente, wo die Aufnahme Material neben einer Frage ist (siehe SohnPractice). */}
        {item.audioUrl && (
          <AudioButton url={item.audioUrl} autoPlay={!item.prompt} withControls={!!item.prompt} />
        )}
        <ClozePrompt text={item.prompt} gapIndex={item.gapIndex} className="test-prompt" />
        {/* Auf der VORDERSEITE, direkt unter dem Satz, wie beim Üben (B-93): die Klausur darf nicht weniger
            Stoff zeigen als die Übung, sonst wäre sie die härtere Aufgabe bei weniger Material. */}
        <BirkenbihlDecoding decoding={item.decoding} />
        <ListRule type={item.type} anyOrder={item.anyOrder} itemIndex={item.itemIndex} />
        {item.hint && typed && <div className="sub" style={{ marginTop: 6 }}>💡 {item.hint}</div>}

        {typed ? (
          item.choices ? (
            <div className="row" style={{ marginTop: 10, gap: 8, flexWrap: "wrap" }}
              role="group" aria-label={t("practice.choices")}>
              {item.choices.map((c, i) => (
                <button type="button" key={`${i}-${c}`} className="btn ghost small" disabled={action.busy}
                  onClick={() => answerAndAdvance({ itemIndex: item.itemIndex, givenAnswer: c })}>{c}</button>
              ))}
            </div>
          ) : item.letterTiles ? (
            <div style={{ marginTop: 10 }}>
              <LetterTiles tiles={item.letterTiles} value={typedAnswer} onChange={setTypedAnswer} onSubmit={submitTyped} />
              <button type="button" className="btn lime" style={{ marginTop: 10 }} disabled={action.busy || !typedAnswer.trim()} onClick={submitTyped}>{t("practice.next")}</button>
            </div>
          ) : item.answerLength ? (
            <div style={{ marginTop: 10 }}>
              <LetterBoxes length={item.answerLength} value={typedAnswer} onChange={setTypedAnswer} onSubmit={submitTyped} pattern={item.answerPattern ?? undefined} />
              <button type="button" className="btn lime" style={{ marginTop: 10 }} disabled={action.busy || !typedAnswer.trim()} onClick={submitTyped}>{t("practice.next")}</button>
            </div>
          ) : (
            <div>
              <form onSubmit={(e) => { e.preventDefault(); submitTyped(); }}>
                <input
                  aria-label={t("test.solution")}
                  name={`answer-${item.itemIndex}`}
                  autoComplete="off"
                  autoCapitalize="off"
                  autoCorrect="off"
                  spellCheck={false}
                  style={{ marginTop: 10, width: "100%", background: "#0c0e2c", border: "1.5px solid var(--stroke)", borderRadius: 12, color: "var(--ink)", padding: 12, fontSize: 15 }}
                  placeholder={t("test.solutionPlaceholder")}
                  value={typedAnswer}
                  onChange={(e) => setTypedAnswer(e.target.value)}
                />
              </form>
              <button type="button" className="btn lime" style={{ marginTop: 10 }} disabled={action.busy || !typedAnswer.trim()} onClick={submitTyped}>{t("practice.next")}</button>
            </div>
          )
        ) : (
          <SelfAssessAnswer
            reveal={item.reveal}
            alternatives={item.revealAlternatives}
            revealed={revealed}
            busy={action.busy}
            onReveal={() => setRevealed(true)}
            onJudge={(wasKnown) => answerAndAdvance({ itemIndex: item.itemIndex, wasKnown })}
          />
        )}
      </div>
    </div>
  );
}

/**
 * Die Selbsteinschätzung einer Klausurfrage: erst denken, dann aufdecken, dann ehrlich urteilen.
 *
 * **Die Reihenfolge IST die Prüfung.** Stünde die Lösung sofort neben „Gewusst / Nicht gewusst", läse das Kind
 * sie und trüge sich „Gewusst" ein – der Test wäre ein Formular, keine Prüfung. Der Zustand `revealed` ist
 * darum die einzige Bedingung: `item.reveal` ist auf einer nicht-getippten Stufe **immer** gesetzt (der Server
 * schickt die Lösung mit der Frage), ein zusätzliches `|| reveal !== null` machte das Aufdecken zu totem Code.
 *
 * **Exportiert, damit die Reihenfolge prüfbar ist** (Muster `TestResult`): eine reine Props-Komponente, kein
 * Laden, kein `fetch` – der Fehler war unsichtbar, weil ihn niemand ohne den Server ansehen konnte.
 */
export function SelfAssessAnswer({ reveal, alternatives, revealed, busy, onReveal, onJudge }: {
  // Wie `alternatives` optional: ein nullable Feld darf im Vertrag auch fehlen (siehe `?? null` in `start`).
  reveal?: string | null;
  alternatives?: readonly string[] | null;
  revealed: boolean;
  busy: boolean;
  onReveal: () => void;
  onJudge: (wasKnown: boolean) => void;
}) {
  const { t } = useTranslation();
  if (!revealed) return (
    <div style={{ marginTop: 10 }}>
      <button type="button" className="btn ghost small" onClick={onReveal}>{t("test.reveal")}</button>
    </div>
  );
  return (
    <div style={{ marginTop: 10 }}>
      <div className="rev" style={{ color: "var(--cyan)", fontWeight: 800, marginBottom: 8 }}>→ {reveal ?? t("test.revealedFallback")}</div>
      <RevealAlternatives alternatives={alternatives} />
      <div className="judge" style={{ marginTop: 8 }}>
        <button type="button" className="btn red small" disabled={busy} onClick={() => onJudge(false)}>{t("test.notKnown")}</button>
        <button type="button" className="btn lime small" disabled={busy} onClick={() => onJudge(true)}>{t("test.known")}</button>
      </div>
    </div>
  );
}

/**
 * Der Auswertungsschirm der Klausur. **Exportiert, damit er prüfbar ist**: er ist eine reine
 * Props-Komponente (kein Laden, kein `fetch`), und genau die Regeln, die hier hängen – teilen alle Zeilen
 * ihren Aufgabentext? gibt es Fehlnennungen? – sind die, die man sonst nur von Hand ansehen könnte.
 */
export function TestResult({ result, skin, onHome, onRetry }: {
  result: TestSubmitResponse; skin: import("../lib/skins").Skin; onHome: () => void; onRetry: () => void;
}) {
  const { t } = useTranslation();
  const stageLabel = useStageLabel();
  const pct = result.scorePercent;
  const ring = { background: `conic-gradient(${result.passed ? "var(--lime)" : "var(--red)"} 0 ${pct}%, #0c0e2c ${pct}% 100%)` };
  // Teilen alle Zeilen ihren Aufgabentext (eine Menge – jede Zeile trägt dieselbe Anweisung), steht er EINMAL
  // oben, und groß gesetzt wird die Lösung. Sonst schreit 16-mal derselbe Auftrag und die Antwort, die das Kind
  // lernen soll, flüstert daneben. Lückentexte sind ausgenommen: dort unterscheidet `gapIndex` die Zeilen, und
  // ihr Text gehört in jede (B-76).
  const sharedPrompt = result.items.length > 1
    && result.items.every((o) => o.prompt === result.items[0].prompt && o.gapIndex == null)
    ? result.items[0].prompt
    : null;
  return (
    <div className="sohn-body">
      <div className="victory">
        <div style={{ fontFamily: "var(--font-display)", letterSpacing: ".2em", color: "var(--muted)", fontSize: 12 }}>
          {t("test.header", { stage: stageLabel(result.stage).toUpperCase() })}
        </div>
        <div className={`vtitle ${result.passed ? "win" : "lose"}`}>{result.passed ? t("test.victory") : t("test.almost")}</div>
        <div className="ring" style={ring}><b>{pct}%<small>{result.correctItems} / {result.totalItems}</small></b></div>
        <Mascot skin={skin} mood={result.passed ? "hyped" : "sleepy"} size={84} />
        <p className="sub">{t("test.passMark", { pct: result.passPercent })}</p>

        <div className="card" style={{ width: "100%", marginTop: 4, textAlign: "left" }}>
          {sharedPrompt && <p className="sub" style={{ marginTop: 0 }}>{sharedPrompt}</p>}
          {result.items.map((o) => (
            <div className="row" key={o.itemIndex} style={{ padding: "4px 0" }}>
              {/* Das Zeichen ist die Optik, das Wort die Bedeutung: allein gelesen heißt „❌ Berlin" für das Kind
                  „Berlin war falsch", während der Server „Berlin hast du nicht genannt" meint – und genau das ist
                  bei einer Menge die Lehre der Übung (B-77/E2). */}
              <span aria-hidden="true">{o.wasCorrect ? "✅" : "❌"}</span>
              {!sharedPrompt && <span className="sr-only">{o.wasCorrect ? t("test.correct") : t("test.incorrect")}</span>}
              {sharedPrompt
                // Die Lösung ist hier die Lehre – sie steht groß, wo der Aufgabentext oben schon steht.
                ? <>
                    <span className="test-prompt">{o.expected}</span>
                    <span className="sub" style={{ marginLeft: "auto" }}>{o.wasCorrect ? t("test.named") : t("test.notNamed")}</span>
                  </>
                : <>
                    {/* Auch hier die Lücke ausweisen: sonst stehen beim Lückentext lauter gleiche Zeilen –
                        ausgerechnet auf dem Bildschirm, auf dem das Kind seine Fehler nachliest. */}
                    <ClozePrompt text={o.prompt} gapIndex={o.gapIndex} className="test-prompt" />
                    <span className="sub" style={{ marginLeft: "auto" }}>{o.expected}</span>
                  </>}
            </div>
          ))}
        </div>

        {/* Bei einer Menge zählt die Zeile darüber, was das Kind VERGESSEN hat – ohne diese Liste
            verschwände lautlos, was es tatsächlich getippt hat (B-77/E2). */}
        {result.wrongMentions && result.wrongMentions.length > 0 && (
          <div className="card" style={{ width: "100%", marginTop: 4, textAlign: "left" }}>
            <p className="sub" style={{ marginTop: 0 }}>{t("test.didNotCount")}</p>
            {result.wrongMentions.map((m, i) => (
              // Bewusst leiser als die Lösung oben: was das Kind fälschlich getippt hat, ist die Nebensache –
              // die Lehre ist der Eintrag, den es vergessen hat.
              <div className="row" key={`${m}-${i}`} style={{ padding: "4px 0" }}>
                <span aria-hidden="true">❌</span>
                <span className="sub">{m}</span>
              </div>
            ))}
          </div>
        )}

        {/* Ohne attemptsRemaining bot der Knopf einen Versuch an, den der Server längst abgewiesen hätte
            (ApiErrors.TestAttemptsExhausted, B-62) - ein Klick danach landete nur in der Fehlerbox. */}
        {!result.passed && result.attemptsRemaining > 0 &&
          <button type="button" className="btn gold" onClick={onRetry}>{t("test.tryAgain")}</button>}
        <button type="button" className="btn ghost" onClick={onHome}>{t("progress.toHome")}</button>
      </div>
    </div>
  );
}

const isPlanInactive = (e: unknown) => e instanceof ApiError && e.code === "plan_inactive";
