import { flushSync } from "react-dom";

/**
 * B-661: die Sonderzeichen, die eine Handy-Tastatur nicht immer hat. Die Leiste bietet sie je getippter Sprache an
 * (`keyboardLang` der Karte, die Lernsprache der Reihe), damit kein Wort an einer fehlenden Taste hängt.
 *
 * Nur Kleinbuchstaben (Entscheidung 1): der Vergleich am Server ignoriert Groß/klein ohnehin. Eine Sprache ohne
 * Eintrag bekommt keine Leiste – Englisch braucht keine, Latein wird ohne Längen geschrieben.
 */
const JE_SPRACHE: Record<string, readonly string[]> = {
  fr: ["é", "è", "ê", "ë", "à", "â", "ç", "î", "ï", "ô", "û", "ù", "ü", "œ"],
  de: ["ä", "ö", "ü", "ß"],
  es: ["á", "é", "í", "ó", "ú", "ñ", "ü", "¿", "¡"],
};

/** Ein Zeichen, das eine Grundtastatur ohne Umschalten nicht hat: alles jenseits von ASCII, das ein Buchstabe ist. */
const istSonderzeichen = (z: string) => z > "\u007f" && z.toLowerCase() !== z.toUpperCase();

/**
 * Die Zeichen der Leiste. `wort` nur dort, wo das Wort ohnehin zu sehen ist (Lernkarte, Abschreiben – Entscheidung 3):
 * dann kommt jedes seiner Sonderzeichen dazu, und kein Wort bleibt unabschreibbar. Im Test und beim Tippen aus dem Kopf
 * NIE das Wort hereinreichen – die Leiste verriete, welche Akzente die Lösung trägt.
 */
export function leistenZeichen(sprache: string | null | undefined, wort?: string | null): string[] {
  const fest = (sprache && JE_SPRACHE[sprache.toLowerCase()]) || [];
  // NFC: ein zerlegt gespeichertes „î“ (i + U+0302) käme sonst als zwei Zeichen, von denen keines eine Taste ist.
  const ausDemWort = wort ? [...wort.normalize("NFC").toLowerCase()].filter(istSonderzeichen) : [];
  return [...new Set([...fest, ...ausDemWort])];
}

/** Zeichen auf und unter dem Buchstaben weg (é→e, ç→c, î→i); ß, œ und Satzzeichen bleiben. Steuert nur den Hinweis. */
export const ohneDiakritika = (s: string) => s.normalize("NFD").replace(/\p{M}/gu, "").normalize("NFC");

/**
 * Setzt `zeichen` an der Schreibmarke von `feld` ein und meldet den neuen Wert; die Marke steht danach hinter dem
 * Zeichen. Über `onWert` statt über ein `insertText`-Ereignis: so greift die Kopiersperre nicht, die mehrzeichige
 * Eingaben verwirft – ein Leistenzeichen ist kein Einfügen.
 */
export function einsetzen(feld: HTMLInputElement | null, wert: string, zeichen: string, onWert: (wert: string) => void) {
  const von = feld?.selectionStart ?? wert.length;
  const bis = feld?.selectionEnd ?? von;
  // flushSync: the new value is in the field before this function returns, so the caret can be placed right away. A
  // caret deferred to the next frame raced fast typing – keystrokes landed first, then the late caret jumped back
  // behind the inserted character and the following letters came out scrambled ("dîern", flaky E2E sonderzeichen).
  flushSync(() => onWert(wert.slice(0, von) + zeichen + wert.slice(bis)));
  const marke = von + zeichen.length;
  feld?.focus();
  feld?.setSelectionRange(marke, marke);
}
