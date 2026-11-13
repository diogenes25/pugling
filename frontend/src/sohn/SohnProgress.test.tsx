import { act, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "../lib/api";
import type { PlanResponse } from "../lib/types";

/**
 * B-254: der Weg-Tab nimmt denselben Plan wie die Startseite. Vorher lud er die im Browser gemerkte Plan-Id ungeprüft
 * – war der Plan gelöscht oder gehörte die Id zu einer älteren Datenbank, las das Kind „Study plan not found.“.
 */
const setPlanId = vi.fn();
const plans = vi.fn();
const overview = vi.fn();
const overviewProgress = vi.fn();
const weekCoins = vi.fn();
const STALE = 99;

vi.mock("./SohnApp", () => ({ useSohn: () => ({ planId: STALE, setPlanId, childId: 7, celebrate: () => {} }) }));
vi.mock("../lib/auth", () => ({ useAuth: () => ({ signOut: () => {} }) }));
vi.mock("./MyObjectives", () => ({ MyObjectives: () => null }));
vi.mock("./GamificationPanels", () => ({ BadgesGallery: () => null, MissionsPanel: () => <div>Missionen-Panel</div> }));
vi.mock("../lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("../lib/api")>();
  return {
    ...original,
    api: {
      plans: (...a: unknown[]) => plans(...a),
      overview: (...a: unknown[]) => overview(...a),
      overviewProgress: (...a: unknown[]) => overviewProgress(...a),
      weekCoins: () => weekCoins(),
    },
  };
});

const { SohnProgress } = await import("./SohnProgress");

const plan = (id: number, isPlayable: boolean): PlanResponse => ({
  id, childId: 7, title: `Plan ${id}`, startDate: "2026-10-01", endDate: "2026-12-01", active: true, positionCount: 1,
  isPlayable,
} as PlanResponse);

beforeEach(() => {
  setPlanId.mockReset();
  plans.mockReset();
  // Was der Server für die gemerkte Id antwortet: es gibt den Plan nicht (mehr).
  overview.mockReset().mockImplementation((id: number) => id === STALE
    ? Promise.reject(new ApiError(404, "Study plan not found.", "trace-1"))
    : Promise.resolve({ title: `Plan ${id}`, today: { goalsMet: 0, goalsTotal: 0, positions: [] }, currentStreak: 0 }));
  overviewProgress.mockReset().mockResolvedValue({ days: [], totalPoints: 0, daysComplete: 0, totalDays: 7, currentStreak: 0 });
  weekCoins.mockReset().mockResolvedValue({ weekStart: "2026-11-09", thisWeek: 0, lastWeekSoFar: 0, lastWeekTotal: 0 });
});

const renderProgress = () => render(<MemoryRouter><SohnProgress /></MemoryRouter>);

describe("SohnProgress mit einer gemerkten Plan-Id, die nicht mehr gilt", () => {
  // Rot, wenn der Weg wieder die gemerkte Id lädt: dann steht die englische Server-Zeile da.
  it("zeigt ohne Plan den Hinweis zum Pfad statt eines Serverfehlers", async () => {
    plans.mockResolvedValue([]);
    renderProgress();

    expect(await screen.findByText("Wähle zuerst eine Mission auf dem Pfad.")).toBeTruthy();
    expect(overview).not.toHaveBeenCalled();
  });

  // Rot, wenn der Weg die Auswahl der Startseite nicht teilt: er lüde die gemerkte Id statt des spielbaren Plans.
  it("nimmt den spielbaren Plan und merkt ihn sich", async () => {
    plans.mockResolvedValue([plan(4, false), plan(5, true)]);
    renderProgress();

    // Positiv: der Weg des gewählten Plans steht da (sein Titel kommt nur aus der Antwort für Plan 5).
    expect(await screen.findByText(/Plan 5/)).toBeTruthy();
    expect(overview).not.toHaveBeenCalledWith(STALE);
    expect(setPlanId).toHaveBeenCalledWith(5);
    expect(screen.queryByText(/Study plan not found/)).toBeNull();
  });

  // B-324. Rot, wenn der Zweig „gemerkt“ fehlt (`find((p) => p.id === planId) ??` gestrichen): dann gewänne der spielbare
  // Plan, und eine bewusste Wahl auf der Startseite spränge sofort zurück. Die gemerkte Id ist hier – anders als oben –
  // ein Plan des Kindes, und er ist ausdrücklich NICHT spielbar, damit sich „gemerkt“ und „spielbar“ unterscheiden.
  it("bleibt beim gemerkten Plan, auch wenn ein anderer spielbar ist", async () => {
    overview.mockImplementation((id: number) =>
      Promise.resolve({ title: `Plan ${id}`, today: { goalsMet: 0, goalsTotal: 0, positions: [] }, currentStreak: 0 }));
    plans.mockResolvedValue([plan(5, true), plan(STALE, false)]);
    renderProgress();

    expect(await screen.findByText(new RegExp(`Plan ${STALE}`))).toBeTruthy();
    expect(overview).toHaveBeenCalledWith(STALE);
    expect(overview).not.toHaveBeenCalledWith(5);
    expect(setPlanId).not.toHaveBeenCalled();
  });
});

