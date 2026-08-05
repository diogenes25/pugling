import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { TestResult } from "./SohnTest";
import { DEFAULT_SKIN } from "../lib/skins";
import type { ItemOutcome, TestSubmitResponse } from "../lib/types";

/**
 * Der Auswertungsschirm der Klausur (B-77/E2). Bei einer Menge nennt er, was das Kind **vergessen** hat, und
 * darf nicht bei sechzehn Zeilen denselben Auftrag wiederholen: der Aufgabentext steht dann einmal oben, und
 * groß gesetzt wird die Lösung. Dazu die Fehlnennungen – ohne sie verschwände lautlos, was das Kind tippte.
 */
describe("TestResult", () => {
  // Getippt statt per `as` gecastet: der Cast schaltete die Prüfung auf überzählige Felder ab, ein
  // `wrongMention` statt `wrongMentions` wäre also stumm durchgelaufen – dieselbe Fehlerklasse, die bei
  // B-77/R3 nur auffiel, WEIL `contract.ts` generiert ist.
  const base: Omit<TestSubmitResponse, "items" | "wrongMentions"> = {
    attemptId: 1, stage: 4, totalItems: 3, correctItems: 2, scorePercent: 67,
    passed: false, passPercent: 90, attemptsRemaining: 1,
  };
  const view = (result: TestSubmitResponse) =>
    render(<TestResult result={result} skin={DEFAULT_SKIN} onHome={() => {}} onRetry={() => {}} />);

  const listItems: ItemOutcome[] = [
    { itemIndex: 0, prompt: "Nenne alle 16 Bundesländer.", expected: "Bayern", givenAnswer: "Bayern", wasCorrect: true, gapIndex: null },
    { itemIndex: 1, prompt: "Nenne alle 16 Bundesländer.", expected: "Hessen", givenAnswer: "Hessen", wasCorrect: true, gapIndex: null },
    { itemIndex: 2, prompt: "Nenne alle 16 Bundesländer.", expected: "Berlin", givenAnswer: null, wasCorrect: false, gapIndex: null },
  ];

  it("zeigt den geteilten Aufgabentext einmal und die Lösungen je Zeile", () => {
    const { container } = view({ ...base, items: listItems });

    // Einmal, nicht dreimal - sonst schreit der Auftrag und die Lehre flüstert.
    expect(container.textContent?.match(/Nenne alle 16 Bundesländer\./g)).toHaveLength(1);
    expect(screen.getByText("Berlin")).toBeTruthy();
    expect(screen.getByText("Bayern")).toBeTruthy();
  });

  /**
   * Bei einer Menge heißt das Kreuz „nicht genannt", nicht „falsch geantwortet" – und das ist die Lehre der
   * Übung. Allein gelesen wäre „❌ Berlin" für das Kind die Behauptung, Berlin sei die falsche Antwort.
   */
  it("beschriftet die Zeilen mit genannt / nicht genannt statt nur mit einem Zeichen", () => {
    view({ ...base, items: listItems });
    expect(screen.getAllByText("genannt")).toHaveLength(2);
    expect(screen.getByText("nicht genannt")).toBeTruthy();
  });

  it("führt die Fehlnennungen auf – und lässt den Kasten weg, wenn es keine gibt", () => {
    view({ ...base, items: listItems, wrongMentions: ["Wien", "Bayern"] });
    expect(screen.getByText("Das zählte nicht:")).toBeTruthy();
    expect(screen.getByText("Wien")).toBeTruthy();

    const { container } = view({ ...base, items: listItems, wrongMentions: null });
    expect(container.textContent).not.toContain("Das zählte nicht:");
  });

  /**
   * Der Lückentext ist ausgenommen: seine Zeilen teilen den Text zwar auch, unterscheiden sich aber über
   * `gapIndex` – zöge man ihn nach oben, wäre die Reparatur von B-76 auf diesem Schirm wieder weg.
   */
  it("lässt den Lückentext-Zeilen ihren Text, weil die Lücke sie unterscheidet", () => {
    const clozeItems: ItemOutcome[] = [
      { itemIndex: 0, prompt: "A: {{1}}, how are you? B: I'm {{2}}.", expected: "Hello", givenAnswer: "Hello", wasCorrect: true, gapIndex: 1 },
      { itemIndex: 1, prompt: "A: {{1}}, how are you? B: I'm {{2}}.", expected: "fine", givenAnswer: "x", wasCorrect: false, gapIndex: 2 },
    ];
    const { container } = view({ ...base, items: clozeItems });

    expect(screen.queryAllByLabelText("gesuchte Lücke")).toHaveLength(2);
    expect(container.textContent).not.toContain("{{");
  });

  /**
   * Ohne diese Bedingung bot der Knopf einen Versuch an, den der Server längst abgewiesen hätte
   * (ApiErrors.TestAttemptsExhausted, B-62) - ein Klick danach landete nur in der Fehlerbox.
   */
  it("verbirgt den Retry-Knopf, wenn keine Tages-Versuche mehr übrig sind", () => {
    view({ ...base, items: listItems, attemptsRemaining: 0 });
    expect(screen.queryByRole("button", { name: "Nochmal versuchen" })).toBeNull();
  });

  it("zeigt den Retry-Knopf bei verbleibenden Versuchen weiter an", () => {
    view({ ...base, items: listItems, attemptsRemaining: 1 });
    expect(screen.getByRole("button", { name: "Nochmal versuchen" })).toBeTruthy();
  });
});
