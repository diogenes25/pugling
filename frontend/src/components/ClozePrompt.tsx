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
 * Ohne Lücken (jeder andere Übungstyp) bleibt es beim schlichten Text, **einschließlich der Klasse**: Die
 * Zusatzklasse wird nur gesetzt, wenn wirklich ein Platzhalter gerendert wird – sonst überschriebe die
 * Lückentext-Typografie die der Vokabelkarte, und jedes Wort im Sohn-Web würde kleiner.
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
  const gaps = parts.filter((p) => "gap" in p);

  // Rückfall auf den unveränderten Text, wenn die gefragte Lücke im Text gar nicht vorkommt. Der Editor
  // verhindert das beim Anlegen (`gapProblem`), umnummerierter Altbestand kann es trotzdem: alle Lücken
  // neutral zu zeichnen wäre schlimmer als die rohe Vorlage – dann rät das Kind ohne jeden Anhalt.
  const asked = gapIndex != null && gaps.some((p) => "gap" in p && p.gap === gapIndex);
  if (gaps.length === 0 || (gapIndex != null && !asked)) return <div className={className}>{text}</div>;

  return (
    <div className={`${className} cloze-prompt`}>
      {parts.map((part, i) =>
        "text" in part ? (
          <span key={i}>{part.text}</span>
        ) : (
          // `role="img"` statt eines nackten <span>: Auf `role="generic"` ist ein `aria-label` laut ARIA
          // unzulässig und wird von Screenreadern verworfen – das Zeichen selbst ("?" bzw. "…") lesen die
          // meisten bei Standard-Ausführlichkeit gar nicht vor. Die Rolle macht den Namen zum Inhalt.
          // Sichtbar unterscheidet die Form, nicht nur die Farbe.
          <span
            key={i}
            role="img"
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
