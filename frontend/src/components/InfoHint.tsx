import { useEffect, useId, useRef, useState } from "react";
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

  return (
    <span className="info-hint" ref={wrapRef}>
      <button type="button" className="info-hint-btn" aria-expanded={open} aria-controls={open ? id : undefined}
        // Der vorgelesene Name nennt das Feld: „Info" allein ist in einem Formular mit zehn Hinweisen wertlos.
        aria-label={`Erklärung zu „${help.title}"`}
        onClick={() => setOpen((o) => !o)}>i</button>
      {open && (
        <span className="info-hint-pop" id={id} role="note">
          <b>{help.title}</b>
          <span>{help.text}</span>
        </span>
      )}
    </span>
  );
}

/**
 * Feld-Beschriftung mit angehängtem „ⓘ". Ersetzt ein `<label>` eins zu eins – der Hinweis steht als
 * Geschwister *neben* dem Label und nicht darin, sonst finge der Klick aufs Fragezeichen den Klick auf
 * die Beschriftung ab (die den Fokus ins Eingabefeld setzt).
 */
export function FieldLabel({ htmlFor, topic, children }: {
  htmlFor?: string;
  topic: HelpTopic;
  children: React.ReactNode;
}) {
  return (
    <span className="label-row">
      <label htmlFor={htmlFor}>{children}</label>
      <InfoHint topic={topic} />
    </span>
  );
}
