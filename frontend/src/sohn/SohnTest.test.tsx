import { act, fireEvent, render, screen } from "@testing-library/react";
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
const submitTest = vi.fn();

// Ein echter Skin: der Ergebnis-Bildschirm zeichnet das Maskottchen (der Neustart-Fall erreicht ihn).
const { SKINS } = await import("../lib/skins");
vi.mock("./SohnApp", () => ({
  useSohn: () => ({ planId: 3, refreshWallet: () => {}, setStreak: () => {}, skin: SKINS[0], celebrate: () => {} }),
}));
vi.mock("../lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("../lib/api")>();
  return {
    ...original,
    api: {
      startTest: (...a: unknown[]) => startTest(...a),
      nextTest: (...a: unknown[]) => nextTest(...a),
      answerTest: (...a: unknown[]) => answerTest(...a),
      submitTest: (...a: unknown[]) => submitTest(...a),
      overview: () => Promise.resolve({ currentStreak: 0 }),
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
    // Gegenstück zum Fall darunter: ein vorübergehender Fehler lässt sich fortsetzen.
    expect(screen.getByRole("button", { name: "Weiter versuchen" })).toBeTruthy();
  });

  // B-330: ist der Plan nicht mehr dran, holt „Weiter versuchen“ nur dieselbe 403 – der Knopf wäre eine Falle. Rot, wenn er
  // auch hier steht.
  // Derselbe Fall beim Neustart aus dem Ergebnis: dort steht noch die Id des abgegebenen Versuchs. Rot, wenn `start()` sie
  // stehen lässt – dann böte „Weiter versuchen“ den alten Versuch an und holte nur wieder die 403 (Delta-Review B-330).
  it("bietet nach einem Neustart, den der Plan nicht mehr erlaubt, kein Weiter an", async () => {
    await setLanguage("de");
    nextTest.mockReset().mockResolvedValueOnce({ item: null, done: true, cursor: 0, total: 0 });
    submitTest.mockResolvedValueOnce({ passed: false, scorePercent: 0, passPercent: 80, correctItems: 0, totalItems: 0, stage: 3, items: [], attemptsRemaining: 1 });
    renderTest();

    const retry = await screen.findByRole("button", { name: "Nochmal versuchen" });
    startTest.mockRejectedValueOnce(new ApiError(403, "This study plan is not currently active. Ask your parent.", "trace-7", "plan_inactive"));
    fireEvent.click(retry);

    expect(await screen.findByText("Dieser Lernplan ist gerade nicht dran. Frag deine Eltern, welcher Plan jetzt gilt.")).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Weiter versuchen" })).toBeNull();
  });

  // Und nach einem Fortsetzen: erst ein vorübergehender Fehler, dann ist der Plan nicht mehr dran. Rot, wenn `resume()` den
  // Code nicht merkt – dann stünde der Knopf weiter da.
  it("nimmt das Weiter weg, wenn das Fortsetzen auf einen Plan trifft, der nicht mehr dran ist", async () => {
    await setLanguage("de");
    answerTest.mockRejectedValueOnce(new ApiError(500, "Server kaputt.", "trace-1"));
    renderTest();
    fireEvent.click(await screen.findByRole("button", { name: "das Pferd" }));
    const resume = await screen.findByRole("button", { name: "Weiter versuchen" });

    nextTest.mockRejectedValueOnce(new ApiError(403, "This study plan is not currently active. Ask your parent.", "trace-8", "plan_inactive"));
    fireEvent.click(resume);

    expect(await screen.findByText("Dieser Lernplan ist gerade nicht dran. Frag deine Eltern, welcher Plan jetzt gilt.")).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Weiter versuchen" })).toBeNull();
  });

  it("bietet bei einem Plan, der nicht mehr dran ist, kein Weiter an", async () => {
    await setLanguage("de");
    answerTest.mockRejectedValueOnce(new ApiError(403, "This study plan is not currently active. Ask your parent.", "trace-6", "plan_inactive"));
    renderTest();

    fireEvent.click(await screen.findByRole("button", { name: "das Pferd" }));

    expect(await screen.findByText("Dieser Lernplan ist gerade nicht dran. Frag deine Eltern, welcher Plan jetzt gilt.")).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Weiter versuchen" })).toBeNull();
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

  // B-325 (AK 3 von B-320). Rot, wenn `answerAndAdvance` die Sperre verliert (`action.run(fn)` → `fn()`) oder die
  // Ref-Prüfung in `useAction.run` fehlt: dann schickt ein Doppelklick zwei Antworten auf dieselbe Cursor-Frage. Die
  // Antwort hängt, bis beide Klicks durch sind.
  it("schickt bei einem Doppelklick eine Antwort und zeigt nichts", async () => {
    let ack!: (v: { done: boolean }) => void;
    answerTest.mockReturnValueOnce(new Promise((r) => { ack = r; }));
    nextTest.mockResolvedValueOnce({ item, done: false, cursor: 0, total: 2 })
      .mockResolvedValueOnce({ item: { ...item, itemIndex: 1, prompt: "the sheep" }, done: false, cursor: 1, total: 2 });
    renderTest();

    const button = await screen.findByRole("button", { name: "das Pferd" });
    // Beide Klicks in EINEM `act`: dazwischen rendert React nicht, `disabled={action.busy}` steht also noch nicht am Knopf –
    // nur so trifft der zweite Klick die Ref-Sperre selbst (gemessen: mit zwei einzelnen `fireEvent` fing `disabled` ihn ab,
    // und die Mutante „Ref-Prüfung weg“ blieb grün).
    act(() => { fireEvent.click(button); fireEvent.click(button); });
    await act(async () => { ack({ done: false }); });

    expect(await screen.findByText("the sheep")).toBeTruthy();
    expect(answerTest).toHaveBeenCalledTimes(1);
    // Fällt nur als Nebenwirkung der Mutante (die zweite, ungemockte Antwort wirft) – kein eigener Beleg für „zeigt nichts“.
    expect(document.querySelector(".error-box")).toBeNull();
  });
});

/** B-326: nach einem Reload mit einer Plan-Id, die es nicht mehr gibt, geht es zur Basis – die korrigiert die Wahl. */
describe("SohnTest mit einem Plan, den es nicht mehr gibt", () => {
  // Rot, wenn der Start-Fehler wieder als Meldung steht: dann liest das Kind „Study plan not found. (Ref: …)“.
  it("führt zur Basis statt die Serverzeile zu zeigen", async () => {
    startTest.mockReset().mockRejectedValue(new ApiError(404, "Study plan not found.", "trace-9", "not_found"));
    render(
      <MemoryRouter initialEntries={["/sohn/test/5"]}>
        <Routes>
          <Route path="/sohn/test/:positionId" element={<SohnTest />} />
          <Route path="/sohn" element={<p>BASIS</p>} />
        </Routes>
      </MemoryRouter>,
    );

    expect(await screen.findByText("BASIS")).toBeTruthy();
    // Absichtserklärung, kein eigener Beleg: ist die Basis da, ist der Bildschirm schon weg – rot macht ihn nur, was die
    // Zeile davor schon rot macht.
    expect(screen.queryByText(/Study plan not found/)).toBeNull();
  });

  // Rot, wenn jeder Start-Fehler zur Basis führt: ein Serverfehler ist keine verschwundene Klausur, das Kind braucht die
  // Meldung (B-320) – nur 404 heißt „gibt es nicht mehr“.
  it("zeigt einen anderen Start-Fehler weiter an", async () => {
    startTest.mockReset().mockRejectedValue(new ApiError(500, "Server kaputt.", "trace-8"));
    renderTest();

    expect(await screen.findByText("Server kaputt. (Ref: trace-8)")).toBeTruthy();
  });
});
