import { useEffect, useState } from "react";
import { api } from "../api";
import type { LearningSession, RewardOffer } from "../types";

const SOHN_ID = 2; // Seed; später dynamisch

function fmtMin(seconds: number) {
  return `${Math.round(seconds / 60)} min`;
}

export function VaterView() {
  const [sessions, setSessions] = useState<LearningSession[]>([]);
  const [offers, setOffers] = useState<RewardOffer[]>([]);
  const [balance, setBalance] = useState(0);

  const refresh = () => {
    api.sessions(SOHN_ID).then(setSessions).catch(console.error);
    api.offers().then(setOffers).catch(console.error);
    api.balance(SOHN_ID).then(r => setBalance(r.balance)).catch(console.error);
  };
  useEffect(refresh, []);

  async function decide(id: number, accept: boolean) {
    await api.decideOffer(id, accept);
    refresh();
  }

  const totalActive = sessions.reduce((s, x) => s + x.activeSeconds, 0);
  const totalIdle = sessions.reduce((s, x) => s + x.idleSeconds, 0);
  const openOffers = offers.filter(o => o.status === 0);

  return (
    <div>
      <h2>Überblick (30 Tage)</h2>
      <div className="card">
        <div className="stat"><span>Punktestand</span><span className="badge">{balance}</span></div>
        <div className="stat"><span>Aktive Lernzeit</span><span>{fmtMin(totalActive)}</span></div>
        <div className="stat"><span>Inaktive Zeit (nur geguckt)</span><span>{fmtMin(totalIdle)}</span></div>
        <div className="stat"><span>Lerneinheiten</span><span>{sessions.length}</span></div>
      </div>

      {openOffers.length > 0 && <h2>Offene Angebote</h2>}
      {openOffers.map(o => (
        <div className="card" key={o.id}>
          <p style={{ marginBottom: 10 }}>
            <strong>{o.title}</strong> für {o.costPoints} Punkte
          </p>
          <div className="row">
            <button className="danger" onClick={() => decide(o.id, false)}>Ablehnen</button>
            <button className="success" onClick={() => decide(o.id, true)}>Annehmen</button>
          </div>
        </div>
      ))}

      <h2>Letzte Lerneinheiten</h2>
      {sessions.slice(0, 10).map(s => (
        <div className="card" key={s.id}>
          <div className="stat"><span>{new Date(s.startedAt).toLocaleString("de-DE")}</span><span className="badge">+{s.pointsEarned}</span></div>
          <div className="stat"><span>Aktiv / Inaktiv</span><span>{fmtMin(s.activeSeconds)} / {fmtMin(s.idleSeconds)}</span></div>
          <div className="stat" style={{ border: 0 }}><span>Karten (davon neu)</span><span>{s.cardsReviewed} ({s.newCards})</span></div>
        </div>
      ))}
      {sessions.length === 0 && <p style={{ color: "#777" }}>Noch keine Lerneinheiten.</p>}
    </div>
  );
}
