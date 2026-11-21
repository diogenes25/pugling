import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ConfigEditor, emptyExtra, emptyRow } from "./exerciseConfig";

/*
 * B-607 AK 1: das Zeilenfeld „Hinweis" im Anlegen-Formular einer Vokabel-Übung trägt die Erklärung, wo der Hinweis
 * erscheint. Zwei Zeilen, weil das Feld je Zeile steht – der Knopf muss an jeder hängen, nicht nur an der ersten.
 */
describe("Anlegen-Formular Vokabeln – Erklärung am Hinweis (B-607)", () => {
  // Rot, wenn `RowField label="Hinweis"` ohne `topic` bleibt (Ausgangszustand): dann gibt es keinen Knopf.
  it("jede Zeile trägt den Erklärungs-Knopf am Hinweis", () => {
    render(<ConfigEditor type="Vocabulary" rows={[emptyRow("Vocabulary"), emptyRow("Vocabulary")]}
      extra={emptyExtra("Vocabulary")} setExtra={() => {}} patchRow={() => {}} addRow={() => {}} removeRow={() => {}} />);
    expect(screen.getAllByRole("button", { name: /Erklärung zu „Hinweis"/ })).toHaveLength(2);
  });
});
