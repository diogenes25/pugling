import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import type { SubjectLevel } from "../lib/types";
import { ListenAusfall } from "../components/ListenAusfall";

/**
 * Die Notenstufe des Kindes je Vokabel-Fach im Kind-Hub (B-743): der Vater sieht die **Zahl** und den Bestwert – anders
 * als das Kind, das unter Stufe 3 nur sein nächstes Ziel liest. Immer nur dieses eine Kind (B-682 Entscheidung 6: nie
 * im Vergleich mit Geschwistern). Der erste Abruf des Tages bewertet die Stufe neu; darum steht hier nichts zum Speichern.
 */
export function NotenstufenSection({ childId, childName }: { childId: number; childName: string }) {
  const stufen = useAsync<SubjectLevel[]>(() => api.childSubjectLevels(childId), [childId]);

  return (
    <section>
      <h3 className="h-section">Notenstufe</h3>
      <p className="sub">
        Auf welchem Notenstand {childName} in einem Vokabel-Fach steht: der Anteil sicher beherrschter Wörter am Stoff
        des aktiven Plans, umgerechnet mit dem Notenschlüssel. Einmal am Tag neu bewertet; sie sinkt höchstens eine Stufe
        pro Woche. {childName} sieht unter Stufe 3 keine Zahl, nur das nächste Ziel.
      </p>
      <ListenAusfall was="Die Notenstufen" fehler={stufen.error} laedt={stufen.loading} onRetry={stufen.reload} schluessel={childId} />
      {stufen.loading && stufen.data === null && <div className="loading">Lade…</div>}
      {stufen.data && stufen.data.length === 0 && (
        <p className="muted">Noch keine Stufe – sie entsteht in Fächern mit Vokabeln im aktiven Plan.</p>
      )}
      {stufen.data && stufen.data.length > 0 && (
        <table className="table">
          <thead><tr><th>Fach</th><th>Stufe</th><th>Bestwert</th><th>Bis zur nächsten Stufe</th></tr></thead>
          <tbody>
            {stufen.data.map((s) => (
              <tr key={s.subjectId}>
                <td>{s.subjectName}</td>
                <td><strong>{s.level}</strong></td>
                <td>{s.bestLevel}</td>
                <td className="muted">
                  {s.wordsToNextLevel == null ? "beste Stufe"
                    : s.wordsToNextLevel === 0 ? "erreicht – zählt ab morgen"
                      : `noch ${s.wordsToNextLevel} ${s.wordsToNextLevel === 1 ? "Wort" : "Wörter"}`}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
