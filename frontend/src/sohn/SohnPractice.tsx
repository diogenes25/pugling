import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { ApiError, api, errorMessage } from "../lib/api";
import { useSohn } from "./SohnApp";
import { LetterBoxes } from "../components/LetterBoxes";
import { AudioButton } from "../components/AudioButton";
import { ClozePrompt } from "../components/ClozePrompt";
import { ListRule } from "../components/ListRule";
import { Passage } from "../components/Passage";
import { RevealAlternatives } from "../components/RevealAlternatives";
import { BirkenbihlDecoding } from "../components/BirkenbihlDecoding";
import type { PracticeCard, PositionSession, ReviewOutcome } from "../lib/types";

// Kleine Anerkennung bei jedem Treffer – Variation sorgt für Abwechslung (Daumen, Stern, Feuer, Muskel).
const SMALL_EMOJI = ["👍", "⭐", "🔥", "💪", "✨"];

type Phase = "loading" | "front" | "back" | "done" | "empty" | "error";

/**
 * Wie eine Review-Antwort dargestellt wird – als reine Regel statt inline in `judge()`, damit sie ohne
 * Bildschirm und `fetch` prüfbar ist (Muster `SelfAssessAnswer`).
 * <p>
 * `displayOnly` (ShowBoth/B-96, Kennenlernen) gibt IMMER `"none"` zurück, unabhängig von
 * `outcome.wasCorrect`: der Server wertet diese Stufe nie (siehe `PositionPracticeController.Review`),
 * `wasCorrect` steht dort einfach auf `false`, weil kein `wasKnown` mitgeschickt wird – ohne diese
 * Sonderregel läse das Kind nach jeder betrachteten Karte ein „Leider nicht.", ein Urteil, das die Stufe
 * per Story-Vorgabe gerade NICHT fällen darf.
 */
