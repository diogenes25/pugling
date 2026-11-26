import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { mengeAntwort } from "../lib/mengeAntwort";

/** Zwei Felder sind der Regelfall einer quadratischen Gleichung; mehr als vier sähe aus wie ein Formular (Entscheidung 2). */
const START = 2;
const HOECHSTENS = 4;

/**
 * B-712: die Antwortfläche einer Lösungsmenge – getrennte Zahlenfelder (zwei, per „+“ bis vier) und der Umschalter
 * „keine Lösung“. Das Kind muss die Trenner-Regel nicht kennen: die Felder werden über `mengeAntwort` zu dem einen Text
 * gebunden, den der Server wertet.
 *
 * Die Felder hält die Komponente selbst: aus dem gebundenen, sortierten Text ließen sie sich nicht zurücklesen, ohne dass
 * die Werte beim Tippen das Feld wechseln. `kartenId` setzt sie zurück, **ohne** die Fläche neu einzuhängen – wie
 * `Antwortflaeche` (Review B-526): stand der Fokus in der Fläche, springt er ins erste Feld, und am Handy bleibt die
 * Tastatur offen.
 *
 * `inputMode="decimal"` statt `type="number"` (Entscheidung 3): die Zahlentastatur mit Komma, und das Komma wird nicht
 * verschluckt. Die Felder sperren per `readOnly`, nicht `disabled` – so behält ein fokussiertes Feld den Fokus (B-532).
 */
export function Mengeneingabe({ kartenId, onWert, onAbschicken, gesperrt }: {
  kartenId: string | number;
  onWert: (wert: string) => void;
  onAbschicken: () => void;
  gesperrt: boolean;
}) {
  const { t } = useTranslation();
  const [felder, setFelder] = useState<string[]>(() => Array(START).fill(""));
  const [keine, setKeine] = useState(false);
  const refs = useRef<(HTMLInputElement | null)[]>([]);
  const flaeche = useRef<HTMLFormElement | null>(null);

  // Welches Feld nach dem nächsten Commit den Fokus bekommt – ein Feld, das es vorher noch nicht gab (oder nicht mehr).
  const fokusAuf = useRef<number | null>(null);

  const [gesehen, setGesehen] = useState(kartenId);
  if (gesehen !== kartenId) {
    setGesehen(kartenId);
    setFelder(Array(START).fill(""));
    setKeine(false);
    // Neue Karte: stand der Fokus in der Fläche (das Kind hat mit Enter abgeschickt), wandert er ins erste Feld. Gemessen
    // VOR dem Commit – stand er im dritten oder vierten Feld, fällt das mit dem Zurücksetzen weg und der Fokus auf `body`.
    if (flaeche.current?.contains(document.activeElement)) fokusAuf.current = 0;
  }
  useEffect(() => {
    if (fokusAuf.current === null) return;
    refs.current[fokusAuf.current]?.focus();
    fokusAuf.current = null;
  });

  const melden = (naechste: string[], keineLoesung: boolean) => onWert(mengeAntwort(naechste, keineLoesung));

  function setzen(i: number, wert: string) {
    if (gesperrt || keine) return;
    const naechste = felder.slice();
    naechste[i] = wert;
    setFelder(naechste);
    melden(naechste, false);
  }

  function umschalten() {
    if (gesperrt) return;
    const an = !keine;
    const leer = Array(felder.length).fill("");
    setKeine(an);
    setFelder(leer);
    melden(leer, an);
  }

  function dazu() {
    if (gesperrt || keine || felder.length >= HOECHSTENS) return;
    // Das neue Feld bekommt den Fokus; beim vierten verschwindet „+“ und nähme ihn sonst mit.
    fokusAuf.current = felder.length;
    setFelder([...felder, ""]);
  }

  return (
    <form ref={flaeche} className="mengeneingabe" onSubmit={(e) => e.preventDefault()}>
      <div className="row" style={{ gap: 8, flexWrap: "wrap" }}>
        {felder.map((f, i) => (
          <input
            key={i} ref={(el) => { refs.current[i] = el; }}
            className="lektion-feld" style={{ flex: "1 1 6em", minWidth: "5em" }}
            aria-label={t("lektion.setField", { n: i + 1 })} name={`loesung-${i + 1}`}
            inputMode="decimal" autoComplete="off" autoCorrect="off" spellCheck={false}
            value={f} onChange={(e) => setzen(i, e.target.value)} readOnly={gesperrt || keine}
            // Enter schickt aus jedem Feld ab (ein Formular mit mehreren Feldern tut das ohne Absende-Knopf nicht).
            onKeyDown={(e) => { if (e.key === "Enter") { e.preventDefault(); onAbschicken(); } }}
          />
        ))}
      </div>
      <div className="row" style={{ gap: 8, marginTop: 8, flexWrap: "wrap" }}>
        {felder.length < HOECHSTENS && (
          <button type="button" className="btn ghost small" disabled={gesperrt || keine} onClick={dazu}>
            {t("lektion.setAdd")}
          </button>
        )}
        <button type="button" className={`btn ghost small lektion-option${keine ? " gewaehlt" : ""}`}
          aria-pressed={keine} disabled={gesperrt} onClick={umschalten}>
          {t("lektion.setNone")}
        </button>
      </div>
    </form>
  );
}
