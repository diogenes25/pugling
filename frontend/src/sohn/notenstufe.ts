import type { TFunction } from "i18next";

/**
 * Der Satz zur Notenstufe eines Fachs auf der Fachkarte (B-743). Eine reine Funktion, weil die Regel eine Produktregel
 * ist, die im JSX leicht rutscht (B-682 Entscheidung 3, B-683 Entscheidung 3):
 *
 * - Stufe 1 bis 3 heißen „Einser-“, „Zweier-“, „Dreier-Stand“ – die Note steht nur dort, wo sie positiv wirkt.
 * - Stufe 4 bis 6 tragen **kein** Stufenwort, nur das nächste Ziel („noch N Wörter sicher bis zur nächsten Stufe“).
 * - Ein Abstieg sieht genauso aus wie jede andere Stufe: neue Stufe, Weg zurück – nie „abgestiegen“. Darum nimmt die
 *   Funktion die vorige Stufe gar nicht erst an.
 *
 * `wordsToNext` kommt vom Server (live gezählt); `0` heißt „erreicht, die Stufe folgt mit dem nächsten Tag“, `null`
 * steht auf der besten Stufe.
 */
/** Die drei benannten Stufen; jede weitere hat bewusst keinen Schlüssel – es gibt kein „Vierer-Stand“ zu übersetzen. */
const STAND = ["home.levelStand1", "home.levelStand2", "home.levelStand3"] as const;

export function notenstufeText(t: TFunction, level: number, wordsToNext: number | null | undefined): string {
  const ziel = wordsToNext == null
    ? null
    : wordsToNext === 0
      ? t("home.levelReached")
      : t("home.levelGoal", { count: wordsToNext });
  if (level >= 1 && level <= 3) {
    const stand = t(STAND[level - 1]);
    return ziel ? `${stand} · ${ziel}` : stand;
  }
  return ziel ?? t("home.levelKeepGoing");
}
