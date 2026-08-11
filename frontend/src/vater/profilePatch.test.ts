import { describe, expect, it } from "vitest";
import { profileFormValues, profilePatch, type ProfileFormValues } from "./profilePatch";
import { FREETEXT_SUBJECT } from "./subjectField";
import type { CreatorProfileResponse, SchoolType } from "../lib/types";

/*
 * B-148. Die Regel des Fachlehrer-Formulars – dieselbe Fehlerklasse wie beim Lehrbuch, dieselbe Zeile.
 */

const profil = (o: Partial<CreatorProfileResponse> = {}): CreatorProfileResponse => ({
  id: 1, name: "Englisch 8", subjectId: 3, subjectName: "Englisch", schoolTypes: "Gymnasium" as SchoolType,
  gradeMin: 7, gradeMax: 8, seriesId: 5, sourceLang: "en", targetLang: "de", persona: null,
  didactics: null, defaultTypes: ["Vocabulary"], active: true, ...o,
} as CreatorProfileResponse);

const geladen = (o: Partial<CreatorProfileResponse> = {}) => profileFormValues(profil(o));
const patch = (form: ProfileFormValues, o: Partial<CreatorProfileResponse> = {}) => profilePatch(geladen(o), form);

describe("profileFormValues", () => {
  it("bildet ein Fach ohne Katalog-Id auf den Freitext-Sentinel ab", () => {
    expect(geladen({ subjectId: null, subjectName: "Erdkunde" }).subjectId).toBe(FREETEXT_SUBJECT);
  });
});

describe("profilePatch – nur das Geänderte", () => {
  it("schickt nichts, wenn nichts angefasst wurde", () => {
    expect(patch(geladen())).toBeNull();
  });

  it("erkennt eine geänderte Typenliste, auch bei gleicher Länge", () => {
    expect(patch({ ...geladen(), defaultTypes: ["Cloze"] })).toEqual({ defaultTypes: ["Cloze"] });
    expect(patch({ ...geladen(), defaultTypes: ["Vocabulary"] })).toBeNull();
  });

  it("erkennt eine Umsortierung der Typenliste NICHT als Änderung", () => {
    // Sichtbar ist die feste Reihenfolge der Pillen, gespeichert die Klick-Reihenfolge. Verglichen wird
    // als Menge, sonst schickte ein Ab- und Wiederanwählen eine „Änderung", die niemand sehen kann.
    expect(patch({ ...geladen({ defaultTypes: ["Vocabulary", "Cloze"] }), defaultTypes: ["Cloze", "Vocabulary"] },
      { defaultTypes: ["Vocabulary", "Cloze"] })).toBeNull();
  });

  it("schickt eine geleerte Sprache NICHT", () => {
    // Der Server kennt für die Sprachen keinen leeren Zustand (`seriesDerivation.ts`, FIELD_FALLBACKS):
    // ein `""` käme nie an, die Oberfläche meldete aber „Gespeichert.".
    expect(patch({ ...geladen(), sourceLang: "" })).toBeNull();
  });
});

describe("profilePatch – leeren gegen unverändert", () => {
  it("leert Reihe und Klassenstufen nur auf ausdrückliche Wahl", () => {
    expect(patch({ ...geladen(), seriesId: "" })).toEqual({ clearSeries: true });
    expect(patch({ ...geladen(), gradeMin: "" })).toEqual({ clearGradeMin: true });
  });

  /*
   * Die Schulart ist das Nachbarfeld mit derselben Fehlerklasse, gefunden vom `frontend-reviewer`:
   * Eine gespeicherte KOMBINATION ("Realschule, Gymnasium") ist im Pulldown nicht auszuwählen. Wird sie
   * im Ladezustand auf `None` normalisiert, stehen beide Seiten des Vergleichs auf `None` — die
   * Kombination überlebt zwar, aber „– für alle –" ist nicht mehr herstellbar. Genau der Tausch
   * (Zerstörung gegen Unerreichbarkeit), den Entscheidung 1 fürs Fach verworfen hat.
   */
  const kombi = { schoolTypes: "Realschule, Gymnasium" as SchoolType };

  it("lässt eine Schulart-Kombination unangetastet, wenn ein ANDERES Feld geändert wird", () => {
    expect(patch({ ...geladen(kombi), name: "Neu" }, kombi)).toEqual({ name: "Neu" });
  });

  it("lässt „für alle“ trotzdem herstellbar", () => {
    expect(patch({ ...geladen(kombi), schoolTypes: "None" as SchoolType }, kombi))
      .toEqual({ schoolTypes: "None" });
  });

  it("lässt ein Freitext-Fach unangetastet, wenn ein ANDERES Feld geändert wird", () => {
    const orphan = { subjectId: null, subjectName: "Erdkunde" };
    expect(patch({ ...geladen(orphan), name: "Neu" }, orphan)).toEqual({ name: "Neu" });
  });

  it("entfernt ein Freitext-Fach über „– fachneutral –“", () => {
    const orphan = { subjectId: null, subjectName: "Erdkunde" };
    expect(patch({ ...geladen(orphan), subjectId: "" }, orphan)).toEqual({ clearSubject: true });
  });
});
