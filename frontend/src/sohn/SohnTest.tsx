import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../lib/api";
import { useSohn } from "./SohnApp";
import { Mascot } from "../components/Mascot";
import type { AnswerDto, TestAttemptResponse, TestStage, TestSubmitResponse } from "../lib/types";

const TYPED: TestStage[] = ["LetterBoxes", "FreeText", "Audio"];

export function SohnTest() {
  const { planId, refreshWallet, setStreak, skin } = useSohn();
  const nav = useNavigate();

  const [attempt, setAttempt] = useState<TestAttemptResponse | null>(null);
  const [answers, setAnswers] = useState<Record<number, AnswerDto>>({});
  const [revealed, setRevealed] = useState<Set<number>>(new Set());
  const [result, setResult] = useState<TestSubmitResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!planId) { nav("/sohn"); return; }
    let alive = true;
    api.startTest(planId)
      .then((a) => { if (alive) setAttempt(a); })
      .catch((e) => { if (alive) setError(e instanceof Error ? e.message : "Test konnte nicht starten."); });
    return () => { alive = false; };
  }, [planId, nav]);

  if (error) return <div className="sohn-body"><div className="error-box">{error}</div>
    <button type="button" className="btn ghost" onClick={() => nav("/sohn")}>Zur Basis</button></div>;

  if (result) return <TestResult result={result} skin={skin} onHome={() => nav("/sohn")} onRetry={() => {
    setResult(null); setAttempt(null); setAnswers({}); setRevealed(new Set());
    if (planId) api.startTest(planId).then(setAttempt).catch((e) => setError(String(e)));
  }} />;

  if (!attempt) return <div className="sohn-body"><div className="loading">Test wird vorbereitet…</div></div>;

  const typed = TYPED.includes(attempt.stage);

  function setText(vocabId: number, val: string) {
    setAnswers((a) => ({ ...a, [vocabId]: { vocabularyId: vocabId, givenAnswer: val } }));
  }
  function setKnown(vocabId: number, known: boolean) {
    setAnswers((a) => ({ ...a, [vocabId]: { vocabularyId: vocabId, wasKnown: known } }));
    setRevealed((r) => new Set(r).add(vocabId));
  }

  async function submit() {
    if (!planId || !attempt) return;
    setBusy(true);
    try {
      const payload: AnswerDto[] = attempt.items.map(
        (it) => answers[it.vocabularyId] ?? { vocabularyId: it.vocabularyId, givenAnswer: typed ? "" : null, wasKnown: typed ? null : false },
      );
      const res = await api.submitTest(planId, attempt.attemptId, payload);
      setResult(res);
      setStreak((await api.today(planId)).currentStreak);
      refreshWallet();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Abgabe fehlgeschlagen.");
    } finally {
      setBusy(false);
    }
  }

  const answeredCount = attempt.items.filter((it) => {
    const a = answers[it.vocabularyId];
    return a && (typed ? (a.givenAnswer ?? "").trim().length > 0 : a.wasKnown !== undefined);
  }).length;

  return (
    <div className="sohn-body">
      <div className="row">
        <span className="screen-title" style={{ margin: 0 }}>Tagestest</span>
        <span className="pill cyan" style={{ marginLeft: "auto" }}>{stageLabel(attempt.stage)}</span>
      </div>
      <p className="sub">{typed ? "Tippe die Übersetzung." : "Denk nach, dann aufdecken und ehrlich bewerten."}</p>

      {attempt.items.map((it) => {
        const a = answers[it.vocabularyId];
        return (
          <div className="card" key={it.vocabularyId}>
            <div className="row">
              <b style={{ fontSize: 17 }}>{it.prompt}</b>
              {it.audioUrl && <audio controls src={it.audioUrl} style={{ marginLeft: "auto", height: 30 }} />}
            </div>

            {typed ? (
              <input
                className="tabnum"
                style={{ marginTop: 10, width: "100%", background: "#0c0e2c", border: "1.5px solid var(--stroke)", borderRadius: 12, color: "var(--ink)", padding: 12, fontSize: 15 }}
                placeholder={it.answerLength ? `${it.answerLength} Buchstaben` : "Übersetzung…"}
                value={a?.givenAnswer ?? ""}
                onChange={(e) => setText(it.vocabularyId, e.target.value)}
              />
            ) : (
              <div style={{ marginTop: 10 }}>
                {revealed.has(it.vocabularyId) || it.translation ? (
                  <div className="rev" style={{ color: "var(--cyan)", fontWeight: 800, marginBottom: 8 }}>→ {it.translation ?? "(aufgedeckt)"}</div>
                ) : (
                  <button type="button" className="btn ghost small" onClick={() => setRevealed((r) => new Set(r).add(it.vocabularyId))}>Aufdecken 🔄</button>
                )}
                {revealed.has(it.vocabularyId) && (
                  <div className="judge" style={{ marginTop: 8 }}>
                    <button type="button" className={`btn small ${a?.wasKnown === false ? "red" : "ghost"}`} onClick={() => setKnown(it.vocabularyId, false)}>Nicht gewusst</button>
                    <button type="button" className={`btn small ${a?.wasKnown === true ? "lime" : "ghost"}`} onClick={() => setKnown(it.vocabularyId, true)}>Gewusst</button>
                  </div>
                )}
              </div>
            )}
          </div>
        );
      })}

      <button type="button" className="btn gold" onClick={submit} disabled={busy}>
        {busy ? "…" : `Abgeben (${answeredCount}/${attempt.totalItems})`}
      </button>
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
          TAGESTEST · {result.stage.toUpperCase()}
        </div>
        <div className={`vtitle ${result.passed ? "win" : "lose"}`}>{result.passed ? "SIEG!" : "FAST!"}</div>
        <div className="ring" style={ring}><b>{pct}%<small>{result.correctItems} / {result.totalItems}</small></b></div>
        <Mascot skin={skin} mood={result.passed ? "hyped" : "sleepy"} size={84} />
        <div className="reward">
          <div className="card">🪙<span style={{ color: "var(--gold)" }}>+{result.dayProgress.pointsAwarded}</span></div>
          {result.dayProgress.dayComplete && <div className="card">🔥<span style={{ color: "#ffb36b" }}>Tag komplett</span></div>}
        </div>

        <div className="card" style={{ width: "100%", marginTop: 4, textAlign: "left" }}>
          {result.items.map((o) => (
            <div className="row" key={o.vocabularyId} style={{ padding: "4px 0" }}>
              <span>{o.wasCorrect ? "✅" : "❌"}</span>
              <b>{o.word}</b>
              <span className="sub" style={{ marginLeft: "auto" }}>{o.expectedTranslation}</span>
            </div>
          ))}
        </div>

        {!result.passed && <button type="button" className="btn gold" onClick={onRetry}>Nochmal versuchen</button>}
        <button type="button" className="btn ghost" onClick={onHome}>Zur Basis</button>
      </div>
    </div>
  );
}

function stageLabel(s: TestStage): string {
  return ({ ShowBoth: "Zeigen", SelfAssess: "Selbstcheck", LetterBoxes: "Buchstaben", FreeText: "Tippen", Audio: "Hören" } as const)[s];
}
