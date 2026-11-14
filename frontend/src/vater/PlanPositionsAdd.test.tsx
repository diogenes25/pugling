import { act, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import type { ExerciseSummary, Paged } from "../lib/types";
import { PlanPositions } from "./PlanPositions";

/*
 * B-534 (Review): die Rückmeldung des Hinzufügens landet am Hinzufügen, nicht über der Tabelle. In einem langen Plan
 * stand „hinzugefügt" bzw. ein 409 sonst außer Sicht. Gefälscht wird der Abruf, nicht das `fetch`.
 */
const EX = {
  id: 5, type: "Cloze", title: "Lückentext 1", defaultUseLeitner: false, defaultRequireTypedTest: false,
  defaultItemCount: null, gradeMin: null, gradeMax: null, categoryName: null, source: null, description: null,
  averageRating: null, ratingCount: 0, itemCount: 3,
} as unknown as ExerciseSummary;

describe("PlanPositions – Hinzufügen", () => {
  afterEach(() => vi.restoreAllMocks());

  // Rot, solange `AddPosition` die geteilte Aktion der Zeilen benutzt: dann steht die Meldung im Banner über der
  // Tabelle, außerhalb des Panels.
  it.each([
    ["Erfolg", () => Promise.resolve({}), "Übung als Position hinzugefügt."],
    ["409", () => Promise.reject(new Error("Die Übung steckt schon im Plan.")), "Die Übung steckt schon im Plan."],
  ])("zeigt die Meldung (%s) im Hinzufügen-Panel", async (_fall, antwort, text) => {
    vi.spyOn(api, "positions").mockResolvedValue([]);
    vi.spyOn(api, "subjects").mockResolvedValue([]);
    vi.spyOn(api, "exerciseTypes").mockResolvedValue([]);
    vi.spyOn(api, "textbookSeries").mockResolvedValue([]);
    vi.spyOn(api, "searchExercises").mockResolvedValue({ items: [EX], total: 1 } as unknown as Paged<ExerciseSummary>);
    vi.spyOn(api, "addPosition").mockImplementation(antwort as never);
    render(<AuthProvider><PlanPositions planId={1} /></AuthProvider>);

    // Leerer Plan: das Panel ist offen.
    fireEvent.click(await screen.findByRole("radio", { name: /Lückentext 1/ }));
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "+ Position hinzufügen" })); });

    const meldung = await screen.findByText(text);
    expect(meldung.closest(".add-panel")).not.toBeNull();
  });
});

describe("PlanPositions – Lesefehler", () => {
  afterEach(() => vi.restoreAllMocks());

  // B-183. Rot, solange der Ausfall ein nacktes `banner err` ist: dann gibt es keinen „Erneut laden"-Knopf, und der
  // Vater käme nur per F5 an seine Positionen.
  it("bietet nach gescheitertem Laden „Erneut laden“ und zeigt danach die Positionen", async () => {
    const positions = vi.spyOn(api, "positions")
      .mockRejectedValueOnce(new Error("Netz weg"))
      .mockResolvedValue([]);
    vi.spyOn(api, "subjects").mockResolvedValue([]);
    vi.spyOn(api, "exerciseTypes").mockResolvedValue([]);
    vi.spyOn(api, "textbookSeries").mockResolvedValue([]);
    vi.spyOn(api, "searchExercises").mockResolvedValue({ items: [], total: 0 } as unknown as Paged<ExerciseSummary>);
    render(<AuthProvider><PlanPositions planId={1} /></AuthProvider>);
    // Den abgelehnten Abruf erst durchlaufen lassen (B-284), sonst steht noch „Lade Positionen…".
    await act(async () => {});

    await act(async () => { fireEvent.click(await screen.findByRole("button", { name: "Erneut laden" })); });

    expect(positions).toHaveBeenCalledTimes(2);
    expect(await screen.findByText(/Noch keine Übungen im Plan/)).toBeTruthy();
  });
});
