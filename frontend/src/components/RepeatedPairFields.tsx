import { useEffect, useRef, useState } from "react";

/** A pair of values held by one row - the shape `RepeatedPairFields` reads and writes. */
export interface Pair {
  word: string;
  gloss: string;
}

/**
 * Wie {@link RepeatedTextFields}, aber für **Paare** statt Einzelwerte – **zwei** Eingabefelder je Zeile.
 *
 * Der Gegenentwurf ist das kommagetrennte Sammelfeld "Wort:wörtlich, Wort:wörtlich", **zweifach**
 * zerlegt (erst an `,`, dann an `:`). Das hatte denselben Fehler wie das Sammelfeld, das
 * {@link RepeatedTextFields} ablöste – nur mit zwei verschachtelten Trennzeichen statt einem: ein Komma
 * ODER ein Doppelpunkt in Wort oder Glosse zerriss den Eintrag lautlos in zusätzliche, falsche Paare. Bei
 * einer wörtlichen Übersetzung (Umschreibungen wie „ist im Begriff zu, …") ist das der Normalfall, kein
 * Rand ([B-72](../../../docs/backlog/B-72-birkenbihl-dekodierung-paarfelder.md)).
 *
 * `RepeatedTextFields` selbst passt nicht: es führt `values: string[]` – ein Wert je Zeile. Ein Wortpaar
 * ist zwei Werte je Zeile, das Prop auf ein Tupel-Array umzustellen bräche das bestehende Interface für
 * alle heutigen Aufrufer ohne Nutzen für sie.
 *
 * Die **Daten** liegen im aufrufenden Formular, nicht hier – wie bei `RepeatedTextFields`.
 */
export function RepeatedPairFields({ wordLabel, glossLabel, pairs, onChange, addLabel, scope, disabled = false }: {
  /** Fachlicher Name des ersten Feldes je Zeile, **im Singular** (z. B. „Wort"). */
  wordLabel: string;
  /** Fachlicher Name des zweiten Feldes je Zeile, **im Singular** (z. B. „wörtlich"). */
  glossLabel: string;
  pairs: Pair[];
  onChange: (pairs: Pair[]) => void;
  /** Beschriftung des Hinzufügen-Knopfs; ohne Angabe „+ <wordLabel>". */
  addLabel?: string;
  /** Unterscheidet mehrere Instanzen auf einem Bildschirm (Muster wie `RepeatedTextFields`). */
  scope?: string;
  disabled?: boolean;
}) {
  const suffix = scope ? ` (${scope})` : "";
  const wordName = (i: number) => `${wordLabel} ${i + 1}${suffix}`;
  const glossName = (i: number) => `${glossLabel} ${i + 1}${suffix}`;
  const addText = addLabel ?? `+ ${wordLabel}`;

  // Ein neu angelegtes Paar bekommt den Fokus auf sein erstes Feld - dieselbe Begründung wie in `RepeatedTextFields`.
  const wordInputs = useRef<(HTMLInputElement | null)[]>([]);
  const [focusIndex, setFocusIndex] = useState<number | null>(null);
  useEffect(() => {
    if (focusIndex === null) return;
    wordInputs.current[focusIndex]?.focus();
    setFocusIndex(null);
  }, [focusIndex]);

  function patch(i: number, patch: Partial<Pair>) {
    onChange(pairs.map((p, idx) => (idx === i ? { ...p, ...patch } : p)));
  }
  function remove(i: number) {
    onChange(pairs.filter((_, idx) => idx !== i));
  }
  function add() {
    onChange([...pairs, { word: "", gloss: "" }]);
    setFocusIndex(pairs.length);
  }

  return (
    <span style={{ display: "flex", flexDirection: "column", gap: 4 }}>
      {pairs.map((p, i) => {
        // Kein umgebendes Formular vorausgesetzt - und wo eines steht (Birkenbihl steckt in
        // VaterExerciseCreate.tsx' <form>), darf Enter hier nicht absenden, sondern legt das nächste
        // Wortpaar an. Aus einer Zeile ohne Wort heraus nicht: sonst wächst die Liste beim bloßen
        // Bestätigen-Wollen (Muster wie `RepeatedTextFields`).
        const onEnter = (e: React.KeyboardEvent) => {
          if (e.key !== "Enter") return;
          e.preventDefault();
          if (!disabled && p.word.trim()) add();
        };
        return (
          <span key={i} className="row" style={{ gap: 4, alignItems: "center" }}>
            <input aria-label={wordName(i)} value={p.word} disabled={disabled}
              ref={(el) => { wordInputs.current[i] = el; }}
              onChange={(e) => patch(i, { word: e.target.value })} onKeyDown={onEnter} />
            <input aria-label={glossName(i)} value={p.gloss} disabled={disabled}
              onChange={(e) => patch(i, { gloss: e.target.value })} onKeyDown={onEnter} />
            <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
              disabled={disabled} aria-label={`${wordName(i)} entfernen`} onClick={() => remove(i)}>×</button>
          </span>
        );
      })}
      <span>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto", fontSize: 12 }}
          aria-label={`${addText}${suffix}`}
          disabled={disabled} onClick={add}>{addText}</button>
      </span>
    </span>
  );
}

/**
 * Die getippten Paare, wie sie gesendet werden: getrimmt, Zeilen mit leerem Wort ausgeschlossen (eine
 * Glosse ohne Wort ist kein Paar). Muster wie {@link nonEmpty} in `RepeatedTextFields`.
 */
export function nonEmptyPairs(pairs: Pair[]): { learningWord: string; gloss: string | null }[] {
  return pairs
    .map((p) => ({ word: p.word.trim(), gloss: p.gloss.trim() }))
    .filter((p) => p.word.length > 0)
    .map((p) => ({ learningWord: p.word, gloss: p.gloss || null }));
}
