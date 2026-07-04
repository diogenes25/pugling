import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api, errorMessage } from "../lib/api";
import { useSohn } from "./SohnApp";
import type { PlanItemResponse, ReviewOutcome, SessionResponse, TodayResponse } from "../lib/types";

// Kleine Anerkennung bei jedem Treffer – Variation sorgt für Abwechslung (Daumen, Stern, Feuer, Muskel).
const SMALL_EMOJI = ["👍", "⭐", "🔥", "💪", "✨"];

type Phase = "loading" | "front" | "back" | "done" | "empty" | "error";

export function SohnPractice() {
  const { planId, refreshWallet, setStreak, celebrate } = useSohn();
  const nav = useNavigate();

  const [phase, setPhase] = useState<Phase>("loading");
  const [error, setError] = useState<string | null>(null);
  const [cards, setCards] = useState<PlanItemResponse[]>([]);
  const [idx, setIdx] = useState(0);
  const [combo, setCombo] = useState(0);
  const [earned, setEarned] = useState(0);
  const [toast, setToast] = useState<string | null>(null);
  const [lastOutcome, setLastOutcome] = useState<ReviewOutcome | null>(null);

  const session = useRef<SessionResponse | null>(null);
  const stage = useRef<number>(2);
  const startedIso = useRef<number>(Date.now());

  // Session starten + fällige Karten laden.
  useEffect(() => {
    if (!planId) { nav("/sohn"); return; }
    let alive = true;
    (async () => {
      try {
        const [today, sess]: [TodayResponse, SessionResponse] = await Promise.all([
          api.today(planId), api.startSession(planId),
        ]);
        if (!alive) return;
        session.current = sess;
        stage.current = today.recommendedStage || 2;
        const due = today.dueItems.filter((i) => i.kind === "Vocabulary");
        setCards(due);
        setPhase(due.length === 0 ? "empty" : "front");
      } catch (e) {
        if (alive) { setError(errorMessage(e)); setPhase("error"); }
      }
    })();
    return () => { alive = false; };
  }, [planId, nav]);

  // Heartbeat: alle 12s aktive Zeit melden (Server clamped ohnehin). Session am Ende schließen.
  useEffect(() => {
    if (!planId) return;
    const iv = setInterval(() => {
      if (session.current) api.heartbeat(planId, session.current.id, 12, true).catch(() => {});
    }, 12000);
    return () => {
      clearInterval(iv);
      if (planId && session.current) {
        const secs = Math.round((Date.now() - startedIso.current) / 1000) % 12;
        api.heartbeat(planId, session.current.id, secs, true).catch(() => {});
        api.endSession(planId, session.current.id).catch(() => {});
      }
    };
  }, [planId]);

  async function judge(wasCorrect: boolean) {
    if (!planId || !session.current) return;
    const card = cards[idx];
    try {
      // Flip-Karte = Selbsteinschätzung: der Server bewertet, wir liefern nur das WasKnown-Flag.
      const outcome = await api.review(planId, session.current.id, { contentId: card.contentId, wasKnown: wasCorrect });
      setLastOutcome(outcome);
      setCombo(outcome.combo); // serverseitig gezählt
      if (wasCorrect) {
        // Alle tatsächlich gebuchten Anteile zählen (Basis + Combo + Speed), sonst weicht die
        // Rundensumme vom Wallet ab.
        setEarned((e) => e + outcome.awarded + outcome.comboBonus + outcome.speedBonus);
        if (outcome.comboBonus > 0) {
          // Combo-Meilenstein: mittlere Feier, ab ×10 der große fliegende Kämpfer.
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
      }
    } catch { /* Bewertung ist idempotent genug; UI läuft weiter */ }
    next();
  }

  function next() {
    if (idx + 1 >= cards.length) {
      // Streak/Fortschritt aktualisieren, dann Abschluss zeigen.
      if (planId) api.today(planId).then((t) => setStreak(t.currentStreak)).catch(() => {});
      setPhase("done");
    } else {
      setIdx((i) => i + 1);
      setPhase("front");
    }
  }

  if (phase === "loading") return <div className="sohn-body"><div className="loading">Runde wird geladen…</div></div>;
  if (phase === "error") return <div className="sohn-body"><div className="error-box">{error}</div></div>;

  if (phase === "empty") return (
    <div className="sohn-body">
      <div className="card" style={{ textAlign: "center" }}>
        <div className="screen-title">Nichts fällig 🎉</div>
        <p className="sub">Alle Karten sind aktuell im Kasten weit genug oben. Mach den Tagestest oder komm später wieder.</p>
      </div>
      <button type="button" className="btn" onClick={() => nav("/sohn/test")}>🎯 Zum Tagestest</button>
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
        <button type="button" className="btn gold" onClick={() => nav("/sohn/test")} style={{ marginTop: 10 }}>🎯 Weiter zum Test</button>
        <button type="button" className="btn ghost" onClick={() => nav("/sohn")}>Zur Basis</button>
      </div>
    </div>
  );

  const card = cards[idx];
  return (
    <div className="sohn-body">
      <div className="row">
        <span className="pill cyan">Karte {idx + 1} / {cards.length}</span>
        <span className="pill mag" style={{ marginLeft: "auto" }}>Box {card.box}</span>
      </div>

      <div className="flash">
        <div className="combo">{combo >= 2 ? `⚡ COMBO ×${combo}` : " "}</div>
        <div className="fcard">
          <div className="lang">Wort</div>
          <div className="word">{card.label}</div>
          {phase === "back" && <div className="rev">→ {card.detail}</div>}
        </div>

        {phase === "front" ? (
          <button type="button" className="btn" onClick={() => setPhase("back")}>Umdrehen 🔄</button>
        ) : (
          <div className="judge">
            <button type="button" className="btn red small" onClick={() => judge(false)}>Nochmal</button>
            <button type="button" className="btn lime small" onClick={() => judge(true)}>Gewusst!</button>
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
