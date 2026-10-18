import { useEffect, useState } from "react";

/**
 * Der letzte Fehler eines Abrufs, der den **Start seiner Wiederholung überlebt**.
 *
 * `useAsync.reload()` setzt `error` auf `null` und `loading` auf `true` im selben Schritt – und der
 * Hand-Lader des Shop-Verlaufs tut dasselbe. Ein Banner, das allein an `fehler` hängt, verschwindet damit
 * genau in dem Moment, in dem der Nutzer den Wiederhol-Knopf darin drückt: Der Knopf ist unter dem Finger
 * weg, der Fokus fällt auf `<body>`, und während der Wiederholung steht nichts da. Scheitert sie erneut,
 * blitzt das Banner nur wieder auf; dauert sie lange, sieht es aus, als hätte es geklappt.
 *
 * Die Regel stand vorher nur in `ListenAusfall` (B-188) und fehlte an drei weiteren Stellen (B-216). Sie
 * ist hier ein Hook und kein Baustein, weil die vier Flächen verschieden aussehen (Satzanfang, Zählzeile,
 * Knopf unter statt neben der Meldung) und nur die Regel teilen. **Nicht** in `useAsync` – ob das
 * Gedächtnis ein querschnittliches Feld des Hooks sein soll, ist die offene Architekturfrage von B-164
 * und liegt beim Nutzer; dieser Hook greift ihr nicht vor, weil er nur dort wirkt, wo er aufgerufen wird.
 *
 * Die Rückgabe ist das, was angezeigt gehört: der aktuelle Fehler, sonst der festgehaltene, solange
 * geladen wird, sonst `null`. Erst dadurch ist der Zustand „Fehler steht, Abfrage läuft" erreichbar – und
 * ein `disabled={laedt}` am Knopf kein toter Code mehr.
 *
 * **`schluessel` sagt, wofür das Gedächtnis gilt (B-227).** `useAsync` erzeugt denselben Übergang
 * `error = null, loading = true` auch bei einem Wechsel der Abhängigkeiten – und eine *andere* Abfrage ist
 * keine Wiederholung: Nach einer gescheiterten Suche gehört beim Filterwechsel der alte Fehler zu einer
 * Abfrage, die es nicht mehr gibt, und „Suche läuft…" ist die Wahrheit. Der Aufrufer reicht darum den
 * Schlüssel der laufenden Abfrage (im Trefferbereich und im Planbau `filterKey`, der dort schon
 * `zeilenVeraltet` trägt); ein gemerkter Fehler zählt nur unter dem Schlüssel, unter dem er auftrat.
 * Abfragen ohne wechselnde Abhängigkeiten (Shop-Verlauf, die `ListenAusfall`-Stellen) lassen ihn weg –
 * `undefined` ist dann immer gleich `undefined`. Eine Ausnahme ist nur scheinbar eine: Die Arten-Liste des
 * Assistenten hängt am Fach, aber ihr `ListenAusfall` lebt in Schritt 3 und das Fach-Feld in Schritt 2 –
 * der Baustein ist abgebaut, bevor sich das Fach ändern kann (Reviewer-Hinweis, Nachtlauf 2026-10-17).
 * Wandert das Fach-Feld je nach Schritt 3, braucht die Stelle den Schlüssel. Verglichen wird am
 * Rückgabepunkt, nicht per Effekt-Reset: ein Effekt liefe erst **nach** dem Render, und ein Render lang
 * stünde der alte Text noch da.
 */
export function useGemerkterFehler(fehler: string | null, laedt: boolean, schluessel?: unknown): string | null {
  const [letzter, setLetzter] = useState<{ fehler: string; schluessel: unknown } | null>(null);
  useEffect(() => {
    if (fehler) setLetzter({ fehler, schluessel });
    else if (!laedt) setLetzter(null);
    // `schluessel` ist bewusst KEINE Abhängigkeit (im E2E gemessen, Nachtlauf 2026-10-17): Ein Filterwechsel
    // rendert zuerst mit dem ALTEN Fehler und dem NEUEN Schlüssel – `useAsync` räumt `error` erst in seinem
    // Effekt. Liefe dieser Effekt in jenem Render, merkte er den alten Fehler unter dem neuen Schlüssel, und
    // der Vergleich unten hielte ihn für den Fehler der neuen Suche. Gemerkt wird darum nur, wenn `fehler`
    // selbst erscheint – mit dem Schlüssel, der in diesem Moment gilt.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [fehler, laedt]);

  if (fehler) return fehler;
  return laedt && letzter !== null && letzter.schluessel === schluessel ? letzter.fehler : null;
}
