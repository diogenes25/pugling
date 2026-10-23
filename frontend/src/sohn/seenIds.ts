// Erfolgs-Erkennung: die zuletzt gesehenen erreichten IDs pro Kind in localStorage, damit ein neu
// freigeschalteter Erfolg genau einmal gefeiert wird – auch über Reloads. Beim allerersten Laden
// (Schlüssel fehlt) wird still geseedet, sonst würden Alt-Erfolge nachgefeiert.
// Geteilt von Missionen, Abzeichen und den großen Zielen (B-36), damit es das Muster nur einmal gibt.

export function isFirstLoad(key: string): boolean {
  try {
    return localStorage.getItem(key) === null;
  } catch {
    return true;
  }
}

export function readSeen(key: string): Set<number> {
  try {
    const raw = localStorage.getItem(key);
    return raw ? new Set(JSON.parse(raw) as number[]) : new Set();
  } catch {
    return new Set();
  }
}

export function writeSeen(key: string, ids: number[]): void {
  try {
    localStorage.setItem(key, JSON.stringify(ids));
  } catch {
    /* localStorage kann fehlen (Privatmodus): dann bleibt der Key leer → es wird still geseedet und
       nie gefeiert. Lieber eine ausbleibende als eine falsche Feier. */
  }
}

/**
 * Gibt die erstmals erreichten IDs zurück (beim allerersten Laden keine) und merkt sie sich **für immer**.
 *
 * Anders als bei Missionen, die sich täglich zurücksetzen und darum den Stand überschreiben: ein Ziel wird
 * nur einmal gefeiert. Der Server rechnet den Status live (eine falsche Antwort kann ein Ziel wieder auf
 * „offen" ziehen), zahlt die Belohnung aber genau einmal – eine zweite Feier „+50 🪙" verspräche Münzen,
 * die nie kommen. Und ein gerade nicht geladener Eintrag (jenseits der ersten Seite) bleibt so ebenfalls
 * gesehen, statt beim Wiederauftauchen nachgefeiert zu werden.
 */
export function takeFreshAchieved(key: string, achievedIds: number[]): number[] {
  const first = isFirstLoad(key);
  const seen = readSeen(key);
  const fresh = achievedIds.filter((id) => !seen.has(id));
  for (const id of achievedIds) seen.add(id);
  writeSeen(key, [...seen]);
  return first ? [] : fresh;
}
