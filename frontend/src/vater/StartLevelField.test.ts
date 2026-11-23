import { describe, expect, it } from "vitest";
import type { ExerciseSummary } from "../lib/types";
import { aufbauStufeText, startLevelOptions } from "./StartLevelField";
import { cadencesFor, withRemediation, withStaged } from "./PlanPositions";

const ex = (id: number, buildLevel: number | null = null, buildTargetExerciseId: number | null = null) =>
  ({ id, seriesUnitId: 1, buildLevel, buildTargetExerciseId } as unknown as ExerciseSummary);

/* B-841 (B-704 AK 3, 4): das Feld „Start-Stufe“ nur bei einer Übung mit Vorstufen, mit den Stufen, die der Server annimmt. */
describe("startLevelOptions", () => {
  // Rot, wenn das Feld auch ohne Vorstufen Optionen hätte (es stünde dann immer da).
  it("hat ohne Vorstufen keine Optionen", () => {
    expect(startLevelOptions([ex(5), ex(6, 1, 9)], 5)).toEqual([]);
  });

  // Rot, wenn eine Stufe ohne Vorstufe angeboten würde (der Server lehnte sie ab) oder die Vorstufe einer anderen
  // Übung zählte.
  it("bietet ohne Weg, die Stufen der eigenen Vorstufen und das Ziel an", () => {
    expect(startLevelOptions([ex(5), ex(6, 1, 5), ex(7, 1, 5), ex(8, 2, 9)], 5).map((o) => o.value)).toEqual(["", 1, 3]);
  });

  // Rot, wenn das Feld die Katalog-Stufe zeigt statt des Platzes auf dem Weg, den Zeile und Band nennen: mit nur einer
  // Vorstufe auf Stufe 2 ist sie „Stufe 1 von 2“, das Ziel „Stufe 2 von 2“.
  it("beschriftet nach dem Platz auf dem Weg", () => {
    expect(startLevelOptions([ex(6, 2, 5)], 5).map((o) => [o.value, o.label])).toEqual([
      ["", "ohne Aufbau-Weg (Ziel-Übung direkt)"],
      [2, "Stufe 1 von 2 · einsetzen mit Auswahl"],
      [3, "Stufe 2 von 2 · Ziel-Übung"],
    ]);
  });
});

/* B-841 (B-704 AK 6): eine gestufte Position bekommt bis B-702 kein Tages- oder Wochenziel. */
describe("Rhythmus einer gestuften Position", () => {
  // Rot, wenn die Auswahl Tages- oder Wochenziel zulässt.
  it("bietet nur frei und – mit Nacharbeit – bis erledigt an", () => {
    expect(cadencesFor(false, true)).toEqual(["None"]);
    expect(cadencesFor(true, true)).toEqual(["UntilDone", "None"]);
    expect(cadencesFor(false)).toEqual(["Daily", "Weekly", "None"]);
  });

  // Rot, wenn das Setzen der Start-Stufe ein gewähltes Tagesziel stehen ließe (der Server antwortete validation_error).
  it("fällt beim Setzen der Stufe vom Tagesziel auf frei", () => {
    const s = { cadence: "Daily", remediation: false } as Parameters<typeof withStaged>[0];
    expect(withStaged(s, true, null)).toEqual({ settings: { ...s, cadence: "None" }, verdraengt: "Daily" });
    expect(withStaged({ ...s, cadence: "UntilDone", remediation: true }, true, null).settings.cadence).toBe("UntilDone");
  });

  // Rot, wenn das verdrängte Ziel beim Wegfall des Wegs nicht zurückkommt (still eine Position ohne Pflicht) – oder
  // wenn es einen Rhythmus überschreibt, den der Vater inzwischen selbst gesetzt hat.
  it("gibt das verdrängte Ziel zurück, wenn der Weg wegfällt", () => {
    const frei = { cadence: "None", remediation: false } as Parameters<typeof withStaged>[0];
    expect(withStaged(frei, false, "Weekly")).toEqual({ settings: { ...frei, cadence: "Weekly" }, verdraengt: null });
    expect(withStaged({ ...frei, cadence: "Daily" }, false, "Weekly").settings.cadence).toBe("Daily");
    expect(withStaged(frei, false, null).settings.cadence).toBe("None");
  });

  // Rot, wenn das Abwählen der Nacharbeit auf einem Weg aufs Tagesziel fiele.
  it("fällt beim Abwählen der Nacharbeit auf frei", () => {
    const s = { cadence: "UntilDone", remediation: true } as Parameters<typeof withRemediation>[0];
    expect(withRemediation(s, false, true).cadence).toBe("None");
    expect(withRemediation(s, false).cadence).toBe("Daily");
  });
});

/* B-841 (B-704 Entscheidung 4): die Stufe in der Vater-Auswertung. */
describe("aufbauStufeText", () => {
  const b = { level: 1, of: 3, playedExerciseId: 6, playedTitle: "Wörter erkennen", targetExerciseId: 5, targetTitle: "Tiere" };
  // Rot, wenn die Vorstufe nicht genannt wird oder auf dem Ziel ein leerer Zusatz stünde.
  it("nennt die gespielte Vorstufe, auf dem Ziel nur die Stufe", () => {
    expect(aufbauStufeText(b)).toBe("Stufe 1 von 3 · gerade: Wörter erkennen");
    expect(aufbauStufeText({ ...b, level: 3, playedExerciseId: 5, playedTitle: "Tiere" })).toBe("Stufe 3 von 3");
  });
});
