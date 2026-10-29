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

/**
 * Ob ein Lückentext mit gezeichneten Lücken erscheint – oder als unveränderter Text. Rückfall, wenn es keine Lücke gibt
 * oder die gefragte im Text nicht vorkommt (umnummerierter Altbestand). Eine Regel für Anzeige (`ClozePrompt`) und
 * Ansage (`TaskAnnouncer`), damit beide nie verschieden reden (B-286).
 */
export function showsGaps(parts: ClozePart[], gapIndex: number | null | undefined): boolean {
  const gaps = parts.filter((p) => "gap" in p);
  if (gaps.length === 0) return false;
  return gapIndex == null || gaps.some((p) => "gap" in p && p.gap === gapIndex);
}
