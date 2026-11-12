import { describe, expect, it } from "vitest";
import { choicesClass, isWordBank, WORD_BANK_MIN } from "./choices";

/** B-489: Wortbank als Chips, Multiple-Choice vollbreit – entschieden an der Menge. */
describe("isWordBank", () => {
  // Rot, wenn die Grenze auf vier oder darunter fällt: dann würden MC-Optionen zu schmalen Chips.
  it("lässt eine übliche Multiple-Choice mit vier Optionen vollbreit", () => {
    expect(isWordBank(["a", "b", "c", "d"])).toBe(false);
    expect(choicesClass(["a", "b", "c", "d"])).toBe("row");
  });

  // Rot, wenn die Grenze über 15 steigt oder die Klasse fehlt: dann stünde die Wortbank wieder Wort für Wort untereinander.
  it("setzt eine Wortbank von 15 Wörtern als Chips", () => {
    const bank = Array.from({ length: 15 }, (_, i) => `w${i}`);
    expect(isWordBank(bank)).toBe(true);
    expect(choicesClass(bank)).toBe("row choices-bank");
  });

  // Rot bei einem Vergleichsfehler an der Grenze (> statt >=).
  it("zählt genau ab der Grenze als Wortbank", () => {
    expect(isWordBank(Array.from({ length: WORD_BANK_MIN }, (_, i) => `${i}`))).toBe(true);
    expect(isWordBank(Array.from({ length: WORD_BANK_MIN - 1 }, (_, i) => `${i}`))).toBe(false);
  });

  it("verträgt fehlende Auswahl", () => {
    expect(isWordBank(null)).toBe(false);
  });
});
