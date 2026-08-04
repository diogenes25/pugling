import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { RevealAlternatives } from "./RevealAlternatives";

/**
 * B-70: Beim Aufdecken sah das Kind nur die primäre Übersetzung und trug sich für eine gleichwertige Antwort
 * selbst als falsch ein. Geprüft wird beides – dass die Alternativen ankommen **und** dass der Regelfall ohne
 * Alternative unverändert stumm bleibt (sonst stünde unter jeder Karte eine leere Zeile).
 */
describe("RevealAlternatives", () => {
  it("nennt jede gleichwertige Antwort", () => {
    render(<RevealAlternatives alternatives={["sehr groß", "enorm"]} />);

    expect(screen.getByText(/auch richtig:/).textContent).toBe("auch richtig: sehr groß · enorm");
  });

  /**
   * Die drei Formen, die der Vertrag zulässt: das Feld ist optional und nullable, und eine leere Liste ist
   * dasselbe wie „keine Alternative" – ein älteres Backend schickt es gar nicht mit.
   */
  it("schweigt ohne Alternativen", () => {
    for (const leer of [undefined, null, []] as const) {
      const { container } = render(<RevealAlternatives alternatives={leer} />);
      expect(container.textContent).toBe("");
    }
  });
});
