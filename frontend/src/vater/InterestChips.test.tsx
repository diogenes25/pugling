import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { InterestChips } from "./ExerciseEditModal";
import type { InterestTagResponse } from "../lib/types";

/*
 * B-206. Die Einkleidung war unsichtbar: Der KI-Creator trug sie ein, das Formular reichte sie stumm
 * durch, und ein anderswo gelöschtes Stichwort machte das Speichern unmöglich — mit einer Meldung, die
 * eine Größe nannte, für die es kein Feld gab.
 *
 * Geprüft wird der **reine** Teil (Props rein, Rückruf raus), nicht der Bildschirm: die Ladelogik hängt am
 * Netz, und `frontend/CLAUDE.md` verbietet den nachgebauten Bildschirm mit gefälschtem `fetch`. Den Weg
 * durch die echte App fährt `e2e/einkleidung.spec.ts`.
 */

const KNOWN: InterestTagResponse[] = [
  { id: 3, slug: "einhorn", label: "Einhorn", facet: "Animal", synonyms: [], color: null,
    mediaCount: 0, childCount: 0, exerciseCount: 1, createdAt: "2026-01-01T00:00:00Z" },
  { id: 7, slug: "comic", label: "Comic", facet: "Style", synonyms: [], color: null,
    mediaCount: 0, childCount: 0, exerciseCount: 0, createdAt: "2026-01-01T00:00:00Z" },
];

describe("Einkleidung im Bearbeiten-Dialog", () => {
  // AK 1: mit den NAMEN, nicht mit Ids - eine Id sagt dem Vater nichts.
  it("zeigt die Einkleidung mit Namen", () => {
    render(<InterestChips tagIds={[3]} known={KNOWN} onChange={() => {}} disabled={false} />);

    expect(screen.getByText("Einhorn")).toBeTruthy();
    expect(screen.queryByText("Stichwort #3")).toBeNull();
  });

  // AK 2: sonst sieht "nicht eingekleidet" aus wie "das Feld gibt es nicht" - die Verwechslung aus B-174.
  it("sagt ausdrücklich, wenn eine Übung nicht eingekleidet ist", () => {
    render(<InterestChips tagIds={[]} known={KNOWN} onChange={() => {}} disabled={false} />);

    expect(screen.getByText(/Nicht eingekleidet/)).toBeTruthy();
  });

  /*
   * AK 4: Die Auswahlliste ist gedeckelt (`take=200`). Ein Chip ohne Beschriftung wäre wieder eine Größe,
   * die man nicht sieht - genau der Defekt, den diese Story schließt.
   */
  it("nennt die Id, wenn der Name nicht in der geladenen Liste steht", () => {
    render(<InterestChips tagIds={[42]} known={KNOWN} onChange={() => {}} disabled={false} />);

    expect(screen.getByText(/Stichwort #42/)).toBeTruthy();
    expect(screen.queryByText(/Nicht eingekleidet/)).toBeNull();
  });

  it("meldet ein Entfernen mit der verbleibenden Menge nach oben", () => {
    const onChange = vi.fn();
    render(<InterestChips tagIds={[3, 7]} known={KNOWN} onChange={onChange} disabled={false} />);

    fireEvent.click(screen.getByRole("button", { name: "Einhorn entfernen" }));
    expect(onChange).toHaveBeenCalledWith([7]);
  });

  it("trägt ein gewähltes Interesse nach", () => {
    const onChange = vi.fn();
    render(<InterestChips tagIds={[3]} known={KNOWN} onChange={onChange} disabled={false} />);

    fireEvent.change(screen.getByRole("combobox"), { target: { value: "7" } });
    fireEvent.click(screen.getByRole("button", { name: "Einkleidung hinzufügen" }));
    expect(onChange).toHaveBeenCalledWith([3, 7]);
  });

  // Was schon dransteht, darf nicht zweimal wählbar sein: der Server hielte die Dublette aus, der Vater
  // sähe aber zwei gleiche Chips und wüsste nicht, welchen er entfernt.
  it("bietet nur an, was noch nicht dransteht", () => {
    render(<InterestChips tagIds={[3]} known={KNOWN} onChange={() => {}} disabled={false} />);

    const optionen = screen.getAllByRole("option").map((o) => o.textContent);
    expect(optionen).toContain("Comic");
    expect(optionen).not.toContain("Einhorn");
  });

  it("sperrt Entfernen und Hinzufügen, solange gespeichert wird", () => {
    const onChange = vi.fn();
    render(<InterestChips tagIds={[3]} known={KNOWN} onChange={onChange} disabled={true} />);

    fireEvent.click(screen.getByRole("button", { name: "Einhorn entfernen" }));
    expect(onChange).not.toHaveBeenCalled();
    expect(screen.getByRole("button", { name: "Einkleidung hinzufügen" }).hasAttribute("disabled")).toBe(true);
  });
});
