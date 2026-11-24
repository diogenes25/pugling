import { act, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import { BereitschaftZeile, ClassTestManager, FensterEditor, RepeatPanel, VorbereitungsZelle } from "./VaterClassTests";
import type { ClassTestReadiness, KlassenarbeitRepeat, KlassenarbeitResponse } from "../lib/types";

/*
 * B-496: „keine schwach benotete Arbeit" ist auch die Antwort für ein Kind ganz ohne bewertete Arbeit. Der Leerfall darf
 * darum kein Erfolg sein. Rot, wenn er wieder 👍 bzw. „nichts nachzuholen" sagt.
 */
const state = (data: KlassenarbeitRepeat) =>
  ({ data, loading: false, error: null, showPlaceholder: false, stale: false, reload: () => {} });

describe("Wiederholen aus Klassenarbeiten – leer", () => {
  it("meldet keinen Erfolg", () => {
    render(<RepeatPanel repeat={state({ minBadGrade: 4, sources: [] } as unknown as KlassenarbeitRepeat)} />);
    // Der genaue neutrale Satz (B-507): ein Ausschluss zweier Erfolgswörter ließe jedes dritte durch.
    expect(screen.getByText("Keine Arbeit mit Note ≥ 4.0 – aus Klassenarbeiten ist nichts zu wiederholen.")).toBeTruthy();
  });
});

/*
 * B-380: Titel und Termin einer neuen Klassenarbeit sind Pflicht. Leer ist „Planen" gesperrt, beide Labels sagen
 * „(Pflicht)", Enter bleibt stumm. Gefälscht werden die Abrufe des Bausteins, nicht das `fetch`.
 */
describe("Klassenarbeit planen – Pflichtfelder (B-380)", () => {
  afterEach(() => vi.restoreAllMocks());
  const zeige = async () => {
    vi.spyOn(api, "classTests").mockResolvedValue({ items: [], total: 0 } as never);
    vi.spyOn(api, "subjects").mockResolvedValue([]);
    vi.spyOn(api, "classTestRepeat").mockResolvedValue({ minBadGrade: 4, sources: [] } as never);
    const view = render(<AuthProvider><ClassTestManager childId={1} /></AuthProvider>);
    await act(async () => {});
    return view;
  };

  // AK 1, beide Zeilen. Rot, wenn am Knopf `|| !form.title.trim()` oder `|| !form.scheduledDate` fehlt.
  it("sperrt „Planen“, bis Titel und Termin gesetzt sind", async () => {
    await zeige();
    const knopf = screen.getByRole("button", { name: "Planen" }) as HTMLButtonElement;
    expect(knopf.disabled).toBe(true);
    fireEvent.change(screen.getByLabelText("Titel (Pflicht)"), { target: { value: "Vokabeltest" } });
    expect(knopf.disabled).toBe(true);
    fireEvent.change(screen.getByLabelText("Termin (Pflicht)"), { target: { value: "2026-12-01" } });
    expect(knopf.disabled).toBe(false);
    fireEvent.change(screen.getByLabelText("Titel (Pflicht)"), { target: { value: " " } });
    expect(knopf.disabled).toBe(true);
  });

  // AK 1. Rot, wenn eine `fail`-Zeile zurückkommt („Titel nötig." / „Termin nötig.") oder trotzdem angelegt wird.
  it("schickt nach Enter ohne Pflichtwerte nichts und meldet nichts", async () => {
    const anlegen = vi.spyOn(api, "createClassTest");
    const { container } = await zeige();
    fireEvent.change(screen.getByLabelText("Titel (Pflicht)"), { target: { value: "Vokabeltest" } });
    fireEvent.submit(container.querySelector("form")!);
    expect(anlegen).not.toHaveBeenCalled();
    expect(screen.queryByText(/nötig/)).toBeNull();
  });
});

/*
 * B-757: das Vorbereitungs-Fenster im Dialog. Die Zahl „etwa N Karten je Tag“ kommt vom Server; leer heißt Standard, 0 aus.
 * Je Fall steht, welche Änderung ihn rot macht.
 */
describe("Vorbereitungs-Fenster (B-757)", () => {
  afterEach(() => vi.restoreAllMocks());
  const arbeit = (over: Partial<KlassenarbeitResponse>): KlassenarbeitResponse => ({
    id: 5, childId: 1, subjectId: 3, subjectName: "Englisch", title: "Vokabeltest", topic: null, scheduledDate: "2026-12-01",
    status: "Planned", grade: null, gradeComment: null, directExerciseCount: 2, tags: [], createdAt: "2026-11-23T10:00:00Z",
    prepWindowDays: null, prepWindowDefault: 7, prepCardsPerDay: 4, prepBlockedReason: null, prepNotExecutableCount: 0, ...over,
  } as KlassenarbeitResponse);

  // AK 1 (Anlegen). Rot, wenn das Formular `prepWindowDays` nicht schickt oder ein leeres Feld als 0 schickt.
  it("schickt das Fenster beim Anlegen – leer als Standard, 0 als aus", async () => {
    vi.spyOn(api, "classTests").mockResolvedValue({ items: [], total: 0 } as never);
    vi.spyOn(api, "subjects").mockResolvedValue([]);
    vi.spyOn(api, "classTestRepeat").mockResolvedValue({ minBadGrade: 4, sources: [] } as never);
    const anlegen = vi.spyOn(api, "createClassTest").mockResolvedValue({} as never);
    render(<AuthProvider><ClassTestManager childId={1} /></AuthProvider>);
    await act(async () => {});
    const planen = async (fenster: string) => {
      fireEvent.change(screen.getByLabelText("Titel (Pflicht)"), { target: { value: "Vokabeltest" } });
      fireEvent.change(screen.getByLabelText("Termin (Pflicht)"), { target: { value: "2026-12-01" } });
      fireEvent.change(screen.getByLabelText("Vorbereitung (Tage)"), { target: { value: fenster } });
      await act(async () => { fireEvent.click(screen.getByRole("button", { name: "Planen" })); });
    };
    await planen("");
    expect(anlegen.mock.calls[0][0]).toMatchObject({ prepWindowDays: null });
    await planen("0");
    expect(anlegen.mock.calls[1][0]).toMatchObject({ prepWindowDays: 0 });
    await planen("3");
    expect(anlegen.mock.calls[2][0]).toMatchObject({ prepWindowDays: 3 });
  });

  // AK 2. Rot, wenn die Zelle selbst rechnet (Zahl ≠ Server) oder bei Fenster 0 bzw. nicht geplanter Arbeit eine Zahl zeigt.
  it("zeigt die Zahl des Servers, bei Fenster 0 und nicht geplanten Arbeiten keine", () => {
    const { rerender, container } = render(<VorbereitungsZelle k={arbeit({ prepCardsPerDay: 4 })} />);
    expect(container.textContent).toContain("etwa 4 Karten je Tag");
    expect(container.textContent).toContain("7 Tage (Standard)");
    rerender(<VorbereitungsZelle k={arbeit({ prepWindowDays: 0, prepCardsPerDay: null })} />);
    expect(container.textContent).toBe("aus");
    // B-943: mit einer Zahl im Datensatz – sonst bleibt „Karten“ auch ohne die Statusprüfung fern.
    rerender(<VorbereitungsZelle k={arbeit({ status: "Written", prepCardsPerDay: 4 })} />);
    expect(container.textContent).toBe("–");
    rerender(<VorbereitungsZelle k={arbeit({ status: "Cancelled", prepCardsPerDay: 4 })} />);
    expect(container.textContent).toBe("–");
  });

  // B-941 AK 5. Rot, wenn der Hinweis fehlt oder bei 0 erscheint.
  it("nennt ausgelassene Übungen ohne Ausführ-Recht – und nur, wenn es welche gibt", () => {
    const { rerender, container } = render(<VorbereitungsZelle k={arbeit({ prepNotExecutableCount: 2 })} />);
    expect(container.textContent).toContain("2 Übungen ohne Ausführ-Recht ausgelassen");
    rerender(<VorbereitungsZelle k={arbeit({ prepNotExecutableCount: 1 })} />);
    expect(container.textContent).toContain("1 Übung ohne Ausführ-Recht ausgelassen");
    rerender(<VorbereitungsZelle k={arbeit({ prepNotExecutableCount: 0 })} />);
    expect(container.textContent).not.toMatch(/ausgelassen/);
  });

  // AK 3. Rot, wenn der Grund fehlt oder bei vorhandener Pflicht ein Grund steht.
  it("nennt den Grund, wenn keine Pflicht entsteht – und nur dann", () => {
    const { rerender, container } = render(<VorbereitungsZelle k={arbeit({ prepBlockedReason: "no_active_plan", prepCardsPerDay: null })} />);
    expect(container.textContent).toBe("keine Pflicht: kein aktiver Plan im Fach");
    rerender(<VorbereitungsZelle k={arbeit({ subjectId: null, prepBlockedReason: "no_subject", prepCardsPerDay: null })} />);
    expect(container.textContent).toBe("keine Pflicht: die Arbeit hat kein Fach");
    rerender(<VorbereitungsZelle k={arbeit({})} />);
    expect(container.textContent).not.toMatch(/keine Pflicht/);
  });

  // AK 1 (Ändern) und AK 4. Rot, wenn das Feld den gespeicherten Wert nicht zeigt, ein leeres Feld nichts zurücksetzt
  // (kein `clearPrepWindow`) oder eine Zahl ohne `prepWindowDays` gesendet wird.
  it("zeigt den gespeicherten Wert, speichert eine Zahl und setzt leer auf den Standard zurück", async () => {
    const aendern = vi.spyOn(api, "updateClassTest").mockResolvedValue({} as never);
    render(<AuthProvider><FensterEditor k={arbeit({ prepWindowDays: 5 })} onSaved={() => {}} /></AuthProvider>);
    const feld = screen.getByLabelText("Vorbereitung (Tage, leer = Standard 7)") as HTMLInputElement;
    expect(feld.value).toBe("5");
    fireEvent.change(feld, { target: { value: "3" } });
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "Fenster speichern" })); });
    expect(aendern).toHaveBeenLastCalledWith(5, { prepWindowDays: 3 });
    fireEvent.change(feld, { target: { value: "" } });
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "Fenster speichern" })); });
    expect(aendern).toHaveBeenLastCalledWith(5, { clearPrepWindow: true });
  });

  // AK 4, erste Hälfte. Rot, wenn das Nachtragen einer Note das Fenster mitschickt (und damit überschreiben könnte).
  it("die Note nachtragen schickt kein Fenster", async () => {
    vi.spyOn(api, "classTests").mockResolvedValue({ items: [arbeit({ prepWindowDays: 5 })], total: 1 } as never);
    vi.spyOn(api, "subjects").mockResolvedValue([]);
    vi.spyOn(api, "classTestRepeat").mockResolvedValue({ minBadGrade: 4, sources: [] } as never);
    const aendern = vi.spyOn(api, "updateClassTest").mockResolvedValue({} as never);
    render(<AuthProvider><ClassTestManager childId={1} /></AuthProvider>);
    await act(async () => {});
    fireEvent.change(screen.getByTitle("Note"), { target: { value: "2" } });
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "OK" })); });
    expect(aendern).toHaveBeenCalledWith(5, { grade: 2, status: "Written" });
  });

  // B-942. Rot, wenn die Detailkarte den Editor am gespeicherten Wert schlüsselt: das Neuladen nach dem Speichern bringt
  // `prepWindowDays: 3` statt 5, der Editor würde neu montiert und verlöre die Meldung.
  it("die Erfolgsmeldung des Fensters übersteht das Neuladen der Detailkarte", async () => {
    vi.spyOn(api, "classTests").mockResolvedValue({ items: [arbeit({ prepWindowDays: 5 })], total: 1 } as never);
    vi.spyOn(api, "subjects").mockResolvedValue([]);
    vi.spyOn(api, "classTestRepeat").mockResolvedValue({ minBadGrade: 4, sources: [] } as never);
    vi.spyOn(api, "classTestPractice").mockResolvedValue({ daysUntil: 7, exercises: [] } as never);
    vi.spyOn(api, "searchExercises").mockResolvedValue({ items: [], total: 0 } as never);
    const detail = vi.spyOn(api, "classTest")
      .mockResolvedValueOnce({ klassenarbeit: arbeit({ prepWindowDays: 5 }), assignedExercises: [] } as never)
      .mockResolvedValue({ klassenarbeit: arbeit({ prepWindowDays: 3 }), assignedExercises: [] } as never);
    vi.spyOn(api, "updateClassTest").mockResolvedValue({} as never);
    render(<AuthProvider><ClassTestManager childId={1} /></AuthProvider>);
    await act(async () => {});
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "Übungen & Vorbereiten" })); });
    fireEvent.change(screen.getByLabelText("Vorbereitung (Tage, leer = Standard 7)"), { target: { value: "3" } });
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "Fenster speichern" })); });
    await act(async () => {});
    expect(detail).toHaveBeenCalledTimes(2); // das Neuladen ist wirklich passiert
    expect(screen.getByText("Vorbereitung: 3 Tage.")).toBeTruthy();
  });
});

