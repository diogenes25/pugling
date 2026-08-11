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

const geladen = (o: Partial<CreatorProfileResponse> = {}) => {
  const p = profil(o);
  const schoolTypes = (p.schoolTypes === "Gymnasium" ? "Gymnasium" : "None") as SchoolType;
  return profileFormValues(p, schoolTypes, "en", "de");
};
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

  it("lässt ein Freitext-Fach unangetastet, wenn ein ANDERES Feld geändert wird", () => {
    const orphan = { subjectId: null, subjectName: "Erdkunde" };
    expect(patch({ ...geladen(orphan), name: "Neu" }, orphan)).toEqual({ name: "Neu" });
  });

  it("entfernt ein Freitext-Fach über „– fachneutral –“", () => {
    const orphan = { subjectId: null, subjectName: "Erdkunde" };
    expect(patch({ ...geladen(orphan), subjectId: "" }, orphan)).toEqual({ clearSubject: true });
  });
});
