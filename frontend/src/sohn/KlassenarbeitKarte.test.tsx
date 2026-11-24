import { act, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import "../lib/i18n";
import { AuthProvider } from "../lib/auth";
import type { ClassTestReadiness, UpcomingClassTest } from "../lib/types";

const upcoming = vi.fn<(childId: number) => Promise<UpcomingClassTest[]>>();
const readiness = vi.fn<(childId: number, id: number) => Promise<ClassTestReadiness>>();
vi.mock("../lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("../lib/api")>();
  return {
    ...original,
    api: {
      ...original.api,
      upcomingClassTests: (childId: number) => upcoming(childId),
      classTestReadiness: (childId: number, id: number) => readiness(childId, id),
    },
  };
});

const { KlassenarbeitAnzeige, KlassenarbeitKarte, naechsteArbeit } = await import("./KlassenarbeitKarte");

const arbeit = (over: Partial<UpcomingClassTest>): UpcomingClassTest => ({
  id: 1, title: "Vokabeltest Unit 2", subjectId: 3, subjectName: "Englisch", scheduledDate: "2026-11-28", daysUntil: 5, ...over,
});
const stand = (over: Partial<ClassTestReadiness>): ClassTestReadiness => ({
  classTestId: 1, exerciseCount: 2, vocabularyExerciseCount: 2, totalItems: 10, secureItems: 4, weakBelowPercent: 50, ...over,
});

afterEach(() => { upcoming.mockReset(); readiness.mockReset(); });

/*
 * B-750/B-751 – die Klassenarbeit auf der Startseite. Je Fall steht, welche Änderung ihn rot macht.
 */
