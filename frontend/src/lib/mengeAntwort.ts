/**
 * B-712: die Mengen-Eingabe einer Gleichungs-Karte als der eine Text, den der Server schon versteht (B-710) – Zahlen
 * aufsteigend, mit `;` getrennt, die leere Menge als festes `keine Lösung`. Der Server wertet die Menge ohnehin
 * ungeordnet; sortiert wird, damit das Kind in der Auflösung dieselbe Schreibweise sieht, die es geschickt hat.
 */

/** Der feste Text der leeren Menge – Vertragswert des Servers (`NumericAnswer.NoSolution`), nie übersetzt. */
export const KEINE_LOESUNG = "keine Lösung";

/** Komma und Punkt sind beide Dezimaltrenner (B-639); nur zum Sortieren gelesen, gesendet wird der getippte Text. */
const zahl = (text: string): number => Number(text.replace(",", ".").replace("−", "-"));

/**
 * Felder und Umschalter → Antworttext. Leere Felder fallen weg, jedes Feld geht **unverändert** hinaus („2,5“ bleibt
 * „2,5“ – den Vergleich macht der Server). Nicht lesbare Zahlen stehen hinten, damit der Server sie als falsch wertet,
 * statt dass sie hier verschwinden. Alles leer und ohne Umschalter: leer – „Prüfen“ bleibt dann gesperrt.
 */
export function mengeAntwort(felder: readonly string[], keineLoesung: boolean): string {
  if (keineLoesung) return KEINE_LOESUNG;
  const werte = felder.map((f) => f.trim()).filter((f) => f !== "");
  const rang = (f: string) => (Number.isNaN(zahl(f)) ? Number.POSITIVE_INFINITY : zahl(f));
  return [...werte].sort((a, b) => rang(a) - rang(b)).join(";");
}
