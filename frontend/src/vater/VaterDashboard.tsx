import { Link } from "react-router-dom";
import { ListenAusfall } from "../components/ListenAusfall";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import { unitAmount } from "../lib/shop";
import { formatDate } from "../lib/datum";
import { ChildrenTimeline } from "./ChildrenTimeline";
import { bestaetigeFreigabe, entscheideEinloesung } from "./einloesungen";
import { useAuth } from "../lib/auth";
import type { ActivationRequest, ChildDay, ChildrenDashboard, Paged } from "../lib/types";

/*
 * Layout inline statt in index.css: kein Stilblock, den andere Seiten mitlesen, und eine Liste aus Karten statt einer
 * Tabelle bricht am Handy (390 px) um, statt seitlich zu scrollen (B-749, AK 5).
 */
const LISTE: React.CSSProperties = { listStyle: "none", margin: 0, padding: 0, display: "grid", gap: 10 };
const ZAHLEN: React.CSSProperties = {
  display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(110px, 1fr))", gap: 8, margin: "8px 0 0",
};

/**
 * Die Betreuen-Startseite (B-749): je Kind, ob und wie viel es heute gelernt hat und ob das Tagesziel steht, darunter alles,
 * was einzulösen ist – über alle Kinder, ohne ein Kind zu wählen. Gebaut für den Blick am Handy; die Verwaltung (Kinder
 * anlegen, Stammdaten, der Weg zu den Plänen) liegt unter „Kinder".
 */
export function VaterDashboard() {
  const { session } = useAuth();
  const adultId = session!.id;

  const today = useAsync<ChildrenDashboard>(() => api.childrenDaily(), [adultId]);
  const offen = useAsync<Paged<ActivationRequest>>(
    () => api.allActivations({ status: "Pending", take: 100 }), [adultId]);
  const action = useAction();

  // Der Kindname je Zeile kommt aus der Tagesübersicht (B-747 Entscheidung 2: kein Namensfeld im Vertrag). Beide listen
  // dieselben Kinder – die des Vaters; fehlt die Übersicht, steht die Id statt einer leeren Stelle.
  const nameOf = (childId: number) =>
    today.data?.children.find((c) => c.childId === childId)?.name ?? `Kind #${childId}`;

  async function decide(r: ActivationRequest, approve: boolean) {
    if (approve && !bestaetigeFreigabe(r)) return;
    await action.run(() => entscheideEinloesung(r, approve),
      approve ? `${nameOf(r.childId)}: „${r.articleTitle}" freigegeben.` : `${nameOf(r.childId)}: „${r.articleTitle}" abgelehnt.`);
    // Auch nach einem Fehler neu laden: hat ein anderer Erwachsener schon entschieden, darf die Zeile nicht stehen bleiben.
    offen.reload();
  }

  return (
    <>
      <section>
        <h2 className="h-section">Heute</h2>
        <ListenAusfall was="Die Tagesübersicht" fehler={today.error} laedt={today.loading} onRetry={today.reload} />
        {today.showPlaceholder ? <div className="loading">Lade…</div> : today.data && (
          today.data.children.length === 0
            ? <p className="muted">Noch keine Kinder. <Link to="/vater/kinder">Kind anlegen →</Link></p>
            : <ul className="vater-heute" aria-label="Kinder heute" style={LISTE}>
              {today.data.children.map((c) => <HeuteKind key={c.childId} c={c} />)}
            </ul>
        )}
      </section>

      <section>
        <h2 className="h-section">Einlösen</h2>
        <StatusBanner message={action.message} style={{ marginBottom: 8 }} />
        <ListenAusfall was="Die offenen Einlöse-Wünsche" fehler={offen.error} laedt={offen.loading} onRetry={offen.reload} />
        {offen.showPlaceholder ? <div className="loading">Lade…</div> : offen.data && (
          offen.data.items.length === 0
            ? <p className="muted">Nichts einzulösen.</p>
            : <ul className="vater-einloesen" aria-label="Offene Einlöse-Wünsche" style={LISTE}>
              {offen.data.items.map((r) => (
                <li key={r.id} className="card" style={{ display: "flex", flexDirection: "column", gap: 8 }}>
                  <div>
                    <strong>{nameOf(r.childId)}</strong>: {unitAmount(r.requestedQuantity, r.unitType)} „{r.articleTitle}"
                    <span className="muted"> · {formatDate(r.requestedAt)}</span>
                  </div>
                  <div className="row" style={{ gap: 8, flexWrap: "wrap" }}>
                    {r.canApprove && <button type="button" className="btn lime inline-btn" style={{ width: "auto" }}
                      aria-label={`${nameOf(r.childId)}: „${r.articleTitle}" freigeben`}
                      disabled={action.busy} onClick={() => decide(r, true)}>Freigeben</button>}
                    {r.canReject && <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
                      aria-label={`${nameOf(r.childId)}: „${r.articleTitle}" ablehnen`}
                      disabled={action.busy} onClick={() => decide(r, false)}>Ablehnen</button>}
                  </div>
                </li>
              ))}
              {offen.data.total > offen.data.items.length && (
                <li className="muted">
                  … und {offen.data.total - offen.data.items.length} weitere – im <Link to="/vater/shop">Shop</Link> je Kind.
                </li>
              )}
            </ul>
        )}
      </section>

      <ChildrenTimeline />
    </>
  );
}

/**
 * Ein Kind am heutigen Tag: Status, Antwortzahl (B-770: wie viel, unabhängig von Bonuspunkten), Tagesziel als „n von m"
 * und Punkte. Das Tagesziel liest `goalsMet`/`goalsTotal` (B-749 Entscheidung 3).
 */
function HeuteKind({ c }: { c: ChildDay }) {
  return (
    <li className="card">
      <div className="row" style={{ alignItems: "center", gap: 8, flexWrap: "wrap" }}>
        <Link to={`/vater/kind/${c.childId}`}><strong>{c.name}</strong></Link>
        {c.goalsTotal === 0 ? <span className="pill">kein Tagesziel</span>
          : c.dutyDone ? <span className="pill lime">✓ Tagesziel geschafft</span>
          : c.practiced ? <span className="pill">dran</span>
          : <span className="pill mag">offen</span>}
      </div>
      <dl className="vater-heute-zahlen" style={ZAHLEN}>
        <div><dt className="muted" style={{ fontSize: 13 }}>Geübt heute</dt><dd style={{ margin: 0, fontWeight: 600 }}>{c.answersToday} {c.answersToday === 1 ? "Antwort" : "Antworten"}</dd></div>
        <div><dt className="muted" style={{ fontSize: 13 }}>Tagesziel</dt><dd style={{ margin: 0, fontWeight: 600 }}>{c.goalsTotal === 0 ? "–" : `${c.goalsMet} von ${c.goalsTotal}`}</dd></div>
        <div><dt className="muted" style={{ fontSize: 13 }}>Punkte heute</dt><dd style={{ margin: 0, fontWeight: 600 }}>{c.pointsToday}</dd></div>
      </dl>
    </li>
  );
}
