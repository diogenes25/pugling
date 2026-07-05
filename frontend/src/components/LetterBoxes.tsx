import { useRef } from "react";

/**
 * Buchstaben-Kästchen für ein Wort bekannter Länge: eine Reihe Einzelfelder, die sich wie ein Feld tippt
 * (Auto-Weiterspringen, Backspace zurück, Enter sendet). Macht getippte Vokabelabfragen greifbarer als ein
 * schlichtes Textfeld. Der zusammengesetzte Wert wird nach oben gereicht; die Bewertung bleibt serverseitig.
 */
export function LetterBoxes({ length, value, onChange, onSubmit }: {
  length: number;
  value: string;
  onChange: (v: string) => void;
  onSubmit?: () => void;
}) {
  const refs = useRef<(HTMLInputElement | null)[]>([]);
  const chars = Array.from({ length }, (_, i) => value[i] ?? "");

  function setChar(i: number, raw: string) {
    const ch = raw.slice(-1); // nur das zuletzt getippte Zeichen übernehmen
    const next = chars.slice();
    next[i] = ch;
    onChange(next.join(""));
    if (ch && i + 1 < length) refs.current[i + 1]?.focus();
  }

  function onKeyDown(i: number, e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === "Enter") { e.preventDefault(); onSubmit?.(); return; }
    if (e.key === "Backspace" && !chars[i] && i > 0) refs.current[i - 1]?.focus();
  }

  return (
    <div className="letterboxes" role="group" aria-label="Buchstaben-Kästchen">
      {chars.map((ch, i) => (
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
      ))}
    </div>
  );
}
