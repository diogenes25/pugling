import { describe, expect, it } from "vitest";
import type { PositionStatus } from "../lib/types";
import { nachFach } from "./tagessicht";

const position = (over: Partial<PositionStatus>): PositionStatus => ({
  positionId: 1, exerciseId: 1, exerciseTitle: "Wörter", exerciseType: "Vocabulary", renderer: "flashcards",
  order: 1, cadence: "Daily", checkMode: "PositionTest", useLeitner: true, testable: true, goalMet: false,
  dueCount: 0, poolSize: 10, pointsGoalMet: 20, introducedCount: 0, penaltyCoins: 0,
  seriesUnitId: 1, seriesUnitLabel: "Unit 1", planId: 1, planTitle: "Plan", subjectName: "Englisch", ...over,
} as PositionStatus);

/*
 * B-606 AK 2 – die Startseite gliedert die Pflichten aller aktiven Pläne nach Fach. Je Fall steht, welche Änderung ihn
 * rot macht.
 */
describe("nachFach", () => {
  it("legt die Positionen zweier Pläne nach ihrem Fach ab und behält jede Plan-Id", () => {
    // Rot, wenn nach Plan statt nach Fach gruppiert wird (drei Gruppen statt zwei, Englisch zerfiele in Plan 5 und 7).
    const gruppen = nachFach([
      position({ positionId: 1, planId: 5, subjectName: "Englisch" }),
      position({ positionId: 2, planId: 6, subjectName: "Französisch" }),
      position({ positionId: 3, planId: 7, subjectName: "Englisch" }),
    ]);
    expect(gruppen.map((g) => g.fach)).toEqual(["Englisch", "Französisch"]);
    expect(gruppen[0].positions.map((p) => [p.positionId, p.planId])).toEqual([[1, 5], [3, 7]]);
    expect(gruppen[1].positions.map((p) => [p.positionId, p.planId])).toEqual([[2, 6]]);
  });

  it("hält die Reihenfolge des Servers – die Fächer nach ihrem ersten Auftreten, nicht alphabetisch", () => {
    // Rot, wenn die Gruppen sortiert werden (Französisch vor Mathe).
    const gruppen = nachFach([
      position({ positionId: 1, subjectName: "Mathe" }),
      position({ positionId: 2, subjectName: "Französisch" }),
    ]);
    expect(gruppen.map((g) => g.fach)).toEqual(["Mathe", "Französisch"]);
  });

  it("sammelt Übungen ohne Fach in einer eigenen Gruppe mit leerem Namen", () => {
    // Rot, wenn eine Position ohne Fach verloren geht oder einem anderen Fach zugeschlagen wird.
    const gruppen = nachFach([
      position({ positionId: 1, subjectName: "Englisch" }),
      position({ positionId: 2, subjectName: "" }),
    ]);
    expect(gruppen).toHaveLength(2);
    expect(gruppen[1]).toEqual({ fach: "", positions: [expect.objectContaining({ positionId: 2 })] });
  });
});
