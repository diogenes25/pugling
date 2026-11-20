import { act, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import { VaterKinder } from "./VaterKinder";

/*
 * B-380: der Vorname des neuen Kindes ist Pflicht – leer ist „Kind anlegen" gesperrt, das Label sagt „(Pflicht)", Enter
 * bleibt stumm. Gefälscht werden die zwei Abrufe der Seite (seit B-749 liegt das Formular auf der Verwaltungsseite), nicht das `fetch` (Muster `PlanPositionsAdd.test.tsx`).
 */
describe("Kinder-Verwaltung – Kind anlegen, Pflichtfeld Name (B-380)", () => {
  beforeEach(() => {
    localStorage.setItem("pugling.session", JSON.stringify({
      token: "t", role: "Supervisor", id: 1, name: "Papa", expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
    }));
    vi.spyOn(api, "children").mockResolvedValue([]);
    vi.spyOn(api, "plans").mockResolvedValue([]);
  });
  afterEach(() => { vi.restoreAllMocks(); localStorage.removeItem("pugling.session"); });

  const zeige = async () => {
    const view = render(<AuthProvider><MemoryRouter><VaterKinder /></MemoryRouter></AuthProvider>);
    await act(async () => {});
    return view;
  };

  // AK 1. Rot, wenn am Knopf das `|| !name.trim()` fehlt.
  it("sperrt „Kind anlegen“ ohne Namen", async () => {
    await zeige();
    const knopf = screen.getByRole("button", { name: "Kind anlegen" }) as HTMLButtonElement;
    expect(knopf.disabled).toBe(true);
    fireEvent.change(screen.getByLabelText("Name (Pflicht)"), { target: { value: "Lena" } });
    expect(knopf.disabled).toBe(false);
  });

  // AK 1. Rot, wenn die `fail`-Zeile zurückkommt („Bitte einen Vornamen angeben.") oder trotzdem angelegt wird.
  it("schickt nach Enter ohne Namen nichts und meldet nichts", async () => {
    const anlegen = vi.spyOn(api, "createChild");
    const { container } = await zeige();
    fireEvent.submit(container.querySelector("form")!);
    expect(anlegen).not.toHaveBeenCalled();
    expect(screen.queryByText(/Vornamen/)).toBeNull();
  });

  // AK 3. Die PIN-Regel bleibt eine Meldung nach dem Klick. Rot, wenn sie mit verschwand.
  it("meldet eine zu kurze PIN weiter nach dem Absenden", async () => {
    const anlegen = vi.spyOn(api, "createChild");
    await zeige();
    fireEvent.change(screen.getByLabelText("Name (Pflicht)"), { target: { value: "Lena" } });
    fireEvent.change(screen.getByLabelText("PIN"), { target: { value: "12" } });
    fireEvent.click(screen.getByRole("button", { name: "Kind anlegen" }));
    expect(screen.getAllByRole("status").some((s) => /PIN/.test(s.textContent ?? ""))).toBe(true);
    expect(anlegen).not.toHaveBeenCalled();
  });
});
