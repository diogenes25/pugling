import { useEffect, useState } from "react";

/**
 * Buchstaben ordnen (B-05): Der Server schickt die Buchstaben der Lösung gemischt (`letterTiles`), das Kind legt
 * sie in die richtige Reihenfolge. Das gelegte Wort geht wie jede getippte Antwort nach oben – die Bewertung
 * bleibt serverseitig und kennt diese Stufe nicht gesondert.
 *
 * Bewusst ohne Ziehen: Das Projekt hat keine Drag-&-Drop-Bibliothek, und die Story entscheidet keine (Sichtung
 * 2026-10-23). Bedient wird per Klick/Tipp auf eine Kachel oder per Tastatur – ein Buchstabe wählt die erste
 * freie passende Kachel, Backspace nimmt die letzte zurück, Enter prüft. Damit ist die Tastatur nicht die
 * Ersatz-, sondern die zweite gleichwertige Bedienart (AK 5).
 *
 * Welche Kachel gelegt ist, merkt sich die Komponente über ihre **Position**, nicht über den Buchstaben: bei
 * doppelten Buchstaben („Kaffee“) wäre sonst unklar, welche der beiden E-Kacheln schon benutzt ist. Wer eine neue
 * Karte zeigt, gibt darum einen neuen `key` – und setzt der Aufrufer `value` auf "", fängt sie von vorn an.
 *
 * Eine gelegte Kachel ist `aria-disabled`, nicht `disabled`: Ein Knopf, der mit dem Fokus darauf gesperrt wird, gibt
 * den Fokus im Browser an `body` ab – danach erreichten die getippten Buchstaben den Block nicht mehr (gemessen in
 * `e2e/buchstaben-ordnen.spec.ts`: von „kaufen“ kam „kauf“ an). jsdom kennt diese Regel nicht.
 */
export function LetterTiles({ tiles, value, onChange, onSubmit }: {
  tiles: readonly string[];
  value: string;
  onChange: (v: string) => void;
  onSubmit?: () => void;
}) {
  const [picked, setPicked] = useState<number[]>([]);

  function update(next: number[]) {
    setPicked(next);
    onChange(next.map((i) => tiles[i]).join(""));
  }

  // Der Aufrufer hat geleert (nach dem Absenden): die gelegten Kacheln gehören zur alten Antwort.
  useEffect(() => {
    if (value === "" && picked.length > 0) setPicked([]);
  }, [value, picked.length]);

  const pick = (i: number) => { if (!picked.includes(i)) update([...picked, i]); };
  const undo = () => update(picked.slice(0, -1));

  function onKeyDown(e: React.KeyboardEvent) {
    if (e.key === "Backspace") { e.preventDefault(); undo(); return; }
    // Enter prüft nur von einer Kachel aus. Auf „⌫ Zurück“ und „Neu anfangen“ gehört Enter dem Knopf selbst –
    // sonst schickte es das halbe Wort zur Bewertung ab, statt es zurückzunehmen (Review Lauf D).
    if (e.key === "Enter") {
      if ((e.target as HTMLElement).closest(".lettertiles-pool")) { e.preventDefault(); onSubmit?.(); }
      return;
    }
    if (e.key.length !== 1) return;
    const free = tiles.findIndex((t, i) => !picked.includes(i) && t.toLowerCase() === e.key.toLowerCase());
    if (free >= 0) { e.preventDefault(); pick(free); }
  }

  const word = picked.map((i) => tiles[i]).join("");
  return (
    // Die Tastatur greift überall im Block, auch wenn gerade eine Kachel den Fokus hat.
    <div className="lettertiles" onKeyDown={onKeyDown}>
      <div className="lettertiles-word" aria-hidden="true">
        {tiles.map((_, i) => <span key={i} className="lettertiles-slot">{word[i] ?? ""}</span>)}
      </div>
      {/* Immer im DOM, nur der Inhalt wechselt (B-134): so wird jede gelegte Kachel angesagt. */}
      <p className="sr-only" role="status" aria-live="polite">{word ? `Gelegt: ${word.split("").join(" ")}` : ""}</p>
      <div className="lettertiles-pool" role="group" aria-label="Buchstaben zum Ordnen">
        {tiles.map((t, i) => (
          <button type="button" key={i} className="btn ghost lettertiles-tile" aria-disabled={picked.includes(i)}
            aria-label={`Buchstabe ${t}`} onClick={() => pick(i)}>{t}</button>
        ))}
      </div>
      <div className="row" style={{ gap: 8, justifyContent: "center" }}>
        <button type="button" className="btn ghost small" disabled={picked.length === 0} onClick={undo}>⌫ Zurück</button>
        <button type="button" className="btn ghost small" disabled={picked.length === 0} onClick={() => update([])}>Neu anfangen</button>
      </div>
    </div>
  );
}
