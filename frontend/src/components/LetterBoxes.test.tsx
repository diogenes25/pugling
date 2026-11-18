import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { LetterBoxes } from "./LetterBoxes";
import { SonderzeichenLeiste } from "./Sonderzeichen";

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
    // Beide festen Leerzeichen (Index 2 und 7) sind schon Teil des gemeldeten Werts, ohne dass sie je
    // getippt wurden - und der Wert ist STELLENGETREU: jede noch leere tippbare Stelle trägt ein
    // Leerzeichen bei, damit `value[i]` dasselbe Kästchen bezeichnet wie `i`.
    expect(onChange).toHaveBeenCalledWith(`t${" ".repeat(LENGTH - 1)}`);
  });

  // Der Wert muss stellengetreu bleiben, sonst rutscht das feste Maskenzeichen in ein tippbares Kästchen:
  // ein `join("")` über die Rohwerte überspringt leere Stellen, feste Zeichen tragen aber immer bei.
  it("laesst das feste Leerzeichen nicht in ein tippbares Kaestchen rutschen", () => {
    // Maske eines zweiteiligen Worts: Index 5 ist das feste Leerzeichen, alles andere ist zu tippen.
    const onChange = vi.fn();
    const { rerender } = render(<LetterBoxes length={9} value="" onChange={onChange} pattern="_____ ___" />);

    fireEvent.change(screen.getByLabelText("Buchstabe 1 von 9"), { target: { value: "g" } });
    const gemeldet = onChange.mock.calls[0][0] as string;
    expect(gemeldet[5]).toBe(" ");

    // Mit diesem Wert neu gerendert bleibt das zweite Kästchen leer (nicht das Leerzeichen von Index 5),
    // ist also wegen `maxLength={1}` weiter beschreibbar.
    rerender(<LetterBoxes length={9} value={gemeldet} onChange={onChange} pattern="_____ ___" />);
    expect(screen.getByLabelText<HTMLInputElement>("Buchstabe 2 von 9").value).toBe("");

    // Und eine übersprungene Stelle verschiebt die späteren Buchstaben nicht: das 7. Kästchen schreibt an
    // Stelle 6, nicht dorthin, wo ein kollabierter String es hinlegen würde.
    fireEvent.change(screen.getByLabelText("Buchstabe 7 von 9"), { target: { value: "t" } });
    const aufrufe = onChange.mock.calls;
    const zweiter = aufrufe[aufrufe.length - 1][0] as string;
    expect(zweiter[0]).toBe("g");
    expect(zweiter[5]).toBe(" ");
    expect(zweiter[6]).toBe("t");
  });

  it("ohne pattern verhaelt sich die Komponente wie zuvor (jedes Feld tippbar)", () => {
    const onChange = vi.fn();
    render(<LetterBoxes length={3} value="" onChange={onChange} />);
    expect(screen.getAllByRole("textbox")).toHaveLength(3);
    fireEvent.change(screen.getByLabelText("Buchstabe 1 von 3"), { target: { value: "a" } });
    expect(document.activeElement).toBe(screen.getByLabelText("Buchstabe 2 von 3"));
  });
});

describe("LetterBoxes – Sonderzeichen (B-661)", () => {
  // Rot, wenn das Zeichen nicht im fokussierten Kästchen landet oder nicht weiterspringt.
  it("setzt ins fokussierte Kästchen ein und springt weiter", () => {
    const onChange = vi.fn();
    render(<LetterBoxes length={3} value="" onChange={onChange} sonderzeichen={["î"]} />);
    fireEvent.focus(screen.getByLabelText("Buchstabe 2 von 3"));
    fireEvent.click(screen.getByRole("button", { name: "î einsetzen" }));
    expect(onChange).toHaveBeenLastCalledWith(" î ");
  });

  // Review B-661: die Kästchen überleben einen Kartenwechsel. Rot, wenn ein gemerktes Kästchen der längeren Vorkarte
  // den Druck auf der kürzeren stumm verschluckt, statt ins erste leere zu setzen.
  it("setzt nach einem Wechsel auf eine kürzere Karte ins erste leere Kästchen", () => {
    const onChange = vi.fn();
    const { rerender } = render(<LetterBoxes length={5} value="" onChange={onChange} sonderzeichen={["î"]} />);
    fireEvent.focus(screen.getByLabelText("Buchstabe 5 von 5"));
    rerender(<LetterBoxes length={2} value="" onChange={onChange} sonderzeichen={["î"]} />);
    fireEvent.click(screen.getByRole("button", { name: "î einsetzen" }));
    expect(onChange).toHaveBeenLastCalledWith("î ");
  });
});

describe("SonderzeichenLeiste", () => {
  // Rot, wenn ein Knopf das Fokussieren nicht mehr abfängt – am Handy klappte dann nach jedem Zeichen die Tastatur zu.
  // Die E2E kann das nicht sehen: `einsetzen` fokussiert das Feld danach ohnehin wieder.
  it("nimmt dem Feld beim Antippen nicht den Fokus", () => {
    render(<SonderzeichenLeiste zeichen={["î"]} onZeichen={vi.fn()} />);
    const knopf = screen.getByRole("button", { name: "î einsetzen" });
    expect(fireEvent.pointerDown(knopf)).toBe(false);
    expect(fireEvent.mouseDown(knopf)).toBe(false);
  });
});
