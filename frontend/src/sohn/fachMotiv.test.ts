import { describe, expect, it } from "vitest";
import { fachMotiv } from "./fachMotiv";

// Rot wird jeder Fall, sobald die Erkennung am Fachnamen bricht – etwa wenn die Umlaut-Normalisierung fehlt
// („Französisch“) oder ein Muster zu gierig trifft („Arten“ ist nicht Kunst).
describe("fachMotiv", () => {
  it.each([
    ["Englisch", "en", "flagge"],
    ["Französisch", "fr", "flagge"],
    ["franzoesisch", "fr", "flagge"],
    ["Latein", "la", "flagge"],
    ["Mathematik", "mathe", "muster"],
    ["Biologie", "bio", "muster"],
    ["Kunst", "kunst", "muster"],
  ])("%s → %s", (name, key, art) => {
    expect(fachMotiv(name)).toMatchObject({ key, art });
  });

  it("gibt einem unbekannten Fach kein eigenes Motiv – es behält die Reihenfarbe seines Platzes", () => {
    expect(fachMotiv("Arten und Lebensräume")).toBeNull();
    expect(fachMotiv("")).toBeNull();
  });

  it("färbt Englisch rot und Französisch blau, unabhängig von der Reihenfolge der Fächer", () => {
    expect(fachMotiv("Englisch")?.farbe[0]).toBe("#ff4d6d");
    expect(fachMotiv("Französisch")?.farbe[0]).toBe("#2f6bff");
  });
});