export function reviewFeedback(outcome: ReviewOutcome | null | undefined, displayOnly: boolean):
  | { kind: "none" }
  | { kind: "correct"; awarded: number; combo: number; comboBonus: number; speedBonus: number; box: number }
  | { kind: "wrong"; expected: string | null } {
  if (!outcome || displayOnly) return { kind: "none" };
  if (outcome.wasCorrect) {
    return {
      kind: "correct", awarded: outcome.awarded, combo: outcome.combo,
      comboBonus: outcome.comboBonus, speedBonus: outcome.speedBonus, box: outcome.box,
    };
  }
  return { kind: "wrong", expected: outcome.expected ?? null };
}

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
  // Abwechslung: Tipp erst auf Wunsch aufdecken; Tempo-Modus (persistiert) blendet eine Countdown-Leiste ein.
  const [hintShown, setHintShown] = useState(false);
  const [tempo, setTempo] = useState(() => localStorage.getItem("pugling.tempo") === "1");

  function toggleTempo() {
    setTempo((t) => {
      const next = !t;
      localStorage.setItem("pugling.tempo", next ? "1" : "0");
      return next;
    });
  }

  const [busy, setBusy] = useState(false);
  const judging = useRef(false);
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
        const sessionId = session.current.id;
        // Erst die Rest-Sekunden, dann schließen: `end` wertet das Ziel sofort aus, und bei einer Übung
        // ohne einzelne Inhalte hängt die Pflicht an der Verweildauer. Kämen die Sekunden danach an,
        // stünde die Gutschrift bis zum Periodenschluss aus.
        api.heartbeat(planId, positionId, sessionId, secs, true)
          .catch(() => {})
          .then(() => api.endSession(planId, positionId, sessionId))
          .catch(() => {});
      }
    };
  }, [planId, positionId]);

  /*
   * Eine Bewertung je Karte, und die Sperre sitzt im `useRef` **vor** dem ersten `await`: `busy` als State
   * steht erst nach dem Re-Render am Knopf. Seit die Auswahl ausgespielt wird (B-73), stehen hier drei
   * Knöpfe nebeneinander statt einer – zwei schnelle Tipper schickten sonst zwei `review` auf denselben
   * `itemIndex` **und** zwei `next()`, der Zähler sprang von 1/5 auf 3/5 und Karte 2 kam nie.
   */
  async function judge(card: PracticeCard, payload: { wasKnown?: boolean; givenAnswer?: string }) {
    if (!planId || !session.current || judging.current) return;
    judging.current = true;
    setBusy(true);
    try {
      const outcome = await api.review(planId, positionId, session.current.id, { itemIndex: card.itemIndex, ...payload });
      setLastOutcome(outcome ?? null);
      setCombo(outcome?.combo ?? 0);
      const feedback = reviewFeedback(outcome, card.displayOnly ?? false);
      if (feedback.kind === "correct") {
        setEarned((e) => e + feedback.awarded + feedback.comboBonus + feedback.speedBonus);
        if (feedback.comboBonus > 0) {
          const tier = feedback.combo >= 10 ? "big" : "medium";
          celebrate(tier, tier === "big" ? "🥷" : "🎉", `COMBO ×${feedback.combo}`, `+${feedback.comboBonus} 🪙 Bonus`);
        } else {
          celebrate("small", SMALL_EMOJI[feedback.combo % SMALL_EMOJI.length]);
        }
        if (feedback.awarded > 0) {
          setToast(`+${feedback.awarded} 🪙${feedback.box ? ` · Box ${feedback.box}` : ""}`);
          setTimeout(() => setToast(null), 1100);
        }
        refreshWallet();
      } else if (feedback.kind === "wrong") {
        // Ohne getroffenen Eintrag gibt es keine Lösung zu nennen: bei einer Menge (ungeordnete Liste) wäre
        // „Lösung: Hessen" willkürlich, solange ein Dutzend Einträge offen ist – und verriete einen, der noch
        // gefragt wird. Der Server liefert dann `null`, und hier bleibt es bei der schlichten Absage.
        setToast(feedback.expected ? `Lösung: ${feedback.expected}` : "Leider nicht.");
        setTimeout(() => setToast(null), 1600);
      }
      // feedback.kind === "none": kein Outcome ODER eine Anzeigenurstufe (ShowBoth) - kein Urteil, kein Toast.
    } catch { /* Bewertung ist idempotent genug; UI läuft weiter */ }
    next();
    judging.current = false;
    setBusy(false);
  }

  function next() {
    setTypedAnswer("");
    setHintShown(false);
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
  const typed = card.reveal === null; // getippte Stufe → Eingabe; sonst Flip-Karte (Selbsteinschätzung) oder ShowBoth (sofort offen, kein Urteil)
  const submitTyped = () => { if (typedAnswer.trim()) judge(card, { givenAnswer: typedAnswer }); };

  /**
   * „Anderes Bild": das abgelehnte kommt nie wieder. Die neue Wahl wird lokal in die Karte gespiegelt,
   * damit der Wechsel sofort sichtbar ist – ein Neuladen der Sitzung würde den Fortschritt anfassen.
   * Ohne Alternative antwortet der Server 409; dann bleibt das Bild stehen und wir sagen es nur.
   */
  async function reshuffleImage() {
    if (!planId || !session.current) return;
    const at = idx;
    try {
      // Nicht `next` nennen – so heißt die Karten-Weiterschaltung oben.
      const picked = await api.reshuffleCardImage(planId, positionId, session.current.id, cards[at].itemIndex);
      setCards((prev) => prev.map((c, i) =>
        i === at ? { ...c, imageUrl: picked.imageUrl, imageAlt: picked.imageAlt } : c));
    } catch (e) {
      setToast(e instanceof ApiError && e.code === "media_no_alternative"
        ? "Mehr Bilder gibt es dafür nicht 🙂"
        : errorMessage(e));
    }
  }
  return (
    <div className="sohn-body">
      <div className="row">
        <span className="pill cyan">Karte {idx + 1} / {cards.length}</span>
        <span className="row" style={{ marginLeft: "auto", gap: 8 }}>
          {combo >= 2 && <span className="pill mag">⚡ COMBO ×{combo}</span>}
          <button type="button" className={`pill toggle-pill ${tempo ? "lime" : ""}`} onClick={toggleTempo} aria-pressed={tempo}>
            ⚡ Tempo
          </button>
          {/*
            Sichtbarer Ausweg aus der laufenden Runde. Ohne Rückfrage, weil eine Rückfrage über eine
            Aktion, deren Wort schon die Wahrheit sagt, zum Wegklicken erzieht. Der Server-Aufruf (`end`)
            gehört bewusst NICHT hierher – das Cleanup des Heartbeat-Effekts oben schickt die Rest-Sekunden
            und beendet die Sitzung; ein zweiter Aufruf hier wäre eine Doppelquelle für dieselbe Sache.
            Folgenlos ist das Verlassen nur beim Leitner-Üben (jede gespielte Karte ist schon gebucht):
            bei einer Übung ohne automatische Prüfung misst die Pflicht EINE Sitzung gegen die Schwelle,
            und die nächste Runde beginnt wieder bei null.
          */}
          <button type="button" className="pill toggle-pill exit-pill" onClick={() => nav("/sohn")}>
            Runde beenden
          </button>
        </span>
      </div>

      {/* Tempo-Modus: Countdown-Leiste je Karte (rein visueller Ansporn; der Schnell-Bonus zählt serverseitig). */}
      {tempo && typed && phase !== "back" && <div className="tempo-bar" key={idx}><i /></div>}

      <div className="flash">
        <div className="fcard">
          <div className="lang">Aufgabe</div>
          {/*
            Das Bild kommt nur auf Stufen, auf denen es die Lösung nicht verraten kann – der Server
            entscheidet das, das Frontend rendert nur, was da ist. Es steht über dem Wort, weil es beim
            Einprägen hilft (Bild + Wort zusammen), nicht als Dekoration darunter.
          */}
          {card.imageUrl && (
            <CardImage
              key={card.imageUrl}
              url={card.imageUrl}
              alt={card.imageAlt ?? ""}
              onReshuffle={reshuffleImage}
            />
          )}
          <Passage text={card.passage} />
          {/*
            Aufnahme und Frage nebeneinander, nicht entweder-oder: Beim Hörverstehen braucht das Kind
            beides. Dass die Vokabel-Hörstufe ihr Wort verschweigt, entscheidet der Server – er schickt
            dort keinen `prompt`. Eine Regel, die hier stünde, müsste jeder neue Audio-Typ nachtragen.

            Fehlt der Text, IST die Aufnahme die Aufgabe: kurz, also anspielen und der Knopf genügt. Steht
            eine Frage daneben, ist sie das Material und kann lang sein – dann die Bedienelemente, sonst
            könnte das Kind sie nicht anhalten. Die Unterscheidung liest sich aus den gelieferten Feldern,
            nicht aus dem Übungstyp.
          */}
          {card.audioUrl && (
            <AudioButton url={card.audioUrl} autoPlay={!card.prompt} withControls={!!card.prompt} />
          )}
          <ClozePrompt text={card.prompt} gapIndex={card.gapIndex} />
          {/* Die Dekodierung steht auf der VORDERSEITE, direkt unter dem Satz: sie ist die Methode, nicht die
              Lösung – man liest den Satz und seine Entschlüsselung zusammen (B-78). */}
          <BirkenbihlDecoding decoding={card.decoding} />
          <ListRule type={card.type} anyOrder={card.anyOrder} itemIndex={card.itemIndex} />
          {card.hint && typed && (
            hintShown
              ? <div className="sub">💡 {card.hint}</div>
              : <button type="button" className="btn ghost small" style={{ marginTop: 6 }} onClick={() => setHintShown(true)}>💡 Tipp</button>
          )}
          {/* ShowBoth (B-96): a free display stage shows both sides at once, no flip needed - the server
              flags this via `displayOnly`, distinct from self-assessment which reveals only after "Umdrehen". */}
          {(phase === "back" || card.displayOnly) && card.reveal && <div className="rev">→ {card.reveal}</div>}
          {(phase === "back" || card.displayOnly) && card.reveal && <RevealAlternatives alternatives={card.revealAlternatives} />}
        </div>

        {/* Gruppe statt loser Knöpfe: ein Screenreader liest sonst „Leeds, Schaltfläche" ohne Bezug zur
            Frage. Der Key trägt den Index mit – der Autor darf dieselbe Option zweimal eintragen. */}
        {typed && card.choices ? (
          <div className="row" style={{ gap: 8, flexWrap: "wrap" }} role="group" aria-label="Antwortmöglichkeiten">
            {card.choices.map((c, i) => (
              <button type="button" key={`${i}-${c}`} className="btn ghost" disabled={busy}
                onClick={() => judge(card, { givenAnswer: c })}>{c}</button>
            ))}
          </div>
        ) : typed ? (
          <div>
            {card.answerLength ? (
              <LetterBoxes length={card.answerLength} value={typedAnswer} onChange={setTypedAnswer} onSubmit={submitTyped} />
            ) : (
              <form onSubmit={(e) => { e.preventDefault(); submitTyped(); }}>
                <input
                  aria-label="Deine Antwort"
                  name="answer"
                  autoComplete="off"
                  autoCapitalize="off"
                  autoCorrect="off"
                  spellCheck={false}
                  style={{ width: "100%", background: "#0c0e2c", border: "1.5px solid var(--stroke)", borderRadius: 12, color: "var(--ink)", padding: 12, fontSize: 15 }}
                  placeholder="Antwort…"
                  value={typedAnswer}
                  onChange={(e) => setTypedAnswer(e.target.value)}
                />
              </form>
            )}
            <button type="button" className="btn lime" style={{ marginTop: 10 }} disabled={!typedAnswer.trim()} onClick={submitTyped}>Prüfen</button>
          </div>
        ) : card.displayOnly ? (
          // Kennenlernen: no judgment to make, so no "Gewusst?" buttons - just move on.
          <button type="button" className="btn lime" disabled={busy} onClick={() => judge(card, {})}>Weiter →</button>
        ) : phase === "front" ? (
          <button type="button" className="btn" onClick={() => setPhase("back")}>Umdrehen 🔄</button>
        ) : (
          <div className="judge">
            <button type="button" className="btn red small" onClick={() => judge(card, { wasKnown: false })}>Nochmal</button>
            <button type="button" className="btn lime small" onClick={() => judge(card, { wasKnown: true })}>Gewusst!</button>
          </div>
        )}
      </div>

      {toast && <div className="toast" role="status" aria-live="polite">{toast}</div>}
      {lastOutcome?.dueOn && phase === "front" && (
        <p className="sub" style={{ textAlign: "center" }}>Nächste Fälligkeit: {lastOutcome.dueOn}</p>
      )}
    </div>
  );
}

