import type { PracticeCard } from "../lib/types";

/*
 * Die Form einer Aufgabe (B-526), abgeleitet aus den Feldern der Karte: welche Arbeitsanweisung darübersteht, ob
 * die Frage in der Sprechblase oder als volle Karte erscheint, und wie geantwortet wird. Der Server entscheidet,
 * was eine Karte mitbringt; hier wird nur gelesen. Bewusst an den Feldern und nicht am Übungstyp oder an der
 * Stufennummer – dieselbe Zahl bedeutet je Typ etwas anderes (B-524), und eine neue Antwortform soll hier an
 * genau einer Stelle nachgezogen werden.
 */

/** Die Felder, an denen sich die Form entscheidet. Übungs- und Testkarte tragen sie beide. */
export type FormKarte = Pick<PracticeCard, "reveal" | "choices" | "letterTiles" | "answerLength" | "audioUrl" | "prompt"
  | "passage" | "decoding" | "cueBox"> & { displayOnly?: boolean; introduction?: boolean; imageUrl?: string | null }
  & Partial<Pick<PracticeCard, "dictationSpokenAnswer">>;

export type Antwortart = "auswahl" | "ordnen" | "kaestchen" | "freitext" | "selbst" | "kennenlernen";

/** Schlüssel der Arbeitsanweisung unter `lektion.*`. */
export type AnweisungKey =
  | "lektion.choose" | "lektion.order" | "lektion.boxes" | "lektion.cueBox" | "lektion.listen" | "lektion.write"
  | "lektion.self" | "lektion.intro" | "lektion.look";

export interface Aufgabenform {
  antwort: Antwortart;
  anweisung: AnweisungKey;
  /** Sprechblase am Maskottchen für Wort oder Satz; volle Karte, sobald mehr Material dabei ist. */
  darstellung: "blase" | "karte";
  /**
   * B-597: Das Gerät liest das Fremdwort vor, die Karte zeigt keinen Text. Nur wo der Server ein Diktat ohne Aufnahme
   * schickt UND das Gerät eine Stimme für die Sprache hat – sonst bleibt es Freitext mit dem deutschen Wort.
   */
  diktat: boolean;
}

const hat = (liste: readonly unknown[] | null | undefined) => (liste?.length ?? 0) > 0;

/**
 * Wie geantwortet wird. Die Reihenfolge ist die der Antwortfläche: eine Lösung, die mitkommt, heißt „nicht
 * getippt“ (offen = Kennenlernen, sonst Selbsteinschätzung); unter den getippten gewinnt, was die Karte am
 * genauesten vorgibt.
 */
function antwortart(karte: FormKarte): Antwortart {
  if (karte.reveal != null) return karte.displayOnly ? "kennenlernen" : "selbst";
  if (karte.choices) return "auswahl";
  if (karte.letterTiles) return "ordnen";
  if (karte.answerLength) return "kaestchen";
  return "freitext";
}

function anweisung(karte: FormKarte, antwort: Antwortart): AnweisungKey {
  switch (antwort) {
    // „Neu“ nur, wo der Server die Karte als Erstkontakt meldet: eine Anzeigestufe des Tages zeigt auch Bekanntes.
    case "kennenlernen": return karte.introduction === false ? "lektion.look" : "lektion.intro";
    case "selbst": return "lektion.self";
    case "auswahl": return "lektion.choose";
    case "ordnen": return "lektion.order";
    default:
      // Der Wortkasten und die reine Höraufgabe sagen mehr über die Aufgabe als die Form des Eingabefelds.
      if (hat(karte.cueBox)) return "lektion.cueBox";
      if (karte.audioUrl && !karte.prompt) return "lektion.listen";
      return antwort === "kaestchen" ? "lektion.boxes" : "lektion.write";
  }
}

/**
 * B-597: Wird diese Karte diktiert? Nur wo der Server ein Diktat ohne Aufnahme schickt und das Gerät eine Stimme für die
 * Sprache hat. Eine Stelle für Karte und Ansage, damit der Screenreader nicht das Wort vorliest, das die Karte verbirgt.
 * Die Stimmen lädt Chrome oft erst nach dem ersten Blick nach: bis dahin steht die erste Diktatkarte kurz als Freitext mit
 * dem deutschen Wort da – keine Lösung, nur die Übersetzungshilfe, und ab der zweiten Karte nicht mehr.
 */
export function istDiktat(karte: Partial<Pick<PracticeCard, "dictationSpokenAnswer">>, stimmeFuerDiktat: boolean): boolean {
  return stimmeFuerDiktat && !!karte.dictationSpokenAnswer;
}

/** `stimmeFuerDiktat` ist Pflicht: ein vergessenes Argument machte jedes Diktat still zur Übersetzungsaufgabe. */
export function aufgabenform(karte: FormKarte, stimmeFuerDiktat: boolean): Aufgabenform {
  const antwort = antwortart(karte);
  const diktat = istDiktat(karte, stimmeFuerDiktat);
  // Eine Aufnahme MIT Frage bringt Bedienelemente mit (anhalten, zurückspulen) – die passen nicht in die Blase.
  const lang = !!karte.passage || !!karte.imageUrl || hat(karte.cueBox) || hat(karte.decoding)
    || (!!karte.audioUrl && !!karte.prompt);
  return { antwort, anweisung: diktat ? "lektion.listen" : anweisung(karte, antwort), darstellung: lang ? "karte" : "blase", diktat };
}
