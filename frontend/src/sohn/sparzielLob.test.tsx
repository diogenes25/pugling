import { act, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import "../lib/i18n";
import { DEFAULT_SKIN } from "../lib/skins";
import type { MyPraise, PositionStatus, SavingsGoal, TodayResponse } from "../lib/types";

vi.mock("./SohnApp", () => ({
  useSohn: () => ({
    planId: 1, setPlanId: () => {}, childId: 7, celebrate: () => {}, skin: DEFAULT_SKIN, refreshWallet: () => {}, setStreak: () => {},
  }),
}));
vi.mock("../lib/auth", () => ({ useAuth: () => ({ signOut: () => {} }) }));

const goal = vi.fn<() => Promise<SavingsGoal | undefined>>();
const praise = vi.fn<() => Promise<MyPraise[]>>();
const seen = vi.fn<(ids: number[]) => Promise<void>>();
vi.mock("../lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("../lib/api")>();
  return {
    ...original,
    api: {
      ...original.api,
      today: () => Promise.resolve(sicht),
      myInterests: () => Promise.resolve({ needsIntro: false }),
      mySubjectLevels: () => Promise.resolve([]),
      upcomingClassTests: () => Promise.resolve([]),
      feedbackNotices: () => Promise.resolve([]),
      mySavingsGoal: () => goal(),
      myPraise: () => praise(),
      markPraiseSeen: (ids: number[]) => seen(ids),
    },
  };
});

const { SohnHome } = await import("./SohnHome");

const position = {
  positionId: 1, exerciseId: 1, exerciseTitle: "Wörter", exerciseType: "Vocabulary", renderer: "flashcards",
  order: 1, cadence: "Daily", checkMode: "PositionTest", useLeitner: true, testable: true, goalMet: false,
  goalProgress: 0, dueCount: 3, poolSize: 10, pointsGoalMet: 20, introducedCount: 4, imprintedCount: 4, penaltyCoins: 0, owesDuty: true,
  seriesUnitId: 1, seriesUnitLabel: "Unit 1", planId: 1, planTitle: "Plan", subjectName: "Englisch",
} as unknown as PositionStatus;
const sicht: TodayResponse = {
  childId: 7, day: "2026-11-25", dutyDone: false, goalsTotal: 1, goalsMet: 0, outstanding: [],
  rewardsUnlocked: false, currentStreak: 0, positions: [position],
  dailyBox: { claimedToday: false, openedToday: false, waiting: 0, coinsAwarded: null, gemsAwarded: null, streakAtClaim: 0 },
  rewardGoalsTotal: 1, rewardGoalsMet: 0, rewardCadence: "Daily",
} as TodayResponse;

const ziel = (over: Partial<SavingsGoal> = {}): SavingsGoal => ({
  listingId: 5, title: "Kino mit Papa", available: true, coinPrice: 166, gemPrice: 0, coins: 96, gems: 0,
  missingCoins: 70, missingGems: 0, daysAtPace: 14, currentStock: 3, nextRefillAtUtc: null, ...over,
} as SavingsGoal);

const zeige = async () => {
  render(<MemoryRouter><SohnHome /></MemoryRouter>);
  await act(async () => {});
};

beforeEach(() => {
  goal.mockReset().mockResolvedValue(undefined);
  praise.mockReset().mockResolvedValue([]);
  seen.mockReset().mockResolvedValue(undefined);
});

/*
 * B-593 AK 10 – der Sparziel-Streifen der Startseite. Je Fall steht dabei, welche Änderung ihn rot macht.
 */
describe("Sparziel auf der Startseite", () => {
  it("sagt bei einem reinen Gem-Preis „Noch N Gems“ ohne Tage", async () => {
    // Rot, wenn ein Gem-Ziel als nackte Zahl oder mit „und“ ohne Münzbetrag davor erscheint.
    goal.mockResolvedValue(ziel({ coinPrice: 0, gemPrice: 5, missingCoins: 0, missingGems: 3, daysAtPace: null }));
    await zeige();
    const streifen = screen.getByTestId("sparziel");
    expect(streifen.textContent).toContain("Noch 3 Gems");
    expect(streifen.textContent).not.toContain("und 3");
  });

  it("zeigt ohne Ziel keinen Streifen", async () => {
    // Rot, wenn der Streifen auch ohne Ziel (204 → undefined) mit leerem Titel erscheint.
    await zeige();
    expect(screen.getByText("Wörter")).toBeTruthy();
    expect(screen.queryByTestId("sparziel")).toBeNull();
  });

  it("nennt Ziel, fehlende Münzen und die Tage als Prognose – nie als Pflicht-Tage", async () => {
    // Rot, wenn die Tage ohne „bei deinem Tempo etwa“ dastehen oder „Pflicht“ als Zusage auftaucht.
    goal.mockResolvedValue(ziel());
    await zeige();
    const streifen = screen.getByTestId("sparziel");
    expect(streifen.textContent).toContain("Kino mit Papa");
    expect(streifen.textContent).toContain("Noch 70 Münzen");
    expect(streifen.textContent).toContain("bei deinem Tempo etwa 14 Tage");
    expect(streifen.textContent).not.toContain("Pflicht");
  });

  it("lässt die Tageszeile weg, wenn der Server keine Zahl hat", async () => {
    // Rot, wenn `daysAtPace: null` als „etwa 0 Tage“ oder „etwa null Tage“ erscheint.
    goal.mockResolvedValue(ziel({ daysAtPace: null }));
    await zeige();
    const streifen = screen.getByTestId("sparziel");
    expect(streifen.textContent).toContain("Noch 70 Münzen");
    expect(streifen.textContent).not.toContain("Tempo");
  });

  it("zeigt ein verschwundenes Angebot als nicht mehr verfügbar, mit dem Weg, es zu entfernen", async () => {
    // Rot, wenn ein gelöschtes Angebot (Id leer, Titel bleibt) still verschwindet oder ohne Entfernen-Knopf dasteht.
    goal.mockResolvedValue(ziel({ listingId: null, available: false, coinPrice: null, missingCoins: null, daysAtPace: null }));
    await zeige();
    const streifen = screen.getByTestId("sparziel");
    expect(streifen.textContent).toContain("Kino mit Papa");
    expect(streifen.textContent).toContain("Nicht mehr verfügbar");
    expect(screen.getByRole("button", { name: "Ziel entfernen" })).toBeTruthy();
  });
});

