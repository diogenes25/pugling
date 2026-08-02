import { describe, expect, it } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { ClozeForm, listsForSave } from "./ClozeTexts";
import type { ClozeResponse } from "../lib/types";

/*
 * Der Regressionstest zu B-69. Das Formular hängt an keinem Netzaufruf, solange nicht abgesendet wird –
 * darum ist das hier ein Baustein-Test und kein nachgebauter Bildschirm (siehe frontend/CLAUDE.md).
 *
 * Der Fehler, den er festhält: Die Lücken-Alternativen lagen als zerlegtes Array im State, und der
 * Feldwert wurde daraus bei jedem Tastendruck neu zusammengesetzt. Ein gerade getipptes Komma war damit
 * sofort wieder weg – eine zweite Alternative ließ sich nicht eintippen, nur einfügen.
 */

const TEXT = "Good {{1}}, how are you?";

function cloze(gaps: ClozeResponse["gaps"]): ClozeResponse {
  return {
    id: 1, key: "cz_test", title: "Test", sourceLanguage: "en", targetLanguage: "de",
    text: TEXT, translation: null, gaps, wordBank: null, createdAt: "2026-01-01T00:00:00Z",
  };
}

describe("ClozeForm – gleichwertige Lösungen", () => {
  it("nimmt eine zweite Alternative an, die getippt wird", () => {
    render(<ClozeForm existing={cloze([{ index: 1, answer: "morning" }])} onDone={() => {}} />);

    fireEvent.click(screen.getByRole("button", { name: "+ Auch richtig (Lücke 1)" }));
    fireEvent.change(screen.getByLabelText("Auch richtig 1 (Lücke 1)"), { target: { value: "morgens" } });
    fireEvent.click(screen.getByRole("button", { name: "+ Auch richtig (Lücke 1)" }));
    fireEvent.change(screen.getByLabelText("Auch richtig 2 (Lücke 1)"), { target: { value: "früh" } });

    expect((screen.getByLabelText("Auch richtig 1 (Lücke 1)") as HTMLInputElement).value).toBe("morgens");
    expect((screen.getByLabelText("Auch richtig 2 (Lücke 1)") as HTMLInputElement).value).toBe("früh");
  });

  /*
   * Der Nachweis für „tippbar" steckt im Fall darüber: zwei Felder, in jedes ein Wert. Dieser Fall hier
   * deckt den EINFÜGE-Pfad ab – einen ganzen Wert in einem Schritt. Der kam auch am alten Sammelfeld
   * durch; er scheitert erst, wenn jemand die Zerlegung zurückholt.
   */
  it("hält einen Wert, der selbst ein Komma enthält, zusammen", () => {
    render(<ClozeForm existing={cloze([{ index: 1, answer: "morning" }])} onDone={() => {}} />);

    fireEvent.click(screen.getByRole("button", { name: "+ Auch richtig (Lücke 1)" }));
    fireEvent.change(screen.getByLabelText("Auch richtig 1 (Lücke 1)"),
      { target: { value: "morgens, ganz früh" } });

    expect((screen.getByLabelText("Auch richtig 1 (Lücke 1)") as HTMLInputElement).value)
      .toBe("morgens, ganz früh");
    // Und nicht als zwei Felder – genau das war der Fehler.
    expect(screen.queryByLabelText("Auch richtig 2 (Lücke 1)")).toBeNull();
  });

  it("zeigt gespeicherte Alternativen je in einem eigenen Feld", () => {
    render(<ClozeForm existing={cloze([{ index: 1, answer: "morning", alternatives: ["morgens", "früh"] }])}
      onDone={() => {}} />);

    expect((screen.getByLabelText("Auch richtig 1 (Lücke 1)") as HTMLInputElement).value).toBe("morgens");
    expect((screen.getByLabelText("Auch richtig 2 (Lücke 1)") as HTMLInputElement).value).toBe("früh");
  });

  it("unterscheidet die Felder zweier Lücken über den scope", () => {
    render(<ClozeForm existing={cloze([
      { index: 1, answer: "morning", alternatives: ["morgens"] },
      { index: 2, answer: "are", alternatives: ["bist"] },
    ])} onDone={() => {}} />);

    expect((screen.getByLabelText("Auch richtig 1 (Lücke 1)") as HTMLInputElement).value).toBe("morgens");
    expect((screen.getByLabelText("Auch richtig 1 (Lücke 2)") as HTMLInputElement).value).toBe("bist");
  });

  it("führt den Wortpool ebenfalls als Einzelfelder", () => {
    render(<ClozeForm existing={{ ...cloze([{ index: 1, answer: "morning" }]), wordBank: ["morning", "evening"] }}
      onDone={() => {}} />);

    expect((screen.getByLabelText("Wort 1") as HTMLInputElement).value).toBe("morning");
    expect((screen.getByLabelText("Wort 2") as HTMLInputElement).value).toBe("evening");
  });
});

/*
 * Der Sendeweg als reine Funktion – der Formular-Test kommt nicht daran, weil Vitest hier kein `fetch`
 * fälschen soll. Geprüft wird vor allem der `clear`-Schalter: Ohne ihn meldet ein geräumtes Feld
 * „Gespeichert." und der alte Wortpool steht weiter da (`null` heißt serverseitig „nicht angegeben").
 */
describe("listsForSave", () => {
  it("räumt leere und ungetrimmte Werte weg", () => {
    const { gaps } = listsForSave([{ index: 1, answer: "a", alternatives: ["", "  ", " b "] }], []);
    expect(gaps[0].alternatives).toEqual(["b"]);
  });

  it("macht aus „keine Alternativen“ ein null, nicht eine leere Liste", () => {
    const { gaps } = listsForSave([{ index: 1, answer: "a", alternatives: [] }], []);
    expect(gaps[0].alternatives).toBeNull();
  });

  it("setzt den Leeren-Schalter genau dann, wenn der Wortpool leer ist", () => {
    expect(listsForSave([], []).clearWordBank).toBe(true);
    expect(listsForSave([], ["  "]).clearWordBank).toBe(true);
    expect(listsForSave([], ["morning"]).clearWordBank).toBe(false);
  });

  it("schickt den gefüllten Wortpool getrimmt und den leeren als `null`", () => {
    expect(listsForSave([], [" morning ", ""]).wordBank).toEqual(["morning"]);
    expect(listsForSave([], []).wordBank).toBeNull();
  });
});
