import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import { normalizeAnswer } from "./exerciseConfig";

/**
 * B-493: dieselbe Fallliste wie Server (`FalllisteAntwortvergleichTests`) und Lehrwerk-Lader (`test_laden.py`). Wahr ist
 * der Server; fällt hier ein Fall, vergleicht das Vater-Web anders als die Wertung – der Editor sagte dann „steht nicht in
 * der Auswahl", obwohl der Server die Antwort darin findet, oder umgekehrt.
 */
const liste = JSON.parse(readFileSync(resolve(process.cwd(), "../docs/fallliste-antwortvergleich.json"), "utf-8")) as {
  faelle: { a: string; b: string; gleich: boolean; warum: string }[];
};

describe("normalizeAnswer folgt der Fallliste des Servers (B-493)", () => {
  it("hat Fälle", () => { expect(liste.faelle.length).toBeGreaterThan(10); });

  for (const fall of liste.faelle)
    it(fall.warum, () => {
      expect(normalizeAnswer(fall.a) === normalizeAnswer(fall.b)).toBe(fall.gleich);
    });
});
