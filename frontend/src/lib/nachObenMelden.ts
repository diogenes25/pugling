import { errorMessage } from "./api";
import type { ActionMessage, ActionState } from "./useAction";

/**
 * Eine Server-Handlung ausfuehren und ihr Ergebnis an den Meldungsplatz der **Seite** geben.
 *
 * Lag bis B-224 dateiprivat in `VaterLehrwerke.tsx`; seit auch das Verlags-Vokabular (`PublisherAdmin`) nach
 * oben meldet, ist eine Seite die falsche Heimat.
 *
 * Der Schnitt (B-212): Am Formular bleibt nur, was der Bildschirm **selbst** festgestellt hat - die
 * Eingabepruefung und "Nichts geaendert.". Alles, was vom Server zurueckkommt, reist nach oben. Der Grund
 * ist nicht Ordnungsliebe: Ein zweiter Meldungsplatz kann von niemandem geraeumt werden als von dem
 * Formular, in dem er steckt. Ein rotes "Name schon vergeben" unter Reihe A stand darum neben dem gruenen
 * "Reihe ... geloescht." der Seite, sobald der Creator an Reihe B weitermachte - zwei Meldungen zu zwei
 * Handlungen in einem Blickfeld, und keine sagt, zu welcher sie gehoert.
 *
 * Drei Dinge daran sind nicht offensichtlich:
 *
 * 1. **Der Fehlertext wird beim Durchfliegen abgegriffen**, nicht hinterher aus `action.message` gelesen:
 *    das ist Zustand und im selben Tick noch der alte.
 * 2. **`action.clear()` am Ende ist tragend.** `useAction` schreibt seinen Fehler selbst in `message`;
 *    ohne das Raeumen stuende er zusaetzlich am Formular - genau die zwei Plaetze, die hier verschwinden.
 * 3. **Ein verworfener Aufruf bekommt keine Meldung.** Die Wiedereintritts-Sperre liefert ebenfalls
 *    `null`, hat aber nichts getan; nur ein echter Fehler wird gemeldet. (Geräumt wird trotzdem — heute
 *    folgenlos, weil `runFor` den Banner beim Start des laufenden Aufrufs schon genullt hat.)
 *
 * `okText` darf eine Funktion sein: Beim Anlegen nennt die Meldung den Namen, den der **Server**
 * zurückgibt – gleicher Slug = gleiche Reihe, und die kann anders geschrieben sein als das Feld.
 *
 * Eine Annahme, die nicht im Typ steht: Der Erfolgsfall wird an `ergebnis !== null` erkannt. `runFor`
 * liefert `null` bei Fehler UND bei verworfenem Aufruf, ein leerer Antwortkörper dagegen `undefined` —
 * das trägt, solange kein Aufruf `null` als Erfolgswert liefert.
 */
export async function nachObenMelden<T>(
  action: ActionState,
  onMeldung: (m: ActionMessage | null) => void,
  fn: () => Promise<T>,
  okText: string | ((ergebnis: T) => string),
): Promise<T | null> {
  /*
   * `try/catch` um den **Aufruf**, nicht `fn().catch(...)`: Ein synchroner Wurf aus `fn()` erzeugt gar
   * kein Promise, das abgelehnt werden könnte — der Fehlertext bliebe leer, `onMeldung` schwiege, und
   * `action.clear()` räumte darunter noch die Meldung weg, die `useAction` selbst gesetzt hat. Ergebnis
   * wäre vollständiges Schweigen: genau die Fehlerklasse, gegen die dieser Sprint angetreten ist.
   */
  let gefangen = false;
  let text = "";
  const ergebnis = await action.runFor(async () => {
    try {
      return await fn();
    } catch (e: unknown) {
      // Beim Durchfliegen abgreifen, nicht hinterher aus `action.message` lesen: das ist Zustand und im
      // selben Tick noch der alte.
      gefangen = true;
      text = errorMessage(e);
      throw e;
    }
  });
  if (ergebnis === null) {
    // `gefangen` und nicht `text`: Eine leere Fehlermeldung ist immer noch ein Fehler, während ein von
    // der Wiedereintritts-Sperre verworfener Aufruf gar nicht erst in den `catch` gelaufen ist.
    if (gefangen) onMeldung({ ok: false, text });
    action.clear();
    return null;
  }
  onMeldung({ ok: true, text: typeof okText === "function" ? okText(ergebnis) : okText });
  action.clear();
  return ergebnis;
}
