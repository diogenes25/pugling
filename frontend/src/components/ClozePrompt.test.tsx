import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ClozePrompt } from "./ClozePrompt";
import { clozeParts } from "../lib/cloze";

/**
 * B-76: Zwei Lücken desselben Textes ergaben zwei zeichengleiche Karten. Der Server nennt jetzt die
 * gefragte Lücke, und hier wird geprüft, dass sie auch **sichtbar** anders ist – sonst wäre die
 * Reparatur auf halbem Weg stehen geblieben.
 */
describe("ClozePrompt", () => {
  const text = "A: {{1}}, how are you? B: I'm {{2}}, thank you.";

  it("hebt die gefragte Lücke hervor und lässt die übrigen neutral", () => {
    render(<ClozePrompt text={text} gapIndex={2} />);

    expect(screen.getByLabelText("gesuchte Lücke").textContent).toBe("?");
    expect(screen.getByLabelText("Lücke 1").textContent).toBe("…");
    // Genau eine ist die gefragte - sonst hätte das Kind wieder die Wahl.
    expect(screen.queryAllByLabelText("gesuchte Lücke")).toHaveLength(1);
  });

  it("zeigt die rohe Vorlagensyntax nirgends", () => {
    const { container } = render(<ClozePrompt text={text} gapIndex={1} />);

    expect(container.textContent).not.toContain("{{");
    expect(container.textContent).toContain("how are you?");
  });

  it("zeigt ohne Lückennummer schlicht den Text – jeder andere Übungstyp", () => {
    const { container } = render(<ClozePrompt text="to run – laufen" />);

    expect(container.textContent).toBe("to run – laufen");
    expect(screen.queryByLabelText("gesuchte Lücke")).toBeNull();
  });

  it("fällt auf den unveränderten Text zurück, wenn der Platzhalter fehlt (Altbestand)", () => {
    // Der Editor verhindert das beim Anlegen; für längst gespeicherte Übungen gilt das nicht.
    const { container } = render(<ClozePrompt text="Ein Satz ganz ohne Lücke." gapIndex={1} />);

    expect(container.textContent).toBe("Ein Satz ganz ohne Lücke.");
  });

  it("zerlegt den Text in Stücke und Lücken", () => {
    expect(clozeParts("a {{1}} b")).toEqual([{ text: "a " }, { gap: 1 }, { text: " b" }]);
    // Eine Lücke ganz am Anfang erzeugt kein leeres Textstück davor.
    expect(clozeParts("{{3}}!")).toEqual([{ gap: 3 }, { text: "!" }]);
  });
});
