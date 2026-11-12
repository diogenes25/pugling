/**
 * Ob eine Auswahl als Wortbank (Chips nebeneinander) erscheint statt als Multiple-Choice (vollbreite Knöpfe).
 *
 * Entschieden an der **Menge**, nicht am Übungstyp: Multiple-Choice schickt eine Handvoll Optionen, die lang sein
 * dürfen und am Daumen breite Ziele verdienen; eine Wortbank zehn bis zwanzig kurze Wörter, die vollbreit
 * untereinander mehrere Bildschirme füllen (B-489). Die Grenze liegt über der üblichen MC-Größe von vier.
 */
export const WORD_BANK_MIN = 7;

export function isWordBank(choices: readonly string[] | null | undefined): boolean {
  return (choices?.length ?? 0) >= WORD_BANK_MIN;
}

/** Die Klasse der Auswahl-Gruppe: `row` immer, `choices-bank` nur bei einer Wortbank. */
export function choicesClass(choices: readonly string[] | null | undefined): string {
  return isWordBank(choices) ? "row choices-bank" : "row";
}
