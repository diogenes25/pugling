import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { api, errorMessage } from "../lib/api";
import { useSohn } from "./SohnApp";
import { Mascot } from "../components/Mascot";
import { LetterBoxes } from "../components/LetterBoxes";
import { AudioButton } from "../components/AudioButton";
import type { AnswerDto, TestItem, TestSubmitResponse } from "../lib/types";

// Vokabel-Teststufen (numerisch, serverseitig erzwungen): 1 Zeigen … 5 Hören.
const STAGE_LABEL: Record<number, string> = {
  1: "Zeigen", 2: "Selbstcheck", 3: "Buchstaben", 4: "Tippen", 5: "Hören", 6: "Auswahl",
};
const stageLabel = (s: number) => STAGE_LABEL[s] ?? `Stufe ${s}`;

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

  const [attemptId, setAttemptId] = useState<number | null>(null);
  const [stage, setStage] = useState(0);
  const [total, setTotal] = useState(0);
  const [cursor, setCursor] = useState(0);
  const [item, setItem] = useState<TestItem | null>(null);
  const [typedAnswer, setTypedAnswer] = useState("");
  const [revealed, setRevealed] = useState(false);
  const [result, setResult] = useState<TestSubmitResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function finish(id: number) {
    if (!planId) return;
    const res = await api.submitTest(planId, positionId, id);
    setResult(res);
    setItem(null);
    celebrate(res.passed ? "big" : "small", res.passed ? "🎉" : "💪",
      res.passed ? "SIEG!" : undefined, res.passed ? `${res.scorePercent}%` : undefined);
    setStreak((await api.overview(planId)).currentStreak);
    refreshWallet();
  }

  async function start() {
    if (!planId) return;
    setError(null); setResult(null); setItem(null); setTypedAnswer(""); setRevealed(false); setCursor(0);
    try {
      const a = await api.startTest(planId, positionId);
      setAttemptId(a.attemptId); setStage(a.stage); setTotal(a.totalItems);
      const first = await api.nextTest(planId, positionId, a.attemptId);
      if (first.done) await finish(a.attemptId);
      else { setItem(first.item); setCursor(first.cursor); }
    } catch (e) { setError(errorMessage(e)); }
  }

  useEffect(() => {
    if (!planId || !positionId) { nav("/sohn"); return; }
    let alive = true;
    (async () => { if (alive) await start(); })();
    return () => { alive = false; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [planId, positionId, nav]);

  // Laufenden Versuch nach einem transienten Fehler fortsetzen: aktuelle Cursor-Frage erneut holen.
  async function resume() {
    if (!planId || attemptId === null) { await start(); return; }
    setError(null);
    try {
      const nx = await api.nextTest(planId, positionId, attemptId);
      if (nx.done) { await finish(attemptId); return; }
      setItem(nx.item); setCursor(nx.cursor); setTypedAnswer(""); setRevealed(false);
    } catch (e) { setError(errorMessage(e)); }
  }

  // Antwort abgeben (server-geführt: der Server adressiert stets die aktuelle Cursor-Frage) und weiterrücken.
  async function answerAndAdvance(dto: AnswerDto) {
    if (!planId || attemptId === null || busy) return;
    setBusy(true);
    try {
      const ack = await api.answerTest(planId, positionId, attemptId, dto);
      if (ack.done) { await finish(attemptId); return; }
      const nx = await api.nextTest(planId, positionId, attemptId);
      if (nx.done) { await finish(attemptId); return; }
      setItem(nx.item); setCursor(nx.cursor); setTypedAnswer(""); setRevealed(false);
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  // Bei einem transienten Fehler den LAUFENDEN Versuch fortsetzen können (nicht verwerfen): resume() holt
  // die aktuelle Frage des bestehenden Attempts erneut, statt über start() einen neuen Versuch anzulegen.
  if (error) return <div className="sohn-body"><div className="error-box">{error}</div>
    {attemptId !== null && <button type="button" className="btn lime" onClick={resume}>Weiter versuchen</button>}
    <button type="button" className="btn ghost" onClick={() => nav("/sohn")}>Zur Basis</button></div>;

  if (result) return (
    <TestResult result={result} skin={skin} onHome={() => nav("/sohn")} onRetry={start} />
  );

  if (!item) return <div className="sohn-body"><div className="loading">Test wird vorbereitet…</div></div>;

  // Getippte Stufe: Server liefert keine aufgedeckte Lösung (reveal === null) → Eingabefeld.
  const typed = item.reveal === null;
  const submitTyped = () => { if (typedAnswer.trim()) answerAndAdvance({ itemIndex: item.itemIndex, givenAnswer: typedAnswer }); };
  const showSolution = revealed || item.reveal !== null;

  return (
    <div className="sohn-body">
      <div className="row">
        <span className="screen-title" style={{ margin: 0 }}>Tagestest</span>
        <span className="row" style={{ marginLeft: "auto", gap: 8 }}>
          <span className="pill cyan">Frage {Math.min(cursor + 1, total)} / {total}</span>
          <span className="pill mag">{stageLabel(stage)}</span>
        </span>
      </div>
      <p className="sub">{typed ? "Tippe die Lösung – zurück geht nicht." : "Denk nach, dann aufdecken und ehrlich bewerten."}</p>

      <div className="card">
        {/* Hör-Stufe: Wort vorlesen statt zeigen (sonst wäre „Hören → tippen" keine Höraufgabe). */}
        {item.audioUrl
          ? <AudioButton url={item.audioUrl} label="🔊 Vokabel anhören" />
          : <b style={{ fontSize: 17 }}>{item.prompt}</b>}
        {item.hint && typed && <div className="sub" style={{ marginTop: 6 }}>💡 {item.hint}</div>}

        {typed ? (
          item.choices ? (
            <div className="row" style={{ marginTop: 10, gap: 8, flexWrap: "wrap" }}>
              {item.choices.map((c) => (
                <button type="button" key={c} className="btn ghost small" disabled={busy}
                  onClick={() => answerAndAdvance({ itemIndex: item.itemIndex, givenAnswer: c })}>{c}</button>
              ))}
            </div>
          ) : item.answerLength ? (
            <div style={{ marginTop: 10 }}>
              <LetterBoxes length={item.answerLength} value={typedAnswer} onChange={setTypedAnswer} onSubmit={submitTyped} />
              <button type="button" className="btn lime" style={{ marginTop: 10 }} disabled={busy || !typedAnswer.trim()} onClick={submitTyped}>Weiter →</button>
            </div>
          ) : (
            <div>
              <form onSubmit={(e) => { e.preventDefault(); submitTyped(); }}>
                <input
                  aria-label="Lösung"
                  name={`answer-${item.itemIndex}`}
                  autoComplete="off"
                  autoCapitalize="off"
                  autoCorrect="off"
                  spellCheck={false}
                  style={{ marginTop: 10, width: "100%", background: "#0c0e2c", border: "1.5px solid var(--stroke)", borderRadius: 12, color: "var(--ink)", padding: 12, fontSize: 15 }}
                  placeholder="Lösung…"
                  value={typedAnswer}
                  onChange={(e) => setTypedAnswer(e.target.value)}
                />
              </form>
              <button type="button" className="btn lime" style={{ marginTop: 10 }} disabled={busy || !typedAnswer.trim()} onClick={submitTyped}>Weiter →</button>
            </div>
          )
        ) : (
          <div style={{ marginTop: 10 }}>
            {showSolution ? (
              <div className="rev" style={{ color: "var(--cyan)", fontWeight: 800, marginBottom: 8 }}>→ {item.reveal ?? "(aufgedeckt)"}</div>
            ) : (
              <button type="button" className="btn ghost small" onClick={() => setRevealed(true)}>Aufdecken 🔄</button>
            )}
            {showSolution && (
              <div className="judge" style={{ marginTop: 8 }}>
                <button type="button" className="btn red small" disabled={busy} onClick={() => answerAndAdvance({ itemIndex: item.itemIndex, wasKnown: false })}>Nicht gewusst</button>
                <button type="button" className="btn lime small" disabled={busy} onClick={() => answerAndAdvance({ itemIndex: item.itemIndex, wasKnown: true })}>Gewusst</button>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

function TestResult({ result, skin, onHome, onRetry }: {
  result: TestSubmitResponse; skin: import("../lib/skins").Skin; onHome: () => void; onRetry: () => void;
}) {
  const pct = result.scorePercent;
  const ring = { background: `conic-gradient(${result.passed ? "var(--lime)" : "var(--red)"} 0 ${pct}%, #0c0e2c ${pct}% 100%)` };
  return (
    <div className="sohn-body">
      <div className="victory">
        <div style={{ fontFamily: "var(--font-display)", letterSpacing: ".2em", color: "var(--muted)", fontSize: 12 }}>
          TEST · {stageLabel(result.stage).toUpperCase()}
        </div>
        <div className={`vtitle ${result.passed ? "win" : "lose"}`}>{result.passed ? "SIEG!" : "FAST!"}</div>
        <div className="ring" style={ring}><b>{pct}%<small>{result.correctItems} / {result.totalItems}</small></b></div>
        <Mascot skin={skin} mood={result.passed ? "hyped" : "sleepy"} size={84} />
        <p className="sub">Bestehensgrenze {result.passPercent}%</p>

        <div className="card" style={{ width: "100%", marginTop: 4, textAlign: "left" }}>
          {result.items.map((o) => (
            <div className="row" key={o.itemIndex} style={{ padding: "4px 0" }}>
              <span>{o.wasCorrect ? "✅" : "❌"}</span>
              <b>{o.prompt}</b>
              <span className="sub" style={{ marginLeft: "auto" }}>{o.expected}</span>
            </div>
          ))}
        </div>

        {!result.passed && <button type="button" className="btn gold" onClick={onRetry}>Nochmal versuchen</button>}
        <button type="button" className="btn ghost" onClick={onHome}>Zur Basis</button>
      </div>
    </div>
  );
}
