import { useEffect, useState } from "react";
import { api } from "../api";
import { useActivityTracker } from "../hooks/useActivityTracker";
import type { Topic, VocabCard } from "../types";

const USER_ID = 2; // Sohn (Seed); später über Login

export function SohnView() {
  const [topics, setTopics] = useState<Topic[]>([]);
  const [balance, setBalance] = useState(0);
  const [sessionId, setSessionId] = useState<number | null>(null);
  const [cards, setCards] = useState<VocabCard[]>([]);
  const [index, setIndex] = useState(0);
  const [flipped, setFlipped] = useState(false);
  const [sessionPoints, setSessionPoints] = useState(0);
  const [offerTitle, setOfferTitle] = useState("");
  const [offerCost, setOfferCost] = useState(20);

  const { noteInteraction } = useActivityTracker(sessionId);

  const refresh = () => {
    api.topics().then(setTopics).catch(console.error);
    api.balance(USER_ID).then(r => setBalance(r.balance)).catch(console.error);
  };
  useEffect(refresh, []);

  async function start(topicId: number) {
    const [session, due] = await Promise.all([
      api.startSession(USER_ID, topicId),
      api.dueCards(topicId),
    ]);
    setSessionId(session.id);
    setCards(due);
    setIndex(0);
    setFlipped(false);
    setSessionPoints(0);
  }

  async function answer(correct: boolean) {
    noteInteraction();
    if (sessionId === null) return;
    const card = cards[index];
    const result = await api.review(sessionId, card.id, correct);
    setSessionPoints(p => p + result.awarded);
    if (index + 1 < cards.length) {
      setIndex(index + 1);
      setFlipped(false);
    } else {
      await end();
    }
  }

  async function end() {
    if (sessionId === null) return;
    await api.endSession(sessionId);
    setSessionId(null);
    setCards([]);
    refresh();
  }

  async function makeOffer() {
    if (!offerTitle) return;
    await api.createOffer(USER_ID, offerTitle, offerCost);
    setOfferTitle("");
    alert("Angebot an Papa geschickt!");
  }

  // ---- Lernmodus ----
  if (sessionId !== null && cards.length > 0) {
    const card = cards[index];
    return (
      <div>
        <div className="stat">
          <span>Karte {index + 1} / {cards.length}</span>
          <span className="badge">+{sessionPoints} Punkte</span>
        </div>
        <div
          className="card vocab-card"
          onClick={() => { setFlipped(f => !f); noteInteraction(); }}
        >
          {flipped ? card.back : card.front}
        </div>
        {flipped ? (
          <div className="row">
            <button className="danger" onClick={() => answer(false)}>Wusste ich nicht</button>
            <button className="success" onClick={() => answer(true)}>Gewusst!</button>
          </div>
        ) : (
          <p style={{ textAlign: "center", color: "#777" }}>Tippe auf die Karte zum Umdrehen</p>
        )}
        <button className="secondary" style={{ marginTop: 16, width: "100%" }} onClick={end}>
          Beenden
        </button>
      </div>
    );
  }

  // ---- Startbildschirm ----
  return (
    <div>
      <div className="card">
        <div className="stat" style={{ border: 0 }}>
          <span>Deine Punkte</span>
          <span className="badge">{balance}</span>
        </div>
      </div>

      <h2>Lernen</h2>
      {topics.map(t => (
        <div className="card" key={t.id}>
          <div className="stat" style={{ border: 0 }}>
            <span>{t.name} ({t.cardCount} Karten)</span>
            <button onClick={() => start(t.id)}>Start</button>
          </div>
        </div>
      ))}

      <h2>Punkte eintauschen</h2>
      <div className="card">
        <input
          placeholder="z.B. 30 min Fernsehen"
          value={offerTitle}
          onChange={e => setOfferTitle(e.target.value)}
        />
        <input
          type="number"
          value={offerCost}
          onChange={e => setOfferCost(Number(e.target.value))}
        />
        <button style={{ width: "100%" }} onClick={makeOffer} disabled={!offerTitle || offerCost > balance}>
          Papa anbieten ({offerCost} Punkte)
        </button>
      </div>
    </div>
  );
}
