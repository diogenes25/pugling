/** One word pair read from a pasted list. */
export interface ParsedPair {
  word: string;
  translation: string;
  /** Further columns of the line: equally valid translations. */
  alternatives: string[];
}

export interface ParsedVocabList {
  pairs: ParsedPair[];
  /** Lines that carry text but no separator - shown to the user instead of being dropped silently. */
  rejected: string[];
}

/*
 * Separators in order of trust. A tab or a semicolon is what a spreadsheet or an export produces and never occurs
 * inside a word; the spaced dash and " = " are how people type a list by hand. A comma is deliberately no separator:
 * "Abfall, Müll" is one translation in the store, and French phrases carry commas of their own.
 */
const SEPARATORS: RegExp[] = [/\t+/, /\s*;\s*/, /\s+[–—-]\s+/, /\s+=\s+/];

function splitLine(line: string): string[] | null {
  for (const sep of SEPARATORS) {
    const parts = line.split(sep).map((p) => p.trim()).filter((p) => p.length > 0);
    if (parts.length >= 2) return parts;
  }
  return null;
}

/**
 * Reads a pasted vocabulary list, one pair per line ("la ville ; die Stadt"). A pair that repeats an earlier one
 * (same word and translation, case ignored) is kept once: a list copied from two pages overlaps, and the store would
 * answer the second with "exists" anyway.
 */
export function parseVocabList(text: string): ParsedVocabList {
  const pairs: ParsedPair[] = [];
  const rejected: string[] = [];
  const seen = new Set<string>();
  for (const raw of text.split(/\r?\n/)) {
    const line = raw.trim();
    if (line.length === 0) continue;
    const parts = splitLine(line);
    if (!parts) { rejected.push(line); continue; }
    const [word, translation, ...alternatives] = parts;
    const key = `${word.toLowerCase()}\u0000${translation.toLowerCase()}`;
    if (seen.has(key)) continue;
    seen.add(key);
    pairs.push({ word, translation, alternatives });
  }
  return { pairs, rejected };
}
