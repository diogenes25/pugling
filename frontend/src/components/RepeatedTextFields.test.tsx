import { fireEvent, render, screen } from "@testing-library/react";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import { RepeatedTextFields, nonEmpty } from "./RepeatedTextFields";

/**
 * Das Wiederhol-Feld ersetzt das kommagetrennte Sammelfeld – und der Test hält genau den Grund fest:
 * ein Wert **mit Komma** muss unverändert wieder herauskommen. Das Sammelfeld zerriss ihn beim Senden
 * stillschweigend in zwei, und der Nutzer sah davon nichts.
 */
function Harness({ start = [] as string[] }) {
  const [values, setValues] = useState<string[]>(start);
  return (
    <>
      <RepeatedTextFields label="Variante" values={values} onChange={setValues} />
      {/* Der abgeschickte Stand, sichtbar gemacht – geprüft wird, was das Formular senden würde. */}
      <output data-testid="out">{JSON.stringify(nonEmpty(values) ?? null)}</output>
    </>
  );
}

describe("RepeatedTextFields", () => {
  it("legt Varianten an, tippt sie einzeln und entfernt sie wieder", () => {
    render(<Harness />);

    fireEvent.click(screen.getByRole("button", { name: "+ Variante" }));
    fireEvent.click(screen.getByRole("button", { name: "+ Variante" }));
    fireEvent.change(screen.getByLabelText("Variante 1"), { target: { value: "riesig" } });
    fireEvent.change(screen.getByLabelText("Variante 2"), { target: { value: "enorm" } });
    expect(screen.getByTestId("out").textContent).toBe('["riesig","enorm"]');

    fireEvent.click(screen.getByRole("button", { name: "Variante 1 entfernen" }));
    // Die *zweite* bleibt stehen und rutscht auf Position 1 - entfernt wird der Wert, nicht der Platz.
    expect(screen.getByLabelText("Variante 1")).toHaveProperty("value", "enorm");
    expect(screen.getByTestId("out").textContent).toBe('["enorm"]');
  });

  it("trägt eine Übersetzung mit Komma unverändert", () => {
    render(<Harness start={["groß, wirklich groß"]} />);

    expect(screen.getByLabelText("Variante 1")).toHaveProperty("value", "groß, wirklich groß");
    // EIN Wert, nicht zwei: das ist der Fehler des Komma-Sammelfelds.
    expect(screen.getByTestId("out").textContent).toBe('["groß, wirklich groß"]');
  });

  it("sendet nichts, wenn nur leere Felder stehen", () => {
    render(<Harness start={["  "]} />);

    // `null` statt `[]`: "keine angegeben" hat im Vertrag genau eine Schreibweise.
    expect(screen.getByTestId("out").textContent).toBe("null");
  });

  it("legt per Enter die nächste Variante an und setzt den Fokus hinein", () => {
    render(<Harness start={["riesig"]} />);

    fireEvent.keyDown(screen.getByLabelText("Variante 1"), { key: "Enter" });
    const zweites = screen.getByLabelText("Variante 2");
    // Ohne den Fokussprung passiert für die Tastatur sichtbar nichts.
    expect(document.activeElement).toBe(zweites);

    // Aus einem LEEREN Feld heraus wächst die Liste nicht - sonst stapelt Enter stumm leere Felder.
    fireEvent.keyDown(zweites, { key: "Enter" });
    expect(screen.queryByLabelText("Variante 3")).toBeNull();
  });

  it("nennt den Hinzufügen-Knopf so, wie er dasteht", () => {
    // WCAG 2.5.3: der sichtbare Text muss im zugänglichen Namen stecken, sonst trifft ihn eine
    // Spracheingabe nicht. Ein eigenes `addLabel` muss dabei durchschlagen.
    render(<RepeatedTextFields label="Variante" addLabel="+ Alternative" scope="Zeile 2"
      values={[]} onChange={() => {}} />);

    expect(screen.getByRole("button", { name: "+ Alternative (Zeile 2)" }).textContent).toBe("+ Alternative");
  });

  it("unterscheidet mehrere Instanzen über `scope`", () => {
    // Drei Anlege-Zeilen tragen dreimal dieselbe Komponente. Ohne `scope` hießen alle Felder „Variante 1"
    // und wären für Screenreader wie Test nur über ihre Position auseinanderzuhalten.
    render(
      <>
        <RepeatedTextFields label="Variante" scope="Zeile 1" values={["riesig"]} onChange={() => {}} />
        <RepeatedTextFields label="Variante" scope="Zeile 2" values={["winzig"]} onChange={() => {}} />
      </>,
    );

    expect(screen.getByLabelText("Variante 1 (Zeile 1)")).toHaveProperty("value", "riesig");
    expect(screen.getByLabelText("Variante 1 (Zeile 2)")).toHaveProperty("value", "winzig");
    expect(screen.getByRole("button", { name: "Variante 1 (Zeile 2) entfernen" })).toBeDefined();
  });
});
