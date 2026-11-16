import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { DefaultStageSelect, hasDefaultStage } from "./exerciseConfig";

/*
 * B-599, Entscheidung 5 – die Standard-Abfrageform kommt aus dem Server-Manifest, für Vokabeln wie für den Lückentext.
 * Je Fall steht dabei, welche Änderung ihn rot macht.
 */
describe("DefaultStageSelect", () => {
  it("bietet „Standard“ und genau die Stufen des Manifests an und gibt Zahl oder leer zurück", () => {
    // Rot, wenn wieder eine feste Liste im Frontend stünde (die drei Lückentext-Stufen fehlten) oder die Auswahl
    // eine Zeichenkette statt der Stufennummer meldete.
    const onChange = vi.fn();
    render(<DefaultStageSelect stages={[{ value: 6, label: "Übersetzung + Buchstabenkästchen" }, { value: 8, label: "Übersetzung + Auswahl je Lücke" }]}
      value="" onChange={onChange} />);
    const feld = screen.getByRole("combobox", { name: "Standard-Abfrageform" }) as HTMLSelectElement;
    expect([...feld.options].map((o) => o.text)).toEqual(
      ["Standard (Vorgabe des Typs)", "Übersetzung + Buchstabenkästchen", "Übersetzung + Auswahl je Lücke"]);
    fireEvent.change(feld, { target: { value: "8" } });
    expect(onChange).toHaveBeenLastCalledWith(8);
    fireEvent.change(feld, { target: { value: "" } });
    expect(onChange).toHaveBeenLastCalledWith("");
  });

  it("gilt für Vokabeln und Lückentext, nicht für andere Typen", () => {
    // Rot, wenn der Lückentext das Feld nicht bekäme (der Stand vor B-599).
    expect(["Vocabulary", "Cloze", "Matching"].map(hasDefaultStage)).toEqual([true, true, false]);
  });

  it("zeigt einen gespeicherten Wert, den die Liste noch nicht kennt, statt „Standard“", () => {
    // Rot, wenn das Feld während des Ladens des Manifests „Standard“ zeigte, obwohl die Übung Stufe 8 gespeichert hat.
    render(<DefaultStageSelect stages={[]} value={8} onChange={() => {}} />);
    const feld = screen.getByRole("combobox", { name: "Standard-Abfrageform" }) as HTMLSelectElement;
    expect(feld.selectedOptions[0].text).toBe("Stufe 8");
  });
});