/**
 * Das Bild zur Karte. Es ist Teil der Lernhilfe, nicht Deko – deshalb bekommt es Platz und einen
 * Alt-Text (der Server liefert ihn nur mit, wenn er die Lösung nicht verrät).
 *
 * Der „anderes Bild"-Knopf ist bewusst klein und unaufdringlich: Bildkonstanz ist beim Vokabellernen
 * der Merkeffekt, das Wechseln ist die Ausnahme. Genau deshalb ist es aber wichtig, dass es sie gibt –
 * ein Motiv, das ein Kind nicht mag, arbeitet gegen das Lernen.
 */
function CardImage({ url, alt, onReshuffle }: { url: string; alt: string; onReshuffle: () => void }) {
  const [busy, setBusy] = useState(false);
  return (
    <figure style={{ margin: "0 0 10px", textAlign: "center" }}>
      <img
        src={url}
        alt={alt}
        style={{ maxWidth: "100%", maxHeight: 180, objectFit: "contain", borderRadius: 12 }}
      />
      <figcaption>
        <button
          type="button"
          className="btn ghost small"
          style={{ width: "auto", marginTop: 4 }}
          disabled={busy}
          onClick={async () => { setBusy(true); try { await onReshuffle(); } finally { setBusy(false); } }}
        >
          🔄 anderes Bild
        </button>
      </figcaption>
    </figure>
  );
}
