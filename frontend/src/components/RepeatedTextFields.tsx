import { useEffect, useRef, useState } from "react";

/**
 * Eine Liste gleichartiger Textwerte – **ein Eingabefeld je Wert**, mit „+ …" und Entfernen.
 *
 * Der Gegenentwurf ist das kommagetrennte Sammelfeld, wie es die drei Übungs-Editoren in
 * `vater/exerciseConfig.tsx` noch benutzen. Das hat einen echten Fehler und nicht bloß einen Stil:
 * ein Wert, der **selbst ein Komma enthält** („groß, wirklich groß"), ist dort nicht eintragbar –
 * beim Senden zerreißt ihn `splitList` stillschweigend in zwei. Für die gleichwertigen
 * Übersetzungen einer Vokabel ist genau das ein realistischer Fall.
 *
 * Die **Daten** liegen im aufrufenden Formular, nicht hier – damit Abbrechen dort zurücksetzen kann;
 * eigener State ist nur der Fokus-Zeiger. Leere Felder sind erlaubt (man tippt ja gerade) – aussortiert
 * wird beim Absenden, siehe {@link nonEmpty}.
 *
 * Nachzuziehen sind die drei Komma-Felder in
 * [B-69](../../../docs/backlog/B-69-wiederhol-felder-alternativen.md).
 */
export function RepeatedTextFields({ label, values, onChange, addLabel, scope, placeholder, disabled = false }: {
  /** Fachlicher Name **im Singular** – er trägt die Namen aller Knöpfe („Variante 2 entfernen"). */
  label: string;
  values: string[];
  onChange: (values: string[]) => void;
  /** Beschriftung des Hinzufügen-Knopfs; ohne Angabe „+ <label>". */
  addLabel?: string;
  /**
   * Unterscheidet **mehrere Instanzen auf einem Bildschirm** („Zeile 2") und geht in jeden Namen ein.
   * Nötig, weil sonst drei Anlege-Zeilen dreimal „Variante 1" heißen – für einen Screenreader und für
   * einen Test sind sie dann nur über ihre Position auseinanderzuhalten (dieselbe Begründung wie beim
   * `TagAdder` in `vater/VaterVocab.tsx`).
   */
  scope?: string;
  placeholder?: string;
  disabled?: boolean;
}) {
  const suffix = scope ? ` (${scope})` : "";
  const name = (i: number) => `${label} ${i + 1}${suffix}`;
  const addText = addLabel ?? `+ ${label}`;

  /*
   * Ein neu angelegtes Feld bekommt den Fokus. Ohne das passiert für die Tastatur sichtbar nichts – das
   * Feld erscheint zwei Tabs weiter, und wer Enter zweimal drückt, stapelt stumm leere Felder. Die einzige
   * eigene Zustandsgröße der Komponente, und sie trägt keine Daten: nur „welches Feld ist gerade neu".
   */
  const inputs = useRef<(HTMLInputElement | null)[]>([]);
  const [focusIndex, setFocusIndex] = useState<number | null>(null);
  useEffect(() => {
    if (focusIndex === null) return;
    inputs.current[focusIndex]?.focus();
    setFocusIndex(null);
  }, [focusIndex]);

  function patch(i: number, value: string) {
    onChange(values.map((v, idx) => (idx === i ? value : v)));
  }
  function remove(i: number) {
    onChange(values.filter((_, idx) => idx !== i));
  }
  function add() {
    onChange([...values, ""]);
    setFocusIndex(values.length);
  }

  return (
    <span style={{ display: "flex", flexDirection: "column", gap: 4 }}>
      {values.map((v, i) => (
        <span key={i} className="row" style={{ gap: 4, alignItems: "center" }}>
          <input aria-label={name(i)} value={v} placeholder={placeholder} disabled={disabled}
            ref={(el) => { inputs.current[i] = el; }}
            onChange={(e) => patch(i, e.target.value)}
            /* Kein umgebendes Formular vorausgesetzt – und wo eines steht, darf Enter hier nicht
               absenden, sondern legt die nächste Variante an. Aus einem leeren Feld heraus nicht: sonst
               wächst die Liste, während man nur bestätigen wollte. */
            onKeyDown={(e) => {
              if (e.key !== "Enter") return;
              e.preventDefault();
              if (!disabled && v.trim()) add();
            }} />
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
            disabled={disabled} aria-label={`${name(i)} entfernen`} onClick={() => remove(i)}>×</button>
        </span>
      ))}
      <span>
        {/*
          Der zugängliche Name **enthält den sichtbaren Text** und hängt nur den `scope` an. Ein
          abweichender Name („Variante hinzufügen") verfehlte WCAG 2.5.3 – eine Spracheingabe, die
          „+ Variante" vorliest, träfe den Knopf nicht – und ein eigenes `addLabel` bliebe wirkungslos.
        */}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto", fontSize: 12 }}
          aria-label={`${addText}${suffix}`}
          disabled={disabled} onClick={add}>{addText}</button>
      </span>
    </span>
  );
}

/**
 * Die getippten Werte, wie sie gesendet werden: getrimmt, ohne Leerzeilen. `undefined` statt einer
 * leeren Liste, weil der Vertrag „keine angegeben" als `null`/weggelassen führt – eine leere Liste
 * wäre eine zweite Schreibweise für dasselbe.
 */
export function nonEmpty(values: string[]): string[] | undefined {
  const cleaned = values.map((v) => v.trim()).filter((v) => v.length > 0);
  return cleaned.length > 0 ? cleaned : undefined;
}
