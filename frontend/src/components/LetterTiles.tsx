import { useEffect, useRef, useState } from "react";
import { useChromeT } from "../lib/chromeText";

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
 * doppelten Buchstaben („Kaffee“) wäre sonst unklar, welche der beiden E-Kacheln schon benutzt ist. Neue Kacheln
 * (`tiles`) sind eine neue Karte: die gelegten gehören zur alten und werden verworfen – **ohne** `key` von außen,
 * denn ein Neuaufbau nähme die fokussierte Kachel mit und ließe den Fokus auf `body` fallen (Review Lauf D).
 *
 * **Der Fokus folgt der Karte**, aber nur, wenn er im Block stand oder verloren ging (`body`): Wer per Enter prüft,
 * steht danach auf der ersten Kachel der nächsten Karte und tippt weiter. Auf `body` liegt der Fokus auch nach der
 * Kästchen-Karte (ihr `<input>` ist weg) und nach einem Klick auf „Prüfen“ – der Knopf wird mit dem Absenden
 * gesperrt, und Chromium gibt den Fokus eines gesperrten Knopfs ab (gemessen in `e2e/buchstaben-ordnen.spec.ts`,
 * alle drei Übergänge). Nur ein lebendiges Bedienelement außerhalb behält ihn, etwa das „×“ des Vorschau-Dialogs,
 * wenn die Kacheln erst nach dem Öffnen eintreffen. `tiles` muss referenzstabil sein (jeder Render eine neue Liste
 * hieße jeder Render ein Neustart), und der Aufrufer leert `value` im selben Render wie die Kacheln.
 *
 * Eine gelegte Kachel ist `aria-disabled`, nicht `disabled`: Ein Knopf, der mit dem Fokus darauf gesperrt wird, gibt
 * den Fokus im Browser an `body` ab – danach erreichten die getippten Buchstaben den Block nicht mehr (gemessen in
 * `e2e/buchstaben-ordnen.spec.ts`: von „kaufen“ kam „kauf“ an). jsdom kennt diese Regel nicht.
 *
 * **Mehrere Wörter (B-634):** Der Server mischt jedes Wort für sich und schickt die Kacheln Wort für Wort, die
 * Wortgrenzen als `pattern` („__ ____ __“). Jedes Wort hat dann seinen eigenen Kachelblock und seine eigenen Felder;
 * eine Kachel landet immer in ihrem Wort, und die Leerzeichen setzt die Komponente selbst – sie sind keine Kachel,
 * die Lösung legt sie ja schon fest. Ohne `pattern` ist alles ein Wort wie bisher.
 */
