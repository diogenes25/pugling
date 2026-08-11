import type { ExerciseSearchParams, SchoolType } from "../lib/types";

/**
 * Die Filter des Assistenten-Schritts „Übungen" als **reine Regel** (B-18).
 *
 * Sie liegt hier statt im Bildschirm, weil sie an **zwei** Stellen gebraucht wird — bei jeder Eingabe für
 * die Trefferliste und noch einmal beim „Alle wählen"-Nachschlag mit höherem `take`. Zwei Kopien derselben
 * Abbildung wären genau die Konstellation, in der eine von beiden einen später ergänzten Filter vergisst:
 * Die Liste zeigte dann etwas anderes an, als der Knopf übernimmt, und niemand sähe es.
 *
 * Prüfbar ist sie nur hier: Welche Query der Server bekommt, sieht man am Bildschirm nicht.
 */

/** Der Formularzustand des Schritts, so wie die Eingabefelder ihn tragen. */
export type WizardFilter = {
  subjectId: number;
  grade?: number;
  schoolType?: SchoolType;
  search: string;
  categoryId: number | "";
  type: string;
  source: string;
};

/**
 * Nur gesetzte Filter reisen mit. Leer heißt **„alle"**, nicht „leerer Wert": Ein `categoryId=` oder
 * `type=` in der Query wäre für den Server ein Filter auf nichts, und die Trefferliste bliebe ohne
 * sichtbaren Grund leer.
 */
export function wizardSearchParams(f: WizardFilter, take?: number): ExerciseSearchParams {
  return {
    subjectId: f.subjectId,
    grade: f.grade,
    schoolType: f.schoolType,
    search: f.search.trim() || undefined,
    categoryId: f.categoryId === "" ? undefined : f.categoryId,
    type: f.type || undefined,
    source: f.source.trim() || undefined,
    take,
  };
}
