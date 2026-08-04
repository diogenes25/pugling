/**
 * Die gleichwertigen Antworten unter der aufgedeckten Lösung – „auch richtig: …" (B-70).
 *
 * Seit B-65 zählt bei den getippten Stufen jede erklärte Übersetzung. Bei der Selbsteinschätzung urteilt aber
 * das Kind selbst, und es sieht nur die primäre: wer „sehr groß" gedacht hat und „riesig" aufgedeckt bekommt,
 * trägt sich als falsch ein. Genau der Schaden, den B-65 für die Bewertung behoben hat – hier in der Anzeige.
 *
 * Eine eigene Zeile statt einer Kommaliste hinter der Lösung: „die Antwort" und „auch richtig" sind zwei
 * Aussagen, und der Regelfall ohne Alternative soll unverändert aussehen. Und eine geteilte Komponente statt
 * dreier Kopien, weil dieselbe Zeile in Übung, Klausur und Vater-Vorschau steht (Muster `ListRule`).
 */
export function RevealAlternatives({ alternatives }: { alternatives?: readonly string[] | null }) {
  if (!alternatives?.length) return null;
  // Mittelpunkt als Trenner, nicht Komma: eine Übersetzung darf selbst ein Komma enthalten.
  // Der Abstand steht in der CSS-Klasse, nicht als Prop: er ist eine Entscheidung, nicht drei (und die
  // UA-Vorgabe eines <p> hätte die Zeile von der Lösung weggedrückt, zu der sie gehört – wie bei `.list-rule`).
  return <p className="sub reveal-alternatives">auch richtig: {alternatives.join(" · ")}</p>;
}
