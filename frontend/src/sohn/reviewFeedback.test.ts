import { describe, expect, it } from "vitest";
import { reviewFeedback } from "./SohnPractice";

/**
 * Die Regel, welches Feedback eine Review-Antwort auslöst – als reine Funktion geprüft (Muster
 * `SelfAssessAnswer.test.tsx`), damit sie ohne Bildschirm und `fetch` testbar bleibt.
 *
 * Der Fehler, den dieser Test verhindert (gefunden im Review zu B-96): eine ShowBoth-Karte
 * ("Kennenlernen") wird vom Server nie gewertet – `wasCorrect` steht dort schlicht auf `false`, weil
 * kein `wasKnown` mitgeschickt wird. Ohne die `displayOnly`-Sonderregel läse das Kind nach jeder
 * betrachteten Karte ein „Leider nicht." – ein Urteil, das die Stufe per Story-Vorgabe (B-96,
 * Akzeptanzkriterium 2) gerade NICHT fällen darf.
 */
describe("reviewFeedback", () => {
  const outcome = (overrides: Partial<Parameters<typeof reviewFeedback>[0]> = {}) => ({
    wasCorrect: false, expected: null, awarded: 0, box: 1, combo: 0, comboBonus: 0, speedBonus: 0, done: false,
    ...overrides,
  });

  it("zeigt kein Urteil auf einer Anzeigenurstufe (ShowBoth) - selbst wenn wasCorrect zufällig true wäre", () => {
    expect(reviewFeedback(outcome({ wasCorrect: false }), true)).toEqual({ kind: "none" });
    expect(reviewFeedback(outcome({ wasCorrect: true, awarded: 5 }), true)).toEqual({ kind: "none" });
  });

  it("zeigt kein Urteil ohne Outcome (Netzwerkfehler, idempotent weiterlaufend)", () => {
    expect(reviewFeedback(null, false)).toEqual({ kind: "none" });
    expect(reviewFeedback(undefined, false)).toEqual({ kind: "none" });
  });

  it("meldet einen Treffer auf einer normalen (gewerteten) Stufe", () => {
    const result = reviewFeedback(outcome({ wasCorrect: true, awarded: 10, box: 2, combo: 3, comboBonus: 5 }), false);
    expect(result).toEqual({ kind: "correct", awarded: 10, combo: 3, comboBonus: 5, speedBonus: 0, box: 2 });
  });

  it("meldet einen Fehlschlag mit Lösung auf einer normalen Stufe", () => {
    expect(reviewFeedback(outcome({ wasCorrect: false, expected: "hallo" }), false))
      .toEqual({ kind: "wrong", expected: "hallo" });
  });

  it("meldet einen Fehlschlag ohne Lösung (Mengen-Modus: keine Zuordnung möglich)", () => {
    expect(reviewFeedback(outcome({ wasCorrect: false, expected: null }), false))
      .toEqual({ kind: "wrong", expected: null });
  });
});
