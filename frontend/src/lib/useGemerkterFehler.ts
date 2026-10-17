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
 * **Bekannte Grenze (B-227):** Der Hook weiß nicht, ob dieselbe Abfrage wiederholt wird oder eine andere
 * startet – `useAsync` setzt bei einem Wechsel der Abhängigkeiten ebenfalls `error = null, loading = true`.
 * Bei einer Suche mit wechselndem Filter steht darum nach einem Fehler für die Dauer der *neuen* Suche noch
 * der alte Text. Für Abfragen ohne wechselnde Abhängigkeiten (`ListenAusfall`, Shop-Verlauf) ist das
 * gegenstandslos.
 */
export function useGemerkterFehler(fehler: string | null, laedt: boolean): string | null {
  const [letzter, setLetzter] = useState<string | null>(null);
  useEffect(() => {
    if (fehler) setLetzter(fehler);
    else if (!laedt) setLetzter(null);
  }, [fehler, laedt]);

  return fehler ?? (laedt ? letzter : null);
}
