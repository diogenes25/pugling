import { fireEvent, render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import "../lib/i18n";
import { DEFAULT_SKIN } from "../lib/skins";
import type { PositionStatus } from "../lib/types";

// Die Startseite hängt an Kontext, den der Pfad nur für den Begleiter braucht.
vi.mock("./SohnApp", () => ({
  useSohn: () => ({ planId: 1, setPlanId: () => {}, childId: 7, celebrate: () => {}, skin: DEFAULT_SKIN }),
}));
vi.mock("../lib/auth", () => ({ useAuth: () => ({ signOut: () => {} }) }));

const { Pfad } = await import("./SohnHome");

const position = (over: Partial<PositionStatus>): PositionStatus => ({
  positionId: 1, exerciseId: 1, exerciseTitle: "Wörter 1", exerciseType: "Vocabulary", renderer: "flashcards",
  order: 1, cadence: "Daily", checkMode: "PositionTest", useLeitner: true, testable: true, goalMet: false,
  dueCount: 3, poolSize: 10, pointsGoalMet: 20, introducedCount: 4, penaltyCoins: 5,
  seriesUnitId: 1, seriesUnitLabel: "Unit 1", ...over,
} as PositionStatus);

// Ein Mittwoch, 22:30 UTC – bis zum Server-Tagesende zwei angefangene Stunden.
const JETZT = new Date(Date.UTC(2026, 10, 11, 22, 30));
const zeige = (positions: PositionStatus[]) =>
  render(<MemoryRouter><Pfad positions={positions} jetzt={JETZT} /></MemoryRouter>);
const knoten = (titel: string) => screen.getByRole("button", { name: new RegExp(`^${titel}`) });

/*
 * B-525 – der Lernpfad auf der Startseite. Die Regeln selbst prüft `pfad.test.ts`; hier steht, dass der Knoten sie
 * auch zeigt. Je Fall steht dabei, welche Änderung ihn rot macht.
 */
describe("Knoten auf dem Lernpfad", () => {
  it("gibt einer freien Position kein Häkchen, einer erfüllten Pflicht schon", () => {
    // B-467. Rot, wenn der Knoten `goalMet` ungeprüft zeigt – oder wenn das Häkchen ganz wegfällt.
    zeige([
      position({ positionId: 1, exerciseTitle: "Frei geübt", cadence: "None", goalMet: true }),
      position({ positionId: 2, exerciseTitle: "Pflicht erledigt", goalMet: true }),
    ]);
    expect(knoten("Frei geübt").textContent).not.toContain("✓");
    expect(knoten("Pflicht erledigt").textContent).toContain("✓");
    expect(knoten("Pflicht erledigt").getAttribute("aria-label")).toContain("Ziel erfüllt");
  });

  it("hebt genau einen Knoten als nächsten hervor und keinen, wenn die Pflicht erfüllt ist", () => {
    // Rot, wenn jeder offene Knoten „START“ trägt – oder wenn nach erfüllter Pflicht noch einer hervorgehoben wird.
    const { unmount } = zeige([
      position({ positionId: 1, exerciseTitle: "Erste" }),
      position({ positionId: 2, exerciseTitle: "Zweite" }),
    ]);
    expect(screen.getAllByText("START")).toHaveLength(1);
    expect(knoten("Erste").className).toContain("naechste");
    expect(knoten("Zweite").className).not.toContain("naechste");
    // Das sichtbare START ist versteckter Schmuck – der Vorlesename muss den nächsten Schritt selbst nennen.
    expect(knoten("Erste").getAttribute("aria-label")).toContain("als Nächstes dran");
    expect(knoten("Erste").getAttribute("aria-current")).toBe("step");
    expect(knoten("Zweite").getAttribute("aria-label")).not.toContain("als Nächstes dran");
    unmount();

    zeige([position({ goalMet: true })]);
    expect(screen.queryByText("START")).toBeNull();
  });

  it("zeigt am offenen Pflicht-Knoten Frist und Malus – und keinen Betrag, wenn es keinen gibt", () => {
    // Rot, wenn der Malus fehlt, oder wenn bei 0 Münzen „sonst −0 Münzen“ dastünde.
    zeige([
      position({ positionId: 1, exerciseTitle: "Mit Malus", penaltyCoins: 5 }),
      position({ positionId: 2, exerciseTitle: "Ohne Malus", penaltyCoins: 0 }),
      position({ positionId: 3, exerciseTitle: "Wochenziel", cadence: "Weekly", penaltyCoins: 1 }),
    ]);
    expect(knoten("Mit Malus").getAttribute("aria-label")).toContain("noch 2 Std., sonst −5 Münzen");
    expect(knoten("Ohne Malus").getAttribute("aria-label")).toContain("noch 2 Std.");
    expect(knoten("Ohne Malus").getAttribute("aria-label")).not.toContain("sonst");
    expect(knoten("Wochenziel").getAttribute("aria-label")).toContain("bis Sonntag, sonst −1 Münze");
    // Sichtbar am Knoten, nicht nur im Vorlesenamen – und dort versteckt, damit es nicht doppelt angesagt wird.
    // Rot, wenn die Zeile unter dem Knoten fehlt.
    const zeile = (titel: string) => knoten(titel).parentElement!.querySelector(".pfad-frist");
    expect(zeile("Mit Malus")!.textContent).toBe("⏰ noch 2 Std. · sonst −5 Münzen");
    expect(zeile("Mit Malus")!.getAttribute("aria-hidden")).toBe("true");
    expect(zeile("Ohne Malus")!.textContent).toBe("⏰ noch 2 Std.");
  });

  it("zeichnet den Ring nur, wenn die Position Inhalte hat", () => {
    // Rot, wenn eine Position ohne Pool (Aufsatz) einen Ring bekäme.
    zeige([
      position({ positionId: 1, exerciseTitle: "Mit Pool" }),
      position({ positionId: 2, exerciseTitle: "Aufsatz", poolSize: 0, introducedCount: 0 }),
    ]);
    expect(knoten("Mit Pool").querySelector(".pfad-ring")).not.toBeNull();
    expect(knoten("Aufsatz").querySelector(".pfad-ring")).toBeNull();
    expect(knoten("Mit Pool").querySelector(".pfad-ring-anteil")).not.toBeNull();
  });

  it("zeichnet bei null kennengelernten Inhalten nur die leere Bahn", () => {
    // Rot, wenn der Bogen auch bei 0 gezeichnet wird: sein rundes Linienende malt dann einen goldenen Punkt, der
    // wie ein erster Fortschritt aussieht (im Rollengang gesehen).
    zeige([position({ exerciseTitle: "Neu", introducedCount: 0 })]);
    expect(knoten("Neu").querySelector(".pfad-ring")).not.toBeNull();
    expect(knoten("Neu").querySelector(".pfad-ring-anteil")).toBeNull();
  });

  it("gliedert nach der Unit und beginnt ein neues Banner, wo sie wechselt", () => {
    // Rot, wenn alle Positionen unter einem einzigen Banner stünden.
    zeige([
      position({ positionId: 1, seriesUnitId: 2, seriesUnitLabel: "Unit 2 – En ville" }),
      position({ positionId: 2, exerciseTitle: "Wörter 2", seriesUnitId: 3, seriesUnitLabel: "Unit 3 – Au marché" }),
    ]);
    expect(screen.getByRole("region", { name: "Unit 2 – En ville" })).toBeTruthy();
    expect(within(screen.getByRole("region", { name: "Unit 3 – Au marché" })).getByRole("button", { name: /^Wörter 2/ })).toBeTruthy();
  });
});

describe("Das Blatt zu einem Knoten", () => {
  it("führt mit „Los“ ins Üben und bietet den Test als zweiten Weg", () => {
    // Rot, wenn „Los“ direkt in den Test führt, obwohl Karten fällig sind, oder wenn der Test aus dem Blatt fiele –
    // er ist der einzige Weg, das Ziel zu erfüllen.
    zeige([position({ positionId: 17 })]);
    fireEvent.click(knoten("Wörter 1"));
    const blatt = screen.getByRole("dialog", { name: "Wörter 1" });
    expect(within(blatt).getByRole("link", { name: "▶ LOS" }).getAttribute("href")).toBe("/sohn/practice/17");
    expect(within(blatt).getByRole("link", { name: "🎯 Test" }).getAttribute("href")).toBe("/sohn/test/17");
    expect(blatt.textContent).toContain("4 von 10 schon kennengelernt · 3 fällig");
    // Frist und Malus stehen auch im Blatt – dort lesbar für alle, nicht versteckt. Rot, wenn sie dort fehlen.
    const frist = blatt.querySelector(".pfad-frist")!;
    expect(frist.textContent).toBe("⏰ noch 2 Std. · sonst −5 Münzen");
    expect(frist.getAttribute("aria-hidden")).toBeNull();
  });

  it("startet mit „Los“ den Test, wenn nichts zu üben ist", () => {
    // Rot, wenn „Los“ in eine leere Übungsrunde führte.
    zeige([position({ positionId: 17, dueCount: 0, introducedCount: 10 })]);
    fireEvent.click(knoten("Wörter 1"));
    const blatt = screen.getByRole("dialog");
    expect(within(blatt).getByRole("link", { name: "▶ LOS" }).getAttribute("href")).toBe("/sohn/test/17");
    expect(within(blatt).getByRole("link", { name: "▶ Üben" }).getAttribute("href")).toBe("/sohn/practice/17");
  });

  it("bietet an einem nicht prüfbaren Tag keinen Test an", () => {
    // Rot, wenn `testable = false` trotzdem einen Test-Knopf zeigte – der Test antwortete mit 400 (B-96).
    zeige([position({ positionId: 17, testable: false })]);
    fireEvent.click(knoten("Wörter 1"));
    const blatt = screen.getByRole("dialog");
    expect(within(blatt).getByRole("link", { name: "▶ LOS" }).getAttribute("href")).toBe("/sohn/practice/17");
    expect(within(blatt).queryByRole("link", { name: "🎯 Test" })).toBeNull();
  });

  it("hält den Fokus im Blatt, schließt mit Escape und gibt den Fokus an den Knoten zurück", () => {
    // Rot, wenn das Blatt nur per Maus zu schließen wäre, der Fokus danach im Nichts stünde – oder wenn Tab hinter
    // den Schleier wanderte, während `aria-modal` den Hintergrund für Screenreader ausblendet.
    zeige([position({})]);
    knoten("Wörter 1").focus();
    fireEvent.click(knoten("Wörter 1"));
    const blatt = screen.getByRole("dialog");
    const los = within(blatt).getByRole("link", { name: "▶ LOS" });
    expect(document.activeElement).toBe(los);
    const letzter = within(blatt).getByRole("link", { name: "🎯 Test" });
    letzter.focus();
    fireEvent.keyDown(window, { key: "Tab" });
    expect(document.activeElement).toBe(within(blatt).getByRole("button", { name: "Schließen" }));
    fireEvent.keyDown(window, { key: "Escape" });
    expect(screen.queryByRole("dialog")).toBeNull();
    expect(document.activeElement).toBe(knoten("Wörter 1"));
  });
});
