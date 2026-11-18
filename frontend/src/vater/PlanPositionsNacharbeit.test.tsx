import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { PositionReport } from "../lib/types";
import { NacharbeitReport } from "./PlanPositions";

// Nur die Felder, die der Nacharbeits-Teil liest; der Rest der Auswertung spielt hier keine Rolle.
function report(items: { itemIndex: number; answer: string; remediation: { cue: string; doneAt: string | null } | null }[]) {
  return {
    totalItems: items.length,
    completedOn: "2026-11-18",
    items: items.map((it) => ({
      ...it,
      remediation: it.remediation && { ...it.remediation, solvedByStage: [] },
    })),
  } as unknown as PositionReport;
}

describe("NacharbeitReport (B-667)", () => {
  // Rot, wenn der Kopf gegen alle Inhalte zählt („2/3“) oder die Zeile hinter dem Pool als „offen“ erscheint.
  it("zählt und zeigt nur die Aufgaben des Pools", () => {
    render(<NacharbeitReport stufenName={() => "frei"} report={report([
      { itemIndex: 0, answer: "eastwards", remediation: { cue: "Richtung Osten", doneAt: "2026-11-17" } },
      { itemIndex: 1, answer: "asleep", remediation: { cue: "schlief ein", doneAt: "2026-11-18" } },
      { itemIndex: 2, answer: "as soon as", remediation: null },
    ])} />);

    expect(screen.getByText(/Nacharbeit: 2\/2 erledigt/)).toBeTruthy();
    expect(screen.queryByText("offen")).toBeNull();
    expect(screen.queryByText("as soon as")).toBeNull();
  });

  // Gegenprobe: eine offene Aufgabe im Pool bleibt „offen“ und zählt mit – rot, wenn die Filterung zu viel wegnimmt.
  it("lässt eine offene Aufgabe des Pools stehen", () => {
    render(<NacharbeitReport stufenName={() => "frei"} report={report([
      { itemIndex: 0, answer: "eastwards", remediation: { cue: "Richtung Osten", doneAt: "2026-11-17" } },
      { itemIndex: 1, answer: "asleep", remediation: { cue: "schlief ein", doneAt: null } },
    ])} />);

    expect(screen.getByText(/Nacharbeit: 1\/2 erledigt/)).toBeTruthy();
    expect(screen.getByText("offen")).toBeTruthy();
  });
});
