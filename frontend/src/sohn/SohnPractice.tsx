import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { api, errorMessage } from "../lib/api";
import { useSohn } from "./SohnApp";
import type { PracticeCard, PositionSession, ReviewOutcome } from "../lib/types";

// Kleine Anerkennung bei jedem Treffer – Variation sorgt für Abwechslung (Daumen, Stern, Feuer, Muskel).
const SMALL_EMOJI = ["👍", "⭐", "🔥", "💪", "✨"];

type Phase = "loading" | "front" | "back" | "done" | "empty" | "error";

export function SohnPractice() {
  const { planId, refreshWallet, setStreak, celebrate } = useSohn();
  const { positionId: positionIdRaw } = useParams();
  const positionId = Number(positionIdRaw);
  const nav = useNavigate();

  const [phase, setPhase] = useState<Phase>("loading");
  const [error, setError] = useState<string | null>(null);
  const [cards, setCards] = useState<PracticeCard[]>([]);
  const [idx, setIdx] = useState(0);
  const [typedAnswer, setTypedAnswer] = useState("");
  const [combo, setCombo] = useState(0);
  const [earned, setEarned] = useState(0);
  const [toast, setToast] = useState<string | null>(null);
  const [lastOutcome, setLastOutcome] = useState<ReviewOutcome | null>(null);

  const session = useRef<PositionSession | null>(null);
  const startedIso = useRef<number>(Date.now());

  // Sitzung starten + fällige Karten laden.
  useEffect(() => {
    if (!planId || !positionId) { nav("/sohn"); return; }
    let alive = true;
    (async () => {
      try {
        const sess = await api.startSession(planId, positionId);
        if (!alive) return;
        session.current = sess;
        const due = await api.cards(planId, positionId, sess.id);
        if (!alive) return;
        setCards(due);
        setPhase(due.length === 0 ? "empty" : "front");
      } catch (e) {
        if (alive) { setError(errorMessage(e)); setPhase("error"); }
      }
    })();
    return () => { alive = false; };
  }, [planId, positionId, nav]);

  // Heartbeat: alle 12s aktive Zeit melden (Server clamped ohnehin). Session am Ende schließen.
  useEffect(() => {
    if (!planId || !positionId) return;
    const iv = setInterval(() => {
      if (session.current) api.heartbeat(planId, positionId, session.current.id, 12, true).catch(() => {});
    }, 12000);
    return () => {
      clearInterval(iv);
      if (session.current) {
        const secs = Math.round((Date.now() - startedIso.current) / 1000) % 12;
        api.heartbeat(planId, positionId, session.current.id, secs, true).catch(() => {});
        api.endSession(planId, positionId, session.current.id).catch(() => {});
      }
    };
  }, [planId, positionId]);

  async function judge(card: PracticeCard, payload: { wasKnown?: boolean; givenAnswer?: string }) {
    if (!planId || !session.current) return;
    try {
      const outcome = await api.review(planId, positionId, session.current.id, { itemIndex: card.itemIndex, ...payload });
      setLastOutcome(outcome);
      setCombo(outcome.combo);
      if (outcome.wasCorrect) {
        setEarned((e) => e + outcome.awarded + outcome.comboBonus + outcome.speedBonus);
        if (outcome.comboBonus > 0) {
          const tier = outcome.combo >= 10 ? "big" : "medium";
          celebrate(tier, tier === "big" ? "🥷" : "🎉", `COMBO ×${outcome.combo}`, `+${outcome.comboBonus} 🪙 Bonus`);
        } else {
          celebrate("small", SMALL_EMOJI[outcome.combo % SMALL_EMOJI.length]);
        }
        if (outcome.awarded > 0) {
          setToast(`+${outcome.awarded} 🪙${outcome.box ? ` · Box ${outcome.box}` : ""}`);
          setTimeout(() => setToast(null), 1100);
        }
        refreshWallet();
      } else {
        setToast(`Lösung: ${outcome.expected}`);
        setTimeout(() => setToast(null), 1600);
      }
    } catch { /* Bewertung ist idempotent genug; UI läuft weiter */ }
    next();
  }

  function next() {
    setTypedAnswer("");
    if (idx + 1 >= cards.length) {
      if (planId) api.overview(planId).then((o) => setStreak(o.currentStreak)).catch(() => {});
      setPhase("done");
    } else {
      setIdx((i) => i + 1);
      setPhase("front");
    }
  }

  if (phase === "loading") return <div className="sohn-body"><div className="loading">Runde wird geladen…</div></div>;
  if (phase === "error") return <div className="sohn-body"><div className="error-box">{error}</div>
    <button type="button" className="btn ghost" onClick={() => nav("/sohn")}>Zur Basis</button></div>;

  if (phase === "empty") return (
    <div className="sohn-body">
      <div className="card" style={{ textAlign: "center" }}>
        <div className="screen-title">Nichts fällig 🎉</div>
        <p className="sub">Alle Karten sind aktuell weit genug im Kasten. Mach den Test oder komm später wieder.</p>
      </div>
      <button type="button" className="btn" onClick={() => nav(`/sohn/test/${positionId}`)}>🎯 Zum Test</button>
      <button type="button" className="btn ghost" onClick={() => nav("/sohn")}>Zur Basis</button>
    </div>
  );

  if (phase === "done") return (
    <div className="sohn-body">
      <div className="victory">
        <div className="vtitle win">RUNDE FERTIG!</div>
        <div className="reward">
          <div className="card">🪙<span style={{ color: "var(--gold)" }}>+{earned}</span></div>
          <div className="card">🃏<span>{cards.length} Karten</span></div>
        </div>
        <button type="button" className="btn gold" onClick={() => nav(`/sohn/test/${positionId}`)} style={{ marginTop: 10 }}>🎯 Weiter zum Test</button>
        <button type="button" className="btn ghost" onClick={() => nav("/sohn")}>Zur Basis</button>
      </div>
    </div>
  );

  const card = cards[idx];
  const typed = card.reveal === null; // getippte Stufe → Eingabe; sonst Flip-Karte (Selbsteinschätzung)
  return (
    <div className="sohn-body">
      <div className="row">
        <span className="pill cyan">Karte {idx + 1} / {cards.length}</span>
        {combo >= 2 && <span className="pill mag" style={{ marginLeft: "auto" }}>⚡ COMBO ×{combo}</span>}
      </div>

      <div className="flash">
        <div className="fcard">
          <div className="lang">Aufgabe</div>
          <div className="word">{card.prompt}</div>
          {card.hint && typed && <div className="sub">💡 {card.hint}</div>}
          {phase === "back" && card.reveal && <div className="rev">→ {card.reveal}</div>}
        </div>

        {typed ? (
          <form onSubmit={(e) => { e.preventDefault(); if (typedAnswer.trim()) judge(card, { givenAnswer: typedAnswer }); }}>
            <input
              autoFocus
              className="tabnum"
              style={{ width: "100%", background: "#0c0e2c", border: "1.5px solid var(--stroke)", borderRadius: 12, color: "var(--ink)", padding: 12, fontSize: 15 }}
              placeholder={card.answerLength ? `${card.answerLength} Buchstaben` : "Antwort…"}
              value={typedAnswer}
              onChange={(e) => setTypedAnswer(e.target.value)}
            />
            <button type="submit" className="btn lime" style={{ marginTop: 10 }} disabled={!typedAnswer.trim()}>Prüfen</button>
          </form>
        ) : phase === "front" ? (
          <button type="button" className="btn" onClick={() => setPhase("back")}>Umdrehen 🔄</button>
        ) : (
          <div className="judge">
            <button type="button" className="btn red small" onClick={() => judge(card, { wasKnown: false })}>Nochmal</button>
            <button type="button" className="btn lime small" onClick={() => judge(card, { wasKnown: true })}>Gewusst!</button>
          </div>
        )}
      </div>

      {toast && <div className="toast">{toast}</div>}
      {lastOutcome?.dueOn && phase === "front" && (
        <p className="sub" style={{ textAlign: "center" }}>Nächste Fälligkeit: {lastOutcome.dueOn}</p>
      )}
    </div>
  );
}
