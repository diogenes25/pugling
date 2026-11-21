import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { TestResult } from "./SohnTest";
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
    render(<TestResult result={result} onHome={() => {}} onRetry={() => {}} onPractice={() => {}} />);

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
   * B-524: Ein Schreibfehler bleibt falsch gewertet, aber der Schirm sagt, dass das Wort da war und nur die
   * Schreibweise fehlte – samt dem, was das Kind getippt hat. Rot, wenn `misspelled` nicht mehr gelesen wird.
   */
  it("weist einen Schreibfehler als eigene Kategorie aus, mit der getippten Antwort", () => {
    const items: ItemOutcome[] = [
      { itemIndex: 0, prompt: "The {{1}} opens at eight. {{2}} is high.", expected: "clinic", givenAnswer: "clinik",
        wasCorrect: false, gapIndex: 1, misspelled: true },
      { itemIndex: 1, prompt: "The {{1}} opens at eight. {{2}} is high.", expected: "height", givenAnswer: "dog",
        wasCorrect: false, gapIndex: 2, misspelled: false },
    ];
    const { container } = view({ ...base, totalItems: 2, correctItems: 0, scorePercent: 0, items });

    expect(screen.getByText("Du: clinik · Falsch geschrieben")).toBeTruthy();
    // Das falsche Wort bekommt keine Schreibfehler-Kennung, aber seine Antwort steht da – das Kind soll sehen, was es
    // geschrieben hat. Rot, wenn `misspelled` an der falschen Zeile hängt oder die Antwort fehlt.
    expect(screen.getByText("Du: dog")).toBeTruthy();
    expect(container.textContent?.match(/Falsch geschrieben/g)).toHaveLength(2); // sichtbar + Vorleser, nur Zeile 1
  });

  /**
   * Jede Lückentext-Zeile trug den ganzen Absatz – bei zehn Aufgaben zehnmal, und die Antworten gingen darin unter.
   * Rot, wenn die Zeile wieder den ganzen Text zeigt (dann stünde „We aren't late" zweimal da), wenn die eigene
   * Antwort fehlt oder auch an der richtigen Zeile steht.
   */
  it("zeigt je Lücke nur ihren Satz, die falsche Antwort und die Lösung", () => {
    const text = "1. Your sister plays the drums, {{1}} 2. We aren't late, {{2}}";
    const items: ItemOutcome[] = [
      { itemIndex: 0, prompt: text, expected: "doesn't she?", givenAnswer: "doesn't she?", wasCorrect: true, gapIndex: 1 },
      { itemIndex: 1, prompt: text, expected: "are we?", givenAnswer: "aren't we?", wasCorrect: false, gapIndex: 2 },
    ];
    view({ ...base, totalItems: 2, correctItems: 1, scorePercent: 50, items });

    expect(screen.getAllByText(/We aren't late/)).toHaveLength(1);
    expect(screen.getByText("Du: aren't we?")).toBeTruthy();
    expect(screen.queryByText("Du: doesn't she?")).toBeNull();
    expect(screen.getByText("Richtig: are we?")).toBeTruthy();
    expect(screen.getByText("Richtig: doesn't she?")).toBeTruthy();
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

  /**
   * B-430 (c): Nach einem verpatzten Test führt ein Knopf zum Üben – ohne „diese Wörter“ zu versprechen, weil der
   * Test seine Fehler in der Position nicht fällig macht. Je Fall die Änderung, die ihn rot macht: Bedingung
   * umgedreht oder entfernt (Fall 2), an die Restversuche gekoppelt (Fall 3), falscher Rückruf (Fall 1).
   */
  it("bietet nach nicht bestandenem Test „Üben“ an und ruft dafür onPractice", () => {
    const onPractice = vi.fn(); const onRetry = vi.fn();
    render(<TestResult result={{ ...base, items: listItems }} onHome={() => {}} onRetry={onRetry} onPractice={onPractice} />);
    fireEvent.click(screen.getByRole("button", { name: "▶ Üben" }));
    expect(onPractice).toHaveBeenCalledTimes(1);
    expect(onRetry).not.toHaveBeenCalled();
  });

  it("lässt „Üben“ weg, wenn der Test bestanden ist", () => {
    view({ ...base, items: listItems, passed: true, correctItems: 3, scorePercent: 100 });
    expect(screen.queryByRole("button", { name: "▶ Üben" })).toBeNull();
  });

  it("bietet „Üben“ auch an, wenn für heute kein Versuch mehr übrig ist – gerade dann bleibt nur das", () => {
    view({ ...base, items: listItems, attemptsRemaining: 0 });
    expect(screen.getByRole("button", { name: "▶ Üben" })).toBeTruthy();
  });
});
