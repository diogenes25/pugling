import { render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import "../lib/i18n";
import { DEFAULT_SKIN } from "../lib/skins";
import type { PositionStatus, SubjectLevel, TodayResponse } from "../lib/types";

vi.mock("./SohnApp", () => ({
  useSohn: () => ({
    planId: 1, setPlanId: () => {}, childId: 7, celebrate: () => {}, skin: DEFAULT_SKIN, refreshWallet: () => {}, setStreak: () => {},
  }),
}));
vi.mock("../lib/auth", () => ({ useAuth: () => ({ signOut: () => {} }) }));

const today = vi.fn<() => Promise<TodayResponse>>();
const levels = vi.fn<() => Promise<SubjectLevel[]>>();
vi.mock("../lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("../lib/api")>();
  return {
    ...original,
    api: {
      ...original.api,
      today: () => today(),
      mySubjectLevels: () => levels(),
      myInterests: () => Promise.resolve({ needsIntro: false }),
      // Sparziel (B-593) und Lob (B-377) sind hier kein Thema – leer, ohne Netz.
      mySavingsGoal: () => Promise.resolve(undefined),
      myPraise: () => Promise.resolve([]),
    },
  };
});

const { SohnHome } = await import("./SohnHome");

const position = (over: Partial<PositionStatus>): PositionStatus => ({
  positionId: 1, exerciseId: 1, exerciseTitle: "Wörter", exerciseType: "Vocabulary", renderer: "flashcards",
  order: 1, cadence: "Daily", checkMode: "PositionTest", useLeitner: true, testable: true, goalMet: false,
  dueCount: 3, poolSize: 10, pointsGoalMet: 20, introducedCount: 4, imprintedCount: 4, penaltyCoins: 0,
  seriesUnitId: 1, seriesUnitLabel: "Unit 1", planId: 1, planTitle: "Plan", subjectName: "Englisch", ...over,
} as PositionStatus);

const sicht = (positions: PositionStatus[]): TodayResponse => ({
  childId: 7, day: "2026-11-24", dutyDone: false, goalsTotal: positions.length, goalsMet: 0, outstanding: [],
  rewardsUnlocked: false, currentStreak: 0, positions,
  dailyBox: { claimedToday: false, openedToday: false, waiting: 0, coinsAwarded: null, gemsAwarded: null, streakAtClaim: 0 },
  rewardGoalsTotal: positions.length, rewardGoalsMet: 0, rewardCadence: "Daily",
} as TodayResponse);

const zweiFaecher = sicht([
  position({ positionId: 1, subjectName: "Englisch", exerciseTitle: "Englisch 1" }),
  position({ positionId: 2, subjectName: "Mathe", exerciseTitle: "Mathe 1", exerciseType: "Arithmetic" }),
]);

/*
 * B-743: die Notenstufe auf der Fachkarte. Sie hängt am Fach, das der Server liefert, und ist Beiwerk der Startseite.
 */
describe("Notenstufe auf der Fachkarte", () => {
  // AK 3: nur das Fach, das der Server liefert, trägt eine Stufenzeile. Rot, wenn die Karte eine leere oder erfundene
  // Stufe zeigte (etwa jede Karte die erste Stufe der Liste).
  it("zeigt die Stufe am gelieferten Fach und keine an Mathe", async () => {
    today.mockResolvedValue(zweiFaecher);
    levels.mockResolvedValue([{ subjectId: 3, subjectName: "Englisch", level: 5, bestLevel: 4, wordsToNextLevel: 12 }]);
    render(<MemoryRouter><SohnHome /></MemoryRouter>);

    const englisch = await screen.findByRole("region", { name: "Englisch" });
    expect(await within(englisch).findByText("noch 12 Wörter sicher bis zur nächsten Stufe")).toBeTruthy();
    const mathe = screen.getByRole("region", { name: "Mathe" });
    expect(within(mathe).queryByTestId("fach-stufe")).toBeNull();
  });

  // AK 5: scheitert der Abruf der Stufen, steht die Startseite trotzdem – ohne Stufenzeile. Rot, wenn der Fehler der
  // Stufen-Abfrage die Seite leerte oder die Tagessicht an ihr hinge.
  it("lässt die Fachkarten stehen, wenn die Stufen nicht laden", async () => {
    today.mockResolvedValue(zweiFaecher);
    levels.mockRejectedValue(new Error("kaputt"));
    render(<MemoryRouter><SohnHome /></MemoryRouter>);

    expect(await screen.findByRole("region", { name: "Englisch" })).toBeTruthy();
    expect(screen.getByRole("region", { name: "Mathe" })).toBeTruthy();
    expect(screen.queryByTestId("fach-stufe")).toBeNull();
  });
});
