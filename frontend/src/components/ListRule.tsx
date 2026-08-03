import type { ExerciseTypeKey } from "../lib/uiTypes";

/**
 * Die Spielregel einer Listen-Karte – eine Zeile unter dem Aufgabentext (B-77).
 *
 * Alle Karten einer Liste tragen dieselbe Anweisung („Nenne alle 16 Bundesländer."), und das ist richtig: es
 * ist eine Aufgabe. Welche **Antwort** gerade zählt, unterscheidet die Karten aber, und das steht in der
 * Übungs-Konfiguration (`ordered`), die kein Kind sieht. Der Server sagt darum je Karte, welche Regel gilt;
 * formuliert wird sie hier – ein fertiger Satz vom Server wäre deutsche Produktsprache im Backend und stünde
 * der Mehrsprachigkeit im Weg (dieselbe Abwägung wie bei `ClozePrompt`/`gapIndex`).
 *
 * Zwei Fälle, zwei Gründe:
 *
 * - `anyOrder` – die Liste ist eine **Menge**: jede noch nicht genannte Antwort zählt. Die Zeile hängt allein
 *   an diesem Feld, nicht am Typ: sie ist für jeden mengenweise bewerteten Typ richtig.
 * - Sonst ist die Liste eine **Folge**, und die Karte fragt einen bestimmten Platz. Dieser Fall hängt am Typ,
 *   denn nur bei einer Liste teilen sich die Karten ihren Text – eine Vokabelkarte braucht kein „Eintrag 8".
 */
// Der Schlüssel aus dem Typ-Manifest, getippt: ein Tippfehler („list") machte die Komponente stumm, und
// stumm heißt hier „das Kind sieht nicht, welchen Eintrag es nennen soll" – nichts, was auffällt.
const LIST_KEY: ExerciseTypeKey = "List";

export function ListRule({ type, anyOrder, itemIndex }: {
  type?: string | null;
  anyOrder?: boolean | null;
  itemIndex: number;
}) {
  // „den du noch nicht genannt hast", nicht „der noch nicht dran war": bestraft wird nach E4 die eigene
  // Wiederholung, nicht eine Kartenreihenfolge – und genau das muss das Kind lesen.
  if (anyOrder) {
    return <p className="sub list-rule">Nenne einen Eintrag, den du noch nicht genannt hast.</p>;
  }
  if (type !== LIST_KEY) return null;
  // Die Position ist 1-basiert für das Kind – und sie kommt aus `itemIndex`, nicht aus dem Fragezähler: die
  // Prüfungsreihenfolge ist serverseitig eingefroren („schwächste zuerst"), „Frage 3" ist also nicht „Eintrag 3".
  // Ausgezeichnet wie die gefragte Lücke beim Lückentext, denn hier ist es das einzige Unterscheidungsmerkmal.
  return <p className="sub list-rule position">Eintrag {itemIndex + 1}</p>;
}
