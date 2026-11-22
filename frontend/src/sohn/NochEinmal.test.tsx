import { render, screen, act } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { de } from "../i18n/de";
import { setLanguage } from "../lib/i18n";

/**
 * B-660: der Leerzustand des Übens führt weiter. Je Lage des Bonus-Trainings ein Fall; jeder nennt die Änderung, die ihn
 * rot macht. Die Lagen unterscheiden sich in den Werten, die der Fall setzt (Sperre, Pool, Wert, Takt), damit kein Fall
 * nur den Ausgangszustand prüft.
 */
const overview = vi.fn();
const extraTraining = vi.fn();

vi.mock("./SohnApp", () => ({ useSohn: () => ({ childId: 7 }) }));
vi.mock("../lib/auth", () => ({ useAuth: () => ({ signOut: () => {} }) }));
vi.mock("../lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("../lib/api")>();
  return {
    ...original,
    api: {
      overview: (...a: unknown[]) => overview(...a),
      extraTraining: (...a: unknown[]) => extraTraining(...a),
    },
  };
});

const { NochEinmal } = await import("./NochEinmal");

// Position 5 übt Übung 42, Position 6 Übung 99: wer die Position nicht auf ihre Übung abbildet, greift die falsche.
const POSITIONS = [{ positionId: 6, exerciseId: 99 }, { positionId: 5, exerciseId: 42 }];
const exercise = (exerciseId: number, nextRunPercent: number, extra: Partial<{ done: boolean; repeatable: boolean }> = {}) =>
  ({ exerciseId, nextRunPercent, done: false, repeatable: true, ...extra });
const offen = (exercises: unknown[], deckel: { coinsToday: number; dailyCoinCap: number | null } = { coinsToday: 0, dailyCoinCap: 90 }) =>
  ({ unlocked: true, outstanding: [], rewardCadence: "Daily", exercises, ...deckel });

beforeEach(async () => {
  await setLanguage("de");
  overview.mockReset().mockResolvedValue({ currentStreak: 0, today: { positions: POSITIONS } });
  extraTraining.mockReset();
});

async function zeige() {
  const view = render(<MemoryRouter><NochEinmal planId={3} positionId={5} /></MemoryRouter>);
  await act(async () => {});
  return view;
}

describe("NochEinmal, Sperre offen", () => {
  // AK 1/2. Rot, wenn der Block die Übung der Position nicht findet (dann der Wert von Übung 99, 25 %) oder nicht verlinkt.
  it("führt ins Bonus-Training mit genau dieser Übung und nennt ihren heutigen Wert", async () => {
    extraTraining.mockResolvedValue(offen([exercise(99, 25), exercise(42, 50)]));
    await zeige();

    expect(screen.getByRole("link", { name: de.sohn.practice.againPlay }).getAttribute("href")).toBe("/sohn/bonus/42");
    expect(screen.getByText(/heute noch 50 % wert/)).toBeTruthy();
    expect(overview).toHaveBeenCalledWith(3);
  });

  // AK 2, Wert 0: weiter üben ja, Münzen nein. Rot, wenn der Satz bei 0 % einen Wert verspricht.
  it("sagt bei 0 % ehrlich, dass es heute keine Münzen mehr gibt", async () => {
    extraTraining.mockResolvedValue(offen([exercise(42, 0, { done: true, repeatable: true })]));
    await zeige();

    expect(screen.getByText(de.sohn.practice.againNoCoins)).toBeTruthy();
    expect(screen.queryByText(/% wert/)).toBeNull();
    expect(screen.getByRole("link", { name: de.sohn.practice.againPlay })).toBeTruthy();
  });

  // Rot, wenn eine feste Übung, die heute nur noch ihre Lösungen zeigte, trotzdem einen Knopf bekommt (Muster Bonus-Liste).
  it("bietet eine heute erledigte feste Übung nicht an", async () => {
    extraTraining.mockResolvedValue(offen([exercise(42, 0, { done: true, repeatable: false })]));
    const { container } = await zeige();

    expect(container.textContent).toBe("");
  });

  // Eine angefangene Runde wird fortgesetzt. Rot, wenn der Knopf dann „Noch einmal“ sagt.
  it("sagt bei einer angefangenen Runde „Weiterspielen“", async () => {
    extraTraining.mockResolvedValue(offen([{ ...exercise(42, 50), openRunId: 17 }]));
    await zeige();

    expect(screen.getByRole("link", { name: de.sohn.bonus.resume }).getAttribute("href")).toBe("/sohn/bonus/42");
  });

  // Voller Tagesdeckel: die Übung wäre noch 100 % wert, zahlt aber nichts. Rot, wenn der Block dann Münzen verspricht.
  it("verspricht bei vollem Tagesdeckel keine Münzen, lässt Vokabeln aber weiter üben", async () => {
    extraTraining.mockResolvedValue(offen([exercise(42, 100)], { coinsToday: 90, dailyCoinCap: 90 }));
    await zeige();

    expect(screen.getByText(de.sohn.practice.againNoCoins)).toBeTruthy();
    expect(screen.queryByText(/% wert/)).toBeNull();
    expect(screen.getByRole("link", { name: de.sohn.practice.againPlay })).toBeTruthy();
  });

  // Voller Tagesdeckel und eine feste Übung: sie wartet bis morgen (sie zeigte sonst nur die Lösungen). Rot, wenn sie trotzdem
  // einen Knopf bekommt.
  it("bietet eine feste Übung bei vollem Tagesdeckel nicht an", async () => {
    extraTraining.mockResolvedValue(offen([exercise(42, 100, { repeatable: false })], { coinsToday: 90, dailyCoinCap: 90 }));
    const { container } = await zeige();

    expect(container.textContent).toBe("");
  });

  // B-533 Entscheidung 6: eine Nicht-Vokabel-Pflicht liegt nicht im Pool. Rot, wenn der Block dann irgendeine Übung
  // des Pools anbietet (hier liegt nur Übung 99 darin).
  it("bleibt leer, wenn die Übung nicht im Bonus-Pool liegt", async () => {
    extraTraining.mockResolvedValue(offen([exercise(99, 100)]));
    const { container } = await zeige();

    expect(container.textContent).toBe("");
  });
});

