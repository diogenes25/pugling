import { act, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { de } from "../i18n/de";
import { ApiError } from "../lib/api";
import { setLanguage } from "../lib/i18n";
import type { PracticeCard, ReviewOutcome } from "../lib/types";

/**
 * B-284: eine Bewertung, die die Sperre verwirft, schaltet die Karte nicht weiter. Vorher lief `next()` nach `action.run`
 * immer – auch für den verworfenen zweiten Aufruf –, und ein doppeltes Enter übersprang eine Karte.
 */
const review = vi.fn();
const startSession = vi.fn();

vi.mock("./SohnApp", () => ({
  useSohn: () => ({ planId: 3, refreshWallet: () => {}, setStreak: () => {}, celebrate: () => {} }),
}));
vi.mock("../lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("../lib/api")>();
  return {
    ...original,
    api: {
      startSession: (...a: unknown[]) => startSession(...a),
      cards: () => Promise.resolve(["eins", "zwei", "drei"].map((prompt, i) => card(i, prompt))),
      review: (...a: unknown[]) => review(...a),
      heartbeat: () => Promise.resolve(),
      endSession: () => Promise.resolve(),
      overview: () => Promise.resolve({ currentStreak: 0 }),
    },
  };
});

const { SohnPractice } = await import("./SohnPractice");

/** Eine getippte Karte (`reveal: null`) ohne Auswahl und ohne Kästchen – also mit dem freien Textfeld. */
function card(itemIndex: number, prompt: string): PracticeCard {
  return { itemIndex, prompt, reveal: null } as PracticeCard;
}

beforeEach(() => {
  review.mockReset();
  startSession.mockReset().mockResolvedValue({ id: 11, testable: true });
});

