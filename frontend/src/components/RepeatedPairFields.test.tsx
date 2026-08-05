import { fireEvent, render, screen } from "@testing-library/react";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import { RepeatedPairFields, nonEmptyPairs, type Pair } from "./RepeatedPairFields";

/**
 * Das Wiederhol-Paarfeld ersetzt das kommagetrennte Sammelfeld der Birkenbihl-Dekodierung (B-72) - und der
 * Test hält genau den Grund fest: ein Wort oder eine Glosse **mit Komma oder Doppelpunkt** muss unverändert
 * wieder herauskommen. Das Sammelfeld zerriss beide Zeichen beim Senden lautlos in zusätzliche Paare.
 */
function Harness({ start = [] as Pair[] }) {
  const [pairs, setPairs] = useState<Pair[]>(start);
  return (
    <>
      <RepeatedPairFields wordLabel="Wort" glossLabel="wörtlich" pairs={pairs} onChange={setPairs} />
      <output data-testid="out">{JSON.stringify(nonEmptyPairs(pairs))}</output>
    </>
  );
}

describe("RepeatedPairFields", () => {
  it("legt Wortpaare an, tippt sie einzeln und entfernt sie wieder", () => {
    render(<Harness />);

    fireEvent.click(screen.getByRole("button", { name: "+ Wort" }));
    fireEvent.change(screen.getByLabelText("Wort 1"), { target: { value: "is" } });
    fireEvent.change(screen.getByLabelText("wörtlich 1"), { target: { value: "ist" } });
    expect(screen.getByTestId("out").textContent).toBe('[{"learningWord":"is","gloss":"ist"}]');

    fireEvent.click(screen.getByRole("button", { name: "+ Wort" }));
    fireEvent.change(screen.getByLabelText("Wort 2"), { target: { value: "about" } });
    fireEvent.change(screen.getByLabelText("wörtlich 2"), { target: { value: "im Begriff" } });
    expect(screen.getByTestId("out").textContent)
      .toBe('[{"learningWord":"is","gloss":"ist"},{"learningWord":"about","gloss":"im Begriff"}]');

    fireEvent.click(screen.getByRole("button", { name: "Wort 1 entfernen" }));
    // Das ZWEITE Paar bleibt stehen und rutscht auf Position 1 - entfernt wird das Paar, nicht der Platz.
    expect(screen.getByLabelText("Wort 1")).toHaveProperty("value", "about");
    expect(screen.getByTestId("out").textContent).toBe('[{"learningWord":"about","gloss":"im Begriff"}]');
  });

  it("trägt ein Wort/eine Glosse mit Komma UND Doppelpunkt unverändert", () => {
    render(<Harness start={[{ word: "is:about", gloss: "ist:im Begriff zu, gerade" }]} />);

    expect(screen.getByLabelText("Wort 1")).toHaveProperty("value", "is:about");
    expect(screen.getByLabelText("wörtlich 1")).toHaveProperty("value", "ist:im Begriff zu, gerade");
    // EIN Paar, nicht mehrere: das ist der Fehler des Komma-/Doppelpunkt-Sammelfelds.
    expect(screen.getByTestId("out").textContent)
      .toBe('[{"learningWord":"is:about","gloss":"ist:im Begriff zu, gerade"}]');
  });

  it("sendet keine Zeile mit leerem Wort - auch wenn die Glosse gefüllt ist", () => {
    render(<Harness start={[{ word: "  ", gloss: "ist" }]} />);
    expect(screen.getByTestId("out").textContent).toBe("[]");
  });

  it("setzt beim Anlegen den Fokus auf das neue Wort-Feld", () => {
    render(<Harness start={[{ word: "is", gloss: "ist" }]} />);

    fireEvent.click(screen.getByRole("button", { name: "+ Wort" }));
    expect(document.activeElement).toBe(screen.getByLabelText("Wort 2"));
  });

  /*
   * Birkenbihl steckt beim Anlegen in einem echten <form> (VaterExerciseCreate.tsx) - anders als der
   * (unerreichbare) Leseweg ist dieser Schreibweg live. Ohne den Schutz sendete Enter das Formular ab,
   * bevor die restlichen Wortpaare getippt sind - dasselbe Muster wie in `RepeatedTextFields`.
   */
  it("legt per Enter das nächste Wortpaar an, statt ein umgebendes Formular abzusenden", () => {
    render(<Harness start={[{ word: "is", gloss: "ist" }]} />);

    fireEvent.keyDown(screen.getByLabelText("Wort 1"), { key: "Enter" });
    const zweitesWort = screen.getByLabelText("Wort 2");
    expect(document.activeElement).toBe(zweitesWort);

    // Aus einer Zeile OHNE Wort heraus wächst die Liste nicht - sonst stapelt Enter stumm leere Zeilen.
    fireEvent.keyDown(zweitesWort, { key: "Enter" });
    expect(screen.queryByLabelText("Wort 3")).toBeNull();
  });

  it("unterscheidet mehrere Instanzen über `scope`", () => {
    render(
      <>
        <RepeatedPairFields wordLabel="Wort" glossLabel="wörtlich" scope="Satz 1"
          pairs={[{ word: "is", gloss: "ist" }]} onChange={() => {}} />
        <RepeatedPairFields wordLabel="Wort" glossLabel="wörtlich" scope="Satz 2"
          pairs={[{ word: "was", gloss: "war" }]} onChange={() => {}} />
      </>,
    );

    expect(screen.getByLabelText("Wort 1 (Satz 1)")).toHaveProperty("value", "is");
    expect(screen.getByLabelText("Wort 1 (Satz 2)")).toHaveProperty("value", "was");
    expect(screen.getByRole("button", { name: "Wort 1 (Satz 2) entfernen" })).toBeDefined();
  });
});
