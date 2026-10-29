import { clozeParts, showsGaps } from "../lib/cloze";
import { useChromeT } from "../lib/chromeText";

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
 * Fehlt der Text ganz, kommt **nichts** – auf der Vokabel-Hörstufe lässt der Server ihn bewusst weg, damit
 * aus „hören und tippen" keine Leseaufgabe wird. Das Bauteil rendert dann keinen leeren Kasten.
 *
 * `className` bleibt beim Aufrufer: Übungsrunde und Klausur setzen den Aufgabentext unterschiedlich groß,
 * und das ist ihre Entscheidung, nicht die dieses Bauteils.
 */
export function ClozePrompt({ text, gapIndex, className = "word" }: {
  text?: string | null;
  gapIndex?: number | null;
  className?: string;
}) {
  const t = useChromeT();
  if (!text) return null;
  const parts = clozeParts(text);

  // Rückfall auf den unveränderten Text, wenn die gefragte Lücke im Text gar nicht vorkommt. Der Editor
  // verhindert das beim Anlegen (`gapProblem`), umnummerierter Altbestand kann es trotzdem: alle Lücken
  // neutral zu zeichnen wäre schlimmer als die rohe Vorlage – dann rät das Kind ohne jeden Anhalt.
  if (!showsGaps(parts, gapIndex)) return <div className={className}>{text}</div>;

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
            aria-label={part.gap === gapIndex ? t("play.gapAsked") : t("play.gap", { n: part.gap })}
          >
            {part.gap === gapIndex ? "?" : "…"}
          </span>
        ),
      )}
    </div>
  );
}

/**
 * Sagt die aktuelle Aufgabe in einer Live-Region an (B-286). Beim Kartenwechsel behält die Eingabe den Fokus – ein
 * Screenreader läse sonst nur das Feld vor und nie, was gefragt ist. Den Fokus auf die Karte zu ziehen hieße, die
 * Regel aus B-05 (Fokus auf der ersten Kachel) zu brechen; die Region lässt ihn, wo er ist.
 *
 * Die Region steht **immer** im DOM (B-134) und wechselt nur ihren Text. Übung und Klausur hängen sie darum über alle
 * ihre Ansichten (Laden, Fehler, Karte): so wächst auch die erste Aufgabe und die nach einem Fehler fortgesetzte in
 * einen schon vorhandenen Knoten. Zwei Karten mit gleichem Text werden nur einmal angesagt; die Hörstufe ohne Text sagt
 * nichts – ihr Ton spielt an.
 */
export function TaskAnnouncer({ text, gapIndex }: { text?: string | null; gapIndex?: number | null }) {
  const t = useChromeT();
  return <p className="sr-only" role="status" aria-live="polite">{text ? t("play.newTask", { task: spoken(text, gapIndex, t) }) : ""}</p>;
}

/** Der Text, wie `ClozePrompt` ihn zeigt, mit den Lücken als Wörter – dieselbe Rückfall-Regel. */
function spoken(text: string, gapIndex: number | null | undefined, t: ReturnType<typeof useChromeT>): string {
  const parts = clozeParts(text);
  if (!showsGaps(parts, gapIndex)) return text;
  return parts.map((p) => ("text" in p ? p.text : p.gap === gapIndex ? t("play.gapAsked") : t("play.gap", { n: p.gap }))).join("");
}
