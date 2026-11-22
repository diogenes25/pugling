import { describe, expect, it } from "vitest";

import { buildTargetOptions } from "./BuildPathField";
import type { ExerciseSummary } from "../lib/types";

/** Nur die Felder, die die Regel liest – der Rest der Zeile spielt für die Auswahl keine Rolle. */
const row = (id: number, seriesUnitId: number, buildTargetExerciseId: number | null = null) =>
  ({ id, seriesUnitId, title: `Übung ${id}`, buildTargetExerciseId }) as ExerciseSummary;

/*
 * B-704 AK 2: „Vorstufe von" bietet nur an, was der Server annimmt (B-700): Übungen der eigenen Unit, nicht die Übung
 * selbst, keine, die selbst Vorstufe ist. Je Fall eine Zeile in der Eingabe, die nur er herausnimmt.
 */
describe("buildTargetOptions – welche Übungen als Ziel taugen", () => {
  const rows = [row(1, 3), row(2, 3), row(4, 9), row(5, 3, 2)];

  // Rote Probe: den Filter auf `seriesUnitId` streichen – dann steht Übung 4 aus Unit 9 in der Liste.
  it("bietet nur Übungen der eigenen Unit an", () => {
    expect(buildTargetOptions(rows, 1, 3).map((e) => e.id)).not.toContain(4);
  });

  // Rote Probe: `e.id !== selfId` streichen – dann bietet die Liste die Übung als ihr eigenes Ziel an.
  it("bietet die Übung nicht als ihr eigenes Ziel an", () => {
    expect(buildTargetOptions(rows, 1, 3).map((e) => e.id)).not.toContain(1);
  });

  // Rote Probe: `buildTargetExerciseId == null` streichen – dann stünde Vorstufe 5 als Ziel da (Kette, Server: 400).
  it("bietet keine Vorstufe als Ziel an", () => {
    expect(buildTargetOptions(rows, 1, 3).map((e) => e.id)).toEqual([2]);
  });
});
