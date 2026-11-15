import { describe, expect, it } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { AddPanel } from "./AddPanel";

describe("AddPanel", () => {
  // Rot, sobald das Formular wieder dauerhaft neben dem Bestand steht (B-534).
  it("hält das Formular zu, bis der Knopf es öffnet", () => {
    render(<AddPanel label="Weiteres Buch"><button type="submit">Buch hinterlegen</button></AddPanel>);
    const toggle = screen.getByRole("button", { name: "Weiteres Buch" });
    expect(toggle.getAttribute("aria-expanded")).toBe("false");
    expect(screen.queryByRole("button", { name: "Buch hinterlegen" })).toBeNull();

    fireEvent.click(toggle);
    expect(toggle.getAttribute("aria-expanded")).toBe("true");
    expect(screen.getByRole("button", { name: "Buch hinterlegen" })).toBeTruthy();
  });

  // Rot, wenn der Leerfall das Formular hinter einem Klick versteckt – dort ist es der einzige nächste Schritt.
  it("öffnet im Leerfall gleich", () => {
    render(<AddPanel label="Weiteres Buch" defaultOpen><button type="submit">Buch hinterlegen</button></AddPanel>);
    expect(screen.getByRole("button", { name: "Buch hinterlegen" })).toBeTruthy();
  });

  // Rot, wenn `aria-controls` auch zugeklappt gesetzt ist: dann zeigt der Knopf auf eine Id, die nicht im DOM steht.
  it("verweist per aria-controls nur auf einen Inhalt, den es gibt", () => {
    render(<AddPanel label="Weiteres Buch"><span>Inhalt</span></AddPanel>);
    const toggle = screen.getByRole("button", { name: "Weiteres Buch" });
    expect(toggle.getAttribute("aria-controls")).toBeNull();

    fireEvent.click(toggle);
    const id = toggle.getAttribute("aria-controls");
    expect(id).toBeTruthy();
    expect(document.getElementById(id!)?.textContent).toBe("Inhalt");
  });

  // Rot, wenn das „+"/„−" in den Namen rutscht: dann wechselte der Knopf beim Öffnen seinen Namen.
  it("behält seinen Namen beim Öffnen", () => {
    render(<AddPanel label="Weiteres Buch"><span>Inhalt</span></AddPanel>);
    fireEvent.click(screen.getByRole("button", { name: "Weiteres Buch" }));
    expect(screen.getByRole("button", { name: "Weiteres Buch" })).toBeTruthy();
  });
});
