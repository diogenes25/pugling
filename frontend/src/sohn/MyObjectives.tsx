import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import type { Objective, Paged } from "../lib/types";

/**
 * „Meine großen Ziele" – die Sohn-Sicht auf die Objectives, die der Vater gesetzt hat.
 *
 * Bewusst **nur lesend**: Ziele setzt und schneidet der Vater, das Kind sieht, woran es ist. Der Server
 * liefert ausschließlich *aktive* Ziele; ein stillgelegtes taucht hier gar nicht erst auf, damit niemand
 * auf etwas hinarbeitet, das nicht mehr zählt.
 *
 * Die Währung steht dran, weil sie den Unterschied macht: 🪙 verbindlich = echte Belohnung beim Papa,
 * 💎 Dehnungsziel = Skins. Ohne diesen Hinweis sähen beide Ziele gleich aus.
 */
export function MyObjectives() {
  const objectives = useAsync<Paged<Objective>>(() => api.myObjectives({ take: 5 }), []);

  if (objectives.loading) return <div className="card"><div className="loading">Lade Ziele…</div></div>;
  // Ein Fehler hier darf den Trophäenweg nicht abwürgen – die Ziele sind Beiwerk, kein Kern der Seite.
  if (objectives.error || !objectives.data || objectives.data.items.length === 0) return null;

  return (
    <div className="card">
      <div className="row" style={{ marginBottom: 8 }}>
        <b style={{ fontSize: 13 }}>🎯 Meine großen Ziele</b>
        {/* „offen" wäre falsch: `total` ist die Zahl aller *aktiven* Ziele – erreichte zählen mit, und
            nachrechnen kann man es hier nicht, weil nur die ersten fünf geladen sind. */}
        <span className="sub" style={{ marginLeft: "auto" }}>{objectives.data.total} Ziele</span>
      </div>

      {objectives.data.items.map((o) => (
        <div key={o.id} style={{ padding: "6px 0" }}>
          <div className="row">
            <b>{o.title}</b>
            <span className="chip" style={{ marginLeft: "auto" }}>
              {o.kind === "Committed" ? "🪙" : "💎"}<b className="tabnum">{o.rewardOnComplete}</b>
            </span>
          </div>

          {/* Der Balken ist das Wichtigste: „wie weit bin ich" liest sich schneller als jede Zahl. */}
          <div className="bar" aria-hidden><i style={{ width: `${o.progressPercent}%` }} /></div>
          <div className="sub" style={{ fontSize: 11 }}>
            {o.achievedCount}/{o.totalCount} Etappen
            {o.status === "Achieved" ? " · geschafft! 🎉" : o.status === "Overdue" ? " · Termin verpasst" : ""}
            {o.dueDate ? ` · bis ${o.dueDate}` : ""}
          </div>

          {/* Nur die noch offenen Etappen: erledigte abzuhaken ist schön, aber sie verstopfen die Liste. */}
          {o.keyResults.filter((k) => k.status !== "Achieved").slice(0, 3).map((k) => (
            <div className="row" key={k.id} style={{ padding: "2px 0" }}>
              <span>⬜</span>
              <span className="sub">{k.title ?? k.scope}</span>
              <span className="sub tabnum" style={{ marginLeft: "auto" }}>
                {k.currentValue}/{k.targetValue}
              </span>
            </div>
          ))}
        </div>
      ))}
    </div>
  );
}
