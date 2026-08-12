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

/**
 * Ein Vergleichsschlüssel über die **Suchkriterien** (B-161).
 *
 * Er existiert, weil die Auswahl beim Wechsel der Kriterien fallen muss und ein Objektvergleich per Referenz
 * bei *jedem* Rendern anders ausfiele. Bewusst über `wizardSearchParams` gebildet und nicht über die
 * Rohfelder: dann zählt genau das als Änderung, was der Server auch anders beantwortet — ein Leerschlag am
 * Ende der Inhaltssuche (`"abc "` → `"abc"`) verwirft die Auswahl also nicht.
 *
 * `take` gehört ausdrücklich **nicht** dazu: eine größere Seite ist dieselbe Suche.
 */
export function wizardFilterKey(f: WizardFilter): string {
  const { take: _ignoriert, ...kriterien } = wizardSearchParams(f);
  return JSON.stringify(kriterien);
}

/**
 * Wie viele der gewählten Übungen stehen **nicht** in der gezeigten Liste (B-161).
 *
 * „Alle wählen" darf bis zu 500 Treffer wählen, gerendert wird aber nur die geladene Seite — die Differenz
 * ist für den Vater sonst unsichtbar, und `toggle` gibt es nur an einer gerenderten Zeile. Die Zahl macht
 * aus „(500 gewählt)" neben „5 passende Übungen" einen lesbaren Zustand statt eines Widerspruchs.
 */
export function unsichtbareAuswahl(selected: readonly number[], geladeneIds: readonly number[]): number {
  const geladen = new Set(geladeneIds);
  return selected.filter((id) => !geladen.has(id)).length;
}

/**
 * Was mit der Auswahl geschieht, wenn sich die Suchkriterien geändert haben (B-161).
 *
 * Die Regel liegt hier und nicht als `if` im Effekt, weil sie den **Defekt** dieser Story trägt: dass eine
 * Auswahl den Filterwechsel überlebte. Als Funktion ist sie rot zu bekommen; als Zeile im Bildschirm hing
 * sie nur am Rollengang.
 *
 * `null` heißt „nichts tun" — der Schlüssel ist derselbe. Ein Hinweis entsteht **nur**, wenn wirklich etwas
 * wegfiel: sonst begrüßte der Assistent den Vater mit „Auswahl zurückgesetzt", bevor er etwas getan hat
 * (und stumm zu leeren wäre die andere Falle, B-116).
 */
export function auswahlNachFilterwechsel(vorher: readonly number[], keyAlt: string, keyNeu: string):
{ selected: number[]; hinweis: string | null } | null {
  if (keyAlt === keyNeu) return null;
  return {
    selected: [],
    // Gebeugt, wie an den vergleichbaren Stellen des Hauses („Plan/Pläne", „Kind/Kindern"): „1 Übungen"
    // liest sich wie ein Platzhalter, den jemand vergessen hat.
    hinweis: vorher.length > 0
      ? `Auswahl zurückgesetzt (${vorher.length} ${vorher.length === 1 ? "Übung" : "Übungen"}),`
        + " weil sich die Suche geändert hat."
      : null,
  };
}
