import { act, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import { ClassTestManager, RepeatPanel } from "./VaterClassTests";
import type { KlassenarbeitRepeat } from "../lib/types";

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
