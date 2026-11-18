import type { PositionStatus } from "../lib/types";
import { hatPflicht, istTagespflicht } from "./pfad";

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

/** Stand eines Fachs für seinen Reiter: Tages- und Wochenziele, die offenen Ziel-Münzen und ob die Pflicht geschafft ist. */
export interface FachStand {
  heute: { met: number; total: number };
  woche: { met: number; total: number };
  /** Münzen, die die noch offenen Ziele bringen (`pointsGoalMet`) – was der Reiter verspricht, zahlt der Server beim Erfüllen. */
  offeneMuenzen: number;
  /** Alle Ziele dieses Fachs mit Takt sind erfüllt (ohne Ziele: `false` – es gibt nichts zu feiern). */
  geschafft: boolean;
  /** Positionen mit Takt, in der Reihenfolge des Servers – der Pflicht-Pfad. */
  pflicht: PositionStatus[];
  /** Positionen ohne Takt – die Zusatzübungen. */
  zusatz: PositionStatus[];
}

/**
 * Der Stand eines Fachs für den aufklappbaren Reiter der Startseite: dieselbe Zählung wie „Ziele heute“ oben
 * (`istTagespflicht`, die Nacharbeit zählt zu heute), nur auf ein Fach beschränkt. Gelesen, nicht gerechnet: ob ein
 * Ziel erfüllt ist, sagt `goalMet` vom Server.
 */
export function fachStand(positions: readonly PositionStatus[]): FachStand {
  const pflicht = positions.filter(hatPflicht);
  const zaehle = (ziele: PositionStatus[]) => ({ met: ziele.filter((p) => p.goalMet).length, total: ziele.length });
  return {
    heute: zaehle(pflicht.filter((p) => istTagespflicht(p.cadence))),
    woche: zaehle(pflicht.filter((p) => p.cadence === "Weekly")),
    offeneMuenzen: pflicht.filter((p) => !p.goalMet).reduce((s, p) => s + p.pointsGoalMet, 0),
    geschafft: pflicht.length > 0 && pflicht.every((p) => p.goalMet),
    pflicht,
    zusatz: positions.filter((p) => !hatPflicht(p)),
  };
}
