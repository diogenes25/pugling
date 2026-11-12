import { describe, expect, it } from "vitest";
import { DELETE_EXERCISE_QUESTION, fachDerZeile } from "./VaterExercises";
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

/*
 * B-499 (wie B-418): Löschen geht nur bei einer unbenutzten Übung (sonst `409 exercise_in_use`). Die Frage darf darum
 * keine Wirkung auf Lehrpläne ankündigen. Rot, wenn die alte Ankündigung zurückkehrt oder die Frage nicht mehr sagt, was
 * wirklich passiert.
 */
describe("Übung löschen", () => {
  it("kündigt keine Wirkung auf Lehrpläne an und nennt, was verschwindet", () => {
    expect(DELETE_EXERCISE_QUESTION).not.toMatch(/Lehrpl|betroffen/);
    expect(DELETE_EXERCISE_QUESTION).toMatch(/aus dem Katalog/);
  });

  // B-499 (Review): eine unbenutzte Übung kann trotzdem gespielt worden sein – mit ihr geht der Lernstand der Kinder an
  // ihren Vokabeln (Cascade `ExerciseItem` → `ItemProgress`). Rot, wenn die Frage das verschweigt.
  it("nennt den Lernstand der Kinder, der mitgeht", () => {
    expect(DELETE_EXERCISE_QUESTION).toMatch(/Lernstand/);
  });
});
