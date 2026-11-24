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
      // Die Notenstufe (B-743) ist hier kein Thema – leer, ohne Netz.
      mySubjectLevels: () => Promise.resolve([]),
      // Die Klassenarbeit-Karte (B-750) ebenso: ohne Arbeit steht sie nicht da.
      upcomingClassTests: () => Promise.resolve([]),
    },
  };
});

const { FachKarte, Pfad, SohnHome } = await import("./SohnHome");
type Belohnung = Pick<TodayResponse, "rewardGoalsTotal" | "rewardGoalsMet" | "rewardCadence" | "rewardsUnlocked">;

const position = (over: Partial<PositionStatus>): PositionStatus => ({
  positionId: 1, exerciseId: 1, exerciseTitle: "Wörter", exerciseType: "Vocabulary", renderer: "flashcards",
  order: 1, cadence: "Daily", checkMode: "PositionTest", useLeitner: true, testable: true, goalMet: false,
  dueCount: 3, poolSize: 10, pointsGoalMet: 20, introducedCount: 4, imprintedCount: 4, penaltyCoins: 0,
  seriesUnitId: 1, seriesUnitLabel: "Unit 1", planId: 1, planTitle: "Plan", subjectName: "Englisch", ...over,
} as PositionStatus);

/*
 * B-606 AK 2 – eine Position der Startseite führt in den Weg ihres eigenen Plans. Üben und Test spielen den gemerkten
 * Plan, also muss der Tipp auf den Knoten (seit B-686 der Start selbst) den Plan der Position merken.
 */
