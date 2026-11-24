import i18next from "i18next";
import { describe, expect, it } from "vitest";
import "../lib/i18n";
import { notenstufeText } from "./notenstufe";

// Deutsch ist die Sprache des Kindes und der Ort, an dem die Regel gemessen wird (die Wörter „Vierer“ & Co.).
const t = i18next.getFixedT("de");

/** Die Stufenwörter, die unter Stufe 3 nie erscheinen dürfen (B-682 AK 7, B-743 AK 1). */
const SCHLECHTE_WOERTER = /vierer|fünfer|sechser|4er|5er|6er/i;

describe("notenstufeText", () => {
  // AK 1, Stufe 1 bis 3: der Stand-Text steht da. Rot, wenn eine dieser Stufen nur den Zielsatz trüge.
  it.each([
    [1, null, "Einser-Stand"],
    [2, 4, "Zweier-Stand"],
    [3, 9, "Dreier-Stand"],
  ])("Stufe %i nennt ihren Stand", (level, toNext, stand) => {
    expect(notenstufeText(t, level, toNext)).toContain(stand);
  });

  // AK 1, Stufe 4 bis 6: nur der Zielsatz, kein Stufenwort. Rot, wenn die Funktion für jede Stufe einen Namen bildete
  // (etwa `levelStand${level}` ohne die Grenze bei 3) – dann stünde dort der Schlüssel oder ein „Vierer-Stand“.
  it.each([4, 5, 6])("Stufe %i trägt nur das nächste Ziel", (level) => {
    const text = notenstufeText(t, level, 12);
    expect(text).toBe("noch 12 Wörter sicher bis zur nächsten Stufe");
    expect(text).not.toMatch(SCHLECHTE_WOERTER);
    expect(text).not.toMatch(/stand/i);
  });

  // Zielsatz im Singular und „erreicht“: 0 heißt, die Stufe kommt mit dem nächsten Tag (der Server bewertet täglich).
  // Rot, wenn 0 als „noch 0 Wörter“ erschiene.
  it("sagt bei 0 fehlenden Wörtern, dass die Stufe ab morgen zählt, und kennt den Singular", () => {
    expect(notenstufeText(t, 5, 0)).toBe("nächste Stufe geschafft – sie zählt ab morgen");
    expect(notenstufeText(t, 4, 1)).toBe("noch 1 Wort sicher bis zur nächsten Stufe");
  });

  // AK 2, Absichtserklärung: die Funktion kennt die vorige Stufe nicht und kann einen Abstieg darum gar nicht
  // kommentieren – nach einem Abstieg sieht die Karte aus wie jede andere Stufe. Diese Fälle halten nur fest, dass die
  // neue Stufe nie „abgestiegen“ sagt, unter 3 kein Stufenwort trägt und den nächsten Schritt nennt; rot werden sie
  // erst, wenn jemand die Texte in `de.ts` so ändert. Ob eine auf 2 oder 3 gesunkene Stufe ihren Namen tragen darf,
  // ist offen (B-743, `wartet_auf`).
  it.each([
    [2, 3],
    [4, 5],
    [5, 8],
    [6, 20],
  ])("nach einem Abstieg auf %i: neue Stufe, Weg zurück, kein Tadel", (nachher, toNext) => {
    const text = notenstufeText(t, nachher, toNext);
    expect(text).not.toMatch(/abgestiegen|abstieg|schlechter/i);
    if (nachher > 3) expect(text).not.toMatch(SCHLECHTE_WOERTER);
    expect(text).toContain(`noch ${toNext} Wörter sicher bis zur nächsten Stufe`);
  });
});
