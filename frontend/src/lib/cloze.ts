/*
 * Die Platzhalter-Syntax des Lückentexts: `{{1}}` im Text gehört zur Lücke mit `index` 1.
 *
 * Sie wird an zwei Enden gebraucht, und darum liegt sie hier statt in einer der beiden Ansichten: Der
 * Vater-Editor führt die Lücken-Zeilen am Text nach, und die Sohn-Ansicht muss zeigen, welche Lücke gerade
 * gefragt ist. Vorher stand der Parser nur im Editor – der Sohn bekam die rohen `{{n}}` zu sehen.
 */

/** Die Platzhalter-Nummern in Reihenfolge ihres Auftretens; Dubletten zählen einmal. */
export function placeholderIndices(text: string): number[] {
  const found = [...text.matchAll(/\{\{(\d+)\}\}/g)].map((m) => Number(m[1]));
  return [...new Set(found)];
}

/** Ein Textstück beim Zerlegen: entweder gewöhnlicher Text oder eine Lücke mit ihrer Nummer. */
export type ClozePart = { text: string } | { gap: number };

/**
 * Zerlegt den Text in Stücke und Lücken, damit die Ansicht die gefragte hervorheben kann.
 *
 * Findet sich kein einziger Platzhalter, kommt der Text **unverändert** als ein Stück zurück. Das ist
 * Absicht und kein Sonderfall zum Wegoptimieren: Der Editor prüft beim Anlegen, dass Text und Lücken
 * zusammenpassen (`gapProblem`), für längst gespeicherte Übungen gilt das aber nicht. Lieber ein
 * schmuckloser Text als eine leere Karte.
 */
export function clozeParts(text: string): ClozePart[] {
  const parts: ClozePart[] = [];
  let last = 0;
  for (const match of text.matchAll(/\{\{(\d+)\}\}/g)) {
    if (match.index > last) parts.push({ text: text.slice(last, match.index) });
    parts.push({ gap: Number(match[1]) });
    last = match.index + match[0].length;
  }
  if (last < text.length) parts.push({ text: text.slice(last) });
  return parts;
}