export function LetterTiles({ tiles, value, onChange, onSubmit, gesperrt = false, pattern }: {
  tiles: readonly string[];
  value: string;
  onChange: (v: string) => void;
  onSubmit?: () => void;
  /** Die Wortgrenzen einer Lösung aus mehreren Wörtern (B-634), z. B. „__ ____ __“; ohne = ein Wort. */
  pattern?: string | null;
  /** Nichts legen und nichts zurücknehmen (B-532) – als `aria-disabled`, aus demselben Fokus-Grund wie oben. */
  gesperrt?: boolean;
}) {
  const t = useChromeT();
  const [picked, setPicked] = useState<number[]>([]);
  const pool = useRef<HTMLDivElement>(null);

  // Neue Kacheln = neue Karte: Zustand noch im Render zurücksetzen (React: „adjusting state when a prop
  // changes“), damit kein gelegter Index der alten Karte je auf die neuen Kacheln zeigt.
  const [shown, setShown] = useState(tiles);
  if (tiles !== shown) {
    setShown(tiles);
    setPicked([]);
  }

  const woerter = wortBereiche(tiles.length, pattern);
  const wortVon = (i: number) => woerter.findIndex((w) => i >= w.start && i < w.start + w.laenge);
  // Je Wort die gelegten Kacheln in Lege-Reihenfolge – das gelegte Wort ist ihre Folge, die Wörter mit Leerzeichen.
  const gelegt = (p: readonly number[]) => woerter.map((_, w) => p.filter((i) => wortVon(i) === w).map((i) => tiles[i]).join(""));

  function update(next: number[]) {
    if (gesperrt) return;
    setPicked(next);
    onChange(gelegt(next).join(" ").trimEnd());
  }

  // Der Aufrufer hat geleert (nach dem Absenden): die gelegten Kacheln gehören zur alten Antwort.
  useEffect(() => {
    if (value === "" && picked.length > 0) setPicked([]);
  }, [value, picked.length]);

  useEffect(() => {
    const active = document.activeElement;
    if (!active || active === document.body || pool.current?.contains(active)) {
      pool.current?.querySelector<HTMLButtonElement>("button")?.focus();
    }
  }, [tiles]);

  const pick = (i: number) => { if (!picked.includes(i)) update([...picked, i]); };
  const undo = () => update(picked.slice(0, -1));

  function onKeyDown(e: React.KeyboardEvent) {
    // Strg+R, Alt+Tab, Cmd+… sind Browser- und Systemkürzel, keine Buchstaben: sonst legte Strg+R eine R-Kachel.
    if (e.ctrlKey || e.metaKey || e.altKey) return;
    if (e.key === "Backspace") { e.preventDefault(); undo(); return; }
    // Enter prüft nur von einer Kachel aus. Auf „⌫ Zurück“ und „Neu anfangen“ gehört Enter dem Knopf selbst –
    // sonst schickte es das halbe Wort zur Bewertung ab, statt es zurückzunehmen (Review Lauf D).
    if (e.key === "Enter") {
      if ((e.target as HTMLElement).closest(".lettertiles-pool")) { e.preventDefault(); onSubmit?.(); }
      return;
    }
    if (e.key.length !== 1) return;
    // Die erste freie passende Kachel. Bei mehreren Wörtern ist das von selbst eine im ersten noch nicht vollen Wort: die
    // Wörter liegen der Reihe nach, und jedes Wort davor ist voll.
    const free = tiles.findIndex((t, i) => !picked.includes(i) && t.toLowerCase() === e.key.toLowerCase());
    if (free >= 0) { e.preventDefault(); pick(free); }
  }

  const proWort = gelegt(picked);
  const word = proWort.join("");
  return (
    // Die Tastatur greift überall im Block, auch wenn gerade eine Kachel den Fokus hat.
    <div className="lettertiles" onKeyDown={onKeyDown}>
      <div className="lettertiles-word" aria-hidden="true">
        {woerter.map((w, n) => (
          <span key={n} className="lettertiles-wort">
            {Array.from({ length: w.laenge }, (_, i) => <span key={i} className="lettertiles-slot">{proWort[n][i] ?? ""}</span>)}
          </span>
        ))}
      </div>
      {/* Immer im DOM, nur der Inhalt wechselt (B-134): so wird jede gelegte Kachel angesagt. */}
      {/* Je Wort buchstabiert, die Wörter durch „ / “ getrennt – sonst hörte man die Wortgrenze nicht (B-634). */}
      <p className="sr-only" role="status" aria-live="polite">
        {word ? t("play.placed", { letters: proWort.filter(Boolean).map((w) => w.split("").join(" ")).join(" / ") }) : ""}
      </p>
      <div ref={pool} className="lettertiles-pool" role="group" aria-label={t("play.tilesPool")}>
        {woerter.map((w, n) => (
          // Mehrere Wörter: jedes ist eine benannte Gruppe, damit ein Screenreader Zahl und Länge der Wörter hört.
          <span key={n} className="lettertiles-wort" role={woerter.length > 1 ? "group" : undefined}
            aria-label={woerter.length > 1 ? t("play.tilesWord", { n: n + 1, count: w.laenge }) : undefined}>
            {tiles.slice(w.start, w.start + w.laenge).map((tile, k) => {
              const i = w.start + k;
              return (
                <button type="button" key={i} className="btn ghost lettertiles-tile" aria-disabled={gesperrt || picked.includes(i)}
                  aria-label={t("play.tile", { letter: tile })} onClick={() => pick(i)}>{tile}</button>
              );
            })}
          </span>
        ))}
      </div>
      <div className="row" style={{ gap: 8, justifyContent: "center" }}>
        <button type="button" className="btn ghost small" disabled={gesperrt || picked.length === 0} onClick={undo}>{t("play.undo")}</button>
        <button type="button" className="btn ghost small" disabled={gesperrt || picked.length === 0} onClick={() => update([])}>{t("play.restart")}</button>
      </div>
    </div>
  );
}

/**
 * Die Kachel-Bereiche je Wort (B-634): aus dem Muster die Länge jedes Worts, Kachel für Kachel aufaddiert. Passt das
 * Muster nicht zur Kachelzahl (fehlt, oder alter Stand), ist alles ein Wort – lieber ein Block als falsch geteilte.
 */
export function wortBereiche(anzahl: number, pattern?: string | null): { start: number; laenge: number }[] {
  const laengen = (pattern ?? "").split(" ").map((w) => w.length).filter((l) => l > 0);
  if (laengen.length < 2 || laengen.reduce((a, b) => a + b, 0) !== anzahl) return [{ start: 0, laenge: anzahl }];
  let start = 0;
  return laengen.map((laenge) => { const w = { start, laenge }; start += laenge; return w; });
}