describe("NochEinmal, Sperre zu", () => {
  // AK 1, geschlossene Sperre: wie viele Ziele fehlen. Rot, wenn der Satz fehlt oder nicht zählt.
  it("nennt, wie viele Tagesziele noch fehlen", async () => {
    extraTraining.mockResolvedValue({ unlocked: false, outstanding: ["A", "B"], rewardCadence: "Daily", exercises: [exercise(42, 100)] });
    await zeige();

    expect(screen.getByText(/Noch 2 Tagesziele offen/)).toBeTruthy();
    expect(screen.queryByRole("link")).toBeNull();
  });

  // Rot, wenn eine Woche ohne Tagesziele als Tagesziele angesagt wird (der Server sperrt dann über die Wochenziele).
  it("spricht bei reinen Wochenzielen von Wochenzielen", async () => {
    extraTraining.mockResolvedValue({ unlocked: false, outstanding: ["A"], rewardCadence: "Weekly", exercises: [exercise(42, 100)] });
    await zeige();

    expect(screen.getByText(/Noch 1 Wochenziel offen/)).toBeTruthy();
  });

  // Review B-660: der Server liefert den Pool auch bei Sperre. Eine Übung, die nie darin liegt, bekommt keinen Satz, der nach
  // der Sperre eine bezahlte Wiederholung verspricht. Rot, wenn die Sperre vor dem Pool geprüft wird.
  it("verspricht bei Sperre nichts für eine Übung außerhalb des Pools", async () => {
    extraTraining.mockResolvedValue({ unlocked: false, outstanding: ["A"], rewardCadence: "Daily", exercises: [exercise(99, 100)] });
    const { container } = await zeige();

    expect(container.textContent).toBe("");
  });

  // Rot, wenn der Block ohne offene Ziele trotzdem „noch 0 offen“ verspricht.
  it("bleibt leer, wenn die Sperre keine offenen Ziele nennt", async () => {
    extraTraining.mockResolvedValue({ unlocked: false, outstanding: [], rewardCadence: "None", exercises: [exercise(42, 100)] });
    const { container } = await zeige();

    expect(container.textContent).toBe("");
  });
});

// Beiwerk: ein Ladefehler belegt den Leerzustand nicht mit einem Fehlerkasten. Rot, wenn er gezeigt wird.
it("lässt den Block bei einem Ladefehler still weg", async () => {
  extraTraining.mockRejectedValue(new Error("weg"));
  const { container } = await zeige();

  expect(container.textContent).toBe("");
});
