import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import { useAction } from "../lib/useAction";
import { NewObjective } from "./VaterZiele";

/*
 * B-380: der Titel eines großen Ziels ist Pflicht – leer ist „Ziel anlegen" gesperrt, das Label sagt „(Pflicht)". Das
 * Formular hat kein `<form>`, also kein Enter-Absenden; geprüft wird der Knopf.
 */
function Rahmen() {
  const action = useAction();
  return <NewObjective childId={1} subjects={[]} action={action} onCreated={() => {}} />;
}

describe("Großes Ziel – Pflichtfeld Titel (B-380)", () => {
  afterEach(() => vi.restoreAllMocks());

  // AK 1. Rot, wenn am Knopf das `|| !title.trim()` fehlt (dann ist er leer bedienbar und legt nach Klick nichts an –
  // bzw. mit `fail`-Zeile meldete er „Bitte einen Titel angeben.").
  it("sperrt „Ziel anlegen“ ohne Titel", () => {
    const anlegen = vi.spyOn(api, "createObjective");
    render(<AuthProvider><Rahmen /></AuthProvider>);
    fireEvent.click(screen.getByRole("button", { name: "+ Großes Ziel anlegen" }));
    const knopf = screen.getByRole("button", { name: "Ziel anlegen" }) as HTMLButtonElement;
    expect(knopf.disabled).toBe(true);
    fireEvent.click(knopf);
    expect(anlegen).not.toHaveBeenCalled();
    expect(screen.queryByText(/Bitte einen Titel/)).toBeNull();
    fireEvent.change(screen.getByLabelText("Titel (Pflicht)"), { target: { value: "Englisch bis Ostern" } });
    expect(knopf.disabled).toBe(false);
  });
});
