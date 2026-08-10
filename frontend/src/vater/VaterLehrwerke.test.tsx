import { describe, expect, it } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { UnitForm } from "./VaterLehrwerke";

/*
 * Der Regressionstest zu B-129. `UnitForm` löst keinen Netzaufruf aus, solange nicht abgesendet wird –
 * hier wird nur das Themen-Feld bedient, also ein Baustein-Test und kein nachgebauter Bildschirm
 * (siehe frontend/CLAUDE.md).
 *
 * Der Zugriff läuft über den Entfernen-Knopf des Chips (`aria-label`), nicht über seinen Text: der Chip
 * enthält Text UND Knopf, sein `textContent` ist also „Thema×" und trifft keinen exakten Textvergleich.
 */
describe("UnitForm – Themen-Eingabe", () => {
  const feld = () => screen.getByLabelText("Themen der Unit");
  const chip = (thema: string) => screen.queryByRole("button", { name: `Thema ${thema} entfernen` });

  it("verwirft eine angefangene Eingabe bei Escape, statt sie beim Wegklicken anzulegen", () => {
    render(<UnitForm seriesId={1} onDone={() => {}} />);

    fireEvent.change(feld(), { target: { value: "Halbfertig" } });
    fireEvent.keyDown(feld(), { key: "Escape" });
    fireEvent.blur(feld());

    expect(chip("Halbfertig")).toBeNull();
    expect((feld() as HTMLInputElement).value).toBe("");
  });

  it("rettet weiterhin eine Eingabe, die ohne Enter verlassen wird", () => {
    render(<UnitForm seriesId={1} onDone={() => {}} />);

    fireEvent.change(feld(), { target: { value: "Gerettet" } });
    fireEvent.blur(feld());

    expect(chip("Gerettet")).not.toBeNull();
  });

  it("fügt bei Enter hinzu, ohne das Formular abzuschicken", () => {
    render(<UnitForm seriesId={1} onDone={() => {}} />);

    fireEvent.change(feld(), { target: { value: "Mit Enter" } });
    fireEvent.keyDown(feld(), { key: "Enter" });

    expect(chip("Mit Enter")).not.toBeNull();
    expect((feld() as HTMLInputElement).value).toBe("");
  });
});
