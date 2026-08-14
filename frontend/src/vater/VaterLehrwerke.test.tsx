import { describe, expect, it } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { createExerciseHref, seriesFilterQuery, UnitForm, type SeriesFilterValues } from "./VaterLehrwerke";
import type { TextbookSeriesResponse } from "../lib/types";

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

/*
 * Der Weg Unit → Übung. Geprüft wird die **Adresse**, weil genau sie die Arbeit spart: kommt die Auswahl
 * nicht als Query mit, steht im Anlege-Formular wieder die erste Reihe (frontend/CLAUDE.md).
 */
describe("createExerciseHref", () => {
  const series = (over: Partial<TextbookSeriesResponse> = {}): TextbookSeriesResponse => ({
    id: 7, name: "Access", slug: "access", schoolTypes: "None", isOwn: true, unitCount: 2,
    createdAt: "2026-08-12T00:00:00Z", subjectId: 3, ...over,
  });

  it("reicht Fach, Reihe und Unit ans Anlege-Formular durch", () => {
    expect(createExerciseHref(series(), 42))
      .toBe("/vater/exercises/neu?subjectId=3&seriesId=7&seriesUnitId=42");
  });

  it("lässt ein fehlendes Fach weg, statt subjectId=null zu schicken", () => {
    // Kann nur über einen Aufrufer ohne die `canHostExercises`-Schranke passieren – dann trägt die
    // Adresse lieber kein Fach als eines, das `Number(null) || ""` im Formular zu „irgendwas" macht.
    expect(createExerciseHref(series({ subjectId: null }), 42))
      .toBe("/vater/exercises/neu?seriesId=7&seriesUnitId=42");
  });
});

/*
 * Die Filterzeile über der Reihenliste (remark #1: „die Suche soll auch nach Fach selektiert werden").
 * Geprüft wird die Abbildung Formular → Abfrage, weil dort der stille Fehler sitzt: `Number("")` ist `0`,
 * und ein `subjectId=0` filterte auf ein Fach, das es nicht gibt – die Liste bliebe grundlos leer.
 */
describe("seriesFilterQuery", () => {
  const leer: SeriesFilterValues = { search: "", subjectId: "", publisherId: "", schoolTypes: "", grade: "" };

  it("reicht das gewählte Fach als Zahl an die Abfrage durch", () => {
    // Rot, sobald `subjectId` nicht mehr in die Abfrage wandert – der Fall, den remark #1 gemeldet hat.
    expect(seriesFilterQuery({ ...leer, subjectId: "3" }).subjectId).toBe(3);
  });

  it("lässt jede leere Auswahl ganz weg, statt 0 oder einen leeren String zu filtern", () => {
    // Rot bei `Number(v.subjectId)` ohne die Leer-Prüfung: dann stünde hier 0 statt undefined.
    expect(seriesFilterQuery(leer)).toEqual({
      search: undefined, subjectId: undefined, publisherId: undefined, schoolTypes: undefined, grade: undefined,
    });
  });

  it("trägt die übrigen Filter unverändert mit", () => {
    expect(seriesFilterQuery({ search: "Access", subjectId: "3", publisherId: "7", schoolTypes: "Gymnasium", grade: "6" }))
      .toEqual({ search: "Access", subjectId: 3, publisherId: 7, schoolTypes: "Gymnasium", grade: 6 });
  });
});
