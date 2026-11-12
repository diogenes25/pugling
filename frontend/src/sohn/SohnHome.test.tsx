import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import "../lib/i18n";
import type { PositionStatus } from "../lib/types";

// Die Startseite hängt an Kontext, den die Karte nicht braucht.
vi.mock("./SohnApp", () => ({ useSohn: () => ({ planId: 1, setPlanId: () => {}, childId: 7, celebrate: () => {} }) }));
vi.mock("../lib/auth", () => ({ useAuth: () => ({ signOut: () => {} }) }));

const { PositionCard } = await import("./SohnHome");

const position = (cadence: PositionStatus["cadence"], goalMet: boolean): PositionStatus => ({
  positionId: 1, exerciseTitle: "Unit 1", exerciseType: "Vocabulary", cadence, goalMet,
  useLeitner: true, testable: false, poolSize: 8, introducedCount: 8, dueCount: 0,
} as PositionStatus);

/*
 * B-467: eine freie Position hat keine Pflicht – der Server meldet sie als erfüllt (`goalMet: true`), auch ungespielt.
 * Rot, wenn die Karte ihr trotzdem Häkchen oder grünen Rahmen gibt; rot auch, wenn eine erfüllte Pflicht beides verliert.
 */
describe("Positionskarte auf der Startseite", () => {
  it("gibt einer freien Position kein Häkchen und keinen grünen Rahmen", () => {
    const { container } = render(<MemoryRouter><PositionCard pos={position("None", true)} /></MemoryRouter>);
    expect(screen.queryByText("✓")).toBeNull();
    expect((container.querySelector(".card") as HTMLElement).style.borderColor).toBe("");
  });

  it("gibt einer erfüllten Pflicht ihr Häkchen und den grünen Rahmen", () => {
    const { container } = render(<MemoryRouter><PositionCard pos={position("Daily", true)} /></MemoryRouter>);
    expect(screen.getByText("✓")).toBeTruthy();
    expect((container.querySelector(".card") as HTMLElement).style.borderColor).toBe("var(--lime)");
  });
});
