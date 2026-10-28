import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "../lib/api";
import { setLanguage } from "../lib/i18n";
import type { TestItem } from "../lib/types";

/**
 * B-320: scheitert das Beantworten einer Klausurfrage, sieht das Kind die Meldung. Vorher las der Bildschirm sie aus
 * `action.message` des Renders, in dem der Klick entstand – beim ersten Fehlschlag `null`, also nichts.
 */
const startTest = vi.fn();
const nextTest = vi.fn();
const answerTest = vi.fn();

vi.mock("./SohnApp", () => ({
  useSohn: () => ({ planId: 3, refreshWallet: () => {}, setStreak: () => {}, skin: undefined, celebrate: () => {} }),
}));
vi.mock("../lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("../lib/api")>();
  return {
    ...original,
    api: {
      startTest: (...a: unknown[]) => startTest(...a),
      nextTest: (...a: unknown[]) => nextTest(...a),
      answerTest: (...a: unknown[]) => answerTest(...a),
    },
  };
});

const { SohnTest } = await import("./SohnTest");

const item: TestItem = { itemIndex: 0, prompt: "the horse", stage: 3, reveal: null, choices: ["das Pferd", "das Schaf"] };

beforeEach(() => {
  startTest.mockReset().mockResolvedValue({ attemptId: 9, stage: 3, totalItems: 2 });
  nextTest.mockReset().mockResolvedValue({ item, done: false, cursor: 0, total: 2 });
  answerTest.mockReset();
});

function renderTest() {
  return render(
    <MemoryRouter initialEntries={["/sohn/test/5"]}>
      <Routes><Route path="/sohn/test/:positionId" element={<SohnTest />} /></Routes>
    </MemoryRouter>,
  );
}

describe("SohnTest, Fehler beim Beantworten", () => {
  afterEach(async () => { await setLanguage("de"); });

  // Rot, wenn der Fehler wieder aus `action.message` des Klick-Renders gelesen wird: beim ersten Fehlschlag ist der
  // `null`, und der Bildschirm bleibt bei der Frage stehen.
  it("zeigt den ersten Fehlschlag", async () => {
    answerTest.mockRejectedValueOnce(new ApiError(500, "Server kaputt.", "trace-1"));
    renderTest();

    fireEvent.click(await screen.findByRole("button", { name: "das Pferd" }));

    expect(await screen.findByText("Server kaputt. (Ref: trace-1)")).toBeTruthy();
  });

  // Rot, wenn der Fang den deutschen `errorMessage` statt des Arcade-Formatierers nimmt: dann stünde der deutsche Satz in
  // der englischen Arcade. Der Fall oben sieht das nicht – für einen Fehler ohne Code sagen beide dasselbe.
  it("meldet in der englischen Arcade den englischen Satz", async () => {
    await setLanguage("en");
    answerTest.mockRejectedValueOnce(new ApiError(429, "No attempts left.", "trace-2", "test_attempts_exhausted"));
    renderTest();

    fireEvent.click(await screen.findByRole("button", { name: "das Pferd" }));

    expect(await screen.findByText("You've used up your tries for today. You can have another go tomorrow.")).toBeTruthy();
  });

  // Rot, wenn das Ergebnis der Antwort nicht mehr den Zähler der Aufgabe weiterrückt – Kontrolle, dass der Erfolgsweg
  // durch den Umbau des Fehlerwegs nicht mitgerissen wird (die Antwort lief vorher wie nachher in `action.run`).
  it("rückt nach einer angenommenen Antwort zur nächsten Frage", async () => {
    answerTest.mockResolvedValueOnce({ done: false });
    nextTest.mockResolvedValueOnce({ item, done: false, cursor: 0, total: 2 })
      .mockResolvedValueOnce({ item: { ...item, itemIndex: 1, prompt: "the sheep" }, done: false, cursor: 1, total: 2 });
    renderTest();

    fireEvent.click(await screen.findByRole("button", { name: "das Pferd" }));

    expect(await screen.findByText("the sheep")).toBeTruthy();
  });
});
