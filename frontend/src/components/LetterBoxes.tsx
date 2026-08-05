import { useRef } from "react";

/**
 * Buchstaben-Kästchen für ein Wort bekannter Länge: eine Reihe Einzelfelder, die sich wie ein Feld tippt
 * (Auto-Weiterspringen, Backspace zurück, Enter sendet). Macht getippte Vokabelabfragen greifbarer als ein
 * schlichtes Textfeld. Der zusammengesetzte Wert wird nach oben gereicht; die Bewertung bleibt serverseitig.
 *
 * `pattern` (B-66): die vom Server gelieferte Maske ("__ ____ __") markiert je Zeichen, ob es zu tippen ist
 * (`_`) oder schon feststeht (jedes andere Zeichen, z. B. Leerzeichen/Satzzeichen). Feste Stellen werden als
 * reiner Text gerendert (kein `<input>`, damit sie nie im Tab-/Sprung-Fokus auftauchen) und tragen ihren
 * Wert unabhängig vom bisherigen `value` bei – so ist der zusammengesetzte String ab dem ersten Tastendruck
 * korrekt, auch an Stellen, die das Kind nie berührt.
 */
export function LetterBoxes({ length, value, onChange, onSubmit, pattern }: {
  length: number;
  value: string;
  onChange: (v: string) => void;
  onSubmit?: () => void;
  pattern?: string;
}) {
  const refs = useRef<(HTMLInputElement | null)[]>([]);
  const isFixed = (i: number) => pattern != null && pattern[i] !== "_";
  const chars = Array.from({ length }, (_, i) => isFixed(i) ? pattern![i] : (value[i] ?? ""));

  // Das nächste TIPPBARE Feld in eine Richtung - überspringt beliebig viele feste Felder am Stück
  // (z. B. ", " oder ein Bindestrich neben einem Leerzeichen), nicht nur ein einzelnes.
  function nextEditable(from: number, dir: 1 | -1): number {
    let j = from + dir;
    while (j >= 0 && j < length && isFixed(j)) j += dir;
    return j;
  }

  function setChar(i: number, raw: string) {
    const ch = raw.slice(-1); // nur das zuletzt getippte Zeichen übernehmen
    const next = chars.slice();
    next[i] = ch;
    onChange(next.join(""));
    if (ch) {
      const target = nextEditable(i, 1);
      if (target < length) refs.current[target]?.focus();
    }
  }

  function onKeyDown(i: number, e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === "Enter") { e.preventDefault(); onSubmit?.(); return; }
    if (e.key === "Backspace" && !chars[i]) {
      const target = nextEditable(i, -1);
      if (target >= 0) refs.current[target]?.focus();
    }
  }

  return (
    <div className="letterboxes" role="group" aria-label="Buchstaben-Kästchen">
      {chars.map((ch, i) => (
        isFixed(i) ? (
          // Kein `aria-hidden`: das Zeichen ist kein Geheimnis (es steht sowieso sichtbar da), und ein Kind
          // mit Screenreader müsste sonst erraten, WAS zwischen zwei Sprüngen übersprungen wurde. Ein
          // rohes Leerzeichen liest mancher Screenreader stumm, darum ein sprechbares Label.
          <span key={i} className="lbox lbox-fixed" aria-label={ch === " " ? "Leerzeichen" : ch}>{ch}</span>
        ) : (
          <input
            key={i}
            ref={(el) => { refs.current[i] = el; }}
            className="lbox"
            inputMode="text"
            maxLength={1}
            autoComplete="off"
            autoCapitalize="off"
            autoCorrect="off"
            spellCheck={false}
            value={ch}
            aria-label={`Buchstabe ${i + 1} von ${length}`}
            onChange={(e) => setChar(i, e.target.value)}
            onKeyDown={(e) => onKeyDown(i, e)}
          />
        )
      ))}
    </div>
  );
}
