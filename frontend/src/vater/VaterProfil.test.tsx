import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { ProfileForm } from "./VaterProfil";

/*
 * B-380: der eigene Name ist Pflicht – leer gesperrt, „(Pflicht)" im Label, Enter stumm. Baustein-Test: das Formular ruft
 * erst beim Absenden das Netz.
 */
const zeige = () => render(<ProfileForm name="Papa" email={null} onSaved={() => {}} />);

describe("Profil – Pflichtfeld Name (B-380)", () => {
  afterEach(() => vi.restoreAllMocks());

  // AK 1. Rot, wenn am Knopf das `|| !name.trim()` fehlt.
  it("sperrt „Speichern“ bei leerem Namen", () => {
    zeige();
    const knopf = screen.getByRole("button", { name: "Speichern" }) as HTMLButtonElement;
    expect(knopf.disabled).toBe(false);
    fireEvent.change(screen.getByLabelText("Name (Pflicht)"), { target: { value: "" } });
    expect(knopf.disabled).toBe(true);
  });

  // AK 1. Rot, wenn die `fail`-Zeile zurückkommt („Der Name darf nicht leer sein.").
  it("schickt nach Enter mit leerem Namen nichts und meldet nichts", () => {
    const speichern = vi.spyOn(api, "updateMyAccount");
    const { container } = zeige();
    fireEvent.change(screen.getByLabelText("Name (Pflicht)"), { target: { value: "" } });
    fireEvent.submit(container.querySelector("form")!);
    expect(speichern).not.toHaveBeenCalled();
    expect(screen.queryByText(/darf nicht leer/)).toBeNull();
  });

  // AK 3. Zwei ungleiche PINs sind eine Regel, kein leeres Feld – die Meldung bleibt. Rot, wenn sie mit verschwand.
  it("meldet ungleiche PINs weiter nach dem Absenden", () => {
    zeige();
    fireEvent.change(screen.getByLabelText(/^Neue PIN \(/), { target: { value: "1234" } });
    fireEvent.change(screen.getByLabelText("Neue PIN wiederholen"), { target: { value: "4321" } });
    fireEvent.click(screen.getByRole("button", { name: "Speichern" }));
    expect(screen.getByText("Die beiden PINs stimmen nicht überein.")).toBeTruthy();
  });
});
