import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { api, errorMessage } from "../lib/api";
import { useSohn } from "./SohnApp";
import { Mascot } from "../components/Mascot";
import type { AnswerDto, TestAttemptResponse, TestSubmitResponse } from "../lib/types";

// Vokabel-Teststufen (numerisch, serverseitig erzwungen): 1 Zeigen … 5 Hören.
const STAGE_LABEL: Record<number, string> = {
  1: "Zeigen", 2: "Selbstcheck", 3: "Buchstaben", 4: "Tippen", 5: "Hören",
};
const stageLabel = (s: number) => STAGE_LABEL[s] ?? `Stufe ${s}`;

export function SohnTest() {
  const { planId, refreshWallet, setStreak, skin, celebrate } = useSohn();
  const { positionId: positionIdRaw } = useParams();
  const positionId = Number(positionIdRaw);
  const nav = useNavigate();

  const [attempt, setAttempt] = useState<TestAttemptResponse | null>(null);
  const [answers, setAnswers] = useState<Record<number, AnswerDto>>({});
  const [revealed, setRevealed] = useState<Set<number>>(new Set());
  const [result, setResult] = useState<TestSubmitResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!planId || !positionId) { nav("/sohn"); return; }
    let alive = true;
    api.startTest(planId, positionId)
      .then((a) => { if (alive) setAttempt(a); })
      .catch((e) => { if (alive) setError(errorMessage(e)); });
    return () => { alive = false; };
  }, [planId, positionId, nav]);

  if (error) return <div className="sohn-body"><div className="error-box">{error}</div>
    <button type="button" className="btn ghost" onClick={() => nav("/sohn")}>Zur Basis</button></div>;

  if (result) return (
    <TestResult result={result} skin={skin} onHome={() => nav("/sohn")} onRetry={() => {
      setResult(null); setAttempt(null); setAnswers({}); setRevealed(new Set());
      if (planId) api.startTest(planId, positionId).then(setAttempt).catch((e) => setError(errorMessage(e)));
    }} />
  );

  if (!attempt) return <div className="sohn-body"><div className="loading">Test wird vorbereitet…</div></div>;

  // Getippte Stufe: Server liefert keine aufgedeckte Lösung (reveal === null) → Eingabefeld.
  const typed = attempt.items.every((it) => it.reveal === null);

  function setText(itemIndex: number, val: string) {
    setAnswers((a) => ({ ...a, [itemIndex]: { itemIndex, givenAnswer: val } }));
  }
  function setKnown(itemIndex: number, known: boolean) {
    setAnswers((a) => ({ ...a, [itemIndex]: { itemIndex, wasKnown: known } }));
    setRevealed((r) => new Set(r).add(itemIndex));
  }

  async function submit() {
    if (!planId || !attempt) return;
    setBusy(true);
    try {
      const payload: AnswerDto[] = attempt.items.map(
        (it) => answers[it.itemIndex] ?? { itemIndex: it.itemIndex, givenAnswer: typed ? "" : null, wasKnown: typed ? null : false },
      );
      const res = await api.submitTest(planId, positionId, attempt.attemptId, payload);
      setResult(res);
      celebrate(res.passed ? "big" : "small", res.passed ? "🎉" : "💪",
        res.passed ? "SIEG!" : undefined, res.passed ? `${res.scorePercent}%` : undefined);
      setStreak((await api.overview(planId)).currentStreak);
      refreshWallet();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  const answeredCount = attempt.items.filter((it) => {
    const a = answers[it.itemIndex];
    return a && (typed ? (a.givenAnswer ?? "").trim().length > 0 : a.wasKnown !== undefined);
  }).length;

  return (
    <div className="sohn-body">
      <div className="row">
        <span className="screen-title" style={{ margin: 0 }}>Test</span>
        <span className="pill cyan" style={{ marginLeft: "auto" }}>{stageLabel(attempt.stage)}</span>
      </div>
      <p className="sub">{typed ? "Tippe die Lösung." : "Denk nach, dann aufdecken und ehrlich bewerten."}</p>

      {attempt.items.map((it) => {
        const a = answers[it.itemIndex];
        return (
          <div className="card" key={it.itemIndex}>
            <b style={{ fontSize: 17 }}>{it.prompt}</b>

            {typed ? (
              <input
                className="tabnum"
                style={{ marginTop: 10, width: "100%", background: "#0c0e2c", border: "1.5px solid var(--stroke)", borderRadius: 12, color: "var(--ink)", padding: 12, fontSize: 15 }}
                placeholder={it.answerLength ? `${it.answerLength} Buchstaben` : "Lösung…"}
                value={a?.givenAnswer ?? ""}
                onChange={(e) => setText(it.itemIndex, e.target.value)}
              />
            ) : (
              <div style={{ marginTop: 10 }}>
                {revealed.has(it.itemIndex) || it.reveal ? (
                  <div className="rev" style={{ color: "var(--cyan)", fontWeight: 800, marginBottom: 8 }}>→ {it.reveal ?? "(aufgedeckt)"}</div>
                ) : (
                  <button type="button" className="btn ghost small" onClick={() => setRevealed((r) => new Set(r).add(it.itemIndex))}>Aufdecken 🔄</button>
                )}
                {/* Bewerten, sobald die Lösung sichtbar ist – entweder vom Server aufgedeckt (Stufe
                    „Zeigen") oder per „Aufdecken" (Stufe „Selbstcheck"). Ohne dieses `|| it.reveal`
                    fehlten die Buttons, wenn der Server die Lösung schon mitschickt → Test unlösbar. */}
                {(revealed.has(it.itemIndex) || it.reveal) && (
                  <div className="judge" style={{ marginTop: 8 }}>
                    <button type="button" className={`btn small ${a?.wasKnown === false ? "red" : "ghost"}`} onClick={() => setKnown(it.itemIndex, false)}>Nicht gewusst</button>
                    <button type="button" className={`btn small ${a?.wasKnown === true ? "lime" : "ghost"}`} onClick={() => setKnown(it.itemIndex, true)}>Gewusst</button>
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
