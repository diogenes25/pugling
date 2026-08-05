import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { LetterBoxes } from "./LetterBoxes";

/**
 * Das Buchstabenkästchen mit Trennzeichen-Maske (B-66): Leer-/Satzzeichen stehen laut `pattern` schon fest
 * und dürfen weder eingebbar sein noch den Sprung zwischen den Feldern unterbrechen - auch nicht, wenn
 * mehrere feste Felder aufeinanderfolgen.
 */
describe("LetterBoxes", () => {
  // "to grow up" → Maske "__ ____ __": Felder 2 und 7 (Index) sind Leerzeichen, fest.
  const PATTERN = "__ ____ __";
  const LENGTH = PATTERN.length;

  it("rendert feste Stellen als Text, nicht als Eingabefeld", () => {
    render(<LetterBoxes length={LENGTH} value="" onChange={vi.fn()} pattern={PATTERN} />);

    // Zwei Leerzeichen-Stellen (Index 2 und 7) sind keine Inputs mit Buchstaben-Label.
    expect(screen.queryByLabelText("Buchstabe 3 von 10")).toBeNull();
    expect(screen.queryByLabelText("Buchstabe 8 von 10")).toBeNull();
    // Alle anderen acht Stellen bleiben tippbare Felder.
    expect(screen.getAllByRole("textbox")).toHaveLength(8);
  });

  it("springt beim Tippen über eine feste Stelle hinweg vorwärts", () => {
    render(<LetterBoxes length={LENGTH} value="" onChange={vi.fn()} pattern={PATTERN} />);

    // Feld 2 (Index 1, "o" von "to") ist das letzte tippbare vor dem festen Leerzeichen bei Index 2.
    fireEvent.change(screen.getByLabelText("Buchstabe 2 von 10"), { target: { value: "o" } });
    // Der Fokus überspringt das feste Feld 3 und landet auf dem nächsten tippbaren, Feld 4 ("g").
    expect(document.activeElement).toBe(screen.getByLabelText("Buchstabe 4 von 10"));
  });

  it("springt bei Backspace über mehrere aufeinanderfolgende feste Stellen rückwärts", () => {
    // Maske mit ZWEI direkt aufeinanderfolgenden festen Zeichen (Index 2 "-" und Index 3 " ") vor dem
    // letzten Buchstaben (Index 4).
    render(<LetterBoxes length={5} value="" onChange={vi.fn()} pattern="__- _" />);

    const letzterBuchstabe = screen.getByLabelText("Buchstabe 5 von 5");
    letzterBuchstabe.focus();
    // Backspace auf einem LEEREN Feld springt zurück - über BEIDE festen Felder hinweg, nicht nur eines.
    fireEvent.keyDown(letzterBuchstabe, { key: "Backspace" });
    expect(document.activeElement).toBe(screen.getByLabelText("Buchstabe 2 von 5"));
  });

  it("komponiert feste Zeichen automatisch in den gemeldeten Wert mit ein", () => {
    const onChange = vi.fn();
    render(<LetterBoxes length={LENGTH} value="" onChange={onChange} pattern={PATTERN} />);

    fireEvent.change(screen.getByLabelText("Buchstabe 1 von 10"), { target: { value: "t" } });
    // Beide festen Leerzeichen (Index 2 und 7) sind schon Teil des gemeldeten Werts - "t" plus die zwei
    // festen Leerzeichen, die noch leeren tippbaren Stellen tragen nichts zum String bei -, ohne dass sie
    // je getippt wurden.
    expect(onChange).toHaveBeenCalledWith("t  ");
  });

  it("ohne pattern verhaelt sich die Komponente wie zuvor (jedes Feld tippbar)", () => {
    const onChange = vi.fn();
    render(<LetterBoxes length={3} value="" onChange={onChange} />);
    expect(screen.getAllByRole("textbox")).toHaveLength(3);
    fireEvent.change(screen.getByLabelText("Buchstabe 1 von 3"), { target: { value: "a" } });
    expect(document.activeElement).toBe(screen.getByLabelText("Buchstabe 2 von 3"));
  });
});
