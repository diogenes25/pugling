import { fireEvent, render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import type { ChildPeriod, ChildrenOverview, OverviewGranularity } from "../lib/types";
import { ChildrenTimeline, periodLabel, rangeFor } from "./ChildrenTimeline";

/*
 * B-39: die Kinder über die Zeit. Gerechnet wird am Server; hier steht, dass die Balken die Antwort zeigen – so viele, wie
 * sie Perioden trägt, mit Text neben der Farbe, und mit einem Weg in den Lernstand je Kind.
 */
const periode = (start: string, goalsMet: number, goalsTotal: number, dutyDone = false): ChildPeriod =>
  ({ start, end: start, goalsTotal, goalsMet, points: goalsMet * 5, dutyDone });

const antwort = (granularity: OverviewGranularity, perioden: ChildPeriod[]): ChildrenOverview => ({
  from: "2026-03-01", to: "2026-03-31", granularity,
  children: [{ childId: 4, name: "Friedrich", periods: perioden }, { childId: 6, name: "Theodor", periods: perioden }],
});

const zeige = () => render(<AuthProvider><MemoryRouter><ChildrenTimeline /></MemoryRouter></AuthProvider>);

describe("Zeitachse der Kinder (B-39)", () => {
  afterEach(() => vi.restoreAllMocks());

  // AK 3. Rot, wenn der Umschalter die Antwort nicht abfragt oder die Zeile nicht je Periode einen Balken zeichnet.
  it("zeichnet je Periode der Antwort einen Balken und fragt beim Umschalten neu", async () => {
    const abruf = vi.spyOn(api, "childrenOverview").mockImplementation(async (q) =>
      q.granularity === "Month"
        ? antwort("Month", [periode("2026-01-01", 20, 31), periode("2026-02-01", 28, 28, true)])
        : antwort(q.granularity, [periode("2026-03-02", 7, 7, true), periode("2026-03-09", 3, 7), periode("2026-03-16", 0, 0)]));
    zeige();

    const friedrich = await screen.findByRole("list", { name: "Zeitachse von Friedrich" });
    expect(within(friedrich).getAllByRole("listitem")).toHaveLength(3);
    fireEvent.click(screen.getByRole("button", { name: "Monat" }));
    await vi.waitFor(() => expect(within(screen.getByRole("list", { name: "Zeitachse von Friedrich" })).getAllByRole("listitem")).toHaveLength(2));
    expect(abruf.mock.calls.map((c) => c[0].granularity)).toEqual(["Week", "Month"]);
  });

  // AK 5. Rot, wenn ein Balken seinen Status nur über die Farbe zeigt (dann fehlte der Name mit „von … Zielen“).
  it("nennt je Balken den Status in Worten", async () => {
    vi.spyOn(api, "childrenOverview").mockResolvedValue(antwort("Week", [periode("2026-03-02", 7, 7, true), periode("2026-03-09", 3, 7), periode("2026-03-16", 0, 0)]));
    zeige();

    const balken = within(await screen.findByRole("list", { name: "Zeitachse von Friedrich" })).getAllByRole("listitem");
    // Als Text im Element (sr-only), nicht nur als `aria-label` am Listeneintrag – Review-Fund.
    expect(balken.map((b) => b.querySelector(".sr-only")?.textContent)).toEqual([
      "Woche ab 02.03.2026: 7 von 7 Zielen – Pflicht geschafft ✓, 35 Punkte",
      "Woche ab 09.03.2026: 3 von 7 Zielen, 15 Punkte",
      "Woche ab 16.03.2026: kein Ziel",
    ]);
  });

  // AK 4. Rot, wenn der Link fehlt oder auf ein anderes Kind zeigt.
  it("führt aus jeder Kindzeile in deren Lernstand", async () => {
    vi.spyOn(api, "childrenOverview").mockResolvedValue(antwort("Week", [periode("2026-03-02", 1, 1, true)]));
    zeige();

    expect((await screen.findByRole("link", { name: "Lernstand von Friedrich" })).getAttribute("href")).toBe("/vater/kind/4/lernstand");
    expect(screen.getByRole("link", { name: "Lernstand von Theodor" }).getAttribute("href")).toBe("/vater/kind/6/lernstand");
  });
});

describe("Zeitachse – Zeitraum je Raster", () => {
  // Entscheidung 9: der Server nimmt höchstens 92 Tage. Rot, wenn ein Raster mehr verlangt (dann käme nur ein Fehler) –
  // geprüft am längsten Fall des Monats-Rasters (drei Monate mit 30, 31, 31 Tagen bis zum letzten Tag).
  it("bleibt in jedem Raster innerhalb von 92 Tagen", () => {
    const tage = (r: { from: string; to: string }) => (Date.parse(r.to) - Date.parse(r.from)) / 86_400_000 + 1;
    const ende = new Date(Date.UTC(2026, 7, 31));
    expect(tage(rangeFor("Day", ende))).toBe(14);
    expect(rangeFor("Month", ende)).toEqual({ from: "2026-06-01", to: "2026-08-31" });
    expect(periodLabel(periode("2026-03-01", 0, 0), "Day")).toBe("01.03.2026: kein Ziel");
  });

  // Review-Fund: die Wochen beginnen an einem Montag, sonst wäre der erste Balken eine halbe Woche. Rot, wenn das Raster
  // nur Tage zurückzählt (an einem Mittwoch begänne es dann an einem Donnerstag).
  it("beginnt das Wochen-Raster an einem Montag, zwölf Wochen bis heute", () => {
    const mittwoch = rangeFor("Week", new Date(Date.UTC(2026, 8, 2)));
    expect(mittwoch).toEqual({ from: "2026-06-15", to: "2026-09-02" });
    expect(new Date(`${mittwoch.from}T00:00:00Z`).getUTCDay()).toBe(1);
  });

  // Review-Fund: der Monat heißt wie ein Monat. Rot, wenn er wieder als Datumsspanne erscheint.
  it("nennt einen Monat beim Namen", () => {
    expect(periodLabel(periode("2026-06-01", 20, 30), "Month")).toBe("Juni 2026: 20 von 30 Zielen, 100 Punkte");
  });
});
