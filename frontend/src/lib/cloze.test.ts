import { describe, expect, it } from "vitest";
import { clozeSentence } from "./cloze";

describe("clozeSentence", () => {
  // Der Fall aus der Bonus-Auswertung: nummerierte Aufgaben ohne Satzpunkt – nur die Nummern trennen sie.
  const tags = "1. Your sister plays the drums, {{1}} 2. We aren't late, {{2}} 3. You can swim, {{3}}";

  it("schneidet nummerierte Aufgaben an ihrer Nummer", () => {
    // Rot, wenn die Nummer nicht als Grenze zählt: dann käme der ganze Text.
    expect(clozeSentence(tags, 1)).toBe("1. Your sister plays the drums, {{1}}");
    expect(clozeSentence(tags, 2)).toBe("2. We aren't late, {{2}}");
    // Rot, wenn das Ende nicht bis zum Textende reicht.
    expect(clozeSentence(tags, 3)).toBe("3. You can swim, {{3}}");
  });

  it("schneidet Fließtext am Satzende, Lücke mitten im Satz", () => {
    // Rot, wenn nur der Text VOR der Lücke genommen würde: der Rest des Satzes fehlte.
    expect(clozeSentence("The cat {{1}} on the mat. Then it {{2}} away!", 1)).toBe("The cat {{1}} on the mat.");
    expect(clozeSentence("The cat {{1}} on the mat. Then it {{2}} away!", 2)).toBe("Then it {{2}} away!");
  });

  it("bleibt beim ganzen Text, wenn die Lücke fehlt oder keine gefragt ist", () => {
    expect(clozeSentence("A {{1}}. B {{2}}.", 7)).toBe("A {{1}}. B {{2}}.");
    expect(clozeSentence("A {{1}}. B {{2}}.", null)).toBe("A {{1}}. B {{2}}.");
  });
});
