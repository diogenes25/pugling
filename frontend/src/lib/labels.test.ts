import { describe, expect, it } from "vitest";
import { signedAmount } from "./labels";

/** B-425: ein Buchungsbetrag trägt seine Währung – sonst liest sich ein Gem-Geschenk als Münzen. */
describe("signedAmount", () => {
  // AK 3. Rot, wenn ein Gem-Eintrag ohne 💎 erscheint (Symbol weggelassen oder fest 🪙).
  it("zeigt Gems mit 💎", () => { expect(signedAmount(300, "Gems")).toBe("+300 💎"); });
  // Rot, wenn Münzen das Gem-Symbol bekommen.
  it("zeigt Münzen mit 🪙", () => { expect(signedAmount(50, "Coins")).toBe("+50 🪙"); });
  // Rot, wenn ein Abzug ein doppeltes oder gar kein Vorzeichen trägt.
  it("zeigt einen Abzug mit seinem Minus", () => { expect(signedAmount(-5, "Coins")).toBe("\u22125 🪙"); });
});
