import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { RecognitionField } from "./exerciseConfig";

/*
 * B-597, Entscheidung 7 – das Feld heißt „Erkennen“, nennt die Richtungen in Wörtern und sagt fest dazu, dass immer das
 * Fremdwort geschrieben wird. Je Fall steht dabei, welche Änderung ihn rot macht.
 */
describe("RecognitionField", () => {
  it("bietet die drei Richtungen unter dem Namen „Erkennen“ an und schreibt den Vertragswert zurück", () => {
    // Rot, wenn das Feld wieder „Abfragerichtung“ hieße, eine Option fehlte oder ein anderer Wert als der des
    // Vertrags (`direction`) zurückkäme.
    const onChange = vi.fn();
    render(<RecognitionField value="front-to-back" onChange={onChange} />);
    const feld = screen.getByRole("combobox", { name: "Erkennen" }) as HTMLSelectElement;
    expect([...feld.options].map((o) => [o.value, o.text])).toEqual([
      ["front-to-back", "Fremdwort → Deutsch"],
      ["back-to-front", "Deutsch → Fremdwort"],
      ["both", "beides"],
    ]);
    fireEvent.change(feld, { target: { value: "both" } });
    expect(onChange).toHaveBeenCalledWith("both");
  });

  it("sagt fest am Feld, dass immer das Fremdwort geschrieben wird", () => {
    // Rot, wenn der Hinweis fehlte oder nicht als Beschreibung am Feld hinge (ein Screenreader hörte ihn dann nicht).
    render(<RecognitionField value={undefined} onChange={() => {}} />);
    const feld = screen.getByRole("combobox", { name: "Erkennen" });
    const hinweis = screen.getByText("Geschrieben wird immer das Fremdwort.");
    expect(feld.getAttribute("aria-describedby")).toBe(hinweis.id);
    // Ohne gespeicherten Wert die Vorgabe der Anlege-Wege (Entscheidung 8).
    expect((feld as HTMLSelectElement).value).toBe("front-to-back");
  });
});
