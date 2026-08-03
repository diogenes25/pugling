import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ClozePrompt } from "./ClozePrompt";
import { clozeParts } from "../lib/cloze";

/**
 * B-76: Zwei Lücken desselben Textes ergaben zwei zeichengleiche Karten. Der Server nennt jetzt die
 * gefragte Lücke, und hier wird geprüft, dass sie auch **sichtbar** anders ist – sonst wäre die
 * Reparatur auf halbem Weg stehen geblieben.
 */
describe("ClozePrompt", () => {
  const text = "A: {{1}}, how are you? B: I'm {{2}}, thank you.";

  it("hebt die gefragte Lücke hervor und lässt die übrigen neutral", () => {
    render(<ClozePrompt text={text} gapIndex={2} />);

    // Genau eine ist die gefragte - sonst hätte das Kind wieder die Wahl. Die Zählung steht vorn:
    // `getByLabelText` würde bei Mehrfachtreffer schon werfen und die Zusicherung damit verdecken.
    expect(screen.queryAllByLabelText("gesuchte Lücke")).toHaveLength(1);
    expect(screen.getByLabelText("gesuchte Lücke").textContent).toBe("?");
    expect(screen.getByLabelText("Lücke 1").textContent).toBe("…");
  });

  it("benennt die Lücken über eine Rolle, die Benennung erlaubt", () => {
    render(<ClozePrompt text={text} gapIndex={1} />);

    // Auf `role="generic"` (ein nacktes <span>) ist `aria-label` unzulässig; die Hilfstechnik verwürfe es,
    // während der Test es über die Namensberechnung trotzdem fände. Darum die Rolle festnageln.
    expect(screen.getAllByRole("img")).toHaveLength(2);
  });

  it("zeigt die rohe Vorlagensyntax nirgends", () => {
    const { container } = render(<ClozePrompt text={text} gapIndex={1} />);

    expect(container.textContent).not.toContain("{{");
    expect(container.textContent).toContain("how are you?");
  });

  it("zeigt ohne Lückennummer schlicht den Text – jeder andere Übungstyp", () => {
    const { container } = render(<ClozePrompt text="to run – laufen" />);

    expect(container.textContent).toBe("to run – laufen");
    expect(screen.queryByLabelText("gesuchte Lücke")).toBeNull();
    // Ohne Lücke darf die Lückentext-Typografie nicht greifen, sonst schrumpft jede Vokabelkarte mit.
    expect(container.querySelector(".cloze-prompt")).toBeNull();
    expect(container.querySelector(".word")).not.toBeNull();
  });

  it("fällt auf den unveränderten Text zurück, wenn der Platzhalter fehlt (Altbestand)", () => {
    // Der Editor verhindert das beim Anlegen; für längst gespeicherte Übungen gilt das nicht.
    const { container } = render(<ClozePrompt text="Ein Satz ganz ohne Lücke." gapIndex={1} />);

    expect(container.textContent).toBe("Ein Satz ganz ohne Lücke.");
  });

  it("fällt auch zurück, wenn die gefragte Lücke im Text nicht vorkommt", () => {
    // Der gefährlichere Altbestands-Fall: Platzhalter da, aber umnummeriert. Alle Lücken neutral zu
    // zeichnen wäre schlimmer als die rohe Vorlage – dann rät das Kind ohne jeden Anhalt.
    const { container } = render(<ClozePrompt text={text} gapIndex={7} />);

    expect(container.textContent).toBe(text);
    expect(screen.queryByLabelText("gesuchte Lücke")).toBeNull();
  });

  it("hebt einen doppelt gesetzten Platzhalter an beiden Stellen hervor", () => {
    // `{{1}}` darf zweimal im Text stehen – `placeholderIndices` dedupliziert bewusst. Beide Stellen
    // meinen dieselbe Lücke, also werden auch beide markiert.
    render(<ClozePrompt text="{{1}} und nochmal {{1}}" gapIndex={1} />);

    expect(screen.getAllByLabelText("gesuchte Lücke")).toHaveLength(2);
  });

  it("rendert nichts, wenn der Server keinen Text schickt", () => {
    // Vokabel-Hörstufe (B-75/E3): dort verschweigt der Server das Wort. Ein leerer Kasten wäre schlimmer
    // als keiner - er sähe aus wie ein Ladefehler.
    const { container } = render(<ClozePrompt text={null} />);

    expect(container.firstChild).toBeNull();
  });

  it("zerlegt den Text in Stücke und Lücken", () => {
    expect(clozeParts("a {{1}} b")).toEqual([{ text: "a " }, { gap: 1 }, { text: " b" }]);
    // Eine Lücke ganz am Anfang erzeugt kein leeres Textstück davor.
    expect(clozeParts("{{3}}!")).toEqual([{ gap: 3 }, { text: "!" }]);
  });
});
