import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { UsagePanel } from "./VaterExercises";
import type { ExerciseSummary, ExerciseUsage } from "../lib/types";

/*
 * B-409. Die Verwendungs-Anzeige kannte keine Ziel-Etappen, obwohl sie das Löschen einer Übung genauso sperren wie eine
 * Position. Hing eine Übung nur an einem Ziel, stand hier „Löschen ist möglich" – der Server antwortete 409.
 *
 * Bausteintest: die Frage ist, welchen Satz das Panel bei welcher Antwort zeigt. Den Klickweg trägt
 * `e2e/verwendung-ziele.spec.ts`.
 */

/** Nur die Felder, die das Panel liest; der Rest ist für diese Frage gleichgültig. */
const exercise = { isOwner: false, executePublic: true } as ExerciseSummary;

const usage = (over: Partial<ExerciseUsage> = {}): ExerciseUsage => ({
  plans: [], classTests: [], goals: [], otherLearnersCount: 0, ...over,
});

const panel = (u: ExerciseUsage) =>
  render(<UsagePanel usage={u} exercise={exercise} busy={false} onToggleSharing={() => {}} />);

describe("UsagePanel", () => {
  // Fall 4. Rot, wenn `hasOwn` die Ziele nicht mitzählt (dann kommt der „nirgends"-Satz) oder die Zeile fehlt.
  it("nennt eine eigene Ziel-Etappe und verspricht kein Löschen", () => {
    panel(usage({
      goals: [{ objectiveId: 7, objectiveTitle: "Englisch sicher", keyResultTitle: "Unit 3 sitzt", childId: 1, childName: "Max" }],
    }));
    expect(screen.getByText("Ziele: Englisch sicher – Unit 3 sitzt (Max)")).toBeTruthy();
    expect(screen.queryByText(/nirgends eingesetzt/)).toBeNull();
  });

  // Teil von Fall 4: eine Etappe ohne eigenen Titel nennt nur das Ziel, kein „ – null".
  it("lässt den Etappen-Titel weg, wenn keiner gesetzt ist", () => {
    panel(usage({
      goals: [{ objectiveTitle: "Französisch", keyResultTitle: null, objectiveId: 8, childId: 2, childName: "Lea" }],
    }));
    expect(screen.getByText("Ziele: Französisch (Lea)")).toBeTruthy();
  });

  // Fall 5, Gegenprobe: ohne jede Verwendung bleibt der Satz, wie er war – auch vor dem Fix grün.
  it("sagt bei gar keiner Verwendung weiterhin, dass Löschen möglich ist", () => {
    panel(usage());
    expect(screen.getByText(/nirgends eingesetzt.*Löschen ist möglich/)).toBeTruthy();
  });
});
