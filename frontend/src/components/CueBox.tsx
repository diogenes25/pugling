/**
 * Der Wortkasten des Schul-Vokabeltests (B-524): die Bedeutungen aller Lücken plus Ablenker, vom Server
 * sortiert und auf jeder Karte gleich. Er ersetzt auf dieser Stufe den Hinweis an der Lücke – das Kind
 * sucht die passende Bedeutung selbst und schreibt das englische Wort frei.
 *
 * Ein Baustein für Arcade und Vater-Testmodus: Die Beschriftung kommt vom Aufrufer, weil die Arcade über
 * `t()` spricht und das Vater-Web deutsch ist. Reihenfolge und Inhalt bestimmt allein der Server – hier
 * wird nichts umsortiert, sonst verriete eine Client-Ordnung wieder die Zuordnung.
 */
export function CueBox({ words, label }: { words?: readonly string[] | null; label: string }) {
  if (!words || words.length === 0) return null;
  return (
    <div role="group" aria-label={label} style={{ marginTop: 8 }}>
      <div className="sub" style={{ marginBottom: 4 }}>{label}</div>
      <ul style={{ display: "flex", flexWrap: "wrap", gap: 6, listStyle: "none", padding: 0, margin: 0 }}>
        {words.map((w) => <li key={w} className="chip" style={{ padding: "4px 10px" }}>{w}</li>)}
      </ul>
    </div>
  );
}
