import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import type { ChildResponse } from "../lib/types";
import { CoreDataSection } from "./VaterKind";

/*
 * B-380: der Name des Kindes ist Pflicht. Leer ist „Speichern" gesperrt, das Label sagt „(Pflicht)", und Enter schickt
 * nichts – ohne Meldung. Die Stammdaten rufen erst beim Absenden das Netz; darum ein Baustein-Test (frontend/CLAUDE.md).
 */
const KIND = {
  id: 7, name: "Theo", birthYear: 2014, grade: 7, schoolType: "Gymnasium", gender: "None", interests: [], profileNotes: null,
} as unknown as ChildResponse;

const zeige = () => render(<MemoryRouter><CoreDataSection child={KIND} onSaved={() => {}} /></MemoryRouter>);

describe("Kind-Stammdaten – Pflichtfeld Name (B-380)", () => {
  afterEach(() => vi.restoreAllMocks());

  // AK 1. Rot, wenn am Knopf das `|| !form.name.trim()` fehlt.
  it("sperrt „Speichern“ bei leerem Namen", () => {
    zeige();
    const knopf = screen.getByRole("button", { name: "Speichern" }) as HTMLButtonElement;
    expect(knopf.disabled).toBe(false);
    fireEvent.change(screen.getByLabelText("Name (Pflicht)"), { target: { value: "  " } });
    expect(knopf.disabled).toBe(true);
  });

  // AK 1. Rot, wenn die `fail`-Zeile zurückkommt („Der Name darf nicht leer sein.") oder trotzdem gespeichert wird.
  it("schickt nach Enter mit leerem Namen nichts und meldet nichts", () => {
    const speichern = vi.spyOn(api, "updateChild");
    const { container } = zeige();
    fireEvent.change(screen.getByLabelText("Name (Pflicht)"), { target: { value: "" } });
    fireEvent.submit(container.querySelector("form")!);
    expect(speichern).not.toHaveBeenCalled();
    expect(screen.queryByText(/darf nicht leer/)).toBeNull();
  });

  // AK 3. Die PIN-Regel ist keine Pflichtfeld-Prüfung und meldet weiter nach dem Klick. Rot, wenn sie mit verschwand.
  it("meldet eine zu kurze PIN weiter nach dem Absenden", () => {
    const speichern = vi.spyOn(api, "updateChild");
    zeige();
    fireEvent.change(screen.getByLabelText(/^PIN/), { target: { value: "12" } });
    fireEvent.click(screen.getByRole("button", { name: "Speichern" }));
    expect(screen.getAllByRole("status").some((st) => /PIN/.test(st.textContent ?? ""))).toBe(true);
    expect(speichern).not.toHaveBeenCalled();
  });
});
