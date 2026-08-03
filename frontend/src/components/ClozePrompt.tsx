import { clozeParts } from "../lib/cloze";

/**
 * Der Aufgabentext eines Lückentexts – mit der **gefragten** Lücke hervorgehoben.
 *
 * Alle Karten eines Lückentexts tragen denselben Text; das ist richtig, es ist ja ein Text. Ohne eine
 * Auszeichnung, welche Lücke gerade dran ist, sind zwei Karten aber zeichengleich, und das Kind muss raten
 * (B-76). Die Nummer kommt darum vom Server (`gapIndex`) und die Darstellung von hier – der Server bliebe
 * sonst in der Pflicht, Text zu setzen, und müsste dabei entweder die Nachbarlücken verraten oder rohe
 * `{{n}}` stehen lassen.
 *
 * Ohne `gapIndex` (jeder andere Übungstyp) bleibt es beim schlichten Text.
 *
 * `className` bleibt beim Aufrufer: Übungsrunde und Klausur setzen den Aufgabentext unterschiedlich groß,
 * und das ist ihre Entscheidung, nicht die dieses Bauteils.
 */
export function ClozePrompt({ text, gapIndex, className = "word" }: {
  text: string;
  gapIndex?: number | null;
  className?: string;
}) {
  const parts = clozeParts(text);

  return (
    <div className={`${className} cloze-prompt`}>
      {parts.map((part, i) =>
        "text" in part ? (
          <span key={i}>{part.text}</span>
        ) : (
          // Die gefragte Lücke ist auch ohne Farbe erkennbar (Fragezeichen statt Strichen) – Farbe allein
          // trägt keine Bedeutung.
          <span
            key={i}
            className={part.gap === gapIndex ? "cloze-gap asked" : "cloze-gap"}
            aria-label={part.gap === gapIndex ? "gesuchte Lücke" : `Lücke ${part.gap}`}
          >
            {part.gap === gapIndex ? "?" : "…"}
          </span>
        ),
      )}
    </div>
  );
}