describe("Klassenarbeit-Karte", () => {
  it("nennt Titel, Fach und die Tage bis zum Termin", () => {
    // B-750 AK 1. Rot, wenn die Karte `daysUntil` nicht zeigt oder Titel bzw. Fach fehlt.
    render(<KlassenarbeitAnzeige arbeit={arbeit({})} bereitschaft={null} />);
    const karte = screen.getByRole("region", { name: "Nächste Klassenarbeit" });
    expect(karte.textContent).toContain("Vokabeltest Unit 2");
    expect(karte.textContent).toContain("Englisch");
    expect(karte.textContent).toContain("in 5 Tagen");
    expect(karte.textContent).toContain("28.11.2026");
  });

  it("sagt am Termintag „heute“ und am Tag davor „morgen“", () => {
    // B-750 AK 1 (heute = 0). Rot, wenn 0 als „in 0 Tagen“ erscheint.
    const { rerender } = render(<KlassenarbeitAnzeige arbeit={arbeit({ daysUntil: 0 })} bereitschaft={null} />);
    expect(screen.getByRole("region").textContent).toContain("heute");
    rerender(<KlassenarbeitAnzeige arbeit={arbeit({ daysUntil: 1 })} bereitschaft={null} />);
    expect(screen.getByRole("region").textContent).toContain("morgen");
  });

  it("zeigt die früheste Arbeit, die der Server zuerst liefert", () => {
    // B-750 AK 4. Rot, wenn die Karte die letzte (späteste) der Liste nimmt.
    const frueh = arbeit({ id: 1, daysUntil: 2 });
    expect(naechsteArbeit([frueh, arbeit({ id: 2, daysUntil: 9 })])).toBe(frueh);
    expect(naechsteArbeit([])).toBeNull();
  });

  it("steht ohne Arbeit gar nicht da – kein leerer Kasten", async () => {
    // B-750 AK 4. Rot, wenn die Karte ohne Inhalt gerendert wird.
    upcoming.mockResolvedValue([]);
    const { container } = render(<AuthProvider><KlassenarbeitKarte childId={7} /></AuthProvider>);
    await act(async () => {});
    expect(upcoming).toHaveBeenCalledWith(7);
    expect(container.innerHTML).toBe("");
    expect(readiness).not.toHaveBeenCalled();
  });

  it("lädt die nächste Arbeit und ihre Bereitschaft", async () => {
    // B-750 + B-751 im Zusammenspiel. Rot, wenn die Bereitschaft einer anderen Arbeit gefragt wird als der gezeigten.
    upcoming.mockResolvedValue([arbeit({ id: 11, title: "Erste" }), arbeit({ id: 12, title: "Zweite" })]);
    readiness.mockResolvedValue(stand({ classTestId: 11, totalItems: 8, secureItems: 6 }));
    render(<AuthProvider><KlassenarbeitKarte childId={7} /></AuthProvider>);
    await act(async () => {});
    await act(async () => {});
    expect(readiness).toHaveBeenCalledWith(7, 11);
    expect(screen.getByRole("region").textContent).toContain("Erste");
    expect(screen.getByText("6 von 8 Wörtern sicher")).toBeTruthy();
  });

  it("zeigt nach einem Kindwechsel weder die alte Arbeit noch ihre Bereitschaft", async () => {
    // Rot, wenn die Karte die Liste des vorigen Kindes stehen lässt (`stale` nicht gelesen).
    upcoming.mockResolvedValueOnce([arbeit({ id: 11, title: "Arbeit von Kind 7" })]);
    readiness.mockResolvedValue(stand({ classTestId: 11 }));
    const { rerender, container } = render(<AuthProvider><KlassenarbeitKarte childId={7} /></AuthProvider>);
    await act(async () => {});
    await act(async () => {});
    expect(container.textContent).toContain("Arbeit von Kind 7");
    upcoming.mockReturnValueOnce(new Promise(() => {})); // Kind 8 lädt noch
    rerender(<AuthProvider><KlassenarbeitKarte childId={8} /></AuthProvider>);
    await act(async () => {});
    expect(container.textContent).not.toContain("Arbeit von Kind 7");
  });

  it("zeigt keine Bereitschaft, die zu einer anderen Arbeit gehört", async () => {
    // Rot, wenn die Karte `classTestId` der Bereitschaft nicht gegen die gezeigte Arbeit prüft.
    upcoming.mockResolvedValue([arbeit({ id: 11 })]);
    readiness.mockResolvedValue(stand({ classTestId: 99, totalItems: 8, secureItems: 6 }));
    render(<AuthProvider><KlassenarbeitKarte childId={7} /></AuthProvider>);
    await act(async () => {});
    await act(async () => {});
    expect(screen.queryByText("6 von 8 Wörtern sicher")).toBeNull();
  });

  it("nennt für ein Vokabel-Bündel „x von y Wörtern sicher“", () => {
    // B-751 AK 2. Rot, wenn Zähler und Nenner vertauscht oder nicht angezeigt werden.
    render(<KlassenarbeitAnzeige arbeit={arbeit({})} bereitschaft={stand({ totalItems: 10, secureItems: 4 })} />);
    expect(screen.getByText("4 von 10 Wörtern sicher")).toBeTruthy();
  });

  it("sagt für ein Bündel ohne Vokabeln „kein Maß“ statt einer Zahl", () => {
    // B-751 AK 2. Rot, wenn ein Bündel ohne Vokabeln eine Zahl (etwa „0 von 0“) zeigt.
    render(<KlassenarbeitAnzeige arbeit={arbeit({})}
      bereitschaft={stand({ vocabularyExerciseCount: 0, totalItems: null, secureItems: null })} />);
    const karte = screen.getByRole("region");
    expect(karte.textContent).toContain("Für diese Arbeit gibt es kein Wörter-Maß.");
    expect(karte.textContent).not.toMatch(/von \d+ W/);
  });

  it("zeigt ohne Übungen in der Arbeit keine Bereitschaft", () => {
    // Rot, wenn eine Arbeit ohne Bündel „kein Maß“ behauptet – es gibt dort nichts zu messen.
    render(<KlassenarbeitAnzeige arbeit={arbeit({})}
      bereitschaft={stand({ exerciseCount: 0, vocabularyExerciseCount: 0, totalItems: null, secureItems: null })} />);
    expect(screen.getByRole("region").textContent).not.toContain("Maß");
  });
});
