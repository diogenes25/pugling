import { render, renderHook, screen } from "@testing-library/react";
import { useState } from "react";
import { describe, expect, it } from "vitest";

/**
 * Bewacht die **Werkzeugkette** der Komponententests, nicht das Produkt: dass React Testing Library
 * unter `happy-dom` überhaupt rendert, dass `renderHook` da ist (darauf baut die Sperre in B-43 auf)
 * und dass `src/test-setup.ts` das DOM zwischen den Fällen abräumt.
 *
 * Der Aufräum-Fall ist der eigentliche Grund für diese Datei. Ohne ihn ist ein ausgehängtes
 * `setupFiles` **unsichtbar**, bis irgendwann ein fremder Test in fremder Reihenfolge kippt.
 */
function Marker() {
  return <p>genau einmal im Dokument</p>;
}

function useZaehler() {
  const [wert, setWert] = useState(0);
  return { wert, hoch: () => setWert((v) => v + 1) };
}

describe("Werkzeugkette der Komponententests", () => {
  it("rendert ein Bauteil und findet es im DOM", () => {
    render(<Marker />);
    // `getByText` **wirft** bei zwei Treffern – deshalb ist dieser Fall die andere Hälfte des
    // Aufräum-Beweises und kein bloßer Rauchtest: läuft er als zweiter, kippt er statt des Falls
    // unten. Wer ihn auf `queryByText` oder `getAllByText(…)[0]` umschreibt, halbiert den Wächter.
    expect(screen.getByText("genau einmal im Dokument")).toBeDefined();
  });

  it("räumt vor diesem Fall auf – der Text des Vorgängers steht nicht mehr da", () => {
    // Fehlt das `cleanup`, sind es zwei Treffer. Gemessen: ohne `setupFiles` ist der Lauf in **jeder**
    // gewürfelten Reihenfolge rot, mit ihm grün – einer der beiden Fälle erwischt es immer.
    render(<Marker />);
    expect(screen.getAllByText("genau einmal im Dokument")).toHaveLength(1);
  });

  it("gibt einen Hook einzeln aus, ohne Bildschirm ringsum", () => {
    const { result } = renderHook(() => useZaehler());
    expect(result.current.wert).toBe(0);
  });
});
