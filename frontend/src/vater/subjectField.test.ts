import { describe, expect, it } from "vitest";
import { FREETEXT_SUBJECT, subjectFormValue, subjectPatch } from "./subjectField";

/*
 * B-148. Die Regel des Fach-Felds, geteilt von drei Formularen (Reihe, Lehrbuch, Fachlehrer-Profil).
 *
 * Sie liegt in einer eigenen Datei, weil ihre teuerste Zusicherung unsichtbar ist: „der Sentinel erreicht
 * den PATCH-Rumpf nie". Das sieht man weder am Bildschirm noch im Netzwerk-Tab eines Durchlaufs, in dem
 * niemand das Feld anfasst — nur hier.
 */

describe("subjectFormValue – drei Zustände, nicht zwei", () => {
  it("bildet ein Katalog-Fach auf seine Id ab", () => {
    expect(subjectFormValue({ subjectId: 7, subjectName: "Englisch" })).toBe("7");
  });

  it("bildet ein Fach ohne Katalog-Id auf den Freitext-Sentinel ab", () => {
    // Der Zustand nach einem gelöschten Fach: `SetNull` räumt die Id, der Name bleibt als Rückfallebene.
    expect(subjectFormValue({ subjectId: null, subjectName: "Erdkunde" })).toBe(FREETEXT_SUBJECT);
  });

  it("bildet „gar kein Fach“ auf den leeren String ab", () => {
    expect(subjectFormValue({ subjectId: null, subjectName: null })).toBe("");
  });

  it("liest einen fehlenden Namen wie keinen Namen", () => {
    // Die drei Response-Typen unterscheiden sich darin, ob das Feld optional oder nullable ist.
    expect(subjectFormValue({ subjectId: null, subjectName: "" })).toBe("");
    expect(subjectFormValue({})).toBe("");
  });

  it("kollidiert nicht mit einer Fach-Id", () => {
    // Eine Eigenschaft der Wahl, nicht des Typs: `FREETEXT_SUBJECT` darf nie das Ergebnis von
    // `String(id)` sein können, sonst wäre der Anzeigezustand von einem echten Fach nicht zu trennen.
    expect(Number.isNaN(Number(FREETEXT_SUBJECT))).toBe(true);
  });
});

describe("subjectPatch – was ins PATCH gehört", () => {
  it("schickt nichts, solange das Feld unverändert ist", () => {
    expect(subjectPatch("7", "7")).toBeNull();
    expect(subjectPatch("", "")).toBeNull();
  });

  it("schickt beim Wechsel nur die Id – den Namen leitet der Server ab", () => {
    expect(subjectPatch("7", "9")).toEqual({ subjectId: 9 });
  });

  it("schickt beim Leeren nur den Schalter – er räumt Id UND Namen", () => {
    expect(subjectPatch("7", "")).toEqual({ clearSubject: true });
  });

  /*
   * Die beiden Fälle, an denen die ganze Story hängt.
   *
   * Der erste ist der Defekt von B-148: Ein Freitext-Fach bleibt stehen, solange niemand das Feld anfasst
   * — vorher schickte das Formular bei JEDEM Speichern `clearSubject`, weil es den Schalter aus dem
   * Momentanwert ableitete statt aus einem Vergleich.
   *
   * Der zweite ist der Defekt von B-143, den ein reiner Vergleich neu erzeugt hätte: Ohne ihn wäre der
   * Freitext-Name unzerstörbar UND unentfernbar.
   */
  it("lässt ein Freitext-Fach unangetastet, solange niemand es anfasst", () => {
    expect(subjectPatch(FREETEXT_SUBJECT, FREETEXT_SUBJECT)).toBeNull();
  });

  it("entfernt ein Freitext-Fach über die Leer-Option", () => {
    expect(subjectPatch(FREETEXT_SUBJECT, "")).toEqual({ clearSubject: true });
  });

  it("lässt den Sentinel NIE in den Rumpf", () => {
    // Über die Oberfläche unerreichbar (die Option ist `disabled`) – aber „unerreichbar, weil das
    // Formular es verhindert" ist die Art Zusicherung, die beim nächsten Umbau kippt. `Number(…)` wäre
    // hier `NaN`, und der Server bekäme ein Feld, das er nicht deuten kann.
    expect(subjectPatch("", FREETEXT_SUBJECT)).toBeNull();
    expect(subjectPatch("7", FREETEXT_SUBJECT)).toBeNull();
  });
});
