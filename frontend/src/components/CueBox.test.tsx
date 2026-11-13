import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { CueBox } from "./CueBox";

/**
 * Der Wortkasten (B-524) zeigt, was der Server schickt, in dessen Reihenfolge – sortiert hat der Server, damit
 * die Position nichts über die Zuordnung verrät. Ohne Kasten (andere Stufe) rendert er nichts.
 */
describe("CueBox", () => {
  // Rot, wenn der Baustein selbst umsortiert: „Rassismus“ stünde dann vor „Gemeinschaft“ nicht mehr hinten.
  it("zeigt die Wörter in der Reihenfolge des Servers", () => {
    render(<CueBox words={["Gesundheit", "Gemeinschaft", "Rassismus"]} label="Wortkasten" />);
    const box = screen.getByRole("group", { name: "Wortkasten" });
    expect([...box.querySelectorAll("li")].map((li) => li.textContent)).toEqual(["Gesundheit", "Gemeinschaft", "Rassismus"]);
  });

  // Rot, wenn ein leerer oder fehlender Kasten eine leere Überschrift hinterließe.
  it("rendert nichts ohne Wörter", () => {
    const { container: a } = render(<CueBox words={null} label="Wortkasten" />);
    const { container: b } = render(<CueBox words={[]} label="Wortkasten" />);
    expect(a.textContent).toBe("");
    expect(b.textContent).toBe("");
  });
});
