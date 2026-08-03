/**
 * Der Stoff, auf den sich eine Frage bezieht: der Lesetext einer Leseverstehen-Übung, die übergreifende
 * Anweisung einer Grammatik-Übung. Er gehört der *Übung*, nicht der einzelnen Karte, und steht darum über
 * der Frage – man liest ihn zuerst.
 *
 * Eigenes Bauteil, weil drei Ansichten ihn zeigen (Üben, Klausur, Testmodus des Vaters) und weil er
 * Tastatur und Screenreader etwas schuldet: Ein Kasten mit `overflow` ist in Chrome und Safari **nicht**
 * fokussierbar, solange nichts Fokussierbares darin liegt – ohne `tabIndex` käme man mit der Tastatur nicht
 * über die sichtbare Höhe hinaus. Und ohne Namen läse ein Screenreader den Text ohne jede Ansage direkt
 * vor der Frage.
 */
export function Passage({ text, label = "Text zur Aufgabe" }: { text?: string | null; label?: string }) {
  if (!text) return null;

  return (
    // `tabIndex={0}` macht den Scroll-Bereich per Tastatur erreichbar, die Rolle trägt seinen Namen.
    <div className="passage" tabIndex={0} role="group" aria-label={label}>
      {text}
    </div>
  );
}
