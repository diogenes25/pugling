import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ClozePrompt, TaskAnnouncer } from "./ClozePrompt";
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

  // B-486, Testmodus des Vaters: rot, wenn die gewählte Antwort nicht in ihrer Lücke erscheint (wieder „…").
  it("zeigt eine gewählte Antwort in ihrer Lücke", () => {
    render(<ClozePrompt text={text} gapIndex={2} filled={{ 1: "Hello" }} />);

    expect(screen.getByLabelText("Lücke 1: Hello").textContent).toBe("Hello");
    expect(screen.getByLabelText("gesuchte Lücke").textContent).toBe("?");
  });

  // B-486: rot, wenn ein Tipp auf die Lücke nicht zu ihr springt (Handler fehlt oder meldet die falsche Nummer).
  it("meldet beim Antippen einer Lücke deren Nummer", () => {
    const hits: number[] = [];
    render(<ClozePrompt text={text} gapIndex={2} onGap={(g) => hits.push(g)} />);

    fireEvent.click(screen.getByRole("button", { name: "Lücke 1" }));
    expect(hits).toEqual([1]);
    // Die gefragte trägt `aria-current`, sonst sagt der Knopf nur „gesuchte Lücke" ohne Zustand.
    expect(screen.getByRole("button", { name: "gesuchte Lücke" }).getAttribute("aria-current")).toBe("true");
  });

  // Rot, wenn die Kind-Ansicht (ohne `onGap`) plötzlich Knöpfe bekäme – dort gibt es kein Ziel zum Hinspringen.
  it("bleibt ohne onGap bei Bildern statt Knöpfen", () => {
    render(<ClozePrompt text={text} gapIndex={2} filled={{ 1: "Hello" }} />);

    expect(screen.queryAllByRole("button")).toHaveLength(0);
  });

  it("zerlegt den Text in Stücke und Lücken", () => {
    expect(clozeParts("a {{1}} b")).toEqual([{ text: "a " }, { gap: 1 }, { text: " b" }]);
    // Eine Lücke ganz am Anfang erzeugt kein leeres Textstück davor.
    expect(clozeParts("{{3}}!")).toEqual([{ gap: 3 }, { text: "!" }]);
  });
});

/**
 * B-286: Nach dem Kartenwechsel behält die Eingabe den Fokus – ein Screenreader hört die neue Aufgabe nur über diese
 * Region. Sie spricht die Lücken wie `ClozePrompt` sie benennt, nicht als `{{n}}`.
 */
describe("TaskAnnouncer", () => {
  it("sagt die Aufgabe mit benannten Lücken an", () => {
    render(<TaskAnnouncer text="A: {{1}}, how are you? B: I'm {{2}}." gapIndex={2} />);

    expect(screen.getByRole("status").textContent).toBe("Neue Aufgabe: A: Lücke 1, how are you? B: I'm gesuchte Lücke.");
  });

  // Dieselbe Rückfall-Regel wie `ClozePrompt`: fehlt die gefragte Lücke im Text, gilt der unveränderte Text.
  it("fällt wie die Anzeige auf den rohen Text zurück", () => {
    render(<TaskAnnouncer text="Le {{1}} est grand." gapIndex={5} />);

    expect(screen.getByRole("status").textContent).toBe("Neue Aufgabe: Le {{1}} est grand.");
  });

  // B-134: die Region steht auch ohne Aufgabe im DOM (Hörstufe ohne Text), damit die nächste Ansage hineinwachsen kann.
  it("steht ohne Text leer im DOM", () => {
    render(<TaskAnnouncer text={null} gapIndex={null} />);

    expect(screen.getByRole("status").textContent).toBe("");
  });
});