describe("SohnPractice, doppeltes Absenden", () => {
  // Rot, wenn `judge` nach einem verworfenen `action.run` wieder `next()` ruft: dann rückt der zweite Aufruf sofort eine
  // Karte weiter, der erste nach seiner Antwort noch eine – das Kind landet bei „drei", „zwei" kam nie.
  it("rückt bei zweimal Enter während der Prüfung genau eine Karte weiter", async () => {
    let answer!: (o: ReviewOutcome | null) => void;
    review.mockReturnValueOnce(new Promise((r) => { answer = r; }));
    render(
      <MemoryRouter initialEntries={["/sohn/practice/5"]}>
        <Routes><Route path="/sohn/practice/:positionId" element={<SohnPractice />} /></Routes>
      </MemoryRouter>,
    );

    const input = await screen.findByRole("textbox");
    expect(screen.getByText("eins")).toBeTruthy();
    fireEvent.change(input, { target: { value: "one" } });
    const form = input.closest("form")!;
    fireEvent.submit(form);
    fireEvent.submit(form);
    // Rot, wenn statt des laufenden der verworfene Aufruf weiterschaltet (`if (!ran)`): das Endbild wäre dasselbe. Das
    // leere `act` lässt den verworfenen Aufruf erst zu Ende laufen – ohne es prüfte die Zeile vor ihm und bliebe grün.
    await act(async () => {});
    expect(screen.getByText("eins")).toBeTruthy();
    await act(async () => { answer(null); });

    expect(await screen.findByText("zwei")).toBeTruthy();
    expect(screen.queryByText("drei")).toBeNull();
    expect(review).toHaveBeenCalledTimes(1);
  });

  // Rot, wenn das Weiterschalten in den Erfolgsweg rückt (`next()` im `try`) oder der innere Fang fehlt (`run` liefert
  // dann `false`): eine gescheiterte Bewertung ließe das Kind auf der Karte stehen.
  it("rückt auch nach einer gescheiterten Bewertung weiter", async () => {
    review.mockRejectedValueOnce(new Error("offline"));
    render(
      <MemoryRouter initialEntries={["/sohn/practice/5"]}>
        <Routes><Route path="/sohn/practice/:positionId" element={<SohnPractice />} /></Routes>
      </MemoryRouter>,
    );

    const input = await screen.findByRole("textbox");
    fireEvent.change(input, { target: { value: "one" } });
    fireEvent.submit(input.closest("form")!);

    expect(await screen.findByText("zwei")).toBeTruthy();
  });

  // B-333 (Nachschau zu B-330): nur `plan_inactive` beendet die Runde, jeder andere Server-Fehler nicht. Ein 403 mit anderem
  // Code, damit beide naheliegenden Verkürzungen fallen: `e instanceof ApiError` und `e.status === 403` statt des Codes.
  it("rückt nach einem anderen Server-Fehler weiter, auch bei 403", async () => {
    review.mockRejectedValueOnce(new ApiError(403, "Forbidden.", "trace-6", "forbidden"));
    render(
      <MemoryRouter initialEntries={["/sohn/practice/5"]}>
        <Routes><Route path="/sohn/practice/:positionId" element={<SohnPractice />} /></Routes>
      </MemoryRouter>,
    );

    const input = await screen.findByRole("textbox");
    fireEvent.change(input, { target: { value: "one" } });
    fireEvent.submit(input.closest("form")!);

    expect(await screen.findByText("zwei")).toBeTruthy();
  });

  // B-330: anders als ein Netzfehler endet ein Plan, der mitten in der Runde nicht mehr dran ist, die Runde – sonst blättert
  // das Kind weiter, ohne dass etwas zählt. Rot, wenn der Fang den Code wieder verschluckt oder danach doch weiterschaltet
  // (dann steht „zwei" statt der Meldung da). Welcher Formatierer den Satz liefert, trägt `arcadeSpielweg.test.tsx`.
  it("beendet die Runde mit dem Satz der Arcade, wenn der Plan nicht mehr dran ist", async () => {
    await setLanguage("de");
    review.mockRejectedValueOnce(new ApiError(403, "This study plan is not currently active. Ask your parent.", "trace-5", "plan_inactive"));
    render(
      <MemoryRouter initialEntries={["/sohn/practice/5"]}>
        <Routes><Route path="/sohn/practice/:positionId" element={<SohnPractice />} /></Routes>
      </MemoryRouter>,
    );

    const input = await screen.findByRole("textbox");
    fireEvent.change(input, { target: { value: "one" } });
    fireEvent.submit(input.closest("form")!);

    expect(await screen.findByText(de.sohn.problem.plan_inactive)).toBeTruthy();
    expect(screen.queryByText("zwei")).toBeNull();
  });
});

/** B-326: wie in der Klausur – ein Plan, den es nicht mehr gibt, führt zur Basis. */
describe("SohnPractice mit einem Plan, den es nicht mehr gibt", () => {
  function renderWithHome() {
    return render(
      <MemoryRouter initialEntries={["/sohn/practice/5"]}>
        <Routes>
          <Route path="/sohn/practice/:positionId" element={<SohnPractice />} />
          <Route path="/sohn" element={<p>BASIS</p>} />
        </Routes>
      </MemoryRouter>,
    );
  }

  // Rot, wenn der Start-Fehler wieder als Meldung steht.
  it("führt zur Basis statt die Serverzeile zu zeigen", async () => {
    startSession.mockRejectedValue(new ApiError(404, "Study plan not found.", "trace-9", "not_found"));
    renderWithHome();

    expect(await screen.findByText("BASIS")).toBeTruthy();
    // Absichtserklärung, kein eigener Beleg: ist die Basis da, ist der Bildschirm schon weg – rot macht ihn nur, was die
    // Zeile davor schon rot macht.
    expect(screen.queryByText(/Study plan not found/)).toBeNull();
  });

  // Rot, wenn jeder Start-Fehler zur Basis führt.
  it("zeigt einen anderen Start-Fehler weiter an", async () => {
    startSession.mockRejectedValue(new ApiError(500, "Server kaputt.", "trace-8"));
    renderWithHome();

    expect(await screen.findByText("Server kaputt. (Ref: trace-8)")).toBeTruthy();
  });
});
