/**
 * Ein `<select>` für eine **Referenz** (Fach, Verlag, Reihe, Unit) – also für einen Wert, dessen
 * Optionen aus einem eigenen, unabhängig scheiternden Abruf kommen.
 *
 * Warum das ein Bauteil ist und keine sieben Handgriffe (B-172): Ein `<select>` mit Anzeigegröße 1 wählt
 * ohne passende Option **die erste selbst** – im Browser gemessen, nicht nur unter happy-dom. Fehlt der
 * geladene Wert in der Liste, zeigt das Feld also „– keine Angabe –", während der Formularzustand noch den
 * echten Wert hält. Anzeige und Zustand driften, der Nutzer entscheidet auf falscher Information, und weil
 * alle sieben Felder einen `clear…`-Schalter speisen, zerstört jeder seiner beiden Auswege den Wert.
 *
 * Die Regel dagegen ist klein, aber sie muss an sieben Stellen gleich sein – und siebenmal von sieben hat
 * die Disziplin hier verloren. Darum eine Stelle, mit demselben Argument, mit dem `subjectField.ts` den
 * PATCH-Rumpf an einer Stelle hält.
 */

/** Eine Option, vom Aufrufer schon fertig beschriftet – das Bauteil kennt keine Fachbegriffe. */
export interface ReferenceOption {
  value: string;
  label: string;
}

export interface ReferenceSelectProps {
  /** DOM-Id fürs `htmlFor` des Labels. */
  id: string;
  /** Der Formularwert. */
  value: string;
  onChange: (value: string) => void;
  /**
   * Der **geladene** Wert und sein Name – nicht der Formularwert.
   *
   * Der Unterschied ist tragend (B-143, B-172): Die Sonderoption soll stehen bleiben, während der Nutzer
   * etwas anderes probiert. Leitete man sie aus `value` ab, verschwände sie beim ersten Wechsel und der
   * Weg zurück wäre zu.
   */
  loadedId: number | null | undefined;
  loadedName: string | null | undefined;
  options: readonly ReferenceOption[];
  /**
   * Beschriftung der leeren Wahl, z. B. „– keine Angabe –". **Weggelassen** für ein Pflichtfeld (B-194:
   * ein Plan hat immer ein Kind) – dann gibt es keine Leer-Option, denn sie wäre ein wählbarer Zustand,
   * den der Vertrag nicht kennt.
   */
  emptyLabel?: string;
  /**
   * Der Sentinel für „trägt einen Namen, aber kein Katalog-Objekt" – nur die Fach-Felder haben diesen
   * Zustand (`FREETEXT_SUBJECT`). Ohne ihn wird die Freitext-Option nicht angeboten.
   */
  freetextValue?: string;
  disabled?: boolean;
  /**
   * Wahr, solange die Liste **unterwegs** ist. „Die Liste kennt den Wert nicht" hat zwei Ursachen – noch
   * nicht da, oder nicht gekommen –, und nur die zweite heißt „(nicht geladen)" (B-229). Solange sie
   * unterwegs ist, heißt die Sonderoption „(lädt…)" und das Feld ist gesperrt: eine Wahl aus einer Liste,
   * die noch nicht da ist, ist keine Wahl.
   *
   * Welches Feld von `useAsync` hier hineingehört, entscheidet, ob die Liste einen Schlüssel hat: lädt sie
   * genau einmal (Kinderliste im Plan-Formular), reicht `showPlaceholder`; hängt sie an einem Schlüssel und
   * lädt bei jedem Wechsel neu (Units der gewählten Reihe), muss es `loading` sein – `showPlaceholder` ist
   * nach dem ersten Abschluss dauerhaft falsch (B-235). Weggelassen nur, wo die Liste nachweislich auf
   * Seitenebene lädt – gleichzeitig mit der Seite, in der Praxis vor dem Klick auf „Bearbeiten"; ein
   * Mechanismus, der das Formular bis dahin sperrt, ist das nicht (Nachschau 2026-10-23). „Nachweislich"
   * heißt: am `useAsync` nachgesehen, nicht gezählt; genau daran ist B-229 vorbeigegangen.
   */
  laedt?: boolean;
}

/**
 * Die Sonderoption, aus dem geladenen Stand abgeleitet. Höchstens eine – die beiden Fälle schließen sich
 * über `loadedId` gegenseitig aus, und das erzwingt die Bedingung, nicht die Reihenfolge der JSX-Zeilen.
 */
function specialOption(
  loadedId: number | null | undefined,
  loadedName: string | null | undefined,
  options: readonly ReferenceOption[],
  freetextValue: string | undefined,
  laedt: boolean,
): (ReferenceOption & { disabled: boolean; sperrtFeld: boolean }) | null {
  if (loadedId == null) {
    // Freitext: ein Zustand, den der Nutzer sehen, aber nicht herstellen darf – darum gesperrt (B-143).
    if (!loadedName || !freetextValue) return null;
    return { value: freetextValue, label: `${loadedName} (Freitext)`, disabled: true, sperrtFeld: false };
  }
  const wert = String(loadedId);
  if (options.some((o) => o.value === wert)) return null;
  const name = loadedName ?? `#${wert}`;
  // Noch unterwegs: die Liste ist nicht ausgefallen, sie ist nur noch nicht da (B-229). Die Option bleibt
  // wählbar (sie IST der Wert), gesperrt wird das Feld – bis die Liste steht, gibt es nichts zu wählen.
  if (laedt) return { value: wert, label: `${name} (lädt…)`, disabled: false, sperrtFeld: true };
  // Nicht geladen: ein völlig reguläres Katalog-Objekt, das gerade fehlt. **Nicht** gesperrt – sonst wäre
  // ein Wechsel eine Einbahnstraße, und der Weg zurück führte nur übers Zuklappen des Formulars, das
  // alle anderen Eingaben mitverwirft (B-172, Entscheidung 4).
  return { value: wert, label: `${name} (nicht geladen)`, disabled: false, sperrtFeld: false };
}

export function ReferenceSelect({
  id, value, onChange, loadedId, loadedName, options, emptyLabel, freetextValue, disabled, laedt = false,
}: ReferenceSelectProps) {
  const sonder = specialOption(loadedId, loadedName, options, freetextValue, laedt);
  return (
    <select id={id} value={value} disabled={disabled || sonder?.sperrtFeld} onChange={(e) => onChange(e.target.value)}>
      {emptyLabel !== undefined && <option value="">{emptyLabel}</option>}
      {sonder && <option value={sonder.value} disabled={sonder.disabled}>{sonder.label}</option>}
      {options.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
    </select>
  );
}
