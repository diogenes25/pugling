import { describe, expect, it } from "vitest";
import { textbookFormValues, textbookPatch, type TextbookFormValues } from "./textbookPatch";
import { FREETEXT_SUBJECT } from "./subjectField";
import type { TextbookResponse } from "../lib/types";

/*
 * B-148. Die Regel des Lehrbuch-Formulars, Vorbild `seriesPatch.test.ts`.
 *
 * Der Defekt, den diese Datei festhält: Der Rumpf entstand aus dem Momentanwert
 * (`clearSubject: dto.subjectId == null`), und weil das Formular ein Freitext-Fach nicht darstellen
 * konnte, hieß „das Feld zeigt nichts" fälschlich „der Nutzer hat geleert" — jedes Speichern eines
 * beliebigen anderen Feldes zerstörte den Fachnamen.
 */

const buch = (o: Partial<TextbookResponse> = {}): TextbookResponse => ({
  id: 1, title: "Access 8", subjectId: 3, subjectName: "Englisch", grade: 8, publisher: "Cornelsen",
  isbn: null, seriesId: 5, currentUnitId: 9, currentChapter: "Unit 4", seriesName: null,
  currentUnitLabel: null, ...o,
} as TextbookResponse);

const geladen = (o: Partial<TextbookResponse> = {}) => textbookFormValues(buch(o));
const patch = (form: TextbookFormValues, o: Partial<TextbookResponse> = {}) => textbookPatch(geladen(o), form);

describe("textbookFormValues", () => {
  it("trägt jedes Feld als String, leere Werte als leeren String", () => {
    expect(geladen({ grade: null, publisher: null, seriesId: null, currentUnitId: null, currentChapter: null }))
      .toEqual({ title: "Access 8", subjectId: "3", grade: "", publisher: "", seriesId: "", currentUnitId: "", currentChapter: "" });
  });

  it("bildet ein Fach ohne Katalog-Id auf den Freitext-Sentinel ab", () => {
    expect(geladen({ subjectId: null, subjectName: "Erdkunde" }).subjectId).toBe(FREETEXT_SUBJECT);
  });
});

describe("textbookPatch – nur das Geänderte", () => {
  it("schickt nichts, wenn nichts angefasst wurde", () => {
    expect(patch(geladen())).toBeNull();
  });

  it("schickt nur das eine geänderte Feld", () => {
    expect(patch({ ...geladen(), currentChapter: "Unit 5" })).toEqual({ currentChapter: "Unit 5" });
  });

  it("schickt beim Fachwechsel nur die Id – den Namen leitet der Server ab", () => {
    expect(patch({ ...geladen(), subjectId: "4" })).toEqual({ subjectId: 4 });
  });
});

describe("textbookPatch – leeren gegen unverändert", () => {
  it("leert Klasse, Reihe und Unit nur auf ausdrückliche Wahl", () => {
    expect(patch({ ...geladen(), grade: "" })).toEqual({ clearGrade: true });
    expect(patch({ ...geladen(), currentUnitId: "" })).toEqual({ clearUnit: true });
  });

  it("schickt beim Entfernen der Reihe keinen zweiten Schalter für die Unit", () => {
    // Der Server räumt die Unit über `clearSeries` mit; `clearUnit` daneben wäre Rauschen, und ein
    // Leser müsste raten, ob die beiden dasselbe oder Verschiedenes bedeuten.
    expect(patch({ ...geladen(), seriesId: "", currentUnitId: "" })).toEqual({ clearSeries: true });
  });

  /*
   * Die beiden Fälle, um die es in dieser Story geht.
   */
  it("lässt ein Freitext-Fach unangetastet, wenn ein ANDERES Feld geändert wird", () => {
    const orphan = { subjectId: null, subjectName: "Erdkunde" };
    expect(patch({ ...geladen(orphan), currentChapter: "Unit 5" }, orphan))
      .toEqual({ currentChapter: "Unit 5" });
  });

  it("entfernt ein Freitext-Fach über „– keine Angabe –“", () => {
    const orphan = { subjectId: null, subjectName: "Erdkunde" };
    expect(patch({ ...geladen(orphan), subjectId: "" }, orphan)).toEqual({ clearSubject: true });
  });
});
