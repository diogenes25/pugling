import { act, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, useLocation } from "react-router-dom";
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

  // B-950: der Link der Startseite wählt das Kind vor. Rot, wenn die Seite `?childId=` nicht liest (sie fragt dann alle Kinder ab).
  it("wählt das Kind aus dem Link vor", async () => {
    vi.spyOn(api, "children").mockResolvedValue([{ id: 2, name: "Theo" }] as never);
    const list = vi.spyOn(api, "listFeedback").mockResolvedValue([offen]);
    render(<AuthProvider><MemoryRouter initialEntries={["/vater/rueckmeldungen?childId=2"]}><VaterRueckmeldungen /></MemoryRouter></AuthProvider>);
    await act(async () => {});
    expect(list).toHaveBeenCalledWith(2, "Open");
  });

  // Review-Fund: ein Kind, das es nicht gibt, gilt als „alle Kinder“ – sonst zeigte die Auswahl „alle“, während die Liste
  // nach einer fremden Id filtert. Rot, wenn die Seite die Id ungeprüft übernimmt.
  it("nimmt ein unbekanntes Kind aus dem Link als „alle Kinder“", async () => {
    vi.spyOn(api, "children").mockResolvedValue([{ id: 5, name: "Fritz" }] as never);
    const list = vi.spyOn(api, "listFeedback").mockResolvedValue([]);
    render(<AuthProvider><MemoryRouter initialEntries={["/vater/rueckmeldungen?childId=2"]}><VaterRueckmeldungen /></MemoryRouter></AuthProvider>);
    await act(async () => {});
    expect(list).toHaveBeenLastCalledWith(undefined, "Open");
    // Rot, wenn die Seite vor dem Laden der Kinder schon mit der ungeprüften Id fragt.
    expect(list).not.toHaveBeenCalledWith(2, "Open");
    expect((screen.getByLabelText("Kind") as HTMLSelectElement).value).toBe("");
  });

  // Review-Fund: scheitert das Laden der Kinder, kann die Auswahl nur „alle Kinder“ zeigen – die Liste muss dazu passen.
  // Rot, wenn die Seite dann bei der Id aus dem Link bleibt oder gar nicht mehr fragt.
  it("fragt alle Kinder ab, wenn die Kinder nicht laden", async () => {
    vi.spyOn(api, "children").mockRejectedValue(new Error("kaputt"));
    const list = vi.spyOn(api, "listFeedback").mockResolvedValue([]);
    render(<AuthProvider><MemoryRouter initialEntries={["/vater/rueckmeldungen?childId=2"]}><VaterRueckmeldungen /></MemoryRouter></AuthProvider>);
    await act(async () => {});
    expect(list).toHaveBeenLastCalledWith(undefined, "Open");
    expect(list).not.toHaveBeenCalledWith(2, "Open");
  });

  // Review-Fund: die Auswahl schreibt in die URL zurück, „alle Kinder“ entfernt den Parameter. Rot, wenn der Wechsel nur im
  // State lebt (F5 oder Zurück brächte das alte Kind wieder).
  it("schreibt die Auswahl in die URL", async () => {
    vi.spyOn(api, "children").mockResolvedValue([{ id: 2, name: "Theo" }] as never);
    vi.spyOn(api, "listFeedback").mockResolvedValue([]);
    let ort = "";
    const Ort = () => { ort = useLocation().search; return null; };
    render(<AuthProvider><MemoryRouter initialEntries={["/vater/rueckmeldungen?childId=2"]}><VaterRueckmeldungen /><Ort /></MemoryRouter></AuthProvider>);
    await act(async () => {});
    fireEvent.change(screen.getByLabelText("Kind"), { target: { value: "" } });
    await act(async () => {});
    expect(ort).toBe("");
  });
});
