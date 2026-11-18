import { act, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { DEFAULT_SKIN } from "../lib/skins";
import type { ItemOutcome, TestSubmitResponse } from "../lib/types";
import { KartenMeldung, MeldungsHinweis } from "./KartenMeldung";
import { TestResult } from "./SohnTest";

/**
 * B-629: der Rückmelde-Knopf an der Karte, sein Formular und der Hinweis nach „erledigt“. Je Fall steht, welche Änderung
 * ihn rot macht.
 */
describe("KartenMeldung", () => {
  afterEach(() => vi.restoreAllMocks());

  // AK 1: rot, wenn der Knopf fehlt, weniger als acht Gründe kommen oder Absenden ohne Grund und Text frei ist.
  it("öffnet acht Gründe und sperrt Absenden ohne Grund und Text", () => {
    render(<KartenMeldung planId={3} positionId={7} itemIndex={1} />);
    fireEvent.click(screen.getByRole("button", { name: /Rückmeldung/ }));

    expect(screen.getAllByRole("button", { pressed: false })).toHaveLength(8);
    const senden = screen.getByRole("button", { name: "Absenden" }) as HTMLButtonElement;
    expect(senden.disabled).toBe(true);
    fireEvent.change(screen.getByLabelText("Willst du noch etwas dazu schreiben?"), { target: { value: "  " } });
    expect(senden.disabled).toBe(true);
    fireEvent.change(screen.getByLabelText("Willst du noch etwas dazu schreiben?"), { target: { value: "komisch" } });
    expect(senden.disabled).toBe(false);
  });

  // AK 1 + AK 3: rot, wenn die Auswahl nicht mehrfach geht, die Karte nicht mitreist oder nach dem Absenden kein Dank steht.
  it("schickt mehrere Gründe mit der Karte und dankt danach", async () => {
    const report = vi.spyOn(api, "reportFeedback").mockResolvedValue({ ids: [1, 2] });
    render(<KartenMeldung planId={3} positionId={7} itemIndex={1} />);
    fireEvent.click(screen.getByRole("button", { name: /Rückmeldung/ }));
    fireEvent.click(screen.getByRole("button", { name: "Falsch übersetzt" }));
    fireEvent.click(screen.getByRole("button", { name: "Zu schwer" }));
    expect(screen.getByRole("button", { name: "Falsch übersetzt" }).getAttribute("aria-pressed")).toBe("true");

    fireEvent.click(screen.getByRole("button", { name: "Absenden" }));
    await act(async () => {});

    expect(report).toHaveBeenCalledWith(3, 7, { reasons: ["WrongTranslation", "TooHard"], text: null, itemIndex: 1 });
    expect(screen.getByRole("status").textContent).toBe("Danke, ist angekommen!");
    expect(screen.queryByRole("button", { name: /Rückmeldung/ })).toBeNull();
  });

  // Risiko 1: rot, wenn ein Tastendruck im Formular die Arcade erreicht (Enter würde dort eine Antwort abschicken).
  it("lässt keinen Tastendruck aus dem Formular hinaus", () => {
    const arcade = vi.fn();
    render(<div onKeyDown={arcade}><KartenMeldung planId={3} positionId={7} itemIndex={0} /></div>);
    fireEvent.click(screen.getByRole("button", { name: /Rückmeldung/ }));
    fireEvent.keyDown(screen.getByLabelText("Willst du noch etwas dazu schreiben?"), { key: "Enter" });
    expect(arcade).not.toHaveBeenCalled();
  });
});

describe("TestResult mit Rückmeldung (B-629 AK 2)", () => {
  const result: TestSubmitResponse = {
    attemptId: 1, stage: 4, totalItems: 2, correctItems: 1, scorePercent: 50, passed: false, passPercent: 80,
    attemptsRemaining: 1, wrongMentions: [],
    items: [
      { itemIndex: 0, prompt: "hello", expected: "hallo", givenAnswer: "halo", wasCorrect: false, gapIndex: null },
      { itemIndex: 1, prompt: "goodbye", expected: "tschüss", givenAnswer: "tschüss", wasCorrect: true, gapIndex: null },
    ] satisfies ItemOutcome[],
  };

  // Rot, wenn das Ergebnisblatt keinen Knopf je Karte trägt – oder ihn auch ohne Plan (Vorschau) zeigt.
  it("trägt je Karte einen Knopf, nur mit Plan und Position", () => {
    const { unmount } = render(<TestResult result={result} skin={DEFAULT_SKIN} onHome={() => {}} onRetry={() => {}}
      onPractice={() => {}} rueckmeldung={{ planId: 3, positionId: 7 }} />);
    expect(screen.getAllByRole("button", { name: /Rückmeldung/ })).toHaveLength(2);
    unmount();

    render(<TestResult result={result} skin={DEFAULT_SKIN} onHome={() => {}} onRetry={() => {}} onPractice={() => {}} />);
    expect(screen.queryByRole("button", { name: /Rückmeldung/ })).toBeNull();
  });
});

describe("MeldungsHinweis (B-629 AK 3)", () => {
  afterEach(() => vi.restoreAllMocks());

  // Rot, wenn der Hinweis das Wort der Karte nicht nennt, nach „Alles klar“ stehen bleibt oder nicht quittiert wird.
  it("zeigt den erledigten Fall einmal und quittiert ihn", async () => {
    vi.spyOn(api, "feedbackNotices").mockResolvedValue([
      { id: 5, exerciseId: 9, exerciseTitle: "Unité 5", itemIndex: 2, cue: "voyage", reason: "WrongTranslation", doneAt: "2026-11-18T20:00:00Z" },
    ]);
    const ack = vi.spyOn(api, "ackFeedbackNotice").mockResolvedValue(undefined);
    render(<MeldungsHinweis childId={4} />);
    await act(async () => {});

    expect(screen.getByText("Deine Meldung zu „voyage“ wurde erledigt ✔")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Alles klar" }));
    await act(async () => {});

    expect(ack).toHaveBeenCalledWith(4, 5);
    expect(screen.queryByText(/wurde erledigt/)).toBeNull();
  });
});
