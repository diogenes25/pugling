import { useEffect, useId, useLayoutEffect, useRef, useState } from "react";
import { FIELD_HELP, type HelpTopic } from "../lib/fieldHelp";

/*
 * Erklärung zu einem Eingabefeld, abrufbar über ein „ⓘ" neben der Beschriftung.
 *
 * Warum eine eigene Komponente statt `title=`: Ein Browser-Tooltip erscheint erst nach einer Sekunde
 * Verharren, ist auf dem Handy gar nicht erreichbar und lässt sich nicht formatieren – ausgerechnet die
 * Felder, die Erklärung brauchen (Malus, Leitner, Combo), bedient der Vater am Telefon.
 *
 * Der Text steht NICHT hier, sondern in [fieldHelp.ts](../lib/fieldHelp.ts): dieselbe Größe taucht in
 * mehreren Masken auf (der Assistent stellt dieselbe Position ein wie die Plan-Seite), und zwei
 * Formulierungen desselben Begriffs sind schlimmer als keine.
 */
export function InfoHint({ topic }: { topic: HelpTopic }) {
  const help = FIELD_HELP[topic];
  const id = useId();
  const [open, setOpen] = useState(false);
  const wrapRef = useRef<HTMLSpanElement | null>(null);
  const popRef = useRef<HTMLSpanElement | null>(null);
  // Lage im sichtbaren Bereich (B-263): wie weit nach links verschoben, und ob nach oben geklappt.
  const [place, setPlace] = useState<Placement>(REST);
  const placeRef = useRef<Placement>(REST);

  // Escape schließt, Klick daneben schließt – ein Popover, das offen bleibt, verdeckt das nächste Feld.
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => { if (e.key === "Escape") setOpen(false); };
    const onDown = (e: MouseEvent) => {
      if (!wrapRef.current?.contains(e.target as Node)) setOpen(false);
    };
    window.addEventListener("keydown", onKey);
    window.addEventListener("mousedown", onDown);
    return () => {
      window.removeEventListener("keydown", onKey);
      window.removeEventListener("mousedown", onDown);
    };
  }, [open]);

  // Am Knopf verankert und bis zu 320 px breit, stand das Popover neben einem Feld am rechten Rand über:
  // am 768-px-Tablet scrollte dadurch die ganze Seite seitlich (B-68), in einem scrollenden Rahmen wurde es
  // abgeschnitten – unten genauso, seit `.vater-main` den Überlauf selbst aufnimmt. Gemessen einmal je
  // Öffnen, bevor der Browser zeichnet; die schon angewandte Lage wird herausgerechnet, damit ein zweiter
  // Lauf dasselbe Ergebnis liefert.
  useLayoutEffect(() => {
    const apply = (next: Placement) => { placeRef.current = next; setPlace(next); };
    if (!open) { apply(REST); return; }
    const pop = popRef.current;
    const wrap = wrapRef.current;
    if (!pop || !wrap) return;
    const bounds = visibleBounds(wrap);
    const rect = pop.getBoundingClientRect();
    const btn = wrap.getBoundingClientRect();
    // Ruhelage: ohne Verschiebung, unter dem Knopf.
    const left = rect.left + placeRef.current.shift;
    const right = rect.right + placeRef.current.shift;
    const height = rect.height;
    const over = right - (bounds.right - EDGE);
    // Nie über den linken Rand hinaus: ist der Platz zu schmal, bleibt wenigstens der Textanfang lesbar.
    const room = left - (bounds.left + EDGE);
    const shift = over > 0 ? Math.max(0, Math.min(over, room)) : 0;
    // Nach oben nur, wenn es unten abgeschnitten würde UND oben Platz hat.
    const below = btn.bottom + GAP + height;
    const up = below > bounds.bottom - EDGE && btn.top - GAP - height >= bounds.top + EDGE;
    apply({ shift, up });
  }, [open]);

  return (
    <span className="info-hint" ref={wrapRef}>
      <button type="button" className="info-hint-btn" aria-expanded={open} aria-controls={open ? id : undefined}
        // Der vorgelesene Name nennt das Feld: „Info" allein ist in einem Formular mit zehn Hinweisen wertlos.
        aria-label={`Erklärung zu „${help.title}"`}
        onClick={() => setOpen((o) => !o)}>i</button>
      {open && (
        <span className={`info-hint-pop${place.up ? " up" : ""}`} id={id} role="note" ref={popRef}
          style={place.shift > 0 ? { marginLeft: -place.shift } : undefined}>
          <b>{help.title}</b>
          <span>{help.text}</span>
        </span>
      )}
    </span>
  );
}

type Placement = { shift: number; up: boolean };
const REST: Placement = { shift: 0, up: false };
/** Abstand, den das Popover zum Rand hält, an den es sonst stieße. */
const EDGE = 8;
/** Abstand zwischen Knopf und Popover, wie in `.info-hint-pop` (index.css: `calc(100% + 6px)`). */
const GAP = 6;

/**
 * Der Bereich, in dem ein Element tatsächlich zu sehen ist: waagrecht das Fenster, verengt um jeden Vorfahren,
 * der seinen Überlauf beschneidet (der `overflowX`-Rahmen einer Tabelle, `.vater-main`); senkrecht nur diese
 * Vorfahren – über den Fensterrand hinaus scrollt die Seite, das schneidet nichts ab.
 */
function visibleBounds(el: HTMLElement): { left: number; right: number; top: number; bottom: number } {
  let left = 0;
  let right = document.documentElement.clientWidth;
  let top = -Infinity;
  let bottom = Infinity;
  for (let a = el.parentElement; a && a !== document.documentElement; a = a.parentElement) {
    const style = getComputedStyle(a);
    const r = a.getBoundingClientRect();
    if (style.overflowX !== "visible") {
      left = Math.max(left, r.left + a.clientLeft);
      right = Math.min(right, r.left + a.clientLeft + a.clientWidth);
    }
    if (style.overflowY !== "visible") {
      top = Math.max(top, r.top + a.clientTop);
      bottom = Math.min(bottom, r.top + a.clientTop + a.clientHeight);
    }
  }
  return { left, right, top, bottom };
}

/**
 * Feld-Beschriftung mit angehängtem „ⓘ". Ersetzt ein `<label>` eins zu eins – der Hinweis steht als
 * Geschwister *neben* dem Label und nicht darin, sonst finge der Klick aufs Fragezeichen den Klick auf
 * die Beschriftung ab (die den Fokus ins Eingabefeld setzt).
 *
 * `id` braucht, wer mit dem Label eine ganze Gruppe benennt (`role="group"` + `aria-labelledby`, B-236): dann
 * gibt es kein einzelnes Feld für `htmlFor`. Die `id` sitzt am Label, nicht an der Zeile – sonst hieße die
 * Gruppe auch „Erklärung zu …".
 */
export function FieldLabel({ htmlFor, id, topic, children }: {
  htmlFor?: string;
  id?: string;
  topic: HelpTopic;
  children: React.ReactNode;
}) {
  return (
    <span className="label-row">
      <label htmlFor={htmlFor} id={id}>{children}</label>
      <InfoHint topic={topic} />
    </span>
  );
}