/*
 * B-377 AK 8 – das Lob-Overlay der Startseite.
 */
describe("Lob auf der Startseite", () => {
  it("erscheint bei leerer Liste nicht", async () => {
    // Rot, wenn das Overlay ohne ungelesenes Lob aufgeht.
    await zeige();
    expect(screen.queryByRole("dialog")).toBeNull();
  });

  it("zeigt Absender und Text als Text und stempelt beim „Danke!“ die gezeigten Lobe", async () => {
    // Rot, wenn der Lobtext durch `t()` läuft (der Schlüssel käme übersetzt an) oder „Danke!“ ohne Stempel schließt.
    praise.mockResolvedValue([
      { id: 11, supervisorName: "Papa", text: "lob.thanks", createdAt: "2026-11-25T08:00:00Z" },
      { id: 12, supervisorName: "Mama", text: "Super gemacht!", createdAt: "2026-11-25T07:00:00Z" },
    ] as MyPraise[]);
    await zeige();
    const dialog = screen.getByRole("dialog");
    expect(dialog.textContent).toContain("lob.thanks");
    expect(dialog.textContent).toContain("Super gemacht!");
    expect(dialog.textContent).toContain("Lob von Mama");

    fireEvent.click(screen.getByRole("button", { name: "Danke!" }));
    await act(async () => {});
    expect(seen).toHaveBeenCalledWith([11, 12]);
    expect(screen.queryByRole("dialog")).toBeNull();
  });

  it("schließt auch, wenn der Stempel scheitert – das Lob kommt dann beim nächsten Öffnen wieder", async () => {
    // Rot, wenn das Overlay nach gescheitertem Stempel offen bleibt (Endlosschleife für das Kind, Risiko im Story-Text).
    praise.mockResolvedValue([{ id: 4, supervisorName: "Papa", text: "Gut!", createdAt: "2026-11-25T08:00:00Z" }] as MyPraise[]);
    seen.mockRejectedValue(new Error("offline"));
    await zeige();
    fireEvent.click(screen.getByRole("button", { name: "Danke!" }));
    await act(async () => {});
    expect(seen).toHaveBeenCalledWith([4]);
    expect(screen.queryByRole("dialog")).toBeNull();
  });

  it("lässt die Startseite stehen, wenn das Laden der Lobe scheitert", async () => {
    // Rot, wenn ein gescheitertes `me/praise` die Startseite blockiert oder ein leeres Overlay öffnet.
    praise.mockRejectedValue(new Error("offline"));
    await zeige();
    expect(screen.getByText("Wörter")).toBeTruthy();
    expect(screen.queryByRole("dialog")).toBeNull();
  });

  it("schließt mit Escape ohne Stempel", async () => {
    // Rot, wenn Escape das Lob als gelesen stempelt (es soll beim nächsten Öffnen wiederkommen) oder gar nicht schließt.
    praise.mockResolvedValue([{ id: 5, supervisorName: "Papa", text: "Prima!", createdAt: "2026-11-25T08:00:00Z" }] as MyPraise[]);
    await zeige();
    expect(document.activeElement?.textContent).toBe("Danke!");
    fireEvent.keyDown(document, { key: "Escape" });
    await act(async () => {});
    expect(screen.queryByRole("dialog")).toBeNull();
    expect(seen).not.toHaveBeenCalled();
  });

  it("nennt bei einem einzigen Lob den Absender in der Überschrift", async () => {
    // Rot, wenn die Überschrift den Namen verliert (das Kind sähe nicht, wer es gelobt hat).
    praise.mockResolvedValue([{ id: 3, supervisorName: "Oma", text: "Toll!", createdAt: "2026-11-25T08:00:00Z" }] as MyPraise[]);
    await zeige();
    expect(screen.getByRole("heading", { name: "Lob von Oma" })).toBeTruthy();
  });
});
