import { describe, expect, it } from "vitest";
import { ledgerLabelKey, pointKindLabel, signedAmount } from "./labels";

/** B-425: ein Buchungsbetrag trägt seine Währung – sonst liest sich ein Gem-Geschenk als Münzen. */
describe("signedAmount", () => {
  // AK 3. Rot, wenn ein Gem-Eintrag ohne 💎 erscheint (Symbol weggelassen oder fest 🪙).
  it("zeigt Gems mit 💎", () => { expect(signedAmount(300, "Gems")).toBe("+300 💎"); });
  // Rot, wenn Münzen das Gem-Symbol bekommen.
  it("zeigt Münzen mit 🪙", () => { expect(signedAmount(50, "Coins")).toBe("+50 🪙"); });
  // Rot, wenn ein Abzug ein doppeltes oder gar kein Vorzeichen trägt.
  it("zeigt einen Abzug mit seinem Minus", () => { expect(signedAmount(-5, "Coins")).toBe("\u22125 🪙"); });
});

/** B-769 AK 4: ein positiver Malus-Eintrag ist die Rücknahme nach einer berechtigten Meldung, kein gerissenes Ziel. */
describe("Malus nach Vorzeichen", () => {
  // Rot, wenn beide Vorzeichen gleich beschriftet sind.
  it("beschriftet die Rücknahme anders als den Malus", () => {
    expect(pointKindLabel("GoalPenalty", 5)).toBe("Malus zurückgenommen");
    expect(pointKindLabel("GoalPenalty", -5)).toBe("Pflicht gerissen (Malus)");
  });
  // Die Sohn-Arcade liest den Schlüssel ihrer Übersetzung hierüber. Rot, wenn er das Vorzeichen nicht beachtet.
  it("liefert der Sohn-Arcade den Schlüssel der Rücknahme", () => {
    expect(ledgerLabelKey("GoalPenalty", 5)).toBe("GoalPenaltyRefund");
    expect(ledgerLabelKey("GoalPenalty", -5)).toBe("GoalPenalty");
    expect(ledgerLabelKey("Goal", 5)).toBe("Goal");
  });
});
