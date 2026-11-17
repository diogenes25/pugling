/** Ein Stück der Lösung; `falsch` markiert die Buchstaben, an denen sich die eigene Antwort verschrieben hat. */
export interface LoesungsStueck {
  text: string;
  falsch: boolean;
}

/**
 * Markiert in der Lösung, wo die eigene Antwort sich verschrieben hat – aber nur bei einem **einfachen** Verschreiben:
 * höchstens zwei Buchstaben vertauscht, vergessen oder zu viel, und nicht mehr als die Hälfte des Worts. Sonst `null`,
 * denn bei einem ganz anderen Wort wäre jede Markierung Rauschen.
 *
 * Verglichen wird ohne Groß-/Kleinschreibung (die zählt der Server nicht); Akzente zählen, denn ein `e` statt `é` ist
 * genau der Fehler, den das Kind sehen soll. Ein überzähliger Buchstabe der Antwort markiert den Nachbarn in der Lösung,
 * an dem er stand – sonst bliebe ein „wett“ statt „wet“ ohne jede Markierung.
 */
export function markiereSchreibfehler(gegeben: string, loesung: string): LoesungsStueck[] | null {
  const a = [...gegeben.trim().toLowerCase()];
  const l = [...loesung];
  const b = l.map((c) => c.toLowerCase());
  if (a.length === 0 || b.length === 0) return null;

  // Levenshtein-Tabelle: d[i][j] = Abstand zwischen a[0..i) und b[0..j).
  const d = Array.from({ length: a.length + 1 }, (_, i) => Array.from({ length: b.length + 1 }, (_, j) => (i === 0 ? j : j === 0 ? i : 0)));
  for (let i = 1; i <= a.length; i++)
    for (let j = 1; j <= b.length; j++)
      d[i][j] = Math.min(d[i - 1][j] + 1, d[i][j - 1] + 1, d[i - 1][j - 1] + (a[i - 1] === b[j - 1] ? 0 : 1));

  const abstand = d[a.length][b.length];
  if (abstand === 0 || abstand > 2 || abstand * 2 > b.length) return null;

  // Rückweg: welche Lösungsbuchstaben falsch, fehlend oder neben einem überzähligen Buchstaben stehen.
  const falsch = new Array<boolean>(b.length).fill(false);
  let i = a.length, j = b.length;
  while (i > 0 || j > 0) {
    if (i > 0 && j > 0 && d[i][j] === d[i - 1][j - 1] + (a[i - 1] === b[j - 1] ? 0 : 1)) {
      if (a[i - 1] !== b[j - 1]) falsch[j - 1] = true;
      i--; j--;
    } else if (j > 0 && d[i][j] === d[i][j - 1] + 1) {
      falsch[j - 1] = true; // in der Antwort vergessen
      j--;
    } else {
      falsch[Math.min(j, b.length - 1)] = true; // in der Antwort zu viel: der Nachbar in der Lösung
      i--;
    }
  }

  const stuecke: LoesungsStueck[] = [];
  l.forEach((c, k) => {
    const letztes = stuecke[stuecke.length - 1];
    if (letztes && letztes.falsch === falsch[k]) letztes.text += c;
    else stuecke.push({ text: c, falsch: falsch[k] });
  });
  return stuecke;
}