describe("Weg von der Kind-Tagessicht", () => {
  it("merkt beim Start den Plan der Position, nicht den bisher gemerkten", () => {
    // Rot, wenn der Knoten nur navigiert: Üben öffnete dann die Position im falschen (gemerkten) Plan.
    render(<MemoryRouter><Pfad positions={[position({ positionId: 9, planId: 42, exerciseTitle: "Französisch 1" })]} /></MemoryRouter>);
    fireEvent.click(screen.getByRole("link", { name: /^Französisch 1/ }));
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
  const sicht = (positions: PositionStatus[], belohnung?: Belohnung): TodayResponse => ({
    childId: 7, day: "2026-11-17", dutyDone: false, goalsTotal: positions.length, goalsMet: 0, outstanding: [],
    rewardsUnlocked: false, currentStreak: 0, positions,
    dailyBox: { claimedToday: false, openedToday: false, waiting: 0, coinsAwarded: null, gemsAwarded: null, streakAtClaim: 0 },
    rewardGoalsTotal: positions.length, rewardGoalsMet: 0, rewardCadence: "Daily", ...belohnung,
  });

  /*
   * B-612 AK 2 und 3: Balken und Truhen-Text kommen aus den Belohnungsfeldern des Servers, die Zeilen „Ziele heute“ und
   * „Ziele diese Woche“ zählen weiter je Takt. Die Positionen sind so gewählt, dass flach nachgerechnet „Tagesziele 2/2“
   * herauskäme – der Server meldet aber „Woche 0/1“ (etwa weil er über Pläne anders wählt).
   */
  it("nimmt Balken und Truhen-Text vom Server, zählt die Zeilen aber je Takt", async () => {
    today.mockResolvedValue(sicht([
      position({ positionId: 1, goalMet: true, exerciseTitle: "Eins" }),
      position({ positionId: 2, goalMet: true, exerciseTitle: "Zwei" }),
    ], { rewardGoalsTotal: 1, rewardGoalsMet: 0, rewardCadence: "Weekly", rewardsUnlocked: false }));
    const { container } = render(<MemoryRouter><SohnHome /></MemoryRouter>);

    // Rot, solange der Balken aus `today.positions` rechnet: dann 100 % statt 0 %.
    await screen.findByText("Schaffe deine Wochenziele, dann öffnet sich die Truhe.");
    expect((container.querySelector(".bar.lime i") as HTMLElement).style.width).toBe("0%");
    // Rot, wenn der Bonus-Einstieg seine Zielart wieder selbst bestimmt (dann „Erst die Tagesziele“).
    expect(screen.getByText("🔒 Erst die Wochenziele – dann öffnet das Bonus-Training.")).toBeTruthy();
    // AK 3 – rot, wenn die Zeile „Ziele heute“ durch die Belohnungszahlen ersetzt wird (dann 0 / 1).
    const heute = screen.getByText("🎯 Ziele heute").closest(".row") as HTMLElement;
    expect(heute.textContent).toContain("2 / 2");
  });

  it("zeigt je Fach einen Abschnitt, einen START über beide und führt in den Plan der Position", async () => {
    setPlanId.mockClear();
    today.mockResolvedValue(sicht([
      position({ positionId: 1, planId: 11, planTitle: "Englisch-Plan", subjectName: "Englisch", exerciseTitle: "Englisch 1" }),
      position({ positionId: 2, planId: 22, planTitle: "Französisch-Plan", subjectName: "Französisch", exerciseTitle: "Französisch 1" }),
    ]));
    render(<MemoryRouter><SohnHome /></MemoryRouter>);

    // Rot, wenn die Startseite wieder der Übersicht eines Plans folgt: dann gibt es keine Fach-Abschnitte.
    expect(await screen.findByRole("region", { name: "Englisch" })).toBeTruthy();
    const franzoesisch = screen.getByRole("region", { name: "Französisch" });
    // Das Fach ohne START ist zugeklappt; aufgeklappt zeigt es seinen Pfad.
    fireEvent.click(within(franzoesisch).getByRole("button", { expanded: false }));
    expect(within(franzoesisch).getByRole("link", { name: /^Französisch 1/ })).toBeTruthy();
    // Rot, wenn die Zeile unter „Tagesmission“ die Fächer wiederholt statt die Pläne zu nennen.
    expect(screen.getByText("Englisch-Plan · Französisch-Plan")).toBeTruthy();
    // Rot, wenn SohnHome `naechsteId` nicht weiterreicht: jeder Fach-Pfad zeigte dann seinen eigenen START.
    expect(screen.getAllByText("START")).toHaveLength(1);

    // Rot, wenn der Knoten den gemerkten Plan nicht auf den der Position umstellt (Üben spielte dann Plan 11).
    fireEvent.click(screen.getByRole("link", { name: /^Französisch 1/ }));
    expect(setPlanId).toHaveBeenCalledWith(22);
  });
});

/*
 * Die Fächer als aufklappbare Reiter: der Kopf sagt, was die Pflicht des Fachs noch will, aufgeklappt kommt erst die
 * Pflicht, die Zusatzübungen stehen getrennt und zugeklappt darunter – sonst scrollt das Kind am ersten Fach vorbei.
 */
describe("Fach-Reiter", () => {
  const zeige = (positions: PositionStatus[], naechsteId: number | null) =>
    render(<MemoryRouter><FachKarte id="f" farbe={0} titel="Englisch" positions={positions} naechsteId={naechsteId} /></MemoryRouter>);
  const kopf = () => screen.getAllByRole("button")[0];

  it("klappt ein Fach ohne START zu und mit START auf", () => {
    // Rot, wenn jedes Fach offen startet (das lange Scrollen) oder auch das Fach mit dem START zu ist.
    const { unmount } = zeige([position({ positionId: 1, exerciseTitle: "Pflicht" })], null);
    expect(kopf().getAttribute("aria-expanded")).toBe("false");
    expect(screen.queryByRole("link", { name: /^Pflicht/ })).toBeNull();
    unmount();
    zeige([position({ positionId: 1, exerciseTitle: "Pflicht" })], 1);
    expect(kopf().getAttribute("aria-expanded")).toBe("true");
    expect(screen.getByRole("link", { name: /^Pflicht/ })).toBeTruthy();
  });

  it("nennt im Kopf Tages- und Wochenstand und die Münzen der offenen Ziele", () => {
    // Rot, wenn der Kopf erfüllte Ziele mitzählt, die Woche in „heute“ mischt oder die Münzen weglässt.
    zeige([
      position({ positionId: 1, cadence: "Daily", goalMet: true, pointsGoalMet: 20 }),
      position({ positionId: 2, cadence: "Daily", goalMet: false, pointsGoalMet: 30 }),
      position({ positionId: 3, cadence: "Weekly", goalMet: false, pointsGoalMet: 15 }),
      position({ positionId: 4, cadence: "None", goalMet: true, pointsGoalMet: 0 }),
    ], null);
    const text = kopf().textContent ?? "";
    expect(text).toContain("Heute 1/2");
    expect(text).toContain("Woche 0/1");
    expect(text).toContain("noch 45");
  });

  it("klappt ein einziges Fach auch ohne START auf", () => {
    // Rot, wenn ein Kind mit nur einem Fach nach erfüllter Pflicht einen leeren, zugeklappten Reiter sieht.
    render(<MemoryRouter><FachKarte id="f" farbe={0} titel="Englisch" positions={[position({ goalMet: true })]} naechsteId={null} einzig /></MemoryRouter>);
    expect(kopf().getAttribute("aria-expanded")).toBe("true");
  });

  it("trennt die Zusatzübungen von der Pflicht und klappt sie erst auf Wunsch auf", () => {
    // Rot, wenn freie Positionen wieder zwischen den Pflichten im Pfad stehen.
    zeige([
      position({ positionId: 1, exerciseTitle: "Pflicht", cadence: "Daily" }),
      position({ positionId: 2, exerciseTitle: "Kür", cadence: "None", goalMet: true }),
    ], 1);
    expect(screen.queryByRole("link", { name: /^Kür/ })).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: /1 Zusatzübung/ }));
    expect(screen.getByRole("link", { name: /^Kür/ })).toBeTruthy();
  });

  it("sagt „Pflicht geschafft“, wenn alle Ziele des Fachs erfüllt sind", () => {
    // Rot, wenn ein Fach mit erfüllter Pflicht weiter offene Münzen verspricht.
    zeige([position({ positionId: 1, cadence: "Daily", goalMet: true })], null);
    expect(kopf().textContent).toContain("Pflicht geschafft");
    expect(kopf().textContent).not.toContain("noch");
  });
});
