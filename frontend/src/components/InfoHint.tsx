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
  // Lage im Fenster (B-263): gemessen beim Öffnen, `null` bis dahin.
  const [place, setPlace] = useState<Placement | null>(null);
  // Wo der Knopf beim Messen stand – der Bezug, an dem Bewegung erkannt wird.
  const anchorRef = useRef<{ left: number; top: number } | null>(null);
  // Neues Messen bei unbewegtem Knopf (Größenänderung nur der Höhe): ein Zähler, an dem der Mess-Effekt hängt.
  const [measureTick, setMeasureTick] = useState(0);

  // Escape schließt, Klick daneben schließt – ein Popover, das offen bleibt, verdeckt das nächste Feld.
  // Das Popover steht fest zum Fenster und hängt nur über seinen Anker am Knopf: bewegt sich der Knopf, liefe es
  // vom Feld weg. Darum prüft es je Frame, ob der Knopf noch dort steht, wo er beim Messen stand – das fängt
  // Scrollen in jedem Rahmen (auch nur waagrecht) und Inhalt, der darüber aufgeht, ohne dass gescrollt wird (B-273).
  // Ein Scrollen von VOR dem Öffnen (der Browser rollt den Knopf ins Bild) schließt es nicht: gemessen wird danach.
  // Eine Größenänderung schließt nur, wenn sie den Knopf bewegt; sonst – etwa die Bildschirmtastatur, die in manchen
  // Browsern das Layout-Fenster niedriger macht – wird es am selben Knopf neu ausgerichtet.
  useEffect(() => {
    if (!open) return;
    const close = () => setOpen(false);
    const moved = () => {
      const at = anchorRef.current;
      const now = wrapRef.current?.getBoundingClientRect();
      return !at || !now || Math.abs(now.left - at.left) > 1 || Math.abs(now.top - at.top) > 1;
    };
    let frame = 0;
    const watch = () => {
      if (anchorRef.current && moved()) { close(); return; }
      frame = requestAnimationFrame(watch);
    };
    frame = requestAnimationFrame(watch);
    const onResize = () => {
      if (moved()) close();
      else setMeasureTick((n) => n + 1);
    };
    const onKey = (e: KeyboardEvent) => { if (e.key === "Escape") close(); };
    const onDown = (e: MouseEvent) => {
      if (!wrapRef.current?.contains(e.target as Node)) close();
    };
    window.addEventListener("keydown", onKey);
    window.addEventListener("mousedown", onDown);
    window.addEventListener("resize", onResize);
    return () => {
      cancelAnimationFrame(frame);
      window.removeEventListener("keydown", onKey);
      window.removeEventListener("mousedown", onDown);
      window.removeEventListener("resize", onResize);
    };
  }, [open]);

  // Am Knopf verankert und bis zu 320 px breit, stand das Popover neben einem Feld am rechten Rand über: am
  // 768-px-Tablet scrollte dadurch die ganze Seite seitlich (B-68), und in jedem Rahmen mit beschnittenem
  // Überlauf (`.vater-main`, der `overflowX`-Rahmen einer Tabelle) wurde es abgeschnitten – auch unten, wo
  // der Rahmen niedriger ist als das Popover. `position: fixed` hebt es aus allen diesen Rahmen; die Lage
  // rechnet es darum selbst aus den Maßen des Knopfs, je Öffnen und je Größenänderung, bevor der Browser zeichnet.
  useLayoutEffect(() => {
    if (!open) { setPlace(null); return; }
    const pop = popRef.current;
    const wrap = wrapRef.current;
    if (!pop || !wrap) return;
    const btn = wrap.getBoundingClientRect();
    const viewW = document.documentElement.clientWidth;
    anchorRef.current = { left: btn.left, top: btn.top };
    const { width, height } = pop.getBoundingClientRect();
    const viewH = document.documentElement.clientHeight;
    // Ruhelage links am Knopf; nach links verschoben, so weit es rechts übersteht – nie über den linken
    // Rand hinaus: ist das Fenster zu schmal, bleibt wenigstens der Textanfang lesbar.
    const left = Math.max(EDGE, Math.min(btn.left + REST_DX, viewW - EDGE - width));
    // Nach oben nur, wenn es unten abgeschnitten würde UND oben Platz hat.
    const below = btn.bottom + GAP;
    const up = below + height > viewH - EDGE && btn.top - GAP - height >= EDGE;
    // Passt es weder unten noch oben (Querformat am Handy), ins Fenster schieben, auch über den Knopf: weiter-
    // scrollen schlösse es ja. Höher als das Fenster wird es nie – `max-height` in index.css, es scrollt dann selbst.
    const top = Math.max(EDGE, Math.min(up ? btn.top - GAP - height : below, viewH - EDGE - height));
    setPlace({ left, top, up });
  }, [open, measureTick]);

  return (
    <span className="info-hint" ref={wrapRef}>
      <button type="button" className="info-hint-btn" aria-expanded={open} aria-controls={open ? id : undefined}
        // Der vorgelesene Name nennt das Feld: „Info" allein ist in einem Formular mit zehn Hinweisen wertlos.
        aria-label={`Erklärung zu „${help.title}"`}
        onClick={() => setOpen((o) => !o)}>i</button>
      {open && (
        <span className={`info-hint-pop${place?.up ? " up" : ""}`} id={id} role="note" ref={popRef}
          style={place ? { left: place.left, top: place.top } : { visibility: "hidden" }}>
          <b>{help.title}</b>
          <span>{help.text}</span>
        </span>
      )}
    </span>
  );
}

/** `up` trägt keine eigene Gestalt mehr (die Lage steckt in `top`); die Klasse `.up` macht das Klappen prüfbar. */
type Placement = { left: number; top: number; up: boolean };
/** Abstand, den das Popover zum Fensterrand hält, an den es sonst stieße. */
const EDGE = 8;
/** Abstand zwischen Knopf und Popover. */
const GAP = 6;
/** Ruhelage: das Popover beginnt 6 px links vom Knopf, wie vor B-263. */
const REST_DX = -6;

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
