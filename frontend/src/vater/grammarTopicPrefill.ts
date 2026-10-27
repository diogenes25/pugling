/**
 * Vorbelegung der Grammatik-Themen einer neuen Übung aus ihrer Unit (B-155, Entscheidung 2): **vorbelegen, nicht
 * vererben**. Beim Wechsel der Unit übernimmt die Auswahl die Themen der neuen Unit; was der Creator danach
 * abwählt oder ergänzt, bleibt stehen, bis er die Unit erneut wechselt. Eine Vererbung machte jede Übung einer
 * gemischten Unit fälschlich zu „present perfect“ – der Creator muss abwählen können, und seine Abwahl darf nicht
 * bei jedem Render zurückkommen.
 *
 * Als reine Funktion (Vorbild `seriesDerivation.ts`), damit AK 5 nicht nur im Bildschirm hängt: Welche Themen die
 * Nutzlast trägt, sieht man am Formular nicht.
 */
export function prefillGrammarTopics(args: {
  /** Die Unit, die jetzt gewählt ist (`null` = keine). */
  unitId: number | null;
  /** Die Unit, für die die aktuelle Auswahl galt (`null` = noch keine). */
  previousUnitId: number | null;
  /** Die Themen der jetzt gewählten Unit. */
  unitTopicIds: readonly number[];
  /** Die aktuelle Auswahl des Creators. */
  current: readonly number[];
}): number[] {
  const { unitId, previousUnitId, unitTopicIds, current } = args;
  // Dieselbe Unit wie zuvor: die Auswahl gehört dem Creator, auch wenn sie leer ist.
  if (unitId === previousUnitId) return [...current];
  // Unit gewechselt (oder abgewählt): die Themen der neuen Unit sind der Vorschlag, die alte Auswahl verfällt.
  return unitId === null ? [] : [...new Set(unitTopicIds)];
}
