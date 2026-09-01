/**
 * Das Fach-Feld, für alle drei Formulare, die es tragen: Lehrwerk-Reihe, Lehrbuch am Kind und
 * Fachlehrer-Profil (B-148).
 *
 * Hier steht der **PATCH-Rumpf**: was vom Fach in die Anfrage gehört, und die Zusicherung, dass der
 * Sentinel sie nie erreicht. Die hält man nicht dreimal unabhängig richtig – darum eine Stelle.
 *
 * **Die Anzeige steht nicht hier**, sondern in `components/ReferenceSelect.tsx`. Das war einmal anders
 * begründet: „von den sieben Schaltern sind sechs heil … kaputt wird es erst bei einem Feld, dessen
 * Ladezustand das Formular nicht abbilden kann, und das ist heute nur `subjectId`". Der letzte Halbsatz
 * war falsch (B-172). Ein `<select>` mit Anzeigegröße 1 wählt ohne passende Option die erste selbst –
 * fehlt die Optionsliste, zeigt das Feld also „– keine Angabe –", während der Formularzustand noch den
 * echten Wert hält. Das trifft **jede** Referenz, deren Optionen aus einem eigenen Abruf kommen: Fach,
 * Verlag, Reihe und Unit, sieben Felder. Nicht der Schalter war das Problem, sondern die Eingabe, die
 * das Formular ihm lieferte.
 */

/**
 * Der Formularwert für „diese Zeile trägt einen Fachnamen, aber kein Katalog-Fach" (B-143).
 *
 * Er ist ein **Anzeigezustand, kein Wert**: er erreicht den Server nie. Das Formular braucht ihn trotzdem,
 * weil `""` sonst zwei verschiedene Dinge hieße – „kein Fach" und „ein Fach, das der Katalog nicht kennt".
 * Genau diese Verwechslung war der Defekt, an der Reihe (B-143) wie am Lehrbuch (B-148).
 *
 * Kein numerischer Wert kann damit kollidieren – das ist eine Eigenschaft dieser Wahl, nicht des Typs, und
 * darum hält ein Testfall sie fest.
 */
export const FREETEXT_SUBJECT = "__freetext__";

/**
 * Was die drei Zeilen gemeinsam haben. Bewusst strukturell statt über die drei Response-Typen: Sie sind
 * generiert und driften unabhängig voneinander, und die Regel hier interessiert nur dieses Feldpaar.
 */
export type SubjectCarrier = {
  subjectId?: number | null;
  subjectName?: string | null;
};

/** Drei Fälle, nicht zwei: Katalog-Fach, Freitext-Fach, nichts. */
export function subjectFormValue(row: SubjectCarrier): string {
  if (row.subjectId != null) return String(row.subjectId);
  return row.subjectName ? FREETEXT_SUBJECT : "";
}

/**
 * Was vom Fach in den PATCH-Rumpf gehört. `null` heißt: nichts – das Feld ist unverändert.
 *
 * Das Fach hängt an **zwei** Spalten: `subjectId` zeigt in den Katalog, `subjectName` trägt die Zeile auch
 * ohne Katalog-Fach. Beim **Leeren** räumt der Server beide über `clearSubject` ab; beim **Wechsel** genügt
 * die Id, weil er den Namen seit B-142 selbst daraus ableitet (`TextbookSeriesController`,
 * `TextbooksController`, `CreatorProfilesController` – drei Stellen, eine Bedeutung).
 */
export type SubjectPatch = { subjectId?: number; clearSubject?: true };

export function subjectPatch(loaded: string, form: string): SubjectPatch | null {
  if (form === loaded) return null;
  // Nur der Schalter: der Controller räumt Id UND Namen. Das ist auch der Weg, auf dem ein Freitext-Fach
  // verschwindet – vom Sentinel auf „– keine Angabe –" ist ein Unterschied (B-143).
  if (form === "") return { clearSubject: true };
  // Der Sentinel geht NIE mit. Über die Oberfläche kann er hier gar nicht ankommen (die Option ist
  // `disabled`), aber „unerreichbar, weil das Formular es verhindert" ist die Art Zusicherung, die beim
  // nächsten Umbau kippt – und `Number("__freetext__")` wäre `NaN` im Rumpf.
  if (form === FREETEXT_SUBJECT) return null;
  // Nur die Id: den Anzeigenamen holt der Server aus dem Katalog (B-142). Ihn mitzuschicken wäre totes
  // Feld — und schlimmer, es sähe aus wie eine Regel, die es nicht mehr gibt.
  return { subjectId: Number(form) };
}
