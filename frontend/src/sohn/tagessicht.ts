import type { PositionStatus } from "../lib/types";

/*
 * Die Kind-Tagessicht auf der Startseite (B-606): die Pflichten aller aktiven Pläne aus `student/children/{id}/today`,
 * nach Fach gegliedert. Der Server liefert die Positionen Plan für Plan in seiner Reihenfolge; hier wird nur gruppiert,
 * nicht neu gerechnet – ob die Pflicht erfüllt und die Truhe offen ist, sagt der Server.
 */

/** Die Positionen eines Fachs, in der Reihenfolge des Servers. */
export interface FachGruppe {
  /** Fachname, wie der Server ihn meldet; leer, wenn die Übung keinem Fach zugeordnet ist. */
  fach: string;
  positions: PositionStatus[];
}

/**
 * Gruppiert die Positionen nach Fach (`subjectName`). Die Fächer stehen in der Reihenfolge ihres ersten Auftretens,
 * die Positionen darin in der Reihenfolge des Servers – zwei Pläne desselben Fachs landen in einer Gruppe, jede Position
 * behält ihre `planId` und führt damit in den Weg ihres eigenen Plans.
 */
export function nachFach(positions: readonly PositionStatus[]): FachGruppe[] {
  const gruppen = new Map<string, FachGruppe>();
  for (const pos of positions) {
    const fach = pos.subjectName ?? "";
    let gruppe = gruppen.get(fach);
    if (!gruppe) gruppen.set(fach, gruppe = { fach, positions: [] });
    gruppe.positions.push(pos);
  }
  return [...gruppen.values()];
}
