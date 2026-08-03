import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Passage } from "./Passage";

/**
 * B-75: Der Lesetext kam beim Kind gar nicht an. Jetzt kommt er – und muss auch bedienbar sein: Der
 * Kasten ist gedeckelt und scrollt, was ihn ohne `tabIndex` für die Tastatur unerreichbar machte und ohne
 * Namen für den Screenreader zu einem unangekündigten Textblock direkt vor der Frage.
 */
describe("Passage", () => {
  it("zeigt den Text als benannten, mit der Tastatur erreichbaren Bereich", () => {
    render(<Passage text="Tom goes to Brighton in July." />);

    const box = screen.getByRole("group", { name: "Text zur Aufgabe" });
    expect(box.textContent).toBe("Tom goes to Brighton in July.");
    // Ein scrollendes <div> ohne fokussierbaren Inhalt fokussieren Chrome und Safari nicht von selbst.
    expect(box.getAttribute("tabindex")).toBe("0");
  });

  it("nimmt einen eigenen Namen an – ein Lesetext ist keine Anweisung", () => {
    render(<Passage text="Setze das Verb ins Simple Past." label="Anweisung" />);

    expect(screen.getByRole("group", { name: "Anweisung" }).textContent)
      .toBe("Setze das Verb ins Simple Past.");
  });

  it("rendert nichts, wo die Übung kein Material hat", () => {
    // Der Normalfall: Vokabeln, Übersetzung, Rechnen – und ein Hörverstehen, dessen Transkript bewusst
    // draußen bleibt. Ein leerer Kasten sähe dort wie ein Ladefehler aus.
    const { container } = render(<Passage text={null} />);

    expect(container.firstChild).toBeNull();
  });

  it("rendert nichts bei einem leeren Text", () => {
    // Der Server normalisiert Leerstrings zwar zu null, aber das Bauteil verlässt sich nicht darauf.
    const { container } = render(<Passage text="" />);

    expect(container.firstChild).toBeNull();
  });
});