/*
 * B-949: der Vater liest „x von y Wörtern sicher“ vom selben Endpunkt wie das Kind. Gefälscht wird der Abruf des Bausteins.
 */
describe("Bereitschaft in der Vater-Liste (B-949)", () => {
  afterEach(() => vi.restoreAllMocks());
  const arbeit = (over: Partial<KlassenarbeitResponse>): KlassenarbeitResponse => ({
    id: 7, childId: 3, status: "Planned", directExerciseCount: 1, tags: [], ...over,
  } as KlassenarbeitResponse);
  const stand = (over: Partial<ClassTestReadiness>) => ({
    classTestId: 7, exerciseCount: 2, vocabularyExerciseCount: 1, totalItems: 209, secureItems: 21, weakBelowPercent: 50, ...over,
  } as ClassTestReadiness);

  // AK 1. Rot, wenn die Zeile fehlt, andere Zahlen zeigt oder Kind und Arbeit nicht an den Endpunkt reicht.
  it("zeigt die Zahlen des Servers für eine geplante Arbeit", async () => {
    const abruf = vi.spyOn(api, "classTestReadiness").mockResolvedValue(stand({}));
    render(<AuthProvider><BereitschaftZeile k={arbeit({})} /></AuthProvider>);
    await act(async () => {});
    expect(screen.getByText("21 von 209 Wörtern sicher")).toBeTruthy();
    expect(abruf).toHaveBeenCalledWith(3, 7);
  });

  // AK 2. Rot, wenn ein Bündel ohne Vokabeln „0 von 0“ oder „null von null“ zeigt.
  it("zeigt nichts ohne Vokabeln", async () => {
    vi.spyOn(api, "classTestReadiness").mockResolvedValue(stand({ totalItems: null, secureItems: null }));
    const { container } = render(<AuthProvider><BereitschaftZeile k={arbeit({})} /></AuthProvider>);
    await act(async () => {});
    expect(container.textContent).toBe("");
  });

  // AK 3. Rot, wenn eine geschriebene oder abgesagte Arbeit abfragt oder eine Zeile zeigt.
  it("fragt bei geschriebenen und abgesagten Arbeiten nicht", async () => {
    const abruf = vi.spyOn(api, "classTestReadiness").mockResolvedValue(stand({}));
    for (const status of ["Written", "Cancelled"] as const) {
      const { container, unmount } = render(<AuthProvider><BereitschaftZeile k={arbeit({ status })} /></AuthProvider>);
      await act(async () => {});
      expect(container.textContent).toBe("");
      unmount();
    }
    expect(abruf).not.toHaveBeenCalled();
  });

  // AK 4. Rot, wenn die Zeile einen Fehler als Text (Meldung, Fehlerkasten) in die Liste bringt.
  it("lässt die Zeile nach einem Fehler weg", async () => {
    vi.spyOn(api, "classTestReadiness").mockRejectedValue(new Error("kaputt"));
    const { container } = render(<AuthProvider><BereitschaftZeile k={arbeit({})} /></AuthProvider>);
    await act(async () => {});
    expect(container.textContent).toBe("");
  });

  // Review-Fund: weist der Vater Übungen zu, lädt die Liste neu – die Zahl muss mitziehen. Rot, wenn die Abhängigkeiten das
  // Bündel nicht kennen (die Zeile bliebe beim alten Stand bzw. erschiene nie).
  it("lädt neu, wenn sich das Bündel ändert", async () => {
    const abruf = vi.spyOn(api, "classTestReadiness").mockResolvedValue(stand({}));
    const { rerender } = render(<AuthProvider><BereitschaftZeile k={arbeit({ directExerciseCount: 1, tags: [] })} /></AuthProvider>);
    await act(async () => {});
    rerender(<AuthProvider><BereitschaftZeile k={arbeit({ directExerciseCount: 2, tags: [] })} /></AuthProvider>);
    await act(async () => {});
    expect(abruf).toHaveBeenCalledTimes(2);
  });
});
