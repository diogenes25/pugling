import { fireEvent, render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import "../lib/i18n";
import { DEFAULT_SKIN } from "../lib/skins";
import type { PositionStatus, TodayResponse } from "../lib/types";

const setPlanId = vi.fn();
vi.mock("./SohnApp", () => ({
  useSohn: () => ({
    planId: 1, setPlanId, childId: 7, celebrate: () => {}, skin: DEFAULT_SKIN, refreshWallet: () => {}, setStreak: () => {},
  }),
}));
vi.mock("../lib/auth", () => ({ useAuth: () => ({ signOut: () => {} }) }));

const today = vi.fn<(childId: number) => Promise<TodayResponse>>();
vi.mock("../lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("../lib/api")>();
  return {
    ...original,
    api: {
      ...original.api,
      today: (childId: number) => today(childId),
      // Der Interessen-Hinweis ist hier kein Thema – stumm, ohne Netz.
      myInterests: () => Promise.resolve({ needsIntro: false }),
    },
  };
});

const { Pfad, SohnHome } = await import("./SohnHome");

const position = (over: Partial<PositionStatus>): PositionStatus => ({
  positionId: 1, exerciseId: 1, exerciseTitle: "Wörter", exerciseType: "Vocabulary", renderer: "flashcards",
  order: 1, cadence: "Daily", checkMode: "PositionTest", useLeitner: true, testable: true, goalMet: false,
  dueCount: 3, poolSize: 10, pointsGoalMet: 20, introducedCount: 4, penaltyCoins: 0,
  seriesUnitId: 1, seriesUnitLabel: "Unit 1", planId: 1, planTitle: "Plan", subjectName: "Englisch", ...over,
} as PositionStatus);

/*
 * B-606 AK 2 – eine Position der Startseite führt in den Weg ihres eigenen Plans. Üben und Test spielen den gemerkten
 * Plan, also muss „Los“ den Plan der Position merken.
 */
describe("Weg von der Kind-Tagessicht", () => {
  it("merkt beim Los den Plan der Position, nicht den bisher gemerkten", () => {
    // Rot, wenn „Los“ nur navigiert: Üben öffnete dann die Position im falschen (gemerkten) Plan.
    render(<MemoryRouter><Pfad positions={[position({ positionId: 9, planId: 42, exerciseTitle: "Französisch 1" })]} /></MemoryRouter>);
    fireEvent.click(screen.getByRole("button", { name: /^Französisch 1/ }));
    fireEvent.click(screen.getByRole("link", { name: /LOS/ }));
    expect(setPlanId).toHaveBeenCalledWith(42);
  });

  it("hebt über mehrere Pfade nur den vorgegebenen nächsten Knoten hervor", () => {
    // Rot, wenn jeder Fach-Pfad seinen eigenen ersten offenen Knoten als START zeigt.
    render(<MemoryRouter><Pfad positions={[position({ positionId: 3, exerciseTitle: "Offen" })]} naechsteId={99} /></MemoryRouter>);
    expect(screen.queryByText("START")).toBeNull();
  });
});

/*
 * B-606 AK 2 – die Startseite selbst: sie liest die Kind-Tagessicht (`api.today`) über alle Pläne, gliedert nach Fach,
 * zeigt einen einzigen START und führt jede Position in den Weg ihres Plans.
 */
describe("Startseite über zwei Pläne", () => {
  const sicht = (positions: PositionStatus[]): TodayResponse => ({
    childId: 7, day: "2026-11-17", dutyDone: false, goalsTotal: positions.length, goalsMet: 0, outstanding: [],
    rewardsUnlocked: false, currentStreak: 0, positions,
    dailyBox: { claimedToday: false, openedToday: false, waiting: 0, coinsAwarded: null, gemsAwarded: null, streakAtClaim: 0 },
  });

  it("zeigt je Fach einen Abschnitt, einen START über beide und führt in den Plan der Position", async () => {
    setPlanId.mockClear();
    today.mockResolvedValue(sicht([
      position({ positionId: 1, planId: 11, planTitle: "Englisch-Plan", subjectName: "Englisch", exerciseTitle: "Englisch 1" }),
      position({ positionId: 2, planId: 22, planTitle: "Französisch-Plan", subjectName: "Französisch", exerciseTitle: "Französisch 1" }),
    ]));
    render(<MemoryRouter><SohnHome /></MemoryRouter>);

    // Rot, wenn die Startseite wieder der Übersicht eines Plans folgt: dann gibt es keine Fach-Überschriften.
    const faecher = await screen.findAllByRole("heading", { level: 2 });
    expect(faecher.map((h) => h.textContent)).toEqual(["Englisch", "Französisch"]);
    expect(within(screen.getByRole("region", { name: "Französisch" })).getByRole("button", { name: /^Französisch 1/ })).toBeTruthy();
    // Rot, wenn die Zeile unter „Tagesmission“ die Fächer wiederholt statt die Pläne zu nennen.
    expect(screen.getByText("Englisch-Plan · Französisch-Plan")).toBeTruthy();
    // Rot, wenn SohnHome `naechsteId` nicht weiterreicht: jeder Fach-Pfad zeigte dann seinen eigenen START.
    expect(screen.getAllByText("START")).toHaveLength(1);

    // Rot, wenn „Los“ den gemerkten Plan nicht auf den der Position umstellt (Üben spielte dann Plan 11).
    fireEvent.click(screen.getByRole("button", { name: /^Französisch 1/ }));
    fireEvent.click(screen.getByRole("link", { name: /LOS/ }));
    expect(setPlanId).toHaveBeenCalledWith(22);
  });
});
