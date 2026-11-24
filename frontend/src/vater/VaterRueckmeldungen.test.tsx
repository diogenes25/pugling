import { act, fireEvent, render, screen, within } from "@testing-library/react";
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
  doneAt: null, testItemResultId: null, testAnswerCorrect: null, outcome: null,
};

// B-767: eine Meldung, die auf eine im Test falsch gewertete Antwort zeigt.
const mitTestAntwort: FeedbackEntry = { ...offen, id: 12, reason: "TooStrict", testItemResultId: 77, testAnswerCorrect: false };

/**
 * B-629 AK 4, Review-Fund: nach einem Filterwechsel stehen bis zur Antwort die alten Zeilen da – ihre Knöpfe sind gesperrt.
 * Rot, wenn die Seite `stale` nicht liest (ein Klick im Ladefenster erledigte eine Meldung des vorigen Filters).
 */
describe("VaterRueckmeldungen", () => {
  afterEach(() => { vi.restoreAllMocks(); vi.unstubAllGlobals(); });

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

  // B-769 AK 1: das Urteil gibt es nur für eine Meldung mit Test-Antwort, ohne sie bleibt „Erledigt“. Rot, wenn „Berechtigt“
  // auch ohne Bezug erscheint oder mit Bezug fehlt.
  it("zeigt Berechtigt und Abgewiesen nur an einer Meldung mit Test-Antwort", async () => {
    vi.spyOn(api, "children").mockResolvedValue([]);
    vi.spyOn(api, "listFeedback").mockResolvedValue([offen, mitTestAntwort]);
    render(<AuthProvider><MemoryRouter><VaterRueckmeldungen /></MemoryRouter></AuthProvider>);
    await act(async () => {});

    const zeilen = screen.getAllByRole("row").slice(1);
    expect(within(zeilen[0]).getByRole("button", { name: "Erledigt" })).toBeTruthy();
    expect(within(zeilen[0]).queryByRole("button", { name: "Berechtigt" })).toBeNull();
    expect(within(zeilen[1]).getByRole("button", { name: "Berechtigt" })).toBeTruthy();
    expect(within(zeilen[1]).getByRole("button", { name: "Abgewiesen" })).toBeTruthy();
    expect(within(zeilen[1]).queryByRole("button", { name: "Erledigt" })).toBeNull();
    expect(within(zeilen[1]).getByText("im Test als falsch gewertet")).toBeTruthy();
  });

  // Review-Fund nach dem Merge der Nachtläufe: hat der Test die gemeldete Antwort richtig gewertet, rechnet „Berechtigt“
  // nichts nach – das Kind läse trotzdem „der Test ist nachgerechnet“. Rot, wenn das Urteil nur an `testItemResultId` hängt.
  it("bietet an einer im Test richtig gewerteten Antwort kein Urteil an", async () => {
    vi.spyOn(api, "children").mockResolvedValue([]);
    vi.spyOn(api, "listFeedback").mockResolvedValue([{ ...mitTestAntwort, testAnswerCorrect: true }]);
    render(<AuthProvider><MemoryRouter><VaterRueckmeldungen /></MemoryRouter></AuthProvider>);
    await act(async () => {});

    const zeile = screen.getAllByRole("row")[1];
    expect(within(zeile).getByRole("button", { name: "Erledigt" })).toBeTruthy();
    expect(within(zeile).queryByRole("button", { name: "Berechtigt" })).toBeNull();
  });

  // B-769 AK 2: während „Berechtigt“ unterwegs ist, sind die Knöpfe gesperrt, und ein zweiter Klick sendet nichts. Rot, wenn
  // `disabled={busy}` fehlt (der Knopf bliebe klickbar) oder das Urteil nicht mitgeschickt wird.
  it("sperrt die Knöpfe, solange das Urteil gesendet wird", async () => {
    vi.spyOn(api, "children").mockResolvedValue([]);
    vi.spyOn(api, "listFeedback").mockResolvedValue([mitTestAntwort]);
    vi.stubGlobal("confirm", () => true);
    const update = vi.spyOn(api, "updateFeedback").mockImplementation(() => new Promise(() => {}));
    render(<AuthProvider><MemoryRouter><VaterRueckmeldungen /></MemoryRouter></AuthProvider>);
    await act(async () => {});

    const berechtigt = screen.getByRole("button", { name: "Berechtigt" }) as HTMLButtonElement;
    fireEvent.click(berechtigt);
    await act(async () => {});
    fireEvent.click(berechtigt);

    expect(update).toHaveBeenCalledTimes(1);
    expect(update).toHaveBeenCalledWith(12, "Done", "Justified");
    expect(berechtigt.disabled).toBe(true);
    expect((screen.getByRole("button", { name: "Abgewiesen" }) as HTMLButtonElement).disabled).toBe(true);
  });

  // B-767: „berechtigt“ ist endgültig – die erledigte Meldung zeigt ihr Urteil und kein „Wieder öffnen“, eine abgewiesene
  // schon. Rot, wenn der Knopf an einer berechtigten Meldung steht (der Server lehnte ihn mit 409 ab).
  it("bietet an einer berechtigten Meldung kein Wieder öffnen an", async () => {
    vi.spyOn(api, "children").mockResolvedValue([]);
    vi.spyOn(api, "listFeedback").mockResolvedValue([
      { ...mitTestAntwort, status: "Done", outcome: "Justified" },
      { ...mitTestAntwort, id: 13, status: "Done", outcome: "Rejected" },
    ]);
    render(<AuthProvider><MemoryRouter><VaterRueckmeldungen /></MemoryRouter></AuthProvider>);
    await act(async () => {});

    const zeilen = screen.getAllByRole("row").slice(1);
    expect(within(zeilen[0]).getByText("berechtigt")).toBeTruthy();
    expect(within(zeilen[0]).queryByRole("button", { name: "Wieder öffnen" })).toBeNull();
    expect(within(zeilen[1]).getByText("abgewiesen")).toBeTruthy();
    expect(within(zeilen[1]).getByRole("button", { name: "Wieder öffnen" })).toBeTruthy();
  });

  // Review-Fund: „Berechtigt“ ist endgültig, darum fragt die Seite vorher. Rot, wenn ein „Abbrechen“ trotzdem sendet oder
  // einen Erfolg meldet (die Rückfrage fehlt oder steht in `run`).
  it("sendet nach Abbrechen der Rückfrage nichts", async () => {
    vi.spyOn(api, "children").mockResolvedValue([]);
    vi.spyOn(api, "listFeedback").mockResolvedValue([mitTestAntwort]);
    vi.stubGlobal("confirm", () => false);
    const update = vi.spyOn(api, "updateFeedback");
    render(<AuthProvider><MemoryRouter><VaterRueckmeldungen /></MemoryRouter></AuthProvider>);
    await act(async () => {});

    fireEvent.click(screen.getByRole("button", { name: "Berechtigt" }));
    await act(async () => {});

    expect(update).not.toHaveBeenCalled();
    expect(screen.queryByText(/Als berechtigt erledigt/)).toBeNull();
  });
});
