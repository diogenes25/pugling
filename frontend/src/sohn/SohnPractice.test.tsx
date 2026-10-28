import { act, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { PracticeCard, ReviewOutcome } from "../lib/types";

/**
 * B-284: eine Bewertung, die die Sperre verwirft, schaltet die Karte nicht weiter. Vorher lief `next()` nach `action.run`
 * immer – auch für den verworfenen zweiten Aufruf –, und ein doppeltes Enter übersprang eine Karte.
 */
const review = vi.fn();

vi.mock("./SohnApp", () => ({
  useSohn: () => ({ planId: 3, refreshWallet: () => {}, setStreak: () => {}, celebrate: () => {} }),
}));
vi.mock("../lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("../lib/api")>();
  return {
    ...original,
    api: {
      startSession: () => Promise.resolve({ id: 11, testable: true }),
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
});