// B-525: die Positionen sind der Pfad auf der Startseite; hier stünden sie doppelt. Rot, wenn die Liste zurückkehrt
// (der Titel der Position erschiene wieder) oder wenn die Missionen nicht mit umgezogen sind.
describe("Erfolge", () => {
  it("zeigen die Missionen, aber keine Positionsliste mehr", async () => {
    plans.mockResolvedValue([plan(5, true)]);
    overview.mockImplementation((id: number) => Promise.resolve({
      title: `Plan ${id}`, currentStreak: 0,
      today: { goalsMet: 1, goalsTotal: 1, positions: [{ positionId: 2, exerciseTitle: "Pflicht erledigt", cadence: "Daily", goalMet: true }] },
    }));
    renderProgress();

    expect(await screen.findByText("Missionen-Panel")).toBeTruthy();
    expect(screen.queryByText("Pflicht erledigt")).toBeNull();
  });
});

/** B-253 (G-6): die eigene Woche gegen die eigene Vorwoche bis jetzt – neutral, ohne Tadel. */
describe("Erfolge, Wochenvergleich", () => {
  // AK 5. Rot, wenn die Zeile fehlt oder die Zahlen nicht vom Server kommen.
  it("zeigt diese Woche neben der Vorwoche bis jetzt", async () => {
    plans.mockResolvedValue([plan(5, true)]);
    weekCoins.mockResolvedValue({ weekStart: "2026-11-09", thisWeek: 50, lastWeekSoFar: 50, lastWeekTotal: 77 });
    renderProgress();

    expect(await screen.findByText(/Diese Woche 50 🪙 · letzte Woche bis jetzt 50/)).toBeTruthy();
    expect(screen.getByText(/ganze letzte Woche: 77/)).toBeTruthy();
  });

  // AK 5, weniger als letzte Woche: neutral. Rot, wenn die Zeile dann ein Urteil fällte („schlechter“) oder rot färbte.
  it("bleibt bei weniger als letzte Woche neutral", async () => {
    plans.mockResolvedValue([plan(5, true)]);
    weekCoins.mockResolvedValue({ weekStart: "2026-11-09", thisWeek: 10, lastWeekSoFar: 50, lastWeekTotal: 77 });
    renderProgress();

    const zeile = (await screen.findByText(/Diese Woche 10 🪙/)).closest("p")!;
    expect(zeile.textContent).not.toMatch(/schlechter/i);
    expect(zeile.innerHTML).not.toMatch(/red|danger|err/i);
  });

  // AK 5, 0/0: nichts zu vergleichen. Rot, wenn die Zeile auch dann erscheint.
  it("lässt die Zeile weg, wenn beide Wochen 0 sind", async () => {
    plans.mockResolvedValue([plan(5, true)]);
    weekCoins.mockResolvedValue({ weekStart: "2026-11-09", thisWeek: 0, lastWeekSoFar: 0, lastWeekTotal: 30 });
    renderProgress();

    await screen.findByText("Missionen-Panel");
    await screen.findByText(/Plan 5/);
    expect(weekCoins).toHaveBeenCalled();
    // Die Antwort erst ankommen lassen – sonst prüfte die Zeile den Stand davor und bliebe immer grün.
    await act(async () => {});
    expect(screen.queryByText(/Diese Woche/)).toBeNull();
  });
});
