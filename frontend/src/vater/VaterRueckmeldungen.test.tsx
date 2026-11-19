import { act, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import type { FeedbackEntry } from "../lib/types";
import { VaterRueckmeldungen } from "./VaterRueckmeldungen";

const offen: FeedbackEntry = {
  id: 11, childId: 2, childName: "Theo", studyPlanId: 5, planPositionId: 8, exerciseId: 9, exerciseTitle: "Unit 1",
  itemIndex: 0, cue: "voyage", solution: "Reise", childAnswer: "Reiße", reason: "WrongTranslation", reach: "Everyone",
  count: 1, text: "stimmt nicht", status: "Open", createdAt: "2026-11-18T20:00:00Z", lastReportedAt: "2026-11-18T20:00:00Z",
  doneAt: null,
};

/**
 * B-629 AK 4, Review-Fund: nach einem Filterwechsel stehen bis zur Antwort die alten Zeilen da – ihre Knöpfe sind gesperrt.
 * Rot, wenn die Seite `stale` nicht liest (ein Klick im Ladefenster erledigte eine Meldung des vorigen Filters).
 */
describe("VaterRueckmeldungen", () => {
  afterEach(() => vi.restoreAllMocks());

  it("sperrt die Knöpfe der alten Zeilen, solange der neue Filter lädt", async () => {
    vi.spyOn(api, "children").mockResolvedValue([]);
    const list = vi.spyOn(api, "listFeedback")
      .mockResolvedValueOnce([offen])
      .mockImplementationOnce(() => new Promise(() => {}));
    render(<AuthProvider><MemoryRouter><VaterRueckmeldungen /></MemoryRouter></AuthProvider>);
    await act(async () => {});
    const knopf = screen.getByRole("button", { name: "Erledigt" }) as HTMLButtonElement;
    expect(knopf.disabled).toBe(false);

    fireEvent.change(screen.getByLabelText("Status"), { target: { value: "Done" } });
    await act(async () => {});

    expect(list).toHaveBeenLastCalledWith(undefined, "Done");
    expect((screen.getByRole("button", { name: "Erledigt" }) as HTMLButtonElement).disabled).toBe(true);
  });
});
