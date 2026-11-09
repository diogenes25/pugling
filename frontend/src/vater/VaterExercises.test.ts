import { describe, expect, it } from "vitest";
import { fachDerZeile } from "./VaterExercises";
import type { SubjectResponse } from "../lib/types";

/** B-431, Review-Fund: das Fach einer Zeile in der gemischten Liste „Meine Übungen". */
describe("fachDerZeile", () => {
  const FAECHER = [{ id: 3, name: "Englisch" }] as SubjectResponse[];

  // Rot, wenn die Zeile ihr Fach auch bei gesetztem Fach-Filter nennt (dann stünde es doppelt da).
  it("schweigt, wenn ein Fach gefiltert ist", () => { expect(fachDerZeile(3, 3, FAECHER)).toBeNull(); });
  it("nennt das Fach in der gemischten Liste", () => { expect(fachDerZeile("", 3, FAECHER)).toBe("Englisch"); });
  // Rot, wenn „ohne Fach" erscheint, solange die Fächer noch laden oder ihr Abruf scheiterte (`null`).
  it("behauptet nichts, solange die Fächer fehlen", () => { expect(fachDerZeile("", 3, null)).toBeNull(); });
  // Rot, wenn eine unbekannte Id als „ohne Fach" gelesen wird – die Übung hat ja eins.
  it("nennt eine unbekannte Fach-Id als Nummer", () => { expect(fachDerZeile("", 9, FAECHER)).toBe("Fach #9"); });
  it("sagt „ohne Fach\" nur ohne Fach-Id", () => { expect(fachDerZeile("", null, FAECHER)).toBe("ohne Fach"); });
});
